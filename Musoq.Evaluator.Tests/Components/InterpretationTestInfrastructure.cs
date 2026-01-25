using System;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Entity with binary content for testing Interpret().
/// </summary>
public class BinaryEntity
{
    public static readonly IReadOnlyDictionary<string, int> NameToIndexMap = new Dictionary<string, int>
    {
        { nameof(Name), 0 },
        { nameof(Content), 1 },
        { nameof(Data), 1 } // Alias for Content
    };

    public static readonly IReadOnlyDictionary<int, Func<BinaryEntity, object>> IndexToObjectAccessMap =
        new Dictionary<int, Func<BinaryEntity, object>>
        {
            { 0, e => e.Name },
            { 1, e => e.Content }
        };

    public string Name { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();

    public byte[] Data
    {
        get => Content;
        set => Content = value;
    }
}

/// <summary>
///     Entity with text content for testing Parse().
/// </summary>
public class TextEntity
{
    public static readonly IReadOnlyDictionary<string, int> NameToIndexMap = new Dictionary<string, int>
    {
        { nameof(Name), 0 },
        { nameof(Text), 1 },
        { nameof(Line), 1 } // Alias for Text
    };

    public static readonly IReadOnlyDictionary<int, Func<TextEntity, object>> IndexToObjectAccessMap =
        new Dictionary<int, Func<TextEntity, object>>
        {
            { 0, e => e.Name },
            { 1, e => e.Text }
        };

    public string Name { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Line => Text;
}

/// <summary>
///     Table for binary entities.
/// </summary>
public class BinaryEntityTable : ISchemaTable
{
    public ISchemaColumn[] Columns =>
    [
        new SchemaColumn(nameof(BinaryEntity.Name), 0, typeof(string)),
        new SchemaColumn(nameof(BinaryEntity.Content), 1, typeof(byte[])),
        new SchemaColumn(nameof(BinaryEntity.Data), 1, typeof(byte[]))
    ];

    public SchemaTableMetadata Metadata => new(typeof(BinaryEntity));

    public ISchemaColumn GetColumnByName(string name)
    {
        return Array.Find(Columns, c => c.ColumnName == name)!;
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Array.FindAll(Columns, c => c.ColumnName == name);
    }
}

/// <summary>
///     Table for text entities.
/// </summary>
public class TextEntityTable : ISchemaTable
{
    public ISchemaColumn[] Columns =>
    [
        new SchemaColumn(nameof(TextEntity.Name), 0, typeof(string)),
        new SchemaColumn(nameof(TextEntity.Text), 1, typeof(string)),
        new SchemaColumn(nameof(TextEntity.Line), 1, typeof(string))
    ];

    public SchemaTableMetadata Metadata => new(typeof(TextEntity));

    public ISchemaColumn GetColumnByName(string name)
    {
        return Array.Find(Columns, c => c.ColumnName == name)!;
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Array.FindAll(Columns, c => c.ColumnName == name);
    }
}

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

/// <summary>
///     Schema provider for binary entities.
/// </summary>
public class BinarySchemaProvider : ISchemaProvider
{
    private readonly IDictionary<string, IEnumerable<BinaryEntity>> _values;

    public BinarySchemaProvider(IDictionary<string, IEnumerable<BinaryEntity>> values)
    {
        _values = values;
    }

    public ISchema GetSchema(string schema)
    {
        if (_values.TryGetValue(schema, out var entities)) return new BinarySchema(entities);
        throw new InvalidOperationException($"Schema '{schema}' not found");
    }
}

/// <summary>
///     Schema provider for text entities.
/// </summary>
public class TextSchemaProvider : ISchemaProvider
{
    private readonly IDictionary<string, IEnumerable<TextEntity>> _values;

    public TextSchemaProvider(IDictionary<string, IEnumerable<TextEntity>> values)
    {
        _values = values;
    }

    public ISchema GetSchema(string schema)
    {
        if (_values.TryGetValue(schema, out var entities)) return new TextSchema(entities);
        throw new InvalidOperationException($"Schema '{schema}' not found");
    }
}

/// <summary>
///     Schema provider supporting both binary and text entities.
/// </summary>
public class MixedSchemaProvider : ISchemaProvider
{
    private readonly IDictionary<string, IEnumerable<BinaryEntity>> _binaryValues;
    private readonly IDictionary<string, IEnumerable<TextEntity>> _textValues;

    public MixedSchemaProvider(
        IDictionary<string, IEnumerable<BinaryEntity>> binaryValues,
        IDictionary<string, IEnumerable<TextEntity>> textValues)
    {
        _binaryValues = binaryValues;
        _textValues = textValues;
    }

    public ISchema GetSchema(string schema)
    {
        if (_binaryValues.TryGetValue(schema, out var binaryEntities))
            return new BinarySchema(binaryEntities);
        if (_textValues.TryGetValue(schema, out var textEntities))
            return new TextSchema(textEntities);
        throw new InvalidOperationException($"Schema '{schema}' not found");
    }
}

/// <summary>
///     Simple schema column implementation.
/// </summary>
public class SchemaColumn : ISchemaColumn
{
    public SchemaColumn(string columnName, int columnIndex, Type columnType)
    {
        ColumnName = columnName;
        ColumnIndex = columnIndex;
        ColumnType = columnType;
    }

    public string ColumnName { get; }
    public int ColumnIndex { get; }
    public Type ColumnType { get; }
}

/// <summary>
///     Generic row source for test entities.
/// </summary>
public class TestEntitySource<T> : RowSource
{
    private readonly IEnumerable<T> _entities;
    private readonly IReadOnlyDictionary<int, Func<T, object>> _indexToObjectAccessMap;
    private readonly IReadOnlyDictionary<string, int> _nameToIndexMap;

    public TestEntitySource(
        IEnumerable<T> entities,
        IReadOnlyDictionary<string, int> nameToIndexMap,
        IReadOnlyDictionary<int, Func<T, object>> indexToObjectAccessMap)
    {
        _entities = entities;
        _nameToIndexMap = nameToIndexMap;
        _indexToObjectAccessMap = indexToObjectAccessMap;
    }

    public override IEnumerable<IObjectResolver> Rows
    {
        get
        {
            foreach (var entity in _entities)
                yield return new EntityResolver<T>(entity, _nameToIndexMap, _indexToObjectAccessMap);
        }
    }
}

/// <summary>
///     Generic object resolver for entities.
/// </summary>
public class EntityResolver<T> : IObjectResolver
{
    private readonly T _entity;
    private readonly IReadOnlyDictionary<int, Func<T, object>> _indexToObjectAccessMap;
    private readonly IReadOnlyDictionary<string, int> _nameToIndexMap;

    public EntityResolver(
        T entity,
        IReadOnlyDictionary<string, int> nameToIndexMap,
        IReadOnlyDictionary<int, Func<T, object>> indexToObjectAccessMap)
    {
        _entity = entity;
        _nameToIndexMap = nameToIndexMap;
        _indexToObjectAccessMap = indexToObjectAccessMap;
    }

    public object[] Contexts => [];

    public object this[string name]
    {
        get
        {
            if (_nameToIndexMap.TryGetValue(name, out var index))
                return _indexToObjectAccessMap[index](_entity);
            return null!;
        }
    }

    public object this[int index]
    {
        get
        {
            if (_indexToObjectAccessMap.TryGetValue(index, out var accessor))
                return accessor(_entity);
            return null!;
        }
    }

    public bool HasColumn(string name)
    {
        return _nameToIndexMap.ContainsKey(name);
    }
}
