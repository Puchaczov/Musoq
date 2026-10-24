// === Parsed Query ===
/*
binary Body { A: byte, B: byte };
                    binary Frame {
                        ExactLength: byte,
                        Exact: substream[ExactLength] as Body exact,
                        LaxLength: byte,
                        Lax: substream[LaxLength] as Body lax,
                        InlineLength: byte,
                        Inline: substream[InlineLength] as { A: byte, B: byte }
                    };
                    select f.Exact.A, f.Exact.B, f.Lax.A, f.Lax.B, f.Inline.A, f.Inline.B
                    from #test.files() src
                    cross apply Interpret<Frame>(src.Content) f
*/

// === Logical Plan ===
/*
MultiStatement
  Project [src.Content as src.Content, f.Exact as f.Exact, f.Lax as f.Lax, f.Inline as f.Inline]
    Apply [Cross]
      SchemaScan [#test.files() as src]
      InterpretSource [#Frame(src.Content) as f]
  Project [f.Exact.A as f.Exact.A, f.Exact.B as f.Exact.B, f.Lax.A as f.Lax.A, f.Lax.B as f.Lax.B, f.Inline.A as f.Inline.A, f.Inline.B as f.Inline.B]
    CteRef [srcf as srcf]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [src.Content as src.Content, f.Exact as f.Exact, f.Lax as f.Lax, f.Inline as f.Inline]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as src]
      PhysicalInterpretSource [#Frame(src.Content) as f]
  PhysicalProject [f.Exact.A as f.Exact.A, f.Exact.B as f.Exact.B, f.Lax.A as f.Lax.A, f.Lax.B as f.Lax.B, f.Inline.A as f.Inline.A, f.Inline.B as f.Inline.B]
    PhysicalCteRef [srcf as srcf]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [src: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [f: object]
      ExactLength: byte <- property ExactLength
      Exact: object <- property Exact
      Exact.A: byte <- nested property Exact.A
      Exact.B: byte <- nested property Exact.B
      LaxLength: byte <- property LaxLength
      Lax: object <- property Lax
      Lax.A: byte <- nested property Lax.A
      Lax.B: byte <- nested property Lax.B
      InlineLength: byte <- property InlineLength
      Inline: object <- property Inline
    Generated [ResultRow0]
      f.Exact.A: byte <- field f_Exact_A
      f.Exact.B: byte <- field f_Exact_B
      f.Lax.A: byte <- field f_Lax_A
      f.Lax.B: byte <- field f_Lax_B
      f.Inline.A: byte <- field f_Inline_A
      f.Inline.B: byte <- field f_Inline_B

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [src: BinaryEntity] -> srcRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    ChunkedForEach [src in srcRows]
      InterpretSource [Frame.Interpret(src.Content) -> fRows]
      ScalarForEach [f in fRows]
        AppendShape [result <- ResultRow0(f.Exact.A: f.Exact.A, f.Exact.B: f.Exact.B, f.Lax.A: f.Lax.A, f.Lax.B: f.Lax.B, f.Inline.A: f.Inline.A, f.Inline.B: f.Inline.B)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q309_SpecBinaryStructuredSubstreams
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
            new Column("f.Exact.A", typeof(byte), 0),
            new Column("f.Exact.B", typeof(byte), 1),
            new Column("f.Lax.A", typeof(byte), 2),
            new Column("f.Lax.B", typeof(byte), 3),
            new Column("f.Inline.A", typeof(byte), 4),
            new Column("f.Inline.B", typeof(byte), 5)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_src_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Content", typeof(byte[]), 0) });
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
                    var __srcSchema = provider.GetSchema("#test");
                    var srcRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>.Open(__srcSchema, "files", new SourceExecutionContext("src:1", sourceExecutionPlans["src:1"], token, __schemaColumns_compiled_src_0, sourceRuntimeSettingsBySourceContextId["src:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                    var srcRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>(srcRowsSource.Chunks, __musoqProgressContext, "src:1") : srcRowsSource.Chunks;
                    foreach (var srcChunk in srcRows)
                    {
                        if (srcChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity> srcChunkView)
                        {
                            if (srcChunkView.Source is Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity[] srcChunkViewArray)
                            {
                                int srcChunkViewOffset = srcChunkView.Offset;
                                for (int srcIndex = 0, srcIndexCount = srcChunkView.Count; srcIndex < srcIndexCount; ++srcIndex)
                                {
                                    if ((srcIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var src = srcChunkViewArray[srcChunkViewOffset + srcIndex];
                                    var _interpreter_Frame = new Musoq.Generated.Interpreters.Frame();
                                    var fRows = _interpreter_Frame.Interpret(src.Content);
                                    if (fRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var f = fRows;
                                        yield return new ResultRow0(f.Exact.A, f.Exact.B, f.Lax.A, f.Lax.B, ((Musoq.Generated.Interpreters.Inline_Inline)f.Inline).A, ((Musoq.Generated.Interpreters.Inline_Inline)f.Inline).B);
                                    }
                                }

                                continue;
                            }

                            if (srcChunkView.Source is List<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity> srcChunkViewList)
                            {
                                int srcChunkViewOffset = srcChunkView.Offset;
                                for (int srcIndex = 0, srcIndexCount = srcChunkView.Count; srcIndex < srcIndexCount; ++srcIndex)
                                {
                                    if ((srcIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var src = srcChunkViewList[srcChunkViewOffset + srcIndex];
                                    var _interpreter_Frame = new Musoq.Generated.Interpreters.Frame();
                                    var fRows = _interpreter_Frame.Interpret(src.Content);
                                    if (fRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var f = fRows;
                                        yield return new ResultRow0(f.Exact.A, f.Exact.B, f.Lax.A, f.Lax.B, ((Musoq.Generated.Interpreters.Inline_Inline)f.Inline).A, ((Musoq.Generated.Interpreters.Inline_Inline)f.Inline).B);
                                    }
                                }

                                continue;
                            }
                        }

                        for (int srcIndex = 0, srcIndexCount = srcChunk.Count; srcIndex < srcIndexCount; ++srcIndex)
                        {
                            if ((srcIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var src = srcChunk[srcIndex];
                            var _interpreter_Frame = new Musoq.Generated.Interpreters.Frame();
                            var fRows = _interpreter_Frame.Interpret(src.Content);
                            if (fRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var f = fRows;
                                yield return new ResultRow0(f.Exact.A, f.Exact.B, f.Lax.A, f.Lax.B, ((Musoq.Generated.Interpreters.Inline_Inline)f.Inline).A, ((Musoq.Generated.Interpreters.Inline_Inline)f.Inline).B);
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
            public ResultRow0(byte __value0, byte __value1, byte __value2, byte __value3, byte __value4, byte __value5)
            {
                f_Exact_A = __value0;
                f_Exact_B = __value1;
                f_Lax_A = __value2;
                f_Lax_B = __value3;
                f_Inline_A = __value4;
                f_Inline_B = __value5;
            }

            public override int Count => 6;
            public byte f_Exact_A { get; private set; }
            public byte f_Exact_B { get; private set; }
            public byte f_Inline_A { get; private set; }
            public byte f_Inline_B { get; private set; }
            public byte f_Lax_A { get; private set; }
            public byte f_Lax_B { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.f_Exact_A = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.f_Exact_B = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.f_Lax_A = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.f_Lax_B = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.f_Inline_A = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.f_Inline_B = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.f_Exact_A),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.f_Exact_B),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.f_Lax_A),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.f_Lax_B),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.f_Inline_A),
                5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.f_Inline_B),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "f.Exact.A" => true,
                "f_Exact_A" => true,
                "f.Exact.B" => true,
                "f_Exact_B" => true,
                "f.Lax.A" => true,
                "f_Lax_A" => true,
                "f.Lax.B" => true,
                "f_Lax_B" => true,
                "f.Inline.A" => true,
                "f_Inline_A" => true,
                "f.Inline.B" => true,
                "f_Inline_B" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.f_Exact_A, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.f_Exact_B, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.f_Lax_A, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.f_Lax_B, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.f_Inline_A, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(5, this.f_Inline_B, ref visitor);
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
    /// Generated interpreter for binary schema 'Body'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Body : BytesInterpreterBase<Body>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Body value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("A"))
            {
                writer.WritePropertyName("A");
                var fieldValue = this.A;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("B"))
            {
                writer.WritePropertyName("B");
                var fieldValue = this.B;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the A field value.</summary>
        public byte A { get; set; }
        /// <summary>Gets the B field value.</summary>
        public byte B { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Body";

        /// <inheritdoc/>
        public override Body InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new Body();
            RecordParsedValue(__parsedValue);
            SetCurrentField("A");
            var _a = ReadByte(data);
            __parsedValue.A = _a;
            RecordParsedField("A");
            SetCurrentField("B");
            var _b = ReadByte(data);
            __parsedValue.B = _b;
            RecordParsedField("B");
            return __parsedValue;
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'Frame'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Frame : BytesInterpreterBase<Frame>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Frame value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("ExactLength"))
            {
                writer.WritePropertyName("ExactLength");
                var fieldValue = this.ExactLength;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Exact"))
            {
                writer.WritePropertyName("Exact");
                var fieldValue = this.Exact;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("LaxLength"))
            {
                writer.WritePropertyName("LaxLength");
                var fieldValue = this.LaxLength;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Lax"))
            {
                writer.WritePropertyName("Lax");
                var fieldValue = this.Lax;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("InlineLength"))
            {
                writer.WritePropertyName("InlineLength");
                var fieldValue = this.InlineLength;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Inline"))
            {
                writer.WritePropertyName("Inline");
                var fieldValue = this.Inline;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the ExactLength field value.</summary>
        public byte ExactLength { get; set; }
        /// <summary>Gets the Exact field value.</summary>
        public Body Exact { get; set; }
        /// <summary>Gets the LaxLength field value.</summary>
        public byte LaxLength { get; set; }
        /// <summary>Gets the Lax field value.</summary>
        public Body Lax { get; set; }
        /// <summary>Gets the InlineLength field value.</summary>
        public byte InlineLength { get; set; }
        /// <summary>Gets the Inline field value.</summary>
        public Inline_Inline Inline { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Frame";

        /// <inheritdoc/>
        public override Frame InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new Frame();
            RecordParsedValue(__parsedValue);
            SetCurrentField("ExactLength");
            var _exactLength = ReadByte(data);
            __parsedValue.ExactLength = _exactLength;
            RecordParsedField("ExactLength");
            SetCurrentField("Exact");
            var __exact_substreamLength = ResolveExpressionSize<byte>(() => checked((byte?)(_exactLength)), ParseErrorCode.InvalidSize);
            var __exact_substreamSlice = ReadSubstreamSlice(data, __exact_substreamLength);
            var __exact_substreamStart = ParsePosition;
            var __exact_substreamInterpreter = new Body();
            var _exact = InterpretNestedAt(__exact_substreamInterpreter, __exact_substreamSlice, 0, "Exact");
            EnsureSubstreamFullyConsumed("Exact", __exact_substreamLength, __exact_substreamInterpreter);
            ParsePosition = __exact_substreamStart + __exact_substreamLength;
            BitOffset = 0;
            __parsedValue.Exact = _exact;
            RecordParsedField("Exact");
            SetCurrentField("LaxLength");
            var _laxLength = ReadByte(data);
            __parsedValue.LaxLength = _laxLength;
            RecordParsedField("LaxLength");
            SetCurrentField("Lax");
            var __lax_substreamLength = ResolveExpressionSize<byte>(() => checked((byte?)(_laxLength)), ParseErrorCode.InvalidSize);
            var __lax_substreamSlice = ReadSubstreamSlice(data, __lax_substreamLength);
            var __lax_substreamStart = ParsePosition;
            var __lax_substreamInterpreter = new Body();
            var _lax = InterpretNestedAt(__lax_substreamInterpreter, __lax_substreamSlice, 0, "Lax");
            ParsePosition = __lax_substreamStart + __lax_substreamLength;
            BitOffset = 0;
            __parsedValue.Lax = _lax;
            RecordParsedField("Lax");
            SetCurrentField("InlineLength");
            var _inlineLength = ReadByte(data);
            __parsedValue.InlineLength = _inlineLength;
            RecordParsedField("InlineLength");
            SetCurrentField("Inline");
            var __inline_substreamLength = ResolveExpressionSize<byte>(() => checked((byte?)(_inlineLength)), ParseErrorCode.InvalidSize);
            var __inline_substreamSlice = ReadSubstreamSlice(data, __inline_substreamLength);
            var __inline_substreamStart = ParsePosition;
            var __inline_substreamInterpreter = new Inline_Inline();
            var _inline = InterpretNestedAt(__inline_substreamInterpreter, __inline_substreamSlice, 0, "Inline");
            EnsureSubstreamFullyConsumed("Inline", __inline_substreamLength, __inline_substreamInterpreter);
            ParsePosition = __inline_substreamStart + __inline_substreamLength;
            BitOffset = 0;
            __parsedValue.Inline = _inline;
            RecordParsedField("Inline");
            return __parsedValue;
        }
    }

    /// <summary>
    /// Generated nested interpreter for inline schema 'Inline_Inline'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Inline_Inline : BytesInterpreterBase<Inline_Inline>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Inline_Inline value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("A"))
            {
                writer.WritePropertyName("A");
                var fieldValue = this.A;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("B"))
            {
                writer.WritePropertyName("B");
                var fieldValue = this.B;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the A field value.</summary>
        public byte A { get; set; }
        /// <summary>Gets the B field value.</summary>
        public byte B { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Inline_Inline";

        /// <inheritdoc/>
        public override Inline_Inline InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new Inline_Inline();
            RecordParsedValue(__parsedValue);
            SetCurrentField("A");
            var _a = ReadByte(data);
            __parsedValue.A = _a;
            RecordParsedField("A");
            SetCurrentField("B");
            var _b = ReadByte(data);
            __parsedValue.B = _b;
            RecordParsedField("B");
            return __parsedValue;
        }
    }
}
