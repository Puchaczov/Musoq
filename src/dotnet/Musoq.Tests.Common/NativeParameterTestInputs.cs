using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Tests.Common;

/// <summary>Converts structural fixture values to their independent JSON input representation.</summary>
public static class NativeParameterTestInputs
{
    public static object? ToFixtureJsonValue(object? value) =>
        value switch
        {
            StructuralValue { Kind: StructuralTypeKind.Record } record => record.Fields.ToDictionary(
                static field => field.Key,
                static field => ToFixtureJsonValue(field.Value)
            ),
            StructuralValue { Kind: StructuralTypeKind.Collection } collection => collection
                .Elements.Select(ToFixtureJsonValue)
                .ToArray(),
            StructuralValue scalar => scalar.Scalar,
            IEnumerable<StructuralValue> collection => collection.Select(ToFixtureJsonValue).ToArray(),
            _ => value,
        };
}
