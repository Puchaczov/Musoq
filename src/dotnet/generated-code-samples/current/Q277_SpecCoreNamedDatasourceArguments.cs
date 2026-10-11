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
  Project [d.Value as Value, d.First as First, d.Second as Second, p.Value as Value, p.First as First, p.Second as Second]
    CteRef [dp as dp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [d.Value as d.Value, d.First as d.First, d.Second as d.Second, p.Value as p.Value, p.First as p.First, p.Second as p.Second]
    PhysicalNestedLoopJoin [Cross] [TRUE]
      PhysicalSchemaScan [#named.any($label, 4) as d]
      PhysicalSchemaScan [#named.any('positional', 7) as p]
  PhysicalProject [d.Value as Value, d.First as First, d.Second as Second, p.Value as Value, p.First as First, p.Second as Second]
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
      Value: int <- field Value
      First: string <- field First
      Second: int <- field Second
      Value: int <- field Value_1
      First: string <- field First_1
      Second: int <- field Second_1

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [d: NamedDatasourceSampleEntity] -> dRows
    SourceScan [p: NamedDatasourceSampleEntity] -> pRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    MaterializeChunked [pRows -> pRowsBuffer]
    ChunkedForEach [d in dRows]
      Let [dFirst: string = d.First]
      Let [dSecond: int = d.Second]
      Let [dValue: int = d.Value]
      ForEach [p in pRowsBuffer]
        AppendShape [result <- ResultRow0(Value: dValue, First: dFirst, Second: dSecond, Value: p.Value, First: p.First, Second: p.Second)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Value", typeof(int), 0),
            new Column("First", typeof(string), 1),
            new Column("Second", typeof(int), 2),
            new Column("Value", typeof(int), 3),
            new Column("First", typeof(string), 4),
            new Column("Second", typeof(int), 5)
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
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                var paramLabel = ScriptParameterBinder.GetOptional<string>(__musoqExecutionState, "label", "parameter");
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, new string[] { "label" });
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    var __dSchema = provider.GetSchema("#named");
                    var dRowsSource = SourceInvocation<Musoq.Evaluator.Tests.NamedDatasourceSampleEntity>.Open(__dSchema, "any", new SourceExecutionContext("d:1", sourceExecutionPlans["d:1"], token, __schemaColumns_compiled_d_0, sourceRuntimeSettingsBySourceContextId["d:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Create(paramLabel, global::Musoq.Schema.SourceArguments.Create((int)4, global::Musoq.Schema.SourceArguments.Empty)));
                    var dRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.NamedDatasourceSampleEntity>(dRowsSource.Chunks, __musoqProgressContext, "d:1") : dRowsSource.Chunks;
                    var __pSchema = provider.GetSchema("#named");
                    var pRowsSource = SourceInvocation<Musoq.Evaluator.Tests.NamedDatasourceSampleEntity>.Open(__pSchema, "any", new SourceExecutionContext("p:1", sourceExecutionPlans["p:1"], token, __schemaColumns_compiled_d_0, sourceRuntimeSettingsBySourceContextId["p:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Create((string)"positional", global::Musoq.Schema.SourceArguments.Create((int)7, global::Musoq.Schema.SourceArguments.Empty)));
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
                                        __musoqFinalShapeRows.Add(new ResultRow0(dValue, dFirst, dSecond, p.Value, p.First, p.Second));
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
                                        __musoqFinalShapeRows.Add(new ResultRow0(dValue, dFirst, dSecond, p.Value, p.First, p.Second));
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
                                __musoqFinalShapeRows.Add(new ResultRow0(dValue, dFirst, dSecond, p.Value, p.First, p.Second));
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
                Value = __value0;
                First = __value1;
                Second = __value2;
                Value_1 = __value3;
                First_1 = __value4;
                Second_1 = __value5;
            }

            public override int Count => 6;
            public string First { get; private set; }
            public string First_1 { get; private set; }
            public int Second { get; private set; }
            public int Second_1 { get; private set; }
            public int Value { get; private set; }
            public int Value_1 { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.First = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Second = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value_1 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.First_1 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Second_1 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.First),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Second),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value_1),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.First_1),
                5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Second_1),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Value" => true,
                "First" => true,
                "Second" => true,
                "Value_1" => true,
                "First_1" => true,
                "Second_1" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Value, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.First, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Second, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Value_1, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.First_1, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(5, this.Second_1, ref visitor);
            }
        }
    }
}
