// === Parsed Query ===
/*
select Position, Sum(NextValue()) filter (where AcceptRow()) over (order by Position rows between 2 preceding and current row) as Total from #schema.first() order by Position
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [ko3iko.Position]
    Project [ko3iko.Position as Position, WindowRef(0) as Total]
      Window [Sum(idx:0; order: ko3iko.Position; args: NextValue(); filter: AcceptRow(); frame: rows between 2 preceding and current row)]
        SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [ko3iko.Position]
    PhysicalProject [ko3iko.Position as Position, WindowRef(0) as Total]
      PhysicalWindow [Sum(idx:0; order: ko3iko.Position; args: NextValue(); filter: AcceptRow(); frame: rows between 2 preceding and current row)]
        PhysicalMaterialize
          PhysicalSchemaScan [#schema.first() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: VolatileWindowRow]
      Position: int <- property Position
    Generated [ResultRow0]
      Position: int <- field Position
      Total: double? <- field Total

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: VolatileWindowRow] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    ComputeSumWindowKernel[BoundedRows] [resultSums <- resultWindowRows value NextValue() order by ko3iko.Position ASC frame rows between 2 preceding and current row]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultShape0(Position: ko3iko.Position, Total: resultSums[windowIndex])]
    SortShapeRows [result -> resultSorted by Position ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P43_TypedWindowCapturedVolatileInputs_Full
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
            new Column("Total", typeof(double?), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0) });
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
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowRow>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowRow>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                var resultSumsIntOrderBuilder = new Musoq.Evaluator.Helpers.WindowIntOrderBuilder(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowRow ko3iko = resultWindowRows[windowIndex];
                    resultSumsIntOrderBuilder.Add((int)ko3iko.Position, windowIndex);
                }

                var resultSumsPartitions = resultSumsIntOrderBuilder.ToSortedPartitionSet(false);
                var resultSums = new double? [resultWindowRows.Count];
                var resultSumsBatchValues = new double? [resultWindowRows.Count];
                var resultSumsCapturedFilter = new bool[resultWindowRows.Count];
                for (int resultSumsCaptureIndex = 0; resultSumsCaptureIndex < resultWindowRows.Count; ++resultSumsCaptureIndex)
                {
                    Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowRow ko3iko = resultWindowRows[resultSumsCaptureIndex];
                    resultSumsCapturedFilter[resultSumsCaptureIndex] = (bool)(bool)Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowLibrary.AcceptRow((Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowRow)ko3iko);
                    if (resultSumsCapturedFilter[resultSumsCaptureIndex])
                    {
                        resultSumsBatchValues[resultSumsCaptureIndex] = (double?)(double?)Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowLibrary.NextValue((Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowRow)ko3iko);
                    }
                }

                for (int resultSumsPartitionSetIndex = 0; resultSumsPartitionSetIndex < resultSumsPartitions.PartitionCount; ++resultSumsPartitionSetIndex)
                {
                    var resultSumsPartitionStart = resultSumsPartitions.GetStart(resultSumsPartitionSetIndex);
                    var resultSumsPartitionCount = resultSumsPartitions.GetLength(resultSumsPartitionSetIndex);
                    var resultSumsPartitionIndices = resultSumsPartitions.Indices;
                    Musoq.Plugins.SumAggregateKernel<double>.State resultSumsState = default(Musoq.Plugins.SumAggregateKernel<double>.State)!;
                    Musoq.Plugins.SumAggregateKernel<double>.Reset(ref resultSumsState);
                    for (int resultSumsPartitionIndex = 0; resultSumsPartitionIndex < resultSumsPartitionCount; ++resultSumsPartitionIndex)
                    {
                        var resultSumsOutputIndex = resultSumsPartitionIndices[resultSumsPartitionStart + resultSumsPartitionIndex];
                        var resultSumsFrameStart = Math.Max(0, resultSumsPartitionIndex - 2);
                        var resultSumsFrameEnd = resultSumsPartitionIndex;
                        Musoq.Plugins.SumAggregateKernel<double>.Reset(ref resultSumsState);
                        for (var resultSumsFrameIndex = Math.Max(0, resultSumsFrameStart); resultSumsFrameIndex <= resultSumsFrameEnd && resultSumsFrameIndex < resultSumsPartitionCount; resultSumsFrameIndex++)
                        {
                            var resultSumsCurrentIndex = resultSumsPartitionIndices[resultSumsPartitionStart + resultSumsFrameIndex];
                            if (resultSumsCapturedFilter[resultSumsCurrentIndex])
                            {
                                Musoq.Plugins.SumAggregateKernel<double>.Set(ref resultSumsState, (double?)resultSumsBatchValues[resultSumsCurrentIndex]);
                            }
                        }

                        resultSums[resultSumsOutputIndex] = Musoq.Plugins.SumAggregateKernel<double>.Get(in resultSumsState);
                    }
                }

                var result = new List<ResultShape0>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowRow ko3iko = resultWindowRows[windowIndex];
                    result.Add(new ResultShape0(ko3iko.Position, (double?)resultSums[windowIndex]));
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
                    var __op9Handle = profileRecorder?.GetOperatorHandle("op9", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op10Handle = profileRecorder?.GetOperatorHandle("op10", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op11Handle = profileRecorder?.GetOperatorHandle("op11", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op12Handle = profileRecorder?.GetOperatorHandle("op12", "MaterializeChunked") ?? OperatorProfileHandle.None;
                    var __op13Handle = profileRecorder?.GetOperatorHandle("op13", "ComputeSumWindowKernel") ?? OperatorProfileHandle.None;
                    var __op14Handle = profileRecorder?.GetOperatorHandle("op14", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op15Handle = profileRecorder?.GetOperatorHandle("op15", "ForEachIndexed") ?? OperatorProfileHandle.None;
                    var __op16Handle = profileRecorder?.GetOperatorHandle("op16", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op17Handle = profileRecorder?.GetOperatorHandle("op17", "SortShapeRows") ?? OperatorProfileHandle.None;
                    var __op19Handle = profileRecorder?.GetOperatorHandle("op19", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    long __op16OutputRows = 0L;
                    var __op9Scope = profileRecorder?.BeginOperatorValue(__op9Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op9Scope.Dispose();
                    var __op10Scope = profileRecorder?.BeginOperatorValue(__op10Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op10Scope.Dispose();
                    var __op11Scope = profileRecorder?.BeginOperatorValue(__op11Handle) ?? OperatorProfileValueScope.None;
                    var __ko3ikoSchema = provider.GetSchema("#schema");
                    var ko3ikoRowsProfile = profileRecorder?.CreateSourceRecorder("ko3iko");
                    var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowRow>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress, ko3ikoRowsProfile == null ? SourceDiagnostics.None : ko3ikoRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                    var ko3ikoRows = ko3ikoRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowRow>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowRow>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowRow>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks, ko3ikoRowsProfile);
                    __op11Scope.Dispose();
                    var __op12Scope = profileRecorder?.BeginOperatorValue(__op12Handle) ?? OperatorProfileValueScope.None;
                    var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                    __op12Scope.AddOutputRows(resultWindowRows.Count);
                    __op12Scope.Dispose();
                    var __op13Scope = profileRecorder?.BeginOperatorValue(__op13Handle) ?? OperatorProfileValueScope.None;
                    var resultSumsIntOrderBuilder = new Musoq.Evaluator.Helpers.WindowIntOrderBuilder(resultWindowRows.Count);
                    for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                    {
                        Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowRow ko3iko = resultWindowRows[windowIndex];
                        resultSumsIntOrderBuilder.Add((int)ko3iko.Position, windowIndex);
                    }

                    var resultSumsPartitions = resultSumsIntOrderBuilder.ToSortedPartitionSet(false);
                    var resultSums = new double? [resultWindowRows.Count];
                    var resultSumsBatchValues = new double? [resultWindowRows.Count];
                    var resultSumsCapturedFilter = new bool[resultWindowRows.Count];
                    for (int resultSumsCaptureIndex = 0; resultSumsCaptureIndex < resultWindowRows.Count; ++resultSumsCaptureIndex)
                    {
                        Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowRow ko3iko = resultWindowRows[resultSumsCaptureIndex];
                        resultSumsCapturedFilter[resultSumsCaptureIndex] = (bool)(bool)Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowLibrary.AcceptRow((Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowRow)ko3iko);
                        if (resultSumsCapturedFilter[resultSumsCaptureIndex])
                        {
                            resultSumsBatchValues[resultSumsCaptureIndex] = (double?)(double?)Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowLibrary.NextValue((Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowRow)ko3iko);
                        }
                    }

                    for (int resultSumsPartitionSetIndex = 0; resultSumsPartitionSetIndex < resultSumsPartitions.PartitionCount; ++resultSumsPartitionSetIndex)
                    {
                        var resultSumsPartitionStart = resultSumsPartitions.GetStart(resultSumsPartitionSetIndex);
                        var resultSumsPartitionCount = resultSumsPartitions.GetLength(resultSumsPartitionSetIndex);
                        var resultSumsPartitionIndices = resultSumsPartitions.Indices;
                        Musoq.Plugins.SumAggregateKernel<double>.State resultSumsState = default(Musoq.Plugins.SumAggregateKernel<double>.State)!;
                        Musoq.Plugins.SumAggregateKernel<double>.Reset(ref resultSumsState);
                        for (int resultSumsPartitionIndex = 0; resultSumsPartitionIndex < resultSumsPartitionCount; ++resultSumsPartitionIndex)
                        {
                            var resultSumsOutputIndex = resultSumsPartitionIndices[resultSumsPartitionStart + resultSumsPartitionIndex];
                            var resultSumsFrameStart = Math.Max(0, resultSumsPartitionIndex - 2);
                            var resultSumsFrameEnd = resultSumsPartitionIndex;
                            Musoq.Plugins.SumAggregateKernel<double>.Reset(ref resultSumsState);
                            for (var resultSumsFrameIndex = Math.Max(0, resultSumsFrameStart); resultSumsFrameIndex <= resultSumsFrameEnd && resultSumsFrameIndex < resultSumsPartitionCount; resultSumsFrameIndex++)
                            {
                                var resultSumsCurrentIndex = resultSumsPartitionIndices[resultSumsPartitionStart + resultSumsFrameIndex];
                                if (resultSumsCapturedFilter[resultSumsCurrentIndex])
                                {
                                    Musoq.Plugins.SumAggregateKernel<double>.Set(ref resultSumsState, (double?)resultSumsBatchValues[resultSumsCurrentIndex]);
                                }
                            }

                            resultSums[resultSumsOutputIndex] = Musoq.Plugins.SumAggregateKernel<double>.Get(in resultSumsState);
                        }
                    }

                    __op13Scope.Dispose();
                    var __op14Scope = profileRecorder?.BeginOperatorValue(__op14Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>(resultWindowRows.Count);
                    __op14Scope.Dispose();
                    long __op15InputRows = 0L;
                    long __op15OutputRows = 0L;
                    var __op15Scope = profileRecorder?.BeginOperatorValue(__op15Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                        {
                            if ((windowIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.VolatileWindowRow ko3iko = resultWindowRows[windowIndex];
                            __op15InputRows++;
                            __op15OutputRows++;
                            result.Add(new ResultShape0(ko3iko.Position, (double?)resultSums[windowIndex]));
                            __op16OutputRows += 1;
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
                    var __op19Scope = profileRecorder?.BeginOperatorValue(__op19Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op19Scope.Dispose();
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, double? __value1)
            {
                Position = __value0;
                Total = __value1;
            }

            public override int Count => 2;
            public int Position { get; private set; }
            public double? Total { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<double?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double?>(this.Total),
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
            public ResultShape0(int Position, double? Total)
            {
                this.Position = Position;
                this.Total = Total;
            }

            public int Position { get; }
            public double? Total { get; }
        }

        private readonly struct WindowResultSumsOrderKeysKey : System.IEquatable<WindowResultSumsOrderKeysKey>, System.IComparable<WindowResultSumsOrderKeysKey>
        {
            private readonly int _value0;
            public WindowResultSumsOrderKeysKey(int value0)
            {
                _value0 = value0;
            }

            public int RawValues => _value0;

            public int CompareTo(WindowResultSumsOrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultSumsOrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultSumsOrderKeysKey other && Equals(other);
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
