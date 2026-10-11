// === Parsed Query ===
/*
select p.Position as Position, p.Payload.Value as Value from #positional.all() p order by Position
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [p.Position]
    Project [p.Position as Position, p.Payload.Value as Value]
      SchemaScan [#positional.all() as p]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [p.Position]
    PhysicalProject [p.Position as Position, p.Payload.Value as Value]
      PhysicalSchemaScan [#positional.all() as p] [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=20408C8CC7AD3F550FE9461437258166D139CC6CD34102E1904B151639A0E63D]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    Generated [QueryRow_20408C8CC7AD_S]
      Payload: NativePayload <- field Field0
      Position: int <- field Field1
    Generated [ResultRow0]
      Position: int <- field Position
      Value: decimal? <- field Value

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [p: QueryRow_20408C8CC7AD_S] -> pRows [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=20408C8CC7AD3F550FE9461437258166D139CC6CD34102E1904B151639A0E63D]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [p in pRows]
      AppendShape [result <- ResultShape0(Position: p.Position, Value: p.Payload?.Value)]
    SortShapeRows [result -> resultSorted by Position ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P73_NativeSourceNullablePayload_Full
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
            new Column("Value", typeof(decimal?), 1)
        };
        private static readonly QueryRowShape __queryRowShape_20408C8CC7AD = new QueryRowShape(new QueryRowField[] { new QueryRowField(0, 0, "Payload", typeof(Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativePayload), true), new QueryRowField(1, 1, "Position", typeof(int), false) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_p_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Payload", typeof(Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativePayload), 0), new Column("Position", typeof(int), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            ArgumentNullException.ThrowIfNull(profileRecorder);
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0_Profiled(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken, profileRecorder), token);
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
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __pSchema = provider.GetSchema("#positional");
                var __pSchemaQueryRows = __pSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#positional.all' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape 20408C8CC7AD3F550FE9461437258166D139CC6CD34102E1904B151639A0E63D).");
                var pRowsSource = QuerySourceInvocation<QueryRow_20408C8CC7AD_S, QueryRowMaterializer_20408C8CC7AD_S>.Open(__pSchemaQueryRows, "all", new QueryScopedRowSourceRequest(new SourceExecutionContext("p:1", sourceExecutionPlans["p:1"], token, __schemaColumns_compiled_p_0, sourceRuntimeSettingsBySourceContextId["p:1"], logger, OnDataSourceProgress), __queryRowShape_20408C8CC7AD), global::Musoq.Schema.SourceArguments.Empty);
                var pRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_20408C8CC7AD_S>(pRowsSource.Chunks, __musoqProgressContext, "p:1") : pRowsSource.Chunks;
                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var pChunk in pRows)
                {
                    if (pChunk is global::Musoq.Schema.DataSources.RowChunk<QueryRow_20408C8CC7AD_S> pChunkView)
                    {
                        if (pChunkView.Source is QueryRow_20408C8CC7AD_S[] pChunkViewArray)
                        {
                            int pChunkViewOffset = pChunkView.Offset;
                            for (int pIndex = 0, pIndexCount = pChunkView.Count; pIndex < pIndexCount; ++pIndex)
                            {
                                if ((pIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var p = pChunkViewArray[pChunkViewOffset + pIndex];
                                result.Add(new ResultShape0(p.Field1, p.Field0?.Value));
                            }

                            continue;
                        }

                        if (pChunkView.Source is List<QueryRow_20408C8CC7AD_S> pChunkViewList)
                        {
                            int pChunkViewOffset = pChunkView.Offset;
                            for (int pIndex = 0, pIndexCount = pChunkView.Count; pIndex < pIndexCount; ++pIndex)
                            {
                                if ((pIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var p = pChunkViewList[pChunkViewOffset + pIndex];
                                result.Add(new ResultShape0(p.Field1, p.Field0?.Value));
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
                        result.Add(new ResultShape0(p.Field1, p.Field0?.Value));
                    }
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
                    var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                    ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                    var __musoqFinalShapeRows = new List<ResultShape0>();
                    var __op10Handle = profileRecorder?.GetOperatorHandle("op10", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op11Handle = profileRecorder?.GetOperatorHandle("op11", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op12Handle = profileRecorder?.GetOperatorHandle("op12", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op13Handle = profileRecorder?.GetOperatorHandle("op13", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op14Handle = profileRecorder?.GetOperatorHandle("op14", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op15Handle = profileRecorder?.GetOperatorHandle("op15", "ChunkedForEach") ?? OperatorProfileHandle.None;
                    var __op16Handle = profileRecorder?.GetOperatorHandle("op16", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op17Handle = profileRecorder?.GetOperatorHandle("op17", "SortShapeRows") ?? OperatorProfileHandle.None;
                    long __op16OutputRows = 0L;
                    var __op10Scope = profileRecorder?.BeginOperatorValue(__op10Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op10Scope.Dispose();
                    var __op11Scope = profileRecorder?.BeginOperatorValue(__op11Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op11Scope.Dispose();
                    var __op12Scope = profileRecorder?.BeginOperatorValue(__op12Handle) ?? OperatorProfileValueScope.None;
                    var __pSchema = provider.GetSchema("#positional");
                    var pRowsProfile = profileRecorder?.CreateSourceRecorder("p");
                    var __pSchemaQueryRows = __pSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#positional.all' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape 20408C8CC7AD3F550FE9461437258166D139CC6CD34102E1904B151639A0E63D).");
                    var pRowsSource = QuerySourceInvocation<QueryRow_20408C8CC7AD_S, QueryRowMaterializer_20408C8CC7AD_S>.Open(__pSchemaQueryRows, "all", new QueryScopedRowSourceRequest(new SourceExecutionContext("p:1", sourceExecutionPlans["p:1"], token, __schemaColumns_compiled_p_0, sourceRuntimeSettingsBySourceContextId["p:1"], logger, OnDataSourceProgress, pRowsProfile == null ? SourceDiagnostics.None : pRowsProfile.CreateDiagnostics()), __queryRowShape_20408C8CC7AD), global::Musoq.Schema.SourceArguments.Empty);
                    var pRows = pRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_20408C8CC7AD_S>(pRowsSource.Chunks, __musoqProgressContext, "p:1") : pRowsSource.Chunks : ProfiledChunkedEnumerable<QueryRow_20408C8CC7AD_S>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_20408C8CC7AD_S>(pRowsSource.Chunks, __musoqProgressContext, "p:1") : pRowsSource.Chunks, pRowsProfile);
                    __op12Scope.Dispose();
                    var __op13Scope = profileRecorder?.BeginOperatorValue(__op13Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>();
                    __op13Scope.Dispose();
                    var __op14Scope = profileRecorder?.BeginOperatorValue(__op14Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op14Scope.Dispose();
                    long __op15InputRows = 0L;
                    long __op15OutputRows = 0L;
                    var __op15Scope = profileRecorder?.BeginOperatorValue(__op15Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        foreach (var pChunk in pRows)
                        {
                            if (pChunk is global::Musoq.Schema.DataSources.RowChunk<QueryRow_20408C8CC7AD_S> pChunkView)
                            {
                                if (pChunkView.Source is QueryRow_20408C8CC7AD_S[] pChunkViewArray)
                                {
                                    int pChunkViewOffset = pChunkView.Offset;
                                    for (int pIndex = 0, pIndexCount = pChunkView.Count; pIndex < pIndexCount; ++pIndex)
                                    {
                                        if ((pIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var p = pChunkViewArray[pChunkViewOffset + pIndex];
                                        __op15InputRows++;
                                        __op15OutputRows++;
                                        result.Add(new ResultShape0(p.Field1, p.Field0?.Value));
                                        __op16OutputRows += 1;
                                    }

                                    continue;
                                }

                                if (pChunkView.Source is List<QueryRow_20408C8CC7AD_S> pChunkViewList)
                                {
                                    int pChunkViewOffset = pChunkView.Offset;
                                    for (int pIndex = 0, pIndexCount = pChunkView.Count; pIndex < pIndexCount; ++pIndex)
                                    {
                                        if ((pIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var p = pChunkViewList[pChunkViewOffset + pIndex];
                                        __op15InputRows++;
                                        __op15OutputRows++;
                                        result.Add(new ResultShape0(p.Field1, p.Field0?.Value));
                                        __op16OutputRows += 1;
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
                                __op15InputRows++;
                                __op15OutputRows++;
                                result.Add(new ResultShape0(p.Field1, p.Field0?.Value));
                                __op16OutputRows += 1;
                            }
                        }
                    }
                    finally
                    {
                        __op15Scope.AddInputRows(__op15InputRows);
                        __op15Scope.AddOutputRows(__op15OutputRows);
                        __op15Scope.Dispose();
                    }

                    var __op17Scope = profileRecorder?.BeginOperatorValue(__op17Handle) ?? OperatorProfileValueScope.None;
                    __op17Scope.AddInputRows(result.Count);
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

                    __op17Scope.AddOutputRows(__musoqFinalShapeRows.Count);
                    __op17Scope.Dispose();
                    if (__op16OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op16Handle, __op16OutputRows);
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

        private readonly struct QueryRowMaterializer_20408C8CC7AD_S : IQueryRowMaterializer<QueryRow_20408C8CC7AD_S>
        {
            public static QueryRow_20408C8CC7AD_S Materialize<TReader>(scoped ref TReader reader)
                where TReader : IQuerySourceFieldReader, allows ref struct => new QueryRow_20408C8CC7AD_S(reader.Read<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativePayload>(0), reader.Read<int>(1));
        }

        private readonly struct QueryRow_20408C8CC7AD_S
        {
            public QueryRow_20408C8CC7AD_S(Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativePayload Field0, int Field1)
            {
                this.Field0 = Field0;
                this.Field1 = Field1;
            }

            public Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativePayload Field0 { get; }
            public int Field1 { get; }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, decimal? __value1)
            {
                Position = __value0;
                Value = __value1;
            }

            public override int Count => 2;
            public int Position { get; private set; }
            public decimal? Value { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Value),
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
            public ResultShape0(int Position, decimal? Value)
            {
                this.Position = Position;
                this.Value = Value;
            }

            public int Position { get; }
            public decimal? Value { get; }
        }
    }
}
