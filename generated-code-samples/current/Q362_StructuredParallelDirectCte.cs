// === Parsed Query ===
/*
with patterns as (
    select p1.Id, p1.Pattern
    from values { (Id: 'todo', Pattern: 'TODO') } p1
), numbers as (
    select p2.Value
    from values { (Value: 1), (Value: 2) } p2
)
select m.PatternId, n.Value
from #inputs.match('TODO', patterns: patterns) m
cross join #inputs.numbers(values: numbers) n
*/

// === Logical Plan ===
/*
Cte
  Definition [patterns]
    MultiStatement
      Project [p1.Id as p1.Id, p1.Pattern as p1.Pattern]
        ValuesScan [1 rows as p1]
  Definition [numbers]
    MultiStatement
      Project [p2.Value as p2.Value]
        ValuesScan [2 rows as p2]
  Query
    MultiStatement
      Project [m.PatternId as m.PatternId, n.Value as n.Value]
        Join [Cross] [TRUE]
          SchemaScan [#inputs.match('TODO', CTE(patterns)) as m]
          SchemaScan [#inputs.numbers(CTE(numbers)) as n]
      Project [m.PatternId as m.PatternId, n.Value as n.Value]
        CteRef [mn as mn]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [patterns]
    PhysicalMultiStatement
      PhysicalProject [p1.Id as p1.Id, p1.Pattern as p1.Pattern]
        PhysicalValuesScan [1 rows as p1]
  Definition [numbers]
    PhysicalMultiStatement
      PhysicalProject [p2.Value as p2.Value]
        PhysicalValuesScan [2 rows as p2]
  Query
    PhysicalMultiStatement
      PhysicalProject [m.PatternId as m.PatternId, n.Value as n.Value]
        PhysicalNestedLoopJoin [Cross] [TRUE]
          PhysicalSchemaScan [#inputs.match('TODO', CTE(patterns)) as m]
          PhysicalSchemaScan [#inputs.numbers(CTE(numbers)) as n]
      PhysicalProject [m.PatternId as m.PatternId, n.Value as n.Value]
        PhysicalCteRef [mn as mn]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Id: string <- field Id
      Pattern: string <- field Pattern
    Generated [Cte0Row0]
      p1.Id: string <- field p1_Id
      p1.Pattern: string <- field p1_Pattern
    UnknownShape [ValuesRowShape]
      Value: int <- field Value
    Generated [Cte1Row0]
      p2.Value: int <- field p2_Value
    SourceEntity [m: PatternMatchRow]
      PatternId: string <- property PatternId
    SourceEntity [n: NumberRow]
      Value: int <- property Value
    Generated [ResultRow0]
      m.PatternId: string <- field m_PatternId
      n.Value: int <- field n_Value
  StoredTableRepresentations
    [0] GeneratedRowList; row=Cte0Row0; ownership=ConstructFresh; lifetime=Execution
    [1] GeneratedRowList; row=Cte1Row0; ownership=Copy; lifetime=Execution
  StructuralPreparation
    Musoq.Examples.DataSources.StructuredInputs.PatternInput; constructor=constructor:clr:Musoq.Examples.DataSources.StructuredInputs.PatternInput@Musoq.Examples.DataSources.StructuredInputs(primitive:string,primitive:string,primitive:string); shape=(Id: string?, Pattern: string?); origin=Cte; lifetime=Inline; metrics=TypedPrepass; ownership=ConstructFresh; limits=depth=32;nodes=100000;strings=67108864; defaults=-,-,ExecutionLiteral { ReturnType = string, Value = string:006C00690074006500720061006C }

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    ParallelBlock [cte-level-0, tasks 2, maxDegree 2]
      ParallelTask [patterns -> __parallelCteLevel0Task0Result]
        PhaseBoundary [Begin:cte0]
        PhaseBoundary [From:cte0]
        CreateValuesRows [cte0_p1Rows: p1Values23663CDFRow0 x 1]
        CreateTable [cte0: Cte0Row0]
        PhaseBoundary [Select:cte0]
        ForEach [p1 in cte0_p1Rows]
          AppendRow [cte0 <- Cte0Row0(p1.Id: p1.Id, p1.Pattern: p1.Pattern)]
        Assign [__parallelCteLevel0Task0Result = cte0]
        PhaseBoundary [End:cte0]
      ParallelTask [numbers -> __parallelCteLevel0Task1Result]
        PhaseBoundary [Begin:cte1]
        PhaseBoundary [From:cte1]
        CreateValuesRows [cte1_p2Rows: p2Values992C6A43Row0 x 2]
        CreateTable [cte1: Cte1Row0]
        PhaseBoundary [Select:cte1]
        ForEach [p2 in cte1_p2Rows]
          AppendRow [cte1 <- Cte1Row0(p2.Value: p2.Value)]
        Assign [__parallelCteLevel0Task1Result = cte1]
        PhaseBoundary [End:cte1]
      ParallelMerge
        StoreTable [__parallelCteLevel0Task0Result -> _cteRowResults.Slot0: List<Cte0Row0>]
        StoreTable [__parallelCteLevel0Task1Result -> _cteRowResults.Slot1: List<Cte1Row0>]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte2]
    SourceScan [m: PatternMatchRow] -> mRows
    SourceScan [n: NumberRow] -> nRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    MaterializeChunked [nRows -> nRowsBuffer]
    ChunkedForEach [m in mRows]
      Let [mPatternId: string = m.PatternId]
      ForEach [n in nRowsBuffer]
        AppendShape [result <- ResultShape0(m.PatternId: mPatternId, n.Value: n.Value)]
    PhaseBoundary [End:cte2]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q362_StructuredParallelDirectCte
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_cte0_0 = new Column[]
        {
            new Column("p1.Id", typeof(string), 0),
            new Column("p1.Pattern", typeof(string), 1)
        };
        private static readonly Column[] __columns_compiled_cte1_1 = new Column[]
        {
            new Column("p2.Value", typeof(int), 0)
        };
        private static readonly Column[] __columns_compiled_result_4 = new Column[]
        {
            new Column("m.PatternId", typeof(string), 0),
            new Column("n.Value", typeof(int), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_m_2 = Array.AsReadOnly(new ISchemaColumn[] { new Column("PatternId", typeof(string), 0) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_n_3 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Value", typeof(int), 0) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public IDictionary<string, System.Object> Parameters { get; } = new Dictionary<string, System.Object>(StringComparer.Ordinal);
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }

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
                yield return new ResultRow0(__musoqShapeRow.m_PatternId, __musoqShapeRow.n_Value);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                List<Cte0Row0> __parallelCteLevel0Task0Result = null;
                List<Cte1Row0> __parallelCteLevel0Task1Result = null;
                var cteLevel0Runner = new CteLevel0Runner(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                Parallel.Invoke(new ParallelOptions() { CancellationToken = token, MaxDegreeOfParallelism = 2 }, cteLevel0Runner.RunCteLevel0Task0, cteLevel0Runner.RunCteLevel0Task1);
                token.ThrowIfCancellationRequested();
                __parallelCteLevel0Task0Result = cteLevel0Runner.Task0Result;
                __parallelCteLevel0Task1Result = cteLevel0Runner.Task1Result;
                _cteRowResults.Slot0 = __parallelCteLevel0Task0Result;
                _cteRowResults.Slot1 = __parallelCteLevel0Task1Result;
                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte2", QueryPhase.Begin);
                try
                {
                    var __mSchema = provider.GetSchema("#inputs");
                    var mRowsSourceContext = new SourceExecutionContext("m:3", sourceExecutionPlans["m:3"], token, __schemaColumns_compiled_m_2, sourceRuntimeSettingsBySourceContextId["m:3"], logger, OnDataSourceProgress);
                    Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow> mRowsSource;
                    try
                    {
                        Musoq.Examples.DataSources.StructuredInputs.MatchSource mRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.MatchSource("TODO", __musoqPrepareCte_58d3d7af1e4c1fc9(_cteRowResults.Slot0, token), mRowsSourceContext);
                        mRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.MatchSource, Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow>(__mSchema, "match", mRowsSourceInstance, mRowsSourceContext, "#inputs", "m", "m:3");
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
                        throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "match", "m", "m:3", exception);
                    }

                    var mRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow>(mRowsSource.Chunks, __musoqProgressContext, "m:3") : mRowsSource.Chunks;
                    var __nSchema = provider.GetSchema("#inputs");
                    var nRowsSourceContext = new SourceExecutionContext("n:3", sourceExecutionPlans["n:3"], token, __schemaColumns_compiled_n_3, sourceRuntimeSettingsBySourceContextId["n:3"], logger, OnDataSourceProgress);
                    Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.NumberRow> nRowsSource;
                    try
                    {
                        Musoq.Examples.DataSources.StructuredInputs.NumbersSource nRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.NumbersSource(__musoqPrepareCte_0f62f7df03ef7c5f(_cteRowResults.Slot1, token), nRowsSourceContext);
                        nRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.NumbersSource, Musoq.Examples.DataSources.StructuredInputs.NumberRow>(__nSchema, "numbers", nRowsSourceInstance, nRowsSourceContext, "#inputs", "n", "n:3");
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
                        throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "numbers", "n", "n:3", exception);
                    }

                    var nRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.NumberRow>(nRowsSource.Chunks, __musoqProgressContext, "n:3") : nRowsSource.Chunks;
                    var nRowsBuffer = EvaluationHelper.MaterializeChunkedRows(nRows);
                    foreach (var mChunk in mRows)
                    {
                        if (mChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow> mChunkView)
                        {
                            if (mChunkView.Source is Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow[] mChunkViewArray)
                            {
                                int mChunkViewOffset = mChunkView.Offset;
                                for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                {
                                    if ((mIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var m = mChunkViewArray[mChunkViewOffset + mIndex];
                                    string mPatternId = m.PatternId;
                                    foreach (var n in nRowsBuffer)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        __musoqFinalShapeRows.Add(new ResultShape0(mPatternId, n.Value));
                                    }
                                }

                                continue;
                            }

                            if (mChunkView.Source is List<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow> mChunkViewList)
                            {
                                int mChunkViewOffset = mChunkView.Offset;
                                for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                                {
                                    if ((mIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var m = mChunkViewList[mChunkViewOffset + mIndex];
                                    string mPatternId = m.PatternId;
                                    foreach (var n in nRowsBuffer)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        __musoqFinalShapeRows.Add(new ResultShape0(mPatternId, n.Value));
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
                            string mPatternId = m.PatternId;
                            foreach (var n in nRowsBuffer)
                            {
                                token.ThrowIfCancellationRequested();
                                __musoqFinalShapeRows.Add(new ResultShape0(mPatternId, n.Value));
                            }
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte2", QueryPhase.End);
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
        private static List<Cte0Row0> BuildCteLevel0Task0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            List<Cte0Row0> __parallelCteLevel0Task0Result = null;
            token.ThrowIfCancellationRequested();
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            List<Cte0Row0> cte0 = null!;
            try
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.From);
                p1Values23663CDFRow0[] cte0_p1Rows = new p1Values23663CDFRow0[]
                {
                    new p1Values23663CDFRow0("todo", "TODO")
                };
                cte0 = new List<Cte0Row0>();
                OnPhaseChanged("compiled:cte0", QueryPhase.Select);
                foreach (var p1 in cte0_p1Rows)
                {
                    token.ThrowIfCancellationRequested();
                    cte0.Add(new Cte0Row0(p1.Id, p1.Pattern));
                }

                __parallelCteLevel0Task0Result = cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }

            return __parallelCteLevel0Task0Result;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte1Row0> BuildCteLevel0Task1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            List<Cte1Row0> __parallelCteLevel0Task1Result = null;
            token.ThrowIfCancellationRequested();
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            List<Cte1Row0> cte1 = null!;
            try
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.From);
                p2Values992C6A43Row0[] cte1_p2Rows = new p2Values992C6A43Row0[]
                {
                    new p2Values992C6A43Row0(1),
                    new p2Values992C6A43Row0(2)
                };
                cte1 = new List<Cte1Row0>();
                OnPhaseChanged("compiled:cte1", QueryPhase.Select);
                foreach (var p2 in cte1_p2Rows)
                {
                    token.ThrowIfCancellationRequested();
                    cte1.Add(new Cte1Row0(p2.Value));
                }

                __parallelCteLevel0Task1Result = cte1;
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }

            return __parallelCteLevel0Task1Result;
        }

        private static int[] __musoqPrepareCte_0f62f7df03ef7c5f(IReadOnlyList<Cte1Row0>? rows, CancellationToken token)
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
                    }
                }

                global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("cte", "n.argument[0]", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "n:3");
            }

            if (rows is null || rows.Count == 0)
                return System.Array.Empty<int>();
            token.ThrowIfCancellationRequested();
            var result = new int[rows.Count];
            for (var index = 0; index < rows.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                var row = rows[index];
                result[index] = row.p2_Value;
            }

            return result;
        }

        private static Musoq.Examples.DataSources.StructuredInputs.PatternInput[] __musoqPrepareCte_58d3d7af1e4c1fc9(IReadOnlyList<Cte0Row0>? rows, CancellationToken token)
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
                        if (__musoqStructural_cteRow1.p1_Id != null)
                            __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)__musoqStructural_cteRow1.p1_Id.Length * 2L));
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        if (__musoqStructural_cteRow1.p1_Pattern != null)
                            __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)__musoqStructural_cteRow1.p1_Pattern.Length * 2L));
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        if ("literal" != null)
                            __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)"literal".Length * 2L));
                    }
                }

                global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("cte", "m.argument[1]", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "m:3");
            }

            if (rows is null || rows.Count == 0)
                return System.Array.Empty<Musoq.Examples.DataSources.StructuredInputs.PatternInput>();
            token.ThrowIfCancellationRequested();
            var result = new Musoq.Examples.DataSources.StructuredInputs.PatternInput[rows.Count];
            for (var index = 0; index < rows.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                var row = rows[index];
                result[index] = new Musoq.Examples.DataSources.StructuredInputs.PatternInput(row.p1_Id, row.p1_Pattern, "literal");
            }

            return result;
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(string __value0, string __value1)
            {
                p1_Id = __value0;
                p1_Pattern = __value1;
            }

            public string p1_Id { get; }
            public string p1_Pattern { get; }
        }

        private sealed class Cte1Row0
        {
            public Cte1Row0(int __value0)
            {
                p2_Value = __value0;
            }

            public int p2_Value { get; }
        }

        private sealed class CteLevel0Runner
        {
            private readonly CteRowResults _cteRowResults;
            private readonly Microsoft.Extensions.Logging.ILogger _logger;
            private readonly QueryRunContext? _musoqProgressContext;
            private readonly Musoq.Schema.DataSourceEventHandler _onDataSourceProgress;
            private readonly Action<string, QueryPhase> _onPhaseChanged;
            private readonly Musoq.Evaluator.QueryProgressEventHandler _onQueryProgress;
            private readonly Musoq.Schema.ISchemaProvider _provider;
            private readonly IReadOnlyDictionary<string, SourceExecutionPlan> _sourceExecutionPlans;
            private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _sourceRuntimeSettingsBySourceContextId;
            private readonly CancellationToken _token;
            public CteLevel0Runner(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
            {
                _provider = provider;
                _sourceRuntimeSettingsBySourceContextId = sourceRuntimeSettingsBySourceContextId;
                _sourceExecutionPlans = sourceExecutionPlans;
                _logger = logger;
                _token = token;
                _musoqProgressContext = __musoqProgressContext;
                _onDataSourceProgress = OnDataSourceProgress;
                _onQueryProgress = OnQueryProgress;
                _onPhaseChanged = OnPhaseChanged;
                this._cteRowResults = _cteRowResults;
            }

            public List<Cte0Row0> Task0Result { get; private set; }
            public List<Cte1Row0> Task1Result { get; private set; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void RunCteLevel0Task0()
            {
                Task0Result = BuildCteLevel0Task0(_provider, _sourceRuntimeSettingsBySourceContextId, _sourceExecutionPlans, _logger, _token, _musoqProgressContext, _onDataSourceProgress, _onQueryProgress, _onPhaseChanged, _cteRowResults);
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void RunCteLevel0Task1()
            {
                Task1Result = BuildCteLevel0Task1(_provider, _sourceRuntimeSettingsBySourceContextId, _sourceExecutionPlans, _logger, _token, _musoqProgressContext, _onDataSourceProgress, _onQueryProgress, _onPhaseChanged, _cteRowResults);
            }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
            public List<Cte1Row0> Slot1;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, int __value1)
            {
                m_PatternId = __value0;
                n_Value = __value1;
            }

            public override int Count => 2;
            public string m_PatternId { get; private set; }
            public int n_Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        m_PatternId = (string)value;
                        break;
                    case 1:
                        n_Value = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "m.PatternId" => true,
                "m_PatternId" => true,
                "PatternId" => true,
                "n.Value" => true,
                "n_Value" => true,
                "Value" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)m_PatternId,
                1 => (object)n_Value,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "m.PatternId" => (object)m_PatternId,
                "m_PatternId" => (object)m_PatternId,
                "PatternId" => (object)m_PatternId,
                "n.Value" => (object)n_Value,
                "n_Value" => (object)n_Value,
                "Value" => (object)n_Value,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string m_PatternId, int n_Value)
            {
                this.m_PatternId = m_PatternId;
                this.n_Value = n_Value;
            }

            public string m_PatternId { get; }
            public int n_Value { get; }
        }

        private sealed class p1Values23663CDFRow0 : Row
        {
            public p1Values23663CDFRow0(string __value0, string __value1)
            {
                Id = __value0;
                Pattern = __value1;
            }

            public override int Count => 2;
            public string Id { get; private set; }
            public string Pattern { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Id = (string)value;
                        break;
                    case 1:
                        Pattern = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Pattern" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Id,
                1 => (object)Pattern,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Id" => (object)Id,
                "Pattern" => (object)Pattern,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class p2Values992C6A43Row0 : Row
        {
            public p2Values992C6A43Row0(int __value0)
            {
                Value = __value0;
            }

            public override int Count => 1;
            public int Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Value = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Value" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Value,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Value" => (object)Value,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
