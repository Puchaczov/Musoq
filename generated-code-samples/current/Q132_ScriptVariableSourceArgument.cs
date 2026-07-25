// === Parsed Query ===
/*
let prefix: string = 'KEY'
                      let key: string = $prefix + '_1'
                      select Key, Value
                      from #parameterized.items($key)
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Key as Key, ko3iko.Value as Value]
    SchemaScan [#parameterized.items($key) as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Key as Key, ko3iko.Value as Value]
    PhysicalSchemaScan [#parameterized.items($key) as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: ScriptParameterSampleEntity]
      Key: string <- reflected member Key
      Value: string <- reflected member Value
    Generated [ResultRow0]
      Key: string <- field Key
      Value: string <- field Value

  Body
    SourceScan [ko3iko: object] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [ko3iko in ko3ikoRows]
      AppendShape [result <- ResultShape0(Key: ko3iko.Key, Value: ko3iko.Value)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q132_ScriptVariableSourceArgument
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
            new Column("Key", typeof(string), 0),
            new Column("Value", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Key", typeof(string), 0), new Column("Value", typeof(string), 1) });
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
            OnPhaseChanged("compiled", QueryPhase.Select);
            var __musoqExecutionState = ExecutionState.Capture(Parameters);
            ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
            const string letKey = "KEY_1";
            var __reflected_ko3iko_Key_0 = EvaluationHelper.GetNestedValueAccessor(EvaluationHelper.GetRequiredType("Musoq.Evaluator.Tests.GeneratedCodeSamplesCatalog+ScriptParameterSampleEntity, Musoq.Evaluator.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"), "Key");
            var __reflected_ko3iko_Value_1 = EvaluationHelper.GetNestedValueAccessor(EvaluationHelper.GetRequiredType("Musoq.Evaluator.Tests.GeneratedCodeSamplesCatalog+ScriptParameterSampleEntity, Musoq.Evaluator.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"), "Value");
            var __ko3ikoSchema = provider.GetSchema("#parameterized");
            var ko3ikoRows = EvaluationHelper.GetRowSourceChunks(__ko3ikoSchema, EvaluationHelper.GetRequiredType("Musoq.Evaluator.Tests.GeneratedCodeSamplesCatalog+ScriptParameterSampleEntity, Musoq.Evaluator.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"), "items", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), new object[] { letKey });
            var __musoqTableSourceRows = ko3ikoRows;
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectRowsSerial<object, ResultRow0>(__musoqTableSourceRows, (ko3iko) => true, (ko3iko) => new ResultRow0((string)__reflected_ko3iko_Key_0(ko3iko), (string)__reflected_ko3iko_Value_1(ko3iko)), token), token, onCompleted: () =>
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
                Key = __value0;
                Value = __value1;
            }

            public override int Count => 2;
            public string Key { get; private set; }
            public string Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Key = (string)value;
                        break;
                    case 1:
                        Value = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Key" => true,
                "Value" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Key,
                1 => (object)Value,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Key" => (object)Key,
                "Value" => (object)Value,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Key, string Value)
            {
                this.Key = Key;
                this.Value = Value;
            }

            public string Key { get; }
            public string Value { get; }
        }
    }
}
