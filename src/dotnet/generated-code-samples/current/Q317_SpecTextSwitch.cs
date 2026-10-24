// === Parsed Query ===
/*
text Comment { _: literal '#', Text: rest };
                    text KeyValue { Key: until '=', Value: rest };
                    text ConfigLine {
                        Content: switch {
                            pattern '#' => Comment,
                            _ => KeyValue
                        }
                    };
                    select c.Content.Key, c.Content.Text
                    from #test.lines() f
                    cross apply Parse<ConfigLine>(f.Line) c
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Line as f.Line, c.Content as c.Content]
    Apply [Cross]
      SchemaScan [#test.lines() as f]
      InterpretSource [#ConfigLine(f.Line) as c]
  Project [c.Content.Key as c.Content.Key, c.Content.Text as c.Content.Text]
    CteRef [fc as fc]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Line as f.Line, c.Content as c.Content]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.lines() as f]
      PhysicalInterpretSource [#ConfigLine(f.Line) as c]
  PhysicalProject [c.Content.Key as c.Content.Key, c.Content.Text as c.Content.Text]
    PhysicalCteRef [fc as fc]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: TextEntity]
      Line: string <- property Line
    SourceEntity [c: object]
      Content: object <- property Content
    Generated [ResultRow0]
      c.Content.Key: object <- field c_Content_Key
      c.Content.Text: object <- field c_Content_Text

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: TextEntity] -> fRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [ConfigLine.Parse(f.Line) -> cRows]
      ScalarForEach [c in cRows]
        Let [content: object = c.Content]
        AppendShape [result <- ResultRow0(c.Content.Key: UnionMember(Key, 1 cases), c.Content.Text: UnionMember(Text, 1 cases))]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q317_SpecTextSwitch
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("c.Content.Key", typeof(object), 0),
            new Column("c.Content.Text", typeof(object), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_f_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Line", typeof(string), 0) });
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
            var __musoqMaterializedTable = QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
            _ = __musoqMaterializedTable.Count;
            return __musoqMaterializedTable;
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
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    var __fSchema = provider.GetSchema("#test");
                    var fRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity>.Open(__fSchema, "lines", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                    var fRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity>(fRowsSource.Chunks, __musoqProgressContext, "f:1") : fRowsSource.Chunks;
                    foreach (var fChunk in fRows)
                    {
                        if (fChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity> fChunkView)
                        {
                            if (fChunkView.Source is Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity[] fChunkViewArray)
                            {
                                int fChunkViewOffset = fChunkView.Offset;
                                for (int fIndex = 0, fIndexCount = fChunkView.Count; fIndex < fIndexCount; ++fIndex)
                                {
                                    if ((fIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var f = fChunkViewArray[fChunkViewOffset + fIndex];
                                    var _interpreter_ConfigLine = new Musoq.Generated.Interpreters.ConfigLine();
                                    var cRows = _interpreter_ConfigLine.Parse(f.Line);
                                    if (cRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var c = cRows;
                                        object content = c.Content;
                                        yield return new ResultRow0((content switch
                                        {
                                            Musoq.Generated.Interpreters.KeyValue __unionMember => (object)__unionMember.Key,
                                            _ => (object)null
                                        }), (content switch
                                        {
                                            Musoq.Generated.Interpreters.Comment __unionMember => (object)__unionMember.Text,
                                            _ => (object)null
                                        }));
                                    }
                                }

                                continue;
                            }

                            if (fChunkView.Source is List<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity> fChunkViewList)
                            {
                                int fChunkViewOffset = fChunkView.Offset;
                                for (int fIndex = 0, fIndexCount = fChunkView.Count; fIndex < fIndexCount; ++fIndex)
                                {
                                    if ((fIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var f = fChunkViewList[fChunkViewOffset + fIndex];
                                    var _interpreter_ConfigLine = new Musoq.Generated.Interpreters.ConfigLine();
                                    var cRows = _interpreter_ConfigLine.Parse(f.Line);
                                    if (cRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var c = cRows;
                                        object content = c.Content;
                                        yield return new ResultRow0((content switch
                                        {
                                            Musoq.Generated.Interpreters.KeyValue __unionMember => (object)__unionMember.Key,
                                            _ => (object)null
                                        }), (content switch
                                        {
                                            Musoq.Generated.Interpreters.Comment __unionMember => (object)__unionMember.Text,
                                            _ => (object)null
                                        }));
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
                            var _interpreter_ConfigLine = new Musoq.Generated.Interpreters.ConfigLine();
                            var cRows = _interpreter_ConfigLine.Parse(f.Line);
                            if (cRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var c = cRows;
                                object content = c.Content;
                                yield return new ResultRow0((content switch
                                {
                                    Musoq.Generated.Interpreters.KeyValue __unionMember => (object)__unionMember.Key,
                                    _ => (object)null
                                }), (content switch
                                {
                                    Musoq.Generated.Interpreters.Comment __unionMember => (object)__unionMember.Text,
                                    _ => (object)null
                                }));
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
            public ResultRow0(object __value0, object __value1)
            {
                c_Content_Key = __value0;
                c_Content_Text = __value1;
            }

            public override int Count => 2;
            public object c_Content_Key { get; private set; }
            public object c_Content_Text { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, object>(this.c_Content_Key = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<object, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, object>(this.c_Content_Text = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<object, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, object>(this.c_Content_Key),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, object>(this.c_Content_Text),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "c.Content.Key" => true,
                "c_Content_Key" => true,
                "Key" => true,
                "c.Content.Text" => true,
                "c_Content_Text" => true,
                "Text" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.c_Content_Key, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.c_Content_Text, ref visitor);
            }
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
    /// Generated interpreter for text schema 'Comment'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Comment : TextInterpreterBase<Comment>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Comment value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Text"))
            {
                writer.WritePropertyName("Text");
                var fieldValue = this.Text;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Text field value.</summary>
        public string? Text { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Comment";

        /// <inheritdoc/>
        public override Comment ParseAt(ReadOnlySpan<char> data, int offset)
        {
            ParsePosition = offset;
            SetCurrentField(null);
            var __parsedValue = new Comment();
            RecordParsedValue(__parsedValue);
            SetCurrentField("_");
            ExpectLiteral(data, "#", fieldName: "_");
            SetCurrentField("Text");
            var _text = ReadRest(data, fieldName: "Text");
            __parsedValue.Text = _text;
            RecordParsedField("Text");
            return __parsedValue;
        }
    }

    /// <summary>
    /// Generated interpreter for text schema 'KeyValue'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class KeyValue : TextInterpreterBase<KeyValue>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(KeyValue value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Key"))
            {
                writer.WritePropertyName("Key");
                var fieldValue = this.Key;
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

        /// <summary>Gets the Key field value.</summary>
        public string? Key { get; set; }
        /// <summary>Gets the Value field value.</summary>
        public string? Value { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "KeyValue";

        /// <inheritdoc/>
        public override KeyValue ParseAt(ReadOnlySpan<char> data, int offset)
        {
            ParsePosition = offset;
            SetCurrentField(null);
            var __parsedValue = new KeyValue();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Key");
            var _key = ReadUntil(data, "=", fieldName: "Key");
            __parsedValue.Key = _key;
            RecordParsedField("Key");
            SetCurrentField("Value");
            var _value = ReadRest(data, fieldName: "Value");
            __parsedValue.Value = _value;
            RecordParsedField("Value");
            return __parsedValue;
        }
    }

    /// <summary>
    /// Generated interpreter for text schema 'ConfigLine'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class ConfigLine : TextInterpreterBase<ConfigLine>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(ConfigLine value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Content"))
            {
                writer.WritePropertyName("Content");
                var fieldValue = this.Content;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Content field value.</summary>
        public object? Content { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "ConfigLine";

        /// <inheritdoc/>
        public override ConfigLine ParseAt(ReadOnlySpan<char> data, int offset)
        {
            ParsePosition = offset;
            SetCurrentField(null);
            var __parsedValue = new ConfigLine();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Content");
            object? _content = default;
            if (LookaheadMatchesPattern(data, @"#", fieldName: "Content"))
            {
                var _interp_content_selected = new Comment();
                var _result_content_selected = ParseNested(_interp_content_selected, data, "Content");
                var _content_selected = _result_content_selected;
                _content = _content_selected;
            }
            else
            {
                var _interp_content_selected = new KeyValue();
                var _result_content_selected = ParseNested(_interp_content_selected, data, "Content");
                var _content_selected = _result_content_selected;
                _content = _content_selected;
            }

            __parsedValue.Content = _content;
            RecordParsedField("Content");
            return __parsedValue;
        }
    }
}
