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
      Content: ExpandoObject <- property Content
    Generated [ResultRow0]
      c.Content.Key: object <- field c_Content_Key
      c.Content.Text: object <- field c_Content_Text

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: TextEntity] -> fRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [ConfigLine.Parse(f.Line) -> cRows]
      ScalarForEach [c in cRows]
        AppendShape [result <- ResultShape0(c.Content.Key: c.Content.Key, c.Content.Text: c.Content.Text)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("c.Content.Key", typeof(object), 0),
            new Column("c.Content.Text", typeof(object), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_f_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Line", typeof(string), 1) });
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
                yield return new ResultRow0(__musoqShapeRow.c_Content_Key, __musoqShapeRow.c_Content_Text);
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
                    var fRowsSource = __fSchema.GetRowSource<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity>("lines", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), Array.Empty<object>());
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
                                        yield return new ResultShape0(GeneratedDictionaryAccess.GetValue(c.Content, "Key"), GeneratedDictionaryAccess.GetValue(c.Content, "Text"));
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
                                        yield return new ResultShape0(GeneratedDictionaryAccess.GetValue(c.Content, "Key"), GeneratedDictionaryAccess.GetValue(c.Content, "Text"));
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
                                yield return new ResultShape0(GeneratedDictionaryAccess.GetValue(c.Content, "Key"), GeneratedDictionaryAccess.GetValue(c.Content, "Text"));
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

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        c_Content_Key = value;
                        break;
                    case 1:
                        c_Content_Text = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

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
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)c_Content_Key,
                1 => (object)c_Content_Text,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "c.Content.Key" => (object)c_Content_Key,
                "c_Content_Key" => (object)c_Content_Key,
                "Key" => (object)c_Content_Key,
                "c.Content.Text" => (object)c_Content_Text,
                "c_Content_Text" => (object)c_Content_Text,
                "Text" => (object)c_Content_Text,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(object c_Content_Key, object c_Content_Text)
            {
                this.c_Content_Key = c_Content_Key;
                this.c_Content_Text = c_Content_Text;
            }

            public object c_Content_Key { get; }
            public object c_Content_Text { get; }
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
    public sealed class Comment : TextInterpreterBase<Comment>
    {
        /// <summary>Gets the Text field value.</summary>
        public string? Text { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Comment";

        /// <inheritdoc/>
        public override Comment ParseAt(ReadOnlySpan<char> data, int offset)
        {
            ParsePosition = offset;
            SetCurrentField(null);
            SetCurrentField("_");
            ExpectLiteral(data, "#", fieldName: "_");
            SetCurrentField("Text");
            var _text = ReadRest(data, fieldName: "Text");
            RecordParsedField("Text", _text);
            return new Comment
            {
                Text = _text
            };
        }
    }

    /// <summary>
    /// Generated interpreter for text schema 'KeyValue'.
    /// </summary>
    public sealed class KeyValue : TextInterpreterBase<KeyValue>
    {
        /// <summary>Gets the Key field value.</summary>
        public string? Key { get; init; }
        /// <summary>Gets the Value field value.</summary>
        public string? Value { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "KeyValue";

        /// <inheritdoc/>
        public override KeyValue ParseAt(ReadOnlySpan<char> data, int offset)
        {
            ParsePosition = offset;
            SetCurrentField(null);
            SetCurrentField("Key");
            var _key = ReadUntil(data, "=", fieldName: "Key");
            RecordParsedField("Key", _key);
            SetCurrentField("Value");
            var _value = ReadRest(data, fieldName: "Value");
            RecordParsedField("Value", _value);
            return new KeyValue
            {
                Key = _key,
                Value = _value
            };
        }
    }

    /// <summary>
    /// Generated interpreter for text schema 'ConfigLine'.
    /// </summary>
    public sealed class ConfigLine : TextInterpreterBase<ConfigLine>
    {
        /// <summary>Gets the Content field value.</summary>
        public object? Content { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "ConfigLine";

        /// <inheritdoc/>
        public override ConfigLine ParseAt(ReadOnlySpan<char> data, int offset)
        {
            ParsePosition = offset;
            SetCurrentField(null);
            SetCurrentField("Content");
            var __content_expando = new System.Dynamic.ExpandoObject();
            var __content_dict = (System.Collections.Generic.IDictionary<string, object?>)__content_expando;
            __content_dict["Text"] = null;
            __content_dict["Key"] = null;
            __content_dict["Value"] = null;
            if (LookaheadMatchesPattern(data, @"#", fieldName: "Content"))
            {
                var _interp_Comment = new Comment();
                var _result_Comment = ParseNested(_interp_Comment, data, "Content");
                __content_dict["Text"] = _result_Comment.Text;
            }
            else
            {
                var _interp_KeyValue = new KeyValue();
                var _result_KeyValue = ParseNested(_interp_KeyValue, data, "Content");
                __content_dict["Key"] = _result_KeyValue.Key;
                __content_dict["Value"] = _result_KeyValue.Value;
            }

            var _content = __content_expando;
            RecordParsedField("Content", _content);
            return new ConfigLine
            {
                Content = _content
            };
        }
    }
}
