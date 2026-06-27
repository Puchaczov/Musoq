using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Schema.SourcePlanning;

public sealed class SourcePlanningSchemaProvider : ISchemaProvider
{
    private readonly SourcePlanningMode _mode;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<SourcePlanningEntity>> _rowsBySchema;

    public SourcePlanningSchemaProvider(
        SourcePlanningMode mode,
        IReadOnlyDictionary<string, IReadOnlyList<SourcePlanningEntity>> rowsBySchema,
        SourcePlanningRecorder? recorder = null)
    {
        _mode = mode;
        _rowsBySchema = rowsBySchema;
        Recorder = recorder ?? new SourcePlanningRecorder();
    }

    public SourcePlanningRecorder Recorder { get; }

    public IReadOnlyCollection<SourcePlanRequest> Requests => Recorder.Requests;

    public IReadOnlyCollection<SourceExecutionPlan> ExecutionPlans => Recorder.ExecutionPlans;

    public ISchema GetSchema(string schema)
    {
        if (!_rowsBySchema.TryGetValue(schema, out var rows) &&
            !_rowsBySchema.TryGetValue(NormalizeSchemaName(schema), out rows))
        {
            throw new KeyNotFoundException($"No source-planning test schema rows registered for '{schema}'.");
        }

        return new SourcePlanningSchema(NormalizeSchemaName(schema), rows, _mode, Recorder);
    }

    public static SourcePlanningSchemaProvider CreateSingle(
        SourcePlanningMode mode,
        IReadOnlyList<SourcePlanningEntity> rows,
        string schemaName = "#sp")
    {
        return new SourcePlanningSchemaProvider(
            mode,
            new Dictionary<string, IReadOnlyList<SourcePlanningEntity>>(StringComparer.OrdinalIgnoreCase)
            {
                [schemaName] = rows
            });
    }

    public static IReadOnlyDictionary<string, IReadOnlyList<SourcePlanningEntity>> CreatePair(
        IReadOnlyList<SourcePlanningEntity> rows)
    {
        return new Dictionary<string, IReadOnlyList<SourcePlanningEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["#left"] = rows,
            ["#right"] = rows.Select(row => new SourcePlanningEntity
            {
                Id = row.Id + 1000,
                Name = $"Right-{row.Name}",
                Category = row.Category,
                Score = row.Score + 7,
                CreatedAt = row.CreatedAt.AddDays(1),
                JoinKey = row.JoinKey,
                ExpensivePayload = $"Right-{row.ExpensivePayload}"
            }).ToArray()
        };
    }

    private static string NormalizeSchemaName(string schema)
    {
        return schema.TrimStart('#');
    }
}
