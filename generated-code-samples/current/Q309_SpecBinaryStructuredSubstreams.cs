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
      LaxLength: byte <- property LaxLength
      Lax: object <- property Lax
      InlineLength: byte <- property InlineLength
      Inline: object <- property Inline
    Generated [ResultRow0]
      f.Exact.A: object <- field f_Exact_A
      f.Exact.B: object <- field f_Exact_B
      f.Lax.A: object <- field f_Lax_A
      f.Lax.B: object <- field f_Lax_B
      f.Inline.A: object <- field f_Inline_A
      f.Inline.B: object <- field f_Inline_B

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [src: BinaryEntity] -> srcRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [src in srcRows]
      InterpretSource [Frame.Interpret(src.Content) -> fRows]
      ScalarForEach [f in fRows]
        AppendShape [result <- ResultShape0(f.Exact.A: f.Exact.A, f.Exact.B: f.Exact.B, f.Lax.A: f.Lax.A, f.Lax.B: f.Lax.B, f.Inline.A: f.Inline.A, f.Inline.B: f.Inline.B)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("f.Exact.A", typeof(object), 0),
            new Column("f.Exact.B", typeof(object), 1),
            new Column("f.Lax.A", typeof(object), 2),
            new Column("f.Lax.B", typeof(object), 3),
            new Column("f.Inline.A", typeof(object), 4),
            new Column("f.Inline.B", typeof(object), 5)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_src_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Content", typeof(byte[]), 0) });
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
                yield return new ResultRow0(__musoqShapeRow.f_Exact_A, __musoqShapeRow.f_Exact_B, __musoqShapeRow.f_Lax_A, __musoqShapeRow.f_Lax_B, __musoqShapeRow.f_Inline_A, __musoqShapeRow.f_Inline_B);
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
                    var __srcSchema = provider.GetSchema("#test");
                    var srcRowsSource = __srcSchema.GetRowSource<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>("files", new SourceExecutionContext("src:1", sourceExecutionPlans["src:1"], token, __schemaColumns_compiled_src_0, sourceRuntimeSettingsBySourceContextId["src:1"], logger, OnDataSourceProgress), Array.Empty<object>());
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
                                        yield return new ResultShape0(((Musoq.Generated.Interpreters.Body)f.Exact).A, ((Musoq.Generated.Interpreters.Body)f.Exact).B, ((Musoq.Generated.Interpreters.Body)f.Lax).A, ((Musoq.Generated.Interpreters.Body)f.Lax).B, ((Musoq.Generated.Interpreters.Inline_Inline)f.Inline).A, ((Musoq.Generated.Interpreters.Inline_Inline)f.Inline).B);
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
                                        yield return new ResultShape0(((Musoq.Generated.Interpreters.Body)f.Exact).A, ((Musoq.Generated.Interpreters.Body)f.Exact).B, ((Musoq.Generated.Interpreters.Body)f.Lax).A, ((Musoq.Generated.Interpreters.Body)f.Lax).B, ((Musoq.Generated.Interpreters.Inline_Inline)f.Inline).A, ((Musoq.Generated.Interpreters.Inline_Inline)f.Inline).B);
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
                                yield return new ResultShape0(((Musoq.Generated.Interpreters.Body)f.Exact).A, ((Musoq.Generated.Interpreters.Body)f.Exact).B, ((Musoq.Generated.Interpreters.Body)f.Lax).A, ((Musoq.Generated.Interpreters.Body)f.Lax).B, ((Musoq.Generated.Interpreters.Inline_Inline)f.Inline).A, ((Musoq.Generated.Interpreters.Inline_Inline)f.Inline).B);
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
            public ResultRow0(object __value0, object __value1, object __value2, object __value3, object __value4, object __value5)
            {
                f_Exact_A = __value0;
                f_Exact_B = __value1;
                f_Lax_A = __value2;
                f_Lax_B = __value3;
                f_Inline_A = __value4;
                f_Inline_B = __value5;
            }

            public override int Count => 6;
            public object f_Exact_A { get; private set; }
            public object f_Exact_B { get; private set; }
            public object f_Inline_A { get; private set; }
            public object f_Inline_B { get; private set; }
            public object f_Lax_A { get; private set; }
            public object f_Lax_B { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        f_Exact_A = value;
                        break;
                    case 1:
                        f_Exact_B = value;
                        break;
                    case 2:
                        f_Lax_A = value;
                        break;
                    case 3:
                        f_Lax_B = value;
                        break;
                    case 4:
                        f_Inline_A = value;
                        break;
                    case 5:
                        f_Inline_B = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

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
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)f_Exact_A,
                1 => (object)f_Exact_B,
                2 => (object)f_Lax_A,
                3 => (object)f_Lax_B,
                4 => (object)f_Inline_A,
                5 => (object)f_Inline_B,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "f.Exact.A" => (object)f_Exact_A,
                "f_Exact_A" => (object)f_Exact_A,
                "f.Exact.B" => (object)f_Exact_B,
                "f_Exact_B" => (object)f_Exact_B,
                "f.Lax.A" => (object)f_Lax_A,
                "f_Lax_A" => (object)f_Lax_A,
                "f.Lax.B" => (object)f_Lax_B,
                "f_Lax_B" => (object)f_Lax_B,
                "f.Inline.A" => (object)f_Inline_A,
                "f_Inline_A" => (object)f_Inline_A,
                "f.Inline.B" => (object)f_Inline_B,
                "f_Inline_B" => (object)f_Inline_B,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(object f_Exact_A, object f_Exact_B, object f_Lax_A, object f_Lax_B, object f_Inline_A, object f_Inline_B)
            {
                this.f_Exact_A = f_Exact_A;
                this.f_Exact_B = f_Exact_B;
                this.f_Lax_A = f_Lax_A;
                this.f_Lax_B = f_Lax_B;
                this.f_Inline_A = f_Inline_A;
                this.f_Inline_B = f_Inline_B;
            }

            public object f_Exact_A { get; }
            public object f_Exact_B { get; }
            public object f_Inline_A { get; }
            public object f_Inline_B { get; }
            public object f_Lax_A { get; }
            public object f_Lax_B { get; }
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
    public sealed class Body : BytesInterpreterBase<Body>
    {
        /// <summary>Gets the A field value.</summary>
        public byte A { get; init; }
        /// <summary>Gets the B field value.</summary>
        public byte B { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Body";

        /// <inheritdoc/>
        public override Body InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("A");
            var _a = ReadByte(data);
            RecordParsedField("A", _a);
            SetCurrentField("B");
            var _b = ReadByte(data);
            RecordParsedField("B", _b);
            return new Body
            {
                A = _a,
                B = _b
            };
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'Frame'.
    /// </summary>
    public sealed class Frame : BytesInterpreterBase<Frame>
    {
        /// <summary>Gets the ExactLength field value.</summary>
        public byte ExactLength { get; init; }
        /// <summary>Gets the Exact field value.</summary>
        public Body Exact { get; init; }
        /// <summary>Gets the LaxLength field value.</summary>
        public byte LaxLength { get; init; }
        /// <summary>Gets the Lax field value.</summary>
        public Body Lax { get; init; }
        /// <summary>Gets the InlineLength field value.</summary>
        public byte InlineLength { get; init; }
        /// <summary>Gets the Inline field value.</summary>
        public Inline_Inline Inline { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Frame";

        /// <inheritdoc/>
        public override Frame InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("ExactLength");
            var _exactLength = ReadByte(data);
            RecordParsedField("ExactLength", _exactLength);
            SetCurrentField("Exact");
            var __exact_substreamLength = (int)_exactLength;
            var __exact_substreamSlice = ReadSubstreamSlice(data, __exact_substreamLength);
            var __exact_substreamInterpreter = new Body();
            var _exact = InterpretNestedAt(__exact_substreamInterpreter, __exact_substreamSlice, 0, "Exact");
            EnsureSubstreamFullyConsumed("Exact", __exact_substreamLength, __exact_substreamInterpreter.BytesConsumed);
            ParsePosition += __exact_substreamLength;
            RecordParsedField("Exact", _exact);
            SetCurrentField("LaxLength");
            var _laxLength = ReadByte(data);
            RecordParsedField("LaxLength", _laxLength);
            SetCurrentField("Lax");
            var __lax_substreamLength = (int)_laxLength;
            var __lax_substreamSlice = ReadSubstreamSlice(data, __lax_substreamLength);
            var __lax_substreamInterpreter = new Body();
            var _lax = InterpretNestedAt(__lax_substreamInterpreter, __lax_substreamSlice, 0, "Lax");
            ParsePosition += __lax_substreamLength;
            RecordParsedField("Lax", _lax);
            SetCurrentField("InlineLength");
            var _inlineLength = ReadByte(data);
            RecordParsedField("InlineLength", _inlineLength);
            SetCurrentField("Inline");
            var __inline_substreamLength = (int)_inlineLength;
            var __inline_substreamSlice = ReadSubstreamSlice(data, __inline_substreamLength);
            var __inline_substreamInterpreter = new Inline_Inline();
            var _inline = InterpretNestedAt(__inline_substreamInterpreter, __inline_substreamSlice, 0, "Inline");
            EnsureSubstreamFullyConsumed("Inline", __inline_substreamLength, __inline_substreamInterpreter.BytesConsumed);
            ParsePosition += __inline_substreamLength;
            RecordParsedField("Inline", _inline);
            return new Frame
            {
                ExactLength = _exactLength,
                Exact = _exact,
                LaxLength = _laxLength,
                Lax = _lax,
                InlineLength = _inlineLength,
                Inline = _inline
            };
        }
    }

    /// <summary>
    /// Generated nested interpreter for inline schema 'Inline_Inline'.
    /// </summary>
    public sealed class Inline_Inline : BytesInterpreterBase<Inline_Inline>
    {
        /// <summary>Gets the A field value.</summary>
        public byte A { get; init; }
        /// <summary>Gets the B field value.</summary>
        public byte B { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Inline_Inline";

        /// <inheritdoc/>
        public override Inline_Inline InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("A");
            var _a = ReadByte(data);
            RecordParsedField("A", _a);
            SetCurrentField("B");
            var _b = ReadByte(data);
            RecordParsedField("B", _b);
            return new Inline_Inline
            {
                A = _a,
                B = _b
            };
        }
    }
}
