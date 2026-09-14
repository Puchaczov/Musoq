using BenchmarkDotNet.Attributes;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class RLikeBenchmarkTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void BenchmarkMatrices_ShouldCoverCardinalityLengthDomainAndApplyFanOut()
    {
        CollectionAssert.AreEqual(
            new object[] { 1, 2, 64, 512, 4096 },
            Parameters<RLikeExecutionBenchmark>(nameof(RLikeExecutionBenchmark.PatternCardinality)));
        CollectionAssert.AreEqual(
            Enum.GetValues<RLikeBenchmarkScenario>().Cast<object>().ToArray(),
            Parameters<RLikeExecutionBenchmark>(nameof(RLikeExecutionBenchmark.Scenario)));
        CollectionAssert.AreEqual(
            new object[] { 4, 64, 4096 },
            Parameters<RLikeInputLengthBenchmark>(nameof(RLikeInputLengthBenchmark.InputLength)));
        CollectionAssert.AreEqual(
            new object[] { 1, 8, 64 },
            Parameters<RLikeApplyBenchmark>(nameof(RLikeApplyBenchmark.FanOut)));
    }

    [TestMethod]
    public void DynamicBenchmark_ShouldPreserveCountAndHashAcrossRepeatedRuns()
    {
        using var benchmark = new RLikeExecutionBenchmark
        {
            RowCount = 128,
            PatternCardinality = 64,
            Scenario = RLikeBenchmarkScenario.Complex
        };
        benchmark.Setup();

        using var result = benchmark.DynamicRLike_Run();

        Assert.HasCount(128, result);
        Assert.AreEqual(benchmark.ResultHash, DynamicLikeBenchmarkData.ComputeResultHash(result));
        TestContext.WriteLine($"dynamic/Complex/cardinality-64: {benchmark.ResultHash}");
    }

    [TestMethod]
    [DataRow(RLikeBenchmarkScenario.Literal, 1, "97BB7DF94C7C0EF5A23F5618375614542D2B4984996123026A2D2E6EA5806FC5")]
    [DataRow(RLikeBenchmarkScenario.Unicode, 4096, "845B7FCC1EC1FBF761F14C4CFC083053F884A3F0DE3B509731DB5103EA45ABF7")]
    [DataRow(RLikeBenchmarkScenario.Complex, 4096, "BAA9D8F5308F655395ECB617F6E40318D574CDFB547A03452D903B97F1CF9A54")]
    public void DynamicQualificationFixture_ShouldExposeStableResultHash(
        RLikeBenchmarkScenario scenario,
        int cardinality,
        string expectedHash)
    {
        using var benchmark = new RLikeExecutionBenchmark
        {
            RowCount = 4096,
            PatternCardinality = cardinality,
            Scenario = scenario
        };
        benchmark.Setup();

        using var result = benchmark.DynamicRLike_Run();

        Assert.HasCount(4096, result);
        Assert.AreEqual(expectedHash, benchmark.ResultHash);
        Assert.AreEqual(benchmark.ResultHash, DynamicLikeBenchmarkData.ComputeResultHash(result));
        TestContext.WriteLine($"dynamic/{scenario}/cardinality-{cardinality}: {benchmark.ResultHash}");
    }

    [TestMethod]
    [DataRow(RLikeBenchmarkScenario.Literal, "EB92153F3EB9467184CFC367C3F9599F848AE308B12223DD27E61F36C563A38F")]
    [DataRow(RLikeBenchmarkScenario.Complex, "2B2D5BD560EA87D595E4E5AA002C5DC1DD263630E5E6D66FE514F006CEA685DE")]
    [DataRow(RLikeBenchmarkScenario.Unicode, "B2CBF8A05718911676F94DE10A30E4036F749528436A330F70BBF760DEB5DEFB")]
    public void ConstantQualificationFixture_ShouldExposeStableResultHash(
        RLikeBenchmarkScenario scenario,
        string expectedHash)
    {
        using var benchmark = new RLikeConstantBenchmark
        {
            RowCount = 128,
            Scenario = scenario
        };
        benchmark.Setup();

        using var result = benchmark.ConstantRLike_Run();

        Assert.HasCount(128, result);
        Assert.AreEqual(expectedHash, benchmark.ResultHash);
        Assert.AreEqual(benchmark.ResultHash, DynamicLikeBenchmarkData.ComputeResultHash(result));
        TestContext.WriteLine($"constant/{scenario}: {benchmark.ResultHash}");
    }

    [TestMethod]
    [DataRow(RLikeBenchmarkScenario.Literal, 4, "074E77F2A798371DB2EDC844AB8713D45E5CBE73A75BBBED5A3B81C9906E7CD6")]
    [DataRow(RLikeBenchmarkScenario.Complex, 4096, "F15C3B2E89A0E641833D8C2C1A984C056DDE5A7C14609295B07E36B118CD2F43")]
    public void InputLengthQualificationFixture_ShouldExposeStableResultHash(
        RLikeBenchmarkScenario scenario,
        int inputLength,
        string expectedHash)
    {
        using var benchmark = new RLikeInputLengthBenchmark
        {
            RowCount = 128,
            InputLength = inputLength,
            Scenario = scenario
        };
        benchmark.Setup();

        using var result = benchmark.RLike_ByInputLength();

        Assert.HasCount(128, result);
        Assert.AreEqual(expectedHash, benchmark.ResultHash);
        Assert.AreEqual(benchmark.ResultHash, DynamicLikeBenchmarkData.ComputeResultHash(result));
        TestContext.WriteLine($"length/{scenario}/{inputLength}: {benchmark.ResultHash}");
    }

    [TestMethod]
    [DataRow(1, "4EC523B20A528DF7A577BA3A83BD973A41F5843B5E2ED45E2C87C24B04067D2D")]
    [DataRow(8, "4DCDDFB18DFCF47D713191D5A6EA6C03C6950047B0A50DC9B9340C7FF8620590")]
    [DataRow(64, "0E42E91377154960BDC8D5692EF181A60CA63CABDB1AE8A3ECB639EB12756645")]
    public void CorrelatedApplyBenchmark_ShouldPreserveResultsAndSourceWork(
        int fanOut,
        string expectedHash)
    {
        using var benchmark = new RLikeApplyBenchmark { OuterRowCount = 8, FanOut = fanOut };
        benchmark.Setup();

        benchmark.ResetCounters();
        using var inner = benchmark.RLikeApply_InnerPattern();
        Assert.HasCount(benchmark.ExpectedResultCount, inner);
        Assert.AreEqual(expectedHash, benchmark.InnerPatternResultHash);
        Assert.AreEqual(benchmark.InnerPatternResultHash, DynamicLikeBenchmarkData.ComputeResultHash(inner));
        Assert.AreEqual(1, benchmark.SourceInvocations);
        Assert.AreEqual(8, benchmark.ChildrenReads);
        Assert.AreEqual(0, benchmark.PatternReads);
        TestContext.WriteLine($"apply/inner/fanout-{fanOut}: {benchmark.InnerPatternResultHash}");

        benchmark.ResetCounters();
        using var outer = benchmark.RLikeApply_OuterPattern();
        Assert.HasCount(benchmark.ExpectedResultCount, outer);
        Assert.AreEqual(expectedHash, benchmark.OuterPatternResultHash);
        Assert.AreEqual(benchmark.OuterPatternResultHash, DynamicLikeBenchmarkData.ComputeResultHash(outer));
        Assert.AreEqual(1, benchmark.SourceInvocations);
        Assert.AreEqual(8, benchmark.ChildrenReads);
        Assert.AreEqual(8, benchmark.PatternReads);
        TestContext.WriteLine($"apply/outer/fanout-{fanOut}: {benchmark.OuterPatternResultHash}");
    }

    [TestMethod]
    public void DynamicInspection_ShouldExposeExplicitRLikeExecution()
    {
        var inspection = RLikeExecutionBenchmark.InspectQuery();

        Assert.IsTrue(ContainsValue<ExecutionDynamicRLikeMatch>(inspection.ExecutionPlan));
        Assert.IsFalse(ContainsValue<ExecutionPatternMatch>(inspection.ExecutionPlan));
        Assert.Contains("Operators.RLikeDynamic", inspection.GeneratedCSharpCode);
        Assert.Contains("new Musoq.Evaluator.RLikeMatcherCacheSlot(false)", inspection.GeneratedCSharpCode);
        Assert.DoesNotContain("new Musoq.Evaluator.Operators().RLike", inspection.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CorrelatedApplyInspection_ShouldKeepInnerPatternDynamicAndPrepareOuterPattern()
    {
        var inspections = RLikeApplyBenchmark.InspectQueries();

        Assert.IsTrue(
            ContainsValue<ExecutionDynamicRLikeMatch>(inspections.Inner.ExecutionPlan),
            inspections.Inner.OptimizedExecutionPlanText);
        Assert.IsFalse(ContainsValue<ExecutionPrepareRLikeMatcher>(inspections.Inner.ExecutionPlan));
        Assert.IsTrue(
            ContainsValue<ExecutionPrepareRLikeMatcher>(inspections.Outer.ExecutionPlan),
            inspections.Outer.OptimizedExecutionPlanText);
        Assert.IsTrue(
            ContainsValue<ExecutionPreparedRLikeMatch>(inspections.Outer.ExecutionPlan),
            inspections.Outer.OptimizedExecutionPlanText);
        Assert.IsFalse(ContainsValue<ExecutionDynamicRLikeMatch>(inspections.Outer.ExecutionPlan));
        Assert.Contains("Operators.RLikeDynamic", inspections.Inner.GeneratedCSharpCode);
        Assert.Contains("Operators.PrepareRLike", inspections.Outer.GeneratedCSharpCode);
        Assert.Contains("Operators.RLikePrepared", inspections.Outer.GeneratedCSharpCode);
        Assert.DoesNotContain("new Musoq.Evaluator.Operators().RLike", inspections.Inner.GeneratedCSharpCode);
        Assert.DoesNotContain("new Musoq.Evaluator.Operators().RLike", inspections.Outer.GeneratedCSharpCode);
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
                if (value?.GetType().Namespace == typeof(ExecutionPlan).Namespace)
                    pending.Push(value!);
                else if (value is System.Collections.IEnumerable values and not string)
                {
                    foreach (var item in values)
                    {
                        if (item?.GetType().Namespace == typeof(ExecutionPlan).Namespace)
                            pending.Push(item!);
                    }
                }
            }
        }

        return false;
    }

    private static object[] Parameters<TBenchmark>(string propertyName)
    {
        var property = typeof(TBenchmark).GetProperty(propertyName)
            ?? throw new AssertFailedException($"Missing property {typeof(TBenchmark).Name}.{propertyName}.");
        var attribute = property.GetCustomAttributes(typeof(ParamsAttribute), false)
            .Cast<ParamsAttribute>()
            .Single();
        return attribute.Values.Select(static value => value!).ToArray();
    }
}
