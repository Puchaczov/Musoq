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
  Project [p.Count as Count, it.Tag as Tag, it.Value as Value]
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
  PhysicalProject [p.Count as Count, it.Tag as Tag, it.Value as Value]
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
      Count: byte <- field Count_
      Tag: byte <- field Tag
      Value: short <- field Value

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [f: BinaryEntity] -> statement0_fRows
    CreateTable [statement0: Statement0Row0]
    ChunkedForEach [f in statement0_fRows]
      InterpretSource [InlineArrayPacket.Interpret(f.Content) -> statement0_pRows]
      ScalarForEach [p in statement0_pRows]
        AppendRow [statement0 <- Statement0Row0(f.Content: f.Content, p.Count: p.Count, p.Items: p.Items)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    ForEach [fp in _cteRowResults.Slot0]
      Let [fppCount: byte = fp.p.Count]
      EnumerableSource [fp.p.Items -> itRows]
      ChunkedForEach [it in itRows]
        AppendShape [result <- ResultRow0(Count: fppCount, Tag: it.Tag, Value: it.Value)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Count", typeof(byte), 0),
            new Column("Tag", typeof(byte), 1),
            new Column("Value", typeof(short), 2)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("f.Content", typeof(byte[]), 0),
            new Column("p.Count", typeof(byte), 1),
            new Column("p.Items", typeof(Musoq.Generated.Interpreters.Inline_Items[]), 2)
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

                        Statement0Row0 fp = __storedTable0Rows[__storedTable0Index];
                        byte fppCount = fp.p_Count;
                        var itRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Generated.Interpreters.Inline_Items>(fp.p_Items);
                        foreach (var itChunk in itRows)
                        {
                            for (int itIndex = 0, itIndexCount = itChunk.Count; itIndex < itIndexCount; ++itIndex)
                            {
                                if ((itIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var it = itChunk[itIndex];
                                __musoqFinalShapeRows.Add(new ResultRow0(fppCount, it.Tag, it.Value));
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
                Count_ = __value0;
                Tag = __value1;
                Value = __value2;
            }

            public override int Count => 3;
            public byte Count_ { get; private set; }
            public byte Tag { get; private set; }
            public short Value { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Count_ = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Tag = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<short, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Count_),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Tag),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short>(this.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Count" => true,
                "Count_" => true,
                "Tag" => true,
                "Value" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Count_, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Tag, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Value, ref visitor);
            }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(byte[] __value0, byte __value1, Musoq.Generated.Interpreters.Inline_Items[] __value2)
            {
                f_Content = __value0;
                p_Count = __value1;
                p_Items = __value2;
            }

            public byte[] f_Content { get; }
            public byte p_Count { get; }
            public Musoq.Generated.Interpreters.Inline_Items[] p_Items { get; }
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
    public sealed class InlineArrayPacket : BytesInterpreterBase<InlineArrayPacket>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(InlineArrayPacket value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Count"))
            {
                writer.WritePropertyName("Count");
                var fieldValue = this.Count;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Items"))
            {
                writer.WritePropertyName("Items");
                var fieldValue = this.Items;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Count field value.</summary>
        public byte Count { get; set; }
        /// <summary>Gets the Items field value.</summary>
        public Inline_Items[] Items { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "InlineArrayPacket";

        /// <inheritdoc/>
        public override InlineArrayPacket InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new InlineArrayPacket();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Count");
            var _count = ReadByte(data);
            __parsedValue.Count = _count;
            RecordParsedField("Count");
            SetCurrentField("Items");
            var __items_list = new System.Collections.Generic.List<Inline_Items>();
            for (int __items_i = 0; __items_i < (int)_count; __items_i++)
            {
                var ___items_list_elemInterpreter = new Inline_Items();
                var ___items_list_elem = InterpretNested(___items_list_elemInterpreter, data, "Items");
                __items_list.Add(___items_list_elem);
            }

            var _items = __items_list.ToArray();
            __parsedValue.Items = _items;
            RecordParsedField("Items");
            return __parsedValue;
        }
    }

    /// <summary>
    /// Generated nested interpreter for inline schema 'Inline_Items'.
    /// </summary>
    public sealed class Inline_Items : BytesInterpreterBase<Inline_Items>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Inline_Items value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Tag"))
            {
                writer.WritePropertyName("Tag");
                var fieldValue = this.Tag;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Value"))
            {
                writer.WritePropertyName("Value");
                var fieldValue = this.Value;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Tag field value.</summary>
        public byte Tag { get; set; }
        /// <summary>Gets the Value field value.</summary>
        public short Value { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Inline_Items";

        /// <inheritdoc/>
        public override Inline_Items InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new Inline_Items();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Tag");
            var _tag = ReadByte(data);
            __parsedValue.Tag = _tag;
            RecordParsedField("Tag");
            SetCurrentField("Value");
            var _value = ReadInt16Le(data);
            __parsedValue.Value = _value;
            RecordParsedField("Value");
            return __parsedValue;
        }
    }
}
