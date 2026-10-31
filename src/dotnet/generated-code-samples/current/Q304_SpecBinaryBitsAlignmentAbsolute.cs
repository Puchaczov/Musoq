// === Parsed Query ===
/*
binary PositionedFlags {
                        Flags: bits[3],
                        _: align[8],
                        NextByte: byte,
                        Signature: int le at 0
                    };
                    select p.Flags, p.NextByte, p.Signature
                    from #test.files() f
                    cross apply Interpret<PositionedFlags>(f.Content) p
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, p.Flags as p.Flags, p.NextByte as p.NextByte, p.Signature as p.Signature]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#PositionedFlags(f.Content) as p]
  Project [p.Flags as Flags, p.NextByte as NextByte, p.Signature as Signature]
    CteRef [fp as fp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [p.Flags as p.Flags, p.NextByte as p.NextByte, p.Signature as p.Signature]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#PositionedFlags(f.Content) as p]
  PhysicalProject [p.Flags as Flags, p.NextByte as NextByte, p.Signature as Signature]
    PhysicalCteRef [fp as fp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [p: object]
      Flags: byte <- property Flags
      NextByte: byte <- property NextByte
      Signature: int <- property Signature
    Generated [ResultRow0]
      Flags: byte <- field Flags
      NextByte: byte <- field NextByte
      Signature: int <- field Signature

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [PositionedFlags.Interpret(f.Content) -> pRows]
      ScalarForEach [p in pRows]
        AppendShape [result <- ResultRow0(Flags: p.Flags, NextByte: p.NextByte, Signature: p.Signature)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q304_SpecBinaryBitsAlignmentAbsolute
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
            new Column("Flags", typeof(byte), 0),
            new Column("NextByte", typeof(byte), 1),
            new Column("Signature", typeof(int), 2)
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
                                    var _interpreter_PositionedFlags = new Musoq.Generated.Interpreters.PositionedFlags();
                                    var pRows = _interpreter_PositionedFlags.Interpret(f.Content);
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        yield return new ResultRow0(p.Flags, p.NextByte, p.Signature);
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
                                    var _interpreter_PositionedFlags = new Musoq.Generated.Interpreters.PositionedFlags();
                                    var pRows = _interpreter_PositionedFlags.Interpret(f.Content);
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        yield return new ResultRow0(p.Flags, p.NextByte, p.Signature);
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
                            var _interpreter_PositionedFlags = new Musoq.Generated.Interpreters.PositionedFlags();
                            var pRows = _interpreter_PositionedFlags.Interpret(f.Content);
                            if (pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = pRows;
                                yield return new ResultRow0(p.Flags, p.NextByte, p.Signature);
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
            public ResultRow0(byte __value0, byte __value1, int __value2)
            {
                Flags = __value0;
                NextByte = __value1;
                Signature = __value2;
            }

            public override int Count => 3;
            public byte Flags { get; private set; }
            public byte NextByte { get; private set; }
            public int Signature { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Flags = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.NextByte = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Signature = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Flags),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.NextByte),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Signature),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Flags" => true,
                "NextByte" => true,
                "Signature" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Flags, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.NextByte, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Signature, ref visitor);
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
    /// Generated interpreter for binary schema 'PositionedFlags'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class PositionedFlags : BytesInterpreterBase<PositionedFlags>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(PositionedFlags value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Flags"))
            {
                writer.WritePropertyName("Flags");
                var fieldValue = this.Flags;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("NextByte"))
            {
                writer.WritePropertyName("NextByte");
                var fieldValue = this.NextByte;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Signature"))
            {
                writer.WritePropertyName("Signature");
                var fieldValue = this.Signature;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Flags field value.</summary>
        public byte Flags { get; set; }
        /// <summary>Gets the NextByte field value.</summary>
        public byte NextByte { get; set; }
        /// <summary>Gets the Signature field value.</summary>
        public int Signature { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "PositionedFlags";

        /// <inheritdoc/>
        public override PositionedFlags InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new PositionedFlags();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Flags");
            var _flags = (byte)ReadBits(data, 3);
            __parsedValue.Flags = _flags;
            RecordParsedField("Flags");
            SetCurrentField("_");
            AlignToBits(data, 8);
            SetCurrentField("NextByte");
            var _nextByte = ReadByte(data);
            __parsedValue.NextByte = _nextByte;
            RecordParsedField("NextByte");
            SetCurrentField("Signature");
            SeekTo(0);
            var _signature = ReadInt32Le(data);
            __parsedValue.Signature = _signature;
            RecordParsedField("Signature");
            return __parsedValue;
        }
    }
}
