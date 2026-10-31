// === Parsed Query ===
/*
select a.VolatileValue, b.Value from #licm.outers() a outer apply a.Middles b where a.VolatileValue > 0
*/

// === Logical Plan ===
/*
MultiStatement
  Project [a.VolatileValue as a.VolatileValue, a.Middles as a.Middles, b.Value as b.Value]
    Apply [Outer]
      SchemaScan [#licm.outers() as a]
      PropertySource [a.Middles as b] [apply: Cross] [type: LoopInvariantSampleMiddle[]]
  Project [a.VolatileValue as VolatileValue, b.Value as Value]
    Filter [(a.VolatileValue > 0)]
      CteRef [ab as ab]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.VolatileValue as a.VolatileValue, b.Value as b.Value]
    PhysicalNestedLoopApply [Outer]
      PhysicalSchemaScan [#licm.outers() as a]
      PhysicalPropertySource [a.Middles as b] [apply: Cross] [type: LoopInvariantSampleMiddle[]]
  PhysicalProject [a.VolatileValue as VolatileValue, b.Value as Value]
    PhysicalFilter [(a.VolatileValue > 0)]
      PhysicalCteRef [ab as ab]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: LoopInvariantSampleOuter]
      VolatileValue: int <- property VolatileValue
      Middles: LoopInvariantSampleMiddle[] <- property Middles
    SourceEntity [b: LoopInvariantSampleMiddle]
      Id: int <- property Id
      Value: int <- property Value
      VolatileValue: int <- property VolatileValue
      Leaves: LoopInvariantSampleLeaf[] <- property Leaves
    Generated [Statement0Row0]
      a.VolatileValue: int <- field a_VolatileValue
      b.Value: int? <- field b_Value
    TableRow [ab]
      a.VolatileValue: int <- field a_VolatileValue
      b.Value: int? <- field b_Value
    Generated [ResultRow0]
      VolatileValue: int <- field VolatileValue
      Value: int? <- field Value

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [a: LoopInvariantSampleOuter] -> statement0_aRows
    CreateTable [statement0: Statement0Row0]
    PhaseBoundary [Where]
    ChunkedForEach [a in statement0_aRows]
      EnumerableSource [a.Middles -> statement0_bRows]
      Let [bHasMatch: bool = FALSE]
      ChunkedForEach [b in statement0_bRows]
        Assign [bHasMatch = TRUE]
        AppendRow [statement0 <- Statement0Row0(a.VolatileValue: a.VolatileValue, b.Value: b.Value)]
      If [NOT bHasMatch]
        AppendRow [statement0 <- Statement0Row0(a.VolatileValue: a.VolatileValue, b.Value: NULL)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ForEach [ab in _cteRowResults.Slot0]
      Let [a_VolatileValue: int = ab.a.VolatileValue]
      If [(a_VolatileValue > 0)]
        AppendShape [result <- ResultRow0(VolatileValue: a_VolatileValue, Value: ab.b.Value)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q259_GuardedVolatileOuterApplyPredicate
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("VolatileValue", typeof(int), 0),
            new Column("Value", typeof(int?), 1)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("a.VolatileValue", typeof(int), 0),
            new Column("b.Value", typeof(int?), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("VolatileValue", typeof(int), 0), new Column("Middles", typeof(Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle[]), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultRow0>();
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
                    int a_VolatileValue = ab.a_VolatileValue;
                    if ((a_VolatileValue > 0))
                    {
                        __musoqFinalShapeRows.Add(new ResultRow0(a_VolatileValue, ab.b_Value));
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
            var statement0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleOuter>.Open(__statement0_aSchema, "outers", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
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
                            var statement0_bRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle>(a.Middles);
                            bool bHasMatch = false;
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
                                            bHasMatch = true;
                                            statement0.Add(new Statement0Row0(a.VolatileValue, b.Value));
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
                                            bHasMatch = true;
                                            statement0.Add(new Statement0Row0(a.VolatileValue, b.Value));
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
                                    bHasMatch = true;
                                    statement0.Add(new Statement0Row0(a.VolatileValue, b.Value));
                                }
                            }

                            if ((!bHasMatch))
                            {
                                statement0.Add(new Statement0Row0(a.VolatileValue, null));
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
                            var statement0_bRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle>(a.Middles);
                            bool bHasMatch = false;
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
                                            bHasMatch = true;
                                            statement0.Add(new Statement0Row0(a.VolatileValue, b.Value));
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
                                            bHasMatch = true;
                                            statement0.Add(new Statement0Row0(a.VolatileValue, b.Value));
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
                                    bHasMatch = true;
                                    statement0.Add(new Statement0Row0(a.VolatileValue, b.Value));
                                }
                            }

                            if ((!bHasMatch))
                            {
                                statement0.Add(new Statement0Row0(a.VolatileValue, null));
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
                    var statement0_bRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleMiddle>(a.Middles);
                    bool bHasMatch = false;
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
                                    bHasMatch = true;
                                    statement0.Add(new Statement0Row0(a.VolatileValue, b.Value));
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
                                    bHasMatch = true;
                                    statement0.Add(new Statement0Row0(a.VolatileValue, b.Value));
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
                            bHasMatch = true;
                            statement0.Add(new Statement0Row0(a.VolatileValue, b.Value));
                        }
                    }

                    if ((!bHasMatch))
                    {
                        statement0.Add(new Statement0Row0(a.VolatileValue, null));
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
            public ResultRow0(int __value0, int? __value1)
            {
                VolatileValue = __value0;
                Value = __value1;
            }

            public override int Count => 2;
            public int? Value { get; private set; }
            public int VolatileValue { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.VolatileValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.VolatileValue),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "VolatileValue" => true,
                "Value" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.VolatileValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Value, ref visitor);
            }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(int __value0, int? __value1)
            {
                a_VolatileValue = __value0;
                b_Value = __value1;
            }

            public int a_VolatileValue { get; }
            public int? b_Value { get; }
        }
    }
}
