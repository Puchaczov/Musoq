// === Parsed Query ===
/*
select r.Id as Id, (select d.Id
from bench.lookup() d
where d.Id <= r.Id
order by d.Id desc
take 1) as Match
from bench.rows() r
order by r.Id
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [r.Id]
    Project [r.Id as Id, ApplyScalar[Subquery0](r.Id; MultiStatement
  Take [1]
    Sort [d.Id DESC]
      Project [d.Id as Id]
        Filter [(d.Id <= outer[0])]
          SchemaScan [#bench.lookup() as d]) as Match]
      SchemaScan [#bench.rows() as r]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [r.Id]
    PhysicalProject [r.Id as Id, ApplyScalar[Subquery0](r.Id; MultiStatement
  Take [1]
    Sort [d.Id DESC]
      Project [d.Id as Id]
        Filter [(d.Id <= outer[0])]
          SchemaScan [#bench.lookup() as d]) as Match]
      PhysicalSchemaScan [#bench.rows() as r]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Id: int <- property Id
    GeneratedRecord [ResultRow0WithSortKeys]
      Id: int <- field Id
      Match: int? <- field Match
      __ordinal: int <- field __ordinal
    Generated [ResultRow0]
      Id: int <- field Id
      Match: int? <- field Match

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: FixtureRow] -> rRows
    CreateRecordList [resultOrderRecords: ResultRow0WithSortKeys]
    PhaseBoundary [Select]
    ChunkedForEach [r in rRows]
      Let [id: int = r.Id]
      AppendRecord [resultOrderRecords <- ResultRow0WithSortKeys(Id: id, Match: ApplyScalar[Subquery0](r.Id))]
    OrderRecordList [resultOrderRecords: ResultRow0WithSortKeys by Id ASC]
    TransferOwnedRows [resultOrderRecords -> result: ResultRow0WithSortKeys]
    ReturnDeferredTable [result: ResultRow0WithSortKeys from owned buffer resultOrderRecords]

  Subquery
ExecutionPlan [Subquery0]
  Shapes
    SourceEntity [d: FixtureRow]
      Id: int <- property Id
    GeneratedRecord [Subquery0ResultRow0WithSortKeys]
      Id: int <- field Id
      __ordinal: int <- field __ordinal
    Generated [Subquery0ResultRow0]
      Id: int <- field Id

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [d: FixtureRow] -> dRows
    CreateBoundedRecordList [resultOrderRecords: Subquery0ResultRow0WithSortKeys by Id DESC, take 1]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [d in dRows]
      Let [id: int = d.Id]
      If [(id <= __outer0)]
        AppendRecord [resultOrderRecords <- Subquery0ResultRow0WithSortKeys(Id: id)]
    MaterializeRecordListToShapeRows [resultOrderRecords -> result: Subquery0ResultShape0 fields 0]
    ReturnDeferredTable [result: Subquery0ResultRow0 <- Subquery0ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B10_CorrelatedFallback_BenchmarkTable
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
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_Subquery0_d_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0) });
        private static readonly Column[] __columns_Subquery0_result_1 = new Column[]
        {
            new Column("Id", typeof(int), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0) });
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Match", typeof(int?), 1)
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
                var rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__rSchema, "rows", new SourceExecutionContext("r:2", sourceExecutionPlans["r:2"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:2"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:2"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(rRowsSource.Chunks, __musoqProgressContext, "r:2") : rRowsSource.Chunks;
                var resultOrderRecords = new List<ResultRow0WithSortKeys>();
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
                                int id = r.Id;
                                resultOrderRecords.Add(new ResultRow0WithSortKeys(id, global::Musoq.Evaluator.Runtime.SubqueryExecution.Scalar<Subquery0ResultShape0, int?>(__ExecuteSubquery0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, r.Id), static row => (int?)row.Id, token), resultOrderRecords.Count));
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
                                int id = r.Id;
                                resultOrderRecords.Add(new ResultRow0WithSortKeys(id, global::Musoq.Evaluator.Runtime.SubqueryExecution.Scalar<Subquery0ResultShape0, int?>(__ExecuteSubquery0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, r.Id), static row => (int?)row.Id, token), resultOrderRecords.Count));
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
                        int id = r.Id;
                        resultOrderRecords.Add(new ResultRow0WithSortKeys(id, global::Musoq.Evaluator.Runtime.SubqueryExecution.Scalar<Subquery0ResultShape0, int?>(__ExecuteSubquery0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, r.Id), static row => (int?)row.Id, token), resultOrderRecords.Count));
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

        private static IEnumerable<Subquery0ResultShape0> __ExecuteSubquery0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, int __outer0)
        {
            try
            {
                var __musoqFinalShapeRows = new List<Subquery0ResultShape0>();
                OnPhaseChanged("Subquery0", QueryPhase.Begin);
                OnPhaseChanged("Subquery0", QueryPhase.From);
                var __dSchema = provider.GetSchema("#bench");
                var dRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__dSchema, "lookup", new SourceExecutionContext("d:2", sourceExecutionPlans["d:2"], token, __schemaColumns_Subquery0_d_0, sourceRuntimeSettingsBySourceContextId["d:2"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["d:2"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var dRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(dRowsSource.Chunks, __musoqProgressContext, "d:2") : dRowsSource.Chunks;
                var resultOrderRecords = new Subquery0ResultRow0WithSortKeysComparer.SingleRecordBuffer();
                OnPhaseChanged("Subquery0", QueryPhase.Where);
                OnPhaseChanged("Subquery0", QueryPhase.Select);
                foreach (var dChunk in dRows)
                {
                    if (dChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.FixtureRow> dChunkView)
                    {
                        if (dChunkView.Source is Musoq.Benchmarks.Scenarios.FixtureRow[] dChunkViewArray)
                        {
                            int dChunkViewOffset = dChunkView.Offset;
                            for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                            {
                                if ((dIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var d = dChunkViewArray[dChunkViewOffset + dIndex];
                                int id = d.Id;
                                if ((id <= __outer0))
                                {
                                    resultOrderRecords.Add(new Subquery0ResultRow0WithSortKeys(id, resultOrderRecords.Count));
                                }
                            }

                            continue;
                        }

                        if (dChunkView.Source is List<Musoq.Benchmarks.Scenarios.FixtureRow> dChunkViewList)
                        {
                            int dChunkViewOffset = dChunkView.Offset;
                            for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                            {
                                if ((dIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var d = dChunkViewList[dChunkViewOffset + dIndex];
                                int id = d.Id;
                                if ((id <= __outer0))
                                {
                                    resultOrderRecords.Add(new Subquery0ResultRow0WithSortKeys(id, resultOrderRecords.Count));
                                }
                            }

                            continue;
                        }
                    }

                    for (int dIndex = 0, dIndexCount = dChunk.Count; dIndex < dIndexCount; ++dIndex)
                    {
                        if ((dIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var d = dChunk[dIndex];
                        int id = d.Id;
                        if ((id <= __outer0))
                        {
                            resultOrderRecords.Add(new Subquery0ResultRow0WithSortKeys(id, resultOrderRecords.Count));
                        }
                    }
                }

                foreach (var resultRecord in resultOrderRecords)
                {
                    __musoqFinalShapeRows.Add(new Subquery0ResultShape0(resultRecord.Id));
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("Subquery0", QueryPhase.End);
                }
            }
        }

        private readonly struct Subquery0ResultRow0WithSortKeys
        {
            public Subquery0ResultRow0WithSortKeys(int Id, int __ordinal)
            {
                this.Id = Id;
                this.__ordinal = __ordinal;
            }

            public int Id { get; }
            public int __ordinal { get; }
        }

        private sealed class Subquery0ResultRow0 : Row
        {
            public int Id { get; private set; }

            public Subquery0ResultRow0(int __value0)
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

        private sealed class Subquery0ResultShape0
        {
            public Subquery0ResultShape0(int Id)
            {
                this.Id = Id;
            }

            public int Id { get; }
        }

        private sealed class Subquery0ResultRow0WithSortKeysComparer : IComparer<Subquery0ResultRow0WithSortKeys>
        {
            public static readonly Subquery0ResultRow0WithSortKeysComparer Instance = new Subquery0ResultRow0WithSortKeysComparer();
            public int Compare(Subquery0ResultRow0WithSortKeys left, Subquery0ResultRow0WithSortKeys right)
            {
                var comparison = left.Id.CompareTo(right.Id);
                comparison = -comparison;
                if (comparison != 0)
                    return comparison;
                return left.__ordinal.CompareTo(right.__ordinal);
            }

            public struct SingleRecordBuffer
            {
                private Subquery0ResultRow0WithSortKeys _best;
                private bool _hasRecord;
                public int Count { get; private set; }

                public void Add(Subquery0ResultRow0WithSortKeys candidate)
                {
                    Count++;
                    if (!_hasRecord || Instance.Compare(candidate, _best) < 0)
                    {
                        _best = candidate;
                        _hasRecord = true;
                    }
                }

                public Enumerator GetEnumerator() => new Enumerator(_best, _hasRecord);
                public struct Enumerator(Subquery0ResultRow0WithSortKeys record, bool hasRecord)
                {
                    private bool _pending = hasRecord;
                    public Subquery0ResultRow0WithSortKeys Current => record;

                    public bool MoveNext()
                    {
                        var pending = _pending;
                        _pending = false;
                        return pending;
                    }
                }
            }
        }

        private sealed class ResultRow0WithSortKeys : Row
        {
            public ResultRow0WithSortKeys(int Id, int? Match, int __ordinal)
            {
                this.Id = Id;
                this.Match = Match;
                this.__ordinal = __ordinal;
            }

            public int Id { get; private set; }
            public int? Match { get; private set; }
            public int __ordinal { get; private set; }
            public override int Count => 2;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Match, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Match),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Match" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Match = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }
        }

        private sealed class ResultRow0 : Row
        {
            public int Id { get; private set; }
            public int? Match { get; private set; }

            public ResultRow0(int __value0, int? __value1)
            {
                Id = __value0;
                Match = __value1;
            }

            public override int Count => 2;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Match, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Match),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Match" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Match = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
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
