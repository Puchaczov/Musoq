using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Schema for text entities with string content.
/// </summary>
public class TextSchema : SchemaBase
{
    private readonly IEnumerable<TextEntity> _entities;

    public TextSchema(IEnumerable<TextEntity> entities)
        : base("test", CreateLibrary())
    {
        _entities = entities;
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext,
        params object[] parameters)
    {
        return new TextEntityTable();
    }

    public override RowSource GetRowSource(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        return new TestEntitySource<TextEntity>(
            _entities,
            TextEntity.NameToIndexMap,
            TextEntity.IndexToObjectAccessMap);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodManager = new MethodsManager();
        return new MethodsAggregator(methodManager);
    }
}
