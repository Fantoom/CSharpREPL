using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
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
            state = await CSharpScript.RunAsync("", options);
            Console.WriteLine("Enter #help to show help");
            while (true)
            {
                try
                {
                    var stringBuilder = new StringBuilder();
                    string input = ReadLine.Read("> ");
                    if (!input.EndsWith(';'))
                    {
                        stringBuilder.Append(input);
                        while ((!stringBuilder.ToString().EndsWith(";") && !stringBuilder.ToString().EndsWith('}')) || (stringBuilder.ToString().Count(c => c == '{') != stringBuilder.ToString().Count(c => c == '}') && stringBuilder.ToString().Count(c => c == '{') > 0))
                        {
                            input = ReadLine.Read(". ");
                            stringBuilder.Append(input);
                        }
                        input = stringBuilder.ToString();
                    }
                    var cmd = commands
                        .Where(x => input.Contains(x.Key));
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
                "\"#load <Path to script>\" to load c# script \n"
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
        private async void LoadScript(string s)
        {
            string code;
            if (File.Exists(s))
            {
                code = await File.ReadAllTextAsync(s);
            }
            else
            {
                throw new FileNotFoundException("File not found");
            }

            await EvalAsync(code);
        }
    }
}
