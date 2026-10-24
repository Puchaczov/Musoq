using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class ParameterizedPagingTests
{
    [TestMethod]
    [DataRow("skip $offset take 2", "offset")]
    [DataRow("skip 1 take $limit", "limit")]
    public void DirectReference_ShouldPreserveNameAndSpan(string paging, string name)
    {
        var sql = $"select 1 from #A.entities() {paging}";
        var lexer = new Lexer(sql, true);
        var root = new Parser(lexer, lexer.Diagnostics).ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        var query = ((SingleSetNode)statements.Statements[0].Node).Query;
        var reference = (ParameterReferenceNode)(query.Skip!.Expression is ParameterReferenceNode ? query.Skip.Expression : query.Take!.Expression);
        Assert.AreEqual(name, reference.Name);
        Assert.AreEqual(new TextSpan(sql.IndexOf('$', StringComparison.Ordinal), name.Length + 1), reference.Span);
    }

    [TestMethod]
    [DataRow("skip $offset + 1")]
    [DataRow("take $limit.Value")]
    [DataRow("skip Length('a')")]
    [DataRow("take Value")]
    [DataRow("take $limit[0]")]
    public void UnsupportedPagingExpression_ShouldFailParsing(string paging)
    {
        var lexer = new Lexer($"select 1 from #A.entities() {paging}", true, recoverOnError: true);
        var result = new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
        Assert.IsFalse(result.Success);
        Assert.IsNotEmpty(result.Diagnostics);
    }
}
