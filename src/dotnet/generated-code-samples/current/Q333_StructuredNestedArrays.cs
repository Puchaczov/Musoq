// === Parsed Query ===
/*
select m.Value, m.Row, m.Column
from #inputs.matrix(
    values: array {
        array { 1, 2 },
        array { 3 },
        array {},
    }
) m
*/

// === Logical Plan ===
/*
MultiStatement
  Project [m.Value as Value, m.Row as Row, m.Column as Column]
    SchemaScan [#inputs.matrix(array { array { 1, 2 }, array { 3 }, array {  } }) as m]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [m.Value as Value, m.Row as Row, m.Column as Column]
    PhysicalSchemaScan [#inputs.matrix(array { array { 1, 2 }, array { 3 }, array {  } }) as m]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [m: MatrixValueRow]
      Value: int <- property Value
      Row: int <- property Row
      Column: int <- property Column
    Generated [ResultRow0]
      Value: int <- field Value
      Row: int <- field Row
      Column: int <- field Column

  Body
    PhaseBoundary [Begin]
    Let [__musoqStructural_m_0: IReadOnlyList<int> = array { 1, 2 }]
    Let [__musoqStructural_m_1: IReadOnlyList<int> = array { 3 }]
    Let [__musoqStructural_m_2: IReadOnlyList<int> = array {  }]
    Let [__musoqStructural_m_3: IReadOnlyList<IReadOnlyList<int>> = array { __musoqStructural_m_0, __musoqStructural_m_1, __musoqStructural_m_2 }]
    PhaseBoundary [From]
    SourceScan [m: MatrixValueRow] -> mRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [m in mRows]
      AppendShape [result <- ResultRow0(Value: m.Value, Row: m.Row, Column: m.Column)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q333_StructuredNestedArrays
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
            new Column("Row", typeof(int), 1),
            new Column("Column", typeof(int), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_m_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Value", typeof(int), 0), new Column("Row", typeof(int), 1), new Column("Column", typeof(int), 2) });
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
                OnPhaseChanged("compiled", QueryPhase.Begin);
                IReadOnlyList<int> __musoqStructural_m_0 = new int[]
                {
                    1,
                    2
                };
                IReadOnlyList<int> __musoqStructural_m_1 = new int[]
                {
                    3
                };
                IReadOnlyList<int> __musoqStructural_m_2 = new int[]
                {
                };
                IReadOnlyList<IReadOnlyList<int>> __musoqStructural_m_3 = new IReadOnlyList<int>[]
                {
                    __musoqStructural_m_0,
                    __musoqStructural_m_1,
                    __musoqStructural_m_2
                };
                OnPhaseChanged("compiled", QueryPhase.From);
                var __mSchema = provider.GetSchema("#inputs");
                var mRowsSourceContext = new SourceExecutionContext("m:1", sourceExecutionPlans["m:1"], token, __schemaColumns_compiled_m_0, sourceRuntimeSettingsBySourceContextId["m:1"], logger, OnDataSourceProgress);
                Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.MatrixValueRow> mRowsSource;
                try
                {
                    Musoq.Examples.DataSources.StructuredInputs.MatrixSource mRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.MatrixSource(__musoqCheckStructural_314dcfee34067c17(__musoqStructural_m_3, token), mRowsSourceContext);
                    mRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.MatrixSource, Musoq.Examples.DataSources.StructuredInputs.MatrixValueRow>(__mSchema, "matrix", mRowsSourceInstance, mRowsSourceContext, "#inputs", "m", "m:1");
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
                    throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "matrix", "m", "m:1", exception);
                }

                var mRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.MatrixValueRow>(mRowsSource.Chunks, __musoqProgressContext, "m:1") : mRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var mChunk in mRows)
                {
                    if (mChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Examples.DataSources.StructuredInputs.MatrixValueRow> mChunkView)
                    {
                        if (mChunkView.Source is Musoq.Examples.DataSources.StructuredInputs.MatrixValueRow[] mChunkViewArray)
                        {
                            int mChunkViewOffset = mChunkView.Offset;
                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                            {
                                if ((mIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var m = mChunkViewArray[mChunkViewOffset + mIndex];
                                yield return new ResultRow0(m.Value, m.Row, m.Column);
                            }

                            continue;
                        }

                        if (mChunkView.Source is List<Musoq.Examples.DataSources.StructuredInputs.MatrixValueRow> mChunkViewList)
                        {
                            int mChunkViewOffset = mChunkView.Offset;
                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                            {
                                if ((mIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var m = mChunkViewList[mChunkViewOffset + mIndex];
                                yield return new ResultRow0(m.Value, m.Row, m.Column);
                            }

                            continue;
                        }
                    }

                    for (int mIndex = 0, mIndexCount = mChunk.Count; mIndex < mIndexCount; ++mIndex)
                    {
                        if ((mIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var m = mChunk[mIndex];
                        yield return new ResultRow0(m.Value, m.Row, m.Column);
                    }
                }
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

        private static IReadOnlyList<IReadOnlyList<int>> __musoqCheckStructural_314dcfee34067c17(IReadOnlyList<IReadOnlyList<int>> value, System.Threading.CancellationToken token)
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
                var __musoqStructural_collection1 = (System.Collections.Generic.IReadOnlyList<IReadOnlyList<int>>)value;
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
                        var __musoqStructural_collection4 = (System.Collections.Generic.IReadOnlyList<int>)__musoqStructural_collection1[__musoqStructural_index0];
                        var __musoqStructural_count5 = __musoqStructural_collection4.Count;
                        for (var __musoqStructural_index3 = 0; __musoqStructural_index3 < __musoqStructural_count5; __musoqStructural_index3++)
                        {
                            if ((__musoqStructural_index3 & 1023) == 0)
                                token.ThrowIfCancellationRequested();
                            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                            if (3 > __musoqStructuralMaxDepth)
                                __musoqStructuralMaxDepth = 3;
                        }
                    }
                }
            }

            global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("inline", "m.argument[0]", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "m:1");
            return value;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, int __value1, int __value2)
            {
                Value = __value0;
                Row = __value1;
                Column = __value2;
            }

            public int Column { get; private set; }
            public override int Count => 3;
            public int Row { get; private set; }
            public int Value { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Row = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Column = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Row),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Column),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Value" => true,
                "Row" => true,
                "Column" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Value, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Row, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Column, ref visitor);
            }
        }
    }
}
