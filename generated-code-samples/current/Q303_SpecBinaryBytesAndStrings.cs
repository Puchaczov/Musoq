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
  Project [r.FixedBytes[0] as r.FixedBytes[0], r.DynamicBytes[0] as r.DynamicBytes[0], r.AsciiValue as r.AsciiValue, r.Utf8Value as r.Utf8Value, r.Utf16Value as r.Utf16Value, r.LatinValue as r.LatinValue, r.NullTermValue as r.NullTermValue]
    CteRef [fr as fr]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, r.FixedBytes as r.FixedBytes, r.DynamicBytes as r.DynamicBytes, r.AsciiValue as r.AsciiValue, r.Utf8Value as r.Utf8Value, r.Utf16Value as r.Utf16Value, r.LatinValue as r.LatinValue, r.NullTermValue as r.NullTermValue]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#EncodedRecord(f.Content) as r]
  PhysicalProject [r.FixedBytes[0] as r.FixedBytes[0], r.DynamicBytes[0] as r.DynamicBytes[0], r.AsciiValue as r.AsciiValue, r.Utf8Value as r.Utf8Value, r.Utf16Value as r.Utf16Value, r.LatinValue as r.LatinValue, r.NullTermValue as r.NullTermValue]
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
      r.AsciiValue: string <- field r_AsciiValue
      r.Utf8Value: string <- field r_Utf8Value
      r.Utf16Value: string <- field r_Utf16Value
      r.LatinValue: string <- field r_LatinValue
      r.NullTermValue: string <- field r_NullTermValue

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [EncodedRecord.Interpret(f.Content) -> rRows]
      ScalarForEach [r in rRows]
        AppendShape [result <- ResultShape0(r.FixedBytes[0]: r.FixedBytes[0], r.DynamicBytes[0]: r.DynamicBytes[0], r.AsciiValue: r.AsciiValue, r.Utf8Value: r.Utf8Value, r.Utf16Value: r.Utf16Value, r.LatinValue: r.LatinValue, r.NullTermValue: r.NullTermValue)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("r.FixedBytes[0]", typeof(byte), 0),
            new Column("r.DynamicBytes[0]", typeof(byte), 1),
            new Column("r.AsciiValue", typeof(string), 2),
            new Column("r.Utf8Value", typeof(string), 3),
            new Column("r.Utf16Value", typeof(string), 4),
            new Column("r.LatinValue", typeof(string), 5),
            new Column("r.NullTermValue", typeof(string), 6)
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
                yield return new ResultRow0(__musoqShapeRow.r_FixedBytes_0_, __musoqShapeRow.r_DynamicBytes_0_, __musoqShapeRow.r_AsciiValue, __musoqShapeRow.r_Utf8Value, __musoqShapeRow.r_Utf16Value, __musoqShapeRow.r_LatinValue, __musoqShapeRow.r_NullTermValue);
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
                                    var _interpreter_EncodedRecord = new Musoq.Generated.Interpreters.EncodedRecord();
                                    var rRows = _interpreter_EncodedRecord.Interpret(f.Content);
                                    if (rRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var r = rRows;
                                        yield return new ResultShape0((byte)SafeArrayAccess.GetIndexedElement(r.FixedBytes, 0, typeof(byte)), (byte)SafeArrayAccess.GetIndexedElement(r.DynamicBytes, 0, typeof(byte)), r.AsciiValue, r.Utf8Value, r.Utf16Value, r.LatinValue, r.NullTermValue);
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
                                        yield return new ResultShape0((byte)SafeArrayAccess.GetIndexedElement(r.FixedBytes, 0, typeof(byte)), (byte)SafeArrayAccess.GetIndexedElement(r.DynamicBytes, 0, typeof(byte)), r.AsciiValue, r.Utf8Value, r.Utf16Value, r.LatinValue, r.NullTermValue);
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
                                yield return new ResultShape0((byte)SafeArrayAccess.GetIndexedElement(r.FixedBytes, 0, typeof(byte)), (byte)SafeArrayAccess.GetIndexedElement(r.DynamicBytes, 0, typeof(byte)), r.AsciiValue, r.Utf8Value, r.Utf16Value, r.LatinValue, r.NullTermValue);
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
                r_AsciiValue = __value2;
                r_Utf8Value = __value3;
                r_Utf16Value = __value4;
                r_LatinValue = __value5;
                r_NullTermValue = __value6;
            }

            public override int Count => 7;
            public string r_AsciiValue { get; private set; }
            public byte r_DynamicBytes_0_ { get; private set; }
            public byte r_FixedBytes_0_ { get; private set; }
            public string r_LatinValue { get; private set; }
            public string r_NullTermValue { get; private set; }
            public string r_Utf16Value { get; private set; }
            public string r_Utf8Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        r_FixedBytes_0_ = (byte)value;
                        break;
                    case 1:
                        r_DynamicBytes_0_ = (byte)value;
                        break;
                    case 2:
                        r_AsciiValue = (string)value;
                        break;
                    case 3:
                        r_Utf8Value = (string)value;
                        break;
                    case 4:
                        r_Utf16Value = (string)value;
                        break;
                    case 5:
                        r_LatinValue = (string)value;
                        break;
                    case 6:
                        r_NullTermValue = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "r.FixedBytes[0]" => true,
                "r_FixedBytes_0_" => true,
                "FixedBytes[0]" => true,
                "r.DynamicBytes[0]" => true,
                "r_DynamicBytes_0_" => true,
                "DynamicBytes[0]" => true,
                "r.AsciiValue" => true,
                "r_AsciiValue" => true,
                "AsciiValue" => true,
                "r.Utf8Value" => true,
                "r_Utf8Value" => true,
                "Utf8Value" => true,
                "r.Utf16Value" => true,
                "r_Utf16Value" => true,
                "Utf16Value" => true,
                "r.LatinValue" => true,
                "r_LatinValue" => true,
                "LatinValue" => true,
                "r.NullTermValue" => true,
                "r_NullTermValue" => true,
                "NullTermValue" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)r_FixedBytes_0_,
                1 => (object)r_DynamicBytes_0_,
                2 => (object)r_AsciiValue,
                3 => (object)r_Utf8Value,
                4 => (object)r_Utf16Value,
                5 => (object)r_LatinValue,
                6 => (object)r_NullTermValue,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "r.FixedBytes[0]" => (object)r_FixedBytes_0_,
                "r_FixedBytes_0_" => (object)r_FixedBytes_0_,
                "FixedBytes[0]" => (object)r_FixedBytes_0_,
                "r.DynamicBytes[0]" => (object)r_DynamicBytes_0_,
                "r_DynamicBytes_0_" => (object)r_DynamicBytes_0_,
                "DynamicBytes[0]" => (object)r_DynamicBytes_0_,
                "r.AsciiValue" => (object)r_AsciiValue,
                "r_AsciiValue" => (object)r_AsciiValue,
                "AsciiValue" => (object)r_AsciiValue,
                "r.Utf8Value" => (object)r_Utf8Value,
                "r_Utf8Value" => (object)r_Utf8Value,
                "Utf8Value" => (object)r_Utf8Value,
                "r.Utf16Value" => (object)r_Utf16Value,
                "r_Utf16Value" => (object)r_Utf16Value,
                "Utf16Value" => (object)r_Utf16Value,
                "r.LatinValue" => (object)r_LatinValue,
                "r_LatinValue" => (object)r_LatinValue,
                "LatinValue" => (object)r_LatinValue,
                "r.NullTermValue" => (object)r_NullTermValue,
                "r_NullTermValue" => (object)r_NullTermValue,
                "NullTermValue" => (object)r_NullTermValue,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(byte r_FixedBytes_0_, byte r_DynamicBytes_0_, string r_AsciiValue, string r_Utf8Value, string r_Utf16Value, string r_LatinValue, string r_NullTermValue)
            {
                this.r_FixedBytes_0_ = r_FixedBytes_0_;
                this.r_DynamicBytes_0_ = r_DynamicBytes_0_;
                this.r_AsciiValue = r_AsciiValue;
                this.r_Utf8Value = r_Utf8Value;
                this.r_Utf16Value = r_Utf16Value;
                this.r_LatinValue = r_LatinValue;
                this.r_NullTermValue = r_NullTermValue;
            }

            public string r_AsciiValue { get; }
            public byte r_DynamicBytes_0_ { get; }
            public byte r_FixedBytes_0_ { get; }
            public string r_LatinValue { get; }
            public string r_NullTermValue { get; }
            public string r_Utf16Value { get; }
            public string r_Utf8Value { get; }
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
    public sealed class EncodedRecord : BytesInterpreterBase<EncodedRecord>
    {
        /// <summary>Gets the FixedBytes field value.</summary>
        public byte[] FixedBytes { get; init; }
        /// <summary>Gets the Length field value.</summary>
        public byte Length { get; init; }
        /// <summary>Gets the DynamicBytes field value.</summary>
        public byte[] DynamicBytes { get; init; }
        /// <summary>Gets the AsciiValue field value.</summary>
        public string AsciiValue { get; init; }
        /// <summary>Gets the Utf8Value field value.</summary>
        public string Utf8Value { get; init; }
        /// <summary>Gets the Utf16Value field value.</summary>
        public string Utf16Value { get; init; }
        /// <summary>Gets the LatinValue field value.</summary>
        public string LatinValue { get; init; }
        /// <summary>Gets the NullTermValue field value.</summary>
        public string NullTermValue { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "EncodedRecord";

        /// <inheritdoc/>
        public override EncodedRecord InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("FixedBytes");
            var _fixedBytes = ReadBytes(data, 4);
            RecordParsedField("FixedBytes", _fixedBytes);
            SetCurrentField("Length");
            var _length = ReadByte(data);
            RecordParsedField("Length", _length);
            SetCurrentField("DynamicBytes");
            var _dynamicBytes = ReadBytes(data, (int)_length);
            RecordParsedField("DynamicBytes", _dynamicBytes);
            SetCurrentField("AsciiValue");
            var _asciiValue = ReadString(data, 4, System.Text.Encoding.ASCII).Trim();
            RecordParsedField("AsciiValue", _asciiValue);
            SetCurrentField("Utf8Value");
            var _utf8Value = ReadString(data, 8, System.Text.Encoding.UTF8).TrimEnd();
            RecordParsedField("Utf8Value", _utf8Value);
            SetCurrentField("Utf16Value");
            var _utf16Value = ReadString(data, 8, System.Text.Encoding.Unicode).Trim();
            RecordParsedField("Utf16Value", _utf16Value);
            SetCurrentField("LatinValue");
            var _latinValue = ReadString(data, 4, System.Text.Encoding.Latin1).TrimStart();
            RecordParsedField("LatinValue", _latinValue);
            SetCurrentField("NullTermValue");
            var _nullTermValue = ReadNullTerminatedString(data, 8, System.Text.Encoding.ASCII);
            RecordParsedField("NullTermValue", _nullTermValue);
            return new EncodedRecord
            {
                FixedBytes = _fixedBytes,
                Length = _length,
                DynamicBytes = _dynamicBytes,
                AsciiValue = _asciiValue,
                Utf8Value = _utf8Value,
                Utf16Value = _utf16Value,
                LatinValue = _latinValue,
                NullTermValue = _nullTermValue
            };
        }
    }
}
