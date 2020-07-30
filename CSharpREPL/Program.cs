using System.Threading.Tasks;

namespace CSharpREPL
{
    internal static class Program
    {
        private static async Task Main() => await new REPL().Start();
    }
}