using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
public sealed class StringExtremaExecutionStrategyTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow(ParallelizationMode.None, "select City as Bucket, Min(Name) as Smallest, Max(Name) as Largest from #schema.rows() group by City", "")]
    [DataRow(ParallelizationMode.Full, "select City as Bucket, Min(Name) as Smallest, Max(Name) as Largest from #schema.rows() group by City", "")]
    [DataRow(ParallelizationMode.None, "select City as Bucket, Min(Name) filter (where Name <> '') as Smallest, Max(Name) filter (where Name <> '') as Largest from #schema.rows() group by City", "a")]
    [DataRow(ParallelizationMode.Full, "select City as Bucket, Min(Name) filter (where Name <> '') as Smallest, Max(Name) filter (where Name <> '') as Largest from #schema.rows() group by City", "a")]
    public void GroupedStringExtrema_ShouldMergeChunksWithSelectedStrategy(ParallelizationMode mode, string query, string minimumA)
    {
        var provider = CreateSchemaProvider();
        var options = new CompilationOptions(mode, maxDegreeOfParallelismOverride: 2, forceTableResultMaterialization: true);
        var inspection = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString(), provider, LoggerResolver, options);
        Assert.AreEqual(mode == ParallelizationMode.Full,
            inspection.GeneratedCSharpCode.Contains("Parallel.ForEach<IReadOnlyList<", StringComparison.Ordinal));

        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), provider, LoggerResolver, options);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Bucket", typeof(string)), ("Smallest", typeof(string)), ("Largest", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A", minimumA, "z"], ["B", null, null], ["C", "I", "i"]);
    }

    [TestMethod]
    [DataRow(ParallelizationMode.None)]
    [DataRow(ParallelizationMode.Full)]
    public void UngroupedStringExtrema_ShouldAggregateAcrossChunks(ParallelizationMode mode)
    {
        const string query = "select Min(Name) as Smallest, Max(Name) as Largest from #schema.rows()";
        var options = new CompilationOptions(mode, maxDegreeOfParallelismOverride: 2, forceTableResultMaterialization: true);
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), CreateSchemaProvider(), LoggerResolver, options);
        using var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Smallest", typeof(string)), ("Largest", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["", "z"]);
    }

    private static GenericSchemaProvider CreateSchemaProvider()
    {
        var schema = new GenericSchema<StringExtremaLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
            {
                { "rows", (new GenericEntityTable<StringExtremaEntity>(), new StringExtremaRowSource()) }
            }, executionPermissions: SourceExecutionPermissions.ConcurrentRowProcessing);
        return new GenericSchemaProvider(new Dictionary<string, ISchema> { { "#schema", schema } });
    }

    public sealed record StringExtremaEntity(string City, string? Name);

    public sealed class StringExtremaLibrary : LibraryBase;

    private sealed class StringExtremaRowSource : RowSourceBase<StringExtremaEntity>
    {
        protected override void CollectChunks(IChunkWriter<StringExtremaEntity> writer)
        {
            writer.Write([new StringExtremaEntity("A", null), new StringExtremaEntity("A", "z"), new StringExtremaEntity("B", null), new StringExtremaEntity("C", "I")]);
            writer.Write([new StringExtremaEntity("A", ""), new StringExtremaEntity("A", "a"), new StringExtremaEntity("B", null), new StringExtremaEntity("C", "i")]);
        }
    }
}
