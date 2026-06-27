using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Tests.Schema.Basic;

public class UsedColumnsOrUsedWhereEntity
{
    public static readonly IDictionary<string, int> TestNameToIndexMap = new Dictionary<string, int>
    {
        { nameof(Name), 10 },
        { nameof(City), 11 },
        { nameof(Country), 12 },
        { nameof(Population), 13 },
        { nameof(Month), 14 }
    };

    public static readonly IDictionary<int, Func<UsedColumnsOrUsedWhereEntity, object?>> TestIndexToObjectAccessMap =
        new Dictionary<int, Func<UsedColumnsOrUsedWhereEntity, object?>>
        {
            { 10, arg => arg.Name },
            { 11, arg => arg.City },
            { 12, arg => arg.Country },
            { 13, arg => arg.Population },
            { 14, arg => arg.Month }
        };

    public UsedColumnsOrUsedWhereEntity()
    {
    }

    public UsedColumnsOrUsedWhereEntity(string name)
    {
        Name = name;
    }

    public string Name { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public decimal Population { get; set; }

    public string Month { get; set; } = string.Empty;
}
