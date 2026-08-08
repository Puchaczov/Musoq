using BenchmarkDotNet.Attributes;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Schema;

namespace Musoq.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class TableSymbolTransformationBenchmark
{
    private readonly ISchema _schema = new TableTestSchema([]);
    private readonly ISchemaTable _table = new TableTestTable();
    private readonly ISchemaColumn _ordinality = new TableTestColumn("Ordinal", 4, typeof(int));
    private readonly TableSymbol _single;
    private readonly TableSymbol _second;
    private readonly TableSymbol[] _eight;

    public TableSymbolTransformationBenchmark()
    {
        _single = CreateSymbol("a");
        _second = CreateSymbol("b");
        _eight = [
            CreateSymbol("a"),
            CreateSymbol("b"),
            CreateSymbol("c"),
            CreateSymbol("d"),
            CreateSymbol("e"),
            CreateSymbol("f"),
            CreateSymbol("g"),
            CreateSymbol("h")
        ];
    }

    [Benchmark]
    public string SingleAliasLookup()
    {
        var tableName = string.Empty;
        for (var index = 0; index < 1024; index++)
            tableName = _single.GetTableByAlias("a").TableName;

        return tableName;
    }

    [Benchmark]
    public TableSymbol NullableTransformation()
    {
        return _single.MakeNullableIfPossible();
    }

    [Benchmark]
    public TableSymbol OrdinalityTransformation()
    {
        return _single.WithAdditionalColumn("a", _ordinality);
    }

    [Benchmark]
    public (ISchema Schema, ISchemaTable Table, string TableName) TwoAliasMergeAndResolve()
    {
        return _single.MergeSymbols(_second).GetTableByAlias("b");
    }

    [Benchmark]
    public (ISchema Schema, ISchemaTable Table, string TableName) EightAliasMergeAndResolve()
    {
        var merged = _eight[0];
        for (var index = 1; index < _eight.Length; index++)
            merged = merged.MergeSymbols(_eight[index]);

        return merged.GetTableByAlias("h");
    }

    private TableSymbol CreateSymbol(string alias)
    {
        return new TableSymbol(alias, _schema, _table, hasAlias: true);
    }
}
