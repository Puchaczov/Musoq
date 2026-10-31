using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Syntax;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class SourceSyntaxTests
{
    [TestMethod]
    [DataRow("")]
    [DataRow(" \r\n\t ")]
    [DataRow("-- only a comment")]
    [DataRow("/* a\r\n  block */\r\n-- another")]
    [DataRow("select (a.Price + 1) Label from #items.all() a")]
    [DataRow("select case a.Flag when 1 then 0xFF end from #items.all() a")]
    [DataRow("select -1, 0xFF, 0b101, 0o77, 4ul, true, NULL from #s.rows() a")]
    [DataRow("-- leading\nselect a.Name, /* between */ a.Price from #s.rows() a -- trailing\n")]
    [DataRow("from #s.rows() a where a.Flag=1 select a.Name")]
    [DataRow("param(author: string, minStars: int = 10) select $author from #s.rows() a")]
    [DataRow("params(items: int[]) select $items[0] from #s.rows() a")]
    [DataRow("let count: int = 1; select $count from #s.rows() a")]
    [DataRow("select array { 1, 2 }, (Name: 'A', Age: 3) from #s.rows() a")]
    [DataRow("with p as (select a.Name from #s.rows() a) select p.Name from p")]
    [DataRow("select Sum(Population) over (order by Name rows between unbounded preceding and current row) from #s.rows()")]
    [DataRow("binary Header { Magic: int le, Version: short le }; select h.Magic from #s.rows() a cross apply Interpret<Header>(a.Bytes) h")]
    [DataRow("binary Rec { Items: substream[1] as { Value: byte } exact[2] }")]
    [DataRow("binary Rec { Count: byte, Copy: (Count + 1), Items: (byte repeat until Items[-1] = 0)[2], Tail: byte }")]
    [DataRow("text LogEntry { Level: until ':', Message: rest };")]
    [DataRow("enum Status: int { Ready = 1, Done = 2 }; select Status.Ready from #s.rows() a")]
    public void AcceptedSource_ShouldRetainEveryOriginalCharacter(string source)
    {
        var result = Parser.ParseSyntax(source);

        Assert.IsTrue(result.Success, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.AreEqual(source, string.Concat(result.Tokens.Select(t => t.Text)));
        Assert.AreEqual(source, result.Root!.ToFullString());
        Assert.IsFalse(result.Tokens.Any(t => t.Role == SyntaxTokenRole.Unknown));
    }

    [TestMethod]
    [DataRow("select @ from #s.rows() a")]
    [DataRow("select (a.Name from #s.rows() a")]
    [DataRow("select 1 from #s.rows() a garbage trailing")]
    [DataRow("/* unterminated")]
    [DataRow("select 'unterminated")]
    public void InvalidSource_ShouldReturnOriginalInputDiagnostics(string source)
    {
        var result = Parser.ParseSyntax(source);

        Assert.IsFalse(result.Success);
        Assert.IsNull(result.Root);
        Assert.IsTrue(result.Diagnostics.Count > 0);
        Assert.AreEqual(source, string.Concat(result.Tokens.Select(t => t.Text)));
    }

    [TestMethod]
    public void OriginalCaseSyntax_ShouldRemainAvailableBeforeNormalization()
    {
        const string source = "select (case a.Flag when 1 then -0xFF end) Label from #s.rows() a";
        var result = Parser.ParseSyntax(source);

        Assert.IsTrue(result.Success);
        Assert.IsTrue(Descendants(result.Root!).Any(n => n.Kind == SyntaxKind.CaseExpression));
        Assert.AreEqual(1, result.Tokens.Count(t => t.TokenType == Tokens.TokenType.Case));
        Assert.AreEqual(0, result.Tokens.Count(t => t.TokenType == Tokens.TokenType.Else));
        Assert.AreEqual(0, result.Tokens.Count(t => t.TokenType == Tokens.TokenType.As));
        Assert.AreEqual("0xFF", result.Tokens.Single(t => t.TokenType == Tokens.TokenType.HexadecimalInteger).Text);
    }

    [TestMethod]
    public void SyntaxCapture_ShouldNotChangeOrdinaryAstLabels()
    {
        const string source = "select (a.Price + 1) Label from #s.rows() a";
        var before = new Parser(new Lexer(source, true)).ComposeAll();
        var syntax = Parser.ParseSyntax(source);
        var after = new Parser(new Lexer(source, true)).ComposeAll();
        var statements = (StatementsArrayNode)after.Expression;
        var field = ((SingleSetNode)statements.Statements[0].Node).Query.Select.Fields.Single();

        Assert.IsTrue(syntax.Success);
        Assert.AreEqual(before.Id, after.Id);
        Assert.AreEqual("Label", field.FieldName);
        Assert.IsTrue(field.HasExplicitFieldName);
    }

    [TestMethod]
    public void NullSource_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => Parser.ParseSyntax(null!));
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
