using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Tests.Schema.Generated;

public sealed class GeneratedApplySampleEntity
{
    public static readonly IReadOnlyDictionary<string, int> NameToIndexMap =
        new Dictionary<string, int>
        {
            [nameof(Name)] = 0,
            [nameof(Numbers)] = 1
        };

    public static readonly IReadOnlyDictionary<int, Func<GeneratedApplySampleEntity, object?>> IndexToObjectAccessMap =
        new Dictionary<int, Func<GeneratedApplySampleEntity, object?>>
        {
            [0] = entity => entity.Name,
            [1] = entity => entity.Numbers
        };

    public string Name { get; init; } = string.Empty;

    public int[] Numbers { get; init; } = [];
}
