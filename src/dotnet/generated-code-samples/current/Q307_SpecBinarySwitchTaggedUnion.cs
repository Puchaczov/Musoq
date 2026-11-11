// === Parsed Query ===
/*
binary LoginPayload { UserId: int le };
                    binary DataPayload { Size: short le };
                    binary Packet {
                        Type: byte,
                        Length: byte,
                        Payload: switch Type {
                            1 => Login: LoginPayload,
                            2 => Data: DataPayload,
                            _ => Raw: byte[Length]
                        }
                    };
                    select p.Type, p.Payload.Case, p.Payload.Login.UserId, p.Payload.Data.Size, p.Payload.Raw
                    from #test.files() f
                    cross apply Interpret<Packet>(f.Content) p
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, p.Type as p.Type, p.Payload as p.Payload]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#Packet(f.Content) as p]
  Project [p.Type as Type, p.Payload.Case as p.Payload.Case, p.Payload.Login.UserId as p.Payload.Login.UserId, p.Payload.Data.Size as p.Payload.Data.Size, p.Payload.Raw as p.Payload.Raw]
    CteRef [fp as fp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [p.Type as p.Type, p.Payload as p.Payload]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#Packet(f.Content) as p]
  PhysicalProject [p.Type as Type, p.Payload.Case as p.Payload.Case, p.Payload.Login.UserId as p.Payload.Login.UserId, p.Payload.Data.Size as p.Payload.Data.Size, p.Payload.Raw as p.Payload.Raw]
    PhysicalCteRef [fp as fp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [p: object]
      Type: byte <- property Type
      Length: byte <- property Length
      Payload: ExpandoObject <- property Payload
    Generated [ResultRow0]
      Type: byte <- field Type
      p.Payload.Case: string <- field p_Payload_Case
      p.Payload.Login.UserId: int? <- field p_Payload_Login_UserId
      p.Payload.Data.Size: short? <- field p_Payload_Data_Size
      p.Payload.Raw: byte[] <- field p_Payload_Raw

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [Packet.Interpret(f.Content) -> pRows]
      ScalarForEach [p in pRows]
        AppendShape [result <- ResultRow0(Type: p.Type, p.Payload.Case: p.Payload.Case, p.Payload.Login.UserId: p.Payload.Login.UserId, p.Payload.Data.Size: p.Payload.Data.Size, p.Payload.Raw: p.Payload.Raw)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q307_SpecBinarySwitchTaggedUnion
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
            new Column("Type", typeof(byte), 0),
            new Column("p.Payload.Case", typeof(string), 1),
            new Column("p.Payload.Login.UserId", typeof(int?), 2),
            new Column("p.Payload.Data.Size", typeof(short?), 3),
            new Column("p.Payload.Raw", typeof(byte[]), 4)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_f_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Content", typeof(byte[]), 0) });
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
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    var __fSchema = provider.GetSchema("#test");
                    var fRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>.Open(__fSchema, "files", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["f:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                    var fRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>(fRowsSource.Chunks, __musoqProgressContext, "f:1") : fRowsSource.Chunks;
                    foreach (var fChunk in fRows)
                    {
                        if (fChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity> fChunkView)
                        {
                            if (fChunkView.Source is Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity[] fChunkViewArray)
                            {
                                int fChunkViewOffset = fChunkView.Offset;
                                for (int fIndex = 0, fIndexCount = fChunkView.Count; fIndex < fIndexCount; ++fIndex)
                                {
                                    if ((fIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var f = fChunkViewArray[fChunkViewOffset + fIndex];
                                    var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                                    var pRows = _interpreter_Packet.Interpret(f.Content);
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        yield return new ResultRow0(p.Type, ((Musoq.Generated.Interpreters.Switch_Payload)p.Payload).Case, ((Musoq.Generated.Interpreters.Switch_Payload)p?.Payload)?.Login?.UserId, ((Musoq.Generated.Interpreters.Switch_Payload)p?.Payload)?.Data?.Size, ((Musoq.Generated.Interpreters.Switch_Payload)p?.Payload)?.Raw);
                                    }
                                }

                                continue;
                            }

                            if (fChunkView.Source is List<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity> fChunkViewList)
                            {
                                int fChunkViewOffset = fChunkView.Offset;
                                for (int fIndex = 0, fIndexCount = fChunkView.Count; fIndex < fIndexCount; ++fIndex)
                                {
                                    if ((fIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var f = fChunkViewList[fChunkViewOffset + fIndex];
                                    var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                                    var pRows = _interpreter_Packet.Interpret(f.Content);
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        yield return new ResultRow0(p.Type, ((Musoq.Generated.Interpreters.Switch_Payload)p.Payload).Case, ((Musoq.Generated.Interpreters.Switch_Payload)p?.Payload)?.Login?.UserId, ((Musoq.Generated.Interpreters.Switch_Payload)p?.Payload)?.Data?.Size, ((Musoq.Generated.Interpreters.Switch_Payload)p?.Payload)?.Raw);
                                    }
                                }

                                continue;
                            }
                        }

                        for (int fIndex = 0, fIndexCount = fChunk.Count; fIndex < fIndexCount; ++fIndex)
                        {
                            if ((fIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var f = fChunk[fIndex];
                            var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                            var pRows = _interpreter_Packet.Interpret(f.Content);
                            if (pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = pRows;
                                yield return new ResultRow0(p.Type, ((Musoq.Generated.Interpreters.Switch_Payload)p.Payload).Case, ((Musoq.Generated.Interpreters.Switch_Payload)p?.Payload)?.Login?.UserId, ((Musoq.Generated.Interpreters.Switch_Payload)p?.Payload)?.Data?.Size, ((Musoq.Generated.Interpreters.Switch_Payload)p?.Payload)?.Raw);
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
            public ResultRow0(byte __value0, string __value1, int? __value2, short? __value3, byte[] __value4)
            {
                Type = __value0;
                p_Payload_Case = __value1;
                p_Payload_Login_UserId = __value2;
                p_Payload_Data_Size = __value3;
                p_Payload_Raw = __value4;
            }

            public override int Count => 5;
            public byte Type { get; private set; }
            public string p_Payload_Case { get; private set; }
            public short? p_Payload_Data_Size { get; private set; }
            public int? p_Payload_Login_UserId { get; private set; }
            public byte[] p_Payload_Raw { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Type = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.p_Payload_Case = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.p_Payload_Login_UserId = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short?>(this.p_Payload_Data_Size = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<short?, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte[]>(this.p_Payload_Raw = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte[], T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Type),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.p_Payload_Case),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.p_Payload_Login_UserId),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short?>(this.p_Payload_Data_Size),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte[]>(this.p_Payload_Raw),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Type" => true,
                "p.Payload.Case" => true,
                "p_Payload_Case" => true,
                "Case" => true,
                "p.Payload.Login.UserId" => true,
                "p_Payload_Login_UserId" => true,
                "UserId" => true,
                "p.Payload.Data.Size" => true,
                "p_Payload_Data_Size" => true,
                "Size" => true,
                "p.Payload.Raw" => true,
                "p_Payload_Raw" => true,
                "Raw" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Type, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.p_Payload_Case, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.p_Payload_Login_UserId, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.p_Payload_Data_Size, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.p_Payload_Raw, ref visitor);
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
    /// Generated interpreter for binary schema 'LoginPayload'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class LoginPayload : BytesInterpreterBase<LoginPayload>, IBytesInterpreterStatus<LoginPayload>, IParserCompletionLayoutSource, global::Musoq.Plugins.IJsonPayload
    {
        private static readonly global::Musoq.Schema.Interpreters.ParserCompletionLayout __musoqCompletionLayout = new(new string[] { "UserId" });
        global::Musoq.Schema.Interpreters.ParserCompletionLayout global::Musoq.Schema.Interpreters.IParserCompletionLayoutSource.CompletionLayout => __musoqCompletionLayout;

        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(LoginPayload value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("UserId"))
            {
                writer.WritePropertyName("UserId");
                var fieldValue = this.UserId;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the UserId field value.</summary>
        public int UserId { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "LoginPayload";

        public override LoginPayload InterpretAt(ReadOnlySpan<byte> data, int offset) => ((global::Musoq.Schema.Interpreters.IBytesInterpreterStatus<LoginPayload>)this).ReadAt(data, offset).GetValueOrThrow();
        global::Musoq.Schema.Interpreters.ParseAttempt<LoginPayload> global::Musoq.Schema.Interpreters.IBytesInterpreterStatus<LoginPayload>.ReadAt(ReadOnlySpan<byte> data, int offset)
        {
            LoginPayload? __parsedValue = null;
            try
            {
                if (base.InitializeParsePositionStatus(data, offset)is { } __musoqReadFailure0)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<LoginPayload>.Failed(__parsedValue, __musoqReadFailure0);
                }

                SetCurrentField(null);
                __parsedValue = new LoginPayload();
                RecordParsedValue(__parsedValue);
                SetCurrentField("UserId");
                if (base.PrepareByteInputStatus(data, 4)is { } __musoqReadFailure1)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<LoginPayload>.Failed(__parsedValue, __musoqReadFailure1);
                }

                var _userId = global::System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(base.TakePreparedBytes(data, 4));
                __parsedValue.UserId = _userId;
                base.RecordParsedFieldSlot(0);
                return global::Musoq.Schema.Interpreters.ParseAttempt<LoginPayload>.Success(__parsedValue);
            }
            catch (global::Musoq.Schema.Interpreters.ParseException __parseFailure)
            {
                return global::Musoq.Schema.Interpreters.ParseAttempt<LoginPayload>.Failed(__parsedValue, global::Musoq.Schema.Interpreters.ParseFailure.FromException(__parseFailure));
            }
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'DataPayload'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class DataPayload : BytesInterpreterBase<DataPayload>, IBytesInterpreterStatus<DataPayload>, IParserCompletionLayoutSource, global::Musoq.Plugins.IJsonPayload
    {
        private static readonly global::Musoq.Schema.Interpreters.ParserCompletionLayout __musoqCompletionLayout = new(new string[] { "Size" });
        global::Musoq.Schema.Interpreters.ParserCompletionLayout global::Musoq.Schema.Interpreters.IParserCompletionLayoutSource.CompletionLayout => __musoqCompletionLayout;

        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(DataPayload value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Size"))
            {
                writer.WritePropertyName("Size");
                var fieldValue = this.Size;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Size field value.</summary>
        public short Size { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "DataPayload";

        public override DataPayload InterpretAt(ReadOnlySpan<byte> data, int offset) => ((global::Musoq.Schema.Interpreters.IBytesInterpreterStatus<DataPayload>)this).ReadAt(data, offset).GetValueOrThrow();
        global::Musoq.Schema.Interpreters.ParseAttempt<DataPayload> global::Musoq.Schema.Interpreters.IBytesInterpreterStatus<DataPayload>.ReadAt(ReadOnlySpan<byte> data, int offset)
        {
            DataPayload? __parsedValue = null;
            try
            {
                if (base.InitializeParsePositionStatus(data, offset)is { } __musoqReadFailure0)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<DataPayload>.Failed(__parsedValue, __musoqReadFailure0);
                }

                SetCurrentField(null);
                __parsedValue = new DataPayload();
                RecordParsedValue(__parsedValue);
                SetCurrentField("Size");
                if (base.PrepareByteInputStatus(data, 2)is { } __musoqReadFailure1)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<DataPayload>.Failed(__parsedValue, __musoqReadFailure1);
                }

                var _size = global::System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(base.TakePreparedBytes(data, 2));
                __parsedValue.Size = _size;
                base.RecordParsedFieldSlot(0);
                return global::Musoq.Schema.Interpreters.ParseAttempt<DataPayload>.Success(__parsedValue);
            }
            catch (global::Musoq.Schema.Interpreters.ParseException __parseFailure)
            {
                return global::Musoq.Schema.Interpreters.ParseAttempt<DataPayload>.Failed(__parsedValue, global::Musoq.Schema.Interpreters.ParseFailure.FromException(__parseFailure));
            }
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'Packet'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Packet : BytesInterpreterBase<Packet>, IBytesInterpreterStatus<Packet>, IParserCompletionLayoutSource, global::Musoq.Plugins.IJsonPayload
    {
        private static readonly global::Musoq.Schema.Interpreters.ParserCompletionLayout __musoqCompletionLayout = new(new string[] { "Type", "Length", "Payload" });
        global::Musoq.Schema.Interpreters.ParserCompletionLayout global::Musoq.Schema.Interpreters.IParserCompletionLayoutSource.CompletionLayout => __musoqCompletionLayout;

        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Packet value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Type"))
            {
                writer.WritePropertyName("Type");
                var fieldValue = this.Type;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Length"))
            {
                writer.WritePropertyName("Length");
                var fieldValue = this.Length;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Payload"))
            {
                writer.WritePropertyName("Payload");
                var fieldValue = this.Payload;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Type field value.</summary>
        public byte Type { get; set; }
        /// <summary>Gets the Length field value.</summary>
        public byte Length { get; set; }
        /// <summary>Gets the Payload field value.</summary>
        public Switch_Payload Payload { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Packet";

        public override Packet InterpretAt(ReadOnlySpan<byte> data, int offset) => ((global::Musoq.Schema.Interpreters.IBytesInterpreterStatus<Packet>)this).ReadAt(data, offset).GetValueOrThrow();
        global::Musoq.Schema.Interpreters.ParseAttempt<Packet> global::Musoq.Schema.Interpreters.IBytesInterpreterStatus<Packet>.ReadAt(ReadOnlySpan<byte> data, int offset)
        {
            Packet? __parsedValue = null;
            try
            {
                if (base.InitializeParsePositionStatus(data, offset)is { } __musoqReadFailure0)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Packet>.Failed(__parsedValue, __musoqReadFailure0);
                }

                SetCurrentField(null);
                __parsedValue = new Packet();
                RecordParsedValue(__parsedValue);
                SetCurrentField("Type");
                if (base.PrepareByteInputStatus(data, 1)is { } __musoqReadFailure1)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Packet>.Failed(__parsedValue, __musoqReadFailure1);
                }

                var _type = base.TakePreparedBytes(data, 1)[0];
                __parsedValue.Type = _type;
                base.RecordParsedFieldSlot(0);
                SetCurrentField("Length");
                if (base.PrepareByteInputStatus(data, 1)is { } __musoqReadFailure2)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Packet>.Failed(__parsedValue, __musoqReadFailure2);
                }

                var _length = base.TakePreparedBytes(data, 1)[0];
                __parsedValue.Length = _length;
                base.RecordParsedFieldSlot(1);
                SetCurrentField("Payload");
                string _payload_case = null;
                LoginPayload _payload_Login = default;
                DataPayload _payload_Data = default;
                byte[] _payload_Raw = default;
                if (_type == 1)
                {
                    var __payload_Login_value_interpreter = new LoginPayload();
                    var __musoqReadResult3 = base.InterpretNestedStatus(__payload_Login_value_interpreter, data, "Payload");
                    if (!__musoqReadResult3.Succeeded)
                    {
                        return global::Musoq.Schema.Interpreters.ParseAttempt<Packet>.Failed(__parsedValue, __musoqReadResult3.Failure!);
                    }

                    var _payload_Login_value = __musoqReadResult3.Value!;
                    _payload_Login = _payload_Login_value;
                    _payload_case = "Login";
                }
                else if (_type == 2)
                {
                    var __payload_Data_value_interpreter = new DataPayload();
                    var __musoqReadResult4 = base.InterpretNestedStatus(__payload_Data_value_interpreter, data, "Payload");
                    if (!__musoqReadResult4.Succeeded)
                    {
                        return global::Musoq.Schema.Interpreters.ParseAttempt<Packet>.Failed(__parsedValue, __musoqReadResult4.Failure!);
                    }

                    var _payload_Data_value = __musoqReadResult4.Value!;
                    _payload_Data = _payload_Data_value;
                    _payload_case = "Data";
                }
                else
                {
                    var __musoqReadResult6 = base.ResolveSizeValueStatus<byte>((byte?)(_length), ParseErrorCode.InvalidSize);
                    if (!__musoqReadResult6.Succeeded)
                    {
                        return global::Musoq.Schema.Interpreters.ParseAttempt<Packet>.Failed(__parsedValue, __musoqReadResult6.Failure!);
                    }

                    var __musoqReadSize5 = __musoqReadResult6.Value!;
                    if (base.PrepareByteArrayStatus(data, __musoqReadSize5)is { } __musoqReadFailure7)
                    {
                        return global::Musoq.Schema.Interpreters.ParseAttempt<Packet>.Failed(__parsedValue, __musoqReadFailure7);
                    }

                    var _payload_Raw_value = base.ReadPreparedByteArray(data, __musoqReadSize5);
                    _payload_Raw = _payload_Raw_value;
                    _payload_case = "Raw";
                }

                var _payload = new Switch_Payload
                {
                    Case = _payload_case,
                    Login = _payload_Login,
                    Data = _payload_Data,
                    Raw = _payload_Raw
                };
                __parsedValue.Payload = _payload;
                base.RecordParsedFieldSlot(2);
                return global::Musoq.Schema.Interpreters.ParseAttempt<Packet>.Success(__parsedValue);
            }
            catch (global::Musoq.Schema.Interpreters.ParseException __parseFailure)
            {
                return global::Musoq.Schema.Interpreters.ParseAttempt<Packet>.Failed(__parsedValue, global::Musoq.Schema.Interpreters.ParseFailure.FromException(__parseFailure));
            }
        }
    }

    /// <summary>
    /// Generated tagged-union value for binary switch 'Switch_Payload'.
    /// </summary>
    public sealed class Switch_Payload
    {
        /// <summary>Gets the selected branch alias.</summary>
        public string Case { get; init; }
        /// <summary>Gets the 'Login' branch value; non-null only when selected.</summary>
        public LoginPayload Login { get; init; }
        /// <summary>Gets the 'Data' branch value; non-null only when selected.</summary>
        public DataPayload Data { get; init; }
        /// <summary>Gets the 'Raw' branch value; non-null only when selected.</summary>
        public byte[] Raw { get; init; }
    }
}
