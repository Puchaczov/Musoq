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
  Project [p.ErrorField as p.ErrorField, p.ErrorMessage as p.ErrorMessage, p.BytesConsumed as p.BytesConsumed]
    CteRef [fp as fp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, p.ErrorField as p.ErrorField, p.ErrorMessage as p.ErrorMessage, p.BytesConsumed as p.BytesConsumed]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#DebugPacket(f.Content) as p]
  PhysicalProject [p.ErrorField as p.ErrorField, p.ErrorMessage as p.ErrorMessage, p.BytesConsumed as p.BytesConsumed]
    PhysicalCteRef [fp as fp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [p: object]
      ParsedFields: Dictionary<string, object> <- property ParsedFields
      ErrorField: string <- property ErrorField
      ErrorMessage: string <- property ErrorMessage
      BytesConsumed: int <- property BytesConsumed
    Generated [ResultRow0]
      p.ErrorField: string <- field p_ErrorField
      p.ErrorMessage: string <- field p_ErrorMessage
      p.BytesConsumed: int <- field p_BytesConsumed

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [DebugPacket.PartialInterpret(f.Content) -> pRows]
      ChunkedForEach [p in pRows]
        AppendShape [result <- ResultShape0(p.ErrorField: p.ErrorField, p.ErrorMessage: p.ErrorMessage, p.BytesConsumed: p.BytesConsumed)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("p.ErrorField", typeof(string), 0),
            new Column("p.ErrorMessage", typeof(string), 1),
            new Column("p.BytesConsumed", typeof(int), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_f_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Content", typeof(byte[]), 1) });
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
            var __musoqMaterializedTable = QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
            _ = __musoqMaterializedTable.Count;
            return __musoqMaterializedTable;
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.p_ErrorField, __musoqShapeRow.p_ErrorMessage, __musoqShapeRow.p_BytesConsumed);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    var __fSchema = provider.GetSchema("#test");
                    var fRowsSource = __fSchema.GetRowSource<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>("files", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), Array.Empty<object>());
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
                                            yield return new ResultShape0(p.ErrorField, p.ErrorMessage, p.BytesConsumed);
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
                                            yield return new ResultShape0(p.ErrorField, p.ErrorMessage, p.BytesConsumed);
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
                                    yield return new ResultShape0(p.ErrorField, p.ErrorMessage, p.BytesConsumed);
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
                p_ErrorField = __value0;
                p_ErrorMessage = __value1;
                p_BytesConsumed = __value2;
            }

            public override int Count => 3;
            public int p_BytesConsumed { get; private set; }
            public string p_ErrorField { get; private set; }
            public string p_ErrorMessage { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        p_ErrorField = (string)value;
                        break;
                    case 1:
                        p_ErrorMessage = (string)value;
                        break;
                    case 2:
                        p_BytesConsumed = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "p.ErrorField" => true,
                "p_ErrorField" => true,
                "ErrorField" => true,
                "p.ErrorMessage" => true,
                "p_ErrorMessage" => true,
                "ErrorMessage" => true,
                "p.BytesConsumed" => true,
                "p_BytesConsumed" => true,
                "BytesConsumed" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)p_ErrorField,
                1 => (object)p_ErrorMessage,
                2 => (object)p_BytesConsumed,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "p.ErrorField" => (object)p_ErrorField,
                "p_ErrorField" => (object)p_ErrorField,
                "ErrorField" => (object)p_ErrorField,
                "p.ErrorMessage" => (object)p_ErrorMessage,
                "p_ErrorMessage" => (object)p_ErrorMessage,
                "ErrorMessage" => (object)p_ErrorMessage,
                "p.BytesConsumed" => (object)p_BytesConsumed,
                "p_BytesConsumed" => (object)p_BytesConsumed,
                "BytesConsumed" => (object)p_BytesConsumed,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string p_ErrorField, string p_ErrorMessage, int p_BytesConsumed)
            {
                this.p_ErrorField = p_ErrorField;
                this.p_ErrorMessage = p_ErrorMessage;
                this.p_BytesConsumed = p_BytesConsumed;
            }

            public int p_BytesConsumed { get; }
            public string p_ErrorField { get; }
            public string p_ErrorMessage { get; }
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
    public sealed class DebugPacket : BytesInterpreterBase<DebugPacket>
    {
        /// <summary>Gets the Magic field value.</summary>
        public int Magic { get; init; }
        /// <summary>Gets the Version field value.</summary>
        public byte Version { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "DebugPacket";

        /// <inheritdoc/>
        public override DebugPacket InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("Magic");
            var _magic = ReadInt32Le(data);
            RecordParsedField("Magic", _magic);
            SetCurrentField("Version");
            var _version = ReadByte(data);
            RecordParsedField("Version", _version);
            return new DebugPacket
            {
                Magic = _magic,
                Version = _version
            };
        }
    }
}
