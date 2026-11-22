// === Parsed Query ===
/*
with inputs as (select r.Id as Id, r.Amount as Amount
from bench.rows() r
where r.Id % 2 = 1) select s.Id as Id, s.Amount as Amount
from bench.structured(items: inputs) s
order by s.Id
*/

// === Logical Plan ===
/*
Cte
  Definition [inputs]
    MultiStatement
      Project [r.Id as Id, r.Amount as Amount]
        Filter [((r.Id % 2) = 1)]
          SchemaScan [#bench.rows() as r]
  Query
    MultiStatement
      Sort [s.Id]
        Project [s.Id as Id, s.Amount as Amount]
          SchemaScan [#bench.structured(CTE(inputs)) as s]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [inputs]
    PhysicalMultiStatement
      PhysicalProject [r.Id as Id, r.Amount as Amount]
        PhysicalFilter [((r.Id % 2) = 1)]
          PhysicalSchemaScan [#bench.rows() as r]
  Query
    PhysicalMultiStatement
      PhysicalSort [s.Id]
        PhysicalProject [s.Id as Id, s.Amount as Amount]
          PhysicalSchemaScan [#bench.structured(CTE(inputs)) as s]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Id: int <- property Id
      Amount: decimal <- property Amount
    Generated [Cte0Row0]
      Id: int <- field Id
      Amount: decimal <- field Amount
    SourceEntity [s: NumberInput]
      Id: int <- property Id
      Amount: decimal <- property Amount
    GeneratedRecord [ResultRow0WithSortKeys]
      Id: int <- field Id
      Amount: decimal <- field Amount
      __ordinal: int <- field __ordinal
    Generated [ResultRow0]
      Id: int <- field Id
      Amount: decimal <- field Amount
  StoredTableRepresentations
    [0] GeneratedRowList; row=Cte0Row0; ownership=ConstructFresh; lifetime=Execution
  StructuralPreparation
    Musoq.Benchmarks.Scenarios.NumberInput; constructor=constructor:clr:Musoq.Benchmarks.Scenarios.NumberInput@Musoq.Benchmarks.Scenarios(primitive:int32,primitive:decimal); shape=(Id: int32, Amount: decimal); origin=Cte; lifetime=Inline; metrics=TypedPrepass; ownership=ConstructFresh; limits=depth=32;nodes=100000;strings=67108864; defaults=-,-

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    PhaseBoundary [From]
    SourceScan [r: FixtureRow] -> cte0_rRows
    CreateTable [cte0: Cte0Row0]
    PhaseBoundary [Where:cte0]
    PhaseBoundary [Select:cte0]
    PhaseBoundary [Select]
    ChunkedForEach [r in cte0_rRows]
      Let [id: int = r.Id]
      If [((id % 2) = 1)]
        AppendRow [cte0 <- Cte0Row0(Id: id, Amount: r.Amount)]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    SourceScan [s: NumberInput] -> sRows
    CreateRecordList [resultOrderRecords: ResultRow0WithSortKeys]
    ChunkedForEach [s in sRows]
      AppendRecord [resultOrderRecords <- ResultRow0WithSortKeys(Id: s.Id, Amount: s.Amount)]
    OrderRecordList [resultOrderRecords: ResultRow0WithSortKeys by Id ASC]
    TransferOwnedRows [resultOrderRecords -> result: ResultRow0WithSortKeys]
    ReturnDeferredTable [result: ResultRow0WithSortKeys from owned buffer resultOrderRecords]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B23_StructuredInput_BenchmarkTable
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IContextTableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Amount", typeof(decimal), 1) });
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Amount", typeof(decimal), 1)
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
        public Table Run(CancellationToken token)
        {
            var queryContext = new QueryRunContext(token, TypedParameters, PhaseChanged, DataSourceProgress, this, queryId: "compiled", provider: Provider, sourceRuntimeSettingsBySourceContextId: SourceRuntimeSettingsBySourceContextId, sourceRuntimeSettingDescriptionsBySourceContextId: SourceRuntimeSettingDescriptionsBySourceContextId, sourceExecutionPlans: SourceExecutionPlans, logger: Logger, queryProgress: QueryProgress);
            return QueryRows.DeferredTable<ResultRow0WithSortKeys>("result", __columns_compiled_cte0_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultRow0WithSortKeys>("result", __columns_compiled_cte0_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        private IEnumerable<ResultRow0WithSortKeys> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            var token = queryContext.CancellationToken;
            var __musoqProgressContext = queryContext.QueryProgress == null ? null : queryContext;
            Action<string, QueryPhase> OnPhaseChanged = queryContext.NotifyPhaseChanged;
            DataSourceEventHandler OnDataSourceProgress = queryContext.NotifyDataSourceProgress;
            QueryProgressEventHandler OnQueryProgress = queryContext.QueryProgress;
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(queryContext.TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var __sSchema = provider.GetSchema("#bench");
                var sRowsSourceContext = new SourceExecutionContext("s:2", sourceExecutionPlans["s:2"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["s:2"], logger, OnDataSourceProgress)
                {
                    WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["s:2"].Identity)
                };
                Musoq.Schema.DataSources.RowSource<Musoq.Benchmarks.Scenarios.NumberInput> sRowsSource;
                try
                {
                    Musoq.Benchmarks.Scenarios.StructuredRows sRowsSourceInstance = new Musoq.Benchmarks.Scenarios.StructuredRows(__musoqPrepareCte_3352f56050c859fc(_cteRowResults.Slot0, token), sRowsSourceContext);
                    sRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Benchmarks.Scenarios.StructuredRows, Musoq.Benchmarks.Scenarios.NumberInput>(__sSchema, "structured", sRowsSourceInstance, sRowsSourceContext, "#bench", "s", "s:2");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (global::Musoq.Schema.Exceptions.DataSourceLifecycleException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#bench", "structured", "s", "s:2", exception);
                }

                var sRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.NumberInput>(sRowsSource.Chunks, __musoqProgressContext, "s:2") : sRowsSource.Chunks;
                var resultOrderRecords = new List<ResultRow0WithSortKeys>();
                foreach (var sChunk in sRows)
                {
                    if (sChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.NumberInput> sChunkView)
                    {
                        if (sChunkView.Source is Musoq.Benchmarks.Scenarios.NumberInput[] sChunkViewArray)
                        {
                            int sChunkViewOffset = sChunkView.Offset;
                            for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                            {
                                if ((sIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var s = sChunkViewArray[sChunkViewOffset + sIndex];
                                resultOrderRecords.Add(new ResultRow0WithSortKeys(s.Id, s.Amount, resultOrderRecords.Count));
                            }

                            continue;
                        }

                        if (sChunkView.Source is List<Musoq.Benchmarks.Scenarios.NumberInput> sChunkViewList)
                        {
                            int sChunkViewOffset = sChunkView.Offset;
                            for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                            {
                                if ((sIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var s = sChunkViewList[sChunkViewOffset + sIndex];
                                resultOrderRecords.Add(new ResultRow0WithSortKeys(s.Id, s.Amount, resultOrderRecords.Count));
                            }

                            continue;
                        }
                    }

                    for (int sIndex = 0, sIndexCount = sChunk.Count; sIndex < sIndexCount; ++sIndex)
                    {
                        if ((sIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var s = sChunk[sIndex];
                        resultOrderRecords.Add(new ResultRow0WithSortKeys(s.Id, s.Amount, resultOrderRecords.Count));
                    }
                }

                EvaluationHelper.SortRecordsIfUnordered(resultOrderRecords, ResultRow0WithSortKeysComparer.Instance);
                return resultOrderRecords;
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

        private IEnumerable<ResultRow0WithSortKeys> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, queryContext))
            {
                yield return __musoqShapeRow;
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

        private static Musoq.Benchmarks.Scenarios.NumberInput[] __musoqPrepareCte_3352f56050c859fc(IReadOnlyList<Cte0Row0>? rows, CancellationToken token)
        {
            {
                token.ThrowIfCancellationRequested();
                long __musoqStructuralNodes = 0L;
                long __musoqStructuralStrings = 0L;
                int __musoqStructuralMaxDepth = 0;
                __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                if (1 > __musoqStructuralMaxDepth)
                    __musoqStructuralMaxDepth = 1;
                if (rows is not null)
                {
                    for (var __musoqStructural_cteIndex0 = 0; __musoqStructural_cteIndex0 < rows.Count; __musoqStructural_cteIndex0++)
                    {
                        if ((__musoqStructural_cteIndex0 & 1023) == 0)
                            token.ThrowIfCancellationRequested();
                        var __musoqStructural_cteRow1 = rows[__musoqStructural_cteIndex0];
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (2 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 2;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                    }
                }

                global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("cte", "s.argument[0]", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 300001L, 67108864L, token, "s:2");
            }

            if (rows is null || rows.Count == 0)
                return System.Array.Empty<Musoq.Benchmarks.Scenarios.NumberInput>();
            token.ThrowIfCancellationRequested();
            var result = new Musoq.Benchmarks.Scenarios.NumberInput[rows.Count];
            for (var index = 0; index < rows.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                var row = rows[index];
                result[index] = new Musoq.Benchmarks.Scenarios.NumberInput(row.Id, row.Amount);
            }

            return result;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                var __cte0_rSchema = provider.GetSchema("#bench");
                var cte0_rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__cte0_rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var cte0_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(cte0_rRowsSource.Chunks, __musoqProgressContext, "r:1") : cte0_rRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                foreach (var rChunk in cte0_rRows)
                {
                    if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.FixtureRow> rChunkView)
                    {
                        if (rChunkView.Source is Musoq.Benchmarks.Scenarios.FixtureRow[] rChunkViewArray)
                        {
                            int rChunkViewOffset = rChunkView.Offset;
                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                            {
                                if ((rIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var r = rChunkViewArray[rChunkViewOffset + rIndex];
                                int id = r.Id;
                                if (((id % ((int?)2).GetValueOrDefault()) == 1))
                                {
                                    cte0.Add(new Cte0Row0(id, r.Amount));
                                }
                            }

                            continue;
                        }

                        if (rChunkView.Source is List<Musoq.Benchmarks.Scenarios.FixtureRow> rChunkViewList)
                        {
                            int rChunkViewOffset = rChunkView.Offset;
                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                            {
                                if ((rIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var r = rChunkViewList[rChunkViewOffset + rIndex];
                                int id = r.Id;
                                if (((id % ((int?)2).GetValueOrDefault()) == 1))
                                {
                                    cte0.Add(new Cte0Row0(id, r.Amount));
                                }
                            }

                            continue;
                        }
                    }

                    for (int rIndex = 0, rIndexCount = rChunk.Count; rIndex < rIndexCount; ++rIndex)
                    {
                        if ((rIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var r = rChunk[rIndex];
                        int id = r.Id;
                        if (((id % ((int?)2).GetValueOrDefault()) == 1))
                        {
                            cte0.Add(new Cte0Row0(id, r.Amount));
                        }
                    }
                }

                return cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        private sealed class Cte0Row0
        {
            public int Id { get; }
            public decimal Amount { get; }

            public Cte0Row0(int __value0, decimal __value1)
            {
                Id = __value0;
                Amount = __value1;
            }
        }

        private sealed class ResultRow0WithSortKeys : Row
        {
            public ResultRow0WithSortKeys(int Id, decimal Amount, int __ordinal)
            {
                this.Id = Id;
                this.Amount = Amount;
                this.__ordinal = __ordinal;
            }

            public int Id { get; private set; }
            public decimal Amount { get; private set; }
            public int __ordinal { get; private set; }
            public override int Count => 2;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Amount, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Amount),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Amount" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Amount = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }
        }

        private sealed class ResultRow0 : Row
        {
            public int Id { get; private set; }
            public decimal Amount { get; private set; }

            public ResultRow0(int __value0, decimal __value1)
            {
                Id = __value0;
                Amount = __value1;
            }

            public override int Count => 2;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Amount, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Amount),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Amount" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Amount = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0WithSortKeysComparer : IComparer<ResultRow0WithSortKeys>
        {
            public static readonly ResultRow0WithSortKeysComparer Instance = new ResultRow0WithSortKeysComparer();
            public int Compare(ResultRow0WithSortKeys left, ResultRow0WithSortKeys right)
            {
                var comparison = left.Id.CompareTo(right.Id);
                if (comparison != 0)
                    return comparison;
                return left.__ordinal.CompareTo(right.__ordinal);
            }
        }
    }
}
