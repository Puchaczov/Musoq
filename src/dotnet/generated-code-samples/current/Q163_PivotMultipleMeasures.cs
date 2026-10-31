// === Parsed Query ===
/*
pivot #A.entities()
                  on Month in ('Jan' as Jan, 'Feb' as Feb)
                  using Sum(Money) as Sales, Count(*) as Orders
                  group by City
                  order by City
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.City as ko3iko.City, AggRef(ko3iko.Count(*) filter (where Month = 'Feb')) as ko3iko.Count(*) filter (where Month = 'Feb'), AggRef(ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb')) as ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb'), AggRef(ko3iko.Count(*) filter (where Month = 'Jan')) as ko3iko.Count(*) filter (where Month = 'Jan'), AggRef(ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan')) as ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan')]
    Aggregate [keys: City] [aggs: Count(*) filter (where Month = 'Feb'), Sum(Money) filter (where Month = 'Feb'), Count(*) filter (where Month = 'Jan'), Sum(Money) filter (where Month = 'Jan')]
      SchemaScan [#A.entities() as ko3iko]
  Sort [ko3iko.City]
    Project [ko3iko.City as City, ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan') as Jan_Sales, ko3iko.Count(*) filter (where Month = 'Jan') as Jan_Orders, ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb') as Feb_Sales, ko3iko.Count(*) filter (where Month = 'Feb') as Feb_Orders]
      CteRef [ko3ikoScore as ko3ikoScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.City as ko3iko.City, AggRef(ko3iko.Count(*) filter (where Month = 'Feb')) as ko3iko.Count(*) filter (where Month = 'Feb'), AggRef(ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb')) as ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb'), AggRef(ko3iko.Count(*) filter (where Month = 'Jan')) as ko3iko.Count(*) filter (where Month = 'Jan'), AggRef(ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan')) as ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan')]
    PhysicalSingleKeyAggregate [key: City (String)] [aggs: Count(*) filter (where Month = 'Feb'), Sum(Money) filter (where Month = 'Feb'), Count(*) filter (where Month = 'Jan'), Sum(Money) filter (where Month = 'Jan')]
      PhysicalSchemaScan [#A.entities() as ko3iko]
  PhysicalSort [ko3iko.City]
    PhysicalProject [ko3iko.City as City, ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan') as Jan_Sales, ko3iko.Count(*) filter (where Month = 'Jan') as Jan_Orders, ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb') as Feb_Sales, ko3iko.Count(*) filter (where Month = 'Feb') as Feb_Orders]
      PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      City: string <- property City
      Money: decimal <- property Money
      Month: string <- property Month
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 4]
    Generated [ResultRow0]
      City: string <- field City
      Jan_Sales: decimal? <- field Jan_Sales
      Jan_Orders: long <- field Jan_Orders
      Feb_Sales: decimal? <- field Feb_Sales
      Feb_Orders: long <- field Feb_Orders

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [groups: string -> ResultAggregateGroup]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [month: string = ko3iko.Month]
      Let [__expr: bool? = (month = 'Feb')]
      Let [__expr1: bool? = (month = 'Jan')]
      GetOrAddSingleKeyAggregateGroup [group = groups[ko3iko.City] by City; typed: ResultAggregateGroup]
      TypedAggregateSet [Set(group.__agg0) filter __expr]
      TypedAggregateSet [Set(group.__agg1, ko3iko.Money) filter __expr]
      TypedAggregateSet [Set(group.__agg2) filter __expr1]
      TypedAggregateSet [Set(group.__agg3, ko3iko.Money) filter __expr1]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(City: finalGroup.City, Jan_Sales: ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan'), Jan_Orders: ko3iko.Count(*) filter (where Month = 'Jan'), Feb_Sales: ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb'), Feb_Orders: ko3iko.Count(*) filter (where Month = 'Feb'))]
    SortOwnedBuffer [result by City ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q163_PivotMultipleMeasures
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
            new Column("City", typeof(string), 0),
            new Column("Jan_Sales", typeof(decimal?), 1),
            new Column("Jan_Orders", typeof(long), 2),
            new Column("Feb_Sales", typeof(decimal?), 3),
            new Column("Feb_Orders", typeof(long), 4)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 0), new Column("Money", typeof(decimal), 1), new Column("Month", typeof(string), 2) });
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
                yield return new ResultRow0(__musoqShapeRow.City, __musoqShapeRow.Jan_Sales, __musoqShapeRow.Jan_Orders, __musoqShapeRow.Feb_Sales, __musoqShapeRow.Feb_Orders);
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
                var __ko3ikoSchema = provider.GetSchema("#A");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__ko3ikoSchema, "entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<string, ResultAggregateGroup>();
                ResultAggregateGroup nullGroup = null;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, ko3iko);
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, ko3iko);
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
                        UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, ko3iko);
                    }
                }

                result.EnsureCapacity(groupsToFinalize.Count);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    result.Add(new ResultShape0(finalGroup.__key0, finalGroup.__agg3.HasValue ? (decimal?)finalGroup.__agg3.Value : null, finalGroup.__agg2.Count, finalGroup.__agg1.HasValue ? (decimal?)finalGroup.__agg1.Value : null, finalGroup.__agg0.Count) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.City, right.City);
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
        private static void UpdateGroupsAggregates(List<ResultAggregateGroup> groupsToFinalize, Dictionary<string, ResultAggregateGroup> groups, ref ResultAggregateGroup nullGroup, Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko)
        {
            string month = ko3iko.Month;
            bool? __expr = Operators.SqlCompare<string, string>(month, "Feb", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight));
            bool? __expr1 = Operators.SqlCompare<string, string>(month, "Jan", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight));
            string groupKey = ko3iko.City;
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

            if (__expr == true)
            {
                group.__agg0.Count = checked(group.__agg0.Count + 1L);
            }

            if (__expr == true)
            {
                {
                    var __agg1Input = (decimal?)ko3iko.Money;
                    if (__agg1Input.HasValue)
                    {
                        var __agg1Current = __agg1Input.GetValueOrDefault();
                        group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                        group.__agg1.HasValue = true;
                    }
                }
            }

            if (__expr1 == true)
            {
                group.__agg2.Count = checked(group.__agg2.Count + 1L);
            }

            if (__expr1 == true)
            {
                {
                    var __agg3Input = (decimal?)ko3iko.Money;
                    if (__agg3Input.HasValue)
                    {
                        var __agg3Current = __agg3Input.GetValueOrDefault();
                        group.__agg3.Value = group.__agg3.HasValue ? checked(group.__agg3.Value + __agg3Current) : __agg3Current;
                        group.__agg3.HasValue = true;
                    }
                }
            }
        }

        private sealed class ResultAggregateGroup
        {
            public Musoq.Plugins.CountAllAggregateKernel.State __agg0;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg1;
            public Musoq.Plugins.CountAllAggregateKernel.State __agg2;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg3;
            public readonly string __key0;
            public ResultAggregateGroup(string __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.CountAllAggregateKernel.Reset(ref this.__agg0);
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg1);
                Musoq.Plugins.CountAllAggregateKernel.Reset(ref this.__agg2);
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg3);
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.CountAllAggregateKernel.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg1, in source.__agg1);
                Musoq.Plugins.CountAllAggregateKernel.Merge(ref this.__agg2, in source.__agg2);
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg3, in source.__agg3);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, decimal? __value1, long __value2, decimal? __value3, long __value4)
            {
                City = __value0;
                Jan_Sales = __value1;
                Jan_Orders = __value2;
                Feb_Sales = __value3;
                Feb_Orders = __value4;
            }

            public string City { get; private set; }
            public override int Count => 5;
            public long Feb_Orders { get; private set; }
            public decimal? Feb_Sales { get; private set; }
            public long Jan_Orders { get; private set; }
            public decimal? Jan_Sales { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Jan_Sales = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Jan_Orders = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Feb_Sales = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Feb_Orders = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Jan_Sales),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Jan_Orders),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Feb_Sales),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Feb_Orders),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "City" => true,
                "Jan_Sales" => true,
                "Jan_Orders" => true,
                "Feb_Sales" => true,
                "Feb_Orders" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.City, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Jan_Sales, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Jan_Orders, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Feb_Sales, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.Feb_Orders, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string City, decimal? Jan_Sales, long Jan_Orders, decimal? Feb_Sales, long Feb_Orders)
            {
                this.City = City;
                this.Jan_Sales = Jan_Sales;
                this.Jan_Orders = Jan_Orders;
                this.Feb_Sales = Feb_Sales;
                this.Feb_Orders = Feb_Orders;
            }

            public string City { get; }
            public long Feb_Orders { get; }
            public decimal? Feb_Sales { get; }
            public long Jan_Orders { get; }
            public decimal? Jan_Sales { get; }
            public int __musoqSequence { get; init; }
        }
    }
}
