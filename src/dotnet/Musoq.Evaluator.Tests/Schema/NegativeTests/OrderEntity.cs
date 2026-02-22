using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Tests.Schema.NegativeTests;

public class OrderEntity
{
    public static readonly IReadOnlyDictionary<string, int> NameToIndexMap;
    public static readonly IReadOnlyDictionary<int, Func<OrderEntity, object>> IndexToObjectAccessMap;

    static OrderEntity()
    {
        NameToIndexMap = new Dictionary<string, int>
        {
            { nameof(OrderId), 0 },
            { nameof(PersonId), 1 },
            { nameof(Amount), 2 },
            { nameof(Status), 3 },
            { nameof(OrderDate), 4 },
            { nameof(Notes), 5 }
        };

        IndexToObjectAccessMap = new Dictionary<int, Func<OrderEntity, object>>
        {
            { 0, entity => entity.OrderId },
            { 1, entity => entity.PersonId },
            { 2, entity => entity.Amount },
            { 3, entity => entity.Status },
            { 4, entity => entity.OrderDate },
            { 5, entity => entity.Notes }
        };
    }

    public int OrderId { get; set; }

    public int PersonId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; }

    public DateTime OrderDate { get; set; }

    public string Notes { get; set; }
}
