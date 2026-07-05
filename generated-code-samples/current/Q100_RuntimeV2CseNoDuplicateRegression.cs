/*
raw query string

SELECT Value * 2, Name
              FROM #test.entities()
              WHERE ExpensiveMethod(Value) > 100
*/

/*
logical plan representation string

MultiStatement
  Project [(ko3iko.Value * 2) as Value * 2, ko3iko.Name as Name]
    Filter [(ExpensiveMethod(ko3iko.Value) > 100)]
      SchemaScan [#test.entities() as ko3iko]
*/

/*
physical plan representation string

PhysicalMultiStatement
  PhysicalProject [(ko3iko.Value * 2) as Value * 2, ko3iko.Name as Name]
    PhysicalFilter [(ExpensiveMethod(ko3iko.Value) > 100)]
      PhysicalSchemaScan [#test.entities() as ko3iko]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: RuntimeV2RegressionEntity]
      Name: string <- property Name
      Value: int <- property Value
    Generated [ResultRow0]
      Value * 2: int <- field Value___2
      Name: string <- field Name

  Body
    SourceScan [ko3iko: RuntimeV2RegressionEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateObject [__resultRuntimeV2RegressionLibrary0: RuntimeV2RegressionLibrary]
    ParallelFilterProjectLoop [ko3iko in ko3ikoRows where (ExpensiveMethod(ko3iko.Value) > 100); threshold 4096, maxDegree 24]
      ParallelProject
        Let [value: int = ko3iko.Value]
        If [(ExpensiveMethod(value) > 100)]
          AppendShape [result <- ResultShape0(Value * 2: (value * 2), Name: ko3iko.Name)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q100_RuntimeV2CseNoDuplicateRegression
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Value * 2", typeof(int), 0),
            new Column("Name", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 1), new Column("Value", typeof(int), 5) });
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
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.Where);
            OnPhaseChanged("compiled", QueryPhase.Select);
            var __musoqExecutionState = ExecutionState.Capture(Parameters);
            ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
            var __ko3ikoSchema = provider.GetSchema("#test");
            var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var ko3ikoRows = ko3ikoRowsSource.Chunks;
            var __resultRuntimeV2RegressionLibrary0 = new Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionLibrary();
            var __musoqTableSourceRows = ko3ikoRows;
            if (__musoqTableSourceRows is not IReadOnlyList<IReadOnlyList<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>> _)
            {
                return new QueryTableEnumerable<ResultRow0>((_) => EvaluationHelper.ProjectChunkedRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity, ResultRow0>(__musoqTableSourceRows, 24, (ko3iko) => ((int)__resultRuntimeV2RegressionLibrary0.ExpensiveMethod(ko3iko.Value) > 100), (ko3iko) => new ResultRow0((ko3iko.Value * 2), ko3iko.Name), token), token, onCompleted: () =>
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }, onDisposed: () =>
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                });
            }

            var __musoqTableParallelRows = EvaluationHelper.GetParallelProjectionRowsOrEmpty<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>(__musoqTableSourceRows, 4096);
            return new QueryTableEnumerable<ResultRow0>((_) => QueryRows.FromRowShards(EvaluationHelper.ProjectRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity, ResultRow0>(__musoqTableParallelRows, 24, (ko3iko) => ((int)__resultRuntimeV2RegressionLibrary0.ExpensiveMethod(ko3iko.Value) > 100), (ko3iko) => new ResultRow0((ko3iko.Value * 2), ko3iko.Name), token)), token, onCompleted: () =>
            {
                OnPhaseChanged("compiled", QueryPhase.End);
            }, onDisposed: () =>
            {
                OnPhaseChanged("compiled", QueryPhase.End);
            });
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
            public ResultRow0(int __value0, string __value1)
            {
                Value___2 = __value0;
                Name = __value1;
            }

            public override int Count => 2;
            public string Name { get; private set; }
            public int Value___2 { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Value___2 = (int)value;
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
                "Value * 2" => true,
                "Value___2" => true,
                "Name" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Value___2,
                1 => (object)Name,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Value * 2" => (object)Value___2,
                "Value___2" => (object)Value___2,
                "Name" => (object)Name,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Value___2, string Name)
            {
                this.Value___2 = Value___2;
                this.Name = Name;
            }

            public string Name { get; }
            public int Value___2 { get; }
        }
    }
}
