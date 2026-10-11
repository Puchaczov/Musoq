// === Parsed Query ===
/*
with policy as (
                  from values {
                      ( Name: 'Newtonsoft.Json', Approved: true ),
                      ( Name: 'Legacy.Package', Approved: false )
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
      Project [p.Name as Name, p.Approved as Approved]
        ValuesScan [2 rows as p]
  Query
    MultiStatement
      Project [leftPolicy.Name as leftPolicy.Name, rightPolicy.Name as rightPolicy.Name, rightPolicy.Approved as rightPolicy.Approved]
        Join [Inner] [(leftPolicy.Name = rightPolicy.Name)]
          CteRef [policy as leftPolicy]
          CteRef [policy as rightPolicy]
      Project [leftPolicy.Name as Name, rightPolicy.Approved as Approved]
        Filter [(rightPolicy.Approved = FALSE)]
          CteRef [leftPolicyrightPolicy as leftPolicyrightPolicy]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [policy]
    PhysicalMultiStatement
      PhysicalProject [p.Name as Name, p.Approved as Approved]
        PhysicalValuesScan [2 rows as p]
  Query
    PhysicalMultiStatement
      PhysicalProject [leftPolicy.Name as leftPolicy.Name, rightPolicy.Name as rightPolicy.Name, rightPolicy.Approved as rightPolicy.Approved]
        PhysicalHashJoin [Inner] [build: rightPolicy.Name] [probe: leftPolicy.Name]
          PhysicalCteRef [policy as leftPolicy]
          PhysicalCteRef [policy as rightPolicy]
      PhysicalProject [leftPolicy.Name as Name, rightPolicy.Approved as Approved]
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
      Name: string <- field Name
      Approved: bool <- field Approved
    HashPayload [Cte0HashPayload0]
      Name: string <- field Name
      Approved: bool <- field Approved
    TableRow [leftPolicy]
      Name: string <- field Name
      Approved: bool <- field Approved
    HashPayload [Cte0HashPayload0]
      Name: string <- field Name
      Approved: bool <- field Approved
    TableRow [rightPolicy]
      Name: string <- field Name
      Approved: bool <- field Approved
    Generated [ResultRow0]
      Name: string <- field Name
      Approved: bool <- field Approved

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Where]
    PhaseBoundary [From:cte0]
    CreateValuesRows [cte0_pRows: pValuesD8A584C0Row0 x 2]
    CreateTable [cte0: Cte0Row0]
    CreateHash [cte0HashSidecar0Name: string -> Row]
    ForEach [p in cte0_pRows]
      CreateGeneratedRow [cte0SidecarRow0 <- Cte0Row0(Name: p.Name, Approved: p.Approved)]
      AppendExistingRow [cte0 <- cte0SidecarRow0]
      CreateHashPayload [cte0SidecarPayload0 <- Cte0HashPayload0(Name: p.Name, Approved: p.Approved)]
      HashAdd [cte0HashSidecar0Name[p.Name] += cte0SidecarPayload0]
    StoreCteIndex [cte0HashSidecar0Name -> _cteIndexResults.Slot0 Hash]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    LoadCteIndex [rightPolicyHash <- _cteIndexResults.Slot0 Hash: string]
    ForEach [leftPolicy in _cteRowResults.Slot0]
      HashProbe [rightPolicyHash[leftPolicy.Name] -> rightPolicyHashMatches]
        ForEach [rightPolicy in rightPolicyHashMatches]
          Let [approved: bool = rightPolicy.Approved]
          If [(approved = FALSE)]
            AppendShape [result <- ResultRow0(Name: leftPolicy.Name, Approved: approved)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_cte0_0 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("Approved", typeof(bool), 1)
        };
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }
        public TypedParameterSnapshot TypedParameters { get; set; } = TypedParameterSnapshot.Empty;

        public event DataSourceEventHandler DataSourceProgress;
        public event QueryPhaseEventHandler PhaseChanged;
        public event QueryProgressEventHandler QueryProgress;
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_cte0_0, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return __musoqShapeRow;
            }
        }

        private IEnumerable<ResultRow0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _cteRowResults = new CteRowResults();
                var _cteIndexResults = new CteIndexResults();
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Where);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults, _cteIndexResults);
                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
                try
                {
                    __musoqFinalShapeRows.EnsureCapacity(_cteRowResults.Slot0.Count);
                    var rightPolicyHash = _cteIndexResults.Slot0;
                    var __storedTable0Rows = _cteRowResults.Slot0;
                    for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                    {
                        if ((__storedTable0Index & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        Cte0Row0 leftPolicy = __storedTable0Rows[__storedTable0Index];
                        string key = leftPolicy.Name;
                        if (key != null && rightPolicyHash.TryGetValue(key, out var rightPolicyHashMatches))
                        {
                            foreach (var rightPolicy in rightPolicyHashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                bool approved = rightPolicy.Approved;
                                if ((approved == false))
                                {
                                    __musoqFinalShapeRows.Add(new ResultRow0(leftPolicy.Name, approved));
                                }
                            }
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte1", QueryPhase.End);
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                pValuesD8A584C0Row0[] cte0_pRows = new pValuesD8A584C0Row0[]
                {
                    new pValuesD8A584C0Row0("Newtonsoft.Json", true),
                    new pValuesD8A584C0Row0("Legacy.Package", false)
                };
                var cte0 = new List<Cte0Row0>();
                var cte0HashSidecar0Name = new Dictionary<string, HashJoinBucket<Cte0HashPayload0>>();
                foreach (var p in cte0_pRows)
                {
                    token.ThrowIfCancellationRequested();
                    Cte0Row0 cte0SidecarRow0 = new Cte0Row0(p.Name, p.Approved);
                    cte0.Add(cte0SidecarRow0);
                    Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(p.Name, p.Approved);
                    string cte0HashSidecar0NameKey0 = p.Name;
                    if (cte0HashSidecar0NameKey0 != null)
                    {
                        {
                            ref var cte0HashSidecar0NameBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0HashSidecar0Name, cte0HashSidecar0NameKey0, out var cte0HashSidecar0NameBucket0Exists);
                            if (!cte0HashSidecar0NameBucket0Exists)
                            {
                                cte0HashSidecar0NameBucket0 = new HashJoinBucket<Cte0HashPayload0>(cte0SidecarPayload0);
                            }
                            else
                            {
                                cte0HashSidecar0NameBucket0.Add(cte0SidecarPayload0);
                            }
                        }
                    }
                }

                _cteIndexResults.Slot0 = cte0HashSidecar0Name;
                return cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        private readonly struct Cte0HashPayload0
        {
            public readonly string Name;
            public readonly bool Approved;
            public Cte0HashPayload0(string Name, bool Approved)
            {
                this.Name = Name;
                this.Approved = Approved;
            }
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(string __value0, bool __value1)
            {
                Name = __value0;
                Approved = __value1;
            }

            public bool Approved { get; }
            public string Name { get; }
        }

        private sealed class CteIndexResults
        {
            public Dictionary<string, HashJoinBucket<Cte0HashPayload0>> Slot0;
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, bool __value1)
            {
                Name = __value0;
                Approved = __value1;
            }

            public bool Approved { get; private set; }
            public override int Count => 2;
            public string Name { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.Approved = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<bool, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.Approved),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Approved" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Approved, ref visitor);
            }
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

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.Approved = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<bool, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.Approved),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Approved" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Approved, ref visitor);
            }
        }
    }
}
