// === Parsed Query ===
/*
with numbers as (
    select p.Value
    from values {
        (Value: 3),
        (Value: 1),
        (Value: 2),
    } p
    order by p.Value
    take 2
)
select n.Value
from #inputs.numbers(values: numbers) n
*/

// === Logical Plan ===
/*
Cte
  Definition [numbers]
    MultiStatement
      Take [2]
        Sort [p.Value]
          Project [p.Value as p.Value]
            ValuesScan [3 rows as p]
  Query
    MultiStatement
      Project [n.Value as n.Value]
        SchemaScan [#inputs.numbers(CTE(numbers)) as n]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [numbers]
    PhysicalMultiStatement
      PhysicalTopN [2] [p.Value]
        PhysicalProject [p.Value as p.Value]
          PhysicalValuesScan [3 rows as p]
  Query
    PhysicalMultiStatement
      PhysicalProject [n.Value as n.Value]
        PhysicalSchemaScan [#inputs.numbers(CTE(numbers)) as n]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Value: int <- field Value
    Generated [Cte0Row0]
      p.Value: int <- field p_Value
    SourceEntity [n: NumberRow]
      Value: int <- property Value
    Generated [ResultRow0]
      n.Value: int <- field n_Value

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    PhaseBoundary [From]
    CreateValuesRows [cte0_pRows: pValues5F43885BRow0 x 3]
    CreateTable [cte0: Cte0Row0]
    PhaseBoundary [Select:cte0]
    ForEach [p in cte0_pRows]
      AppendRow [cte0 <- Cte0Row0(p.Value: p.Value)]
    TopNTable [cte0 -> cte0TopN by p.Value ASC, 2]
    StoreTable [cte0TopN -> _tableResults[0]]
    PhaseBoundary [End:cte0]
    SourceScan [n: NumberRow] -> nRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [n in nRows]
      AppendShape [result <- ResultShape0(n.Value: n.Value)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q345_StructuredPagedCteArgument
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
            new Column("p.Value", typeof(int), 0)
        };
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                var _tableResults = new Musoq.Evaluator.Tables.Table[1];
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                Musoq.Evaluator.Tables.Table cte0 = null!;
                try
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.From);
                    OnPhaseChanged("compiled", QueryPhase.From);
                    pValues5F43885BRow0[] cte0_pRows = new pValues5F43885BRow0[]
                    {
                        new pValues5F43885BRow0(3),
                        new pValues5F43885BRow0(1),
                        new pValues5F43885BRow0(2)
                    };
                    cte0 = new Table("cte0", __columns_compiled_cte0_0);
                    OnPhaseChanged("compiled:cte0", QueryPhase.Select);
                    foreach (var p in cte0_pRows)
                    {
                        token.ThrowIfCancellationRequested();
                        cte0.AddDirect(new Cte0Row0(p.Value));
                    }

                    var cte0TopNRows = EvaluationHelper.CastGeneratedRows<Cte0Row0>(cte0.Rows).OrderBy((row) => row, Cte0Row0OrderBy_0AComparer.Instance).Take(2);
                    var cte0TopN = new Table("cte0TopN", __columns_compiled_cte0_0);
                    cte0TopN.EnsureCapacity(Math.Min(cte0.Count, 2));
                    foreach (var copiedRow in cte0TopNRows)
                    {
                        cte0TopN.AddDirect(copiedRow);
                    }

                    _tableResults[0] = cte0TopN;
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
                }

                var __nSchema = provider.GetSchema("#inputs");
                var nRowsSourceContext = new SourceExecutionContext("n:2", sourceExecutionPlans["n:2"], token, __schemaColumns_compiled_n_1, sourceRuntimeSettingsBySourceContextId["n:2"], logger, OnDataSourceProgress);
                Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.NumberRow> nRowsSource;
                try
                {
                    Musoq.Examples.DataSources.StructuredInputs.NumbersSource nRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.NumbersSource(__musoqPrepareCte_86c7c4a685492ef0(EvaluationHelper.CastGeneratedRows<Cte0Row0>(_tableResults[0].Rows), token), nRowsSourceContext);
                    nRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.NumbersSource, Musoq.Examples.DataSources.StructuredInputs.NumberRow>(__nSchema, "numbers", nRowsSourceInstance, nRowsSourceContext, "#inputs", "n", "n:2");
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
                    throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "numbers", "n", "n:2", exception);
                }

                var nRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.NumberRow>(nRowsSource.Chunks, __musoqProgressContext, "n:2") : nRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var nChunk in nRows)
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
                                __musoqFinalShapeRows.Add(new ResultShape0(n.Value));
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
                                __musoqFinalShapeRows.Add(new ResultShape0(n.Value));
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
                        __musoqFinalShapeRows.Add(new ResultShape0(n.Value));
                    }
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

        private static int[] __musoqPrepareCte_86c7c4a685492ef0(IReadOnlyList<Cte0Row0>? rows, CancellationToken token)
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

                global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("cte", "n.argument[0]", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "n:2");
            }

            if (rows is null || rows.Count == 0)
                return System.Array.Empty<int>();
            token.ThrowIfCancellationRequested();
            var result = new int[rows.Count];
            for (var index = 0; index < rows.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                var row = rows[index];
                result[index] = row.p_Value;
            }

            return result;
        }

        private sealed class Cte0Row0 : Row
        {
            public Cte0Row0(int __value0)
            {
                p_Value = __value0;
            }

            public override int Count => 1;
            public int p_Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        p_Value = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "p.Value" => true,
                "p_Value" => true,
                "Value" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)p_Value,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "p.Value" => (object)p_Value,
                "p_Value" => (object)p_Value,
                "Value" => (object)p_Value,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class Cte0Row0OrderBy_0AComparer : IComparer<Cte0Row0>
        {
            public static readonly Cte0Row0OrderBy_0AComparer Instance = new Cte0Row0OrderBy_0AComparer();
            public int Compare(Cte0Row0 left, Cte0Row0 right)
            {
                var comparison = left.p_Value.CompareTo(right.p_Value);
                if (comparison != 0)
                    return comparison;
                return 0;
            }
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

        private sealed class pValues5F43885BRow0 : Row
        {
            public pValues5F43885BRow0(int __value0)
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
