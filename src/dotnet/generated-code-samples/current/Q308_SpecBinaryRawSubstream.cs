// === Parsed Query ===
/*
binary Packet {
                        Kind: byte,
                        Length: byte,
                        Payload: substream[Length] raw,
                        Checksum: byte
                    };
                    select p.Kind, p.Payload, p.Checksum
                    from #test.files() f
                    cross apply Interpret<Packet>(f.Content) p
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, p.Kind as p.Kind, p.Payload as p.Payload, p.Checksum as p.Checksum]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#Packet(f.Content) as p]
  Project [p.Kind as Kind, p.Payload as Payload, p.Checksum as Checksum]
    CteRef [fp as fp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, p.Kind as p.Kind, p.Payload as p.Payload, p.Checksum as p.Checksum]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#Packet(f.Content) as p]
  PhysicalProject [p.Kind as Kind, p.Payload as Payload, p.Checksum as Checksum]
    PhysicalCteRef [fp as fp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [p: object]
      Kind: byte <- property Kind
      Length: byte <- property Length
      Payload: byte[] <- property Payload
      Checksum: byte <- property Checksum
    Generated [ResultRow0]
      Kind: byte <- field Kind
      Payload: byte[] <- field Payload
      Checksum: byte <- field Checksum

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
        AppendShape [result <- ResultRow0(Kind: p.Kind, Payload: p.Payload, Checksum: p.Checksum)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q308_SpecBinaryRawSubstream
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
            new Column("Kind", typeof(byte), 0),
            new Column("Payload", typeof(byte[]), 1),
            new Column("Checksum", typeof(byte), 2)
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
                    var fRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>.Open(__fSchema, "files", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
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
                                        yield return new ResultRow0(p.Kind, p.Payload, p.Checksum);
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
                                        yield return new ResultRow0(p.Kind, p.Payload, p.Checksum);
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
                                yield return new ResultRow0(p.Kind, p.Payload, p.Checksum);
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
            public ResultRow0(byte __value0, byte[] __value1, byte __value2)
            {
                Kind = __value0;
                Payload = __value1;
                Checksum = __value2;
            }

            public byte Checksum { get; private set; }
            public override int Count => 3;
            public byte Kind { get; private set; }
            public byte[] Payload { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Kind = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte[]>(this.Payload = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte[], T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Checksum = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Kind),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte[]>(this.Payload),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Checksum),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Kind" => true,
                "Payload" => true,
                "Checksum" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Kind, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Payload, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Checksum, ref visitor);
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
    /// Generated interpreter for binary schema 'Packet'.
    /// </summary>
    public sealed class Packet : BytesInterpreterBase<Packet>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Packet value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Kind"))
            {
                writer.WritePropertyName("Kind");
                var fieldValue = this.Kind;
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

            if (IsFieldComplete("Checksum"))
            {
                writer.WritePropertyName("Checksum");
                var fieldValue = this.Checksum;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Kind field value.</summary>
        public byte Kind { get; set; }
        /// <summary>Gets the Length field value.</summary>
        public byte Length { get; set; }
        /// <summary>Gets the Payload field value.</summary>
        public byte[] Payload { get; set; }
        /// <summary>Gets the Checksum field value.</summary>
        public byte Checksum { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Packet";

        /// <inheritdoc/>
        public override Packet InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new Packet();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Kind");
            var _kind = ReadByte(data);
            __parsedValue.Kind = _kind;
            RecordParsedField("Kind");
            SetCurrentField("Length");
            var _length = ReadByte(data);
            __parsedValue.Length = _length;
            RecordParsedField("Length");
            SetCurrentField("Payload");
            var _payload = ReadBytes(data, ResolveExpressionSize<byte>(() => checked((byte?)(_length)), ParseErrorCode.InvalidSize));
            __parsedValue.Payload = _payload;
            RecordParsedField("Payload");
            SetCurrentField("Checksum");
            var _checksum = ReadByte(data);
            __parsedValue.Checksum = _checksum;
            RecordParsedField("Checksum");
            return __parsedValue;
        }
    }
}
