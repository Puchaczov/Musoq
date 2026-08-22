// === Parsed Query ===
/*
with recursive seeds (Id) as (select Id from values {{ Id: 1 }} seed), edges (SourceId, TargetId) as (select SourceId, TargetId from values {{ SourceId: 1, TargetId: 2 }, { SourceId: 2, TargetId: 3 }} edge), reachable (Id, Depth) as (select Id, 0 from seeds union (Id) select e.TargetId, r.Depth + 1 from reachable r inner join edges e on e.SourceId = r.Id) select Id, Depth from reachable order by Id
*/

// === Logical Plan ===
/*
Cte
  Definition [seeds]
    MultiStatement
      Project [seed.Id as Id]
        ValuesScan [1 rows as seed]
  Definition [edges]
    MultiStatement
      Project [edge.SourceId as SourceId, edge.TargetId as TargetId]
        ValuesScan [2 rows as edge]
  Definition [reachable]
    RecursiveCte [reachable] [Keyed: Id]
      Anchor
        MultiStatement
          Project [seeds.Id as Id, 0 as Depth]
            CteRef [seeds as seeds]
      RecursiveMember
        MultiStatement
          Project [r.Id as r.Id, r.Depth as r.Depth, e.SourceId as e.SourceId, e.TargetId as e.TargetId]
            Join [Inner] [(e.SourceId = r.Id)]
              CteRef [reachable as r]
              CteRef [edges as e]
          Project [e.TargetId as e.TargetId, (r.Depth + 1) as r.Depth + 1]
            CteRef [re as re]
  Query
    MultiStatement
      Sort [reachable.Id]
        Project [reachable.Id as Id, reachable.Depth as Depth]
          CteRef [reachable as reachable]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [seeds]
    PhysicalMultiStatement
      PhysicalProject [seed.Id as Id]
        PhysicalValuesScan [1 rows as seed]
  Definition [edges]
    PhysicalMultiStatement
      PhysicalProject [edge.SourceId as SourceId, edge.TargetId as TargetId]
        PhysicalValuesScan [2 rows as edge]
  Definition [reachable]
    PhysicalRecursiveCte [reachable] [Keyed: Id]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [seeds.Id as Id, 0 as Depth]
            PhysicalCteRef [seeds as seeds]
      Invariant [__recursive_reachable_invariant_0; ExistingHashIndex; fields SourceId, TargetId]
        PhysicalCteRef [edges as e]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [r.Id as r.Id, r.Depth as r.Depth, e.SourceId as e.SourceId, e.TargetId as e.TargetId]
            PhysicalHashJoin [Inner] [build: e.SourceId] [probe: r.Id]
              PhysicalCteRef [reachable as r]
              PhysicalCteRef [__recursive_reachable_invariant_0 as e]
          PhysicalProject [e.TargetId as e.TargetId, (r.Depth + 1) as r.Depth + 1]
            PhysicalCteRef [re as re]
  Query
    PhysicalMultiStatement
      PhysicalSort [reachable.Id]
        PhysicalProject [reachable.Id as Id, reachable.Depth as Depth]
          PhysicalCteRef [reachable as reachable]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Id: int <- field Id
    Generated [Cte0Row0]
      Id: int <- field Id
    UnknownShape [ValuesRowShape]
      SourceId: int <- field SourceId
      TargetId: int <- field TargetId
    Generated [Cte1Row0]
      SourceId: int <- field SourceId
      TargetId: int <- field TargetId
    HashPayload [Cte1HashPayload0]
      SourceId: int <- field SourceId
      TargetId: int <- field TargetId
    TableRow [seeds]
      Id: int <- field Id
    Generated [Cte2Row0]
      Id: int <- field Id
      Depth: int <- field Depth
    TableRow [r]
      Id: int <- field Id
      Depth: int <- field Depth
    TableRow [e]
      SourceId: int <- field SourceId
      TargetId: int <- field TargetId
    TableRow [reachable]
      Id: int <- field Id
      Depth: int <- field Depth
    Generated [ResultRow0]
      Id: int <- field Id
      Depth: int <- field Depth

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte2]
    PhaseBoundary [Select]
    ParallelBlock [cte-level-0, tasks 2, maxDegree 2]
      ParallelTask [seeds -> __parallelCteLevel0Task0Result]
        PhaseBoundary [Begin:cte0]
        PhaseBoundary [From:cte0]
        CreateValuesRows [cte0_seedRows: seedValues0C8F87F6Row0 x 1]
        CreateTable [cte0: Cte0Row0]
        PhaseBoundary [Select:cte0]
        ForEach [seed in cte0_seedRows]
          AppendRow [cte0 <- Cte0Row0(Id: seed.Id)]
        Assign [__parallelCteLevel0Task0Result = cte0]
        PhaseBoundary [End:cte0]
      ParallelTask [edges -> __parallelCteLevel0Task1Result]
        PhaseBoundary [Begin:cte1]
        PhaseBoundary [From:cte1]
        CreateValuesRows [cte1_edgeRows: edgeValues23A88678Row0 x 2]
        CreateTable [cte1: Cte1Row0]
        CreateHash [cte1HashSidecar0Sourceid: int -> Row]
        ForEach [edge in cte1_edgeRows]
          CreateGeneratedRow [cte1SidecarRow0 <- Cte1Row0(SourceId: edge.SourceId, TargetId: edge.TargetId)]
          AppendExistingRow [cte1 <- cte1SidecarRow0]
          CreateHashPayload [cte1SidecarPayload0 <- Cte1HashPayload0(SourceId: edge.SourceId, TargetId: edge.TargetId)]
          HashAdd [cte1HashSidecar0Sourceid[edge.SourceId] += cte1SidecarPayload0]
        StoreCteIndex [cte1HashSidecar0Sourceid -> _cteIndexResults.Slot0 Hash]
        PhaseBoundary [Select:cte1]
        Assign [__parallelCteLevel0Task1Result = cte1]
        PhaseBoundary [End:cte1]
      ParallelMerge
        StoreTable [__parallelCteLevel0Task0Result -> _cteRowResults.Slot0: List<Cte0Row0>]
        StoreTable [__parallelCteLevel0Task1Result -> _cteRowResults.Slot1: List<Cte1Row0>]
    PhaseBoundary [From:cte2]
    RecursiveCte [reachable; result cte2; frontiers cte2CurrentFrontier, cte2NextFrontier; identity Keyed via cte2Seen (Id); max iterations 1000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        ForEach [seeds in _cteRowResults.Slot0]
          RecursiveAppend [cte2CurrentFrontier <- Cte2Row0(Id: seeds.Id, Depth: 0); identity cte2Seen (Id); guard cte2.Count + cte2CurrentFrontier.Count < 10000000]
      InvariantSetup
        LoadCteIndex [cte2Invariant0Hash <- _cteIndexResults.Slot0 Hash: int]
      RecursiveMember
        ForEach [r in cte2CurrentFrontier]
          HashProbe [cte2Invariant0Hash[r.Id] -> cte2Invariant0HashMatches]
            ForEach [e in cte2Invariant0HashMatches]
              RecursiveAppend [cte2NextFrontier <- Cte2Row0(Id: e.TargetId, Depth: (r.Depth + 1)); identity cte2Seen (Id); guard cte2.Count + cte2NextFrontier.Count < 10000000]
    PhaseBoundary [Select:cte2]
    StoreTable [cte2 -> _cteRowResults.Slot2: List<Cte2Row0>]
    PhaseBoundary [End:cte2]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [reachable in _cteRowResults.Slot2]
      AppendShape [result <- ResultShape0(Id: reachable.Id, Depth: reachable.Depth)]
    SortShapeRows [result -> resultSorted by Id ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q222_RecursiveCteParallelSiblings
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_cte0_0 = new Column[]
        {
            new Column("Id", typeof(int), 0)
        };
        private static readonly Column[] __columns_compiled_cte1_1 = new Column[]
        {
            new Column("SourceId", typeof(int), 0),
            new Column("TargetId", typeof(int), 1)
        };
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Depth", typeof(int), 1)
        };
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public IDictionary<string, System.Object> Parameters { get; } = new Dictionary<string, System.Object>(StringComparer.Ordinal);
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }

        public event DataSourceEventHandler DataSourceProgress;
        public event QueryPhaseEventHandler PhaseChanged;
        public event QueryProgressEventHandler QueryProgress;
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Id, __musoqShapeRow.Depth);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _cteRowResults = new CteRowResults();
                var _cteIndexResults = new CteIndexResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte2", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.Select);
                List<Cte0Row0> __parallelCteLevel0Task0Result = null;
                List<Cte1Row0> __parallelCteLevel0Task1Result = null;
                var cteLevel0Runner = new CteLevel0Runner(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults, _cteIndexResults);
                Parallel.Invoke(new ParallelOptions() { CancellationToken = token, MaxDegreeOfParallelism = 2 }, cteLevel0Runner.RunCteLevel0Task0, cteLevel0Runner.RunCteLevel0Task1);
                token.ThrowIfCancellationRequested();
                __parallelCteLevel0Task0Result = cteLevel0Runner.Task0Result;
                __parallelCteLevel0Task1Result = cteLevel0Runner.Task1Result;
                _cteRowResults.Slot0 = __parallelCteLevel0Task0Result;
                _cteRowResults.Slot1 = __parallelCteLevel0Task1Result;
                OnPhaseChanged("compiled:cte2", QueryPhase.From);
                var cte2 = new List<Cte2Row0>();
                var cte2CurrentFrontier = new List<Cte2Row0>();
                var cte2NextFrontier = new List<Cte2Row0>();
                var cte2Seen = new HashSet<int>();
                int __cte2Iteration = 0;
                int __cte2CancellationCounter = 0;
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 seeds = __storedTable0Rows[__storedTable0Index];
                    var __cte2CurrentFrontierCandidate0 = seeds.Id;
                    var __cte2CurrentFrontierCandidate1 = 0;
                    if (cte2Seen.Add(__cte2CurrentFrontierCandidate0))
                    {
                        if (cte2.Count + cte2CurrentFrontier.Count >= 10000000)
                        {
                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                        }

                        cte2CurrentFrontier.Add(new Cte2Row0(__cte2CurrentFrontierCandidate0, __cte2CurrentFrontierCandidate1));
                    }
                }

                cte2.AddRange(cte2CurrentFrontier);
                if (cte2CurrentFrontier.Count > 0)
                {
                    var cte2Invariant0Hash = _cteIndexResults.Slot0;
                    while (cte2CurrentFrontier.Count > 0)
                    {
                        if ((__cte2Iteration & 63) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        if (__cte2Iteration >= 1000)
                        {
                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 1000);
                        }

                        __cte2Iteration++;
                        cte2NextFrontier.Clear();
                        for (int cte2CurrentFrontierIndex = 0; cte2CurrentFrontierIndex < cte2CurrentFrontier.Count; ++cte2CurrentFrontierIndex)
                        {
                            if (cte2CurrentFrontierIndex != 0 && (cte2CurrentFrontierIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Cte2Row0 r = (Cte2Row0)cte2CurrentFrontier[cte2CurrentFrontierIndex];
                            int key = r.Id;
                            if (cte2Invariant0Hash.TryGetValue(key, out var cte2Invariant0HashMatches))
                            {
                                foreach (var e in cte2Invariant0HashMatches)
                                {
                                    ++__cte2CancellationCounter;
                                    if ((__cte2CancellationCounter & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var __cte2NextFrontierCandidate0 = e.TargetId;
                                    var __cte2NextFrontierCandidate1 = (r.Depth + 1);
                                    if (cte2Seen.Add(__cte2NextFrontierCandidate0))
                                    {
                                        if (cte2.Count + cte2NextFrontier.Count >= 10000000)
                                        {
                                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                        }

                                        cte2NextFrontier.Add(new Cte2Row0(__cte2NextFrontierCandidate0, __cte2NextFrontierCandidate1));
                                    }
                                }
                            }
                        }

                        cte2.AddRange(cte2NextFrontier);
                        var __cte2FrontierSwap = cte2CurrentFrontier;
                        cte2CurrentFrontier = cte2NextFrontier;
                        cte2NextFrontier = __cte2FrontierSwap;
                    }
                }

                OnPhaseChanged("compiled:cte2", QueryPhase.Select);
                _cteRowResults.Slot2 = cte2;
                OnPhaseChanged("compiled:cte2", QueryPhase.End);
                var result = new List<ResultShape0>();
                var __storedTable2Rows = _cteRowResults.Slot2;
                for (int __storedTable2Index = 0; __storedTable2Index < __storedTable2Rows.Count; ++__storedTable2Index)
                {
                    if ((__storedTable2Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte2Row0 reachable = __storedTable2Rows[__storedTable2Index];
                    result.Add(new ResultShape0(reachable.Id, reachable.Depth));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
                    if (comparison != 0)
                        return comparison;
                    return 0;
                }));
                foreach (var resultSortedRowsRow in resultSortedRows)
                {
                    __musoqFinalShapeRows.Add(resultSortedRowsRow);
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
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnDataSourceProgress(object sender, DataSourceEventArgs e)
        {
            DataSourceProgress?.Invoke(this, e);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnPhaseChanged(string queryId, QueryPhase phase)
        {
            PhaseChanged?.Invoke(this, new QueryPhaseEventArgs(queryId, phase));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCteLevel0Task0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults)
        {
            List<Cte0Row0> __parallelCteLevel0Task0Result = null;
            token.ThrowIfCancellationRequested();
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            List<Cte0Row0> cte0 = null!;
            try
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.From);
                seedValues0C8F87F6Row0[] cte0_seedRows = new seedValues0C8F87F6Row0[]
                {
                    new seedValues0C8F87F6Row0(1)
                };
                cte0 = new List<Cte0Row0>();
                OnPhaseChanged("compiled:cte0", QueryPhase.Select);
                foreach (var seed in cte0_seedRows)
                {
                    token.ThrowIfCancellationRequested();
                    cte0.Add(new Cte0Row0(seed.Id));
                }

                __parallelCteLevel0Task0Result = cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }

            return __parallelCteLevel0Task0Result;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte1Row0> BuildCteLevel0Task1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults)
        {
            List<Cte1Row0> __parallelCteLevel0Task1Result = null;
            token.ThrowIfCancellationRequested();
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            List<Cte1Row0> cte1 = null!;
            try
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.From);
                edgeValues23A88678Row0[] cte1_edgeRows = new edgeValues23A88678Row0[]
                {
                    new edgeValues23A88678Row0(1, 2),
                    new edgeValues23A88678Row0(2, 3)
                };
                cte1 = new List<Cte1Row0>();
                var cte1HashSidecar0Sourceid = new Dictionary<int, HashJoinBucket<Cte1HashPayload0>>();
                foreach (var edge in cte1_edgeRows)
                {
                    token.ThrowIfCancellationRequested();
                    Cte1Row0 cte1SidecarRow0 = new Cte1Row0(edge.SourceId, edge.TargetId);
                    cte1.Add(cte1SidecarRow0);
                    Cte1HashPayload0 cte1SidecarPayload0 = new Cte1HashPayload0(edge.SourceId, edge.TargetId);
                    int cte1HashSidecar0SourceidKey0 = edge.SourceId;
                    {
                        ref var cte1HashSidecar0SourceidBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1HashSidecar0Sourceid, cte1HashSidecar0SourceidKey0, out var cte1HashSidecar0SourceidBucket0Exists);
                        if (!cte1HashSidecar0SourceidBucket0Exists)
                        {
                            cte1HashSidecar0SourceidBucket0 = new HashJoinBucket<Cte1HashPayload0>(cte1SidecarPayload0);
                        }
                        else
                        {
                            cte1HashSidecar0SourceidBucket0.Add(cte1SidecarPayload0);
                        }
                    }
                }

                _cteIndexResults.Slot0 = cte1HashSidecar0Sourceid;
                OnPhaseChanged("compiled:cte1", QueryPhase.Select);
                __parallelCteLevel0Task1Result = cte1;
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }

            return __parallelCteLevel0Task1Result;
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(int __value0)
            {
                Id = __value0;
            }

            public int Id { get; }
        }

        private readonly struct Cte1HashPayload0
        {
            public readonly int SourceId;
            public readonly int TargetId;
            public Cte1HashPayload0(int SourceId, int TargetId)
            {
                this.SourceId = SourceId;
                this.TargetId = TargetId;
            }
        }

        private sealed class Cte1Row0
        {
            public Cte1Row0(int __value0, int __value1)
            {
                SourceId = __value0;
                TargetId = __value1;
            }

            public int SourceId { get; }
            public int TargetId { get; }
        }

        private readonly struct Cte2Row0
        {
            public Cte2Row0(int Id, int Depth)
            {
                this.Id = Id;
                this.Depth = Depth;
            }

            public int Id { get; }
            public int Depth { get; }
        }

        private sealed class CteIndexResults
        {
            public Dictionary<int, HashJoinBucket<Cte1HashPayload0>> Slot0;
        }

        private sealed class CteLevel0Runner
        {
            private readonly CteIndexResults _cteIndexResults;
            private readonly CteRowResults _cteRowResults;
            private readonly Microsoft.Extensions.Logging.ILogger _logger;
            private readonly QueryRunContext? _musoqProgressContext;
            private readonly Musoq.Schema.DataSourceEventHandler _onDataSourceProgress;
            private readonly Action<string, QueryPhase> _onPhaseChanged;
            private readonly Musoq.Evaluator.QueryProgressEventHandler _onQueryProgress;
            private readonly Musoq.Schema.ISchemaProvider _provider;
            private readonly IReadOnlyDictionary<string, SourceExecutionPlan> _sourceExecutionPlans;
            private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _sourceRuntimeSettingsBySourceContextId;
            private readonly CancellationToken _token;
            public CteLevel0Runner(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults)
            {
                _provider = provider;
                _sourceRuntimeSettingsBySourceContextId = sourceRuntimeSettingsBySourceContextId;
                _sourceExecutionPlans = sourceExecutionPlans;
                _logger = logger;
                _token = token;
                _musoqProgressContext = __musoqProgressContext;
                _onDataSourceProgress = OnDataSourceProgress;
                _onQueryProgress = OnQueryProgress;
                _onPhaseChanged = OnPhaseChanged;
                this._cteRowResults = _cteRowResults;
                this._cteIndexResults = _cteIndexResults;
            }

            public List<Cte0Row0> Task0Result { get; private set; }
            public List<Cte1Row0> Task1Result { get; private set; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void RunCteLevel0Task0()
            {
                Task0Result = BuildCteLevel0Task0(_provider, _sourceRuntimeSettingsBySourceContextId, _sourceExecutionPlans, _logger, _token, _musoqProgressContext, _onDataSourceProgress, _onQueryProgress, _onPhaseChanged, _cteRowResults, _cteIndexResults);
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void RunCteLevel0Task1()
            {
                Task1Result = BuildCteLevel0Task1(_provider, _sourceRuntimeSettingsBySourceContextId, _sourceExecutionPlans, _logger, _token, _musoqProgressContext, _onDataSourceProgress, _onQueryProgress, _onPhaseChanged, _cteRowResults, _cteIndexResults);
            }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
            public List<Cte1Row0> Slot1;
            public List<Cte2Row0> Slot2;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, int __value1)
            {
                Id = __value0;
                Depth = __value1;
            }

            public override int Count => 2;
            public int Depth { get; private set; }
            public int Id { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Id = (int)value;
                        break;
                    case 1:
                        Depth = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Depth" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Id,
                1 => (object)Depth,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Id" => (object)Id,
                "Depth" => (object)Depth,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id, int Depth)
            {
                this.Id = Id;
                this.Depth = Depth;
            }

            public int Depth { get; }
            public int Id { get; }
        }

        private sealed class edgeValues23A88678Row0 : Row
        {
            public edgeValues23A88678Row0(int __value0, int __value1)
            {
                SourceId = __value0;
                TargetId = __value1;
            }

            public override int Count => 2;
            public int SourceId { get; private set; }
            public int TargetId { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        SourceId = (int)value;
                        break;
                    case 1:
                        TargetId = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "SourceId" => true,
                "TargetId" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)SourceId,
                1 => (object)TargetId,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "SourceId" => (object)SourceId,
                "TargetId" => (object)TargetId,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class seedValues0C8F87F6Row0 : Row
        {
            public seedValues0C8F87F6Row0(int __value0)
            {
                Id = __value0;
            }

            public override int Count => 1;
            public int Id { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Id = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Id,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Id" => (object)Id,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
