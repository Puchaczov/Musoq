// === Parsed Query ===
/*
select a.Value, b.Value from #licm.outers() a cross apply a.Middles b where a.Value > 0 and b.Value > 0
*/

// === Logical Plan ===
/*
MultiStatement
  Project [a.Value as a.Value, a.Middles as a.Middles, b.Value as b.Value]
    Apply [Cross]
      SchemaScan [#licm.outers() as a]
      PropertySource [a.Middles as b] [apply: Cross] [type: LoopInvariantSampleMiddle[]]
  Project [a.Value as a.Value, b.Value as b.Value]
    Filter [((a.Value > 0) AND (b.Value > 0))]
      CteRef [ab as ab]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.Value as a.Value, a.Middles as a.Middles, b.Value as b.Value]
    PhysicalNestedLoopApply [Cross] [guards: PreApplyRight: (a.Value > 0)]
      PhysicalSchemaScan [#licm.outers() as a] [pushdown: (a.Value > 0)]
      PhysicalPropertySource [a.Middles as b] [apply: Cross] [type: LoopInvariantSampleMiddle[]]
  PhysicalProject [a.Value as a.Value, b.Value as b.Value]
    PhysicalFilter [((a.Value > 0) AND (b.Value > 0))]
      PhysicalCteRef [ab as ab]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: LoopInvariantSampleOuter]
      Value: int <- property Value
      Middles: LoopInvariantSampleMiddle[] <- property Middles
    SourceEntity [b: LoopInvariantSampleMiddle]
      Id: int <- property Id
      Value: int <- property Value
      VolatileValue: int <- property VolatileValue
      Leaves: LoopInvariantSampleLeaf[] <- property Leaves
    Generated [Statement0Row0]
      a.Value: int <- field a_Value
      a.Middles: LoopInvariantSampleMiddle[] <- field a_Middles
      b.Value: int <- field b_Value
    TableRow [ab]
      a.Value: int <- field a_Value
      a.Middles: LoopInvariantSampleMiddle[] <- field a_Middles
      b.Value: int <- field b_Value
    Generated [ResultRow0]
      a.Value: int <- field a_Value
      b.Value: int <- field b_Value

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [a: LoopInvariantSampleOuter] -> statement0_aRows
    CreateTable [statement0: Statement0Row0]
    PhaseBoundary [Where]
    ChunkedForEach [a in statement0_aRows]
      Let [aValue: int = a.Value]
      ContinueIf [NOT (a.Value > 0)]
      EnumerableSource [a.Middles -> statement0_bRows]
      ChunkedForEach [b in statement0_bRows]
        AppendRow [statement0 <- Statement0Row0(a.Value: aValue, a.Middles: a.Middles, b.Value: b.Value)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ForEach [ab in _cteRowResults.Slot0]
      Let [b_Value: int = ab.b.Value]
      If [(b_Value > 0)]
        AppendShape [result <- ResultShape0(a.Value: ab.a.Value, b.Value: b_Value)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q258_GuardedStableApplyPredicate
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("a.Value", typeof(int), 0),
            new Column("b.Value", typeof(int), 1)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("a.Value", typeof(int), 0),
            new Column("a.Middles", typeof(Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle[]), 1),
            new Column("b.Value", typeof(int), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Value", typeof(int), 1), new Column("Middles", typeof(Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle[]), 3) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.a_Value, __musoqShapeRow.b_Value);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Where);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled", QueryPhase.Select);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 ab = __storedTable0Rows[__storedTable0Index];
                    int b_Value = ab.b_Value;
                    if ((b_Value > 0))
                    {
                        __musoqFinalShapeRows.Add(new ResultShape0(ab.a_Value, b_Value));
                    }
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            var __statement0_aSchema = provider.GetSchema("#licm");
            var statement0_aRowsSource = __statement0_aSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleOuter>("outers", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var statement0_aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleOuter>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            foreach (var aChunk in statement0_aRows)
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
                            if ((!(a.Value > 0)))
                            {
                                continue;
                            }

                            var statement0_bRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle>(a.Middles);
                            foreach (var bChunk in statement0_bRows)
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
                                            statement0.Add(new Statement0Row0(aValue, a.Middles, b.Value));
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
                                            statement0.Add(new Statement0Row0(aValue, a.Middles, b.Value));
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
                                    statement0.Add(new Statement0Row0(aValue, a.Middles, b.Value));
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
                            if ((!(a.Value > 0)))
                            {
                                continue;
                            }

                            var statement0_bRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle>(a.Middles);
                            foreach (var bChunk in statement0_bRows)
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
                                            statement0.Add(new Statement0Row0(aValue, a.Middles, b.Value));
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
                                            statement0.Add(new Statement0Row0(aValue, a.Middles, b.Value));
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
                                    statement0.Add(new Statement0Row0(aValue, a.Middles, b.Value));
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
                    if ((!(a.Value > 0)))
                    {
                        continue;
                    }

                    var statement0_bRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle>(a.Middles);
                    foreach (var bChunk in statement0_bRows)
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
                                    statement0.Add(new Statement0Row0(aValue, a.Middles, b.Value));
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
                                    statement0.Add(new Statement0Row0(aValue, a.Middles, b.Value));
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
                            statement0.Add(new Statement0Row0(aValue, a.Middles, b.Value));
                        }
                    }
                }
            }

            return statement0;
        }

        private sealed class CteRowResults
        {
            public List<Statement0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, int __value1)
            {
                a_Value = __value0;
                b_Value = __value1;
            }

            public override int Count => 2;
            public int a_Value { get; private set; }
            public int b_Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        a_Value = (int)value;
                        break;
                    case 1:
                        b_Value = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "a.Value" => true,
                "a_Value" => true,
                "b.Value" => true,
                "b_Value" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)a_Value,
                1 => (object)b_Value,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "a.Value" => (object)a_Value,
                "a_Value" => (object)a_Value,
                "b.Value" => (object)b_Value,
                "b_Value" => (object)b_Value,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int a_Value, int b_Value)
            {
                this.a_Value = a_Value;
                this.b_Value = b_Value;
            }

            public int a_Value { get; }
            public int b_Value { get; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(int __value0, Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle[] __value1, int __value2)
            {
                a_Value = __value0;
                a_Middles = __value1;
                b_Value = __value2;
            }

            public Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle[] a_Middles { get; }
            public int a_Value { get; }
            public int b_Value { get; }
        }
    }
}
