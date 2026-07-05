/*
raw query string

select i.Name as Name, Sum(n.Value) as RepeatedSum, Sum(distinct n.Value) as DistinctSum from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Sum(distinct n.Value) desc, Sum(n.Value) desc, i.Name
*/

/*
logical plan representation string

MultiStatement
  Sort [AggRef(inm.Sum(distinct n.Value)) DESC, AggRef(inm.Sum(n.Value)) DESC, i.Name]
    Project [i.Name as Name, AggRef(inm.Sum(n.Value)) as RepeatedSum, AggRef(inm.Sum(distinct n.Value)) as DistinctSum]
      Aggregate [keys: i.Name] [aggs: RepeatedSum, DistinctSum]
        Apply [Cross]
          Apply [Cross]
            SchemaScan [#apply.items() as i]
            PropertySource [i.Numbers as n] [apply: Cross] [type: Int32[]]
          PropertySource [i.Numbers as m] [apply: Cross] [type: Int32[]]
*/

/*
physical plan representation string

PhysicalMultiStatement
  PhysicalSort [AggRef(inm.Sum(distinct n.Value)) DESC, AggRef(inm.Sum(n.Value)) DESC, i.Name]
    PhysicalProject [i.Name as Name, AggRef(inm.Sum(n.Value)) as RepeatedSum, AggRef(inm.Sum(distinct n.Value)) as DistinctSum]
      PhysicalSingleKeyAggregate [key: i.Name (String)] [aggs: RepeatedSum, DistinctSum]
        PhysicalNestedLoopApply [Cross]
          PhysicalNestedLoopApply [Cross]
            PhysicalSchemaScan [#apply.items() as i]
            PhysicalPropertySource [i.Numbers as n] [apply: Cross] [type: Int32[]]
          PhysicalPropertySource [i.Numbers as m] [apply: Cross] [type: Int32[]]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    SourceEntity [i: GeneratedApplySampleEntity]
      Name: string <- property Name
      Numbers: int[] <- property Numbers
    SourceEntity [n: int]
      Value: int <- direct scalar value
    SourceEntity [m: int]
      Value: int <- direct scalar value
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 2]
    Generated [ResultRow0]
      Name: string <- field Name
      RepeatedSum: int? <- field RepeatedSum
      DistinctSum: int? <- field DistinctSum

  Body
    SourceScan [i: GeneratedApplySampleEntity] -> iRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateSingleKeyAggregateContext [groups: string -> ResultAggregateGroup]
    ChunkedForEach [i in iRows]
      EnumerableSource [i.Numbers -> nRows]
      ChunkedForEach [n in nRows]
        EnumerableSource [i.Numbers -> mRows]
        ChunkedForEach [m in mRows]
          Let [value: int = n.Value]
          GetOrAddSingleKeyAggregateGroup [group = groups[i.Name] by i.Name; typed: ResultAggregateGroup]
          TypedAggregateSet [Set(group.__agg0, value)]
          TypedAggregateSet [Set(group.__agg1, value)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(Name: finalGroup.i.Name, RepeatedSum: inm.Sum(n.Value), DistinctSum: inm.Sum(distinct n.Value))]
    SortShapeRows [result -> resultSorted by DistinctSum DESC, RepeatedSum DESC, Name ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q69_ChainedApplyMixedDistinctAggregateSort
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("RepeatedSum", typeof(int?), 1),
            new Column("DistinctSum", typeof(int?), 2)
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.RepeatedSum, __musoqShapeRow.DistinctSum);
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
                var __iSchema = provider.GetSchema("#apply");
                var iRowsSource = __iSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>("items", new SourceExecutionContext("i:1", sourceExecutionPlans["i:1"], token, __schemaColumns_compiled_i_0, sourceRuntimeSettingsBySourceContextId["i:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var iRows = iRowsSource.Chunks;
                var result = new List<ResultShape0>();
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<string, ResultAggregateGroup>();
                ResultAggregateGroup nullGroup = null;
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
                                TraverseNRows(token, i, groupsToFinalize, groups, ref nullGroup);
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
                                TraverseNRows(token, i, groupsToFinalize, groups, ref nullGroup);
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
                        TraverseNRows(token, i, groupsToFinalize, groups, ref nullGroup);
                    }
                }

                result.EnsureCapacity(groupsToFinalize.Count);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    result.Add(new ResultShape0(finalGroup.__key0, finalGroup.__agg0.HasValue ? (int?)finalGroup.__agg0.Value : null, Musoq.Plugins.SumDistinctAggregateKernel<int>.Get(in finalGroup.__agg1)));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = Nullable.Compare(left.DistinctSum, right.DistinctSum);
                    comparison = -comparison;
                    if (comparison != 0)
                        return comparison;
                    comparison = Nullable.Compare(left.RepeatedSum, right.RepeatedSum);
                    comparison = -comparison;
                    if (comparison != 0)
                        return comparison;
                    comparison = StringComparer.Ordinal.Compare(left.Name, right.Name);
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
        private static void TraverseMRows(CancellationToken token, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i, int n, List<ResultAggregateGroup> groupsToFinalize, Dictionary<string, ResultAggregateGroup> groups, ref ResultAggregateGroup nullGroup)
        {
            token.ThrowIfCancellationRequested();
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
        private static void TraverseNRows(CancellationToken token, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i, List<ResultAggregateGroup> groupsToFinalize, Dictionary<string, ResultAggregateGroup> groups, ref ResultAggregateGroup nullGroup)
        {
            token.ThrowIfCancellationRequested();
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
                            TraverseMRows(token, i, n, groupsToFinalize, groups, ref nullGroup);
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
                            TraverseMRows(token, i, n, groupsToFinalize, groups, ref nullGroup);
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
                    TraverseMRows(token, i, n, groupsToFinalize, groups, ref nullGroup);
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void UpdateGroupsAggregates(List<ResultAggregateGroup> groupsToFinalize, Dictionary<string, ResultAggregateGroup> groups, ref ResultAggregateGroup nullGroup, int n, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i)
        {
            int value = n;
            string groupKey = i.Name;
            ResultAggregateGroup group = null;
            if (groupKey != null)
            {
                ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var groupExists);
                if (!groupExists)
                {
                    groupRef = new ResultAggregateGroup(groupKey);
                    groupsToFinalize.Add(groupRef);
                }

                group = groupRef;
            }
            else
            {
                if (nullGroup == null)
                {
                    nullGroup = new ResultAggregateGroup(null);
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

            Musoq.Plugins.SumDistinctAggregateKernel<int>.Set(ref group.__agg1, (int?)value);
        }

        private sealed class ResultAggregateGroup
        {
            public Musoq.Plugins.SumAggregateKernel<int>.State __agg0;
            public Musoq.Plugins.SumDistinctAggregateKernel<int>.State __agg1;
            public readonly string __key0;
            public ResultAggregateGroup(string __key0)
            {
                this.__key0 = __key0;
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.SumAggregateKernel<int>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.SumDistinctAggregateKernel<int>.Merge(ref this.__agg1, in source.__agg1);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, int? __value1, int? __value2)
            {
                Name = __value0;
                RepeatedSum = __value1;
                DistinctSum = __value2;
            }

            public override int Count => 3;
            public int? DistinctSum { get; private set; }
            public string Name { get; private set; }
            public int? RepeatedSum { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        RepeatedSum = (int?)value;
                        break;
                    case 2:
                        DistinctSum = (int?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "RepeatedSum" => true,
                "DistinctSum" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)RepeatedSum,
                2 => (object)DistinctSum,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "RepeatedSum" => (object)RepeatedSum,
                "DistinctSum" => (object)DistinctSum,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Name, int? RepeatedSum, int? DistinctSum)
            {
                this.Name = Name;
                this.RepeatedSum = RepeatedSum;
                this.DistinctSum = DistinctSum;
            }

            public int? DistinctSum { get; }
            public string Name { get; }
            public int? RepeatedSum { get; }
        }
    }
}
