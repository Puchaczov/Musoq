// === Parsed Query ===
/*
with recursive states (Id, ParentId, Depth) as (select Id, ParentId, 0 from values {{ Id: 1, ParentId: null }, { Id: 0, ParentId: 1 }} seed where Id = 1 union (Id) select s.Id + 1, case when s.Id < 0 then null else s.Id end, s.Depth + 1 from states s where s.Depth < 2) select Id, ParentId, Depth from states order by Id
*/

// === Logical Plan ===
/*
Cte
  Definition [states]
    RecursiveCte [states] [Keyed: Id]
      Anchor
        MultiStatement
          Project [seed.Id as Id, seed.ParentId as ParentId, 0 as Depth]
            Filter [(seed.Id = 1)]
              ValuesScan [2 rows as seed]
      RecursiveMember
        MultiStatement
          Project [(s.Id + 1) as s.Id + 1, CASE WHEN (s.Id < 0) THEN NULL ELSE s.Id END as case when s.Id < 0 then null else s.Id end, (s.Depth + 1) as s.Depth + 1]
            Filter [(s.Depth < 2)]
              CteRef [states as s]
  Query
    MultiStatement
      Sort [states.Id]
        Project [states.Id as Id, states.ParentId as ParentId, states.Depth as Depth]
          CteRef [states as states]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [states]
    PhysicalRecursiveCte [states] [Keyed: Id]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [seed.Id as Id, seed.ParentId as ParentId, 0 as Depth]
            PhysicalFilter [(seed.Id = 1)]
              PhysicalValuesScan [2 rows as seed]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [(s.Id + 1) as s.Id + 1, CASE WHEN (s.Id < 0) THEN NULL ELSE s.Id END as case when s.Id < 0 then null else s.Id end, (s.Depth + 1) as s.Depth + 1]
            PhysicalFilter [(s.Depth < 2)]
              PhysicalCteRef [states as s]
  Query
    PhysicalMultiStatement
      PhysicalSort [states.Id]
        PhysicalProject [states.Id as Id, states.ParentId as ParentId, states.Depth as Depth]
          PhysicalCteRef [states as states]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Id: int <- field Id
      ParentId: int? <- field ParentId
    Generated [Cte0Row0]
      Id: int <- field Id
      ParentId: int? <- field ParentId
      Depth: int <- field Depth
    TableRow [s]
      Id: int <- field Id
      ParentId: int? <- field ParentId
      Depth: int <- field Depth
    TableRow [states]
      Id: int <- field Id
      ParentId: int? <- field ParentId
      Depth: int <- field Depth
    Generated [ResultRow0]
      Id: int <- field Id
      ParentId: int? <- field ParentId
      Depth: int <- field Depth

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    RecursiveCte [states; result cte0; frontiers cte0CurrentFrontier, cte0NextFrontier; identity Keyed via cte0Seen (Id); max iterations 1000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        CreateValuesRows [cte0CurrentFrontier_seedRows: seedValues1FDF96BFRow0 x 2]
        ForEach [seed in cte0CurrentFrontier_seedRows]
          If [(seed.Id = 1)]
            RecursiveAppend [cte0CurrentFrontier <- Cte0Row0(Id: seed.Id, ParentId: seed.ParentId, Depth: 0); identity cte0Seen (Id); guard cte0.Count + cte0CurrentFrontier.Count < 10000000]
      RecursiveMember
        ForEach [s in cte0CurrentFrontier]
          If [(s.Depth < 2)]
            Let [id: int = s.Id]
            RecursiveAppend [cte0NextFrontier <- Cte0Row0(Id: (id + 1), ParentId: CASE WHEN (id < 0) THEN NULL ELSE id END, Depth: (s.Depth + 1)); identity cte0Seen (Id); guard cte0.Count + cte0NextFrontier.Count < 10000000]
    PhaseBoundary [Where:cte0]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [From]
    ForEach [states in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Id: states.Id, ParentId: states.ParentId, Depth: states.Depth)]
    PhaseBoundary [Select]
    SortShapeRows [result -> resultSorted by Id ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q215_RecursiveNullableColumns
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
            new Column("ParentId", typeof(int?), 1),
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
                yield return new ResultRow0(__musoqShapeRow.Id, __musoqShapeRow.ParentId, __musoqShapeRow.Depth);
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
                    var cte0Seen = new HashSet<int>();
                    int __cte0Iteration = 0;
                    int __cte0CancellationCounter = 0;
                    seedValues1FDF96BFRow0[] cte0CurrentFrontier_seedRows = new seedValues1FDF96BFRow0[]
                    {
                        new seedValues1FDF96BFRow0(1, null),
                        new seedValues1FDF96BFRow0(0, 1)
                    };
                    foreach (var seed in cte0CurrentFrontier_seedRows)
                    {
                        token.ThrowIfCancellationRequested();
                        if ((seed.Id == 1))
                        {
                            var __cte0CurrentFrontierCandidate0 = seed.Id;
                            var __cte0CurrentFrontierCandidate1 = seed.ParentId;
                            var __cte0CurrentFrontierCandidate2 = 0;
                            if (cte0Seen.Add(__cte0CurrentFrontierCandidate0))
                            {
                                if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                {
                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("states", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                }

                                cte0CurrentFrontier.Add(new Cte0Row0(__cte0CurrentFrontierCandidate0, __cte0CurrentFrontierCandidate1, __cte0CurrentFrontierCandidate2));
                            }
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
                            if ((s.Depth < 2))
                            {
                                int id = s.Id;
                                ++__cte0CancellationCounter;
                                if ((__cte0CancellationCounter & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var __cte0NextFrontierCandidate0 = (id + 1);
                                var __cte0NextFrontierCandidate1 = ((id < 0) ? (int?)null : (int?)id);
                                var __cte0NextFrontierCandidate2 = (s.Depth + 1);
                                if (cte0Seen.Add(__cte0NextFrontierCandidate0))
                                {
                                    if (cte0.Count + cte0NextFrontier.Count >= 10000000)
                                    {
                                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("states", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                    }

                                    cte0NextFrontier.Add(new Cte0Row0(__cte0NextFrontierCandidate0, __cte0NextFrontierCandidate1, __cte0NextFrontierCandidate2));
                                }
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

                    Cte0Row0 states = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(states.Id, states.ParentId, states.Depth));
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
            public Cte0Row0(int Id, int? ParentId, int Depth)
            {
                this.Id = Id;
                this.ParentId = ParentId;
                this.Depth = Depth;
            }

            public int Id { get; }
            public int? ParentId { get; }
            public int Depth { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, int? __value1, int __value2)
            {
                Id = __value0;
                ParentId = __value1;
                Depth = __value2;
            }

            public override int Count => 3;
            public int Depth { get; private set; }
            public int Id { get; private set; }
            public int? ParentId { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Id = (int)value;
                        break;
                    case 1:
                        ParentId = (int?)value;
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
                "ParentId" => true,
                "Depth" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Id,
                1 => (object)ParentId,
                2 => (object)Depth,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Id" => (object)Id,
                "ParentId" => (object)ParentId,
                "Depth" => (object)Depth,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id, int? ParentId, int Depth)
            {
                this.Id = Id;
                this.ParentId = ParentId;
                this.Depth = Depth;
            }

            public int Depth { get; }
            public int Id { get; }
            public int? ParentId { get; }
        }

        private sealed class seedValues1FDF96BFRow0 : Row
        {
            public seedValues1FDF96BFRow0(int __value0, int? __value1)
            {
                Id = __value0;
                ParentId = __value1;
            }

            public override int Count => 2;
            public int Id { get; private set; }
            public int? ParentId { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Id = (int)value;
                        break;
                    case 1:
                        ParentId = (int?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "ParentId" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Id,
                1 => (object)ParentId,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Id" => (object)Id,
                "ParentId" => (object)ParentId,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
