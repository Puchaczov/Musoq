// === Parsed Query ===
/*
with recursive up (Value) as (select Value from values {{ Value: 1 }} seed union all select u.Value + 1 from up u where u.Value < 3), down (Value) as (select Value from values {{ Value: 5 }} seed union all select d.Value - 1 from down d where d.Value > 3) select u.Value as Up, d.Value as Down from up u inner join down d on u.Value + d.Value = 6
*/

// === Logical Plan ===
/*
Cte
  Definition [up]
    RecursiveCte [up] [All]
      Anchor
        MultiStatement
          Project [seed.Value as Value]
            ValuesScan [1 rows as seed]
      RecursiveMember
        MultiStatement
          Project [(u.Value + 1) as u.Value + 1]
            Filter [(u.Value < 3)]
              CteRef [up as u]
  Definition [down]
    RecursiveCte [down] [All]
      Anchor
        MultiStatement
          Project [seed.Value as Value]
            ValuesScan [1 rows as seed]
      RecursiveMember
        MultiStatement
          Project [(d.Value - 1) as d.Value - 1]
            Filter [(d.Value > 3)]
              CteRef [down as d]
  Query
    MultiStatement
      Project [u.Value as u.Value, d.Value as d.Value]
        Join [Inner] [((u.Value + d.Value) = 6)]
          CteRef [up as u]
          CteRef [down as d]
      Project [u.Value as Up, d.Value as Down]
        CteRef [ud as ud]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [up]
    PhysicalRecursiveCte [up] [All]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [seed.Value as Value]
            PhysicalValuesScan [1 rows as seed]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [(u.Value + 1) as u.Value + 1]
            PhysicalFilter [(u.Value < 3)]
              PhysicalCteRef [up as u]
  Definition [down]
    PhysicalRecursiveCte [down] [All]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [seed.Value as Value]
            PhysicalValuesScan [1 rows as seed]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [(d.Value - 1) as d.Value - 1]
            PhysicalFilter [(d.Value > 3)]
              PhysicalCteRef [down as d]
  Query
    PhysicalMultiStatement
      PhysicalProject [u.Value as u.Value, d.Value as d.Value]
        PhysicalNestedLoopJoin [Inner] [((u.Value + d.Value) = 6)]
          PhysicalCteRef [up as u]
          PhysicalCteRef [down as d]
      PhysicalProject [u.Value as Up, d.Value as Down]
        PhysicalCteRef [ud as ud]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Value: int <- field Value
    Generated [Cte0Row0]
      Value: int <- field Value
    TableRow [u]
      Value: int <- field Value
    UnknownShape [ValuesRowShape]
      Value: int <- field Value
    Generated [Cte1Row0]
      Value: int <- field Value
    TableRow [d]
      Value: int <- field Value
    TableRow [u]
      Value: int <- field Value
    TableRow [d]
      Value: int <- field Value
    Generated [ResultRow0]
      Up: int <- field Up
      Down: int <- field Down

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    RecursiveCte [up; result cte0; frontiers cte0CurrentFrontier, cte0NextFrontier; identity none; max iterations 1000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        CreateValuesRows [cte0CurrentFrontier_seedRows: seedValuesA380A8DARow0 x 1]
        ForEach [seed in cte0CurrentFrontier_seedRows]
          RecursiveAppend [cte0CurrentFrontier <- Cte0Row0(Value: seed.Value); identity none; guard cte0.Count + cte0CurrentFrontier.Count < 10000000]
      RecursiveMember
        ForEach [u in cte0CurrentFrontier]
          If [(u.Value < 3)]
            RecursiveAppend [cte0NextFrontier <- Cte0Row0(Value: (u.Value + 1)); identity none; guard cte0.Count + cte0NextFrontier.Count < 10000000]
    PhaseBoundary [Where:cte0]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Begin:cte1]
    PhaseBoundary [From:cte1]
    RecursiveCte [down; result cte1; frontiers cte1CurrentFrontier, cte1NextFrontier; identity none; max iterations 1000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        CreateValuesRows [cte1CurrentFrontier_seedRows: seedValuesA380A8DARow0 x 1]
        ForEach [seed in cte1CurrentFrontier_seedRows]
          RecursiveAppend [cte1CurrentFrontier <- Cte1Row0(Value: seed.Value); identity none; guard cte1.Count + cte1CurrentFrontier.Count < 10000000]
      RecursiveMember
        ForEach [d in cte1CurrentFrontier]
          If [(d.Value > 3)]
            RecursiveAppend [cte1NextFrontier <- Cte1Row0(Value: (d.Value - 1)); identity none; guard cte1.Count + cte1NextFrontier.Count < 10000000]
    PhaseBoundary [Where:cte1]
    PhaseBoundary [Select:cte1]
    StoreTable [cte1 -> _cteRowResults.Slot1: List<Cte1Row0>]
    PhaseBoundary [End:cte1]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte2]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [u in _cteRowResults.Slot0]
      ForEach [d in _cteRowResults.Slot1]
        Let [value: int = u.Value]
        Let [value1: int = d.Value]
        If [((value + value1) = 6)]
          AppendShape [result <- ResultShape0(Up: value, Down: value1)]
    PhaseBoundary [End:cte2]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q207_RecursiveTwoIndependentCtes
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
            new Column("Up", typeof(int), 0),
            new Column("Down", typeof(int), 1)
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_0, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Up, __musoqShapeRow.Down);
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
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.From);
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
                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("up", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
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
                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("up", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 1000);
                        }

                        __cte0Iteration++;
                        cte0NextFrontier.Clear();
                        for (int cte0CurrentFrontierIndex = 0; cte0CurrentFrontierIndex < cte0CurrentFrontier.Count; ++cte0CurrentFrontierIndex)
                        {
                            if (cte0CurrentFrontierIndex != 0 && (cte0CurrentFrontierIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Cte0Row0 u = (Cte0Row0)cte0CurrentFrontier[cte0CurrentFrontierIndex];
                            if ((u.Value < 3))
                            {
                                ++__cte0CancellationCounter;
                                if ((__cte0CancellationCounter & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                if (cte0.Count + cte0NextFrontier.Count >= 10000000)
                                {
                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("up", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                }

                                cte0NextFrontier.Add(new Cte0Row0((u.Value + 1)));
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

                OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled:cte1", QueryPhase.From);
                    var cte1 = new List<Cte1Row0>();
                    var cte1CurrentFrontier = new List<Cte1Row0>();
                    var cte1NextFrontier = new List<Cte1Row0>();
                    int __cte1Iteration = 0;
                    int __cte1CancellationCounter = 0;
                    seedValuesA380A8DARow0[] cte1CurrentFrontier_seedRows = new seedValuesA380A8DARow0[]
                    {
                        new seedValuesA380A8DARow0(5)
                    };
                    foreach (var seed in cte1CurrentFrontier_seedRows)
                    {
                        token.ThrowIfCancellationRequested();
                        if (cte1.Count + cte1CurrentFrontier.Count >= 10000000)
                        {
                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("down", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                        }

                        cte1CurrentFrontier.Add(new Cte1Row0(seed.Value));
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
                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("down", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 1000);
                        }

                        __cte1Iteration++;
                        cte1NextFrontier.Clear();
                        for (int cte1CurrentFrontierIndex = 0; cte1CurrentFrontierIndex < cte1CurrentFrontier.Count; ++cte1CurrentFrontierIndex)
                        {
                            if (cte1CurrentFrontierIndex != 0 && (cte1CurrentFrontierIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Cte1Row0 d = (Cte1Row0)cte1CurrentFrontier[cte1CurrentFrontierIndex];
                            if ((d.Value > 3))
                            {
                                ++__cte1CancellationCounter;
                                if ((__cte1CancellationCounter & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                if (cte1.Count + cte1NextFrontier.Count >= 10000000)
                                {
                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("down", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                }

                                cte1NextFrontier.Add(new Cte1Row0((d.Value - 1)));
                            }
                        }

                        cte1.AddRange(cte1NextFrontier);
                        var __cte1FrontierSwap = cte1CurrentFrontier;
                        cte1CurrentFrontier = cte1NextFrontier;
                        cte1NextFrontier = __cte1FrontierSwap;
                    }

                    OnPhaseChanged("compiled:cte1", QueryPhase.Where);
                    OnPhaseChanged("compiled:cte1", QueryPhase.Select);
                    _cteRowResults.Slot1 = cte1;
                }
                finally
                {
                    OnPhaseChanged("compiled:cte1", QueryPhase.End);
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte2", QueryPhase.Begin);
                try
                {
                    var __storedTable0Rows = _cteRowResults.Slot0;
                    for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                    {
                        if ((__storedTable0Index & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        Cte0Row0 u = __storedTable0Rows[__storedTable0Index];
                        var __storedTable1Rows = _cteRowResults.Slot1;
                        for (int __storedTable1Index = 0; __storedTable1Index < __storedTable1Rows.Count; ++__storedTable1Index)
                        {
                            if ((__storedTable1Index & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Cte1Row0 d = __storedTable1Rows[__storedTable1Index];
                            int value = u.Value;
                            int value1 = d.Value;
                            if (((value + value1) == 6))
                            {
                                __musoqFinalShapeRows.Add(new ResultShape0(value, value1));
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
            public ResultRow0(int __value0, int __value1)
            {
                Up = __value0;
                Down = __value1;
            }

            public override int Count => 2;
            public int Down { get; private set; }
            public int Up { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Up = (int)value;
                        break;
                    case 1:
                        Down = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Up" => true,
                "Down" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Up,
                1 => (object)Down,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Up" => (object)Up,
                "Down" => (object)Down,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Up, int Down)
            {
                this.Up = Up;
                this.Down = Down;
            }

            public int Down { get; }
            public int Up { get; }
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
