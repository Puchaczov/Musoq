// === Parsed Query ===
/*
binary ValidatedRecord {
                        Version: byte const 1,
                        Signature: byte[4] magic [0xCA, 0xFE, 0xBA, 0xBE],
                        Kind: string[4] ascii oneOf ['DATA', 'TEST']
                    };
                    select r.Version, r.Signature, r.Kind
                    from #test.files() f
                    cross apply Interpret<ValidatedRecord>(f.Content) r
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, r.Version as r.Version, r.Signature as r.Signature, r.Kind as r.Kind]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#ValidatedRecord(f.Content) as r]
  Project [r.Version as r.Version, r.Signature as r.Signature, r.Kind as r.Kind]
    CteRef [fr as fr]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, r.Version as r.Version, r.Signature as r.Signature, r.Kind as r.Kind]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#ValidatedRecord(f.Content) as r]
  PhysicalProject [r.Version as r.Version, r.Signature as r.Signature, r.Kind as r.Kind]
    PhysicalCteRef [fr as fr]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [r: object]
      Version: byte <- property Version
      Signature: byte[] <- property Signature
      Kind: string <- property Kind
    Generated [ResultRow0]
      r.Version: byte <- field r_Version
      r.Signature: byte[] <- field r_Signature
      r.Kind: string <- field r_Kind

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [ValidatedRecord.Interpret(f.Content) -> rRows]
      ScalarForEach [r in rRows]
        AppendShape [result <- ResultShape0(r.Version: r.Version, r.Signature: r.Signature, r.Kind: r.Kind)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q305_SpecBinaryChecksConstMagicOneOf
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
            new Column("r.Version", typeof(byte), 0),
            new Column("r.Signature", typeof(byte[]), 1),
            new Column("r.Kind", typeof(string), 2)
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
                yield return new ResultRow0(__musoqShapeRow.r_Version, __musoqShapeRow.r_Signature, __musoqShapeRow.r_Kind);
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
                                    var _interpreter_ValidatedRecord = new Musoq.Generated.Interpreters.ValidatedRecord();
                                    var rRows = _interpreter_ValidatedRecord.Interpret(f.Content);
                                    if (rRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var r = rRows;
                                        yield return new ResultShape0(r.Version, r.Signature, r.Kind);
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
                                    var _interpreter_ValidatedRecord = new Musoq.Generated.Interpreters.ValidatedRecord();
                                    var rRows = _interpreter_ValidatedRecord.Interpret(f.Content);
                                    if (rRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var r = rRows;
                                        yield return new ResultShape0(r.Version, r.Signature, r.Kind);
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
                            var _interpreter_ValidatedRecord = new Musoq.Generated.Interpreters.ValidatedRecord();
                            var rRows = _interpreter_ValidatedRecord.Interpret(f.Content);
                            if (rRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var r = rRows;
                                yield return new ResultShape0(r.Version, r.Signature, r.Kind);
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
            public ResultRow0(byte __value0, byte[] __value1, string __value2)
            {
                r_Version = __value0;
                r_Signature = __value1;
                r_Kind = __value2;
            }

            public override int Count => 3;
            public string r_Kind { get; private set; }
            public byte[] r_Signature { get; private set; }
            public byte r_Version { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        r_Version = (byte)value;
                        break;
                    case 1:
                        r_Signature = (byte[])value;
                        break;
                    case 2:
                        r_Kind = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "r.Version" => true,
                "r_Version" => true,
                "Version" => true,
                "r.Signature" => true,
                "r_Signature" => true,
                "Signature" => true,
                "r.Kind" => true,
                "r_Kind" => true,
                "Kind" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)r_Version,
                1 => (object)r_Signature,
                2 => (object)r_Kind,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "r.Version" => (object)r_Version,
                "r_Version" => (object)r_Version,
                "Version" => (object)r_Version,
                "r.Signature" => (object)r_Signature,
                "r_Signature" => (object)r_Signature,
                "Signature" => (object)r_Signature,
                "r.Kind" => (object)r_Kind,
                "r_Kind" => (object)r_Kind,
                "Kind" => (object)r_Kind,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(byte r_Version, byte[] r_Signature, string r_Kind)
            {
                this.r_Version = r_Version;
                this.r_Signature = r_Signature;
                this.r_Kind = r_Kind;
            }

            public string r_Kind { get; }
            public byte[] r_Signature { get; }
            public byte r_Version { get; }
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
    /// Generated interpreter for binary schema 'ValidatedRecord'.
    /// </summary>
    public sealed class ValidatedRecord : BytesInterpreterBase<ValidatedRecord>
    {
        /// <summary>Gets the Version field value.</summary>
        public byte Version { get; init; }
        /// <summary>Gets the Signature field value.</summary>
        public byte[] Signature { get; init; }
        /// <summary>Gets the Kind field value.</summary>
        public string Kind { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "ValidatedRecord";

        /// <inheritdoc/>
        public override ValidatedRecord InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("Version");
            var _version = ReadByte(data);
            Validate(_version == unchecked((byte)(1)), "Version", "Field 'Version' did not match the expected constant value.");
            RecordParsedField("Version", _version);
            SetCurrentField("Signature");
            var _signature = ReadBytes(data, 4);
            Validate(BytesEqual(_signature, new byte[] { 202, 254, 186, 190 }), "Signature", "Field 'Signature' did not match the expected magic value.");
            RecordParsedField("Signature", _signature);
            SetCurrentField("Kind");
            var _kind = ReadString(data, 4, System.Text.Encoding.ASCII);
            Validate((_kind == "DATA" || _kind == "TEST"), "Kind", "Field 'Kind' value is not one of the allowed values.");
            RecordParsedField("Kind", _kind);
            return new ValidatedRecord
            {
                Version = _version,
                Signature = _signature,
                Kind = _kind
            };
        }
    }
}
