using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class PredicateAdvisoryTests
{
    [TestMethod]
    public void NullComparisons_InPredicateContexts_ReportSpecificWarning()
    {
        foreach (var operatorText in new[] { "=", "<>", "!=", "<", "<=", ">", ">=" })
        {
            var result = Analyze($"select Name from #A.Entities() where Name {operatorText} null");

            Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
            Assert.AreEqual(1, result.Warnings.Count(static item =>
                item.Code == DiagnosticCode.MQ5017_NullComparison), operatorText);
        }
    }

    [TestMethod]
    public void NullComparison_InProjection_IsQuiet()
    {
        var result = Analyze("select Name = null from #A.Entities()");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.IsFalse(result.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5017_NullComparison));
    }

    [TestMethod]
    public void NullChecks_AndDistinctChecks_RemainQuiet()
    {
        var isNull = Analyze("select Name from #A.Entities() where Name is null");
        var isNotNull = Analyze("select Name from #A.Entities() where Name is not null");
        var distinct = Analyze("select Name from #A.Entities() where Name is distinct from null");

        Assert.IsFalse(isNull.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5017_NullComparison));
        Assert.IsFalse(isNotNull.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5017_NullComparison));
        Assert.IsFalse(distinct.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5017_NullComparison));
    }

    [TestMethod]
    public void ConstantConditionsAndColumnProofs_ReportOneSpecificWarning()
    {
        var constantTrue = Analyze("select Name from #A.Entities() where 1 = 1");
        var constantFalse = Analyze("select Name from #A.Entities() where 1 = 2");
        var conflictingEquality = Analyze("select Name from #A.Entities() where Population = 1 and Population = 2");
        var conflictingRange = Analyze("select Name from #A.Entities() where Population > 10 and Population < 5");
        var complementaryNull = Analyze("select Name from #A.Entities() where Population is null or Population is not null");

        AssertCode(constantTrue, DiagnosticCode.MQ5010_TautologicalCondition);
        AssertCode(constantFalse, DiagnosticCode.MQ5011_ContradictoryCondition);
        AssertCode(conflictingEquality, DiagnosticCode.MQ5011_ContradictoryCondition);
        AssertCode(conflictingRange, DiagnosticCode.MQ5011_ContradictoryCondition);
        AssertCode(complementaryNull, DiagnosticCode.MQ5010_TautologicalCondition);
    }

    [TestMethod]
    public void NullLetValue_ReportsNullComparisonWithoutChangingSemantics()
    {
        var result = Analyze("let missing: int? = null; select NullableValue from #A.Entities() where NullableValue = $missing");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.AreEqual(1, result.Warnings.Count(static item =>
            item.Code == DiagnosticCode.MQ5017_NullComparison));
    }

    [TestMethod]
    public void NotIn_WithNullLiteralOrConstantLet_ReportsNullSensitiveWarning()
    {
        var literal = Analyze("select Name from #A.Entities() where Name not in ('Alice', null)");
        var constantLet = Analyze("let missing: string = null; select Name from #A.Entities() where Name not in ('Alice', $missing)");

        AssertCode(literal, DiagnosticCode.MQ5024_NullSensitiveNotIn);
        AssertCode(constantLet, DiagnosticCode.MQ5024_NullSensitiveNotIn);
    }

    [TestMethod]
    public void InAndNullFreeNotIn_RemainQuiet()
    {
        var positive = Analyze("select Name from #A.Entities() where Name in ('Alice', null)");
        var nullFree = Analyze("select Name from #A.Entities() where Name not in ('Alice', 'Bob')");
        var projection = Analyze("select Name not in ('Alice', null) from #A.Entities()");

        Assert.IsFalse(positive.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5024_NullSensitiveNotIn));
        Assert.IsFalse(nullFree.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5024_NullSensitiveNotIn));
        Assert.IsFalse(projection.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5024_NullSensitiveNotIn));
    }

    [TestMethod]
    public void CrossJoin_ShouldNotAnalyzeSyntheticTrueAsAnOnPredicate()
    {
        var result = Analyze("select r.Id, marker.Label from values { { Id: 1 } } r cross join values { { Label: 'x' } } marker");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.IsFalse(result.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5010_TautologicalCondition));
    }

    [TestMethod]
    public void ExplicitTrueInnerJoin_ShouldStillReportTautologicalOnPredicate()
    {
        var result = Analyze("select p.Name from #A.Entities() p inner join #A.Entities() q on true");

        AssertCode(result, DiagnosticCode.MQ5010_TautologicalCondition);
    }

    private static void AssertCode(QueryAnalysisResult result, DiagnosticCode code)
    {
        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.AreEqual(1, result.Warnings.Count(item => item.Code == code), string.Join(" | ", result.Diagnostics));
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
}
