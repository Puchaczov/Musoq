using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.SourcePlanning;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.Exceptions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Alpha14CorrectnessRegressionTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow(0, false)]
    [DataRow(1, false)]
    [DataRow(2, false)]
    [DataRow(0, true)]
    [DataRow(1, true)]
    [DataRow(2, true)]
    public void LikeAnyString_ShouldPreserveNullAndMissingSemantics(int mode, bool negate)
    {
        var provider = new QueryScopedPlanningSchemaProvider(
            [Row(0, null), new Dictionary<string, object?> { ["Id"] = 1 }, Row(2, ""), Row(3, "Alpha")], mode);
        using var compiled = CreateAndRunVirtualMachine(
            $"select d.Id from #json.items() d where d.Name {(negate ? "not like" : "like")} '%' order by d.Id", schemaProvider: provider);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(result, negate ? [[0], [1]] : [[2], [3]]);
        Assert.AreEqual(mode == 0, provider.Recorder.ExecutionPlans.Single().PredicateApplications.Count > 0);
    }

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(1, false)]
    [DataRow(2, false)]
    [DataRow(0, true)]
    [DataRow(1, true)]
    [DataRow(2, true)]
    public async Task SelfCorrelation_ShouldBindSourceColumnsIndependently(int mode, bool general)
    {
        var provider = new QueryScopedPlanningSchemaProvider(Enumerable.Range(0, 9).Select(id => Row(id, "row")).ToArray(), mode);
        var query = general
            ? "select d.Id from #json.items() d where d.Id > 5 and exists (select e.Id from #json.items() e where e.Id > d.Id and e.Id > 5) order by d.Id"
            : "select d.Id from #json.items() d where exists (select e.Id from #json.items() e where e.Id = d.Id and e.Id > 5) order by d.Id";
        using var compiled = CreateAndRunVirtualMachine(query, schemaProvider: provider);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(result, general ? [[6], [7]] : [[6], [7], [8]]);
        using var repeated = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(repeated, general ? [[6], [7]] : [[6], [7], [8]]);

        using var barrier = new Barrier(2);
        provider.OuterSourceBarrier = barrier;
        var independentProvider = new QueryScopedPlanningSchemaProvider([Row(6, "outer"), Row(7, "outer"), Row(8, "outer")], mode)
        {
            InnerRows = [Row(7, "inner")],
            OuterSourceBarrier = barrier
        };
        using var independent = CreateAndRunVirtualMachine(query, schemaProvider: independentProvider);
        await Task.WhenAll(Task.Run(() => AssertCorrelationRows(compiled, general ? [6, 7] : [6, 7, 8])),
            Task.Run(() => AssertCorrelationRows(independent, general ? [6] : [7])));
        foreach (var execution in independentProvider.Executions)
        {
            Assert.IsTrue(execution.Shape.Fields.All(field => field.Name is "Id" or "Name"));
            Assert.IsTrue(execution.ExecutionContext.AllColumns.All(column => column.ColumnName is "Id" or "Name"));
        }
        var identities = independentProvider.Executions.Select(execution => execution.ExecutionContext.Plan.Identity)
            .DistinctBy(identity => identity.SourceContextId).ToArray();
        Assert.HasCount(2, identities);
        CollectionAssert.AreEquivalent(new[] { "d", "e" }, identities.Select(identity => identity.Alias).ToArray());
    }

    [TestMethod]
    [DataRow(false, 0)]
    [DataRow(true, 0)]
    [DataRow(false, 1)]
    [DataRow(true, 1)]
    [DataRow(false, 37)]
    [DataRow(true, 37)]
    public void RegexNegotiation_ShouldCarryTheCompilationBudget(bool negate, int milliseconds)
    {
        var timeout = milliseconds == 0 ? TimeSpan.FromSeconds(2) : TimeSpan.FromMilliseconds(milliseconds);
        var provider = new RegexPlanningSchemaProvider([new SourcePlanningEntity { Id = 1, Name = "Alpha" }]);
        var options = milliseconds == 0 ? TestCompilationOptions : TestCompilationOptions.WithRegexMatchTimeout(timeout);
        var inspection = InstanceCreator.CompileForInspection(
            $"select d.Id from #sp.items() d where d.Name {(negate ? "not rlike" : "rlike")} 'Al'", Guid.NewGuid().ToString(), provider, LoggerResolver, options);
        var predicate = Assert.IsInstanceOfType<SourcePredicateRegexMatch>(provider.Recorder.Requests.Single().Predicate);
        Assert.AreEqual(timeout, predicate.MatchTimeout);
        Assert.AreEqual(negate, predicate.IsNegated);
        Assert.DoesNotContain("PhysicalFilter", inspection.PhysicalPlanText);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void PublicProviderRegexTimeout_ShouldRetainCauseAndActualBudget(bool wrapped)
    {
        var cause = new RegexMatchTimeoutException("secret input", "secret pattern", TimeSpan.FromMilliseconds(17));
        var provider = new RegexPlanningSchemaProvider([new SourcePlanningEntity { Id = 1, Name = "Alpha" }])
        {
            FailureFactory = () => wrapped
                ? new DataSourceLifecycleException(DiagnosticCode.MQ7011_DataSourceReadFailed, "sp", "items", "d", "provider", "read", cause)
                : cause
        };
        using var compiled = CreateAndRunVirtualMachine("select d.Id from #sp.items() d where d.Name rlike 'Al'", schemaProvider: provider);
        var failure = Assert.Throws<QueryExecutionException>(() =>
        {
            using var result = compiled.Run(TestContext.CancellationToken);
            _ = result.Count;
        });
        Assert.AreEqual(DiagnosticCode.MQ7017_RegexMatchTimeout, failure.Envelope!.Code);
        Exception? inner = failure;
        while (inner is not null && inner is not RegexMatchTimeoutException) inner = inner.InnerException;
        Assert.AreSame(cause, inner);
        Assert.AreEqual("17", failure.Envelope.Arguments["timeoutMilliseconds"]);
        Assert.DoesNotContain("secret input", failure.FormatJson());
        Assert.DoesNotContain("secret pattern", failure.FormatJson());
    }

    private static IReadOnlyDictionary<string, object?> Row(int id, string? name) =>
        new Dictionary<string, object?> { ["Id"] = id, ["Name"] = name };

    private void AssertCorrelationRows(CompiledQuery compiled, int[] expected)
    {
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(result, expected.Select(id => new object?[] { id }).ToArray());
    }

    [TestMethod]
    public async Task RegexBudgets_ShouldRemainIsolatedAcrossCompoundAndNestedCompilations()
    {
        await Task.WhenAll(new[] { 1, 37, 2000, 5 }.Select(milliseconds => Task.Run(() =>
        {
            var timeout = TimeSpan.FromMilliseconds(milliseconds);
            var options = TestCompilationOptions.WithRegexMatchTimeout(timeout);
            foreach (var nested in new[] { false, true })
            {
                var provider = new RegexPlanningSchemaProvider([new SourcePlanningEntity { Id = 1, Name = "Alpha" }]);
                var query = nested
                    ? "select d.Id from #sp.items() d where d.Name rlike 'Al' and d.Name not rlike 'Be' and exists (select e.Id from #sp.items() e where e.Id > d.Id and e.Name rlike 'Al')"
                    : "select d.Id from #sp.items() d where d.Name rlike 'Al' and d.Name not rlike 'Be'";
                using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), provider, LoggerResolver, options);
                using var result = compiled.Run(TestContext.CancellationToken);
                TableMaterializationTestHelper.AssertRowsInOrder(result, nested ? [] : [[1]]);
                var predicates = provider.Recorder.Requests.SelectMany(request => RegexPredicates(request.Predicate)).ToArray();
                Assert.HasCount(nested ? 0 : 2, predicates, $"nested={nested}, budget={milliseconds}");
                Assert.IsTrue(predicates.All(predicate => predicate.MatchTimeout == timeout));
                Assert.HasCount(nested ? 0 : 1, predicates.Where(predicate => predicate.IsNegated).ToArray());
                if (nested)
                {
                    var inspection = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString(),
                        new RegexPlanningSchemaProvider([new SourcePlanningEntity { Id = 1, Name = "Alpha" }]), LoggerResolver, options);
                    var plans = ExecutionSubqueryPlans.SelfAndDescendants(inspection.ExecutionPlan!).ToArray();
                    Assert.HasCount(2, plans);
                    Assert.IsTrue(plans.All(plan => plan.RegexMatchTimeout == timeout));
                }
            }
        })));
        Assert.AreEqual(TimeSpan.FromSeconds(2), TestCompilationOptions.RegexMatchTimeout);
    }

    [TestMethod]
    public void CustomRegexBudget_WhenPartiallyAccepted_ShouldKeepOnlyTheRejectedConjunctResidual()
    {
        var provider = new RegexPlanningSchemaProvider([new SourcePlanningEntity { Id = 1, Name = "Alpha" }])
        {
            PlanFactory = request =>
            {
                var predicate = (SourcePredicateLogical)request.Predicate!;
                return SourcePlanResult.AcceptAll(request with { Predicate = predicate.Left }) with { ResidualPredicate = predicate.Right };
            }
        };
        const string query = "select d.Id from #sp.items() d where d.Name rlike 'Al' and d.Name not rlike 'Be'";
        var options = TestCompilationOptions.WithRegexMatchTimeout(TimeSpan.FromMilliseconds(37));
        var inspection = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString(), provider, LoggerResolver, options);
        Assert.Contains("PhysicalFilter", inspection.PhysicalPlanText);
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), provider, LoggerResolver, options);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(result, [1]);
        Assert.IsTrue(provider.Recorder.Requests.SelectMany(request => RegexPredicates(request.Predicate))
            .All(predicate => predicate.MatchTimeout == TimeSpan.FromMilliseconds(37)));
    }

    [TestMethod]
    public void ProviderBacktrackingTimeout_ShouldUseTheNegotiatedBudgetAndPublicException()
    {
        const string pattern = "^(?=(a+)+$)a+$";
        var input = new string('a', 50_000) + "!";
        SourcePredicateRegexMatch? supplied = null;
        var timeout = TimeSpan.FromMilliseconds(1);
        var publicProvider = new RegexPlanningSchemaProvider([new SourcePlanningEntity { Id = 1, Name = input }])
        {
            PlanFactory = request =>
            {
                supplied = (SourcePredicateRegexMatch)request.Predicate!;
                return SourcePlanResult.AcceptAll(request);
            },
            FailureFactory = () =>
            {
                var predicate = supplied!;
                try { _ = Regex.IsMatch(input, predicate.Pattern, RegexOptions.None, predicate.MatchTimeout); }
                catch (RegexMatchTimeoutException exception) { return exception; }
                return new InvalidOperationException("Expected the backtracking regex to time out.");
            }
        };
        using var compiled = InstanceCreator.CompileForExecution(
            $"select d.Id from #sp.items() d where d.Name rlike '{pattern}'", Guid.NewGuid().ToString(), publicProvider,
            LoggerResolver, TestCompilationOptions.WithRegexMatchTimeout(timeout));
        var failure = Assert.Throws<QueryExecutionException>(() =>
        {
            using var result = compiled.Run(TestContext.CancellationToken);
            _ = result.Count;
        });
        Assert.AreEqual(DiagnosticCode.MQ7017_RegexMatchTimeout, failure.Envelope!.Code);
        Assert.AreEqual("1", failure.Envelope.Arguments["timeoutMilliseconds"]);
        var lifecycle = Assert.IsInstanceOfType<DataSourceLifecycleException>(failure.InnerException);
        var cause = Assert.IsInstanceOfType<RegexMatchTimeoutException>(lifecycle.InnerException);
        Assert.AreEqual(timeout, cause.MatchTimeout);
    }

    private static IEnumerable<SourcePredicateRegexMatch> RegexPredicates(SourcePredicateExpression? predicate) => predicate switch
    {
        SourcePredicateRegexMatch regex => [regex],
        SourcePredicateLogical logical => RegexPredicates(logical.Left).Concat(RegexPredicates(logical.Right)),
        _ => []
    };

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    public void MixedNativeAndQueryScopedCorrelation_ShouldPreserveTheExistingPath(int mode)
    {
        var native = new RegexPlanningSchemaProvider(Enumerable.Range(0, 9).Select(id => new SourcePlanningEntity { Id = id }).ToArray());
        var scoped = new QueryScopedPlanningSchemaProvider(Enumerable.Range(0, 9).Select(id => Row(id, "row")).ToArray(), mode);
        using var compiled = CreateAndRunVirtualMachine(
            "select d.Id from #native.items() d where d.Id > 5 and exists (select e.Id from #json.items() e where e.Id > d.Id and e.Id > 5) order by d.Id",
            schemaProvider: new MixedProvider(native, scoped));
        AssertCorrelationRows(compiled, [6, 7]);
    }

    private sealed class MixedProvider(ISchemaProvider native, ISchemaProvider scoped) : ISchemaProvider
    {
        public ISchema GetSchema(string schema) => schema == "#native" ? native.GetSchema(schema) : scoped.GetSchema(schema);
    }

}
