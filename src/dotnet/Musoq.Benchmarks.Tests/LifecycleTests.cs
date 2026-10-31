using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks.Scenarios;
using Musoq.Converter;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class LifecycleTests
{
    [TestMethod]
    public void TypedRegistrations_AllHaveIndependentResultsetOracles()
    {
        Type[] covered = [typeof(ProjectionOutput), typeof(GroupOutput), typeof(WindowOutput)];
        CollectionAssert.AreEquivalent(covered, BenchmarkRegistrations.All.Where(item => item.ResultApi == "Typed").Select(item => item.OutputType).ToArray());
    }

    [TestMethod]
    public void TypedProjection_ReturnsTheSharedQueryResult()
    {
        var query = Typed<ProjectionOutput>();
        ProjectionOutput[] expected = [new(0, "item-0"), new(1, "item-1"), new(2, "item-2"), new(3, "item-3")];
        CollectionAssert.AreEqual(expected, query.Run().ToArray());
        CollectionAssert.AreEqual(expected, query.Run().ToArray());
    }

    [TestMethod]
    public void TypedGroup_ReturnsTheSharedQueryResult()
    {
        var query = Typed<GroupOutput>();
        GroupOutput[] expected = [new(0, 1m, 0), new(1, 2m, 0), new(2, 3m, 1), new(3, 4m, 0)];
        CollectionAssert.AreEqual(expected, query.Run().ToArray());
        CollectionAssert.AreEqual(expected, query.Run().ToArray());
    }

    [TestMethod]
    public void TypedWindow_ReturnsTheSharedQueryResult()
    {
        var query = Typed<WindowOutput>();
        WindowOutput[] expected = [new(0, 1m, 1m), new(1, 3m, 1m), new(2, 5m, 2m), new(3, 7m, 3m)];
        CollectionAssert.AreEqual(expected, query.Run().ToArray());
        CollectionAssert.AreEqual(expected, query.Run().ToArray());
    }

    [TestMethod]
    [DynamicData(nameof(ArtifactCases))]
    public void Artifacts_ReloadSharedQueryAndPreserveResultValues(string id)
    {
        var scenario = ScenarioCatalog.Get(id);
        var provider = new FixtureProvider(FixtureData.TestSize);
        var artifact = QueryArtifacts.Create(scenario, provider);
        using var loaded = QueryArtifacts.Load(scenario, artifact, provider);
        ResultAssertions.Execution(Expectations.Get(id), loaded, scenario);
        ResultAssertions.Execution(Expectations.Get(id), loaded, scenario);
    }

    public static IEnumerable<object[]> ArtifactCases => ScenarioCatalog.ArtifactIds.Select(id => new object[] { id });

    private static Musoq.Evaluator.CompiledTypedQuery<T> Typed<T>()
    {
        var scenario = ScenarioCatalog.Get(BenchmarkRegistrations.TypedScenario<T>());
        return InstanceCreator.CompileForTypedExecution<T>(scenario.Sql, "typed-test", new FixtureProvider(FixtureData.TestSize),
            BenchmarkLogger.Instance, ExecutionSettings.Create(scenario));
    }
}
