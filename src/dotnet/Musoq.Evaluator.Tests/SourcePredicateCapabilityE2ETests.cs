using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.SourcePlanning;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class SourcePredicateCapabilityE2ETests : BasicEntityTestBase
{
    [TestMethod]
    public void TypedPrefixCapability_ShouldPushMatchAndRemoveRuntimeFilter()
    {
        const string query = "select s.Id, s.Name from #sp.items() s where s.Name like 'item-0%'";
        var rows = CreateRows();

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out var baselineProvider);
        var optimized = Run(query, SourcePlanningMode.AcceptTypedStringPredicate, rows, out var optimizedProvider);
        var inspection = Inspect(query, SourcePlanningMode.AcceptTypedStringPredicate, rows);
        var request = optimizedProvider.Requests.Single();
        var plan = optimizedProvider.ExecutionPlans.Single();

        AssertSameTable(baseline, optimized);
        Assert.IsTrue(request.Predicate is SourcePredicateStringMatch);
        var requestedMatch = (SourcePredicateStringMatch)request.Predicate!;
        Assert.AreEqual(SourceStringMatchKind.Prefix, requestedMatch.Kind);
        Assert.AreEqual("item-0%", requestedMatch.OriginalPattern);
        Assert.AreEqual("item-0", requestedMatch.Needle);
        Assert.IsTrue(plan.AcceptedPredicate is SourcePredicateStringMatch);
        Assert.HasCount(1, optimizedProvider.PlanResults.Single().ExecutionPlan.PredicateApplications);
        Assert.HasCount(1, plan.PredicateApplications);
        Assert.AreEqual(SourcePredicateEvaluationPhase.RowFiltering, plan.PredicateApplications[0].Phase);
        Assert.AreEqual(SourceStringMatchKind.Prefix, plan.PredicateApplications[0].Predicate.Kind);
        Assert.IsTrue(optimizedProvider.Recorder.SourceRowsProduced < baselineProvider.Recorder.SourceRowsProduced);
        Assert.DoesNotContain("PhysicalFilter", inspection.PhysicalPlanText);
        Assert.DoesNotContain("LikePrefix(", inspection.GeneratedCSharpCode);
        Assert.Contains(
            "source string-match: column=Name, pattern='item-0%', kind=Prefix, needle='item-0', " +
            "comparison=LikeIgnoreCase, negated=no, acceptedPhase=RowFiltering, fallback=none",
            inspection.PlanningText);
    }

    [TestMethod]
    public void TypedNegatedPrefixCapability_ShouldPreserveNullAndNegationSemantics()
    {
        const string query = "select s.Id, s.Name from #sp.items() s where s.Name not like 'item-0%'";
        var rows = CreateRows();

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.AcceptTypedStringPredicate, rows, out var optimizedProvider);
        var inspection = Inspect(query, SourcePlanningMode.AcceptTypedStringPredicate, rows);
        var request = optimizedProvider.Requests.Single();
        var plan = optimizedProvider.ExecutionPlans.Single();

        AssertSameTable(baseline, optimized);
        Assert.IsTrue(request.Predicate is SourcePredicateStringMatch);
        Assert.IsTrue(((SourcePredicateStringMatch)request.Predicate!).IsNegated);
        Assert.IsTrue(plan.PredicateApplications.Single().Predicate.IsNegated);
        Assert.DoesNotContain("PhysicalFilter", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void UnsupportedSourceLikeShape_ShouldRemainPreparedEvaluatorResidual()
    {
        const string query = "select s.Id, s.Name from #sp.items() s where s.Name like 'item-%0%'";
        var rows = CreateRows();

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.AcceptTypedStringPredicate, rows, out var optimizedProvider);
        var inspection = Inspect(query, SourcePlanningMode.AcceptTypedStringPredicate, rows);

        AssertSameTable(baseline, optimized);
        Assert.IsNull(optimizedProvider.Requests.Single().Predicate);
        Assert.IsEmpty(optimizedProvider.ExecutionPlans.Single().PredicateApplications);
        Assert.Contains("PhysicalFilter", inspection.PhysicalPlanText);
        Assert.Contains("Operators.PrepareLike", inspection.GeneratedCSharpCode);
        Assert.Contains("Operators.LikePrepared", inspection.GeneratedCSharpCode);
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("new Musoq.Evaluator.Operators().Like", StringComparison.Ordinal));
    }

    [TestMethod]
    public void PartiallySupportedTypedPredicate_ShouldPushSupportedConjunctAndRetainUnsupportedResidual()
    {
        const string query = @"
            select s.Id, s.Name
            from #sp.items() s
            where s.Name like 'item-0%'
              and s.Category like 'alpha'";
        var rows = CreateRows();

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.AcceptTypedStringPrefixPredicate, rows, out var optimizedProvider);
        var inspection = Inspect(query, SourcePlanningMode.AcceptTypedStringPrefixPredicate, rows);
        var request = optimizedProvider.Requests.Single();
        var plan = optimizedProvider.ExecutionPlans.Single();

        AssertSameTable(baseline, optimized);
        Assert.IsTrue(request.Predicate is SourcePredicateStringMatch);
        Assert.AreEqual(SourceStringMatchKind.Prefix, ((SourcePredicateStringMatch)request.Predicate!).Kind);
        Assert.IsTrue(plan.AcceptedPredicate is SourcePredicateStringMatch { Kind: SourceStringMatchKind.Prefix });
        Assert.HasCount(1, plan.PredicateApplications);
        Assert.Contains("PhysicalFilter", inspection.PhysicalPlanText);
        Assert.Contains("string.Equals(", inspection.GeneratedCSharpCode);
        Assert.Contains("source capability predicate: requested=yes, accepted=yes, residual=yes -> Partial", inspection.PlanningText);
        Assert.Contains(
            "source string-match: column=Category, pattern='alpha', kind=Exact, needle='alpha', " +
            "comparison=LikeIgnoreCase, negated=no, acceptedPhase=none, fallback=unsupported-kind",
            inspection.PlanningText);
    }

    [TestMethod]
    public void LegacySourceWithoutPredicateCapabilities_ShouldLeaveTypedMatchAsResidual()
    {
        const string query = "select s.Id, s.Name from #sp.items() s where s.Name like '%item%'";
        var rows = CreateRows();

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.AcceptPredicate, rows, out var optimizedProvider);
        var inspection = Inspect(query, SourcePlanningMode.AcceptPredicate, rows);
        var plan = optimizedProvider.ExecutionPlans.Single();

        AssertSameTable(baseline, optimized);
        Assert.IsNull(optimizedProvider.Requests.Single().Predicate);
        Assert.IsNull(plan.AcceptedPredicate);
        Assert.IsEmpty(plan.PredicateApplications);
        Assert.Contains("PhysicalFilter", inspection.PhysicalPlanText);
        Assert.Contains(".Contains(", inspection.GeneratedCSharpCode);
    }

    [TestMethod]
    public void UnknownPredicateCapabilityVersion_ShouldDeferTypedMatchAndEmitDiagnostic()
    {
        const string query = "select s.Id, s.Name from #sp.items() s where s.Name like '%item%'";
        var rows = CreateRows();

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.AcceptTypedStringPredicateUnknownVersion, rows, out var optimizedProvider);
        var inspection = Inspect(query, SourcePlanningMode.AcceptTypedStringPredicateUnknownVersion, rows);
        var plan = optimizedProvider.ExecutionPlans.Single();

        AssertSameTable(baseline, optimized);
        Assert.IsNull(optimizedProvider.Requests.Single().Predicate);
        Assert.IsNull(plan.AcceptedPredicate);
        Assert.IsEmpty(plan.PredicateApplications);
        Assert.Contains("not understood", inspection.PlanningText);
        Assert.Contains("PhysicalFilter", inspection.PhysicalPlanText);
        Assert.Contains(
            "source string-match: column=Name, pattern='%item%', kind=Contains, needle='item', " +
            "comparison=LikeIgnoreCase, negated=no, acceptedPhase=none, fallback=unknown-contract-version",
            inspection.PlanningText);
    }

    [TestMethod]
    public void SupportedLookingOr_ShouldRemainEntirelyResidual()
    {
        const string query = "select s.Id, s.Name from #sp.items() s " +
                             "where s.Name like 'item-0%' or s.Category like 'alpha'";
        var rows = CreateRows();

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.AcceptTypedStringPredicate, rows, out var provider);
        var inspection = Inspect(query, SourcePlanningMode.AcceptTypedStringPredicate, rows);

        AssertSameTable(baseline, optimized);
        Assert.IsNull(provider.Requests.Single().Predicate);
        Assert.IsNull(provider.ExecutionPlans.Single().AcceptedPredicate);
        Assert.IsEmpty(provider.ExecutionPlans.Single().PredicateApplications);
        Assert.Contains("PhysicalFilter", inspection.PhysicalPlanText);
        Assert.Contains("pattern='item-0%'", inspection.PlanningText);
        Assert.Contains("fallback=nested-non-conjunct", inspection.PlanningText);
    }

    [TestMethod]
    [DataRow(SourcePlanningMode.MalformedTypedMissingApplication, false, "missing an evaluation application")]
    [DataRow(SourcePlanningMode.MalformedTypedDuplicateApplication, false, "was not accepted")]
    [DataRow(SourcePlanningMode.MalformedTypedAlteredApplication, false, "was not accepted")]
    [DataRow(SourcePlanningMode.MalformedTypedNestedAccepted, true, "not an exact partition")]
    [DataRow(SourcePlanningMode.MalformedTypedUnknownVersionApplication, false, "unadvertised capability or phase")]
    [DataRow(SourcePlanningMode.MalformedTypedUnadvertisedPhaseApplication, false, "unadvertised capability or phase")]
    public void MaliciousTypedApplication_ShouldFailBeforeSourceExecution(
        SourcePlanningMode mode,
        bool useNestedPredicate,
        string expectedMessage)
    {
        var query = useNestedPredicate
            ? "select s.Id from #sp.items() s where s.Name like 'item-0%' or s.Category like 'alpha'"
            : "select s.Id from #sp.items() s where s.Name like 'item-0%'";
        var provider = new SourcePlanningSchemaProvider(mode, CreateRows());

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, schemaProvider: provider));

        Assert.Contains(expectedMessage, exception.Message);
        Assert.IsEmpty(provider.ExecutionPlans);
        Assert.AreEqual(0, provider.Recorder.SourceRowsProduced);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<SourcePlanningEntity>> CreateRows()
    {
        return new Dictionary<string, IReadOnlyList<SourcePlanningEntity>>
        {
            ["#sp"] = SourcePlanningRows.CreateDefault()
        };
    }

    private Table Run(
        string query,
        SourcePlanningMode mode,
        IReadOnlyDictionary<string, IReadOnlyList<SourcePlanningEntity>> rows,
        out SourcePlanningSchemaProvider provider)
    {
        provider = new SourcePlanningSchemaProvider(mode, rows);
        var compiled = CreateAndRunVirtualMachine(query, schemaProvider: provider);
        return TableMaterializationTestHelper.Materialize(compiled.Run());
    }

    private QueryInspectionResult Inspect(
        string query,
        SourcePlanningMode mode,
        IReadOnlyDictionary<string, IReadOnlyList<SourcePlanningEntity>> rows)
    {
        return InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new SourcePlanningSchemaProvider(mode, rows),
            LoggerResolver,
            TestCompilationOptions);
    }

    private static void AssertSameTable(Table expected, Table actual)
    {
        Assert.AreEqual(expected.Count, actual.Count);
        Assert.AreEqual(expected.Columns.Count(), actual.Columns.Count());

        for (var rowIndex = 0; rowIndex < expected.Count; rowIndex++)
        {
            CollectionAssert.AreEqual(
                expected[rowIndex].Values,
                actual[rowIndex].Values,
                $"Row {rowIndex} differs.");
        }
    }
}
