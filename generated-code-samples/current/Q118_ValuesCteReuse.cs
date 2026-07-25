// === Parsed Query ===
/*
with policy as (
                  from values {
                      { Name: 'Newtonsoft.Json', Approved: true },
                      { Name: 'Legacy.Package', Approved: false }
                  } p
                  select p.Name, p.Approved
              )
              select leftPolicy.Name, rightPolicy.Approved
              from policy leftPolicy
              inner join policy rightPolicy on leftPolicy.Name = rightPolicy.Name
              where rightPolicy.Approved = false
*/

// === Logical Plan ===
/*
Cte
  Definition [policy]
    MultiStatement
      Project [p.Name as p.Name, p.Approved as p.Approved]
        ValuesScan [2 rows as p]
  Query
    MultiStatement
      Project [leftPolicy.Name as leftPolicy.Name, rightPolicy.Name as rightPolicy.Name, rightPolicy.Approved as rightPolicy.Approved]
        Join [Inner] [(leftPolicy.Name = rightPolicy.Name)]
          CteRef [policy as leftPolicy]
          CteRef [policy as rightPolicy]
      Project [leftPolicy.Name as leftPolicy.Name, rightPolicy.Approved as rightPolicy.Approved]
        Filter [(rightPolicy.Approved = FALSE)]
          CteRef [leftPolicyrightPolicy as leftPolicyrightPolicy]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [policy]
    PhysicalMultiStatement
      PhysicalProject [p.Name as p.Name, p.Approved as p.Approved]
        PhysicalValuesScan [2 rows as p]
  Query
    PhysicalMultiStatement
      PhysicalProject [leftPolicy.Name as leftPolicy.Name, rightPolicy.Name as rightPolicy.Name, rightPolicy.Approved as rightPolicy.Approved]
        PhysicalHashJoin [Inner] [build: rightPolicy.Name] [probe: leftPolicy.Name]
          PhysicalCteRef [policy as leftPolicy]
          PhysicalCteRef [policy as rightPolicy]
      PhysicalProject [leftPolicy.Name as leftPolicy.Name, rightPolicy.Approved as rightPolicy.Approved]
        PhysicalFilter [(rightPolicy.Approved = FALSE)]
          PhysicalCteRef [leftPolicyrightPolicy as leftPolicyrightPolicy]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Name: string <- field Name
      Approved: bool <- field Approved
    Generated [Cte0Row0]
      p.Name: string <- field p_Name
      p.Approved: bool <- field p_Approved
    TableRow [leftPolicy]
      p.Name: string <- field p_Name
      p.Approved: bool <- field p_Approved
    TableRow [rightPolicy]
      p.Name: string <- field p_Name
      p.Approved: bool <- field p_Approved
    Generated [ResultRow0]
      leftPolicy.Name: string <- field leftPolicy_Name
      rightPolicy.Approved: bool <- field rightPolicy_Approved

  Body
    CreateValuesRows [cte0_pRows: pValuesD8A584C0Row0 x 2]
    CreateTable [cte0: Cte0Row0]
    ForEach [p in cte0_pRows]
      AppendRow [cte0 <- Cte0Row0(p.Name: p.Name, p.Approved: p.Approved)]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    CtePhase [cte1]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateHash [rightPolicyHash: string -> Row; capacity: _cteRowResults.Slot0.Count]
    ForEach [rightPolicy in _cteRowResults.Slot0]
      HashAdd [rightPolicyHash[rightPolicy.p.Name] += rightPolicy]
    ForEach [leftPolicy in _cteRowResults.Slot0]
      HashProbe [rightPolicyHash[leftPolicy.p.Name] -> rightPolicyHashMatches]
        ForEach [rightPolicy in rightPolicyHashMatches]
          Let [p_Approved: bool = rightPolicy.p.Approved]
          If [(p_Approved = FALSE)]
            AppendShape [result <- ResultShape0(leftPolicy.Name: leftPolicy.p.Name, rightPolicy.Approved: p_Approved)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q118_ValuesCteReuse
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
        private static readonly Column[] __columns_compiled_cte0_0 = new Column[]
        {
            new Column("p.Name", typeof(string), 0),
            new Column("p.Approved", typeof(bool), 1)
        };
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("leftPolicy.Name", typeof(string), 0),
            new Column("rightPolicy.Approved", typeof(bool), 1)
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.leftPolicy_Name, __musoqShapeRow.rightPolicy_Approved);
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
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults);
                var rightPolicyHash = new Dictionary<string, HashJoinBucket<Cte0Row0>>(_cteRowResults.Slot0.Count);
                var __storedTable0Rows = _cteRowResults.Slot0;
                foreach (var rightPolicy in __storedTable0Rows)
                {
                    token.ThrowIfCancellationRequested();
                    string key = rightPolicy.p_Name;
                    if (key == null)
                        continue;
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(rightPolicyHash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<Cte0Row0>(rightPolicy);
                        }
                        else
                        {
                            matches.Add(rightPolicy);
                        }
                    }
                }

                foreach (var leftPolicy in __storedTable0Rows)
                {
                    token.ThrowIfCancellationRequested();
                    string key = leftPolicy.p_Name;
                    if (key != null && rightPolicyHash.TryGetValue(key, out var rightPolicyHashMatches))
                    {
                        foreach (var rightPolicy in rightPolicyHashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            bool p_Approved = rightPolicy.p_Approved;
                            if ((p_Approved == false))
                            {
                                __musoqFinalShapeRows.Add(new ResultShape0(leftPolicy.p_Name, p_Approved));
                            }
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, CteRowResults _cteRowResults)
        {
            pValuesD8A584C0Row0[] cte0_pRows = new pValuesD8A584C0Row0[]
            {
                new pValuesD8A584C0Row0("Newtonsoft.Json", true),
                new pValuesD8A584C0Row0("Legacy.Package", false)
            };
            var cte0 = new List<Cte0Row0>();
            foreach (var p in cte0_pRows)
            {
                token.ThrowIfCancellationRequested();
                cte0.Add(new Cte0Row0(p.Name, p.Approved));
            }

            return cte0;
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(string __value0, bool __value1)
            {
                p_Name = __value0;
                p_Approved = __value1;
            }

            public bool p_Approved { get; }
            public string p_Name { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, bool __value1)
            {
                leftPolicy_Name = __value0;
                rightPolicy_Approved = __value1;
            }

            public override int Count => 2;
            public string leftPolicy_Name { get; private set; }
            public bool rightPolicy_Approved { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        leftPolicy_Name = (string)value;
                        break;
                    case 1:
                        rightPolicy_Approved = (bool)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "leftPolicy.Name" => true,
                "leftPolicy_Name" => true,
                "Name" => true,
                "rightPolicy.Approved" => true,
                "rightPolicy_Approved" => true,
                "Approved" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)leftPolicy_Name,
                1 => (object)rightPolicy_Approved,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "leftPolicy.Name" => (object)leftPolicy_Name,
                "leftPolicy_Name" => (object)leftPolicy_Name,
                "Name" => (object)leftPolicy_Name,
                "rightPolicy.Approved" => (object)rightPolicy_Approved,
                "rightPolicy_Approved" => (object)rightPolicy_Approved,
                "Approved" => (object)rightPolicy_Approved,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string leftPolicy_Name, bool rightPolicy_Approved)
            {
                this.leftPolicy_Name = leftPolicy_Name;
                this.rightPolicy_Approved = rightPolicy_Approved;
            }

            public string leftPolicy_Name { get; }
            public bool rightPolicy_Approved { get; }
        }

        private sealed class pValuesD8A584C0Row0 : Row
        {
            public pValuesD8A584C0Row0(string __value0, bool __value1)
            {
                Name = __value0;
                Approved = __value1;
            }

            public bool Approved { get; private set; }
            public override int Count => 2;
            public string Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        Approved = (bool)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Approved" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)Approved,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "Approved" => (object)Approved,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
