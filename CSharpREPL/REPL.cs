using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;

namespace CSharpREPL
{
	class REPL
	{
		ScriptState state;
		ScriptOptions options = ScriptOptions.Default;
		Dictionary<string, Action<string>> Commands;


		public static ScriptOptions DefaultScriptOptions { get; } = ScriptOptions.Default.AddImports("System", "System.IO", "System.Collections.Generic",
			"System.Console", "System.Diagnostics", "System.Dynamic",
			"System.Linq", "System.Linq.Expressions", "System.Text",
			"System.Threading.Tasks")
			.AddReferences("System", "System.Core", "Microsoft.CSharp");

		public REPL()
		{
			options = DefaultScriptOptions;
			ReadLine.AutoCompletionHandler = new AutoCompletion();
			Commands = new Dictionary<string, Action<string>>()
			{
				{ "#help", (s) => ShowHelp()},
				{ "#n", (s) => AddNamespace(s.Replace("#n","").Replace(";","").Replace(" ",""))},
				{ "#r", (s) => AddReference(s.Replace("#r","").Replace(";","").Replace(" ",""))},
				{ "#load", (s) => LoadScript(s.Replace("#load","").Replace(";","").Replace(" ",""))},
			};
		}

		public REPL(List<string> imports, List<string> references) : this()
		{
			options = options.AddImports(imports);
			options = options.AddReferences(imports);
		}

		public REPL(ScriptOptions options) : this()
		{
			this.options = this.options.AddImports(options.Imports);
			this.options = this.options.AddReferences(options.MetadataReferences);
		}

		public async Task Start()
		{
			state = await CSharpScript.RunAsync("", options);
			Console.WriteLine("Enter #help to show help");
			while (true)
			{
				try
				{
					StringBuilder stringBuilder = new StringBuilder();
					string input = ReadLine.Read("> ");
					if (!input.EndsWith(";"))
					{
						stringBuilder.Append(input);
						while ((!stringBuilder.ToString().EndsWith(";") && !stringBuilder.ToString().EndsWith('}')) || (stringBuilder.ToString().Count(c => c == '{') != stringBuilder.ToString().Count(c => c == '}') && stringBuilder.ToString().Count(c => c == '{') > 0))
						{
							input = ReadLine.Read(". ");
							stringBuilder.Append(input);
						}
						input = stringBuilder.ToString();
					}
					var cmd = Commands.Where(x => input.Contains(x.Key));
					if (cmd.Count() > 0)
					{
						cmd.First().Value(input);
					}
					else
						await EvalAsync(input);
				}
				catch (Exception e)
				{
					Console.WriteLine();
					Console.WriteLine(e.Message);
				}
			}
		}

		private void ShowSuggestions(string input)
		{

		}

		private async Task EvalAsync(string code)
		{
			state = await state.ContinueWithAsync(code, options);
			if (state.ReturnValue != null && !(bool)state.ReturnValue?.Equals(""))
				Console.WriteLine(state.ReturnValue);
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
			if(File.Exists(s))
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
				code = File.ReadAllText(s);
			else
				throw new FileNotFoundException("File not found");
			await EvalAsync(code);
		}
	}
}
