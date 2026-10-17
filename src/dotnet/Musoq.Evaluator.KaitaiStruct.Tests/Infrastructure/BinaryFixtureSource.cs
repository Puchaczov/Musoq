using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.KaitaiStruct.Tests.Infrastructure;

public sealed record BinaryFixture(byte[] Content);

internal sealed class BinaryFixtureSource(byte[] content) : SchemaBase("fixture", CreateLibrary()), ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (schema != "#fixture")
            throw new InvalidOperationException($"Unknown fixture schema: {schema}");
        return this;
    }

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext,
        params object?[] parameters) => new FixtureTable();

    public override RowSource<T> GetRowSource<T, TArguments>(string name,
        SourceExecutionContext executionContext, TArguments nativeArguments) =>
        EnsureSourceType<T, BinaryFixture>(name, new EntitySource<BinaryFixture>(
            [new BinaryFixture[] { new(content) }],
            new Dictionary<string, int> { [nameof(BinaryFixture.Content)] = 0 },
            new Dictionary<int, Func<BinaryFixture, object?>> { [0] = row => row.Content }));

    private static MethodsAggregator CreateLibrary()
    {
        var methods = new MethodsManager();
        methods.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methods);
    }

    private sealed class FixtureTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => [new FixtureColumn()];
        public SchemaTableMetadata Metadata => new(typeof(BinaryFixture));
        public ISchemaColumn? GetColumnByName(string name) => name == "Content" ? new FixtureColumn() : null;
        public ISchemaColumn[] GetColumnsByName(string name) => name == "Content" ? Columns : [];
    }

    private sealed class FixtureColumn : ISchemaColumn
    {
        public string ColumnName => "Content";
        public int ColumnIndex => 0;
        public Type ColumnType => typeof(byte[]);
        public Type SourceReadType => ColumnType;
        public EnumTypeDescriptor? EnumType => null;
    }
}
