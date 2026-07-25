// === Parsed Query ===
/*
SELECT
                Id,
                Value,
                Value * 2,
                Value + 100,
                ExpensiveCompute(Value),
                ExpensiveCompute(Value) * 2,
                ExpensiveCompute(Value) + Value,
                Name,
                StringTransform(Name),
                Category,
                CASE
                    WHEN Value > 500 AND ExpensiveCompute(Value) > 1000 THEN 'VeryHigh'
                    WHEN Value > 200 AND ExpensiveCompute(Value) > 500 THEN 'High'
                    WHEN Value > 100 THEN 'Medium'
                    ELSE 'Low'
                END as Classification,
                Value + ExpensiveCompute(Value) + Value * 2
            FROM #test.entities()
            WHERE Value > 50
              AND ExpensiveCompute(Value) > 0
              AND Value < 900
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Id as Id, ko3iko.Value as Value, (ko3iko.Value * 2) as Value * 2, (ko3iko.Value + 100) as Value + 100, ExpensiveCompute(ko3iko.Value) as ExpensiveCompute(Value), (ExpensiveCompute(ko3iko.Value) * 2) as ExpensiveCompute(Value) * 2, (ExpensiveCompute(ko3iko.Value) + ko3iko.Value) as ExpensiveCompute(Value) + Value, ko3iko.Name as Name, StringTransform(ko3iko.Name) as StringTransform(Name), ko3iko.Category as Category, CASE WHEN ((ko3iko.Value > 500) AND (ExpensiveCompute(ko3iko.Value) > 1000)) THEN 'VeryHigh' WHEN ((ko3iko.Value > 200) AND (ExpensiveCompute(ko3iko.Value) > 500)) THEN 'High' WHEN (ko3iko.Value > 100) THEN 'Medium' ELSE 'Low' END as Classification, ((ko3iko.Value + ExpensiveCompute(ko3iko.Value)) + (ko3iko.Value * 2)) as Value + ExpensiveCompute(Value) + Value * 2]
    Filter [(((ko3iko.Value > 50) AND (ExpensiveCompute(ko3iko.Value) > 0)) AND (ko3iko.Value < 900))]
      SchemaScan [#test.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Id as Id, ko3iko.Value as Value, (ko3iko.Value * 2) as Value * 2, (ko3iko.Value + 100) as Value + 100, ExpensiveCompute(ko3iko.Value) as ExpensiveCompute(Value), (ExpensiveCompute(ko3iko.Value) * 2) as ExpensiveCompute(Value) * 2, (ExpensiveCompute(ko3iko.Value) + ko3iko.Value) as ExpensiveCompute(Value) + Value, ko3iko.Name as Name, StringTransform(ko3iko.Name) as StringTransform(Name), ko3iko.Category as Category, CASE WHEN ((ko3iko.Value > 500) AND (ExpensiveCompute(ko3iko.Value) > 1000)) THEN 'VeryHigh' WHEN ((ko3iko.Value > 200) AND (ExpensiveCompute(ko3iko.Value) > 500)) THEN 'High' WHEN (ko3iko.Value > 100) THEN 'Medium' ELSE 'Low' END as Classification, ((ko3iko.Value + ExpensiveCompute(ko3iko.Value)) + (ko3iko.Value * 2)) as Value + ExpensiveCompute(Value) + Value * 2]
    PhysicalFilter [(((ko3iko.Value > 50) AND (ExpensiveCompute(ko3iko.Value) > 0)) AND (ko3iko.Value < 900))]
      PhysicalSchemaScan [#test.entities() as ko3iko] [pushdown: (ko3iko.Value > 50), (ko3iko.Value < 900)]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BenchmarkParityEntity]
      Id: int <- property Id
      Name: string <- property Name
      Value: int <- property Value
      Category: string <- property Category
    Generated [ResultRow0]
      Id: int <- field Id
      Value: int <- field Value
      Value * 2: int <- field Value___2
      Value + 100: int <- field Value___100
      ExpensiveCompute(Value): decimal <- field ExpensiveCompute_Value_
      ExpensiveCompute(Value) * 2: decimal <- field ExpensiveCompute_Value____2
      ExpensiveCompute(Value) + Value: decimal <- field ExpensiveCompute_Value____Value
      Name: string <- field Name
      StringTransform(Name): string <- field StringTransform_Name_
      Category: string <- field Category
      Classification: string <- field Classification
      Value + ExpensiveCompute(Value) + Value * 2: decimal <- field Value___ExpensiveCompute_Value____Value___2

  Body
    SourceScan [ko3iko: BenchmarkParityEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateObject [__resultBenchmarkParityLibrary0: BenchmarkParityLibrary]
    ParallelFilterProjectLoop [ko3iko in ko3ikoRows where (((ko3iko.Value > 50) AND (ExpensiveCompute(ko3iko.Value) > 0)) AND (ko3iko.Value < 900)); threshold 4096, maxDegree 24]
      ParallelProject
        Let [value: int = ko3iko.Value]
        If [(((value > 50) AND (ExpensiveCompute(value) > 0)) AND (value < 900))]
          Let [name: string = ko3iko.Name]
          Let [__expr: int = (value * 2)]
          Let [expensiveCompute: decimal = ExpensiveCompute(value)]
          AppendShape [result <- ResultShape0(Id: ko3iko.Id, Value: value, Value * 2: __expr, Value + 100: (value + 100), ExpensiveCompute(Value): expensiveCompute, ExpensiveCompute(Value) * 2: (expensiveCompute * 2), ExpensiveCompute(Value) + Value: (expensiveCompute + value), Name: name, StringTransform(Name): StringTransform(name), Category: ko3iko.Category, Classification: CASE WHEN ((value > 500) AND (expensiveCompute > 1000)) THEN 'VeryHigh' WHEN ((value > 200) AND (expensiveCompute > 500)) THEN 'High' WHEN (value > 100) THEN 'Medium' ELSE 'Low' END, Value + ExpensiveCompute(Value) + Value * 2: ((value + expensiveCompute) + __expr))]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q179_BenchmarkOptimizedHeavyMixedMaterialized
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
            new Column("Value", typeof(int), 1),
            new Column("Value * 2", typeof(int), 2),
            new Column("Value + 100", typeof(int), 3),
            new Column("ExpensiveCompute(Value)", typeof(decimal), 4),
            new Column("ExpensiveCompute(Value) * 2", typeof(decimal), 5),
            new Column("ExpensiveCompute(Value) + Value", typeof(decimal), 6),
            new Column("Name", typeof(string), 7),
            new Column("StringTransform(Name)", typeof(string), 8),
            new Column("Category", typeof(string), 9),
            new Column("Classification", typeof(string), 10),
            new Column("Value + ExpensiveCompute(Value) + Value * 2", typeof(decimal), 11)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Name", typeof(string), 1), new Column("Value", typeof(int), 2), new Column("Category", typeof(string), 3) });
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
            var __musoqMaterializedTable = QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
            _ = __musoqMaterializedTable.Count;
            return __musoqMaterializedTable;
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.Where);
            OnPhaseChanged("compiled", QueryPhase.Select);
            var __musoqExecutionState = ExecutionState.Capture(Parameters);
            ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
            var __resultExpensiveComputeCache0 = new System.Collections.Concurrent.ConcurrentDictionary<int, decimal>();
            var __ko3ikoSchema = provider.GetSchema("#test");
            var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var ko3ikoRows = ko3ikoRowsSource.Chunks;
            var __resultBenchmarkParityLibrary0 = new Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityLibrary();
            var __musoqTableSourceRows = ko3ikoRows;
            if (__musoqTableSourceRows is not IReadOnlyList<IReadOnlyList<Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityEntity>> _)
            {
                return new QueryTableEnumerable<ResultRow0>((_) => EvaluationHelper.ProjectChunkedRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityEntity, ResultRow0>(__musoqTableSourceRows, 24, (ko3iko) =>
                {
                    int value = ko3iko.Value;
                    if ((((value > 50) && ((decimal)EvaluationHelper.GetOrAddCachedMethod<Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityLibrary, int, decimal>(__resultExpensiveComputeCache0, __resultBenchmarkParityLibrary0, value, static (__cacheTarget, __cacheKey) => __cacheTarget.ExpensiveCompute(__cacheKey)) > 0)) && (value < 900)))
                    {
                        string name = ko3iko.Name;
                        int __expr = (value * 2);
                        decimal expensiveCompute = (decimal)__resultBenchmarkParityLibrary0.ExpensiveCompute(value);
                        return new ResultRow0(ko3iko.Id, value, __expr, (value + 100), expensiveCompute, (expensiveCompute * 2), (expensiveCompute + value), name, (string)__resultBenchmarkParityLibrary0.StringTransform(name), ko3iko.Category, ((value > 500) && (expensiveCompute > 1000)) ? (string)"VeryHigh" : (((value > 200) && (expensiveCompute > 500)) ? (string)"High" : ((value > 100) ? (string)"Medium" : (string)"Low")), ((value + expensiveCompute) + __expr));
                    }

                    return null;
                }, token), token, onCompleted: () =>
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }, onDisposed: () =>
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                });
            }

            var __musoqTableParallelRows = EvaluationHelper.GetParallelProjectionRowsOrEmpty<Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityEntity>(__musoqTableSourceRows, 4096);
            return new QueryTableEnumerable<ResultRow0>((_) => QueryRows.FromRowShards(EvaluationHelper.ProjectRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityEntity, ResultRow0>(__musoqTableParallelRows, 24, (ko3iko) =>
            {
                int value = ko3iko.Value;
                if ((((value > 50) && ((decimal)EvaluationHelper.GetOrAddCachedMethod<Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityLibrary, int, decimal>(__resultExpensiveComputeCache0, __resultBenchmarkParityLibrary0, value, static (__cacheTarget, __cacheKey) => __cacheTarget.ExpensiveCompute(__cacheKey)) > 0)) && (value < 900)))
                {
                    string name = ko3iko.Name;
                    int __expr = (value * 2);
                    decimal expensiveCompute = (decimal)__resultBenchmarkParityLibrary0.ExpensiveCompute(value);
                    return new ResultRow0(ko3iko.Id, value, __expr, (value + 100), expensiveCompute, (expensiveCompute * 2), (expensiveCompute + value), name, (string)__resultBenchmarkParityLibrary0.StringTransform(name), ko3iko.Category, ((value > 500) && (expensiveCompute > 1000)) ? (string)"VeryHigh" : (((value > 200) && (expensiveCompute > 500)) ? (string)"High" : ((value > 100) ? (string)"Medium" : (string)"Low")), ((value + expensiveCompute) + __expr));
                }

                return null;
            }, token)), token, onCompleted: () =>
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
            public ResultRow0(int __value0, int __value1, int __value2, int __value3, decimal __value4, decimal __value5, decimal __value6, string __value7, string __value8, string __value9, string __value10, decimal __value11)
            {
                Id = __value0;
                Value = __value1;
                Value___2 = __value2;
                Value___100 = __value3;
                ExpensiveCompute_Value_ = __value4;
                ExpensiveCompute_Value____2 = __value5;
                ExpensiveCompute_Value____Value = __value6;
                Name = __value7;
                StringTransform_Name_ = __value8;
                Category = __value9;
                Classification = __value10;
                Value___ExpensiveCompute_Value____Value___2 = __value11;
            }

            public string Category { get; private set; }
            public string Classification { get; private set; }
            public override int Count => 12;
            public decimal ExpensiveCompute_Value_ { get; private set; }
            public decimal ExpensiveCompute_Value____2 { get; private set; }
            public decimal ExpensiveCompute_Value____Value { get; private set; }
            public int Id { get; private set; }
            public string Name { get; private set; }
            public string StringTransform_Name_ { get; private set; }
            public int Value { get; private set; }
            public int Value___100 { get; private set; }
            public int Value___2 { get; private set; }
            public decimal Value___ExpensiveCompute_Value____Value___2 { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Id = (int)value;
                        break;
                    case 1:
                        Value = (int)value;
                        break;
                    case 2:
                        Value___2 = (int)value;
                        break;
                    case 3:
                        Value___100 = (int)value;
                        break;
                    case 4:
                        ExpensiveCompute_Value_ = (decimal)value;
                        break;
                    case 5:
                        ExpensiveCompute_Value____2 = (decimal)value;
                        break;
                    case 6:
                        ExpensiveCompute_Value____Value = (decimal)value;
                        break;
                    case 7:
                        Name = (string)value;
                        break;
                    case 8:
                        StringTransform_Name_ = (string)value;
                        break;
                    case 9:
                        Category = (string)value;
                        break;
                    case 10:
                        Classification = (string)value;
                        break;
                    case 11:
                        Value___ExpensiveCompute_Value____Value___2 = (decimal)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Value" => true,
                "Value * 2" => true,
                "Value___2" => true,
                "Value + 100" => true,
                "Value___100" => true,
                "ExpensiveCompute(Value)" => true,
                "ExpensiveCompute_Value_" => true,
                "ExpensiveCompute(Value) * 2" => true,
                "ExpensiveCompute_Value____2" => true,
                "ExpensiveCompute(Value) + Value" => true,
                "ExpensiveCompute_Value____Value" => true,
                "Name" => true,
                "StringTransform(Name)" => true,
                "StringTransform_Name_" => true,
                "Category" => true,
                "Classification" => true,
                "Value + ExpensiveCompute(Value) + Value * 2" => true,
                "Value___ExpensiveCompute_Value____Value___2" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Id,
                1 => (object)Value,
                2 => (object)Value___2,
                3 => (object)Value___100,
                4 => (object)ExpensiveCompute_Value_,
                5 => (object)ExpensiveCompute_Value____2,
                6 => (object)ExpensiveCompute_Value____Value,
                7 => (object)Name,
                8 => (object)StringTransform_Name_,
                9 => (object)Category,
                10 => (object)Classification,
                11 => (object)Value___ExpensiveCompute_Value____Value___2,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Id" => (object)Id,
                "Value" => (object)Value,
                "Value * 2" => (object)Value___2,
                "Value___2" => (object)Value___2,
                "Value + 100" => (object)Value___100,
                "Value___100" => (object)Value___100,
                "ExpensiveCompute(Value)" => (object)ExpensiveCompute_Value_,
                "ExpensiveCompute_Value_" => (object)ExpensiveCompute_Value_,
                "ExpensiveCompute(Value) * 2" => (object)ExpensiveCompute_Value____2,
                "ExpensiveCompute_Value____2" => (object)ExpensiveCompute_Value____2,
                "ExpensiveCompute(Value) + Value" => (object)ExpensiveCompute_Value____Value,
                "ExpensiveCompute_Value____Value" => (object)ExpensiveCompute_Value____Value,
                "Name" => (object)Name,
                "StringTransform(Name)" => (object)StringTransform_Name_,
                "StringTransform_Name_" => (object)StringTransform_Name_,
                "Category" => (object)Category,
                "Classification" => (object)Classification,
                "Value + ExpensiveCompute(Value) + Value * 2" => (object)Value___ExpensiveCompute_Value____Value___2,
                "Value___ExpensiveCompute_Value____Value___2" => (object)Value___ExpensiveCompute_Value____Value___2,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id, int Value, int Value___2, int Value___100, decimal ExpensiveCompute_Value_, decimal ExpensiveCompute_Value____2, decimal ExpensiveCompute_Value____Value, string Name, string StringTransform_Name_, string Category, string Classification, decimal Value___ExpensiveCompute_Value____Value___2)
            {
                this.Id = Id;
                this.Value = Value;
                this.Value___2 = Value___2;
                this.Value___100 = Value___100;
                this.ExpensiveCompute_Value_ = ExpensiveCompute_Value_;
                this.ExpensiveCompute_Value____2 = ExpensiveCompute_Value____2;
                this.ExpensiveCompute_Value____Value = ExpensiveCompute_Value____Value;
                this.Name = Name;
                this.StringTransform_Name_ = StringTransform_Name_;
                this.Category = Category;
                this.Classification = Classification;
                this.Value___ExpensiveCompute_Value____Value___2 = Value___ExpensiveCompute_Value____Value___2;
            }

            public string Category { get; }
            public string Classification { get; }
            public decimal ExpensiveCompute_Value_ { get; }
            public decimal ExpensiveCompute_Value____2 { get; }
            public decimal ExpensiveCompute_Value____Value { get; }
            public int Id { get; }
            public string Name { get; }
            public string StringTransform_Name_ { get; }
            public int Value { get; }
            public int Value___100 { get; }
            public int Value___2 { get; }
            public decimal Value___ExpensiveCompute_Value____Value___2 { get; }
        }
    }
}
