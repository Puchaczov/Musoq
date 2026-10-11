// === Parsed Query ===
/*
SELECT
                Value,
                ExpensiveCompute(Value),
                Value + ExpensiveCompute(Value),
                Value * ExpensiveCompute(Value),
                Name,
                StringTransform(Name),
                Name + '_' + StringTransform(Name)
            FROM #test.entities()
            WHERE Value > 100
              AND ExpensiveCompute(Value) > 50
              AND Name IS NOT NULL
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Value as Value, ExpensiveCompute(ko3iko.Value) as ExpensiveCompute(Value), (ko3iko.Value + ExpensiveCompute(ko3iko.Value)) as Value + ExpensiveCompute(Value), (ko3iko.Value * ExpensiveCompute(ko3iko.Value)) as Value * ExpensiveCompute(Value), ko3iko.Name as Name, StringTransform(ko3iko.Name) as StringTransform(Name), ((ko3iko.Name || '_') || StringTransform(ko3iko.Name)) as Name + _ + StringTransform(Name)]
    Filter [(((ko3iko.Value > 100) AND (ExpensiveCompute(ko3iko.Value) > 50)) AND ko3iko.Name IS NOT NULL)]
      SchemaScan [#test.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Value as Value, ExpensiveCompute(ko3iko.Value) as ExpensiveCompute(Value), (ko3iko.Value + ExpensiveCompute(ko3iko.Value)) as Value + ExpensiveCompute(Value), (ko3iko.Value * ExpensiveCompute(ko3iko.Value)) as Value * ExpensiveCompute(Value), ko3iko.Name as Name, StringTransform(ko3iko.Name) as StringTransform(Name), ((ko3iko.Name || '_') || StringTransform(ko3iko.Name)) as Name + _ + StringTransform(Name)]
    PhysicalFilter [(((ko3iko.Value > 100) AND (ExpensiveCompute(ko3iko.Value) > 50)) AND ko3iko.Name IS NOT NULL)]
      PhysicalSchemaScan [#test.entities() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BenchmarkParityEntity]
      Name: string <- property Name
      Value: int <- property Value
    Generated [ResultRow0]
      Value: int <- field Value
      ExpensiveCompute(Value): decimal <- field ExpensiveCompute_Value_
      Value + ExpensiveCompute(Value): decimal <- field Value___ExpensiveCompute_Value_
      Value * ExpensiveCompute(Value): decimal <- field Value___ExpensiveCompute_Value__1
      Name: string <- field Name
      StringTransform(Name): string <- field StringTransform_Name_
      Name + _ + StringTransform(Name): string <- field Name_______StringTransform_Name_

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: BenchmarkParityEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateObject [__resultBenchmarkParityLibrary0: BenchmarkParityLibrary]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ParallelFilterProjectLoop [ko3iko in ko3ikoRows where (((ko3iko.Value > 100) AND (ExpensiveCompute(ko3iko.Value) > 50)) AND ko3iko.Name IS NOT NULL); threshold 4096, maxDegree 24]
      ParallelProject
        Let [value: int = ko3iko.Value]
        Let [name: string = ko3iko.Name]
        If [(value > 100)]
          Let [expensiveCompute: decimal = ExpensiveCompute(value)]
          If [((expensiveCompute > 50) AND name IS NOT NULL)]
            Let [stringTransform: string = StringTransform(name)]
            AppendShape [result <- ResultShape0(Value: value, ExpensiveCompute(Value): expensiveCompute, Value + ExpensiveCompute(Value): (value + expensiveCompute), Value * ExpensiveCompute(Value): (value * expensiveCompute), Name: name, StringTransform(Name): stringTransform, Name + _ + StringTransform(Name): ((name || '_') || stringTransform))]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q180_BenchmarkOptimizedMixedColumnMethodMaterialized
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Value", typeof(int), 0),
            new Column("ExpensiveCompute(Value)", typeof(decimal), 1),
            new Column("Value + ExpensiveCompute(Value)", typeof(decimal), 2),
            new Column("Value * ExpensiveCompute(Value)", typeof(decimal), 3),
            new Column("Name", typeof(string), 4),
            new Column("StringTransform(Name)", typeof(string), 5),
            new Column("Name + _ + StringTransform(Name)", typeof(string), 6)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("Value", typeof(int), 1) });
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
            var __musoqMaterializedTable = QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
            _ = __musoqMaterializedTable.Count;
            return __musoqMaterializedTable;
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
            ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
            this.OnPhaseChanged("compiled", QueryPhase.Begin);
            this.OnPhaseChanged("compiled", QueryPhase.From);
            var __ko3ikoSchema = provider.GetSchema("#test");
            var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityEntity>.Open(__ko3ikoSchema, "entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
            var __resultBenchmarkParityLibrary0 = new Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityLibrary();
            var __musoqTableSourceRows = ko3ikoRows;
            this.OnPhaseChanged("compiled", QueryPhase.Where);
            this.OnPhaseChanged("compiled", QueryPhase.Select);
            if (__musoqTableSourceRows is not IReadOnlyList<IReadOnlyList<Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityEntity>> _)
            {
                return new QueryTableEnumerable<ResultRow0>((_) => EvaluationHelper.ProjectChunkedRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityEntity, ResultRow0>(__musoqTableSourceRows, 24, (ko3iko) =>
                {
                    int value = ko3iko.Value;
                    string name = ko3iko.Name;
                    if ((value > 100))
                    {
                        decimal expensiveCompute = (decimal)__resultBenchmarkParityLibrary0.ExpensiveCompute(value);
                        if (((expensiveCompute > 50) && (name != null)))
                        {
                            string stringTransform = (string)__resultBenchmarkParityLibrary0.StringTransform(name);
                            return new ResultRow0(value, expensiveCompute, (value + expensiveCompute), (value * expensiveCompute), name, stringTransform, ((name + "_") + stringTransform));
                        }
                    }

                    return null;
                }, token), token, onCompleted: () =>
                {
                    try
                    {
                        __musoqProgressContext?.CompleteQueryProgress();
                    }
                    finally
                    {
                        OnPhaseChanged("compiled", QueryPhase.End);
                    }
                }, onException: (Exception _) =>
                {
                    try
                    {
                        __musoqProgressContext?.CompleteQueryProgress();
                    }
                    finally
                    {
                        OnPhaseChanged("compiled", QueryPhase.End);
                    }
                }, onDisposed: () =>
                {
                    try
                    {
                        __musoqProgressContext?.CompleteQueryProgress();
                    }
                    finally
                    {
                        OnPhaseChanged("compiled", QueryPhase.End);
                    }
                });
            }

            var __musoqTableParallelRows = EvaluationHelper.GetParallelProjectionRowsOrEmpty<Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityEntity>(__musoqTableSourceRows, 4096);
            return new QueryTableEnumerable<ResultRow0>((_) => QueryRows.FromRowShards(EvaluationHelper.ProjectRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.BenchmarkParityEntity, ResultRow0>(__musoqTableParallelRows, 24, (ko3iko) =>
            {
                int value = ko3iko.Value;
                string name = ko3iko.Name;
                if ((value > 100))
                {
                    decimal expensiveCompute = (decimal)__resultBenchmarkParityLibrary0.ExpensiveCompute(value);
                    if (((expensiveCompute > 50) && (name != null)))
                    {
                        string stringTransform = (string)__resultBenchmarkParityLibrary0.StringTransform(name);
                        return new ResultRow0(value, expensiveCompute, (value + expensiveCompute), (value * expensiveCompute), name, stringTransform, ((name + "_") + stringTransform));
                    }
                }

                return null;
            }, token)), token, onCompleted: () =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }, onException: (Exception _) =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }, onDisposed: () =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
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
            public ResultRow0(int __value0, decimal __value1, decimal __value2, decimal __value3, string __value4, string __value5, string __value6)
            {
                Value = __value0;
                ExpensiveCompute_Value_ = __value1;
                Value___ExpensiveCompute_Value_ = __value2;
                Value___ExpensiveCompute_Value__1 = __value3;
                Name = __value4;
                StringTransform_Name_ = __value5;
                Name_______StringTransform_Name_ = __value6;
            }

            public override int Count => 7;
            public decimal ExpensiveCompute_Value_ { get; private set; }
            public string Name { get; private set; }
            public string Name_______StringTransform_Name_ { get; private set; }
            public string StringTransform_Name_ { get; private set; }
            public int Value { get; private set; }
            public decimal Value___ExpensiveCompute_Value_ { get; private set; }
            public decimal Value___ExpensiveCompute_Value__1 { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.ExpensiveCompute_Value_ = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Value___ExpensiveCompute_Value_ = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Value___ExpensiveCompute_Value__1 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.StringTransform_Name_ = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name_______StringTransform_Name_ = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.ExpensiveCompute_Value_),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Value___ExpensiveCompute_Value_),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Value___ExpensiveCompute_Value__1),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.StringTransform_Name_),
                6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name_______StringTransform_Name_),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Value" => true,
                "ExpensiveCompute(Value)" => true,
                "ExpensiveCompute_Value_" => true,
                "Value + ExpensiveCompute(Value)" => true,
                "Value___ExpensiveCompute_Value_" => true,
                "Value * ExpensiveCompute(Value)" => true,
                "Value___ExpensiveCompute_Value__1" => true,
                "Name" => true,
                "StringTransform(Name)" => true,
                "StringTransform_Name_" => true,
                "Name + _ + StringTransform(Name)" => true,
                "Name_______StringTransform_Name_" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Value, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.ExpensiveCompute_Value_, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Value___ExpensiveCompute_Value_, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Value___ExpensiveCompute_Value__1, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(5, this.StringTransform_Name_, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(6, this.Name_______StringTransform_Name_, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Value, decimal ExpensiveCompute_Value_, decimal Value___ExpensiveCompute_Value_, decimal Value___ExpensiveCompute_Value__1, string Name, string StringTransform_Name_, string Name_______StringTransform_Name_)
            {
                this.Value = Value;
                this.ExpensiveCompute_Value_ = ExpensiveCompute_Value_;
                this.Value___ExpensiveCompute_Value_ = Value___ExpensiveCompute_Value_;
                this.Value___ExpensiveCompute_Value__1 = Value___ExpensiveCompute_Value__1;
                this.Name = Name;
                this.StringTransform_Name_ = StringTransform_Name_;
                this.Name_______StringTransform_Name_ = Name_______StringTransform_Name_;
            }

            public decimal ExpensiveCompute_Value_ { get; }
            public string Name { get; }
            public string Name_______StringTransform_Name_ { get; }
            public string StringTransform_Name_ { get; }
            public int Value { get; }
            public decimal Value___ExpensiveCompute_Value_ { get; }
            public decimal Value___ExpensiveCompute_Value__1 { get; }
        }
    }
}
