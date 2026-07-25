// === Parsed Query ===
/*
with recursive cycle (Id) as (select Id from values {{ Id: 1 }} seed union select (case when c.Id = 1 then 2 else 1 end) from cycle c) select Id from cycle order by Id
*/

// === Logical Plan ===
/*
Cte
  Definition [cycle]
    RecursiveCte [cycle] [FullRow]
      Anchor
        MultiStatement
          Project [seed.Id as Id]
            ValuesScan [1 rows as seed]
      RecursiveMember
        MultiStatement
          Project [CASE WHEN (c.Id = 1) THEN 2 ELSE 1 END as case when c.Id = 1 then 2 else 1 end]
            CteRef [cycle as c]
  Query
    MultiStatement
      Sort [cycle.Id]
        Project [cycle.Id as Id]
          CteRef [cycle as cycle]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [cycle]
    PhysicalRecursiveCte [cycle] [FullRow]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [seed.Id as Id]
            PhysicalValuesScan [1 rows as seed]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [CASE WHEN (c.Id = 1) THEN 2 ELSE 1 END as case when c.Id = 1 then 2 else 1 end]
            PhysicalCteRef [cycle as c]
  Query
    PhysicalMultiStatement
      PhysicalSort [cycle.Id]
        PhysicalProject [cycle.Id as Id]
          PhysicalCteRef [cycle as cycle]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Id: int <- field Id
    Generated [Cte0Row0]
      Id: int <- field Id
    TableRow [c]
      Id: int <- field Id
    TableRow [cycle]
      Id: int <- field Id
    Generated [ResultRow0]
      Id: int <- field Id

  Body
    RecursiveCte [cycle; result cte0; frontiers cte0CurrentFrontier, cte0NextFrontier; identity FullRow via cte0Seen (Id); max iterations 1000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        CreateValuesRows [cte0CurrentFrontier_seedRows: seedValues0C8F87F6Row0 x 1]
        ForEach [seed in cte0CurrentFrontier_seedRows]
          RecursiveAppend [cte0CurrentFrontier <- Cte0Row0(Id: seed.Id); identity cte0Seen (Id); guard cte0.Count + cte0CurrentFrontier.Count < 10000000]
      RecursiveMember
        ForEach [c in cte0CurrentFrontier]
          RecursiveAppend [cte0NextFrontier <- Cte0Row0(Id: CASE WHEN (c.Id = 1) THEN 2 ELSE 1 END); identity cte0Seen (Id); guard cte0.Count + cte0NextFrontier.Count < 10000000]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [cycle in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Id: cycle.Id)]
    SortShapeRows [result -> resultSorted by Id ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q192_RecursiveUnionFullRowCycle
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
            new Column("Id", typeof(int), 0)
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
                yield return new ResultRow0(__musoqShapeRow.Id);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
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
                var cte0Seen = new HashSet<int>();
                int __cte0Iteration = 0;
                int __cte0CancellationCounter = 0;
                seedValues0C8F87F6Row0[] cte0CurrentFrontier_seedRows = new seedValues0C8F87F6Row0[]
                {
                    new seedValues0C8F87F6Row0(1)
                };
                foreach (var seed in cte0CurrentFrontier_seedRows)
                {
                    token.ThrowIfCancellationRequested();
                    var __cte0CurrentFrontierCandidate0 = seed.Id;
                    if (cte0Seen.Add(__cte0CurrentFrontierCandidate0))
                    {
                        if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                        {
                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("cycle", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                        }

                        cte0CurrentFrontier.Add(new Cte0Row0(__cte0CurrentFrontierCandidate0));
                    }
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
                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("cycle", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 1000);
                    }

                    __cte0Iteration++;
                    cte0NextFrontier.Clear();
                    for (int cte0CurrentFrontierIndex = 0; cte0CurrentFrontierIndex < cte0CurrentFrontier.Count; ++cte0CurrentFrontierIndex)
                    {
                        if (cte0CurrentFrontierIndex != 0 && (cte0CurrentFrontierIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        Cte0Row0 c = (Cte0Row0)cte0CurrentFrontier[cte0CurrentFrontierIndex];
                        ++__cte0CancellationCounter;
                        if ((__cte0CancellationCounter & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var __cte0NextFrontierCandidate0 = ((c.Id == 1) ? (int)2 : (int)1);
                        if (cte0Seen.Add(__cte0NextFrontierCandidate0))
                        {
                            if (cte0.Count + cte0NextFrontier.Count >= 10000000)
                            {
                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("cycle", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                            }

                            cte0NextFrontier.Add(new Cte0Row0(__cte0NextFrontierCandidate0));
                        }
                    }

                    cte0.AddRange(cte0NextFrontier);
                    var __cte0FrontierSwap = cte0CurrentFrontier;
                    cte0CurrentFrontier = cte0NextFrontier;
                    cte0NextFrontier = __cte0FrontierSwap;
                }

                _cteRowResults.Slot0 = cte0;
                var result = new List<ResultShape0>();
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 cycle = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(cycle.Id));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
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
            public Cte0Row0(int Id)
            {
                this.Id = Id;
            }

            public int Id { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0)
            {
                Id = __value0;
            }

            public override int Count => 1;
            public int Id { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Id = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Id,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Id" => (object)Id,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id)
            {
                this.Id = Id;
            }

            public int Id { get; }
        }

        private sealed class seedValues0C8F87F6Row0 : Row
        {
            public seedValues0C8F87F6Row0(int __value0)
            {
                Id = __value0;
            }

            public override int Count => 1;
            public int Id { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Id = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Id,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Id" => (object)Id,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
