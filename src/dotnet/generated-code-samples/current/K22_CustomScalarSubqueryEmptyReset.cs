// === Parsed Query ===
/*
select (select SeededTotal(Amount) from #schema.first() where Position < 0) as Total
from #schema.first() where Position = 1
*/

// === Logical Plan ===
/*
Cte
  Definition [_sq_1]
    MultiStatement
      Project [1 as 1, AggRef(ko3iko.SeededTotal(ko3iko.Amount)) as ko3iko.SeededTotal(ko3iko.Amount)]
        Aggregate [keys: 1] [aggs: SeededTotal(Amount)]
          Filter [(ko3iko.Position < 0)]
            SchemaScan [#schema.first() as ko3iko]
      Project [1 as _sq_1_key, ko3iko.SeededTotal(ko3iko.Amount) as _sq_1_value]
        CteRef [ko3ikoScore as ko3ikoScore]
  Query
    MultiStatement
      Project [vo04qt.Position as vo04qt.Position, _sq_1._sq_1_key as _sq_1._sq_1_key, _sq_1._sq_1_value as _sq_1._sq_1_value]
        Join [LeftOuter] [(1 = _sq_1._sq_1_key)]
          SchemaScan [#schema.first() as vo04qt]
          CteRef [_sq_1 as _sq_1]
      Project [_sq_1._sq_1_value as Total]
        Filter [(vo04qt.Position = 1)]
          CteRef [vo04qt_sq_1 as vo04qt_sq_1]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [_sq_1]
    PhysicalMultiStatement
      PhysicalProject [1 as 1, AggRef(ko3iko.SeededTotal(ko3iko.Amount)) as ko3iko.SeededTotal(ko3iko.Amount)]
        PhysicalSingleKeyAggregate [key: 1 (Int16)] [aggs: SeededTotal(Amount)]
          PhysicalFilter [(ko3iko.Position < 0)]
            PhysicalSchemaScan [#schema.first() as ko3iko]
      PhysicalProject [1 as _sq_1_key, ko3iko.SeededTotal(ko3iko.Amount) as _sq_1_value]
        PhysicalCteRef [ko3ikoScore as ko3ikoScore]
  Query
    PhysicalMultiStatement
      PhysicalProject [vo04qt.Position as vo04qt.Position, _sq_1._sq_1_key as _sq_1._sq_1_key, _sq_1._sq_1_value as _sq_1._sq_1_value]
        PhysicalNestedLoopJoin [LeftOuter] [(1 = _sq_1._sq_1_key)]
          PhysicalSchemaScan [#schema.first() as vo04qt] [pushdown: (vo04qt.Position = 1)]
          PhysicalCteRef [_sq_1 as _sq_1]
      PhysicalProject [_sq_1._sq_1_value as Total]
        PhysicalFilter [(vo04qt.Position = 1)]
          PhysicalCteRef [vo04qt_sq_1 as vo04qt_sq_1]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: LibraryExecutionEntity]
      Position: int <- property Position
      Amount: decimal? <- property Amount
    AggregateGroup [Cte0Statement0AggregateGroup; keys: 0; typed aggs: 1]
    Generated [Cte0Statement0Row0]
      ko3iko.SeededTotal(ko3iko.Amount): decimal <- field ko3iko_SeededTotal_ko3iko_Amount_
    TableRow [ko3ikoScore]
      ko3iko.SeededTotal(ko3iko.Amount): decimal <- field ko3iko_SeededTotal_ko3iko_Amount_
    Generated [Cte0Row0]
      _sq_1_key: int <- field _sq_1_key
      _sq_1_value: decimal <- field _sq_1_value
    SourceEntity [vo04qt: LibraryExecutionEntity]
      Position: int <- property Position
    TableRow [_sq_1]
      _sq_1_key: int <- field _sq_1_key
      _sq_1_value: decimal <- field _sq_1_value
    Generated [ResultRow0]
      Total: decimal? <- field Total

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte1]
    PhaseBoundary [Where]
    PhaseBoundary [From:cte0]
    SourceScan [ko3iko: LibraryExecutionEntity] -> cte0_statement0_ko3ikoRows
    CreateTable [cte0_statement0: Cte0Statement0Row0]
    CreateAggregateContext [cte0_statement0RootGroup, cte0_statement0Group, cte0_statement0GroupsToFinalize; typed: Cte0Statement0AggregateGroup]
    PhaseBoundary [Where:cte0]
    ChunkedForEach [ko3iko in cte0_statement0_ko3ikoRows]
      If [(ko3iko.Position < 0)]
        EnsureAggregateGroup [cte0_statement0Group; typed: Cte0Statement0AggregateGroup]
        Let [amount: decimal? = ko3iko.Amount]
        TypedAggregateSet [Set(cte0_statement0Group.__agg0, amount)]
    EnsureCapacity [cte0_statement0 <- cte0_statement0GroupsToFinalize.Count]
    PhaseBoundary [Select:cte0]
    ForEach [cte0_statement0FinalGroup in cte0_statement0GroupsToFinalize]
      AppendRow [cte0_statement0 <- Cte0Statement0Row0(ko3iko.SeededTotal(ko3iko.Amount): ko3iko.SeededTotal(ko3iko.Amount))]
    StoreTable [cte0_statement0 -> _cteRowResults.Slot1: List<Cte0Statement0Row0>]
    PhaseBoundary [End:cte1]
    PhaseBoundary [Begin:cte0]
    CreateTable [cte0: Cte0Row0]
    ForEach [ko3ikoScore in _cteRowResults.Slot1]
      AppendRow [cte0 <- Cte0Row0(_sq_1_key: 1, _sq_1_value: ko3ikoScore.ko3iko.SeededTotal(ko3iko.Amount))]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    SourceScan [vo04qt: LibraryExecutionEntity] -> vo04qtRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    ChunkedForEach [vo04qt in vo04qtRows]
      Let [resultHasMatch: bool = FALSE]
      ForEach [_sq_1 in _cteRowResults.Slot0]
        If [(1 = _sq_1._sq_1_key)]
          Assign [resultHasMatch = TRUE]
          If [(vo04qt.Position = 1)]
            AppendShape [result <- ResultRow0(Total: _sq_1._sq_1_value)]
      If [NOT resultHasMatch]
        If [(vo04qt.Position = 1)]
          AppendShape [result <- ResultRow0(Total: NULL)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
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
        private static readonly Column[] __columns_compiled_cte0_2 = new Column[]
        {
            new Column("_sq_1_key", typeof(int), 0),
            new Column("_sq_1_value", typeof(decimal), 1)
        };
        private static readonly Column[] __columns_compiled_cte0_statement0_1 = new Column[]
        {
            new Column("ko3iko.SeededTotal(ko3iko.Amount)", typeof(decimal), 0)
        };
        private static readonly Column[] __columns_compiled_result_4 = new Column[]
        {
            new Column("Total", typeof(decimal?), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Amount", typeof(decimal?), 1) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_vo04qt_3 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_4, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Where);
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
                try
                {
                    var __vo04qtSchema = provider.GetSchema("#schema");
                    var vo04qtRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__vo04qtSchema, "first", new SourceExecutionContext("vo04qt:2", sourceExecutionPlans["vo04qt:2"], token, __schemaColumns_compiled_vo04qt_3, sourceRuntimeSettingsBySourceContextId["vo04qt:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                    var vo04qtRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(vo04qtRowsSource.Chunks, __musoqProgressContext, "vo04qt:2") : vo04qtRowsSource.Chunks;
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
                                    bool resultHasMatch = false;
                                    var __storedTable0Rows = _cteRowResults.Slot0;
                                    for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                                    {
                                        if ((__storedTable0Index & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        Cte0Row0 _sq_1 = __storedTable0Rows[__storedTable0Index];
                                        if ((Operators.SqlCompare<int, int?>(1, _sq_1._sq_1_key, (int __sqlLeft, int? __sqlRight) => (__sqlLeft == __sqlRight))) == true)
                                        {
                                            resultHasMatch = true;
                                            if ((vo04qt.Position == 1))
                                            {
                                                __musoqFinalShapeRows.Add(new ResultRow0(_sq_1._sq_1_value));
                                            }
                                        }
                                    }

                                    if ((!resultHasMatch))
                                    {
                                        if ((vo04qt.Position == 1))
                                        {
                                            __musoqFinalShapeRows.Add(new ResultRow0(null));
                                        }
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
                                    bool resultHasMatch = false;
                                    var __storedTable0Rows1 = _cteRowResults.Slot0;
                                    for (int __storedTable0Rows1Index = 0; __storedTable0Rows1Index < __storedTable0Rows1.Count; ++__storedTable0Rows1Index)
                                    {
                                        if ((__storedTable0Rows1Index & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        Cte0Row0 _sq_1 = __storedTable0Rows1[__storedTable0Rows1Index];
                                        if ((Operators.SqlCompare<int, int?>(1, _sq_1._sq_1_key, (int __sqlLeft, int? __sqlRight) => (__sqlLeft == __sqlRight))) == true)
                                        {
                                            resultHasMatch = true;
                                            if ((vo04qt.Position == 1))
                                            {
                                                __musoqFinalShapeRows.Add(new ResultRow0(_sq_1._sq_1_value));
                                            }
                                        }
                                    }

                                    if ((!resultHasMatch))
                                    {
                                        if ((vo04qt.Position == 1))
                                        {
                                            __musoqFinalShapeRows.Add(new ResultRow0(null));
                                        }
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
                            bool resultHasMatch = false;
                            var __storedTable0Rows2 = _cteRowResults.Slot0;
                            for (int __storedTable0Rows2Index = 0; __storedTable0Rows2Index < __storedTable0Rows2.Count; ++__storedTable0Rows2Index)
                            {
                                if ((__storedTable0Rows2Index & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                Cte0Row0 _sq_1 = __storedTable0Rows2[__storedTable0Rows2Index];
                                if ((Operators.SqlCompare<int, int?>(1, _sq_1._sq_1_key, (int __sqlLeft, int? __sqlRight) => (__sqlLeft == __sqlRight))) == true)
                                {
                                    resultHasMatch = true;
                                    if ((vo04qt.Position == 1))
                                    {
                                        __musoqFinalShapeRows.Add(new ResultRow0(_sq_1._sq_1_value));
                                    }
                                }
                            }

                            if ((!resultHasMatch))
                            {
                                if ((vo04qt.Position == 1))
                                {
                                    __musoqFinalShapeRows.Add(new ResultRow0(null));
                                }
                            }
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte1", QueryPhase.End);
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                var cte0 = new List<Cte0Row0>(_cteRowResults.Slot1.Count);
                var __storedTable1Rows = _cteRowResults.Slot1;
                for (int __storedTable1Index = 0; __storedTable1Index < __storedTable1Rows.Count; ++__storedTable1Index)
                {
                    if ((__storedTable1Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Statement0Row0 ko3ikoScore = __storedTable1Rows[__storedTable1Index];
                    cte0.Add(new Cte0Row0(1, ko3ikoScore.ko3iko_SeededTotal_ko3iko_Amount_));
                }

                return cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Statement0Row0> BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                var __cte0_statement0_ko3ikoSchema = provider.GetSchema("#schema");
                var cte0_statement0_ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__cte0_statement0_ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:2", sourceExecutionPlans["ko3iko:2"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var cte0_statement0_ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(cte0_statement0_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:2") : cte0_statement0_ko3ikoRowsSource.Chunks;
                var cte0_statement0 = new List<Cte0Statement0Row0>();
                var cte0_statement0GroupsToFinalize = new List<Cte0Statement0AggregateGroup>();
                Cte0Statement0AggregateGroup cte0_statement0Group = new Cte0Statement0AggregateGroup();
                cte0_statement0GroupsToFinalize.Add(cte0_statement0Group);
                foreach (var ko3ikoChunk in cte0_statement0_ko3ikoRows)
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
                                if ((ko3iko.Position < 0))
                                {
                                    if (cte0_statement0Group == null)
                                    {
                                        cte0_statement0Group = new Cte0Statement0AggregateGroup();
                                        cte0_statement0GroupsToFinalize.Add(cte0_statement0Group);
                                    }

                                    decimal? amount = ko3iko.Amount;
                                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Set(ref cte0_statement0Group.__agg0, (decimal?)amount);
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
                                if ((ko3iko.Position < 0))
                                {
                                    if (cte0_statement0Group == null)
                                    {
                                        cte0_statement0Group = new Cte0Statement0AggregateGroup();
                                        cte0_statement0GroupsToFinalize.Add(cte0_statement0Group);
                                    }

                                    decimal? amount = ko3iko.Amount;
                                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Set(ref cte0_statement0Group.__agg0, (decimal?)amount);
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
                        if ((ko3iko.Position < 0))
                        {
                            if (cte0_statement0Group == null)
                            {
                                cte0_statement0Group = new Cte0Statement0AggregateGroup();
                                cte0_statement0GroupsToFinalize.Add(cte0_statement0Group);
                            }

                            decimal? amount = ko3iko.Amount;
                            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Set(ref cte0_statement0Group.__agg0, (decimal?)amount);
                        }
                    }
                }

                cte0_statement0.EnsureCapacity(cte0_statement0GroupsToFinalize.Count);
                foreach (var cte0_statement0FinalGroup in cte0_statement0GroupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    cte0_statement0.Add(new Cte0Statement0Row0(Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Get(in cte0_statement0FinalGroup.__agg0)));
                }

                return cte0_statement0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(int __value0, decimal __value1)
            {
                _sq_1_key = __value0;
                _sq_1_value = __value1;
            }

            public int _sq_1_key { get; }
            public decimal _sq_1_value { get; }
        }

        private sealed class Cte0Statement0AggregateGroup
        {
            public Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.State __agg0;
            public Cte0Statement0AggregateGroup()
            {
                Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Reset(ref this.__agg0);
            }
        }

        private sealed class Cte0Statement0Row0
        {
            public Cte0Statement0Row0(decimal __value0)
            {
                ko3iko_SeededTotal_ko3iko_Amount_ = __value0;
            }

            public decimal ko3iko_SeededTotal_ko3iko_Amount_ { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
            public List<Cte0Statement0Row0> Slot1;
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
    }
}
