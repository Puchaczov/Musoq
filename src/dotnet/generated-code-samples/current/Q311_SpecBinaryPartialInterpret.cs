// === Parsed Query ===
/*
binary DebugPacket { Magic: int le, Version: byte };
                    select p.ErrorField, p.ErrorMessage, p.BytesConsumed
                    from #test.files() f
                    cross apply PartialInterpret<DebugPacket>(f.Content) p
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, p.ErrorField as p.ErrorField, p.ErrorMessage as p.ErrorMessage, p.BytesConsumed as p.BytesConsumed]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#DebugPacket(f.Content) as p]
  Project [p.ErrorField as ErrorField, p.ErrorMessage as ErrorMessage, p.BytesConsumed as BytesConsumed]
    CteRef [fp as fp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, p.ErrorField as p.ErrorField, p.ErrorMessage as p.ErrorMessage, p.BytesConsumed as p.BytesConsumed]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#DebugPacket(f.Content) as p]
  PhysicalProject [p.ErrorField as ErrorField, p.ErrorMessage as ErrorMessage, p.BytesConsumed as BytesConsumed]
    PhysicalCteRef [fp as fp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [p: object]
      ParsedFields: object <- property ParsedFields
      ErrorField: string <- property ErrorField
      ErrorMessage: string <- property ErrorMessage
      BytesConsumed: int <- property BytesConsumed
    Generated [ResultRow0]
      ErrorField: string <- field ErrorField
      ErrorMessage: string <- field ErrorMessage
      BytesConsumed: int <- field BytesConsumed

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [DebugPacket.PartialInterpret(f.Content) -> pRows]
      ChunkedForEach [p in pRows]
        AppendShape [result <- ResultRow0(ErrorField: p.ErrorField, ErrorMessage: p.ErrorMessage, BytesConsumed: p.BytesConsumed)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q311_SpecBinaryPartialInterpret
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
            new Column("ErrorField", typeof(string), 0),
            new Column("ErrorMessage", typeof(string), 1),
            new Column("BytesConsumed", typeof(int), 2)
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
                                    var _interpreter_DebugPacket = new Musoq.Generated.Interpreters.DebugPacket();
                                    var pRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Schema.Interpreters.PartialInterpretResult<Musoq.Generated.Interpreters.DebugPacket>>(EvaluationHelper.WrapScalarForCrossApply<Musoq.Schema.Interpreters.PartialInterpretResult<Musoq.Generated.Interpreters.DebugPacket>>(_interpreter_DebugPacket.PartialInterpret(f.Content)));
                                    foreach (var pChunk in pRows)
                                    {
                                        for (int pIndex = 0, pIndexCount = pChunk.Count; pIndex < pIndexCount; ++pIndex)
                                        {
                                            if ((pIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var p = pChunk[pIndex];
                                            yield return new ResultRow0(p.ErrorField, p.ErrorMessage, p.BytesConsumed);
                                        }
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
                                    var _interpreter_DebugPacket = new Musoq.Generated.Interpreters.DebugPacket();
                                    var pRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Schema.Interpreters.PartialInterpretResult<Musoq.Generated.Interpreters.DebugPacket>>(EvaluationHelper.WrapScalarForCrossApply<Musoq.Schema.Interpreters.PartialInterpretResult<Musoq.Generated.Interpreters.DebugPacket>>(_interpreter_DebugPacket.PartialInterpret(f.Content)));
                                    foreach (var pChunk in pRows)
                                    {
                                        for (int pIndex = 0, pIndexCount = pChunk.Count; pIndex < pIndexCount; ++pIndex)
                                        {
                                            if ((pIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var p = pChunk[pIndex];
                                            yield return new ResultRow0(p.ErrorField, p.ErrorMessage, p.BytesConsumed);
                                        }
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
                            var _interpreter_DebugPacket = new Musoq.Generated.Interpreters.DebugPacket();
                            var pRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Schema.Interpreters.PartialInterpretResult<Musoq.Generated.Interpreters.DebugPacket>>(EvaluationHelper.WrapScalarForCrossApply<Musoq.Schema.Interpreters.PartialInterpretResult<Musoq.Generated.Interpreters.DebugPacket>>(_interpreter_DebugPacket.PartialInterpret(f.Content)));
                            foreach (var pChunk in pRows)
                            {
                                for (int pIndex = 0, pIndexCount = pChunk.Count; pIndex < pIndexCount; ++pIndex)
                                {
                                    if ((pIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var p = pChunk[pIndex];
                                    yield return new ResultRow0(p.ErrorField, p.ErrorMessage, p.BytesConsumed);
                                }
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
            public ResultRow0(string __value0, string __value1, int __value2)
            {
                ErrorField = __value0;
                ErrorMessage = __value1;
                BytesConsumed = __value2;
            }

            public int BytesConsumed { get; private set; }
            public override int Count => 3;
            public string ErrorField { get; private set; }
            public string ErrorMessage { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.ErrorField = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.ErrorMessage = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.BytesConsumed = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.ErrorField),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.ErrorMessage),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.BytesConsumed),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "ErrorField" => true,
                "ErrorMessage" => true,
                "BytesConsumed" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.ErrorField, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.ErrorMessage, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.BytesConsumed, ref visitor);
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
    /// Generated interpreter for binary schema 'DebugPacket'.
    /// </summary>
    public sealed class DebugPacket : BytesInterpreterBase<DebugPacket>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(DebugPacket value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Magic"))
            {
                writer.WritePropertyName("Magic");
                var fieldValue = this.Magic;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Version"))
            {
                writer.WritePropertyName("Version");
                var fieldValue = this.Version;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Magic field value.</summary>
        public int Magic { get; set; }
        /// <summary>Gets the Version field value.</summary>
        public byte Version { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "DebugPacket";

        /// <inheritdoc/>
        public override DebugPacket InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new DebugPacket();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Magic");
            var _magic = ReadInt32Le(data);
            __parsedValue.Magic = _magic;
            RecordParsedField("Magic");
            SetCurrentField("Version");
            var _version = ReadByte(data);
            __parsedValue.Version = _version;
            RecordParsedField("Version");
            return __parsedValue;
        }
    }
}
