// === Parsed Query ===
/*
select left.Value, right.Value
from #inputs.mutable(items: array { (Value: 1) }) left
cross join #inputs.mutable(items: array { (Value: 1) }) right
*/

// === Logical Plan ===
/*
MultiStatement
  Project [left.Value as left.Value, right.Value as right.Value]
    Join [Cross] [TRUE]
      SchemaScan [#inputs.mutable(array { (Value: 1) }) as left]
      SchemaScan [#inputs.mutable(array { (Value: 1) }) as right]
  Project [left.Value as Value, right.Value as Value]
    CteRef [leftright as leftright]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [left.Value as left.Value, right.Value as right.Value]
    PhysicalNestedLoopJoin [Cross] [TRUE]
      PhysicalSchemaScan [#inputs.mutable(array { (Value: 1) }) as left]
      PhysicalSchemaScan [#inputs.mutable(array { (Value: 1) }) as right]
  PhysicalProject [left.Value as Value, right.Value as Value]
    PhysicalCteRef [leftright as leftright]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [left: MutableProbeRow]
      Value: int <- property Value
    SourceEntity [right: MutableProbeRow]
      Value: int <- property Value
    Generated [ResultRow0]
      Value: int <- field Value
      Value: int <- field Value_1
  StructuralPreparation
    Musoq.Examples.DataSources.StructuredInputs.MutableInput; constructor=constructor:clr:Musoq.Examples.DataSources.StructuredInputs.MutableInput@Musoq.Examples.DataSources.StructuredInputs(primitive:int32); shape=(value: int32); origin=Inline; lifetime=Inline; metrics=Runtime; ownership=ConstructFresh; limits=depth=32;nodes=100000;strings=67108864; defaults=-
    Musoq.Examples.DataSources.StructuredInputs.MutableInput; constructor=constructor:clr:Musoq.Examples.DataSources.StructuredInputs.MutableInput@Musoq.Examples.DataSources.StructuredInputs(primitive:int32); shape=(value: int32); origin=Inline; lifetime=Inline; metrics=Runtime; ownership=ConstructFresh; limits=depth=32;nodes=100000;strings=67108864; defaults=-

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    Let [__musoqStructural_left_0: int = 1]
    PrepareStructuralInput [__musoqStructural_left_1: Musoq.Examples.DataSources.StructuredInputs.MutableInput <- (Value: __musoqStructural_left_0); lifetime Inline; shape (value: int32)]
    Let [__musoqStructural_left_2: IReadOnlyList<MutableInput> = array { __musoqStructural_left_1 }]
    SourceScan [left: MutableProbeRow] -> leftRows
    Let [__musoqStructural_right_0: int = 1]
    PrepareStructuralInput [__musoqStructural_right_1: Musoq.Examples.DataSources.StructuredInputs.MutableInput <- (Value: __musoqStructural_right_0); lifetime Inline; shape (value: int32)]
    Let [__musoqStructural_right_2: IReadOnlyList<MutableInput> = array { __musoqStructural_right_1 }]
    SourceScan [right: MutableProbeRow] -> rightRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    MaterializeChunked [rightRows -> rightRowsBuffer]
    ChunkedForEach [left in leftRows]
      Let [leftValue: int = left.Value]
      ForEach [right in rightRowsBuffer]
        AppendShape [result <- ResultRow0(Value: leftValue, Value: right.Value)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q359_StructuredMutableOwnership
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Musoq.Schema;
    using Musoq.Schema.Optimization;
    using Musoq.Evaluator;
    using Musoq.Evaluator.Tables;
    using Musoq.Evaluator.Helpers;
    using Musoq.Evaluator.Runtime;
    using Musoq.Schema.DataSources;
    using System.Linq;

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Value", typeof(int), 0),
            new Column("Value", typeof(int), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_left_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Value", typeof(int), 0) });
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

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return __musoqShapeRow;
            }
        }

        private IEnumerable<ResultRow0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    int __musoqStructural_left_0 = 1;
                    Musoq.Examples.DataSources.StructuredInputs.MutableInput __musoqStructural_left_1 = new Musoq.Examples.DataSources.StructuredInputs.MutableInput(__musoqStructural_left_0);
                    IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.MutableInput> __musoqStructural_left_2 = new Musoq.Examples.DataSources.StructuredInputs.MutableInput[]
                    {
                        __musoqStructural_left_1
                    };
                    var __leftSchema = provider.GetSchema("#inputs");
                    var leftRowsSourceContext = new SourceExecutionContext("left:1", sourceExecutionPlans["left:1"], token, __schemaColumns_compiled_left_0, sourceRuntimeSettingsBySourceContextId["left:1"], logger, OnDataSourceProgress);
                    Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.MutableProbeRow> leftRowsSource;
                    try
                    {
                        Musoq.Examples.DataSources.StructuredInputs.MutableProbeSource leftRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.MutableProbeSource(__musoqCheckStructural_b4696da4def4a07f(__musoqStructural_left_2, token), leftRowsSourceContext);
                        leftRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.MutableProbeSource, Musoq.Examples.DataSources.StructuredInputs.MutableProbeRow>(__leftSchema, "mutable", leftRowsSourceInstance, leftRowsSourceContext, "#inputs", "left", "left:1");
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (global::Musoq.Schema.Exceptions.DataSourceLifecycleException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "mutable", "left", "left:1", exception);
                    }

                    var leftRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.MutableProbeRow>(leftRowsSource.Chunks, __musoqProgressContext, "left:1") : leftRowsSource.Chunks;
                    int __musoqStructural_right_0 = 1;
                    Musoq.Examples.DataSources.StructuredInputs.MutableInput __musoqStructural_right_1 = new Musoq.Examples.DataSources.StructuredInputs.MutableInput(__musoqStructural_right_0);
                    IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.MutableInput> __musoqStructural_right_2 = new Musoq.Examples.DataSources.StructuredInputs.MutableInput[]
                    {
                        __musoqStructural_right_1
                    };
                    var __rightSchema = provider.GetSchema("#inputs");
                    var rightRowsSourceContext = new SourceExecutionContext("right:1", sourceExecutionPlans["right:1"], token, __schemaColumns_compiled_left_0, sourceRuntimeSettingsBySourceContextId["right:1"], logger, OnDataSourceProgress);
                    Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.MutableProbeRow> rightRowsSource;
                    try
                    {
                        Musoq.Examples.DataSources.StructuredInputs.MutableProbeSource rightRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.MutableProbeSource(__musoqCheckStructural_6b60fa05557b670b(__musoqStructural_right_2, token), rightRowsSourceContext);
                        rightRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.MutableProbeSource, Musoq.Examples.DataSources.StructuredInputs.MutableProbeRow>(__rightSchema, "mutable", rightRowsSourceInstance, rightRowsSourceContext, "#inputs", "right", "right:1");
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (global::Musoq.Schema.Exceptions.DataSourceLifecycleException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "mutable", "right", "right:1", exception);
                    }

                    var rightRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.MutableProbeRow>(rightRowsSource.Chunks, __musoqProgressContext, "right:1") : rightRowsSource.Chunks;
                    var rightRowsBuffer = EvaluationHelper.MaterializeChunkedRows(rightRows);
                    foreach (var leftChunk in leftRows)
                    {
                        if (leftChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Examples.DataSources.StructuredInputs.MutableProbeRow> leftChunkView)
                        {
                            if (leftChunkView.Source is Musoq.Examples.DataSources.StructuredInputs.MutableProbeRow[] leftChunkViewArray)
                            {
                                int leftChunkViewOffset = leftChunkView.Offset;
                                for (int leftIndex = 0, leftIndexCount = leftChunkView.Count; leftIndex < leftIndexCount; ++leftIndex)
                                {
                                    if ((leftIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var left = leftChunkViewArray[leftChunkViewOffset + leftIndex];
                                    int leftValue = left.Value;
                                    foreach (var right in rightRowsBuffer)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        __musoqFinalShapeRows.Add(new ResultRow0(leftValue, right.Value));
                                    }
                                }

                                continue;
                            }

                            if (leftChunkView.Source is List<Musoq.Examples.DataSources.StructuredInputs.MutableProbeRow> leftChunkViewList)
                            {
                                int leftChunkViewOffset = leftChunkView.Offset;
                                for (int leftIndex = 0, leftIndexCount = leftChunkView.Count; leftIndex < leftIndexCount; ++leftIndex)
                                {
                                    if ((leftIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var left = leftChunkViewList[leftChunkViewOffset + leftIndex];
                                    int leftValue = left.Value;
                                    foreach (var right in rightRowsBuffer)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        __musoqFinalShapeRows.Add(new ResultRow0(leftValue, right.Value));
                                    }
                                }

                                continue;
                            }
                        }

                        for (int leftIndex = 0, leftIndexCount = leftChunk.Count; leftIndex < leftIndexCount; ++leftIndex)
                        {
                            if ((leftIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var left = leftChunk[leftIndex];
                            int leftValue = left.Value;
                            foreach (var right in rightRowsBuffer)
                            {
                                token.ThrowIfCancellationRequested();
                                __musoqFinalShapeRows.Add(new ResultRow0(leftValue, right.Value));
                            }
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
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

        private static IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.MutableInput> __musoqCheckStructural_6b60fa05557b670b(IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.MutableInput> value, System.Threading.CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            long __musoqStructuralNodes = 0L;
            long __musoqStructuralStrings = 0L;
            int __musoqStructuralMaxDepth = 0;
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (1 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 1;
            if (value != null)
            {
                var __musoqStructural_collection1 = (System.Collections.Generic.IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.MutableInput>)value;
                var __musoqStructural_count2 = __musoqStructural_collection1.Count;
                for (var __musoqStructural_index0 = 0; __musoqStructural_index0 < __musoqStructural_count2; __musoqStructural_index0++)
                {
                    if ((__musoqStructural_index0 & 1023) == 0)
                        token.ThrowIfCancellationRequested();
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (2 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 2;
                    if (__musoqStructural_collection1[__musoqStructural_index0] != null)
                    {
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                    }
                }
            }

            global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("inline", "right.argument[0]", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "right:1");
            return value;
        }

        private static IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.MutableInput> __musoqCheckStructural_b4696da4def4a07f(IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.MutableInput> value, System.Threading.CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            long __musoqStructuralNodes = 0L;
            long __musoqStructuralStrings = 0L;
            int __musoqStructuralMaxDepth = 0;
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (1 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 1;
            if (value != null)
            {
                var __musoqStructural_collection1 = (System.Collections.Generic.IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.MutableInput>)value;
                var __musoqStructural_count2 = __musoqStructural_collection1.Count;
                for (var __musoqStructural_index0 = 0; __musoqStructural_index0 < __musoqStructural_count2; __musoqStructural_index0++)
                {
                    if ((__musoqStructural_index0 & 1023) == 0)
                        token.ThrowIfCancellationRequested();
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (2 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 2;
                    if (__musoqStructural_collection1[__musoqStructural_index0] != null)
                    {
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                    }
                }
            }

            global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("inline", "left.argument[0]", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "left:1");
            return value;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, int __value1)
            {
                Value = __value0;
                Value_1 = __value1;
            }

            public override int Count => 2;
            public int Value { get; private set; }
            public int Value_1 { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value_1 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value_1),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Value" => true,
                "Value_1" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Value, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Value_1, ref visitor);
            }
        }
    }
}
