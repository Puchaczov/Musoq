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
  Project [i.Value as Value]
    CteRef [fsi as fsi]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [s.Items as s.Items]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#ItemStream(f.Content) as s]
  PhysicalProject [i.Value as i.Value]
    PhysicalNestedLoopApply [Cross]
      PhysicalCteRef [fs as fs]
      PhysicalPropertySource [fs.s.Items as i] [apply: Cross] [type: Object[]]
  PhysicalProject [i.Value as Value]
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
      s.Items: object[] <- field s_Items
    TableRow [fs]
      s.Items: object[] <- field s_Items
    SourceEntity [i: object]
      Value: byte <- property Value
    Generated [ResultRow0]
      Value: byte <- field Value

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [f: BinaryEntity] -> statement0_fRows
    CreateTable [statement0: Statement0Row0]
    ChunkedForEach [f in statement0_fRows]
      InterpretSource [ItemStream.Interpret(f.Content) -> statement0_sRows]
      ScalarForEach [s in statement0_sRows]
        AppendRow [statement0 <- Statement0Row0(s.Items: s.Items)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    ForEach [fs in _cteRowResults.Slot0]
      EnumerableSource [fs.s.Items -> iRows]
      ChunkedForEach [i in iRows]
        AppendShape [result <- ResultRow0(Value: i.Value)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Value", typeof(byte), 0)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("s.Items", typeof(Musoq.Generated.Interpreters.Item[]), 0)
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultRow0>();
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
                                __musoqFinalShapeRows.Add(new ResultRow0(i.Value));
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
            var statement0_fRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>.Open(__statement0_fSchema, "files", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
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
                                statement0.Add(new Statement0Row0(s.Items));
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
                                statement0.Add(new Statement0Row0(s.Items));
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
                        statement0.Add(new Statement0Row0(s.Items));
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
                Value = __value0;
            }

            public override int Count => 1;
            public byte Value { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Value" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Value, ref visitor);
            }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(Musoq.Generated.Interpreters.Item[] __value0)
            {
                s_Items = __value0;
            }

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
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Item : BytesInterpreterBase<Item>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Item value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Value"))
            {
                writer.WritePropertyName("Value");
                var fieldValue = this.Value;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Value field value.</summary>
        public byte Value { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Item";

        /// <inheritdoc/>
        public override Item InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new Item();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Value");
            var _value = ReadByte(data);
            __parsedValue.Value = _value;
            RecordParsedField("Value");
            SetCurrentField("_");
            var _discard1 = ReadBytes(data, 2);
            return __parsedValue;
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'ItemStream'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class ItemStream : BytesInterpreterBase<ItemStream>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(ItemStream value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Items"))
            {
                writer.WritePropertyName("Items");
                var fieldValue = this.Items;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Items field value.</summary>
        public Item[] Items { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "ItemStream";

        /// <inheritdoc/>
        public override ItemStream InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new ItemStream();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Items");
            var __items_list = new System.Collections.Generic.List<Item>();
            Item __items_lastElem;
            var __items_iteration = 0;
            while (!IsAtEnd(data))
            {
                EnsureRepeatIteration("Items", __items_iteration++, 10000);
                var __items_startPos = ParsePosition;
                var __items_startBit = BitOffset;
                var ___items_lastElem_value_interpreter = new Item();
                var __items_lastElem_value = InterpretNested(___items_lastElem_value_interpreter, data, "Items");
                __items_lastElem = __items_lastElem_value;
                __items_list.Add(__items_lastElem);
                EnsureRepeatMadeProgress("Items", __items_startPos, __items_startBit);
            }

            var _items = __items_list.ToArray();
            __parsedValue.Items = _items;
            RecordParsedField("Items");
            return __parsedValue;
        }
    }
}
