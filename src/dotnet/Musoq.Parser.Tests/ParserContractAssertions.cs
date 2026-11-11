using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

internal static class ParserContractAssertions
{
    internal static void AssertCompleteScript(RootNode root, Lexer lexer)
    {
        var script = Assert.IsInstanceOfType<StatementsArrayNode>(root.Expression);
        Assert.IsNotEmpty(script.Statements, "A non-empty input must produce script statements.");
        Assert.AreEqual(TokenType.EndOfFile, lexer.Next().TokenType, "Parsing must consume the complete script.");
    }
}
