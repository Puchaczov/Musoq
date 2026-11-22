// === Parsed Query ===
/*
select r.Id as Id, d.Id as Match
from bench.rows() r right join bench.reconcile() d on r.Key = d.Key
order by r.Id
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.Id as r.Id, r.Key as r.Key, d.Id as d.Id, d.Key as d.Key]
    Join [RightOuter] [(r.Key = d.Key)]
      SchemaScan [#bench.rows() as r]
      SchemaScan [#bench.reconcile() as d]
  Sort [r.Id]
    Project [r.Id as Id, d.Id as Match]
      CteRef [rd as rd]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.Id as r.Id, d.Id as d.Id]
    PhysicalHashJoin [RightOuter] [build: r.Key] [probe: d.Key]
      PhysicalSchemaScan [#bench.rows() as r]
      PhysicalSchemaScan [#bench.reconcile() as d]
  PhysicalSort [r.Id]
    PhysicalProject [r.Id as Id, d.Id as Match]
      PhysicalCteRef [rd as rd]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Id: int <- property Id
      Key: int <- property Key
    SourceEntity [d: FixtureRow]
      Id: int <- property Id
      Key: int <- property Key
    Generated [Statement0Row0]
      r.Id: int? <- field r_Id
      d.Id: int <- field d_Id
    TableRow [rd]
      r.Id: int? <- field r_Id
      d.Id: int <- field d_Id
    Generated [ResultRow0]
      Id: int? <- field Id
      Match: int <- field Match

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: FixtureRow] -> statement0_rRows
    SourceScan [d: FixtureRow] -> statement0_dRows
    CreateTable [statement0: Statement0Row0]
    CreateHash [statement0RHash: int? -> FixtureRow]
    PhaseBoundary [Select]
    ChunkedForEach [r in statement0_rRows]
      HashAdd [statement0RHash[r.Key] += r]
    ChunkedForEach [d in statement0_dRows]
      HashProbe [statement0RHash[d.Key] -> statement0RHashMatches] [match: statement0RHashHasMatch]
        ForEach [r in statement0RHashMatches]
          Assign [statement0RHashHasMatch = TRUE]
          AppendRow [statement0 <- Statement0Row0(r.Id: r.Id, d.Id: d.Id)]
      HashProbeNoMatch
        AppendRow [statement0 <- Statement0Row0(r.Id: NULL, d.Id: d.Id)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [rd in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Id: rd.r.Id, Match: rd.d.Id)]
    SortOwnedBuffer [result by Id ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultShape0 from owned buffer result]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B08_RightJoin_BenchmarkTable
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
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("r.Id", typeof(int?), 0),
            new Column("d.Id", typeof(int), 1)
        };
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Id", typeof(int?), 0),
            new Column("Match", typeof(int), 1)
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
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            var token = queryContext.CancellationToken;
            var __musoqProgressContext = queryContext.QueryProgress == null ? null : queryContext;
            Action<string, QueryPhase> OnPhaseChanged = queryContext.NotifyPhaseChanged;
            DataSourceEventHandler OnDataSourceProgress = queryContext.NotifyDataSourceProgress;
            QueryProgressEventHandler OnQueryProgress = queryContext.QueryProgress;
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(queryContext.TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var result = new List<ResultShape0>();
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 rd = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(rd.r_Id, rd.d_Id) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = Nullable.Compare(left.Id, right.Id);
                    if (comparison != 0)
                        return comparison;
                    return left.__musoqSequence.CompareTo(right.__musoqSequence);
                }));
                return result;
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

        private IEnumerable<ResultShape0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            var __statement0_rSchema = provider.GetSchema("#bench");
            var statement0_rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__statement0_rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
            var statement0_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(statement0_rRowsSource.Chunks, __musoqProgressContext, "r:1") : statement0_rRowsSource.Chunks;
            var __statement0_dSchema = provider.GetSchema("#bench");
            var statement0_dRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__statement0_dSchema, "reconcile", new SourceExecutionContext("d:1", sourceExecutionPlans["d:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["d:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["d:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
            var statement0_dRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(statement0_dRowsSource.Chunks, __musoqProgressContext, "d:1") : statement0_dRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            var statement0RHash = new Dictionary<int?, HashJoinBucket<Musoq.Benchmarks.Scenarios.FixtureRow>>();
            foreach (var rChunk in statement0_rRows)
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
                            int? key = r.Key;
                            if (key == null)
                                continue;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0RHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Benchmarks.Scenarios.FixtureRow>(r);
                                }
                                else
                                {
                                    matches.Add(r);
                                }
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
                            int? key = r.Key;
                            if (key == null)
                                continue;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0RHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Benchmarks.Scenarios.FixtureRow>(r);
                                }
                                else
                                {
                                    matches.Add(r);
                                }
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
                    int? key = r.Key;
                    if (key == null)
                        continue;
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0RHash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<Musoq.Benchmarks.Scenarios.FixtureRow>(r);
                        }
                        else
                        {
                            matches.Add(r);
                        }
                    }
                }
            }

            foreach (var dChunk in statement0_dRows)
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
                            bool statement0RHashHasMatch = false;
                            int? key = d.Key;
                            if (key != null && statement0RHash.TryGetValue(key, out var statement0RHashMatches))
                            {
                                foreach (var r in statement0RHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0RHashHasMatch = true;
                                    statement0.Add(new Statement0Row0(r.Id, d.Id));
                                }
                            }

                            if (!statement0RHashHasMatch)
                            {
                                statement0.Add(new Statement0Row0(null, d.Id));
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
                            bool statement0RHashHasMatch = false;
                            int? key = d.Key;
                            if (key != null && statement0RHash.TryGetValue(key, out var statement0RHashMatches))
                            {
                                foreach (var r in statement0RHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0RHashHasMatch = true;
                                    statement0.Add(new Statement0Row0(r.Id, d.Id));
                                }
                            }

                            if (!statement0RHashHasMatch)
                            {
                                statement0.Add(new Statement0Row0(null, d.Id));
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
                    bool statement0RHashHasMatch = false;
                    int? key = d.Key;
                    if (key != null && statement0RHash.TryGetValue(key, out var statement0RHashMatches))
                    {
                        foreach (var r in statement0RHashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            statement0RHashHasMatch = true;
                            statement0.Add(new Statement0Row0(r.Id, d.Id));
                        }
                    }

                    if (!statement0RHashHasMatch)
                    {
                        statement0.Add(new Statement0Row0(null, d.Id));
                    }
                }
            }

            return statement0;
        }

        private sealed class Statement0Row0
        {
            public int? r_Id { get; }
            public int d_Id { get; }

            public Statement0Row0(int? __value0, int __value1)
            {
                r_Id = __value0;
                d_Id = __value1;
            }
        }

        private sealed class ResultShape0 : Row
        {
            public ResultShape0(int? Id, int Match)
            {
                this.Id = Id;
                this.Match = Match;
            }

            public int? Id { get; private set; }
            public int Match { get; private set; }
            public override int Count => 2;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Match, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Match),
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
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Match = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public int __musoqSequence { get; init; }
        }

        private sealed class CteRowResults
        {
            public List<Statement0Row0> Slot0;
        }
    }
}
