// === Parsed Query ===
/*
table MetricsRow { Id: int, Amount: decimal }; couple bench.structured with table MetricsRow as Metrics; with keys as (select r.Key as Id, r.Key + 1d as Amount
from bench.rows() r
group by r.Key) select m.Id as Id, Sum(m.Amount) over (order by m.Id) as Total
from Metrics(items: keys) m
inner join bench.lookup() d on m.Id = d.Id
order by m.Id
*/

// === Logical Plan ===
/*
Cte
  Definition [keys]
    MultiStatement
      Project [r.Key as r.Key]
        Aggregate [keys: r.Key] [aggs: ]
          SchemaScan [#bench.rows() as r]
      Project [r.Key as Id, (r.Key + 1) as Amount]
        CteRef [rScore as rScore]
  Query
    MultiStatement
      Project [m.Id as m.Id, m.Amount as m.Amount, d.Id as d.Id]
        Join [Inner] [(m.Id = d.Id)]
          SchemaScan [#bench.structured(CTE(keys)) as m]
          SchemaScan [#bench.lookup() as d]
      Sort [m.Id]
        Project [m.Id as Id, WindowRef(0) as Total]
          Window [Sum(idx:0; order: m.Id; args: m.Amount)]
            CteRef [md as md]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [keys]
    PhysicalMultiStatement
      PhysicalProject [r.Key as r.Key]
        PhysicalSingleKeyAggregate [key: r.Key (Int32)] [aggs: ]
          PhysicalSchemaScan [#bench.rows() as r]
      PhysicalProject [r.Key as Id, (r.Key + 1) as Amount]
        PhysicalCteRef [rScore as rScore]
  Query
    PhysicalMultiStatement
      PhysicalProject [m.Id as m.Id, m.Amount as m.Amount]
        PhysicalHashJoin [Inner] [build: d.Id] [probe: m.Id]
          PhysicalSchemaScan [#bench.structured(CTE(keys)) as m]
          PhysicalSchemaScan [#bench.lookup() as d]
      PhysicalSort [m.Id]
        PhysicalProject [m.Id as Id, WindowRef(0) as Total]
          PhysicalWindow [Sum(idx:0; order: m.Id; args: m.Amount)]
            PhysicalMaterialize
              PhysicalCteRef [md as md]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Key: int <- property Key
    AggregateGroup [Cte0AggregateGroup; keys: 1; typed aggs: 0]
    Generated [Cte0Row0]
      Id: int <- field Id
      Amount: decimal <- field Amount
    SourceEntity [m: NumberInput]
      Id: int <- property Id
      Amount: decimal <- property Amount
    SourceEntity [d: FixtureRow]
      Id: int <- property Id
    Generated [Statement0Row0]
      m.Id: int <- field m_Id
      m.Amount: decimal <- field m_Amount
    TableRow [md]
      m.Id: int <- field m_Id
      m.Amount: decimal <- field m_Amount
    Generated [ResultRow0]
      Id: int <- field Id
      Total: decimal? <- field Total
  StoredTableRepresentations
    [0] GeneratedRowList; row=Cte0Row0; ownership=ConstructFresh; lifetime=Execution
    [1] GeneratedColumnarBuffer; row=Statement0Row0; ownership=ConstructFresh; lifetime=Execution
  StructuralPreparation
    Musoq.Benchmarks.Scenarios.NumberInput; constructor=constructor:clr:Musoq.Benchmarks.Scenarios.NumberInput@Musoq.Benchmarks.Scenarios(primitive:int32,primitive:decimal); shape=(Id: int32, Amount: decimal); origin=Cte; lifetime=Inline; metrics=TypedPrepass; ownership=ConstructFresh; limits=depth=32;nodes=100000;strings=67108864; defaults=-,-

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [From:cte0]
    SourceScan [r: FixtureRow] -> cte0_rRows
    CreateTable [cte0: Cte0Row0]
    PhaseBoundary [GroupBy:cte0]
    CreateSingleKeyAggregateContext [cte0Groups: int -> Cte0AggregateGroup]
    ChunkedForEach [r in cte0_rRows]
      GetOrAddSingleKeyAggregateGroup [cte0Group = cte0Groups[r.Key] by r.Key; typed: Cte0AggregateGroup]
    EnsureCapacity [cte0 <- cte0GroupsToFinalize.Count]
    PhaseBoundary [Select:cte0]
    ForEach [cte0FinalGroup in cte0GroupsToFinalize]
      AppendRow [cte0 <- Cte0Row0(Id: cte0FinalGroup.r.Key, Amount: (cte0FinalGroup.r.Key + 1))]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Begin:cte1]
    SourceScan [m: NumberInput] -> statement0_mRows
    SourceScan [d: FixtureRow] -> statement0_dRows
    CreateTable [statement0: Statement0Row0]
    CreateHash [statement0DHash: int -> FixtureRow]
    ChunkedForEach [d in statement0_dRows]
      HashAdd [statement0DHash[d.Id] += d]
    ChunkedForEach [m in statement0_mRows]
      HashProbe [statement0DHash[m.Id] -> statement0DHashMatches]
        ForEach [d in statement0DHashMatches]
          AppendRow [statement0 <- Statement0Row0(m.Id: m.Id, m.Amount: m.Amount)]
    StoreTable [statement0 -> _cteRowResults.Slot1: GeneratedColumnarBuffer<Statement0Row0>]
    PhaseBoundary [End:cte1]
    Materialize [_cteRowResults.Slot1 -> resultWindowRows]
    ComputeSumWindowKernel[BoundedRows] [resultSums <- resultWindowRows value md.m.Amount order by md.m.Id ASC frame range between unbounded preceding and current row]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, md in resultWindowRows]
      AppendShape [result <- ResultShape0(Id: md.m.Id, Total: resultSums[windowIndex])]
    SortOwnedBuffer [result by Id ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultShape0 from owned buffer result]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_X10_StructuredPipeline_BenchmarkTable
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
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Key", typeof(int), 0) });
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Amount", typeof(decimal), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_m_2 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Amount", typeof(decimal), 1) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_d_3 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0) });
        private static readonly Column[] __columns_compiled_statement0_4 = new Column[]
        {
            new Column("m.Id", typeof(int), 0),
            new Column("m.Amount", typeof(decimal), 1)
        };
        private static readonly Column[] __columns_compiled_result_5 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Total", typeof(decimal?), 1)
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
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_5, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_5, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
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
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(queryContext.TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var resultWindowRows = _cteRowResults.Slot1;
                var resultSumsOrderKeys = new WindowResultSumsOrderKeysKey[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Statement0Row0 md = resultWindowRows[windowIndex];
                    resultSumsOrderKeys[windowIndex] = new WindowResultSumsOrderKeysKey(md.m_Id);
                }

                var resultSumsPartitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultSumsPartitions, resultSumsOrderKeys, false);
                var resultSums = new decimal? [resultWindowRows.Count];
                for (int resultSumsPartitionSetIndex = 0; resultSumsPartitionSetIndex < resultSumsPartitions.PartitionCount; ++resultSumsPartitionSetIndex)
                {
                    var resultSumsPartitionStart = resultSumsPartitions.GetStart(resultSumsPartitionSetIndex);
                    var resultSumsPartitionCount = resultSumsPartitions.GetLength(resultSumsPartitionSetIndex);
                    var resultSumsPartitionIndices = resultSumsPartitions.Indices;
                    Musoq.Plugins.SumAggregateKernel<decimal>.State resultSumsState = default(Musoq.Plugins.SumAggregateKernel<decimal>.State)!;
                    Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref resultSumsState);
                    for (int resultSumsPartitionIndex = 0; resultSumsPartitionIndex < resultSumsPartitionCount;)
                    {
                        var resultSumsPeerEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultSumsOrderKeys, resultSumsPartitionIndices, resultSumsPartitionStart, resultSumsPartitionCount, resultSumsPartitionIndex);
                        for (int resultSumsPeerIndex = resultSumsPartitionIndex; resultSumsPeerIndex <= resultSumsPeerEnd; ++resultSumsPeerIndex)
                        {
                            var resultSumsCurrentIndex = resultSumsPartitionIndices[resultSumsPartitionStart + resultSumsPeerIndex];
                            Statement0Row0 md = resultWindowRows[resultSumsCurrentIndex];
                            Musoq.Plugins.SumAggregateKernel<decimal>.Set(ref resultSumsState, (decimal?)(md.m_Amount));
                        }

                        var resultSumsPeerValue = Musoq.Plugins.SumAggregateKernel<decimal>.Get(in resultSumsState);
                        for (int resultSumsPeerIndex = resultSumsPartitionIndex; resultSumsPeerIndex <= resultSumsPeerEnd; ++resultSumsPeerIndex)
                        {
                            var resultSumsCurrentIndex = resultSumsPartitionIndices[resultSumsPartitionStart + resultSumsPeerIndex];
                            resultSums[resultSumsCurrentIndex] = resultSumsPeerValue;
                        }

                        resultSumsPartitionIndex = resultSumsPeerEnd + 1;
                    }
                }

                var result = new List<ResultShape0>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 md = resultWindowRows[windowIndex];
                    result.Add(new ResultShape0(md.m_Id, (decimal?)resultSums[windowIndex]) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
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

        private IEnumerable<ResultShape0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
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

        private static Musoq.Benchmarks.Scenarios.NumberInput[] __musoqPrepareCte_4d8d649b642ec877(IReadOnlyList<Cte0Row0>? rows, CancellationToken token)
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

                global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("cte", "m.argument[0]", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 300001L, 67108864L, token, "m:2");
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
                var cte0GroupsToFinalize = new List<Cte0AggregateGroup>();
                var cte0Groups = new Dictionary<int, Cte0AggregateGroup>();
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
                                int groupKey = r.Key;
                                ref var cte0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Groups, groupKey, out var cte0GroupExists);
                                if (!cte0GroupExists)
                                {
                                    cte0GroupRef = new Cte0AggregateGroup(groupKey);
                                    cte0GroupsToFinalize.Add(cte0GroupRef);
                                }

                                Cte0AggregateGroup cte0Group = cte0GroupRef;
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
                                int groupKey = r.Key;
                                ref var cte0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Groups, groupKey, out var cte0GroupExists);
                                if (!cte0GroupExists)
                                {
                                    cte0GroupRef = new Cte0AggregateGroup(groupKey);
                                    cte0GroupsToFinalize.Add(cte0GroupRef);
                                }

                                Cte0AggregateGroup cte0Group = cte0GroupRef;
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
                        int groupKey = r.Key;
                        ref var cte0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Groups, groupKey, out var cte0GroupExists);
                        if (!cte0GroupExists)
                        {
                            cte0GroupRef = new Cte0AggregateGroup(groupKey);
                            cte0GroupsToFinalize.Add(cte0GroupRef);
                        }

                        Cte0AggregateGroup cte0Group = cte0GroupRef;
                    }
                }

                cte0.EnsureCapacity(cte0GroupsToFinalize.Count);
                foreach (var cte0FinalGroup in cte0GroupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    cte0.Add(new Cte0Row0(cte0FinalGroup.__key0, (cte0FinalGroup.__key0 + 1m)));
                }

                return cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static StoredTableColumnBuffer1 BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                var __statement0_mSchema = provider.GetSchema("#bench");
                var statement0_mRowsSourceContext = new SourceExecutionContext("m:2", sourceExecutionPlans["m:2"], token, __schemaColumns_compiled_m_2, sourceRuntimeSettingsBySourceContextId["m:2"], logger, OnDataSourceProgress)
                {
                    WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["m:2"].Identity)
                };
                Musoq.Schema.DataSources.RowSource<Musoq.Benchmarks.Scenarios.NumberInput> statement0_mRowsSource;
                try
                {
                    Musoq.Benchmarks.Scenarios.StructuredRows statement0_mRowsSourceInstance = new Musoq.Benchmarks.Scenarios.StructuredRows(__musoqPrepareCte_4d8d649b642ec877(_cteRowResults.Slot0, token), statement0_mRowsSourceContext);
                    statement0_mRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Benchmarks.Scenarios.StructuredRows, Musoq.Benchmarks.Scenarios.NumberInput>(__statement0_mSchema, "structured", statement0_mRowsSourceInstance, statement0_mRowsSourceContext, "#bench", "m", "m:2");
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
                    throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#bench", "structured", "m", "m:2", exception);
                }

                var statement0_mRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.NumberInput>(statement0_mRowsSource.Chunks, __musoqProgressContext, "m:2") : statement0_mRowsSource.Chunks;
                var __statement0_dSchema = provider.GetSchema("#bench");
                var statement0_dRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__statement0_dSchema, "lookup", new SourceExecutionContext("d:2", sourceExecutionPlans["d:2"], token, __schemaColumns_compiled_d_3, sourceRuntimeSettingsBySourceContextId["d:2"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["d:2"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var statement0_dRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(statement0_dRowsSource.Chunks, __musoqProgressContext, "d:2") : statement0_dRowsSource.Chunks;
                var statement0 = new StoredTableColumnBuffer1();
                var statement0DHash = new Dictionary<int, HashJoinBucket<Musoq.Benchmarks.Scenarios.FixtureRow>>();
                foreach (var dChunk in statement0_dRows)
                {
                    if (dChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.FixtureRow> dChunkView)
                    {
                        if (dChunkView.Source is Musoq.Benchmarks.Scenarios.FixtureRow[] dChunkViewArray)
                        {
                            int dChunkViewOffset = dChunkView.Offset;
                            for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                            {
                                if ((dIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var d = dChunkViewArray[dChunkViewOffset + dIndex];
                                int key = d.Id;
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0DHash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<Musoq.Benchmarks.Scenarios.FixtureRow>(d);
                                    }
                                    else
                                    {
                                        matches.Add(d);
                                    }
                                }
                            }

                            continue;
                        }

                        if (dChunkView.Source is List<Musoq.Benchmarks.Scenarios.FixtureRow> dChunkViewList)
                        {
                            int dChunkViewOffset = dChunkView.Offset;
                            for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                            {
                                if ((dIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var d = dChunkViewList[dChunkViewOffset + dIndex];
                                int key = d.Id;
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0DHash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<Musoq.Benchmarks.Scenarios.FixtureRow>(d);
                                    }
                                    else
                                    {
                                        matches.Add(d);
                                    }
                                }
                            }

                            continue;
                        }
                    }

                    for (int dIndex = 0, dIndexCount = dChunk.Count; dIndex < dIndexCount; ++dIndex)
                    {
                        if ((dIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var d = dChunk[dIndex];
                        int key = d.Id;
                        {
                            ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0DHash, key, out var matchesExists);
                            if (!matchesExists)
                            {
                                matches = new HashJoinBucket<Musoq.Benchmarks.Scenarios.FixtureRow>(d);
                            }
                            else
                            {
                                matches.Add(d);
                            }
                        }
                    }
                }

                foreach (var mChunk in statement0_mRows)
                {
                    if (mChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.NumberInput> mChunkView)
                    {
                        if (mChunkView.Source is Musoq.Benchmarks.Scenarios.NumberInput[] mChunkViewArray)
                        {
                            int mChunkViewOffset = mChunkView.Offset;
                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                            {
                                if ((mIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var m = mChunkViewArray[mChunkViewOffset + mIndex];
                                int key = m.Id;
                                if (statement0DHash.TryGetValue(key, out var statement0DHashMatches))
                                {
                                    foreach (var d in statement0DHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        statement0.Add(m.Id, m.Amount);
                                    }
                                }
                            }

                            continue;
                        }

                        if (mChunkView.Source is List<Musoq.Benchmarks.Scenarios.NumberInput> mChunkViewList)
                        {
                            int mChunkViewOffset = mChunkView.Offset;
                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                            {
                                if ((mIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var m = mChunkViewList[mChunkViewOffset + mIndex];
                                int key = m.Id;
                                if (statement0DHash.TryGetValue(key, out var statement0DHashMatches))
                                {
                                    foreach (var d in statement0DHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        statement0.Add(m.Id, m.Amount);
                                    }
                                }
                            }

                            continue;
                        }
                    }

                    for (int mIndex = 0, mIndexCount = mChunk.Count; mIndex < mIndexCount; ++mIndex)
                    {
                        if ((mIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var m = mChunk[mIndex];
                        int key = m.Id;
                        if (statement0DHash.TryGetValue(key, out var statement0DHashMatches))
                        {
                            foreach (var d in statement0DHashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                statement0.Add(m.Id, m.Amount);
                            }
                        }
                    }
                }

                return statement0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }
        }

        private sealed class Cte0AggregateGroup
        {
            public readonly int __key0;
            public Cte0AggregateGroup(int __key0)
            {
                this.__key0 = __key0;
            }

            public void MergeFrom(Cte0AggregateGroup source)
            {
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

        private readonly struct Statement0Row0
        {
            private readonly StoredTableColumnBuffer1 __buffer;
            private readonly int __index;
            public Statement0Row0(StoredTableColumnBuffer1 buffer, int index)
            {
                this.__buffer = buffer;
                this.__index = index;
            }

            public int m_Id => __buffer.Column0[__index];
            public decimal m_Amount => __buffer.Column1[__index];
        }

        private sealed class ResultShape0 : Row
        {
            public ResultShape0(int Id, decimal? Total)
            {
                this.Id = Id;
                this.Total = Total;
            }

            public int Id { get; private set; }
            public decimal? Total { get; private set; }
            public override int Count => 2;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Total" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public int __musoqSequence { get; init; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
            public StoredTableColumnBuffer1 Slot1;
        }

        private sealed class StoredTableColumn<T>
        {
            private const int ChunkSize = 1024;
            private T[][] _chunks = Array.Empty<T[]>();
            private int _chunkCount;
            private int _count;
            public StoredTableColumn(int capacity = 0)
            {
                EnsureCapacity(capacity);
            }

            public int Count => _count;

            public T this[int index] => _chunks[index >> 10][index & (ChunkSize - 1)];
            public void Add(T value)
            {
                var chunkIndex = _count >> 10;
                if (chunkIndex == _chunkCount)
                    AddChunk();
                _chunks[chunkIndex][_count & (ChunkSize - 1)] = value;
                _count++;
            }

            public void EnsureCapacity(int capacity)
            {
                if (capacity <= 0)
                    return;
                var requiredChunks = ((capacity - 1) / ChunkSize) + 1;
                while (_chunkCount < requiredChunks)
                    AddChunk();
            }

            private void AddChunk()
            {
                if (_chunkCount == _chunks.Length)
                    Array.Resize(ref _chunks, _chunkCount == 0 ? 4 : _chunkCount * 2);
                _chunks[_chunkCount++] = new T[ChunkSize];
            }
        }

        private sealed class StoredTableColumnBuffer1
        {
            public readonly StoredTableColumn<int> Column0;
            public readonly StoredTableColumn<decimal> Column1;
            public StoredTableColumnBuffer1(int capacity = 0)
            {
                Column0 = new StoredTableColumn<int>(capacity);
                Column1 = new StoredTableColumn<decimal>(capacity);
            }

            public int Count => Column0.Count;

            public Statement0Row0 this[int index] => new Statement0Row0(this, index);
            public void Add(int value0, decimal value1)
            {
                Column0.Add(value0);
                Column1.Add(value1);
            }

            public void EnsureCapacity(int capacity)
            {
                Column0.EnsureCapacity(capacity);
                Column1.EnsureCapacity(capacity);
            }
        }

        private readonly struct WindowResultSumsOrderKeysKey : System.IEquatable<WindowResultSumsOrderKeysKey>, System.IComparable<WindowResultSumsOrderKeysKey>
        {
            private readonly int _value0;
            public WindowResultSumsOrderKeysKey(int value0)
            {
                _value0 = value0;
            }

            public int RawValues => _value0;

            public int CompareTo(WindowResultSumsOrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultSumsOrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultSumsOrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0);
                return hash.ToHashCode();
            }

            private static int CompareValue0(int left, int right)
            {
                var comparison = left.CompareTo(right);
                return comparison;
            }
        }
    }
}
