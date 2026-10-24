// === Parsed Query ===
/*
select (select SeededTotal(Amount) from #schema.first() where Position < 0) as Total
from #schema.first() where Position = 1
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ApplyScalar[Subquery0](; MultiStatement
  Project [1 as 1, AggRef(vo04qt.SeededTotal(vo04qt.Amount)) as vo04qt.SeededTotal(vo04qt.Amount)]
    Aggregate [keys: 1] [aggs: SeededTotal(Amount)]
      Filter [(vo04qt.Position < 0)]
        SchemaScan [#schema.first() as vo04qt]
  Project [vo04qt.SeededTotal(vo04qt.Amount) as SeededTotal(Amount)]
    CteRef [vo04qtScore as vo04qtScore]) as Total]
    Filter [(ko3iko.Position = 1)]
      SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ApplyScalar[Subquery0](; MultiStatement
  Project [1 as 1, AggRef(vo04qt.SeededTotal(vo04qt.Amount)) as vo04qt.SeededTotal(vo04qt.Amount)]
    Aggregate [keys: 1] [aggs: SeededTotal(Amount)]
      Filter [(vo04qt.Position < 0)]
        SchemaScan [#schema.first() as vo04qt]
  Project [vo04qt.SeededTotal(vo04qt.Amount) as SeededTotal(Amount)]
    CteRef [vo04qtScore as vo04qtScore]) as Total]
    PhysicalFilter [(ko3iko.Position = 1)]
      PhysicalSchemaScan [#schema.first() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: LibraryExecutionEntity]
      Position: int <- property Position
    Generated [ResultRow0]
      Total: decimal? <- field Total

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: LibraryExecutionEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      If [(ko3iko.Position = 1)]
        AppendShape [result <- ResultRow0(Total: ApplyScalar[Subquery0]())]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]

  Subquery
ExecutionPlan [Subquery0]
  Shapes
    SourceEntity [vo04qt: LibraryExecutionEntity]
      Position: int <- property Position
      Amount: decimal? <- property Amount
    AggregateGroup [Subquery0resultAggregateGroup; keys: 0; typed aggs: 1]
    Generated [Subquery0ResultRow0]
      SeededTotal(Amount): decimal <- field SeededTotal_Amount_

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [vo04qt: LibraryExecutionEntity] -> vo04qtRows
    CreateShapeRows [result: Subquery0ResultRow0 from Subquery0ResultRow0]
    CreateAggregateContext [rootGroup, group, groupsToFinalize; typed: Subquery0resultAggregateGroup]
    PhaseBoundary [Where]
    ChunkedForEach [vo04qt in vo04qtRows]
      If [(vo04qt.Position < 0)]
        EnsureAggregateGroup [group; typed: Subquery0resultAggregateGroup]
        Let [amount: decimal? = vo04qt.Amount]
        TypedAggregateSet [Set(group.__agg0, amount)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    PhaseBoundary [Select]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- Subquery0ResultRow0(SeededTotal(Amount): vo04qt.SeededTotal(vo04qt.Amount))]
    ReturnDeferredTable [result: Subquery0ResultRow0 <- Subquery0ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K22_CustomScalarSubqueryEmptyReset
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
        private static readonly Column[] __columns_Subquery0_result_1 = new Column[]
        {
            new Column("SeededTotal(Amount)", typeof(decimal), 0)
        };
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Total", typeof(decimal?), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_Subquery0_vo04qt_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Amount", typeof(decimal?), 1) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0) });
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
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
            ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
            this.OnPhaseChanged("compiled", QueryPhase.Begin);
            this.OnPhaseChanged("compiled", QueryPhase.From);
            var __ko3ikoSchema = provider.GetSchema("#schema");
            var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:2", sourceExecutionPlans["ko3iko:2"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:2") : ko3ikoRowsSource.Chunks;
            var __musoqTableSourceRows = ko3ikoRows;
            this.OnPhaseChanged("compiled", QueryPhase.Where);
            this.OnPhaseChanged("compiled", QueryPhase.Select);
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectRowsSerial<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity, ResultRow0>(__musoqTableSourceRows, (ko3iko) => (ko3iko.Position == 1), (ko3iko) => new ResultRow0(global::Musoq.Evaluator.Runtime.SubqueryExecution.Scalar<Subquery0ResultRow0, decimal?>(__ExecuteSubquery0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged), static row => (decimal?)row.SeededTotal_Amount_, token)), token), token, onCompleted: () =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }, onException: (Exception _) =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }, onDisposed: () =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            });
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

        private static IEnumerable<Subquery0ResultRow0> __ExecuteSubquery0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged)
        {
            try
            {
                var __musoqFinalShapeRows = new List<Subquery0ResultRow0>();
                OnPhaseChanged("Subquery0", QueryPhase.Begin);
                OnPhaseChanged("Subquery0", QueryPhase.From);
                var __vo04qtSchema = provider.GetSchema("#schema");
                var vo04qtRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__vo04qtSchema, "first", new SourceExecutionContext("vo04qt:2", sourceExecutionPlans["vo04qt:2"], token, __schemaColumns_Subquery0_vo04qt_0, sourceRuntimeSettingsBySourceContextId["vo04qt:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var vo04qtRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(vo04qtRowsSource.Chunks, __musoqProgressContext, "vo04qt:2") : vo04qtRowsSource.Chunks;
                var groupsToFinalize = new List<Subquery0resultAggregateGroup>();
                Subquery0resultAggregateGroup group = new Subquery0resultAggregateGroup();
                groupsToFinalize.Add(group);
                OnPhaseChanged("Subquery0", QueryPhase.Where);
                foreach (var vo04qtChunk in vo04qtRows)
                {
                    if (vo04qtChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> vo04qtChunkView)
                    {
                        if (vo04qtChunkView.Source is Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity[] vo04qtChunkViewArray)
                        {
                            int vo04qtChunkViewOffset = vo04qtChunkView.Offset;
                            for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunkView.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                            {
                                if ((vo04qtIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var vo04qt = vo04qtChunkViewArray[vo04qtChunkViewOffset + vo04qtIndex];
                                if ((vo04qt.Position < 0))
                                {
                                    if (group == null)
                                    {
                                        group = new Subquery0resultAggregateGroup();
                                        groupsToFinalize.Add(group);
                                    }

                                    decimal? amount = vo04qt.Amount;
                                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Set(ref group.__agg0, (decimal?)amount);
                                }
                            }

                            continue;
                        }

                        if (vo04qtChunkView.Source is List<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> vo04qtChunkViewList)
                        {
                            int vo04qtChunkViewOffset = vo04qtChunkView.Offset;
                            for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunkView.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                            {
                                if ((vo04qtIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var vo04qt = vo04qtChunkViewList[vo04qtChunkViewOffset + vo04qtIndex];
                                if ((vo04qt.Position < 0))
                                {
                                    if (group == null)
                                    {
                                        group = new Subquery0resultAggregateGroup();
                                        groupsToFinalize.Add(group);
                                    }

                                    decimal? amount = vo04qt.Amount;
                                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Set(ref group.__agg0, (decimal?)amount);
                                }
                            }

                            continue;
                        }
                    }

                    for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunk.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                    {
                        if ((vo04qtIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var vo04qt = vo04qtChunk[vo04qtIndex];
                        if ((vo04qt.Position < 0))
                        {
                            if (group == null)
                            {
                                group = new Subquery0resultAggregateGroup();
                                groupsToFinalize.Add(group);
                            }

                            decimal? amount = vo04qt.Amount;
                            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Set(ref group.__agg0, (decimal?)amount);
                        }
                    }
                }

                __musoqFinalShapeRows.EnsureCapacity(groupsToFinalize.Count);
                OnPhaseChanged("Subquery0", QueryPhase.Select);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    __musoqFinalShapeRows.Add(new Subquery0ResultRow0(Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Get(in finalGroup.__agg0)));
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
                    OnPhaseChanged("Subquery0", QueryPhase.End);
                }
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(decimal? __value0)
            {
                Total = __value0;
            }

            public override int Count => 1;
            public decimal? Total { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Total" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Total, ref visitor);
            }
        }

        private sealed class Subquery0ResultRow0 : Row
        {
            public Subquery0ResultRow0(decimal __value0)
            {
                SeededTotal_Amount_ = __value0;
            }

            public override int Count => 1;
            public decimal SeededTotal_Amount_ { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.SeededTotal_Amount_ = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.SeededTotal_Amount_),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "SeededTotal(Amount)" => true,
                "SeededTotal_Amount_" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.SeededTotal_Amount_, ref visitor);
            }
        }

        private sealed class Subquery0resultAggregateGroup
        {
            public Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.State __agg0;
            public Subquery0resultAggregateGroup()
            {
                Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Reset(ref this.__agg0);
            }
        }
    }
}
