// === Parsed Query ===
/*
select r.Id, r.Id::long * 2 as Value from #computedtest.rows() r
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.Id as Id, (r.Id::Int64 * 2) as Value]
    SchemaScan [#computedtest.rows() as r]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.Id as Id, r.__computed1 as Value]
    PhysicalSchemaScan [#computedtest.rows() as r] [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=36E4473105344AE35BB29821A3FDF64F9D87B312D3AEE1E460D3243B433BC7FF]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    Generated [QueryRow_36E447310534_S]
      Id: int <- field Field0
      __computed1: long? <- field Field1
    Generated [ResultRow0]
      Id: int <- field Id
      Value: long? <- field Value

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: QueryRow_36E447310534_S] -> rRows [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=36E4473105344AE35BB29821A3FDF64F9D87B312D3AEE1E460D3243B433BC7FF]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [r in rRows]
      AppendShape [result <- ResultRow0(Id: r.Id, Value: r.__computed1)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q265_SourceComputedProjectionAccepted
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
            new Column("Value", typeof(long?), 1)
        };
        private static readonly QueryRowShape __queryRowShape_36E447310534 = new QueryRowShape(new QueryRowField[] { new QueryRowField(0, 0, "Id", typeof(int), false), QueryRowField.ForComputedProjection(1, new SourceComputedProjection("__computed1", new SourceScalarBinary(SourceScalarBinaryOperator.Multiply, new SourceScalarCast(new SourceScalarColumn("Id", typeof(int)), typeof(long?)), new SourceScalarLiteral((int)2, typeof(int)), typeof(long?)), typeof(long?))) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0) });
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
            var __rSchema = provider.GetSchema("#computedtest");
            var __rSchemaQueryRows = __rSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#computedtest.rows' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape 36E4473105344AE35BB29821A3FDF64F9D87B312D3AEE1E460D3243B433BC7FF).");
            var rRowsSource = QuerySourceInvocation<QueryRow_36E447310534_S, QueryRowMaterializer_36E447310534_S>.Open(__rSchemaQueryRows, "rows", new QueryScopedRowSourceRequest(new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, __queryRowShape_36E447310534), global::Musoq.Schema.SourceArguments.Empty);
            var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_36E447310534_S>(rRowsSource.Chunks, __musoqProgressContext, "r:1") : rRowsSource.Chunks;
            var __musoqTableSourceRows = rRows;
            this.OnPhaseChanged("compiled", QueryPhase.Select);
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectRowsSerial<QueryRow_36E447310534_S, ResultRow0>(__musoqTableSourceRows, (r) => true, (r) => new ResultRow0(r.Field0, r.Field1), token), token, onCompleted: () =>
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

        private readonly struct QueryRowMaterializer_36E447310534_S : IQueryRowMaterializer<QueryRow_36E447310534_S>
        {
            public static QueryRow_36E447310534_S Materialize<TReader>(scoped ref TReader reader)
                where TReader : IQuerySourceFieldReader, allows ref struct => new QueryRow_36E447310534_S(reader.Read<int>(0), reader.Read<long?>(1));
        }

        private readonly struct QueryRow_36E447310534_S
        {
            public QueryRow_36E447310534_S(int Field0, long? Field1)
            {
                this.Field0 = Field0;
                this.Field1 = Field1;
            }

            public int Field0 { get; }
            public long? Field1 { get; }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, long? __value1)
            {
                Id = __value0;
                Value = __value1;
            }

            public override int Count => 2;
            public int Id { get; private set; }
            public long? Value { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long?>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long?>(this.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Value" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Value, ref visitor);
            }
        }
    }
}
