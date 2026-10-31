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
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [i: GeneratedApplySampleEntity] -> windowSourceTable_iRows
    CreateRowBuffer [windowSourceTable: List<WindowSourceRow0>]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [groups: string -> WindowSourceTableAggregateGroup]
    PhaseBoundary [Select]
    ChunkedForEach [i in windowSourceTable_iRows]
      EnumerableSource [i.Numbers -> windowSourceTable_nRows]
      ForEach [n in windowSourceTable_nRows]
        Let [nValue: int = n.Value]
        EnumerableSource [i.Numbers -> windowSourceTable_mRows]
        ForEach [m in windowSourceTable_mRows]
          GetOrAddSingleKeyAggregateGroup [group = groups[i.Name] by i.Name; typed: WindowSourceTableAggregateGroup]
          TypedAggregateSet [Set(group.__agg0, nValue)]
    EnsureRowBufferCapacity [windowSourceTable <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendRowBuffer [windowSourceTable <- WindowSourceRow0(i.Name: finalGroup.i.Name, ValueSum: inm.Sum(n.Value))]
    Materialize [windowSourceTable -> resultWindowRows]
    ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows order by windowSource.ValueSum DESC, windowSource.i.Name ASC]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, windowSource in resultWindowRows]
      AppendShape [result <- ResultShape0(Name: windowSource.i.Name, ValueSum: windowSource.ValueSum, GroupRowNo: resultRowNumbers[windowIndex])]
    SortOwnedBuffer [result by GroupRowNo ASC, stable sequence __musoqSequence]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
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
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __windowSourceTable_iSchema = provider.GetSchema("#apply");
                var windowSourceTable_iRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>.Open(__windowSourceTable_iSchema, "items", new SourceExecutionContext("i:1", sourceExecutionPlans["i:1"], token, __schemaColumns_compiled_i_0, sourceRuntimeSettingsBySourceContextId["i:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
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
                    windowSourceTable.Add(new WindowSourceRow0(finalGroup.__key0, finalGroup.__agg0.HasValue ? (int?)finalGroup.__agg0.Value : null));
                }

                var resultWindowRows = EvaluationHelper.MaterializeGeneratedRows<WindowSourceRow0>(windowSourceTable);
                var resultRowNumbersOrderKeys = new WindowResultRowNumbersOrderKeysKey[resultWindowRows.Count];
                ExtractResultRowNumbersWindowKeys(resultWindowRows, resultRowNumbersOrderKeys);
                var resultRowNumbersPartitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
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
                        var resultRowNumbersContext = new Musoq.Plugins.WindowRankingContext(resultRowNumbersPartitionIndex, 0, 0, 0L, resultRowNumbersPartitionCount, token);
                        resultRowNumbers[resultRowNumbersCurrentIndex] = Musoq.Plugins.RowNumberKernel.Get(in resultRowNumbersContext);
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
                    result.Add(new ResultShape0(windowSource.i_Name, windowSource.ValueSum, (long)resultRowNumbers[windowIndex]) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.GroupRowNo.CompareTo(right.GroupRowNo);
                    if (comparison != 0)
                        return comparison;
                    return left.__musoqSequence.CompareTo(right.__musoqSequence);
                }));
                return result;
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
                resultRowNumbersOrderKeys[windowIndex] = new WindowResultRowNumbersOrderKeysKey(windowSource.ValueSum, windowSource.i_Name);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void TraverseWindowSourceTableMRows(CancellationToken token, int n, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i, List<WindowSourceTableAggregateGroup> groupsToFinalize, Dictionary<string, WindowSourceTableAggregateGroup> groups, ref WindowSourceTableAggregateGroup nullGroup)
        {
            token.ThrowIfCancellationRequested();
            int nValue = n;
            var windowSourceTable_mRows = (IEnumerable<int>)i.Numbers ?? Array.Empty<int>();
            var mIndexRows = windowSourceTable_mRows;
            if (mIndexRows is int[] mIndexArray)
                for (int mIndex = 0, mIndexCount = mIndexArray.Length; mIndex < mIndexCount; ++mIndex)
                {
                    if ((mIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    int m = mIndexArray[mIndex];
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
                            group.__agg0.Value = group.__agg0.HasValue ? checked(group.__agg0.Value + __agg0Current) : __agg0Current;
                            group.__agg0.HasValue = true;
                        }
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
                            group.__agg0.Value = group.__agg0.HasValue ? checked(group.__agg0.Value + __agg0Current) : __agg0Current;
                            group.__agg0.HasValue = true;
                        }
                    }
                }
            else
            {
                int mIndex = 0;
                foreach (var m in mIndexRows)
                {
                    token.ThrowIfCancellationRequested();
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
                            group.__agg0.Value = group.__agg0.HasValue ? checked(group.__agg0.Value + __agg0Current) : __agg0Current;
                            group.__agg0.HasValue = true;
                        }
                    }

                    ++mIndex;
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void TraverseWindowSourceTableNRows(CancellationToken token, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i, List<WindowSourceTableAggregateGroup> groupsToFinalize, Dictionary<string, WindowSourceTableAggregateGroup> groups, ref WindowSourceTableAggregateGroup nullGroup)
        {
            token.ThrowIfCancellationRequested();
            var windowSourceTable_nRows = (IEnumerable<int>)i.Numbers ?? Array.Empty<int>();
            var nIndexRows = windowSourceTable_nRows;
            if (nIndexRows is int[] nIndexArray)
                for (int nIndex = 0, nIndexCount = nIndexArray.Length; nIndex < nIndexCount; ++nIndex)
                {
                    if ((nIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    int n = nIndexArray[nIndex];
                    TraverseWindowSourceTableMRows(token, n, i, groupsToFinalize, groups, ref nullGroup);
                }
            else if (nIndexRows is List<int> nIndexConcreteList)
                for (int nIndex = 0, nIndexCount = nIndexConcreteList.Count; nIndex < nIndexCount; ++nIndex)
                {
                    if ((nIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    int n = nIndexConcreteList[nIndex];
                    TraverseWindowSourceTableMRows(token, n, i, groupsToFinalize, groups, ref nullGroup);
                }
            else
            {
                int nIndex = 0;
                foreach (var n in nIndexRows)
                {
                    token.ThrowIfCancellationRequested();
                    TraverseWindowSourceTableMRows(token, n, i, groupsToFinalize, groups, ref nullGroup);
                    ++nIndex;
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

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.ValueSum = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.GroupRowNo = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.ValueSum),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.GroupRowNo),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "ValueSum" => true,
                "GroupRowNo" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.ValueSum, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.GroupRowNo, ref visitor);
            }
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
            public int __musoqSequence { get; init; }
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

            public ValueTuple<int?, string> RawValues => (_value0, _value1);

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

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.i_Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.ValueSum = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.i_Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.ValueSum),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "i.Name" => true,
                "i_Name" => true,
                "Name" => true,
                "ValueSum" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.i_Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.ValueSum, ref visitor);
            }
        }

        private sealed class WindowSourceTableAggregateGroup
        {
            public Musoq.Plugins.SumAggregateKernel<int>.State __agg0;
            public readonly string __key0;
            public WindowSourceTableAggregateGroup(string __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.SumAggregateKernel<int>.Reset(ref this.__agg0);
            }

            public void MergeFrom(WindowSourceTableAggregateGroup source)
            {
                Musoq.Plugins.SumAggregateKernel<int>.Merge(ref this.__agg0, in source.__agg0);
            }
        }
    }
}
