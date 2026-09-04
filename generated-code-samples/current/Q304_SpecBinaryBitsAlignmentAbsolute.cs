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
  Project [p.Flags as p.Flags, p.NextByte as p.NextByte, p.Signature as p.Signature]
    CteRef [fp as fp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, p.Flags as p.Flags, p.NextByte as p.NextByte, p.Signature as p.Signature]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#PositionedFlags(f.Content) as p]
  PhysicalProject [p.Flags as p.Flags, p.NextByte as p.NextByte, p.Signature as p.Signature]
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
      p.Flags: byte <- field p_Flags
      p.NextByte: byte <- field p_NextByte
      p.Signature: int <- field p_Signature

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [PositionedFlags.Interpret(f.Content) -> pRows]
      ScalarForEach [p in pRows]
        AppendShape [result <- ResultShape0(p.Flags: p.Flags, p.NextByte: p.NextByte, p.Signature: p.Signature)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("p.Flags", typeof(byte), 0),
            new Column("p.NextByte", typeof(byte), 1),
            new Column("p.Signature", typeof(int), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_f_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Content", typeof(byte[]), 0) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.p_Flags, __musoqShapeRow.p_NextByte, __musoqShapeRow.p_Signature);
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
                                    var _interpreter_PositionedFlags = new Musoq.Generated.Interpreters.PositionedFlags();
                                    var pRows = _interpreter_PositionedFlags.Interpret(f.Content);
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        yield return new ResultShape0(p.Flags, p.NextByte, p.Signature);
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
                                        yield return new ResultShape0(p.Flags, p.NextByte, p.Signature);
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
                                yield return new ResultShape0(p.Flags, p.NextByte, p.Signature);
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
                p_Flags = __value0;
                p_NextByte = __value1;
                p_Signature = __value2;
            }

            public override int Count => 3;
            public byte p_Flags { get; private set; }
            public byte p_NextByte { get; private set; }
            public int p_Signature { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        p_Flags = (byte)value;
                        break;
                    case 1:
                        p_NextByte = (byte)value;
                        break;
                    case 2:
                        p_Signature = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "p.Flags" => true,
                "p_Flags" => true,
                "Flags" => true,
                "p.NextByte" => true,
                "p_NextByte" => true,
                "NextByte" => true,
                "p.Signature" => true,
                "p_Signature" => true,
                "Signature" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)p_Flags,
                1 => (object)p_NextByte,
                2 => (object)p_Signature,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "p.Flags" => (object)p_Flags,
                "p_Flags" => (object)p_Flags,
                "Flags" => (object)p_Flags,
                "p.NextByte" => (object)p_NextByte,
                "p_NextByte" => (object)p_NextByte,
                "NextByte" => (object)p_NextByte,
                "p.Signature" => (object)p_Signature,
                "p_Signature" => (object)p_Signature,
                "Signature" => (object)p_Signature,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(byte p_Flags, byte p_NextByte, int p_Signature)
            {
                this.p_Flags = p_Flags;
                this.p_NextByte = p_NextByte;
                this.p_Signature = p_Signature;
            }

            public byte p_Flags { get; }
            public byte p_NextByte { get; }
            public int p_Signature { get; }
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
    public sealed class PositionedFlags : BytesInterpreterBase<PositionedFlags>
    {
        /// <summary>Gets the Flags field value.</summary>
        public byte Flags { get; init; }
        /// <summary>Gets the NextByte field value.</summary>
        public byte NextByte { get; init; }
        /// <summary>Gets the Signature field value.</summary>
        public int Signature { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "PositionedFlags";

        /// <inheritdoc/>
        public override PositionedFlags InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("Flags");
            var _flags = (byte)ReadBits(data, 3);
            RecordParsedField("Flags", _flags);
            SetCurrentField("_");
            AlignToBits(data, 8);
            SetCurrentField("NextByte");
            var _nextByte = ReadByte(data);
            RecordParsedField("NextByte", _nextByte);
            SetCurrentField("Signature");
            SeekTo((int)(0));
            var _signature = ReadInt32Le(data);
            RecordParsedField("Signature", _signature);
            return new PositionedFlags
            {
                Flags = _flags,
                NextByte = _nextByte,
                Signature = _signature
            };
        }
    }
}
