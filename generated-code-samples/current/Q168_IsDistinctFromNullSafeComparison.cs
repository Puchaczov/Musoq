/*
raw query string

from values {
    { Label: 'same', LeftValue: 1, RightValue: 1 },
    { Label: 'different', LeftValue: 1, RightValue: 2 },
    { Label: 'both-null', LeftValue: null, RightValue: null },
    { Label: 'left-null', LeftValue: null, RightValue: 3 }
} pairs
select pairs.Label,
       pairs.LeftValue is distinct from pairs.RightValue as IsDifferent,
       pairs.LeftValue is not distinct from pairs.RightValue as IsSame
*/

/*
logical plan representation string

MultiStatement
  Project [pairs.Label as pairs.Label, (pairs.LeftValue IS DISTINCT FROM pairs.RightValue) as IsDifferent, (pairs.LeftValue IS NOT DISTINCT FROM pairs.RightValue) as IsSame]
    ValuesScan [4 rows as pairs]
*/

/*
physical plan representation string

PhysicalMultiStatement
  PhysicalProject [pairs.Label as pairs.Label, (pairs.LeftValue IS DISTINCT FROM pairs.RightValue) as IsDifferent, (pairs.LeftValue IS NOT DISTINCT FROM pairs.RightValue) as IsSame]
    PhysicalValuesScan [4 rows as pairs]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Label: string <- field Label
      LeftValue: int? <- field LeftValue
      RightValue: int? <- field RightValue
    Generated [ResultRow0]
      pairs.Label: string <- field pairs_Label
      IsDifferent: bool <- field IsDifferent
      IsSame: bool <- field IsSame

  Body
    CreateValuesRows [pairsRows: pairsValues691436E3Row0 x 4]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [pairs in pairsRows]
      Let [leftValue: int? = pairs.LeftValue]
      Let [rightValue: int? = pairs.RightValue]
      AppendShape [result <- ResultShape0(pairs.Label: pairs.Label, IsDifferent: (leftValue IS DISTINCT FROM rightValue), IsSame: (leftValue IS NOT DISTINCT FROM rightValue))]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_0 = new Column[]
        {
            new Column("pairs.Label", typeof(string), 0),
            new Column("IsDifferent", typeof(bool), 1),
            new Column("IsSame", typeof(bool), 2)
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
                yield return new ResultRow0(__musoqShapeRow.pairs_Label, __musoqShapeRow.IsDifferent, __musoqShapeRow.IsSame);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                pairsValues691436E3Row0[] pairsRows = new pairsValues691436E3Row0[]
                {
                    new pairsValues691436E3Row0("same", 1, 1),
                    new pairsValues691436E3Row0("different", 1, 2),
                    new pairsValues691436E3Row0("both-null", null, null),
                    new pairsValues691436E3Row0("left-null", null, 3)
                };
                foreach (var pairs in pairsRows)
                {
                    token.ThrowIfCancellationRequested();
                    int? leftValue = pairs.LeftValue;
                    int? rightValue = pairs.RightValue;
                    yield return new ResultShape0(pairs.Label, (leftValue != rightValue), (leftValue == rightValue));
                }
            }
            finally
            {
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, bool __value1, bool __value2)
            {
                pairs_Label = __value0;
                IsDifferent = __value1;
                IsSame = __value2;
            }

            public override int Count => 3;
            public bool IsDifferent { get; private set; }
            public bool IsSame { get; private set; }
            public string pairs_Label { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        pairs_Label = (string)value;
                        break;
                    case 1:
                        IsDifferent = (bool)value;
                        break;
                    case 2:
                        IsSame = (bool)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "pairs.Label" => true,
                "pairs_Label" => true,
                "Label" => true,
                "IsDifferent" => true,
                "IsSame" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)pairs_Label,
                1 => (object)IsDifferent,
                2 => (object)IsSame,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "pairs.Label" => (object)pairs_Label,
                "pairs_Label" => (object)pairs_Label,
                "Label" => (object)pairs_Label,
                "IsDifferent" => (object)IsDifferent,
                "IsSame" => (object)IsSame,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string pairs_Label, bool IsDifferent, bool IsSame)
            {
                this.pairs_Label = pairs_Label;
                this.IsDifferent = IsDifferent;
                this.IsSame = IsSame;
            }

            public bool IsDifferent { get; }
            public bool IsSame { get; }
            public string pairs_Label { get; }
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

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Label = (string)value;
                        break;
                    case 1:
                        LeftValue = (int?)value;
                        break;
                    case 2:
                        RightValue = (int?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Label" => true,
                "LeftValue" => true,
                "RightValue" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Label,
                1 => (object)LeftValue,
                2 => (object)RightValue,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Label" => (object)Label,
                "LeftValue" => (object)LeftValue,
                "RightValue" => (object)RightValue,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
