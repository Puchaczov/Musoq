// === Parsed Query ===
/*
binary LargePacket { Value: int le, Tail: byte[4092] }; select r.Id as Id, p.Value as Value, Length(p.Tail) as TailLength from perf.rows() r cross apply Interpret<LargePacket>(r.Payload) p
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.Id as r.Id, r.Payload as r.Payload, p.Value as p.Value, p.Tail as p.Tail]
    Apply [Cross]
      SchemaScan [#perf.rows() as r]
      InterpretSource [#LargePacket(r.Payload) as p]
  Project [r.Id as Id, p.Value as Value, Length(p.Tail) as TailLength]
    CteRef [rp as rp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.Id as r.Id, p.Value as p.Value, p.Tail as p.Tail]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#perf.rows() as r]
      PhysicalInterpretSource [#LargePacket(r.Payload) as p]
  PhysicalProject [r.Id as Id, p.Value as Value, Length(p.Tail) as TailLength]
    PhysicalCteRef [rp as rp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: PerformanceRow]
      Id: int <- property Id
      Payload: byte[] <- property Payload
    SourceEntity [p: object]
      Value: int <- property Value
      Tail: byte[] <- property Tail
    Generated [ResultRow0]
      Id: int <- field Id
      Value: int <- field Value
      TailLength: int? <- field TailLength

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [r: PerformanceRow] -> rRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateObject [__resultLibraryBase0: LibraryBase]
    ChunkedForEach [r in rRows]
      Let [rId: int = r.Id]
      InterpretSource [LargePacket.Interpret(r.Payload) -> pRows]
      ScalarForEach [p in pRows]
        AppendShape [result <- ResultRow0(Id: rId, Value: p.Value, TailLength: Length(p.Tail))]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B27_Binary4KiB_BenchmarkTable
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IContextTableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Payload", typeof(byte[]), 1) });
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Value", typeof(int), 1),
            new Column("TailLength", typeof(int?), 2)
        };
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public ILogger Logger { get; set; }
        public TypedParameterSnapshot TypedParameters { get; set; } = TypedParameterSnapshot.Empty;
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();

        public event QueryPhaseEventHandler PhaseChanged;
        public event DataSourceEventHandler DataSourceProgress;
        public event QueryProgressEventHandler QueryProgress;
        public Table Run(CancellationToken token)
        {
            var queryContext = new QueryRunContext(token, TypedParameters, PhaseChanged, DataSourceProgress, this, queryId: "compiled", provider: Provider, sourceRuntimeSettingsBySourceContextId: SourceRuntimeSettingsBySourceContextId, sourceRuntimeSettingDescriptionsBySourceContextId: SourceRuntimeSettingDescriptionsBySourceContextId, sourceExecutionPlans: SourceExecutionPlans, logger: Logger, queryProgress: QueryProgress);
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        private IEnumerable<ResultRow0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            var token = queryContext.CancellationToken;
            var __musoqProgressContext = queryContext.QueryProgress == null ? null : queryContext;
            Action<string, QueryPhase> OnPhaseChanged = queryContext.NotifyPhaseChanged;
            DataSourceEventHandler OnDataSourceProgress = queryContext.NotifyDataSourceProgress;
            QueryProgressEventHandler OnQueryProgress = queryContext.QueryProgress;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(queryContext.TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                Musoq.Plugins.LibraryBase __resultLibraryBase0 = default!;
                try
                {
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    var __rSchema = provider.GetSchema("#perf");
                    var rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.PerformanceRow>.Open(__rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                    var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.PerformanceRow>(rRowsSource.Chunks, __musoqProgressContext, "r:1") : rRowsSource.Chunks;
                    __resultLibraryBase0 = new Musoq.Plugins.LibraryBase();
                    foreach (var rChunk in rRows)
                    {
                        if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.PerformanceRow> rChunkView)
                        {
                            if (rChunkView.Source is Musoq.Benchmarks.Scenarios.PerformanceRow[] rChunkViewArray)
                            {
                                int rChunkViewOffset = rChunkView.Offset;
                                for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                                {
                                    if ((rIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var r = rChunkViewArray[rChunkViewOffset + rIndex];
                                    int rId = r.Id;
                                    var _interpreter_LargePacket = new Musoq.Generated.Interpreters.LargePacket();
                                    var pRows = _interpreter_LargePacket.Interpret(r.Payload);
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        yield return new ResultRow0(rId, p.Value, (int?)__resultLibraryBase0.Length<byte>(p.Tail));
                                    }
                                }

                                continue;
                            }

                            if (rChunkView.Source is List<Musoq.Benchmarks.Scenarios.PerformanceRow> rChunkViewList)
                            {
                                int rChunkViewOffset = rChunkView.Offset;
                                for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                                {
                                    if ((rIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var r = rChunkViewList[rChunkViewOffset + rIndex];
                                    int rId = r.Id;
                                    var _interpreter_LargePacket = new Musoq.Generated.Interpreters.LargePacket();
                                    var pRows = _interpreter_LargePacket.Interpret(r.Payload);
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        yield return new ResultRow0(rId, p.Value, (int?)__resultLibraryBase0.Length<byte>(p.Tail));
                                    }
                                }

                                continue;
                            }
                        }

                        for (int rIndex = 0, rIndexCount = rChunk.Count; rIndex < rIndexCount; ++rIndex)
                        {
                            if ((rIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var r = rChunk[rIndex];
                            int rId = r.Id;
                            var _interpreter_LargePacket = new Musoq.Generated.Interpreters.LargePacket();
                            var pRows = _interpreter_LargePacket.Interpret(r.Payload);
                            if (pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = pRows;
                                yield return new ResultRow0(rId, p.Value, (int?)__resultLibraryBase0.Length<byte>(p.Tail));
                            }
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
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

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, queryContext))
            {
                yield return __musoqShapeRow;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnPhaseChanged(string queryId, QueryPhase phase)
        {
            PhaseChanged?.Invoke(this, new QueryPhaseEventArgs(queryId, phase));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnDataSourceProgress(object sender, DataSourceEventArgs e)
        {
            DataSourceProgress?.Invoke(this, e);
        }

        private sealed class ResultRow0 : Row
        {
            public int Id { get; private set; }
            public int Value { get; private set; }
            public int? TailLength { get; private set; }

            public ResultRow0(int __value0, int __value1, int? __value2)
            {
                Id = __value0;
                Value = __value1;
                TailLength = __value2;
            }

            public override int Count => 3;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Value, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.TailLength, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.TailLength),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Value" => true,
                "TailLength" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.TailLength = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }
        }
    }
}

// === SyntaxTree:  ===
// <auto-generated>
// This code was generated by Musoq Interpretation Schema code generator.
// Do not modify this file directly.
// </auto-generated>
#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Dynamic;
using Musoq.Schema.Interpreters;

namespace Musoq.Generated.Interpreters
{
    /// <summary>
    /// Generated interpreter for binary schema 'LargePacket'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class LargePacket : BytesInterpreterBase<LargePacket>, IBytesInterpreterStatus<LargePacket>, IParserCompletionLayoutSource, global::Musoq.Plugins.IJsonPayload
    {
        private static readonly global::Musoq.Schema.Interpreters.ParserCompletionLayout __musoqCompletionLayout = new(new string[] { "Value", "Tail" });
        global::Musoq.Schema.Interpreters.ParserCompletionLayout global::Musoq.Schema.Interpreters.IParserCompletionLayoutSource.CompletionLayout => __musoqCompletionLayout;

        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(LargePacket value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Value"))
            {
                writer.WritePropertyName("Value");
                var fieldValue = this.Value;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Tail"))
            {
                writer.WritePropertyName("Tail");
                var fieldValue = this.Tail;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Value field value.</summary>
        public int Value { get; set; }
        /// <summary>Gets the Tail field value.</summary>
        public byte[] Tail { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "LargePacket";

        public override LargePacket InterpretAt(ReadOnlySpan<byte> data, int offset) => ((global::Musoq.Schema.Interpreters.IBytesInterpreterStatus<LargePacket>)this).ReadAt(data, offset).GetValueOrThrow();
        global::Musoq.Schema.Interpreters.ParseAttempt<LargePacket> global::Musoq.Schema.Interpreters.IBytesInterpreterStatus<LargePacket>.ReadAt(ReadOnlySpan<byte> data, int offset)
        {
            LargePacket? __parsedValue = null;
            try
            {
                if (base.InitializeParsePositionStatus(data, offset)is { } __musoqReadFailure0)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<LargePacket>.Failed(__parsedValue, __musoqReadFailure0);
                }

                SetCurrentField(null);
                __parsedValue = new LargePacket();
                RecordParsedValue(__parsedValue);
                SetCurrentField("Value");
                if (base.PrepareByteInputStatus(data, 4)is { } __musoqReadFailure1)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<LargePacket>.Failed(__parsedValue, __musoqReadFailure1);
                }

                var _value = global::System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(base.TakePreparedBytes(data, 4));
                __parsedValue.Value = _value;
                base.RecordParsedFieldSlot(0);
                SetCurrentField("Tail");
                var __musoqReadSize2 = 4092;
                if (base.PrepareByteArrayStatus(data, __musoqReadSize2)is { } __musoqReadFailure3)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<LargePacket>.Failed(__parsedValue, __musoqReadFailure3);
                }

                var _tail = base.ReadPreparedByteArray(data, __musoqReadSize2);
                __parsedValue.Tail = _tail;
                base.RecordParsedFieldSlot(1);
                return global::Musoq.Schema.Interpreters.ParseAttempt<LargePacket>.Success(__parsedValue);
            }
            catch (global::Musoq.Schema.Interpreters.ParseException __parseFailure)
            {
                return global::Musoq.Schema.Interpreters.ParseAttempt<LargePacket>.Failed(__parsedValue, global::Musoq.Schema.Interpreters.ParseFailure.FromException(__parseFailure));
            }
        }
    }
}
