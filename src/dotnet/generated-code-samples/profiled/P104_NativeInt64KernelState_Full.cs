// === Parsed Query ===
/*
select Sum(Value) as Total, Avg(Value) as Average, Min(Value) as Minimum, Max(Value) as Maximum, Count(Value) as Present from #schema.first()
*/

// === Logical Plan ===
/*
MultiStatement
  Project [1 as 1, AggRef(ko3iko.Count(ko3iko.Value)) as ko3iko.Count(ko3iko.Value), AggRef(ko3iko.Max(ko3iko.Value)) as ko3iko.Max(ko3iko.Value), AggRef(ko3iko.Min(ko3iko.Value)) as ko3iko.Min(ko3iko.Value), AggRef(ko3iko.Avg(ko3iko.Value)) as ko3iko.Avg(ko3iko.Value), AggRef(ko3iko.Sum(ko3iko.Value)) as ko3iko.Sum(ko3iko.Value)]
    Aggregate [keys: 1] [aggs: Count(Value), Max(Value), Min(Value), Avg(Value), Sum(Value)]
      SchemaScan [#schema.first() as ko3iko]
  Project [ko3iko.Sum(ko3iko.Value) as Total, ko3iko.Avg(ko3iko.Value) as Average, ko3iko.Min(ko3iko.Value) as Minimum, ko3iko.Max(ko3iko.Value) as Maximum, ko3iko.Count(ko3iko.Value) as Present]
    CteRef [ko3ikoScore as ko3ikoScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [AggRef(ko3iko.Count(ko3iko.Value)) as ko3iko.Count(ko3iko.Value), AggRef(ko3iko.Max(ko3iko.Value)) as ko3iko.Max(ko3iko.Value), AggRef(ko3iko.Min(ko3iko.Value)) as ko3iko.Min(ko3iko.Value), AggRef(ko3iko.Avg(ko3iko.Value)) as ko3iko.Avg(ko3iko.Value), AggRef(ko3iko.Sum(ko3iko.Value)) as ko3iko.Sum(ko3iko.Value)]
    PhysicalSingleKeyAggregate [key: 1 (Int16)] [aggs: Count(Value), Max(Value), Min(Value), Avg(Value), Sum(Value)]
      PhysicalSchemaScan [#schema.first() as ko3iko]
  PhysicalProject [ko3iko.Sum(ko3iko.Value) as Total, ko3iko.Avg(ko3iko.Value) as Average, ko3iko.Min(ko3iko.Value) as Minimum, ko3iko.Max(ko3iko.Value) as Maximum, ko3iko.Count(ko3iko.Value) as Present]
    PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: NumericInput<long>]
      Value: long? <- property Value
    AggregateGroup [ResultAggregateGroup; keys: 0; typed aggs: 5]
    Generated [ResultRow0]
      Total: long? <- field Total
      Average: decimal? <- field Average
      Minimum: long? <- field Minimum
      Maximum: long? <- field Maximum
      Present: long <- field Present

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: NumericInput<long>] -> ko3ikoRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateAggregateContext [rootGroup, group, groupsToFinalize; typed: ResultAggregateGroup]
    ChunkedForEach [ko3iko in ko3ikoRows]
      EnsureAggregateGroup [group; typed: ResultAggregateGroup]
      Let [value: long? = ko3iko.Value]
      TypedAggregateSet [Set(group.__agg0, value)]
      Let [value1: long? = ko3iko.Value]
      TypedAggregateSet [Set(group.__agg1, value1)]
      Let [value2: long? = ko3iko.Value]
      TypedAggregateSet [Set(group.__agg2, value2)]
      Let [value3: long? = ko3iko.Value]
      TypedAggregateSet [Set(group.__agg3, value3)]
      Let [value4: long? = ko3iko.Value]
      TypedAggregateSet [Set(group.__agg4, value4)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    PhaseBoundary [Select]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultRow0(Total: ko3iko.Sum(ko3iko.Value), Average: ko3iko.Avg(ko3iko.Value), Minimum: ko3iko.Min(ko3iko.Value), Maximum: ko3iko.Max(ko3iko.Value), Present: ko3iko.Count(ko3iko.Value))]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P104_NativeInt64KernelState_Full
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
            new Column("Total", typeof(long?), 0),
            new Column("Average", typeof(decimal?), 1),
            new Column("Minimum", typeof(long?), 2),
            new Column("Maximum", typeof(long?), 3),
            new Column("Present", typeof(long), 4)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Value", typeof(long?), 0) });
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

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            ArgumentNullException.ThrowIfNull(profileRecorder);
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0_Profiled(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken, profileRecorder), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return __musoqShapeRow;
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
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
                var __ko3ikoSchema = provider.GetSchema("#schema");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionPerformanceQueryTests.NumericInput<long>>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionPerformanceQueryTests.NumericInput<long>>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var groupsToFinalize = new List<ResultAggregateGroup>();
                ResultAggregateGroup group = new ResultAggregateGroup();
                groupsToFinalize.Add(group);
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionPerformanceQueryTests.NumericInput<long>> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionPerformanceQueryTests.NumericInput<long>[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                if (group == null)
                                {
                                    group = new ResultAggregateGroup();
                                    groupsToFinalize.Add(group);
                                }

                                long? value = ko3iko.Value;
                                if (((long?)value).HasValue)
                                {
                                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                }

                                long? value1 = ko3iko.Value;
                                {
                                    var __agg1Input = (long?)value1;
                                    if (__agg1Input.HasValue)
                                    {
                                        var __agg1Current = __agg1Input.GetValueOrDefault();
                                        if (!group.__agg1.HasValue || __agg1Current > group.__agg1.Value)
                                        {
                                            group.__agg1.Value = __agg1Current;
                                        }

                                        group.__agg1.HasValue = true;
                                    }
                                }

                                long? value2 = ko3iko.Value;
                                {
                                    var __agg2Input = (long?)value2;
                                    if (__agg2Input.HasValue)
                                    {
                                        var __agg2Current = __agg2Input.GetValueOrDefault();
                                        if (!group.__agg2.HasValue || __agg2Current < group.__agg2.Value)
                                        {
                                            group.__agg2.Value = __agg2Current;
                                        }

                                        group.__agg2.HasValue = true;
                                    }
                                }

                                long? value3 = ko3iko.Value;
                                Musoq.Plugins.AverageKernel<long, decimal>.Set(ref group.__agg3, (long?)value3);
                                long? value4 = ko3iko.Value;
                                {
                                    var __agg4Input = (long?)value4;
                                    if (__agg4Input.HasValue)
                                    {
                                        var __agg4Current = __agg4Input.GetValueOrDefault();
                                        group.__agg4.Value = group.__agg4.HasValue ? checked(group.__agg4.Value + __agg4Current) : __agg4Current;
                                        group.__agg4.HasValue = true;
                                    }
                                }
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionPerformanceQueryTests.NumericInput<long>> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                if (group == null)
                                {
                                    group = new ResultAggregateGroup();
                                    groupsToFinalize.Add(group);
                                }

                                long? value = ko3iko.Value;
                                if (((long?)value).HasValue)
                                {
                                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                }

                                long? value1 = ko3iko.Value;
                                {
                                    var __agg1Input = (long?)value1;
                                    if (__agg1Input.HasValue)
                                    {
                                        var __agg1Current = __agg1Input.GetValueOrDefault();
                                        if (!group.__agg1.HasValue || __agg1Current > group.__agg1.Value)
                                        {
                                            group.__agg1.Value = __agg1Current;
                                        }

                                        group.__agg1.HasValue = true;
                                    }
                                }

                                long? value2 = ko3iko.Value;
                                {
                                    var __agg2Input = (long?)value2;
                                    if (__agg2Input.HasValue)
                                    {
                                        var __agg2Current = __agg2Input.GetValueOrDefault();
                                        if (!group.__agg2.HasValue || __agg2Current < group.__agg2.Value)
                                        {
                                            group.__agg2.Value = __agg2Current;
                                        }

                                        group.__agg2.HasValue = true;
                                    }
                                }

                                long? value3 = ko3iko.Value;
                                Musoq.Plugins.AverageKernel<long, decimal>.Set(ref group.__agg3, (long?)value3);
                                long? value4 = ko3iko.Value;
                                {
                                    var __agg4Input = (long?)value4;
                                    if (__agg4Input.HasValue)
                                    {
                                        var __agg4Current = __agg4Input.GetValueOrDefault();
                                        group.__agg4.Value = group.__agg4.HasValue ? checked(group.__agg4.Value + __agg4Current) : __agg4Current;
                                        group.__agg4.HasValue = true;
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
                        if (group == null)
                        {
                            group = new ResultAggregateGroup();
                            groupsToFinalize.Add(group);
                        }

                        long? value = ko3iko.Value;
                        if (((long?)value).HasValue)
                        {
                            group.__agg0.Count = checked(group.__agg0.Count + 1L);
                        }

                        long? value1 = ko3iko.Value;
                        {
                            var __agg1Input = (long?)value1;
                            if (__agg1Input.HasValue)
                            {
                                var __agg1Current = __agg1Input.GetValueOrDefault();
                                if (!group.__agg1.HasValue || __agg1Current > group.__agg1.Value)
                                {
                                    group.__agg1.Value = __agg1Current;
                                }

                                group.__agg1.HasValue = true;
                            }
                        }

                        long? value2 = ko3iko.Value;
                        {
                            var __agg2Input = (long?)value2;
                            if (__agg2Input.HasValue)
                            {
                                var __agg2Current = __agg2Input.GetValueOrDefault();
                                if (!group.__agg2.HasValue || __agg2Current < group.__agg2.Value)
                                {
                                    group.__agg2.Value = __agg2Current;
                                }

                                group.__agg2.HasValue = true;
                            }
                        }

                        long? value3 = ko3iko.Value;
                        Musoq.Plugins.AverageKernel<long, decimal>.Set(ref group.__agg3, (long?)value3);
                        long? value4 = ko3iko.Value;
                        {
                            var __agg4Input = (long?)value4;
                            if (__agg4Input.HasValue)
                            {
                                var __agg4Current = __agg4Input.GetValueOrDefault();
                                group.__agg4.Value = group.__agg4.HasValue ? checked(group.__agg4.Value + __agg4Current) : __agg4Current;
                                group.__agg4.HasValue = true;
                            }
                        }
                    }
                }

                __musoqFinalShapeRows.EnsureCapacity(groupsToFinalize.Count);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    __musoqFinalShapeRows.Add(new ResultRow0(finalGroup.__agg4.HasValue ? (long?)finalGroup.__agg4.Value : null, Musoq.Plugins.AverageKernel<long, decimal>.Get(in finalGroup.__agg3), finalGroup.__agg2.HasValue ? (long?)finalGroup.__agg2.Value : null, finalGroup.__agg1.HasValue ? (long?)finalGroup.__agg1.Value : null, finalGroup.__agg0.Count));
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

        private IEnumerable<ResultRow0> ComputeShapeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
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
                    var __musoqFinalShapeRows = new List<ResultRow0>();
                    var __op13Handle = profileRecorder?.GetOperatorHandle("op13", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op14Handle = profileRecorder?.GetOperatorHandle("op14", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op15Handle = profileRecorder?.GetOperatorHandle("op15", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op17Handle = profileRecorder?.GetOperatorHandle("op17", "CreateAggregateContext") ?? OperatorProfileHandle.None;
                    var __op18Handle = profileRecorder?.GetOperatorHandle("op18", "ChunkedForEach") ?? OperatorProfileHandle.None;
                    var __op19Handle = profileRecorder?.GetOperatorHandle("op19", "EnsureAggregateGroup") ?? OperatorProfileHandle.None;
                    var __op21Handle = profileRecorder?.GetOperatorHandle("op21", "TypedAggregateSet") ?? OperatorProfileHandle.None;
                    var __op23Handle = profileRecorder?.GetOperatorHandle("op23", "TypedAggregateSet") ?? OperatorProfileHandle.None;
                    var __op25Handle = profileRecorder?.GetOperatorHandle("op25", "TypedAggregateSet") ?? OperatorProfileHandle.None;
                    var __op27Handle = profileRecorder?.GetOperatorHandle("op27", "TypedAggregateSet") ?? OperatorProfileHandle.None;
                    var __op29Handle = profileRecorder?.GetOperatorHandle("op29", "TypedAggregateSet") ?? OperatorProfileHandle.None;
                    var __op31Handle = profileRecorder?.GetOperatorHandle("op31", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op32Handle = profileRecorder?.GetOperatorHandle("op32", "ForEach") ?? OperatorProfileHandle.None;
                    var __op33Handle = profileRecorder?.GetOperatorHandle("op33", "AppendShape") ?? OperatorProfileHandle.None;
                    long __op21OutputRows = 0L;
                    long __op23OutputRows = 0L;
                    long __op25OutputRows = 0L;
                    long __op27OutputRows = 0L;
                    long __op29OutputRows = 0L;
                    long __op33OutputRows = 0L;
                    var __op13Scope = profileRecorder?.BeginOperatorValue(__op13Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op13Scope.Dispose();
                    var __op14Scope = profileRecorder?.BeginOperatorValue(__op14Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op14Scope.Dispose();
                    var __op15Scope = profileRecorder?.BeginOperatorValue(__op15Handle) ?? OperatorProfileValueScope.None;
                    var __ko3ikoSchema = provider.GetSchema("#schema");
                    var ko3ikoRowsProfile = profileRecorder?.CreateSourceRecorder("ko3iko");
                    var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionPerformanceQueryTests.NumericInput<long>>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress, ko3ikoRowsProfile == null ? SourceDiagnostics.None : ko3ikoRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                    var ko3ikoRows = ko3ikoRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionPerformanceQueryTests.NumericInput<long>>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionPerformanceQueryTests.NumericInput<long>>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionPerformanceQueryTests.NumericInput<long>>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks, ko3ikoRowsProfile);
                    __op15Scope.Dispose();
                    var __op17Scope = profileRecorder?.BeginOperatorValue(__op17Handle) ?? OperatorProfileValueScope.None;
                    var groupsToFinalize = new List<ResultAggregateGroup>();
                    ResultAggregateGroup group = new ResultAggregateGroup();
                    groupsToFinalize.Add(group);
                    __op17Scope.Dispose();
                    long __op18InputRows = 0L;
                    long __op18OutputRows = 0L;
                    var __op18Scope = profileRecorder?.BeginOperatorValue(__op18Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        foreach (var ko3ikoChunk in ko3ikoRows)
                        {
                            if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionPerformanceQueryTests.NumericInput<long>> ko3ikoChunkView)
                            {
                                if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionPerformanceQueryTests.NumericInput<long>[] ko3ikoChunkViewArray)
                                {
                                    int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                                    for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                                    {
                                        if ((ko3ikoIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                        __op18InputRows++;
                                        __op18OutputRows++;
                                        var __op19Scope = profileRecorder?.BeginOperatorValue(__op19Handle) ?? OperatorProfileValueScope.None;
                                        if (group == null)
                                        {
                                            group = new ResultAggregateGroup();
                                            groupsToFinalize.Add(group);
                                        }

                                        __op19Scope.Dispose();
                                        long? value = ko3iko.Value;
                                        if (((long?)value).HasValue)
                                        {
                                            group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                        }

                                        __op21OutputRows += 1;
                                        long? value1 = ko3iko.Value;
                                        {
                                            var __agg1Input = (long?)value1;
                                            if (__agg1Input.HasValue)
                                            {
                                                var __agg1Current = __agg1Input.GetValueOrDefault();
                                                if (!group.__agg1.HasValue || __agg1Current > group.__agg1.Value)
                                                {
                                                    group.__agg1.Value = __agg1Current;
                                                }

                                                group.__agg1.HasValue = true;
                                            }
                                        }

                                        __op23OutputRows += 1;
                                        long? value2 = ko3iko.Value;
                                        {
                                            var __agg2Input = (long?)value2;
                                            if (__agg2Input.HasValue)
                                            {
                                                var __agg2Current = __agg2Input.GetValueOrDefault();
                                                if (!group.__agg2.HasValue || __agg2Current < group.__agg2.Value)
                                                {
                                                    group.__agg2.Value = __agg2Current;
                                                }

                                                group.__agg2.HasValue = true;
                                            }
                                        }

                                        __op25OutputRows += 1;
                                        long? value3 = ko3iko.Value;
                                        Musoq.Plugins.AverageKernel<long, decimal>.Set(ref group.__agg3, (long?)value3);
                                        __op27OutputRows += 1;
                                        long? value4 = ko3iko.Value;
                                        {
                                            var __agg4Input = (long?)value4;
                                            if (__agg4Input.HasValue)
                                            {
                                                var __agg4Current = __agg4Input.GetValueOrDefault();
                                                group.__agg4.Value = group.__agg4.HasValue ? checked(group.__agg4.Value + __agg4Current) : __agg4Current;
                                                group.__agg4.HasValue = true;
                                            }
                                        }

                                        __op29OutputRows += 1;
                                    }

                                    continue;
                                }

                                if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionPerformanceQueryTests.NumericInput<long>> ko3ikoChunkViewList)
                                {
                                    int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                                    for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                                    {
                                        if ((ko3ikoIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                        __op18InputRows++;
                                        __op18OutputRows++;
                                        var __op19Scope = profileRecorder?.BeginOperatorValue(__op19Handle) ?? OperatorProfileValueScope.None;
                                        if (group == null)
                                        {
                                            group = new ResultAggregateGroup();
                                            groupsToFinalize.Add(group);
                                        }

                                        __op19Scope.Dispose();
                                        long? value = ko3iko.Value;
                                        if (((long?)value).HasValue)
                                        {
                                            group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                        }

                                        __op21OutputRows += 1;
                                        long? value1 = ko3iko.Value;
                                        {
                                            var __agg1Input = (long?)value1;
                                            if (__agg1Input.HasValue)
                                            {
                                                var __agg1Current = __agg1Input.GetValueOrDefault();
                                                if (!group.__agg1.HasValue || __agg1Current > group.__agg1.Value)
                                                {
                                                    group.__agg1.Value = __agg1Current;
                                                }

                                                group.__agg1.HasValue = true;
                                            }
                                        }

                                        __op23OutputRows += 1;
                                        long? value2 = ko3iko.Value;
                                        {
                                            var __agg2Input = (long?)value2;
                                            if (__agg2Input.HasValue)
                                            {
                                                var __agg2Current = __agg2Input.GetValueOrDefault();
                                                if (!group.__agg2.HasValue || __agg2Current < group.__agg2.Value)
                                                {
                                                    group.__agg2.Value = __agg2Current;
                                                }

                                                group.__agg2.HasValue = true;
                                            }
                                        }

                                        __op25OutputRows += 1;
                                        long? value3 = ko3iko.Value;
                                        Musoq.Plugins.AverageKernel<long, decimal>.Set(ref group.__agg3, (long?)value3);
                                        __op27OutputRows += 1;
                                        long? value4 = ko3iko.Value;
                                        {
                                            var __agg4Input = (long?)value4;
                                            if (__agg4Input.HasValue)
                                            {
                                                var __agg4Current = __agg4Input.GetValueOrDefault();
                                                group.__agg4.Value = group.__agg4.HasValue ? checked(group.__agg4.Value + __agg4Current) : __agg4Current;
                                                group.__agg4.HasValue = true;
                                            }
                                        }

                                        __op29OutputRows += 1;
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
                                __op18InputRows++;
                                __op18OutputRows++;
                                var __op19Scope = profileRecorder?.BeginOperatorValue(__op19Handle) ?? OperatorProfileValueScope.None;
                                if (group == null)
                                {
                                    group = new ResultAggregateGroup();
                                    groupsToFinalize.Add(group);
                                }

                                __op19Scope.Dispose();
                                long? value = ko3iko.Value;
                                if (((long?)value).HasValue)
                                {
                                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                }

                                __op21OutputRows += 1;
                                long? value1 = ko3iko.Value;
                                {
                                    var __agg1Input = (long?)value1;
                                    if (__agg1Input.HasValue)
                                    {
                                        var __agg1Current = __agg1Input.GetValueOrDefault();
                                        if (!group.__agg1.HasValue || __agg1Current > group.__agg1.Value)
                                        {
                                            group.__agg1.Value = __agg1Current;
                                        }

                                        group.__agg1.HasValue = true;
                                    }
                                }

                                __op23OutputRows += 1;
                                long? value2 = ko3iko.Value;
                                {
                                    var __agg2Input = (long?)value2;
                                    if (__agg2Input.HasValue)
                                    {
                                        var __agg2Current = __agg2Input.GetValueOrDefault();
                                        if (!group.__agg2.HasValue || __agg2Current < group.__agg2.Value)
                                        {
                                            group.__agg2.Value = __agg2Current;
                                        }

                                        group.__agg2.HasValue = true;
                                    }
                                }

                                __op25OutputRows += 1;
                                long? value3 = ko3iko.Value;
                                Musoq.Plugins.AverageKernel<long, decimal>.Set(ref group.__agg3, (long?)value3);
                                __op27OutputRows += 1;
                                long? value4 = ko3iko.Value;
                                {
                                    var __agg4Input = (long?)value4;
                                    if (__agg4Input.HasValue)
                                    {
                                        var __agg4Current = __agg4Input.GetValueOrDefault();
                                        group.__agg4.Value = group.__agg4.HasValue ? checked(group.__agg4.Value + __agg4Current) : __agg4Current;
                                        group.__agg4.HasValue = true;
                                    }
                                }

                                __op29OutputRows += 1;
                            }
                        }
                    }
                    finally
                    {
                        __op18Scope.AddInputRows(__op18InputRows);
                        __op18Scope.AddOutputRows(__op18OutputRows);
                        __op18Scope.Dispose();
                    }

                    __musoqFinalShapeRows.EnsureCapacity(groupsToFinalize.Count);
                    var __op31Scope = profileRecorder?.BeginOperatorValue(__op31Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op31Scope.Dispose();
                    long __op32InputRows = 0L;
                    long __op32OutputRows = 0L;
                    var __op32Scope = profileRecorder?.BeginOperatorValue(__op32Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        foreach (var finalGroup in groupsToFinalize)
                        {
                            token.ThrowIfCancellationRequested();
                            __op32InputRows++;
                            __op32OutputRows++;
                            __musoqFinalShapeRows.Add(new ResultRow0(finalGroup.__agg4.HasValue ? (long?)finalGroup.__agg4.Value : null, Musoq.Plugins.AverageKernel<long, decimal>.Get(in finalGroup.__agg3), finalGroup.__agg2.HasValue ? (long?)finalGroup.__agg2.Value : null, finalGroup.__agg1.HasValue ? (long?)finalGroup.__agg1.Value : null, finalGroup.__agg0.Count));
                            __op33OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op32Scope.AddInputRows(__op32InputRows);
                        __op32Scope.AddOutputRows(__op32OutputRows);
                        __op32Scope.Dispose();
                    }

                    if (__op21OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op21Handle, __op21OutputRows);
                    if (__op23OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op23Handle, __op23OutputRows);
                    if (__op25OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op25Handle, __op25OutputRows);
                    if (__op27OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op27Handle, __op27OutputRows);
                    if (__op29OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op29Handle, __op29OutputRows);
                    if (__op33OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op33Handle, __op33OutputRows);
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
            public Musoq.Plugins.CountNullableAggregateKernel<long>.State __agg0;
            public Musoq.Plugins.MaxAggregateKernel<long>.State __agg1;
            public Musoq.Plugins.MinAggregateKernel<long>.State __agg2;
            public Musoq.Plugins.AverageKernel<long, decimal>.State __agg3;
            public Musoq.Plugins.SumAggregateKernel<long>.State __agg4;
            public ResultAggregateGroup()
            {
                Musoq.Plugins.CountNullableAggregateKernel<long>.Reset(ref this.__agg0);
                Musoq.Plugins.MaxAggregateKernel<long>.Reset(ref this.__agg1);
                Musoq.Plugins.MinAggregateKernel<long>.Reset(ref this.__agg2);
                Musoq.Plugins.AverageKernel<long, decimal>.Reset(ref this.__agg3);
                Musoq.Plugins.SumAggregateKernel<long>.Reset(ref this.__agg4);
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.CountNullableAggregateKernel<long>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.MaxAggregateKernel<long>.Merge(ref this.__agg1, in source.__agg1);
                Musoq.Plugins.MinAggregateKernel<long>.Merge(ref this.__agg2, in source.__agg2);
                Musoq.Plugins.AverageKernel<long, decimal>.Merge(ref this.__agg3, in source.__agg3);
                Musoq.Plugins.SumAggregateKernel<long>.Merge(ref this.__agg4, in source.__agg4);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(long? __value0, decimal? __value1, long? __value2, long? __value3, long __value4)
            {
                Total = __value0;
                Average = __value1;
                Minimum = __value2;
                Maximum = __value3;
                Present = __value4;
            }

            public decimal? Average { get; private set; }
            public override int Count => 5;
            public long? Maximum { get; private set; }
            public long? Minimum { get; private set; }
            public long Present { get; private set; }
            public long? Total { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long?, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Average = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long?>(this.Minimum = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long?, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long?>(this.Maximum = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long?, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Present = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long?>(this.Total),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Average),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long?>(this.Minimum),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long?>(this.Maximum),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Present),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Total" => true,
                "Average" => true,
                "Minimum" => true,
                "Maximum" => true,
                "Present" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Average, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Minimum, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Maximum, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.Present, ref visitor);
            }
        }
    }
}
