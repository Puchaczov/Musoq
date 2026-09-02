// === Parsed Query ===
/*
with recursive reachable (Id, Depth) as (select RootId, 0 from #graph.roots() union (Id) select case when e.TargetId is null then r.Id else e.TargetId end, r.Depth + 1 from reachable r outer apply #graph.edges() e where e.SourceId is null or e.SourceId = r.Id) select Id, Depth from reachable order by Id
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
            Apply [Outer]
              CteRef [reachable as r]
              SchemaScan [#graph.edges() as e]
          Project [CASE WHEN e.TargetId IS NULL THEN r.Id ELSE e.TargetId END as case when e.TargetId is null then r.Id else e.TargetId end, (r.Depth + 1) as r.Depth + 1]
            Filter [(e.SourceId IS NULL OR (e.SourceId = r.Id))]
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
      Invariant [__recursive_reachable_invariant_0; Snapshot; fields SourceId, TargetId]
        PhysicalSchemaScan [#graph.edges() as e]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [r.Id as r.Id, r.Depth as r.Depth, e.SourceId as e.SourceId, e.TargetId as e.TargetId]
            PhysicalNestedLoopApply [Outer]
              PhysicalCteRef [reachable as r]
              PhysicalCteRef [__recursive_reachable_invariant_0 as e]
          PhysicalProject [CASE WHEN e.TargetId IS NULL THEN r.Id ELSE e.TargetId END as case when e.TargetId is null then r.Id else e.TargetId end, (r.Depth + 1) as r.Depth + 1]
            PhysicalFilter [(e.SourceId IS NULL OR (e.SourceId = r.Id))]
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
    RecursiveCte [reachable; result cte0; frontiers cte0CurrentFrontier, cte0NextFrontier; identity Keyed via cte0Seen (Id); max iterations 100; max rows 100; max snapshot rows 1]
      Anchor
        SourceScan [ko3iko: RecursiveGraphRoot] -> cte0CurrentFrontier_ko3ikoRows
        ChunkedForEach [ko3iko in cte0CurrentFrontier_ko3ikoRows]
          RecursiveAppend [cte0CurrentFrontier <- Cte0Row0(Id: ko3iko.RootId, Depth: 0); identity cte0Seen (Id); guard cte0.Count + cte0CurrentFrontier.Count < 100]
      InvariantSetup
        CreateTable [cte0Invariant0: Cte0Invariant0Row0]
        SourceScan [e: RecursiveGraphEdge] -> cte0Invariant0_eRows
        ChunkedForEach [e in cte0Invariant0_eRows]
          ScopedBlock
            RecursiveSnapshotGuard [__cte0SnapshotRows < 1; reachable]
            AppendRow [cte0Invariant0 <- Cte0Invariant0Row0(SourceId: e.SourceId, TargetId: e.TargetId)]
      RecursiveMember
        ForEach [r in cte0CurrentFrontier]
          Let [eHasMatch: bool = FALSE]
          ForEach [e in cte0Invariant0]
            Assign [eHasMatch = TRUE]
            Let [sourceId: int? = e.SourceId]
            If [(sourceId IS NULL OR (sourceId = r.Id))]
              Let [targetId: int? = e.TargetId]
              RecursiveAppend [cte0NextFrontier <- Cte0Row0(Id: CASE WHEN targetId IS NULL THEN r.Id ELSE targetId END::int32, Depth: (r.Depth + 1)); identity cte0Seen (Id); guard cte0.Count + cte0NextFrontier.Count < 100]
          If [NOT eHasMatch]
            If [TRUE]
              RecursiveAppend [cte0NextFrontier <- Cte0Row0(Id: CASE WHEN TRUE THEN r.Id ELSE NULL END::int32, Depth: (r.Depth + 1)); identity cte0Seen (Id); guard cte0.Count + cte0NextFrontier.Count < 100]
    PhaseBoundary [Where:cte0]
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
namespace GeneratedSample_Q226_RecursiveSnapshotLimitCodeShape
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
        private static readonly Column[] __columns_compiled_cte0Invariant0_1 = new Column[]
        {
            new Column("SourceId", typeof(int), 0),
            new Column("TargetId", typeof(int), 1)
        };
        private static readonly Column[] __columns_compiled_result_3 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Depth", typeof(int), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_e_2 = Array.AsReadOnly(new ISchemaColumn[] { new Column("SourceId", typeof(int), 0), new Column("TargetId", typeof(int), 1) });
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
                                        if (cte0.Count + cte0CurrentFrontier.Count >= 100)
                                        {
                                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 100);
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
                                        if (cte0.Count + cte0CurrentFrontier.Count >= 100)
                                        {
                                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 100);
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
                                if (cte0.Count + cte0CurrentFrontier.Count >= 100)
                                {
                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 100);
                                }

                                cte0CurrentFrontier.Add(new Cte0Row0(__cte0CurrentFrontierCandidate0, __cte0CurrentFrontierCandidate1));
                            }
                        }
                    }

                    cte0.AddRange(cte0CurrentFrontier);
                    if (cte0CurrentFrontier.Count > 0)
                    {
                        var cte0Invariant0 = new List<Cte0Invariant0Row0>();
                        var __cte0Invariant0_eSchema = provider.GetSchema("#graph");
                        var cte0Invariant0_eRowsSource = __cte0Invariant0_eSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge>("edges", new SourceExecutionContext("e:2", sourceExecutionPlans["e:2"], token, __schemaColumns_compiled_e_2, sourceRuntimeSettingsBySourceContextId["e:2"], logger, OnDataSourceProgress), Array.Empty<object>());
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
                                            if (__cte0SnapshotRows >= 1)
                                            {
                                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 1);
                                            }

                                            __cte0SnapshotRows++;
                                            cte0Invariant0.Add(new Cte0Invariant0Row0(e.SourceId, e.TargetId));
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
                                            if (__cte0SnapshotRows >= 1)
                                            {
                                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 1);
                                            }

                                            __cte0SnapshotRows++;
                                            cte0Invariant0.Add(new Cte0Invariant0Row0(e.SourceId, e.TargetId));
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
                                    if (__cte0SnapshotRows >= 1)
                                    {
                                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 1);
                                    }

                                    __cte0SnapshotRows++;
                                    cte0Invariant0.Add(new Cte0Invariant0Row0(e.SourceId, e.TargetId));
                                }
                            }
                        }

                        while (cte0CurrentFrontier.Count > 0)
                        {
                            if ((__cte0Iteration & 63) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            if (__cte0Iteration >= 100)
                            {
                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 100);
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
                                bool eHasMatch = false;
                                foreach (var e in cte0Invariant0)
                                {
                                    eHasMatch = true;
                                    int? sourceId = e.SourceId;
                                    if ((((sourceId == null) | Operators.SqlCompare<int?, int>(sourceId, r.Id, (int? __sqlLeft, int __sqlRight) => (__sqlLeft == __sqlRight)))) == true)
                                    {
                                        int? targetId = e.TargetId;
                                        ++__cte0CancellationCounter;
                                        if ((__cte0CancellationCounter & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var __cte0NextFrontierCandidate0 = (int)((targetId == null) ? (int?)r.Id : (int?)targetId);
                                        var __cte0NextFrontierCandidate1 = (r.Depth + 1);
                                        if (cte0Seen.Add(__cte0NextFrontierCandidate0))
                                        {
                                            if (cte0.Count + cte0NextFrontier.Count >= 100)
                                            {
                                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 100);
                                            }

                                            cte0NextFrontier.Add(new Cte0Row0(__cte0NextFrontierCandidate0, __cte0NextFrontierCandidate1));
                                        }
                                    }
                                }

                                if ((!eHasMatch))
                                {
                                    if (true)
                                    {
                                        ++__cte0CancellationCounter;
                                        if ((__cte0CancellationCounter & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var __cte0NextFrontierCandidate0 = (int)(true ? (int?)r.Id : (int?)null);
                                        var __cte0NextFrontierCandidate1 = (r.Depth + 1);
                                        if (cte0Seen.Add(__cte0NextFrontierCandidate0))
                                        {
                                            if (cte0.Count + cte0NextFrontier.Count >= 100)
                                            {
                                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 100);
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

                    OnPhaseChanged("compiled:cte0", QueryPhase.Where);
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
