using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Syntax;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class SourceSyntaxScriptTests
{
    [TestMethod]
    [DataRow("binary Rec { Value: byte }", "binary")]
    [DataRow("text Rec { Value: rest }", "text")]
    [DataRow("flags enum Status: int { Ready = 1 }", "enum")]
    [DataRow("explain analyze select Name from #s.rows()", "analyze")]
    public void DeclarationWords_ShouldHaveContextualKeywordRoles(string source, string word)
    {
        var result = Parser.ParseSyntax(source);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(SyntaxTokenRole.Keyword, result.Tokens.First(t => t.Text == word).Role);
    }

    [TestMethod]
    public void NestedTypeArguments_ShouldRetainLogicalClosingExtents()
    {
        const string source = "binary Rec { Value: Box<byte, Pair<int, short>> }";
        var result = Parser.ParseSyntax(source);
        var arguments = Descendants(result.Root!).Where(n => n.Kind == SyntaxKind.TypeArgumentList).ToArray();

        Assert.IsTrue(result.Success);
        Assert.AreEqual(source, string.Concat(result.Tokens.Select(t => t.Text)));
        Assert.IsTrue(arguments.Any(n => n.ToFullString() == "<int, short>"));
        Assert.IsTrue(arguments.Any(n => n.ToFullString() == "<byte, Pair<int, short>>"));
    }

    [TestMethod]
    public void EmbeddedQuery_ShouldExposeItsOwnGrammarRegions()
    {
        var result = Parser.ParseSyntax("profile select Name from #s.rows() where Flag=1");

        Assert.IsTrue(result.Success);
        Assert.IsTrue(Descendants(result.Root!).Any(n => n.Kind == SyntaxKind.WhereClause));
    }

    [TestMethod]
    public void EmbeddedQueryError_ShouldPointIntoTheOriginalScript()
    {
        const string source = "-- lead\nprofile select Name";
        var result = Parser.ParseSyntax(source);

        Assert.IsFalse(result.Success);
        Assert.AreEqual(source.Length, result.Diagnostics.Single().Location.Offset);
        Assert.AreEqual(2, result.Diagnostics.Single().Location.Line);
    }

    private static IEnumerable<SyntaxNode> Descendants(SyntaxNode root)
    {
        var pending = new Stack<SyntaxNode>();
        pending.Push(root);
        while (pending.TryPop(out var node))
        {
            yield return node;
            foreach (var child in node.Children)
                pending.Push(child);
        }
    }
}
