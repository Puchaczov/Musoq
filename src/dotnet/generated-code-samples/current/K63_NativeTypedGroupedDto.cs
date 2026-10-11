// === Parsed Query ===
/*
select Number, Count(Position) as Total from #schema.first() group by Number
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Number as ko3iko.Number, AggRef(ko3iko.Count(ko3iko.Position)) as ko3iko.Count(ko3iko.Position)]
    Aggregate [keys: Number] [aggs: Count(Position)]
      SchemaScan [#schema.first() as ko3iko]
  Project [ko3iko.Number as Number, ko3iko.Count(ko3iko.Position) as Total]
    CteRef [ko3ikoScore as ko3ikoScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Number as ko3iko.Number, AggRef(ko3iko.Count(ko3iko.Position)) as ko3iko.Count(ko3iko.Position)]
    PhysicalSingleKeyAggregate [key: Number (Nullable`1)] [aggs: Count(Position)]
      PhysicalSchemaScan [#schema.first() as ko3iko]
  PhysicalProject [ko3iko.Number as Number, ko3iko.Count(ko3iko.Position) as Total]
    PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: NativeKeyInput]
      Position: int <- property Position
      Number: int? <- property Number
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 1]
    Generated [ResultRow0]
      Number: int? <- field Number
      Total: long <- field Total

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: NativeKeyInput] -> ko3ikoRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [groups: int? -> ResultAggregateGroup]
    ChunkedForEach [ko3iko in ko3ikoRows]
      GetOrAddSingleKeyAggregateGroup [group = groups[ko3iko.Number] by Number; typed: ResultAggregateGroup]
      Let [position: int = ko3iko.Position]
      TypedAggregateSet [Set(group.__agg0, position)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    PhaseBoundary [Select]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultRow0(Number: finalGroup.Number, Total: ko3iko.Count(ko3iko.Position))]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K63_NativeTypedGroupedDto
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

    public sealed class CompiledQuery : BaseOperations, ITypedRunnable<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeSubtotal>, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Number", typeof(int?), 1) });
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Number", typeof(int?), 0),
            new Column("Total", typeof(long), 1)
        };
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public ILogger Logger { get; set; }
        public TypedParameterSnapshot TypedParameters { get; set; } = TypedParameterSnapshot.Empty;
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();

        public event QueryPhaseEventHandler PhaseChanged;
        public event DataSourceEventHandler DataSourceProgress;
        public event QueryProgressEventHandler QueryProgress;
        public IEnumerable<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeSubtotal> Run(TypedQueryRunOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            QueryRunContext? __musoqEnumerationRunContext = null;
            var __musoqInitialRunContext = QueryRunContext.Capture(options, this);
            return new QueryEnumerable<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeSubtotal>((queryToken) =>
            {
                var queryContext = new QueryRunContext(queryToken, __musoqInitialRunContext.TypedParameters, __musoqInitialRunContext.PhaseChanged, __musoqInitialRunContext.DataSourceProgress, __musoqInitialRunContext.Sender, queryId: __musoqInitialRunContext.QueryId, queryProgress: __musoqInitialRunContext.QueryProgress, queryProgressOptions: __musoqInitialRunContext.QueryProgress == null ? null : __musoqInitialRunContext.QueryProgressOptions)
                {
                    TypedParameters = __musoqInitialRunContext.TypedParameters
                };
                __musoqEnumerationRunContext = queryContext;
                return ComputeRows_compiled_0(__musoqInitialRunContext.Provider, __musoqInitialRunContext.SourceRuntimeSettingsBySourceContextId, __musoqInitialRunContext.SourceExecutionPlans, __musoqInitialRunContext.Logger, queryContext);
            }, options.CancellationToken, onCompleted: __musoqInitialRunContext.QueryProgress == null ? null : (Action)(() => __musoqEnumerationRunContext?.CompleteQueryProgress()), onException: __musoqInitialRunContext.QueryProgress == null ? null : (Action<Exception>)(_ => __musoqEnumerationRunContext?.CompleteQueryProgress()), onDisposed: __musoqInitialRunContext.QueryProgress == null ? null : (Action)(() => __musoqEnumerationRunContext?.CompleteQueryProgress()));
        }

        public IEnumerable<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeSubtotal> Run(CancellationToken token)
        {
            return Run(new TypedQueryRunOptions(token, TypedParameters, PhaseChanged, DataSourceProgress, QueryProgress, null));
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            var token = queryContext.CancellationToken;
            var __musoqProgressContext = queryContext.QueryProgress == null ? null : queryContext;
            Action<string, QueryPhase> OnPhaseChanged = queryContext.NotifyPhaseChanged;
            DataSourceEventHandler OnDataSourceProgress = queryContext.NotifyDataSourceProgress;
            QueryProgressEventHandler OnQueryProgress = queryContext.QueryProgress;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(queryContext.TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#schema");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeKeyInput>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeKeyInput>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<int?, ResultAggregateGroup>();
                ResultAggregateGroup nullGroup = null;
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeKeyInput> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeKeyInput[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                int? groupKey = ko3iko.Number;
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

                                int position = ko3iko.Position;
                                if (((int?)position).HasValue)
                                {
                                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                }
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeKeyInput> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                int? groupKey = ko3iko.Number;
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

                                int position = ko3iko.Position;
                                if (((int?)position).HasValue)
                                {
                                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
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
                        int? groupKey = ko3iko.Number;
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

                        int position = ko3iko.Position;
                        if (((int?)position).HasValue)
                        {
                            group.__agg0.Count = checked(group.__agg0.Count + 1L);
                        }
                    }
                }

                __musoqFinalShapeRows.EnsureCapacity(groupsToFinalize.Count);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    __musoqFinalShapeRows.Add(new ResultShape0(finalGroup.__key0, finalGroup.__agg0.Count));
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

        private IEnumerable<Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeSubtotal> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, queryContext))
            {
                yield return new Musoq.Evaluator.Tests.LibraryExecutionNativeRowQueryTests.NativeSubtotal((int?)__musoqShapeRow.Number, (long)__musoqShapeRow.Total);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnPhaseChanged(string queryId, QueryPhase phase)
        {
            PhaseChanged?.Invoke(this, new QueryPhaseEventArgs(queryId, phase));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnDataSourceProgress(object sender, DataSourceEventArgs e)
        {
            DataSourceProgress?.Invoke(this, e);
        }

        private sealed class ResultAggregateGroup
        {
            public readonly int? __key0;
            public Musoq.Plugins.CountNullableAggregateKernel<int>.State __agg0;
            public ResultAggregateGroup(int? __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.CountNullableAggregateKernel<int>.Reset(ref this.__agg0);
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.CountNullableAggregateKernel<int>.Merge(ref this.__agg0, in source.__agg0);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int? Number, long Total)
            {
                this.Number = Number;
                this.Total = Total;
            }

            public int? Number { get; }
            public long Total { get; }
        }
    }
}
