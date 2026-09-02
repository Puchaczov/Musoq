// === Parsed Query ===
/*
select r.Id, r.Name from #queryrowsample.rows() r
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.Id as r.Id, r.Name as r.Name]
    SchemaScan [#queryrowsample.rows() as r]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.Id as r.Id, r.Name as r.Name]
    PhysicalSchemaScan [#queryrowsample.rows() as r] [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=A8CCE6ACE921C950DDFDEA91A09C06EB8E3D1C623485BF45A1E68D0BF21808A4]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    Generated [QueryRow_A8CCE6ACE921_S]
      Id: int <- field Field0
      Name: string <- field Field1
    Generated [ResultRow0]
      r.Id: int <- field r_Id
      r.Name: string <- field r_Name

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: object] -> rRows [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=A8CCE6ACE921C950DDFDEA91A09C06EB8E3D1C623485BF45A1E68D0BF21808A4]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [r in rRows]
      AppendShape [result <- ResultShape0(r.Id: r.Id, r.Name: r.Name)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q237_QueryRowReadonlyStruct
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
            new Column("r.Id", typeof(int), 0),
            new Column("r.Name", typeof(string), 1)
        };
        private static readonly QueryRowShape __queryRowShape_A8CCE6ACE921 = new QueryRowShape(new QueryRowField[] { new QueryRowField(0, 0, "Id", typeof(int), false), new QueryRowField(1, 1, "Name", typeof(string), true) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Name", typeof(string), 1) });
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
            var __rSchemaQueryRows = __rSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#queryrowsample.rows' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape A8CCE6ACE921C950DDFDEA91A09C06EB8E3D1C623485BF45A1E68D0BF21808A4).");
            var rRowsSource = __rSchemaQueryRows.GetQueryScopedRowSource<QueryRow_A8CCE6ACE921_S, QueryRowMaterializer_A8CCE6ACE921_S>("rows", new QueryScopedRowSourceRequest(new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), __queryRowShape_A8CCE6ACE921), Array.Empty<object>());
            var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_A8CCE6ACE921_S>(rRowsSource.Chunks, __musoqProgressContext, "r:1") : rRowsSource.Chunks;
            var __musoqTableSourceRows = rRows;
            this.OnPhaseChanged("compiled", QueryPhase.Select);
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectRowsSerial<QueryRow_A8CCE6ACE921_S, ResultRow0>(__musoqTableSourceRows, (r) => true, (r) => new ResultRow0(r.Field0, r.Field1), token), token, onCompleted: () =>
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

        private readonly struct QueryRowMaterializer_A8CCE6ACE921_S : IQueryRowMaterializer<QueryRow_A8CCE6ACE921_S>
        {
            public static QueryRow_A8CCE6ACE921_S Materialize<TReader>(scoped ref TReader reader)
                where TReader : IQuerySourceFieldReader, allows ref struct => new QueryRow_A8CCE6ACE921_S(reader.Read<int>(0), reader.Read<string>(1));
        }

        private readonly struct QueryRow_A8CCE6ACE921_S
        {
            public QueryRow_A8CCE6ACE921_S(int Field0, string Field1)
            {
                this.Field0 = Field0;
                this.Field1 = Field1;
            }

            public int Field0 { get; }
            public string Field1 { get; }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, string __value1)
            {
                r_Id = __value0;
                r_Name = __value1;
            }

            public override int Count => 2;
            public int r_Id { get; private set; }
            public string r_Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        r_Id = (int)value;
                        break;
                    case 1:
                        r_Name = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "r.Id" => true,
                "r_Id" => true,
                "Id" => true,
                "r.Name" => true,
                "r_Name" => true,
                "Name" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)r_Id,
                1 => (object)r_Name,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "r.Id" => (object)r_Id,
                "r_Id" => (object)r_Id,
                "Id" => (object)r_Id,
                "r.Name" => (object)r_Name,
                "r_Name" => (object)r_Name,
                "Name" => (object)r_Name,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int r_Id, string r_Name)
            {
                this.r_Id = r_Id;
                this.r_Name = r_Name;
            }

            public int r_Id { get; }
            public string r_Name { get; }
        }
    }
}
