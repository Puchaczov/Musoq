// === Parsed Query ===
/*
with p as (select Name as Name from #A.entities()), q as (select Name as Name from #B.entities()) select p.Name, q.Name from p inner join q on p.Name = q.Name
*/

// === Logical Plan ===
/*
Cte
  Definition [p]
    MultiStatement
      Project [ko3iko.Name as Name]
        SchemaScan [#A.entities() as ko3iko]
  Definition [q]
    MultiStatement
      Project [vo04qt.Name as Name]
        SchemaScan [#B.entities() as vo04qt]
  Query
    MultiStatement
      Project [p.Name as p.Name, q.Name as q.Name]
        Join [Inner] [(p.Name = q.Name)]
          CteRef [p as p]
          CteRef [q as q]
      Project [p.Name as p.Name, q.Name as q.Name]
        CteRef [pq as pq]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [p]
    PhysicalMultiStatement
      PhysicalProject [ko3iko.Name as Name]
        PhysicalSchemaScan [#A.entities() as ko3iko]
  Definition [q]
    PhysicalMultiStatement
      PhysicalProject [vo04qt.Name as Name]
        PhysicalSchemaScan [#B.entities() as vo04qt]
  Query
    PhysicalMultiStatement
      PhysicalProject [p.Name as p.Name, q.Name as q.Name]
        PhysicalHashJoin [Inner] [build: q.Name] [probe: p.Name]
          PhysicalCteRef [p as p]
          PhysicalCteRef [q as q]
      PhysicalProject [p.Name as p.Name, q.Name as q.Name]
        PhysicalCteRef [pq as pq]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
    Generated [Cte0Row0]
      Name: string <- field Name
    SourceEntity [vo04qt: BasicEntity]
      Name: string <- property Name
    HashPayload [Cte1HashPayload0]
      Name: string <- field Name
    TableRow [p]
      Name: string <- field Name
    HashPayload [Cte1HashPayload0]
      Name: string <- field Name
    TableRow [q]
      Name: string <- field Name
    Generated [ResultRow0]
      p.Name: string <- field p_Name
      q.Name: string <- field q_Name

  Body
    ParallelBlock [cte-level-0, tasks 2, maxDegree 2]
      ParallelTask [p -> __parallelCteLevel0Task0Result]
        SourceScan [ko3iko: BasicEntity] -> cte0_ko3ikoRows
        CreateTable [cte0: Cte0Row0]
        ChunkedForEach [ko3iko in cte0_ko3ikoRows]
          AppendRow [cte0 <- Cte0Row0(Name: ko3iko.Name)]
        Assign [__parallelCteLevel0Task0Result = cte0]
      ParallelTask [q -> __parallelCteLevel0Task1Result]
        SourceScan [vo04qt: BasicEntity] -> cte1_vo04qtRows
        CreateHash [cte1HashSidecar0Name: string -> Row]
        ChunkedForEach [vo04qt in cte1_vo04qtRows]
          CreateHashPayload [cte1SidecarPayload0 <- Cte1HashPayload0(Name: vo04qt.Name)]
          HashAdd [cte1HashSidecar0Name[vo04qt.Name] += cte1SidecarPayload0]
        StoreCteIndex [cte1HashSidecar0Name -> _cteIndexResults.Slot0 Hash]
      ParallelMerge
        StoreTable [__parallelCteLevel0Task0Result -> _cteRowResults.Slot0: List<Cte0Row0>]
    CtePhase [cte2]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    LoadCteIndex [qHash <- _cteIndexResults.Slot0 Hash: string]
    ForEach [p in _cteRowResults.Slot0]
      HashProbe [qHash[p.Name] -> qHashMatches]
        ForEach [q in qHashMatches]
          AppendShape [result <- ResultShape0(p.Name: p.Name, q.Name: q.Name)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P07_ParallelCte_Full
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
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("Name", typeof(string), 0)
        };
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("p.Name", typeof(string), 0),
            new Column("q.Name", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            ArgumentNullException.ThrowIfNull(profileRecorder);
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0_Profiled(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken, profileRecorder), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.p_Name, __musoqShapeRow.q_Name);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.p_Name, __musoqShapeRow.q_Name);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled:cte2", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var _cteRowResults = new CteRowResults();
                var _cteIndexResults = new CteIndexResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                List<Cte0Row0> __parallelCteLevel0Task0Result = null;
                object __parallelCteLevel0Task1Result = null;
                var cteLevel0Runner = new CteLevel0Runner(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, OnPhaseChanged, _cteRowResults, _cteIndexResults);
                Parallel.Invoke(new ParallelOptions() { CancellationToken = token, MaxDegreeOfParallelism = 2 }, cteLevel0Runner.RunCteLevel0Task0, cteLevel0Runner.RunCteLevel0Task1);
                token.ThrowIfCancellationRequested();
                __parallelCteLevel0Task0Result = cteLevel0Runner.Task0Result;
                __parallelCteLevel0Task1Result = cteLevel0Runner.Task1Result;
                _cteRowResults.Slot0 = __parallelCteLevel0Task0Result;
                var qHash = _cteIndexResults.Slot0;
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 p = __storedTable0Rows[__storedTable0Index];
                    string key = p.Name;
                    if (key != null && qHash.TryGetValue(key, out var qHashMatches))
                    {
                        foreach (var q in qHashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            __musoqFinalShapeRows.Add(new ResultShape0(p.Name, q.Name));
                        }
                    }
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
                OnPhaseChanged("compiled:cte2", QueryPhase.End);
                OnPhaseChanged("compiled", QueryPhase.End);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            var __profileScopeDepth = profileRecorder?.GetCurrentOperatorScopeDepth() ?? 0;
            try
            {
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte2", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.Select);
                try
                {
                    var _cteRowResults = new CteRowResults();
                    var _cteIndexResults = new CteIndexResults();
                    var __musoqExecutionState = ExecutionState.Capture(Parameters);
                    ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                    var __musoqFinalShapeRows = new List<ResultShape0>();
                    var __op21Handle = profileRecorder?.GetOperatorHandle("op21", "ParallelBlock") ?? OperatorProfileHandle.None;
                    var __op36Handle = profileRecorder?.GetOperatorHandle("op36", "StoreTable") ?? OperatorProfileHandle.None;
                    var __op37Handle = profileRecorder?.GetOperatorHandle("op37", "CtePhase") ?? OperatorProfileHandle.None;
                    var __op39Handle = profileRecorder?.GetOperatorHandle("op39", "LoadCteIndex") ?? OperatorProfileHandle.None;
                    var __op40Handle = profileRecorder?.GetOperatorHandle("op40", "ForEach") ?? OperatorProfileHandle.None;
                    var __op41Handle = profileRecorder?.GetOperatorHandle("op41", "HashProbe") ?? OperatorProfileHandle.None;
                    var __op42Handle = profileRecorder?.GetOperatorHandle("op42", "ForEach") ?? OperatorProfileHandle.None;
                    var __op43Handle = profileRecorder?.GetOperatorHandle("op43", "AppendShape") ?? OperatorProfileHandle.None;
                    long __op41InputRows = 0L;
                    long __op42InputRows = 0L;
                    long __op42OutputRows = 0L;
                    long __op43OutputRows = 0L;
                    var __op21Scope = profileRecorder?.BeginOperatorValue(__op21Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        List<Cte0Row0> __parallelCteLevel0Task0Result = null;
                        object __parallelCteLevel0Task1Result = null;
                        var cteLevel0Runner_Profiled = new CteLevel0Runner_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, profileRecorder, OnPhaseChanged, _cteRowResults, _cteIndexResults);
                        Parallel.Invoke(new ParallelOptions() { CancellationToken = token, MaxDegreeOfParallelism = 2 }, cteLevel0Runner_Profiled.RunCteLevel0Task0, cteLevel0Runner_Profiled.RunCteLevel0Task1);
                        token.ThrowIfCancellationRequested();
                        __parallelCteLevel0Task0Result = cteLevel0Runner_Profiled.Task0Result;
                        __parallelCteLevel0Task1Result = cteLevel0Runner_Profiled.Task1Result;
                        var __op36Scope = profileRecorder?.BeginOperatorValue(__op36Handle) ?? OperatorProfileValueScope.None;
                        try
                        {
                            _cteRowResults.Slot0 = __parallelCteLevel0Task0Result;
                            __op36Scope.AddOutputRows(__parallelCteLevel0Task0Result.Count);
                        }
                        finally
                        {
                            __op36Scope.Dispose();
                        }
                    }
                    finally
                    {
                        __op21Scope.Dispose();
                    }

                    var __op37Scope = profileRecorder?.BeginOperatorValue(__op37Handle) ?? OperatorProfileValueScope.None;
                    __op37Scope.Dispose();
                    var __op39Scope = profileRecorder?.BeginOperatorValue(__op39Handle) ?? OperatorProfileValueScope.None;
                    var qHash = _cteIndexResults.Slot0;
                    __op39Scope.Dispose();
                    long __op40InputRows = 0L;
                    long __op40OutputRows = 0L;
                    var __op40Scope = profileRecorder?.BeginOperatorValue(__op40Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        var __storedTable0Rows = _cteRowResults.Slot0;
                        for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                        {
                            if ((__storedTable0Index & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Cte0Row0 p = __storedTable0Rows[__storedTable0Index];
                            __op40InputRows++;
                            __op40OutputRows++;
                            __op41InputRows += 1;
                            string key = p.Name;
                            if (key != null && qHash.TryGetValue(key, out var qHashMatches))
                            {
                                foreach (var q in qHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    __op42InputRows++;
                                    __op42OutputRows++;
                                    __musoqFinalShapeRows.Add(new ResultShape0(p.Name, q.Name));
                                    __op43OutputRows += 1;
                                }
                            }
                        }
                    }
                    finally
                    {
                        __op40Scope.AddInputRows(__op40InputRows);
                        __op40Scope.AddOutputRows(__op40OutputRows);
                        __op40Scope.Dispose();
                    }

                    if (__op41InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op41Handle, __op41InputRows);
                    if (__op42InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op42Handle, __op42InputRows);
                    if (__op42OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op42Handle, __op42OutputRows);
                    if (__op43OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op43Handle, __op43OutputRows);
                    return __musoqFinalShapeRows;
                }
                finally
                {
                    OnPhaseChanged("compiled:cte2", QueryPhase.End);
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCteLevel0Task0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults)
        {
            List<Cte0Row0> __parallelCteLevel0Task0Result = null;
            token.ThrowIfCancellationRequested();
            var __cte0_ko3ikoSchema = provider.GetSchema("#A");
            var cte0_ko3ikoRowsSource = __cte0_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var cte0_ko3ikoRows = cte0_ko3ikoRowsSource.Chunks;
            var cte0 = new List<Cte0Row0>();
            foreach (var ko3ikoChunk in cte0_ko3ikoRows)
            {
                if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkView)
                {
                    if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] ko3ikoChunkViewArray)
                    {
                        int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                        for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                        {
                            if ((ko3ikoIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                            cte0.Add(new Cte0Row0(ko3iko.Name));
                        }

                        continue;
                    }

                    if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkViewList)
                    {
                        int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                        for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                        {
                            if ((ko3ikoIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                            cte0.Add(new Cte0Row0(ko3iko.Name));
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
                    cte0.Add(new Cte0Row0(ko3iko.Name));
                }
            }

            __parallelCteLevel0Task0Result = cte0;
            return __parallelCteLevel0Task0Result;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCteLevel0Task0_Profiled(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.Diagnostics.QueryProfileRecorder profileRecorder, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults)
        {
            List<Cte0Row0> __parallelCteLevel0Task0Result = null;
            token.ThrowIfCancellationRequested();
            var __cte0_ko3ikoSchema = provider.GetSchema("#A");
            var cte0_ko3ikoRowsProfile = profileRecorder?.CreateSourceRecorder("ko3iko");
            var cte0_ko3ikoRowsSource = __cte0_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress, cte0_ko3ikoRowsProfile == null ? SourceDiagnostics.None : cte0_ko3ikoRowsProfile.CreateDiagnostics()), Array.Empty<object>());
            var cte0_ko3ikoRows = cte0_ko3ikoRowsProfile == null ? cte0_ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Create(cte0_ko3ikoRowsSource.Chunks, cte0_ko3ikoRowsProfile);
            var cte0 = new List<Cte0Row0>();
            foreach (var ko3ikoChunk in cte0_ko3ikoRows)
            {
                if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkView)
                {
                    if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] ko3ikoChunkViewArray)
                    {
                        int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                        for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                        {
                            if ((ko3ikoIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                            cte0.Add(new Cte0Row0(ko3iko.Name));
                        }

                        continue;
                    }

                    if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkViewList)
                    {
                        int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                        for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                        {
                            if ((ko3ikoIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                            cte0.Add(new Cte0Row0(ko3iko.Name));
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
                    cte0.Add(new Cte0Row0(ko3iko.Name));
                }
            }

            __parallelCteLevel0Task0Result = cte0;
            return __parallelCteLevel0Task0Result;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static object BuildCteLevel0Task1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                object __parallelCteLevel0Task1Result = null;
                token.ThrowIfCancellationRequested();
                var __cte1_vo04qtSchema = provider.GetSchema("#B");
                var cte1_vo04qtRowsSource = __cte1_vo04qtSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("vo04qt:2", sourceExecutionPlans["vo04qt:2"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["vo04qt:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte1_vo04qtRows = cte1_vo04qtRowsSource.Chunks;
                var cte1HashSidecar0Name = new Dictionary<string, HashJoinBucket<Cte1HashPayload0>>();
                foreach (var vo04qtChunk in cte1_vo04qtRows)
                {
                    if (vo04qtChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> vo04qtChunkView)
                    {
                        if (vo04qtChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] vo04qtChunkViewArray)
                        {
                            int vo04qtChunkViewOffset = vo04qtChunkView.Offset;
                            for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunkView.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                            {
                                if ((vo04qtIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var vo04qt = vo04qtChunkViewArray[vo04qtChunkViewOffset + vo04qtIndex];
                                Cte1HashPayload0 cte1SidecarPayload0 = new Cte1HashPayload0(vo04qt.Name);
                                string cte1HashSidecar0NameKey0 = vo04qt.Name;
                                if (cte1HashSidecar0NameKey0 != null)
                                {
                                    {
                                        ref var cte1HashSidecar0NameBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1HashSidecar0Name, cte1HashSidecar0NameKey0, out var cte1HashSidecar0NameBucket0Exists);
                                        if (!cte1HashSidecar0NameBucket0Exists)
                                        {
                                            cte1HashSidecar0NameBucket0 = new HashJoinBucket<Cte1HashPayload0>(cte1SidecarPayload0);
                                        }
                                        else
                                        {
                                            cte1HashSidecar0NameBucket0.Add(cte1SidecarPayload0);
                                        }
                                    }
                                }
                            }

                            continue;
                        }

                        if (vo04qtChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> vo04qtChunkViewList)
                        {
                            int vo04qtChunkViewOffset = vo04qtChunkView.Offset;
                            for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunkView.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                            {
                                if ((vo04qtIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var vo04qt = vo04qtChunkViewList[vo04qtChunkViewOffset + vo04qtIndex];
                                Cte1HashPayload0 cte1SidecarPayload0 = new Cte1HashPayload0(vo04qt.Name);
                                string cte1HashSidecar0NameKey0 = vo04qt.Name;
                                if (cte1HashSidecar0NameKey0 != null)
                                {
                                    {
                                        ref var cte1HashSidecar0NameBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1HashSidecar0Name, cte1HashSidecar0NameKey0, out var cte1HashSidecar0NameBucket0Exists);
                                        if (!cte1HashSidecar0NameBucket0Exists)
                                        {
                                            cte1HashSidecar0NameBucket0 = new HashJoinBucket<Cte1HashPayload0>(cte1SidecarPayload0);
                                        }
                                        else
                                        {
                                            cte1HashSidecar0NameBucket0.Add(cte1SidecarPayload0);
                                        }
                                    }
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
                        Cte1HashPayload0 cte1SidecarPayload0 = new Cte1HashPayload0(vo04qt.Name);
                        string cte1HashSidecar0NameKey0 = vo04qt.Name;
                        if (cte1HashSidecar0NameKey0 != null)
                        {
                            {
                                ref var cte1HashSidecar0NameBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1HashSidecar0Name, cte1HashSidecar0NameKey0, out var cte1HashSidecar0NameBucket0Exists);
                                if (!cte1HashSidecar0NameBucket0Exists)
                                {
                                    cte1HashSidecar0NameBucket0 = new HashJoinBucket<Cte1HashPayload0>(cte1SidecarPayload0);
                                }
                                else
                                {
                                    cte1HashSidecar0NameBucket0.Add(cte1SidecarPayload0);
                                }
                            }
                        }
                    }
                }

                _cteIndexResults.Slot0 = cte1HashSidecar0Name;
                return __parallelCteLevel0Task1Result;
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static object BuildCteLevel0Task1_Profiled(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.Diagnostics.QueryProfileRecorder profileRecorder, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                object __parallelCteLevel0Task1Result = null;
                token.ThrowIfCancellationRequested();
                var __cte1_vo04qtSchema = provider.GetSchema("#B");
                var cte1_vo04qtRowsProfile = profileRecorder?.CreateSourceRecorder("vo04qt");
                var cte1_vo04qtRowsSource = __cte1_vo04qtSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("vo04qt:2", sourceExecutionPlans["vo04qt:2"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["vo04qt:2"], logger, OnDataSourceProgress, cte1_vo04qtRowsProfile == null ? SourceDiagnostics.None : cte1_vo04qtRowsProfile.CreateDiagnostics()), Array.Empty<object>());
                var cte1_vo04qtRows = cte1_vo04qtRowsProfile == null ? cte1_vo04qtRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Create(cte1_vo04qtRowsSource.Chunks, cte1_vo04qtRowsProfile);
                var cte1HashSidecar0Name = new Dictionary<string, HashJoinBucket<Cte1HashPayload0>>();
                foreach (var vo04qtChunk in cte1_vo04qtRows)
                {
                    if (vo04qtChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> vo04qtChunkView)
                    {
                        if (vo04qtChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] vo04qtChunkViewArray)
                        {
                            int vo04qtChunkViewOffset = vo04qtChunkView.Offset;
                            for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunkView.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                            {
                                if ((vo04qtIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var vo04qt = vo04qtChunkViewArray[vo04qtChunkViewOffset + vo04qtIndex];
                                Cte1HashPayload0 cte1SidecarPayload0 = new Cte1HashPayload0(vo04qt.Name);
                                string cte1HashSidecar0NameKey0 = vo04qt.Name;
                                if (cte1HashSidecar0NameKey0 != null)
                                {
                                    {
                                        ref var cte1HashSidecar0NameBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1HashSidecar0Name, cte1HashSidecar0NameKey0, out var cte1HashSidecar0NameBucket0Exists);
                                        if (!cte1HashSidecar0NameBucket0Exists)
                                        {
                                            cte1HashSidecar0NameBucket0 = new HashJoinBucket<Cte1HashPayload0>(cte1SidecarPayload0);
                                        }
                                        else
                                        {
                                            cte1HashSidecar0NameBucket0.Add(cte1SidecarPayload0);
                                        }
                                    }
                                }
                            }

                            continue;
                        }

                        if (vo04qtChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> vo04qtChunkViewList)
                        {
                            int vo04qtChunkViewOffset = vo04qtChunkView.Offset;
                            for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunkView.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                            {
                                if ((vo04qtIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var vo04qt = vo04qtChunkViewList[vo04qtChunkViewOffset + vo04qtIndex];
                                Cte1HashPayload0 cte1SidecarPayload0 = new Cte1HashPayload0(vo04qt.Name);
                                string cte1HashSidecar0NameKey0 = vo04qt.Name;
                                if (cte1HashSidecar0NameKey0 != null)
                                {
                                    {
                                        ref var cte1HashSidecar0NameBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1HashSidecar0Name, cte1HashSidecar0NameKey0, out var cte1HashSidecar0NameBucket0Exists);
                                        if (!cte1HashSidecar0NameBucket0Exists)
                                        {
                                            cte1HashSidecar0NameBucket0 = new HashJoinBucket<Cte1HashPayload0>(cte1SidecarPayload0);
                                        }
                                        else
                                        {
                                            cte1HashSidecar0NameBucket0.Add(cte1SidecarPayload0);
                                        }
                                    }
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
                        Cte1HashPayload0 cte1SidecarPayload0 = new Cte1HashPayload0(vo04qt.Name);
                        string cte1HashSidecar0NameKey0 = vo04qt.Name;
                        if (cte1HashSidecar0NameKey0 != null)
                        {
                            {
                                ref var cte1HashSidecar0NameBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1HashSidecar0Name, cte1HashSidecar0NameKey0, out var cte1HashSidecar0NameBucket0Exists);
                                if (!cte1HashSidecar0NameBucket0Exists)
                                {
                                    cte1HashSidecar0NameBucket0 = new HashJoinBucket<Cte1HashPayload0>(cte1SidecarPayload0);
                                }
                                else
                                {
                                    cte1HashSidecar0NameBucket0.Add(cte1SidecarPayload0);
                                }
                            }
                        }
                    }
                }

                _cteIndexResults.Slot0 = cte1HashSidecar0Name;
                return __parallelCteLevel0Task1Result;
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(string __value0)
            {
                Name = __value0;
            }

            public string Name { get; }
        }

        private readonly struct Cte1HashPayload0
        {
            public readonly string Name;
            public Cte1HashPayload0(string Name)
            {
                this.Name = Name;
            }
        }

        private sealed class CteIndexResults
        {
            public Dictionary<string, HashJoinBucket<Cte1HashPayload0>> Slot0;
        }

        private sealed class CteLevel0Runner
        {
            private readonly CteIndexResults _cteIndexResults;
            private readonly CteRowResults _cteRowResults;
            private readonly Microsoft.Extensions.Logging.ILogger _logger;
            private readonly Musoq.Schema.DataSourceEventHandler _onDataSourceProgress;
            private readonly Action<string, QueryPhase> _onPhaseChanged;
            private readonly Musoq.Schema.ISchemaProvider _provider;
            private readonly IReadOnlyDictionary<string, SourceExecutionPlan> _sourceExecutionPlans;
            private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _sourceRuntimeSettingsBySourceContextId;
            private readonly CancellationToken _token;
            public CteLevel0Runner(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults)
            {
                _provider = provider;
                _sourceRuntimeSettingsBySourceContextId = sourceRuntimeSettingsBySourceContextId;
                _sourceExecutionPlans = sourceExecutionPlans;
                _logger = logger;
                _token = token;
                _onDataSourceProgress = OnDataSourceProgress;
                _onPhaseChanged = OnPhaseChanged;
                this._cteRowResults = _cteRowResults;
                this._cteIndexResults = _cteIndexResults;
            }

            public List<Cte0Row0> Task0Result { get; private set; }
            public object Task1Result { get; private set; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void RunCteLevel0Task0()
            {
                Task0Result = BuildCteLevel0Task0(_provider, _sourceRuntimeSettingsBySourceContextId, _sourceExecutionPlans, _logger, _token, _onDataSourceProgress, _onPhaseChanged, _cteRowResults, _cteIndexResults);
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void RunCteLevel0Task1()
            {
                Task1Result = BuildCteLevel0Task1(_provider, _sourceRuntimeSettingsBySourceContextId, _sourceExecutionPlans, _logger, _token, _onDataSourceProgress, _onPhaseChanged, _cteRowResults, _cteIndexResults);
            }
        }

        private sealed class CteLevel0Runner_Profiled
        {
            private readonly CteIndexResults _cteIndexResults;
            private readonly CteRowResults _cteRowResults;
            private readonly Microsoft.Extensions.Logging.ILogger _logger;
            private readonly Musoq.Schema.DataSourceEventHandler _onDataSourceProgress;
            private readonly Action<string, QueryPhase> _onPhaseChanged;
            private readonly Musoq.Evaluator.Diagnostics.QueryProfileRecorder _profileRecorder;
            private readonly Musoq.Schema.ISchemaProvider _provider;
            private readonly IReadOnlyDictionary<string, SourceExecutionPlan> _sourceExecutionPlans;
            private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _sourceRuntimeSettingsBySourceContextId;
            private readonly CancellationToken _token;
            public CteLevel0Runner_Profiled(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.Diagnostics.QueryProfileRecorder profileRecorder, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults)
            {
                _provider = provider;
                _sourceRuntimeSettingsBySourceContextId = sourceRuntimeSettingsBySourceContextId;
                _sourceExecutionPlans = sourceExecutionPlans;
                _logger = logger;
                _token = token;
                _onDataSourceProgress = OnDataSourceProgress;
                _profileRecorder = profileRecorder;
                _onPhaseChanged = OnPhaseChanged;
                this._cteRowResults = _cteRowResults;
                this._cteIndexResults = _cteIndexResults;
            }

            public List<Cte0Row0> Task0Result { get; private set; }
            public object Task1Result { get; private set; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void RunCteLevel0Task0()
            {
                Task0Result = BuildCteLevel0Task0_Profiled(_provider, _sourceRuntimeSettingsBySourceContextId, _sourceExecutionPlans, _logger, _token, _onDataSourceProgress, _profileRecorder, _onPhaseChanged, _cteRowResults, _cteIndexResults);
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void RunCteLevel0Task1()
            {
                Task1Result = BuildCteLevel0Task1_Profiled(_provider, _sourceRuntimeSettingsBySourceContextId, _sourceExecutionPlans, _logger, _token, _onDataSourceProgress, _profileRecorder, _onPhaseChanged, _cteRowResults, _cteIndexResults);
            }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                p_Name = __value0;
                q_Name = __value1;
            }

            public override int Count => 2;
            public string p_Name { get; private set; }
            public string q_Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        p_Name = (string)value;
                        break;
                    case 1:
                        q_Name = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "p.Name" => true,
                "p_Name" => true,
                "q.Name" => true,
                "q_Name" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)p_Name,
                1 => (object)q_Name,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "p.Name" => (object)p_Name,
                "p_Name" => (object)p_Name,
                "q.Name" => (object)q_Name,
                "q_Name" => (object)q_Name,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string p_Name, string q_Name)
            {
                this.p_Name = p_Name;
                this.q_Name = q_Name;
            }

            public string p_Name { get; }
            public string q_Name { get; }
        }
    }
}
