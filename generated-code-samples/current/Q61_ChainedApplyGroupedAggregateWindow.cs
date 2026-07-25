// === Parsed Query ===
/*
select i.Name as Name, Sum(n.Value) as ValueSum, RowNumber() over (order by Sum(n.Value) desc, i.Name) as GroupRowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by GroupRowNo
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [WindowRef(0)]
    Project [i.Name as Name, AggRef(inm.Sum(n.Value)) as ValueSum, WindowRef(0) as GroupRowNo]
      Window [RowNumber(idx:0; order: AggRef(inm.Sum(n.Value)) DESC, i.Name)]
        Aggregate [keys: i.Name] [aggs: ValueSum]
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
    PhysicalProject [i.Name as Name, AggRef(inm.Sum(n.Value)) as ValueSum, WindowRef(0) as GroupRowNo]
      PhysicalWindow [RowNumber(idx:0; order: AggRef(inm.Sum(n.Value)) DESC, i.Name)]
        PhysicalMaterialize
          PhysicalSingleKeyAggregate [key: i.Name (String)] [aggs: ValueSum]
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
    AggregateGroup [WindowSourceTableAggregateGroup; keys: 1; typed aggs: 1]
    Generated [WindowSourceRow0]
      i.Name: string <- field i_Name
      ValueSum: int? <- field ValueSum
    TableRow [windowSource]
      i.Name: string <- field i_Name
      ValueSum: int? <- field ValueSum
    Generated [ResultRow0]
      Name: string <- field Name
      ValueSum: int? <- field ValueSum
      GroupRowNo: long <- field GroupRowNo

  Body
    SourceScan [i: GeneratedApplySampleEntity] -> windowSourceTable_iRows
    CreateRowBuffer [windowSourceTable: List<WindowSourceRow0>]
    CreateSingleKeyAggregateContext [groups: string -> WindowSourceTableAggregateGroup]
    ChunkedForEach [i in windowSourceTable_iRows]
      EnumerableSource [i.Numbers -> windowSourceTable_nRows]
      ChunkedForEach [n in windowSourceTable_nRows]
        EnumerableSource [i.Numbers -> windowSourceTable_mRows]
        ChunkedForEach [m in windowSourceTable_mRows]
          Let [value: int = n.Value]
          GetOrAddSingleKeyAggregateGroup [group = groups[i.Name] by i.Name; typed: WindowSourceTableAggregateGroup]
          TypedAggregateSet [Set(group.__agg0, value)]
    EnsureRowBufferCapacity [windowSourceTable <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendRowBuffer [windowSourceTable <- WindowSourceRow0(i.Name: finalGroup.i.Name, ValueSum: inm.Sum(n.Value))]
    Materialize [windowSourceTable -> resultWindowRows]
    ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows order by windowSource.ValueSum DESC, windowSource.i.Name ASC]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, windowSource in resultWindowRows]
      AppendShape [result <- ResultShape0(Name: windowSource.i.Name, ValueSum: windowSource.ValueSum, GroupRowNo: resultRowNumbers[windowIndex])]
    SortShapeRows [result -> resultSorted by GroupRowNo ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q61_ChainedApplyGroupedAggregateWindow
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("ValueSum", typeof(int?), 1),
            new Column("GroupRowNo", typeof(long), 2)
        };
        private static readonly Column[] __columns_compiled_windowSourceTable_1 = new Column[]
        {
            new Column("i.Name", typeof(string), 0),
            new Column("ValueSum", typeof(int?), 1)
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
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.ValueSum, __musoqShapeRow.GroupRowNo);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.GroupBy);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                var __windowSourceTable_iSchema = provider.GetSchema("#apply");
                var windowSourceTable_iRowsSource = __windowSourceTable_iSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>("items", new SourceExecutionContext("i:1", sourceExecutionPlans["i:1"], token, __schemaColumns_compiled_i_0, sourceRuntimeSettingsBySourceContextId["i:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var windowSourceTable_iRows = windowSourceTable_iRowsSource.Chunks;
                var windowSourceTable = new List<WindowSourceRow0>();
                var groupsToFinalize = new List<WindowSourceTableAggregateGroup>();
                var groups = new Dictionary<string, WindowSourceTableAggregateGroup>();
                WindowSourceTableAggregateGroup nullGroup = null;
                PopulateWindowSourceTableSingleKeyGroups(windowSourceTable_iRows, groupsToFinalize, groups, ref nullGroup, token);
                FinalizeWindowSourceTableSingleKeyGroups(windowSourceTable, groupsToFinalize, token);
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
                    result.Add(new ResultShape0(windowSource.i_Name, windowSource.ValueSum, (long)resultRowNumbers[windowIndex]));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.GroupRowNo.CompareTo(right.GroupRowNo);
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
                OnPhaseChanged("compiled", QueryPhase.End);
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
                resultRowNumbersOrderKeys[windowIndex] = new WindowResultRowNumbersOrderKeysKey(windowSource.ValueSum, windowSource.i_Name);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void FinalizeWindowSourceTableSingleKeyGroups(List<WindowSourceRow0> windowSourceTable, List<WindowSourceTableAggregateGroup> groupsToFinalize, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            windowSourceTable.EnsureCapacity(groupsToFinalize.Count);
            foreach (var finalGroup in groupsToFinalize)
            {
                token.ThrowIfCancellationRequested();
                windowSourceTable.Add(new WindowSourceRow0(finalGroup.__key0, finalGroup.__agg0.HasValue ? (int?)finalGroup.__agg0.Value : null));
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void PopulateWindowSourceTableSingleKeyGroups(IEnumerable<IReadOnlyList<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>> rows, List<WindowSourceTableAggregateGroup> groupsToFinalize, Dictionary<string, WindowSourceTableAggregateGroup> groups, ref WindowSourceTableAggregateGroup nullGroup, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            foreach (var iChunk in rows)
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
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void TraverseWindowSourceTableMRows(CancellationToken token, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i, int n, List<WindowSourceTableAggregateGroup> groupsToFinalize, Dictionary<string, WindowSourceTableAggregateGroup> groups, ref WindowSourceTableAggregateGroup nullGroup)
        {
            token.ThrowIfCancellationRequested();
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
                            UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, n, i);
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
                            UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, n, i);
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
                    UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, n, i);
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
                            TraverseWindowSourceTableMRows(token, i, n, groupsToFinalize, groups, ref nullGroup);
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
                            TraverseWindowSourceTableMRows(token, i, n, groupsToFinalize, groups, ref nullGroup);
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
                    TraverseWindowSourceTableMRows(token, i, n, groupsToFinalize, groups, ref nullGroup);
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void UpdateGroupsAggregates(List<WindowSourceTableAggregateGroup> groupsToFinalize, Dictionary<string, WindowSourceTableAggregateGroup> groups, ref WindowSourceTableAggregateGroup nullGroup, int n, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i)
        {
            int value = n;
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
                var __agg0Input = (int?)value;
                if (__agg0Input.HasValue)
                {
                    var __agg0Current = __agg0Input.GetValueOrDefault();
                    group.__agg0.Value = group.__agg0.HasValue ? checked(group.__agg0.Value + __agg0Current) : __agg0Current;
                    group.__agg0.HasValue = true;
                }
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, int? __value1, long __value2)
            {
                Name = __value0;
                ValueSum = __value1;
                GroupRowNo = __value2;
            }

            public override int Count => 3;
            public long GroupRowNo { get; private set; }
            public string Name { get; private set; }
            public int? ValueSum { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        ValueSum = (int?)value;
                        break;
                    case 2:
                        GroupRowNo = (long)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "ValueSum" => true,
                "GroupRowNo" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)ValueSum,
                2 => (object)GroupRowNo,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "ValueSum" => (object)ValueSum,
                "GroupRowNo" => (object)GroupRowNo,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Name, int? ValueSum, long GroupRowNo)
            {
                this.Name = Name;
                this.ValueSum = ValueSum;
                this.GroupRowNo = GroupRowNo;
            }

            public long GroupRowNo { get; }
            public string Name { get; }
            public int? ValueSum { get; }
        }

        private readonly struct WindowResultRowNumbersOrderKeysKey : System.IEquatable<WindowResultRowNumbersOrderKeysKey>, System.IComparable<WindowResultRowNumbersOrderKeysKey>
        {
            private readonly int? _value0;
            private readonly string _value1;
            public WindowResultRowNumbersOrderKeysKey(int? value0, string value1)
            {
                _value0 = value0;
                _value1 = value1;
            }

            public int CompareTo(WindowResultRowNumbersOrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                var comparison1 = CompareValue1(_value1, other._value1);
                if (comparison1 != 0)
                    return comparison1;
                return 0;
            }

            public bool Equals(WindowResultRowNumbersOrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int?>.Default.Equals(_value0, other._value0) && System.String.Equals(_value1, other._value1, System.StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultRowNumbersOrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0);
                hash.Add(_value1, System.StringComparer.Ordinal);
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

            private static int CompareValue1(string left, string right)
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
            public WindowSourceRow0(string __value0, int? __value1)
            {
                i_Name = __value0;
                ValueSum = __value1;
            }

            public override int Count => 2;
            public int? ValueSum { get; private set; }
            public string i_Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        i_Name = (string)value;
                        break;
                    case 1:
                        ValueSum = (int?)value;
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
                "ValueSum" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)i_Name,
                1 => (object)ValueSum,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "i.Name" => (object)i_Name,
                "i_Name" => (object)i_Name,
                "Name" => (object)i_Name,
                "ValueSum" => (object)ValueSum,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class WindowSourceTableAggregateGroup
        {
            public Musoq.Plugins.SumAggregateKernel<int>.State __agg0;
            public readonly string __key0;
            public WindowSourceTableAggregateGroup(string __key0)
            {
                this.__key0 = __key0;
            }

            public void MergeFrom(WindowSourceTableAggregateGroup source)
            {
                Musoq.Plugins.SumAggregateKernel<int>.Merge(ref this.__agg0, in source.__agg0);
            }
        }
    }
}
