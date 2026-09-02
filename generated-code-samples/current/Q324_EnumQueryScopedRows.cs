// === Parsed Query ===
/*
enum JobStatus : short { Queued = 10s, Running = 20s, Finished = 30s };flags enum FileAccess : uint { None = 0ui, Read = 1ui, Write = 2ui, ReadWrite = 3ui };table EnumRows { Id: int, Status: JobStatus, Access: FileAccess };couple #queryrowsample.rows with table EnumRows as Rows;select Status, EnumName(Status) as StatusName, HasAllFlags(Access, 'Read', 'Write') as CanWrite from Rows() where Status in ('Queued', 'Running')
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Status as Status, EnumNameNullable(ko3iko.Status) as StatusName, HasAllFlagsNullable(ko3iko.Access, 3) as CanWrite]
    Filter [((ko3iko.Status = 10) OR (ko3iko.Status = 20))]
      SchemaScan [#queryrowsample.rows() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Status as Status, EnumNameNullable(ko3iko.Status) as StatusName, HasAllFlagsNullable(ko3iko.Access, 3) as CanWrite]
    PhysicalFilter [((ko3iko.Status = 10) OR (ko3iko.Status = 20))]
      PhysicalSchemaScan [#queryrowsample.rows() as ko3iko] [pushdown: ko3iko.Status IN (10, 20)] [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=B9994CBC6B098622748934DC82B5D4412ADA3DF973EC63272596E7EDF94F7B6E]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    Generated [QueryRow_B9994CBC6B09_S]
      Status: short? <- field Field0
      Access: uint? <- field Field1
    Generated [ResultRow0]
      Status: short? <- field Status
      StatusName: string <- field StatusName
      CanWrite: bool <- field CanWrite

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: object] -> ko3ikoRows [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=B9994CBC6B098622748934DC82B5D4412ADA3DF973EC63272596E7EDF94F7B6E]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ParallelFilterProjectLoop [ko3iko in ko3ikoRows where ((ko3iko.Status = 10) OR (ko3iko.Status = 20)); threshold 4096, maxDegree 24]
      ParallelProject
        Let [status: short? = ko3iko.Status]
        If [((status = 10) OR (status = 20))]
          AppendShape [result <- ResultShape0(Status: status, StatusName: EnumNameNullable(status), CanWrite: HasAllFlagsNullable(ko3iko.Access, 3))]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q324_EnumQueryScopedRows
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
            new Column("Status", typeof(short?), 0, typeof(short?), new global::Musoq.Schema.EnumTypeDescriptor("JobStatus", global::Musoq.Schema.EnumTypeOrigin.QueryLocal, global::Musoq.Schema.EnumUnderlyingKind.Int16, false, new global::Musoq.Schema.EnumMemberDescriptor[] { new global::Musoq.Schema.EnumMemberDescriptor("Queued", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.Int16, 10UL)), new global::Musoq.Schema.EnumMemberDescriptor("Running", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.Int16, 20UL)), new global::Musoq.Schema.EnumMemberDescriptor("Finished", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.Int16, 30UL)) })),
            new Column("StatusName", typeof(string), 1),
            new Column("CanWrite", typeof(bool), 2)
        };
        private static readonly QueryRowShape __queryRowShape_B9994CBC6B09 = new QueryRowShape(new QueryRowField[] { new QueryRowField(0, 1, "Status", typeof(short?), typeof(short?), new global::Musoq.Schema.EnumTypeDescriptor("JobStatus", global::Musoq.Schema.EnumTypeOrigin.QueryLocal, global::Musoq.Schema.EnumUnderlyingKind.Int16, false, new global::Musoq.Schema.EnumMemberDescriptor[] { new global::Musoq.Schema.EnumMemberDescriptor("Queued", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.Int16, 10UL)), new global::Musoq.Schema.EnumMemberDescriptor("Running", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.Int16, 20UL)), new global::Musoq.Schema.EnumMemberDescriptor("Finished", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.Int16, 30UL)) }), true, null, global::Musoq.Schema.ColumnStability.Stable), new QueryRowField(1, 2, "Access", typeof(uint?), typeof(uint?), new global::Musoq.Schema.EnumTypeDescriptor("FileAccess", global::Musoq.Schema.EnumTypeOrigin.QueryLocal, global::Musoq.Schema.EnumUnderlyingKind.UInt32, true, new global::Musoq.Schema.EnumMemberDescriptor[] { new global::Musoq.Schema.EnumMemberDescriptor("None", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.UInt32, 0UL)), new global::Musoq.Schema.EnumMemberDescriptor("Read", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.UInt32, 1UL)), new global::Musoq.Schema.EnumMemberDescriptor("Write", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.UInt32, 2UL)), new global::Musoq.Schema.EnumMemberDescriptor("ReadWrite", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.UInt32, 3UL)) }), true, null, global::Musoq.Schema.ColumnStability.Stable) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Status", typeof(short?), 0, typeof(short?), new global::Musoq.Schema.EnumTypeDescriptor("JobStatus", global::Musoq.Schema.EnumTypeOrigin.QueryLocal, global::Musoq.Schema.EnumUnderlyingKind.Int16, false, new global::Musoq.Schema.EnumMemberDescriptor[] { new global::Musoq.Schema.EnumMemberDescriptor("Queued", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.Int16, 10UL)), new global::Musoq.Schema.EnumMemberDescriptor("Running", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.Int16, 20UL)), new global::Musoq.Schema.EnumMemberDescriptor("Finished", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.Int16, 30UL)) })), new Column("Access", typeof(uint?), 1, typeof(uint?), new global::Musoq.Schema.EnumTypeDescriptor("FileAccess", global::Musoq.Schema.EnumTypeOrigin.QueryLocal, global::Musoq.Schema.EnumUnderlyingKind.UInt32, true, new global::Musoq.Schema.EnumMemberDescriptor[] { new global::Musoq.Schema.EnumMemberDescriptor("None", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.UInt32, 0UL)), new global::Musoq.Schema.EnumMemberDescriptor("Read", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.UInt32, 1UL)), new global::Musoq.Schema.EnumMemberDescriptor("Write", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.UInt32, 2UL)), new global::Musoq.Schema.EnumMemberDescriptor("ReadWrite", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.UInt32, 3UL)) })) });
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
            var __ko3ikoSchema = provider.GetSchema("#queryrowsample");
            var __ko3ikoSchemaQueryRows = __ko3ikoSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#queryrowsample.rows' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape B9994CBC6B098622748934DC82B5D4412ADA3DF973EC63272596E7EDF94F7B6E).");
            var ko3ikoRowsSource = __ko3ikoSchemaQueryRows.GetQueryScopedRowSource<QueryRow_B9994CBC6B09_S, QueryRowMaterializer_B9994CBC6B09_S>("rows", new QueryScopedRowSourceRequest(new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), __queryRowShape_B9994CBC6B09), Array.Empty<object>());
            var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_B9994CBC6B09_S>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
            var __musoqTableSourceRows = ko3ikoRows;
            this.OnPhaseChanged("compiled", QueryPhase.Where);
            this.OnPhaseChanged("compiled", QueryPhase.Select);
            if (__musoqTableSourceRows is not IReadOnlyList<IReadOnlyList<QueryRow_B9994CBC6B09_S>> _)
            {
                return new QueryTableEnumerable<ResultRow0>((_) => EvaluationHelper.ProjectChunkedRowsParallel<QueryRow_B9994CBC6B09_S, ResultRow0>(__musoqTableSourceRows, 24, (ko3iko) => ((Operators.SqlCompare<short?, short>(ko3iko.Field0, 10, (short? __sqlLeft, short __sqlRight) => (__sqlLeft == __sqlRight)) | Operators.SqlCompare<short?, short>(ko3iko.Field0, 20, (short? __sqlLeft, short __sqlRight) => (__sqlLeft == __sqlRight)))) == true, (ko3iko) => new ResultRow0(ko3iko.Field0, (ko3iko.Field0 switch
                {
                    10 => "Queued",
                    20 => "Running",
                    30 => "Finished",
                    _ => null
                }), (ko3iko.Field1 switch
                {
                    uint __enumValue0 => ((__enumValue0 & 3u) == 3u),
                    _ => false
                })), token), token, onCompleted: () =>
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

            var __musoqTableParallelRows = EvaluationHelper.GetParallelProjectionRowsOrEmpty<QueryRow_B9994CBC6B09_S>(__musoqTableSourceRows, 4096);
            return new QueryTableEnumerable<ResultRow0>((_) => QueryRows.FromRowShards(EvaluationHelper.ProjectRowsParallel<QueryRow_B9994CBC6B09_S, ResultRow0>(__musoqTableParallelRows, 24, (ko3iko) => ((Operators.SqlCompare<short?, short>(ko3iko.Field0, 10, (short? __sqlLeft, short __sqlRight) => (__sqlLeft == __sqlRight)) | Operators.SqlCompare<short?, short>(ko3iko.Field0, 20, (short? __sqlLeft, short __sqlRight) => (__sqlLeft == __sqlRight)))) == true, (ko3iko) => new ResultRow0(ko3iko.Field0, (ko3iko.Field0 switch
            {
                10 => "Queued",
                20 => "Running",
                30 => "Finished",
                _ => null
            }), (ko3iko.Field1 switch
            {
                uint __enumValue1 => ((__enumValue1 & 3u) == 3u),
                _ => false
            })), token)), token, onCompleted: () =>
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

        private readonly struct QueryRowMaterializer_B9994CBC6B09_S : IQueryRowMaterializer<QueryRow_B9994CBC6B09_S>
        {
            public static QueryRow_B9994CBC6B09_S Materialize<TReader>(scoped ref TReader reader)
                where TReader : IQuerySourceFieldReader, allows ref struct => new QueryRow_B9994CBC6B09_S(reader.Read<short?>(0), reader.Read<uint?>(1));
        }

        private readonly struct QueryRow_B9994CBC6B09_S
        {
            public QueryRow_B9994CBC6B09_S(short? Field0, uint? Field1)
            {
                this.Field0 = Field0;
                this.Field1 = Field1;
            }

            public short? Field0 { get; }
            public uint? Field1 { get; }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(short? __value0, string __value1, bool __value2)
            {
                Status = __value0;
                StatusName = __value1;
                CanWrite = __value2;
            }

            public bool CanWrite { get; private set; }
            public override int Count => 3;
            public short? Status { get; private set; }
            public string StatusName { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Status = (short?)value;
                        break;
                    case 1:
                        StatusName = (string)value;
                        break;
                    case 2:
                        CanWrite = (bool)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Status" => true,
                "StatusName" => true,
                "CanWrite" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Status,
                1 => (object)StatusName,
                2 => (object)CanWrite,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "Status" => (object)Status,
                "StatusName" => (object)StatusName,
                "CanWrite" => (object)CanWrite,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(short? Status, string StatusName, bool CanWrite)
            {
                this.Status = Status;
                this.StatusName = StatusName;
                this.CanWrite = CanWrite;
            }

            public bool CanWrite { get; }
            public short? Status { get; }
            public string StatusName { get; }
        }
    }
}
