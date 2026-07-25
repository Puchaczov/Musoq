// === Parsed Query ===
/*
SELECT ExpensiveCompute(Value) as Computed,
                     ExpensiveCompute(Value) + 10 as PlusTen,
                     CASE WHEN ExpensiveCompute(Value) > 300 THEN 'High' ELSE 'Low' END as Bucket
              FROM #test.entities()
              WHERE ExpensiveCompute(Value) > 50
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ExpensiveCompute(ko3iko.Value) as Computed, (ExpensiveCompute(ko3iko.Value) + 10) as PlusTen, CASE WHEN (ExpensiveCompute(ko3iko.Value) > 300) THEN 'High' ELSE 'Low' END as Bucket]
    Filter [(ExpensiveCompute(ko3iko.Value) > 50)]
      SchemaScan [#test.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ExpensiveCompute(ko3iko.Value) as Computed, (ExpensiveCompute(ko3iko.Value) + 10) as PlusTen, CASE WHEN (ExpensiveCompute(ko3iko.Value) > 300) THEN 'High' ELSE 'Low' END as Bucket]
    PhysicalFilter [(ExpensiveCompute(ko3iko.Value) > 50)]
      PhysicalSchemaScan [#test.entities() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: RuntimeV2RegressionEntity]
      Value: int <- property Value
    Generated [ResultRow0]
      Computed: int <- field Computed
      PlusTen: int <- field PlusTen
      Bucket: string <- field Bucket

  Body
    SourceScan [ko3iko: RuntimeV2RegressionEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateObject [__resultRuntimeV2RegressionLibrary0: RuntimeV2RegressionLibrary]
    ParallelFilterProjectLoop [ko3iko in ko3ikoRows where (ExpensiveCompute(ko3iko.Value) > 50); threshold 4096, maxDegree 24]
      ParallelProject
        If [(ExpensiveCompute(ko3iko.Value) > 50)]
          AppendShape [result <- ResultShape0(Computed: ExpensiveCompute(ko3iko.Value), PlusTen: (ExpensiveCompute(ko3iko.Value) + 10), Bucket: CASE WHEN (ExpensiveCompute(ko3iko.Value) > 300) THEN 'High' ELSE 'Low' END)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q105_RuntimeV2DeterministicMethodCseDisabled
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
            new Column("Computed", typeof(int), 0),
            new Column("PlusTen", typeof(int), 1),
            new Column("Bucket", typeof(string), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Value", typeof(int), 5) });
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
                return new QueryTableEnumerable<ResultRow0>((_) => EvaluationHelper.ProjectChunkedRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity, ResultRow0>(__musoqTableSourceRows, 24, (ko3iko) =>
                {
                    if (((int)__resultRuntimeV2RegressionLibrary0.ExpensiveCompute(ko3iko.Value) > 50))
                    {
                        return new ResultRow0((int)__resultRuntimeV2RegressionLibrary0.ExpensiveCompute(ko3iko.Value), (int)((int)__resultRuntimeV2RegressionLibrary0.ExpensiveCompute(ko3iko.Value) + 10), ((int)__resultRuntimeV2RegressionLibrary0.ExpensiveCompute(ko3iko.Value) > 300) ? (string)"High" : (string)"Low");
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

            var __musoqTableParallelRows = EvaluationHelper.GetParallelProjectionRowsOrEmpty<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>(__musoqTableSourceRows, 4096);
            return new QueryTableEnumerable<ResultRow0>((_) => QueryRows.FromRowShards(EvaluationHelper.ProjectRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity, ResultRow0>(__musoqTableParallelRows, 24, (ko3iko) =>
            {
                if (((int)__resultRuntimeV2RegressionLibrary0.ExpensiveCompute(ko3iko.Value) > 50))
                {
                    return new ResultRow0((int)__resultRuntimeV2RegressionLibrary0.ExpensiveCompute(ko3iko.Value), (int)((int)__resultRuntimeV2RegressionLibrary0.ExpensiveCompute(ko3iko.Value) + 10), ((int)__resultRuntimeV2RegressionLibrary0.ExpensiveCompute(ko3iko.Value) > 300) ? (string)"High" : (string)"Low");
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
            public ResultRow0(int __value0, int __value1, string __value2)
            {
                Computed = __value0;
                PlusTen = __value1;
                Bucket = __value2;
            }

            public string Bucket { get; private set; }
            public int Computed { get; private set; }
            public override int Count => 3;
            public int PlusTen { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Computed = (int)value;
                        break;
                    case 1:
                        PlusTen = (int)value;
                        break;
                    case 2:
                        Bucket = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Computed" => true,
                "PlusTen" => true,
                "Bucket" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Computed,
                1 => (object)PlusTen,
                2 => (object)Bucket,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Computed" => (object)Computed,
                "PlusTen" => (object)PlusTen,
                "Bucket" => (object)Bucket,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Computed, int PlusTen, string Bucket)
            {
                this.Computed = Computed;
                this.PlusTen = PlusTen;
                this.Bucket = Bucket;
            }

            public string Bucket { get; }
            public int Computed { get; }
            public int PlusTen { get; }
        }
    }
}
