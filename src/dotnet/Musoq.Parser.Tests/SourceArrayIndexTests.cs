using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class SourceArrayIndexTests
{
    [TestMethod]
    [DataRow("Items[position]")]
    [DataRow("r.Items[r.Position + 1]")]
    [DataRow("r.Items[$position]")]
    [DataRow("r.Items[ToInt32(r.Position) ?? 0]")]
    [DataRow("r.Items[/* index */ r.Position - 1]")]
    [DataRow("(r.Items)[r.Position]")]
    [DataRow("r.Items[0][r.Position]")]
    public void SourceArrayIndex_ShouldParseTheWholeReceiverAndPreserveSource(string expression)
    {
        var sql = $"select {expression} as Selected from #test.rows() r";
        var syntax = Parser.ParseSyntax(sql);
        Assert.IsTrue(syntax.Success, string.Join("; ", syntax.Diagnostics.Select(d => d.Message)));
        Assert.AreEqual(sql, syntax.Root!.ToFullString());
        Assert.AreEqual(sql, string.Concat(syntax.Tokens.Select(t => t.Text)));

        var root = new Parser(new Lexer(sql, true)).ComposeAll();
        var query = ((SingleSetNode)((StatementsArrayNode)root.Expression).Statements[0].Node).Query;
        var indexed = (ArrayIndexNode)query.Select.Fields[0].Expression;
        Assert.AreEqual(expression, sql.Substring(indexed.Span.Start, indexed.Span.Length));
        Assert.IsFalse(indexed.Array is IdentifierNode { Name: "r" });
    }

    [TestMethod]
    public void IndexedRecordMember_ShouldAttachTheMemberAfterTheIndex()
    {
        var root = new Parser(new Lexer("select r.Items[r.Position].Value from #test.rows() r", true)).ComposeAll();
        var query = ((SingleSetNode)((StatementsArrayNode)root.Expression).Statements[0].Node).Query;
        var member = (DotNode)query.Select.Fields[0].Expression;
        Assert.IsInstanceOfType<ArrayIndexNode>(member.Root);
        Assert.AreEqual("Value", ((IdentifierNode)member.Expression).Name);
    }

    [TestMethod]
    public void MissingIndexBracket_ShouldLocateTheIndexFailure()
    {
        var result = Parser.ParseSyntax("select r.Items[r.Position from #test.rows() r");
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Diagnostics.Any(d => d.Message.Contains("RightSquareBracket")));
    }
}
