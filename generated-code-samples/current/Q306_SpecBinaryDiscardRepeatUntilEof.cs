// === Parsed Query ===
/*
binary Item {
                        Value: byte,
                        _: byte[2]
                    };
                    binary ItemStream {
                        Items: Item repeat until eof
                    };
                    select i.Value
                    from #test.files() f
                    cross apply Interpret<ItemStream>(f.Content) s
                    cross apply s.Items i
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, s.Items as s.Items]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#ItemStream(f.Content) as s]
  Project [fs.f.Content as f.Content, fs.s.Items as s.Items, i.Value as i.Value]
    Apply [Cross]
      CteRef [fs as fs]
      PropertySource [fs.s.Items as i] [apply: Cross] [type: Object[]]
  Project [i.Value as i.Value]
    CteRef [fsi as fsi]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, s.Items as s.Items]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#ItemStream(f.Content) as s]
  PhysicalProject [fs.f.Content as f.Content, fs.s.Items as s.Items, i.Value as i.Value]
    PhysicalNestedLoopApply [Cross]
      PhysicalCteRef [fs as fs]
      PhysicalPropertySource [fs.s.Items as i] [apply: Cross] [type: Object[]]
  PhysicalProject [i.Value as i.Value]
    PhysicalCteRef [fsi as fsi]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [s: object]
      Items: object[] <- property Items
    Generated [Statement0Row0]
      f.Content: byte[] <- field f_Content
      s.Items: object[] <- field s_Items
    TableRow [fs]
      f.Content: byte[] <- field f_Content
      s.Items: object[] <- field s_Items
    SourceEntity [i: object]
      Value: byte <- property Value
    Generated [ResultRow0]
      i.Value: byte <- field i_Value

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [f: BinaryEntity] -> statement0_fRows
    CreateTable [statement0: Statement0Row0]
    ChunkedForEach [f in statement0_fRows]
      InterpretSource [ItemStream.Interpret(f.Content) -> statement0_sRows]
      ScalarForEach [s in statement0_sRows]
        AppendRow [statement0 <- Statement0Row0(f.Content: f.Content, s.Items: s.Items)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [fs in _cteRowResults.Slot0]
      EnumerableSource [fs.s.Items -> iRows]
      ChunkedForEach [i in iRows]
        AppendShape [result <- ResultShape0(i.Value: i.Value)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q306_SpecBinaryDiscardRepeatUntilEof
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("i.Value", typeof(byte), 0)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("f.Content", typeof(byte[]), 0),
            new Column("s.Items", typeof(object[]), 1)
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.i_Value);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
                try
                {
                    var __storedTable0Rows = _cteRowResults.Slot0;
                    for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                    {
                        if ((__storedTable0Index & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        Statement0Row0 fs = __storedTable0Rows[__storedTable0Index];
                        var iRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Generated.Interpreters.Item>(fs.s_Items);
                        foreach (var iChunk in iRows)
                        {
                            for (int iIndex = 0, iIndexCount = iChunk.Count; iIndex < iIndexCount; ++iIndex)
                            {
                                if ((iIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var i = iChunk[iIndex];
                                __musoqFinalShapeRows.Add(new ResultShape0(i.Value));
                            }
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte1", QueryPhase.End);
                }

                return __musoqFinalShapeRows;
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            var __statement0_fSchema = provider.GetSchema("#test");
            var statement0_fRowsSource = __statement0_fSchema.GetRowSource<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>("files", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var statement0_fRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>(statement0_fRowsSource.Chunks, __musoqProgressContext, "f:1") : statement0_fRowsSource.Chunks;
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
                            var _interpreter_ItemStream = new Musoq.Generated.Interpreters.ItemStream();
                            var statement0_sRows = _interpreter_ItemStream.Interpret(f.Content);
                            if (statement0_sRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var s = statement0_sRows;
                                statement0.Add(new Statement0Row0(f.Content, s.Items));
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
                            var _interpreter_ItemStream = new Musoq.Generated.Interpreters.ItemStream();
                            var statement0_sRows = _interpreter_ItemStream.Interpret(f.Content);
                            if (statement0_sRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var s = statement0_sRows;
                                statement0.Add(new Statement0Row0(f.Content, s.Items));
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
                    var _interpreter_ItemStream = new Musoq.Generated.Interpreters.ItemStream();
                    var statement0_sRows = _interpreter_ItemStream.Interpret(f.Content);
                    if (statement0_sRows != null)
                    {
                        token.ThrowIfCancellationRequested();
                        var s = statement0_sRows;
                        statement0.Add(new Statement0Row0(f.Content, s.Items));
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
            public ResultRow0(byte __value0)
            {
                i_Value = __value0;
            }

            public override int Count => 1;
            public byte i_Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        i_Value = (byte)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "i.Value" => true,
                "i_Value" => true,
                "Value" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)i_Value,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "i.Value" => (object)i_Value,
                "i_Value" => (object)i_Value,
                "Value" => (object)i_Value,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(byte i_Value)
            {
                this.i_Value = i_Value;
            }

            public byte i_Value { get; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(byte[] __value0, Musoq.Generated.Interpreters.Item[] __value1)
            {
                f_Content = __value0;
                s_Items = __value1;
            }

            public byte[] f_Content { get; }
            public Musoq.Generated.Interpreters.Item[] s_Items { get; }
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
    /// Generated interpreter for binary schema 'Item'.
    /// </summary>
    public sealed class Item : BytesInterpreterBase<Item>
    {
        /// <summary>Gets the Value field value.</summary>
        public byte Value { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Item";

        /// <inheritdoc/>
        public override Item InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("Value");
            var _value = ReadByte(data);
            RecordParsedField("Value", _value);
            SetCurrentField("_");
            var _discard1 = ReadBytes(data, 2);
            return new Item
            {
                Value = _value
            };
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'ItemStream'.
    /// </summary>
    public sealed class ItemStream : BytesInterpreterBase<ItemStream>
    {
        /// <summary>Gets the Items field value.</summary>
        public Item[] Items { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "ItemStream";

        /// <inheritdoc/>
        public override ItemStream InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("Items");
            var __items_list = new System.Collections.Generic.List<Item>();
            Item __items_lastElem;
            var __items_iteration = 0;
            while (!IsAtEnd(data))
            {
                EnsureRepeatIteration("Items", __items_iteration++);
                var __items_startPos = ParsePosition;
                var __items_startBit = BitOffset;
                var __items_lastElem_interpreter = new Item();
                __items_lastElem = InterpretNested(__items_lastElem_interpreter, data, "Items");
                __items_list.Add(__items_lastElem);
                EnsureRepeatMadeProgress("Items", __items_startPos, __items_startBit);
            }

            var _items = __items_list.ToArray();
            RecordParsedField("Items", _items);
            return new ItemStream
            {
                Items = _items
            };
        }
    }
}
