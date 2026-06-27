using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Schema for binary entities with byte[] content.
/// </summary>
public class BinarySchema(IEnumerable<BinaryEntity> entities) : SchemaBase("test", CreateLibrary())
{
    private readonly IReadOnlyList<BinaryEntity> _entities = entities as IReadOnlyList<BinaryEntity> ?? entities.ToArray();

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        return new BinaryEntityTable();
    }

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters)
    {
        return EnsureSourceType<T, BinaryEntity>(name, new TestEntitySource<BinaryEntity>(
            [_entities],
            BinaryEntity.NameToIndexMap,
            BinaryEntity.IndexToObjectAccessMap));
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();
        return new MethodsAggregator(methodManager);
    }
}
