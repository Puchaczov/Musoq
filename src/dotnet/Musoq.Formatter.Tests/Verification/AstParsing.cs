using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Formatter.Tests.Verification;

internal static class AstParsing
{
    internal static RootNode Parse(string source) => new Parser.Parser(new Lexer(source, true)).ComposeAll();
}
