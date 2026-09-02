// === Parsed Query ===
/*
select r.G0, r.G1, r.G2, r.G3, r.G4 from #queryrowsample.rows() r
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.G0 as r.G0, r.G1 as r.G1, r.G2 as r.G2, r.G3 as r.G3, r.G4 as r.G4]
    SchemaScan [#queryrowsample.rows() as r]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.G0 as r.G0, r.G1 as r.G1, r.G2 as r.G2, r.G3 as r.G3, r.G4 as r.G4]
    PhysicalSchemaScan [#queryrowsample.rows() as r] [query-row:SealedClass;lifetime=ScanLocal;shape=6E1D0F850A2AD7C597621B1B88BC470C0060AD247292C3330E7444134969BE64]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    Generated [QueryRow_6E1D0F850A2A_C]
      G0: Guid <- field Field0
      G1: Guid <- field Field1
      G2: Guid <- field Field2
      G3: Guid <- field Field3
      G4: Guid <- field Field4
    Generated [ResultRow0]
      r.G0: Guid <- field r_G0
      r.G1: Guid <- field r_G1
      r.G2: Guid <- field r_G2
      r.G3: Guid <- field r_G3
      r.G4: Guid <- field r_G4

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: object] -> rRows [query-row:SealedClass;lifetime=ScanLocal;shape=6E1D0F850A2AD7C597621B1B88BC470C0060AD247292C3330E7444134969BE64]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [r in rRows]
      AppendShape [result <- ResultShape0(r.G0: r.G0, r.G1: r.G1, r.G2: r.G2, r.G3: r.G3, r.G4: r.G4)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q238_QueryRowSealedClass
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
            new Column("r.G0", typeof(Guid), 0),
            new Column("r.G1", typeof(Guid), 1),
            new Column("r.G2", typeof(Guid), 2),
            new Column("r.G3", typeof(Guid), 3),
            new Column("r.G4", typeof(Guid), 4)
        };
        private static readonly QueryRowShape __queryRowShape_6E1D0F850A2A = new QueryRowShape(new QueryRowField[] { new QueryRowField(0, 0, "G0", typeof(Guid), false), new QueryRowField(1, 1, "G1", typeof(Guid), false), new QueryRowField(2, 2, "G2", typeof(Guid), false), new QueryRowField(3, 3, "G3", typeof(Guid), false), new QueryRowField(4, 4, "G4", typeof(Guid), false) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("G0", typeof(Guid), 0), new Column("G1", typeof(Guid), 1), new Column("G2", typeof(Guid), 2), new Column("G3", typeof(Guid), 3), new Column("G4", typeof(Guid), 4) });
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
            var __rSchemaQueryRows = __rSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#queryrowsample.rows' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape 6E1D0F850A2AD7C597621B1B88BC470C0060AD247292C3330E7444134969BE64).");
            var rRowsSource = __rSchemaQueryRows.GetQueryScopedRowSource<QueryRow_6E1D0F850A2A_C, QueryRowMaterializer_6E1D0F850A2A_C>("rows", new QueryScopedRowSourceRequest(new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), __queryRowShape_6E1D0F850A2A), Array.Empty<object>());
            var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_6E1D0F850A2A_C>(rRowsSource.Chunks, __musoqProgressContext, "r:1") : rRowsSource.Chunks;
            var __musoqTableSourceRows = rRows;
            this.OnPhaseChanged("compiled", QueryPhase.Select);
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectRowsSerial<QueryRow_6E1D0F850A2A_C, ResultRow0>(__musoqTableSourceRows, (r) => true, (r) => new ResultRow0(r.Field0, r.Field1, r.Field2, r.Field3, r.Field4), token), token, onCompleted: () =>
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

        private readonly struct QueryRowMaterializer_6E1D0F850A2A_C : IQueryRowMaterializer<QueryRow_6E1D0F850A2A_C>
        {
            public static QueryRow_6E1D0F850A2A_C Materialize<TReader>(scoped ref TReader reader)
                where TReader : IQuerySourceFieldReader, allows ref struct => new QueryRow_6E1D0F850A2A_C(reader.Read<Guid>(0), reader.Read<Guid>(1), reader.Read<Guid>(2), reader.Read<Guid>(3), reader.Read<Guid>(4));
        }

        private sealed class QueryRow_6E1D0F850A2A_C
        {
            public QueryRow_6E1D0F850A2A_C(Guid __value0, Guid __value1, Guid __value2, Guid __value3, Guid __value4)
            {
                Field0 = __value0;
                Field1 = __value1;
                Field2 = __value2;
                Field3 = __value3;
                Field4 = __value4;
            }

            public Guid Field0 { get; }
            public Guid Field1 { get; }
            public Guid Field2 { get; }
            public Guid Field3 { get; }
            public Guid Field4 { get; }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(Guid __value0, Guid __value1, Guid __value2, Guid __value3, Guid __value4)
            {
                r_G0 = __value0;
                r_G1 = __value1;
                r_G2 = __value2;
                r_G3 = __value3;
                r_G4 = __value4;
            }

            public override int Count => 5;
            public Guid r_G0 { get; private set; }
            public Guid r_G1 { get; private set; }
            public Guid r_G2 { get; private set; }
            public Guid r_G3 { get; private set; }
            public Guid r_G4 { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        r_G0 = (Guid)value;
                        break;
                    case 1:
                        r_G1 = (Guid)value;
                        break;
                    case 2:
                        r_G2 = (Guid)value;
                        break;
                    case 3:
                        r_G3 = (Guid)value;
                        break;
                    case 4:
                        r_G4 = (Guid)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "r.G0" => true,
                "r_G0" => true,
                "G0" => true,
                "r.G1" => true,
                "r_G1" => true,
                "G1" => true,
                "r.G2" => true,
                "r_G2" => true,
                "G2" => true,
                "r.G3" => true,
                "r_G3" => true,
                "G3" => true,
                "r.G4" => true,
                "r_G4" => true,
                "G4" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)r_G0,
                1 => (object)r_G1,
                2 => (object)r_G2,
                3 => (object)r_G3,
                4 => (object)r_G4,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "r.G0" => (object)r_G0,
                "r_G0" => (object)r_G0,
                "G0" => (object)r_G0,
                "r.G1" => (object)r_G1,
                "r_G1" => (object)r_G1,
                "G1" => (object)r_G1,
                "r.G2" => (object)r_G2,
                "r_G2" => (object)r_G2,
                "G2" => (object)r_G2,
                "r.G3" => (object)r_G3,
                "r_G3" => (object)r_G3,
                "G3" => (object)r_G3,
                "r.G4" => (object)r_G4,
                "r_G4" => (object)r_G4,
                "G4" => (object)r_G4,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(Guid r_G0, Guid r_G1, Guid r_G2, Guid r_G3, Guid r_G4)
            {
                this.r_G0 = r_G0;
                this.r_G1 = r_G1;
                this.r_G2 = r_G2;
                this.r_G3 = r_G3;
                this.r_G4 = r_G4;
            }

            public Guid r_G0 { get; }
            public Guid r_G1 { get; }
            public Guid r_G2 { get; }
            public Guid r_G3 { get; }
            public Guid r_G4 { get; }
        }
    }
}
