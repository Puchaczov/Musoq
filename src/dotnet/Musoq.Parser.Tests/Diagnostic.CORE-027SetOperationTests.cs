using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCore027SetOperationTests
{
    [TestMethod]
    [DataRow("union", typeof(UnionNode))]
    [DataRow("union all", typeof(UnionAllNode))]
    [DataRow("except", typeof(ExceptNode))]
    [DataRow("intersect", typeof(IntersectNode))]
    public void SetOperator_AllForms_ShouldParseWithOptionalKeyLists(string setOperator, Type expectedType)
    {
        var omitted = ParseSetOperator(
            $"select Col from schemaA.methodA() {setOperator} select Col from schemaB.methodB()");
        var empty = ParseSetOperator(
            $"select Col from schemaA.methodA() {setOperator} () select Col from schemaB.methodB()");
        var explicitKeys = ParseSetOperator(
            $"select Col1, Col2 from schemaA.methodA() {setOperator} (Col1, Col2) select Col1, Col2 from schemaB.methodB()");

        Assert.IsInstanceOfType(omitted, expectedType);
        Assert.IsInstanceOfType(empty, expectedType);
        Assert.IsInstanceOfType(explicitKeys, expectedType);
        Assert.IsEmpty(omitted.Keys);
        Assert.IsEmpty(omitted.KeySpans);
        Assert.IsEmpty(empty.Keys);
        Assert.IsEmpty(empty.KeySpans);
        CollectionAssert.AreEqual(new[] { "Col1", "Col2" }, explicitKeys.Keys);
        Assert.HasCount(2, explicitKeys.KeySpans);
    }

    [TestMethod]
    public void SetOperator_ExplicitKeys_ShouldPreserveIdentifierSpansThroughResultModifiers()
    {
        const string query = "select Col1, Col2 from schemaA.methodA() union (Col1, schemaB.Col2) select Col1, Col2 from schemaB.methodB() order by Col1";
        var node = ParseSetOperator(query);
        var keyListStart = query.IndexOf("(Col1, schemaB.Col2)", StringComparison.Ordinal);

        CollectionAssert.AreEqual(new[] { "Col1", "schemaB.Col2" }, node.Keys);
        CollectionAssert.AreEqual(
            new[]
            {
                new TextSpan(keyListStart + 1, "Col1".Length),
                new TextSpan(keyListStart + 7, "schemaB.Col2".Length)
            },
            node.KeySpans.ToArray());
        Assert.IsNotNull(node.ResultOrderBy);
    }

    [TestMethod]
    public void SetOperator_ChainedOperators_ShouldKeepRootModifiersAndEachKeySpan()
    {
        const string query = "select Col from schemaA.methodA() union (Col) select Col from schemaB.methodB() except () select Col from schemaC.methodC() intersect (schemaD.Col) select Col from schemaD.methodD() order by Col take 1";
        var root = ParseSetOperator(query);
        var except = Assert.IsInstanceOfType<ExceptNode>(root.Right);
        var intersect = Assert.IsInstanceOfType<IntersectNode>(except.Right);

        Assert.IsNotNull(root.ResultOrderBy);
        Assert.IsNotNull(root.ResultTake);
        Assert.IsNull(except.ResultOrderBy);
        Assert.IsNull(intersect.ResultOrderBy);
        Assert.HasCount(1, root.KeySpans);
        Assert.IsEmpty(except.KeySpans);
        Assert.HasCount(1, intersect.KeySpans);
        Assert.AreEqual("Col", query.Substring(root.KeySpans[0].Start, root.KeySpans[0].Length));
        Assert.AreEqual("schemaD.Col", query.Substring(intersect.KeySpans[0].Start, intersect.KeySpans[0].Length));
    }

    [TestMethod]
    public void SetOperatorKeyList_TrailingComma_ShouldReportExactDiagnostic()
    {
        const string query = "select Col from schemaA.methodA() union (Col,) select Col from schemaB.methodB()";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2014_TrailingComma,
            "Set operator key list has a trailing comma. Add another key or remove the comma.",
            new TextSpan(query.IndexOf(",)", StringComparison.Ordinal) + 1, 1));
    }

    [TestMethod]
    public void SetOperatorKeyList_LeadingComma_ShouldReportExactDiagnostic()
    {
        const string query = "select Col from schemaA.methodA() union (,Col) select Col from schemaB.methodB()";

        AssertParseDiagnostic(
            query,
            DiagnosticCode.MQ2015_LeadingComma,
            "Set operator key list has a leading comma. Add a key before the comma or remove it.",
            new TextSpan(query.IndexOf(",", StringComparison.Ordinal), 1));
    }

    private static void AssertParseDiagnostic(
        string query,
        DiagnosticCode expectedCode,
        string expectedMessage,
        TextSpan expectedSpan)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        var result = new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(expectedMessage, diagnostic.Message);
        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.AreEqual("Core Spec - Lists", diagnostic.DocsReference);
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    private static SetOperatorNode ParseSetOperator(string query)
    {
        var lexer = new Lexer(query, true);
        var root = new Parser(lexer, lexer.Diagnostics).ComposeAll();
        var statements = (StatementsArrayNode)root.Expression;
        return (SetOperatorNode)statements.Statements[0].Node;
    }
}
