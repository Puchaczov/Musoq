/*
raw query string

SELECT Name,
                     CASE WHEN ExpensiveMethod(Value) > 200 THEN 'High' ELSE 'Low' END
              FROM #test.entities()
*/

/*
logical plan representation string

MultiStatement
  Project [ko3iko.Name as Name, CASE WHEN (ExpensiveMethod(ko3iko.Value) > 200) THEN 'High' ELSE 'Low' END as case when ExpensiveMethod(Value) > 200 then High else Low end]
    SchemaScan [#test.entities() as ko3iko]
*/

/*
physical plan representation string

PhysicalMultiStatement
  PhysicalProject [ko3iko.Name as Name, CASE WHEN (ExpensiveMethod(ko3iko.Value) > 200) THEN 'High' ELSE 'Low' END as case when ExpensiveMethod(Value) > 200 then High else Low end]
    PhysicalSchemaScan [#test.entities() as ko3iko]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BenchmarkParityEntity]
      Name: string <- property Name
      Value: int <- property Value
    Generated [ResultRow0]
      Name: string <- field Name
      case when ExpensiveMethod(Value) > 200 then High else Low end: string <- field case_when_ExpensiveMethod_Value____200_then_High_else_Low_end

  Body
    SourceScan [ko3iko: BenchmarkParityEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateObject [__resultBenchmarkParityLibrary0: BenchmarkParityLibrary]
    ParallelFilterProjectLoop [ko3iko in ko3ikoRows; threshold 4096, maxDegree 24]
      ParallelProject
        AppendShape [result <- ResultShape0(Name: ko3iko.Name, case when ExpensiveMethod(Value) > 200 then High else Low end: CASE WHEN (ExpensiveMethod(ko3iko.Value) > 200) THEN 'High' ELSE 'Low' END)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q177_BenchmarkCseCaseNoDuplicateMaterialized
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
            new Column("Name", typeof(string), 0),
            new Column("case when ExpensiveMethod(Value) > 200 then High else Low end", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 1), new Column("Value", typeof(int), 2) });
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
            OnPhaseChanged("compiled", QueryPhase.Select);
            var __musoqExecutionState = ExecutionState.Capture(Parameters);
            ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
            var __ko3ikoSchema = provider.GetSchema("#test");
            var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var ko3ikoRows = ko3ikoRowsSource.Chunks;
            var __resultBenchmarkParityLibrary0 = new Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityLibrary();
            var __musoqTableSourceRows = ko3ikoRows;
            if (__musoqTableSourceRows is not IReadOnlyList<IReadOnlyList<Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityEntity>> _)
            {
                return new QueryTableEnumerable<ResultRow0>((_) => EvaluationHelper.ProjectChunkedRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityEntity, ResultRow0>(__musoqTableSourceRows, 24, (ko3iko) => true, (ko3iko) => new ResultRow0(ko3iko.Name, ((int)__resultBenchmarkParityLibrary0.ExpensiveMethod(ko3iko.Value) > 200) ? (string)"High" : (string)"Low"), token), token, onCompleted: () =>
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }, onDisposed: () =>
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                });
            }

            var __musoqTableParallelRows = EvaluationHelper.GetParallelProjectionRowsOrEmpty<Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityEntity>(__musoqTableSourceRows, 4096);
            return new QueryTableEnumerable<ResultRow0>((_) => QueryRows.FromRowShards(EvaluationHelper.ProjectRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityEntity, ResultRow0>(__musoqTableParallelRows, 24, (ko3iko) => true, (ko3iko) => new ResultRow0(ko3iko.Name, ((int)__resultBenchmarkParityLibrary0.ExpensiveMethod(ko3iko.Value) > 200) ? (string)"High" : (string)"Low"), token)), token, onCompleted: () =>
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
            public ResultRow0(string __value0, string __value1)
            {
                Name = __value0;
                case_when_ExpensiveMethod_Value____200_then_High_else_Low_end = __value1;
            }

            public override int Count => 2;
            public string Name { get; private set; }
            public string case_when_ExpensiveMethod_Value____200_then_High_else_Low_end { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        case_when_ExpensiveMethod_Value____200_then_High_else_Low_end = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "case when ExpensiveMethod(Value) > 200 then High else Low end" => true,
                "case_when_ExpensiveMethod_Value____200_then_High_else_Low_end" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)case_when_ExpensiveMethod_Value____200_then_High_else_Low_end,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "case when ExpensiveMethod(Value) > 200 then High else Low end" => (object)case_when_ExpensiveMethod_Value____200_then_High_else_Low_end,
                "case_when_ExpensiveMethod_Value____200_then_High_else_Low_end" => (object)case_when_ExpensiveMethod_Value____200_then_High_else_Low_end,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Name, string case_when_ExpensiveMethod_Value____200_then_High_else_Low_end)
            {
                this.Name = Name;
                this.case_when_ExpensiveMethod_Value____200_then_High_else_Low_end = case_when_ExpensiveMethod_Value____200_then_High_else_Low_end;
            }

            public string Name { get; }
            public string case_when_ExpensiveMethod_Value____200_then_High_else_Low_end { get; }
        }
    }
}
