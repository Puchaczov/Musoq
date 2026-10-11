// === Parsed Query ===
/*
select Partition, Position, FirstLabel(Value) over (partition by Partition order by Position, Label rows between unbounded preceding and current row) as Label from #schema.first() order by Partition, Position, Label
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [ko3iko.Partition, ko3iko.Position, WindowRef(0)]
    Project [ko3iko.Partition as Partition, ko3iko.Position as Position, WindowRef(0) as Label]
      Window [FirstLabel(idx:0; partition: ko3iko.Partition; order: ko3iko.Position, ko3iko.Label; args: ko3iko.Value; frame: rows between unbounded preceding and current row)]
        SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [ko3iko.Partition, ko3iko.Position, WindowRef(0)]
    PhysicalProject [ko3iko.Partition as Partition, ko3iko.Position as Position, WindowRef(0) as Label]
      PhysicalWindow [FirstLabel(idx:0; partition: ko3iko.Partition; order: ko3iko.Position, ko3iko.Label; args: ko3iko.Value; frame: rows between unbounded preceding and current row)]
        PhysicalMaterialize
          PhysicalSchemaScan [#schema.first() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: OrderKeyRow]
      Partition: int <- property Partition
      Position: int <- property Position
      Value: int? <- property Value
      Label: string <- property Label
    Generated [ResultRow0]
      Partition: int <- field Partition
      Position: int <- field Position
      Label: string <- field Label

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: OrderKeyRow] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    ComputeFirstLabelWindow [resultFirstLabels <- resultWindowRows value ko3iko.Value partition by ko3iko.Partition order by ko3iko.Position ASC, ko3iko.Label ASC frame rows between unbounded preceding and current row]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultShape0(Partition: ko3iko.Partition, Position: ko3iko.Position, Label: resultFirstLabels[windowIndex])]
    SortShapeRows [result -> resultSorted by Partition ASC, Position ASC, Label ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P53_TypedCompositeOrderKeyContext_Full
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
            new Column("Partition", typeof(int), 0),
            new Column("Position", typeof(int), 1),
            new Column("Label", typeof(string), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Partition", typeof(int), 0), new Column("Position", typeof(int), 1), new Column("Value", typeof(int?), 2), new Column("Label", typeof(string), 3) });
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
                yield return new ResultRow0(__musoqShapeRow.Partition, __musoqShapeRow.Position, __musoqShapeRow.Label);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.Partition, __musoqShapeRow.Position, __musoqShapeRow.Label);
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
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.OrderKeyRow>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.OrderKeyRow>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                var resultFirstLabelsPartitionKeys = new int[resultWindowRows.Count];
                var resultFirstLabelsOrderKeys = new WindowResultFirstLabelsOrderKeysKey[resultWindowRows.Count];
                var resultFirstLabelsValues = new int? [resultWindowRows.Count];
                var resultFirstLabelsOrderValues = new ValueTuple<int, string>[resultWindowRows.Count];
                for (int resultFirstLabelsExtractionIndex = 0; resultFirstLabelsExtractionIndex < resultWindowRows.Count; ++resultFirstLabelsExtractionIndex)
                {
                    Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.OrderKeyRow ko3iko = resultWindowRows[resultFirstLabelsExtractionIndex];
                    resultFirstLabelsPartitionKeys[resultFirstLabelsExtractionIndex] = (int)ko3iko.Partition;
                    resultFirstLabelsOrderKeys[resultFirstLabelsExtractionIndex] = new WindowResultFirstLabelsOrderKeysKey(ko3iko.Position, ko3iko.Label);
                    resultFirstLabelsOrderValues[resultFirstLabelsExtractionIndex] = (ValueTuple<int, string>)resultFirstLabelsOrderKeys[resultFirstLabelsExtractionIndex].RawValues;
                    resultFirstLabelsValues[resultFirstLabelsExtractionIndex] = (int?)(int?)ko3iko.Value;
                }

                var resultFirstLabelsPartitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count, resultFirstLabelsPartitionKeys);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultFirstLabelsPartitions, resultFirstLabelsOrderKeys, false);
                var resultFirstLabels = new string[resultWindowRows.Count];
                for (int resultFirstLabelsPartition = 0; resultFirstLabelsPartition < resultFirstLabelsPartitions.PartitionCount; ++resultFirstLabelsPartition)
                {
                    var resultFirstLabelsStart = resultFirstLabelsPartitions.GetStart(resultFirstLabelsPartition);
                    var resultFirstLabelsCount = resultFirstLabelsPartitions.GetLength(resultFirstLabelsPartition);
                    var resultFirstLabelsIndices = resultFirstLabelsPartitions.Indices;
                    for (int resultFirstLabelsPosition = 0; resultFirstLabelsPosition < resultFirstLabelsCount; ++resultFirstLabelsPosition)
                    {
                        if ((resultFirstLabelsPosition & 1023) == 0)
                            token.ThrowIfCancellationRequested();
                        var resultFirstLabelsCurrent = resultFirstLabelsIndices[resultFirstLabelsStart + resultFirstLabelsPosition];
                        var resultFirstLabelsFrameStart = 0;
                        var resultFirstLabelsFrameEnd = resultFirstLabelsPosition;
                        var resultFirstLabelsContext = new Musoq.Plugins.WindowFrameContext<int?, ValueTuple<int, string>>(resultFirstLabelsValues, resultFirstLabelsOrderValues, new System.ReadOnlySpan<int>(resultFirstLabelsIndices, resultFirstLabelsStart, resultFirstLabelsCount), resultFirstLabelsPosition, resultFirstLabelsFrameStart, resultFirstLabelsFrameEnd, token);
                        resultFirstLabels[resultFirstLabelsCurrent] = Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.FirstLabelKernel.Get(in resultFirstLabelsContext);
                    }
                }

                var result = new List<ResultShape0>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.OrderKeyRow ko3iko = resultWindowRows[windowIndex];
                    result.Add(new ResultShape0(ko3iko.Partition, ko3iko.Position, (string)resultFirstLabels[windowIndex]));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Partition.CompareTo(right.Partition);
                    if (comparison != 0)
                        return comparison;
                    comparison = left.Position.CompareTo(right.Position);
                    if (comparison != 0)
                        return comparison;
                    comparison = StringComparer.Ordinal.Compare(left.Label, right.Label);
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
                    var __op13Handle = profileRecorder?.GetOperatorHandle("op13", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op14Handle = profileRecorder?.GetOperatorHandle("op14", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op15Handle = profileRecorder?.GetOperatorHandle("op15", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op16Handle = profileRecorder?.GetOperatorHandle("op16", "MaterializeChunked") ?? OperatorProfileHandle.None;
                    var __op17Handle = profileRecorder?.GetOperatorHandle("op17", "ComputeFirstLabelWindow") ?? OperatorProfileHandle.None;
                    var __op18Handle = profileRecorder?.GetOperatorHandle("op18", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op19Handle = profileRecorder?.GetOperatorHandle("op19", "ForEachIndexed") ?? OperatorProfileHandle.None;
                    var __op20Handle = profileRecorder?.GetOperatorHandle("op20", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op21Handle = profileRecorder?.GetOperatorHandle("op21", "SortShapeRows") ?? OperatorProfileHandle.None;
                    var __op23Handle = profileRecorder?.GetOperatorHandle("op23", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    long __op20OutputRows = 0L;
                    var __op13Scope = profileRecorder?.BeginOperatorValue(__op13Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op13Scope.Dispose();
                    var __op14Scope = profileRecorder?.BeginOperatorValue(__op14Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op14Scope.Dispose();
                    var __op15Scope = profileRecorder?.BeginOperatorValue(__op15Handle) ?? OperatorProfileValueScope.None;
                    var __ko3ikoSchema = provider.GetSchema("#schema");
                    var ko3ikoRowsProfile = profileRecorder?.CreateSourceRecorder("ko3iko");
                    var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.OrderKeyRow>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress, ko3ikoRowsProfile == null ? SourceDiagnostics.None : ko3ikoRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                    var ko3ikoRows = ko3ikoRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.OrderKeyRow>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.OrderKeyRow>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.OrderKeyRow>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks, ko3ikoRowsProfile);
                    __op15Scope.Dispose();
                    var __op16Scope = profileRecorder?.BeginOperatorValue(__op16Handle) ?? OperatorProfileValueScope.None;
                    var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                    __op16Scope.AddOutputRows(resultWindowRows.Count);
                    __op16Scope.Dispose();
                    var __op17Scope = profileRecorder?.BeginOperatorValue(__op17Handle) ?? OperatorProfileValueScope.None;
                    var resultFirstLabelsPartitionKeys = new int[resultWindowRows.Count];
                    var resultFirstLabelsOrderKeys = new WindowResultFirstLabelsOrderKeysKey[resultWindowRows.Count];
                    var resultFirstLabelsValues = new int? [resultWindowRows.Count];
                    var resultFirstLabelsOrderValues = new ValueTuple<int, string>[resultWindowRows.Count];
                    for (int resultFirstLabelsExtractionIndex = 0; resultFirstLabelsExtractionIndex < resultWindowRows.Count; ++resultFirstLabelsExtractionIndex)
                    {
                        Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.OrderKeyRow ko3iko = resultWindowRows[resultFirstLabelsExtractionIndex];
                        resultFirstLabelsPartitionKeys[resultFirstLabelsExtractionIndex] = (int)ko3iko.Partition;
                        resultFirstLabelsOrderKeys[resultFirstLabelsExtractionIndex] = new WindowResultFirstLabelsOrderKeysKey(ko3iko.Position, ko3iko.Label);
                        resultFirstLabelsOrderValues[resultFirstLabelsExtractionIndex] = (ValueTuple<int, string>)resultFirstLabelsOrderKeys[resultFirstLabelsExtractionIndex].RawValues;
                        resultFirstLabelsValues[resultFirstLabelsExtractionIndex] = (int?)(int?)ko3iko.Value;
                    }

                    var resultFirstLabelsPartitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count, resultFirstLabelsPartitionKeys);
                    WindowFunctionHelpers.SortStructPartitionSetInPlace(resultFirstLabelsPartitions, resultFirstLabelsOrderKeys, false);
                    var resultFirstLabels = new string[resultWindowRows.Count];
                    for (int resultFirstLabelsPartition = 0; resultFirstLabelsPartition < resultFirstLabelsPartitions.PartitionCount; ++resultFirstLabelsPartition)
                    {
                        var resultFirstLabelsStart = resultFirstLabelsPartitions.GetStart(resultFirstLabelsPartition);
                        var resultFirstLabelsCount = resultFirstLabelsPartitions.GetLength(resultFirstLabelsPartition);
                        var resultFirstLabelsIndices = resultFirstLabelsPartitions.Indices;
                        for (int resultFirstLabelsPosition = 0; resultFirstLabelsPosition < resultFirstLabelsCount; ++resultFirstLabelsPosition)
                        {
                            if ((resultFirstLabelsPosition & 1023) == 0)
                                token.ThrowIfCancellationRequested();
                            var resultFirstLabelsCurrent = resultFirstLabelsIndices[resultFirstLabelsStart + resultFirstLabelsPosition];
                            var resultFirstLabelsFrameStart = 0;
                            var resultFirstLabelsFrameEnd = resultFirstLabelsPosition;
                            var resultFirstLabelsContext = new Musoq.Plugins.WindowFrameContext<int?, ValueTuple<int, string>>(resultFirstLabelsValues, resultFirstLabelsOrderValues, new System.ReadOnlySpan<int>(resultFirstLabelsIndices, resultFirstLabelsStart, resultFirstLabelsCount), resultFirstLabelsPosition, resultFirstLabelsFrameStart, resultFirstLabelsFrameEnd, token);
                            resultFirstLabels[resultFirstLabelsCurrent] = Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.FirstLabelKernel.Get(in resultFirstLabelsContext);
                        }
                    }

                    __op17Scope.Dispose();
                    var __op18Scope = profileRecorder?.BeginOperatorValue(__op18Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>(resultWindowRows.Count);
                    __op18Scope.Dispose();
                    long __op19InputRows = 0L;
                    long __op19OutputRows = 0L;
                    var __op19Scope = profileRecorder?.BeginOperatorValue(__op19Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                        {
                            if ((windowIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.OrderKeyRow ko3iko = resultWindowRows[windowIndex];
                            __op19InputRows++;
                            __op19OutputRows++;
                            result.Add(new ResultShape0(ko3iko.Partition, ko3iko.Position, (string)resultFirstLabels[windowIndex]));
                            __op20OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op19Scope.AddInputRows(__op19InputRows);
                        __op19Scope.AddOutputRows(__op19OutputRows);
                        __op19Scope.Dispose();
                    }

                    var __op21Scope = profileRecorder?.BeginOperatorValue(__op21Handle) ?? OperatorProfileValueScope.None;
                    __op21Scope.AddInputRows(result.Count);
                    var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                    {
                        var comparison = left.Partition.CompareTo(right.Partition);
                        if (comparison != 0)
                            return comparison;
                        comparison = left.Position.CompareTo(right.Position);
                        if (comparison != 0)
                            return comparison;
                        comparison = StringComparer.Ordinal.Compare(left.Label, right.Label);
                        if (comparison != 0)
                            return comparison;
                        return 0;
                    }));
                    foreach (var resultSortedRowsRow in resultSortedRows)
                    {
                        __musoqFinalShapeRows.Add(resultSortedRowsRow);
                    }

                    __op21Scope.AddOutputRows(__musoqFinalShapeRows.Count);
                    __op21Scope.Dispose();
                    var __op23Scope = profileRecorder?.BeginOperatorValue(__op23Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op23Scope.Dispose();
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
            public ResultRow0(int __value0, int __value1, string __value2)
            {
                Partition = __value0;
                Position = __value1;
                Label = __value2;
            }

            public override int Count => 3;
            public string Label { get; private set; }
            public int Partition { get; private set; }
            public int Position { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Partition = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Label = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Partition),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Label),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Partition" => true,
                "Position" => true,
                "Label" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Partition, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Label, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Partition, int Position, string Label)
            {
                this.Partition = Partition;
                this.Position = Position;
                this.Label = Label;
            }

            public string Label { get; }
            public int Partition { get; }
            public int Position { get; }
        }

        private readonly struct WindowResultFirstLabelsOrderKeysKey : System.IEquatable<WindowResultFirstLabelsOrderKeysKey>, System.IComparable<WindowResultFirstLabelsOrderKeysKey>
        {
            private readonly int _value0;
            private readonly string _value1;
            public WindowResultFirstLabelsOrderKeysKey(int value0, string value1)
            {
                _value0 = value0;
                _value1 = value1;
            }

            public ValueTuple<int, string> RawValues => (_value0, _value1);

            public int CompareTo(WindowResultFirstLabelsOrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                var comparison1 = CompareValue1(_value1, other._value1);
                if (comparison1 != 0)
                    return comparison1;
                return 0;
            }

            public bool Equals(WindowResultFirstLabelsOrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0) && System.String.Equals(_value1, other._value1, System.StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultFirstLabelsOrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0);
                hash.Add(_value1, System.StringComparer.Ordinal);
                return hash.ToHashCode();
            }

            private static int CompareValue0(int left, int right)
            {
                var comparison = left.CompareTo(right);
                return comparison;
            }

            private static int CompareValue1(string left, string right)
            {
                if (left == null)
                    return right == null ? 0 : -1;
                if (right == null)
                    return 1;
                var comparison = System.String.CompareOrdinal(left, right);
                return comparison;
            }
        }
    }
}
