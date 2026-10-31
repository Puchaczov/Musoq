// === Parsed Query ===
/*
select Partition, Sum(Integral) as Total, Avg(Integral) as Average,
    Avg(distinct Integral) as UniqueAverage from #schema.first()
group by Partition order by Partition
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Partition as ko3iko.Partition, AggRef(ko3iko.Avg(distinct ko3iko.Integral)) as ko3iko.Avg(distinct ko3iko.Integral), AggRef(ko3iko.Avg(ko3iko.Integral)) as ko3iko.Avg(ko3iko.Integral), AggRef(ko3iko.Sum(ko3iko.Integral)) as ko3iko.Sum(ko3iko.Integral)]
    Aggregate [keys: Partition] [aggs: Avg(distinct Integral), Avg(Integral), Sum(Integral)]
      SchemaScan [#schema.first() as ko3iko]
  Sort [ko3iko.Partition]
    Project [ko3iko.Partition as Partition, ko3iko.Sum(ko3iko.Integral) as Total, ko3iko.Avg(ko3iko.Integral) as Average, ko3iko.Avg(distinct ko3iko.Integral) as UniqueAverage]
      CteRef [ko3ikoScore as ko3ikoScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Partition as ko3iko.Partition, AggRef(ko3iko.Avg(distinct ko3iko.Integral)) as ko3iko.Avg(distinct ko3iko.Integral), AggRef(ko3iko.Avg(ko3iko.Integral)) as ko3iko.Avg(ko3iko.Integral), AggRef(ko3iko.Sum(ko3iko.Integral)) as ko3iko.Sum(ko3iko.Integral)]
    PhysicalSingleKeyAggregate [key: Partition (String)] [aggs: Avg(distinct Integral), Avg(Integral), Sum(Integral)]
      PhysicalSchemaScan [#schema.first() as ko3iko]
  PhysicalSort [ko3iko.Partition]
    PhysicalProject [ko3iko.Partition as Partition, ko3iko.Sum(ko3iko.Integral) as Total, ko3iko.Avg(ko3iko.Integral) as Average, ko3iko.Avg(distinct ko3iko.Integral) as UniqueAverage]
      PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: LibraryExecutionEntity]
      Partition: string <- property Partition
      Integral: int? <- property Integral
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 3]
    Generated [ResultRow0]
      Partition: string <- field Partition
      Total: int? <- field Total
      Average: decimal? <- field Average
      UniqueAverage: decimal? <- field UniqueAverage

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: LibraryExecutionEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [groups: string -> ResultAggregateGroup]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      GetOrAddSingleKeyAggregateGroup [group = groups[ko3iko.Partition] by Partition; typed: ResultAggregateGroup]
      Let [integral: int? = ko3iko.Integral]
      TypedAggregateSet [Set(group.__agg0, integral)]
      Let [integral1: int? = ko3iko.Integral]
      TypedAggregateSet [Set(group.__agg1, integral1)]
      Let [integral2: int? = ko3iko.Integral]
      TypedAggregateSet [Set(group.__agg2, integral2)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(Partition: finalGroup.Partition, Total: ko3iko.Sum(ko3iko.Integral), Average: ko3iko.Avg(ko3iko.Integral), UniqueAverage: ko3iko.Avg(distinct ko3iko.Integral))]
    SortOwnedBuffer [result by Partition ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K17_IntegralDecimalAverage
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
            new Column("Partition", typeof(string), 0),
            new Column("Total", typeof(int?), 1),
            new Column("Average", typeof(decimal?), 2),
            new Column("UniqueAverage", typeof(decimal?), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Partition", typeof(string), 0), new Column("Integral", typeof(int?), 1) });
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
                yield return new ResultRow0(__musoqShapeRow.Partition, __musoqShapeRow.Total, __musoqShapeRow.Average, __musoqShapeRow.UniqueAverage);
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
                var __ko3ikoSchema = provider.GetSchema("#schema");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<string, ResultAggregateGroup>();
                ResultAggregateGroup nullGroup = null;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                string groupKey = ko3iko.Partition;
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

                                int? integral = ko3iko.Integral;
                                Musoq.Plugins.AverageDistinctKernel<int, decimal>.Set(ref group.__agg0, (int?)integral);
                                int? integral1 = ko3iko.Integral;
                                Musoq.Plugins.AverageKernel<int, decimal>.Set(ref group.__agg1, (int?)integral1);
                                int? integral2 = ko3iko.Integral;
                                {
                                    var __agg2Input = (int?)integral2;
                                    if (__agg2Input.HasValue)
                                    {
                                        var __agg2Current = __agg2Input.GetValueOrDefault();
                                        group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                        group.__agg2.HasValue = true;
                                    }
                                }
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                string groupKey = ko3iko.Partition;
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

                                int? integral = ko3iko.Integral;
                                Musoq.Plugins.AverageDistinctKernel<int, decimal>.Set(ref group.__agg0, (int?)integral);
                                int? integral1 = ko3iko.Integral;
                                Musoq.Plugins.AverageKernel<int, decimal>.Set(ref group.__agg1, (int?)integral1);
                                int? integral2 = ko3iko.Integral;
                                {
                                    var __agg2Input = (int?)integral2;
                                    if (__agg2Input.HasValue)
                                    {
                                        var __agg2Current = __agg2Input.GetValueOrDefault();
                                        group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                        group.__agg2.HasValue = true;
                                    }
                                }
                            }

                            continue;
                        }
                    }

                    for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunk.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                    {
                        if ((ko3ikoIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var ko3iko = ko3ikoChunk[ko3ikoIndex];
                        string groupKey = ko3iko.Partition;
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

                        int? integral = ko3iko.Integral;
                        Musoq.Plugins.AverageDistinctKernel<int, decimal>.Set(ref group.__agg0, (int?)integral);
                        int? integral1 = ko3iko.Integral;
                        Musoq.Plugins.AverageKernel<int, decimal>.Set(ref group.__agg1, (int?)integral1);
                        int? integral2 = ko3iko.Integral;
                        {
                            var __agg2Input = (int?)integral2;
                            if (__agg2Input.HasValue)
                            {
                                var __agg2Current = __agg2Input.GetValueOrDefault();
                                group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                group.__agg2.HasValue = true;
                            }
                        }
                    }
                }

                result.EnsureCapacity(groupsToFinalize.Count);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    result.Add(new ResultShape0(finalGroup.__key0, finalGroup.__agg2.HasValue ? (int?)finalGroup.__agg2.Value : null, Musoq.Plugins.AverageKernel<int, decimal>.Get(in finalGroup.__agg1), Musoq.Plugins.AverageDistinctKernel<int, decimal>.Get(in finalGroup.__agg0)) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.Partition, right.Partition);
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

        private sealed class ResultAggregateGroup
        {
            public Musoq.Plugins.AverageDistinctKernel<int, decimal>.State __agg0;
            public Musoq.Plugins.AverageKernel<int, decimal>.State __agg1;
            public Musoq.Plugins.SumAggregateKernel<int>.State __agg2;
            public readonly string __key0;
            public ResultAggregateGroup(string __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.AverageDistinctKernel<int, decimal>.Reset(ref this.__agg0);
                Musoq.Plugins.AverageKernel<int, decimal>.Reset(ref this.__agg1);
                Musoq.Plugins.SumAggregateKernel<int>.Reset(ref this.__agg2);
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.AverageDistinctKernel<int, decimal>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.AverageKernel<int, decimal>.Merge(ref this.__agg1, in source.__agg1);
                Musoq.Plugins.SumAggregateKernel<int>.Merge(ref this.__agg2, in source.__agg2);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, int? __value1, decimal? __value2, decimal? __value3)
            {
                Partition = __value0;
                Total = __value1;
                Average = __value2;
                UniqueAverage = __value3;
            }

            public decimal? Average { get; private set; }
            public override int Count => 4;
            public string Partition { get; private set; }
            public int? Total { get; private set; }
            public decimal? UniqueAverage { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Partition = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Average = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.UniqueAverage = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Partition),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Total),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Average),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.UniqueAverage),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Partition" => true,
                "Total" => true,
                "Average" => true,
                "UniqueAverage" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Partition, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Average, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.UniqueAverage, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Partition, int? Total, decimal? Average, decimal? UniqueAverage)
            {
                this.Partition = Partition;
                this.Total = Total;
                this.Average = Average;
                this.UniqueAverage = UniqueAverage;
            }

            public decimal? Average { get; }
            public string Partition { get; }
            public int? Total { get; }
            public decimal? UniqueAverage { get; }
            public int __musoqSequence { get; init; }
        }
    }
}
