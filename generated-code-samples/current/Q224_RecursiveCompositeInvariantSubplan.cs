// === Parsed Query ===
/*
with recursive reachable (Id, Depth) as (select RootId, 0 from #graph.roots() union (Id) select e.TargetId, r.Depth + 1 from #graph.edges() e inner join values {{ Label: 'one-two' }, { Label: 'two-three' }} expected on e.Label = expected.Label inner join reachable r on e.SourceId = r.Id) select Id, Depth from reachable order by Id
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
          Project [e.TargetId as e.TargetId, (r.Depth + 1) as r.Depth + 1]
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
      Invariant [__recursive_eexpected_invariant_0; HashIndex; fields e.SourceId, e.TargetId, e.Label, expected.Label]
        PhysicalHashJoin [Inner] [build: expected.Label] [probe: e.Label]
          PhysicalSchemaScan [#graph.edges() as e]
          PhysicalValuesScan [2 rows as expected]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [eexpected.e.SourceId as e.SourceId, eexpected.e.TargetId as e.TargetId, eexpected.e.Label as e.Label, eexpected.expected.Label as expected.Label, r.Id as Id, r.Depth as Depth]
            PhysicalHashJoin [Inner] [build: eexpected.e.SourceId] [probe: r.Id]
              PhysicalCteRef [__recursive_eexpected_invariant_0 as eexpected]
              PhysicalCteRef [reachable as r]
          PhysicalProject [e.TargetId as e.TargetId, (r.Depth + 1) as r.Depth + 1]
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
      e.Label: string <- field e_Label
      expected.Label: string <- field expected_Label
    TableRow [eexpected]
      e.SourceId: int <- field e_SourceId
      e.TargetId: int <- field e_TargetId
      e.Label: string <- field e_Label
      expected.Label: string <- field expected_Label
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
                CreateGeneratedRow [cte0Invariant0Row <- Cte0Invariant0Row0(e.SourceId: e.SourceId, e.TargetId: e.TargetId, e.Label: e.Label, expected.Label: expected.Label)]
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
    SortShapeRows [result -> resultSorted by Id ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q224_RecursiveCompositeInvariantSubplan
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Depth", typeof(int), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_e_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("SourceId", typeof(int), 0), new Column("TargetId", typeof(int), 1), new Column("Label", typeof(string), 3) });
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
                                                    Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.SourceId, e.TargetId, e.Label, expected.Label);
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
                                                    Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.SourceId, e.TargetId, e.Label, expected.Label);
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
                                            Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.SourceId, e.TargetId, e.Label, expected.Label);
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
            public Cte0Invariant0Row0(int e_SourceId, int e_TargetId, string e_Label, string expected_Label)
            {
                this.e_SourceId = e_SourceId;
                this.e_TargetId = e_TargetId;
                this.e_Label = e_Label;
                this.expected_Label = expected_Label;
            }

            public int e_SourceId { get; }
            public int e_TargetId { get; }
            public string e_Label { get; }
            public string expected_Label { get; }
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

        private sealed class expectedValuesF9227A53Row0 : Row
        {
            public expectedValuesF9227A53Row0(string __value0)
            {
                Label = __value0;
            }

            public override int Count => 1;
            public string Label { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Label = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Label" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Label,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Label" => (object)Label,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
