using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using NuGet.Common;
using NugetWorker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpREPL
{
    internal class REPL
    {
        private ScriptState state;
        private ScriptOptions options;
        private readonly Dictionary<string, Action<string>> commands;

        private static ScriptOptions DefaultScriptOptions { get; } = ScriptOptions.Default
            .AddImports("System", "System.IO", "System.Collections.Generic",
                "System.Console", "System.Diagnostics", "System.Dynamic",
                "System.Linq", "System.Linq.Expressions", "System.Text",
                "System.Threading.Tasks")
            .AddReferences("System", "System.Core", "Microsoft.CSharp");

        public REPL()
        {
            options = DefaultScriptOptions;
            ReadLine.AutoCompletionHandler = new AutoCompletion();
            commands = new Dictionary<string, Action<string>>
            {
                { "#help", _ => ShowHelp()},
                { "#n", s => AddNamespace(RemoveCommandWithDelimiters(s, "#n"))},
                { "#r", s => AddReference(RemoveCommandWithDelimiters(s, "#r"))},
                { "#load", s => LoadScript(RemoveCommandWithDelimiters(s, "#load"))},
                { "#nuget", s => AddNugetPackage(RemoveCommandWithDelimiters(s, "#nuget"))},
            };
        }



        private string RemoveCommandWithDelimiters(string originString, string command)
        {
            var stringBuilder = new StringBuilder(originString);
            stringBuilder.Replace(command, string.Empty);
            stringBuilder.Replace(";", string.Empty);
            stringBuilder.Replace(" ", string.Empty);
            return stringBuilder.ToString();
        }

        public REPL(List<string> imports) : this()
        {
            options = options.AddImports(imports);
            options = options.AddReferences(imports);
        }
        public REPL(ScriptOptions options) : this()
        {
            this.options = options.AddImports(options.Imports);
            this.options = options.AddReferences(options.MetadataReferences);
        }

        public async Task Start()
        {
            NugetHelper.Instance.logger = new NullLogger();
            state = await CSharpScript.RunAsync("", options);
            Console.WriteLine("Enter #help to show help");
            while (true)
            {
                try
                {
                    var stringBuilder = new StringBuilder();
                    string input = ReadLine.Read("> ");
                    if (!input.EndsWith(';') && !input.StartsWith("#"))
                    {
                        stringBuilder.Append(input);
                        while ((!stringBuilder.ToString().EndsWith(";") && !stringBuilder.ToString().EndsWith('}')) || (stringBuilder.ToString().Count(c => c == '{') != stringBuilder.ToString().Count(c => c == '}') && stringBuilder.ToString().Count(c => c == '{') > 0))
                        {
                            input = ReadLine.Read(". ");
                            stringBuilder.Append(input);
                        }
                        input = stringBuilder.ToString();
                    }
                    var cmd = commands.Where(x => x.Key == input.Split(" ")?[0]);
                    if (cmd.Any())
                    {
                        cmd.First().Value(input);
                    }
                    else
                    {
                        await EvalAsync(input);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine();
                    Console.WriteLine(e.Message);
                }
            }
        }

        private async Task EvalAsync(string code)
        {
            state = await state.ContinueWithAsync(code, options);
            if (string.IsNullOrEmpty(state.ReturnValue as string))
            {
                Console.WriteLine(state.ReturnValue);
            }
        }

        private void ShowHelp()
        {
            Console.WriteLine
            (
                "\"#n <namespace name>\" to add namespace \n" +
                "\"#r <Path to assembly>\" or \"#r <assembly name>\" to add reference to an assembly \n" +
                "\"#load <Path to script>\" to load c# script \n" +
                "\"#nuget <Package Name> <Package Version>\" to download nuget package\n"
            );
        }

        private void AddReference(string s)
        {
            if (File.Exists(s))
            {
                MetadataReference.CreateFromFile(s);
            }
            options = options.AddReferences(s);
        }

        private void AddNamespace(string s)
        {
            options = options.AddImports(s);
        }
        private async void LoadScript(string path)
        {
            string[] data;
            try
            {
                if (File.Exists(path))
                {
                    data = await File.ReadAllLinesAsync(path);
                }
                else
                {
                    throw new FileNotFoundException("File not found");
                }
                var groupedData = data.GroupBy(x => commands.ContainsKey(x.Split(" ")?[0]) ? "cmd" : "code").ToDictionary(g => g.Key, g => g.ToList());

                foreach (var cmd in groupedData["cmd"])
                    commands[cmd.Split(" ").First()](cmd.Split(" ").TakeLast(1).First());
                await EvalAsync(string.Join("", groupedData["code"]));
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine(e.Message);
            }
        }
        private async void AddNugetPackage(string s)
        {
            var args = s.Split(" ");
            string packageName = args[0];
            string version = args.Length > 1 ? args[1] : "";
            string downloadDir = Path.GetFullPath(NugetHelper.Instance.GetNugetSettings().NugetFolder);
            if (!Directory.Exists(downloadDir))
                Directory.CreateDirectory(downloadDir);
            if (Directory.GetDirectories(downloadDir, packageName + ".*").Length < 1)
            {
                NugetEngine nugetEngine = new NugetEngine();
                Console.WriteLine($"Downloading nuget package {packageName} {version}...");
                await nugetEngine.GetPackage(packageName, version);
                Console.WriteLine($"{packageName} {version} is downloaded to {downloadDir}");
            }
            else 
            {
                Console.WriteLine($"Package is already exists in {downloadDir}");
            }
        }
    }
}

