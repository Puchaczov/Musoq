using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class QueryScopedRowRendererTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void CompileForInspection_WhenSourceSelectsQueryScopedRows_ShouldEmitTypedCarrierAndMaterializer()
    {
        var result = InstanceCreator.CompileForInspection(
            "select p.Name, p.Value from #queryrows.items() p",
            "query-scoped-renderer",
            new QueryScopedRowsSchemaProvider(),
            _loggerResolver);

        Assert.IsFalse(result.Diagnostics.Any(static diagnostic => diagnostic.IsError), result.PlanningText);
        var code = result.GeneratedCSharpCode;
        Assert.Contains("GetQueryScopedRowSource<QueryRow_", code);
        Assert.Contains("IQueryRowMaterializer<QueryRow_", code);
        Assert.Contains("readonly struct QueryRow_", code);
        Assert.Contains("reader.Read<string>(0)", code);
        Assert.Contains("reader.Read<int>(1)", code);
        Assert.IsFalse(code.Contains("GetRowSource<QueryRow_", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("DynamicInvoke", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileAndRun_WhenSourceSelectsQueryScopedRows_ShouldMaterializeTypedValues()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "select p.Name, p.Value from #queryrows.items() p",
            "query-scoped-execution",
            new QueryScopedRowsSchemaProvider(),
            _loggerResolver);

        Assert.IsTrue(
            result.Succeeded,
            $"{result.CaughtException}{Environment.NewLine}{string.Join(Environment.NewLine, result.Diagnostics)}");
        using var table = result.CompiledQuery!.Run();
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual("right", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
    }

    [TestMethod]
    public void CompileAndRun_WhenMethodRequiresDeclaredEntity_ShouldUseLegacyRowSource()
    {
        const string query = "select p.EntityName() from #queryrows.items() p";
        var provider = new QueryScopedRowsSchemaProvider();
        var inspection = InstanceCreator.CompileForInspection(
            query,
            "query-scoped-entity-method-inspection",
            provider,
            _loggerResolver);

        StringAssert.Contains(
            inspection.GeneratedCSharpCode,
            "GetRowSource<Musoq.Converter.Tests.QueryScopedRowsEntity>");
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains(
            "GetQueryScopedRowSource<",
            StringComparison.Ordinal));
        StringAssert.Contains(inspection.PlanningText, "requires the declared source entity");

        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            "query-scoped-entity-method-execution",
            provider,
            _loggerResolver);

        Assert.IsTrue(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics));
        using var table = result.CompiledQuery!.Run();
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("legacy", table[0][0]);
    }

    [TestMethod]
    public void CompileForInspection_WhenScalarMethodUsesColumn_ShouldKeepQueryScopedRows()
    {
        var inspection = InstanceCreator.CompileForInspection(
            "select DoubleValue(p.Value) from #queryrows.items() p",
            "query-scoped-scalar-method",
            new QueryScopedRowsSchemaProvider(),
            _loggerResolver);

        StringAssert.Contains(inspection.GeneratedCSharpCode, "GetQueryScopedRowSource<");
        StringAssert.Contains(inspection.PlanningText, "used through column values only");
    }

    [TestMethod]
    public void CompileForInspection_WhenOneJoinAliasRequiresEntity_ShouldFallbackOnlyThatSource()
    {
        var inspection = InstanceCreator.CompileForInspection(
            "select a.EntityName(), b.Name from #queryrows.items() a " +
            "inner join #queryrows.items() b on a.Value = b.Value",
            "query-scoped-entity-method-alias",
            new QueryScopedRowsSchemaProvider(),
            _loggerResolver);

        Assert.AreEqual(1, CountOccurrences(
            inspection.GeneratedCSharpCode,
            "GetRowSource<Musoq.Converter.Tests.QueryScopedRowsEntity>"));
        Assert.AreEqual(1, CountOccurrences(inspection.GeneratedCSharpCode, "GetQueryScopedRowSource<"));
        StringAssert.Contains(inspection.GeneratedCSharpCode, "sealed class QueryRow_");
        StringAssert.Contains(inspection.PlanningText, "lifetime=EscapesScan");
    }

    [TestMethod]
    public void CompileForInspection_WhenSameShapeUsesStructAndClass_ShouldEmitDistinctCarriers()
    {
        const string query =
            "with local as (select p.Name, p.Value from #queryrows.items() p) " +
            "select local.Name, local.Value, a.Name, a.Value from local " +
            "inner join #queryrows.items() a on local.Value = a.Value";

        var inspection = InstanceCreator.CompileForInspection(
            query,
            "query-scoped-carrier-identity",
            new QueryScopedRowsSchemaProvider(),
            _loggerResolver);

        Assert.IsFalse(inspection.Diagnostics.Any(static diagnostic => diagnostic.IsError));
        StringAssert.Contains(inspection.GeneratedCSharpCode, "readonly struct QueryRow_");
        StringAssert.Contains(inspection.GeneratedCSharpCode, "_S");
        StringAssert.Contains(inspection.GeneratedCSharpCode, "sealed class QueryRow_");
        StringAssert.Contains(inspection.GeneratedCSharpCode, "_C");
    }

    [TestMethod]
    public void CompileAndRun_WhenQueryNeedsNoSourceFields_ShouldEmitZeroFieldCarrier()
    {
        const string query = "select Count(*) from #queryrows.items() p";
        var provider = new QueryScopedRowsSchemaProvider();
        var inspection = InstanceCreator.CompileForInspection(
            query,
            "query-scoped-zero-fields-inspection",
            provider,
            _loggerResolver);

        StringAssert.Contains(inspection.GeneratedCSharpCode, "GetQueryScopedRowSource<");
        StringAssert.Contains(inspection.GeneratedCSharpCode, "new QueryRowShape(new QueryRowField[]");
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("reader.Read<", StringComparison.Ordinal));

        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            "query-scoped-zero-fields-execution",
            provider,
            _loggerResolver);
        Assert.IsTrue(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics));
        using var table = result.CompiledQuery!.Run();
        Assert.AreEqual(2L, table[0][0]);
    }

    [TestMethod]
    public void CompileAndRun_WhenPredicateNeedsAField_ShouldNotSelectEmptyShape()
    {
        const string query = "select Count(*) from #queryrows.items() p where p.Value > 1";
        var provider = new QueryScopedRowsSchemaProvider();
        var inspection = InstanceCreator.CompileForInspection(
            query,
            "query-scoped-predicate-field-inspection",
            provider,
            _loggerResolver);

        StringAssert.Contains(inspection.GeneratedCSharpCode, "reader.Read<int>(0)");
        Assert.AreEqual(1, CountOccurrences(inspection.GeneratedCSharpCode, "reader.Read<"));

        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            "query-scoped-predicate-field-execution",
            provider,
            _loggerResolver);
        Assert.IsTrue(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics));
        using var table = result.CompiledQuery!.Run();
        Assert.AreEqual(1L, table[0][0]);
    }

    [TestMethod]
    public void CompileForInspection_WhenOrderingNeedsAField_ShouldNotSelectEmptyShape()
    {
        var inspection = InstanceCreator.CompileForInspection(
            "select 1 from #queryrows.items() p order by p.Value desc",
            "query-scoped-order-field-inspection",
            new QueryScopedRowsSchemaProvider(),
            _loggerResolver);

        Assert.IsFalse(inspection.Diagnostics.Any(static diagnostic => diagnostic.IsError));
        StringAssert.Contains(inspection.GeneratedCSharpCode, "reader.Read<int>(0)");
        Assert.AreEqual(1, CountOccurrences(inspection.GeneratedCSharpCode, "reader.Read<"));
    }

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var offset = 0;
        while ((offset = value.IndexOf(token, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += token.Length;
        }

        return count;
    }
}

public sealed class QueryScopedRowsSchemaProvider : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (string.Equals(schema, "queryrows", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(schema, "#queryrows", StringComparison.OrdinalIgnoreCase))
        {
            return new QueryScopedRowsSchema();
        }

        throw new NotSupportedException(schema);
    }
}

public sealed class QueryScopedRowsSchema : SchemaBase, IQueryScopedRowSourceSchema
{
    private static readonly QueryRowInput[] Inputs =
    [
        new("left", 1),
        new("right", 2)
    ];

    public QueryScopedRowsSchema()
        : base("queryrows", CreateLibrary())
    {
        AddTable<QueryScopedRowsTable>("items");
        AddSource<QueryScopedRowsLegacySource>("items");
    }

    public override SourceDescriptor DescribeSource(
        string name,
        SourceDescribeContext context,
        params object?[] parameters)
    {
        return base.DescribeSource(name, context, parameters) with
        {
            TransferCapabilities = SourceTransferCapabilities.QueryScopedRows
        };
    }

    public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer>(
        string name,
        QueryScopedRowSourceRequest request,
        params object?[] parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
    {
        Assert.AreEqual("items", name, ignoreCase: true);
        Assert.IsNotNull(request);
        return new QueryScopedRowsMaterializedSource<TRow, TMaterializer>(Inputs, request.Shape.Fields);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methods = new MethodsManager();
        methods.RegisterLibraries(new QueryScopedRowsLibrary());
        return new MethodsAggregator(methods);
    }
}

public sealed class QueryScopedRowsLibrary : LibraryBase
{
    [BindableMethod]
    public string EntityName(
        [InjectSpecificSource(typeof(QueryScopedRowsEntity))]
        QueryScopedRowsEntity entity)
    {
        return entity.Name;
    }

    [BindableMethod]
    public int DoubleValue(int value)
    {
        return value * 2;
    }
}

public sealed class QueryScopedRowsTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn("Name", 0, typeof(string)),
        new SchemaColumn("Value", 1, typeof(int))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(QueryScopedRowsEntity));

    public ISchemaColumn? GetColumnByName(string name) =>
        Columns.SingleOrDefault(column => string.Equals(column.ColumnName, name, StringComparison.Ordinal));

    public ISchemaColumn[] GetColumnsByName(string name) =>
        Columns.Where(column => string.Equals(column.ColumnName, name, StringComparison.Ordinal)).ToArray();
}

public sealed class QueryScopedRowsLegacySource : RowSourceBase<QueryScopedRowsEntity>
{
    protected override void CollectChunks(IChunkWriter<QueryScopedRowsEntity> writer)
    {
        writer.Write(
        [
            new QueryScopedRowsEntity { Name = "legacy", Value = -1 }
        ]);
    }
}

public sealed class QueryScopedRowsMaterializedSource<TRow, TMaterializer> : RowSourceBase<TRow>
    where TMaterializer : struct, IQueryRowMaterializer<TRow>
{
    private readonly IReadOnlyList<QueryRowInput> _inputs;

    private readonly IReadOnlyList<QueryRowField> _fields;

    public QueryScopedRowsMaterializedSource(
        IReadOnlyList<QueryRowInput> inputs,
        IReadOnlyList<QueryRowField> fields)
    {
        _inputs = inputs;
        _fields = fields;
    }

    protected override void CollectChunks(IChunkWriter<TRow> writer)
    {
        var rows = new List<TRow>(_inputs.Count);
        foreach (var input in _inputs)
        {
            var reader = new QueryRowReader(input, _fields);
            rows.Add(TMaterializer.Materialize<QueryRowReader>(ref reader));
        }

        writer.Write(rows);
    }
}

public readonly record struct QueryRowInput(string Name, int Value);

public struct QueryRowReader(
    QueryRowInput input,
    IReadOnlyList<QueryRowField> fields) : IQuerySourceFieldReader
{
    public T Read<T>(int slot)
    {
        return fields[slot].SourceColumnIndex switch
        {
            0 => (T)(object)input.Name,
            1 => (T)(object)input.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(slot))
        };
    }
}

public sealed class QueryScopedRowsEntity
{
    public string Name { get; init; } = string.Empty;

    public int Value { get; init; }
}
