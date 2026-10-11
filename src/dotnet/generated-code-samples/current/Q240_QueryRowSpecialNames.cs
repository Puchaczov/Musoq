// === Parsed Query ===
/*
select r.[display name], r.[na-me], r.[MiastoŁódź], r.[select] from #queryrowsample.rows() r
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.display name as display name, r.na-me as na-me, r.MiastoŁódź as MiastoŁódź, r.select as select]
    SchemaScan [#queryrowsample.rows() as r]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.display name as display name, r.na-me as na-me, r.MiastoŁódź as MiastoŁódź, r.select as select]
    PhysicalSchemaScan [#queryrowsample.rows() as r] [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=5114E4DFC94BD4C37BD01BF54405165C943EACCCB818459F71B705D773929B0B]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    Generated [QueryRow_5114E4DFC94B_S]
      display name: string <- field Field0
      na-me: int <- field Field1
      MiastoŁódź: string <- field Field2
      select: string <- field Field3
    Generated [ResultRow0]
      display name: string <- field display_name
      na-me: int <- field na_me
      MiastoŁódź: string <- field MiastoŁódź
      select: string <- field select

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: QueryRow_5114E4DFC94B_S] -> rRows [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=5114E4DFC94BD4C37BD01BF54405165C943EACCCB818459F71B705D773929B0B]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [r in rRows]
      AppendShape [result <- ResultRow0(display name: r.display name, na-me: r.na-me, MiastoŁódź: r.MiastoŁódź, select: r.select)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q240_QueryRowSpecialNames
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
            new Column("display name", typeof(string), 0),
            new Column("na-me", typeof(int), 1),
            new Column("MiastoŁódź", typeof(string), 2),
            new Column("select", typeof(string), 3)
        };
        private static readonly QueryRowShape __queryRowShape_5114E4DFC94B = new QueryRowShape(new QueryRowField[] { new QueryRowField(0, 0, "display name", typeof(string), true), new QueryRowField(1, 1, "na-me", typeof(int), false), new QueryRowField(2, 2, "MiastoŁódź", typeof(string), true), new QueryRowField(3, 3, "select", typeof(string), true) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("display name", typeof(string), 0), new Column("na-me", typeof(int), 1), new Column("MiastoŁódź", typeof(string), 2), new Column("select", typeof(string), 3) });
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
            var __rSchema = provider.GetSchema("#queryrowsample");
            var __rSchemaQueryRows = __rSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#queryrowsample.rows' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape 5114E4DFC94BD4C37BD01BF54405165C943EACCCB818459F71B705D773929B0B).");
            var rRowsSource = QuerySourceInvocation<QueryRow_5114E4DFC94B_S, QueryRowMaterializer_5114E4DFC94B_S>.Open(__rSchemaQueryRows, "rows", new QueryScopedRowSourceRequest(new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), __queryRowShape_5114E4DFC94B), global::Musoq.Schema.SourceArguments.Empty);
            var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_5114E4DFC94B_S>(rRowsSource.Chunks, __musoqProgressContext, "r:1") : rRowsSource.Chunks;
            var __musoqTableSourceRows = rRows;
            this.OnPhaseChanged("compiled", QueryPhase.Select);
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectRowsSerial<QueryRow_5114E4DFC94B_S, ResultRow0>(__musoqTableSourceRows, (r) => true, (r) => new ResultRow0(r.Field0, r.Field1, r.Field2, r.Field3), token), token, onCompleted: () =>
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

        private readonly struct QueryRowMaterializer_5114E4DFC94B_S : IQueryRowMaterializer<QueryRow_5114E4DFC94B_S>
        {
            public static QueryRow_5114E4DFC94B_S Materialize<TReader>(scoped ref TReader reader)
                where TReader : IQuerySourceFieldReader, allows ref struct => new QueryRow_5114E4DFC94B_S(reader.Read<string>(0), reader.Read<int>(1), reader.Read<string>(2), reader.Read<string>(3));
        }

        private readonly struct QueryRow_5114E4DFC94B_S
        {
            public QueryRow_5114E4DFC94B_S(string Field0, int Field1, string Field2, string Field3)
            {
                this.Field0 = Field0;
                this.Field1 = Field1;
                this.Field2 = Field2;
                this.Field3 = Field3;
            }

            public string Field0 { get; }
            public int Field1 { get; }
            public string Field2 { get; }
            public string Field3 { get; }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, int __value1, string __value2, string __value3)
            {
                display_name = __value0;
                na_me = __value1;
                MiastoŁódź = __value2;
                select = __value3;
            }

            public override int Count => 4;
            public string MiastoŁódź { get; private set; }
            public string display_name { get; private set; }
            public int na_me { get; private set; }
            public string select { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.display_name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.na_me = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.MiastoŁódź = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.select = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.display_name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.na_me),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.MiastoŁódź),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.select),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "display name" => true,
                "display_name" => true,
                "na-me" => true,
                "na_me" => true,
                "MiastoŁódź" => true,
                "select" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.display_name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.na_me, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.MiastoŁódź, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.select, ref visitor);
            }
        }
    }
}
