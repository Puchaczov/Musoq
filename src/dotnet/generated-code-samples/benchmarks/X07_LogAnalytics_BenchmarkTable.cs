// === Parsed Query ===
/*
text Entry { Level: until '|', Number: until '|', Label: rest }; with logs as (select p.Level as Level, r.Amount as Amount
from bench.rows() r
outer apply TryParse<Entry>(r.Text) p
where p.Number::int % 2 = 0 and p.Label rlike 'item') select Level, Sum(Amount) as Total, RowNumber() over (order by Level) as Position
from logs
group by Level
qualify Position = 1
order by Level
*/

// === Logical Plan ===
/*
Cte
  Definition [logs]
    MultiStatement
      Project [r.Amount as r.Amount, r.Text as r.Text, p.Level as p.Level, p.Number as p.Number, p.Label as p.Label]
        Apply [Outer]
          SchemaScan [#bench.rows() as r]
          InterpretSource [#Entry(r.Text) as p]
      Project [p.Level as Level, r.Amount as Amount]
        Filter [(((p.Number::Int32 % 2) = 0) AND p.Label RLIKE 'item')]
          CteRef [rp as rp]
  Query
    MultiStatement
      Project [logs.Level as logs.Level, AggRef(logs.Sum(logs.Amount)) as logs.Sum(logs.Amount)]
        Aggregate [keys: Level] [aggs: Sum(Amount)]
          CteRef [logs as logs]
      Sort [logs.Level]
        Project [logs.Level as Level, logs.Sum(logs.Amount) as Total, WindowRef(0) as Position]
          Qualify [(WindowRef(0) = 1)]
            Window [RowNumber(idx:0; order: logs.Level)]
              CteRef [logsScore as logsScore]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [logs]
    PhysicalMultiStatement
      PhysicalProject [r.Amount as r.Amount, p.Level as p.Level, p.Number as p.Number, p.Label as p.Label]
        PhysicalNestedLoopApply [Outer]
          PhysicalSchemaScan [#bench.rows() as r]
          PhysicalInterpretSource [#Entry(r.Text) as p]
      PhysicalProject [p.Level as Level, r.Amount as Amount]
        PhysicalFilter [(((p.Number::Int32 % 2) = 0) AND p.Label RLIKE 'item')]
          PhysicalCteRef [rp as rp]
  Query
    PhysicalMultiStatement
      PhysicalProject [logs.Level as logs.Level, AggRef(logs.Sum(logs.Amount)) as logs.Sum(logs.Amount)]
        PhysicalSingleKeyAggregate [key: Level (String)] [aggs: Sum(Amount)]
          PhysicalCteRef [logs as logs]
      PhysicalSort [logs.Level]
        PhysicalProject [logs.Level as Level, logs.Sum(logs.Amount) as Total, WindowRef(0) as Position]
          PhysicalQualify [(WindowRef(0) = 1)]
            PhysicalWindow [RowNumber(idx:0; order: logs.Level)]
              PhysicalMaterialize
                PhysicalCteRef [logsScore as logsScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Amount: decimal <- property Amount
      Text: string <- property Text
    SourceEntity [p: object]
      Level: string <- property Level
      Number: string <- property Number
      Label: string <- property Label
    Generated [Cte0Row0]
      Level: string <- field Level
      Amount: decimal <- field Amount
    TableRow [logs]
      Level: string <- field Level
      Amount: decimal <- field Amount
    AggregateGroup [Statement0AggregateGroup; keys: 1; typed aggs: 1]
    Generated [Statement0Row0]
      logs.Level: string <- field logs_Level
      logs.Sum(logs.Amount): decimal? <- field logs_Sum_logs_Amount_
    TableRow [logsScore]
      logs.Level: string <- field logs_Level
      logs.Sum(logs.Amount): decimal? <- field logs_Sum_logs_Amount_
    Generated [ResultRow0]
      Level: string <- field Level
      Total: decimal? <- field Total
      Position: long <- field Position

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    SourceScan [r: FixtureRow] -> cte0_rRows
    CreateTable [cte0: Cte0Row0]
    ChunkedForEach [r in cte0_rRows]
      InterpretSource [Entry.TryParse(r.Text) -> cte0_pRows]
      Let [pHasMatch: bool = FALSE]
      ScalarForEach [p in cte0_pRows]
        Assign [pHasMatch = TRUE]
        If [(((p.Number::Int32 % 2) = 0) AND STRING_MATCH(p.Label, pattern='item', needle='item', kind=Contains, comparison=Ordinal, strategy=direct-ordinal, anchors=None, literal-span=0:4, fallback=none))]
          AppendRow [cte0 <- Cte0Row0(Level: p.Level, Amount: r.Amount)]
      If [NOT pHasMatch]
        If [((NULL AND FALSE) = TRUE)]
          AppendRow [cte0 <- Cte0Row0(Level: NULL, Amount: r.Amount)]
    PhaseBoundary [End:cte1]
    PhaseBoundary [Where:cte0]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    PhaseBoundary [GroupBy]
    PhaseBoundary [Begin:cte1]
    CreateTable [statement0: Statement0Row0]
    CreateSingleKeyAggregateContext [statement0Groups: string -> Statement0AggregateGroup]
    ForEach [logs in _cteRowResults.Slot0]
      GetOrAddSingleKeyAggregateGroup [statement0Group = statement0Groups[logs.Level] by Level; typed: Statement0AggregateGroup]
      Let [amount: decimal = logs.Amount]
      TypedAggregateSet [Set(statement0Group.__agg0, amount)]
    EnsureCapacity [statement0 <- statement0GroupsToFinalize.Count]
    ForEach [statement0FinalGroup in statement0GroupsToFinalize]
      AppendRow [statement0 <- Statement0Row0(logs.Level: statement0FinalGroup.Level, logs.Sum(logs.Amount): logs.Sum(logs.Amount))]
    StoreTable [statement0 -> _cteRowResults.Slot1: List<Statement0Row0>]
    PhaseBoundary [End:cte1]
    Materialize [_cteRowResults.Slot1 -> resultWindowRows]
    ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows order by logsScore.logs.Level ASC]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, logsScore in resultWindowRows]
      If [(resultRowNumbers[windowIndex] = 1)]
        AppendShape [result <- ResultShape0(Level: logsScore.logs.Level, Total: logsScore.logs.Sum(logs.Amount), Position: resultRowNumbers[windowIndex])]
    SortOwnedBuffer [result by Level ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultShape0 from owned buffer result]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_X07_LogAnalytics_BenchmarkTable
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
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Amount", typeof(decimal), 0), new Column("Text", typeof(string), 1) });
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("Level", typeof(string), 0),
            new Column("Amount", typeof(decimal), 1)
        };
        private static readonly Column[] __columns_compiled_statement0_2 = new Column[]
        {
            new Column("logs.Level", typeof(string), 0),
            new Column("logs.Sum(logs.Amount)", typeof(decimal?), 1)
        };
        private static readonly Column[] __columns_compiled_result_3 = new Column[]
        {
            new Column("Level", typeof(string), 0),
            new Column("Total", typeof(decimal?), 1),
            new Column("Position", typeof(long), 2)
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
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            var token = queryContext.CancellationToken;
            var __musoqProgressContext = global::Musoq.Evaluator.Runtime.TolerantInterpretation.Bind(queryContext);
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
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var resultWindowRows = EvaluationHelper.MaterializeGeneratedRows<Statement0Row0>(_cteRowResults.Slot1);
                var resultRowNumbersOrderKeys = new WindowResultRowNumbersOrderKeysKey[resultWindowRows.Count];
                ExtractResultRowNumbersWindowKeys(resultWindowRows, resultRowNumbersOrderKeys);
                var resultRowNumbersPartitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultRowNumbersPartitions, resultRowNumbersOrderKeys, false);
                var resultRowNumbers = new long[resultWindowRows.Count];
                for (int resultRowNumbersPartitionSetIndex = 0; resultRowNumbersPartitionSetIndex < resultRowNumbersPartitions.PartitionCount; ++resultRowNumbersPartitionSetIndex)
                {
                    var resultRowNumbersPartitionStart = resultRowNumbersPartitions.GetStart(resultRowNumbersPartitionSetIndex);
                    var resultRowNumbersPartitionCount = resultRowNumbersPartitions.GetLength(resultRowNumbersPartitionSetIndex);
                    var resultRowNumbersPartitionIndices = resultRowNumbersPartitions.Indices;
                    var resultRowNumbersPartitionLimit = resultRowNumbersPartitionCount;
                    for (int resultRowNumbersPartitionIndex = 0; resultRowNumbersPartitionIndex < resultRowNumbersPartitionLimit; ++resultRowNumbersPartitionIndex)
                    {
                        var resultRowNumbersCurrentIndex = resultRowNumbersPartitionIndices[resultRowNumbersPartitionStart + resultRowNumbersPartitionIndex];
                        var resultRowNumbersContext = new Musoq.Plugins.WindowRankingContext(resultRowNumbersPartitionIndex, 0, 0, 0L, resultRowNumbersPartitionCount, token);
                        resultRowNumbers[resultRowNumbersCurrentIndex] = Musoq.Plugins.RowNumberKernel.Get(in resultRowNumbersContext);
                    }
                }

                var result = new List<ResultShape0>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 logsScore = resultWindowRows[windowIndex];
                    if (((long)resultRowNumbers[windowIndex] == 1))
                    {
                        result.Add(new ResultShape0(logsScore.logs_Level, logsScore.logs_Sum_logs_Amount_, (long)resultRowNumbers[windowIndex]) { __musoqSequence = result.Count });
                    }
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.Level, right.Level);
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
                                var _interpreter_Entry = new Musoq.Generated.Interpreters.Entry();
                                Musoq.Generated.Interpreters.Entry? cte0_pRows = null;
                                try
                                {
                                    cte0_pRows = global::Musoq.Evaluator.Runtime.TolerantInterpretation.TryParse(_interpreter_Entry, r.Text, __musoqProgressContext, "Entry");
                                }
                                catch (global::Musoq.Schema.Interpreters.ParseException)
                                {
                                    cte0_pRows = null;
                                }

                                bool pHasMatch = false;
                                if (cte0_pRows != null)
                                {
                                    token.ThrowIfCancellationRequested();
                                    var p = cte0_pRows;
                                    pHasMatch = true;
                                    if (((((bool?)((Operators.SqlCompare<int?, int>((int?)(global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt32(p.Number) % ((int?)2).GetValueOrDefault()), 0, (int? __sqlLeft, int __sqlRight) => (__sqlLeft == __sqlRight))))) switch
                                    {
                                        false => (bool?)false,
                                        true => (bool?)((p.Label) switch
                                        {
                                            string __musoqStringMatch1 => __musoqStringMatch1.Contains("item", StringComparison.Ordinal),
                                            _ => false
                                        }),
                                        _ => (bool?)((p.Label) switch
                                        {
                                            string __musoqStringMatch1 => __musoqStringMatch1.Contains("item", StringComparison.Ordinal),
                                            _ => false
                                        }) & (bool?)null
                                    })) == true)
                                    {
                                        cte0.Add(new Cte0Row0(p.Level, r.Amount));
                                    }
                                }

                                if ((!pHasMatch))
                                {
                                    if (((((bool?)((bool?)null)) switch
                                    {
                                        false => (bool?)false,
                                        true => (bool?)false,
                                        _ => (bool?)false & (bool?)null
                                    }) == true))
                                    {
                                        cte0.Add(new Cte0Row0(null, r.Amount));
                                    }
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
                                var _interpreter_Entry = new Musoq.Generated.Interpreters.Entry();
                                Musoq.Generated.Interpreters.Entry? cte0_pRows = null;
                                try
                                {
                                    cte0_pRows = global::Musoq.Evaluator.Runtime.TolerantInterpretation.TryParse(_interpreter_Entry, r.Text, __musoqProgressContext, "Entry");
                                }
                                catch (global::Musoq.Schema.Interpreters.ParseException)
                                {
                                    cte0_pRows = null;
                                }

                                bool pHasMatch = false;
                                if (cte0_pRows != null)
                                {
                                    token.ThrowIfCancellationRequested();
                                    var p = cte0_pRows;
                                    pHasMatch = true;
                                    if (((((bool?)((Operators.SqlCompare<int?, int>((int?)(global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt32(p.Number) % ((int?)2).GetValueOrDefault()), 0, (int? __sqlLeft, int __sqlRight) => (__sqlLeft == __sqlRight))))) switch
                                    {
                                        false => (bool?)false,
                                        true => (bool?)((p.Label) switch
                                        {
                                            string __musoqStringMatch2 => __musoqStringMatch2.Contains("item", StringComparison.Ordinal),
                                            _ => false
                                        }),
                                        _ => (bool?)((p.Label) switch
                                        {
                                            string __musoqStringMatch2 => __musoqStringMatch2.Contains("item", StringComparison.Ordinal),
                                            _ => false
                                        }) & (bool?)null
                                    })) == true)
                                    {
                                        cte0.Add(new Cte0Row0(p.Level, r.Amount));
                                    }
                                }

                                if ((!pHasMatch))
                                {
                                    if (((((bool?)((bool?)null)) switch
                                    {
                                        false => (bool?)false,
                                        true => (bool?)false,
                                        _ => (bool?)false & (bool?)null
                                    }) == true))
                                    {
                                        cte0.Add(new Cte0Row0(null, r.Amount));
                                    }
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
                        var _interpreter_Entry = new Musoq.Generated.Interpreters.Entry();
                        Musoq.Generated.Interpreters.Entry? cte0_pRows = null;
                        try
                        {
                            cte0_pRows = global::Musoq.Evaluator.Runtime.TolerantInterpretation.TryParse(_interpreter_Entry, r.Text, __musoqProgressContext, "Entry");
                        }
                        catch (global::Musoq.Schema.Interpreters.ParseException)
                        {
                            cte0_pRows = null;
                        }

                        bool pHasMatch = false;
                        if (cte0_pRows != null)
                        {
                            token.ThrowIfCancellationRequested();
                            var p = cte0_pRows;
                            pHasMatch = true;
                            if (((((bool?)((Operators.SqlCompare<int?, int>((int?)(global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt32(p.Number) % ((int?)2).GetValueOrDefault()), 0, (int? __sqlLeft, int __sqlRight) => (__sqlLeft == __sqlRight))))) switch
                            {
                                false => (bool?)false,
                                true => (bool?)((p.Label) switch
                                {
                                    string __musoqStringMatch3 => __musoqStringMatch3.Contains("item", StringComparison.Ordinal),
                                    _ => false
                                }),
                                _ => (bool?)((p.Label) switch
                                {
                                    string __musoqStringMatch3 => __musoqStringMatch3.Contains("item", StringComparison.Ordinal),
                                    _ => false
                                }) & (bool?)null
                            })) == true)
                            {
                                cte0.Add(new Cte0Row0(p.Level, r.Amount));
                            }
                        }

                        if ((!pHasMatch))
                        {
                            if (((((bool?)((bool?)null)) switch
                            {
                                false => (bool?)false,
                                true => (bool?)false,
                                _ => (bool?)false & (bool?)null
                            }) == true))
                            {
                                cte0.Add(new Cte0Row0(null, r.Amount));
                            }
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement0Row0> BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                var statement0 = new List<Statement0Row0>();
                var statement0GroupsToFinalize = new List<Statement0AggregateGroup>();
                var statement0Groups = new Dictionary<string, Statement0AggregateGroup>();
                Statement0AggregateGroup statement0NullGroup = null;
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 logs = __storedTable0Rows[__storedTable0Index];
                    string groupKey = logs.Level;
                    Statement0AggregateGroup statement0Group = null;
                    if (groupKey != null)
                    {
                        ref var statement0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0Groups, groupKey, out var statement0GroupExists);
                        if (!statement0GroupExists)
                        {
                            statement0GroupRef = new Statement0AggregateGroup(groupKey);
                            statement0GroupsToFinalize.Add(statement0GroupRef);
                        }

                        statement0Group = statement0GroupRef;
                    }
                    else
                    {
                        if (statement0NullGroup == null)
                        {
                            statement0NullGroup = new Statement0AggregateGroup(null);
                            statement0GroupsToFinalize.Add(statement0NullGroup);
                        }

                        statement0Group = statement0NullGroup;
                    }

                    decimal amount = logs.Amount;
                    {
                        var __agg0Input = (decimal?)amount;
                        if (__agg0Input.HasValue)
                        {
                            var __agg0Current = __agg0Input.GetValueOrDefault();
                            statement0Group.__agg0.Value = statement0Group.__agg0.HasValue ? checked(statement0Group.__agg0.Value + __agg0Current) : __agg0Current;
                            statement0Group.__agg0.HasValue = true;
                        }
                    }
                }

                statement0.EnsureCapacity(statement0GroupsToFinalize.Count);
                foreach (var statement0FinalGroup in statement0GroupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    statement0.Add(new Statement0Row0(statement0FinalGroup.__key0, (statement0FinalGroup.__agg0.HasValue ? (decimal?)statement0FinalGroup.__agg0.Value : null)));
                }

                return statement0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void ExtractResultRowNumbersWindowKeys(IReadOnlyList<Statement0Row0> resultWindowRows, WindowResultRowNumbersOrderKeysKey[] resultRowNumbersOrderKeys)
        {
            for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
            {
                Statement0Row0 logsScore = resultWindowRows[windowIndex];
                resultRowNumbersOrderKeys[windowIndex] = new WindowResultRowNumbersOrderKeysKey(logsScore.logs_Level);
            }
        }

        private sealed class Cte0Row0
        {
            public string Level { get; }
            public decimal Amount { get; }

            public Cte0Row0(string __value0, decimal __value1)
            {
                Level = __value0;
                Amount = __value1;
            }
        }

        private sealed class Statement0AggregateGroup
        {
            public readonly string __key0;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg0;
            public Statement0AggregateGroup(string __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg0);
            }

            public void MergeFrom(Statement0AggregateGroup source)
            {
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
            }
        }

        private sealed class Statement0Row0
        {
            public string logs_Level { get; }
            public decimal? logs_Sum_logs_Amount_ { get; }

            public Statement0Row0(string __value0, decimal? __value1)
            {
                logs_Level = __value0;
                logs_Sum_logs_Amount_ = __value1;
            }
        }

        private sealed class ResultShape0 : Row
        {
            public ResultShape0(string Level, decimal? Total, long Position)
            {
                this.Level = Level;
                this.Total = Total;
                this.Position = Position;
            }

            public string Level { get; private set; }
            public decimal? Total { get; private set; }
            public long Position { get; private set; }
            public override int Count => 3;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Level, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Position, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Level),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Position),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Level" => true,
                "Total" => true,
                "Position" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Level = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public int __musoqSequence { get; init; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
            public List<Statement0Row0> Slot1;
        }

        private readonly struct WindowResultRowNumbersOrderKeysKey : System.IEquatable<WindowResultRowNumbersOrderKeysKey>, System.IComparable<WindowResultRowNumbersOrderKeysKey>
        {
            private readonly string _value0;
            public WindowResultRowNumbersOrderKeysKey(string value0)
            {
                _value0 = value0;
            }

            public string RawValues => _value0;

            public int CompareTo(WindowResultRowNumbersOrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultRowNumbersOrderKeysKey other)
            {
                return System.String.Equals(_value0, other._value0, System.StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultRowNumbersOrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0, System.StringComparer.Ordinal);
                return hash.ToHashCode();
            }

            private static int CompareValue0(string left, string right)
            {
                if (left == null)
                    return right == null ? 0 : -1;
                if (right == null)
                    return 1;
                var comparison = System.String.CompareOrdinal(left, right);
                return comparison;
            }
        }
    }
}

// === SyntaxTree:  ===
// <auto-generated>
// This code was generated by Musoq Interpretation Schema code generator.
// Do not modify this file directly.
// </auto-generated>
#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Dynamic;
using Musoq.Schema.Interpreters;

namespace Musoq.Generated.Interpreters
{
    /// <summary>
    /// Generated interpreter for text schema 'Entry'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Entry : TextInterpreterBase<Entry>, ITextInterpreterStatus<Entry>, IParserCompletionLayoutSource, global::Musoq.Plugins.IJsonPayload
    {
        private static readonly global::Musoq.Schema.Interpreters.ParserCompletionLayout __musoqCompletionLayout = new(new string[] { "Level", "Number", "Label" });
        global::Musoq.Schema.Interpreters.ParserCompletionLayout global::Musoq.Schema.Interpreters.IParserCompletionLayoutSource.CompletionLayout => __musoqCompletionLayout;

        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Entry value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Level"))
            {
                writer.WritePropertyName("Level");
                var fieldValue = this.Level;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Number"))
            {
                writer.WritePropertyName("Number");
                var fieldValue = this.Number;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Label"))
            {
                writer.WritePropertyName("Label");
                var fieldValue = this.Label;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Level field value.</summary>
        public string? Level { get; set; }
        /// <summary>Gets the Number field value.</summary>
        public string? Number { get; set; }
        /// <summary>Gets the Label field value.</summary>
        public string? Label { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Entry";

        public override Entry ParseAt(ReadOnlySpan<char> data, int offset)
        {
            var input = new global::Musoq.Schema.Interpreters.TextParseInput(data);
            return ((global::Musoq.Schema.Interpreters.ITextInterpreterStatus<Entry>)this).ReadAt(ref input, offset).GetValueOrThrow();
        }

        global::Musoq.Schema.Interpreters.ParseAttempt<Entry> global::Musoq.Schema.Interpreters.ITextInterpreterStatus<Entry>.ReadAt(ref global::Musoq.Schema.Interpreters.TextParseInput __musoqTextInput, int offset)
        {
            var data = __musoqTextInput.Text;
            Entry? __parsedValue = null;
            try
            {
                ParsePosition = offset;
                SetCurrentField(null);
                __parsedValue = new Entry();
                RecordParsedValue(__parsedValue);
                SetCurrentField("Level");
                string _level;
                var __musoqRemainingText2 = (uint)ParsePosition <= (uint)data.Length ? data[ParsePosition..] : default;
                var __musoqDelimiterIndex3 = __musoqRemainingText2.IndexOf('\u007C');
                if (__musoqDelimiterIndex3 >= 0)
                {
                    _level = __musoqRemainingText2[..__musoqDelimiterIndex3].ToString();
                    ParsePosition += __musoqDelimiterIndex3 + 1;
                }
                else
                {
                    var __musoqReadResult1 = base.ReadUntilStatus(data, "|", fieldName: "Level");
                    if (!__musoqReadResult1.Succeeded)
                    {
                        return global::Musoq.Schema.Interpreters.ParseAttempt<Entry>.Failed(__parsedValue, __musoqReadResult1.Failure!);
                    }

                    var __musoqDelimitedFallback0 = __musoqReadResult1.Value!;
                    _level = __musoqDelimitedFallback0;
                }

                __parsedValue.Level = _level;
                base.RecordParsedFieldSlot(0);
                SetCurrentField("Number");
                string _number;
                var __musoqRemainingText6 = (uint)ParsePosition <= (uint)data.Length ? data[ParsePosition..] : default;
                var __musoqDelimiterIndex7 = __musoqRemainingText6.IndexOf('\u007C');
                if (__musoqDelimiterIndex7 >= 0)
                {
                    _number = __musoqRemainingText6[..__musoqDelimiterIndex7].ToString();
                    ParsePosition += __musoqDelimiterIndex7 + 1;
                }
                else
                {
                    var __musoqReadResult5 = base.ReadUntilStatus(data, "|", fieldName: "Number");
                    if (!__musoqReadResult5.Succeeded)
                    {
                        return global::Musoq.Schema.Interpreters.ParseAttempt<Entry>.Failed(__parsedValue, __musoqReadResult5.Failure!);
                    }

                    var __musoqDelimitedFallback4 = __musoqReadResult5.Value!;
                    _number = __musoqDelimitedFallback4;
                }

                __parsedValue.Number = _number;
                base.RecordParsedFieldSlot(1);
                SetCurrentField("Label");
                string _label;
                if ((uint)ParsePosition <= (uint)data.Length)
                {
                    _label = data[ParsePosition..].ToString();
                    ParsePosition = data.Length;
                }
                else
                {
                    var __musoqReadResult9 = base.ReadRestStatus(data, fieldName: "Label");
                    if (!__musoqReadResult9.Succeeded)
                    {
                        return global::Musoq.Schema.Interpreters.ParseAttempt<Entry>.Failed(__parsedValue, __musoqReadResult9.Failure!);
                    }

                    var __musoqDelimitedFallback8 = __musoqReadResult9.Value!;
                    _label = __musoqDelimitedFallback8;
                }

                __parsedValue.Label = _label;
                base.RecordParsedFieldSlot(2);
                return global::Musoq.Schema.Interpreters.ParseAttempt<Entry>.Success(__parsedValue);
            }
            catch (global::Musoq.Schema.Interpreters.ParseException __parseFailure)
            {
                return global::Musoq.Schema.Interpreters.ParseAttempt<Entry>.Failed(__parsedValue, global::Musoq.Schema.Interpreters.ParseFailure.FromException(__parseFailure));
            }
        }
    }
}
