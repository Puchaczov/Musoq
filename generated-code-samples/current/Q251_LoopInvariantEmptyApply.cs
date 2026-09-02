// === Parsed Query ===
/*
select a.Value, b.VolatileValue from #licm.outers() a cross apply a.EmptyMiddles b
*/

// === Logical Plan ===
/*
MultiStatement
  Project [a.Value as a.Value, a.EmptyMiddles as a.EmptyMiddles, b.VolatileValue as b.VolatileValue]
    Apply [Cross]
      SchemaScan [#licm.outers() as a]
      PropertySource [a.EmptyMiddles as b] [apply: Cross] [type: LoopInvariantSampleMiddle[]]
  Project [a.Value as a.Value, b.VolatileValue as b.VolatileValue]
    CteRef [ab as ab]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.Value as a.Value, a.EmptyMiddles as a.EmptyMiddles, b.VolatileValue as b.VolatileValue]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#licm.outers() as a]
      PhysicalPropertySource [a.EmptyMiddles as b] [apply: Cross] [type: LoopInvariantSampleMiddle[]]
  PhysicalProject [a.Value as a.Value, b.VolatileValue as b.VolatileValue]
    PhysicalCteRef [ab as ab]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: LoopInvariantSampleOuter]
      Value: int <- property Value
      EmptyMiddles: LoopInvariantSampleMiddle[] <- property EmptyMiddles
    SourceEntity [b: LoopInvariantSampleMiddle]
      Id: int <- property Id
      Value: int <- property Value
      VolatileValue: int <- property VolatileValue
      Leaves: LoopInvariantSampleLeaf[] <- property Leaves
    Generated [ResultRow0]
      a.Value: int <- field a_Value
      b.VolatileValue: int <- field b_VolatileValue

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [a: LoopInvariantSampleOuter] -> aRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [a in aRows]
      Let [aValue: int = a.Value]
      EnumerableSource [a.EmptyMiddles -> bRows]
      ChunkedForEach [b in bRows]
        AppendShape [result <- ResultShape0(a.Value: aValue, b.VolatileValue: b.VolatileValue)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q251_LoopInvariantEmptyApply
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("a.Value", typeof(int), 0),
            new Column("b.VolatileValue", typeof(int), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Value", typeof(int), 1), new Column("EmptyMiddles", typeof(Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle[]), 4) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public IDictionary<string, System.Object> Parameters { get; } = new Dictionary<string, System.Object>(StringComparer.Ordinal);
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }

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
                yield return new ResultRow0(__musoqShapeRow.a_Value, __musoqShapeRow.b_VolatileValue);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    var __aSchema = provider.GetSchema("#licm");
                    var aRowsSource = __aSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleOuter>("outers", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                    var aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleOuter>(aRowsSource.Chunks, __musoqProgressContext, "a:1") : aRowsSource.Chunks;
                    foreach (var aChunk in aRows)
                    {
                        if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleOuter> aChunkView)
                        {
                            if (aChunkView.Source is Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleOuter[] aChunkViewArray)
                            {
                                int aChunkViewOffset = aChunkView.Offset;
                                for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                                {
                                    if ((aIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var a = aChunkViewArray[aChunkViewOffset + aIndex];
                                    int aValue = a.Value;
                                    var bRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle>(a.EmptyMiddles);
                                    foreach (var bChunk in bRows)
                                    {
                                        if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle> bChunkView)
                                        {
                                            if (bChunkView.Source is Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle[] bChunkViewArray)
                                            {
                                                int bChunkViewOffset = bChunkView.Offset;
                                                for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                                {
                                                    if ((bIndex & 1023) == 0)
                                                    {
                                                        token.ThrowIfCancellationRequested();
                                                    }

                                                    var b = bChunkViewArray[bChunkViewOffset + bIndex];
                                                    __musoqFinalShapeRows.Add(new ResultShape0(aValue, b.VolatileValue));
                                                }

                                                continue;
                                            }

                                            if (bChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle> bChunkViewList)
                                            {
                                                int bChunkViewOffset = bChunkView.Offset;
                                                for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                                {
                                                    if ((bIndex & 1023) == 0)
                                                    {
                                                        token.ThrowIfCancellationRequested();
                                                    }

                                                    var b = bChunkViewList[bChunkViewOffset + bIndex];
                                                    __musoqFinalShapeRows.Add(new ResultShape0(aValue, b.VolatileValue));
                                                }

                                                continue;
                                            }
                                        }

                                        for (int bIndex = 0, bIndexCount = bChunk.Count; bIndex < bIndexCount; ++bIndex)
                                        {
                                            if ((bIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var b = bChunk[bIndex];
                                            __musoqFinalShapeRows.Add(new ResultShape0(aValue, b.VolatileValue));
                                        }
                                    }
                                }

                                continue;
                            }

                            if (aChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleOuter> aChunkViewList)
                            {
                                int aChunkViewOffset = aChunkView.Offset;
                                for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                                {
                                    if ((aIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var a = aChunkViewList[aChunkViewOffset + aIndex];
                                    int aValue = a.Value;
                                    var bRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle>(a.EmptyMiddles);
                                    foreach (var bChunk in bRows)
                                    {
                                        if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle> bChunkView)
                                        {
                                            if (bChunkView.Source is Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle[] bChunkViewArray)
                                            {
                                                int bChunkViewOffset = bChunkView.Offset;
                                                for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                                {
                                                    if ((bIndex & 1023) == 0)
                                                    {
                                                        token.ThrowIfCancellationRequested();
                                                    }

                                                    var b = bChunkViewArray[bChunkViewOffset + bIndex];
                                                    __musoqFinalShapeRows.Add(new ResultShape0(aValue, b.VolatileValue));
                                                }

                                                continue;
                                            }

                                            if (bChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle> bChunkViewList)
                                            {
                                                int bChunkViewOffset = bChunkView.Offset;
                                                for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                                {
                                                    if ((bIndex & 1023) == 0)
                                                    {
                                                        token.ThrowIfCancellationRequested();
                                                    }

                                                    var b = bChunkViewList[bChunkViewOffset + bIndex];
                                                    __musoqFinalShapeRows.Add(new ResultShape0(aValue, b.VolatileValue));
                                                }

                                                continue;
                                            }
                                        }

                                        for (int bIndex = 0, bIndexCount = bChunk.Count; bIndex < bIndexCount; ++bIndex)
                                        {
                                            if ((bIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var b = bChunk[bIndex];
                                            __musoqFinalShapeRows.Add(new ResultShape0(aValue, b.VolatileValue));
                                        }
                                    }
                                }

                                continue;
                            }
                        }

                        for (int aIndex = 0, aIndexCount = aChunk.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunk[aIndex];
                            int aValue = a.Value;
                            var bRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle>(a.EmptyMiddles);
                            foreach (var bChunk in bRows)
                            {
                                if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle> bChunkView)
                                {
                                    if (bChunkView.Source is Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle[] bChunkViewArray)
                                    {
                                        int bChunkViewOffset = bChunkView.Offset;
                                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                        {
                                            if ((bIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var b = bChunkViewArray[bChunkViewOffset + bIndex];
                                            __musoqFinalShapeRows.Add(new ResultShape0(aValue, b.VolatileValue));
                                        }

                                        continue;
                                    }

                                    if (bChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle> bChunkViewList)
                                    {
                                        int bChunkViewOffset = bChunkView.Offset;
                                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                        {
                                            if ((bIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var b = bChunkViewList[bChunkViewOffset + bIndex];
                                            __musoqFinalShapeRows.Add(new ResultShape0(aValue, b.VolatileValue));
                                        }

                                        continue;
                                    }
                                }

                                for (int bIndex = 0, bIndexCount = bChunk.Count; bIndex < bIndexCount; ++bIndex)
                                {
                                    if ((bIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var b = bChunk[bIndex];
                                    __musoqFinalShapeRows.Add(new ResultShape0(aValue, b.VolatileValue));
                                }
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, int __value1)
            {
                a_Value = __value0;
                b_VolatileValue = __value1;
            }

            public override int Count => 2;
            public int a_Value { get; private set; }
            public int b_VolatileValue { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        a_Value = (int)value;
                        break;
                    case 1:
                        b_VolatileValue = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "a.Value" => true,
                "a_Value" => true,
                "Value" => true,
                "b.VolatileValue" => true,
                "b_VolatileValue" => true,
                "VolatileValue" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)a_Value,
                1 => (object)b_VolatileValue,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "a.Value" => (object)a_Value,
                "a_Value" => (object)a_Value,
                "Value" => (object)a_Value,
                "b.VolatileValue" => (object)b_VolatileValue,
                "b_VolatileValue" => (object)b_VolatileValue,
                "VolatileValue" => (object)b_VolatileValue,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int a_Value, int b_VolatileValue)
            {
                this.a_Value = a_Value;
                this.b_VolatileValue = b_VolatileValue;
            }

            public int a_Value { get; }
            public int b_VolatileValue { get; }
        }
    }
}
