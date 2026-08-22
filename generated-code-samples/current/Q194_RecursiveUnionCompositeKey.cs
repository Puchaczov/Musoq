// === Parsed Query ===
/*
with recursive states (Id, Branch, Depth) as (select Id, Branch, 0 from values {{ Id: 1, Branch: 'A' }} seed union (Id, Branch) select (case when s.Id = 1 then 2 else 1 end), s.Branch, s.Depth + 1 from states s) select Id, Branch, Depth from states order by Id
*/

// === Logical Plan ===
/*
Cte
  Definition [states]
    RecursiveCte [states] [Keyed: Id, Branch]
      Anchor
        MultiStatement
          Project [seed.Id as Id, seed.Branch as Branch, 0 as Depth]
            ValuesScan [1 rows as seed]
      RecursiveMember
        MultiStatement
          Project [CASE WHEN (s.Id = 1) THEN 2 ELSE 1 END as case when s.Id = 1 then 2 else 1 end, s.Branch as s.Branch, (s.Depth + 1) as s.Depth + 1]
            CteRef [states as s]
  Query
    MultiStatement
      Sort [states.Id]
        Project [states.Id as Id, states.Branch as Branch, states.Depth as Depth]
          CteRef [states as states]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [states]
    PhysicalRecursiveCte [states] [Keyed: Id, Branch]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [seed.Id as Id, seed.Branch as Branch, 0 as Depth]
            PhysicalValuesScan [1 rows as seed]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [CASE WHEN (s.Id = 1) THEN 2 ELSE 1 END as case when s.Id = 1 then 2 else 1 end, s.Branch as s.Branch, (s.Depth + 1) as s.Depth + 1]
            PhysicalCteRef [states as s]
  Query
    PhysicalMultiStatement
      PhysicalSort [states.Id]
        PhysicalProject [states.Id as Id, states.Branch as Branch, states.Depth as Depth]
          PhysicalCteRef [states as states]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Id: int <- field Id
      Branch: string <- field Branch
    Generated [Cte0Row0]
      Id: int <- field Id
      Branch: string <- field Branch
      Depth: int <- field Depth
    TableRow [s]
      Id: int <- field Id
      Branch: string <- field Branch
      Depth: int <- field Depth
    TableRow [states]
      Id: int <- field Id
      Branch: string <- field Branch
      Depth: int <- field Depth
    Generated [ResultRow0]
      Id: int <- field Id
      Branch: string <- field Branch
      Depth: int <- field Depth

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    RecursiveCte [states; result cte0; frontiers cte0CurrentFrontier, cte0NextFrontier; identity Keyed via cte0Seen (Id, Branch); max iterations 1000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        CreateValuesRows [cte0CurrentFrontier_seedRows: seedValues0B641ACERow0 x 1]
        ForEach [seed in cte0CurrentFrontier_seedRows]
          RecursiveAppend [cte0CurrentFrontier <- Cte0Row0(Id: seed.Id, Branch: seed.Branch, Depth: 0); identity cte0Seen (Id, Branch); guard cte0.Count + cte0CurrentFrontier.Count < 10000000]
      RecursiveMember
        ForEach [s in cte0CurrentFrontier]
          RecursiveAppend [cte0NextFrontier <- Cte0Row0(Id: CASE WHEN (s.Id = 1) THEN 2 ELSE 1 END, Branch: s.Branch, Depth: (s.Depth + 1)); identity cte0Seen (Id, Branch); guard cte0.Count + cte0NextFrontier.Count < 10000000]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [From]
    ForEach [states in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Id: states.Id, Branch: states.Branch, Depth: states.Depth)]
    PhaseBoundary [Select]
    SortShapeRows [result -> resultSorted by Id ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q194_RecursiveUnionCompositeKey
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
            new Column("Id", typeof(int), 0),
            new Column("Branch", typeof(string), 1),
            new Column("Depth", typeof(int), 2)
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
                yield return new ResultRow0(__musoqShapeRow.Id, __musoqShapeRow.Branch, __musoqShapeRow.Depth);
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
                    var cte0Seen = new HashSet<ValueTuple<int, string>>();
                    int __cte0Iteration = 0;
                    int __cte0CancellationCounter = 0;
                    seedValues0B641ACERow0[] cte0CurrentFrontier_seedRows = new seedValues0B641ACERow0[]
                    {
                        new seedValues0B641ACERow0(1, "A")
                    };
                    foreach (var seed in cte0CurrentFrontier_seedRows)
                    {
                        token.ThrowIfCancellationRequested();
                        var __cte0CurrentFrontierCandidate0 = seed.Id;
                        var __cte0CurrentFrontierCandidate1 = seed.Branch;
                        var __cte0CurrentFrontierCandidate2 = 0;
                        if (cte0Seen.Add((__cte0CurrentFrontierCandidate0, __cte0CurrentFrontierCandidate1)))
                        {
                            if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                            {
                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("states", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                            }

                            cte0CurrentFrontier.Add(new Cte0Row0(__cte0CurrentFrontierCandidate0, __cte0CurrentFrontierCandidate1, __cte0CurrentFrontierCandidate2));
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
                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("states", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 1000);
                        }

                        __cte0Iteration++;
                        cte0NextFrontier.Clear();
                        for (int cte0CurrentFrontierIndex = 0; cte0CurrentFrontierIndex < cte0CurrentFrontier.Count; ++cte0CurrentFrontierIndex)
                        {
                            if (cte0CurrentFrontierIndex != 0 && (cte0CurrentFrontierIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Cte0Row0 s = (Cte0Row0)cte0CurrentFrontier[cte0CurrentFrontierIndex];
                            ++__cte0CancellationCounter;
                            if ((__cte0CancellationCounter & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var __cte0NextFrontierCandidate0 = ((s.Id == 1) ? (int)2 : (int)1);
                            var __cte0NextFrontierCandidate1 = s.Branch;
                            var __cte0NextFrontierCandidate2 = (s.Depth + 1);
                            if (cte0Seen.Add((__cte0NextFrontierCandidate0, __cte0NextFrontierCandidate1)))
                            {
                                if (cte0.Count + cte0NextFrontier.Count >= 10000000)
                                {
                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("states", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                }

                                cte0NextFrontier.Add(new Cte0Row0(__cte0NextFrontierCandidate0, __cte0NextFrontierCandidate1, __cte0NextFrontierCandidate2));
                            }
                        }

                        cte0.AddRange(cte0NextFrontier);
                        var __cte0FrontierSwap = cte0CurrentFrontier;
                        cte0CurrentFrontier = cte0NextFrontier;
                        cte0NextFrontier = __cte0FrontierSwap;
                    }

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

                    Cte0Row0 states = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(states.Id, states.Branch, states.Depth));
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
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
            public Cte0Row0(int Id, string Branch, int Depth)
            {
                this.Id = Id;
                this.Branch = Branch;
                this.Depth = Depth;
            }

            public int Id { get; }
            public string Branch { get; }
            public int Depth { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, string __value1, int __value2)
            {
                Id = __value0;
                Branch = __value1;
                Depth = __value2;
            }

            public string Branch { get; private set; }
            public override int Count => 3;
            public int Depth { get; private set; }
            public int Id { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Id = (int)value;
                        break;
                    case 1:
                        Branch = (string)value;
                        break;
                    case 2:
                        Depth = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Branch" => true,
                "Depth" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Id,
                1 => (object)Branch,
                2 => (object)Depth,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Id" => (object)Id,
                "Branch" => (object)Branch,
                "Depth" => (object)Depth,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id, string Branch, int Depth)
            {
                this.Id = Id;
                this.Branch = Branch;
                this.Depth = Depth;
            }

            public string Branch { get; }
            public int Depth { get; }
            public int Id { get; }
        }

        private sealed class seedValues0B641ACERow0 : Row
        {
            public seedValues0B641ACERow0(int __value0, string __value1)
            {
                Id = __value0;
                Branch = __value1;
            }

            public string Branch { get; private set; }
            public override int Count => 2;
            public int Id { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Id = (int)value;
                        break;
                    case 1:
                        Branch = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Branch" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Id,
                1 => (object)Branch,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Id" => (object)Id,
                "Branch" => (object)Branch,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
