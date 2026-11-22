// === Parsed Query ===
/*
select r.Id as Id from bench.rows() r where r.Key in (0, 2, 2, null) order by r.Id
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [r.Id]
    Project [r.Id as Id]
      Filter [r.Key IN (0, 2, 2, NULL)]
        SchemaScan [#bench.rows() as r]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [r.Id]
    PhysicalProject [r.Id as Id]
      PhysicalFilter [r.Key IN (0, 2, 2, NULL)]
        PhysicalSchemaScan [#bench.rows() as r] [pushdown: r.Key IN (0, 2, 2, NULL)]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Id: int <- property Id
      Key: int <- property Key
    GeneratedRecord [ResultRow0WithSortKeys]
      Id: int <- field Id
      __ordinal: int <- field __ordinal
    Generated [ResultRow0]
      Id: int <- field Id

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: FixtureRow] -> rRows
    CreateRecordList [resultOrderRecords: ResultRow0WithSortKeys]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [r in rRows]
      If [r.Key IN (0, 2, 2, NULL)]
        AppendRecord [resultOrderRecords <- ResultRow0WithSortKeys(Id: r.Id)]
    OrderRecordList [resultOrderRecords: ResultRow0WithSortKeys by Id ASC]
    TransferOwnedRows [resultOrderRecords -> result: ResultRow0WithSortKeys]
    ReturnDeferredTable [result: ResultRow0WithSortKeys from owned buffer resultOrderRecords]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B05_Membership_BenchmarkTable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Musoq.Schema;
    using Musoq.Schema.Optimization;
    using Musoq.Evaluator;
    using Musoq.Evaluator.Tables;
    using Musoq.Evaluator.Helpers;
    using Musoq.Evaluator.Runtime;
    using Musoq.Schema.DataSources;
    using System.Linq;

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IContextTableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Key", typeof(int), 1) });
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Id", typeof(int), 0)
        };
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public ILogger Logger { get; set; }
        public TypedParameterSnapshot TypedParameters { get; set; } = TypedParameterSnapshot.Empty;
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();

        public event QueryPhaseEventHandler PhaseChanged;
        public event DataSourceEventHandler DataSourceProgress;
        public event QueryProgressEventHandler QueryProgress;
        public Table Run(CancellationToken token)
        {
            var queryContext = new QueryRunContext(token, TypedParameters, PhaseChanged, DataSourceProgress, this, queryId: "compiled", provider: Provider, sourceRuntimeSettingsBySourceContextId: SourceRuntimeSettingsBySourceContextId, sourceRuntimeSettingDescriptionsBySourceContextId: SourceRuntimeSettingDescriptionsBySourceContextId, sourceExecutionPlans: SourceExecutionPlans, logger: Logger, queryProgress: QueryProgress);
            return QueryRows.DeferredTable<ResultRow0WithSortKeys>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultRow0WithSortKeys>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        private IEnumerable<ResultRow0WithSortKeys> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            var token = queryContext.CancellationToken;
            var __musoqProgressContext = queryContext.QueryProgress == null ? null : queryContext;
            Action<string, QueryPhase> OnPhaseChanged = queryContext.NotifyPhaseChanged;
            DataSourceEventHandler OnDataSourceProgress = queryContext.NotifyDataSourceProgress;
            QueryProgressEventHandler OnQueryProgress = queryContext.QueryProgress;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(queryContext.TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __rSchema = provider.GetSchema("#bench");
                var rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(rRowsSource.Chunks, __musoqProgressContext, "r:1") : rRowsSource.Chunks;
                var resultOrderRecords = new List<ResultRow0WithSortKeys>();
                OnPhaseChanged("compiled", QueryPhase.Where);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var rChunk in rRows)
                {
                    if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.FixtureRow> rChunkView)
                    {
                        if (rChunkView.Source is Musoq.Benchmarks.Scenarios.FixtureRow[] rChunkViewArray)
                        {
                            int rChunkViewOffset = rChunkView.Offset;
                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                            {
                                if ((rIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var r = rChunkViewArray[rChunkViewOffset + rIndex];
                                if (((((int?)(r.Key)) switch
                                {
                                    var __membership1Operand => (((int?)(0)) switch
                                    {
                                        var __membership1Member0 when (__membership1Operand is not null && __membership1Member0 is not null && (((decimal?)__membership1Operand == (decimal?)__membership1Member0))) => (bool?)true,
                                        var __membership1Member0 => (((int?)(2)) switch
                                        {
                                            var __membership1Member1 when (__membership1Operand is not null && __membership1Member1 is not null && (((decimal?)__membership1Operand == (decimal?)__membership1Member1))) => (bool?)true,
                                            var __membership1Member1 => (((int?)(2)) switch
                                            {
                                                var __membership1Member2 when (__membership1Operand is not null && __membership1Member2 is not null && (((decimal?)__membership1Operand == (decimal?)__membership1Member2))) => (bool?)true,
                                                var __membership1Member2 => (((DBNull)(null)) switch
                                                {
                                                    var __membership1Member3 when (__membership1Operand is not null && __membership1Member3 is not null && ((__membership1Operand is null && __membership1Member3 is null))) => (bool?)true,
                                                    var __membership1Member3 => (__membership1Operand is null || __membership1Member0 is null || __membership1Member1 is null || __membership1Member2 is null || __membership1Member3 is null) ? (bool?)null : false
                                                })})})})})) == true)
                                {
                                    resultOrderRecords.Add(new ResultRow0WithSortKeys(r.Id, resultOrderRecords.Count));
                                }
                            }

                            continue;
                        }

                        if (rChunkView.Source is List<Musoq.Benchmarks.Scenarios.FixtureRow> rChunkViewList)
                        {
                            int rChunkViewOffset = rChunkView.Offset;
                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                            {
                                if ((rIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var r = rChunkViewList[rChunkViewOffset + rIndex];
                                if (((((int?)(r.Key)) switch
                                {
                                    var __membership2Operand => (((int?)(0)) switch
                                    {
                                        var __membership2Member0 when (__membership2Operand is not null && __membership2Member0 is not null && (((decimal?)__membership2Operand == (decimal?)__membership2Member0))) => (bool?)true,
                                        var __membership2Member0 => (((int?)(2)) switch
                                        {
                                            var __membership2Member1 when (__membership2Operand is not null && __membership2Member1 is not null && (((decimal?)__membership2Operand == (decimal?)__membership2Member1))) => (bool?)true,
                                            var __membership2Member1 => (((int?)(2)) switch
                                            {
                                                var __membership2Member2 when (__membership2Operand is not null && __membership2Member2 is not null && (((decimal?)__membership2Operand == (decimal?)__membership2Member2))) => (bool?)true,
                                                var __membership2Member2 => (((DBNull)(null)) switch
                                                {
                                                    var __membership2Member3 when (__membership2Operand is not null && __membership2Member3 is not null && ((__membership2Operand is null && __membership2Member3 is null))) => (bool?)true,
                                                    var __membership2Member3 => (__membership2Operand is null || __membership2Member0 is null || __membership2Member1 is null || __membership2Member2 is null || __membership2Member3 is null) ? (bool?)null : false
                                                })})})})})) == true)
                                {
                                    resultOrderRecords.Add(new ResultRow0WithSortKeys(r.Id, resultOrderRecords.Count));
                                }
                            }

                            continue;
                        }
                    }

                    for (int rIndex = 0, rIndexCount = rChunk.Count; rIndex < rIndexCount; ++rIndex)
                    {
                        if ((rIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var r = rChunk[rIndex];
                        if (((((int?)(r.Key)) switch
                        {
                            var __membership3Operand => (((int?)(0)) switch
                            {
                                var __membership3Member0 when (__membership3Operand is not null && __membership3Member0 is not null && (((decimal?)__membership3Operand == (decimal?)__membership3Member0))) => (bool?)true,
                                var __membership3Member0 => (((int?)(2)) switch
                                {
                                    var __membership3Member1 when (__membership3Operand is not null && __membership3Member1 is not null && (((decimal?)__membership3Operand == (decimal?)__membership3Member1))) => (bool?)true,
                                    var __membership3Member1 => (((int?)(2)) switch
                                    {
                                        var __membership3Member2 when (__membership3Operand is not null && __membership3Member2 is not null && (((decimal?)__membership3Operand == (decimal?)__membership3Member2))) => (bool?)true,
                                        var __membership3Member2 => (((DBNull)(null)) switch
                                        {
                                            var __membership3Member3 when (__membership3Operand is not null && __membership3Member3 is not null && ((__membership3Operand is null && __membership3Member3 is null))) => (bool?)true,
                                            var __membership3Member3 => (__membership3Operand is null || __membership3Member0 is null || __membership3Member1 is null || __membership3Member2 is null || __membership3Member3 is null) ? (bool?)null : false
                                        })})})})})) == true)
                        {
                            resultOrderRecords.Add(new ResultRow0WithSortKeys(r.Id, resultOrderRecords.Count));
                        }
                    }
                }

                EvaluationHelper.SortRecordsIfUnordered(resultOrderRecords, ResultRow0WithSortKeysComparer.Instance);
                return resultOrderRecords;
            }
            finally
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }
        }

        private IEnumerable<ResultRow0WithSortKeys> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, queryContext))
            {
                yield return __musoqShapeRow;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnPhaseChanged(string queryId, QueryPhase phase)
        {
            PhaseChanged?.Invoke(this, new QueryPhaseEventArgs(queryId, phase));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnDataSourceProgress(object sender, DataSourceEventArgs e)
        {
            DataSourceProgress?.Invoke(this, e);
        }

        private sealed class ResultRow0WithSortKeys : Row
        {
            public ResultRow0WithSortKeys(int Id, int __ordinal)
            {
                this.Id = Id;
                this.__ordinal = __ordinal;
            }

            public int Id { get; private set; }
            public int __ordinal { get; private set; }
            public override int Count => 1;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }
        }

        private sealed class ResultRow0 : Row
        {
            public int Id { get; private set; }

            public ResultRow0(int __value0)
            {
                Id = __value0;
            }

            public override int Count => 1;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }
        }

        private sealed class ResultRow0WithSortKeysComparer : IComparer<ResultRow0WithSortKeys>
        {
            public static readonly ResultRow0WithSortKeysComparer Instance = new ResultRow0WithSortKeysComparer();
            public int Compare(ResultRow0WithSortKeys left, ResultRow0WithSortKeys right)
            {
                var comparison = left.Id.CompareTo(right.Id);
                if (comparison != 0)
                    return comparison;
                return left.__ordinal.CompareTo(right.__ordinal);
            }
        }
    }
}
