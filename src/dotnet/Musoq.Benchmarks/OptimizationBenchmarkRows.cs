namespace Musoq.Benchmarks;

public static class OptimizationBenchmarkRows
{
    public static IReadOnlyList<OptimizationBenchmarkEntity> Create(int count)
    {
        var baseDate = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var hotCategories = new[] { "hot-a", "hot-b", "hot-c" };
        var coldCategories = new[] { "cold-a", "cold-b", "cold-c", "cold-d", "cold-e" };

        return Enumerable.Range(0, count)
            .Select(index =>
            {
                var category = index % 10 < 7
                    ? hotCategories[index % hotCategories.Length]
                    : coldCategories[index % coldCategories.Length];

                return new OptimizationBenchmarkEntity
                {
                    Id = index,
                    Name = $"entity-{index:000000}",
                    Category = category,
                    GroupKey = $"group-{index % 64:00}",
                    JoinKey = index % Math.Max(1, count / 20),
                    Score = (index * 73 + 19) % 10_000,
                    Value = (index * 37 + 11) % 2_000,
                    CreatedAt = baseDate.AddSeconds((index * 97) % 86_400),
                    Payload = $"payload-{category}-{index % 128:000}"
                };
            })
            .ToArray();
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<OptimizationBenchmarkEntity>> CreateSingleSource(
        int count,
        string schemaName = "#bench")
    {
        return CreateSingleSource(Create(count), schemaName);
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<OptimizationBenchmarkEntity>> CreateSingleSource(
        IReadOnlyList<OptimizationBenchmarkEntity> rows,
        string schemaName = "#bench")
    {
        return new Dictionary<string, IReadOnlyList<OptimizationBenchmarkEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            [schemaName] = rows
        };
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<OptimizationBenchmarkEntity>> CreateJoinSources(int count)
    {
        var left = Create(count);
        var right = left.Select(row => new OptimizationBenchmarkEntity
            {
                Id = row.Id + count,
                Name = $"right-{row.Name}",
                Category = row.Category,
                GroupKey = row.GroupKey,
                JoinKey = row.JoinKey,
                Score = row.Score + 17,
                Value = row.Value + 3,
                CreatedAt = row.CreatedAt.AddMinutes(5),
                Payload = $"right-{row.Payload}"
            })
            .ToArray();

        return new Dictionary<string, IReadOnlyList<OptimizationBenchmarkEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["#left"] = left,
            ["#right"] = right
        };
    }
}
