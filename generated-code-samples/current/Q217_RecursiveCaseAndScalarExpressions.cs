// === Parsed Query ===
/*
with recursive labels (Value, Depth, Label) as (select 1, 0, 'one' from values {{ Seed: 1 }} seed union all select l.Value + 1, l.Depth + 1, case when l.Value = 1 then 'even' else 'odd' end from labels l where l.Depth < 2) select Value, Depth, Label from labels order by Depth
*/

// === Logical Plan ===
/*
Cte
  Definition [labels]
    RecursiveCte [labels] [All]
      Anchor
        MultiStatement
          Project [1 as Value, 0 as Depth, 'one' as Label]
            ValuesScan [1 rows as seed]
      RecursiveMember
        MultiStatement
          Project [(l.Value + 1) as l.Value + 1, (l.Depth + 1) as l.Depth + 1, CASE WHEN (l.Value = 1) THEN 'even' ELSE 'odd' END as case when l.Value = 1 then even else odd end]
            Filter [(l.Depth < 2)]
              CteRef [labels as l]
  Query
    MultiStatement
      Sort [labels.Depth]
        Project [labels.Value as Value, labels.Depth as Depth, labels.Label as Label]
          CteRef [labels as labels]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [labels]
    PhysicalRecursiveCte [labels] [All]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [1 as Value, 0 as Depth, 'one' as Label]
            PhysicalValuesScan [1 rows as seed]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [(l.Value + 1) as l.Value + 1, (l.Depth + 1) as l.Depth + 1, CASE WHEN (l.Value = 1) THEN 'even' ELSE 'odd' END as case when l.Value = 1 then even else odd end]
            PhysicalFilter [(l.Depth < 2)]
              PhysicalCteRef [labels as l]
  Query
    PhysicalMultiStatement
      PhysicalSort [labels.Depth]
        PhysicalProject [labels.Value as Value, labels.Depth as Depth, labels.Label as Label]
          PhysicalCteRef [labels as labels]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Seed: int <- field Seed
    Generated [Cte0Row0]
      Value: int <- field Value
      Depth: int <- field Depth
      Label: string <- field Label
    TableRow [l]
      Value: int <- field Value
      Depth: int <- field Depth
      Label: string <- field Label
    TableRow [labels]
      Value: int <- field Value
      Depth: int <- field Depth
      Label: string <- field Label
    Generated [ResultRow0]
      Value: int <- field Value
      Depth: int <- field Depth
      Label: string <- field Label

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    RecursiveCte [labels; result cte0; frontiers cte0CurrentFrontier, cte0NextFrontier; identity none; max iterations 1000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        CreateValuesRows [cte0CurrentFrontier_seedRows: seedValuesD6F9BDFERow0 x 1]
        ForEach [seed in cte0CurrentFrontier_seedRows]
          RecursiveAppend [cte0CurrentFrontier <- Cte0Row0(Value: 1, Depth: 0, Label: 'one'); identity none; guard cte0.Count + cte0CurrentFrontier.Count < 10000000]
      RecursiveMember
        ForEach [l in cte0CurrentFrontier]
          If [(l.Depth < 2)]
            RecursiveAppend [cte0NextFrontier <- Cte0Row0(Value: (l.Value + 1), Depth: (l.Depth + 1), Label: CASE WHEN (l.Value = 1) THEN 'even' ELSE 'odd' END); identity none; guard cte0.Count + cte0NextFrontier.Count < 10000000]
    PhaseBoundary [Where:cte0]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [From]
    ForEach [labels in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Value: labels.Value, Depth: labels.Depth, Label: labels.Label)]
    PhaseBoundary [Select]
    SortShapeRows [result -> resultSorted by Depth ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q217_RecursiveCaseAndScalarExpressions
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
        private static readonly Column[] __columns_compiled_result_0 = new Column[]
        {
            new Column("Value", typeof(int), 0),
            new Column("Depth", typeof(int), 1),
            new Column("Label", typeof(string), 2)
        };
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_0, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Value, __musoqShapeRow.Depth, __musoqShapeRow.Label);
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
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.From);
                    var cte0 = new List<Cte0Row0>();
                    var cte0CurrentFrontier = new List<Cte0Row0>();
                    var cte0NextFrontier = new List<Cte0Row0>();
                    int __cte0Iteration = 0;
                    int __cte0CancellationCounter = 0;
                    seedValuesD6F9BDFERow0[] cte0CurrentFrontier_seedRows = new seedValuesD6F9BDFERow0[]
                    {
                        new seedValuesD6F9BDFERow0(1)
                    };
                    foreach (var seed in cte0CurrentFrontier_seedRows)
                    {
                        token.ThrowIfCancellationRequested();
                        if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                        {
                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("labels", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                        }

                        cte0CurrentFrontier.Add(new Cte0Row0(1, 0, "one"));
                    }

                    cte0.AddRange(cte0CurrentFrontier);
                    while (cte0CurrentFrontier.Count > 0)
                    {
                        if ((__cte0Iteration & 63) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        if (__cte0Iteration >= 1000)
                        {
                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("labels", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 1000);
                        }

                        __cte0Iteration++;
                        cte0NextFrontier.Clear();
                        for (int cte0CurrentFrontierIndex = 0; cte0CurrentFrontierIndex < cte0CurrentFrontier.Count; ++cte0CurrentFrontierIndex)
                        {
                            if (cte0CurrentFrontierIndex != 0 && (cte0CurrentFrontierIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Cte0Row0 l = (Cte0Row0)cte0CurrentFrontier[cte0CurrentFrontierIndex];
                            if ((l.Depth < 2))
                            {
                                ++__cte0CancellationCounter;
                                if ((__cte0CancellationCounter & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                if (cte0.Count + cte0NextFrontier.Count >= 10000000)
                                {
                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("labels", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                }

                                cte0NextFrontier.Add(new Cte0Row0((l.Value + 1), (l.Depth + 1), (l.Value == 1) ? (string)"even" : (string)"odd"));
                            }
                        }

                        cte0.AddRange(cte0NextFrontier);
                        var __cte0FrontierSwap = cte0CurrentFrontier;
                        cte0CurrentFrontier = cte0NextFrontier;
                        cte0NextFrontier = __cte0FrontierSwap;
                    }

                    OnPhaseChanged("compiled:cte0", QueryPhase.Where);
                    OnPhaseChanged("compiled:cte0", QueryPhase.Select);
                    _cteRowResults.Slot0 = cte0;
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
                }

                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.From);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 labels = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(labels.Value, labels.Depth, labels.Label));
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Depth.CompareTo(right.Depth);
                    if (comparison != 0)
                        return comparison;
                    return 0;
                }));
                foreach (var resultSortedRowsRow in resultSortedRows)
                {
                    __musoqFinalShapeRows.Add(resultSortedRowsRow);
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

        private readonly struct Cte0Row0
        {
            public Cte0Row0(int Value, int Depth, string Label)
            {
                this.Value = Value;
                this.Depth = Depth;
                this.Label = Label;
            }

            public int Value { get; }
            public int Depth { get; }
            public string Label { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, int __value1, string __value2)
            {
                Value = __value0;
                Depth = __value1;
                Label = __value2;
            }

            public override int Count => 3;
            public int Depth { get; private set; }
            public string Label { get; private set; }
            public int Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Value = (int)value;
                        break;
                    case 1:
                        Depth = (int)value;
                        break;
                    case 2:
                        Label = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Value" => true,
                "Depth" => true,
                "Label" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Value,
                1 => (object)Depth,
                2 => (object)Label,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Value" => (object)Value,
                "Depth" => (object)Depth,
                "Label" => (object)Label,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Value, int Depth, string Label)
            {
                this.Value = Value;
                this.Depth = Depth;
                this.Label = Label;
            }

            public int Depth { get; }
            public string Label { get; }
            public int Value { get; }
        }

        private sealed class seedValuesD6F9BDFERow0 : Row
        {
            public seedValuesD6F9BDFERow0(int __value0)
            {
                Seed = __value0;
            }

            public override int Count => 1;
            public int Seed { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Seed = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Seed" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Seed,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Seed" => (object)Seed,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
