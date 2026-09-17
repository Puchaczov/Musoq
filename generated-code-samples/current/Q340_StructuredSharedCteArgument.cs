// === Parsed Query ===
/*
with patterns as (
    select p.Id, p.Pattern
    from values {
        (Id: 'todo', Pattern: 'TODO'),
    } p
)
select leftMatch.PatternId, rightMatch.PatternId
from #inputs.match('TODO', patterns: patterns) leftMatch
cross join #inputs.match('TODO', patterns: patterns) rightMatch
*/

// === Logical Plan ===
/*
Cte
  Definition [patterns]
    MultiStatement
      Project [p.Id as p.Id, p.Pattern as p.Pattern]
        ValuesScan [1 rows as p]
  Query
    MultiStatement
      Project [leftMatch.PatternId as leftMatch.PatternId, rightMatch.PatternId as rightMatch.PatternId]
        Join [Cross] [TRUE]
          SchemaScan [#inputs.match('TODO', CTE(patterns)) as leftMatch]
          SchemaScan [#inputs.match('TODO', CTE(patterns)) as rightMatch]
      Project [leftMatch.PatternId as leftMatch.PatternId, rightMatch.PatternId as rightMatch.PatternId]
        CteRef [leftMatchrightMatch as leftMatchrightMatch]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [patterns]
    PhysicalMultiStatement
      PhysicalProject [p.Id as p.Id, p.Pattern as p.Pattern]
        PhysicalValuesScan [1 rows as p]
  Query
    PhysicalMultiStatement
      PhysicalProject [leftMatch.PatternId as leftMatch.PatternId, rightMatch.PatternId as rightMatch.PatternId]
        PhysicalNestedLoopJoin [Cross] [TRUE]
          PhysicalSchemaScan [#inputs.match('TODO', CTE(patterns)) as leftMatch]
          PhysicalSchemaScan [#inputs.match('TODO', CTE(patterns)) as rightMatch]
      PhysicalProject [leftMatch.PatternId as leftMatch.PatternId, rightMatch.PatternId as rightMatch.PatternId]
        PhysicalCteRef [leftMatchrightMatch as leftMatchrightMatch]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Id: string <- field Id
      Pattern: string <- field Pattern
    Generated [Cte0Row0]
      p.Id: string <- field p_Id
      p.Pattern: string <- field p_Pattern
    SourceEntity [leftMatch: PatternMatchRow]
      PatternId: string <- property PatternId
    SourceEntity [rightMatch: PatternMatchRow]
      PatternId: string <- property PatternId
    Generated [ResultRow0]
      leftMatch.PatternId: string <- field leftMatch_PatternId
      rightMatch.PatternId: string <- field rightMatch_PatternId
  StoredTableRepresentations
    [0] GeneratedRowList; row=Cte0Row0; ownership=ConstructFresh; lifetime=Execution
  StructuralPreparation
    Musoq.Examples.DataSources.StructuredInputs.PatternInput; constructor=constructor:clr:Musoq.Examples.DataSources.StructuredInputs.PatternInput@Musoq.Examples.DataSources.StructuredInputs(primitive:string,primitive:string,primitive:string); shape=(Id: string?, Pattern: string?); origin=Cte; lifetime=Inline; metrics=TypedPrepass; ownership=ConstructFresh; limits=depth=32;nodes=100000;strings=67108864; defaults=-,-,ExecutionLiteral { ReturnType = string, Value = string:006C00690074006500720061006C }
    Musoq.Examples.DataSources.StructuredInputs.PatternInput; constructor=constructor:clr:Musoq.Examples.DataSources.StructuredInputs.PatternInput@Musoq.Examples.DataSources.StructuredInputs(primitive:string,primitive:string,primitive:string); shape=(Id: string?, Pattern: string?); origin=Cte; lifetime=Inline; metrics=TypedPrepass; ownership=ConstructFresh; limits=depth=32;nodes=100000;strings=67108864; defaults=-,-,ExecutionLiteral { ReturnType = string, Value = string:006C00690074006500720061006C }

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    CreateValuesRows [cte0_pRows: pValuesCFFA3020Row0 x 1]
    CreateTable [cte0: Cte0Row0]
    PhaseBoundary [Select:cte0]
    ForEach [p in cte0_pRows]
      AppendRow [cte0 <- Cte0Row0(p.Id: p.Id, p.Pattern: p.Pattern)]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    SourceScan [leftMatch: PatternMatchRow] -> leftMatchRows
    SourceScan [rightMatch: PatternMatchRow] -> rightMatchRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    MaterializeChunked [rightMatchRows -> rightMatchRowsBuffer]
    ChunkedForEach [leftMatch in leftMatchRows]
      Let [leftMatchPatternId: string = leftMatch.PatternId]
      ForEach [rightMatch in rightMatchRowsBuffer]
        AppendShape [result <- ResultShape0(leftMatch.PatternId: leftMatchPatternId, rightMatch.PatternId: rightMatch.PatternId)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q340_StructuredSharedCteArgument
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
            new Column("p.Id", typeof(string), 0),
            new Column("p.Pattern", typeof(string), 1)
        };
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("leftMatch.PatternId", typeof(string), 0),
            new Column("rightMatch.PatternId", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_leftMatch_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("PatternId", typeof(string), 0) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.leftMatch_PatternId, __musoqShapeRow.rightMatch_PatternId);
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
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
                try
                {
                    var __leftMatchSchema = provider.GetSchema("#inputs");
                    var leftMatchRowsSourceContext = new SourceExecutionContext("leftMatch:2", sourceExecutionPlans["leftMatch:2"], token, __schemaColumns_compiled_leftMatch_1, sourceRuntimeSettingsBySourceContextId["leftMatch:2"], logger, OnDataSourceProgress);
                    Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow> leftMatchRowsSource;
                    try
                    {
                        Musoq.Examples.DataSources.StructuredInputs.MatchSource leftMatchRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.MatchSource("TODO", __musoqPrepareCte_3c3c288101ed6850(_cteRowResults.Slot0, token), leftMatchRowsSourceContext);
                        leftMatchRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.MatchSource, Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow>(__leftMatchSchema, "match", leftMatchRowsSourceInstance, leftMatchRowsSourceContext, "#inputs", "leftMatch", "leftMatch:2");
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
                        throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "match", "leftMatch", "leftMatch:2", exception);
                    }

                    var leftMatchRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow>(leftMatchRowsSource.Chunks, __musoqProgressContext, "leftMatch:2") : leftMatchRowsSource.Chunks;
                    var __rightMatchSchema = provider.GetSchema("#inputs");
                    var rightMatchRowsSourceContext = new SourceExecutionContext("rightMatch:2", sourceExecutionPlans["rightMatch:2"], token, __schemaColumns_compiled_leftMatch_1, sourceRuntimeSettingsBySourceContextId["rightMatch:2"], logger, OnDataSourceProgress);
                    Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow> rightMatchRowsSource;
                    try
                    {
                        Musoq.Examples.DataSources.StructuredInputs.MatchSource rightMatchRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.MatchSource("TODO", __musoqPrepareCte_89ba721be29109ac(_cteRowResults.Slot0, token), rightMatchRowsSourceContext);
                        rightMatchRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.MatchSource, Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow>(__rightMatchSchema, "match", rightMatchRowsSourceInstance, rightMatchRowsSourceContext, "#inputs", "rightMatch", "rightMatch:2");
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
                        throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "match", "rightMatch", "rightMatch:2", exception);
                    }

                    var rightMatchRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow>(rightMatchRowsSource.Chunks, __musoqProgressContext, "rightMatch:2") : rightMatchRowsSource.Chunks;
                    var rightMatchRowsBuffer = EvaluationHelper.MaterializeChunkedRows(rightMatchRows);
                    foreach (var leftMatchChunk in leftMatchRows)
                    {
                        if (leftMatchChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow> leftMatchChunkView)
                        {
                            if (leftMatchChunkView.Source is Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow[] leftMatchChunkViewArray)
                            {
                                int leftMatchChunkViewOffset = leftMatchChunkView.Offset;
                                for (int leftMatchIndex = 0, leftMatchIndexCount = leftMatchChunkView.Count; leftMatchIndex < leftMatchIndexCount; ++leftMatchIndex)
                                {
                                    if ((leftMatchIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var leftMatch = leftMatchChunkViewArray[leftMatchChunkViewOffset + leftMatchIndex];
                                    string leftMatchPatternId = leftMatch.PatternId;
                                    foreach (var rightMatch in rightMatchRowsBuffer)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        __musoqFinalShapeRows.Add(new ResultShape0(leftMatchPatternId, rightMatch.PatternId));
                                    }
                                }

                                continue;
                            }

                            if (leftMatchChunkView.Source is List<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow> leftMatchChunkViewList)
                            {
                                int leftMatchChunkViewOffset = leftMatchChunkView.Offset;
                                for (int leftMatchIndex = 0, leftMatchIndexCount = leftMatchChunkView.Count; leftMatchIndex < leftMatchIndexCount; ++leftMatchIndex)
                                {
                                    if ((leftMatchIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var leftMatch = leftMatchChunkViewList[leftMatchChunkViewOffset + leftMatchIndex];
                                    string leftMatchPatternId = leftMatch.PatternId;
                                    foreach (var rightMatch in rightMatchRowsBuffer)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        __musoqFinalShapeRows.Add(new ResultShape0(leftMatchPatternId, rightMatch.PatternId));
                                    }
                                }

                                continue;
                            }
                        }

                        for (int leftMatchIndex = 0, leftMatchIndexCount = leftMatchChunk.Count; leftMatchIndex < leftMatchIndexCount; ++leftMatchIndex)
                        {
                            if ((leftMatchIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var leftMatch = leftMatchChunk[leftMatchIndex];
                            string leftMatchPatternId = leftMatch.PatternId;
                            foreach (var rightMatch in rightMatchRowsBuffer)
                            {
                                token.ThrowIfCancellationRequested();
                                __musoqFinalShapeRows.Add(new ResultShape0(leftMatchPatternId, rightMatch.PatternId));
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
                pValuesCFFA3020Row0[] cte0_pRows = new pValuesCFFA3020Row0[]
                {
                    new pValuesCFFA3020Row0("todo", "TODO")
                };
                var cte0 = new List<Cte0Row0>();
                foreach (var p in cte0_pRows)
                {
                    token.ThrowIfCancellationRequested();
                    cte0.Add(new Cte0Row0(p.Id, p.Pattern));
                }

                return cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        private static Musoq.Examples.DataSources.StructuredInputs.PatternInput[] __musoqPrepareCte_3c3c288101ed6850(IReadOnlyList<Cte0Row0>? rows, CancellationToken token)
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
                        if (__musoqStructural_cteRow1.p_Id != null)
                            __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)__musoqStructural_cteRow1.p_Id.Length * 2L));
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        if (__musoqStructural_cteRow1.p_Pattern != null)
                            __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)__musoqStructural_cteRow1.p_Pattern.Length * 2L));
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        if ("literal" != null)
                            __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)"literal".Length * 2L));
                    }
                }

                global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("cte", "leftMatch.argument[1]", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "leftMatch:2");
            }

            if (rows is null || rows.Count == 0)
                return System.Array.Empty<Musoq.Examples.DataSources.StructuredInputs.PatternInput>();
            token.ThrowIfCancellationRequested();
            var result = new Musoq.Examples.DataSources.StructuredInputs.PatternInput[rows.Count];
            for (var index = 0; index < rows.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                var row = rows[index];
                result[index] = new Musoq.Examples.DataSources.StructuredInputs.PatternInput(row.p_Id, row.p_Pattern, "literal");
            }

            return result;
        }

        private static Musoq.Examples.DataSources.StructuredInputs.PatternInput[] __musoqPrepareCte_89ba721be29109ac(IReadOnlyList<Cte0Row0>? rows, CancellationToken token)
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
                        if (__musoqStructural_cteRow1.p_Id != null)
                            __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)__musoqStructural_cteRow1.p_Id.Length * 2L));
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        if (__musoqStructural_cteRow1.p_Pattern != null)
                            __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)__musoqStructural_cteRow1.p_Pattern.Length * 2L));
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        if ("literal" != null)
                            __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)"literal".Length * 2L));
                    }
                }

                global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("cte", "rightMatch.argument[1]", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "rightMatch:2");
            }

            if (rows is null || rows.Count == 0)
                return System.Array.Empty<Musoq.Examples.DataSources.StructuredInputs.PatternInput>();
            token.ThrowIfCancellationRequested();
            var result = new Musoq.Examples.DataSources.StructuredInputs.PatternInput[rows.Count];
            for (var index = 0; index < rows.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                var row = rows[index];
                result[index] = new Musoq.Examples.DataSources.StructuredInputs.PatternInput(row.p_Id, row.p_Pattern, "literal");
            }

            return result;
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(string __value0, string __value1)
            {
                p_Id = __value0;
                p_Pattern = __value1;
            }

            public string p_Id { get; }
            public string p_Pattern { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                leftMatch_PatternId = __value0;
                rightMatch_PatternId = __value1;
            }

            public override int Count => 2;
            public string leftMatch_PatternId { get; private set; }
            public string rightMatch_PatternId { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        leftMatch_PatternId = (string)value;
                        break;
                    case 1:
                        rightMatch_PatternId = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "leftMatch.PatternId" => true,
                "leftMatch_PatternId" => true,
                "rightMatch.PatternId" => true,
                "rightMatch_PatternId" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)leftMatch_PatternId,
                1 => (object)rightMatch_PatternId,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "leftMatch.PatternId" => (object)leftMatch_PatternId,
                "leftMatch_PatternId" => (object)leftMatch_PatternId,
                "rightMatch.PatternId" => (object)rightMatch_PatternId,
                "rightMatch_PatternId" => (object)rightMatch_PatternId,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string leftMatch_PatternId, string rightMatch_PatternId)
            {
                this.leftMatch_PatternId = leftMatch_PatternId;
                this.rightMatch_PatternId = rightMatch_PatternId;
            }

            public string leftMatch_PatternId { get; }
            public string rightMatch_PatternId { get; }
        }

        private sealed class pValuesCFFA3020Row0 : Row
        {
            public pValuesCFFA3020Row0(string __value0, string __value1)
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
    }
}
