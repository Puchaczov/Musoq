// === Parsed Query ===
/*
binary ByteValue {
                Value: byte
            };
            binary ShortValue {
                Value: short le
            };
            binary Pair<T, U> {
                LeftItem: T,
                RightItem: U
            };
            binary LengthPrefixed<T> {
                Count: byte,
                Data: T[Count]
            };
            binary NestedGenericContainer {
                Items: LengthPrefixed<Pair<ByteValue,ShortValue>>
            };
            select
                p.LeftItem.Value as LeftValue,
                p.RightItem.Value as RightValue
            from #test.files() f
            cross apply Interpret<NestedGenericContainer>(f.Content) c
            cross apply c.Items.Data p
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, c.Items as c.Items]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#NestedGenericContainer(f.Content) as c]
  Project [fc.f.Content as f.Content, fc.c.Items as c.Items, p.LeftItem as p.LeftItem, p.RightItem as p.RightItem]
    Apply [Cross]
      CteRef [fc as fc]
      PropertySource [fc.c.Items.Data as p] [apply: Cross] [type: Object[]]
  Project [p.LeftItem.Value as LeftValue, p.RightItem.Value as RightValue]
    CteRef [fcp as fcp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, c.Items as c.Items]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#NestedGenericContainer(f.Content) as c]
  PhysicalProject [fc.f.Content as f.Content, fc.c.Items as c.Items, p.LeftItem as p.LeftItem, p.RightItem as p.RightItem]
    PhysicalNestedLoopApply [Cross]
      PhysicalCteRef [fc as fc]
      PhysicalPropertySource [fc.c.Items.Data as p] [apply: Cross] [type: Object[]]
  PhysicalProject [p.LeftItem.Value as LeftValue, p.RightItem.Value as RightValue]
    PhysicalCteRef [fcp as fcp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [c: object]
      Items: object <- property Items
      Items.Count: byte <- nested property Items.Count
      Items.Data: object[] <- nested property Items.Data
    Generated [Statement0Row0]
      f.Content: byte[] <- field f_Content
      c.Items: object <- field c_Items
    TableRow [fc]
      f.Content: byte[] <- field f_Content
      c.Items: object <- field c_Items
    SourceEntity [p: object]
      LeftItem: object <- property LeftItem
      RightItem: object <- property RightItem
    Generated [ResultRow0]
      LeftValue: byte <- field LeftValue
      RightValue: short <- field RightValue

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [f: BinaryEntity] -> statement0_fRows
    CreateTable [statement0: Statement0Row0]
    ChunkedForEach [f in statement0_fRows]
      InterpretSource [NestedGenericContainer.Interpret(f.Content) -> statement0_cRows]
      ScalarForEach [c in statement0_cRows]
        AppendRow [statement0 <- Statement0Row0(f.Content: f.Content, c.Items: c.Items)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    ForEach [fc in _cteRowResults.Slot0]
      EnumerableSource [fc.c.Items.Data -> pRows]
      ChunkedForEach [p in pRows]
        AppendShape [result <- ResultRow0(LeftValue: p.LeftItem.Value, RightValue: p.RightItem.Value)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q59_BinaryNestedGenericInterpret
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
            new Column("LeftValue", typeof(byte), 0),
            new Column("RightValue", typeof(short), 1)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("f.Content", typeof(byte[]), 0),
            new Column("c.Items", typeof(Musoq.Generated.Interpreters.LengthPrefixed<Musoq.Generated.Interpreters.Pair<Musoq.Generated.Interpreters.ByteValue, Musoq.Generated.Interpreters.ShortValue>>), 1)
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

                        Statement0Row0 fc = __storedTable0Rows[__storedTable0Index];
                        var pRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Generated.Interpreters.Pair<Musoq.Generated.Interpreters.ByteValue, Musoq.Generated.Interpreters.ShortValue>>(fc.c_Items.Data);
                        foreach (var pChunk in pRows)
                        {
                            for (int pIndex = 0, pIndexCount = pChunk.Count; pIndex < pIndexCount; ++pIndex)
                            {
                                if ((pIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var p = pChunk[pIndex];
                                __musoqFinalShapeRows.Add(new ResultRow0(((Musoq.Generated.Interpreters.ByteValue)p.LeftItem).Value, ((Musoq.Generated.Interpreters.ShortValue)p.RightItem).Value));
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
                            var _interpreter_NestedGenericContainer = new Musoq.Generated.Interpreters.NestedGenericContainer();
                            var statement0_cRows = _interpreter_NestedGenericContainer.Interpret(f.Content);
                            if (statement0_cRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var c = statement0_cRows;
                                statement0.Add(new Statement0Row0(f.Content, c.Items));
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
                            var _interpreter_NestedGenericContainer = new Musoq.Generated.Interpreters.NestedGenericContainer();
                            var statement0_cRows = _interpreter_NestedGenericContainer.Interpret(f.Content);
                            if (statement0_cRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var c = statement0_cRows;
                                statement0.Add(new Statement0Row0(f.Content, c.Items));
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
                    var _interpreter_NestedGenericContainer = new Musoq.Generated.Interpreters.NestedGenericContainer();
                    var statement0_cRows = _interpreter_NestedGenericContainer.Interpret(f.Content);
                    if (statement0_cRows != null)
                    {
                        token.ThrowIfCancellationRequested();
                        var c = statement0_cRows;
                        statement0.Add(new Statement0Row0(f.Content, c.Items));
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
            public ResultRow0(byte __value0, short __value1)
            {
                LeftValue = __value0;
                RightValue = __value1;
            }

            public override int Count => 2;
            public byte LeftValue { get; private set; }
            public short RightValue { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.LeftValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short>(this.RightValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<short, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.LeftValue),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short>(this.RightValue),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "LeftValue" => true,
                "RightValue" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.LeftValue, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.RightValue, ref visitor);
            }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(byte[] __value0, Musoq.Generated.Interpreters.LengthPrefixed<Musoq.Generated.Interpreters.Pair<Musoq.Generated.Interpreters.ByteValue, Musoq.Generated.Interpreters.ShortValue>> __value1)
            {
                f_Content = __value0;
                c_Items = __value1;
            }

            public Musoq.Generated.Interpreters.LengthPrefixed<Musoq.Generated.Interpreters.Pair<Musoq.Generated.Interpreters.ByteValue, Musoq.Generated.Interpreters.ShortValue>> c_Items { get; }
            public byte[] f_Content { get; }
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
    /// Generated interpreter for binary schema 'ByteValue'.
    /// </summary>
    public sealed class ByteValue : BytesInterpreterBase<ByteValue>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(ByteValue value, System.Collections.Generic.IReadOnlySet<string> completedFields)
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
        public override string SchemaName => "ByteValue";

        /// <inheritdoc/>
        public override ByteValue InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new ByteValue();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Value");
            var _value = ReadByte(data);
            __parsedValue.Value = _value;
            RecordParsedField("Value");
            return __parsedValue;
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'ShortValue'.
    /// </summary>
    public sealed class ShortValue : BytesInterpreterBase<ShortValue>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(ShortValue value, System.Collections.Generic.IReadOnlySet<string> completedFields)
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
        public short Value { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "ShortValue";

        /// <inheritdoc/>
        public override ShortValue InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new ShortValue();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Value");
            var _value = ReadInt16Le(data);
            __parsedValue.Value = _value;
            RecordParsedField("Value");
            return __parsedValue;
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'Pair'.
    /// This is a generic schema with type parameters: T, U.
    /// </summary>
    public sealed class Pair<T, U> : BytesInterpreterBase<Pair<T, U>>, global::Musoq.Plugins.IJsonPayload where T : IBytesInterpreter<T>, new()
        where U : IBytesInterpreter<U>, new()
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Pair<T, U> value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("LeftItem"))
            {
                writer.WritePropertyName("LeftItem");
                var fieldValue = this.LeftItem;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("RightItem"))
            {
                writer.WritePropertyName("RightItem");
                var fieldValue = this.RightItem;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the LeftItem field value.</summary>
        public T LeftItem { get; set; }
        /// <summary>Gets the RightItem field value.</summary>
        public U RightItem { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Pair";

        /// <inheritdoc/>
        public override Pair<T, U> InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new Pair<T, U>();
            RecordParsedValue(__parsedValue);
            SetCurrentField("LeftItem");
            var __leftItem_interpreter = new T();
            var _leftItem = InterpretNested(__leftItem_interpreter, data, "LeftItem");
            __parsedValue.LeftItem = _leftItem;
            RecordParsedField("LeftItem");
            SetCurrentField("RightItem");
            var __rightItem_interpreter = new U();
            var _rightItem = InterpretNested(__rightItem_interpreter, data, "RightItem");
            __parsedValue.RightItem = _rightItem;
            RecordParsedField("RightItem");
            return __parsedValue;
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'LengthPrefixed'.
    /// This is a generic schema with type parameters: T.
    /// </summary>
    public sealed class LengthPrefixed<T> : BytesInterpreterBase<LengthPrefixed<T>>, global::Musoq.Plugins.IJsonPayload where T : IBytesInterpreter<T>, new()
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(LengthPrefixed<T> value, System.Collections.Generic.IReadOnlySet<string> completedFields)
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

            if (IsFieldComplete("Data"))
            {
                writer.WritePropertyName("Data");
                var fieldValue = this.Data;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Count field value.</summary>
        public byte Count { get; set; }
        /// <summary>Gets the Data field value.</summary>
        public T[] Data { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "LengthPrefixed";

        /// <inheritdoc/>
        public override LengthPrefixed<T> InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new LengthPrefixed<T>();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Count");
            var _count = ReadByte(data);
            __parsedValue.Count = _count;
            RecordParsedField("Count");
            SetCurrentField("Data");
            var __data_list = new System.Collections.Generic.List<T>();
            for (int __data_i = 0; __data_i < (int)_count; __data_i++)
            {
                var __data_elemInterpreter = new T();
                var _elem = InterpretNested(__data_elemInterpreter, data, "Data");
                __data_list.Add(_elem);
            }

            var _data = __data_list.ToArray();
            __parsedValue.Data = _data;
            RecordParsedField("Data");
            return __parsedValue;
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'NestedGenericContainer'.
    /// </summary>
    public sealed class NestedGenericContainer : BytesInterpreterBase<NestedGenericContainer>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(NestedGenericContainer value, System.Collections.Generic.IReadOnlySet<string> completedFields)
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
        public LengthPrefixed<Pair<ByteValue, ShortValue>> Items { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "NestedGenericContainer";

        /// <inheritdoc/>
        public override NestedGenericContainer InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new NestedGenericContainer();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Items");
            var __items_interpreter = new LengthPrefixed<Pair<ByteValue, ShortValue>>();
            var _items = InterpretNested(__items_interpreter, data, "Items");
            __parsedValue.Items = _items;
            RecordParsedField("Items");
            return __parsedValue;
        }
    }
}
