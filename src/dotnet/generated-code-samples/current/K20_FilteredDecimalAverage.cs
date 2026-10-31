// === Parsed Query ===
/*
select Avg(Integral) filter (where Position > 1) as Average,
    Count(*) filter (where Position > 1) as Rows from #schema.first()
*/

// === Logical Plan ===
/*
MultiStatement
  Project [1 as 1, AggRef(ko3iko.Count(*) filter (where Position > 1)) as ko3iko.Count(*) filter (where Position > 1), AggRef(ko3iko.Avg(ko3iko.Integral) filter (where Position > 1)) as ko3iko.Avg(ko3iko.Integral) filter (where Position > 1)]
    Aggregate [keys: 1] [aggs: Count(*) filter (where Position > 1), Avg(Integral) filter (where Position > 1)]
      SchemaScan [#schema.first() as ko3iko]
  Project [ko3iko.Avg(ko3iko.Integral) filter (where Position > 1) as Average, ko3iko.Count(*) filter (where Position > 1) as Rows]
    CteRef [ko3ikoScore as ko3ikoScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [AggRef(ko3iko.Count(*) filter (where Position > 1)) as ko3iko.Count(*) filter (where Position > 1), AggRef(ko3iko.Avg(ko3iko.Integral) filter (where Position > 1)) as ko3iko.Avg(ko3iko.Integral) filter (where Position > 1)]
    PhysicalSingleKeyAggregate [key: 1 (Int16)] [aggs: Count(*) filter (where Position > 1), Avg(Integral) filter (where Position > 1)]
      PhysicalSchemaScan [#schema.first() as ko3iko]
  PhysicalProject [ko3iko.Avg(ko3iko.Integral) filter (where Position > 1) as Average, ko3iko.Count(*) filter (where Position > 1) as Rows]
    PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: LibraryExecutionEntity]
      Position: int <- property Position
      Integral: int? <- property Integral
    AggregateGroup [ResultAggregateGroup; keys: 0; typed aggs: 2]
    Generated [ResultRow0]
      Average: decimal? <- field Average
      Rows: long <- field Rows

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: LibraryExecutionEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateAggregateContext [rootGroup, group, groupsToFinalize; typed: ResultAggregateGroup]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [position: int = ko3iko.Position]
      Let [__expr: bool = (position > 1)]
      EnsureAggregateGroup [group; typed: ResultAggregateGroup]
      TypedAggregateSet [Set(group.__agg0) filter __expr]
      TypedAggregateSet [Set(group.__agg1, ko3iko.Integral) filter __expr]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    PhaseBoundary [Select]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultRow0(Average: ko3iko.Avg(ko3iko.Integral) filter (where Position > 1), Rows: ko3iko.Count(*) filter (where Position > 1))]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K20_FilteredDecimalAverage
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
            new Column("Average", typeof(decimal?), 0),
            new Column("Rows", typeof(long), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Integral", typeof(int?), 1) });
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

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
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
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var groupsToFinalize = new List<ResultAggregateGroup>();
                ResultAggregateGroup group = new ResultAggregateGroup();
                groupsToFinalize.Add(group);
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
                                int position = ko3iko.Position;
                                bool __expr = (position > 1);
                                if (group == null)
                                {
                                    group = new ResultAggregateGroup();
                                    groupsToFinalize.Add(group);
                                }

                                if (__expr)
                                {
                                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                }

                                if (__expr)
                                {
                                    Musoq.Plugins.AverageKernel<int, decimal>.Set(ref group.__agg1, (int?)ko3iko.Integral);
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
                                int position = ko3iko.Position;
                                bool __expr = (position > 1);
                                if (group == null)
                                {
                                    group = new ResultAggregateGroup();
                                    groupsToFinalize.Add(group);
                                }

                                if (__expr)
                                {
                                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                }

                                if (__expr)
                                {
                                    Musoq.Plugins.AverageKernel<int, decimal>.Set(ref group.__agg1, (int?)ko3iko.Integral);
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
                        int position = ko3iko.Position;
                        bool __expr = (position > 1);
                        if (group == null)
                        {
                            group = new ResultAggregateGroup();
                            groupsToFinalize.Add(group);
                        }

                        if (__expr)
                        {
                            group.__agg0.Count = checked(group.__agg0.Count + 1L);
                        }

                        if (__expr)
                        {
                            Musoq.Plugins.AverageKernel<int, decimal>.Set(ref group.__agg1, (int?)ko3iko.Integral);
                        }
                    }
                }

                __musoqFinalShapeRows.EnsureCapacity(groupsToFinalize.Count);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    __musoqFinalShapeRows.Add(new ResultRow0(Musoq.Plugins.AverageKernel<int, decimal>.Get(in finalGroup.__agg1), finalGroup.__agg0.Count));
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
            public Musoq.Plugins.AverageKernel<int, decimal>.State __agg1;
            public ResultAggregateGroup()
            {
                Musoq.Plugins.CountAllAggregateKernel.Reset(ref this.__agg0);
                Musoq.Plugins.AverageKernel<int, decimal>.Reset(ref this.__agg1);
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.CountAllAggregateKernel.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.AverageKernel<int, decimal>.Merge(ref this.__agg1, in source.__agg1);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(decimal? __value0, long __value1)
            {
                Average = __value0;
                Rows = __value1;
            }

            public decimal? Average { get; private set; }
            public override int Count => 2;
            public long Rows { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Average = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Rows = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Average),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Rows),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Average" => true,
                "Rows" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Average, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Rows, ref visitor);
            }
        }
    }
}
