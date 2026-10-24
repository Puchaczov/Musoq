using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.SourcePlanning;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class SourceRegexCapabilityE2ETests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow(@"\AAl", new[] { 1, 3 })]
    [DataRow("ph", new[] { 1 })]
    [DataRow(@"\A(Alpha|Beta)\z", new[] { 1, 2 })]
    [DataRow("[A-Z][a-z]+", new[] { 1, 2, 3 })]
    [DataRow(@"\A(ab)\1\z", new[] { 4 })]
    [DataRow(@"(?i)\Aalpha\z", new[] { 1 })]
    [DataRow("", new[] { 1, 2, 3, 4, 5, 7, 8, 9, 10, 11, 12 })]
    [DataRow("\u017b\u00f3\u0142\u0107", new[] { 5 })]
    [DataRow(@"(?s)\Aa.b\z", new[] { 8 })]
    public void ConstantRegex_ShouldFilterAtSourceAndMatchIndependentExpectedRows(string pattern, int[] expected)
    {
        var query = $"select s.Id from #sp.items() s where s.Name rlike r'{pattern}' order by s.Id";
        var provider = Provider();
        AssertRows(query, provider, expected);
        AssertRows(query, new RegexPlanningSchemaProvider(Rows()) { RejectAll = true }, expected);
        var match = Assert.IsInstanceOfType<SourcePredicateRegexMatch>(provider.Recorder.Requests.Single().Predicate);
        Assert.AreEqual(pattern, match.Pattern);
        Assert.AreEqual(global::Musoq.Evaluator.Runtime.RuntimeCacheOptions.DefaultRegexTimeout, match.MatchTimeout);
        Assert.AreEqual(expected.Length, provider.Recorder.SourceRowsProduced);
        Assert.HasCount(1, provider.Recorder.ExecutionPlans.Single().PredicateApplications);
        var inspection = Inspect(query, Provider());
        Assert.DoesNotContain("PhysicalFilter", inspection.PhysicalPlanText);
        Assert.DoesNotContain("Operators.PrepareRLike", inspection.GeneratedCSharpCode);
        Assert.Contains("source regex-match: column=Name", inspection.PlanningText);
        Assert.Contains("semantics=MusoqRLike", inspection.PlanningText);
        Assert.Contains("acceptedPhase=RowFiltering, fallback=none", inspection.PlanningText);
    }

    [TestMethod]
    public void NotRLike_ShouldIncludeNullInputAndPreserveNegation()
    {
        const string query = @"select s.Id from #sp.items() s where s.Name not rlike r'\AAl' order by s.Id";
        int[] expected = [2, 4, 5, 6, 7, 8, 9, 10, 11, 12];
        var provider = Provider();
        AssertRows(query, provider, expected);
        AssertRows(query, new RegexPlanningSchemaProvider(Rows()) { RejectAll = true }, expected);
        Assert.IsTrue(((SourcePredicateRegexMatch)provider.Recorder.Requests.Single().Predicate!).IsNegated);
    }

    [TestMethod]
    public void MixedConjuncts_ShouldAcceptRegexAndLikeAndComparison()
    {
        const string query = "select s.Id from #sp.items() s where s.Name rlike 'Al' and s.Name like 'Al%' and s.Id > 1 order by s.Id";
        var provider = Provider();
        AssertRows(query, provider, [3]);
        Assert.HasCount(2, provider.Recorder.ExecutionPlans.Single().PredicateApplications);
        Assert.DoesNotContain("PhysicalFilter", Inspect(query, Provider()).PhysicalPlanText);
    }

    [TestMethod]
    public void ProviderMayRejectOneRegexConjunctAndKeepItsCoreFilter()
    {
        const string query = "select s.Id from #sp.items() s where s.Name rlike 'Al' and s.Name rlike 'pha' order by s.Id";
        var provider = new RegexPlanningSchemaProvider(Rows())
        {
            PlanFactory = request =>
            {
                var conjunction = (SourcePredicateLogical)request.Predicate!;
                var accepted = SourcePlanResult.AcceptAll(request with { Predicate = conjunction.Left });
                return accepted with { ResidualPredicate = conjunction.Right };
            }
        };
        AssertRows(query, provider, [1]);
        Assert.HasCount(1, provider.Recorder.ExecutionPlans.Single().PredicateApplications);
        Assert.AreEqual(2, provider.Recorder.SourceRowsProduced);
        var inspection = Inspect(query, provider);
        Assert.Contains("PhysicalFilter", inspection.PhysicalPlanText);
        Assert.Contains("fallback=provider-residual", inspection.PlanningText);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(99)]
    public void MissingOrUnknownCapabilities_ShouldKeepRegexOutOfProviderRequest(int version)
    {
        const string query = "select s.Id from #sp.items() s where s.Name rlike 'Al' order by s.Id";
        var provider = new RegexPlanningSchemaProvider(Rows())
        {
            Capabilities = version == 0 ? SourcePredicateCapabilities.None : Provider().Capabilities with { ContractVersion = version }
        };
        AssertRows(query, provider, [1, 3]);
        Assert.IsNull(provider.Recorder.Requests.Single().Predicate);
        Assert.IsEmpty(provider.Recorder.ExecutionPlans.Single().PredicateApplications);
        Assert.Contains("PhysicalFilter", Inspect(query, provider).PhysicalPlanText);
    }

    [TestMethod]
    public void UnsupportedNegation_ShouldRemainResidual()
    {
        const string query = @"select s.Id from #sp.items() s where s.Name not rlike 'Al' order by s.Id";
        var provider = new RegexPlanningSchemaProvider(Rows())
        {
            Capabilities = new SourcePredicateCapabilities
            {
                RegexMatches = [new SourceRegexMatchCapability(new SourceColumnRef("Name"))]
            }
        };
        AssertRows(query, provider, [2, 4, 5, 6, 7, 8, 9, 10, 11, 12]);
        Assert.IsNull(provider.Recorder.Requests.Single().Predicate);
        Assert.Contains("fallback=unsupported-negation", Inspect(query, provider).PlanningText);
    }

    [TestMethod]
    [DataRow("s.Name rlike 'Al' or s.Name rlike 'Beta'", new[] { 1, 2, 3 })]
    [DataRow("s.Name rlike s.Category", new[] { 1, 3 })]
    [DataRow("s.Name rlike null", new int[0])]
    [DataRow("s.ToUpper(s.Name) rlike 'AL'", new[] { 1, 3 })]
    public void UnsupportedShapes_ShouldRemainCoreFilters(string predicate, int[] expected)
    {
        var query = $"select s.Id from #sp.items() s where {predicate} order by s.Id";
        var provider = Provider();
        AssertRows(query, provider, expected);
        Assert.IsNull(provider.Recorder.Requests.Single().Predicate);
        Assert.IsEmpty(provider.Recorder.ExecutionPlans.Single().PredicateApplications);
    }

    [TestMethod]
    public void CrossSourcePattern_ShouldNotBePassedToEitherSource()
    {
        const string query = "select a.Id, b.Id from #sp.items() a inner join #other.items() b on a.Id = b.Id where a.Name rlike b.Category order by a.Id";
        var provider = Provider();
        using var compiled = CreateAndRunVirtualMachine(query, schemaProvider: provider);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(result, [1, 1], [3, 3]);
        Assert.IsTrue(provider.Recorder.Requests.All(request => request.Predicate == null));
    }

    [TestMethod]
    public void Culture_ShouldBeResolvedDuringExecutionRatherThanCompilation()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            const string query = @"select s.Id from #sp.items() s where s.Name rlike r'(?i)\AI\z' order by s.Id";
            using var pushed = CreateAndRunVirtualMachine(query, schemaProvider: Provider());
            using var baseline = CreateAndRunVirtualMachine(query,
                schemaProvider: new RegexPlanningSchemaProvider(Rows()) { RejectAll = true });
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            using (var result = pushed.Run(TestContext.CancellationToken))
                TableMaterializationTestHelper.AssertRowsInOrder(result, [9], [10]);
            using (var result = baseline.Run(TestContext.CancellationToken))
                TableMaterializationTestHelper.AssertRowsInOrder(result, [9], [10]);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            using (var result = pushed.Run(TestContext.CancellationToken))
                TableMaterializationTestHelper.AssertRowsInOrder(result, [9], [11], [12]);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [TestMethod]
    public void InvalidConstant_ShouldFailBeforeNegotiation()
    {
        var provider = Provider();
        var failure = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine("select s.Id from #sp.items() s where s.Name rlike '['", schemaProvider: provider));
        Assert.Contains("MQ3094", failure.Message);
        Assert.IsEmpty(provider.Recorder.Requests);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SourceReadFailure_ShouldPreserveOnlyTypedRegexTimeoutDiagnostic(bool typedRegex)
    {
        var provider = new RegexPlanningSchemaProvider(Rows())
        {
            FailureFactory = () => typedRegex
                ? new RegexExecutionTimeoutException("RLIKE", "Backtracking",
                    new RegexMatchTimeoutException("secret input", "secret pattern", TimeSpan.FromMilliseconds(17)))
                : new InvalidOperationException("ordinary provider failure")
        };
        using var compiled = CreateAndRunVirtualMachine(
            "select s.Id from #sp.items() s where s.Name rlike 'Al'", schemaProvider: provider);
        var failure = Assert.Throws<QueryExecutionException>(() =>
        {
            using var result = compiled.Run(TestContext.CancellationToken);
            _ = result.Count;
        });
        Assert.AreEqual(typedRegex ? DiagnosticCode.MQ7017_RegexMatchTimeout : DiagnosticCode.MQ7011_DataSourceReadFailed,
            failure.Envelope!.Code);
        var lifecycle = Assert.IsInstanceOfType<DataSourceLifecycleException>(failure.InnerException);
        Assert.AreEqual("read", lifecycle.Operation);
        Assert.AreEqual("s", lifecycle.Alias);
        Assert.IsNotEmpty(lifecycle.SourceContextId);
        Assert.IsNotNull(lifecycle.InnerException);
        Assert.DoesNotContain("secret input", failure.FormatJson());
        Assert.DoesNotContain("secret pattern", failure.FormatJson());
    }

    [TestMethod]
    [DataRow("missing", "missing an evaluation application")]
    [DataRow("duplicate", "was not accepted")]
    [DataRow("pattern", "was not accepted")]
    [DataRow("timeout", "was not accepted")]
    [DataRow("negation", "was not accepted")]
    [DataRow("phase", "unadvertised capability or phase")]
    [DataRow("partition", "not an exact partition")]
    public void MalformedRegexApplication_ShouldFailBeforeOpeningSource(string mutation, string expectedMessage)
    {
        var provider = new RegexPlanningSchemaProvider(Rows())
        {
            PlanFactory = request =>
            {
                var accepted = SourcePlanResult.AcceptAll(request);
                var match = (SourcePredicateRegexMatch)request.Predicate!;
                var changed = new SourcePredicateRegexMatch(match.Column,
                    mutation == "pattern" ? "changed" : match.Pattern,
                    mutation == "timeout" ? TimeSpan.FromMilliseconds(17) : match.MatchTimeout,
                    match.Semantics, mutation == "negation" || match.IsNegated);
                var application = new SourcePredicateApplication(changed,
                    mutation == "phase" ? SourcePredicateEvaluationPhase.CandidateMetadata : SourcePredicateEvaluationPhase.RowFiltering);
                return accepted with
                {
                    ResidualPredicate = mutation == "partition" ? match : null,
                    ExecutionPlan = accepted.ExecutionPlan with
                    {
                        PredicateApplications = mutation switch
                        {
                            "missing" => [],
                            "duplicate" => [application, application],
                            _ => [application]
                        }
                    }
                };
            }
        };
        var failure = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(
            "select s.Id from #sp.items() s where s.Name rlike 'Al'", schemaProvider: provider));
        Assert.Contains(expectedMessage, failure.Message);
        Assert.IsEmpty(provider.Recorder.ExecutionPlans);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(99)]
    public void FabricatedApplicationWithoutKnownCapability_ShouldFail(int version)
    {
        var provider = new RegexPlanningSchemaProvider(Rows())
        {
            Capabilities = Provider().Capabilities with { ContractVersion = version },
            PlanFactory = request => SourcePlanResult.RejectAll(request) with
            {
                ExecutionPlan = SourcePlanResult.RejectAll(request).ExecutionPlan with
                {
                    PredicateApplications = [new SourcePredicateApplication(
                        new SourcePredicateRegexMatch(new SourceColumnRef("Name"), "Al", TimeSpan.FromSeconds(1)),
                        SourcePredicateEvaluationPhase.RowFiltering)]
                }
            }
        };
        var failure = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(
            "select s.Id from #sp.items() s where s.Name rlike 'Al'", schemaProvider: provider));
        Assert.Contains("unadvertised capability or phase", failure.Message);
        Assert.IsEmpty(provider.Recorder.ExecutionPlans);
    }

    [TestMethod]
    [DataRow("inner join")]
    [DataRow("left outer join")]
    [DataRow("full outer join")]
    public void RegexAfterJoin_ShouldRetainItsEvaluationBoundary(string join)
    {
        var query = $"select a.Id from #sp.items() a {join} #other.items() b on a.Id = b.Id where a.Name rlike 'Al' order by a.Id";
        var provider = Provider();
        AssertRows(query, provider, [1, 3]);
        Assert.IsTrue(provider.Recorder.Requests.All(request => request.Predicate == null));
        Assert.Contains("PhysicalFilter", Inspect(query, provider).PhysicalPlanText);
    }

    [TestMethod]
    public void VolatileSourceColumn_ShouldNeverBeOfferedForRegexPushdown()
    {
        var provider = new RegexPlanningSchemaProvider(Rows()) { VolatileName = true };
        const string query = "select s.Id from #sp.items() s where s.Name rlike 'Al' order by s.Id";
        AssertRows(query, provider, [1, 3]);
        Assert.IsNull(provider.Recorder.Requests.Single().Predicate);
        Assert.IsEmpty(provider.Recorder.ExecutionPlans.Single().PredicateApplications);
    }

    private void AssertRows(string query, RegexPlanningSchemaProvider provider, int[] expected)
    {
        using var compiled = CreateAndRunVirtualMachine(query, schemaProvider: provider);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(result,
            expected.Select(id => new object?[] { id }).ToArray());
    }

    private QueryInspectionResult Inspect(string query, RegexPlanningSchemaProvider provider) =>
        InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString(), provider, LoggerResolver, TestCompilationOptions);

    private static RegexPlanningSchemaProvider Provider() => new(Rows());

    private static IReadOnlyList<SourcePlanningEntity> Rows() =>
        new string?[] { "Alpha", "Beta", "Alpine", "abab", "\u017b\u00f3\u0142\u0107", null, "", "a\nb", "I", "\u0131", "i", "\u0130" }
            .Select((name, index) => new SourcePlanningEntity { Id = index + 1, Name = name, Category = @"\AAl" }).ToArray();
}
