// === Parsed Query ===
/*
param (label: string = 'parameter');
                    table NamedArgs {
                        Value: int,
                        First: string,
                        Second: int
                    };
                    table InputShape { Text: string };
                    couple named.any with table NamedArgs as Data;
                    couple #unknown.others with table InputShape as Forward;
                    with Input as (
                        select 'cte' as Text from #unknown.anything()
                    )
                    select d.First, d.Second, p.First, p.Second, c.Text
                    from Data('positional') d
                    cross join Data(second: 4, first: $label) p
                    cross join Forward(Input) c
*/

// === Logical Plan ===
/*
Cte
  Definition [Input]
    MultiStatement
      Project ['cte' as Text]
        SchemaScan [#unknown.anything() as ko3iko]
  Query
    MultiStatement
      Project [d.First as d.First, d.Second as d.Second, p.First as p.First, p.Second as p.Second]
        Join [Cross] [TRUE]
          SchemaScan [#named.any('positional', 7) as d]
          SchemaScan [#named.any($label, 4) as p]
      Project [dp.d.First as d.First, dp.d.Second as d.Second, dp.p.First as p.First, dp.p.Second as p.Second, c.Text as Text]
        Join [Cross] [TRUE]
          CteRef [dp as dp]
          SchemaScan [#unknown.others(Input) as c]
      Project [d.First as First, d.Second as Second, p.First as First, p.Second as Second, c.Text as Text]
        CteRef [dpc as dpc]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [Input]
    PhysicalMultiStatement
      PhysicalProject ['cte' as Text]
        PhysicalSchemaScan [#unknown.anything() as ko3iko] [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=59E11789C84DE0D1692B9659AC357F88F42A5D6546AD03D5C432215D85D62406]
  Query
    PhysicalMultiStatement
      PhysicalProject [d.First as d.First, d.Second as d.Second, p.First as p.First, p.Second as p.Second]
        PhysicalNestedLoopJoin [Cross] [TRUE]
          PhysicalSchemaScan [#named.any('positional', 7) as d]
          PhysicalSchemaScan [#named.any($label, 4) as p]
      PhysicalProject [dp.d.First as d.First, dp.d.Second as d.Second, dp.p.First as p.First, dp.p.Second as p.Second, c.Text as Text]
        PhysicalNestedLoopJoin [Cross] [TRUE]
          PhysicalCteRef [dp as dp]
          PhysicalSchemaScan [#unknown.others(Input) as c] [query-row:SealedClass;lifetime=EscapesScan;shape=5CCBCDD23DD545647DDFD640C554AC8F516CBAF92DEE0B1B5E09C53C2A8B74A8]
      PhysicalProject [d.First as First, d.Second as Second, p.First as First, p.Second as Second, c.Text as Text]
        PhysicalCteRef [dpc as dpc]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    Generated [QueryRow_59E11789C84D_S]
    Generated [Cte0Row0]
      Text: string <- field Text
    SourceEntity [d: NamedDatasourceSampleEntity]
      First: string <- property First
      Second: int <- property Second
    SourceEntity [p: NamedDatasourceSampleEntity]
      First: string <- property First
      Second: int <- property Second
    Generated [Statement0Row0]
      d.First: string <- field d_First
      d.Second: int <- field d_Second
      p.First: string <- field p_First
      p.Second: int <- field p_Second
    TableRow [dp]
      d.First: string <- field d_First
      d.Second: int <- field d_Second
      p.First: string <- field p_First
      p.Second: int <- field p_Second
    Generated [QueryRow_5CCBCDD23DD5_C]
      Text: string <- field Field0
    Generated [ResultRow0]
      First: string <- field First
      Second: int <- field Second
      First: string <- field First_1
      Second: int <- field Second_1
      Text: string <- field Text

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    SourceScan [ko3iko: QueryRow_59E11789C84D_S] -> cte0_ko3ikoRows [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=59E11789C84DE0D1692B9659AC357F88F42A5D6546AD03D5C432215D85D62406]
    CreateTable [cte0: Cte0Row0]
    PhaseBoundary [Select:cte0]
    ChunkedForEach [ko3iko in cte0_ko3ikoRows]
      AppendRow [cte0 <- Cte0Row0(Text: 'cte')]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Begin:cte1]
    SourceScan [d: NamedDatasourceSampleEntity] -> statement0_dRows
    SourceScan [p: NamedDatasourceSampleEntity] -> statement0_pRows
    CreateTable [statement0: Statement0Row0]
    MaterializeChunked [statement0_pRows -> pRowsBuffer]
    ChunkedForEach [d in statement0_dRows]
      Let [dFirst: string = d.First]
      Let [dSecond: int = d.Second]
      ForEach [p in pRowsBuffer]
        AppendRow [statement0 <- Statement0Row0(d.First: dFirst, d.Second: dSecond, p.First: p.First, p.Second: p.Second)]
    StoreTable [statement0 -> _cteRowResults.Slot1: List<Statement0Row0>]
    PhaseBoundary [End:cte1]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte2]
    SourceScan [c: QueryRow_5CCBCDD23DD5_C] -> cRows [query-row:SealedClass;lifetime=EscapesScan;shape=5CCBCDD23DD545647DDFD640C554AC8F516CBAF92DEE0B1B5E09C53C2A8B74A8]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    MaterializeChunked [cRows -> cRowsBuffer]
    ForEach [dp in _cteRowResults.Slot1]
      Let [dpdFirst: string = dp.d.First]
      Let [dpdSecond: int = dp.d.Second]
      Let [dppFirst: string = dp.p.First]
      Let [dppSecond: int = dp.p.Second]
      ForEach [c in cRowsBuffer]
        AppendShape [result <- ResultRow0(First: dpdFirst, Second: dpdSecond, First: dppFirst, Second: dppSecond, Text: c.Text)]
    PhaseBoundary [End:cte2]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q321_SpecTableCoupleArguments
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
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("Text", typeof(string), 0)
        };
        private static readonly Column[] __columns_compiled_result_5 = new Column[]
        {
            new Column("First", typeof(string), 0),
            new Column("Second", typeof(int), 1),
            new Column("First", typeof(string), 2),
            new Column("Second", typeof(int), 3),
            new Column("Text", typeof(string), 4)
        };
        private static readonly Column[] __columns_compiled_statement0_3 = new Column[]
        {
            new Column("d.First", typeof(string), 0),
            new Column("d.Second", typeof(int), 1),
            new Column("p.First", typeof(string), 2),
            new Column("p.Second", typeof(int), 3)
        };
        private static readonly QueryRowShape __queryRowShape_59E11789C84D = new QueryRowShape(new QueryRowField[] { });
        private static readonly QueryRowShape __queryRowShape_5CCBCDD23DD5 = new QueryRowShape(new QueryRowField[] { new QueryRowField(0, 0, "Text", typeof(string), true) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_c_4 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Text", typeof(string), 0) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_d_2 = Array.AsReadOnly(new ISchemaColumn[] { new Column("First", typeof(string), 0), new Column("Second", typeof(int), 1) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = new ScriptParameterContract[]
        {
            new ScriptParameterContract("label", "string", "string", typeof(string), false, false, null, null, true, ScriptParameterDefaultKind.Literal, "parameter")
        };
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = new ScriptParameterDefinition[]
        {
            new ScriptParameterDefinition(new ScriptParameterContract("label", "string", "string", typeof(string), false, false, null, null, true, ScriptParameterDefaultKind.Literal, "parameter"))
        };
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_5, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                var _tableResults = new Musoq.Evaluator.Tables.Table[2];
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                var paramLabel = ScriptParameterBinder.GetOptional<string>(__musoqExecutionState, "label", "parameter");
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, new string[] { "label" });
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _tableResults, _cteRowResults);
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _tableResults, _cteRowResults, paramLabel);
                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte2", QueryPhase.Begin);
                try
                {
                    var __cSchema = provider.GetSchema("#unknown");
                    var __cSchemaQueryRows = __cSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#unknown.others' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape 5CCBCDD23DD545647DDFD640C554AC8F516CBAF92DEE0B1B5E09C53C2A8B74A8).");
                    var cRowsSource = QuerySourceInvocation<QueryRow_5CCBCDD23DD5_C, QueryRowMaterializer_5CCBCDD23DD5_C>.Open(__cSchemaQueryRows, "others", new QueryScopedRowSourceRequest(new SourceExecutionContext("c:2", sourceExecutionPlans["c:2"], token, __schemaColumns_compiled_c_4, sourceRuntimeSettingsBySourceContextId["c:2"], logger, OnDataSourceProgress), __queryRowShape_5CCBCDD23DD5), global::Musoq.Schema.SourceArguments.Create(_tableResults[0], global::Musoq.Schema.SourceArguments.Empty));
                    var cRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_5CCBCDD23DD5_C>(cRowsSource.Chunks, __musoqProgressContext, "c:2") : cRowsSource.Chunks;
                    var cRowsBuffer = EvaluationHelper.MaterializeGeneratedChunkedRows<QueryRow_5CCBCDD23DD5_C>(cRows);
                    var __storedTable1Rows = _cteRowResults.Slot1;
                    for (int __storedTable1Index = 0; __storedTable1Index < __storedTable1Rows.Count; ++__storedTable1Index)
                    {
                        if ((__storedTable1Index & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        Statement0Row0 dp = __storedTable1Rows[__storedTable1Index];
                        string dpdFirst = dp.d_First;
                        int dpdSecond = dp.d_Second;
                        string dppFirst = dp.p_First;
                        int dppSecond = dp.p_Second;
                        for (int cRowsBufferIndex = 0; cRowsBufferIndex < cRowsBuffer.Count; ++cRowsBufferIndex)
                        {
                            if ((cRowsBufferIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            QueryRow_5CCBCDD23DD5_C c = (QueryRow_5CCBCDD23DD5_C)cRowsBuffer[cRowsBufferIndex];
                            __musoqFinalShapeRows.Add(new ResultRow0(dpdFirst, dpdSecond, dppFirst, dppSecond, c.Field0));
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
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, Musoq.Evaluator.Tables.Table[] _tableResults, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                var __cte0_ko3ikoSchema = provider.GetSchema("#unknown");
                var __cte0_ko3ikoSchemaQueryRows = __cte0_ko3ikoSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#unknown.anything' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape 59E11789C84DE0D1692B9659AC357F88F42A5D6546AD03D5C432215D85D62406).");
                var cte0_ko3ikoRowsSource = QuerySourceInvocation<QueryRow_59E11789C84D_S, QueryRowMaterializer_59E11789C84D_S>.Open(__cte0_ko3ikoSchemaQueryRows, "anything", new QueryScopedRowSourceRequest(new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), __queryRowShape_59E11789C84D), global::Musoq.Schema.SourceArguments.Empty);
                var cte0_ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_59E11789C84D_S>(cte0_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : cte0_ko3ikoRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                foreach (var ko3ikoChunk in cte0_ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<QueryRow_59E11789C84D_S> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is QueryRow_59E11789C84D_S[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                cte0.Add(new Cte0Row0("cte"));
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<QueryRow_59E11789C84D_S> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                cte0.Add(new Cte0Row0("cte"));
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
                        cte0.Add(new Cte0Row0("cte"));
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
        private static List<Statement0Row0> BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, Musoq.Evaluator.Tables.Table[] _tableResults, CteRowResults _cteRowResults, string paramLabel)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                var __statement0_dSchema = provider.GetSchema("#named");
                var statement0_dRowsSource = SourceInvocation<Musoq.Evaluator.Tests.NamedDatasourceSampleEntity>.Open(__statement0_dSchema, "any", new SourceExecutionContext("d:2", sourceExecutionPlans["d:2"], token, __schemaColumns_compiled_d_2, sourceRuntimeSettingsBySourceContextId["d:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Create((string)"positional", global::Musoq.Schema.SourceArguments.Create((int)7, global::Musoq.Schema.SourceArguments.Empty)));
                var statement0_dRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.NamedDatasourceSampleEntity>(statement0_dRowsSource.Chunks, __musoqProgressContext, "d:2") : statement0_dRowsSource.Chunks;
                var __statement0_pSchema = provider.GetSchema("#named");
                var statement0_pRowsSource = SourceInvocation<Musoq.Evaluator.Tests.NamedDatasourceSampleEntity>.Open(__statement0_pSchema, "any", new SourceExecutionContext("p:2", sourceExecutionPlans["p:2"], token, __schemaColumns_compiled_d_2, sourceRuntimeSettingsBySourceContextId["p:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Create(paramLabel, global::Musoq.Schema.SourceArguments.Create((int)4, global::Musoq.Schema.SourceArguments.Empty)));
                var statement0_pRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.NamedDatasourceSampleEntity>(statement0_pRowsSource.Chunks, __musoqProgressContext, "p:2") : statement0_pRowsSource.Chunks;
                var statement0 = new List<Statement0Row0>();
                var pRowsBuffer = EvaluationHelper.MaterializeChunkedRows(statement0_pRows);
                foreach (var dChunk in statement0_dRows)
                {
                    if (dChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.NamedDatasourceSampleEntity> dChunkView)
                    {
                        if (dChunkView.Source is Musoq.Evaluator.Tests.NamedDatasourceSampleEntity[] dChunkViewArray)
                        {
                            int dChunkViewOffset = dChunkView.Offset;
                            for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                            {
                                if ((dIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var d = dChunkViewArray[dChunkViewOffset + dIndex];
                                string dFirst = d.First;
                                int dSecond = d.Second;
                                foreach (var p in pRowsBuffer)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0.Add(new Statement0Row0(dFirst, dSecond, p.First, p.Second));
                                }
                            }

                            continue;
                        }

                        if (dChunkView.Source is List<Musoq.Evaluator.Tests.NamedDatasourceSampleEntity> dChunkViewList)
                        {
                            int dChunkViewOffset = dChunkView.Offset;
                            for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                            {
                                if ((dIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var d = dChunkViewList[dChunkViewOffset + dIndex];
                                string dFirst = d.First;
                                int dSecond = d.Second;
                                foreach (var p in pRowsBuffer)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0.Add(new Statement0Row0(dFirst, dSecond, p.First, p.Second));
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
                        string dFirst = d.First;
                        int dSecond = d.Second;
                        foreach (var p in pRowsBuffer)
                        {
                            token.ThrowIfCancellationRequested();
                            statement0.Add(new Statement0Row0(dFirst, dSecond, p.First, p.Second));
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

        private sealed class Cte0Row0
        {
            public Cte0Row0(string __value0)
            {
                Text = __value0;
            }

            public string Text { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
            public List<Statement0Row0> Slot1;
        }

        private readonly struct QueryRowMaterializer_59E11789C84D_S : IQueryRowMaterializer<QueryRow_59E11789C84D_S>
        {
            public static QueryRow_59E11789C84D_S Materialize<TReader>(scoped ref TReader reader)
                where TReader : IQuerySourceFieldReader, allows ref struct => new QueryRow_59E11789C84D_S();
        }

        private readonly struct QueryRowMaterializer_5CCBCDD23DD5_C : IQueryRowMaterializer<QueryRow_5CCBCDD23DD5_C>
        {
            public static QueryRow_5CCBCDD23DD5_C Materialize<TReader>(scoped ref TReader reader)
                where TReader : IQuerySourceFieldReader, allows ref struct => new QueryRow_5CCBCDD23DD5_C(reader.Read<string>(0));
        }

        private readonly struct QueryRow_59E11789C84D_S
        {
            public QueryRow_59E11789C84D_S()
            {
            }
        }

        private sealed class QueryRow_5CCBCDD23DD5_C
        {
            public QueryRow_5CCBCDD23DD5_C(string __value0)
            {
                Field0 = __value0;
            }

            public string Field0 { get; }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, int __value1, string __value2, int __value3, string __value4)
            {
                First = __value0;
                Second = __value1;
                First_1 = __value2;
                Second_1 = __value3;
                Text = __value4;
            }

            public override int Count => 5;
            public string First { get; private set; }
            public string First_1 { get; private set; }
            public int Second { get; private set; }
            public int Second_1 { get; private set; }
            public string Text { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.First = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Second = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.First_1 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Second_1 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Text = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.First),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Second),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.First_1),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Second_1),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Text),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "First" => true,
                "Second" => true,
                "First_1" => true,
                "Second_1" => true,
                "Text" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.First, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Second, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.First_1, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Second_1, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.Text, ref visitor);
            }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(string __value0, int __value1, string __value2, int __value3)
            {
                d_First = __value0;
                d_Second = __value1;
                p_First = __value2;
                p_Second = __value3;
            }

            public string d_First { get; }
            public int d_Second { get; }
            public string p_First { get; }
            public int p_Second { get; }
        }
    }
}
