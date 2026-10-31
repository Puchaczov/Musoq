// === Parsed Query ===
/*
binary Packet { Amount: int le, Currency: byte }; select s.Position as Position, p.ParsedFields as ParsedFields, p.ErrorField as ErrorField, p.BytesConsumed as BytesConsumed from #schema.first() s cross apply PartialInterpret<Packet>(s.Data) p order by s.Position
*/

// === Logical Plan ===
/*
MultiStatement
  Project [s.Position as s.Position, s.Data as s.Data, p.ParsedFields as p.ParsedFields, p.ErrorField as p.ErrorField, p.BytesConsumed as p.BytesConsumed]
    Apply [Cross]
      SchemaScan [#schema.first() as s]
      InterpretSource [#Packet(s.Data) as p]
  Sort [s.Position]
    Project [s.Position as Position, p.ParsedFields as ParsedFields, p.ErrorField as ErrorField, p.BytesConsumed as BytesConsumed]
      CteRef [sp as sp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [s.Position as s.Position, p.ParsedFields as p.ParsedFields, p.ErrorField as p.ErrorField, p.BytesConsumed as p.BytesConsumed]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#schema.first() as s]
      PhysicalInterpretSource [#Packet(s.Data) as p]
  PhysicalSort [s.Position]
    PhysicalProject [s.Position as Position, p.ParsedFields as ParsedFields, p.ErrorField as ErrorField, p.BytesConsumed as BytesConsumed]
      PhysicalCteRef [sp as sp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [s: PartialBytes]
      Position: int <- property Position
      Data: byte[] <- property Data
    SourceEntity [p: object]
      ParsedFields: object <- property ParsedFields
      ErrorField: string <- property ErrorField
      ErrorMessage: string <- property ErrorMessage
      BytesConsumed: int <- property BytesConsumed
      CompletedFields: IReadOnlySet<string> <- property CompletedFields
      IsSuccess: bool <- property IsSuccess
      BitOffset: int <- property BitOffset
    Generated [Statement0Row0]
      s.Position: int <- field s_Position
      p.ParsedFields: object <- field p_ParsedFields
      p.ErrorField: string <- field p_ErrorField
      p.BytesConsumed: int <- field p_BytesConsumed
    TableRow [sp]
      s.Position: int <- field s_Position
      p.ParsedFields: object <- field p_ParsedFields
      p.ErrorField: string <- field p_ErrorField
      p.BytesConsumed: int <- field p_BytesConsumed
    Generated [ResultRow0]
      Position: int <- field Position
      ParsedFields: object <- field ParsedFields
      ErrorField: string <- field ErrorField
      BytesConsumed: int <- field BytesConsumed

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [s: PartialBytes] -> statement0_sRows
    CreateTable [statement0: Statement0Row0]
    PhaseBoundary [Select]
    ChunkedForEach [s in statement0_sRows]
      Let [sPosition: int = s.Position]
      InterpretSource [Packet.PartialInterpret(s.Data) -> statement0_pRows]
      ScalarForEach [p in statement0_pRows]
        AppendRow [statement0 <- Statement0Row0(s.Position: sPosition, p.ParsedFields: p.ParsedFields, p.ErrorField: p.ErrorField, p.BytesConsumed: p.BytesConsumed)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [sp in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Position: sp.s.Position, ParsedFields: sp.p.ParsedFields, ErrorField: sp.p.ErrorField, BytesConsumed: sp.p.BytesConsumed)]
    SortOwnedBuffer [result by Position ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
// === SYNTAX TREE 0 ===
namespace GeneratedSample_P58_TypedPartialPayload_Full
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
            new Column("Position", typeof(int), 0),
            new Column("ParsedFields", typeof(Musoq.Generated.Interpreters.Packet), 1),
            new Column("ErrorField", typeof(string), 2),
            new Column("BytesConsumed", typeof(int), 3)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("s.Position", typeof(int), 0),
            new Column("p.ParsedFields", typeof(Musoq.Generated.Interpreters.Packet), 1),
            new Column("p.ErrorField", typeof(string), 2),
            new Column("p.BytesConsumed", typeof(int), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_s_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Data", typeof(byte[]), 1) });
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
            var __musoqMaterializedTable = QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
            _ = __musoqMaterializedTable.Count;
            return __musoqMaterializedTable;
        }

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            ArgumentNullException.ThrowIfNull(profileRecorder);
            var __musoqMaterializedTable = QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0_Profiled(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken, profileRecorder), token);
            _ = __musoqMaterializedTable.Count;
            return __musoqMaterializedTable;
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.ParsedFields, __musoqShapeRow.ErrorField, __musoqShapeRow.BytesConsumed);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.ParsedFields, __musoqShapeRow.ErrorField, __musoqShapeRow.BytesConsumed);
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

                    Statement0Row0 sp = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(sp.s_Position, sp.p_ParsedFields, sp.p_ErrorField, sp.p_BytesConsumed) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Position.CompareTo(right.Position);
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
                    var __op34Handle = profileRecorder?.GetOperatorHandle("op34", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op41Handle = profileRecorder?.GetOperatorHandle("op41", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op42Handle = profileRecorder?.GetOperatorHandle("op42", "ForEach") ?? OperatorProfileHandle.None;
                    var __op43Handle = profileRecorder?.GetOperatorHandle("op43", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op44Handle = profileRecorder?.GetOperatorHandle("op44", "SortOwnedBuffer") ?? OperatorProfileHandle.None;
                    long __op43OutputRows = 0L;
                    var __op30Scope = profileRecorder?.BeginOperatorValue(__op30Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op30Scope.Dispose();
                    var __op31Scope = profileRecorder?.BeginOperatorValue(__op31Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op31Scope.Dispose();
                    var __op34Scope = profileRecorder?.BeginOperatorValue(__op34Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op34Scope.Dispose();
                    _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, profileRecorder, _cteRowResults);
                    var __op41Scope = profileRecorder?.BeginOperatorValue(__op41Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>();
                    __op41Scope.Dispose();
                    long __op42InputRows = 0L;
                    long __op42OutputRows = 0L;
                    var __op42Scope = profileRecorder?.BeginOperatorValue(__op42Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        var __storedTable0Rows = _cteRowResults.Slot0;
                        for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                        {
                            if ((__storedTable0Index & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Statement0Row0 sp = __storedTable0Rows[__storedTable0Index];
                            __op42InputRows++;
                            __op42OutputRows++;
                            result.Add(new ResultShape0(sp.s_Position, sp.p_ParsedFields, sp.p_ErrorField, sp.p_BytesConsumed) { __musoqSequence = result.Count });
                            __op43OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op42Scope.AddInputRows(__op42InputRows);
                        __op42Scope.AddOutputRows(__op42OutputRows);
                        __op42Scope.Dispose();
                    }

                    var __op44Scope = profileRecorder?.BeginOperatorValue(__op44Handle) ?? OperatorProfileValueScope.None;
                    __op44Scope.AddInputRows(result.Count);
                    result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                    {
                        var comparison = left.Position.CompareTo(right.Position);
                        if (comparison != 0)
                            return comparison;
                        return left.__musoqSequence.CompareTo(right.__musoqSequence);
                    }));
                    __op44Scope.AddOutputRows(result.Count);
                    __op44Scope.Dispose();
                    if (__op43OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op43Handle, __op43OutputRows);
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            var __statement0_sSchema = provider.GetSchema("#schema");
            var statement0_sRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.PartialBytes>.Open(__statement0_sSchema, "first", new SourceExecutionContext("s:1", sourceExecutionPlans["s:1"], token, __schemaColumns_compiled_s_0, sourceRuntimeSettingsBySourceContextId["s:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_sRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.PartialBytes>(statement0_sRowsSource.Chunks, __musoqProgressContext, "s:1") : statement0_sRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            foreach (var sChunk in statement0_sRows)
            {
                if (sChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.PartialBytes> sChunkView)
                {
                    if (sChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.PartialBytes[] sChunkViewArray)
                    {
                        int sChunkViewOffset = sChunkView.Offset;
                        for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                        {
                            if ((sIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var s = sChunkViewArray[sChunkViewOffset + sIndex];
                            int sPosition = s.Position;
                            var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                            var statement0_pRows = _interpreter_Packet.PartialInterpret(s.Data);
                            if (statement0_pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = statement0_pRows;
                                statement0.Add(new Statement0Row0(sPosition, p.ParsedFields, p.ErrorField, p.BytesConsumed));
                            }
                        }

                        continue;
                    }

                    if (sChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.PartialBytes> sChunkViewList)
                    {
                        int sChunkViewOffset = sChunkView.Offset;
                        for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                        {
                            if ((sIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var s = sChunkViewList[sChunkViewOffset + sIndex];
                            int sPosition = s.Position;
                            var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                            var statement0_pRows = _interpreter_Packet.PartialInterpret(s.Data);
                            if (statement0_pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = statement0_pRows;
                                statement0.Add(new Statement0Row0(sPosition, p.ParsedFields, p.ErrorField, p.BytesConsumed));
                            }
                        }

                        continue;
                    }
                }

                for (int sIndex = 0, sIndexCount = sChunk.Count; sIndex < sIndexCount; ++sIndex)
                {
                    if ((sIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var s = sChunk[sIndex];
                    int sPosition = s.Position;
                    var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                    var statement0_pRows = _interpreter_Packet.PartialInterpret(s.Data);
                    if (statement0_pRows != null)
                    {
                        token.ThrowIfCancellationRequested();
                        var p = statement0_pRows;
                        statement0.Add(new Statement0Row0(sPosition, p.ParsedFields, p.ErrorField, p.BytesConsumed));
                    }
                }
            }

            return statement0;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, Musoq.Evaluator.Diagnostics.QueryProfileRecorder profileRecorder, CteRowResults _cteRowResults)
        {
            var __profileScopeDepth = profileRecorder?.GetCurrentOperatorScopeDepth() ?? 0;
            try
            {
                var __op32Handle = profileRecorder?.GetOperatorHandle("op32", "SourceScan") ?? OperatorProfileHandle.None;
                var __op33Handle = profileRecorder?.GetOperatorHandle("op33", "CreateTable") ?? OperatorProfileHandle.None;
                var __op35Handle = profileRecorder?.GetOperatorHandle("op35", "ChunkedForEach") ?? OperatorProfileHandle.None;
                var __op37Handle = profileRecorder?.GetOperatorHandle("op37", "InterpretSource") ?? OperatorProfileHandle.None;
                var __op38Handle = profileRecorder?.GetOperatorHandle("op38", "ScalarForEach") ?? OperatorProfileHandle.None;
                var __op39Handle = profileRecorder?.GetOperatorHandle("op39", "AppendRow") ?? OperatorProfileHandle.None;
                long __op39OutputRows = 0L;
                var __op32Scope = profileRecorder?.BeginOperatorValue(__op32Handle) ?? OperatorProfileValueScope.None;
                var __statement0_sSchema = provider.GetSchema("#schema");
                var statement0_sRowsProfile = profileRecorder?.CreateSourceRecorder("s");
                var statement0_sRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.PartialBytes>.Open(__statement0_sSchema, "first", new SourceExecutionContext("s:1", sourceExecutionPlans["s:1"], token, __schemaColumns_compiled_s_0, sourceRuntimeSettingsBySourceContextId["s:1"], logger, OnDataSourceProgress, statement0_sRowsProfile == null ? SourceDiagnostics.None : statement0_sRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                var statement0_sRows = statement0_sRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.PartialBytes>(statement0_sRowsSource.Chunks, __musoqProgressContext, "s:1") : statement0_sRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.PartialBytes>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.PartialBytes>(statement0_sRowsSource.Chunks, __musoqProgressContext, "s:1") : statement0_sRowsSource.Chunks, statement0_sRowsProfile);
                __op32Scope.Dispose();
                var __op33Scope = profileRecorder?.BeginOperatorValue(__op33Handle) ?? OperatorProfileValueScope.None;
                var statement0 = new List<Statement0Row0>();
                __op33Scope.Dispose();
                long __op35InputRows = 0L;
                long __op35OutputRows = 0L;
                var __op35Scope = profileRecorder?.BeginOperatorValue(__op35Handle) ?? OperatorProfileValueScope.None;
                try
                {
                    foreach (var sChunk in statement0_sRows)
                    {
                        if (sChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.PartialBytes> sChunkView)
                        {
                            if (sChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.PartialBytes[] sChunkViewArray)
                            {
                                int sChunkViewOffset = sChunkView.Offset;
                                for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                                {
                                    if ((sIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var s = sChunkViewArray[sChunkViewOffset + sIndex];
                                    __op35InputRows++;
                                    __op35OutputRows++;
                                    int sPosition = s.Position;
                                    var __op37Scope = profileRecorder?.BeginOperatorValue(__op37Handle) ?? OperatorProfileValueScope.None;
                                    var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                                    var statement0_pRows = _interpreter_Packet.PartialInterpret(s.Data);
                                    __op37Scope.Dispose();
                                    long __op38InputRows = 0L;
                                    long __op38OutputRows = 0L;
                                    var __op38Scope = profileRecorder?.BeginOperatorValue(__op38Handle) ?? OperatorProfileValueScope.None;
                                    try
                                    {
                                        if (statement0_pRows != null)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            var p = statement0_pRows;
                                            __op38InputRows++;
                                            __op38OutputRows++;
                                            statement0.Add(new Statement0Row0(sPosition, p.ParsedFields, p.ErrorField, p.BytesConsumed));
                                            __op39OutputRows += 1;
                                        }
                                    }
                                    finally
                                    {
                                        __op38Scope.AddInputRows(__op38InputRows);
                                        __op38Scope.AddOutputRows(__op38OutputRows);
                                        __op38Scope.Dispose();
                                    }
                                }

                                continue;
                            }

                            if (sChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.PartialBytes> sChunkViewList)
                            {
                                int sChunkViewOffset = sChunkView.Offset;
                                for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                                {
                                    if ((sIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var s = sChunkViewList[sChunkViewOffset + sIndex];
                                    __op35InputRows++;
                                    __op35OutputRows++;
                                    int sPosition = s.Position;
                                    var __op37Scope = profileRecorder?.BeginOperatorValue(__op37Handle) ?? OperatorProfileValueScope.None;
                                    var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                                    var statement0_pRows = _interpreter_Packet.PartialInterpret(s.Data);
                                    __op37Scope.Dispose();
                                    long __op38InputRows = 0L;
                                    long __op38OutputRows = 0L;
                                    var __op38Scope = profileRecorder?.BeginOperatorValue(__op38Handle) ?? OperatorProfileValueScope.None;
                                    try
                                    {
                                        if (statement0_pRows != null)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            var p = statement0_pRows;
                                            __op38InputRows++;
                                            __op38OutputRows++;
                                            statement0.Add(new Statement0Row0(sPosition, p.ParsedFields, p.ErrorField, p.BytesConsumed));
                                            __op39OutputRows += 1;
                                        }
                                    }
                                    finally
                                    {
                                        __op38Scope.AddInputRows(__op38InputRows);
                                        __op38Scope.AddOutputRows(__op38OutputRows);
                                        __op38Scope.Dispose();
                                    }
                                }

                                continue;
                            }
                        }

                        for (int sIndex = 0, sIndexCount = sChunk.Count; sIndex < sIndexCount; ++sIndex)
                        {
                            if ((sIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var s = sChunk[sIndex];
                            __op35InputRows++;
                            __op35OutputRows++;
                            int sPosition = s.Position;
                            var __op37Scope = profileRecorder?.BeginOperatorValue(__op37Handle) ?? OperatorProfileValueScope.None;
                            var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                            var statement0_pRows = _interpreter_Packet.PartialInterpret(s.Data);
                            __op37Scope.Dispose();
                            long __op38InputRows = 0L;
                            long __op38OutputRows = 0L;
                            var __op38Scope = profileRecorder?.BeginOperatorValue(__op38Handle) ?? OperatorProfileValueScope.None;
                            try
                            {
                                if (statement0_pRows != null)
                                {
                                    token.ThrowIfCancellationRequested();
                                    var p = statement0_pRows;
                                    __op38InputRows++;
                                    __op38OutputRows++;
                                    statement0.Add(new Statement0Row0(sPosition, p.ParsedFields, p.ErrorField, p.BytesConsumed));
                                    __op39OutputRows += 1;
                                }
                            }
                            finally
                            {
                                __op38Scope.AddInputRows(__op38InputRows);
                                __op38Scope.AddOutputRows(__op38OutputRows);
                                __op38Scope.Dispose();
                            }
                        }
                    }
                }
                finally
                {
                    __op35Scope.AddInputRows(__op35InputRows);
                    __op35Scope.AddOutputRows(__op35OutputRows);
                    __op35Scope.Dispose();
                }

                if (__op39OutputRows > 0L)
                    profileRecorder?.AddOperatorOutputRows(__op39Handle, __op39OutputRows);
                return statement0;
            }
            catch (Exception __profileException)when (profileRecorder != null && profileRecorder.RecordActiveOperatorException(__profileException, __profileScopeDepth))
            {
                profileRecorder.DisposeActiveOperatorScopes(__profileScopeDepth);
                throw;
            }
        }

        private sealed class CteRowResults
        {
            public List<Statement0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, Musoq.Generated.Interpreters.Packet __value1, string __value2, int __value3)
            {
                Position = __value0;
                ParsedFields = __value1;
                ErrorField = __value2;
                BytesConsumed = __value3;
            }

            public int BytesConsumed { get; private set; }
            public override int Count => 4;
            public string ErrorField { get; private set; }
            public Musoq.Generated.Interpreters.Packet ParsedFields { get; private set; }
            public int Position { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, Musoq.Generated.Interpreters.Packet>(this.ParsedFields = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<Musoq.Generated.Interpreters.Packet, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.ErrorField = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.BytesConsumed = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, Musoq.Generated.Interpreters.Packet>(this.ParsedFields),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.ErrorField),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.BytesConsumed),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "ParsedFields" => true,
                "ErrorField" => true,
                "BytesConsumed" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.ParsedFields, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.ErrorField, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.BytesConsumed, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Position, Musoq.Generated.Interpreters.Packet ParsedFields, string ErrorField, int BytesConsumed)
            {
                this.Position = Position;
                this.ParsedFields = ParsedFields;
                this.ErrorField = ErrorField;
                this.BytesConsumed = BytesConsumed;
            }

            public int BytesConsumed { get; }
            public string ErrorField { get; }
            public Musoq.Generated.Interpreters.Packet ParsedFields { get; }
            public int Position { get; }
            public int __musoqSequence { get; init; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(int __value0, Musoq.Generated.Interpreters.Packet __value1, string __value2, int __value3)
            {
                s_Position = __value0;
                p_ParsedFields = __value1;
                p_ErrorField = __value2;
                p_BytesConsumed = __value3;
            }

            public int p_BytesConsumed { get; }
            public string p_ErrorField { get; }
            public Musoq.Generated.Interpreters.Packet p_ParsedFields { get; }
            public int s_Position { get; }
        }
    }
} // === SYNTAX TREE 1 ===
// <auto-generated>
// This code was generated by Musoq Interpretation Schema code generator.
// Do not modify this file directly.
// </auto-generated>
#nullable enable
using  System ;  using  System . Buffers . Binary ;  using  System . Collections . Generic ;  using  System . Dynamic ;  using  Musoq . Schema . Interpreters ; 

namespace Musoq.Generated.Interpreters
{
    /// <summary>
    /// Generated interpreter for binary schema 'Packet'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Packet : BytesInterpreterBase<Packet>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Packet value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Amount"))
            {
                writer.WritePropertyName("Amount");
                var fieldValue = this.Amount;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Currency"))
            {
                writer.WritePropertyName("Currency");
                var fieldValue = this.Currency;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Amount field value.</summary>
        public int Amount { get; set; }
        /// <summary>Gets the Currency field value.</summary>
        public byte Currency { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Packet";

        /// <inheritdoc/>
        public override Packet InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new Packet();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Amount");
            var _amount = ReadInt32Le(data);
            __parsedValue.Amount = _amount;
            RecordParsedField("Amount");
            SetCurrentField("Currency");
            var _currency = ReadByte(data);
            __parsedValue.Currency = _currency;
            RecordParsedField("Currency");
            return __parsedValue;
        }
    }
}
