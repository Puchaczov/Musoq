// === Parsed Query ===
/*
select Position, RowNumber() over (order by Value) as Ordinal from #schema.first() qualify Ordinal <= 0 order by Position
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [ko3iko.Position]
    Project [ko3iko.Position as Position, WindowRef(0) as Ordinal]
      Qualify [(WindowRef(0) <= 0)]
        Window [RowNumber(idx:0; order: ko3iko.Value)]
          SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [ko3iko.Position]
    PhysicalProject [ko3iko.Position as Position, WindowRef(0) as Ordinal]
      PhysicalQualify [(WindowRef(0) <= 0)]
        PhysicalWindow [RowNumber(idx:0; order: ko3iko.Value)]
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
      Ordinal: long <- field Ordinal

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: NumericRow<int>] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows order by ko3iko.Value ASC]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      If [(resultRowNumbers[windowIndex] <= 0)]
        AppendShape [result <- ResultShape0(Position: ko3iko.Position, Ordinal: resultRowNumbers[windowIndex])]
    SortOwnedBuffer [result by Position ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P81_CatalogOrdinalQualify_Full
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
            new Column("Ordinal", typeof(long), 1)
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
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Ordinal);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Ordinal);
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
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#schema");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.NumericRow<int>>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.NumericRow<int>>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                var resultRowNumbersOrderKeys = new WindowResultRowNumbersOrderKeysKey[resultWindowRows.Count];
                ExtractResultRowNumbersWindowKeys(resultWindowRows, resultRowNumbersOrderKeys);
                var resultRowNumbersPartitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultRowNumbersPartitions, resultRowNumbersOrderKeys, false);
                var resultRowNumbers = new long[resultWindowRows.Count];
                for (int resultRowNumbersPartitionSetIndex = 0; resultRowNumbersPartitionSetIndex < resultRowNumbersPartitions.PartitionCount; ++resultRowNumbersPartitionSetIndex)
                {
                    var resultRowNumbersPartitionStart = resultRowNumbersPartitions.GetStart(resultRowNumbersPartitionSetIndex);
                    var resultRowNumbersPartitionCount = resultRowNumbersPartitions.GetLength(resultRowNumbersPartitionSetIndex);
                    var resultRowNumbersPartitionIndices = resultRowNumbersPartitions.Indices;
                    var resultRowNumbersPartitionLimit = resultRowNumbersPartitionCount;
                    for (int resultRowNumbersPartitionIndex = 0; resultRowNumbersPartitionIndex < resultRowNumbersPartitionLimit; ++resultRowNumbersPartitionIndex)
                    {
                        var resultRowNumbersCurrentIndex = resultRowNumbersPartitionIndices[resultRowNumbersPartitionStart + resultRowNumbersPartitionIndex];
                        var resultRowNumbersContext = new Musoq.Plugins.WindowRankingContext(resultRowNumbersPartitionIndex, 0, 0, 0L, resultRowNumbersPartitionCount, token);
                        resultRowNumbers[resultRowNumbersCurrentIndex] = Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.ShiftedOrdinalKernel.Get(in resultRowNumbersContext);
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
                    if (((long)resultRowNumbers[windowIndex] <= 0))
                    {
                        result.Add(new ResultShape0(ko3iko.Position, (long)resultRowNumbers[windowIndex]) { __musoqSequence = result.Count });
                    }
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Position.CompareTo(right.Position);
                    if (comparison != 0)
                        return comparison;
                    return left.__musoqSequence.CompareTo(right.__musoqSequence);
                }));
                OnPhaseChanged("compiled", QueryPhase.Select);
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
                    var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                    ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                    var __op10Handle = profileRecorder?.GetOperatorHandle("op10", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op11Handle = profileRecorder?.GetOperatorHandle("op11", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op12Handle = profileRecorder?.GetOperatorHandle("op12", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op13Handle = profileRecorder?.GetOperatorHandle("op13", "MaterializeChunked") ?? OperatorProfileHandle.None;
                    var __op14Handle = profileRecorder?.GetOperatorHandle("op14", "ComputeRowNumberWindow") ?? OperatorProfileHandle.None;
                    var __op15Handle = profileRecorder?.GetOperatorHandle("op15", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op16Handle = profileRecorder?.GetOperatorHandle("op16", "ForEachIndexed") ?? OperatorProfileHandle.None;
                    var __op18Handle = profileRecorder?.GetOperatorHandle("op18", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op19Handle = profileRecorder?.GetOperatorHandle("op19", "SortOwnedBuffer") ?? OperatorProfileHandle.None;
                    var __op21Handle = profileRecorder?.GetOperatorHandle("op21", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    long __op18OutputRows = 0L;
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
                    var resultRowNumbersOrderKeys = new WindowResultRowNumbersOrderKeysKey[resultWindowRows.Count];
                    ExtractResultRowNumbersWindowKeys(resultWindowRows, resultRowNumbersOrderKeys);
                    var resultRowNumbersPartitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                    WindowFunctionHelpers.SortStructPartitionSetInPlace(resultRowNumbersPartitions, resultRowNumbersOrderKeys, false);
                    var resultRowNumbers = new long[resultWindowRows.Count];
                    for (int resultRowNumbersPartitionSetIndex = 0; resultRowNumbersPartitionSetIndex < resultRowNumbersPartitions.PartitionCount; ++resultRowNumbersPartitionSetIndex)
                    {
                        var resultRowNumbersPartitionStart = resultRowNumbersPartitions.GetStart(resultRowNumbersPartitionSetIndex);
                        var resultRowNumbersPartitionCount = resultRowNumbersPartitions.GetLength(resultRowNumbersPartitionSetIndex);
                        var resultRowNumbersPartitionIndices = resultRowNumbersPartitions.Indices;
                        var resultRowNumbersPartitionLimit = resultRowNumbersPartitionCount;
                        for (int resultRowNumbersPartitionIndex = 0; resultRowNumbersPartitionIndex < resultRowNumbersPartitionLimit; ++resultRowNumbersPartitionIndex)
                        {
                            var resultRowNumbersCurrentIndex = resultRowNumbersPartitionIndices[resultRowNumbersPartitionStart + resultRowNumbersPartitionIndex];
                            var resultRowNumbersContext = new Musoq.Plugins.WindowRankingContext(resultRowNumbersPartitionIndex, 0, 0, 0L, resultRowNumbersPartitionCount, token);
                            resultRowNumbers[resultRowNumbersCurrentIndex] = Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.ShiftedOrdinalKernel.Get(in resultRowNumbersContext);
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
                            if (((long)resultRowNumbers[windowIndex] <= 0))
                            {
                                result.Add(new ResultShape0(ko3iko.Position, (long)resultRowNumbers[windowIndex]) { __musoqSequence = result.Count });
                                __op18OutputRows += 1;
                            }
                        }
                    }
                    finally
                    {
                        __op16Scope.AddInputRows(__op16InputRows);
                        __op16Scope.AddOutputRows(__op16OutputRows);
                        __op16Scope.Dispose();
                    }

                    var __op19Scope = profileRecorder?.BeginOperatorValue(__op19Handle) ?? OperatorProfileValueScope.None;
                    __op19Scope.AddInputRows(result.Count);
                    result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                    {
                        var comparison = left.Position.CompareTo(right.Position);
                        if (comparison != 0)
                            return comparison;
                        return left.__musoqSequence.CompareTo(right.__musoqSequence);
                    }));
                    __op19Scope.AddOutputRows(result.Count);
                    __op19Scope.Dispose();
                    var __op21Scope = profileRecorder?.BeginOperatorValue(__op21Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op21Scope.Dispose();
                    if (__op18OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op18Handle, __op18OutputRows);
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
        private static void ExtractResultRowNumbersWindowKeys(IReadOnlyList<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.NumericRow<int>> resultWindowRows, WindowResultRowNumbersOrderKeysKey[] resultRowNumbersOrderKeys)
        {
            for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
            {
                Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.NumericRow<int> ko3iko = resultWindowRows[windowIndex];
                resultRowNumbersOrderKeys[windowIndex] = new WindowResultRowNumbersOrderKeysKey(ko3iko.Value);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, long __value1)
            {
                Position = __value0;
                Ordinal = __value1;
            }

            public override int Count => 2;
            public long Ordinal { get; private set; }
            public int Position { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Ordinal = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Ordinal),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "Ordinal" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Ordinal, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Position, long Ordinal)
            {
                this.Position = Position;
                this.Ordinal = Ordinal;
            }

            public long Ordinal { get; }
            public int Position { get; }
            public int __musoqSequence { get; init; }
        }

        private readonly struct WindowResultRowNumbersOrderKeysKey : System.IEquatable<WindowResultRowNumbersOrderKeysKey>, System.IComparable<WindowResultRowNumbersOrderKeysKey>
        {
            private readonly int? _value0;
            public WindowResultRowNumbersOrderKeysKey(int? value0)
            {
                _value0 = value0;
            }

            public int? RawValues => _value0;

            public int CompareTo(WindowResultRowNumbersOrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultRowNumbersOrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int?>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultRowNumbersOrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0);
                return hash.ToHashCode();
            }

            private static int CompareValue0(int? left, int? right)
            {
                if (!left.HasValue)
                    return !right.HasValue ? 0 : -1;
                if (!right.HasValue)
                    return 1;
                var comparison = left.Value.CompareTo(right.Value);
                return comparison;
            }
        }
    }
}
