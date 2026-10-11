// === Parsed Query ===
/*
select Position, Concat(Label, Value) as Pair, Concat(Label, ':', Value) as Triple, Concat(Label, ':', Value, '!') as Four from #schema.first() order by Position
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [ko3iko.Position]
    Project [ko3iko.Position as Position, Concat(ko3iko.Label, ko3iko.Value) as Pair, Concat(ko3iko.Label, ':', ko3iko.Value) as Triple, Concat(ko3iko.Label, ':', ko3iko.Value, '!') as Four]
      SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [ko3iko.Position]
    PhysicalProject [ko3iko.Position as Position, Concat(ko3iko.Label, ko3iko.Value) as Pair, Concat(ko3iko.Label, ':', ko3iko.Value) as Triple, Concat(ko3iko.Label, ':', ko3iko.Value, '!') as Four]
      PhysicalSchemaScan [#schema.first() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: ConcatInput]
      Position: int <- property Position
      Label: string <- property Label
      Value: int? <- property Value
    GeneratedRecord [ResultRow0WithSortKeys]
      Position: int <- field Position
      Pair: string <- field Pair
      Triple: string <- field Triple
      Four: string <- field Four
      __ordinal: int <- field __ordinal
    Generated [ResultRow0]
      Position: int <- field Position
      Pair: string <- field Pair
      Triple: string <- field Triple
      Four: string <- field Four

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: ConcatInput] -> ko3ikoRows
    CreateRecordList [resultOrderRecords: ResultRow0WithSortKeys]
    CreateObject [__resultOrderRecordsLibraryBase0: LibraryBase]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [label: string = ko3iko.Label]
      Let [value: int? = ko3iko.Value]
      AppendRecord [resultOrderRecords <- ResultRow0WithSortKeys(Position: ko3iko.Position, Pair: Concat(label, value), Triple: Concat(label, ':', value), Four: Concat(label, ':', value, '!'))]
    OrderRecordList [resultOrderRecords: ResultRow0WithSortKeys by Position ASC]
    MaterializeRecordListToShapeRows [resultOrderRecords -> result: ResultShape0 fields 0, 1, 2, 3]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P68_TypedFixedArityConcat_Full
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Position", typeof(int), 0),
            new Column("Pair", typeof(string), 1),
            new Column("Triple", typeof(string), 2),
            new Column("Four", typeof(string), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Label", typeof(string), 1), new Column("Value", typeof(int?), 2) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            ArgumentNullException.ThrowIfNull(profileRecorder);
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0_Profiled(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken, profileRecorder), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Pair, __musoqShapeRow.Triple, __musoqShapeRow.Four);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Pair, __musoqShapeRow.Triple, __musoqShapeRow.Four);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#schema");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionConcatQueryTests.ConcatInput>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionConcatQueryTests.ConcatInput>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var resultOrderRecords = new List<ResultRow0WithSortKeys>();
                var __resultOrderRecordsLibraryBase0 = new Musoq.Plugins.LibraryBase();
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionConcatQueryTests.ConcatInput> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionConcatQueryTests.ConcatInput[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                string label = ko3iko.Label;
                                int? value = ko3iko.Value;
                                resultOrderRecords.Add(new ResultRow0WithSortKeys(ko3iko.Position, (string)__resultOrderRecordsLibraryBase0.Concat<string, int?>(label, value), (string)__resultOrderRecordsLibraryBase0.Concat<string, string, int?>(label, ":", value), (string)__resultOrderRecordsLibraryBase0.Concat<string, string, int?, string>(label, ":", value, "!"), resultOrderRecords.Count));
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionConcatQueryTests.ConcatInput> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                string label = ko3iko.Label;
                                int? value = ko3iko.Value;
                                resultOrderRecords.Add(new ResultRow0WithSortKeys(ko3iko.Position, (string)__resultOrderRecordsLibraryBase0.Concat<string, int?>(label, value), (string)__resultOrderRecordsLibraryBase0.Concat<string, string, int?>(label, ":", value), (string)__resultOrderRecordsLibraryBase0.Concat<string, string, int?, string>(label, ":", value, "!"), resultOrderRecords.Count));
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
                        string label = ko3iko.Label;
                        int? value = ko3iko.Value;
                        resultOrderRecords.Add(new ResultRow0WithSortKeys(ko3iko.Position, (string)__resultOrderRecordsLibraryBase0.Concat<string, int?>(label, value), (string)__resultOrderRecordsLibraryBase0.Concat<string, string, int?>(label, ":", value), (string)__resultOrderRecordsLibraryBase0.Concat<string, string, int?, string>(label, ":", value, "!"), resultOrderRecords.Count));
                    }
                }

                resultOrderRecords.Sort(ResultRow0WithSortKeysComparer.Instance);
                foreach (var resultRecord in resultOrderRecords)
                {
                    __musoqFinalShapeRows.Add(new ResultShape0(resultRecord.Position, resultRecord.Pair, resultRecord.Triple, resultRecord.Four));
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
                    var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                    ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                    var __musoqFinalShapeRows = new List<ResultShape0>();
                    var __op19Handle = profileRecorder?.GetOperatorHandle("op19", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op20Handle = profileRecorder?.GetOperatorHandle("op20", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op21Handle = profileRecorder?.GetOperatorHandle("op21", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op22Handle = profileRecorder?.GetOperatorHandle("op22", "CreateRecordList") ?? OperatorProfileHandle.None;
                    var __op23Handle = profileRecorder?.GetOperatorHandle("op23", "CreateObject") ?? OperatorProfileHandle.None;
                    var __op24Handle = profileRecorder?.GetOperatorHandle("op24", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op25Handle = profileRecorder?.GetOperatorHandle("op25", "ChunkedForEach") ?? OperatorProfileHandle.None;
                    var __op28Handle = profileRecorder?.GetOperatorHandle("op28", "AppendRecord") ?? OperatorProfileHandle.None;
                    var __op29Handle = profileRecorder?.GetOperatorHandle("op29", "OrderRecordList") ?? OperatorProfileHandle.None;
                    var __op30Handle = profileRecorder?.GetOperatorHandle("op30", "MaterializeRecordListToShapeRows") ?? OperatorProfileHandle.None;
                    long __op28OutputRows = 0L;
                    var __op19Scope = profileRecorder?.BeginOperatorValue(__op19Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op19Scope.Dispose();
                    var __op20Scope = profileRecorder?.BeginOperatorValue(__op20Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op20Scope.Dispose();
                    var __op21Scope = profileRecorder?.BeginOperatorValue(__op21Handle) ?? OperatorProfileValueScope.None;
                    var __ko3ikoSchema = provider.GetSchema("#schema");
                    var ko3ikoRowsProfile = profileRecorder?.CreateSourceRecorder("ko3iko");
                    var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionConcatQueryTests.ConcatInput>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress, ko3ikoRowsProfile == null ? SourceDiagnostics.None : ko3ikoRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                    var ko3ikoRows = ko3ikoRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionConcatQueryTests.ConcatInput>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionConcatQueryTests.ConcatInput>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionConcatQueryTests.ConcatInput>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks, ko3ikoRowsProfile);
                    __op21Scope.Dispose();
                    var __op22Scope = profileRecorder?.BeginOperatorValue(__op22Handle) ?? OperatorProfileValueScope.None;
                    var resultOrderRecords = new List<ResultRow0WithSortKeys>();
                    __op22Scope.Dispose();
                    var __op23Scope = profileRecorder?.BeginOperatorValue(__op23Handle) ?? OperatorProfileValueScope.None;
                    var __resultOrderRecordsLibraryBase0 = new Musoq.Plugins.LibraryBase();
                    __op23Scope.Dispose();
                    var __op24Scope = profileRecorder?.BeginOperatorValue(__op24Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op24Scope.Dispose();
                    long __op25InputRows = 0L;
                    long __op25OutputRows = 0L;
                    var __op25Scope = profileRecorder?.BeginOperatorValue(__op25Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        foreach (var ko3ikoChunk in ko3ikoRows)
                        {
                            if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionConcatQueryTests.ConcatInput> ko3ikoChunkView)
                            {
                                if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionConcatQueryTests.ConcatInput[] ko3ikoChunkViewArray)
                                {
                                    int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                                    for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                                    {
                                        if ((ko3ikoIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                        __op25InputRows++;
                                        __op25OutputRows++;
                                        string label = ko3iko.Label;
                                        int? value = ko3iko.Value;
                                        resultOrderRecords.Add(new ResultRow0WithSortKeys(ko3iko.Position, (string)__resultOrderRecordsLibraryBase0.Concat<string, int?>(label, value), (string)__resultOrderRecordsLibraryBase0.Concat<string, string, int?>(label, ":", value), (string)__resultOrderRecordsLibraryBase0.Concat<string, string, int?, string>(label, ":", value, "!"), resultOrderRecords.Count));
                                        __op28OutputRows += 1;
                                    }

                                    continue;
                                }

                                if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionConcatQueryTests.ConcatInput> ko3ikoChunkViewList)
                                {
                                    int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                                    for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                                    {
                                        if ((ko3ikoIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                        __op25InputRows++;
                                        __op25OutputRows++;
                                        string label = ko3iko.Label;
                                        int? value = ko3iko.Value;
                                        resultOrderRecords.Add(new ResultRow0WithSortKeys(ko3iko.Position, (string)__resultOrderRecordsLibraryBase0.Concat<string, int?>(label, value), (string)__resultOrderRecordsLibraryBase0.Concat<string, string, int?>(label, ":", value), (string)__resultOrderRecordsLibraryBase0.Concat<string, string, int?, string>(label, ":", value, "!"), resultOrderRecords.Count));
                                        __op28OutputRows += 1;
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
                                __op25InputRows++;
                                __op25OutputRows++;
                                string label = ko3iko.Label;
                                int? value = ko3iko.Value;
                                resultOrderRecords.Add(new ResultRow0WithSortKeys(ko3iko.Position, (string)__resultOrderRecordsLibraryBase0.Concat<string, int?>(label, value), (string)__resultOrderRecordsLibraryBase0.Concat<string, string, int?>(label, ":", value), (string)__resultOrderRecordsLibraryBase0.Concat<string, string, int?, string>(label, ":", value, "!"), resultOrderRecords.Count));
                                __op28OutputRows += 1;
                            }
                        }
                    }
                    finally
                    {
                        __op25Scope.AddInputRows(__op25InputRows);
                        __op25Scope.AddOutputRows(__op25OutputRows);
                        __op25Scope.Dispose();
                    }

                    var __op29Scope = profileRecorder?.BeginOperatorValue(__op29Handle) ?? OperatorProfileValueScope.None;
                    __op29Scope.AddInputRows(resultOrderRecords.Count);
                    resultOrderRecords.Sort(ResultRow0WithSortKeysComparer.Instance);
                    __op29Scope.AddOutputRows(resultOrderRecords.Count);
                    __op29Scope.Dispose();
                    var __op30Scope = profileRecorder?.BeginOperatorValue(__op30Handle) ?? OperatorProfileValueScope.None;
                    __op30Scope.AddInputRows(resultOrderRecords.Count);
                    foreach (var resultRecord in resultOrderRecords)
                    {
                        __musoqFinalShapeRows.Add(new ResultShape0(resultRecord.Position, resultRecord.Pair, resultRecord.Triple, resultRecord.Four));
                    }

                    __op30Scope.AddOutputRows(__musoqFinalShapeRows.Count);
                    __op30Scope.Dispose();
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, string __value1, string __value2, string __value3)
            {
                Position = __value0;
                Pair = __value1;
                Triple = __value2;
                Four = __value3;
            }

            public override int Count => 4;
            public string Four { get; private set; }
            public string Pair { get; private set; }
            public int Position { get; private set; }
            public string Triple { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Pair = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Triple = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Four = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Pair),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Triple),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Four),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "Pair" => true,
                "Triple" => true,
                "Four" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Pair, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Triple, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Four, ref visitor);
            }
        }

        private sealed class ResultRow0WithSortKeys
        {
            public ResultRow0WithSortKeys(int Position, string Pair, string Triple, string Four, int __ordinal)
            {
                this.Position = Position;
                this.Pair = Pair;
                this.Triple = Triple;
                this.Four = Four;
                this.__ordinal = __ordinal;
            }

            public string Four { get; }
            public string Pair { get; }
            public int Position { get; }
            public string Triple { get; }
            public int __ordinal { get; }
        }

        private sealed class ResultRow0WithSortKeysComparer : IComparer<ResultRow0WithSortKeys>
        {
            public static readonly ResultRow0WithSortKeysComparer Instance = new ResultRow0WithSortKeysComparer();
            public int Compare(ResultRow0WithSortKeys left, ResultRow0WithSortKeys right)
            {
                var comparison = left.Position.CompareTo(right.Position);
                if (comparison != 0)
                    return comparison;
                return left.__ordinal.CompareTo(right.__ordinal);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Position, string Pair, string Triple, string Four)
            {
                this.Position = Position;
                this.Pair = Pair;
                this.Triple = Triple;
                this.Four = Four;
            }

            public string Four { get; }
            public string Pair { get; }
            public int Position { get; }
            public string Triple { get; }
        }
    }
}
