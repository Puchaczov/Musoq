// === Parsed Query ===
/*
select m.PatternId, m.MatchText
from #inputs.match(
    'TODO FIXME',
    patterns: array {
        (Id: 'todo', Pattern: 'TODO'),
        (Pattern: 'FIXME', Id: 'fixme'),
    }
) m
*/

// === Logical Plan ===
/*
MultiStatement
  Project [m.PatternId as PatternId, m.MatchText as MatchText]
    SchemaScan [#inputs.match('TODO FIXME', array { (Id: 'todo', Pattern: 'TODO'), (Pattern: 'FIXME', Id: 'fixme') }) as m]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [m.PatternId as PatternId, m.MatchText as MatchText]
    PhysicalSchemaScan [#inputs.match('TODO FIXME', array { (Id: 'todo', Pattern: 'TODO'), (Pattern: 'FIXME', Id: 'fixme') }) as m]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [m: PatternMatchRow]
      PatternId: string <- property PatternId
      MatchText: string <- property MatchText
    Generated [ResultRow0]
      PatternId: string <- field PatternId
      MatchText: string <- field MatchText
  StructuralPreparation
    Musoq.Examples.DataSources.StructuredInputs.PatternInput; constructor=constructor:clr:Musoq.Examples.DataSources.StructuredInputs.PatternInput@Musoq.Examples.DataSources.StructuredInputs(primitive:string,primitive:string,primitive:string); shape=(Id: string?, Pattern: string?); origin=Inline; lifetime=Inline; metrics=Runtime; ownership=ConstructFresh; limits=depth=32;nodes=100000;strings=67108864; defaults=-,-,ExecutionLiteral { InterpretationShape = , ReturnType = string, Value = string:006C00690074006500720061006C }
    Musoq.Examples.DataSources.StructuredInputs.PatternInput; constructor=constructor:clr:Musoq.Examples.DataSources.StructuredInputs.PatternInput@Musoq.Examples.DataSources.StructuredInputs(primitive:string,primitive:string,primitive:string); shape=(Pattern: string?, Id: string?); origin=Inline; lifetime=Inline; metrics=Runtime; ownership=ConstructFresh; limits=depth=32;nodes=100000;strings=67108864; defaults=-,-,ExecutionLiteral { InterpretationShape = , ReturnType = string, Value = string:006C00690074006500720061006C }

  Body
    PhaseBoundary [Begin]
    Let [__musoqStructural_m_0: string = 'todo']
    Let [__musoqStructural_m_1: string = 'TODO']
    PrepareStructuralInput [__musoqStructural_m_2: Musoq.Examples.DataSources.StructuredInputs.PatternInput <- (Id: __musoqStructural_m_0, Pattern: __musoqStructural_m_1); lifetime Inline; shape (Id: string?, Pattern: string?)]
    Let [__musoqStructural_m_3: string = 'FIXME']
    Let [__musoqStructural_m_4: string = 'fixme']
    PrepareStructuralInput [__musoqStructural_m_5: Musoq.Examples.DataSources.StructuredInputs.PatternInput <- (Pattern: __musoqStructural_m_3, Id: __musoqStructural_m_4); lifetime Inline; shape (Pattern: string?, Id: string?)]
    Let [__musoqStructural_m_6: IReadOnlyList<PatternInput> = array { __musoqStructural_m_2, __musoqStructural_m_5 }]
    PhaseBoundary [From]
    SourceScan [m: PatternMatchRow] -> mRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [m in mRows]
      AppendShape [result <- ResultRow0(PatternId: m.PatternId, MatchText: m.MatchText)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q331_StructuredRecordArray
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
            new Column("PatternId", typeof(string), 0),
            new Column("MatchText", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_m_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("PatternId", typeof(string), 0), new Column("MatchText", typeof(string), 1) });
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
                string __musoqStructural_m_0 = "todo";
                string __musoqStructural_m_1 = "TODO";
                Musoq.Examples.DataSources.StructuredInputs.PatternInput __musoqStructural_m_2 = new Musoq.Examples.DataSources.StructuredInputs.PatternInput(__musoqStructural_m_0, __musoqStructural_m_1, "literal");
                string __musoqStructural_m_3 = "FIXME";
                string __musoqStructural_m_4 = "fixme";
                Musoq.Examples.DataSources.StructuredInputs.PatternInput __musoqStructural_m_5 = new Musoq.Examples.DataSources.StructuredInputs.PatternInput(__musoqStructural_m_4, __musoqStructural_m_3, "literal");
                IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.PatternInput> __musoqStructural_m_6 = new Musoq.Examples.DataSources.StructuredInputs.PatternInput[]
                {
                    __musoqStructural_m_2,
                    __musoqStructural_m_5
                };
                OnPhaseChanged("compiled", QueryPhase.From);
                var __mSchema = provider.GetSchema("#inputs");
                var mRowsSourceContext = new SourceExecutionContext("m:1", sourceExecutionPlans["m:1"], token, __schemaColumns_compiled_m_0, sourceRuntimeSettingsBySourceContextId["m:1"], logger, OnDataSourceProgress);
                Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow> mRowsSource;
                try
                {
                    Musoq.Examples.DataSources.StructuredInputs.MatchSource mRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.MatchSource(__musoqCheckStructural_d97fc6504a38a2bf("TODO FIXME", token), __musoqCheckStructural_389b095bd9c4e750(__musoqStructural_m_6, token), mRowsSourceContext);
                    mRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.MatchSource, Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow>(__mSchema, "match", mRowsSourceInstance, mRowsSourceContext, "#inputs", "m", "m:1");
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
                    throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "match", "m", "m:1", exception);
                }

                var mRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow>(mRowsSource.Chunks, __musoqProgressContext, "m:1") : mRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var mChunk in mRows)
                {
                    if (mChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow> mChunkView)
                    {
                        if (mChunkView.Source is Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow[] mChunkViewArray)
                        {
                            int mChunkViewOffset = mChunkView.Offset;
                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                            {
                                if ((mIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var m = mChunkViewArray[mChunkViewOffset + mIndex];
                                __musoqFinalShapeRows.Add(new ResultRow0(m.PatternId, m.MatchText));
                            }

                            continue;
                        }

                        if (mChunkView.Source is List<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow> mChunkViewList)
                        {
                            int mChunkViewOffset = mChunkView.Offset;
                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                            {
                                if ((mIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var m = mChunkViewList[mChunkViewOffset + mIndex];
                                __musoqFinalShapeRows.Add(new ResultRow0(m.PatternId, m.MatchText));
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
                        __musoqFinalShapeRows.Add(new ResultRow0(m.PatternId, m.MatchText));
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

        private static IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.PatternInput> __musoqCheckStructural_389b095bd9c4e750(IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.PatternInput> value, System.Threading.CancellationToken token)
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
                var __musoqStructural_collection1 = (System.Collections.Generic.IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.PatternInput>)value;
                var __musoqStructural_count2 = __musoqStructural_collection1.Count;
                for (var __musoqStructural_index0 = 0; __musoqStructural_index0 < __musoqStructural_count2; __musoqStructural_index0++)
                {
                    if ((__musoqStructural_index0 & 1023) == 0)
                        token.ThrowIfCancellationRequested();
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (2 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 2;
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (3 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 3;
                    if (__musoqStructural_collection1[__musoqStructural_index0].Id != null)
                        __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)__musoqStructural_collection1[__musoqStructural_index0].Id.Length * 2L));
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (3 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 3;
                    if (__musoqStructural_collection1[__musoqStructural_index0].Pattern != null)
                        __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)__musoqStructural_collection1[__musoqStructural_index0].Pattern.Length * 2L));
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (3 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 3;
                    if (__musoqStructural_collection1[__musoqStructural_index0].Mode != null)
                        __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)__musoqStructural_collection1[__musoqStructural_index0].Mode.Length * 2L));
                }
            }

            global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("inline", "m.argument[1]", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "m:1");
            return value;
        }

        private static string __musoqCheckStructural_d97fc6504a38a2bf(string value, System.Threading.CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            long __musoqStructuralNodes = 0L;
            long __musoqStructuralStrings = 0L;
            int __musoqStructuralMaxDepth = 0;
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (1 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 1;
            if (value != null)
                __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)value.Length * 2L));
            global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("inline", "m.argument[0]", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "m:1");
            return value;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                PatternId = __value0;
                MatchText = __value1;
            }

            public override int Count => 2;
            public string MatchText { get; private set; }
            public string PatternId { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.PatternId = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.MatchText = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.PatternId),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.MatchText),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "PatternId" => true,
                "MatchText" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.PatternId, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.MatchText, ref visitor);
            }
        }
    }
}
