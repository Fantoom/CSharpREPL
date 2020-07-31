using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharpREPL
{
    internal class AutoCompletion : IAutoCompleteHandler
    {
        private readonly char[] openBrackets = { '{', '(' };
        public char[] Separators { get; set; } = { ' ', '.' };

        public string[] GetSuggestions(string text, int index)
        {
            if (text.LastIndexOf('.') > 0 && (index - text.LastIndexOf('.') <= 1 && index - text.LastIndexOf('.') >= -1))
            {

                string end = text.Split(".").Last();
                int i = text.LastIndexOf('.') - 1;
                string input = text.Remove(text.LastIndexOf('.')).Trim();
                while (char.IsLetterOrDigit(text[i]) && i > 0)
                {
                    i--;
                }
                if (i > 0)
                {
                    input = input.Remove(0, i + 1);
                }
                else if (!char.IsLetterOrDigit(input[0]))
                {
                    input = input.Remove(0,1);
                }


                var type = GetType(input);
                if (type != null)
                    return type.GetMembers()
                        .Select(x => x.Name)
                        .Where(x => x.Contains(end))
                        .ToArray();
                else
                    return new string[] { "" };
            }
            else
            {
                string input = text.Trim();
                int i = index;
                while (char.IsLetterOrDigit(text[i]) && i > 0)
                {
                    i--;
                }
                if (i >= 0 && !char.IsLetterOrDigit(text[0]))
                {
                    input = input.Remove(0, i + 1);
                }

                return GetAllTypes(input)
                    .Where(x => x.Name.StartsWith(input))
                    .Select(x => x.Name)
                    .ToArray();
            }
        }

        private static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            return type ?? (AppDomain.CurrentDomain
                    .GetAssemblies()
                    .FirstOrDefault(x => x.GetTypes().Any(t => t.Name == typeName))
                    ?.GetTypes()
                    .FirstOrDefault(t => t.Name == typeName));
        }
        public static List<Type> GetAllTypes(string typeName)
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(x => x.GetTypes().Any(t => t.Name.Contains(typeName)))
                ?.GetTypes().Where(x => x.Name.Contains(typeName)).ToList();
        }
    }
}
