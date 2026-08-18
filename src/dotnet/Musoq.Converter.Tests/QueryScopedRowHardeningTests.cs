using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator.Exceptions;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class QueryScopedRowHardeningTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void CsvJsonAndXmlStyleReaders_ShouldMaterializeThroughConcreteRefStructs()
    {
        var csv = new CsvOrdinalReader([42, "csv"]);
        var json = new JsonPropertyReader(new Dictionary<string, object?>
        {
            ["id"] = 43,
            ["label"] = "json"
        });
        var xml = new XmlPathReader(new Dictionary<string, object?>
        {
            ["/row/@id"] = 44,
            ["/row/label"] = "xml"
        });

        Assert.AreEqual(new TestQueryRow(42, "csv"), Materialize<CsvOrdinalReader>(ref csv));
        Assert.AreEqual(new TestQueryRow(43, "json"), Materialize<JsonPropertyReader>(ref json));
        Assert.AreEqual(new TestQueryRow(44, "xml"), Materialize<XmlPathReader>(ref xml));
    }

    [TestMethod]
    public void MissingAndNullFields_ShouldRemainTypedAndNullable()
    {
        var reader = new MissingFieldReader();
        var row = NullableMaterializer.Materialize<MissingFieldReader>(ref reader);

        Assert.IsNull(row.Number);
        Assert.IsNull(row.Text);
    }

    [TestMethod]
    public void ReaderFailure_ShouldPropagateFromStaticMaterializer()
    {
        Assert.Throws<InvalidOperationException>(MaterializeThrowingReader);
    }

    [TestMethod]
    public void DifferingProjectedShapes_ShouldUseDifferentCarrierIdentities()
    {
        var provider = new QueryScopedRowsSchemaProvider();
        var nameInspection = InstanceCreator.CompileForInspection(
            "select p.Name from #queryrows.items() p",
            "query-row-name-shape",
            provider,
            _loggerResolver);
        var valueInspection = InstanceCreator.CompileForInspection(
            "select p.Value from #queryrows.items() p",
            "query-row-value-shape",
            provider,
            _loggerResolver);

        var nameCarrier = ExtractCarrierName(nameInspection.GeneratedCSharpCode);
        var valueCarrier = ExtractCarrierName(valueInspection.GeneratedCSharpCode);
        Assert.AreNotEqual(nameCarrier, valueCarrier);
    }

    [TestMethod]
    public void AdvertisedCapabilityWithoutRuntimeImplementation_ShouldFailDeterministically()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "select p.Name from #queryrows.items() p",
            "query-row-capability-mismatch",
            new CapabilityMismatchSchemaProvider(),
            _loggerResolver);

        Assert.IsTrue(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics));
        using var table = result.CompiledQuery!.Run();
        var exception = Assert.Throws<QueryExecutionException>(() => _ = table.Count);
        Assert.Contains("does not implement IQueryScopedRowSourceSchema", exception.ToString());
    }

    [TestMethod]
    public void QueryScopedRows_ShouldFlowThroughPredicateAndJoinExecution()
    {
        var provider = new QueryScopedRowsSchemaProvider();
        var filtered = InstanceCreator.CompileWithDiagnostics(
            "select p.Name from #queryrows.items() p where p.Value > 1",
            "query-row-filter",
            provider,
            _loggerResolver);
        Assert.IsTrue(filtered.Succeeded, string.Join(Environment.NewLine, filtered.Diagnostics));
        using (var filteredTable = filtered.CompiledQuery!.Run())
        {
            Assert.AreEqual(1, filteredTable.Count);
            Assert.AreEqual("right", filteredTable[0][0]);
        }

        var joined = InstanceCreator.CompileWithDiagnostics(
            "select p.Name, q.Name from #queryrows.items() p inner join #queryrows.items() q on p.Value = q.Value",
            "query-row-join",
            provider,
            _loggerResolver);
        Assert.IsTrue(joined.Succeeded, string.Join(Environment.NewLine, joined.Diagnostics));
        using var joinedTable = joined.CompiledQuery!.Run();
        Assert.AreEqual(2, joinedTable.Count);
    }

    [TestMethod]
    public void QueryScopedRows_ShouldFlowThroughOuterNullGroupingCteAndSetOperations()
    {
        var provider = new QueryScopedRowsSchemaProvider();
        var outer = InstanceCreator.CompileWithDiagnostics(
            "select p.Name, q.Name from #queryrows.items() p left join #queryrows.items() q on q.Value = 99",
            "query-row-outer-null",
            provider,
            _loggerResolver);
        Assert.IsTrue(outer.Succeeded, string.Join(Environment.NewLine, outer.Diagnostics));
        using (var outerTable = outer.CompiledQuery!.Run())
        {
            Assert.AreEqual(2, outerTable.Count);
            Assert.IsNull(outerTable[0][1]);
        }

        var grouped = InstanceCreator.CompileWithDiagnostics(
            "select p.Value, count(p.Name) from #queryrows.items() p group by p.Value",
            "query-row-group",
            provider,
            _loggerResolver);
        Assert.IsTrue(grouped.Succeeded, string.Join(Environment.NewLine, grouped.Diagnostics));
        using (var groupedTable = grouped.CompiledQuery!.Run())
            Assert.AreEqual(2, groupedTable.Count);

        var set = InstanceCreator.CompileWithDiagnostics(
            "select p.Value from #queryrows.items() p union all select q.Value from #queryrows.items() q",
            "query-row-set",
            provider,
            _loggerResolver);
        Assert.IsTrue(set.Succeeded, string.Join(Environment.NewLine, set.Diagnostics));
        using var setTable = set.CompiledQuery!.Run();
        Assert.AreEqual(4, setTable.Count);

        var cte = InstanceCreator.CompileWithDiagnostics(
            "with valuesCte as (select p.Value as Value from #queryrows.items() p) select v.Value from valuesCte v",
            "query-row-cte",
            provider,
            _loggerResolver);
        Assert.IsTrue(cte.Succeeded, string.Join(Environment.NewLine, cte.Diagnostics));
        using var cteTable = cte.CompiledQuery!.Run();
        Assert.AreEqual(2, cteTable.Count);
    }

    [TestMethod]
    public void WideQueryScopedShape_ShouldUseSealedClassCarrier()
    {
        var result = InstanceCreator.CompileForInspection(
            "select w.G0, w.G1, w.G2, w.G3, w.G4 from #widequeryrows.items() w",
            "query-row-wide",
            new WideQueryRowsSchemaProvider(),
            _loggerResolver);

        Assert.IsFalse(result.Diagnostics.Any(static diagnostic => diagnostic.IsError), result.PlanningText);
        Assert.Contains("private sealed class QueryRow_", result.GeneratedCSharpCode);
        Assert.Contains("reader.Read<Guid>(0)", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void QueryRowShapes_ShouldSupportSpecialNamesAndZeroColumns()
    {
        var special = new QueryRowShape(
        [
            new QueryRowField(0, 7, "json.path/value", typeof(string), true)
        ]);
        var empty = new QueryRowShape([]);

        Assert.AreEqual(1, special.Fields.Count);
        Assert.AreEqual(7, special.Fields[0].SourceColumnIndex);
        Assert.AreEqual(0, empty.Fields.Count);
        Assert.AreNotEqual(special.Fingerprint, empty.Fingerprint);
    }

    [TestMethod]
    public void QueryScopedRows_ShouldFlowThroughWindowExecution()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "select p.Name, RowNumber() over (order by p.Value) as Rank from #queryrows.items() p",
            "query-row-window",
            new QueryScopedRowsSchemaProvider(),
            _loggerResolver);

        Assert.IsTrue(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics));
        using var table = result.CompiledQuery!.Run();
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(1L, table[0][1]);
        Assert.AreEqual(2L, table[1][1]);
    }

    private static TestQueryRow Materialize<TReader>(ref TReader reader)
        where TReader : IQuerySourceFieldReader, allows ref struct
    {
        return TestMaterializer.Materialize<TReader>(ref reader);
    }

    private static void MaterializeThrowingReader()
    {
        var reader = new ThrowingReader();
        _ = ThrowingMaterializer.Materialize<ThrowingReader>(ref reader);
    }

    private static string ExtractCarrierName(string code)
    {
        var match = Regex.Match(code, "QueryRow_[A-F0-9]{12}", RegexOptions.CultureInvariant);
        Assert.IsTrue(match.Success, code);
        return match.Value;
    }
}

public readonly record struct TestQueryRow(int Number, string Text);

public readonly struct TestMaterializer : IQueryRowMaterializer<TestQueryRow>
{
    public static TestQueryRow Materialize<TReader>(scoped ref TReader reader)
        where TReader : IQuerySourceFieldReader, allows ref struct
    {
        return new TestQueryRow(reader.Read<int>(0), reader.Read<string>(1));
    }
}

public readonly struct NullableTestQueryRow(int? number, string? text)
{
    public int? Number { get; } = number;

    public string? Text { get; } = text;
}

public readonly struct NullableMaterializer : IQueryRowMaterializer<NullableTestQueryRow>
{
    public static NullableTestQueryRow Materialize<TReader>(scoped ref TReader reader)
        where TReader : IQuerySourceFieldReader, allows ref struct
    {
        return new NullableTestQueryRow(reader.Read<int?>(0), reader.Read<string?>(1));
    }
}

public readonly struct ThrowingMaterializer : IQueryRowMaterializer<TestQueryRow>
{
    public static TestQueryRow Materialize<TReader>(scoped ref TReader reader)
        where TReader : IQuerySourceFieldReader, allows ref struct
    {
        return new TestQueryRow(reader.Read<int>(0), reader.Read<string>(1));
    }
}

public ref struct CsvOrdinalReader(IReadOnlyList<object?> values) : IQuerySourceFieldReader
{
    public T Read<T>(int slot) => (T)values[slot]!;
}

public ref struct JsonPropertyReader(IReadOnlyDictionary<string, object?> properties) : IQuerySourceFieldReader
{
    public T Read<T>(int slot)
    {
        var name = slot switch
        {
            0 => "id",
            1 => "label",
            _ => throw new ArgumentOutOfRangeException(nameof(slot))
        };
        return properties.TryGetValue(name, out var value) && value != null ? (T)value : default!;
    }
}

public ref struct XmlPathReader(IReadOnlyDictionary<string, object?> paths) : IQuerySourceFieldReader
{
    public T Read<T>(int slot)
    {
        var path = slot switch
        {
            0 => "/row/@id",
            1 => "/row/label",
            _ => throw new ArgumentOutOfRangeException(nameof(slot))
        };
        return paths.TryGetValue(path, out var value) && value != null ? (T)value : default!;
    }
}

public ref struct MissingFieldReader : IQuerySourceFieldReader
{
    public T Read<T>(int slot) => default!;
}

public ref struct ThrowingReader : IQuerySourceFieldReader
{
    public T Read<T>(int slot) => throw new InvalidOperationException("reader failure");
}

public sealed class CapabilityMismatchSchemaProvider : ISchemaProvider
{
    public ISchema GetSchema(string schema) => new CapabilityMismatchSchema();
}

public sealed class CapabilityMismatchSchema : SchemaBase
{
    public CapabilityMismatchSchema()
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

    private static MethodsAggregator CreateLibrary()
    {
        var methods = new MethodsManager();
        methods.RegisterLibraries(new EmptyLibrary());
        return new MethodsAggregator(methods);
    }
}
