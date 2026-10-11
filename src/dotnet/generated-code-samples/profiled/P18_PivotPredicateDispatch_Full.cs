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
      Let [money: decimal = ko3iko.Money]
      TypedAggregateSet [Set(group.__agg1, money) filter __expr]
      TypedAggregateSet [Set(group.__agg2) filter __expr1]
      Let [money1: decimal = ko3iko.Money]
      TypedAggregateSet [Set(group.__agg3, money1) filter __expr1]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(City: finalGroup.City, Jan_Sales: ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan'), Jan_Orders: ko3iko.Count(*) filter (where Month = 'Jan'), Feb_Sales: ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb'), Feb_Orders: ko3iko.Count(*) filter (where Month = 'Feb'))]
    SortShapeRows [result -> resultSorted by City ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P18_PivotPredicateDispatch_Full
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Musoq.Schema;
    using Musoq.Schema.Diagnostics;
    using Musoq.Schema.Optimization;
    using Musoq.Evaluator;
    using Musoq.Evaluator.Diagnostics;
    using Musoq.Evaluator.Tables;
    using Musoq.Evaluator.Helpers;
    using Musoq.Evaluator.Runtime;
    using Musoq.Schema.DataSources;
    using System.Linq;

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable, IProfiledRunnable
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

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            ArgumentNullException.ThrowIfNull(profileRecorder);
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0_Profiled(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken, profileRecorder), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.City, __musoqShapeRow.Jan_Sales, __musoqShapeRow.Jan_Orders, __musoqShapeRow.Feb_Sales, __musoqShapeRow.Feb_Orders);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
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
                var __musoqFinalShapeRows = new List<ResultShape0>();
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

                                decimal money = ko3iko.Money;
                                if (__expr == true)
                                {
                                    {
                                        var __agg1Input = (decimal?)money;
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

                                decimal money1 = ko3iko.Money;
                                if (__expr1 == true)
                                {
                                    {
                                        var __agg3Input = (decimal?)money1;
                                        if (__agg3Input.HasValue)
                                        {
                                            var __agg3Current = __agg3Input.GetValueOrDefault();
                                            group.__agg3.Value = group.__agg3.HasValue ? checked(group.__agg3.Value + __agg3Current) : __agg3Current;
                                            group.__agg3.HasValue = true;
                                        }
                                    }
                                }
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

                                decimal money = ko3iko.Money;
                                if (__expr == true)
                                {
                                    {
                                        var __agg1Input = (decimal?)money;
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

                                decimal money1 = ko3iko.Money;
                                if (__expr1 == true)
                                {
                                    {
                                        var __agg3Input = (decimal?)money1;
                                        if (__agg3Input.HasValue)
                                        {
                                            var __agg3Current = __agg3Input.GetValueOrDefault();
                                            group.__agg3.Value = group.__agg3.HasValue ? checked(group.__agg3.Value + __agg3Current) : __agg3Current;
                                            group.__agg3.HasValue = true;
                                        }
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

                        decimal money = ko3iko.Money;
                        if (__expr == true)
                        {
                            {
                                var __agg1Input = (decimal?)money;
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

                        decimal money1 = ko3iko.Money;
                        if (__expr1 == true)
                        {
                            {
                                var __agg3Input = (decimal?)money1;
                                if (__agg3Input.HasValue)
                                {
                                    var __agg3Current = __agg3Input.GetValueOrDefault();
                                    group.__agg3.Value = group.__agg3.HasValue ? checked(group.__agg3.Value + __agg3Current) : __agg3Current;
                                    group.__agg3.HasValue = true;
                                }
                            }
                        }
                    }
                }

                result.EnsureCapacity(groupsToFinalize.Count);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    result.Add(new ResultShape0(finalGroup.__key0, finalGroup.__agg3.HasValue ? (decimal?)finalGroup.__agg3.Value : null, finalGroup.__agg2.Count, finalGroup.__agg1.HasValue ? (decimal?)finalGroup.__agg1.Value : null, finalGroup.__agg0.Count));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.City, right.City);
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

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            var __profileScopeDepth = profileRecorder?.GetCurrentOperatorScopeDepth() ?? 0;
            try
            {
                QueryProgressEventHandler OnQueryProgress = QueryProgress;
                var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
                Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
                try
                {
                    var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                    ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                    var __musoqFinalShapeRows = new List<ResultShape0>();
                    var __op15Handle = profileRecorder?.GetOperatorHandle("op15", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op16Handle = profileRecorder?.GetOperatorHandle("op16", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op17Handle = profileRecorder?.GetOperatorHandle("op17", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op18Handle = profileRecorder?.GetOperatorHandle("op18", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op19Handle = profileRecorder?.GetOperatorHandle("op19", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op20Handle = profileRecorder?.GetOperatorHandle("op20", "CreateSingleKeyAggregateContext") ?? OperatorProfileHandle.None;
                    var __op21Handle = profileRecorder?.GetOperatorHandle("op21", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op22Handle = profileRecorder?.GetOperatorHandle("op22", "ChunkedForEach") ?? OperatorProfileHandle.None;
                    var __op26Handle = profileRecorder?.GetOperatorHandle("op26", "GetOrAddSingleKeyAggregateGroup") ?? OperatorProfileHandle.None;
                    var __op27Handle = profileRecorder?.GetOperatorHandle("op27", "TypedAggregateSet") ?? OperatorProfileHandle.None;
                    var __op29Handle = profileRecorder?.GetOperatorHandle("op29", "TypedAggregateSet") ?? OperatorProfileHandle.None;
                    var __op30Handle = profileRecorder?.GetOperatorHandle("op30", "TypedAggregateSet") ?? OperatorProfileHandle.None;
                    var __op32Handle = profileRecorder?.GetOperatorHandle("op32", "TypedAggregateSet") ?? OperatorProfileHandle.None;
                    var __op33Handle = profileRecorder?.GetOperatorHandle("op33", "EnsureShapeCapacity") ?? OperatorProfileHandle.None;
                    var __op34Handle = profileRecorder?.GetOperatorHandle("op34", "ForEach") ?? OperatorProfileHandle.None;
                    var __op35Handle = profileRecorder?.GetOperatorHandle("op35", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op36Handle = profileRecorder?.GetOperatorHandle("op36", "SortShapeRows") ?? OperatorProfileHandle.None;
                    long __op26InputRows = 0L;
                    long __op26OutputRows = 0L;
                    long __op27OutputRows = 0L;
                    long __op29OutputRows = 0L;
                    long __op30OutputRows = 0L;
                    long __op32OutputRows = 0L;
                    long __op35OutputRows = 0L;
                    var __op15Scope = profileRecorder?.BeginOperatorValue(__op15Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op15Scope.Dispose();
                    var __op16Scope = profileRecorder?.BeginOperatorValue(__op16Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op16Scope.Dispose();
                    var __op17Scope = profileRecorder?.BeginOperatorValue(__op17Handle) ?? OperatorProfileValueScope.None;
                    var __ko3ikoSchema = provider.GetSchema("#A");
                    var ko3ikoRowsProfile = profileRecorder?.CreateSourceRecorder("ko3iko");
                    var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__ko3ikoSchema, "entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress, ko3ikoRowsProfile == null ? SourceDiagnostics.None : ko3ikoRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                    var ko3ikoRows = ko3ikoRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks, ko3ikoRowsProfile);
                    __op17Scope.Dispose();
                    var __op18Scope = profileRecorder?.BeginOperatorValue(__op18Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>();
                    __op18Scope.Dispose();
                    var __op19Scope = profileRecorder?.BeginOperatorValue(__op19Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.GroupBy);
                    __op19Scope.Dispose();
                    var __op20Scope = profileRecorder?.BeginOperatorValue(__op20Handle) ?? OperatorProfileValueScope.None;
                    var groupsToFinalize = new List<ResultAggregateGroup>();
                    var groups = new Dictionary<string, ResultAggregateGroup>();
                    ResultAggregateGroup nullGroup = null;
                    __op20Scope.Dispose();
                    var __op21Scope = profileRecorder?.BeginOperatorValue(__op21Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op21Scope.Dispose();
                    long __op22InputRows = 0L;
                    long __op22OutputRows = 0L;
                    var __op22Scope = profileRecorder?.BeginOperatorValue(__op22Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
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
                                        __op22InputRows++;
                                        __op22OutputRows++;
                                        string month = ko3iko.Month;
                                        bool? __expr = Operators.SqlCompare<string, string>(month, "Feb", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight));
                                        bool? __expr1 = Operators.SqlCompare<string, string>(month, "Jan", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight));
                                        __op26InputRows += 1;
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

                                        __op26OutputRows += 1;
                                        if (__expr == true)
                                        {
                                            group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                        }

                                        __op27OutputRows += 1;
                                        decimal money = ko3iko.Money;
                                        if (__expr == true)
                                        {
                                            {
                                                var __agg1Input = (decimal?)money;
                                                if (__agg1Input.HasValue)
                                                {
                                                    var __agg1Current = __agg1Input.GetValueOrDefault();
                                                    group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                                    group.__agg1.HasValue = true;
                                                }
                                            }
                                        }

                                        __op29OutputRows += 1;
                                        if (__expr1 == true)
                                        {
                                            group.__agg2.Count = checked(group.__agg2.Count + 1L);
                                        }

                                        __op30OutputRows += 1;
                                        decimal money1 = ko3iko.Money;
                                        if (__expr1 == true)
                                        {
                                            {
                                                var __agg3Input = (decimal?)money1;
                                                if (__agg3Input.HasValue)
                                                {
                                                    var __agg3Current = __agg3Input.GetValueOrDefault();
                                                    group.__agg3.Value = group.__agg3.HasValue ? checked(group.__agg3.Value + __agg3Current) : __agg3Current;
                                                    group.__agg3.HasValue = true;
                                                }
                                            }
                                        }

                                        __op32OutputRows += 1;
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
                                        __op22InputRows++;
                                        __op22OutputRows++;
                                        string month = ko3iko.Month;
                                        bool? __expr = Operators.SqlCompare<string, string>(month, "Feb", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight));
                                        bool? __expr1 = Operators.SqlCompare<string, string>(month, "Jan", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight));
                                        __op26InputRows += 1;
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

                                        __op26OutputRows += 1;
                                        if (__expr == true)
                                        {
                                            group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                        }

                                        __op27OutputRows += 1;
                                        decimal money = ko3iko.Money;
                                        if (__expr == true)
                                        {
                                            {
                                                var __agg1Input = (decimal?)money;
                                                if (__agg1Input.HasValue)
                                                {
                                                    var __agg1Current = __agg1Input.GetValueOrDefault();
                                                    group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                                    group.__agg1.HasValue = true;
                                                }
                                            }
                                        }

                                        __op29OutputRows += 1;
                                        if (__expr1 == true)
                                        {
                                            group.__agg2.Count = checked(group.__agg2.Count + 1L);
                                        }

                                        __op30OutputRows += 1;
                                        decimal money1 = ko3iko.Money;
                                        if (__expr1 == true)
                                        {
                                            {
                                                var __agg3Input = (decimal?)money1;
                                                if (__agg3Input.HasValue)
                                                {
                                                    var __agg3Current = __agg3Input.GetValueOrDefault();
                                                    group.__agg3.Value = group.__agg3.HasValue ? checked(group.__agg3.Value + __agg3Current) : __agg3Current;
                                                    group.__agg3.HasValue = true;
                                                }
                                            }
                                        }

                                        __op32OutputRows += 1;
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
                                __op22InputRows++;
                                __op22OutputRows++;
                                string month = ko3iko.Month;
                                bool? __expr = Operators.SqlCompare<string, string>(month, "Feb", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight));
                                bool? __expr1 = Operators.SqlCompare<string, string>(month, "Jan", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight));
                                __op26InputRows += 1;
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

                                __op26OutputRows += 1;
                                if (__expr == true)
                                {
                                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                }

                                __op27OutputRows += 1;
                                decimal money = ko3iko.Money;
                                if (__expr == true)
                                {
                                    {
                                        var __agg1Input = (decimal?)money;
                                        if (__agg1Input.HasValue)
                                        {
                                            var __agg1Current = __agg1Input.GetValueOrDefault();
                                            group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                            group.__agg1.HasValue = true;
                                        }
                                    }
                                }

                                __op29OutputRows += 1;
                                if (__expr1 == true)
                                {
                                    group.__agg2.Count = checked(group.__agg2.Count + 1L);
                                }

                                __op30OutputRows += 1;
                                decimal money1 = ko3iko.Money;
                                if (__expr1 == true)
                                {
                                    {
                                        var __agg3Input = (decimal?)money1;
                                        if (__agg3Input.HasValue)
                                        {
                                            var __agg3Current = __agg3Input.GetValueOrDefault();
                                            group.__agg3.Value = group.__agg3.HasValue ? checked(group.__agg3.Value + __agg3Current) : __agg3Current;
                                            group.__agg3.HasValue = true;
                                        }
                                    }
                                }

                                __op32OutputRows += 1;
                            }
                        }
                    }
                    finally
                    {
                        __op22Scope.AddInputRows(__op22InputRows);
                        __op22Scope.AddOutputRows(__op22OutputRows);
                        __op22Scope.Dispose();
                    }

                    var __op33Scope = profileRecorder?.BeginOperatorValue(__op33Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        result.EnsureCapacity(groupsToFinalize.Count);
                    }
                    finally
                    {
                        __op33Scope.Dispose();
                    }

                    long __op34InputRows = 0L;
                    long __op34OutputRows = 0L;
                    var __op34Scope = profileRecorder?.BeginOperatorValue(__op34Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        foreach (var finalGroup in groupsToFinalize)
                        {
                            token.ThrowIfCancellationRequested();
                            __op34InputRows++;
                            __op34OutputRows++;
                            result.Add(new ResultShape0(finalGroup.__key0, finalGroup.__agg3.HasValue ? (decimal?)finalGroup.__agg3.Value : null, finalGroup.__agg2.Count, finalGroup.__agg1.HasValue ? (decimal?)finalGroup.__agg1.Value : null, finalGroup.__agg0.Count));
                            __op35OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op34Scope.AddInputRows(__op34InputRows);
                        __op34Scope.AddOutputRows(__op34OutputRows);
                        __op34Scope.Dispose();
                    }

                    var __op36Scope = profileRecorder?.BeginOperatorValue(__op36Handle) ?? OperatorProfileValueScope.None;
                    __op36Scope.AddInputRows(result.Count);
                    var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                    {
                        var comparison = StringComparer.Ordinal.Compare(left.City, right.City);
                        if (comparison != 0)
                            return comparison;
                        return 0;
                    }));
                    foreach (var resultSortedRowsRow in resultSortedRows)
                    {
                        __musoqFinalShapeRows.Add(resultSortedRowsRow);
                    }

                    __op36Scope.AddOutputRows(__musoqFinalShapeRows.Count);
                    __op36Scope.Dispose();
                    if (__op26InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op26Handle, __op26InputRows);
                    if (__op26OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op26Handle, __op26OutputRows);
                    if (__op27OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op27Handle, __op27OutputRows);
                    if (__op29OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op29Handle, __op29OutputRows);
                    if (__op30OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op30Handle, __op30OutputRows);
                    if (__op32OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op32Handle, __op32OutputRows);
                    if (__op35OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op35Handle, __op35OutputRows);
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
            catch (Exception __profileException)when (profileRecorder != null && profileRecorder.RecordActiveOperatorException(__profileException, __profileScopeDepth))
            {
                profileRecorder.DisposeActiveOperatorScopes(__profileScopeDepth);
                throw;
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
        }
    }
}
