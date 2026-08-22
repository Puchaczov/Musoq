// === Parsed Query ===
/*
with recursive edges (SourceId, TargetId) as (select SourceId, TargetId from #graph.edges()), reachable (Id, Depth) as (select RootId, 0 from #graph.roots() union (Id) select e.TargetId, r.Depth + 1 from reachable r inner join edges e on e.SourceId = r.Id) select Id, Depth from reachable order by Id
*/

// === Logical Plan ===
/*
Cte
  Definition [edges]
    MultiStatement
      Project [ko3iko.SourceId as SourceId, ko3iko.TargetId as TargetId]
        SchemaScan [#graph.edges() as ko3iko]
  Definition [reachable]
    RecursiveCte [reachable] [Keyed: Id]
      Anchor
        MultiStatement
          Project [vo04qt.RootId as Id, 0 as Depth]
            SchemaScan [#graph.roots() as vo04qt]
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
  Definition [edges]
    PhysicalMultiStatement
      PhysicalProject [ko3iko.SourceId as SourceId, ko3iko.TargetId as TargetId]
        PhysicalSchemaScan [#graph.edges() as ko3iko]
  Definition [reachable]
    PhysicalRecursiveCte [reachable] [Keyed: Id]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [vo04qt.RootId as Id, 0 as Depth]
            PhysicalSchemaScan [#graph.roots() as vo04qt]
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
    SourceEntity [ko3iko: RecursiveGraphEdge]
      SourceId: int <- property SourceId
      TargetId: int <- property TargetId
    Generated [Cte0Row0]
      SourceId: int <- field SourceId
      TargetId: int <- field TargetId
    HashPayload [Cte0HashPayload0]
      SourceId: int <- field SourceId
      TargetId: int <- field TargetId
    SourceEntity [vo04qt: RecursiveGraphRoot]
      RootId: int <- property RootId
    Generated [Cte1Row0]
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
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [From:cte0]
    SourceScan [ko3iko: RecursiveGraphEdge] -> cte0_ko3ikoRows
    CreateTable [cte0: Cte0Row0]
    CreateHash [cte0HashSidecar0Sourceid: int -> Row]
    ChunkedForEach [ko3iko in cte0_ko3ikoRows]
      CreateGeneratedRow [cte0SidecarRow0 <- Cte0Row0(SourceId: ko3iko.SourceId, TargetId: ko3iko.TargetId)]
      AppendExistingRow [cte0 <- cte0SidecarRow0]
      CreateHashPayload [cte0SidecarPayload0 <- Cte0HashPayload0(SourceId: ko3iko.SourceId, TargetId: ko3iko.TargetId)]
      HashAdd [cte0HashSidecar0Sourceid[ko3iko.SourceId] += cte0SidecarPayload0]
    StoreCteIndex [cte0HashSidecar0Sourceid -> _cteIndexResults.Slot0 Hash]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Begin:cte1]
    PhaseBoundary [From:cte1]
    RecursiveCte [reachable; result cte1; frontiers cte1CurrentFrontier, cte1NextFrontier; identity Keyed via cte1Seen (Id); max iterations 1000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        SourceScan [vo04qt: RecursiveGraphRoot] -> cte1CurrentFrontier_vo04qtRows
        ChunkedForEach [vo04qt in cte1CurrentFrontier_vo04qtRows]
          RecursiveAppend [cte1CurrentFrontier <- Cte1Row0(Id: vo04qt.RootId, Depth: 0); identity cte1Seen (Id); guard cte1.Count + cte1CurrentFrontier.Count < 10000000]
      InvariantSetup
        LoadCteIndex [cte1Invariant0Hash <- _cteIndexResults.Slot0 Hash: int]
      RecursiveMember
        ForEach [r in cte1CurrentFrontier]
          HashProbe [cte1Invariant0Hash[r.Id] -> cte1Invariant0HashMatches]
            ForEach [e in cte1Invariant0HashMatches]
              RecursiveAppend [cte1NextFrontier <- Cte1Row0(Id: e.TargetId, Depth: (r.Depth + 1)); identity cte1Seen (Id); guard cte1.Count + cte1NextFrontier.Count < 10000000]
    PhaseBoundary [Select:cte1]
    StoreTable [cte1 -> _cteRowResults.Slot1: List<Cte1Row0>]
    PhaseBoundary [End:cte1]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [reachable in _cteRowResults.Slot1]
      AppendShape [result <- ResultShape0(Id: reachable.Id, Depth: reachable.Depth)]
    SortShapeRows [result -> resultSorted by Id ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q205_RecursivePriorMaterializedCte
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
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("SourceId", typeof(int), 0),
            new Column("TargetId", typeof(int), 1)
        };
        private static readonly Column[] __columns_compiled_result_3 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Depth", typeof(int), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("SourceId", typeof(int), 0), new Column("TargetId", typeof(int), 1) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_vo04qt_2 = Array.AsReadOnly(new ISchemaColumn[] { new Column("RootId", typeof(int), 0) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults, _cteIndexResults);
                OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled:cte1", QueryPhase.From);
                    var cte1 = new List<Cte1Row0>();
                    var cte1CurrentFrontier = new List<Cte1Row0>();
                    var cte1NextFrontier = new List<Cte1Row0>();
                    var cte1Seen = new HashSet<int>();
                    int __cte1Iteration = 0;
                    int __cte1CancellationCounter = 0;
                    var __cte1CurrentFrontier_vo04qtSchema = provider.GetSchema("#graph");
                    var cte1CurrentFrontier_vo04qtRowsSource = __cte1CurrentFrontier_vo04qtSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot>("roots", new SourceExecutionContext("vo04qt:2", sourceExecutionPlans["vo04qt:2"], token, __schemaColumns_compiled_vo04qt_2, sourceRuntimeSettingsBySourceContextId["vo04qt:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                    var cte1CurrentFrontier_vo04qtRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot>(cte1CurrentFrontier_vo04qtRowsSource.Chunks, __musoqProgressContext, "vo04qt:2") : cte1CurrentFrontier_vo04qtRowsSource.Chunks;
                    foreach (var vo04qtChunk in cte1CurrentFrontier_vo04qtRows)
                    {
                        if (vo04qtChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot> vo04qtChunkView)
                        {
                            if (vo04qtChunkView.Source is Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot[] vo04qtChunkViewArray)
                            {
                                int vo04qtChunkViewOffset = vo04qtChunkView.Offset;
                                for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunkView.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                                {
                                    if ((vo04qtIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var vo04qt = vo04qtChunkViewArray[vo04qtChunkViewOffset + vo04qtIndex];
                                    var __cte1CurrentFrontierCandidate0 = vo04qt.RootId;
                                    var __cte1CurrentFrontierCandidate1 = 0;
                                    if (cte1Seen.Add(__cte1CurrentFrontierCandidate0))
                                    {
                                        if (cte1.Count + cte1CurrentFrontier.Count >= 10000000)
                                        {
                                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                        }

                                        cte1CurrentFrontier.Add(new Cte1Row0(__cte1CurrentFrontierCandidate0, __cte1CurrentFrontierCandidate1));
                                    }
                                }

                                continue;
                            }

                            if (vo04qtChunkView.Source is List<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot> vo04qtChunkViewList)
                            {
                                int vo04qtChunkViewOffset = vo04qtChunkView.Offset;
                                for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunkView.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                                {
                                    if ((vo04qtIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var vo04qt = vo04qtChunkViewList[vo04qtChunkViewOffset + vo04qtIndex];
                                    var __cte1CurrentFrontierCandidate0 = vo04qt.RootId;
                                    var __cte1CurrentFrontierCandidate1 = 0;
                                    if (cte1Seen.Add(__cte1CurrentFrontierCandidate0))
                                    {
                                        if (cte1.Count + cte1CurrentFrontier.Count >= 10000000)
                                        {
                                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                        }

                                        cte1CurrentFrontier.Add(new Cte1Row0(__cte1CurrentFrontierCandidate0, __cte1CurrentFrontierCandidate1));
                                    }
                                }

                                continue;
                            }
                        }

                        for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunk.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                        {
                            if ((vo04qtIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var vo04qt = vo04qtChunk[vo04qtIndex];
                            var __cte1CurrentFrontierCandidate0 = vo04qt.RootId;
                            var __cte1CurrentFrontierCandidate1 = 0;
                            if (cte1Seen.Add(__cte1CurrentFrontierCandidate0))
                            {
                                if (cte1.Count + cte1CurrentFrontier.Count >= 10000000)
                                {
                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                }

                                cte1CurrentFrontier.Add(new Cte1Row0(__cte1CurrentFrontierCandidate0, __cte1CurrentFrontierCandidate1));
                            }
                        }
                    }

                    cte1.AddRange(cte1CurrentFrontier);
                    if (cte1CurrentFrontier.Count > 0)
                    {
                        var cte1Invariant0Hash = _cteIndexResults.Slot0;
                        while (cte1CurrentFrontier.Count > 0)
                        {
                            if ((__cte1Iteration & 63) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            if (__cte1Iteration >= 1000)
                            {
                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 1000);
                            }

                            __cte1Iteration++;
                            cte1NextFrontier.Clear();
                            for (int cte1CurrentFrontierIndex = 0; cte1CurrentFrontierIndex < cte1CurrentFrontier.Count; ++cte1CurrentFrontierIndex)
                            {
                                if (cte1CurrentFrontierIndex != 0 && (cte1CurrentFrontierIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                Cte1Row0 r = (Cte1Row0)cte1CurrentFrontier[cte1CurrentFrontierIndex];
                                int key = r.Id;
                                if (cte1Invariant0Hash.TryGetValue(key, out var cte1Invariant0HashMatches))
                                {
                                    foreach (var e in cte1Invariant0HashMatches)
                                    {
                                        ++__cte1CancellationCounter;
                                        if ((__cte1CancellationCounter & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var __cte1NextFrontierCandidate0 = e.TargetId;
                                        var __cte1NextFrontierCandidate1 = (r.Depth + 1);
                                        if (cte1Seen.Add(__cte1NextFrontierCandidate0))
                                        {
                                            if (cte1.Count + cte1NextFrontier.Count >= 10000000)
                                            {
                                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                            }

                                            cte1NextFrontier.Add(new Cte1Row0(__cte1NextFrontierCandidate0, __cte1NextFrontierCandidate1));
                                        }
                                    }
                                }
                            }

                            cte1.AddRange(cte1NextFrontier);
                            var __cte1FrontierSwap = cte1CurrentFrontier;
                            cte1CurrentFrontier = cte1NextFrontier;
                            cte1NextFrontier = __cte1FrontierSwap;
                        }
                    }

                    OnPhaseChanged("compiled:cte1", QueryPhase.Select);
                    _cteRowResults.Slot1 = cte1;
                }
                finally
                {
                    OnPhaseChanged("compiled:cte1", QueryPhase.End);
                }

                var result = new List<ResultShape0>();
                var __storedTable1Rows = _cteRowResults.Slot1;
                for (int __storedTable1Index = 0; __storedTable1Index < __storedTable1Rows.Count; ++__storedTable1Index)
                {
                    if ((__storedTable1Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte1Row0 reachable = __storedTable1Rows[__storedTable1Index];
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
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                var __cte0_ko3ikoSchema = provider.GetSchema("#graph");
                var cte0_ko3ikoRowsSource = __cte0_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge>("edges", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte0_ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge>(cte0_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : cte0_ko3ikoRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                var cte0HashSidecar0Sourceid = new Dictionary<int, HashJoinBucket<Cte0HashPayload0>>();
                foreach (var ko3ikoChunk in cte0_ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                Cte0Row0 cte0SidecarRow0 = new Cte0Row0(ko3iko.SourceId, ko3iko.TargetId);
                                cte0.Add(cte0SidecarRow0);
                                Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(ko3iko.SourceId, ko3iko.TargetId);
                                int cte0HashSidecar0SourceidKey0 = ko3iko.SourceId;
                                {
                                    ref var cte0HashSidecar0SourceidBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0HashSidecar0Sourceid, cte0HashSidecar0SourceidKey0, out var cte0HashSidecar0SourceidBucket0Exists);
                                    if (!cte0HashSidecar0SourceidBucket0Exists)
                                    {
                                        cte0HashSidecar0SourceidBucket0 = new HashJoinBucket<Cte0HashPayload0>(cte0SidecarPayload0);
                                    }
                                    else
                                    {
                                        cte0HashSidecar0SourceidBucket0.Add(cte0SidecarPayload0);
                                    }
                                }
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                Cte0Row0 cte0SidecarRow0 = new Cte0Row0(ko3iko.SourceId, ko3iko.TargetId);
                                cte0.Add(cte0SidecarRow0);
                                Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(ko3iko.SourceId, ko3iko.TargetId);
                                int cte0HashSidecar0SourceidKey0 = ko3iko.SourceId;
                                {
                                    ref var cte0HashSidecar0SourceidBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0HashSidecar0Sourceid, cte0HashSidecar0SourceidKey0, out var cte0HashSidecar0SourceidBucket0Exists);
                                    if (!cte0HashSidecar0SourceidBucket0Exists)
                                    {
                                        cte0HashSidecar0SourceidBucket0 = new HashJoinBucket<Cte0HashPayload0>(cte0SidecarPayload0);
                                    }
                                    else
                                    {
                                        cte0HashSidecar0SourceidBucket0.Add(cte0SidecarPayload0);
                                    }
                                }
                            }

                            continue;
                        }
                    }

                    for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunk.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                    {
                        if ((ko3ikoIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var ko3iko = ko3ikoChunk[ko3ikoIndex];
                        Cte0Row0 cte0SidecarRow0 = new Cte0Row0(ko3iko.SourceId, ko3iko.TargetId);
                        cte0.Add(cte0SidecarRow0);
                        Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(ko3iko.SourceId, ko3iko.TargetId);
                        int cte0HashSidecar0SourceidKey0 = ko3iko.SourceId;
                        {
                            ref var cte0HashSidecar0SourceidBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0HashSidecar0Sourceid, cte0HashSidecar0SourceidKey0, out var cte0HashSidecar0SourceidBucket0Exists);
                            if (!cte0HashSidecar0SourceidBucket0Exists)
                            {
                                cte0HashSidecar0SourceidBucket0 = new HashJoinBucket<Cte0HashPayload0>(cte0SidecarPayload0);
                            }
                            else
                            {
                                cte0HashSidecar0SourceidBucket0.Add(cte0SidecarPayload0);
                            }
                        }
                    }
                }

                _cteIndexResults.Slot0 = cte0HashSidecar0Sourceid;
                return cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        private readonly struct Cte0HashPayload0
        {
            public readonly int SourceId;
            public readonly int TargetId;
            public Cte0HashPayload0(int SourceId, int TargetId)
            {
                this.SourceId = SourceId;
                this.TargetId = TargetId;
            }
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(int __value0, int __value1)
            {
                SourceId = __value0;
                TargetId = __value1;
            }

            public int SourceId { get; }
            public int TargetId { get; }
        }

        private readonly struct Cte1Row0
        {
            public Cte1Row0(int Id, int Depth)
            {
                this.Id = Id;
                this.Depth = Depth;
            }

            public int Id { get; }
            public int Depth { get; }
        }

        private sealed class CteIndexResults
        {
            public Dictionary<int, HashJoinBucket<Cte0HashPayload0>> Slot0;
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
            public List<Cte1Row0> Slot1;
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
    }
}
