using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Schema for text entities with string content.
/// </summary>
public class TextSchema(IEnumerable<TextEntity> entities) : SchemaBase("test", CreateLibrary())
{
    private readonly IReadOnlyList<TextEntity> _entities = entities as IReadOnlyList<TextEntity> ?? entities.ToArray();

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        return new TextEntityTable();
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        return EnsureSourceType<T, TextEntity>(name, new TestEntitySource<TextEntity>(
            [_entities],
            TextEntity.NameToIndexMap,
            TextEntity.IndexToObjectAccessMap));
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();
        return new MethodsAggregator(methodManager);
    }
}
