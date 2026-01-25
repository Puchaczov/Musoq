using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.Generic;

public class GenericEntityTable<TTableEntity> : ISchemaTable
{
    static GenericEntityTable()
    {
        var type = typeof(TTableEntity);
        var properties = type.GetProperties();

        var nameToIndexMap = new Dictionary<string, int>();
        var indexToObjectAccessMap = new Dictionary<int, Func<TTableEntity, object>>();

        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];

            nameToIndexMap.Add(property.Name, i);
            indexToObjectAccessMap.Add(i, entity => property.GetValue(entity));
        }

        NameToIndexMap = nameToIndexMap;
        IndexToObjectAccessMap = indexToObjectAccessMap;
    }

    public GenericEntityTable()
    {
        Columns = NameToIndexMap
            .Select(pair =>
                new SchemaColumn(pair.Key, pair.Value, typeof(TTableEntity).GetProperty(pair.Key)!.PropertyType))
            .Cast<ISchemaColumn>()
            .ToArray();
    }

    // ReSharper disable once StaticMemberInGenericType
    public static IReadOnlyDictionary<string, int> NameToIndexMap { get; }

    public static IReadOnlyDictionary<int, Func<TTableEntity, object>> IndexToObjectAccessMap { get; }
    public ISchemaColumn[] Columns { get; }

    public ISchemaColumn GetColumnByName(string name)
    {
        return Columns[NameToIndexMap[name]];
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        if (NameToIndexMap.TryGetValue(name, out var index))
            return
            [
                Columns[index]
            ];

        return [];
    }

    public SchemaTableMetadata Metadata { get; } = new(typeof(TTableEntity));
}
