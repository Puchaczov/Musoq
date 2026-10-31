// === Parsed Query ===
/*
select i.Name as Name, Avg(n.Value) as RepeatedAvg, Avg(distinct n.Value) as DistinctAvg from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name order by Avg(distinct n.Value) desc, Avg(n.Value) desc, i.Name
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [AggRef(inm.Avg(distinct n.Value)) DESC, AggRef(inm.Avg(n.Value)) DESC, i.Name]
    Project [i.Name as Name, AggRef(inm.Avg(n.Value)) as RepeatedAvg, AggRef(inm.Avg(distinct n.Value)) as DistinctAvg]
      Aggregate [keys: i.Name] [aggs: RepeatedAvg, DistinctAvg]
        Apply [Cross]
          Apply [Cross]
            SchemaScan [#apply.items() as i]
            PropertySource [i.Numbers as n] [apply: Cross] [type: Int32[]]
          PropertySource [i.Numbers as m] [apply: Cross] [type: Int32[]]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [AggRef(inm.Avg(distinct n.Value)) DESC, AggRef(inm.Avg(n.Value)) DESC, i.Name]
    PhysicalProject [i.Name as Name, AggRef(inm.Avg(n.Value)) as RepeatedAvg, AggRef(inm.Avg(distinct n.Value)) as DistinctAvg]
      PhysicalSingleKeyAggregate [key: i.Name (String)] [aggs: RepeatedAvg, DistinctAvg]
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
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 2]
    Generated [ResultRow0]
      Name: string <- field Name
      RepeatedAvg: decimal? <- field RepeatedAvg
      DistinctAvg: decimal? <- field DistinctAvg

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [i: GeneratedApplySampleEntity] -> iRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [groups: string -> ResultAggregateGroup]
    PhaseBoundary [Select]
    ChunkedForEach [i in iRows]
      EnumerableSource [i.Numbers -> nRows]
      ForEach [n in nRows]
        Let [nValue: int = n.Value]
        EnumerableSource [i.Numbers -> mRows]
        ForEach [m in mRows]
          GetOrAddSingleKeyAggregateGroup [group = groups[i.Name] by i.Name; typed: ResultAggregateGroup]
          TypedAggregateSet [Set(group.__agg0, nValue)]
          TypedAggregateSet [Set(group.__agg1, nValue)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(Name: finalGroup.i.Name, RepeatedAvg: inm.Avg(n.Value), DistinctAvg: inm.Avg(distinct n.Value))]
    SortOwnedBuffer [result by DistinctAvg DESC, RepeatedAvg DESC, Name ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q71_ChainedApplyMixedDistinctAvgAggregateSort
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
            new Column("Name", typeof(string), 0),
            new Column("RepeatedAvg", typeof(decimal?), 1),
            new Column("DistinctAvg", typeof(decimal?), 2)
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.RepeatedAvg, __musoqShapeRow.DistinctAvg);
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
                var __iSchema = provider.GetSchema("#apply");
                var iRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>.Open(__iSchema, "items", new SourceExecutionContext("i:1", sourceExecutionPlans["i:1"], token, __schemaColumns_compiled_i_0, sourceRuntimeSettingsBySourceContextId["i:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var iRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>(iRowsSource.Chunks, __musoqProgressContext, "i:1") : iRowsSource.Chunks;
                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<string, ResultAggregateGroup>();
                ResultAggregateGroup nullGroup = null;
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
                    result.Add(new ResultShape0(finalGroup.__key0, Musoq.Plugins.AverageKernel<int, decimal>.Get(in finalGroup.__agg0), Musoq.Plugins.AverageDistinctKernel<int, decimal>.Get(in finalGroup.__agg1)) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = Nullable.Compare(left.DistinctAvg, right.DistinctAvg);
                    comparison = -comparison;
                    if (comparison != 0)
                        return comparison;
                    comparison = Nullable.Compare(left.RepeatedAvg, right.RepeatedAvg);
                    comparison = -comparison;
                    if (comparison != 0)
                        return comparison;
                    comparison = StringComparer.Ordinal.Compare(left.Name, right.Name);
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
        private static void TraverseMRows(CancellationToken token, int n, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i, List<ResultAggregateGroup> groupsToFinalize, Dictionary<string, ResultAggregateGroup> groups, ref ResultAggregateGroup nullGroup)
        {
            token.ThrowIfCancellationRequested();
            int nValue = n;
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
                    UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, nValue, i);
                }
            else if (mIndexRows is List<int> mIndexConcreteList)
                for (int mIndex = 0, mIndexCount = mIndexConcreteList.Count; mIndex < mIndexCount; ++mIndex)
                {
                    if ((mIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    int m = mIndexConcreteList[mIndex];
                    UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, nValue, i);
                }
            else
            {
                int mIndex = 0;
                foreach (var m in mIndexRows)
                {
                    token.ThrowIfCancellationRequested();
                    UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, nValue, i);
                    ++mIndex;
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void TraverseNRows(CancellationToken token, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i, List<ResultAggregateGroup> groupsToFinalize, Dictionary<string, ResultAggregateGroup> groups, ref ResultAggregateGroup nullGroup)
        {
            token.ThrowIfCancellationRequested();
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
                    TraverseMRows(token, n, i, groupsToFinalize, groups, ref nullGroup);
                }
            else if (nIndexRows is List<int> nIndexConcreteList)
                for (int nIndex = 0, nIndexCount = nIndexConcreteList.Count; nIndex < nIndexCount; ++nIndex)
                {
                    if ((nIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    int n = nIndexConcreteList[nIndex];
                    TraverseMRows(token, n, i, groupsToFinalize, groups, ref nullGroup);
                }
            else
            {
                int nIndex = 0;
                foreach (var n in nIndexRows)
                {
                    token.ThrowIfCancellationRequested();
                    TraverseMRows(token, n, i, groupsToFinalize, groups, ref nullGroup);
                    ++nIndex;
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void UpdateGroupsAggregates(List<ResultAggregateGroup> groupsToFinalize, Dictionary<string, ResultAggregateGroup> groups, ref ResultAggregateGroup nullGroup, int nValue, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i)
        {
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

            Musoq.Plugins.AverageKernel<int, decimal>.Set(ref group.__agg0, (int?)nValue);
            Musoq.Plugins.AverageDistinctKernel<int, decimal>.Set(ref group.__agg1, (int?)nValue);
        }

        private sealed class ResultAggregateGroup
        {
            public Musoq.Plugins.AverageKernel<int, decimal>.State __agg0;
            public Musoq.Plugins.AverageDistinctKernel<int, decimal>.State __agg1;
            public readonly string __key0;
            public ResultAggregateGroup(string __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.AverageKernel<int, decimal>.Reset(ref this.__agg0);
                Musoq.Plugins.AverageDistinctKernel<int, decimal>.Reset(ref this.__agg1);
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.AverageKernel<int, decimal>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.AverageDistinctKernel<int, decimal>.Merge(ref this.__agg1, in source.__agg1);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, decimal? __value1, decimal? __value2)
            {
                Name = __value0;
                RepeatedAvg = __value1;
                DistinctAvg = __value2;
            }

            public override int Count => 3;
            public decimal? DistinctAvg { get; private set; }
            public string Name { get; private set; }
            public decimal? RepeatedAvg { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.RepeatedAvg = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.DistinctAvg = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.RepeatedAvg),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.DistinctAvg),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "RepeatedAvg" => true,
                "DistinctAvg" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.RepeatedAvg, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.DistinctAvg, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Name, decimal? RepeatedAvg, decimal? DistinctAvg)
            {
                this.Name = Name;
                this.RepeatedAvg = RepeatedAvg;
                this.DistinctAvg = DistinctAvg;
            }

            public decimal? DistinctAvg { get; }
            public string Name { get; }
            public decimal? RepeatedAvg { get; }
            public int __musoqSequence { get; init; }
        }
    }
}
