// === Parsed Query ===
/*
with recursive reachable (Id, Depth) as (select RootId, 0 from #graph.roots() union (Id) select e.TargetId, r.Depth + 1 from #graph.edges() e inner join values {( Label: 'one-two' ), ( Label: 'two-three' )} expected on e.Label = expected.Label inner join reachable r on e.SourceId = r.Id) select Id, Depth from reachable order by Id
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
          Project [e.SourceId as e.SourceId, e.TargetId as e.TargetId, e.Label as e.Label, expected.Label as expected.Label]
            Join [Inner] [(e.Label = expected.Label)]
              SchemaScan [#graph.edges() as e]
              ValuesScan [2 rows as expected]
          Project [eexpected.e.SourceId as e.SourceId, eexpected.e.TargetId as e.TargetId, eexpected.e.Label as e.Label, eexpected.expected.Label as expected.Label, r.Id as Id, r.Depth as Depth]
            Join [Inner] [(eexpected.e.SourceId = r.Id)]
              CteRef [eexpected as eexpected]
              CteRef [reachable as r]
          Project [e.TargetId as TargetId, (r.Depth + 1) as r.Depth + 1]
            CteRef [eexpectedr as eexpectedr]
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
      Invariant [__recursive_eexpected_invariant_0; HashIndex; fields e.SourceId, e.TargetId]
        PhysicalHashJoin [Inner] [build: expected.Label] [probe: e.Label]
          PhysicalSchemaScan [#graph.edges() as e]
          PhysicalValuesScan [2 rows as expected]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [eexpected.e.TargetId as e.TargetId, r.Id as Id, r.Depth as Depth]
            PhysicalHashJoin [Inner] [build: eexpected.e.SourceId] [probe: r.Id]
              PhysicalCteRef [__recursive_eexpected_invariant_0 as eexpected]
              PhysicalCteRef [reachable as r]
          PhysicalProject [e.TargetId as TargetId, (r.Depth + 1) as r.Depth + 1]
            PhysicalCteRef [eexpectedr as eexpectedr]
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
      Label: string <- property Label
    UnknownShape [ValuesRowShape]
      Label: string <- field Label
    Generated [Cte0Invariant0Row0]
      e.SourceId: int <- field e_SourceId
      e.TargetId: int <- field e_TargetId
    TableRow [eexpected]
      e.SourceId: int <- field e_SourceId
      e.TargetId: int <- field e_TargetId
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
        CreateValuesRows [cte0Invariant0_expectedRows: expectedValuesF9227A53Row0 x 2]
        CreateHash [cte0Invariant0ExpectedHash: string -> object; capacity: 2]
        ForEach [expected in cte0Invariant0_expectedRows]
          HashAdd [cte0Invariant0ExpectedHash[expected.Label] += expected]
        ChunkedForEach [e in cte0Invariant0_eRows]
          HashProbe [cte0Invariant0ExpectedHash[e.Label] -> cte0Invariant0ExpectedHashMatches]
            ForEach [expected in cte0Invariant0ExpectedHashMatches]
              ScopedBlock
                RecursiveSnapshotGuard [__cte0SnapshotRows < 10000000; reachable]
                CreateGeneratedRow [cte0Invariant0Row <- Cte0Invariant0Row0(e.SourceId: e.SourceId, e.TargetId: e.TargetId)]
                HashAdd [cte0Invariant0Hash[cte0Invariant0Row.e.SourceId] += cte0Invariant0Row]
      RecursiveMember
        ForEach [r in cte0CurrentFrontier]
          HashProbe [cte0Invariant0Hash[r.Id] -> cte0Invariant0HashMatches]
            ForEach [eexpected in cte0Invariant0HashMatches]
              RecursiveAppend [cte0NextFrontier <- Cte0Row0(Id: eexpected.e.TargetId, Depth: (r.Depth + 1)); identity cte0Seen (Id); guard cte0.Count + cte0NextFrontier.Count < 10000000]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [From]
    ForEach [reachable in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Id: reachable.Id, Depth: reachable.Depth)]
    PhaseBoundary [Select]
    SortOwnedBuffer [result by Id ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P19_RecursiveStableScalarInvariant_Full
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable, IProfiledRunnable
    {
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Depth", typeof(int), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_e_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("SourceId", typeof(int), 0), new Column("TargetId", typeof(int), 1), new Column("Label", typeof(string), 2) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("RootId", typeof(int), 0) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }
        public TypedParameterSnapshot TypedParameters { get; set; } = TypedParameterSnapshot.Empty;

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
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
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
                    var cte0CurrentFrontier_ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot>.Open(__cte0CurrentFrontier_ko3ikoSchema, "roots", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
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
                        var cte0Invariant0_eRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge>.Open(__cte0Invariant0_eSchema, "edges", new SourceExecutionContext("e:2", sourceExecutionPlans["e:2"], token, __schemaColumns_compiled_e_1, sourceRuntimeSettingsBySourceContextId["e:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                        var cte0Invariant0_eRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge>(cte0Invariant0_eRowsSource.Chunks, __musoqProgressContext, "e:2") : cte0Invariant0_eRowsSource.Chunks;
                        expectedValuesF9227A53Row0[] cte0Invariant0_expectedRows = new expectedValuesF9227A53Row0[]
                        {
                            new expectedValuesF9227A53Row0("one-two"),
                            new expectedValuesF9227A53Row0("two-three")
                        };
                        var cte0Invariant0ExpectedHash = new Dictionary<string, HashJoinBucket<expectedValuesF9227A53Row0>>(2);
                        foreach (var expected in cte0Invariant0_expectedRows)
                        {
                            token.ThrowIfCancellationRequested();
                            string key = expected.Label;
                            if (key == null)
                                continue;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Invariant0ExpectedHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<expectedValuesF9227A53Row0>(expected);
                                }
                                else
                                {
                                    matches.Add(expected);
                                }
                            }
                        }

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
                                        string key = e.Label;
                                        if (key != null && cte0Invariant0ExpectedHash.TryGetValue(key, out var cte0Invariant0ExpectedHashMatches))
                                        {
                                            foreach (var expected in cte0Invariant0ExpectedHashMatches)
                                            {
                                                token.ThrowIfCancellationRequested();
                                                {
                                                    if (__cte0SnapshotRows >= 10000000)
                                                    {
                                                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 10000000);
                                                    }

                                                    __cte0SnapshotRows++;
                                                    Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.SourceId, e.TargetId);
                                                    int cte0Invariant0HashKey = cte0Invariant0Row.e_SourceId;
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
                                        string key = e.Label;
                                        if (key != null && cte0Invariant0ExpectedHash.TryGetValue(key, out var cte0Invariant0ExpectedHashMatches))
                                        {
                                            foreach (var expected in cte0Invariant0ExpectedHashMatches)
                                            {
                                                token.ThrowIfCancellationRequested();
                                                {
                                                    if (__cte0SnapshotRows >= 10000000)
                                                    {
                                                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 10000000);
                                                    }

                                                    __cte0SnapshotRows++;
                                                    Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.SourceId, e.TargetId);
                                                    int cte0Invariant0HashKey = cte0Invariant0Row.e_SourceId;
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
                                string key = e.Label;
                                if (key != null && cte0Invariant0ExpectedHash.TryGetValue(key, out var cte0Invariant0ExpectedHashMatches))
                                {
                                    foreach (var expected in cte0Invariant0ExpectedHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        {
                                            if (__cte0SnapshotRows >= 10000000)
                                            {
                                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 10000000);
                                            }

                                            __cte0SnapshotRows++;
                                            Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.SourceId, e.TargetId);
                                            int cte0Invariant0HashKey = cte0Invariant0Row.e_SourceId;
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
                                    foreach (var eexpected in cte0Invariant0HashMatches)
                                    {
                                        ++__cte0CancellationCounter;
                                        if ((__cte0CancellationCounter & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var __cte0NextFrontierCandidate0 = eexpected.e_TargetId;
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
                    result.Add(new ResultShape0(reachable.Id, reachable.Depth) { __musoqSequence = result.Count });
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
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
                    var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                    ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                    var __op30Handle = profileRecorder?.GetOperatorHandle("op30", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op31Handle = profileRecorder?.GetOperatorHandle("op31", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op32Handle = profileRecorder?.GetOperatorHandle("op32", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op33Handle = profileRecorder?.GetOperatorHandle("op33", "RecursiveCte") ?? OperatorProfileHandle.None;
                    var __op35Handle = profileRecorder?.GetOperatorHandle("op35", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op36Handle = profileRecorder?.GetOperatorHandle("op36", "ChunkedForEach") ?? OperatorProfileHandle.None;
                    var __op37Handle = profileRecorder?.GetOperatorHandle("op37", "RecursiveAppend") ?? OperatorProfileHandle.None;
                    var __op39Handle = profileRecorder?.GetOperatorHandle("op39", "CreateHash") ?? OperatorProfileHandle.None;
                    var __op40Handle = profileRecorder?.GetOperatorHandle("op40", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op41Handle = profileRecorder?.GetOperatorHandle("op41", "CreateValuesRows") ?? OperatorProfileHandle.None;
                    var __op42Handle = profileRecorder?.GetOperatorHandle("op42", "CreateHash") ?? OperatorProfileHandle.None;
                    var __op43Handle = profileRecorder?.GetOperatorHandle("op43", "ForEach") ?? OperatorProfileHandle.None;
                    var __op44Handle = profileRecorder?.GetOperatorHandle("op44", "HashAdd") ?? OperatorProfileHandle.None;
                    var __op45Handle = profileRecorder?.GetOperatorHandle("op45", "ChunkedForEach") ?? OperatorProfileHandle.None;
                    var __op46Handle = profileRecorder?.GetOperatorHandle("op46", "HashProbe") ?? OperatorProfileHandle.None;
                    var __op47Handle = profileRecorder?.GetOperatorHandle("op47", "ForEach") ?? OperatorProfileHandle.None;
                    var __op48Handle = profileRecorder?.GetOperatorHandle("op48", "ScopedBlock") ?? OperatorProfileHandle.None;
                    var __op49Handle = profileRecorder?.GetOperatorHandle("op49", "RecursiveSnapshotGuard") ?? OperatorProfileHandle.None;
                    var __op50Handle = profileRecorder?.GetOperatorHandle("op50", "CreateGeneratedRow") ?? OperatorProfileHandle.None;
                    var __op51Handle = profileRecorder?.GetOperatorHandle("op51", "HashAdd") ?? OperatorProfileHandle.None;
                    var __op53Handle = profileRecorder?.GetOperatorHandle("op53", "ForEach") ?? OperatorProfileHandle.None;
                    var __op54Handle = profileRecorder?.GetOperatorHandle("op54", "HashProbe") ?? OperatorProfileHandle.None;
                    var __op55Handle = profileRecorder?.GetOperatorHandle("op55", "ForEach") ?? OperatorProfileHandle.None;
                    var __op56Handle = profileRecorder?.GetOperatorHandle("op56", "RecursiveAppend") ?? OperatorProfileHandle.None;
                    var __op57Handle = profileRecorder?.GetOperatorHandle("op57", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op58Handle = profileRecorder?.GetOperatorHandle("op58", "StoreTable") ?? OperatorProfileHandle.None;
                    var __op59Handle = profileRecorder?.GetOperatorHandle("op59", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op60Handle = profileRecorder?.GetOperatorHandle("op60", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op61Handle = profileRecorder?.GetOperatorHandle("op61", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op62Handle = profileRecorder?.GetOperatorHandle("op62", "ForEach") ?? OperatorProfileHandle.None;
                    var __op63Handle = profileRecorder?.GetOperatorHandle("op63", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op64Handle = profileRecorder?.GetOperatorHandle("op64", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op65Handle = profileRecorder?.GetOperatorHandle("op65", "SortOwnedBuffer") ?? OperatorProfileHandle.None;
                    long __op37InputRows = 0L;
                    long __op37OutputRows = 0L;
                    long __op44OutputRows = 0L;
                    long __op46InputRows = 0L;
                    long __op47InputRows = 0L;
                    long __op47OutputRows = 0L;
                    long __op51OutputRows = 0L;
                    long __op54InputRows = 0L;
                    long __op55InputRows = 0L;
                    long __op55OutputRows = 0L;
                    long __op56InputRows = 0L;
                    long __op56OutputRows = 0L;
                    long __op63OutputRows = 0L;
                    var __op30Scope = profileRecorder?.BeginOperatorValue(__op30Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op30Scope.Dispose();
                    var __op31Scope = profileRecorder?.BeginOperatorValue(__op31Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                    __op31Scope.Dispose();
                    try
                    {
                        var __op32Scope = profileRecorder?.BeginOperatorValue(__op32Handle) ?? OperatorProfileValueScope.None;
                        OnPhaseChanged("compiled:cte0", QueryPhase.From);
                        __op32Scope.Dispose();
                        long __op33InputRows = 0L;
                        long __op33OutputRows = 0L;
                        var __op33Scope = profileRecorder?.BeginOperatorValue(__op33Handle) ?? OperatorProfileValueScope.None;
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
                            var __op35Scope = profileRecorder?.BeginOperatorValue(__op35Handle) ?? OperatorProfileValueScope.None;
                            var __cte0CurrentFrontier_ko3ikoSchema = provider.GetSchema("#graph");
                            var cte0CurrentFrontier_ko3ikoRowsProfile = profileRecorder?.CreateSourceRecorder("ko3iko");
                            var cte0CurrentFrontier_ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot>.Open(__cte0CurrentFrontier_ko3ikoSchema, "roots", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress, cte0CurrentFrontier_ko3ikoRowsProfile == null ? SourceDiagnostics.None : cte0CurrentFrontier_ko3ikoRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                            var cte0CurrentFrontier_ko3ikoRows = cte0CurrentFrontier_ko3ikoRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot>(cte0CurrentFrontier_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : cte0CurrentFrontier_ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot>(cte0CurrentFrontier_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : cte0CurrentFrontier_ko3ikoRowsSource.Chunks, cte0CurrentFrontier_ko3ikoRowsProfile);
                            __op35Scope.Dispose();
                            long __op36InputRows = 0L;
                            long __op36OutputRows = 0L;
                            var __op36Scope = profileRecorder?.BeginOperatorValue(__op36Handle) ?? OperatorProfileValueScope.None;
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
                                                __op36InputRows++;
                                                __op36OutputRows++;
                                                __op37InputRows += 1;
                                                var __cte0CurrentFrontierCandidate0 = ko3iko.RootId;
                                                var __cte0CurrentFrontierCandidate1 = 0;
                                                if (cte0Seen.Add(__cte0CurrentFrontierCandidate0))
                                                {
                                                    if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                                    {
                                                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                                    }

                                                    cte0CurrentFrontier.Add(new Cte0Row0(__cte0CurrentFrontierCandidate0, __cte0CurrentFrontierCandidate1));
                                                    __op37OutputRows += 1;
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
                                                __op36InputRows++;
                                                __op36OutputRows++;
                                                __op37InputRows += 1;
                                                var __cte0CurrentFrontierCandidate0 = ko3iko.RootId;
                                                var __cte0CurrentFrontierCandidate1 = 0;
                                                if (cte0Seen.Add(__cte0CurrentFrontierCandidate0))
                                                {
                                                    if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                                    {
                                                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                                    }

                                                    cte0CurrentFrontier.Add(new Cte0Row0(__cte0CurrentFrontierCandidate0, __cte0CurrentFrontierCandidate1));
                                                    __op37OutputRows += 1;
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
                                        __op36InputRows++;
                                        __op36OutputRows++;
                                        __op37InputRows += 1;
                                        var __cte0CurrentFrontierCandidate0 = ko3iko.RootId;
                                        var __cte0CurrentFrontierCandidate1 = 0;
                                        if (cte0Seen.Add(__cte0CurrentFrontierCandidate0))
                                        {
                                            if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                            {
                                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                            }

                                            cte0CurrentFrontier.Add(new Cte0Row0(__cte0CurrentFrontierCandidate0, __cte0CurrentFrontierCandidate1));
                                            __op37OutputRows += 1;
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                __op36Scope.AddInputRows(__op36InputRows);
                                __op36Scope.AddOutputRows(__op36OutputRows);
                                __op36Scope.Dispose();
                            }

                            cte0.AddRange(cte0CurrentFrontier);
                            if (cte0CurrentFrontier.Count > 0)
                            {
                                var __op39Scope = profileRecorder?.BeginOperatorValue(__op39Handle) ?? OperatorProfileValueScope.None;
                                var cte0Invariant0Hash = new Dictionary<int, HashJoinBucket<Cte0Invariant0Row0>>();
                                __op39Scope.Dispose();
                                var __op40Scope = profileRecorder?.BeginOperatorValue(__op40Handle) ?? OperatorProfileValueScope.None;
                                var __cte0Invariant0_eSchema = provider.GetSchema("#graph");
                                var cte0Invariant0_eRowsProfile = profileRecorder?.CreateSourceRecorder("e");
                                var cte0Invariant0_eRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge>.Open(__cte0Invariant0_eSchema, "edges", new SourceExecutionContext("e:2", sourceExecutionPlans["e:2"], token, __schemaColumns_compiled_e_1, sourceRuntimeSettingsBySourceContextId["e:2"], logger, OnDataSourceProgress, cte0Invariant0_eRowsProfile == null ? SourceDiagnostics.None : cte0Invariant0_eRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                                var cte0Invariant0_eRows = cte0Invariant0_eRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge>(cte0Invariant0_eRowsSource.Chunks, __musoqProgressContext, "e:2") : cte0Invariant0_eRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge>(cte0Invariant0_eRowsSource.Chunks, __musoqProgressContext, "e:2") : cte0Invariant0_eRowsSource.Chunks, cte0Invariant0_eRowsProfile);
                                __op40Scope.Dispose();
                                var __op41Scope = profileRecorder?.BeginOperatorValue(__op41Handle) ?? OperatorProfileValueScope.None;
                                expectedValuesF9227A53Row0[] cte0Invariant0_expectedRows = new expectedValuesF9227A53Row0[]
                                {
                                    new expectedValuesF9227A53Row0("one-two"),
                                    new expectedValuesF9227A53Row0("two-three")
                                };
                                __op41Scope.AddOutputRows(cte0Invariant0_expectedRows.Length);
                                __op41Scope.Dispose();
                                var __op42Scope = profileRecorder?.BeginOperatorValue(__op42Handle) ?? OperatorProfileValueScope.None;
                                var cte0Invariant0ExpectedHash = new Dictionary<string, HashJoinBucket<expectedValuesF9227A53Row0>>(2);
                                __op42Scope.Dispose();
                                long __op43InputRows = 0L;
                                long __op43OutputRows = 0L;
                                var __op43Scope = profileRecorder?.BeginOperatorValue(__op43Handle) ?? OperatorProfileValueScope.None;
                                try
                                {
                                    foreach (var expected in cte0Invariant0_expectedRows)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        __op43InputRows++;
                                        __op43OutputRows++;
                                        string key = expected.Label;
                                        if (key == null)
                                            continue;
                                        {
                                            ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Invariant0ExpectedHash, key, out var matchesExists);
                                            if (!matchesExists)
                                            {
                                                matches = new HashJoinBucket<expectedValuesF9227A53Row0>(expected);
                                            }
                                            else
                                            {
                                                matches.Add(expected);
                                            }
                                        }

                                        __op44OutputRows += 1;
                                    }
                                }
                                finally
                                {
                                    __op43Scope.AddInputRows(__op43InputRows);
                                    __op43Scope.AddOutputRows(__op43OutputRows);
                                    __op43Scope.Dispose();
                                }

                                long __op45InputRows = 0L;
                                long __op45OutputRows = 0L;
                                var __op45Scope = profileRecorder?.BeginOperatorValue(__op45Handle) ?? OperatorProfileValueScope.None;
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
                                                    __op45InputRows++;
                                                    __op45OutputRows++;
                                                    __op46InputRows += 1;
                                                    string key = e.Label;
                                                    if (key != null && cte0Invariant0ExpectedHash.TryGetValue(key, out var cte0Invariant0ExpectedHashMatches))
                                                    {
                                                        foreach (var expected in cte0Invariant0ExpectedHashMatches)
                                                        {
                                                            token.ThrowIfCancellationRequested();
                                                            __op47InputRows++;
                                                            __op47OutputRows++;
                                                            var __op48Scope = profileRecorder?.BeginOperatorValue(__op48Handle) ?? OperatorProfileValueScope.None;
                                                            {
                                                                var __op49Scope = profileRecorder?.BeginOperatorValue(__op49Handle) ?? OperatorProfileValueScope.None;
                                                                if (__cte0SnapshotRows >= 10000000)
                                                                {
                                                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 10000000);
                                                                }

                                                                __cte0SnapshotRows++;
                                                                __op49Scope.Dispose();
                                                                var __op50Scope = profileRecorder?.BeginOperatorValue(__op50Handle) ?? OperatorProfileValueScope.None;
                                                                Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.SourceId, e.TargetId);
                                                                __op50Scope.Dispose();
                                                                int cte0Invariant0HashKey = cte0Invariant0Row.e_SourceId;
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

                                                                __op51OutputRows += 1;
                                                            }

                                                            __op48Scope.Dispose();
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
                                                    __op45InputRows++;
                                                    __op45OutputRows++;
                                                    __op46InputRows += 1;
                                                    string key = e.Label;
                                                    if (key != null && cte0Invariant0ExpectedHash.TryGetValue(key, out var cte0Invariant0ExpectedHashMatches))
                                                    {
                                                        foreach (var expected in cte0Invariant0ExpectedHashMatches)
                                                        {
                                                            token.ThrowIfCancellationRequested();
                                                            __op47InputRows++;
                                                            __op47OutputRows++;
                                                            var __op48Scope = profileRecorder?.BeginOperatorValue(__op48Handle) ?? OperatorProfileValueScope.None;
                                                            {
                                                                var __op49Scope = profileRecorder?.BeginOperatorValue(__op49Handle) ?? OperatorProfileValueScope.None;
                                                                if (__cte0SnapshotRows >= 10000000)
                                                                {
                                                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 10000000);
                                                                }

                                                                __cte0SnapshotRows++;
                                                                __op49Scope.Dispose();
                                                                var __op50Scope = profileRecorder?.BeginOperatorValue(__op50Handle) ?? OperatorProfileValueScope.None;
                                                                Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.SourceId, e.TargetId);
                                                                __op50Scope.Dispose();
                                                                int cte0Invariant0HashKey = cte0Invariant0Row.e_SourceId;
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

                                                                __op51OutputRows += 1;
                                                            }

                                                            __op48Scope.Dispose();
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
                                            __op45InputRows++;
                                            __op45OutputRows++;
                                            __op46InputRows += 1;
                                            string key = e.Label;
                                            if (key != null && cte0Invariant0ExpectedHash.TryGetValue(key, out var cte0Invariant0ExpectedHashMatches))
                                            {
                                                foreach (var expected in cte0Invariant0ExpectedHashMatches)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                    __op47InputRows++;
                                                    __op47OutputRows++;
                                                    var __op48Scope = profileRecorder?.BeginOperatorValue(__op48Handle) ?? OperatorProfileValueScope.None;
                                                    {
                                                        var __op49Scope = profileRecorder?.BeginOperatorValue(__op49Handle) ?? OperatorProfileValueScope.None;
                                                        if (__cte0SnapshotRows >= 10000000)
                                                        {
                                                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 10000000);
                                                        }

                                                        __cte0SnapshotRows++;
                                                        __op49Scope.Dispose();
                                                        var __op50Scope = profileRecorder?.BeginOperatorValue(__op50Handle) ?? OperatorProfileValueScope.None;
                                                        Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.SourceId, e.TargetId);
                                                        __op50Scope.Dispose();
                                                        int cte0Invariant0HashKey = cte0Invariant0Row.e_SourceId;
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

                                                        __op51OutputRows += 1;
                                                    }

                                                    __op48Scope.Dispose();
                                                }
                                            }
                                        }
                                    }
                                }
                                finally
                                {
                                    __op45Scope.AddInputRows(__op45InputRows);
                                    __op45Scope.AddOutputRows(__op45OutputRows);
                                    __op45Scope.Dispose();
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
                                    __op33InputRows += cte0CurrentFrontier.Count;
                                    long __op53InputRows = 0L;
                                    long __op53OutputRows = 0L;
                                    var __op53Scope = profileRecorder?.BeginOperatorValue(__op53Handle) ?? OperatorProfileValueScope.None;
                                    try
                                    {
                                        for (int cte0CurrentFrontierIndex = 0; cte0CurrentFrontierIndex < cte0CurrentFrontier.Count; ++cte0CurrentFrontierIndex)
                                        {
                                            if (cte0CurrentFrontierIndex != 0 && (cte0CurrentFrontierIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            Cte0Row0 r = (Cte0Row0)cte0CurrentFrontier[cte0CurrentFrontierIndex];
                                            __op53InputRows++;
                                            __op53OutputRows++;
                                            __op54InputRows += 1;
                                            int key = r.Id;
                                            if (cte0Invariant0Hash.TryGetValue(key, out var cte0Invariant0HashMatches))
                                            {
                                                foreach (var eexpected in cte0Invariant0HashMatches)
                                                {
                                                    __op55InputRows++;
                                                    __op55OutputRows++;
                                                    __op56InputRows += 1;
                                                    ++__cte0CancellationCounter;
                                                    if ((__cte0CancellationCounter & 1023) == 0)
                                                    {
                                                        token.ThrowIfCancellationRequested();
                                                    }

                                                    var __cte0NextFrontierCandidate0 = eexpected.e_TargetId;
                                                    var __cte0NextFrontierCandidate1 = (r.Depth + 1);
                                                    if (cte0Seen.Add(__cte0NextFrontierCandidate0))
                                                    {
                                                        if (cte0.Count + cte0NextFrontier.Count >= 10000000)
                                                        {
                                                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                                        }

                                                        cte0NextFrontier.Add(new Cte0Row0(__cte0NextFrontierCandidate0, __cte0NextFrontierCandidate1));
                                                        __op56OutputRows += 1;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        __op53Scope.AddInputRows(__op53InputRows);
                                        __op53Scope.AddOutputRows(__op53OutputRows);
                                        __op53Scope.Dispose();
                                    }

                                    __op33OutputRows += cte0NextFrontier.Count;
                                    cte0.AddRange(cte0NextFrontier);
                                    var __cte0FrontierSwap = cte0CurrentFrontier;
                                    cte0CurrentFrontier = cte0NextFrontier;
                                    cte0NextFrontier = __cte0FrontierSwap;
                                }
                            }
                        }
                        finally
                        {
                            __op33Scope.AddInputRows(__op33InputRows);
                            __op33Scope.AddOutputRows(__op33OutputRows);
                            __op33Scope.Dispose();
                        }

                        var __op57Scope = profileRecorder?.BeginOperatorValue(__op57Handle) ?? OperatorProfileValueScope.None;
                        OnPhaseChanged("compiled:cte0", QueryPhase.Select);
                        __op57Scope.Dispose();
                        var __op58Scope = profileRecorder?.BeginOperatorValue(__op58Handle) ?? OperatorProfileValueScope.None;
                        try
                        {
                            _cteRowResults.Slot0 = cte0;
                            __op58Scope.AddOutputRows(cte0.Count);
                        }
                        finally
                        {
                            __op58Scope.Dispose();
                        }
                    }
                    finally
                    {
                        var __op59Scope = profileRecorder?.BeginOperatorValue(__op59Handle) ?? OperatorProfileValueScope.None;
                        OnPhaseChanged("compiled:cte0", QueryPhase.End);
                        __op59Scope.Dispose();
                    }

                    var __op60Scope = profileRecorder?.BeginOperatorValue(__op60Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>();
                    __op60Scope.Dispose();
                    var __op61Scope = profileRecorder?.BeginOperatorValue(__op61Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op61Scope.Dispose();
                    long __op62InputRows = 0L;
                    long __op62OutputRows = 0L;
                    var __op62Scope = profileRecorder?.BeginOperatorValue(__op62Handle) ?? OperatorProfileValueScope.None;
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
                            __op62InputRows++;
                            __op62OutputRows++;
                            result.Add(new ResultShape0(reachable.Id, reachable.Depth) { __musoqSequence = result.Count });
                            __op63OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op62Scope.AddInputRows(__op62InputRows);
                        __op62Scope.AddOutputRows(__op62OutputRows);
                        __op62Scope.Dispose();
                    }

                    var __op64Scope = profileRecorder?.BeginOperatorValue(__op64Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op64Scope.Dispose();
                    var __op65Scope = profileRecorder?.BeginOperatorValue(__op65Handle) ?? OperatorProfileValueScope.None;
                    __op65Scope.AddInputRows(result.Count);
                    result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                    {
                        var comparison = left.Id.CompareTo(right.Id);
                        if (comparison != 0)
                            return comparison;
                        return left.__musoqSequence.CompareTo(right.__musoqSequence);
                    }));
                    __op65Scope.AddOutputRows(result.Count);
                    __op65Scope.Dispose();
                    if (__op37InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op37Handle, __op37InputRows);
                    if (__op37OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op37Handle, __op37OutputRows);
                    if (__op44OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op44Handle, __op44OutputRows);
                    if (__op46InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op46Handle, __op46InputRows);
                    if (__op47InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op47Handle, __op47InputRows);
                    if (__op47OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op47Handle, __op47OutputRows);
                    if (__op51OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op51Handle, __op51OutputRows);
                    if (__op54InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op54Handle, __op54InputRows);
                    if (__op55InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op55Handle, __op55InputRows);
                    if (__op55OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op55Handle, __op55OutputRows);
                    if (__op56InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op56Handle, __op56InputRows);
                    if (__op56OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op56Handle, __op56OutputRows);
                    if (__op63OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op63Handle, __op63OutputRows);
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
            public Cte0Invariant0Row0(int e_SourceId, int e_TargetId)
            {
                this.e_SourceId = e_SourceId;
                this.e_TargetId = e_TargetId;
            }

            public int e_SourceId { get; }
            public int e_TargetId { get; }
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

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Depth = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Depth),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Depth" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Depth, ref visitor);
            }
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
            public int __musoqSequence { get; init; }
        }

        private sealed class expectedValuesF9227A53Row0 : Row
        {
            public expectedValuesF9227A53Row0(string __value0)
            {
                Label = __value0;
            }

            public override int Count => 1;
            public string Label { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Label = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Label),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Label" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Label, ref visitor);
            }
        }
    }
}
