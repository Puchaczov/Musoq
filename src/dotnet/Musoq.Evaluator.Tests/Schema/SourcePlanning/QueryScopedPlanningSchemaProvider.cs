using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Evaluator.Tests.Schema.Dynamic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Optimization;
using Musoq.Tests.Common.SourcePlanning;

namespace Musoq.Evaluator.Tests.Schema.SourcePlanning;

internal sealed class QueryScopedPlanningSchemaProvider(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, int mode) : ISchemaProvider
{
    private readonly IReadOnlyList<IReadOnlyDictionary<string, object?>> _rows = rows;
    private readonly int _mode = mode;
    internal SourcePlanningRecorder Recorder { get; } = new();
    internal ConcurrentBag<QueryScopedRowSourceRequest> Executions { get; } = [];
    internal IReadOnlyList<IReadOnlyDictionary<string, object?>>? InnerRows { get; init; }
    internal Barrier? OuterSourceBarrier { get; set; }

    public ISchema GetSchema(string schema) => new PlanningSchema(this);

    private sealed class PlanningSchema(QueryScopedPlanningSchemaProvider provider)
        : DynamicSchema(new Dictionary<string, Type> { ["Id"] = typeof(int), ["Name"] = typeof(string) }, [])
    {
        public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
        {
            var table = new StrictTable();
            foreach (var column in metadataContext.AllColumns)
                if (table.GetColumnByName(column.ColumnName) == null)
                    throw new InvalidOperationException($"Unknown source column '{column.ColumnName}'.");
            return table;
        }

        public override SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters)
            => base.DescribeSource(name, context, parameters) with
            {
                PredicateCapabilities = provider._mode == 1 ? SourcePredicateCapabilities.None : new SourcePredicateCapabilities
                {
                    StringMatches = [new SourceStringMatchCapability(new SourceColumnRef("Name"), SourceStringMatchOperations.All, supportsNegation: true)]
                }
            };

        public override SourcePlanResult TryPlanSource(string name, SourcePlanRequest request, params object?[] parameters)
        {
            provider.Recorder.RecordRequest(request);
            var result = provider._mode == 0 ? SourcePlanResult.AcceptAll(request) : SourcePlanResult.RejectAll(request);
            provider.Recorder.RecordPlanResult(result);
            return result;
        }

        public override RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer, TArguments>(
            string name, QueryScopedRowSourceRequest request, TArguments parameters)
        {
            if (request.ExecutionContext.Plan.Identity.Alias == "d" && provider.OuterSourceBarrier is { } barrier &&
                !barrier.SignalAndWait(TimeSpan.FromSeconds(30), request.ExecutionContext.EndWorkToken))
                throw new TimeoutException("Correlated executions did not overlap at their outer source boundary.");
            provider.Executions.Add(request);
            provider.Recorder.RecordExecutionPlan(request.ExecutionContext.Plan);
            var input = request.ExecutionContext.Plan.Identity.Alias == "e" ? provider.InnerRows ?? provider._rows : provider._rows;
            var filtered = SourcePlanningRowExecution.ApplyPlan(input, request.ExecutionContext.Plan,
                new SourcePlanningRowExecutionOptions<IReadOnlyDictionary<string, object?>>(
                    "unused", column => row => row.GetValueOrDefault(column), row => row["Id"], (values, _) => values));
            return new NativeDynamicTestSource<TRow, TMaterializer>(filtered.Cast<dynamic>(), request.Shape.Fields);
        }
    }

    private sealed class StrictTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = [new SchemaColumn("Id", 0, typeof(int)), new SchemaColumn("Name", 1, typeof(string))];
        public SchemaTableMetadata Metadata { get; } = new(typeof(QueryScopedRowContract)) { TypeOrigin = TableSchemaTypeOrigin.TableDefined };
        public ISchemaColumn? GetColumnByName(string name) => Columns.SingleOrDefault(column => column.ColumnName == name);
        public ISchemaColumn[] GetColumnsByName(string name) => Columns.Where(column => column.ColumnName == name).ToArray();
    }
}
