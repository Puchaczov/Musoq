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
  Project [a.Value as Value, b.VolatileValue as VolatileValue]
    CteRef [ab as ab]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.Value as a.Value, a.EmptyMiddles as a.EmptyMiddles, b.VolatileValue as b.VolatileValue]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#licm.outers() as a]
      PhysicalPropertySource [a.EmptyMiddles as b] [apply: Cross] [type: LoopInvariantSampleMiddle[]]
  PhysicalProject [a.Value as Value, b.VolatileValue as VolatileValue]
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
      Value: int <- field Value
      VolatileValue: int <- field VolatileValue

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [a: LoopInvariantSampleOuter] -> aRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    ChunkedForEach [a in aRows]
      Let [aValue: int = a.Value]
      EnumerableSource [a.EmptyMiddles -> bRows]
      ChunkedForEach [b in bRows]
        AppendShape [result <- ResultRow0(Value: aValue, VolatileValue: b.VolatileValue)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Value", typeof(int), 0),
            new Column("VolatileValue", typeof(int), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Value", typeof(int), 0), new Column("EmptyMiddles", typeof(Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle[]), 1) });
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
                    var __aSchema = provider.GetSchema("#licm");
                    var aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleOuter>.Open(__aSchema, "outers", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
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
                                                    __musoqFinalShapeRows.Add(new ResultRow0(aValue, b.VolatileValue));
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
                                                    __musoqFinalShapeRows.Add(new ResultRow0(aValue, b.VolatileValue));
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
                                            __musoqFinalShapeRows.Add(new ResultRow0(aValue, b.VolatileValue));
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
                                                    __musoqFinalShapeRows.Add(new ResultRow0(aValue, b.VolatileValue));
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
                                                    __musoqFinalShapeRows.Add(new ResultRow0(aValue, b.VolatileValue));
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
                                            __musoqFinalShapeRows.Add(new ResultRow0(aValue, b.VolatileValue));
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
                                            __musoqFinalShapeRows.Add(new ResultRow0(aValue, b.VolatileValue));
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
                                            __musoqFinalShapeRows.Add(new ResultRow0(aValue, b.VolatileValue));
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
                                    __musoqFinalShapeRows.Add(new ResultRow0(aValue, b.VolatileValue));
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
                Value = __value0;
                VolatileValue = __value1;
            }

            public override int Count => 2;
            public int Value { get; private set; }
            public int VolatileValue { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.VolatileValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.VolatileValue),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Value" => true,
                "VolatileValue" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Value, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.VolatileValue, ref visitor);
            }
        }
    }
}
