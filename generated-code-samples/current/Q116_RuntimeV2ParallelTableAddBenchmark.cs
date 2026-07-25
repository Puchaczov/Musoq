// === Parsed Query ===
/*
SELECT Id, Name, Value, Category, HeavyComputation(Value) as Heavy
              FROM #test.entities()
              WHERE Value > 100
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Id as Id, ko3iko.Name as Name, ko3iko.Value as Value, ko3iko.Category as Category, HeavyComputation(ko3iko.Value) as Heavy]
    Filter [(ko3iko.Value > 100)]
      SchemaScan [#test.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Id as Id, ko3iko.Name as Name, ko3iko.Value as Value, ko3iko.Category as Category, HeavyComputation(ko3iko.Value) as Heavy]
    PhysicalFilter [(ko3iko.Value > 100)]
      PhysicalSchemaScan [#test.entities() as ko3iko] [pushdown: (ko3iko.Value > 100)]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: RuntimeV2RegressionEntity]
      Id: int <- property Id
      Name: string <- property Name
      Value: int <- property Value
      Category: string <- property Category
    Generated [ResultRow0]
      Id: int <- field Id
      Name: string <- field Name
      Value: int <- field Value
      Category: string <- field Category
      Heavy: int <- field Heavy

  Body
    SourceScan [ko3iko: RuntimeV2RegressionEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateObject [__resultRuntimeV2RegressionLibrary0: RuntimeV2RegressionLibrary]
    ParallelFilterProjectLoop [ko3iko in ko3ikoRows where (ko3iko.Value > 100); threshold 4096, maxDegree 24]
      ParallelProject
        Let [value: int = ko3iko.Value]
        If [(value > 100)]
          AppendShape [result <- ResultShape0(Id: ko3iko.Id, Name: ko3iko.Name, Value: value, Category: ko3iko.Category, Heavy: HeavyComputation(value))]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q116_RuntimeV2ParallelTableAddBenchmark
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
            new Column("Id", typeof(int), 0),
            new Column("Name", typeof(string), 1),
            new Column("Value", typeof(int), 2),
            new Column("Category", typeof(string), 3),
            new Column("Heavy", typeof(int), 4)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Name", typeof(string), 1), new Column("Value", typeof(int), 5), new Column("Category", typeof(string), 6) });
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
                return new QueryTableEnumerable<ResultRow0>((_) => EvaluationHelper.ProjectChunkedRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity, ResultRow0>(__musoqTableSourceRows, 24, (ko3iko) => (ko3iko.Value > 100), (ko3iko) => new ResultRow0(ko3iko.Id, ko3iko.Name, ko3iko.Value, ko3iko.Category, (int)__resultRuntimeV2RegressionLibrary0.HeavyComputation(ko3iko.Value)), token), token, onCompleted: () =>
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }, onDisposed: () =>
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                });
            }

            var __musoqTableParallelRows = EvaluationHelper.GetParallelProjectionRowsOrEmpty<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>(__musoqTableSourceRows, 4096);
            return new QueryTableEnumerable<ResultRow0>((_) => QueryRows.FromRowShards(EvaluationHelper.ProjectRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity, ResultRow0>(__musoqTableParallelRows, 24, (ko3iko) => (ko3iko.Value > 100), (ko3iko) => new ResultRow0(ko3iko.Id, ko3iko.Name, ko3iko.Value, ko3iko.Category, (int)__resultRuntimeV2RegressionLibrary0.HeavyComputation(ko3iko.Value)), token)), token, onCompleted: () =>
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
            public ResultRow0(int __value0, string __value1, int __value2, string __value3, int __value4)
            {
                Id = __value0;
                Name = __value1;
                Value = __value2;
                Category = __value3;
                Heavy = __value4;
            }

            public string Category { get; private set; }
            public override int Count => 5;
            public int Heavy { get; private set; }
            public int Id { get; private set; }
            public string Name { get; private set; }
            public int Value { get; private set; }

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
                    case 2:
                        Value = (int)value;
                        break;
                    case 3:
                        Category = (string)value;
                        break;
                    case 4:
                        Heavy = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Name" => true,
                "Value" => true,
                "Category" => true,
                "Heavy" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Id,
                1 => (object)Name,
                2 => (object)Value,
                3 => (object)Category,
                4 => (object)Heavy,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Id" => (object)Id,
                "Name" => (object)Name,
                "Value" => (object)Value,
                "Category" => (object)Category,
                "Heavy" => (object)Heavy,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id, string Name, int Value, string Category, int Heavy)
            {
                this.Id = Id;
                this.Name = Name;
                this.Value = Value;
                this.Category = Category;
                this.Heavy = Heavy;
            }

            public string Category { get; }
            public int Heavy { get; }
            public int Id { get; }
            public string Name { get; }
            public int Value { get; }
        }
    }
}
