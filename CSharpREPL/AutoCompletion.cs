using System;
using System.Linq;

namespace CSharpREPL
{
    internal class AutoCompletion : IAutoCompleteHandler
    {
        private readonly char[] openBrackets = { '{', '(' };
        public char[] Separators { get; set; } = { ' ', '.' };

        public string[] GetSuggestions(string text, int index)
        {
            if (text.LastIndexOf('.') <= 0 ||
                (index - text.LastIndexOf('.') >= 1 && index - text.LastIndexOf('.') <= -1))
            {
                return null;
            }

            string end = text.Split(".").Last();
            int i = text.LastIndexOf('.') - 1;
            string input = text.Remove(text.LastIndexOf('.')).Trim();
            while (!this.openBrackets.Contains(text[i]) && i > 0)
            {
                i--;
            }
            if (i >= 0)
            {
                input = input.Remove(0, i + 1);
            }

            var type = GetType(input);
            return type.GetMembers()
                .Select(x => x.Name)
                .Where(x => x.Contains(end))
                .ToArray();

        }

        private static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            return type ?? (AppDomain.CurrentDomain
                    .GetAssemblies()
                    .FirstOrDefault(x => x.GetTypes()
                        .Any(t => t.Name == typeName))
                    ?.GetTypes()
                    .FirstOrDefault(t => t.Name == typeName));
        }
        public static Type GetAllTypes(string typeName)
        {
            var type = Type.GetType(typeName);
            return type ?? (AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(x => x.GetTypes()
                    .Any(t => t.Name.Contains(typeName)))
                ?.GetTypes()
                .FirstOrDefault(t => t.Name == typeName));
        }
    }
}
