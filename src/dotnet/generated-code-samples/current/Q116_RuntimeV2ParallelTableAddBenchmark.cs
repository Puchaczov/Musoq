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
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: RuntimeV2RegressionEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateObject [__resultRuntimeV2RegressionLibrary0: RuntimeV2RegressionLibrary]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Name", typeof(string), 1),
            new Column("Value", typeof(int), 2),
            new Column("Category", typeof(string), 3),
            new Column("Heavy", typeof(int), 4)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Name", typeof(string), 1), new Column("Value", typeof(int), 2), new Column("Category", typeof(string), 3) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
            var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>.Open(__ko3ikoSchema, "entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
            var __resultRuntimeV2RegressionLibrary0 = new Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionLibrary();
            var __musoqTableSourceRows = ko3ikoRows;
            this.OnPhaseChanged("compiled", QueryPhase.Where);
            this.OnPhaseChanged("compiled", QueryPhase.Select);
            if (__musoqTableSourceRows is not IReadOnlyList<IReadOnlyList<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>> _)
            {
                return new QueryTableEnumerable<ResultRow0>((_) => EvaluationHelper.ProjectChunkedRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity, ResultRow0>(__musoqTableSourceRows, 24, (ko3iko) => (ko3iko.Value > 100), (ko3iko) => new ResultRow0(ko3iko.Id, ko3iko.Name, ko3iko.Value, ko3iko.Category, (int)__resultRuntimeV2RegressionLibrary0.HeavyComputation(ko3iko.Value)), token), token, onCompleted: () =>
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

            var __musoqTableParallelRows = EvaluationHelper.GetParallelProjectionRowsOrEmpty<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity>(__musoqTableSourceRows, 4096);
            return new QueryTableEnumerable<ResultRow0>((_) => QueryRows.FromRowShards(EvaluationHelper.ProjectRowsParallel<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2RegressionEntity, ResultRow0>(__musoqTableParallelRows, 24, (ko3iko) => (ko3iko.Value > 100), (ko3iko) => new ResultRow0(ko3iko.Id, ko3iko.Name, ko3iko.Value, ko3iko.Category, (int)__resultRuntimeV2RegressionLibrary0.HeavyComputation(ko3iko.Value)), token)), token, onCompleted: () =>
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

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Category = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Heavy = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Category),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Heavy),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Name" => true,
                "Value" => true,
                "Category" => true,
                "Heavy" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Value, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Category, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.Heavy, ref visitor);
            }
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
