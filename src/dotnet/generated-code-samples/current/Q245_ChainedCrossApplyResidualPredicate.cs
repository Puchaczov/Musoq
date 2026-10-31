// === Parsed Query ===
/*
select m.Value as Value from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m where i.Name = 'left' and n.Value = 1 and m.Value = 2
*/

// === Logical Plan ===
/*
MultiStatement
  Project [m.Value as Value]
    Filter [(((i.Name = 'left') AND (n.Value = 1)) AND (m.Value = 2))]
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
    PhysicalFilter [(((i.Name = 'left') AND (n.Value = 1)) AND (m.Value = 2))]
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
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [i in iRows]
      ContinueIf [NOT ((i.Name = 'left') = TRUE)]
      EnumerableSource [i.Numbers -> nRows]
      ForEach [n in nRows]
        ContinueIf [NOT (n.Value = 1)]
        EnumerableSource [i.Numbers -> mRows]
        ForEach [m in mRows]
          Let [value: int = m.Value]
          If [(value = 2)]
            AppendShape [result <- ResultRow0(Value: value)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q245_ChainedCrossApplyResidualPredicate
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
            new Column("Value", typeof(int), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_i_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("Numbers", typeof(int[]), 1) });
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
                var __iSchema = provider.GetSchema("#apply");
                var iRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>.Open(__iSchema, "items", new SourceExecutionContext("i:1", sourceExecutionPlans["i:1"], token, __schemaColumns_compiled_i_0, sourceRuntimeSettingsBySourceContextId["i:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
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
                                if ((!(Operators.SqlCompare<string, string>(i.Name, "left", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) == true)))
                                {
                                    continue;
                                }

                                var nRows = (IEnumerable<int>)i.Numbers ?? Array.Empty<int>();
                                var nIndexRows = nRows;
                                if (nIndexRows is int[] nIndexArray)
                                    for (int nIndex = 0, nIndexCount = nIndexArray.Length; nIndex < nIndexCount; ++nIndex)
                                    {
                                        if ((nIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        int n = nIndexArray[nIndex];
                                        if ((!(n == 1)))
                                        {
                                            continue;
                                        }

                                        var mRows = (IEnumerable<int>)i.Numbers ?? Array.Empty<int>();
                                        var mIndexRows = mRows;
                                        if (mIndexRows is int[] mIndexArray)
                                            for (int mIndex = 0, mIndexCount = mIndexArray.Length; mIndex < mIndexCount; ++mIndex)
                                            {
                                                if ((mIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                int m = mIndexArray[mIndex];
                                                int value = m;
                                                if ((value == 2))
                                                {
                                                    __musoqFinalShapeRows.Add(new ResultRow0(value));
                                                }
                                            }
                                        else if (mIndexRows is List<int> mIndexConcreteList)
                                            for (int mIndex = 0, mIndexCount = mIndexConcreteList.Count; mIndex < mIndexCount; ++mIndex)
                                            {
                                                if ((mIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                int m = mIndexConcreteList[mIndex];
                                                int value = m;
                                                if ((value == 2))
                                                {
                                                    __musoqFinalShapeRows.Add(new ResultRow0(value));
                                                }
                                            }
                                        else
                                        {
                                            int mIndex = 0;
                                            foreach (var m in mIndexRows)
                                            {
                                                token.ThrowIfCancellationRequested();
                                                int value = m;
                                                if ((value == 2))
                                                {
                                                    __musoqFinalShapeRows.Add(new ResultRow0(value));
                                                }

                                                ++mIndex;
                                            }
                                        }
                                    }
                                else if (nIndexRows is List<int> nIndexConcreteList)
                                    for (int nIndex = 0, nIndexCount = nIndexConcreteList.Count; nIndex < nIndexCount; ++nIndex)
                                    {
                                        if ((nIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        int n = nIndexConcreteList[nIndex];
                                        if ((!(n == 1)))
                                        {
                                            continue;
                                        }

                                        var mRows = (IEnumerable<int>)i.Numbers ?? Array.Empty<int>();
                                        var mIndexRows = mRows;
                                        if (mIndexRows is int[] mIndexArray)
                                            for (int mIndex = 0, mIndexCount = mIndexArray.Length; mIndex < mIndexCount; ++mIndex)
                                            {
                                                if ((mIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                int m = mIndexArray[mIndex];
                                                int value = m;
                                                if ((value == 2))
                                                {
                                                    __musoqFinalShapeRows.Add(new ResultRow0(value));
                                                }
                                            }
                                        else if (mIndexRows is List<int> mIndexConcreteList)
                                            for (int mIndex = 0, mIndexCount = mIndexConcreteList.Count; mIndex < mIndexCount; ++mIndex)
                                            {
                                                if ((mIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                int m = mIndexConcreteList[mIndex];
                                                int value = m;
                                                if ((value == 2))
                                                {
                                                    __musoqFinalShapeRows.Add(new ResultRow0(value));
                                                }
                                            }
                                        else
                                        {
                                            int mIndex = 0;
                                            foreach (var m in mIndexRows)
                                            {
                                                token.ThrowIfCancellationRequested();
                                                int value = m;
                                                if ((value == 2))
                                                {
                                                    __musoqFinalShapeRows.Add(new ResultRow0(value));
                                                }

                                                ++mIndex;
                                            }
                                        }
                                    }
                                else
                                {
                                    int nIndex = 0;
                                    foreach (var n in nIndexRows)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        if ((!(n == 1)))
                                        {
                                            continue;
                                        }

                                        var mRows = (IEnumerable<int>)i.Numbers ?? Array.Empty<int>();
                                        var mIndexRows = mRows;
                                        if (mIndexRows is int[] mIndexArray)
                                            for (int mIndex = 0, mIndexCount = mIndexArray.Length; mIndex < mIndexCount; ++mIndex)
                                            {
                                                if ((mIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                int m = mIndexArray[mIndex];
                                                int value = m;
                                                if ((value == 2))
                                                {
                                                    __musoqFinalShapeRows.Add(new ResultRow0(value));
                                                }
                                            }
                                        else if (mIndexRows is List<int> mIndexConcreteList)
                                            for (int mIndex = 0, mIndexCount = mIndexConcreteList.Count; mIndex < mIndexCount; ++mIndex)
                                            {
                                                if ((mIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                int m = mIndexConcreteList[mIndex];
                                                int value = m;
                                                if ((value == 2))
                                                {
                                                    __musoqFinalShapeRows.Add(new ResultRow0(value));
                                                }
                                            }
                                        else
                                        {
                                            int mIndex = 0;
                                            foreach (var m in mIndexRows)
                                            {
                                                token.ThrowIfCancellationRequested();
                                                int value = m;
                                                if ((value == 2))
                                                {
                                                    __musoqFinalShapeRows.Add(new ResultRow0(value));
                                                }

                                                ++mIndex;
                                            }
                                        }

                                        ++nIndex;
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
                                if ((!(Operators.SqlCompare<string, string>(i.Name, "left", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) == true)))
                                {
                                    continue;
                                }

                                var nRows = (IEnumerable<int>)i.Numbers ?? Array.Empty<int>();
                                var nIndexRows = nRows;
                                if (nIndexRows is int[] nIndexArray)
                                    for (int nIndex = 0, nIndexCount = nIndexArray.Length; nIndex < nIndexCount; ++nIndex)
                                    {
                                        if ((nIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        int n = nIndexArray[nIndex];
                                        if ((!(n == 1)))
                                        {
                                            continue;
                                        }

                                        var mRows = (IEnumerable<int>)i.Numbers ?? Array.Empty<int>();
                                        var mIndexRows = mRows;
                                        if (mIndexRows is int[] mIndexArray)
                                            for (int mIndex = 0, mIndexCount = mIndexArray.Length; mIndex < mIndexCount; ++mIndex)
                                            {
                                                if ((mIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                int m = mIndexArray[mIndex];
                                                int value = m;
                                                if ((value == 2))
                                                {
                                                    __musoqFinalShapeRows.Add(new ResultRow0(value));
                                                }
                                            }
                                        else if (mIndexRows is List<int> mIndexConcreteList)
                                            for (int mIndex = 0, mIndexCount = mIndexConcreteList.Count; mIndex < mIndexCount; ++mIndex)
                                            {
                                                if ((mIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                int m = mIndexConcreteList[mIndex];
                                                int value = m;
                                                if ((value == 2))
                                                {
                                                    __musoqFinalShapeRows.Add(new ResultRow0(value));
                                                }
                                            }
                                        else
                                        {
                                            int mIndex = 0;
                                            foreach (var m in mIndexRows)
                                            {
                                                token.ThrowIfCancellationRequested();
                                                int value = m;
                                                if ((value == 2))
                                                {
                                                    __musoqFinalShapeRows.Add(new ResultRow0(value));
                                                }

                                                ++mIndex;
                                            }
                                        }
                                    }
                                else if (nIndexRows is List<int> nIndexConcreteList)
                                    for (int nIndex = 0, nIndexCount = nIndexConcreteList.Count; nIndex < nIndexCount; ++nIndex)
                                    {
                                        if ((nIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        int n = nIndexConcreteList[nIndex];
                                        if ((!(n == 1)))
                                        {
                                            continue;
                                        }

                                        var mRows = (IEnumerable<int>)i.Numbers ?? Array.Empty<int>();
                                        var mIndexRows = mRows;
                                        if (mIndexRows is int[] mIndexArray)
                                            for (int mIndex = 0, mIndexCount = mIndexArray.Length; mIndex < mIndexCount; ++mIndex)
                                            {
                                                if ((mIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                int m = mIndexArray[mIndex];
                                                int value = m;
                                                if ((value == 2))
                                                {
                                                    __musoqFinalShapeRows.Add(new ResultRow0(value));
                                                }
                                            }
                                        else if (mIndexRows is List<int> mIndexConcreteList)
                                            for (int mIndex = 0, mIndexCount = mIndexConcreteList.Count; mIndex < mIndexCount; ++mIndex)
                                            {
                                                if ((mIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                int m = mIndexConcreteList[mIndex];
                                                int value = m;
                                                if ((value == 2))
                                                {
                                                    __musoqFinalShapeRows.Add(new ResultRow0(value));
                                                }
                                            }
                                        else
                                        {
                                            int mIndex = 0;
                                            foreach (var m in mIndexRows)
                                            {
                                                token.ThrowIfCancellationRequested();
                                                int value = m;
                                                if ((value == 2))
                                                {
                                                    __musoqFinalShapeRows.Add(new ResultRow0(value));
                                                }

                                                ++mIndex;
                                            }
                                        }
                                    }
                                else
                                {
                                    int nIndex = 0;
                                    foreach (var n in nIndexRows)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        if ((!(n == 1)))
                                        {
                                            continue;
                                        }

                                        var mRows = (IEnumerable<int>)i.Numbers ?? Array.Empty<int>();
                                        var mIndexRows = mRows;
                                        if (mIndexRows is int[] mIndexArray)
                                            for (int mIndex = 0, mIndexCount = mIndexArray.Length; mIndex < mIndexCount; ++mIndex)
                                            {
                                                if ((mIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                int m = mIndexArray[mIndex];
                                                int value = m;
                                                if ((value == 2))
                                                {
                                                    __musoqFinalShapeRows.Add(new ResultRow0(value));
                                                }
                                            }
                                        else if (mIndexRows is List<int> mIndexConcreteList)
                                            for (int mIndex = 0, mIndexCount = mIndexConcreteList.Count; mIndex < mIndexCount; ++mIndex)
                                            {
                                                if ((mIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                int m = mIndexConcreteList[mIndex];
                                                int value = m;
                                                if ((value == 2))
                                                {
                                                    __musoqFinalShapeRows.Add(new ResultRow0(value));
                                                }
                                            }
                                        else
                                        {
                                            int mIndex = 0;
                                            foreach (var m in mIndexRows)
                                            {
                                                token.ThrowIfCancellationRequested();
                                                int value = m;
                                                if ((value == 2))
                                                {
                                                    __musoqFinalShapeRows.Add(new ResultRow0(value));
                                                }

                                                ++mIndex;
                                            }
                                        }

                                        ++nIndex;
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
                        if ((!(Operators.SqlCompare<string, string>(i.Name, "left", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) == true)))
                        {
                            continue;
                        }

                        var nRows = (IEnumerable<int>)i.Numbers ?? Array.Empty<int>();
                        var nIndexRows = nRows;
                        if (nIndexRows is int[] nIndexArray)
                            for (int nIndex = 0, nIndexCount = nIndexArray.Length; nIndex < nIndexCount; ++nIndex)
                            {
                                if ((nIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                int n = nIndexArray[nIndex];
                                if ((!(n == 1)))
                                {
                                    continue;
                                }

                                var mRows = (IEnumerable<int>)i.Numbers ?? Array.Empty<int>();
                                var mIndexRows = mRows;
                                if (mIndexRows is int[] mIndexArray)
                                    for (int mIndex = 0, mIndexCount = mIndexArray.Length; mIndex < mIndexCount; ++mIndex)
                                    {
                                        if ((mIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        int m = mIndexArray[mIndex];
                                        int value = m;
                                        if ((value == 2))
                                        {
                                            __musoqFinalShapeRows.Add(new ResultRow0(value));
                                        }
                                    }
                                else if (mIndexRows is List<int> mIndexConcreteList)
                                    for (int mIndex = 0, mIndexCount = mIndexConcreteList.Count; mIndex < mIndexCount; ++mIndex)
                                    {
                                        if ((mIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        int m = mIndexConcreteList[mIndex];
                                        int value = m;
                                        if ((value == 2))
                                        {
                                            __musoqFinalShapeRows.Add(new ResultRow0(value));
                                        }
                                    }
                                else
                                {
                                    int mIndex = 0;
                                    foreach (var m in mIndexRows)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        int value = m;
                                        if ((value == 2))
                                        {
                                            __musoqFinalShapeRows.Add(new ResultRow0(value));
                                        }

                                        ++mIndex;
                                    }
                                }
                            }
                        else if (nIndexRows is List<int> nIndexConcreteList)
                            for (int nIndex = 0, nIndexCount = nIndexConcreteList.Count; nIndex < nIndexCount; ++nIndex)
                            {
                                if ((nIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                int n = nIndexConcreteList[nIndex];
                                if ((!(n == 1)))
                                {
                                    continue;
                                }

                                var mRows = (IEnumerable<int>)i.Numbers ?? Array.Empty<int>();
                                var mIndexRows = mRows;
                                if (mIndexRows is int[] mIndexArray)
                                    for (int mIndex = 0, mIndexCount = mIndexArray.Length; mIndex < mIndexCount; ++mIndex)
                                    {
                                        if ((mIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        int m = mIndexArray[mIndex];
                                        int value = m;
                                        if ((value == 2))
                                        {
                                            __musoqFinalShapeRows.Add(new ResultRow0(value));
                                        }
                                    }
                                else if (mIndexRows is List<int> mIndexConcreteList)
                                    for (int mIndex = 0, mIndexCount = mIndexConcreteList.Count; mIndex < mIndexCount; ++mIndex)
                                    {
                                        if ((mIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        int m = mIndexConcreteList[mIndex];
                                        int value = m;
                                        if ((value == 2))
                                        {
                                            __musoqFinalShapeRows.Add(new ResultRow0(value));
                                        }
                                    }
                                else
                                {
                                    int mIndex = 0;
                                    foreach (var m in mIndexRows)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        int value = m;
                                        if ((value == 2))
                                        {
                                            __musoqFinalShapeRows.Add(new ResultRow0(value));
                                        }

                                        ++mIndex;
                                    }
                                }
                            }
                        else
                        {
                            int nIndex = 0;
                            foreach (var n in nIndexRows)
                            {
                                token.ThrowIfCancellationRequested();
                                if ((!(n == 1)))
                                {
                                    continue;
                                }

                                var mRows = (IEnumerable<int>)i.Numbers ?? Array.Empty<int>();
                                var mIndexRows = mRows;
                                if (mIndexRows is int[] mIndexArray)
                                    for (int mIndex = 0, mIndexCount = mIndexArray.Length; mIndex < mIndexCount; ++mIndex)
                                    {
                                        if ((mIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        int m = mIndexArray[mIndex];
                                        int value = m;
                                        if ((value == 2))
                                        {
                                            __musoqFinalShapeRows.Add(new ResultRow0(value));
                                        }
                                    }
                                else if (mIndexRows is List<int> mIndexConcreteList)
                                    for (int mIndex = 0, mIndexCount = mIndexConcreteList.Count; mIndex < mIndexCount; ++mIndex)
                                    {
                                        if ((mIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        int m = mIndexConcreteList[mIndex];
                                        int value = m;
                                        if ((value == 2))
                                        {
                                            __musoqFinalShapeRows.Add(new ResultRow0(value));
                                        }
                                    }
                                else
                                {
                                    int mIndex = 0;
                                    foreach (var m in mIndexRows)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        int value = m;
                                        if ((value == 2))
                                        {
                                            __musoqFinalShapeRows.Add(new ResultRow0(value));
                                        }

                                        ++mIndex;
                                    }
                                }

                                ++nIndex;
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

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Value" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Value, ref visitor);
            }
        }
    }
}
