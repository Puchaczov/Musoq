// === Parsed Query ===
/*
enum JobStatus : short { Queued = 10s, Running = 20s, Finished = 30s };flags enum FileAccess : uint { None = 0ui, Read = 1ui, Write = 2ui, ReadWrite = 3ui };table EnumRows { Id: int, Status: JobStatus, Access: FileAccess };couple #queryrowsample.rows with table EnumRows as Rows;select Status, EnumName(Status) as StatusName, HasAllFlags(Access, 'Read', 'Write') as CanWrite from Rows() where Status in ('Queued', 'Running')
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Status as Status, EnumNameNullable(ko3iko.Status) as StatusName, HasAllFlagsNullable(ko3iko.Access, 3) as CanWrite]
    Filter [ko3iko.Status IN (10, 20)]
      SchemaScan [#queryrowsample.rows() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Status as Status, EnumNameNullable(ko3iko.Status) as StatusName, HasAllFlagsNullable(ko3iko.Access, 3) as CanWrite]
    PhysicalFilter [ko3iko.Status IN (10, 20)]
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
    SourceScan [ko3iko: QueryRow_B9994CBC6B09_S] -> ko3ikoRows [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=B9994CBC6B098622748934DC82B5D4412ADA3DF973EC63272596E7EDF94F7B6E]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [status: short? = ko3iko.Status]
      If [status IN (10, 20)]
        AppendShape [result <- ResultRow0(Status: status, StatusName: EnumNameNullable(status), CanWrite: HasAllFlagsNullable(ko3iko.Access, 3))]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Status", typeof(short?), 0, typeof(short?), new global::Musoq.Schema.EnumTypeDescriptor("JobStatus", global::Musoq.Schema.EnumTypeOrigin.QueryLocal, global::Musoq.Schema.EnumUnderlyingKind.Int16, false, new global::Musoq.Schema.EnumMemberDescriptor[] { new global::Musoq.Schema.EnumMemberDescriptor("Queued", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.Int16, 10UL)), new global::Musoq.Schema.EnumMemberDescriptor("Running", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.Int16, 20UL)), new global::Musoq.Schema.EnumMemberDescriptor("Finished", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.Int16, 30UL)) })),
            new Column("StatusName", typeof(string), 1),
            new Column("CanWrite", typeof(bool), 2)
        };
        private static readonly short? [] __inSet_compiled_0 = new short? []
        {
            10,
            20
        };
        private static readonly QueryRowShape __queryRowShape_B9994CBC6B09 = new QueryRowShape(new QueryRowField[] { new QueryRowField(0, 1, "Status", typeof(short?), typeof(short?), new global::Musoq.Schema.EnumTypeDescriptor("JobStatus", global::Musoq.Schema.EnumTypeOrigin.QueryLocal, global::Musoq.Schema.EnumUnderlyingKind.Int16, false, new global::Musoq.Schema.EnumMemberDescriptor[] { new global::Musoq.Schema.EnumMemberDescriptor("Queued", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.Int16, 10UL)), new global::Musoq.Schema.EnumMemberDescriptor("Running", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.Int16, 20UL)), new global::Musoq.Schema.EnumMemberDescriptor("Finished", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.Int16, 30UL)) }), true, null, global::Musoq.Schema.ColumnStability.Stable), new QueryRowField(1, 2, "Access", typeof(uint?), typeof(uint?), new global::Musoq.Schema.EnumTypeDescriptor("FileAccess", global::Musoq.Schema.EnumTypeOrigin.QueryLocal, global::Musoq.Schema.EnumUnderlyingKind.UInt32, true, new global::Musoq.Schema.EnumMemberDescriptor[] { new global::Musoq.Schema.EnumMemberDescriptor("None", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.UInt32, 0UL)), new global::Musoq.Schema.EnumMemberDescriptor("Read", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.UInt32, 1UL)), new global::Musoq.Schema.EnumMemberDescriptor("Write", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.UInt32, 2UL)), new global::Musoq.Schema.EnumMemberDescriptor("ReadWrite", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.UInt32, 3UL)) }), true, null, global::Musoq.Schema.ColumnStability.Stable) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Status", typeof(short?), 0, typeof(short?), new global::Musoq.Schema.EnumTypeDescriptor("JobStatus", global::Musoq.Schema.EnumTypeOrigin.QueryLocal, global::Musoq.Schema.EnumUnderlyingKind.Int16, false, new global::Musoq.Schema.EnumMemberDescriptor[] { new global::Musoq.Schema.EnumMemberDescriptor("Queued", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.Int16, 10UL)), new global::Musoq.Schema.EnumMemberDescriptor("Running", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.Int16, 20UL)), new global::Musoq.Schema.EnumMemberDescriptor("Finished", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.Int16, 30UL)) })), new Column("Access", typeof(uint?), 1, typeof(uint?), new global::Musoq.Schema.EnumTypeDescriptor("FileAccess", global::Musoq.Schema.EnumTypeOrigin.QueryLocal, global::Musoq.Schema.EnumUnderlyingKind.UInt32, true, new global::Musoq.Schema.EnumMemberDescriptor[] { new global::Musoq.Schema.EnumMemberDescriptor("None", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.UInt32, 0UL)), new global::Musoq.Schema.EnumMemberDescriptor("Read", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.UInt32, 1UL)), new global::Musoq.Schema.EnumMemberDescriptor("Write", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.UInt32, 2UL)), new global::Musoq.Schema.EnumMemberDescriptor("ReadWrite", global::Musoq.Schema.EnumScalarValue.FromRaw(global::Musoq.Schema.EnumUnderlyingKind.UInt32, 3UL)) })) });
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
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return __musoqShapeRow;
            }
        }

        private IEnumerable<ResultRow0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#queryrowsample");
                var __ko3ikoSchemaQueryRows = __ko3ikoSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#queryrowsample.rows' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape B9994CBC6B098622748934DC82B5D4412ADA3DF973EC63272596E7EDF94F7B6E).");
                var ko3ikoRowsSource = QuerySourceInvocation<QueryRow_B9994CBC6B09_S, QueryRowMaterializer_B9994CBC6B09_S>.Open(__ko3ikoSchemaQueryRows, "rows", new QueryScopedRowSourceRequest(new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), __queryRowShape_B9994CBC6B09), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_B9994CBC6B09_S>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Where);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<QueryRow_B9994CBC6B09_S> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is QueryRow_B9994CBC6B09_S[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                short? status = ko3iko.Field0;
                                if (Musoq.Evaluator.Operators.SqlIn(status, membershipValue => (Array.IndexOf(__inSet_compiled_0, membershipValue) >= 0), false) == true)
                                {
                                    yield return new ResultRow0(status, (status switch
                                    {
                                        10 => "Queued",
                                        20 => "Running",
                                        30 => "Finished",
                                        _ => null
                                    }), (ko3iko.Field1 switch
                                    {
                                        uint __enumValue0 => ((__enumValue0 & 3u) == 3u),
                                        _ => false
                                    }));
                                }
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<QueryRow_B9994CBC6B09_S> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                short? status = ko3iko.Field0;
                                if (Musoq.Evaluator.Operators.SqlIn(status, membershipValue => (Array.IndexOf(__inSet_compiled_0, membershipValue) >= 0), false) == true)
                                {
                                    yield return new ResultRow0(status, (status switch
                                    {
                                        10 => "Queued",
                                        20 => "Running",
                                        30 => "Finished",
                                        _ => null
                                    }), (ko3iko.Field1 switch
                                    {
                                        uint __enumValue1 => ((__enumValue1 & 3u) == 3u),
                                        _ => false
                                    }));
                                }
                            }

                            continue;
                        }
                    }

                    for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunk.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                    {
                        if ((ko3ikoIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var ko3iko = ko3ikoChunk[ko3ikoIndex];
                        short? status = ko3iko.Field0;
                        if (Musoq.Evaluator.Operators.SqlIn(status, membershipValue => (Array.IndexOf(__inSet_compiled_0, membershipValue) >= 0), false) == true)
                        {
                            yield return new ResultRow0(status, (status switch
                            {
                                10 => "Queued",
                                20 => "Running",
                                30 => "Finished",
                                _ => null
                            }), (ko3iko.Field1 switch
                            {
                                uint __enumValue2 => ((__enumValue2 & 3u) == 3u),
                                _ => false
                            }));
                        }
                    }
                }
            }
            finally
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }
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

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short?>(this.Status = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<short?, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.StatusName = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.CanWrite = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<bool, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short?>(this.Status),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.StatusName),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.CanWrite),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Status" => true,
                "StatusName" => true,
                "CanWrite" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Status, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.StatusName, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.CanWrite, ref visitor);
            }
        }
    }
}
