using System;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Converter.Tests.Schema;

public sealed class RawStringLiteralSchemaProvider : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        return new RawStringLiteralSchema();
    }
}

public sealed class RawStringLiteralSchema : SchemaBase
{
    private const string Files = "files";

    public RawStringLiteralSchema()
        : base("raw", CreateLibrary())
    {
    }

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        if (string.Equals(name, Files, StringComparison.OrdinalIgnoreCase))
            return new RawFilesTable();

        throw new NotSupportedException(name);
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        if (string.Equals(name, Files, StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSourceType<T, RawFilesEntity>(
                name,
                new RawFilesRowSource((string)parameters[0]!, (bool)parameters[1]!));
        }

        throw new NotSupportedException(name);
    }

    public override SchemaMethodInfo[] GetConstructors()
    {
        return TypeHelper.GetSchemaMethodInfosForType<RawFilesRowSource>(Files);
    }

    public override SchemaMethodInfo[] GetRawConstructors(
        string methodName,
        SourceMetadataContext metadataContext)
    {
        return string.Equals(methodName, Files, StringComparison.OrdinalIgnoreCase)
            ? GetConstructors()
            : [];
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new EmptyLibrary());
        return new MethodsAggregator(methodsManager);
    }
}

public sealed class RawFilesTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(RawFilesEntity.Path), 0, typeof(string)),
        new SchemaColumn(nameof(RawFilesEntity.Recursive), 1, typeof(bool))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(RawFilesEntity));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }
}

public sealed class RawFilesEntity(string path, bool recursive)
{
    public string Path { get; } = path;

    public bool Recursive { get; } = recursive;
}

public sealed class RawFilesRowSource(string path, bool recursive) : RowSourceBase<RawFilesEntity>
{
    protected override void CollectChunks(IChunkWriter<RawFilesEntity> writer)
    {
        writer.Write([new RawFilesEntity(path, recursive)]);
    }
}
