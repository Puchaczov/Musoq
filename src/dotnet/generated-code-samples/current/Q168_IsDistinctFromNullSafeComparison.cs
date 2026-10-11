// === Parsed Query ===
/*
from values {
    ( Label: 'same', LeftValue: 1, RightValue: 1 ),
    ( Label: 'different', LeftValue: 1, RightValue: 2 ),
    ( Label: 'both-null', LeftValue: null, RightValue: null ),
    ( Label: 'left-null', LeftValue: null, RightValue: 3 )
} pairs
select pairs.Label,
       pairs.LeftValue is distinct from pairs.RightValue as IsDifferent,
       pairs.LeftValue is not distinct from pairs.RightValue as IsSame
*/

// === Logical Plan ===
/*
MultiStatement
  Project [pairs.Label as Label, (pairs.LeftValue IS DISTINCT FROM pairs.RightValue) as IsDifferent, (pairs.LeftValue IS NOT DISTINCT FROM pairs.RightValue) as IsSame]
    ValuesScan [4 rows as pairs]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [pairs.Label as Label, (pairs.LeftValue IS DISTINCT FROM pairs.RightValue) as IsDifferent, (pairs.LeftValue IS NOT DISTINCT FROM pairs.RightValue) as IsSame]
    PhysicalValuesScan [4 rows as pairs]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Label: string <- field Label
      LeftValue: int? <- field LeftValue
      RightValue: int? <- field RightValue
    Generated [ResultRow0]
      Label: string <- field Label
      IsDifferent: bool <- field IsDifferent
      IsSame: bool <- field IsSame

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    CreateValuesRows [pairsRows: pairsValues691436E3Row0 x 4]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ForEach [pairs in pairsRows]
      Let [leftValue: int? = pairs.LeftValue]
      Let [rightValue: int? = pairs.RightValue]
      AppendShape [result <- ResultRow0(Label: pairs.Label, IsDifferent: (leftValue IS DISTINCT FROM rightValue), IsSame: (leftValue IS NOT DISTINCT FROM rightValue))]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q168_IsDistinctFromNullSafeComparison
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
        private static readonly Column[] __columns_compiled_result_0 = new Column[]
        {
            new Column("Label", typeof(string), 0),
            new Column("IsDifferent", typeof(bool), 1),
            new Column("IsSame", typeof(bool), 2)
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_0, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                pairsValues691436E3Row0[] pairsRows = new pairsValues691436E3Row0[]
                {
                    new pairsValues691436E3Row0("same", 1, 1),
                    new pairsValues691436E3Row0("different", 1, 2),
                    new pairsValues691436E3Row0("both-null", null, null),
                    new pairsValues691436E3Row0("left-null", null, 3)
                };
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var pairs in pairsRows)
                {
                    token.ThrowIfCancellationRequested();
                    int? leftValue = pairs.LeftValue;
                    int? rightValue = pairs.RightValue;
                    yield return new ResultRow0(pairs.Label, (leftValue != rightValue), (leftValue == rightValue));
                }
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, bool __value1, bool __value2)
            {
                Label = __value0;
                IsDifferent = __value1;
                IsSame = __value2;
            }

            public override int Count => 3;
            public bool IsDifferent { get; private set; }
            public bool IsSame { get; private set; }
            public string Label { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Label = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.IsDifferent = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<bool, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.IsSame = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<bool, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Label),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.IsDifferent),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.IsSame),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Label" => true,
                "IsDifferent" => true,
                "IsSame" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Label, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.IsDifferent, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.IsSame, ref visitor);
            }
        }

        private sealed class pairsValues691436E3Row0 : Row
        {
            public pairsValues691436E3Row0(string __value0, int? __value1, int? __value2)
            {
                Label = __value0;
                LeftValue = __value1;
                RightValue = __value2;
            }

            public override int Count => 3;
            public string Label { get; private set; }
            public int? LeftValue { get; private set; }
            public int? RightValue { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Label = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.LeftValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.RightValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Label),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.LeftValue),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.RightValue),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Label" => true,
                "LeftValue" => true,
                "RightValue" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Label, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.LeftValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.RightValue, ref visitor);
            }
        }
    }
}
