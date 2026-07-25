// === Parsed Query ===
/*
with recursive walk (Id) as (select Id from values {{ Id: 1 }} seed union all select w.Id + 1 from walk w where w.Id < 3) select w.Id, l.Name from walk w inner join values {{ Id: 1, Name: 'root' }, { Id: 2, Name: 'middle' }, { Id: 3, Name: 'leaf' }} l on w.Id = l.Id
*/

// === Logical Plan ===
/*
Cte
  Definition [walk]
    RecursiveCte [walk] [All]
      Anchor
        MultiStatement
          Project [seed.Id as Id]
            ValuesScan [1 rows as seed]
      RecursiveMember
        MultiStatement
          Project [(w.Id + 1) as w.Id + 1]
            Filter [(w.Id < 3)]
              CteRef [walk as w]
  Query
    MultiStatement
      Project [w.Id as w.Id, l.Id as l.Id, l.Name as l.Name]
        Join [Inner] [(w.Id = l.Id)]
          CteRef [walk as w]
          ValuesScan [3 rows as l]
      Project [w.Id as w.Id, l.Name as l.Name]
        CteRef [wl as wl]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [walk]
    PhysicalRecursiveCte [walk] [All]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [seed.Id as Id]
            PhysicalValuesScan [1 rows as seed]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [(w.Id + 1) as w.Id + 1]
            PhysicalFilter [(w.Id < 3)]
              PhysicalCteRef [walk as w]
  Query
    PhysicalMultiStatement
      PhysicalProject [w.Id as w.Id, l.Id as l.Id, l.Name as l.Name]
        PhysicalHashJoin [Inner] [build: l.Id] [probe: w.Id]
          PhysicalCteRef [walk as w]
          PhysicalValuesScan [3 rows as l]
      PhysicalProject [w.Id as w.Id, l.Name as l.Name]
        PhysicalCteRef [wl as wl]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Id: int <- field Id
    Generated [Cte0Row0]
      Id: int <- field Id
    TableRow [w]
      Id: int <- field Id
    TableRow [w]
      Id: int <- field Id
    UnknownShape [ValuesRowShape]
      Id: int <- field Id
      Name: string <- field Name
    Generated [ResultRow0]
      w.Id: int <- field w_Id
      l.Name: string <- field l_Name

  Body
    RecursiveCte [walk; result cte0; frontiers cte0CurrentFrontier, cte0NextFrontier; identity none; max iterations 1000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        CreateValuesRows [cte0CurrentFrontier_seedRows: seedValues0C8F87F6Row0 x 1]
        ForEach [seed in cte0CurrentFrontier_seedRows]
          RecursiveAppend [cte0CurrentFrontier <- Cte0Row0(Id: seed.Id); identity none; guard cte0.Count + cte0CurrentFrontier.Count < 10000000]
      RecursiveMember
        ForEach [w in cte0CurrentFrontier]
          If [(w.Id < 3)]
            RecursiveAppend [cte0NextFrontier <- Cte0Row0(Id: (w.Id + 1)); identity none; guard cte0.Count + cte0NextFrontier.Count < 10000000]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    CtePhase [cte1]
    CreateValuesRows [lRows: lValues927A8BAERow0 x 3]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateHash [lHash: int -> object; capacity: 3]
    ForEach [l in lRows]
      HashAdd [lHash[l.Id] += l]
    ForEach [w in _cteRowResults.Slot0]
      HashProbe [lHash[w.Id] -> lHashMatches]
        ForEach [l in lHashMatches]
          AppendShape [result <- ResultShape0(w.Id: w.Id, l.Name: l.Name)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q212_RecursiveOuterJoin
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
            new Column("w.Id", typeof(int), 0),
            new Column("l.Name", typeof(string), 1)
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_0, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.w_Id, __musoqShapeRow.l_Name);
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
                seedValues0C8F87F6Row0[] cte0CurrentFrontier_seedRows = new seedValues0C8F87F6Row0[]
                {
                    new seedValues0C8F87F6Row0(1)
                };
                foreach (var seed in cte0CurrentFrontier_seedRows)
                {
                    token.ThrowIfCancellationRequested();
                    if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                    {
                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("walk", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                    }

                    cte0CurrentFrontier.Add(new Cte0Row0(seed.Id));
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
                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("walk", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 1000);
                    }

                    __cte0Iteration++;
                    cte0NextFrontier.Clear();
                    for (int cte0CurrentFrontierIndex = 0; cte0CurrentFrontierIndex < cte0CurrentFrontier.Count; ++cte0CurrentFrontierIndex)
                    {
                        if (cte0CurrentFrontierIndex != 0 && (cte0CurrentFrontierIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        Cte0Row0 w = (Cte0Row0)cte0CurrentFrontier[cte0CurrentFrontierIndex];
                        if ((w.Id < 3))
                        {
                            ++__cte0CancellationCounter;
                            if ((__cte0CancellationCounter & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            if (cte0.Count + cte0NextFrontier.Count >= 10000000)
                            {
                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("walk", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                            }

                            cte0NextFrontier.Add(new Cte0Row0((w.Id + 1)));
                        }
                    }

                    cte0.AddRange(cte0NextFrontier);
                    var __cte0FrontierSwap = cte0CurrentFrontier;
                    cte0CurrentFrontier = cte0NextFrontier;
                    cte0NextFrontier = __cte0FrontierSwap;
                }

                _cteRowResults.Slot0 = cte0;
                lValues927A8BAERow0[] lRows = new lValues927A8BAERow0[]
                {
                    new lValues927A8BAERow0(1, "root"),
                    new lValues927A8BAERow0(2, "middle"),
                    new lValues927A8BAERow0(3, "leaf")
                };
                var lHash = new Dictionary<int, HashJoinBucket<lValues927A8BAERow0>>(3);
                foreach (var l in lRows)
                {
                    token.ThrowIfCancellationRequested();
                    int key = l.Id;
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(lHash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<lValues927A8BAERow0>(l);
                        }
                        else
                        {
                            matches.Add(l);
                        }
                    }
                }

                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 w = __storedTable0Rows[__storedTable0Index];
                    int key = w.Id;
                    if (lHash.TryGetValue(key, out var lHashMatches))
                    {
                        foreach (var l in lHashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            __musoqFinalShapeRows.Add(new ResultShape0(w.Id, l.Name));
                        }
                    }
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
            public ResultRow0(int __value0, string __value1)
            {
                w_Id = __value0;
                l_Name = __value1;
            }

            public override int Count => 2;
            public string l_Name { get; private set; }
            public int w_Id { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        w_Id = (int)value;
                        break;
                    case 1:
                        l_Name = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "w.Id" => true,
                "w_Id" => true,
                "Id" => true,
                "l.Name" => true,
                "l_Name" => true,
                "Name" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)w_Id,
                1 => (object)l_Name,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "w.Id" => (object)w_Id,
                "w_Id" => (object)w_Id,
                "Id" => (object)w_Id,
                "l.Name" => (object)l_Name,
                "l_Name" => (object)l_Name,
                "Name" => (object)l_Name,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int w_Id, string l_Name)
            {
                this.w_Id = w_Id;
                this.l_Name = l_Name;
            }

            public string l_Name { get; }
            public int w_Id { get; }
        }

        private sealed class lValues927A8BAERow0 : Row
        {
            public lValues927A8BAERow0(int __value0, string __value1)
            {
                Id = __value0;
                Name = __value1;
            }

            public override int Count => 2;
            public int Id { get; private set; }
            public string Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Id = (int)value;
                        break;
                    case 1:
                        Name = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Name" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Id,
                1 => (object)Name,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Id" => (object)Id,
                "Name" => (object)Name,
                _ => throw new KeyNotFoundException(name)
            };
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
