// === Parsed Query ===
/*
select i.Name as Name, Min(n.Value) as RepeatedMin, Min(distinct n.Value) as DistinctMin, Max(n.Value) as RepeatedMax, Max(distinct n.Value) as DistinctMax, RowNumber() over (order by Max(distinct n.Value) desc, Max(n.Value) desc, Min(distinct n.Value), Min(n.Value), i.Name) as MixedMinMaxRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by MixedMinMaxRowNo
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [WindowRef(0)]
    Project [i.Name as Name, AggRef(inm.Min(n.Value)) as RepeatedMin, AggRef(inm.Min(distinct n.Value)) as DistinctMin, AggRef(inm.Max(n.Value)) as RepeatedMax, AggRef(inm.Max(distinct n.Value)) as DistinctMax, WindowRef(0) as MixedMinMaxRowNo]
      Window [RowNumber(idx:0; order: AggRef(inm.Max(distinct n.Value)) DESC, AggRef(inm.Max(n.Value)) DESC, AggRef(inm.Min(distinct n.Value)), AggRef(inm.Min(n.Value)), i.Name)]
        Aggregate [keys: i.Name] [aggs: RepeatedMin, DistinctMin, RepeatedMax, DistinctMax]
          Apply [Cross]
            Apply [Cross]
              SchemaScan [#apply.items() as i]
              PropertySource [i.Numbers as n] [apply: Cross] [type: Int32[]]
            PropertySource [i.Numbers as m] [apply: Cross] [type: Int32[]]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [WindowRef(0)]
    PhysicalProject [i.Name as Name, AggRef(inm.Min(n.Value)) as RepeatedMin, AggRef(inm.Min(distinct n.Value)) as DistinctMin, AggRef(inm.Max(n.Value)) as RepeatedMax, AggRef(inm.Max(distinct n.Value)) as DistinctMax, WindowRef(0) as MixedMinMaxRowNo]
      PhysicalWindow [RowNumber(idx:0; order: AggRef(inm.Max(distinct n.Value)) DESC, AggRef(inm.Max(n.Value)) DESC, AggRef(inm.Min(distinct n.Value)), AggRef(inm.Min(n.Value)), i.Name)]
        PhysicalMaterialize
          PhysicalSingleKeyAggregate [key: i.Name (String)] [aggs: RepeatedMin, DistinctMin, RepeatedMax, DistinctMax]
            PhysicalNestedLoopApply [Cross]
              PhysicalNestedLoopApply [Cross]
                PhysicalSchemaScan [#apply.items() as i]
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
    AggregateGroup [WindowSourceTableAggregateGroup; keys: 1; typed aggs: 4]
    Generated [WindowSourceRow0]
      i.Name: string <- field i_Name
      RepeatedMin: int? <- field RepeatedMin
      DistinctMin: int? <- field DistinctMin
      RepeatedMax: int? <- field RepeatedMax
      DistinctMax: int? <- field DistinctMax
    TableRow [windowSource]
      i.Name: string <- field i_Name
      RepeatedMin: int? <- field RepeatedMin
      DistinctMin: int? <- field DistinctMin
      RepeatedMax: int? <- field RepeatedMax
      DistinctMax: int? <- field DistinctMax
    Generated [ResultRow0]
      Name: string <- field Name
      RepeatedMin: int? <- field RepeatedMin
      DistinctMin: int? <- field DistinctMin
      RepeatedMax: int? <- field RepeatedMax
      DistinctMax: int? <- field DistinctMax
      MixedMinMaxRowNo: long <- field MixedMinMaxRowNo

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [i: GeneratedApplySampleEntity] -> windowSourceTable_iRows
    CreateRowBuffer [windowSourceTable: List<WindowSourceRow0>]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [groups: string -> WindowSourceTableAggregateGroup]
    PhaseBoundary [Select]
    ChunkedForEach [i in windowSourceTable_iRows]
      EnumerableSource [i.Numbers -> windowSourceTable_nRows]
      ChunkedForEach [n in windowSourceTable_nRows]
        Let [nValue: int = n.Value]
        EnumerableSource [i.Numbers -> windowSourceTable_mRows]
        ChunkedForEach [m in windowSourceTable_mRows]
          GetOrAddSingleKeyAggregateGroup [group = groups[i.Name] by i.Name; typed: WindowSourceTableAggregateGroup]
          TypedAggregateSet [Set(group.__agg0, nValue)]
          TypedAggregateSet [Set(group.__agg1, nValue)]
          TypedAggregateSet [Set(group.__agg2, nValue)]
          TypedAggregateSet [Set(group.__agg3, nValue)]
    EnsureRowBufferCapacity [windowSourceTable <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendRowBuffer [windowSourceTable <- WindowSourceRow0(i.Name: finalGroup.i.Name, RepeatedMin: inm.Min(n.Value), DistinctMin: inm.Min(distinct n.Value), RepeatedMax: inm.Max(n.Value), DistinctMax: inm.Max(distinct n.Value))]
    Materialize [windowSourceTable -> resultWindowRows]
    ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows order by windowSource.DistinctMax DESC, windowSource.RepeatedMax DESC, windowSource.DistinctMin ASC, windowSource.RepeatedMin ASC, windowSource.i.Name ASC]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, windowSource in resultWindowRows]
      AppendShape [result <- ResultShape0(Name: windowSource.i.Name, RepeatedMin: windowSource.RepeatedMin, DistinctMin: windowSource.DistinctMin, RepeatedMax: windowSource.RepeatedMax, DistinctMax: windowSource.DistinctMax, MixedMinMaxRowNo: resultRowNumbers[windowIndex])]
    SortShapeRows [result -> resultSorted by MixedMinMaxRowNo ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q72_ChainedApplyMixedDistinctMinMaxAggregateWindow
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
            new Column("Name", typeof(string), 0),
            new Column("RepeatedMin", typeof(int?), 1),
            new Column("DistinctMin", typeof(int?), 2),
            new Column("RepeatedMax", typeof(int?), 3),
            new Column("DistinctMax", typeof(int?), 4),
            new Column("MixedMinMaxRowNo", typeof(long), 5)
        };
        private static readonly Column[] __columns_compiled_windowSourceTable_1 = new Column[]
        {
            new Column("i.Name", typeof(string), 0),
            new Column("RepeatedMin", typeof(int?), 1),
            new Column("DistinctMin", typeof(int?), 2),
            new Column("RepeatedMax", typeof(int?), 3),
            new Column("DistinctMax", typeof(int?), 4)
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.RepeatedMin, __musoqShapeRow.DistinctMin, __musoqShapeRow.RepeatedMax, __musoqShapeRow.DistinctMax, __musoqShapeRow.MixedMinMaxRowNo);
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
                var __windowSourceTable_iSchema = provider.GetSchema("#apply");
                var windowSourceTable_iRowsSource = __windowSourceTable_iSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>("items", new SourceExecutionContext("i:1", sourceExecutionPlans["i:1"], token, __schemaColumns_compiled_i_0, sourceRuntimeSettingsBySourceContextId["i:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var windowSourceTable_iRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>(windowSourceTable_iRowsSource.Chunks, __musoqProgressContext, "i:1") : windowSourceTable_iRowsSource.Chunks;
                var windowSourceTable = new List<WindowSourceRow0>();
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var groupsToFinalize = new List<WindowSourceTableAggregateGroup>();
                var groups = new Dictionary<string, WindowSourceTableAggregateGroup>();
                WindowSourceTableAggregateGroup nullGroup = null;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var iChunk in windowSourceTable_iRows)
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
                                TraverseWindowSourceTableNRows(token, i, groupsToFinalize, groups, ref nullGroup);
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
                                TraverseWindowSourceTableNRows(token, i, groupsToFinalize, groups, ref nullGroup);
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
                        TraverseWindowSourceTableNRows(token, i, groupsToFinalize, groups, ref nullGroup);
                    }
                }

                windowSourceTable.EnsureCapacity(groupsToFinalize.Count);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    windowSourceTable.Add(new WindowSourceRow0(finalGroup.__key0, finalGroup.__agg0.HasValue ? (int?)finalGroup.__agg0.Value : null, Musoq.Plugins.MinDistinctAggregateKernel<int>.Get(in finalGroup.__agg1), finalGroup.__agg2.HasValue ? (int?)finalGroup.__agg2.Value : null, Musoq.Plugins.MaxDistinctAggregateKernel<int>.Get(in finalGroup.__agg3)));
                }

                var resultWindowRows = EvaluationHelper.MaterializeGeneratedRows<WindowSourceRow0>(windowSourceTable);
                var resultRowNumbersOrderKeys = new WindowResultRowNumbersOrderKeysKey[resultWindowRows.Count];
                ExtractResultRowNumbersWindowKeys(resultWindowRows, resultRowNumbersOrderKeys);
                var resultRowNumbersPartitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count, null);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultRowNumbersPartitions, resultRowNumbersOrderKeys, false);
                var resultRowNumbers = new long[resultWindowRows.Count];
                for (int resultRowNumbersPartitionSetIndex = 0; resultRowNumbersPartitionSetIndex < resultRowNumbersPartitions.PartitionCount; ++resultRowNumbersPartitionSetIndex)
                {
                    var resultRowNumbersPartitionStart = resultRowNumbersPartitions.GetStart(resultRowNumbersPartitionSetIndex);
                    var resultRowNumbersPartitionCount = resultRowNumbersPartitions.GetLength(resultRowNumbersPartitionSetIndex);
                    var resultRowNumbersPartitionIndices = resultRowNumbersPartitions.Indices;
                    var resultRowNumbersPartitionLimit = resultRowNumbersPartitionCount;
                    for (int resultRowNumbersPartitionIndex = 0; resultRowNumbersPartitionIndex < resultRowNumbersPartitionLimit; ++resultRowNumbersPartitionIndex)
                    {
                        var resultRowNumbersCurrentIndex = resultRowNumbersPartitionIndices[resultRowNumbersPartitionStart + resultRowNumbersPartitionIndex];
                        resultRowNumbers[resultRowNumbersCurrentIndex] = resultRowNumbersPartitionIndex + 1L;
                    }
                }

                var result = new List<ResultShape0>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    WindowSourceRow0 windowSource = resultWindowRows[windowIndex];
                    result.Add(new ResultShape0(windowSource.i_Name, windowSource.RepeatedMin, windowSource.DistinctMin, windowSource.RepeatedMax, windowSource.DistinctMax, (long)resultRowNumbers[windowIndex]));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.MixedMinMaxRowNo.CompareTo(right.MixedMinMaxRowNo);
                    if (comparison != 0)
                        return comparison;
                    return 0;
                }));
                foreach (var resultSortedRowsRow in resultSortedRows)
                {
                    __musoqFinalShapeRows.Add(resultSortedRowsRow);
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
        private static void ExtractResultRowNumbersWindowKeys(IReadOnlyList<WindowSourceRow0> resultWindowRows, WindowResultRowNumbersOrderKeysKey[] resultRowNumbersOrderKeys)
        {
            for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
            {
                WindowSourceRow0 windowSource = resultWindowRows[windowIndex];
                resultRowNumbersOrderKeys[windowIndex] = new WindowResultRowNumbersOrderKeysKey(windowSource.DistinctMax, windowSource.RepeatedMax, windowSource.DistinctMin, windowSource.RepeatedMin, windowSource.i_Name);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void TraverseWindowSourceTableMRows(CancellationToken token, int n, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i, List<WindowSourceTableAggregateGroup> groupsToFinalize, Dictionary<string, WindowSourceTableAggregateGroup> groups, ref WindowSourceTableAggregateGroup nullGroup)
        {
            token.ThrowIfCancellationRequested();
            int nValue = n;
            var windowSourceTable_mRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
            foreach (var mChunk in windowSourceTable_mRows)
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
                            UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, nValue, i);
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
                            UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, nValue, i);
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
                    UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, nValue, i);
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void TraverseWindowSourceTableNRows(CancellationToken token, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i, List<WindowSourceTableAggregateGroup> groupsToFinalize, Dictionary<string, WindowSourceTableAggregateGroup> groups, ref WindowSourceTableAggregateGroup nullGroup)
        {
            token.ThrowIfCancellationRequested();
            var windowSourceTable_nRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
            foreach (var nChunk in windowSourceTable_nRows)
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
                            TraverseWindowSourceTableMRows(token, n, i, groupsToFinalize, groups, ref nullGroup);
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
                            TraverseWindowSourceTableMRows(token, n, i, groupsToFinalize, groups, ref nullGroup);
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
                    TraverseWindowSourceTableMRows(token, n, i, groupsToFinalize, groups, ref nullGroup);
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void UpdateGroupsAggregates(List<WindowSourceTableAggregateGroup> groupsToFinalize, Dictionary<string, WindowSourceTableAggregateGroup> groups, ref WindowSourceTableAggregateGroup nullGroup, int nValue, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i)
        {
            string groupKey = i.Name;
            WindowSourceTableAggregateGroup group = null;
            if (groupKey != null)
            {
                ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var groupExists);
                if (!groupExists)
                {
                    groupRef = new WindowSourceTableAggregateGroup(groupKey);
                    groupsToFinalize.Add(groupRef);
                }

                group = groupRef;
            }
            else
            {
                if (nullGroup == null)
                {
                    nullGroup = new WindowSourceTableAggregateGroup(null);
                    groupsToFinalize.Add(nullGroup);
                }

                group = nullGroup;
            }

            {
                var __agg0Input = (int?)nValue;
                if (__agg0Input.HasValue)
                {
                    var __agg0Current = __agg0Input.GetValueOrDefault();
                    if (!group.__agg0.HasValue || __agg0Current < group.__agg0.Value)
                    {
                        group.__agg0.Value = __agg0Current;
                    }

                    group.__agg0.HasValue = true;
                }
            }

            Musoq.Plugins.MinDistinctAggregateKernel<int>.Set(ref group.__agg1, (int?)nValue);
            {
                var __agg2Input = (int?)nValue;
                if (__agg2Input.HasValue)
                {
                    var __agg2Current = __agg2Input.GetValueOrDefault();
                    if (!group.__agg2.HasValue || __agg2Current > group.__agg2.Value)
                    {
                        group.__agg2.Value = __agg2Current;
                    }

                    group.__agg2.HasValue = true;
                }
            }

            Musoq.Plugins.MaxDistinctAggregateKernel<int>.Set(ref group.__agg3, (int?)nValue);
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, int? __value1, int? __value2, int? __value3, int? __value4, long __value5)
            {
                Name = __value0;
                RepeatedMin = __value1;
                DistinctMin = __value2;
                RepeatedMax = __value3;
                DistinctMax = __value4;
                MixedMinMaxRowNo = __value5;
            }

            public override int Count => 6;
            public int? DistinctMax { get; private set; }
            public int? DistinctMin { get; private set; }
            public long MixedMinMaxRowNo { get; private set; }
            public string Name { get; private set; }
            public int? RepeatedMax { get; private set; }
            public int? RepeatedMin { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        RepeatedMin = (int?)value;
                        break;
                    case 2:
                        DistinctMin = (int?)value;
                        break;
                    case 3:
                        RepeatedMax = (int?)value;
                        break;
                    case 4:
                        DistinctMax = (int?)value;
                        break;
                    case 5:
                        MixedMinMaxRowNo = (long)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "RepeatedMin" => true,
                "DistinctMin" => true,
                "RepeatedMax" => true,
                "DistinctMax" => true,
                "MixedMinMaxRowNo" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)RepeatedMin,
                2 => (object)DistinctMin,
                3 => (object)RepeatedMax,
                4 => (object)DistinctMax,
                5 => (object)MixedMinMaxRowNo,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "RepeatedMin" => (object)RepeatedMin,
                "DistinctMin" => (object)DistinctMin,
                "RepeatedMax" => (object)RepeatedMax,
                "DistinctMax" => (object)DistinctMax,
                "MixedMinMaxRowNo" => (object)MixedMinMaxRowNo,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Name, int? RepeatedMin, int? DistinctMin, int? RepeatedMax, int? DistinctMax, long MixedMinMaxRowNo)
            {
                this.Name = Name;
                this.RepeatedMin = RepeatedMin;
                this.DistinctMin = DistinctMin;
                this.RepeatedMax = RepeatedMax;
                this.DistinctMax = DistinctMax;
                this.MixedMinMaxRowNo = MixedMinMaxRowNo;
            }

            public int? DistinctMax { get; }
            public int? DistinctMin { get; }
            public long MixedMinMaxRowNo { get; }
            public string Name { get; }
            public int? RepeatedMax { get; }
            public int? RepeatedMin { get; }
        }

        private readonly struct WindowResultRowNumbersOrderKeysKey : System.IEquatable<WindowResultRowNumbersOrderKeysKey>, System.IComparable<WindowResultRowNumbersOrderKeysKey>
        {
            private readonly int? _value0;
            private readonly int? _value1;
            private readonly int? _value2;
            private readonly int? _value3;
            private readonly string _value4;
            public WindowResultRowNumbersOrderKeysKey(int? value0, int? value1, int? value2, int? value3, string value4)
            {
                _value0 = value0;
                _value1 = value1;
                _value2 = value2;
                _value3 = value3;
                _value4 = value4;
            }

            public int CompareTo(WindowResultRowNumbersOrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                var comparison1 = CompareValue1(_value1, other._value1);
                if (comparison1 != 0)
                    return comparison1;
                var comparison2 = CompareValue2(_value2, other._value2);
                if (comparison2 != 0)
                    return comparison2;
                var comparison3 = CompareValue3(_value3, other._value3);
                if (comparison3 != 0)
                    return comparison3;
                var comparison4 = CompareValue4(_value4, other._value4);
                if (comparison4 != 0)
                    return comparison4;
                return 0;
            }

            public bool Equals(WindowResultRowNumbersOrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int?>.Default.Equals(_value0, other._value0) && System.Collections.Generic.EqualityComparer<int?>.Default.Equals(_value1, other._value1) && System.Collections.Generic.EqualityComparer<int?>.Default.Equals(_value2, other._value2) && System.Collections.Generic.EqualityComparer<int?>.Default.Equals(_value3, other._value3) && System.String.Equals(_value4, other._value4, System.StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultRowNumbersOrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0);
                hash.Add(_value1);
                hash.Add(_value2);
                hash.Add(_value3);
                hash.Add(_value4, System.StringComparer.Ordinal);
                return hash.ToHashCode();
            }

            private static int CompareValue0(int? left, int? right)
            {
                if (!left.HasValue)
                    return !right.HasValue ? 0 : 1;
                if (!right.HasValue)
                    return -1;
                var comparison = left.Value.CompareTo(right.Value);
                return -comparison;
            }

            private static int CompareValue1(int? left, int? right)
            {
                if (!left.HasValue)
                    return !right.HasValue ? 0 : 1;
                if (!right.HasValue)
                    return -1;
                var comparison = left.Value.CompareTo(right.Value);
                return -comparison;
            }

            private static int CompareValue2(int? left, int? right)
            {
                if (!left.HasValue)
                    return !right.HasValue ? 0 : -1;
                if (!right.HasValue)
                    return 1;
                var comparison = left.Value.CompareTo(right.Value);
                return comparison;
            }

            private static int CompareValue3(int? left, int? right)
            {
                if (!left.HasValue)
                    return !right.HasValue ? 0 : -1;
                if (!right.HasValue)
                    return 1;
                var comparison = left.Value.CompareTo(right.Value);
                return comparison;
            }

            private static int CompareValue4(string left, string right)
            {
                if (left == null)
                    return right == null ? 0 : -1;
                if (right == null)
                    return 1;
                var comparison = System.String.CompareOrdinal(left, right);
                return comparison;
            }
        }

        private sealed class WindowSourceRow0 : Row
        {
            public WindowSourceRow0(string __value0, int? __value1, int? __value2, int? __value3, int? __value4)
            {
                i_Name = __value0;
                RepeatedMin = __value1;
                DistinctMin = __value2;
                RepeatedMax = __value3;
                DistinctMax = __value4;
            }

            public override int Count => 5;
            public int? DistinctMax { get; private set; }
            public int? DistinctMin { get; private set; }
            public int? RepeatedMax { get; private set; }
            public int? RepeatedMin { get; private set; }
            public string i_Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        i_Name = (string)value;
                        break;
                    case 1:
                        RepeatedMin = (int?)value;
                        break;
                    case 2:
                        DistinctMin = (int?)value;
                        break;
                    case 3:
                        RepeatedMax = (int?)value;
                        break;
                    case 4:
                        DistinctMax = (int?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "i.Name" => true,
                "i_Name" => true,
                "Name" => true,
                "RepeatedMin" => true,
                "DistinctMin" => true,
                "RepeatedMax" => true,
                "DistinctMax" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)i_Name,
                1 => (object)RepeatedMin,
                2 => (object)DistinctMin,
                3 => (object)RepeatedMax,
                4 => (object)DistinctMax,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "i.Name" => (object)i_Name,
                "i_Name" => (object)i_Name,
                "Name" => (object)i_Name,
                "RepeatedMin" => (object)RepeatedMin,
                "DistinctMin" => (object)DistinctMin,
                "RepeatedMax" => (object)RepeatedMax,
                "DistinctMax" => (object)DistinctMax,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class WindowSourceTableAggregateGroup
        {
            public Musoq.Plugins.MinAggregateKernel<int>.State __agg0;
            public Musoq.Plugins.MinDistinctAggregateKernel<int>.State __agg1;
            public Musoq.Plugins.MaxAggregateKernel<int>.State __agg2;
            public Musoq.Plugins.MaxDistinctAggregateKernel<int>.State __agg3;
            public readonly string __key0;
            public WindowSourceTableAggregateGroup(string __key0)
            {
                this.__key0 = __key0;
            }

            public void MergeFrom(WindowSourceTableAggregateGroup source)
            {
                Musoq.Plugins.MinAggregateKernel<int>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.MinDistinctAggregateKernel<int>.Merge(ref this.__agg1, in source.__agg1);
                Musoq.Plugins.MaxAggregateKernel<int>.Merge(ref this.__agg2, in source.__agg2);
                Musoq.Plugins.MaxDistinctAggregateKernel<int>.Merge(ref this.__agg3, in source.__agg3);
            }
        }
    }
}
