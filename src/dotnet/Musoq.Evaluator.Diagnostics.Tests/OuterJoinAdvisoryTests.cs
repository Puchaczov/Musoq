using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class OuterJoinAdvisoryTests
{
    [TestMethod]
    public void NullableOptionalColumnIsNull_ReportsAmbiguousPresenceWarning()
    {
        var result = Analyze("select a.Name, b.Name from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b.Name is null");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        AssertCode(result, DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck);
        Assert.IsFalse(result.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter));
    }

    [TestMethod]
    public void NonNullableOptionalColumnIsNull_RemainsUnambiguous()
    {
        var result = Analyze("select a.Name, b.Population from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b.Population is null");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.IsFalse(result.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck));
    }

    [TestMethod]
    public void WhereFilterOnOptionalSide_ReportsNullRejectingWarning()
    {
        var result = Analyze("select a.Name, b.Name from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b.Name = 'match'");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        AssertCode(result, DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter);
    }

    [TestMethod]
    public void PresenceGuard_SuppressesBothOuterJoinWarnings()
    {
        var result = Analyze("select a.Name, b.Name from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b is present and b.Name is null");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.IsFalse(result.Warnings.Any(static warning =>
            warning.Code is DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck or DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter));
    }

    [TestMethod]
    public void PreservingOrBranch_DoesNotReportNullRejectingWarning()
    {
        var result = Analyze("select a.Name, b.Name from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b.Name = 'match' or a.Id = 1");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.IsFalse(result.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter));
    }

    [TestMethod]
    public void InnerJoin_DoesNotProduceOuterJoinWarnings()
    {
        var result = Analyze("select a.Name, b.Name from #A.Entities() a join #B.Entities() b on a.Id = b.Id where b.Name = 'match'");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.IsFalse(result.Warnings.Any(static warning =>
            warning.Code is DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck or DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter));
    }

    [TestMethod]
    public void FullJoin_ReportsAmbiguousChecksForBothOptionalSides()
    {
        var result = Analyze("select a.Name, b.Name from #A.Entities() a full join #B.Entities() b on a.Id = b.Id where a.Name is null or b.Name is null");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.AreEqual(2, result.Warnings.Count(static warning => warning.Code == DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck));
    }

    [TestMethod]
    public void RightJoinAndAsOfLeftJoin_RecognizeTheOptionalSide()
    {
        var right = Analyze("select a.Name, b.Name from #A.Entities() a right join #B.Entities() b on a.Id = b.Id where a.Name = 'match'");
        var asOfLeft = Analyze("select a.Name, b.Name from #A.Entities() a asof left join #B.Entities() b on a.Population >= b.Population where b.Name = 'match'");

        AssertCode(right, DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter);
        AssertCode(asOfLeft, DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter);
    }

    [TestMethod]
    public void OuterApply_RecognizesTheOptionalAppliedSide()
    {
        var result = Analyze("select a.Name, b.Name from #A.Entities() a outer apply #B.Entities() b where b.Name = 'match'");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        AssertCode(result, DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter);
    }

    [TestMethod]
    public void PatternRangeAndMembershipFilters_AreProvenToRejectMissingRows()
    {
        var like = Analyze("select a.Name, b.Name from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b.Name like 'match%'");
        var rlike = Analyze("select a.Name, b.Name from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b.Name rlike 'match'");
        var between = Analyze("select a.Name, b.Population from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b.Population between 1 and 2");
        var inList = Analyze("select a.Name, b.Id from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b.Id in (1, 2)");

        AssertCode(like, DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter);
        AssertCode(rlike, DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter);
        AssertCode(between, DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter);
        AssertCode(inList, DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter);
    }

    [TestMethod]
    public void JoinOnRestriction_DoesNotReportWhereNullRejection()
    {
        var result = Analyze("select a.Name, b.Name from #A.Entities() a left join #B.Entities() b on a.Id = b.Id and b.Name = 'match'");

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        Assert.IsFalse(result.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter));
    }

    [TestMethod]
    public void MissingRowProof_HandlesNegatedPatternsAndConstantBooleanBranches()
    {
        var negated = Analyze("select a.Name, b.Name from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b.Name not like 'match%'");
        var andFalse = Analyze("select a.Name, b.Name from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b.Name = 'match' and false");
        var orTrue = Analyze("select a.Name, b.Name from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b.Name = 'match' or true");
        var notNull = Analyze("select a.Name, b.Name from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b.Name is not null");

        AssertCode(negated, DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter);
        AssertCode(andFalse, DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter);
        Assert.IsFalse(orTrue.Warnings.Any(static warning => warning.Code == DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter));
        AssertCode(notNull, DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter);
    }

    private static void AssertCode(QueryAnalysisResult result, DiagnosticCode code)
    {
        var matches = result.Warnings.Where(item => item.Code == code).ToArray();
        Assert.AreEqual(1, matches.Length, string.Join(" | ", result.Diagnostics));
        Assert.AreEqual(DiagnosticPhase.Bind, matches[0].Phase);
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        var provider = new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [],
                ["#B"] = []
            });
        return new QueryAnalyzer(provider).Analyze(query);
    }
}
