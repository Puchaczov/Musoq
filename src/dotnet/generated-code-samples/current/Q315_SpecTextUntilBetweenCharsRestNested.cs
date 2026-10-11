// === Parsed Query ===
/*
text Structured {
                        Prefix: chars[2],
                        Tag: between '[' ']' nested,
                        Key: until '=',
                        Value: rest
                    };
                    select s.Prefix, s.Tag, s.Key, s.Value
                    from #test.lines() f
                    cross apply Parse<Structured>(f.Line) s
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Line as f.Line, s.Prefix as s.Prefix, s.Tag as s.Tag, s.Key as s.Key, s.Value as s.Value]
    Apply [Cross]
      SchemaScan [#test.lines() as f]
      InterpretSource [#Structured(f.Line) as s]
  Project [s.Prefix as Prefix, s.Tag as Tag, s.Key as Key, s.Value as Value]
    CteRef [fs as fs]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Line as f.Line, s.Prefix as s.Prefix, s.Tag as s.Tag, s.Key as s.Key, s.Value as s.Value]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.lines() as f]
      PhysicalInterpretSource [#Structured(f.Line) as s]
  PhysicalProject [s.Prefix as Prefix, s.Tag as Tag, s.Key as Key, s.Value as Value]
    PhysicalCteRef [fs as fs]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: TextEntity]
      Line: string <- property Line
    SourceEntity [s: object]
      Prefix: string <- property Prefix
      Tag: string <- property Tag
      Key: string <- property Key
      Value: string <- property Value
    Generated [ResultRow0]
      Prefix: string <- field Prefix
      Tag: string <- field Tag
      Key: string <- field Key
      Value: string <- field Value

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: TextEntity] -> fRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [Structured.Parse(f.Line) -> sRows]
      ScalarForEach [s in sRows]
        AppendShape [result <- ResultRow0(Prefix: s.Prefix, Tag: s.Tag, Key: s.Key, Value: s.Value)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q315_SpecTextUntilBetweenCharsRestNested
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
            new Column("Prefix", typeof(string), 0),
            new Column("Tag", typeof(string), 1),
            new Column("Key", typeof(string), 2),
            new Column("Value", typeof(string), 3)
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                                    var _interpreter_Structured = new Musoq.Generated.Interpreters.Structured();
                                    var sRows = _interpreter_Structured.Parse(f.Line);
                                    if (sRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var s = sRows;
                                        yield return new ResultRow0(s.Prefix, s.Tag, s.Key, s.Value);
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
                                    var _interpreter_Structured = new Musoq.Generated.Interpreters.Structured();
                                    var sRows = _interpreter_Structured.Parse(f.Line);
                                    if (sRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var s = sRows;
                                        yield return new ResultRow0(s.Prefix, s.Tag, s.Key, s.Value);
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
                            var _interpreter_Structured = new Musoq.Generated.Interpreters.Structured();
                            var sRows = _interpreter_Structured.Parse(f.Line);
                            if (sRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var s = sRows;
                                yield return new ResultRow0(s.Prefix, s.Tag, s.Key, s.Value);
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
            public ResultRow0(string __value0, string __value1, string __value2, string __value3)
            {
                Prefix = __value0;
                Tag = __value1;
                Key = __value2;
                Value = __value3;
            }

            public override int Count => 4;
            public string Key { get; private set; }
            public string Prefix { get; private set; }
            public string Tag { get; private set; }
            public string Value { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Prefix = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Tag = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Key = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Prefix),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Tag),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Key),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Prefix" => true,
                "Tag" => true,
                "Key" => true,
                "Value" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Prefix, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Tag, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Key, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Value, ref visitor);
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
    /// Generated interpreter for text schema 'Structured'.
    /// </summary>
    public sealed class Structured : TextInterpreterBase<Structured>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Structured value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Prefix"))
            {
                writer.WritePropertyName("Prefix");
                var fieldValue = this.Prefix;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Tag"))
            {
                writer.WritePropertyName("Tag");
                var fieldValue = this.Tag;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

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

        /// <summary>Gets the Prefix field value.</summary>
        public string? Prefix { get; set; }
        /// <summary>Gets the Tag field value.</summary>
        public string? Tag { get; set; }
        /// <summary>Gets the Key field value.</summary>
        public string? Key { get; set; }
        /// <summary>Gets the Value field value.</summary>
        public string? Value { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Structured";

        /// <inheritdoc/>
        public override Structured ParseAt(ReadOnlySpan<char> data, int offset)
        {
            ParsePosition = offset;
            SetCurrentField(null);
            var __parsedValue = new Structured();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Prefix");
            var _prefix = ReadChars(data, 2, fieldName: "Prefix");
            __parsedValue.Prefix = _prefix;
            RecordParsedField("Prefix");
            SetCurrentField("Tag");
            var _tag = ReadBetween(data, "[", "]", nested: true, fieldName: "Tag");
            __parsedValue.Tag = _tag;
            RecordParsedField("Tag");
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
}
