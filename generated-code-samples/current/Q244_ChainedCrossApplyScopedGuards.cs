// === Parsed Query ===
/*
select m.Value as Value from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m where i.Name = 'left' and n.Value = 1
*/

// === Logical Plan ===
/*
MultiStatement
  Project [m.Value as Value]
    Filter [((i.Name = 'left') AND (n.Value = 1))]
      Apply [Cross]
        Apply [Cross]
          SchemaScan [#apply.items() as i]
          PropertySource [i.Numbers as n] [apply: Cross] [type: Int32[]]
        PropertySource [i.Numbers as m] [apply: Cross] [type: Int32[]]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [m.Value as Value]
    PhysicalFilter [((i.Name = 'left') AND (n.Value = 1))]
      PhysicalNestedLoopApply [Cross] [guards: PreApplyRight: (n.Value = 1)]
        PhysicalNestedLoopApply [Cross] [guards: PreApplyRight: (i.Name = 'left')]
          PhysicalSchemaScan [#apply.items() as i] [pushdown: (i.Name = 'left')]
          PhysicalPropertySource [i.Numbers as n] [apply: Cross] [type: Int32[]]
        PhysicalPropertySource [i.Numbers as m] [apply: Cross] [type: Int32[]]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [i: GeneratedApplySampleEntity]
      Name: string <- property Name
      Numbers: int[] <- property Numbers
    SourceEntity [n: int]
      Value: int <- direct scalar value
    SourceEntity [m: int]
      Value: int <- direct scalar value
    Generated [ResultRow0]
      Value: int <- field Value

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [i: GeneratedApplySampleEntity] -> iRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [i in iRows]
      ContinueIf [NOT (i.Name = 'left')]
      EnumerableSource [i.Numbers -> nRows]
      ChunkedForEach [n in nRows]
        ContinueIf [NOT (n.Value = 1)]
        EnumerableSource [i.Numbers -> mRows]
        ChunkedForEach [m in mRows]
          AppendShape [result <- ResultShape0(Value: m.Value)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q244_ChainedCrossApplyScopedGuards
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
            new Column("Value", typeof(int), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_i_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("Numbers", typeof(int[]), 1) });
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
                yield return new ResultRow0(__musoqShapeRow.Value);
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
                var __iSchema = provider.GetSchema("#apply");
                var iRowsSource = __iSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>("items", new SourceExecutionContext("i:1", sourceExecutionPlans["i:1"], token, __schemaColumns_compiled_i_0, sourceRuntimeSettingsBySourceContextId["i:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var iRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>(iRowsSource.Chunks, __musoqProgressContext, "i:1") : iRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Where);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var iChunk in iRows)
                {
                    if (iChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity> iChunkView)
                    {
                        if (iChunkView.Source is Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity[] iChunkViewArray)
                        {
                            int iChunkViewOffset = iChunkView.Offset;
                            for (int iIndex = 0, iIndexCount = iChunkView.Count; iIndex < iIndexCount; ++iIndex)
                            {
                                if ((iIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var i = iChunkViewArray[iChunkViewOffset + iIndex];
                                if ((!(i.Name == "left")))
                                {
                                    continue;
                                }

                                var nRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
                                foreach (var nChunk in nRows)
                                {
                                    if (nChunk is global::Musoq.Schema.DataSources.RowChunk<int> nChunkView)
                                    {
                                        if (nChunkView.Source is int[] nChunkViewArray)
                                        {
                                            int nChunkViewOffset = nChunkView.Offset;
                                            for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                                            {
                                                if ((nIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                var n = nChunkViewArray[nChunkViewOffset + nIndex];
                                                if ((!(n == 1)))
                                                {
                                                    continue;
                                                }

                                                var mRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
                                                foreach (var mChunk in mRows)
                                                {
                                                    if (mChunk is global::Musoq.Schema.DataSources.RowChunk<int> mChunkView)
                                                    {
                                                        if (mChunkView.Source is int[] mChunkViewArray)
                                                        {
                                                            int mChunkViewOffset = mChunkView.Offset;
                                                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                                            {
                                                                if ((mIndex & 1023) == 0)
                                                                {
                                                                    token.ThrowIfCancellationRequested();
                                                                }

                                                                var m = mChunkViewArray[mChunkViewOffset + mIndex];
                                                                __musoqFinalShapeRows.Add(new ResultShape0(m));
                                                            }

                                                            continue;
                                                        }

                                                        if (mChunkView.Source is List<int> mChunkViewList)
                                                        {
                                                            int mChunkViewOffset = mChunkView.Offset;
                                                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                                            {
                                                                if ((mIndex & 1023) == 0)
                                                                {
                                                                    token.ThrowIfCancellationRequested();
                                                                }

                                                                var m = mChunkViewList[mChunkViewOffset + mIndex];
                                                                __musoqFinalShapeRows.Add(new ResultShape0(m));
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
                                                        __musoqFinalShapeRows.Add(new ResultShape0(m));
                                                    }
                                                }
                                            }

                                            continue;
                                        }

                                        if (nChunkView.Source is List<int> nChunkViewList)
                                        {
                                            int nChunkViewOffset = nChunkView.Offset;
                                            for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                                            {
                                                if ((nIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                var n = nChunkViewList[nChunkViewOffset + nIndex];
                                                if ((!(n == 1)))
                                                {
                                                    continue;
                                                }

                                                var mRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
                                                foreach (var mChunk in mRows)
                                                {
                                                    if (mChunk is global::Musoq.Schema.DataSources.RowChunk<int> mChunkView)
                                                    {
                                                        if (mChunkView.Source is int[] mChunkViewArray)
                                                        {
                                                            int mChunkViewOffset = mChunkView.Offset;
                                                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                                            {
                                                                if ((mIndex & 1023) == 0)
                                                                {
                                                                    token.ThrowIfCancellationRequested();
                                                                }

                                                                var m = mChunkViewArray[mChunkViewOffset + mIndex];
                                                                __musoqFinalShapeRows.Add(new ResultShape0(m));
                                                            }

                                                            continue;
                                                        }

                                                        if (mChunkView.Source is List<int> mChunkViewList)
                                                        {
                                                            int mChunkViewOffset = mChunkView.Offset;
                                                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                                            {
                                                                if ((mIndex & 1023) == 0)
                                                                {
                                                                    token.ThrowIfCancellationRequested();
                                                                }

                                                                var m = mChunkViewList[mChunkViewOffset + mIndex];
                                                                __musoqFinalShapeRows.Add(new ResultShape0(m));
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
                                                        __musoqFinalShapeRows.Add(new ResultShape0(m));
                                                    }
                                                }
                                            }

                                            continue;
                                        }
                                    }

                                    for (int nIndex = 0, nIndexCount = nChunk.Count; nIndex < nIndexCount; ++nIndex)
                                    {
                                        if ((nIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var n = nChunk[nIndex];
                                        if ((!(n == 1)))
                                        {
                                            continue;
                                        }

                                        var mRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
                                        foreach (var mChunk in mRows)
                                        {
                                            if (mChunk is global::Musoq.Schema.DataSources.RowChunk<int> mChunkView)
                                            {
                                                if (mChunkView.Source is int[] mChunkViewArray)
                                                {
                                                    int mChunkViewOffset = mChunkView.Offset;
                                                    for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                                    {
                                                        if ((mIndex & 1023) == 0)
                                                        {
                                                            token.ThrowIfCancellationRequested();
                                                        }

                                                        var m = mChunkViewArray[mChunkViewOffset + mIndex];
                                                        __musoqFinalShapeRows.Add(new ResultShape0(m));
                                                    }

                                                    continue;
                                                }

                                                if (mChunkView.Source is List<int> mChunkViewList)
                                                {
                                                    int mChunkViewOffset = mChunkView.Offset;
                                                    for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                                    {
                                                        if ((mIndex & 1023) == 0)
                                                        {
                                                            token.ThrowIfCancellationRequested();
                                                        }

                                                        var m = mChunkViewList[mChunkViewOffset + mIndex];
                                                        __musoqFinalShapeRows.Add(new ResultShape0(m));
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
                                                __musoqFinalShapeRows.Add(new ResultShape0(m));
                                            }
                                        }
                                    }
                                }
                            }

                            continue;
                        }

                        if (iChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity> iChunkViewList)
                        {
                            int iChunkViewOffset = iChunkView.Offset;
                            for (int iIndex = 0, iIndexCount = iChunkView.Count; iIndex < iIndexCount; ++iIndex)
                            {
                                if ((iIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var i = iChunkViewList[iChunkViewOffset + iIndex];
                                if ((!(i.Name == "left")))
                                {
                                    continue;
                                }

                                var nRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
                                foreach (var nChunk in nRows)
                                {
                                    if (nChunk is global::Musoq.Schema.DataSources.RowChunk<int> nChunkView)
                                    {
                                        if (nChunkView.Source is int[] nChunkViewArray)
                                        {
                                            int nChunkViewOffset = nChunkView.Offset;
                                            for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                                            {
                                                if ((nIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                var n = nChunkViewArray[nChunkViewOffset + nIndex];
                                                if ((!(n == 1)))
                                                {
                                                    continue;
                                                }

                                                var mRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
                                                foreach (var mChunk in mRows)
                                                {
                                                    if (mChunk is global::Musoq.Schema.DataSources.RowChunk<int> mChunkView)
                                                    {
                                                        if (mChunkView.Source is int[] mChunkViewArray)
                                                        {
                                                            int mChunkViewOffset = mChunkView.Offset;
                                                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                                            {
                                                                if ((mIndex & 1023) == 0)
                                                                {
                                                                    token.ThrowIfCancellationRequested();
                                                                }

                                                                var m = mChunkViewArray[mChunkViewOffset + mIndex];
                                                                __musoqFinalShapeRows.Add(new ResultShape0(m));
                                                            }

                                                            continue;
                                                        }

                                                        if (mChunkView.Source is List<int> mChunkViewList)
                                                        {
                                                            int mChunkViewOffset = mChunkView.Offset;
                                                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                                            {
                                                                if ((mIndex & 1023) == 0)
                                                                {
                                                                    token.ThrowIfCancellationRequested();
                                                                }

                                                                var m = mChunkViewList[mChunkViewOffset + mIndex];
                                                                __musoqFinalShapeRows.Add(new ResultShape0(m));
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
                                                        __musoqFinalShapeRows.Add(new ResultShape0(m));
                                                    }
                                                }
                                            }

                                            continue;
                                        }

                                        if (nChunkView.Source is List<int> nChunkViewList)
                                        {
                                            int nChunkViewOffset = nChunkView.Offset;
                                            for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                                            {
                                                if ((nIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                var n = nChunkViewList[nChunkViewOffset + nIndex];
                                                if ((!(n == 1)))
                                                {
                                                    continue;
                                                }

                                                var mRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
                                                foreach (var mChunk in mRows)
                                                {
                                                    if (mChunk is global::Musoq.Schema.DataSources.RowChunk<int> mChunkView)
                                                    {
                                                        if (mChunkView.Source is int[] mChunkViewArray)
                                                        {
                                                            int mChunkViewOffset = mChunkView.Offset;
                                                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                                            {
                                                                if ((mIndex & 1023) == 0)
                                                                {
                                                                    token.ThrowIfCancellationRequested();
                                                                }

                                                                var m = mChunkViewArray[mChunkViewOffset + mIndex];
                                                                __musoqFinalShapeRows.Add(new ResultShape0(m));
                                                            }

                                                            continue;
                                                        }

                                                        if (mChunkView.Source is List<int> mChunkViewList)
                                                        {
                                                            int mChunkViewOffset = mChunkView.Offset;
                                                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                                            {
                                                                if ((mIndex & 1023) == 0)
                                                                {
                                                                    token.ThrowIfCancellationRequested();
                                                                }

                                                                var m = mChunkViewList[mChunkViewOffset + mIndex];
                                                                __musoqFinalShapeRows.Add(new ResultShape0(m));
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
                                                        __musoqFinalShapeRows.Add(new ResultShape0(m));
                                                    }
                                                }
                                            }

                                            continue;
                                        }
                                    }

                                    for (int nIndex = 0, nIndexCount = nChunk.Count; nIndex < nIndexCount; ++nIndex)
                                    {
                                        if ((nIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var n = nChunk[nIndex];
                                        if ((!(n == 1)))
                                        {
                                            continue;
                                        }

                                        var mRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
                                        foreach (var mChunk in mRows)
                                        {
                                            if (mChunk is global::Musoq.Schema.DataSources.RowChunk<int> mChunkView)
                                            {
                                                if (mChunkView.Source is int[] mChunkViewArray)
                                                {
                                                    int mChunkViewOffset = mChunkView.Offset;
                                                    for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                                    {
                                                        if ((mIndex & 1023) == 0)
                                                        {
                                                            token.ThrowIfCancellationRequested();
                                                        }

                                                        var m = mChunkViewArray[mChunkViewOffset + mIndex];
                                                        __musoqFinalShapeRows.Add(new ResultShape0(m));
                                                    }

                                                    continue;
                                                }

                                                if (mChunkView.Source is List<int> mChunkViewList)
                                                {
                                                    int mChunkViewOffset = mChunkView.Offset;
                                                    for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                                    {
                                                        if ((mIndex & 1023) == 0)
                                                        {
                                                            token.ThrowIfCancellationRequested();
                                                        }

                                                        var m = mChunkViewList[mChunkViewOffset + mIndex];
                                                        __musoqFinalShapeRows.Add(new ResultShape0(m));
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
                                                __musoqFinalShapeRows.Add(new ResultShape0(m));
                                            }
                                        }
                                    }
                                }
                            }

                            continue;
                        }
                    }

                    for (int iIndex = 0, iIndexCount = iChunk.Count; iIndex < iIndexCount; ++iIndex)
                    {
                        if ((iIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var i = iChunk[iIndex];
                        if ((!(i.Name == "left")))
                        {
                            continue;
                        }

                        var nRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
                        foreach (var nChunk in nRows)
                        {
                            if (nChunk is global::Musoq.Schema.DataSources.RowChunk<int> nChunkView)
                            {
                                if (nChunkView.Source is int[] nChunkViewArray)
                                {
                                    int nChunkViewOffset = nChunkView.Offset;
                                    for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                                    {
                                        if ((nIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var n = nChunkViewArray[nChunkViewOffset + nIndex];
                                        if ((!(n == 1)))
                                        {
                                            continue;
                                        }

                                        var mRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
                                        foreach (var mChunk in mRows)
                                        {
                                            if (mChunk is global::Musoq.Schema.DataSources.RowChunk<int> mChunkView)
                                            {
                                                if (mChunkView.Source is int[] mChunkViewArray)
                                                {
                                                    int mChunkViewOffset = mChunkView.Offset;
                                                    for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                                    {
                                                        if ((mIndex & 1023) == 0)
                                                        {
                                                            token.ThrowIfCancellationRequested();
                                                        }

                                                        var m = mChunkViewArray[mChunkViewOffset + mIndex];
                                                        __musoqFinalShapeRows.Add(new ResultShape0(m));
                                                    }

                                                    continue;
                                                }

                                                if (mChunkView.Source is List<int> mChunkViewList)
                                                {
                                                    int mChunkViewOffset = mChunkView.Offset;
                                                    for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                                    {
                                                        if ((mIndex & 1023) == 0)
                                                        {
                                                            token.ThrowIfCancellationRequested();
                                                        }

                                                        var m = mChunkViewList[mChunkViewOffset + mIndex];
                                                        __musoqFinalShapeRows.Add(new ResultShape0(m));
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
                                                __musoqFinalShapeRows.Add(new ResultShape0(m));
                                            }
                                        }
                                    }

                                    continue;
                                }

                                if (nChunkView.Source is List<int> nChunkViewList)
                                {
                                    int nChunkViewOffset = nChunkView.Offset;
                                    for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                                    {
                                        if ((nIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var n = nChunkViewList[nChunkViewOffset + nIndex];
                                        if ((!(n == 1)))
                                        {
                                            continue;
                                        }

                                        var mRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
                                        foreach (var mChunk in mRows)
                                        {
                                            if (mChunk is global::Musoq.Schema.DataSources.RowChunk<int> mChunkView)
                                            {
                                                if (mChunkView.Source is int[] mChunkViewArray)
                                                {
                                                    int mChunkViewOffset = mChunkView.Offset;
                                                    for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                                    {
                                                        if ((mIndex & 1023) == 0)
                                                        {
                                                            token.ThrowIfCancellationRequested();
                                                        }

                                                        var m = mChunkViewArray[mChunkViewOffset + mIndex];
                                                        __musoqFinalShapeRows.Add(new ResultShape0(m));
                                                    }

                                                    continue;
                                                }

                                                if (mChunkView.Source is List<int> mChunkViewList)
                                                {
                                                    int mChunkViewOffset = mChunkView.Offset;
                                                    for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                                    {
                                                        if ((mIndex & 1023) == 0)
                                                        {
                                                            token.ThrowIfCancellationRequested();
                                                        }

                                                        var m = mChunkViewList[mChunkViewOffset + mIndex];
                                                        __musoqFinalShapeRows.Add(new ResultShape0(m));
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
                                                __musoqFinalShapeRows.Add(new ResultShape0(m));
                                            }
                                        }
                                    }

                                    continue;
                                }
                            }

                            for (int nIndex = 0, nIndexCount = nChunk.Count; nIndex < nIndexCount; ++nIndex)
                            {
                                if ((nIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var n = nChunk[nIndex];
                                if ((!(n == 1)))
                                {
                                    continue;
                                }

                                var mRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
                                foreach (var mChunk in mRows)
                                {
                                    if (mChunk is global::Musoq.Schema.DataSources.RowChunk<int> mChunkView)
                                    {
                                        if (mChunkView.Source is int[] mChunkViewArray)
                                        {
                                            int mChunkViewOffset = mChunkView.Offset;
                                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                            {
                                                if ((mIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                var m = mChunkViewArray[mChunkViewOffset + mIndex];
                                                __musoqFinalShapeRows.Add(new ResultShape0(m));
                                            }

                                            continue;
                                        }

                                        if (mChunkView.Source is List<int> mChunkViewList)
                                        {
                                            int mChunkViewOffset = mChunkView.Offset;
                                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                            {
                                                if ((mIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                var m = mChunkViewList[mChunkViewOffset + mIndex];
                                                __musoqFinalShapeRows.Add(new ResultShape0(m));
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
                                        __musoqFinalShapeRows.Add(new ResultShape0(m));
                                    }
                                }
                            }
                        }
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0)
            {
                Value = __value0;
            }

            public override int Count => 1;
            public int Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Value = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Value" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Value,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Value" => (object)Value,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Value)
            {
                this.Value = Value;
            }

            public int Value { get; }
        }
    }
}
