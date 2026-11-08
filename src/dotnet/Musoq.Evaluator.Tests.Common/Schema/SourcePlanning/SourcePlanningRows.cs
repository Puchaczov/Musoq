using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Tests.Schema.SourcePlanning;

public static class SourcePlanningRows
{
    public static IReadOnlyList<SourcePlanningEntity> CreateDefault()
    {
        var baseDate = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var categories = new[] { "alpha", "beta", "gamma", "delta" };

        return Enumerable.Range(0, 24)
            .Select(index => new SourcePlanningEntity
            {
                Id = index + 1,
                Name = $"item-{index:00}",
                Category = categories[(index * 3) % categories.Length],
                Score = (index * 17 + 11) % 101,
                CreatedAt = baseDate.AddHours((index * 5) % 37),
                JoinKey = index % 6,
                ExpensivePayload = $"payload-{index:00}"
            })
            .ToArray();
    }
}
