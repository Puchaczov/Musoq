using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class PrecisionDiagnosticTests : BasicEntityTestBase
{
    [TestMethod]
    public void UnsupportedCastTarget_UsesDedicatedBindDiagnostic()
    {
        var diagnostic = AnalyzeSingleError("select 'value'::NotAType from #A.Entities()", DiagnosticCode.MQ3090_UnsupportedCastTarget);

        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        StringAssert.Contains(diagnostic.Message, "NotAType");
    }

    [TestMethod]
    public void InvalidConstantCast_IsRejectedBeforeExecution()
    {
        var diagnostic = AnalyzeSingleError("select 'not-a-number'::Int32 from #A.Entities()", DiagnosticCode.MQ3091_InvalidConstantCast);

        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        StringAssert.Contains(diagnostic.Message, "Int32");
    }

    [TestMethod]
    public void InvalidConstantRegex_IsRejectedBeforeExecution()
    {
        var diagnostic = AnalyzeSingleError(
            "select Name from #A.Entities() where Name rlike '['",
            DiagnosticCode.MQ3094_InvalidConstantRegex);

        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        StringAssert.Contains(diagnostic.Message, "regex");
    }

    [TestMethod]
    public void AggregateInGroupBy_UsesDedicatedBindDiagnostic()
    {
        var diagnostic = AnalyzeSingleError(
            "select City, Count(Name) from #A.Entities() group by Count(Name)",
            DiagnosticCode.MQ3092_AggregateInGroupBy);

        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
    }

    [TestMethod]
    public void OrderByOrdinal_UsesDedicatedBindDiagnostic()
    {
        var diagnostic = AnalyzeSingleError(
            "select Name from #A.Entities() order by 1",
            DiagnosticCode.MQ3093_OrderByOrdinalUnsupported);

        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
    }

    [TestMethod]
    public void VariableKeyAccess_UsesDedicatedBindDiagnostic()
    {
        var diagnostic = AnalyzeSingleError(
            "select Dictionary[key] from #A.Entities()",
            DiagnosticCode.MQ3096_UnsupportedVariableKeyAccess);

        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
    }

    [TestMethod]
    public void MultiRowValuesScalarSubquery_IsRejectedBeforeExecution()
    {
        const string query = "select (select Value from values { { Value: 1 }, { Value: 2 } } valuesSource) from #A.Entities()";
        var scalar = GetScalarSubquery(query);
        var scalarQuery = scalar.Subquery as QueryNode;
        Assert.IsNotNull(scalarQuery);
        var scalarSource = scalarQuery.From is ExpressionFromNode expressionFrom
            ? expressionFrom.Expression
            : scalarQuery.From;
        Assert.IsInstanceOfType<ValuesFromNode>(scalarSource);
        Assert.AreEqual(2, ((ValuesFromNode)scalarSource).Rows.Count);

        var diagnostic = AnalyzeSingleError(
            query,
            DiagnosticCode.MQ3095_ScalarSubqueryCardinality);

        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
    }

    [TestMethod]
    public void ValidConstantCastAndRegex_RemainErrorFree()
    {
        var result = CreateAnalyzer().Analyze(
            "select '42'::Int32 from #A.Entities() where Name rlike 'A.*'");

        Assert.IsFalse(result.Errors.Any(), Describe(result));
    }

    private static Diagnostic AnalyzeSingleError(string query, DiagnosticCode expectedCode)
    {
        var result = CreateAnalyzer().Analyze(query);

        Assert.IsTrue(result.IsParsed, Describe(result));
        var errors = result.Errors.ToArray();
        Assert.AreEqual(1, errors.Length, Describe(result));
        Assert.AreEqual(expectedCode, errors[0].Code, Describe(result));
        return errors[0];
    }

    private static string Describe(QueryAnalysisResult result) =>
        string.Join("\n", result.Diagnostics.Select(diagnostic => $"[{diagnostic.Code}] {diagnostic.Message}"));

    private static QueryAnalyzer CreateAnalyzer()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("Alpha", "Poland", 1)]
        };

        return new QueryAnalyzer(new BasicSchemaProvider<BasicEntity>(sources));
    }

    private static ScalarSubqueryNode GetScalarSubquery(string query)
    {
        var collector = new ScalarCollector();
        var root = new Musoq.Parser.Parser(new Lexer(query, true)).ComposeAll();
        root.Accept(new ScalarTraversal(collector));
        Assert.AreEqual(1, collector.Nodes.Count);
        return collector.Nodes[0];
    }

    private sealed class ScalarCollector : NoOpExpressionVisitor
    {
        public List<ScalarSubqueryNode> Nodes { get; } = [];

        public override void Visit(ScalarSubqueryNode node) => Nodes.Add(node);
    }

    private sealed class ScalarTraversal(ScalarCollector collector)
        : RawTraverseVisitor<ScalarCollector>(collector)
    {
    }
}
