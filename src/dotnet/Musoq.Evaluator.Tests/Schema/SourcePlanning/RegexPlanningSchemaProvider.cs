using System;
using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests.Schema.SourcePlanning;

public sealed class RegexPlanningSchemaProvider(IReadOnlyList<SourcePlanningEntity> rows) : ISchemaProvider
{
    public SourcePlanningRecorder Recorder { get; } = new();

    public SourcePredicateCapabilities Capabilities { get; init; } = new()
    {
        RegexMatches = [new SourceRegexMatchCapability(new SourceColumnRef("Name"), supportsNegation: true)],
        StringMatches = [new SourceStringMatchCapability(new SourceColumnRef("Name"), SourceStringMatchOperations.All)]
    };

    public bool RejectAll { get; init; }

    public bool VolatileName { get; init; }

    public Func<SourcePlanRequest, SourcePlanResult>? PlanFactory { get; init; }

    public Func<Exception>? FailureFactory { get; init; }

    public ISchema GetSchema(string schema) => new RegexSchema(schema.TrimStart('#'), this, rows);

    private sealed class RegexSchema(
        string schemaName,
        RegexPlanningSchemaProvider provider,
        IReadOnlyList<SourcePlanningEntity> sourceRows) : SchemaBase(schemaName, CreateLibrary())
    {
        public override ISchemaTable GetTableByName(string name, SourceMetadataContext context, params object?[] parameters)
        {
            var table = new SourcePlanningTable();
            if (provider.VolatileName)
                table.Columns[1] = new SchemaColumn("Name", 1, typeof(string), ColumnStability.Volatile);
            return table;
        }

        public override SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters)
            => base.DescribeSource(name, context, parameters) with { PredicateCapabilities = provider.Capabilities };

        public override SourcePlanResult TryPlanSource(string name, SourcePlanRequest request, params object?[] parameters)
        {
            provider.Recorder.RecordRequest(request);
            var result = provider.PlanFactory?.Invoke(request) ??
                (provider.RejectAll ? SourcePlanResult.RejectAll(request) : SourcePlanResult.AcceptAll(request));
            provider.Recorder.RecordPlanResult(result);
            return result;
        }

        public override RowSource<T> GetRowSource<T, TArguments>(
            string name, SourceExecutionContext context, TArguments arguments)
        {
            provider.Recorder.RecordExecutionPlan(context.Plan);
            return EnsureSourceType<T, SourcePlanningEntity>(name,
                new SourcePlanningRowSource(sourceRows, context.Plan, provider.Recorder, provider.FailureFactory));
        }

        private static MethodsAggregator CreateLibrary()
        {
            var manager = new MethodsManager();
            manager.RegisterLibraries(new LibraryBase());
            return new MethodsAggregator(manager);
        }
    }
}
