using BenchmarkDotNet.Attributes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class DynamicLikeBenchmarkTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void BenchmarkMatrices_ShouldCoverRequiredCardinalitiesLengthsDomainsAndFanOuts()
    {
        CollectionAssert.AreEqual(
            new object[] { 1, 2, 64, 512, 4096 },
            Parameters<DynamicLikeExecutionBenchmark>(nameof(DynamicLikeExecutionBenchmark.PatternCardinality)));
        CollectionAssert.AreEqual(
            Enum.GetValues<DynamicLikeBenchmarkScenario>().Cast<object>().ToArray(),
            Parameters<DynamicLikeExecutionBenchmark>(nameof(DynamicLikeExecutionBenchmark.Scenario)));
        CollectionAssert.AreEqual(
            new object[] { 4, 64, 4096 },
            Parameters<DynamicLikeInputLengthBenchmark>(nameof(DynamicLikeInputLengthBenchmark.InputLength)));
        CollectionAssert.AreEqual(
            Enum.GetValues<DynamicLikeBenchmarkScenario>().Cast<object>().ToArray(),
            Parameters<DynamicLikeInputLengthBenchmark>(nameof(DynamicLikeInputLengthBenchmark.Scenario)));
        CollectionAssert.AreEqual(
            new object[] { 1, 8, 64 },
            Parameters<DynamicLikeApplyBenchmark>(nameof(DynamicLikeApplyBenchmark.FanOut)));
    }

    [TestMethod]
    public void BenchmarkFixtures_ShouldBeUnsealedForBenchmarkDotNetInvocation()
    {
        var benchmarkTypes = new[]
        {
            typeof(DynamicLikeExecutionBenchmark),
            typeof(DynamicLikeInputLengthBenchmark),
            typeof(DynamicLikeApplyBenchmark),
            typeof(DynamicLikeCompilationBenchmark)
        };

        foreach (var benchmarkType in benchmarkTypes)
            Assert.IsFalse(benchmarkType.IsSealed, benchmarkType.FullName);
    }

    [TestMethod]
    [DataRow(DynamicLikeBenchmarkScenario.Ascii, 1, "AA3E9ABE832C6CB723EE3C2D2CE038DCF4C6A294E411623909AE33AE93E6ED99")]
    [DataRow(DynamicLikeBenchmarkScenario.Ascii, 4096, "B249F853D8A9248084FE14A9A05CEA4A62E21291A4DFDFD006D2F8A87812DF0B")]
    [DataRow(DynamicLikeBenchmarkScenario.Unicode, 1, "6938C7AE97D14F3B59861647548F3D6A297FC21B8D853D4CE190BD982183A642")]
    [DataRow(DynamicLikeBenchmarkScenario.Unicode, 4096, "C79EEAFE47E7ABEC003E4A1982A09D238A8D3F6DE52AD36C26929F82D3140464")]
    [DataRow(DynamicLikeBenchmarkScenario.Wildcard, 1, "703E28D3B8952D9AD8961F69728D89B18E895018D894BE84D2E11E405679CA2F")]
    [DataRow(DynamicLikeBenchmarkScenario.Wildcard, 4096, "E87AEE3ECEF437015DF526BE6B048A57D530E3EE6ABF844F304A543D97664381")]
    public void DynamicExecution_ShouldPreserveCountAndHashAcrossRepeatedRuns(
        DynamicLikeBenchmarkScenario scenario,
        int cardinality,
        string expectedHash)
    {
        using var benchmark = new DynamicLikeExecutionBenchmark
        {
            RowCount = 4096,
            PatternCardinality = cardinality,
            Scenario = scenario
        };
        benchmark.Setup();

        using var result = benchmark.DynamicLike_Run();

        Assert.HasCount(4096, result);
        Assert.AreEqual(expectedHash, benchmark.ResultHash);
        Assert.AreEqual(benchmark.ResultHash, DynamicLikeBenchmarkData.ComputeResultHash(result));
        Assert.AreEqual(cardinality <= 512 ? 0 : cardinality, benchmark.BaselineMatcherConstructions);
        TestContext.WriteLine($"dynamic/{scenario}/cardinality-{cardinality}: {benchmark.ResultHash}");
    }

    [TestMethod]
    [DataRow(512, 0)]
    [DataRow(513, 513)]
    public void BaselineMatcherConstructionCounter_ShouldUseTheRuntimePatternCacheCapacity(
        int cardinality,
        int expectedConstructions)
    {
        var benchmark = new DynamicLikeExecutionBenchmark
        {
            PatternCardinality = cardinality
        };

        Assert.AreEqual(expectedConstructions, benchmark.BaselineMatcherConstructions);
    }

    [TestMethod]
    [DataRow(DynamicLikeBenchmarkScenario.Ascii, 4, "73AC27490A77120DAF73FE07B4A484A0F5859EEAE91B75CD19A31BEE22B14213")]
    [DataRow(DynamicLikeBenchmarkScenario.Unicode, 64, "1A8ADF933AFF96E02957FD2487E15EF714EE33544C87DDE97C460FA672992354")]
    [DataRow(DynamicLikeBenchmarkScenario.Wildcard, 4096, "C2D90865D7EB218BB0CA45A924A1A6BD1688E007DDCE8DCAD223F3BFD29C29FF")]
    public void InputLengthExecution_ShouldPreserveCountAndHashAcrossRepeatedRuns(
        DynamicLikeBenchmarkScenario scenario,
        int inputLength,
        string expectedHash)
    {
        using var benchmark = new DynamicLikeInputLengthBenchmark
        {
            RowCount = 128,
            InputLength = inputLength,
            Scenario = scenario
        };
        benchmark.Setup();

        using var result = benchmark.DynamicLike_ByInputLength();

        Assert.HasCount(128, result);
        Assert.AreEqual(expectedHash, benchmark.ResultHash);
        Assert.AreEqual(benchmark.ResultHash, DynamicLikeBenchmarkData.ComputeResultHash(result));
        TestContext.WriteLine($"length/{scenario}/{inputLength}: {benchmark.ResultHash}");
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(8)]
    [DataRow(64)]
    public void CorrelatedApply_ShouldPreserveResultsAndReportExactSourceWork(int fanOut)
    {
        using var benchmark = new DynamicLikeApplyBenchmark
        {
            OuterRowCount = 8,
            FanOut = fanOut
        };
        benchmark.Setup();
        var expectedHash = fanOut switch
        {
            1 => "4EC523B20A528DF7A577BA3A83BD973A41F5843B5E2ED45E2C87C24B04067D2D",
            8 => "4DCDDFB18DFCF47D713191D5A6EA6C03C6950047B0A50DC9B9340C7FF8620590",
            64 => "0E42E91377154960BDC8D5692EF181A60CA63CABDB1AE8A3ECB639EB12756645",
            _ => throw new AssertFailedException($"Unexpected fan-out {fanOut}.")
        };

        benchmark.ResetCounters();
        using var inner = benchmark.DynamicLikeApply_InnerPattern();
        Assert.HasCount(benchmark.ExpectedResultCount, inner);
        Assert.AreEqual(expectedHash, benchmark.InnerPatternResultHash);
        Assert.AreEqual(benchmark.InnerPatternResultHash, DynamicLikeBenchmarkData.ComputeResultHash(inner));
        Assert.AreEqual(1, benchmark.SourceInvocations);
        Assert.AreEqual(8, benchmark.ChildrenReads);
        Assert.AreEqual(0, benchmark.PatternReads);
        TestContext.WriteLine($"apply/inner/fanout-{fanOut}: {benchmark.InnerPatternResultHash}");

        benchmark.ResetCounters();
        using var outer = benchmark.DynamicLikeApply_OuterPattern();
        Assert.HasCount(benchmark.ExpectedResultCount, outer);
        Assert.AreEqual(expectedHash, benchmark.OuterPatternResultHash);
        Assert.AreEqual(benchmark.OuterPatternResultHash, DynamicLikeBenchmarkData.ComputeResultHash(outer));
        Assert.AreEqual(1, benchmark.SourceInvocations);
        Assert.AreEqual(8, benchmark.ChildrenReads);
        Assert.AreEqual(8, benchmark.PatternReads);
        TestContext.WriteLine($"apply/outer/fanout-{fanOut}: {benchmark.OuterPatternResultHash}");
    }

    [TestMethod]
    public void GeneratedArtifacts_ShouldKeepSameRowAndInnerApplyPatternsDynamic()
    {
        var dynamicInspection = DynamicLikeExecutionBenchmark.InspectQuery();
        var applyInspections = DynamicLikeApplyBenchmark.InspectQueries();

        foreach (var inspection in new[]
                 {
                     dynamicInspection,
                     applyInspections.Inner
                 })
        {
            Assert.IsTrue(
                ContainsValue<ExecutionDynamicLikeMatch>(inspection.ExecutionPlan),
                inspection.OptimizedExecutionPlanText);
            Assert.Contains("Operators.LikeDynamic", inspection.GeneratedCSharpCode);
            Assert.Contains(
                "new Musoq.Evaluator.LikeMatcherCacheSlot(false)",
                inspection.GeneratedCSharpCode,
                inspection.GeneratedCSharpCode);
            Assert.IsFalse(
                inspection.GeneratedCSharpCode.Contains("new Musoq.Evaluator.Operators().Like", StringComparison.Ordinal),
                inspection.GeneratedCSharpCode);
        }
    }

    [TestMethod]
    public void GeneratedArtifacts_WhenDynamicLikeProjectionIsSerial_ShouldYieldFinalRowsDirectly()
    {
        var inspections = new[]
        {
            DynamicLikeExecutionBenchmark.InspectQuery(),
            DynamicLikeApplyBenchmark.InspectQueries().Inner
        };

        foreach (var inspection in inspections)
        {
            Assert.Contains(
                "private IEnumerable<ResultRow0> ComputeShapeRows_compiled_0(",
                inspection.GeneratedCSharpCode,
                inspection.GeneratedCSharpCode);
            Assert.Contains(
                "yield return new ResultRow0(",
                inspection.GeneratedCSharpCode,
                inspection.GeneratedCSharpCode);
            Assert.IsFalse(
                inspection.GeneratedCSharpCode.Contains("class ResultShape0", StringComparison.Ordinal),
                inspection.GeneratedCSharpCode);
            Assert.IsFalse(
                inspection.GeneratedCSharpCode.Contains("new ResultShape0(", StringComparison.Ordinal),
                inspection.GeneratedCSharpCode);
        }
    }

    [TestMethod]
    public void GeneratedArtifacts_ShouldPrepareOuterApplyPatternInsideOuterLoop()
    {
        var inspection = DynamicLikeApplyBenchmark.InspectQueries().Outer;

        Assert.IsTrue(
            ContainsValue<ExecutionPreparedLikeMatch>(inspection.ExecutionPlan),
            inspection.OptimizedExecutionPlanText);
        Assert.IsTrue(
            ContainsValue<ExecutionPrepareLikeMatcher>(inspection.ExecutionPlan),
            inspection.OptimizedExecutionPlanText);
        Assert.IsFalse(
            ContainsValue<ExecutionDynamicLikeMatch>(inspection.ExecutionPlan),
            inspection.OptimizedExecutionPlanText);
        Assert.Contains("Operators.PrepareLike", inspection.GeneratedCSharpCode);
        Assert.Contains("Operators.LikePrepared", inspection.GeneratedCSharpCode);
        Assert.IsFalse(
            inspection.GeneratedCSharpCode.Contains("new Musoq.Evaluator.LikeMatcherCacheSlot", StringComparison.Ordinal),
            inspection.GeneratedCSharpCode);
        Assert.IsFalse(
            inspection.GeneratedCSharpCode.Contains("new Musoq.Evaluator.Operators().Like", StringComparison.Ordinal),
            inspection.GeneratedCSharpCode);

        var outerItemIndex = inspection.GeneratedCSharpCode.IndexOf("var m1 =", StringComparison.Ordinal);
        var prepareIndex = inspection.GeneratedCSharpCode.IndexOf(
            "PreparedLikeMatcher __likeMatcher",
            StringComparison.Ordinal);
        var innerSetupIndex = inspection.GeneratedCSharpCode.IndexOf(
            "EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Benchmarks.DynamicLikeApplyChild>(m1.Children)",
            StringComparison.Ordinal);
        var innerItemIndex = inspection.GeneratedCSharpCode.IndexOf("var m2 =", StringComparison.Ordinal);
        var matchIndex = inspection.GeneratedCSharpCode.IndexOf("Operators.LikePrepared", StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, outerItemIndex, inspection.GeneratedCSharpCode);
        Assert.IsGreaterThan(outerItemIndex, prepareIndex, inspection.GeneratedCSharpCode);
        Assert.IsGreaterThan(prepareIndex, innerSetupIndex, inspection.GeneratedCSharpCode);
        Assert.IsGreaterThan(innerSetupIndex, innerItemIndex, inspection.GeneratedCSharpCode);
        Assert.IsGreaterThan(innerItemIndex, matchIndex, inspection.GeneratedCSharpCode);
    }

    private static bool ContainsValue<T>(object? root)
    {
        var pending = new Stack<object>();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        if (root is not null)
            pending.Push(root);

        while (pending.TryPop(out var current))
        {
            if (current is T)
                return true;
            if (!visited.Add(current))
                continue;

            foreach (var property in current.GetType().GetProperties())
            {
                if (property.GetIndexParameters().Length != 0)
                    continue;

                var value = property.GetValue(current);
                if (IsExecutionIrValue(value))
                    pending.Push(value!);
                else if (value is System.Collections.IEnumerable values and not string)
                {
                    foreach (var item in values)
                    {
                        if (IsExecutionIrValue(item))
                            pending.Push(item!);
                    }
                }
            }
        }

        return false;
    }

    private static bool IsExecutionIrValue(object? value) =>
        value?.GetType().Namespace == typeof(ExecutionPlan).Namespace;

    private static object[] Parameters<TBenchmark>(string propertyName)
    {
        var property = typeof(TBenchmark).GetProperty(propertyName)
            ?? throw new AssertFailedException($"Missing property {typeof(TBenchmark).Name}.{propertyName}.");
        var attribute = property.GetCustomAttributes(typeof(ParamsAttribute), inherit: false)
            .Cast<ParamsAttribute>()
            .Single();
        return attribute.Values.Select(static value => value!).ToArray();
    }
}
