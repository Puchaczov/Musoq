// === Parsed Query ===
/*
with Records as (select (Position: Position, Detail: (Value: Value)) as Payload from #schema.first()) select Payload.Position as Position, Payload.Detail.Value as Value from Records order by Position
*/

// === Logical Plan ===
/*
Cte
  Definition [Records]
    MultiStatement
      Project [(Position: ko3iko.Position, Detail: (Value: ko3iko.Value)) as Payload]
        SchemaScan [#schema.first() as ko3iko]
  Query
    MultiStatement
      Sort [Records.Payload.F0]
        Project [Records.Payload.F0 as Position, Records.Payload.F1.F0 as Value]
          CteRef [Records as Records]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [Records]
    PhysicalMultiStatement
      PhysicalProject [(Position: ko3iko.Position, Detail: (Value: ko3iko.Value)) as Payload]
        PhysicalSchemaScan [#schema.first() as ko3iko]
  Query
    PhysicalMultiStatement
      PhysicalSort [Records.Payload.F0]
        PhysicalProject [Records.Payload.F0 as Position, Records.Payload.F1.F0 as Value]
          PhysicalCteRef [Records as Records]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: NativeRecordInput]
      Position: int <- property Position
      Value: int? <- property Value
    Generated [Cte0Row0]
      Payload: StructuralValue <- field Payload
    TableRow [Records]
      Payload: StructuralValue <- field Payload
    Generated [ResultRow0]
      Position: int <- field Position
      Value: int? <- field Value

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [From:cte0]
    SourceScan [ko3iko: NativeRecordInput] -> cte0_ko3ikoRows
    CreateTable [cte0: Cte0Row0]
    PhaseBoundary [Select:cte0]
    ChunkedForEach [ko3iko in cte0_ko3ikoRows]
      AppendRow [cte0 <- Cte0Row0(Payload: (Position: ko3iko.Position, Detail: (Value: ko3iko.Value)))]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [Records in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Position: Records.Payload.F0, Value: Records.Payload.F1.F0)]
    SortShapeRows [result -> resultSorted by Position ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P63_TypedNativeRecordMembersThroughCte_Full
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
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("Payload", typeof(__musoqStructuralCarrier_0_root), 0)
        };
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Position", typeof(int), 0),
            new Column("Value", typeof(int?), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Value", typeof(int?), 1) });
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
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Value);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Value);
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
                var __musoqFinalShapeRows = new List<ResultShape0>();
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

                    Cte0Row0 Records = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(Records.Payload.F0, Records.Payload.F1.F0));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Position.CompareTo(right.Position);
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
                    var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                    ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                    var __musoqFinalShapeRows = new List<ResultShape0>();
                    var __op14Handle = profileRecorder?.GetOperatorHandle("op14", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op15Handle = profileRecorder?.GetOperatorHandle("op15", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op17Handle = profileRecorder?.GetOperatorHandle("op17", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op26Handle = profileRecorder?.GetOperatorHandle("op26", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op27Handle = profileRecorder?.GetOperatorHandle("op27", "ForEach") ?? OperatorProfileHandle.None;
                    var __op28Handle = profileRecorder?.GetOperatorHandle("op28", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op29Handle = profileRecorder?.GetOperatorHandle("op29", "SortShapeRows") ?? OperatorProfileHandle.None;
                    long __op28OutputRows = 0L;
                    var __op14Scope = profileRecorder?.BeginOperatorValue(__op14Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op14Scope.Dispose();
                    var __op15Scope = profileRecorder?.BeginOperatorValue(__op15Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op15Scope.Dispose();
                    var __op17Scope = profileRecorder?.BeginOperatorValue(__op17Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op17Scope.Dispose();
                    _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, profileRecorder, _cteRowResults);
                    var __op26Scope = profileRecorder?.BeginOperatorValue(__op26Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>();
                    __op26Scope.Dispose();
                    long __op27InputRows = 0L;
                    long __op27OutputRows = 0L;
                    var __op27Scope = profileRecorder?.BeginOperatorValue(__op27Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        var __storedTable0Rows = _cteRowResults.Slot0;
                        for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                        {
                            if ((__storedTable0Index & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Cte0Row0 Records = __storedTable0Rows[__storedTable0Index];
                            __op27InputRows++;
                            __op27OutputRows++;
                            result.Add(new ResultShape0(Records.Payload.F0, Records.Payload.F1.F0));
                            __op28OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op27Scope.AddInputRows(__op27InputRows);
                        __op27Scope.AddOutputRows(__op27OutputRows);
                        __op27Scope.Dispose();
                    }

                    var __op29Scope = profileRecorder?.BeginOperatorValue(__op29Handle) ?? OperatorProfileValueScope.None;
                    __op29Scope.AddInputRows(result.Count);
                    var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                    {
                        var comparison = left.Position.CompareTo(right.Position);
                        if (comparison != 0)
                            return comparison;
                        return 0;
                    }));
                    foreach (var resultSortedRowsRow in resultSortedRows)
                    {
                        __musoqFinalShapeRows.Add(resultSortedRowsRow);
                    }

                    __op29Scope.AddOutputRows(__musoqFinalShapeRows.Count);
                    __op29Scope.Dispose();
                    if (__op28OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op28Handle, __op28OutputRows);
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                var __cte0_ko3ikoSchema = provider.GetSchema("#schema");
                var cte0_ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionBoundaryQueryTests.NativeRecordInput>.Open(__cte0_ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var cte0_ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionBoundaryQueryTests.NativeRecordInput>(cte0_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : cte0_ko3ikoRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                foreach (var ko3ikoChunk in cte0_ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionBoundaryQueryTests.NativeRecordInput> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionBoundaryQueryTests.NativeRecordInput[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                cte0.Add(new Cte0Row0(new __musoqStructuralCarrier_0_root(ko3iko.Position, new __musoqStructuralCarrier_0_1(ko3iko.Value, 1UL), 3UL)));
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionBoundaryQueryTests.NativeRecordInput> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                cte0.Add(new Cte0Row0(new __musoqStructuralCarrier_0_root(ko3iko.Position, new __musoqStructuralCarrier_0_1(ko3iko.Value, 1UL), 3UL)));
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
                        cte0.Add(new Cte0Row0(new __musoqStructuralCarrier_0_root(ko3iko.Position, new __musoqStructuralCarrier_0_1(ko3iko.Value, 1UL), 3UL)));
                    }
                }

                return cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, Musoq.Evaluator.Diagnostics.QueryProfileRecorder profileRecorder, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                var __profileScopeDepth = profileRecorder?.GetCurrentOperatorScopeDepth() ?? 0;
                try
                {
                    var __op19Handle = profileRecorder?.GetOperatorHandle("op19", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op20Handle = profileRecorder?.GetOperatorHandle("op20", "CreateTable") ?? OperatorProfileHandle.None;
                    var __op22Handle = profileRecorder?.GetOperatorHandle("op22", "ChunkedForEach") ?? OperatorProfileHandle.None;
                    var __op23Handle = profileRecorder?.GetOperatorHandle("op23", "AppendRow") ?? OperatorProfileHandle.None;
                    long __op23OutputRows = 0L;
                    var __op19Scope = profileRecorder?.BeginOperatorValue(__op19Handle) ?? OperatorProfileValueScope.None;
                    var __cte0_ko3ikoSchema = provider.GetSchema("#schema");
                    var cte0_ko3ikoRowsProfile = profileRecorder?.CreateSourceRecorder("ko3iko");
                    var cte0_ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionBoundaryQueryTests.NativeRecordInput>.Open(__cte0_ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress, cte0_ko3ikoRowsProfile == null ? SourceDiagnostics.None : cte0_ko3ikoRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                    var cte0_ko3ikoRows = cte0_ko3ikoRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionBoundaryQueryTests.NativeRecordInput>(cte0_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : cte0_ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionBoundaryQueryTests.NativeRecordInput>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionBoundaryQueryTests.NativeRecordInput>(cte0_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : cte0_ko3ikoRowsSource.Chunks, cte0_ko3ikoRowsProfile);
                    __op19Scope.Dispose();
                    var __op20Scope = profileRecorder?.BeginOperatorValue(__op20Handle) ?? OperatorProfileValueScope.None;
                    var cte0 = new List<Cte0Row0>();
                    __op20Scope.Dispose();
                    long __op22InputRows = 0L;
                    long __op22OutputRows = 0L;
                    var __op22Scope = profileRecorder?.BeginOperatorValue(__op22Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        foreach (var ko3ikoChunk in cte0_ko3ikoRows)
                        {
                            if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionBoundaryQueryTests.NativeRecordInput> ko3ikoChunkView)
                            {
                                if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionBoundaryQueryTests.NativeRecordInput[] ko3ikoChunkViewArray)
                                {
                                    int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                                    for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                                    {
                                        if ((ko3ikoIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                        __op22InputRows++;
                                        __op22OutputRows++;
                                        cte0.Add(new Cte0Row0(new __musoqStructuralCarrier_0_root(ko3iko.Position, new __musoqStructuralCarrier_0_1(ko3iko.Value, 1UL), 3UL)));
                                        __op23OutputRows += 1;
                                    }

                                    continue;
                                }

                                if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionBoundaryQueryTests.NativeRecordInput> ko3ikoChunkViewList)
                                {
                                    int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                                    for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                                    {
                                        if ((ko3ikoIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                        __op22InputRows++;
                                        __op22OutputRows++;
                                        cte0.Add(new Cte0Row0(new __musoqStructuralCarrier_0_root(ko3iko.Position, new __musoqStructuralCarrier_0_1(ko3iko.Value, 1UL), 3UL)));
                                        __op23OutputRows += 1;
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
                                __op22InputRows++;
                                __op22OutputRows++;
                                cte0.Add(new Cte0Row0(new __musoqStructuralCarrier_0_root(ko3iko.Position, new __musoqStructuralCarrier_0_1(ko3iko.Value, 1UL), 3UL)));
                                __op23OutputRows += 1;
                            }
                        }
                    }
                    finally
                    {
                        __op22Scope.AddInputRows(__op22InputRows);
                        __op22Scope.AddOutputRows(__op22OutputRows);
                        __op22Scope.Dispose();
                    }

                    if (__op23OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op23Handle, __op23OutputRows);
                    return cte0;
                }
                catch (Exception __profileException)when (profileRecorder != null && profileRecorder.RecordActiveOperatorException(__profileException, __profileScopeDepth))
                {
                    profileRecorder.DisposeActiveOperatorScopes(__profileScopeDepth);
                    throw;
                }
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(__musoqStructuralCarrier_0_root __value0)
            {
                Payload = __value0;
            }

            public __musoqStructuralCarrier_0_root Payload { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, int? __value1)
            {
                Position = __value0;
                Value = __value1;
            }

            public override int Count => 2;
            public int Position { get; private set; }
            public int? Value { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "Value" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Value, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Position, int? Value)
            {
                this.Position = Position;
                this.Value = Value;
            }

            public int Position { get; }
            public int? Value { get; }
        }

        [global::System.Text.Json.Serialization.JsonConverter(typeof(__musoqStructuralCarrier_0_1JsonConverter))]
        private readonly struct __musoqStructuralCarrier_0_1
        {
            public readonly int? F0;
            public readonly ulong P0;
            public __musoqStructuralCarrier_0_1(int? f0, ulong p0)
            {
                F0 = f0;
                P0 = p0;
            }
        }

        private sealed class __musoqStructuralCarrier_0_1JsonConverter : global::System.Text.Json.Serialization.JsonConverter<__musoqStructuralCarrier_0_1>
        {
            public __musoqStructuralCarrier_0_1JsonConverter()
            {
            }

            public override __musoqStructuralCarrier_0_1 Read(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
            {
                if (reader.TokenType != global::System.Text.Json.JsonTokenType.StartObject)
                    throw new global::System.Text.Json.JsonException("A native record requires a JSON object.");
                int? field0 = default;
                ulong p0 = 0;
                while (reader.Read())
                {
                    if (reader.TokenType == global::System.Text.Json.JsonTokenType.EndObject)
                    {
                        if ((p0 & 1UL) == 0)
                            throw new global::System.Text.Json.JsonException("A required native field is missing.");
                        return new __musoqStructuralCarrier_0_1(field0, p0);
                    }

                    if (reader.TokenType != global::System.Text.Json.JsonTokenType.PropertyName)
                        throw new global::System.Text.Json.JsonException("A native record requires named fields.");
                    if (reader.ValueTextEquals("Value"))
                    {
                        if ((p0 & 1UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field0 = global::System.Text.Json.JsonSerializer.Deserialize<int?>(ref reader, options)!;
                        p0 |= 1UL;
                        continue;
                    }

                    throw new global::System.Text.Json.JsonException("The native record contains an undeclared field.");
                }

                throw new global::System.Text.Json.JsonException("The native record is incomplete.");
            }

            public override void Write(global::System.Text.Json.Utf8JsonWriter writer, __musoqStructuralCarrier_0_1 value, global::System.Text.Json.JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                if ((value.P0 & 1UL) != 0)
                {
                    writer.WritePropertyName("Value");
                    global::System.Text.Json.JsonSerializer.Serialize<int?>(writer, value.F0, options);
                }

                writer.WriteEndObject();
            }
        }

        [global::System.Text.Json.Serialization.JsonConverter(typeof(__musoqStructuralCarrier_0_rootJsonConverter))]
        private readonly struct __musoqStructuralCarrier_0_root
        {
            public readonly int F0;
            public readonly __musoqStructuralCarrier_0_1 F1;
            public readonly ulong P0;
            public __musoqStructuralCarrier_0_root(int f0, __musoqStructuralCarrier_0_1 f1, ulong p0)
            {
                F0 = f0;
                F1 = f1;
                P0 = p0;
            }
        }

        private sealed class __musoqStructuralCarrier_0_rootJsonConverter : global::System.Text.Json.Serialization.JsonConverter<__musoqStructuralCarrier_0_root>
        {
            public __musoqStructuralCarrier_0_rootJsonConverter()
            {
            }

            public override __musoqStructuralCarrier_0_root Read(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
            {
                if (reader.TokenType != global::System.Text.Json.JsonTokenType.StartObject)
                    throw new global::System.Text.Json.JsonException("A native record requires a JSON object.");
                int field0 = default;
                __musoqStructuralCarrier_0_1 field1 = default;
                ulong p0 = 0;
                while (reader.Read())
                {
                    if (reader.TokenType == global::System.Text.Json.JsonTokenType.EndObject)
                    {
                        if ((p0 & 1UL) == 0)
                            throw new global::System.Text.Json.JsonException("A required native field is missing.");
                        if ((p0 & 2UL) == 0)
                            throw new global::System.Text.Json.JsonException("A required native field is missing.");
                        return new __musoqStructuralCarrier_0_root(field0, field1, p0);
                    }

                    if (reader.TokenType != global::System.Text.Json.JsonTokenType.PropertyName)
                        throw new global::System.Text.Json.JsonException("A native record requires named fields.");
                    if (reader.ValueTextEquals("Position"))
                    {
                        if ((p0 & 1UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field0 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 1UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("Detail"))
                    {
                        if ((p0 & 2UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field1 = global::System.Text.Json.JsonSerializer.Deserialize<__musoqStructuralCarrier_0_1>(ref reader, options)!;
                        p0 |= 2UL;
                        continue;
                    }

                    throw new global::System.Text.Json.JsonException("The native record contains an undeclared field.");
                }

                throw new global::System.Text.Json.JsonException("The native record is incomplete.");
            }

            public override void Write(global::System.Text.Json.Utf8JsonWriter writer, __musoqStructuralCarrier_0_root value, global::System.Text.Json.JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                if ((value.P0 & 1UL) != 0)
                {
                    writer.WritePropertyName("Position");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F0, options);
                }

                if ((value.P0 & 2UL) != 0)
                {
                    writer.WritePropertyName("Detail");
                    global::System.Text.Json.JsonSerializer.Serialize<__musoqStructuralCarrier_0_1>(writer, value.F1, options);
                }

                writer.WriteEndObject();
            }
        }
    }
}
