// === Parsed Query ===
/*
with recursive reachable (Id, Depth) as (select RootId, 0 from #graph.roots() union (Id) select e.TargetId, r.Depth + 1 from reachable r inner join #graph.edges() e on e.SourceId = r.Id) select Id, Depth from reachable order by Id
*/

// === Logical Plan ===
/*
Cte
  Definition [reachable]
    RecursiveCte [reachable] [Keyed: Id]
      Anchor
        MultiStatement
          Project [ko3iko.RootId as Id, 0 as Depth]
            SchemaScan [#graph.roots() as ko3iko]
      RecursiveMember
        MultiStatement
          Project [r.Id as r.Id, r.Depth as r.Depth, e.SourceId as e.SourceId, e.TargetId as e.TargetId]
            Join [Inner] [(e.SourceId = r.Id)]
              CteRef [reachable as r]
              SchemaScan [#graph.edges() as e]
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
  Definition [reachable]
    PhysicalRecursiveCte [reachable] [Keyed: Id]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [ko3iko.RootId as Id, 0 as Depth]
            PhysicalSchemaScan [#graph.roots() as ko3iko]
      Invariant [__recursive_reachable_invariant_0; HashIndex; fields SourceId, TargetId]
        PhysicalSchemaScan [#graph.edges() as e]
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
    SourceEntity [ko3iko: RecursiveGraphRoot]
      RootId: int <- property RootId
    Generated [Cte0Row0]
      Id: int <- field Id
      Depth: int <- field Depth
    SourceEntity [e: RecursiveGraphEdge]
      SourceId: int <- property SourceId
      TargetId: int <- property TargetId
    Generated [Cte0Invariant0Row0]
      SourceId: int <- field SourceId
      TargetId: int <- field TargetId
    TableRow [r]
      Id: int <- field Id
      Depth: int <- field Depth
    TableRow [reachable]
      Id: int <- field Id
      Depth: int <- field Depth
    Generated [ResultRow0]
      Id: int <- field Id
      Depth: int <- field Depth

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    RecursiveCte [reachable; result cte0; frontiers cte0CurrentFrontier, cte0NextFrontier; identity Keyed via cte0Seen (Id); max iterations 1000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        SourceScan [ko3iko: RecursiveGraphRoot] -> cte0CurrentFrontier_ko3ikoRows
        ChunkedForEach [ko3iko in cte0CurrentFrontier_ko3ikoRows]
          RecursiveAppend [cte0CurrentFrontier <- Cte0Row0(Id: ko3iko.RootId, Depth: 0); identity cte0Seen (Id); guard cte0.Count + cte0CurrentFrontier.Count < 10000000]
      InvariantSetup
        CreateHash [cte0Invariant0Hash: int -> Row]
        SourceScan [e: RecursiveGraphEdge] -> cte0Invariant0_eRows
        ChunkedForEach [e in cte0Invariant0_eRows]
          ScopedBlock
            RecursiveSnapshotGuard [__cte0SnapshotRows < 10000000; reachable]
            CreateGeneratedRow [cte0Invariant0Row <- Cte0Invariant0Row0(SourceId: e.SourceId, TargetId: e.TargetId)]
            HashAdd [cte0Invariant0Hash[cte0Invariant0Row.SourceId] += cte0Invariant0Row]
      RecursiveMember
        ForEach [r in cte0CurrentFrontier]
          HashProbe [cte0Invariant0Hash[r.Id] -> cte0Invariant0HashMatches]
            ForEach [e in cte0Invariant0HashMatches]
              RecursiveAppend [cte0NextFrontier <- Cte0Row0(Id: e.TargetId, Depth: (r.Depth + 1)); identity cte0Seen (Id); guard cte0.Count + cte0NextFrontier.Count < 10000000]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [From]
    ForEach [reachable in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Id: reachable.Id, Depth: reachable.Depth)]
    PhaseBoundary [Select]
    SortShapeRows [result -> resultSorted by Id ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P12_RecursiveInvariantIndexedEdges_Full
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Musoq.Schema;
    using Musoq.Schema.Diagnostics;
    using Musoq.Schema.Optimization;
    using Musoq.Evaluator;
    using Musoq.Evaluator.Diagnostics;
    using Musoq.Evaluator.Tables;
    using Musoq.Evaluator.Helpers;
    using Musoq.Evaluator.Runtime;
    using Musoq.Schema.DataSources;
    using System.Linq;

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, IProfiledRunnable
    {
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Depth", typeof(int), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_e_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("SourceId", typeof(int), 0), new Column("TargetId", typeof(int), 1) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("RootId", typeof(int), 0) });
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

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            ArgumentNullException.ThrowIfNull(profileRecorder);
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0_Profiled(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken, profileRecorder), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Id, __musoqShapeRow.Depth);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
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
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.From);
                    var cte0 = new List<Cte0Row0>();
                    var cte0CurrentFrontier = new List<Cte0Row0>();
                    var cte0NextFrontier = new List<Cte0Row0>();
                    int __cte0SnapshotRows = 0;
                    var cte0Seen = new HashSet<int>();
                    int __cte0Iteration = 0;
                    int __cte0CancellationCounter = 0;
                    var __cte0CurrentFrontier_ko3ikoSchema = provider.GetSchema("#graph");
                    var cte0CurrentFrontier_ko3ikoRowsSource = __cte0CurrentFrontier_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot>("roots", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                    var cte0CurrentFrontier_ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot>(cte0CurrentFrontier_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : cte0CurrentFrontier_ko3ikoRowsSource.Chunks;
                    foreach (var ko3ikoChunk in cte0CurrentFrontier_ko3ikoRows)
                    {
                        if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot> ko3ikoChunkView)
                        {
                            if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot[] ko3ikoChunkViewArray)
                            {
                                int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                                for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                                {
                                    if ((ko3ikoIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                    var __cte0CurrentFrontierCandidate0 = ko3iko.RootId;
                                    var __cte0CurrentFrontierCandidate1 = 0;
                                    if (cte0Seen.Add(__cte0CurrentFrontierCandidate0))
                                    {
                                        if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                        {
                                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                        }

                                        cte0CurrentFrontier.Add(new Cte0Row0(__cte0CurrentFrontierCandidate0, __cte0CurrentFrontierCandidate1));
                                    }
                                }

                                continue;
                            }

                            if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot> ko3ikoChunkViewList)
                            {
                                int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                                for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                                {
                                    if ((ko3ikoIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                    var __cte0CurrentFrontierCandidate0 = ko3iko.RootId;
                                    var __cte0CurrentFrontierCandidate1 = 0;
                                    if (cte0Seen.Add(__cte0CurrentFrontierCandidate0))
                                    {
                                        if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                        {
                                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                        }

                                        cte0CurrentFrontier.Add(new Cte0Row0(__cte0CurrentFrontierCandidate0, __cte0CurrentFrontierCandidate1));
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
                            var __cte0CurrentFrontierCandidate0 = ko3iko.RootId;
                            var __cte0CurrentFrontierCandidate1 = 0;
                            if (cte0Seen.Add(__cte0CurrentFrontierCandidate0))
                            {
                                if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                {
                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                }

                                cte0CurrentFrontier.Add(new Cte0Row0(__cte0CurrentFrontierCandidate0, __cte0CurrentFrontierCandidate1));
                            }
                        }
                    }

                    cte0.AddRange(cte0CurrentFrontier);
                    if (cte0CurrentFrontier.Count > 0)
                    {
                        var cte0Invariant0Hash = new Dictionary<int, HashJoinBucket<Cte0Invariant0Row0>>();
                        var __cte0Invariant0_eSchema = provider.GetSchema("#graph");
                        var cte0Invariant0_eRowsSource = __cte0Invariant0_eSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge>("edges", new SourceExecutionContext("e:2", sourceExecutionPlans["e:2"], token, __schemaColumns_compiled_e_1, sourceRuntimeSettingsBySourceContextId["e:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                        var cte0Invariant0_eRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge>(cte0Invariant0_eRowsSource.Chunks, __musoqProgressContext, "e:2") : cte0Invariant0_eRowsSource.Chunks;
                        foreach (var eChunk in cte0Invariant0_eRows)
                        {
                            if (eChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge> eChunkView)
                            {
                                if (eChunkView.Source is Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge[] eChunkViewArray)
                                {
                                    int eChunkViewOffset = eChunkView.Offset;
                                    for (int eIndex = 0, eIndexCount = eChunkView.Count; eIndex < eIndexCount; ++eIndex)
                                    {
                                        if ((eIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var e = eChunkViewArray[eChunkViewOffset + eIndex];
                                        {
                                            if (__cte0SnapshotRows >= 10000000)
                                            {
                                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 10000000);
                                            }

                                            __cte0SnapshotRows++;
                                            Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.SourceId, e.TargetId);
                                            int cte0Invariant0HashKey = cte0Invariant0Row.SourceId;
                                            {
                                                ref var cte0Invariant0HashMatches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Invariant0Hash, cte0Invariant0HashKey, out var cte0Invariant0HashMatchesExists);
                                                if (!cte0Invariant0HashMatchesExists)
                                                {
                                                    cte0Invariant0HashMatches = new HashJoinBucket<Cte0Invariant0Row0>(cte0Invariant0Row);
                                                }
                                                else
                                                {
                                                    cte0Invariant0HashMatches.Add(cte0Invariant0Row);
                                                }
                                            }
                                        }
                                    }

                                    continue;
                                }

                                if (eChunkView.Source is List<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge> eChunkViewList)
                                {
                                    int eChunkViewOffset = eChunkView.Offset;
                                    for (int eIndex = 0, eIndexCount = eChunkView.Count; eIndex < eIndexCount; ++eIndex)
                                    {
                                        if ((eIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var e = eChunkViewList[eChunkViewOffset + eIndex];
                                        {
                                            if (__cte0SnapshotRows >= 10000000)
                                            {
                                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 10000000);
                                            }

                                            __cte0SnapshotRows++;
                                            Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.SourceId, e.TargetId);
                                            int cte0Invariant0HashKey = cte0Invariant0Row.SourceId;
                                            {
                                                ref var cte0Invariant0HashMatches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Invariant0Hash, cte0Invariant0HashKey, out var cte0Invariant0HashMatchesExists);
                                                if (!cte0Invariant0HashMatchesExists)
                                                {
                                                    cte0Invariant0HashMatches = new HashJoinBucket<Cte0Invariant0Row0>(cte0Invariant0Row);
                                                }
                                                else
                                                {
                                                    cte0Invariant0HashMatches.Add(cte0Invariant0Row);
                                                }
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
                                {
                                    if (__cte0SnapshotRows >= 10000000)
                                    {
                                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 10000000);
                                    }

                                    __cte0SnapshotRows++;
                                    Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.SourceId, e.TargetId);
                                    int cte0Invariant0HashKey = cte0Invariant0Row.SourceId;
                                    {
                                        ref var cte0Invariant0HashMatches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Invariant0Hash, cte0Invariant0HashKey, out var cte0Invariant0HashMatchesExists);
                                        if (!cte0Invariant0HashMatchesExists)
                                        {
                                            cte0Invariant0HashMatches = new HashJoinBucket<Cte0Invariant0Row0>(cte0Invariant0Row);
                                        }
                                        else
                                        {
                                            cte0Invariant0HashMatches.Add(cte0Invariant0Row);
                                        }
                                    }
                                }
                            }
                        }

                        while (cte0CurrentFrontier.Count > 0)
                        {
                            if ((__cte0Iteration & 63) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            if (__cte0Iteration >= 1000)
                            {
                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 1000);
                            }

                            __cte0Iteration++;
                            cte0NextFrontier.Clear();
                            for (int cte0CurrentFrontierIndex = 0; cte0CurrentFrontierIndex < cte0CurrentFrontier.Count; ++cte0CurrentFrontierIndex)
                            {
                                if (cte0CurrentFrontierIndex != 0 && (cte0CurrentFrontierIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                Cte0Row0 r = (Cte0Row0)cte0CurrentFrontier[cte0CurrentFrontierIndex];
                                int key = r.Id;
                                if (cte0Invariant0Hash.TryGetValue(key, out var cte0Invariant0HashMatches))
                                {
                                    foreach (var e in cte0Invariant0HashMatches)
                                    {
                                        ++__cte0CancellationCounter;
                                        if ((__cte0CancellationCounter & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var __cte0NextFrontierCandidate0 = e.TargetId;
                                        var __cte0NextFrontierCandidate1 = (r.Depth + 1);
                                        if (cte0Seen.Add(__cte0NextFrontierCandidate0))
                                        {
                                            if (cte0.Count + cte0NextFrontier.Count >= 10000000)
                                            {
                                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                            }

                                            cte0NextFrontier.Add(new Cte0Row0(__cte0NextFrontierCandidate0, __cte0NextFrontierCandidate1));
                                        }
                                    }
                                }
                            }

                            cte0.AddRange(cte0NextFrontier);
                            var __cte0FrontierSwap = cte0CurrentFrontier;
                            cte0CurrentFrontier = cte0NextFrontier;
                            cte0NextFrontier = __cte0FrontierSwap;
                        }
                    }

                    OnPhaseChanged("compiled:cte0", QueryPhase.Select);
                    _cteRowResults.Slot0 = cte0;
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
                }

                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.From);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 reachable = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(reachable.Id, reachable.Depth));
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
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

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            var __profileScopeDepth = profileRecorder?.GetCurrentOperatorScopeDepth() ?? 0;
            try
            {
                QueryProgressEventHandler OnQueryProgress = QueryProgress;
                var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
                Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
                try
                {
                    var _cteRowResults = new CteRowResults();
                    var __musoqExecutionState = ExecutionState.Capture(Parameters);
                    ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                    var __musoqFinalShapeRows = new List<ResultShape0>();
                    var __op24Handle = profileRecorder?.GetOperatorHandle("op24", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op25Handle = profileRecorder?.GetOperatorHandle("op25", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op26Handle = profileRecorder?.GetOperatorHandle("op26", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op27Handle = profileRecorder?.GetOperatorHandle("op27", "RecursiveCte") ?? OperatorProfileHandle.None;
                    var __op29Handle = profileRecorder?.GetOperatorHandle("op29", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op30Handle = profileRecorder?.GetOperatorHandle("op30", "ChunkedForEach") ?? OperatorProfileHandle.None;
                    var __op31Handle = profileRecorder?.GetOperatorHandle("op31", "RecursiveAppend") ?? OperatorProfileHandle.None;
                    var __op33Handle = profileRecorder?.GetOperatorHandle("op33", "CreateHash") ?? OperatorProfileHandle.None;
                    var __op34Handle = profileRecorder?.GetOperatorHandle("op34", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op35Handle = profileRecorder?.GetOperatorHandle("op35", "ChunkedForEach") ?? OperatorProfileHandle.None;
                    var __op36Handle = profileRecorder?.GetOperatorHandle("op36", "ScopedBlock") ?? OperatorProfileHandle.None;
                    var __op37Handle = profileRecorder?.GetOperatorHandle("op37", "RecursiveSnapshotGuard") ?? OperatorProfileHandle.None;
                    var __op38Handle = profileRecorder?.GetOperatorHandle("op38", "CreateGeneratedRow") ?? OperatorProfileHandle.None;
                    var __op39Handle = profileRecorder?.GetOperatorHandle("op39", "HashAdd") ?? OperatorProfileHandle.None;
                    var __op41Handle = profileRecorder?.GetOperatorHandle("op41", "ForEach") ?? OperatorProfileHandle.None;
                    var __op42Handle = profileRecorder?.GetOperatorHandle("op42", "HashProbe") ?? OperatorProfileHandle.None;
                    var __op43Handle = profileRecorder?.GetOperatorHandle("op43", "ForEach") ?? OperatorProfileHandle.None;
                    var __op44Handle = profileRecorder?.GetOperatorHandle("op44", "RecursiveAppend") ?? OperatorProfileHandle.None;
                    var __op45Handle = profileRecorder?.GetOperatorHandle("op45", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op46Handle = profileRecorder?.GetOperatorHandle("op46", "StoreTable") ?? OperatorProfileHandle.None;
                    var __op47Handle = profileRecorder?.GetOperatorHandle("op47", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op48Handle = profileRecorder?.GetOperatorHandle("op48", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op49Handle = profileRecorder?.GetOperatorHandle("op49", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op50Handle = profileRecorder?.GetOperatorHandle("op50", "ForEach") ?? OperatorProfileHandle.None;
                    var __op51Handle = profileRecorder?.GetOperatorHandle("op51", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op52Handle = profileRecorder?.GetOperatorHandle("op52", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op53Handle = profileRecorder?.GetOperatorHandle("op53", "SortShapeRows") ?? OperatorProfileHandle.None;
                    long __op31InputRows = 0L;
                    long __op31OutputRows = 0L;
                    long __op39OutputRows = 0L;
                    long __op42InputRows = 0L;
                    long __op43InputRows = 0L;
                    long __op43OutputRows = 0L;
                    long __op44InputRows = 0L;
                    long __op44OutputRows = 0L;
                    long __op51OutputRows = 0L;
                    var __op24Scope = profileRecorder?.BeginOperatorValue(__op24Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op24Scope.Dispose();
                    var __op25Scope = profileRecorder?.BeginOperatorValue(__op25Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                    __op25Scope.Dispose();
                    try
                    {
                        var __op26Scope = profileRecorder?.BeginOperatorValue(__op26Handle) ?? OperatorProfileValueScope.None;
                        OnPhaseChanged("compiled:cte0", QueryPhase.From);
                        __op26Scope.Dispose();
                        long __op27InputRows = 0L;
                        long __op27OutputRows = 0L;
                        var __op27Scope = profileRecorder?.BeginOperatorValue(__op27Handle) ?? OperatorProfileValueScope.None;
                        List<Cte0Row0> cte0;
                        try
                        {
                            cte0 = new List<Cte0Row0>();
                            var cte0CurrentFrontier = new List<Cte0Row0>();
                            var cte0NextFrontier = new List<Cte0Row0>();
                            int __cte0SnapshotRows = 0;
                            var cte0Seen = new HashSet<int>();
                            int __cte0Iteration = 0;
                            int __cte0CancellationCounter = 0;
                            var __op29Scope = profileRecorder?.BeginOperatorValue(__op29Handle) ?? OperatorProfileValueScope.None;
                            var __cte0CurrentFrontier_ko3ikoSchema = provider.GetSchema("#graph");
                            var cte0CurrentFrontier_ko3ikoRowsProfile = profileRecorder?.CreateSourceRecorder("ko3iko");
                            var cte0CurrentFrontier_ko3ikoRowsSource = __cte0CurrentFrontier_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot>("roots", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress, cte0CurrentFrontier_ko3ikoRowsProfile == null ? SourceDiagnostics.None : cte0CurrentFrontier_ko3ikoRowsProfile.CreateDiagnostics()), Array.Empty<object>());
                            var cte0CurrentFrontier_ko3ikoRows = cte0CurrentFrontier_ko3ikoRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot>(cte0CurrentFrontier_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : cte0CurrentFrontier_ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot>(cte0CurrentFrontier_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : cte0CurrentFrontier_ko3ikoRowsSource.Chunks, cte0CurrentFrontier_ko3ikoRowsProfile);
                            __op29Scope.Dispose();
                            long __op30InputRows = 0L;
                            long __op30OutputRows = 0L;
                            var __op30Scope = profileRecorder?.BeginOperatorValue(__op30Handle) ?? OperatorProfileValueScope.None;
                            try
                            {
                                foreach (var ko3ikoChunk in cte0CurrentFrontier_ko3ikoRows)
                                {
                                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot> ko3ikoChunkView)
                                    {
                                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot[] ko3ikoChunkViewArray)
                                        {
                                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                                            {
                                                if ((ko3ikoIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                                __op30InputRows++;
                                                __op30OutputRows++;
                                                __op31InputRows += 1;
                                                var __cte0CurrentFrontierCandidate0 = ko3iko.RootId;
                                                var __cte0CurrentFrontierCandidate1 = 0;
                                                if (cte0Seen.Add(__cte0CurrentFrontierCandidate0))
                                                {
                                                    if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                                    {
                                                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                                    }

                                                    cte0CurrentFrontier.Add(new Cte0Row0(__cte0CurrentFrontierCandidate0, __cte0CurrentFrontierCandidate1));
                                                    __op31OutputRows += 1;
                                                }
                                            }

                                            continue;
                                        }

                                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot> ko3ikoChunkViewList)
                                        {
                                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                                            {
                                                if ((ko3ikoIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                                __op30InputRows++;
                                                __op30OutputRows++;
                                                __op31InputRows += 1;
                                                var __cte0CurrentFrontierCandidate0 = ko3iko.RootId;
                                                var __cte0CurrentFrontierCandidate1 = 0;
                                                if (cte0Seen.Add(__cte0CurrentFrontierCandidate0))
                                                {
                                                    if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                                    {
                                                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                                    }

                                                    cte0CurrentFrontier.Add(new Cte0Row0(__cte0CurrentFrontierCandidate0, __cte0CurrentFrontierCandidate1));
                                                    __op31OutputRows += 1;
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
                                        __op30InputRows++;
                                        __op30OutputRows++;
                                        __op31InputRows += 1;
                                        var __cte0CurrentFrontierCandidate0 = ko3iko.RootId;
                                        var __cte0CurrentFrontierCandidate1 = 0;
                                        if (cte0Seen.Add(__cte0CurrentFrontierCandidate0))
                                        {
                                            if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                            {
                                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                            }

                                            cte0CurrentFrontier.Add(new Cte0Row0(__cte0CurrentFrontierCandidate0, __cte0CurrentFrontierCandidate1));
                                            __op31OutputRows += 1;
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                __op30Scope.AddInputRows(__op30InputRows);
                                __op30Scope.AddOutputRows(__op30OutputRows);
                                __op30Scope.Dispose();
                            }

                            cte0.AddRange(cte0CurrentFrontier);
                            if (cte0CurrentFrontier.Count > 0)
                            {
                                var __op33Scope = profileRecorder?.BeginOperatorValue(__op33Handle) ?? OperatorProfileValueScope.None;
                                var cte0Invariant0Hash = new Dictionary<int, HashJoinBucket<Cte0Invariant0Row0>>();
                                __op33Scope.Dispose();
                                var __op34Scope = profileRecorder?.BeginOperatorValue(__op34Handle) ?? OperatorProfileValueScope.None;
                                var __cte0Invariant0_eSchema = provider.GetSchema("#graph");
                                var cte0Invariant0_eRowsProfile = profileRecorder?.CreateSourceRecorder("e");
                                var cte0Invariant0_eRowsSource = __cte0Invariant0_eSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge>("edges", new SourceExecutionContext("e:2", sourceExecutionPlans["e:2"], token, __schemaColumns_compiled_e_1, sourceRuntimeSettingsBySourceContextId["e:2"], logger, OnDataSourceProgress, cte0Invariant0_eRowsProfile == null ? SourceDiagnostics.None : cte0Invariant0_eRowsProfile.CreateDiagnostics()), Array.Empty<object>());
                                var cte0Invariant0_eRows = cte0Invariant0_eRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge>(cte0Invariant0_eRowsSource.Chunks, __musoqProgressContext, "e:2") : cte0Invariant0_eRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge>(cte0Invariant0_eRowsSource.Chunks, __musoqProgressContext, "e:2") : cte0Invariant0_eRowsSource.Chunks, cte0Invariant0_eRowsProfile);
                                __op34Scope.Dispose();
                                long __op35InputRows = 0L;
                                long __op35OutputRows = 0L;
                                var __op35Scope = profileRecorder?.BeginOperatorValue(__op35Handle) ?? OperatorProfileValueScope.None;
                                try
                                {
                                    foreach (var eChunk in cte0Invariant0_eRows)
                                    {
                                        if (eChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge> eChunkView)
                                        {
                                            if (eChunkView.Source is Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge[] eChunkViewArray)
                                            {
                                                int eChunkViewOffset = eChunkView.Offset;
                                                for (int eIndex = 0, eIndexCount = eChunkView.Count; eIndex < eIndexCount; ++eIndex)
                                                {
                                                    if ((eIndex & 1023) == 0)
                                                    {
                                                        token.ThrowIfCancellationRequested();
                                                    }

                                                    var e = eChunkViewArray[eChunkViewOffset + eIndex];
                                                    __op35InputRows++;
                                                    __op35OutputRows++;
                                                    var __op36Scope = profileRecorder?.BeginOperatorValue(__op36Handle) ?? OperatorProfileValueScope.None;
                                                    {
                                                        var __op37Scope = profileRecorder?.BeginOperatorValue(__op37Handle) ?? OperatorProfileValueScope.None;
                                                        if (__cte0SnapshotRows >= 10000000)
                                                        {
                                                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 10000000);
                                                        }

                                                        __cte0SnapshotRows++;
                                                        __op37Scope.Dispose();
                                                        var __op38Scope = profileRecorder?.BeginOperatorValue(__op38Handle) ?? OperatorProfileValueScope.None;
                                                        Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.SourceId, e.TargetId);
                                                        __op38Scope.Dispose();
                                                        int cte0Invariant0HashKey = cte0Invariant0Row.SourceId;
                                                        {
                                                            ref var cte0Invariant0HashMatches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Invariant0Hash, cte0Invariant0HashKey, out var cte0Invariant0HashMatchesExists);
                                                            if (!cte0Invariant0HashMatchesExists)
                                                            {
                                                                cte0Invariant0HashMatches = new HashJoinBucket<Cte0Invariant0Row0>(cte0Invariant0Row);
                                                            }
                                                            else
                                                            {
                                                                cte0Invariant0HashMatches.Add(cte0Invariant0Row);
                                                            }
                                                        }

                                                        __op39OutputRows += 1;
                                                    }

                                                    __op36Scope.Dispose();
                                                }

                                                continue;
                                            }

                                            if (eChunkView.Source is List<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge> eChunkViewList)
                                            {
                                                int eChunkViewOffset = eChunkView.Offset;
                                                for (int eIndex = 0, eIndexCount = eChunkView.Count; eIndex < eIndexCount; ++eIndex)
                                                {
                                                    if ((eIndex & 1023) == 0)
                                                    {
                                                        token.ThrowIfCancellationRequested();
                                                    }

                                                    var e = eChunkViewList[eChunkViewOffset + eIndex];
                                                    __op35InputRows++;
                                                    __op35OutputRows++;
                                                    var __op36Scope = profileRecorder?.BeginOperatorValue(__op36Handle) ?? OperatorProfileValueScope.None;
                                                    {
                                                        var __op37Scope = profileRecorder?.BeginOperatorValue(__op37Handle) ?? OperatorProfileValueScope.None;
                                                        if (__cte0SnapshotRows >= 10000000)
                                                        {
                                                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 10000000);
                                                        }

                                                        __cte0SnapshotRows++;
                                                        __op37Scope.Dispose();
                                                        var __op38Scope = profileRecorder?.BeginOperatorValue(__op38Handle) ?? OperatorProfileValueScope.None;
                                                        Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.SourceId, e.TargetId);
                                                        __op38Scope.Dispose();
                                                        int cte0Invariant0HashKey = cte0Invariant0Row.SourceId;
                                                        {
                                                            ref var cte0Invariant0HashMatches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Invariant0Hash, cte0Invariant0HashKey, out var cte0Invariant0HashMatchesExists);
                                                            if (!cte0Invariant0HashMatchesExists)
                                                            {
                                                                cte0Invariant0HashMatches = new HashJoinBucket<Cte0Invariant0Row0>(cte0Invariant0Row);
                                                            }
                                                            else
                                                            {
                                                                cte0Invariant0HashMatches.Add(cte0Invariant0Row);
                                                            }
                                                        }

                                                        __op39OutputRows += 1;
                                                    }

                                                    __op36Scope.Dispose();
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
                                            __op35InputRows++;
                                            __op35OutputRows++;
                                            var __op36Scope = profileRecorder?.BeginOperatorValue(__op36Handle) ?? OperatorProfileValueScope.None;
                                            {
                                                var __op37Scope = profileRecorder?.BeginOperatorValue(__op37Handle) ?? OperatorProfileValueScope.None;
                                                if (__cte0SnapshotRows >= 10000000)
                                                {
                                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 10000000);
                                                }

                                                __cte0SnapshotRows++;
                                                __op37Scope.Dispose();
                                                var __op38Scope = profileRecorder?.BeginOperatorValue(__op38Handle) ?? OperatorProfileValueScope.None;
                                                Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.SourceId, e.TargetId);
                                                __op38Scope.Dispose();
                                                int cte0Invariant0HashKey = cte0Invariant0Row.SourceId;
                                                {
                                                    ref var cte0Invariant0HashMatches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Invariant0Hash, cte0Invariant0HashKey, out var cte0Invariant0HashMatchesExists);
                                                    if (!cte0Invariant0HashMatchesExists)
                                                    {
                                                        cte0Invariant0HashMatches = new HashJoinBucket<Cte0Invariant0Row0>(cte0Invariant0Row);
                                                    }
                                                    else
                                                    {
                                                        cte0Invariant0HashMatches.Add(cte0Invariant0Row);
                                                    }
                                                }

                                                __op39OutputRows += 1;
                                            }

                                            __op36Scope.Dispose();
                                        }
                                    }
                                }
                                finally
                                {
                                    __op35Scope.AddInputRows(__op35InputRows);
                                    __op35Scope.AddOutputRows(__op35OutputRows);
                                    __op35Scope.Dispose();
                                }

                                while (cte0CurrentFrontier.Count > 0)
                                {
                                    if ((__cte0Iteration & 63) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    if (__cte0Iteration >= 1000)
                                    {
                                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 1000);
                                    }

                                    __cte0Iteration++;
                                    cte0NextFrontier.Clear();
                                    __op27InputRows += cte0CurrentFrontier.Count;
                                    long __op41InputRows = 0L;
                                    long __op41OutputRows = 0L;
                                    var __op41Scope = profileRecorder?.BeginOperatorValue(__op41Handle) ?? OperatorProfileValueScope.None;
                                    try
                                    {
                                        for (int cte0CurrentFrontierIndex = 0; cte0CurrentFrontierIndex < cte0CurrentFrontier.Count; ++cte0CurrentFrontierIndex)
                                        {
                                            if (cte0CurrentFrontierIndex != 0 && (cte0CurrentFrontierIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            Cte0Row0 r = (Cte0Row0)cte0CurrentFrontier[cte0CurrentFrontierIndex];
                                            __op41InputRows++;
                                            __op41OutputRows++;
                                            __op42InputRows += 1;
                                            int key = r.Id;
                                            if (cte0Invariant0Hash.TryGetValue(key, out var cte0Invariant0HashMatches))
                                            {
                                                foreach (var e in cte0Invariant0HashMatches)
                                                {
                                                    __op43InputRows++;
                                                    __op43OutputRows++;
                                                    __op44InputRows += 1;
                                                    ++__cte0CancellationCounter;
                                                    if ((__cte0CancellationCounter & 1023) == 0)
                                                    {
                                                        token.ThrowIfCancellationRequested();
                                                    }

                                                    var __cte0NextFrontierCandidate0 = e.TargetId;
                                                    var __cte0NextFrontierCandidate1 = (r.Depth + 1);
                                                    if (cte0Seen.Add(__cte0NextFrontierCandidate0))
                                                    {
                                                        if (cte0.Count + cte0NextFrontier.Count >= 10000000)
                                                        {
                                                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                                        }

                                                        cte0NextFrontier.Add(new Cte0Row0(__cte0NextFrontierCandidate0, __cte0NextFrontierCandidate1));
                                                        __op44OutputRows += 1;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        __op41Scope.AddInputRows(__op41InputRows);
                                        __op41Scope.AddOutputRows(__op41OutputRows);
                                        __op41Scope.Dispose();
                                    }

                                    __op27OutputRows += cte0NextFrontier.Count;
                                    cte0.AddRange(cte0NextFrontier);
                                    var __cte0FrontierSwap = cte0CurrentFrontier;
                                    cte0CurrentFrontier = cte0NextFrontier;
                                    cte0NextFrontier = __cte0FrontierSwap;
                                }
                            }
                        }
                        finally
                        {
                            __op27Scope.AddInputRows(__op27InputRows);
                            __op27Scope.AddOutputRows(__op27OutputRows);
                            __op27Scope.Dispose();
                        }

                        var __op45Scope = profileRecorder?.BeginOperatorValue(__op45Handle) ?? OperatorProfileValueScope.None;
                        OnPhaseChanged("compiled:cte0", QueryPhase.Select);
                        __op45Scope.Dispose();
                        var __op46Scope = profileRecorder?.BeginOperatorValue(__op46Handle) ?? OperatorProfileValueScope.None;
                        try
                        {
                            _cteRowResults.Slot0 = cte0;
                            __op46Scope.AddOutputRows(cte0.Count);
                        }
                        finally
                        {
                            __op46Scope.Dispose();
                        }
                    }
                    finally
                    {
                        var __op47Scope = profileRecorder?.BeginOperatorValue(__op47Handle) ?? OperatorProfileValueScope.None;
                        OnPhaseChanged("compiled:cte0", QueryPhase.End);
                        __op47Scope.Dispose();
                    }

                    var __op48Scope = profileRecorder?.BeginOperatorValue(__op48Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>();
                    __op48Scope.Dispose();
                    var __op49Scope = profileRecorder?.BeginOperatorValue(__op49Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op49Scope.Dispose();
                    long __op50InputRows = 0L;
                    long __op50OutputRows = 0L;
                    var __op50Scope = profileRecorder?.BeginOperatorValue(__op50Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        var __storedTable0Rows = _cteRowResults.Slot0;
                        for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                        {
                            if ((__storedTable0Index & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Cte0Row0 reachable = __storedTable0Rows[__storedTable0Index];
                            __op50InputRows++;
                            __op50OutputRows++;
                            result.Add(new ResultShape0(reachable.Id, reachable.Depth));
                            __op51OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op50Scope.AddInputRows(__op50InputRows);
                        __op50Scope.AddOutputRows(__op50OutputRows);
                        __op50Scope.Dispose();
                    }

                    var __op52Scope = profileRecorder?.BeginOperatorValue(__op52Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op52Scope.Dispose();
                    var __op53Scope = profileRecorder?.BeginOperatorValue(__op53Handle) ?? OperatorProfileValueScope.None;
                    __op53Scope.AddInputRows(result.Count);
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

                    __op53Scope.AddOutputRows(__musoqFinalShapeRows.Count);
                    __op53Scope.Dispose();
                    if (__op31InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op31Handle, __op31InputRows);
                    if (__op31OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op31Handle, __op31OutputRows);
                    if (__op39OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op39Handle, __op39OutputRows);
                    if (__op42InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op42Handle, __op42InputRows);
                    if (__op43InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op43Handle, __op43InputRows);
                    if (__op43OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op43Handle, __op43OutputRows);
                    if (__op44InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op44Handle, __op44InputRows);
                    if (__op44OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op44Handle, __op44OutputRows);
                    if (__op51OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op51Handle, __op51OutputRows);
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
            catch (Exception __profileException)when (profileRecorder != null && profileRecorder.RecordActiveOperatorException(__profileException, __profileScopeDepth))
            {
                profileRecorder.DisposeActiveOperatorScopes(__profileScopeDepth);
                throw;
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

        private readonly struct Cte0Invariant0Row0
        {
            public Cte0Invariant0Row0(int SourceId, int TargetId)
            {
                this.SourceId = SourceId;
                this.TargetId = TargetId;
            }

            public int SourceId { get; }
            public int TargetId { get; }
        }

        private readonly struct Cte0Row0
        {
            public Cte0Row0(int Id, int Depth)
            {
                this.Id = Id;
                this.Depth = Depth;
            }

            public int Id { get; }
            public int Depth { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
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
