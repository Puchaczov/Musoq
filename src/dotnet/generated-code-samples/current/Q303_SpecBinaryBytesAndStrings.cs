// === Parsed Query ===
/*
binary EncodedRecord {
                        FixedBytes: byte[4],
                        Length: byte,
                        DynamicBytes: byte[Length],
                        AsciiValue: string[4] ascii trim,
                        Utf8Value: string[8] utf8 rtrim,
                        Utf16Value: string[8] utf16le trim,
                        LatinValue: string[4] latin1 ltrim,
                        NullTermValue: string[8] ascii nullterm
                    };
                    select r.FixedBytes[0], r.DynamicBytes[0], r.AsciiValue, r.Utf8Value, r.Utf16Value, r.LatinValue, r.NullTermValue
                    from #test.files() f
                    cross apply Interpret<EncodedRecord>(f.Content) r
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, r.FixedBytes as r.FixedBytes, r.DynamicBytes as r.DynamicBytes, r.AsciiValue as r.AsciiValue, r.Utf8Value as r.Utf8Value, r.Utf16Value as r.Utf16Value, r.LatinValue as r.LatinValue, r.NullTermValue as r.NullTermValue]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#EncodedRecord(f.Content) as r]
  Project [r.FixedBytes[0] as r.FixedBytes[0], r.DynamicBytes[0] as r.DynamicBytes[0], r.AsciiValue as AsciiValue, r.Utf8Value as Utf8Value, r.Utf16Value as Utf16Value, r.LatinValue as LatinValue, r.NullTermValue as NullTermValue]
    CteRef [fr as fr]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.FixedBytes as r.FixedBytes, r.DynamicBytes as r.DynamicBytes, r.AsciiValue as r.AsciiValue, r.Utf8Value as r.Utf8Value, r.Utf16Value as r.Utf16Value, r.LatinValue as r.LatinValue, r.NullTermValue as r.NullTermValue]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#EncodedRecord(f.Content) as r]
  PhysicalProject [r.FixedBytes[0] as r.FixedBytes[0], r.DynamicBytes[0] as r.DynamicBytes[0], r.AsciiValue as AsciiValue, r.Utf8Value as Utf8Value, r.Utf16Value as Utf16Value, r.LatinValue as LatinValue, r.NullTermValue as NullTermValue]
    PhysicalCteRef [fr as fr]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [r: object]
      FixedBytes: byte[] <- property FixedBytes
      Length: byte <- property Length
      DynamicBytes: byte[] <- property DynamicBytes
      AsciiValue: string <- property AsciiValue
      Utf8Value: string <- property Utf8Value
      Utf16Value: string <- property Utf16Value
      LatinValue: string <- property LatinValue
      NullTermValue: string <- property NullTermValue
    Generated [ResultRow0]
      r.FixedBytes[0]: byte <- field r_FixedBytes_0_
      r.DynamicBytes[0]: byte <- field r_DynamicBytes_0_
      AsciiValue: string <- field AsciiValue
      Utf8Value: string <- field Utf8Value
      Utf16Value: string <- field Utf16Value
      LatinValue: string <- field LatinValue
      NullTermValue: string <- field NullTermValue

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [EncodedRecord.Interpret(f.Content) -> rRows]
      ScalarForEach [r in rRows]
        AppendShape [result <- ResultRow0(r.FixedBytes[0]: GetTypedReadOnlyListElement(r.FixedBytes, 0), r.DynamicBytes[0]: GetTypedReadOnlyListElement(r.DynamicBytes, 0), AsciiValue: r.AsciiValue, Utf8Value: r.Utf8Value, Utf16Value: r.Utf16Value, LatinValue: r.LatinValue, NullTermValue: r.NullTermValue)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q303_SpecBinaryBytesAndStrings
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
            new Column("r.FixedBytes[0]", typeof(byte), 0),
            new Column("r.DynamicBytes[0]", typeof(byte), 1),
            new Column("AsciiValue", typeof(string), 2),
            new Column("Utf8Value", typeof(string), 3),
            new Column("Utf16Value", typeof(string), 4),
            new Column("LatinValue", typeof(string), 5),
            new Column("NullTermValue", typeof(string), 6)
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
                                    var _interpreter_EncodedRecord = new Musoq.Generated.Interpreters.EncodedRecord();
                                    var rRows = _interpreter_EncodedRecord.Interpret(f.Content);
                                    if (rRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var r = rRows;
                                        yield return new ResultRow0((byte)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<byte[], byte>(r.FixedBytes, 0), (byte)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<byte[], byte>(r.DynamicBytes, 0), r.AsciiValue, r.Utf8Value, r.Utf16Value, r.LatinValue, r.NullTermValue);
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
                                    var _interpreter_EncodedRecord = new Musoq.Generated.Interpreters.EncodedRecord();
                                    var rRows = _interpreter_EncodedRecord.Interpret(f.Content);
                                    if (rRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var r = rRows;
                                        yield return new ResultRow0((byte)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<byte[], byte>(r.FixedBytes, 0), (byte)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<byte[], byte>(r.DynamicBytes, 0), r.AsciiValue, r.Utf8Value, r.Utf16Value, r.LatinValue, r.NullTermValue);
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
                            var _interpreter_EncodedRecord = new Musoq.Generated.Interpreters.EncodedRecord();
                            var rRows = _interpreter_EncodedRecord.Interpret(f.Content);
                            if (rRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var r = rRows;
                                yield return new ResultRow0((byte)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<byte[], byte>(r.FixedBytes, 0), (byte)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<byte[], byte>(r.DynamicBytes, 0), r.AsciiValue, r.Utf8Value, r.Utf16Value, r.LatinValue, r.NullTermValue);
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
            public ResultRow0(byte __value0, byte __value1, string __value2, string __value3, string __value4, string __value5, string __value6)
            {
                r_FixedBytes_0_ = __value0;
                r_DynamicBytes_0_ = __value1;
                AsciiValue = __value2;
                Utf8Value = __value3;
                Utf16Value = __value4;
                LatinValue = __value5;
                NullTermValue = __value6;
            }

            public string AsciiValue { get; private set; }
            public override int Count => 7;
            public string LatinValue { get; private set; }
            public string NullTermValue { get; private set; }
            public string Utf16Value { get; private set; }
            public string Utf8Value { get; private set; }
            public byte r_DynamicBytes_0_ { get; private set; }
            public byte r_FixedBytes_0_ { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.r_FixedBytes_0_ = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.r_DynamicBytes_0_ = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.AsciiValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Utf8Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Utf16Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.LatinValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.NullTermValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.r_FixedBytes_0_),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.r_DynamicBytes_0_),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.AsciiValue),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Utf8Value),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Utf16Value),
                5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.LatinValue),
                6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.NullTermValue),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "r.FixedBytes[0]" => true,
                "r_FixedBytes_0_" => true,
                "FixedBytes[0]" => true,
                "r.DynamicBytes[0]" => true,
                "r_DynamicBytes_0_" => true,
                "DynamicBytes[0]" => true,
                "AsciiValue" => true,
                "Utf8Value" => true,
                "Utf16Value" => true,
                "LatinValue" => true,
                "NullTermValue" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.r_FixedBytes_0_, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.r_DynamicBytes_0_, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.AsciiValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Utf8Value, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.Utf16Value, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(5, this.LatinValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(6, this.NullTermValue, ref visitor);
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
    /// Generated interpreter for binary schema 'EncodedRecord'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class EncodedRecord : BytesInterpreterBase<EncodedRecord>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(EncodedRecord value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("FixedBytes"))
            {
                writer.WritePropertyName("FixedBytes");
                var fieldValue = this.FixedBytes;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Length"))
            {
                writer.WritePropertyName("Length");
                var fieldValue = this.Length;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("DynamicBytes"))
            {
                writer.WritePropertyName("DynamicBytes");
                var fieldValue = this.DynamicBytes;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("AsciiValue"))
            {
                writer.WritePropertyName("AsciiValue");
                var fieldValue = this.AsciiValue;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Utf8Value"))
            {
                writer.WritePropertyName("Utf8Value");
                var fieldValue = this.Utf8Value;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Utf16Value"))
            {
                writer.WritePropertyName("Utf16Value");
                var fieldValue = this.Utf16Value;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("LatinValue"))
            {
                writer.WritePropertyName("LatinValue");
                var fieldValue = this.LatinValue;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("NullTermValue"))
            {
                writer.WritePropertyName("NullTermValue");
                var fieldValue = this.NullTermValue;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the FixedBytes field value.</summary>
        public byte[] FixedBytes { get; set; }
        /// <summary>Gets the Length field value.</summary>
        public byte Length { get; set; }
        /// <summary>Gets the DynamicBytes field value.</summary>
        public byte[] DynamicBytes { get; set; }
        /// <summary>Gets the AsciiValue field value.</summary>
        public string AsciiValue { get; set; }
        /// <summary>Gets the Utf8Value field value.</summary>
        public string Utf8Value { get; set; }
        /// <summary>Gets the Utf16Value field value.</summary>
        public string Utf16Value { get; set; }
        /// <summary>Gets the LatinValue field value.</summary>
        public string LatinValue { get; set; }
        /// <summary>Gets the NullTermValue field value.</summary>
        public string NullTermValue { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "EncodedRecord";

        /// <inheritdoc/>
        public override EncodedRecord InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new EncodedRecord();
            RecordParsedValue(__parsedValue);
            SetCurrentField("FixedBytes");
            var _fixedBytes = ReadBytes(data, 4);
            __parsedValue.FixedBytes = _fixedBytes;
            RecordParsedField("FixedBytes");
            SetCurrentField("Length");
            var _length = ReadByte(data);
            __parsedValue.Length = _length;
            RecordParsedField("Length");
            SetCurrentField("DynamicBytes");
            var _dynamicBytes = ReadBytes(data, ResolveExpressionSize<byte>(() => checked((byte?)(_length)), ParseErrorCode.InvalidSize));
            __parsedValue.DynamicBytes = _dynamicBytes;
            RecordParsedField("DynamicBytes");
            SetCurrentField("AsciiValue");
            var _asciiValue = ReadString(data, 4, System.Text.Encoding.ASCII).Trim();
            __parsedValue.AsciiValue = _asciiValue;
            RecordParsedField("AsciiValue");
            SetCurrentField("Utf8Value");
            var _utf8Value = ReadString(data, 8, System.Text.Encoding.UTF8).TrimEnd();
            __parsedValue.Utf8Value = _utf8Value;
            RecordParsedField("Utf8Value");
            SetCurrentField("Utf16Value");
            var _utf16Value = ReadString(data, 8, System.Text.Encoding.Unicode).Trim();
            __parsedValue.Utf16Value = _utf16Value;
            RecordParsedField("Utf16Value");
            SetCurrentField("LatinValue");
            var _latinValue = ReadString(data, 4, System.Text.Encoding.Latin1).TrimStart();
            __parsedValue.LatinValue = _latinValue;
            RecordParsedField("LatinValue");
            SetCurrentField("NullTermValue");
            var _nullTermValue = ReadNullTerminatedString(data, 8, System.Text.Encoding.ASCII);
            __parsedValue.NullTermValue = _nullTermValue;
            RecordParsedField("NullTermValue");
            return __parsedValue;
        }
    }
}
