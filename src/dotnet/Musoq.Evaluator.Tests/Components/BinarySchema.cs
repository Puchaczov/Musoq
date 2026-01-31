using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Schema for binary entities with byte[] content.
/// </summary>
public class BinarySchema : SchemaBase
{
    private readonly IEnumerable<BinaryEntity> _entities;

    public BinarySchema(IEnumerable<BinaryEntity> entities)
        : base("test", CreateLibrary())
    {
        _entities = entities;
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext,
        params object[] parameters)
    {
        return new BinaryEntityTable();
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new TestEntitySource<BinaryEntity>(
            _entities,
            BinaryEntity.NameToIndexMap,
            BinaryEntity.IndexToObjectAccessMap);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();
        return new MethodsAggregator(methodManager);
    }
}
