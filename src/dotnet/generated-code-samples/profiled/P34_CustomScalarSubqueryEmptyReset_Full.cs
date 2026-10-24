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
namespace GeneratedSample_P34_CustomScalarSubqueryEmptyReset_Full
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
            foreach (var __musoqShapeRow in ProfiledOperatorEnumerable<ResultRow0>.Create(ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder), profileRecorder, profileRecorder?.GetCurrentOperatorScopeDepth() ?? 0))
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
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#schema");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:2", sourceExecutionPlans["ko3iko:2"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:2") : ko3ikoRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Where);
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
                                if ((ko3iko.Position == 1))
                                {
                                    yield return new ResultRow0(global::Musoq.Evaluator.Runtime.SubqueryExecution.Scalar<Subquery0ResultRow0, decimal?>(__ExecuteSubquery0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged), static row => (decimal?)row.SeededTotal_Amount_, token));
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
                                if ((ko3iko.Position == 1))
                                {
                                    yield return new ResultRow0(global::Musoq.Evaluator.Runtime.SubqueryExecution.Scalar<Subquery0ResultRow0, decimal?>(__ExecuteSubquery0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged), static row => (decimal?)row.SeededTotal_Amount_, token));
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
                        if ((ko3iko.Position == 1))
                        {
                            yield return new ResultRow0(global::Musoq.Evaluator.Runtime.SubqueryExecution.Scalar<Subquery0ResultRow0, decimal?>(__ExecuteSubquery0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged), static row => (decimal?)row.SeededTotal_Amount_, token));
                        }
                    }
                }
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
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __op8Handle = profileRecorder?.GetOperatorHandle("op8", "PhaseBoundary") ?? OperatorProfileHandle.None;
                var __op9Handle = profileRecorder?.GetOperatorHandle("op9", "PhaseBoundary") ?? OperatorProfileHandle.None;
                var __op10Handle = profileRecorder?.GetOperatorHandle("op10", "SourceScan") ?? OperatorProfileHandle.None;
                var __op12Handle = profileRecorder?.GetOperatorHandle("op12", "PhaseBoundary") ?? OperatorProfileHandle.None;
                var __op13Handle = profileRecorder?.GetOperatorHandle("op13", "PhaseBoundary") ?? OperatorProfileHandle.None;
                var __op14Handle = profileRecorder?.GetOperatorHandle("op14", "ChunkedForEach") ?? OperatorProfileHandle.None;
                var __op16Handle = profileRecorder?.GetOperatorHandle("op16", "AppendShape") ?? OperatorProfileHandle.None;
                long __op16OutputRows = 0L;
                var __op8Scope = profileRecorder?.BeginOperatorValue(__op8Handle) ?? OperatorProfileValueScope.None;
                OnPhaseChanged("compiled", QueryPhase.Begin);
                __op8Scope.Dispose();
                var __op9Scope = profileRecorder?.BeginOperatorValue(__op9Handle) ?? OperatorProfileValueScope.None;
                OnPhaseChanged("compiled", QueryPhase.From);
                __op9Scope.Dispose();
                var __op10Scope = profileRecorder?.BeginOperatorValue(__op10Handle) ?? OperatorProfileValueScope.None;
                var __ko3ikoSchema = provider.GetSchema("#schema");
                var ko3ikoRowsProfile = profileRecorder?.CreateSourceRecorder("ko3iko");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:2", sourceExecutionPlans["ko3iko:2"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:2"], logger, OnDataSourceProgress, ko3ikoRowsProfile == null ? SourceDiagnostics.None : ko3ikoRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = ko3ikoRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:2") : ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:2") : ko3ikoRowsSource.Chunks, ko3ikoRowsProfile);
                __op10Scope.Dispose();
                var __op12Scope = profileRecorder?.BeginOperatorValue(__op12Handle) ?? OperatorProfileValueScope.None;
                OnPhaseChanged("compiled", QueryPhase.Where);
                __op12Scope.Dispose();
                var __op13Scope = profileRecorder?.BeginOperatorValue(__op13Handle) ?? OperatorProfileValueScope.None;
                OnPhaseChanged("compiled", QueryPhase.Select);
                __op13Scope.Dispose();
                long __op14InputRows = 0L;
                long __op14OutputRows = 0L;
                var __op14Scope = profileRecorder?.BeginOperatorValue(__op14Handle) ?? OperatorProfileValueScope.None;
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
                                    __op14InputRows++;
                                    __op14OutputRows++;
                                    if ((ko3iko.Position == 1))
                                    {
                                        yield return new ResultRow0(global::Musoq.Evaluator.Runtime.SubqueryExecution.Scalar<Subquery0ResultRow0, decimal?>(__ExecuteSubquery0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, profileRecorder), static row => (decimal?)row.SeededTotal_Amount_, token));
                                        __op16OutputRows += 1;
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
                                    __op14InputRows++;
                                    __op14OutputRows++;
                                    if ((ko3iko.Position == 1))
                                    {
                                        yield return new ResultRow0(global::Musoq.Evaluator.Runtime.SubqueryExecution.Scalar<Subquery0ResultRow0, decimal?>(__ExecuteSubquery0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, profileRecorder), static row => (decimal?)row.SeededTotal_Amount_, token));
                                        __op16OutputRows += 1;
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
                            __op14InputRows++;
                            __op14OutputRows++;
                            if ((ko3iko.Position == 1))
                            {
                                yield return new ResultRow0(global::Musoq.Evaluator.Runtime.SubqueryExecution.Scalar<Subquery0ResultRow0, decimal?>(__ExecuteSubquery0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, profileRecorder), static row => (decimal?)row.SeededTotal_Amount_, token));
                                __op16OutputRows += 1;
                            }
                        }
                    }
                }
                finally
                {
                    __op14Scope.AddInputRows(__op14InputRows);
                    __op14Scope.AddOutputRows(__op14OutputRows);
                    __op14Scope.Dispose();
                }

                if (__op16OutputRows > 0L)
                    profileRecorder?.AddOperatorOutputRows(__op16Handle, __op16OutputRows);
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

        private static IEnumerable<Subquery0ResultRow0> __ExecuteSubquery0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, Musoq.Evaluator.Diagnostics.QueryProfileRecorder profileRecorder)
        {
            var __profileScopeDepth = profileRecorder?.GetCurrentOperatorScopeDepth() ?? 0;
            try
            {
                try
                {
                    var __musoqFinalShapeRows = new List<Subquery0ResultRow0>();
                    var __op10Handle = profileRecorder?.GetOperatorHandle("op10", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op11Handle = profileRecorder?.GetOperatorHandle("op11", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op12Handle = profileRecorder?.GetOperatorHandle("op12", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op14Handle = profileRecorder?.GetOperatorHandle("op14", "CreateAggregateContext") ?? OperatorProfileHandle.None;
                    var __op15Handle = profileRecorder?.GetOperatorHandle("op15", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op16Handle = profileRecorder?.GetOperatorHandle("op16", "ChunkedForEach") ?? OperatorProfileHandle.None;
                    var __op18Handle = profileRecorder?.GetOperatorHandle("op18", "EnsureAggregateGroup") ?? OperatorProfileHandle.None;
                    var __op20Handle = profileRecorder?.GetOperatorHandle("op20", "TypedAggregateSet") ?? OperatorProfileHandle.None;
                    var __op22Handle = profileRecorder?.GetOperatorHandle("op22", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op23Handle = profileRecorder?.GetOperatorHandle("op23", "ForEach") ?? OperatorProfileHandle.None;
                    var __op24Handle = profileRecorder?.GetOperatorHandle("op24", "AppendShape") ?? OperatorProfileHandle.None;
                    long __op20OutputRows = 0L;
                    long __op24OutputRows = 0L;
                    var __op10Scope = profileRecorder?.BeginOperatorValue(__op10Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("Subquery0", QueryPhase.Begin);
                    __op10Scope.Dispose();
                    var __op11Scope = profileRecorder?.BeginOperatorValue(__op11Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("Subquery0", QueryPhase.From);
                    __op11Scope.Dispose();
                    var __op12Scope = profileRecorder?.BeginOperatorValue(__op12Handle) ?? OperatorProfileValueScope.None;
                    var __vo04qtSchema = provider.GetSchema("#schema");
                    var vo04qtRowsProfile = profileRecorder?.CreateSourceRecorder("vo04qt");
                    var vo04qtRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__vo04qtSchema, "first", new SourceExecutionContext("vo04qt:2", sourceExecutionPlans["vo04qt:2"], token, __schemaColumns_Subquery0_vo04qt_0, sourceRuntimeSettingsBySourceContextId["vo04qt:2"], logger, OnDataSourceProgress, vo04qtRowsProfile == null ? SourceDiagnostics.None : vo04qtRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                    var vo04qtRows = vo04qtRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(vo04qtRowsSource.Chunks, __musoqProgressContext, "vo04qt:2") : vo04qtRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(vo04qtRowsSource.Chunks, __musoqProgressContext, "vo04qt:2") : vo04qtRowsSource.Chunks, vo04qtRowsProfile);
                    __op12Scope.Dispose();
                    var __op14Scope = profileRecorder?.BeginOperatorValue(__op14Handle) ?? OperatorProfileValueScope.None;
                    var groupsToFinalize = new List<Subquery0resultAggregateGroup>();
                    Subquery0resultAggregateGroup group = new Subquery0resultAggregateGroup();
                    groupsToFinalize.Add(group);
                    __op14Scope.Dispose();
                    var __op15Scope = profileRecorder?.BeginOperatorValue(__op15Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("Subquery0", QueryPhase.Where);
                    __op15Scope.Dispose();
                    long __op16InputRows = 0L;
                    long __op16OutputRows = 0L;
                    var __op16Scope = profileRecorder?.BeginOperatorValue(__op16Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
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
                                        __op16InputRows++;
                                        __op16OutputRows++;
                                        if ((vo04qt.Position < 0))
                                        {
                                            var __op18Scope = profileRecorder?.BeginOperatorValue(__op18Handle) ?? OperatorProfileValueScope.None;
                                            if (group == null)
                                            {
                                                group = new Subquery0resultAggregateGroup();
                                                groupsToFinalize.Add(group);
                                            }

                                            __op18Scope.Dispose();
                                            decimal? amount = vo04qt.Amount;
                                            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Set(ref group.__agg0, (decimal?)amount);
                                            __op20OutputRows += 1;
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
                                        __op16InputRows++;
                                        __op16OutputRows++;
                                        if ((vo04qt.Position < 0))
                                        {
                                            var __op18Scope = profileRecorder?.BeginOperatorValue(__op18Handle) ?? OperatorProfileValueScope.None;
                                            if (group == null)
                                            {
                                                group = new Subquery0resultAggregateGroup();
                                                groupsToFinalize.Add(group);
                                            }

                                            __op18Scope.Dispose();
                                            decimal? amount = vo04qt.Amount;
                                            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Set(ref group.__agg0, (decimal?)amount);
                                            __op20OutputRows += 1;
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
                                __op16InputRows++;
                                __op16OutputRows++;
                                if ((vo04qt.Position < 0))
                                {
                                    var __op18Scope = profileRecorder?.BeginOperatorValue(__op18Handle) ?? OperatorProfileValueScope.None;
                                    if (group == null)
                                    {
                                        group = new Subquery0resultAggregateGroup();
                                        groupsToFinalize.Add(group);
                                    }

                                    __op18Scope.Dispose();
                                    decimal? amount = vo04qt.Amount;
                                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Set(ref group.__agg0, (decimal?)amount);
                                    __op20OutputRows += 1;
                                }
                            }
                        }
                    }
                    finally
                    {
                        __op16Scope.AddInputRows(__op16InputRows);
                        __op16Scope.AddOutputRows(__op16OutputRows);
                        __op16Scope.Dispose();
                    }

                    __musoqFinalShapeRows.EnsureCapacity(groupsToFinalize.Count);
                    var __op22Scope = profileRecorder?.BeginOperatorValue(__op22Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("Subquery0", QueryPhase.Select);
                    __op22Scope.Dispose();
                    long __op23InputRows = 0L;
                    long __op23OutputRows = 0L;
                    var __op23Scope = profileRecorder?.BeginOperatorValue(__op23Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        foreach (var finalGroup in groupsToFinalize)
                        {
                            token.ThrowIfCancellationRequested();
                            __op23InputRows++;
                            __op23OutputRows++;
                            __musoqFinalShapeRows.Add(new Subquery0ResultRow0(Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Get(in finalGroup.__agg0)));
                            __op24OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op23Scope.AddInputRows(__op23InputRows);
                        __op23Scope.AddOutputRows(__op23OutputRows);
                        __op23Scope.Dispose();
                    }

                    if (__op20OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op20Handle, __op20OutputRows);
                    if (__op24OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op24Handle, __op24OutputRows);
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
            catch (Exception __profileException)when (profileRecorder != null && profileRecorder.RecordActiveOperatorException(__profileException, __profileScopeDepth))
            {
                profileRecorder.DisposeActiveOperatorScopes(__profileScopeDepth);
                throw;
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
