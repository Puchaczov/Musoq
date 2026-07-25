// === Parsed Query ===
/*
binary InlineArrayPacket {
                Count: byte,
                Items: { Tag: byte, Value: short le }[Count]
            };
            select
                p.Count,
                it.Tag,
                it.Value
            from #test.files() f
            cross apply Interpret<InlineArrayPacket>(f.Content) p
            cross apply p.Items it
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, p.Count as p.Count, p.Items as p.Items]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#InlineArrayPacket(f.Content) as p]
  Project [fp.f.Content as f.Content, fp.p.Count as p.Count, fp.p.Items as p.Items, it.Tag as it.Tag, it.Value as it.Value]
    Apply [Cross]
      CteRef [fp as fp]
      PropertySource [fp.p.Items as it] [apply: Cross] [type: Object[]]
  Project [p.Count as p.Count, it.Tag as it.Tag, it.Value as it.Value]
    CteRef [fpit as fpit]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, p.Count as p.Count, p.Items as p.Items]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#InlineArrayPacket(f.Content) as p]
  PhysicalProject [fp.f.Content as f.Content, fp.p.Count as p.Count, fp.p.Items as p.Items, it.Tag as it.Tag, it.Value as it.Value]
    PhysicalNestedLoopApply [Cross]
      PhysicalCteRef [fp as fp]
      PhysicalPropertySource [fp.p.Items as it] [apply: Cross] [type: Object[]]
  PhysicalProject [p.Count as p.Count, it.Tag as it.Tag, it.Value as it.Value]
    PhysicalCteRef [fpit as fpit]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [p: object]
      Count: byte <- property Count
      Items: object[] <- property Items
    Generated [Statement0Row0]
      f.Content: byte[] <- field f_Content
      p.Count: byte <- field p_Count
      p.Items: object[] <- field p_Items
    TableRow [fp]
      f.Content: byte[] <- field f_Content
      p.Count: byte <- field p_Count
      p.Items: object[] <- field p_Items
    SourceEntity [it: object]
      Tag: byte <- property Tag
      Value: short <- property Value
    Generated [ResultRow0]
      p.Count: byte <- field p_Count
      it.Tag: byte <- field it_Tag
      it.Value: short <- field it_Value

  Body
    SourceScan [f: BinaryEntity] -> statement0_fRows
    CreateTable [statement0: Statement0Row0]
    ChunkedForEach [f in statement0_fRows]
      InterpretSource [InlineArrayPacket.Interpret(f.Content) -> statement0_pRows]
      ScalarForEach [p in statement0_pRows]
        AppendRow [statement0 <- Statement0Row0(f.Content: f.Content, p.Count: p.Count, p.Items: p.Items)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CtePhase [cte1]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [fp in _cteRowResults.Slot0]
      EnumerableSource [fp.p.Items -> itRows]
      ChunkedForEach [it in itRows]
        AppendShape [result <- ResultShape0(p.Count: fp.p.Count, it.Tag: it.Tag, it.Value: it.Value)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q55_BinaryInlineArrayInterpret
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("p.Count", typeof(byte), 0),
            new Column("it.Tag", typeof(byte), 1),
            new Column("it.Value", typeof(short), 2)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("f.Content", typeof(byte[]), 0),
            new Column("p.Count", typeof(byte), 1),
            new Column("p.Items", typeof(object[]), 2)
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.p_Count, __musoqShapeRow.it_Tag, __musoqShapeRow.it_Value);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 fp = __storedTable0Rows[__storedTable0Index];
                    var itRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Generated.Interpreters.Inline_Items>((Musoq.Generated.Interpreters.Inline_Items[])fp.p_Items);
                    foreach (var itChunk in itRows)
                    {
                        for (int itIndex = 0, itIndexCount = itChunk.Count; itIndex < itIndexCount; ++itIndex)
                        {
                            if ((itIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var it = itChunk[itIndex];
                            __musoqFinalShapeRows.Add(new ResultShape0(fp.p_Count, it.Tag, it.Value));
                        }
                    }
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, CteRowResults _cteRowResults)
        {
            var __statement0_fSchema = provider.GetSchema("#test");
            var statement0_fRowsSource = __statement0_fSchema.GetRowSource<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>("files", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var statement0_fRows = statement0_fRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            foreach (var fChunk in statement0_fRows)
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
                            var _interpreter_InlineArrayPacket = new Musoq.Generated.Interpreters.InlineArrayPacket();
                            var statement0_pRows = _interpreter_InlineArrayPacket.Interpret(f.Content);
                            if (statement0_pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = statement0_pRows;
                                statement0.Add(new Statement0Row0(f.Content, p.Count, p.Items));
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
                            var _interpreter_InlineArrayPacket = new Musoq.Generated.Interpreters.InlineArrayPacket();
                            var statement0_pRows = _interpreter_InlineArrayPacket.Interpret(f.Content);
                            if (statement0_pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = statement0_pRows;
                                statement0.Add(new Statement0Row0(f.Content, p.Count, p.Items));
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
                    var _interpreter_InlineArrayPacket = new Musoq.Generated.Interpreters.InlineArrayPacket();
                    var statement0_pRows = _interpreter_InlineArrayPacket.Interpret(f.Content);
                    if (statement0_pRows != null)
                    {
                        token.ThrowIfCancellationRequested();
                        var p = statement0_pRows;
                        statement0.Add(new Statement0Row0(f.Content, p.Count, p.Items));
                    }
                }
            }

            return statement0;
        }

        private sealed class CteRowResults
        {
            public List<Statement0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(byte __value0, byte __value1, short __value2)
            {
                p_Count = __value0;
                it_Tag = __value1;
                it_Value = __value2;
            }

            public override int Count => 3;
            public byte it_Tag { get; private set; }
            public short it_Value { get; private set; }
            public byte p_Count { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        p_Count = (byte)value;
                        break;
                    case 1:
                        it_Tag = (byte)value;
                        break;
                    case 2:
                        it_Value = (short)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "p.Count" => true,
                "p_Count" => true,
                "Count" => true,
                "it.Tag" => true,
                "it_Tag" => true,
                "Tag" => true,
                "it.Value" => true,
                "it_Value" => true,
                "Value" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)p_Count,
                1 => (object)it_Tag,
                2 => (object)it_Value,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "p.Count" => (object)p_Count,
                "p_Count" => (object)p_Count,
                "Count" => (object)p_Count,
                "it.Tag" => (object)it_Tag,
                "it_Tag" => (object)it_Tag,
                "Tag" => (object)it_Tag,
                "it.Value" => (object)it_Value,
                "it_Value" => (object)it_Value,
                "Value" => (object)it_Value,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(byte p_Count, byte it_Tag, short it_Value)
            {
                this.p_Count = p_Count;
                this.it_Tag = it_Tag;
                this.it_Value = it_Value;
            }

            public byte it_Tag { get; }
            public short it_Value { get; }
            public byte p_Count { get; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(byte[] __value0, byte __value1, object[] __value2)
            {
                f_Content = __value0;
                p_Count = __value1;
                p_Items = __value2;
            }

            public byte[] f_Content { get; }
            public byte p_Count { get; }
            public object[] p_Items { get; }
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
    /// Generated interpreter for binary schema 'InlineArrayPacket'.
    /// </summary>
    public sealed class InlineArrayPacket : BytesInterpreterBase<InlineArrayPacket>
    {
        /// <summary>Gets the Count field value.</summary>
        public byte Count { get; init; }
        /// <summary>Gets the Items field value.</summary>
        public Inline_Items[] Items { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "InlineArrayPacket";

        /// <inheritdoc/>
        public override InlineArrayPacket InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            ParsePosition = offset;
            BitOffset = 0;
            var _count = ReadByte(data);
            var __items_list = new System.Collections.Generic.List<Inline_Items>();
            for (int __items_i = 0; __items_i < (int)_count; __items_i++)
            {
                var ___items_list_elemInterpreter = new Inline_Items();
                var ___items_list_elem = ___items_list_elemInterpreter.InterpretAt(data, ParsePosition);
                ParsePosition = ___items_list_elemInterpreter.BytesConsumed;
                __items_list.Add(___items_list_elem);
            }

            var _items = __items_list.ToArray();
            return new InlineArrayPacket
            {
                Count = _count,
                Items = _items
            };
        }
    }

    /// <summary>
    /// Generated nested interpreter for inline schema 'Inline_Items'.
    /// </summary>
    public sealed class Inline_Items : BytesInterpreterBase<Inline_Items>
    {
        /// <summary>Gets the Tag field value.</summary>
        public byte Tag { get; init; }
        /// <summary>Gets the Value field value.</summary>
        public short Value { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Inline_Items";

        /// <inheritdoc/>
        public override Inline_Items InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            ParsePosition = offset;
            BitOffset = 0;
            var _tag = ReadByte(data);
            var _value = ReadInt16Le(data);
            return new Inline_Items
            {
                Tag = _tag,
                Value = _value
            };
        }
    }
}
