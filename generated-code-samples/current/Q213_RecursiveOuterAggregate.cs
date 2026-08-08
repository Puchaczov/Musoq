// === Parsed Query ===
/*
with recursive walk (Id, Depth) as (select Id, 0 from values {{ Id: 1 }} seed union all select w.Id + 1, w.Depth + 1 from walk w where w.Depth < 3) select Count(Id) as NodeCount, Max(Depth) as MaxDepth from walk
*/

// === Logical Plan ===
/*
Cte
  Definition [walk]
    RecursiveCte [walk] [All]
      Anchor
        MultiStatement
          Project [seed.Id as Id, 0 as Depth]
            ValuesScan [1 rows as seed]
      RecursiveMember
        MultiStatement
          Project [(w.Id + 1) as w.Id + 1, (w.Depth + 1) as w.Depth + 1]
            Filter [(w.Depth < 3)]
              CteRef [walk as w]
  Query
    MultiStatement
      Project [1 as 1, AggRef(walk.Max(walk.Depth)) as walk.Max(walk.Depth), AggRef(walk.Count(walk.Id)) as walk.Count(walk.Id)]
        Aggregate [keys: 1] [aggs: Max(Depth), Count(Id)]
          CteRef [walk as walk]
      Project [walk.Count(walk.Id) as NodeCount, walk.Max(walk.Depth) as MaxDepth]
        CteRef [walkScore as walkScore]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [walk]
    PhysicalRecursiveCte [walk] [All]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [seed.Id as Id, 0 as Depth]
            PhysicalValuesScan [1 rows as seed]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [(w.Id + 1) as w.Id + 1, (w.Depth + 1) as w.Depth + 1]
            PhysicalFilter [(w.Depth < 3)]
              PhysicalCteRef [walk as w]
  Query
    PhysicalMultiStatement
      PhysicalProject [1 as 1, AggRef(walk.Max(walk.Depth)) as walk.Max(walk.Depth), AggRef(walk.Count(walk.Id)) as walk.Count(walk.Id)]
        PhysicalSingleKeyAggregate [key: 1 (Int16)] [aggs: Max(Depth), Count(Id)]
          PhysicalCteRef [walk as walk]
      PhysicalProject [walk.Count(walk.Id) as NodeCount, walk.Max(walk.Depth) as MaxDepth]
        PhysicalCteRef [walkScore as walkScore]
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
    TableRow [w]
      Id: int <- field Id
      Depth: int <- field Depth
    TableRow [walk]
      Id: int <- field Id
      Depth: int <- field Depth
    AggregateGroup [ResultAggregateGroup; keys: 0; typed aggs: 2]
    Generated [ResultRow0]
      NodeCount: long <- field NodeCount
      MaxDepth: int? <- field MaxDepth

  Body
    RecursiveCte [walk; result cte0; frontiers cte0CurrentFrontier, cte0NextFrontier; identity none; max iterations 1000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        CreateValuesRows [cte0CurrentFrontier_seedRows: seedValues0C8F87F6Row0 x 1]
        ForEach [seed in cte0CurrentFrontier_seedRows]
          RecursiveAppend [cte0CurrentFrontier <- Cte0Row0(Id: seed.Id, Depth: 0); identity none; guard cte0.Count + cte0CurrentFrontier.Count < 10000000]
      RecursiveMember
        ForEach [w in cte0CurrentFrontier]
          If [(w.Depth < 3)]
            RecursiveAppend [cte0NextFrontier <- Cte0Row0(Id: (w.Id + 1), Depth: (w.Depth + 1)); identity none; guard cte0.Count + cte0NextFrontier.Count < 10000000]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateAggregateContext [rootGroup, group, groupsToFinalize; typed: ResultAggregateGroup]
    ForEach [walk in _cteRowResults.Slot0]
      Let [depth: int = walk.Depth]
      Let [id: int = walk.Id]
      EnsureAggregateGroup [group; typed: ResultAggregateGroup]
      TypedAggregateSet [Set(group.__agg0, depth)]
      TypedAggregateSet [Set(group.__agg1, id)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(NodeCount: walk.Count(walk.Id), MaxDepth: walk.Max(walk.Depth))]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q213_RecursiveOuterAggregate
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
            new Column("NodeCount", typeof(long), 0),
            new Column("MaxDepth", typeof(int?), 1)
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
                yield return new ResultRow0(__musoqShapeRow.NodeCount, __musoqShapeRow.MaxDepth);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.Where);
            OnPhaseChanged("compiled", QueryPhase.GroupBy);
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

                    cte0CurrentFrontier.Add(new Cte0Row0(seed.Id, 0));
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
                        if ((w.Depth < 3))
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

                            cte0NextFrontier.Add(new Cte0Row0((w.Id + 1), (w.Depth + 1)));
                        }
                    }

                    cte0.AddRange(cte0NextFrontier);
                    var __cte0FrontierSwap = cte0CurrentFrontier;
                    cte0CurrentFrontier = cte0NextFrontier;
                    cte0NextFrontier = __cte0FrontierSwap;
                }

                _cteRowResults.Slot0 = cte0;
                var groupsToFinalize = new List<ResultAggregateGroup>();
                ResultAggregateGroup group = new ResultAggregateGroup();
                groupsToFinalize.Add(group);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 walk = __storedTable0Rows[__storedTable0Index];
                    int depth = walk.Depth;
                    int id = walk.Id;
                    if (group == null)
                    {
                        group = new ResultAggregateGroup();
                        groupsToFinalize.Add(group);
                    }

                    {
                        var __agg0Input = (int?)depth;
                        if (__agg0Input.HasValue)
                        {
                            var __agg0Current = __agg0Input.GetValueOrDefault();
                            if (!group.__agg0.HasValue || __agg0Current > group.__agg0.Value)
                            {
                                group.__agg0.Value = __agg0Current;
                            }

                            group.__agg0.HasValue = true;
                        }
                    }

                    if (((int?)id).HasValue)
                    {
                        group.__agg1.Count = checked(group.__agg1.Count + 1L);
                    }
                }

                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    __musoqFinalShapeRows.Add(new ResultShape0(finalGroup.__agg1.Count, finalGroup.__agg0.HasValue ? (int?)finalGroup.__agg0.Value : null));
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
            public Cte0Row0(int Id, int Depth)
            {
                this.Id = Id;
                this.Depth = Depth;
            }

            public int Id { get; }
            public int Depth { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultAggregateGroup
        {
            public Musoq.Plugins.MaxAggregateKernel<int>.State __agg0;
            public Musoq.Plugins.CountNullableAggregateKernel<int>.State __agg1;
            public ResultAggregateGroup()
            {
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.MaxAggregateKernel<int>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.CountNullableAggregateKernel<int>.Merge(ref this.__agg1, in source.__agg1);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(long __value0, int? __value1)
            {
                NodeCount = __value0;
                MaxDepth = __value1;
            }

            public override int Count => 2;
            public int? MaxDepth { get; private set; }
            public long NodeCount { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        NodeCount = (long)value;
                        break;
                    case 1:
                        MaxDepth = (int?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "NodeCount" => true,
                "MaxDepth" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)NodeCount,
                1 => (object)MaxDepth,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "NodeCount" => (object)NodeCount,
                "MaxDepth" => (object)MaxDepth,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(long NodeCount, int? MaxDepth)
            {
                this.NodeCount = NodeCount;
                this.MaxDepth = MaxDepth;
            }

            public int? MaxDepth { get; }
            public long NodeCount { get; }
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
