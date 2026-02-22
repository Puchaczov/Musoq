using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Tests.Schema.NegativeTests;

public class NestedEntity
{
    public static readonly IReadOnlyDictionary<string, int> NameToIndexMap;
    public static readonly IReadOnlyDictionary<int, Func<NestedEntity, object>> IndexToObjectAccessMap;

    static NestedEntity()
    {
        NameToIndexMap = new Dictionary<string, int>
        {
            { nameof(Id), 0 },
            { nameof(Info), 1 }
        };

        IndexToObjectAccessMap = new Dictionary<int, Func<NestedEntity, object>>
        {
            { 0, entity => entity.Id },
            { 1, entity => entity.Info }
        };
    }

    public int Id { get; set; }

    public ComplexInfo Info { get; set; }
}
