using System;
using System.Collections.Generic;
using Musoq.Converter.Tests.Schema;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    private static EntitySetSchemaProvider CreateEntitySetSchemaProvider()
    {
        return new EntitySetSchemaProvider(new Dictionary<string, IReadOnlyList<EntitySetEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["#A"] =
            [
                new EntitySetEntity { City = "Warsaw", Country = "PL", Population = 500, Name = "a1" },
                new EntitySetEntity { City = "Berlin", Country = "DE", Population = 80, Name = "a2" }
            ],
            ["#B"] =
            [
                new EntitySetEntity { City = "Krakow", Country = "PL", Population = 300, Name = "b1" },
                new EntitySetEntity { City = "Munich", Country = "DE", Population = 200, Name = "b2" }
            ],
            ["#C"] =
            [
                new EntitySetEntity { City = "Prague", Country = "CZ", Population = 150, Name = "c1" }
            ]
        });
    }
}
