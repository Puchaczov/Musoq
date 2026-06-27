using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    private const string MaterializationQuery =
        "select e.Id, e.Name from #counting.entities() e where e.Id > 1";

    [TestMethod]
    public void Run_WhenTableResultMaterializationIsDefault_ShouldRemainLazy()
    {
        var provider = MaterializationCountingSchemaProvider.CreateDefault();
        var compiled = CompileForExecution(MaterializationQuery, provider);

        Assert.AreEqual(0, provider.RowsRead);

        var table = compiled.Run();

        Assert.AreEqual(0, provider.RowsRead);
        Assert.AreEqual(0, provider.ChunksRead);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(3, provider.RowsRead);
        Assert.AreEqual(1, provider.ChunksRead);
    }

    [TestMethod]
    public void Run_WhenTableResultMaterializationIsForced_ShouldMaterializeBeforeReturning()
    {
        var provider = MaterializationCountingSchemaProvider.CreateDefault();
        var compiled = CompileForExecution(
            MaterializationQuery,
            provider,
            new CompilationOptions().WithTableResultMaterialization());

        var table = compiled.Run();

        Assert.AreEqual(3, provider.RowsRead);
        Assert.AreEqual(1, provider.ChunksRead);
        Assert.AreEqual(2, table.Count);
    }

    [TestMethod]
    public void Run_WhenTableResultMaterializationIsForced_ShouldProduceSameRowsAsDefault()
    {
        var lazyProvider = MaterializationCountingSchemaProvider.CreateDefault();
        var materializedProvider = MaterializationCountingSchemaProvider.CreateDefault();
        var lazy = CompileForExecution(MaterializationQuery, lazyProvider);
        var materialized = CompileForExecution(
            MaterializationQuery,
            materializedProvider,
            new CompilationOptions().WithTableResultMaterialization());

        var lazyRows = ReadRows(lazy.Run());
        var materializedRows = ReadRows(materialized.Run());

        CollectionAssert.AreEqual(lazyRows, materializedRows);
    }

    [TestMethod]
    public void CompileForInspection_WhenTableResultMaterializationIsForced_ShouldGenerateDifferentRunMethod()
    {
        var provider = MaterializationCountingSchemaProvider.CreateDefault();
        var lazy = Inspect(MaterializationQuery, provider);
        var materialized = Inspect(
            MaterializationQuery,
            provider,
            new CompilationOptions().WithTableResultMaterialization());

        Assert.IsFalse(lazy.GeneratedCSharpCode.Contains("__musoqMaterializedTable", StringComparison.Ordinal));
        Assert.Contains("__musoqMaterializedTable", materialized.GeneratedCSharpCode);
        Assert.Contains("__musoqMaterializedTable.Count", materialized.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultQueryIsCached_ShouldNotReuseLazyRunnableForForcedMaterialization()
    {
        var provider = MaterializationCountingSchemaProvider.CreateDefault();

        var lazy = CompileForExecution(MaterializationQuery, provider);
        _ = lazy.Run();
        Assert.AreEqual(0, provider.RowsRead);

        var materialized = CompileForExecution(
            MaterializationQuery,
            provider,
            new CompilationOptions().WithTableResultMaterialization());

        _ = materialized.Run();

        Assert.AreEqual(3, provider.RowsRead);
    }

    private static string[] ReadRows(Table table)
    {
        return table
            .Select(row => string.Join("|", row[0], row[1]))
            .ToArray();
    }
}

public sealed class MaterializationCountingSchemaProvider(
    IReadOnlyList<MaterializationCountingEntity> rows,
    MaterializationCountingState state) : ISchemaProvider
{
    public int ChunksRead => state.ChunksRead;

    public int RowsRead => state.RowsRead;

    public static MaterializationCountingSchemaProvider CreateDefault()
    {
        return new MaterializationCountingSchemaProvider(
            [
                new MaterializationCountingEntity(1, "one"),
                new MaterializationCountingEntity(2, "two"),
                new MaterializationCountingEntity(3, "three")
            ],
            new MaterializationCountingState());
    }

    public ISchema GetSchema(string schema)
    {
        if (string.Equals(schema.TrimStart('#'), "counting", StringComparison.OrdinalIgnoreCase))
            return new MaterializationCountingSchema(rows, state);

        throw new NotSupportedException(schema);
    }
}

public sealed class MaterializationCountingSchema(
    IReadOnlyList<MaterializationCountingEntity> rows,
    MaterializationCountingState state) : SchemaBase("counting", CreateLibrary())
{
    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        if (string.Equals(name, "entities", StringComparison.OrdinalIgnoreCase))
            return new MaterializationCountingTable();

        throw new NotSupportedException(name);
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        if (string.Equals(name, "entities", StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSourceType<T, MaterializationCountingEntity>(
                name,
                new MaterializationCountingRowSource(rows, state));
        }

        throw new NotSupportedException(name);
    }

    public override SchemaMethodInfo[] GetConstructors()
    {
        return TypeHelper.GetSchemaMethodInfosForType<MaterializationCountingRowSource>("entities");
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new EmptyLibrary());

        return new MethodsAggregator(methodsManager);
    }
}

public sealed class MaterializationCountingTable : ISchemaTable
{
    public ISchemaColumn[] Columns =>
    [
        new SchemaColumn(nameof(MaterializationCountingEntity.Id), 0, typeof(int)),
        new SchemaColumn(nameof(MaterializationCountingEntity.Name), 1, typeof(string))
    ];

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(MaterializationCountingEntity));
}

public sealed class MaterializationCountingRowSource(
    IReadOnlyList<MaterializationCountingEntity> rows,
    MaterializationCountingState state) : RowSourceBase<MaterializationCountingEntity>
{
    protected override void CollectChunks(IChunkWriter<MaterializationCountingEntity> writer)
    {
        Interlocked.Increment(ref state.ChunksRead);
        Interlocked.Add(ref state.RowsRead, rows.Count);
        writer.Write(rows);
    }
}

public sealed record MaterializationCountingEntity(int Id, string Name);

public sealed class MaterializationCountingState
{
    public int ChunksRead;

    public int RowsRead;
}
