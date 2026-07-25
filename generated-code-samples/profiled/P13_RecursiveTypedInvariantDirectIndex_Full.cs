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
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [reachable in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Id: reachable.Id, Depth: reachable.Depth)]
    SortShapeRows [result -> resultSorted by Id ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P13_RecursiveTypedInvariantDirectIndex_Full
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable, IProfiledRunnable
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
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken, null), token);
        }

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            ArgumentNullException.ThrowIfNull(profileRecorder);
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken, profileRecorder), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.Id, __musoqShapeRow.Depth);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            var __profileScopeDepth = profileRecorder?.GetCurrentOperatorScopeDepth() ?? 0;
            try
            {
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.Select);
                try
                {
                    var _cteRowResults = new CteRowResults();
                    var __musoqExecutionState = ExecutionState.Capture(Parameters);
                    ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                    var __musoqFinalShapeRows = new List<ResultShape0>();
                    var __op34Handle = profileRecorder?.GetOperatorHandle("op34", "RecursiveCte") ?? OperatorProfileHandle.None;
                    var __op36Handle = profileRecorder?.GetOperatorHandle("op36", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op37Handle = profileRecorder?.GetOperatorHandle("op37", "ChunkedForEach") ?? OperatorProfileHandle.None;
                    var __op38Handle = profileRecorder?.GetOperatorHandle("op38", "RecursiveAppend") ?? OperatorProfileHandle.None;
                    var __op40Handle = profileRecorder?.GetOperatorHandle("op40", "CreateHash") ?? OperatorProfileHandle.None;
                    var __op41Handle = profileRecorder?.GetOperatorHandle("op41", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op42Handle = profileRecorder?.GetOperatorHandle("op42", "CreateValuesRows") ?? OperatorProfileHandle.None;
                    var __op43Handle = profileRecorder?.GetOperatorHandle("op43", "CreateHash") ?? OperatorProfileHandle.None;
                    var __op44Handle = profileRecorder?.GetOperatorHandle("op44", "ForEach") ?? OperatorProfileHandle.None;
                    var __op45Handle = profileRecorder?.GetOperatorHandle("op45", "HashAdd") ?? OperatorProfileHandle.None;
                    var __op46Handle = profileRecorder?.GetOperatorHandle("op46", "ChunkedForEach") ?? OperatorProfileHandle.None;
                    var __op47Handle = profileRecorder?.GetOperatorHandle("op47", "HashProbe") ?? OperatorProfileHandle.None;
                    var __op48Handle = profileRecorder?.GetOperatorHandle("op48", "ForEach") ?? OperatorProfileHandle.None;
                    var __op49Handle = profileRecorder?.GetOperatorHandle("op49", "ScopedBlock") ?? OperatorProfileHandle.None;
                    var __op50Handle = profileRecorder?.GetOperatorHandle("op50", "RecursiveSnapshotGuard") ?? OperatorProfileHandle.None;
                    var __op51Handle = profileRecorder?.GetOperatorHandle("op51", "CreateGeneratedRow") ?? OperatorProfileHandle.None;
                    var __op52Handle = profileRecorder?.GetOperatorHandle("op52", "HashAdd") ?? OperatorProfileHandle.None;
                    var __op54Handle = profileRecorder?.GetOperatorHandle("op54", "ForEach") ?? OperatorProfileHandle.None;
                    var __op55Handle = profileRecorder?.GetOperatorHandle("op55", "HashProbe") ?? OperatorProfileHandle.None;
                    var __op56Handle = profileRecorder?.GetOperatorHandle("op56", "ForEach") ?? OperatorProfileHandle.None;
                    var __op57Handle = profileRecorder?.GetOperatorHandle("op57", "RecursiveAppend") ?? OperatorProfileHandle.None;
                    var __op58Handle = profileRecorder?.GetOperatorHandle("op58", "StoreTable") ?? OperatorProfileHandle.None;
                    var __op59Handle = profileRecorder?.GetOperatorHandle("op59", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op60Handle = profileRecorder?.GetOperatorHandle("op60", "ForEach") ?? OperatorProfileHandle.None;
                    var __op61Handle = profileRecorder?.GetOperatorHandle("op61", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op62Handle = profileRecorder?.GetOperatorHandle("op62", "SortShapeRows") ?? OperatorProfileHandle.None;
                    long __op38InputRows = 0L;
                    long __op38OutputRows = 0L;
                    long __op45OutputRows = 0L;
                    long __op47InputRows = 0L;
                    long __op48InputRows = 0L;
                    long __op48OutputRows = 0L;
                    long __op52OutputRows = 0L;
                    long __op55InputRows = 0L;
                    long __op56InputRows = 0L;
                    long __op56OutputRows = 0L;
                    long __op57InputRows = 0L;
                    long __op57OutputRows = 0L;
                    long __op61OutputRows = 0L;
                    long __op34InputRows = 0L;
                    long __op34OutputRows = 0L;
                    var __op34Scope = profileRecorder?.BeginOperatorValue(__op34Handle) ?? OperatorProfileValueScope.None;
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
                        var __op36Scope = profileRecorder?.BeginOperatorValue(__op36Handle) ?? OperatorProfileValueScope.None;
                        var __cte0CurrentFrontier_ko3ikoSchema = provider.GetSchema("#graph");
                        var cte0CurrentFrontier_ko3ikoRowsProfile = profileRecorder?.CreateSourceRecorder("ko3iko");
                        var cte0CurrentFrontier_ko3ikoRowsSource = __cte0CurrentFrontier_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot>("roots", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress, cte0CurrentFrontier_ko3ikoRowsProfile == null ? SourceDiagnostics.None : cte0CurrentFrontier_ko3ikoRowsProfile.CreateDiagnostics()), Array.Empty<object>());
                        var cte0CurrentFrontier_ko3ikoRows = cte0CurrentFrontier_ko3ikoRowsProfile == null ? cte0CurrentFrontier_ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphRoot>.Create(cte0CurrentFrontier_ko3ikoRowsSource.Chunks, cte0CurrentFrontier_ko3ikoRowsProfile);
                        __op36Scope.Dispose();
                        long __op37InputRows = 0L;
                        long __op37OutputRows = 0L;
                        var __op37Scope = profileRecorder?.BeginOperatorValue(__op37Handle) ?? OperatorProfileValueScope.None;
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
                                            __op37InputRows++;
                                            __op37OutputRows++;
                                            __op38InputRows += 1;
                                            var __cte0CurrentFrontierCandidate0 = ko3iko.RootId;
                                            var __cte0CurrentFrontierCandidate1 = 0;
                                            if (cte0Seen.Add(__cte0CurrentFrontierCandidate0))
                                            {
                                                if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                                {
                                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                                }

                                                cte0CurrentFrontier.Add(new Cte0Row0(__cte0CurrentFrontierCandidate0, __cte0CurrentFrontierCandidate1));
                                                __op38OutputRows += 1;
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
                                            __op37InputRows++;
                                            __op37OutputRows++;
                                            __op38InputRows += 1;
                                            var __cte0CurrentFrontierCandidate0 = ko3iko.RootId;
                                            var __cte0CurrentFrontierCandidate1 = 0;
                                            if (cte0Seen.Add(__cte0CurrentFrontierCandidate0))
                                            {
                                                if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                                {
                                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                                }

                                                cte0CurrentFrontier.Add(new Cte0Row0(__cte0CurrentFrontierCandidate0, __cte0CurrentFrontierCandidate1));
                                                __op38OutputRows += 1;
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
                                    __op37InputRows++;
                                    __op37OutputRows++;
                                    __op38InputRows += 1;
                                    var __cte0CurrentFrontierCandidate0 = ko3iko.RootId;
                                    var __cte0CurrentFrontierCandidate1 = 0;
                                    if (cte0Seen.Add(__cte0CurrentFrontierCandidate0))
                                    {
                                        if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                        {
                                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                        }

                                        cte0CurrentFrontier.Add(new Cte0Row0(__cte0CurrentFrontierCandidate0, __cte0CurrentFrontierCandidate1));
                                        __op38OutputRows += 1;
                                    }
                                }
                            }
                        }
                        finally
                        {
                            __op37Scope.AddInputRows(__op37InputRows);
                            __op37Scope.AddOutputRows(__op37OutputRows);
                            __op37Scope.Dispose();
                        }

                        cte0.AddRange(cte0CurrentFrontier);
                        if (cte0CurrentFrontier.Count > 0)
                        {
                            var __op40Scope = profileRecorder?.BeginOperatorValue(__op40Handle) ?? OperatorProfileValueScope.None;
                            var cte0Invariant0Hash = new Dictionary<int, HashJoinBucket<Cte0Invariant0Row0>>();
                            __op40Scope.Dispose();
                            var __op41Scope = profileRecorder?.BeginOperatorValue(__op41Handle) ?? OperatorProfileValueScope.None;
                            var __cte0Invariant0_eSchema = provider.GetSchema("#graph");
                            var cte0Invariant0_eRowsProfile = profileRecorder?.CreateSourceRecorder("e");
                            var cte0Invariant0_eRowsSource = __cte0Invariant0_eSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge>("edges", new SourceExecutionContext("e:2", sourceExecutionPlans["e:2"], token, __schemaColumns_compiled_e_1, sourceRuntimeSettingsBySourceContextId["e:2"], logger, OnDataSourceProgress, cte0Invariant0_eRowsProfile == null ? SourceDiagnostics.None : cte0Invariant0_eRowsProfile.CreateDiagnostics()), Array.Empty<object>());
                            var cte0Invariant0_eRows = cte0Invariant0_eRowsProfile == null ? cte0Invariant0_eRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge>.Create(cte0Invariant0_eRowsSource.Chunks, cte0Invariant0_eRowsProfile);
                            __op41Scope.Dispose();
                            var __op42Scope = profileRecorder?.BeginOperatorValue(__op42Handle) ?? OperatorProfileValueScope.None;
                            expectedValuesF9227A53Row0[] cte0Invariant0_expectedRows = new expectedValuesF9227A53Row0[]
                            {
                                new expectedValuesF9227A53Row0("one-two"),
                                new expectedValuesF9227A53Row0("two-three")
                            };
                            __op42Scope.AddOutputRows(cte0Invariant0_expectedRows.Length);
                            __op42Scope.Dispose();
                            var __op43Scope = profileRecorder?.BeginOperatorValue(__op43Handle) ?? OperatorProfileValueScope.None;
                            var cte0Invariant0ExpectedHash = new Dictionary<string, HashJoinBucket<expectedValuesF9227A53Row0>>(2);
                            __op43Scope.Dispose();
                            long __op44InputRows = 0L;
                            long __op44OutputRows = 0L;
                            var __op44Scope = profileRecorder?.BeginOperatorValue(__op44Handle) ?? OperatorProfileValueScope.None;
                            try
                            {
                                foreach (var expected in cte0Invariant0_expectedRows)
                                {
                                    token.ThrowIfCancellationRequested();
                                    __op44InputRows++;
                                    __op44OutputRows++;
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

                                    __op45OutputRows += 1;
                                }
                            }
                            finally
                            {
                                __op44Scope.AddInputRows(__op44InputRows);
                                __op44Scope.AddOutputRows(__op44OutputRows);
                                __op44Scope.Dispose();
                            }

                            long __op46InputRows = 0L;
                            long __op46OutputRows = 0L;
                            var __op46Scope = profileRecorder?.BeginOperatorValue(__op46Handle) ?? OperatorProfileValueScope.None;
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
                                                __op46InputRows++;
                                                __op46OutputRows++;
                                                __op47InputRows += 1;
                                                string key = e.Label;
                                                if (key != null && cte0Invariant0ExpectedHash.TryGetValue(key, out var cte0Invariant0ExpectedHashMatches))
                                                {
                                                    foreach (var expected in cte0Invariant0ExpectedHashMatches)
                                                    {
                                                        token.ThrowIfCancellationRequested();
                                                        __op48InputRows++;
                                                        __op48OutputRows++;
                                                        var __op49Scope = profileRecorder?.BeginOperatorValue(__op49Handle) ?? OperatorProfileValueScope.None;
                                                        {
                                                            var __op50Scope = profileRecorder?.BeginOperatorValue(__op50Handle) ?? OperatorProfileValueScope.None;
                                                            if (__cte0SnapshotRows >= 10000000)
                                                            {
                                                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 10000000);
                                                            }

                                                            __cte0SnapshotRows++;
                                                            __op50Scope.Dispose();
                                                            var __op51Scope = profileRecorder?.BeginOperatorValue(__op51Handle) ?? OperatorProfileValueScope.None;
                                                            Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.SourceId, e.TargetId, e.Label, expected.Label);
                                                            __op51Scope.Dispose();
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

                                                            __op52OutputRows += 1;
                                                        }

                                                        __op49Scope.Dispose();
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
                                                __op46InputRows++;
                                                __op46OutputRows++;
                                                __op47InputRows += 1;
                                                string key = e.Label;
                                                if (key != null && cte0Invariant0ExpectedHash.TryGetValue(key, out var cte0Invariant0ExpectedHashMatches))
                                                {
                                                    foreach (var expected in cte0Invariant0ExpectedHashMatches)
                                                    {
                                                        token.ThrowIfCancellationRequested();
                                                        __op48InputRows++;
                                                        __op48OutputRows++;
                                                        var __op49Scope = profileRecorder?.BeginOperatorValue(__op49Handle) ?? OperatorProfileValueScope.None;
                                                        {
                                                            var __op50Scope = profileRecorder?.BeginOperatorValue(__op50Handle) ?? OperatorProfileValueScope.None;
                                                            if (__cte0SnapshotRows >= 10000000)
                                                            {
                                                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 10000000);
                                                            }

                                                            __cte0SnapshotRows++;
                                                            __op50Scope.Dispose();
                                                            var __op51Scope = profileRecorder?.BeginOperatorValue(__op51Handle) ?? OperatorProfileValueScope.None;
                                                            Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.SourceId, e.TargetId, e.Label, expected.Label);
                                                            __op51Scope.Dispose();
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

                                                            __op52OutputRows += 1;
                                                        }

                                                        __op49Scope.Dispose();
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
                                        __op46InputRows++;
                                        __op46OutputRows++;
                                        __op47InputRows += 1;
                                        string key = e.Label;
                                        if (key != null && cte0Invariant0ExpectedHash.TryGetValue(key, out var cte0Invariant0ExpectedHashMatches))
                                        {
                                            foreach (var expected in cte0Invariant0ExpectedHashMatches)
                                            {
                                                token.ThrowIfCancellationRequested();
                                                __op48InputRows++;
                                                __op48OutputRows++;
                                                var __op49Scope = profileRecorder?.BeginOperatorValue(__op49Handle) ?? OperatorProfileValueScope.None;
                                                {
                                                    var __op50Scope = profileRecorder?.BeginOperatorValue(__op50Handle) ?? OperatorProfileValueScope.None;
                                                    if (__cte0SnapshotRows >= 10000000)
                                                    {
                                                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("reachable", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 10000000);
                                                    }

                                                    __cte0SnapshotRows++;
                                                    __op50Scope.Dispose();
                                                    var __op51Scope = profileRecorder?.BeginOperatorValue(__op51Handle) ?? OperatorProfileValueScope.None;
                                                    Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.SourceId, e.TargetId, e.Label, expected.Label);
                                                    __op51Scope.Dispose();
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

                                                    __op52OutputRows += 1;
                                                }

                                                __op49Scope.Dispose();
                                            }
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                __op46Scope.AddInputRows(__op46InputRows);
                                __op46Scope.AddOutputRows(__op46OutputRows);
                                __op46Scope.Dispose();
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
                                __op34InputRows += cte0CurrentFrontier.Count;
                                long __op54InputRows = 0L;
                                long __op54OutputRows = 0L;
                                var __op54Scope = profileRecorder?.BeginOperatorValue(__op54Handle) ?? OperatorProfileValueScope.None;
                                try
                                {
                                    for (int cte0CurrentFrontierIndex = 0; cte0CurrentFrontierIndex < cte0CurrentFrontier.Count; ++cte0CurrentFrontierIndex)
                                    {
                                        if (cte0CurrentFrontierIndex != 0 && (cte0CurrentFrontierIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        Cte0Row0 r = (Cte0Row0)cte0CurrentFrontier[cte0CurrentFrontierIndex];
                                        __op54InputRows++;
                                        __op54OutputRows++;
                                        __op55InputRows += 1;
                                        int key = r.Id;
                                        if (cte0Invariant0Hash.TryGetValue(key, out var cte0Invariant0HashMatches))
                                        {
                                            foreach (var eexpected in cte0Invariant0HashMatches)
                                            {
                                                __op56InputRows++;
                                                __op56OutputRows++;
                                                __op57InputRows += 1;
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
                                                    __op57OutputRows += 1;
                                                }
                                            }
                                        }
                                    }
                                }
                                finally
                                {
                                    __op54Scope.AddInputRows(__op54InputRows);
                                    __op54Scope.AddOutputRows(__op54OutputRows);
                                    __op54Scope.Dispose();
                                }

                                __op34OutputRows += cte0NextFrontier.Count;
                                cte0.AddRange(cte0NextFrontier);
                                var __cte0FrontierSwap = cte0CurrentFrontier;
                                cte0CurrentFrontier = cte0NextFrontier;
                                cte0NextFrontier = __cte0FrontierSwap;
                            }
                        }
                    }
                    finally
                    {
                        __op34Scope.AddInputRows(__op34InputRows);
                        __op34Scope.AddOutputRows(__op34OutputRows);
                        __op34Scope.Dispose();
                    }

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

                    var __op59Scope = profileRecorder?.BeginOperatorValue(__op59Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>();
                    __op59Scope.Dispose();
                    long __op60InputRows = 0L;
                    long __op60OutputRows = 0L;
                    var __op60Scope = profileRecorder?.BeginOperatorValue(__op60Handle) ?? OperatorProfileValueScope.None;
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
                            __op60InputRows++;
                            __op60OutputRows++;
                            result.Add(new ResultShape0(reachable.Id, reachable.Depth));
                            __op61OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op60Scope.AddInputRows(__op60InputRows);
                        __op60Scope.AddOutputRows(__op60OutputRows);
                        __op60Scope.Dispose();
                    }

                    var __op62Scope = profileRecorder?.BeginOperatorValue(__op62Handle) ?? OperatorProfileValueScope.None;
                    __op62Scope.AddInputRows(result.Count);
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

                    __op62Scope.AddOutputRows(__musoqFinalShapeRows.Count);
                    __op62Scope.Dispose();
                    if (__op38InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op38Handle, __op38InputRows);
                    if (__op38OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op38Handle, __op38OutputRows);
                    if (__op45OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op45Handle, __op45OutputRows);
                    if (__op47InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op47Handle, __op47InputRows);
                    if (__op48InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op48Handle, __op48InputRows);
                    if (__op48OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op48Handle, __op48OutputRows);
                    if (__op52OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op52Handle, __op52OutputRows);
                    if (__op55InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op55Handle, __op55InputRows);
                    if (__op56InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op56Handle, __op56InputRows);
                    if (__op56OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op56Handle, __op56OutputRows);
                    if (__op57InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op57Handle, __op57InputRows);
                    if (__op57OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op57Handle, __op57OutputRows);
                    if (__op61OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op61Handle, __op61OutputRows);
                    return __musoqFinalShapeRows;
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
                    OnPhaseChanged("compiled", QueryPhase.End);
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
