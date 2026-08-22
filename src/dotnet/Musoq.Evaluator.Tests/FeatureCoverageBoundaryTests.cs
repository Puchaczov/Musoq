using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class FeatureCoverageBoundaryTests
{
    [TestMethod]
    [FeatureEvidence("system-range-source", FeatureEvidenceKind.RuntimeNegativeDiagnostic)]
    public void SystemRange_WithoutHostRegistration_ShouldReportUnknownSchema()
    {
        var result = Analyze("select Value from system.range(1, 5)");

        AssertPrimaryDiagnostic(result, DiagnosticCode.MQ3010_UnknownSchema);
    }

    [TestMethod]
    [FeatureEvidence("generic-interpretation-schema-sql", FeatureEvidenceKind.RuntimeNegativeDiagnostic)]
    public void GenericInterpretationSchema_InSqlPipeline_ShouldReportUnsupportedSyntax()
    {
        var result = Analyze(
            """
            binary Data { Value: int le };
            binary Wrapper<T> { Item: T };
            select item.Item.Value
            from #A.Entities() a
            cross apply Interpret<Wrapper<Data>>(a.Name) item
            """);

        AssertPrimaryDiagnostic(result, DiagnosticCode.MQ2001_UnexpectedToken);
    }

    [TestMethod]
    [FeatureEvidence("asof-right-join", FeatureEvidenceKind.RuntimeNegativeDiagnostic)]
    public void AsOfRightJoin_ShouldReportUnsupportedSyntax()
    {
        var result = Analyze(
            """
            select a.Name
            from #A.Entities() a
            asof right join #A.Entities() b on a.Population >= b.Population
            """);

        AssertPrimaryDiagnostic(result, DiagnosticCode.MQ2001_UnexpectedToken);
    }

    [TestMethod]
    [FeatureEvidence("groups-exclude-window-frames", FeatureEvidenceKind.RuntimeNegativeDiagnostic)]
    public void GroupsAndExcludeFrames_ShouldReportUnsupportedSyntax()
    {
        var groups = Analyze(
            "select Sum(Population) over (order by Population groups between current row and current row) from #A.Entities()");
        var exclude = Analyze(
            "select Sum(Population) over (order by Population rows between current row and current row exclude current row) from #A.Entities()");

        AssertPrimaryDiagnostic(groups, DiagnosticCode.MQ2009_InvalidOrderByExpression);
        AssertPrimaryDiagnostic(exclude, DiagnosticCode.MQ2001_UnexpectedToken);
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        var provider = new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = []
            });
        return new QueryAnalyzer(provider).Analyze(query);
    }

    private static void AssertPrimaryDiagnostic(QueryAnalysisResult result, DiagnosticCode expectedCode)
    {
        Assert.IsTrue(result.HasErrors, string.Join(" | ", result.Diagnostics));
        var primary = result.Errors.First();
        Assert.AreEqual(expectedCode, primary.Code, string.Join(" | ", result.Diagnostics));
    }
}
