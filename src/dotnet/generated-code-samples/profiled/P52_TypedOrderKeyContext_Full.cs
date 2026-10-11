// === Parsed Query ===
/*
select Position, KeyWeighted(Value) over (order by Position rows between 1 preceding and current row) as Total from #schema.first() order by Position
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [ko3iko.Position]
    Project [ko3iko.Position as Position, WindowRef(0) as Total]
      Window [KeyWeighted(idx:0; order: ko3iko.Position; args: ko3iko.Value; frame: rows between 1 preceding and current row)]
        SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [ko3iko.Position]
    PhysicalProject [ko3iko.Position as Position, WindowRef(0) as Total]
      PhysicalWindow [KeyWeighted(idx:0; order: ko3iko.Position; args: ko3iko.Value; frame: rows between 1 preceding and current row)]
        PhysicalMaterialize
          PhysicalSchemaScan [#schema.first() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: NumericRow<int>]
      Position: int <- property Position
      Value: int? <- property Value
    Generated [ResultRow0]
      Position: int <- field Position
      Total: decimal? <- field Total

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: NumericRow<int>] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    ComputeKeyWeightedWindow [resultKeyWeighteds <- resultWindowRows value ko3iko.Value order by ko3iko.Position ASC frame rows between 1 preceding and current row]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultShape0(Position: ko3iko.Position, Total: resultKeyWeighteds[windowIndex])]
    SortShapeRows [result -> resultSorted by Position ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P52_TypedOrderKeyContext_Full
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
            new Column("Total", typeof(decimal?), 1)
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
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Total);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Total);
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
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.NumericRow<int>>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.NumericRow<int>>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                var resultKeyWeightedsOrderKeys = new WindowResultKeyWeightedsOrderKeysKey[resultWindowRows.Count];
                var resultKeyWeightedsValues = new int? [resultWindowRows.Count];
                var resultKeyWeightedsOrderValues = new int[resultWindowRows.Count];
                for (int resultKeyWeightedsExtractionIndex = 0; resultKeyWeightedsExtractionIndex < resultWindowRows.Count; ++resultKeyWeightedsExtractionIndex)
                {
                    Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.NumericRow<int> ko3iko = resultWindowRows[resultKeyWeightedsExtractionIndex];
                    resultKeyWeightedsOrderKeys[resultKeyWeightedsExtractionIndex] = new WindowResultKeyWeightedsOrderKeysKey(ko3iko.Position);
                    resultKeyWeightedsOrderValues[resultKeyWeightedsExtractionIndex] = (int)resultKeyWeightedsOrderKeys[resultKeyWeightedsExtractionIndex].RawValues;
                    resultKeyWeightedsValues[resultKeyWeightedsExtractionIndex] = (int?)(int?)ko3iko.Value;
                }

                var resultKeyWeightedsPartitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultKeyWeightedsPartitions, resultKeyWeightedsOrderKeys, false);
                var resultKeyWeighteds = new decimal? [resultWindowRows.Count];
                for (int resultKeyWeightedsPartition = 0; resultKeyWeightedsPartition < resultKeyWeightedsPartitions.PartitionCount; ++resultKeyWeightedsPartition)
                {
                    var resultKeyWeightedsStart = resultKeyWeightedsPartitions.GetStart(resultKeyWeightedsPartition);
                    var resultKeyWeightedsCount = resultKeyWeightedsPartitions.GetLength(resultKeyWeightedsPartition);
                    var resultKeyWeightedsIndices = resultKeyWeightedsPartitions.Indices;
                    for (int resultKeyWeightedsPosition = 0; resultKeyWeightedsPosition < resultKeyWeightedsCount; ++resultKeyWeightedsPosition)
                    {
                        if ((resultKeyWeightedsPosition & 1023) == 0)
                            token.ThrowIfCancellationRequested();
                        var resultKeyWeightedsCurrent = resultKeyWeightedsIndices[resultKeyWeightedsStart + resultKeyWeightedsPosition];
                        var resultKeyWeightedsFrameStart = Math.Max(0, resultKeyWeightedsPosition - 1);
                        var resultKeyWeightedsFrameEnd = resultKeyWeightedsPosition;
                        var resultKeyWeightedsContext = new Musoq.Plugins.WindowFrameContext<int?, int>(resultKeyWeightedsValues, resultKeyWeightedsOrderValues, new System.ReadOnlySpan<int>(resultKeyWeightedsIndices, resultKeyWeightedsStart, resultKeyWeightedsCount), resultKeyWeightedsPosition, resultKeyWeightedsFrameStart, resultKeyWeightedsFrameEnd, token);
                        resultKeyWeighteds[resultKeyWeightedsCurrent] = Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.KeyWeightedKernel.Get(in resultKeyWeightedsContext);
                    }
                }

                var result = new List<ResultShape0>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.NumericRow<int> ko3iko = resultWindowRows[windowIndex];
                    result.Add(new ResultShape0(ko3iko.Position, (decimal?)resultKeyWeighteds[windowIndex]));
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

                OnPhaseChanged("compiled", QueryPhase.Select);
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
                    var __op13Handle = profileRecorder?.GetOperatorHandle("op13", "MaterializeChunked") ?? OperatorProfileHandle.None;
                    var __op14Handle = profileRecorder?.GetOperatorHandle("op14", "ComputeKeyWeightedWindow") ?? OperatorProfileHandle.None;
                    var __op15Handle = profileRecorder?.GetOperatorHandle("op15", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op16Handle = profileRecorder?.GetOperatorHandle("op16", "ForEachIndexed") ?? OperatorProfileHandle.None;
                    var __op17Handle = profileRecorder?.GetOperatorHandle("op17", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op18Handle = profileRecorder?.GetOperatorHandle("op18", "SortShapeRows") ?? OperatorProfileHandle.None;
                    var __op20Handle = profileRecorder?.GetOperatorHandle("op20", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    long __op17OutputRows = 0L;
                    var __op10Scope = profileRecorder?.BeginOperatorValue(__op10Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op10Scope.Dispose();
                    var __op11Scope = profileRecorder?.BeginOperatorValue(__op11Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op11Scope.Dispose();
                    var __op12Scope = profileRecorder?.BeginOperatorValue(__op12Handle) ?? OperatorProfileValueScope.None;
                    var __ko3ikoSchema = provider.GetSchema("#schema");
                    var ko3ikoRowsProfile = profileRecorder?.CreateSourceRecorder("ko3iko");
                    var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.NumericRow<int>>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress, ko3ikoRowsProfile == null ? SourceDiagnostics.None : ko3ikoRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                    var ko3ikoRows = ko3ikoRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.NumericRow<int>>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.NumericRow<int>>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.NumericRow<int>>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks, ko3ikoRowsProfile);
                    __op12Scope.Dispose();
                    var __op13Scope = profileRecorder?.BeginOperatorValue(__op13Handle) ?? OperatorProfileValueScope.None;
                    var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                    __op13Scope.AddOutputRows(resultWindowRows.Count);
                    __op13Scope.Dispose();
                    var __op14Scope = profileRecorder?.BeginOperatorValue(__op14Handle) ?? OperatorProfileValueScope.None;
                    var resultKeyWeightedsOrderKeys = new WindowResultKeyWeightedsOrderKeysKey[resultWindowRows.Count];
                    var resultKeyWeightedsValues = new int? [resultWindowRows.Count];
                    var resultKeyWeightedsOrderValues = new int[resultWindowRows.Count];
                    for (int resultKeyWeightedsExtractionIndex = 0; resultKeyWeightedsExtractionIndex < resultWindowRows.Count; ++resultKeyWeightedsExtractionIndex)
                    {
                        Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.NumericRow<int> ko3iko = resultWindowRows[resultKeyWeightedsExtractionIndex];
                        resultKeyWeightedsOrderKeys[resultKeyWeightedsExtractionIndex] = new WindowResultKeyWeightedsOrderKeysKey(ko3iko.Position);
                        resultKeyWeightedsOrderValues[resultKeyWeightedsExtractionIndex] = (int)resultKeyWeightedsOrderKeys[resultKeyWeightedsExtractionIndex].RawValues;
                        resultKeyWeightedsValues[resultKeyWeightedsExtractionIndex] = (int?)(int?)ko3iko.Value;
                    }

                    var resultKeyWeightedsPartitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                    WindowFunctionHelpers.SortStructPartitionSetInPlace(resultKeyWeightedsPartitions, resultKeyWeightedsOrderKeys, false);
                    var resultKeyWeighteds = new decimal? [resultWindowRows.Count];
                    for (int resultKeyWeightedsPartition = 0; resultKeyWeightedsPartition < resultKeyWeightedsPartitions.PartitionCount; ++resultKeyWeightedsPartition)
                    {
                        var resultKeyWeightedsStart = resultKeyWeightedsPartitions.GetStart(resultKeyWeightedsPartition);
                        var resultKeyWeightedsCount = resultKeyWeightedsPartitions.GetLength(resultKeyWeightedsPartition);
                        var resultKeyWeightedsIndices = resultKeyWeightedsPartitions.Indices;
                        for (int resultKeyWeightedsPosition = 0; resultKeyWeightedsPosition < resultKeyWeightedsCount; ++resultKeyWeightedsPosition)
                        {
                            if ((resultKeyWeightedsPosition & 1023) == 0)
                                token.ThrowIfCancellationRequested();
                            var resultKeyWeightedsCurrent = resultKeyWeightedsIndices[resultKeyWeightedsStart + resultKeyWeightedsPosition];
                            var resultKeyWeightedsFrameStart = Math.Max(0, resultKeyWeightedsPosition - 1);
                            var resultKeyWeightedsFrameEnd = resultKeyWeightedsPosition;
                            var resultKeyWeightedsContext = new Musoq.Plugins.WindowFrameContext<int?, int>(resultKeyWeightedsValues, resultKeyWeightedsOrderValues, new System.ReadOnlySpan<int>(resultKeyWeightedsIndices, resultKeyWeightedsStart, resultKeyWeightedsCount), resultKeyWeightedsPosition, resultKeyWeightedsFrameStart, resultKeyWeightedsFrameEnd, token);
                            resultKeyWeighteds[resultKeyWeightedsCurrent] = Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.KeyWeightedKernel.Get(in resultKeyWeightedsContext);
                        }
                    }

                    __op14Scope.Dispose();
                    var __op15Scope = profileRecorder?.BeginOperatorValue(__op15Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>(resultWindowRows.Count);
                    __op15Scope.Dispose();
                    long __op16InputRows = 0L;
                    long __op16OutputRows = 0L;
                    var __op16Scope = profileRecorder?.BeginOperatorValue(__op16Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                        {
                            if ((windowIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.NumericRow<int> ko3iko = resultWindowRows[windowIndex];
                            __op16InputRows++;
                            __op16OutputRows++;
                            result.Add(new ResultShape0(ko3iko.Position, (decimal?)resultKeyWeighteds[windowIndex]));
                            __op17OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op16Scope.AddInputRows(__op16InputRows);
                        __op16Scope.AddOutputRows(__op16OutputRows);
                        __op16Scope.Dispose();
                    }

                    var __op18Scope = profileRecorder?.BeginOperatorValue(__op18Handle) ?? OperatorProfileValueScope.None;
                    __op18Scope.AddInputRows(result.Count);
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

                    __op18Scope.AddOutputRows(__musoqFinalShapeRows.Count);
                    __op18Scope.Dispose();
                    var __op20Scope = profileRecorder?.BeginOperatorValue(__op20Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op20Scope.Dispose();
                    if (__op17OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op17Handle, __op17OutputRows);
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
            public ResultRow0(int __value0, decimal? __value1)
            {
                Position = __value0;
                Total = __value1;
            }

            public override int Count => 2;
            public int Position { get; private set; }
            public decimal? Total { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "Total" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Position, decimal? Total)
            {
                this.Position = Position;
                this.Total = Total;
            }

            public int Position { get; }
            public decimal? Total { get; }
        }

        private readonly struct WindowResultKeyWeightedsOrderKeysKey : System.IEquatable<WindowResultKeyWeightedsOrderKeysKey>, System.IComparable<WindowResultKeyWeightedsOrderKeysKey>
        {
            private readonly int _value0;
            public WindowResultKeyWeightedsOrderKeysKey(int value0)
            {
                _value0 = value0;
            }

            public int RawValues => _value0;

            public int CompareTo(WindowResultKeyWeightedsOrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultKeyWeightedsOrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultKeyWeightedsOrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0);
                return hash.ToHashCode();
            }

            private static int CompareValue0(int left, int right)
            {
                var comparison = left.CompareTo(right);
                return comparison;
            }
        }
    }
}
