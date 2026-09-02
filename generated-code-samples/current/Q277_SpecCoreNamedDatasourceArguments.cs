// === Parsed Query ===
/*
param(label: string = 'parameter'); table NamedShape { Value: int, First: string, Second: int }; couple #named.any with table NamedShape as Data; select d.Value, d.First, d.Second, p.Value, p.First, p.Second from Data(second: 4, first: $label) d cross join Data(first: 'positional') p
*/

// === Logical Plan ===
/*
MultiStatement
  Project [d.Value as d.Value, d.First as d.First, d.Second as d.Second, p.Value as p.Value, p.First as p.First, p.Second as p.Second]
    Join [Cross] [TRUE]
      SchemaScan [#named.any($label, 4) as d]
      SchemaScan [#named.any('positional', 7) as p]
  Project [d.Value as d.Value, d.First as d.First, d.Second as d.Second, p.Value as p.Value, p.First as p.First, p.Second as p.Second]
    CteRef [dp as dp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [d.Value as d.Value, d.First as d.First, d.Second as d.Second, p.Value as p.Value, p.First as p.First, p.Second as p.Second]
    PhysicalNestedLoopJoin [Cross] [TRUE]
      PhysicalSchemaScan [#named.any($label, 4) as d]
      PhysicalSchemaScan [#named.any('positional', 7) as p]
  PhysicalProject [d.Value as d.Value, d.First as d.First, d.Second as d.Second, p.Value as p.Value, p.First as p.First, p.Second as p.Second]
    PhysicalCteRef [dp as dp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [d: NamedDatasourceSampleEntity]
      Value: int <- property Value
      First: string <- property First
      Second: int <- property Second
    SourceEntity [p: NamedDatasourceSampleEntity]
      Value: int <- property Value
      First: string <- property First
      Second: int <- property Second
    Generated [ResultRow0]
      d.Value: int <- field d_Value
      d.First: string <- field d_First
      d.Second: int <- field d_Second
      p.Value: int <- field p_Value
      p.First: string <- field p_First
      p.Second: int <- field p_Second

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [d: NamedDatasourceSampleEntity] -> dRows
    SourceScan [p: NamedDatasourceSampleEntity] -> pRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    MaterializeChunked [pRows -> pRowsBuffer]
    ChunkedForEach [d in dRows]
      Let [dFirst: string = d.First]
      Let [dSecond: int = d.Second]
      Let [dValue: int = d.Value]
      ForEach [p in pRowsBuffer]
        AppendShape [result <- ResultShape0(d.Value: dValue, d.First: dFirst, d.Second: dSecond, p.Value: p.Value, p.First: p.First, p.Second: p.Second)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q277_SpecCoreNamedDatasourceArguments
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("d.Value", typeof(int), 0),
            new Column("d.First", typeof(string), 1),
            new Column("d.Second", typeof(int), 2),
            new Column("p.Value", typeof(int), 3),
            new Column("p.First", typeof(string), 4),
            new Column("p.Second", typeof(int), 5)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_d_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Value", typeof(int), 0), new Column("First", typeof(string), 1), new Column("Second", typeof(int), 2) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.d_Value, __musoqShapeRow.d_First, __musoqShapeRow.d_Second, __musoqShapeRow.p_Value, __musoqShapeRow.p_First, __musoqShapeRow.p_Second);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                var paramLabel = ScriptParameterBinder.GetOptional<string>(__musoqExecutionState.Parameters, "label", "parameter");
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, new string[] { "label" });
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    var __dSchema = provider.GetSchema("#named");
                    var dRowsSource = __dSchema.GetRowSource<Musoq.Evaluator.Tests.NamedDatasourceSampleEntity>("any", new SourceExecutionContext("d:1", sourceExecutionPlans["d:1"], token, __schemaColumns_compiled_d_0, sourceRuntimeSettingsBySourceContextId["d:1"], logger, OnDataSourceProgress), new object[] { paramLabel, 4 });
                    var dRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.NamedDatasourceSampleEntity>(dRowsSource.Chunks, __musoqProgressContext, "d:1") : dRowsSource.Chunks;
                    var __pSchema = provider.GetSchema("#named");
                    var pRowsSource = __pSchema.GetRowSource<Musoq.Evaluator.Tests.NamedDatasourceSampleEntity>("any", new SourceExecutionContext("p:1", sourceExecutionPlans["p:1"], token, __schemaColumns_compiled_d_0, sourceRuntimeSettingsBySourceContextId["p:1"], logger, OnDataSourceProgress), new object[] { "positional", 7 });
                    var pRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.NamedDatasourceSampleEntity>(pRowsSource.Chunks, __musoqProgressContext, "p:1") : pRowsSource.Chunks;
                    var pRowsBuffer = EvaluationHelper.MaterializeChunkedRows(pRows);
                    foreach (var dChunk in dRows)
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
                                    int dValue = d.Value;
                                    foreach (var p in pRowsBuffer)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        __musoqFinalShapeRows.Add(new ResultShape0(dValue, dFirst, dSecond, p.Value, p.First, p.Second));
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
                                    int dValue = d.Value;
                                    foreach (var p in pRowsBuffer)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        __musoqFinalShapeRows.Add(new ResultShape0(dValue, dFirst, dSecond, p.Value, p.First, p.Second));
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
                            int dValue = d.Value;
                            foreach (var p in pRowsBuffer)
                            {
                                token.ThrowIfCancellationRequested();
                                __musoqFinalShapeRows.Add(new ResultShape0(dValue, dFirst, dSecond, p.Value, p.First, p.Second));
                            }
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, string __value1, int __value2, int __value3, string __value4, int __value5)
            {
                d_Value = __value0;
                d_First = __value1;
                d_Second = __value2;
                p_Value = __value3;
                p_First = __value4;
                p_Second = __value5;
            }

            public override int Count => 6;
            public string d_First { get; private set; }
            public int d_Second { get; private set; }
            public int d_Value { get; private set; }
            public string p_First { get; private set; }
            public int p_Second { get; private set; }
            public int p_Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        d_Value = (int)value;
                        break;
                    case 1:
                        d_First = (string)value;
                        break;
                    case 2:
                        d_Second = (int)value;
                        break;
                    case 3:
                        p_Value = (int)value;
                        break;
                    case 4:
                        p_First = (string)value;
                        break;
                    case 5:
                        p_Second = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "d.Value" => true,
                "d_Value" => true,
                "d.First" => true,
                "d_First" => true,
                "d.Second" => true,
                "d_Second" => true,
                "p.Value" => true,
                "p_Value" => true,
                "p.First" => true,
                "p_First" => true,
                "p.Second" => true,
                "p_Second" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)d_Value,
                1 => (object)d_First,
                2 => (object)d_Second,
                3 => (object)p_Value,
                4 => (object)p_First,
                5 => (object)p_Second,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "d.Value" => (object)d_Value,
                "d_Value" => (object)d_Value,
                "d.First" => (object)d_First,
                "d_First" => (object)d_First,
                "d.Second" => (object)d_Second,
                "d_Second" => (object)d_Second,
                "p.Value" => (object)p_Value,
                "p_Value" => (object)p_Value,
                "p.First" => (object)p_First,
                "p_First" => (object)p_First,
                "p.Second" => (object)p_Second,
                "p_Second" => (object)p_Second,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int d_Value, string d_First, int d_Second, int p_Value, string p_First, int p_Second)
            {
                this.d_Value = d_Value;
                this.d_First = d_First;
                this.d_Second = d_Second;
                this.p_Value = p_Value;
                this.p_First = p_First;
                this.p_Second = p_Second;
            }

            public string d_First { get; }
            public int d_Second { get; }
            public int d_Value { get; }
            public string p_First { get; }
            public int p_Second { get; }
            public int p_Value { get; }
        }
    }
}
