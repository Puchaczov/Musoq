/*
raw query string

binary TextPacket {
                Length: byte,
                Text: string[Length] ascii trim
            };
            select
                p.Length,
                p.Text
            from #test.files() f
            cross apply Interpret<TextPacket>(f.Content) p
*/

/*
logical plan representation string

MultiStatement
  Project [f.Content as f.Content, p.Length as p.Length, p.Text as p.Text]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#TextPacket(f.Content) as p]
  Project [p.Length as p.Length, p.Text as p.Text]
    CteRef [fp as fp]
*/

/*
physical plan representation string

PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, p.Length as p.Length, p.Text as p.Text]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#TextPacket(f.Content) as p]
  PhysicalProject [p.Length as p.Length, p.Text as p.Text]
    PhysicalCteRef [fp as fp]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [p: object]
      Length: byte <- property Length
      Text: string <- property Text
    Generated [ResultRow0]
      p.Length: byte <- field p_Length
      p.Text: string <- field p_Text

  Body
    CtePhase [cte0]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [TextPacket.Interpret(f.Content) -> pRows]
      ScalarForEach [p in pRows]
        AppendShape [result <- ResultShape0(p.Length: p.Length, p.Text: p.Text)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q52_BinaryStringInterpret
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("p.Length", typeof(byte), 0),
            new Column("p.Text", typeof(string), 1)
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
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.p_Length, __musoqShapeRow.p_Text);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                var __fSchema = provider.GetSchema("#test");
                var fRowsSource = __fSchema.GetRowSource<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>("files", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var fRows = fRowsSource.Chunks;
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
                                var _interpreter_TextPacket = new Musoq.Generated.Interpreters.TextPacket();
                                var pRows = _interpreter_TextPacket.Interpret(f.Content);
                                if (pRows != null)
                                {
                                    token.ThrowIfCancellationRequested();
                                    var p = pRows;
                                    __musoqFinalShapeRows.Add(new ResultShape0(p.Length, p.Text));
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
                                var _interpreter_TextPacket = new Musoq.Generated.Interpreters.TextPacket();
                                var pRows = _interpreter_TextPacket.Interpret(f.Content);
                                if (pRows != null)
                                {
                                    token.ThrowIfCancellationRequested();
                                    var p = pRows;
                                    __musoqFinalShapeRows.Add(new ResultShape0(p.Length, p.Text));
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
                        var _interpreter_TextPacket = new Musoq.Generated.Interpreters.TextPacket();
                        var pRows = _interpreter_TextPacket.Interpret(f.Content);
                        if (pRows != null)
                        {
                            token.ThrowIfCancellationRequested();
                            var p = pRows;
                            __musoqFinalShapeRows.Add(new ResultShape0(p.Length, p.Text));
                        }
                    }
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
                OnPhaseChanged("compiled", QueryPhase.End);
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
            public ResultRow0(byte __value0, string __value1)
            {
                p_Length = __value0;
                p_Text = __value1;
            }

            public override int Count => 2;
            public byte p_Length { get; private set; }
            public string p_Text { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        p_Length = (byte)value;
                        break;
                    case 1:
                        p_Text = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "p.Length" => true,
                "p_Length" => true,
                "Length" => true,
                "p.Text" => true,
                "p_Text" => true,
                "Text" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)p_Length,
                1 => (object)p_Text,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "p.Length" => (object)p_Length,
                "p_Length" => (object)p_Length,
                "Length" => (object)p_Length,
                "p.Text" => (object)p_Text,
                "p_Text" => (object)p_Text,
                "Text" => (object)p_Text,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(byte p_Length, string p_Text)
            {
                this.p_Length = p_Length;
                this.p_Text = p_Text;
            }

            public byte p_Length { get; }
            public string p_Text { get; }
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
    /// Generated interpreter for binary schema 'TextPacket'.
    /// </summary>
    public sealed class TextPacket : BytesInterpreterBase<TextPacket>
    {
        /// <summary>Gets the Length field value.</summary>
        public byte Length { get; init; }
        /// <summary>Gets the Text field value.</summary>
        public string Text { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "TextPacket";

        /// <inheritdoc/>
        public override TextPacket InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            ParsePosition = offset;
            BitOffset = 0;
            var _length = ReadByte(data);
            var _text = ReadString(data, (int)_length, System.Text.Encoding.ASCII).Trim();
            return new TextPacket
            {
                Length = _length,
                Text = _text
            };
        }
    }
}
