using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Syntax;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class SourceSyntaxKeywordTests
{
    [TestMethod]
    [DataRow("select Sum(Value) over (order by Name rows between unbounded preceding and current row) from #s.rows()", "rows")]
    [DataRow("select Sum(Value) over (order by Name rows between unbounded preceding and current row) from #s.rows()", "unbounded")]
    [DataRow("select Sum(Value) over (order by Name rows between unbounded preceding and current row) from #s.rows()", "preceding")]
    [DataRow("select Count(Value) filter (where Flag=1) from #s.rows()", "filter")]
    [DataRow("select * exclude (Secret) from #s.rows()", "exclude")]
    [DataRow("select * rename (Name as Label) from #s.rows()", "rename")]
    [DataRow("with recursive nums (n) as (select 1 n from #s.rows()) select n from nums", "recursive")]
    [DataRow("select Value from #s.rows() qualify Value>0", "qualify")]
    public void ContextualGrammarWord_ShouldHaveKeywordRole(string source, string word)
    {
        var result = Parser.ParseSyntax(source);
        var token = result.Tokens.First(t => t.Text.Equals(word, StringComparison.Ordinal));

        Assert.IsTrue(result.Success);
        Assert.AreEqual(SyntaxTokenRole.Keyword, token.Role);
    }

    [TestMethod]
    public void NonRecursiveCteNamedRecursive_ShouldRemainAnIdentifier()
    {
        var result = Parser.ParseSyntax("with recursive as (select Name from #s.rows()) select Name from recursive");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(SyntaxTokenRole.Identifier, result.Tokens.First(t => t.Text == "recursive").Role);
    }
}
