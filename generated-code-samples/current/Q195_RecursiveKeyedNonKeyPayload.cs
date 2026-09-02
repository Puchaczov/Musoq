// === Parsed Query ===
/*
with recursive paths (Id, Depth, Path) as (select Id, 0, '1' from values {{ Id: 1 }} seed union (Id) select (case when p.Id = 1 then 2 else 1 end), p.Depth + 1, p.Path + '->' + (case when p.Id = 1 then '2' else '1' end) from paths p) select Id, Depth, Path from paths order by Id
*/

// === Logical Plan ===
/*
Cte
  Definition [paths]
    RecursiveCte [paths] [Keyed: Id]
      Anchor
        MultiStatement
          Project [seed.Id as Id, 0 as Depth, '1' as Path]
            ValuesScan [1 rows as seed]
      RecursiveMember
        MultiStatement
          Project [CASE WHEN (p.Id = 1) THEN 2 ELSE 1 END as case when p.Id = 1 then 2 else 1 end, (p.Depth + 1) as p.Depth + 1, ((p.Path || '->') || CASE WHEN (p.Id = 1) THEN '2' ELSE '1' END) as p.Path + -> + case when p.Id = 1 then 2 else 1 end]
            CteRef [paths as p]
  Query
    MultiStatement
      Sort [paths.Id]
        Project [paths.Id as Id, paths.Depth as Depth, paths.Path as Path]
          CteRef [paths as paths]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [paths]
    PhysicalRecursiveCte [paths] [Keyed: Id]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [seed.Id as Id, 0 as Depth, '1' as Path]
            PhysicalValuesScan [1 rows as seed]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [CASE WHEN (p.Id = 1) THEN 2 ELSE 1 END as case when p.Id = 1 then 2 else 1 end, (p.Depth + 1) as p.Depth + 1, ((p.Path || '->') || CASE WHEN (p.Id = 1) THEN '2' ELSE '1' END) as p.Path + -> + case when p.Id = 1 then 2 else 1 end]
            PhysicalCteRef [paths as p]
  Query
    PhysicalMultiStatement
      PhysicalSort [paths.Id]
        PhysicalProject [paths.Id as Id, paths.Depth as Depth, paths.Path as Path]
          PhysicalCteRef [paths as paths]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Id: int <- field Id
    Generated [Cte0Row0]
      Id: int <- field Id
      Depth: int <- field Depth
      Path: string <- field Path
    TableRow [p]
      Id: int <- field Id
      Depth: int <- field Depth
      Path: string <- field Path
    TableRow [paths]
      Id: int <- field Id
      Depth: int <- field Depth
      Path: string <- field Path
    Generated [ResultRow0]
      Id: int <- field Id
      Depth: int <- field Depth
      Path: string <- field Path

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    RecursiveCte [paths; result cte0; frontiers cte0CurrentFrontier, cte0NextFrontier; identity Keyed via cte0Seen (Id); max iterations 1000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        CreateValuesRows [cte0CurrentFrontier_seedRows: seedValues0C8F87F6Row0 x 1]
        ForEach [seed in cte0CurrentFrontier_seedRows]
          RecursiveAppend [cte0CurrentFrontier <- Cte0Row0(Id: seed.Id, Depth: 0, Path: '1'); identity cte0Seen (Id); guard cte0.Count + cte0CurrentFrontier.Count < 10000000]
      RecursiveMember
        ForEach [p in cte0CurrentFrontier]
          Let [id: int = p.Id]
          RecursiveAppend [cte0NextFrontier <- Cte0Row0(Id: CASE WHEN (id = 1) THEN 2 ELSE 1 END, Depth: (p.Depth + 1), Path: ((p.Path || '->') || CASE WHEN (id = 1) THEN '2' ELSE '1' END)); identity cte0Seen (Id); guard cte0.Count + cte0NextFrontier.Count < 10000000]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [From]
    ForEach [paths in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Id: paths.Id, Depth: paths.Depth, Path: paths.Path)]
    PhaseBoundary [Select]
    SortShapeRows [result -> resultSorted by Id ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q195_RecursiveKeyedNonKeyPayload
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
            new Column("Depth", typeof(int), 1),
            new Column("Path", typeof(string), 2)
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
                yield return new ResultRow0(__musoqShapeRow.Id, __musoqShapeRow.Depth, __musoqShapeRow.Path);
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
                    seedValues0C8F87F6Row0[] cte0CurrentFrontier_seedRows = new seedValues0C8F87F6Row0[]
                    {
                        new seedValues0C8F87F6Row0(1)
                    };
                    foreach (var seed in cte0CurrentFrontier_seedRows)
                    {
                        token.ThrowIfCancellationRequested();
                        var __cte0CurrentFrontierCandidate0 = seed.Id;
                        var __cte0CurrentFrontierCandidate1 = 0;
                        var __cte0CurrentFrontierCandidate2 = "1";
                        if (cte0Seen.Add(__cte0CurrentFrontierCandidate0))
                        {
                            if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                            {
                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("paths", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
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
                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("paths", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 1000);
                        }

                        __cte0Iteration++;
                        cte0NextFrontier.Clear();
                        for (int cte0CurrentFrontierIndex = 0; cte0CurrentFrontierIndex < cte0CurrentFrontier.Count; ++cte0CurrentFrontierIndex)
                        {
                            if (cte0CurrentFrontierIndex != 0 && (cte0CurrentFrontierIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Cte0Row0 p = (Cte0Row0)cte0CurrentFrontier[cte0CurrentFrontierIndex];
                            int id = p.Id;
                            ++__cte0CancellationCounter;
                            if ((__cte0CancellationCounter & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var __cte0NextFrontierCandidate0 = ((id == 1) ? (int)2 : (int)1);
                            var __cte0NextFrontierCandidate1 = (p.Depth + 1);
                            var __cte0NextFrontierCandidate2 = ((p.Path + "->") + ((id == 1) ? (string)"2" : (string)"1"));
                            if (cte0Seen.Add(__cte0NextFrontierCandidate0))
                            {
                                if (cte0.Count + cte0NextFrontier.Count >= 10000000)
                                {
                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("paths", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
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

                    Cte0Row0 paths = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(paths.Id, paths.Depth, paths.Path));
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
            public Cte0Row0(int Id, int Depth, string Path)
            {
                this.Id = Id;
                this.Depth = Depth;
                this.Path = Path;
            }

            public int Id { get; }
            public int Depth { get; }
            public string Path { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, int __value1, string __value2)
            {
                Id = __value0;
                Depth = __value1;
                Path = __value2;
            }

            public override int Count => 3;
            public int Depth { get; private set; }
            public int Id { get; private set; }
            public string Path { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Id = (int)value;
                        break;
                    case 1:
                        Depth = (int)value;
                        break;
                    case 2:
                        Path = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Depth" => true,
                "Path" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Id,
                1 => (object)Depth,
                2 => (object)Path,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Id" => (object)Id,
                "Depth" => (object)Depth,
                "Path" => (object)Path,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id, int Depth, string Path)
            {
                this.Id = Id;
                this.Depth = Depth;
                this.Path = Path;
            }

            public int Depth { get; }
            public int Id { get; }
            public string Path { get; }
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
