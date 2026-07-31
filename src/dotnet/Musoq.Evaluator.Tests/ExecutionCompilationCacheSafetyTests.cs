using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ExecutionCompilationCacheSafetyTests
{
    private static ILoggerResolver LoggerResolver { get; } = new TestsLoggerResolver();

    [TestMethod]
    public void ExecutionCompilationCache_WhenSourceSignatureChanges_ShouldNotReuseStaleArtifact()
    {
        const string query = "select Id from #A.Entities()";
        var options = new CompilationOptions(ParallelizationMode.Full);
        var firstEntities = CreateEntities(1);
        var secondEntities = CreateEntities(2);
        var firstProvider = CreateProvider(firstEntities);
        var secondProvider = CreateProvider(secondEntities);

        var first = InstanceCreator.CompileForExecution(
            query,
            "ExecutionCompilationCacheSourceSignature",
            firstProvider,
            LoggerResolver,
            options);
        var firstTable = TableMaterializationTestHelper.Materialize(first.Run());

        var second = InstanceCreator.CompileForExecution(
            query,
            "ExecutionCompilationCacheSourceSignature",
            secondProvider,
            LoggerResolver,
            options);
        var secondTable = TableMaterializationTestHelper.Materialize(second.Run());

        Assert.AreEqual(1, firstTable.Count);
        Assert.AreEqual(2, secondTable.Count);
        CollectionAssert.AreEqual(new[] { 0 }, firstTable.Select(row => (int)row[0]).ToArray());
        CollectionAssert.AreEqual(new[] { 0, 1 }, secondTable.Select(row => (int)row[0]).ToArray());
    }

    private static BasicSchemaProvider<BasicEntity> CreateProvider(List<BasicEntity> entities)
    {
        return new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", entities }
        });
    }

    private static List<BasicEntity> CreateEntities(int count)
    {
        return Enumerable.Range(0, count)
            .Select(static id => new BasicEntity { Id = id, Name = $"Entity_{id}" })
            .ToList();
    }
}
