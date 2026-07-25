// === Parsed Query ===
/*
with recursive first (Value) as (select Value from values {{ Value: 1 }} seed union all select f.Value + 1 from first f where f.Value < 3), second (Value) as (select Value from first where Value = 2 union all select s.Value + 1 from second s where s.Value < 4) select Value from second order by Value
*/

// === Logical Plan ===
/*
Cte
  Definition [first]
    RecursiveCte [first] [All]
      Anchor
        MultiStatement
          Project [seed.Value as Value]
            ValuesScan [1 rows as seed]
      RecursiveMember
        MultiStatement
          Project [(f.Value + 1) as f.Value + 1]
            Filter [(f.Value < 3)]
              CteRef [first as f]
  Definition [second]
    RecursiveCte [second] [All]
      Anchor
        MultiStatement
          Project [first.Value as Value]
            Filter [(first.Value = 2)]
              CteRef [first as first]
      RecursiveMember
        MultiStatement
          Project [(s.Value + 1) as s.Value + 1]
            Filter [(s.Value < 4)]
              CteRef [second as s]
  Query
    MultiStatement
      Sort [second.Value]
        Project [second.Value as Value]
          CteRef [second as second]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [first]
    PhysicalRecursiveCte [first] [All]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [seed.Value as Value]
            PhysicalValuesScan [1 rows as seed]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [(f.Value + 1) as f.Value + 1]
            PhysicalFilter [(f.Value < 3)]
              PhysicalCteRef [first as f]
  Definition [second]
    PhysicalRecursiveCte [second] [All]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [first.Value as Value]
            PhysicalFilter [(first.Value = 2)]
              PhysicalCteRef [first as first]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [(s.Value + 1) as s.Value + 1]
            PhysicalFilter [(s.Value < 4)]
              PhysicalCteRef [second as s]
  Query
    PhysicalMultiStatement
      PhysicalSort [second.Value]
        PhysicalProject [second.Value as Value]
          PhysicalCteRef [second as second]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Value: int <- field Value
    Generated [Cte0Row0]
      Value: int <- field Value
    TableRow [f]
      Value: int <- field Value
    TableRow [first]
      Value: int <- field Value
    Generated [Cte1Row0]
      Value: int <- field Value
    TableRow [s]
      Value: int <- field Value
    TableRow [second]
      Value: int <- field Value
    Generated [ResultRow0]
      Value: int <- field Value

  Body
    RecursiveCte [first; result cte0; frontiers cte0CurrentFrontier, cte0NextFrontier; identity none; max iterations 1000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        CreateValuesRows [cte0CurrentFrontier_seedRows: seedValuesA380A8DARow0 x 1]
        ForEach [seed in cte0CurrentFrontier_seedRows]
          RecursiveAppend [cte0CurrentFrontier <- Cte0Row0(Value: seed.Value); identity none; guard cte0.Count + cte0CurrentFrontier.Count < 10000000]
      RecursiveMember
        ForEach [f in cte0CurrentFrontier]
          If [(f.Value < 3)]
            RecursiveAppend [cte0NextFrontier <- Cte0Row0(Value: (f.Value + 1)); identity none; guard cte0.Count + cte0NextFrontier.Count < 10000000]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    RecursiveCte [second; result cte1; frontiers cte1CurrentFrontier, cte1NextFrontier; identity none; max iterations 1000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        ForEach [first in _cteRowResults.Slot0]
          If [(first.Value = 2)]
            RecursiveAppend [cte1CurrentFrontier <- Cte1Row0(Value: first.Value); identity none; guard cte1.Count + cte1CurrentFrontier.Count < 10000000]
      RecursiveMember
        ForEach [s in cte1CurrentFrontier]
          If [(s.Value < 4)]
            RecursiveAppend [cte1NextFrontier <- Cte1Row0(Value: (s.Value + 1)); identity none; guard cte1.Count + cte1NextFrontier.Count < 10000000]
    StoreTable [cte1 -> _cteRowResults.Slot1: List<Cte1Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [second in _cteRowResults.Slot1]
      AppendShape [result <- ResultShape0(Value: second.Value)]
    SortShapeRows [result -> resultSorted by Value ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q208_RecursiveDependsOnEarlierRecursive
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_0 = new Column[]
        {
            new Column("Value", typeof(int), 0)
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
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_0, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Value);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.Where);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                var cte0 = new List<Cte0Row0>();
                var cte0CurrentFrontier = new List<Cte0Row0>();
                var cte0NextFrontier = new List<Cte0Row0>();
                int __cte0Iteration = 0;
                int __cte0CancellationCounter = 0;
                seedValuesA380A8DARow0[] cte0CurrentFrontier_seedRows = new seedValuesA380A8DARow0[]
                {
                    new seedValuesA380A8DARow0(1)
                };
                foreach (var seed in cte0CurrentFrontier_seedRows)
                {
                    token.ThrowIfCancellationRequested();
                    if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                    {
                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("first", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                    }

                    cte0CurrentFrontier.Add(new Cte0Row0(seed.Value));
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
                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("first", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 1000);
                    }

                    __cte0Iteration++;
                    cte0NextFrontier.Clear();
                    for (int cte0CurrentFrontierIndex = 0; cte0CurrentFrontierIndex < cte0CurrentFrontier.Count; ++cte0CurrentFrontierIndex)
                    {
                        if (cte0CurrentFrontierIndex != 0 && (cte0CurrentFrontierIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        Cte0Row0 f = (Cte0Row0)cte0CurrentFrontier[cte0CurrentFrontierIndex];
                        if ((f.Value < 3))
                        {
                            ++__cte0CancellationCounter;
                            if ((__cte0CancellationCounter & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            if (cte0.Count + cte0NextFrontier.Count >= 10000000)
                            {
                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("first", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                            }

                            cte0NextFrontier.Add(new Cte0Row0((f.Value + 1)));
                        }
                    }

                    cte0.AddRange(cte0NextFrontier);
                    var __cte0FrontierSwap = cte0CurrentFrontier;
                    cte0CurrentFrontier = cte0NextFrontier;
                    cte0NextFrontier = __cte0FrontierSwap;
                }

                _cteRowResults.Slot0 = cte0;
                var cte1 = new List<Cte1Row0>();
                var cte1CurrentFrontier = new List<Cte1Row0>();
                var cte1NextFrontier = new List<Cte1Row0>();
                int __cte1Iteration = 0;
                int __cte1CancellationCounter = 0;
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 first = __storedTable0Rows[__storedTable0Index];
                    if ((first.Value == 2))
                    {
                        if (cte1.Count + cte1CurrentFrontier.Count >= 10000000)
                        {
                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("second", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                        }

                        cte1CurrentFrontier.Add(new Cte1Row0(first.Value));
                    }
                }

                cte1.AddRange(cte1CurrentFrontier);
                while (cte1CurrentFrontier.Count > 0)
                {
                    if ((__cte1Iteration & 63) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    if (__cte1Iteration >= 1000)
                    {
                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("second", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 1000);
                    }

                    __cte1Iteration++;
                    cte1NextFrontier.Clear();
                    for (int cte1CurrentFrontierIndex = 0; cte1CurrentFrontierIndex < cte1CurrentFrontier.Count; ++cte1CurrentFrontierIndex)
                    {
                        if (cte1CurrentFrontierIndex != 0 && (cte1CurrentFrontierIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        Cte1Row0 s = (Cte1Row0)cte1CurrentFrontier[cte1CurrentFrontierIndex];
                        if ((s.Value < 4))
                        {
                            ++__cte1CancellationCounter;
                            if ((__cte1CancellationCounter & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            if (cte1.Count + cte1NextFrontier.Count >= 10000000)
                            {
                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("second", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                            }

                            cte1NextFrontier.Add(new Cte1Row0((s.Value + 1)));
                        }
                    }

                    cte1.AddRange(cte1NextFrontier);
                    var __cte1FrontierSwap = cte1CurrentFrontier;
                    cte1CurrentFrontier = cte1NextFrontier;
                    cte1NextFrontier = __cte1FrontierSwap;
                }

                _cteRowResults.Slot1 = cte1;
                var result = new List<ResultShape0>();
                var __storedTable1Rows = _cteRowResults.Slot1;
                for (int __storedTable1Index = 0; __storedTable1Index < __storedTable1Rows.Count; ++__storedTable1Index)
                {
                    if ((__storedTable1Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte1Row0 second = __storedTable1Rows[__storedTable1Index];
                    result.Add(new ResultShape0(second.Value));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Value.CompareTo(right.Value);
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
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
                OnPhaseChanged("compiled", QueryPhase.End);
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
            public Cte0Row0(int Value)
            {
                this.Value = Value;
            }

            public int Value { get; }
        }

        private readonly struct Cte1Row0
        {
            public Cte1Row0(int Value)
            {
                this.Value = Value;
            }

            public int Value { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
            public List<Cte1Row0> Slot1;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0)
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

        private sealed class ResultShape0
        {
            public ResultShape0(int Value)
            {
                this.Value = Value;
            }

            public int Value { get; }
        }

        private sealed class seedValuesA380A8DARow0 : Row
        {
            public seedValuesA380A8DARow0(int __value0)
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
