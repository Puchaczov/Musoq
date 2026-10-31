// === Parsed Query ===
/*
select Partition, Sum(Integral) as Integers, Sum(Amount) as Decimals,
       Sum(Floating) as Doubles, Count(Amount) as Values
from #schema.first() group by Partition order by Partition
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Partition as ko3iko.Partition, AggRef(ko3iko.Count(ko3iko.Amount)) as ko3iko.Count(ko3iko.Amount), AggRef(ko3iko.Sum(ko3iko.Floating)) as ko3iko.Sum(ko3iko.Floating), AggRef(ko3iko.Sum(ko3iko.Amount)) as ko3iko.Sum(ko3iko.Amount), AggRef(ko3iko.Sum(ko3iko.Integral)) as ko3iko.Sum(ko3iko.Integral)]
    Aggregate [keys: Partition] [aggs: Count(Amount), Sum(Floating), Sum(Amount), Sum(Integral)]
      SchemaScan [#schema.first() as ko3iko]
  Sort [ko3iko.Partition]
    Project [ko3iko.Partition as Partition, ko3iko.Sum(ko3iko.Integral) as Integers, ko3iko.Sum(ko3iko.Amount) as Decimals, ko3iko.Sum(ko3iko.Floating) as Doubles, ko3iko.Count(ko3iko.Amount) as Values]
      CteRef [ko3ikoScore as ko3ikoScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Partition as ko3iko.Partition, AggRef(ko3iko.Count(ko3iko.Amount)) as ko3iko.Count(ko3iko.Amount), AggRef(ko3iko.Sum(ko3iko.Floating)) as ko3iko.Sum(ko3iko.Floating), AggRef(ko3iko.Sum(ko3iko.Amount)) as ko3iko.Sum(ko3iko.Amount), AggRef(ko3iko.Sum(ko3iko.Integral)) as ko3iko.Sum(ko3iko.Integral)]
    PhysicalSingleKeyAggregate [key: Partition (String)] [aggs: Count(Amount), Sum(Floating), Sum(Amount), Sum(Integral)]
      PhysicalSchemaScan [#schema.first() as ko3iko]
  PhysicalSort [ko3iko.Partition]
    PhysicalProject [ko3iko.Partition as Partition, ko3iko.Sum(ko3iko.Integral) as Integers, ko3iko.Sum(ko3iko.Amount) as Decimals, ko3iko.Sum(ko3iko.Floating) as Doubles, ko3iko.Count(ko3iko.Amount) as Values]
      PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: LibraryExecutionEntity]
      Partition: string <- property Partition
      Integral: int? <- property Integral
      Amount: decimal? <- property Amount
      Floating: double? <- property Floating
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 4]
    Generated [ResultRow0]
      Partition: string <- field Partition
      Integers: int? <- field Integers
      Decimals: decimal? <- field Decimals
      Doubles: double? <- field Doubles
      Values: long <- field Values

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
      Let [amount: decimal? = ko3iko.Amount]
      TypedAggregateSet [Set(group.__agg0, amount)]
      Let [floating: double? = ko3iko.Floating]
      TypedAggregateSet [Set(group.__agg1, floating)]
      Let [amount1: decimal? = ko3iko.Amount]
      TypedAggregateSet [Set(group.__agg2, amount1)]
      Let [integral: int? = ko3iko.Integral]
      TypedAggregateSet [Set(group.__agg3, integral)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(Partition: finalGroup.Partition, Integers: ko3iko.Sum(ko3iko.Integral), Decimals: ko3iko.Sum(ko3iko.Amount), Doubles: ko3iko.Sum(ko3iko.Floating), Values: ko3iko.Count(ko3iko.Amount))]
    SortOwnedBuffer [result by Partition ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P20_LibraryMixedNumericGroups_Full
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
            new Column("Partition", typeof(string), 0),
            new Column("Integers", typeof(int?), 1),
            new Column("Decimals", typeof(decimal?), 2),
            new Column("Doubles", typeof(double?), 3),
            new Column("Values", typeof(long), 4)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Partition", typeof(string), 0), new Column("Integral", typeof(int?), 1), new Column("Amount", typeof(decimal?), 2), new Column("Floating", typeof(double?), 3) });
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
                yield return new ResultRow0(__musoqShapeRow.Partition, __musoqShapeRow.Integers, __musoqShapeRow.Decimals, __musoqShapeRow.Doubles, __musoqShapeRow.Values);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.Partition, __musoqShapeRow.Integers, __musoqShapeRow.Decimals, __musoqShapeRow.Doubles, __musoqShapeRow.Values);
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

                                decimal? amount = ko3iko.Amount;
                                if (((decimal?)amount).HasValue)
                                {
                                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                }

                                double? floating = ko3iko.Floating;
                                {
                                    var __agg1Input = (double?)floating;
                                    if (__agg1Input.HasValue)
                                    {
                                        var __agg1Current = __agg1Input.GetValueOrDefault();
                                        group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                        group.__agg1.HasValue = true;
                                    }
                                }

                                decimal? amount1 = ko3iko.Amount;
                                {
                                    var __agg2Input = (decimal?)amount1;
                                    if (__agg2Input.HasValue)
                                    {
                                        var __agg2Current = __agg2Input.GetValueOrDefault();
                                        group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                        group.__agg2.HasValue = true;
                                    }
                                }

                                int? integral = ko3iko.Integral;
                                {
                                    var __agg3Input = (int?)integral;
                                    if (__agg3Input.HasValue)
                                    {
                                        var __agg3Current = __agg3Input.GetValueOrDefault();
                                        group.__agg3.Value = group.__agg3.HasValue ? checked(group.__agg3.Value + __agg3Current) : __agg3Current;
                                        group.__agg3.HasValue = true;
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

                                decimal? amount = ko3iko.Amount;
                                if (((decimal?)amount).HasValue)
                                {
                                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                }

                                double? floating = ko3iko.Floating;
                                {
                                    var __agg1Input = (double?)floating;
                                    if (__agg1Input.HasValue)
                                    {
                                        var __agg1Current = __agg1Input.GetValueOrDefault();
                                        group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                        group.__agg1.HasValue = true;
                                    }
                                }

                                decimal? amount1 = ko3iko.Amount;
                                {
                                    var __agg2Input = (decimal?)amount1;
                                    if (__agg2Input.HasValue)
                                    {
                                        var __agg2Current = __agg2Input.GetValueOrDefault();
                                        group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                        group.__agg2.HasValue = true;
                                    }
                                }

                                int? integral = ko3iko.Integral;
                                {
                                    var __agg3Input = (int?)integral;
                                    if (__agg3Input.HasValue)
                                    {
                                        var __agg3Current = __agg3Input.GetValueOrDefault();
                                        group.__agg3.Value = group.__agg3.HasValue ? checked(group.__agg3.Value + __agg3Current) : __agg3Current;
                                        group.__agg3.HasValue = true;
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

                        decimal? amount = ko3iko.Amount;
                        if (((decimal?)amount).HasValue)
                        {
                            group.__agg0.Count = checked(group.__agg0.Count + 1L);
                        }

                        double? floating = ko3iko.Floating;
                        {
                            var __agg1Input = (double?)floating;
                            if (__agg1Input.HasValue)
                            {
                                var __agg1Current = __agg1Input.GetValueOrDefault();
                                group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                group.__agg1.HasValue = true;
                            }
                        }

                        decimal? amount1 = ko3iko.Amount;
                        {
                            var __agg2Input = (decimal?)amount1;
                            if (__agg2Input.HasValue)
                            {
                                var __agg2Current = __agg2Input.GetValueOrDefault();
                                group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                group.__agg2.HasValue = true;
                            }
                        }

                        int? integral = ko3iko.Integral;
                        {
                            var __agg3Input = (int?)integral;
                            if (__agg3Input.HasValue)
                            {
                                var __agg3Current = __agg3Input.GetValueOrDefault();
                                group.__agg3.Value = group.__agg3.HasValue ? checked(group.__agg3.Value + __agg3Current) : __agg3Current;
                                group.__agg3.HasValue = true;
                            }
                        }
                    }
                }

                result.EnsureCapacity(groupsToFinalize.Count);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    result.Add(new ResultShape0(finalGroup.__key0, finalGroup.__agg3.HasValue ? (int?)finalGroup.__agg3.Value : null, finalGroup.__agg2.HasValue ? (decimal?)finalGroup.__agg2.Value : null, finalGroup.__agg1.HasValue ? (double?)finalGroup.__agg1.Value : null, finalGroup.__agg0.Count) { __musoqSequence = result.Count });
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
                    var __op16Handle = profileRecorder?.GetOperatorHandle("op16", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op17Handle = profileRecorder?.GetOperatorHandle("op17", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op18Handle = profileRecorder?.GetOperatorHandle("op18", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op19Handle = profileRecorder?.GetOperatorHandle("op19", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op20Handle = profileRecorder?.GetOperatorHandle("op20", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op21Handle = profileRecorder?.GetOperatorHandle("op21", "CreateSingleKeyAggregateContext") ?? OperatorProfileHandle.None;
                    var __op22Handle = profileRecorder?.GetOperatorHandle("op22", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op23Handle = profileRecorder?.GetOperatorHandle("op23", "ChunkedForEach") ?? OperatorProfileHandle.None;
                    var __op24Handle = profileRecorder?.GetOperatorHandle("op24", "GetOrAddSingleKeyAggregateGroup") ?? OperatorProfileHandle.None;
                    var __op26Handle = profileRecorder?.GetOperatorHandle("op26", "TypedAggregateSet") ?? OperatorProfileHandle.None;
                    var __op28Handle = profileRecorder?.GetOperatorHandle("op28", "TypedAggregateSet") ?? OperatorProfileHandle.None;
                    var __op30Handle = profileRecorder?.GetOperatorHandle("op30", "TypedAggregateSet") ?? OperatorProfileHandle.None;
                    var __op32Handle = profileRecorder?.GetOperatorHandle("op32", "TypedAggregateSet") ?? OperatorProfileHandle.None;
                    var __op33Handle = profileRecorder?.GetOperatorHandle("op33", "EnsureShapeCapacity") ?? OperatorProfileHandle.None;
                    var __op34Handle = profileRecorder?.GetOperatorHandle("op34", "ForEach") ?? OperatorProfileHandle.None;
                    var __op35Handle = profileRecorder?.GetOperatorHandle("op35", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op36Handle = profileRecorder?.GetOperatorHandle("op36", "SortOwnedBuffer") ?? OperatorProfileHandle.None;
                    long __op24InputRows = 0L;
                    long __op24OutputRows = 0L;
                    long __op26OutputRows = 0L;
                    long __op28OutputRows = 0L;
                    long __op30OutputRows = 0L;
                    long __op32OutputRows = 0L;
                    long __op35OutputRows = 0L;
                    var __op16Scope = profileRecorder?.BeginOperatorValue(__op16Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op16Scope.Dispose();
                    var __op17Scope = profileRecorder?.BeginOperatorValue(__op17Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op17Scope.Dispose();
                    var __op18Scope = profileRecorder?.BeginOperatorValue(__op18Handle) ?? OperatorProfileValueScope.None;
                    var __ko3ikoSchema = provider.GetSchema("#schema");
                    var ko3ikoRowsProfile = profileRecorder?.CreateSourceRecorder("ko3iko");
                    var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress, ko3ikoRowsProfile == null ? SourceDiagnostics.None : ko3ikoRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                    var ko3ikoRows = ko3ikoRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks, ko3ikoRowsProfile);
                    __op18Scope.Dispose();
                    var __op19Scope = profileRecorder?.BeginOperatorValue(__op19Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>();
                    __op19Scope.Dispose();
                    var __op20Scope = profileRecorder?.BeginOperatorValue(__op20Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.GroupBy);
                    __op20Scope.Dispose();
                    var __op21Scope = profileRecorder?.BeginOperatorValue(__op21Handle) ?? OperatorProfileValueScope.None;
                    var groupsToFinalize = new List<ResultAggregateGroup>();
                    var groups = new Dictionary<string, ResultAggregateGroup>();
                    ResultAggregateGroup nullGroup = null;
                    __op21Scope.Dispose();
                    var __op22Scope = profileRecorder?.BeginOperatorValue(__op22Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op22Scope.Dispose();
                    long __op23InputRows = 0L;
                    long __op23OutputRows = 0L;
                    var __op23Scope = profileRecorder?.BeginOperatorValue(__op23Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
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
                                        __op23InputRows++;
                                        __op23OutputRows++;
                                        __op24InputRows += 1;
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

                                        __op24OutputRows += 1;
                                        decimal? amount = ko3iko.Amount;
                                        if (((decimal?)amount).HasValue)
                                        {
                                            group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                        }

                                        __op26OutputRows += 1;
                                        double? floating = ko3iko.Floating;
                                        {
                                            var __agg1Input = (double?)floating;
                                            if (__agg1Input.HasValue)
                                            {
                                                var __agg1Current = __agg1Input.GetValueOrDefault();
                                                group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                                group.__agg1.HasValue = true;
                                            }
                                        }

                                        __op28OutputRows += 1;
                                        decimal? amount1 = ko3iko.Amount;
                                        {
                                            var __agg2Input = (decimal?)amount1;
                                            if (__agg2Input.HasValue)
                                            {
                                                var __agg2Current = __agg2Input.GetValueOrDefault();
                                                group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                                group.__agg2.HasValue = true;
                                            }
                                        }

                                        __op30OutputRows += 1;
                                        int? integral = ko3iko.Integral;
                                        {
                                            var __agg3Input = (int?)integral;
                                            if (__agg3Input.HasValue)
                                            {
                                                var __agg3Current = __agg3Input.GetValueOrDefault();
                                                group.__agg3.Value = group.__agg3.HasValue ? checked(group.__agg3.Value + __agg3Current) : __agg3Current;
                                                group.__agg3.HasValue = true;
                                            }
                                        }

                                        __op32OutputRows += 1;
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
                                        __op23InputRows++;
                                        __op23OutputRows++;
                                        __op24InputRows += 1;
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

                                        __op24OutputRows += 1;
                                        decimal? amount = ko3iko.Amount;
                                        if (((decimal?)amount).HasValue)
                                        {
                                            group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                        }

                                        __op26OutputRows += 1;
                                        double? floating = ko3iko.Floating;
                                        {
                                            var __agg1Input = (double?)floating;
                                            if (__agg1Input.HasValue)
                                            {
                                                var __agg1Current = __agg1Input.GetValueOrDefault();
                                                group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                                group.__agg1.HasValue = true;
                                            }
                                        }

                                        __op28OutputRows += 1;
                                        decimal? amount1 = ko3iko.Amount;
                                        {
                                            var __agg2Input = (decimal?)amount1;
                                            if (__agg2Input.HasValue)
                                            {
                                                var __agg2Current = __agg2Input.GetValueOrDefault();
                                                group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                                group.__agg2.HasValue = true;
                                            }
                                        }

                                        __op30OutputRows += 1;
                                        int? integral = ko3iko.Integral;
                                        {
                                            var __agg3Input = (int?)integral;
                                            if (__agg3Input.HasValue)
                                            {
                                                var __agg3Current = __agg3Input.GetValueOrDefault();
                                                group.__agg3.Value = group.__agg3.HasValue ? checked(group.__agg3.Value + __agg3Current) : __agg3Current;
                                                group.__agg3.HasValue = true;
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
                                __op23InputRows++;
                                __op23OutputRows++;
                                __op24InputRows += 1;
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

                                __op24OutputRows += 1;
                                decimal? amount = ko3iko.Amount;
                                if (((decimal?)amount).HasValue)
                                {
                                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                }

                                __op26OutputRows += 1;
                                double? floating = ko3iko.Floating;
                                {
                                    var __agg1Input = (double?)floating;
                                    if (__agg1Input.HasValue)
                                    {
                                        var __agg1Current = __agg1Input.GetValueOrDefault();
                                        group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                        group.__agg1.HasValue = true;
                                    }
                                }

                                __op28OutputRows += 1;
                                decimal? amount1 = ko3iko.Amount;
                                {
                                    var __agg2Input = (decimal?)amount1;
                                    if (__agg2Input.HasValue)
                                    {
                                        var __agg2Current = __agg2Input.GetValueOrDefault();
                                        group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                        group.__agg2.HasValue = true;
                                    }
                                }

                                __op30OutputRows += 1;
                                int? integral = ko3iko.Integral;
                                {
                                    var __agg3Input = (int?)integral;
                                    if (__agg3Input.HasValue)
                                    {
                                        var __agg3Current = __agg3Input.GetValueOrDefault();
                                        group.__agg3.Value = group.__agg3.HasValue ? checked(group.__agg3.Value + __agg3Current) : __agg3Current;
                                        group.__agg3.HasValue = true;
                                    }
                                }

                                __op32OutputRows += 1;
                            }
                        }
                    }
                    finally
                    {
                        __op23Scope.AddInputRows(__op23InputRows);
                        __op23Scope.AddOutputRows(__op23OutputRows);
                        __op23Scope.Dispose();
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
                            result.Add(new ResultShape0(finalGroup.__key0, finalGroup.__agg3.HasValue ? (int?)finalGroup.__agg3.Value : null, finalGroup.__agg2.HasValue ? (decimal?)finalGroup.__agg2.Value : null, finalGroup.__agg1.HasValue ? (double?)finalGroup.__agg1.Value : null, finalGroup.__agg0.Count) { __musoqSequence = result.Count });
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
                    result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                    {
                        var comparison = StringComparer.Ordinal.Compare(left.Partition, right.Partition);
                        if (comparison != 0)
                            return comparison;
                        return left.__musoqSequence.CompareTo(right.__musoqSequence);
                    }));
                    __op36Scope.AddOutputRows(result.Count);
                    __op36Scope.Dispose();
                    if (__op24InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op24Handle, __op24InputRows);
                    if (__op24OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op24Handle, __op24OutputRows);
                    if (__op26OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op26Handle, __op26OutputRows);
                    if (__op28OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op28Handle, __op28OutputRows);
                    if (__op30OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op30Handle, __op30OutputRows);
                    if (__op32OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op32Handle, __op32OutputRows);
                    if (__op35OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op35Handle, __op35OutputRows);
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
            public Musoq.Plugins.CountNullableAggregateKernel<decimal>.State __agg0;
            public Musoq.Plugins.SumAggregateKernel<double>.State __agg1;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg2;
            public Musoq.Plugins.SumAggregateKernel<int>.State __agg3;
            public readonly string __key0;
            public ResultAggregateGroup(string __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.CountNullableAggregateKernel<decimal>.Reset(ref this.__agg0);
                Musoq.Plugins.SumAggregateKernel<double>.Reset(ref this.__agg1);
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg2);
                Musoq.Plugins.SumAggregateKernel<int>.Reset(ref this.__agg3);
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.CountNullableAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.SumAggregateKernel<double>.Merge(ref this.__agg1, in source.__agg1);
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg2, in source.__agg2);
                Musoq.Plugins.SumAggregateKernel<int>.Merge(ref this.__agg3, in source.__agg3);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, int? __value1, decimal? __value2, double? __value3, long __value4)
            {
                Partition = __value0;
                Integers = __value1;
                Decimals = __value2;
                Doubles = __value3;
                Values = __value4;
            }

            public override int Count => 5;
            public decimal? Decimals { get; private set; }
            public double? Doubles { get; private set; }
            public int? Integers { get; private set; }
            public string Partition { get; private set; }
            public long Values { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Partition = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Integers = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Decimals = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double?>(this.Doubles = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<double?, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Values = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Partition),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Integers),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Decimals),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double?>(this.Doubles),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Values),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Partition" => true,
                "Integers" => true,
                "Decimals" => true,
                "Doubles" => true,
                "Values" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Partition, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Integers, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Decimals, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Doubles, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.Values, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Partition, int? Integers, decimal? Decimals, double? Doubles, long Values)
            {
                this.Partition = Partition;
                this.Integers = Integers;
                this.Decimals = Decimals;
                this.Doubles = Doubles;
                this.Values = Values;
            }

            public decimal? Decimals { get; }
            public double? Doubles { get; }
            public int? Integers { get; }
            public string Partition { get; }
            public long Values { get; }
            public int __musoqSequence { get; init; }
        }
    }
}
