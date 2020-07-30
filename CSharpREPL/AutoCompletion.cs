using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Linq;

namespace CSharpREPL
{
	class AutoCompletion : IAutoCompleteHandler
	{

		public char[] Separators { get; set; } = new char[] { ' ', '.'};

		public string[] GetSuggestions(string text, int index)
		{
			if (text.LastIndexOf('.') > 0 && (index - text.LastIndexOf('.') < 1 || index - text.LastIndexOf('.') > -1))
			{
				string end = text.Split(".").Last();
				string input;
				int i = text.LastIndexOf('.') - 1;
				input = text.Remove(text.LastIndexOf('.')).Trim();
				while (!(new List<string> { "{", "(" }.Contains(text[i].ToString())) && i > 0)
				{
					i--;
				}
				if(i >= 0)
					input = input.Remove(0,i+1);

				var type = GetType(input);
				return type.GetMembers().Select(x => x.Name).Where(x => x.Contains(end)).ToArray();
			}
			else 
			{
				return null;
			}
			//return new string[] { "init", "clone", "pull", "push" };
		}

		public static Type GetType(string typeName)
		{
			var type = Type.GetType(typeName);
			if (type != null) return type;
			/*foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
			{
				type = a.GetType(typeName);
				if (type != null)
					return type;
			}*/

			return AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetTypes().Any(t => t.Name == typeName)).FirstOrDefault().GetTypes().FirstOrDefault(t => t.Name == typeName);
		}
		public static Type GetAllTypes(string typeName)
		{
			var type = Type.GetType(typeName);
			if (type != null) return type;
			/*foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
			{
				type = a.GetType(typeName);
				if (type != null)
					return type;
			}*/

			return AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetTypes().Any(t => t.Name.Contains(typeName))).FirstOrDefault().GetTypes().FirstOrDefault(t => t.Name == typeName);
		}
	}
}
