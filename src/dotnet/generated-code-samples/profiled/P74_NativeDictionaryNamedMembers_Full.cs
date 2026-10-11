// === Parsed Query ===
/*
select p.Position as Position, p.Signals.Temperature as Temperature from #schema.first() p order by Position
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [p.Position]
    Project [p.Position as Position, p.Signals.Temperature as Temperature]
      SchemaScan [#schema.first() as p]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [p.Position]
    PhysicalProject [p.Position as Position, p.Signals.Temperature as Temperature]
      PhysicalSchemaScan [#schema.first() as p]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [p: NativeSignalInput]
      Position: int <- property Position
      Signals: IReadOnlyDictionary<string, double?> <- property Signals
    GeneratedRecord [ResultRow0WithSortKeys]
      Position: int <- field Position
      Temperature: double? <- field Temperature
      __ordinal: int <- field __ordinal
    Generated [ResultRow0]
      Position: int <- field Position
      Temperature: double? <- field Temperature

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [p: NativeSignalInput] -> pRows
    CreateRecordList [resultOrderRecords: ResultRow0WithSortKeys]
    PhaseBoundary [Select]
    ChunkedForEach [p in pRows]
      AppendRecord [resultOrderRecords <- ResultRow0WithSortKeys(Position: p.Position, Temperature: GetTypedDictionaryValue(p.Signals, 'Temperature'))]
    OrderRecordList [resultOrderRecords: ResultRow0WithSortKeys by Position ASC]
    MaterializeRecordListToShapeRows [resultOrderRecords -> result: ResultShape0 fields 0, 1]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P74_NativeDictionaryNamedMembers_Full
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
            new Column("Temperature", typeof(double?), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_p_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Signals", typeof(IReadOnlyDictionary<string, double?>), 1) });
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
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Temperature);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Temperature);
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
                var __pSchema = provider.GetSchema("#schema");
                var pRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeSignalInput>.Open(__pSchema, "first", new SourceExecutionContext("p:1", sourceExecutionPlans["p:1"], token, __schemaColumns_compiled_p_0, sourceRuntimeSettingsBySourceContextId["p:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var pRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeSignalInput>(pRowsSource.Chunks, __musoqProgressContext, "p:1") : pRowsSource.Chunks;
                var resultOrderRecords = new List<ResultRow0WithSortKeys>();
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var pChunk in pRows)
                {
                    if (pChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeSignalInput> pChunkView)
                    {
                        if (pChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeSignalInput[] pChunkViewArray)
                        {
                            int pChunkViewOffset = pChunkView.Offset;
                            for (int pIndex = 0, pIndexCount = pChunkView.Count; pIndex < pIndexCount; ++pIndex)
                            {
                                if ((pIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var p = pChunkViewArray[pChunkViewOffset + pIndex];
                                resultOrderRecords.Add(new ResultRow0WithSortKeys(p.Position, (double?)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedDictionaryValue<IReadOnlyDictionary<string, double?>, string, double?>(p.Signals, "Temperature"), resultOrderRecords.Count));
                            }

                            continue;
                        }

                        if (pChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeSignalInput> pChunkViewList)
                        {
                            int pChunkViewOffset = pChunkView.Offset;
                            for (int pIndex = 0, pIndexCount = pChunkView.Count; pIndex < pIndexCount; ++pIndex)
                            {
                                if ((pIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var p = pChunkViewList[pChunkViewOffset + pIndex];
                                resultOrderRecords.Add(new ResultRow0WithSortKeys(p.Position, (double?)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedDictionaryValue<IReadOnlyDictionary<string, double?>, string, double?>(p.Signals, "Temperature"), resultOrderRecords.Count));
                            }

                            continue;
                        }
                    }

                    for (int pIndex = 0, pIndexCount = pChunk.Count; pIndex < pIndexCount; ++pIndex)
                    {
                        if ((pIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var p = pChunk[pIndex];
                        resultOrderRecords.Add(new ResultRow0WithSortKeys(p.Position, (double?)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedDictionaryValue<IReadOnlyDictionary<string, double?>, string, double?>(p.Signals, "Temperature"), resultOrderRecords.Count));
                    }
                }

                resultOrderRecords.Sort(ResultRow0WithSortKeysComparer.Instance);
                foreach (var resultRecord in resultOrderRecords)
                {
                    __musoqFinalShapeRows.Add(new ResultShape0(resultRecord.Position, resultRecord.Temperature));
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
                    var __op14Handle = profileRecorder?.GetOperatorHandle("op14", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op15Handle = profileRecorder?.GetOperatorHandle("op15", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op16Handle = profileRecorder?.GetOperatorHandle("op16", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op17Handle = profileRecorder?.GetOperatorHandle("op17", "CreateRecordList") ?? OperatorProfileHandle.None;
                    var __op18Handle = profileRecorder?.GetOperatorHandle("op18", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op19Handle = profileRecorder?.GetOperatorHandle("op19", "ChunkedForEach") ?? OperatorProfileHandle.None;
                    var __op20Handle = profileRecorder?.GetOperatorHandle("op20", "AppendRecord") ?? OperatorProfileHandle.None;
                    var __op21Handle = profileRecorder?.GetOperatorHandle("op21", "OrderRecordList") ?? OperatorProfileHandle.None;
                    var __op22Handle = profileRecorder?.GetOperatorHandle("op22", "MaterializeRecordListToShapeRows") ?? OperatorProfileHandle.None;
                    long __op20OutputRows = 0L;
                    var __op14Scope = profileRecorder?.BeginOperatorValue(__op14Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op14Scope.Dispose();
                    var __op15Scope = profileRecorder?.BeginOperatorValue(__op15Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op15Scope.Dispose();
                    var __op16Scope = profileRecorder?.BeginOperatorValue(__op16Handle) ?? OperatorProfileValueScope.None;
                    var __pSchema = provider.GetSchema("#schema");
                    var pRowsProfile = profileRecorder?.CreateSourceRecorder("p");
                    var pRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeSignalInput>.Open(__pSchema, "first", new SourceExecutionContext("p:1", sourceExecutionPlans["p:1"], token, __schemaColumns_compiled_p_0, sourceRuntimeSettingsBySourceContextId["p:1"], logger, OnDataSourceProgress, pRowsProfile == null ? SourceDiagnostics.None : pRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                    var pRows = pRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeSignalInput>(pRowsSource.Chunks, __musoqProgressContext, "p:1") : pRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeSignalInput>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeSignalInput>(pRowsSource.Chunks, __musoqProgressContext, "p:1") : pRowsSource.Chunks, pRowsProfile);
                    __op16Scope.Dispose();
                    var __op17Scope = profileRecorder?.BeginOperatorValue(__op17Handle) ?? OperatorProfileValueScope.None;
                    var resultOrderRecords = new List<ResultRow0WithSortKeys>();
                    __op17Scope.Dispose();
                    var __op18Scope = profileRecorder?.BeginOperatorValue(__op18Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op18Scope.Dispose();
                    long __op19InputRows = 0L;
                    long __op19OutputRows = 0L;
                    var __op19Scope = profileRecorder?.BeginOperatorValue(__op19Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        foreach (var pChunk in pRows)
                        {
                            if (pChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeSignalInput> pChunkView)
                            {
                                if (pChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeSignalInput[] pChunkViewArray)
                                {
                                    int pChunkViewOffset = pChunkView.Offset;
                                    for (int pIndex = 0, pIndexCount = pChunkView.Count; pIndex < pIndexCount; ++pIndex)
                                    {
                                        if ((pIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var p = pChunkViewArray[pChunkViewOffset + pIndex];
                                        __op19InputRows++;
                                        __op19OutputRows++;
                                        resultOrderRecords.Add(new ResultRow0WithSortKeys(p.Position, (double?)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedDictionaryValue<IReadOnlyDictionary<string, double?>, string, double?>(p.Signals, "Temperature"), resultOrderRecords.Count));
                                        __op20OutputRows += 1;
                                    }

                                    continue;
                                }

                                if (pChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeSignalInput> pChunkViewList)
                                {
                                    int pChunkViewOffset = pChunkView.Offset;
                                    for (int pIndex = 0, pIndexCount = pChunkView.Count; pIndex < pIndexCount; ++pIndex)
                                    {
                                        if ((pIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var p = pChunkViewList[pChunkViewOffset + pIndex];
                                        __op19InputRows++;
                                        __op19OutputRows++;
                                        resultOrderRecords.Add(new ResultRow0WithSortKeys(p.Position, (double?)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedDictionaryValue<IReadOnlyDictionary<string, double?>, string, double?>(p.Signals, "Temperature"), resultOrderRecords.Count));
                                        __op20OutputRows += 1;
                                    }

                                    continue;
                                }
                            }

                            for (int pIndex = 0, pIndexCount = pChunk.Count; pIndex < pIndexCount; ++pIndex)
                            {
                                if ((pIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var p = pChunk[pIndex];
                                __op19InputRows++;
                                __op19OutputRows++;
                                resultOrderRecords.Add(new ResultRow0WithSortKeys(p.Position, (double?)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedDictionaryValue<IReadOnlyDictionary<string, double?>, string, double?>(p.Signals, "Temperature"), resultOrderRecords.Count));
                                __op20OutputRows += 1;
                            }
                        }
                    }
                    finally
                    {
                        __op19Scope.AddInputRows(__op19InputRows);
                        __op19Scope.AddOutputRows(__op19OutputRows);
                        __op19Scope.Dispose();
                    }

                    var __op21Scope = profileRecorder?.BeginOperatorValue(__op21Handle) ?? OperatorProfileValueScope.None;
                    __op21Scope.AddInputRows(resultOrderRecords.Count);
                    resultOrderRecords.Sort(ResultRow0WithSortKeysComparer.Instance);
                    __op21Scope.AddOutputRows(resultOrderRecords.Count);
                    __op21Scope.Dispose();
                    var __op22Scope = profileRecorder?.BeginOperatorValue(__op22Handle) ?? OperatorProfileValueScope.None;
                    __op22Scope.AddInputRows(resultOrderRecords.Count);
                    foreach (var resultRecord in resultOrderRecords)
                    {
                        __musoqFinalShapeRows.Add(new ResultShape0(resultRecord.Position, resultRecord.Temperature));
                    }

                    __op22Scope.AddOutputRows(__musoqFinalShapeRows.Count);
                    __op22Scope.Dispose();
                    if (__op20OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op20Handle, __op20OutputRows);
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
            public ResultRow0(int __value0, double? __value1)
            {
                Position = __value0;
                Temperature = __value1;
            }

            public override int Count => 2;
            public int Position { get; private set; }
            public double? Temperature { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double?>(this.Temperature = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<double?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double?>(this.Temperature),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "Temperature" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Temperature, ref visitor);
            }
        }

        private sealed class ResultRow0WithSortKeys
        {
            public ResultRow0WithSortKeys(int Position, double? Temperature, int __ordinal)
            {
                this.Position = Position;
                this.Temperature = Temperature;
                this.__ordinal = __ordinal;
            }

            public int Position { get; }
            public double? Temperature { get; }
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
            public ResultShape0(int Position, double? Temperature)
            {
                this.Position = Position;
                this.Temperature = Temperature;
            }

            public int Position { get; }
            public double? Temperature { get; }
        }
    }
}
