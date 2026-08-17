using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class ParamsAliasParserTests
{
    [TestMethod]
    [DataRow("params", DisplayName = "lowercase")]
    [DataRow("PARAMS", DisplayName = "uppercase")]
    [DataRow("PaRaMs", DisplayName = "mixed case")]
    public void ParamsAlias_ShouldParseAsParameterBlock(string keyword)
    {
        var root = Parse($"{keyword}(author: string) select $author from #test.rows()");
        var statements = (StatementsArrayNode)root.Expression;

        Assert.HasCount(2, statements.Statements);
        var parameterBlock = (ParameterBlockNode)statements.Statements[0].Node;
        Assert.HasCount(1, parameterBlock.Parameters);
        Assert.AreEqual("author", parameterBlock.Parameters[0].Name);
        Assert.AreEqual("string", parameterBlock.Parameters[0].TypeName);
    }

    [TestMethod]
    public void ParamsAlias_ShouldPreserveCanonicalAstIdentityAndRendering()
    {
        var canonical = GetParameterBlock(Parse(
            "param(author: string, limit: int = 100, since: datetime? = null) select $author from #test.rows()"));
        var alias = GetParameterBlock(Parse(
            "params(author: string, limit: int = 100, since: datetime? = null) select $author from #test.rows()"));

        Assert.AreEqual(canonical.Id, alias.Id);
        Assert.AreEqual(canonical.ToString(), alias.ToString());
        Assert.IsTrue(alias.ToString().StartsWith("param (", StringComparison.Ordinal));
        Assert.HasCount(canonical.Parameters.Length, alias.Parameters);

        for (var index = 0; index < canonical.Parameters.Length; index++)
            Assert.AreEqual(canonical.Parameters[index].Id, alias.Parameters[index].Id);
    }

    [TestMethod]
    public void ParamsAlias_ShouldSupportEmptyAndComplexDeclarations()
    {
        var empty = GetParameterBlock(Parse("params() select 1 from #test.rows()"));
        var complex = GetParameterBlock(Parse(
            "params ( ids: int[], optional: string? = null ); select 1 from #test.rows()"));

        Assert.IsEmpty(empty.Parameters);
        Assert.HasCount(2, complex.Parameters);
        Assert.AreEqual("int[]", complex.Parameters[0].DeclaredTypeName);
        Assert.IsFalse(complex.Parameters[0].HasDefaultValue);
        Assert.AreEqual("string?", complex.Parameters[1].DeclaredTypeName);
        Assert.IsInstanceOfType<NullNode>(complex.Parameters[1].DefaultValue);
    }

    [TestMethod]
    [DataRow(
        "params(string author) select 1 from #test.rows()",
        DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration,
        DisplayName = "C# style parameter")]
    [DataRow(
        "params([string]$author) select 1 from #test.rows()",
        DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax,
        DisplayName = "PowerShell style parameter")]
    public void ParamsAlias_MalformedDeclarations_ShouldKeepExistingDiagnostics(
        string query,
        DiagnosticCode expectedCode)
    {
        var result = ParseWithDiagnostics(query);

        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());
        Assert.AreEqual(expectedCode, result.Diagnostics[0].Code, result.FormatDiagnostics());
    }

    [TestMethod]
    public void ParamsAlias_ShouldNotBecomeGlobalKeyword()
    {
        Assert.IsFalse(KeywordLookup.TryGetKeyword("params", out _));
        Assert.IsFalse(KeywordLookup.TryGetSchemaKeyword("params", out _));

        var result = ParseWithDiagnostics("select params, params() from #test.rows()");

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
    }

    private static RootNode Parse(string query)
    {
        return new Parser(new Lexer(query, true)).ComposeAll();
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var diagnostics = new DiagnosticBag();
        return new Parser(new Lexer(query, true), diagnostics).ParseWithDiagnostics();
    }

    private static ParameterBlockNode GetParameterBlock(RootNode root)
    {
        var statements = (StatementsArrayNode)root.Expression;
        return (ParameterBlockNode)statements.Statements[0].Node;
    }
}
