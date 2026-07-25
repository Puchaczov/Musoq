using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;

namespace Musoq.Converter.Tests.Schema;

public sealed class ApplyCandidateSchemaProvider(IReadOnlyList<ApplyCandidateEntity> rows) : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (!ApplyCandidateSchema.MatchesName(schema))
            throw new NotSupportedException(schema);

        return new ApplyCandidateSchema(rows);
    }
}

public sealed class ApplyCandidateSchema(IReadOnlyList<ApplyCandidateEntity> rows)
    : SchemaBase(SchemaName, CreateLibrary())
{
    public const string SchemaName = "apply";

    private const string Items = "items";
    private const string Related = "related";

    public static bool MatchesName(string schema)
    {
        return string.Equals(schema, SchemaName, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(schema, $"#{SchemaName}", StringComparison.OrdinalIgnoreCase);
    }

    private static readonly SchemaMethodInfo[] RawConstructors =
    [
        new(
            Related,
            new SchemaConstructorInfo(
                typeof(RelatedSourceSignature)
                    .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                    .Single(),
                false,
                ("name", typeof(string)),
                ("limit", typeof(int)))),
        new(
            Related,
            new SchemaConstructorInfo(
                typeof(RelatedLegacySignature)
                    .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                    .Single(),
                false,
                ("numbers", typeof(int[]))))
    ];

    public override SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext)
    {
        return RawConstructors;
    }

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        if (string.Equals(name, Items, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, Related, StringComparison.OrdinalIgnoreCase))
        {
            return new ApplyCandidateTable();
        }

        throw new NotSupportedException(name);
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        if (string.Equals(name, Items, StringComparison.OrdinalIgnoreCase))
            return EnsureSourceType<T, ApplyCandidateEntity>(name, new ApplyCandidateRowSource(rows));

        if (string.Equals(name, Related, StringComparison.OrdinalIgnoreCase))
            return EnsureSourceType<T, ApplyCandidateEntity>(name, new ApplyCandidateRowSource(CreateRelatedRows(parameters)));

        throw new NotSupportedException(name);
    }

    private ApplyCandidateEntity[] CreateRelatedRows(object?[] parameters)
    {
        if (parameters.Length == 0 || parameters[0] is not string currentName)
            return [];

        return rows.Where(row => !string.Equals(row.Name, currentName, StringComparison.Ordinal)).ToArray();
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();

        methodsManager.RegisterLibraries(new EmptyLibrary());

        return new MethodsAggregator(methodsManager);
    }
}

public sealed class RelatedSourceSignature
{
    public RelatedSourceSignature(string name, int limit = 1)
    {
        _ = (name, limit);
    }
}

public sealed class RelatedLegacySignature
{
    public RelatedLegacySignature(int[] numbers)
    {
        _ = numbers;
    }
}

public sealed class ApplyCandidateTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(ApplyCandidateEntity.Name), 0, typeof(string)),
        new SchemaColumn(nameof(ApplyCandidateEntity.Line), 1, typeof(string)),
        new SchemaColumn(nameof(ApplyCandidateEntity.Numbers), 2, typeof(int[])),
        new SchemaColumn(nameof(ApplyCandidateEntity.Content), 3, typeof(byte[]))
    ];

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column => column.ColumnName == name);
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column => column.ColumnName == name).ToArray();
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(ApplyCandidateEntity));
}

public sealed class ApplyCandidateRowSource(IReadOnlyList<ApplyCandidateEntity> rows)
    : RowSourceBase<ApplyCandidateEntity>
{
    protected override void CollectChunks(IChunkWriter<ApplyCandidateEntity> writer)
    {
        writer.Write(rows.ToArray());
    }
}

public sealed class ApplyCandidateEntity
{
    public static readonly IReadOnlyDictionary<string, int> NameToIndexMap =
        new Dictionary<string, int>
        {
            [nameof(Name)] = 0,
            [nameof(Line)] = 1,
            [nameof(Numbers)] = 2,
            [nameof(Content)] = 3
        };

    public static readonly IReadOnlyDictionary<int, Func<ApplyCandidateEntity, object>> IndexToObjectAccessMap =
        new Dictionary<int, Func<ApplyCandidateEntity, object>>
        {
            [0] = entity => entity.Name,
            [1] = entity => entity.Line,
            [2] = entity => entity.Numbers,
            [3] = entity => entity.Content
        };

    public string Name { get; init; } = string.Empty;

    public string Line { get; init; } = string.Empty;

    public int[] Numbers { get; init; } = [];

    public byte[] Content { get; init; } = [];
}
