// === Parsed Query ===
/*
select r.Id as Id, d.Id as Match
from bench.rows() r
outer apply (select e.Id, e.Key
from bench.empty() e
where e.Key = r.Key) d
order by r.Id
*/

// === Logical Plan ===
/*
Cte
  Definition [_dt_1]
    MultiStatement
      Project [e.Id as Id, e.Key as Key]
        SchemaScan [#bench.empty() as e]
  Query
    MultiStatement
      Project [r.Id as r.Id, r.Key as r.Key, d.Id as d.Id, d.Key as d.Key]
        Join [LeftOuter] [(d.Key = r.Key)]
          SchemaScan [#bench.rows() as r]
          CteRef [_dt_1 as d]
      Sort [r.Id]
        Project [r.Id as Id, d.Id as Match]
          CteRef [rd as rd]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [_dt_1]
    PhysicalMultiStatement
      PhysicalProject [e.Id as Id, e.Key as Key]
        PhysicalSchemaScan [#bench.empty() as e]
  Query
    PhysicalMultiStatement
      PhysicalProject [r.Id as r.Id, d.Id as d.Id]
        PhysicalHashJoin [LeftOuter] [build: d.Key] [probe: r.Key]
          PhysicalSchemaScan [#bench.rows() as r]
          PhysicalCteRef [_dt_1 as d]
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
    SourceEntity [e: FixtureRow]
      Id: int <- property Id
      Key: int <- property Key
    HashPayload [DHashPayload0]
      Id: int <- field Id
    TableRow [d]
      Id: int <- field Id
      Key: int <- field Key
    Generated [Statement0Row0]
      r.Id: int <- field r_Id
      d.Id: int? <- field d_Id
    TableRow [rd]
      r.Id: int <- field r_Id
      d.Id: int? <- field d_Id
    Generated [ResultRow0]
      Id: int <- field Id
      Match: int? <- field Match

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte1]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [End:cte0]
    SourceScan [r: FixtureRow] -> statement0_rRows
    SourceScan [e: FixtureRow] -> cte0_eRows
    CreateTable [statement0: Statement0Row0]
    CreateHash [statement0DHash: int? -> Row]
    ChunkedForEach [e in cte0_eRows]
      Let [key: int? = e.Key]
      ContinueIf [(key = NULL)]
      CreateHashPayload [d <- DHashPayload0(Id: e.Id)]
      HashAdd [statement0DHash[key] += d]
    ChunkedForEach [r in statement0_rRows]
      HashProbe [statement0DHash[r.Key] -> statement0DHashMatches] [match: statement0DHashHasMatch]
        ForEach [d in statement0DHashMatches]
          Assign [statement0DHashHasMatch = TRUE]
          AppendRow [statement0 <- Statement0Row0(r.Id: r.Id, d.Id: d.Id)]
      HashProbeNoMatch
        AppendRow [statement0 <- Statement0Row0(r.Id: r.Id, d.Id: NULL)]
    StoreTable [statement0 -> _cteRowResults.Slot1: List<Statement0Row0>]
    PhaseBoundary [End:cte1]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [rd in _cteRowResults.Slot1]
      AppendShape [result <- ResultShape0(Id: rd.r.Id, Match: rd.d.Id)]
    SortOwnedBuffer [result by Id ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultShape0 from owned buffer result]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B13_OuterApply_BenchmarkTable
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
            new Column("r.Id", typeof(int), 0),
            new Column("d.Id", typeof(int?), 1)
        };
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
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
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var result = new List<ResultShape0>();
                var __storedTable1Rows = _cteRowResults.Slot1;
                for (int __storedTable1Index = 0; __storedTable1Index < __storedTable1Rows.Count; ++__storedTable1Index)
                {
                    if ((__storedTable1Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 rd = __storedTable1Rows[__storedTable1Index];
                    result.Add(new ResultShape0(rd.r_Id, rd.d_Id) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
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
        private static List<Statement0Row0> BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                var __statement0_rSchema = provider.GetSchema("#bench");
                var statement0_rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__statement0_rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var statement0_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(statement0_rRowsSource.Chunks, __musoqProgressContext, "r:1") : statement0_rRowsSource.Chunks;
                var __cte0_eSchema = provider.GetSchema("#bench");
                var cte0_eRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__cte0_eSchema, "empty", new SourceExecutionContext("e:2", sourceExecutionPlans["e:2"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["e:2"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["e:2"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var cte0_eRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(cte0_eRowsSource.Chunks, __musoqProgressContext, "e:2") : cte0_eRowsSource.Chunks;
                var statement0 = new List<Statement0Row0>();
                var statement0DHash = new Dictionary<int?, HashJoinBucket<DHashPayload0>>();
                foreach (var eChunk in cte0_eRows)
                {
                    if (eChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.FixtureRow> eChunkView)
                    {
                        if (eChunkView.Source is Musoq.Benchmarks.Scenarios.FixtureRow[] eChunkViewArray)
                        {
                            int eChunkViewOffset = eChunkView.Offset;
                            for (int eIndex = 0, eIndexCount = eChunkView.Count; eIndex < eIndexCount; ++eIndex)
                            {
                                if ((eIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var e = eChunkViewArray[eChunkViewOffset + eIndex];
                                int? key = e.Key;
                                if ((key == null))
                                {
                                    continue;
                                }

                                DHashPayload0 d = new DHashPayload0(e.Id);
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0DHash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<DHashPayload0>(d);
                                    }
                                    else
                                    {
                                        matches.Add(d);
                                    }
                                }
                            }

                            continue;
                        }

                        if (eChunkView.Source is List<Musoq.Benchmarks.Scenarios.FixtureRow> eChunkViewList)
                        {
                            int eChunkViewOffset = eChunkView.Offset;
                            for (int eIndex = 0, eIndexCount = eChunkView.Count; eIndex < eIndexCount; ++eIndex)
                            {
                                if ((eIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var e = eChunkViewList[eChunkViewOffset + eIndex];
                                int? key = e.Key;
                                if ((key == null))
                                {
                                    continue;
                                }

                                DHashPayload0 d = new DHashPayload0(e.Id);
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0DHash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<DHashPayload0>(d);
                                    }
                                    else
                                    {
                                        matches.Add(d);
                                    }
                                }
                            }

                            continue;
                        }
                    }

                    for (int eIndex = 0, eIndexCount = eChunk.Count; eIndex < eIndexCount; ++eIndex)
                    {
                        if ((eIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var e = eChunk[eIndex];
                        int? key = e.Key;
                        if ((key == null))
                        {
                            continue;
                        }

                        DHashPayload0 d = new DHashPayload0(e.Id);
                        {
                            ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0DHash, key, out var matchesExists);
                            if (!matchesExists)
                            {
                                matches = new HashJoinBucket<DHashPayload0>(d);
                            }
                            else
                            {
                                matches.Add(d);
                            }
                        }
                    }
                }

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
                                bool statement0DHashHasMatch = false;
                                int? key = r.Key;
                                if (key != null && statement0DHash.TryGetValue(key, out var statement0DHashMatches))
                                {
                                    foreach (var d in statement0DHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        statement0DHashHasMatch = true;
                                        statement0.Add(new Statement0Row0(r.Id, d.Id));
                                    }
                                }

                                if (!statement0DHashHasMatch)
                                {
                                    statement0.Add(new Statement0Row0(r.Id, null));
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
                                bool statement0DHashHasMatch = false;
                                int? key = r.Key;
                                if (key != null && statement0DHash.TryGetValue(key, out var statement0DHashMatches))
                                {
                                    foreach (var d in statement0DHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        statement0DHashHasMatch = true;
                                        statement0.Add(new Statement0Row0(r.Id, d.Id));
                                    }
                                }

                                if (!statement0DHashHasMatch)
                                {
                                    statement0.Add(new Statement0Row0(r.Id, null));
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
                        bool statement0DHashHasMatch = false;
                        int? key = r.Key;
                        if (key != null && statement0DHash.TryGetValue(key, out var statement0DHashMatches))
                        {
                            foreach (var d in statement0DHashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                statement0DHashHasMatch = true;
                                statement0.Add(new Statement0Row0(r.Id, d.Id));
                            }
                        }

                        if (!statement0DHashHasMatch)
                        {
                            statement0.Add(new Statement0Row0(r.Id, null));
                        }
                    }
                }

                return statement0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }
        }

        private readonly struct DHashPayload0
        {
            public readonly int Id;
            public DHashPayload0(int Id)
            {
                this.Id = Id;
            }
        }

        private sealed class Statement0Row0
        {
            public int r_Id { get; }
            public int? d_Id { get; }

            public Statement0Row0(int __value0, int? __value1)
            {
                r_Id = __value0;
                d_Id = __value1;
            }
        }

        private sealed class ResultShape0 : Row
        {
            public ResultShape0(int Id, int? Match)
            {
                this.Id = Id;
                this.Match = Match;
            }

            public int Id { get; private set; }
            public int? Match { get; private set; }
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

            public int __musoqSequence { get; init; }
        }

        private sealed class CteRowResults
        {
            public List<Statement0Row0> Slot1;
        }
    }
}
