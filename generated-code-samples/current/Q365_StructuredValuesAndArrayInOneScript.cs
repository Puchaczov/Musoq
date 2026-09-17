// === Parsed Query ===
/*
let values = array { 1, 2, 3 };

select n.Value
from values { (Label: 'numbers') } p
cross apply #inputs.numbers(values: $values) n
*/

// === Logical Plan ===
/*
MultiStatement
  Project [n.Value as n.Value]
    Apply [Cross]
      ValuesScan [1 rows as p]
      SchemaScan [#inputs.numbers(<IReadOnlyList`1>$values) as n]
  Project [n.Value as n.Value]
    CteRef [pn as pn]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [n.Value as n.Value]
    PhysicalNestedLoopApply [Cross]
      PhysicalValuesScan [1 rows as p]
      PhysicalSchemaScan [#inputs.numbers(<IReadOnlyList`1>$values) as n]
  PhysicalProject [n.Value as n.Value]
    PhysicalCteRef [pn as pn]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Label: string <- field Label
    SourceEntity [n: NumberRow]
      Value: int <- property Value
    Generated [Statement0Row0]
      n.Value: int <- field n_Value
    TableRow [pn]
      n.Value: int <- field n_Value
    Generated [ResultRow0]
      n.Value: int <- field n_Value

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    CreateValuesRows [statement0_pRows: pValues6C41F491Row0 x 1]
    CreateTable [statement0: Statement0Row0]
    ForEach [p in statement0_pRows]
      Let [__musoqStructural_statement0_n_0: IReadOnlyList<int> = convert<sequence<int32>>($values)]
      SourceScan [n: NumberRow] -> statement0_nRows
      ChunkedForEach [n in statement0_nRows]
        AppendRow [statement0 <- Statement0Row0(n.Value: n.Value)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ForEach [pn in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(n.Value: pn.n.Value)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q365_StructuredValuesAndArrayInOneScript
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
        private static readonly Column[] __columns_compiled_statement0_0 = new Column[]
        {
            new Column("n.Value", typeof(int), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_n_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Value", typeof(int), 0) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_statement0_0, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.n_Value);
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
                int[] letValues = new int[]
                {
                    1,
                    2,
                    3
                };
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults, letValues);
                OnPhaseChanged("compiled", QueryPhase.Select);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 pn = __storedTable0Rows[__storedTable0Index];
                    __musoqFinalShapeRows.Add(new ResultShape0(pn.n_Value));
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
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, int[] letValues)
        {
            pValues6C41F491Row0[] statement0_pRows = new pValues6C41F491Row0[]
            {
                new pValues6C41F491Row0("numbers")
            };
            var statement0 = new List<Statement0Row0>();
            foreach (var p in statement0_pRows)
            {
                token.ThrowIfCancellationRequested();
                IReadOnlyList<int> __musoqStructural_statement0_n_0 = letValues;
                var __statement0_nSchema = provider.GetSchema("#inputs");
                var statement0_nRowsSourceContext = new SourceExecutionContext("n:1", sourceExecutionPlans["n:1"], token, __schemaColumns_compiled_n_1, sourceRuntimeSettingsBySourceContextId["n:1"], logger, OnDataSourceProgress);
                Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.NumberRow> statement0_nRowsSource;
                try
                {
                    Musoq.Examples.DataSources.StructuredInputs.NumbersSource statement0_nRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.NumbersSource(__musoqCheckStructural_9d1dda1139f4e5b0(__musoqStructural_statement0_n_0, token), statement0_nRowsSourceContext);
                    statement0_nRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.NumbersSource, Musoq.Examples.DataSources.StructuredInputs.NumberRow>(__statement0_nSchema, "numbers", statement0_nRowsSourceInstance, statement0_nRowsSourceContext, "#inputs", "n", "n:1");
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
                    throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "numbers", "n", "n:1", exception);
                }

                var statement0_nRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.NumberRow>(statement0_nRowsSource.Chunks, __musoqProgressContext, "n:1") : statement0_nRowsSource.Chunks;
                foreach (var nChunk in statement0_nRows)
                {
                    if (nChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Examples.DataSources.StructuredInputs.NumberRow> nChunkView)
                    {
                        if (nChunkView.Source is Musoq.Examples.DataSources.StructuredInputs.NumberRow[] nChunkViewArray)
                        {
                            int nChunkViewOffset = nChunkView.Offset;
                            for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                            {
                                if ((nIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var n = nChunkViewArray[nChunkViewOffset + nIndex];
                                statement0.Add(new Statement0Row0(n.Value));
                            }

                            continue;
                        }

                        if (nChunkView.Source is List<Musoq.Examples.DataSources.StructuredInputs.NumberRow> nChunkViewList)
                        {
                            int nChunkViewOffset = nChunkView.Offset;
                            for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                            {
                                if ((nIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var n = nChunkViewList[nChunkViewOffset + nIndex];
                                statement0.Add(new Statement0Row0(n.Value));
                            }

                            continue;
                        }
                    }

                    for (int nIndex = 0, nIndexCount = nChunk.Count; nIndex < nIndexCount; ++nIndex)
                    {
                        if ((nIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var n = nChunk[nIndex];
                        statement0.Add(new Statement0Row0(n.Value));
                    }
                }
            }

            return statement0;
        }

        private static IReadOnlyList<int> __musoqCheckStructural_9d1dda1139f4e5b0(IReadOnlyList<int> value, System.Threading.CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            long __musoqStructuralNodes = 0L;
            long __musoqStructuralStrings = 0L;
            int __musoqStructuralMaxDepth = 0;
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (1 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 1;
            if (value != null)
            {
                var __musoqStructural_collection1 = (System.Collections.Generic.IReadOnlyList<int>)value;
                var __musoqStructural_count2 = __musoqStructural_collection1.Count;
                for (var __musoqStructural_index0 = 0; __musoqStructural_index0 < __musoqStructural_count2; __musoqStructural_index0++)
                {
                    if ((__musoqStructural_index0 & 1023) == 0)
                        token.ThrowIfCancellationRequested();
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (2 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 2;
                }
            }

            global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("let", "$values", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "n:1");
            return value;
        }

        private sealed class CteRowResults
        {
            public List<Statement0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0)
            {
                n_Value = __value0;
            }

            public override int Count => 1;
            public int n_Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        n_Value = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "n.Value" => true,
                "n_Value" => true,
                "Value" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)n_Value,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "n.Value" => (object)n_Value,
                "n_Value" => (object)n_Value,
                "Value" => (object)n_Value,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int n_Value)
            {
                this.n_Value = n_Value;
            }

            public int n_Value { get; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(int __value0)
            {
                n_Value = __value0;
            }

            public int n_Value { get; }
        }

        private sealed class pValues6C41F491Row0 : Row
        {
            public pValues6C41F491Row0(string __value0)
            {
                Label = __value0;
            }

            public override int Count => 1;
            public string Label { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Label = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Label" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Label,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Label" => (object)Label,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
