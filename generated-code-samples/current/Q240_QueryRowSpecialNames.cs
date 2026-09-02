// === Parsed Query ===
/*
select r.[display name], r.[na-me], r.[MiastoŁódź], r.[select] from #queryrowsample.rows() r
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.display name as r.display name, r.na-me as r.na-me, r.MiastoŁódź as r.MiastoŁódź, r.select as r.select]
    SchemaScan [#queryrowsample.rows() as r]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.display name as r.display name, r.na-me as r.na-me, r.MiastoŁódź as r.MiastoŁódź, r.select as r.select]
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
      r.display name: string <- field r_display_name
      r.na-me: int <- field r_na_me
      r.MiastoŁódź: string <- field r_MiastoŁódź
      r.select: string <- field r_select

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: object] -> rRows [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=5114E4DFC94BD4C37BD01BF54405165C943EACCCB818459F71B705D773929B0B]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [r in rRows]
      AppendShape [result <- ResultShape0(r.display name: r.display name, r.na-me: r.na-me, r.MiastoŁódź: r.MiastoŁódź, r.select: r.select)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("r.display name", typeof(string), 0),
            new Column("r.na-me", typeof(int), 1),
            new Column("r.MiastoŁódź", typeof(string), 2),
            new Column("r.select", typeof(string), 3)
        };
        private static readonly QueryRowShape __queryRowShape_5114E4DFC94B = new QueryRowShape(new QueryRowField[] { new QueryRowField(0, 0, "display name", typeof(string), true), new QueryRowField(1, 1, "na-me", typeof(int), false), new QueryRowField(2, 2, "MiastoŁódź", typeof(string), true), new QueryRowField(3, 3, "select", typeof(string), true) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("display name", typeof(string), 0), new Column("na-me", typeof(int), 1), new Column("MiastoŁódź", typeof(string), 2), new Column("select", typeof(string), 3) });
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
            var __musoqExecutionState = ExecutionState.Capture(Parameters);
            ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
            this.OnPhaseChanged("compiled", QueryPhase.Begin);
            this.OnPhaseChanged("compiled", QueryPhase.From);
            var __rSchema = provider.GetSchema("#queryrowsample");
            var __rSchemaQueryRows = __rSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#queryrowsample.rows' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape 5114E4DFC94BD4C37BD01BF54405165C943EACCCB818459F71B705D773929B0B).");
            var rRowsSource = __rSchemaQueryRows.GetQueryScopedRowSource<QueryRow_5114E4DFC94B_S, QueryRowMaterializer_5114E4DFC94B_S>("rows", new QueryScopedRowSourceRequest(new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), __queryRowShape_5114E4DFC94B), Array.Empty<object>());
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
                r_display_name = __value0;
                r_na_me = __value1;
                r_MiastoŁódź = __value2;
                r_select = __value3;
            }

            public override int Count => 4;
            public string r_MiastoŁódź { get; private set; }
            public string r_display_name { get; private set; }
            public int r_na_me { get; private set; }
            public string r_select { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        r_display_name = (string)value;
                        break;
                    case 1:
                        r_na_me = (int)value;
                        break;
                    case 2:
                        r_MiastoŁódź = (string)value;
                        break;
                    case 3:
                        r_select = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "r.display name" => true,
                "r_display_name" => true,
                "display name" => true,
                "r.na-me" => true,
                "r_na_me" => true,
                "na-me" => true,
                "r.MiastoŁódź" => true,
                "r_MiastoŁódź" => true,
                "MiastoŁódź" => true,
                "r.select" => true,
                "r_select" => true,
                "select" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)r_display_name,
                1 => (object)r_na_me,
                2 => (object)r_MiastoŁódź,
                3 => (object)r_select,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "r.display name" => (object)r_display_name,
                "r_display_name" => (object)r_display_name,
                "display name" => (object)r_display_name,
                "r.na-me" => (object)r_na_me,
                "r_na_me" => (object)r_na_me,
                "na-me" => (object)r_na_me,
                "r.MiastoŁódź" => (object)r_MiastoŁódź,
                "r_MiastoŁódź" => (object)r_MiastoŁódź,
                "MiastoŁódź" => (object)r_MiastoŁódź,
                "r.select" => (object)r_select,
                "r_select" => (object)r_select,
                "select" => (object)r_select,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string r_display_name, int r_na_me, string r_MiastoŁódź, string r_select)
            {
                this.r_display_name = r_display_name;
                this.r_na_me = r_na_me;
                this.r_MiastoŁódź = r_MiastoŁódź;
                this.r_select = r_select;
            }

            public string r_MiastoŁódź { get; }
            public string r_display_name { get; }
            public int r_na_me { get; }
            public string r_select { get; }
        }
    }
}
