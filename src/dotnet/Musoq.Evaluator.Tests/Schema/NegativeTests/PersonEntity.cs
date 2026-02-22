using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Tests.Schema.NegativeTests;

public class PersonEntity
{
    public static readonly IReadOnlyDictionary<string, int> NameToIndexMap;
    public static readonly IReadOnlyDictionary<int, Func<PersonEntity, object>> IndexToObjectAccessMap;

    static PersonEntity()
    {
        NameToIndexMap = new Dictionary<string, int>
        {
            { nameof(Id), 0 },
            { nameof(Name), 1 },
            { nameof(Age), 2 },
            { nameof(City), 3 },
            { nameof(Salary), 4 },
            { nameof(BirthDate), 5 },
            { nameof(ManagerId), 6 },
            { nameof(Email), 7 }
        };

        IndexToObjectAccessMap = new Dictionary<int, Func<PersonEntity, object>>
        {
            { 0, entity => entity.Id },
            { 1, entity => entity.Name },
            { 2, entity => entity.Age },
            { 3, entity => entity.City },
            { 4, entity => entity.Salary },
            { 5, entity => entity.BirthDate },
            { 6, entity => entity.ManagerId },
            { 7, entity => entity.Email }
        };
    }

    public int Id { get; set; }

    public string Name { get; set; }

    public int Age { get; set; }

    public string City { get; set; }

    public decimal Salary { get; set; }

    public DateTime BirthDate { get; set; }

    public int? ManagerId { get; set; }

    public string Email { get; set; }
}
