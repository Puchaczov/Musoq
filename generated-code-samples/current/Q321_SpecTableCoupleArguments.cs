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
      Project [d.First as d.First, d.Second as d.Second, p.First as p.First, p.Second as p.Second, c.Text as c.Text]
        CteRef [dpc as dpc]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [Input]
    PhysicalMultiStatement
      PhysicalProject ['cte' as Text]
        PhysicalSchemaScan [#unknown.anything() as ko3iko]
  Query
    PhysicalMultiStatement
      PhysicalProject [d.First as d.First, d.Second as d.Second, p.First as p.First, p.Second as p.Second]
        PhysicalNestedLoopJoin [Cross] [TRUE]
          PhysicalSchemaScan [#named.any('positional', 7) as d]
          PhysicalSchemaScan [#named.any($label, 4) as p]
      PhysicalProject [dp.d.First as d.First, dp.d.Second as d.Second, dp.p.First as p.First, dp.p.Second as p.Second, c.Text as Text]
        PhysicalNestedLoopJoin [Cross] [TRUE]
          PhysicalCteRef [dp as dp]
          PhysicalSchemaScan [#unknown.others(Input) as c]
      PhysicalProject [d.First as d.First, d.Second as d.Second, p.First as p.First, p.Second as p.Second, c.Text as c.Text]
        PhysicalCteRef [dpc as dpc]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    ExpandoAdapter [ko3iko: ko3ikoDynamicRow0]
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
    ExpandoAdapter [c: cDynamicRow0]
      Text: string <- expando key "Text"
    Generated [ResultRow0]
      d.First: string <- field d_First
      d.Second: int <- field d_Second
      p.First: string <- field p_First
      p.Second: int <- field p_Second
      c.Text: string <- field c_Text

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    SourceScan [ko3iko: IReadOnlyDictionary<string, object>] -> cte0_ko3ikoRows
    CreateTable [cte0: Cte0Row0]
    PhaseBoundary [Select:cte0]
    ChunkedForEach [ko3ikoResolver in cte0_ko3ikoRows]
      AdaptExpando [ko3iko: ko3ikoDynamicRow0 <- ko3ikoResolver]
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
    SourceScan [c: IReadOnlyDictionary<string, object>] -> cRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    MaterializeChunked [cRows -> cRowsBuffer]
    ForEach [dp in _cteRowResults.Slot1]
      Let [dpdFirst: string = dp.d.First]
      Let [dpdSecond: int = dp.d.Second]
      Let [dppFirst: string = dp.p.First]
      Let [dppSecond: int = dp.p.Second]
      ForEach [cResolver in cRowsBuffer]
        AdaptExpando [c: cDynamicRow0 <- cResolver]
        AppendShape [result <- ResultShape0(d.First: dpdFirst, d.Second: dpdSecond, p.First: dppFirst, p.Second: dppSecond, c.Text: c.Text)]
    PhaseBoundary [End:cte2]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("Text", typeof(string), 0)
        };
        private static readonly Column[] __columns_compiled_result_5 = new Column[]
        {
            new Column("d.First", typeof(string), 0),
            new Column("d.Second", typeof(int), 1),
            new Column("p.First", typeof(string), 2),
            new Column("p.Second", typeof(int), 3),
            new Column("c.Text", typeof(string), 4)
        };
        private static readonly Column[] __columns_compiled_statement0_3 = new Column[]
        {
            new Column("d.First", typeof(string), 0),
            new Column("d.Second", typeof(int), 1),
            new Column("p.First", typeof(string), 2),
            new Column("p.Second", typeof(int), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_c_4 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Text", typeof(string), 0) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_d_2 = Array.AsReadOnly(new ISchemaColumn[] { new Column("First", typeof(string), 1), new Column("Second", typeof(int), 2) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_5, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.d_First, __musoqShapeRow.d_Second, __musoqShapeRow.p_First, __musoqShapeRow.p_Second, __musoqShapeRow.c_Text);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _tableResults = new Musoq.Evaluator.Tables.Table[2];
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                var paramLabel = ScriptParameterBinder.GetOptional<string>(__musoqExecutionState.Parameters, "label", "parameter");
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, new string[] { "label" });
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _tableResults, _cteRowResults);
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _tableResults, _cteRowResults, paramLabel);
                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte2", QueryPhase.Begin);
                try
                {
                    var __cSchema = provider.GetSchema("#unknown");
                    var cRowsSource = __cSchema.GetRowSource<IReadOnlyDictionary<string, object>>("others", new SourceExecutionContext("c:2", sourceExecutionPlans["c:2"], token, __schemaColumns_compiled_c_4, sourceRuntimeSettingsBySourceContextId["c:2"], logger, OnDataSourceProgress), new object[] { _tableResults[0] });
                    var cRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<IReadOnlyDictionary<string, object>>(cRowsSource.Chunks, __musoqProgressContext, "c:2") : cRowsSource.Chunks;
                    var cRowsBuffer = EvaluationHelper.MaterializeChunkedRows(cRows);
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
                        foreach (var cResolver in cRowsBuffer)
                        {
                            token.ThrowIfCancellationRequested();
                            var c = new cDynamicRow0(cResolver.TryGetValue("Text", out var __dynamicValue0_0) ? (string)__dynamicValue0_0 : default(string));
                            __musoqFinalShapeRows.Add(new ResultShape0(dpdFirst, dpdSecond, dppFirst, dppSecond, c.Text));
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
                var cte0_ko3ikoRowsSource = __cte0_ko3ikoSchema.GetRowSource<IReadOnlyDictionary<string, object>>("anything", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte0_ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<IReadOnlyDictionary<string, object>>(cte0_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : cte0_ko3ikoRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                foreach (var ko3ikoResolverChunk in cte0_ko3ikoRows)
                {
                    if (ko3ikoResolverChunk is global::Musoq.Schema.DataSources.RowChunk<IReadOnlyDictionary<string, object>> ko3ikoResolverChunkView)
                    {
                        if (ko3ikoResolverChunkView.Source is IReadOnlyDictionary<string, object>[] ko3ikoResolverChunkViewArray)
                        {
                            int ko3ikoResolverChunkViewOffset = ko3ikoResolverChunkView.Offset;
                            for (int ko3ikoResolverIndex = 0, ko3ikoResolverIndexCount = ko3ikoResolverChunkView.Count; ko3ikoResolverIndex < ko3ikoResolverIndexCount; ++ko3ikoResolverIndex)
                            {
                                if ((ko3ikoResolverIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3ikoResolver = ko3ikoResolverChunkViewArray[ko3ikoResolverChunkViewOffset + ko3ikoResolverIndex];
                                var ko3iko = new ko3ikoDynamicRow0();
                                cte0.Add(new Cte0Row0("cte"));
                            }

                            continue;
                        }

                        if (ko3ikoResolverChunkView.Source is List<IReadOnlyDictionary<string, object>> ko3ikoResolverChunkViewList)
                        {
                            int ko3ikoResolverChunkViewOffset = ko3ikoResolverChunkView.Offset;
                            for (int ko3ikoResolverIndex = 0, ko3ikoResolverIndexCount = ko3ikoResolverChunkView.Count; ko3ikoResolverIndex < ko3ikoResolverIndexCount; ++ko3ikoResolverIndex)
                            {
                                if ((ko3ikoResolverIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3ikoResolver = ko3ikoResolverChunkViewList[ko3ikoResolverChunkViewOffset + ko3ikoResolverIndex];
                                var ko3iko = new ko3ikoDynamicRow0();
                                cte0.Add(new Cte0Row0("cte"));
                            }

                            continue;
                        }
                    }

                    for (int ko3ikoResolverIndex = 0, ko3ikoResolverIndexCount = ko3ikoResolverChunk.Count; ko3ikoResolverIndex < ko3ikoResolverIndexCount; ++ko3ikoResolverIndex)
                    {
                        if ((ko3ikoResolverIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var ko3ikoResolver = ko3ikoResolverChunk[ko3ikoResolverIndex];
                        var ko3iko = new ko3ikoDynamicRow0();
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
                var statement0_dRowsSource = __statement0_dSchema.GetRowSource<Musoq.Evaluator.Tests.NamedDatasourceSampleEntity>("any", new SourceExecutionContext("d:2", sourceExecutionPlans["d:2"], token, __schemaColumns_compiled_d_2, sourceRuntimeSettingsBySourceContextId["d:2"], logger, OnDataSourceProgress), new object[] { "positional", 7 });
                var statement0_dRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.NamedDatasourceSampleEntity>(statement0_dRowsSource.Chunks, __musoqProgressContext, "d:2") : statement0_dRowsSource.Chunks;
                var __statement0_pSchema = provider.GetSchema("#named");
                var statement0_pRowsSource = __statement0_pSchema.GetRowSource<Musoq.Evaluator.Tests.NamedDatasourceSampleEntity>("any", new SourceExecutionContext("p:2", sourceExecutionPlans["p:2"], token, __schemaColumns_compiled_d_2, sourceRuntimeSettingsBySourceContextId["p:2"], logger, OnDataSourceProgress), new object[] { paramLabel, 4 });
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, int __value1, string __value2, int __value3, string __value4)
            {
                d_First = __value0;
                d_Second = __value1;
                p_First = __value2;
                p_Second = __value3;
                c_Text = __value4;
            }

            public override int Count => 5;
            public string c_Text { get; private set; }
            public string d_First { get; private set; }
            public int d_Second { get; private set; }
            public string p_First { get; private set; }
            public int p_Second { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        d_First = (string)value;
                        break;
                    case 1:
                        d_Second = (int)value;
                        break;
                    case 2:
                        p_First = (string)value;
                        break;
                    case 3:
                        p_Second = (int)value;
                        break;
                    case 4:
                        c_Text = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "d.First" => true,
                "d_First" => true,
                "d.Second" => true,
                "d_Second" => true,
                "p.First" => true,
                "p_First" => true,
                "p.Second" => true,
                "p_Second" => true,
                "c.Text" => true,
                "c_Text" => true,
                "Text" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)d_First,
                1 => (object)d_Second,
                2 => (object)p_First,
                3 => (object)p_Second,
                4 => (object)c_Text,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "d.First" => (object)d_First,
                "d_First" => (object)d_First,
                "d.Second" => (object)d_Second,
                "d_Second" => (object)d_Second,
                "p.First" => (object)p_First,
                "p_First" => (object)p_First,
                "p.Second" => (object)p_Second,
                "p_Second" => (object)p_Second,
                "c.Text" => (object)c_Text,
                "c_Text" => (object)c_Text,
                "Text" => (object)c_Text,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string d_First, int d_Second, string p_First, int p_Second, string c_Text)
            {
                this.d_First = d_First;
                this.d_Second = d_Second;
                this.p_First = p_First;
                this.p_Second = p_Second;
                this.c_Text = c_Text;
            }

            public string c_Text { get; }
            public string d_First { get; }
            public int d_Second { get; }
            public string p_First { get; }
            public int p_Second { get; }
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

        private sealed class cDynamicRow0
        {
            public cDynamicRow0(string Text)
            {
                this.Text = Text;
            }

            public string Text { get; }
        }

        private sealed class ko3ikoDynamicRow0
        {
            public ko3ikoDynamicRow0()
            {
            }
        }
    }
}
