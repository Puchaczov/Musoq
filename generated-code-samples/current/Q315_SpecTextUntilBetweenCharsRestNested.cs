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
  Project [s.Prefix as s.Prefix, s.Tag as s.Tag, s.Key as s.Key, s.Value as s.Value]
    CteRef [fs as fs]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Line as f.Line, s.Prefix as s.Prefix, s.Tag as s.Tag, s.Key as s.Key, s.Value as s.Value]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.lines() as f]
      PhysicalInterpretSource [#Structured(f.Line) as s]
  PhysicalProject [s.Prefix as s.Prefix, s.Tag as s.Tag, s.Key as s.Key, s.Value as s.Value]
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
      s.Prefix: string <- field s_Prefix
      s.Tag: string <- field s_Tag
      s.Key: string <- field s_Key
      s.Value: string <- field s_Value

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: TextEntity] -> fRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [Structured.Parse(f.Line) -> sRows]
      ScalarForEach [s in sRows]
        AppendShape [result <- ResultShape0(s.Prefix: s.Prefix, s.Tag: s.Tag, s.Key: s.Key, s.Value: s.Value)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("s.Prefix", typeof(string), 0),
            new Column("s.Tag", typeof(string), 1),
            new Column("s.Key", typeof(string), 2),
            new Column("s.Value", typeof(string), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_f_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Line", typeof(string), 0) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.s_Prefix, __musoqShapeRow.s_Tag, __musoqShapeRow.s_Key, __musoqShapeRow.s_Value);
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
                                    var _interpreter_Structured = new Musoq.Generated.Interpreters.Structured();
                                    var sRows = _interpreter_Structured.Parse(f.Line);
                                    if (sRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var s = sRows;
                                        yield return new ResultShape0(s.Prefix, s.Tag, s.Key, s.Value);
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
                                        yield return new ResultShape0(s.Prefix, s.Tag, s.Key, s.Value);
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
                                yield return new ResultShape0(s.Prefix, s.Tag, s.Key, s.Value);
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
                s_Prefix = __value0;
                s_Tag = __value1;
                s_Key = __value2;
                s_Value = __value3;
            }

            public override int Count => 4;
            public string s_Key { get; private set; }
            public string s_Prefix { get; private set; }
            public string s_Tag { get; private set; }
            public string s_Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        s_Prefix = (string)value;
                        break;
                    case 1:
                        s_Tag = (string)value;
                        break;
                    case 2:
                        s_Key = (string)value;
                        break;
                    case 3:
                        s_Value = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "s.Prefix" => true,
                "s_Prefix" => true,
                "Prefix" => true,
                "s.Tag" => true,
                "s_Tag" => true,
                "Tag" => true,
                "s.Key" => true,
                "s_Key" => true,
                "Key" => true,
                "s.Value" => true,
                "s_Value" => true,
                "Value" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)s_Prefix,
                1 => (object)s_Tag,
                2 => (object)s_Key,
                3 => (object)s_Value,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "s.Prefix" => (object)s_Prefix,
                "s_Prefix" => (object)s_Prefix,
                "Prefix" => (object)s_Prefix,
                "s.Tag" => (object)s_Tag,
                "s_Tag" => (object)s_Tag,
                "Tag" => (object)s_Tag,
                "s.Key" => (object)s_Key,
                "s_Key" => (object)s_Key,
                "Key" => (object)s_Key,
                "s.Value" => (object)s_Value,
                "s_Value" => (object)s_Value,
                "Value" => (object)s_Value,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string s_Prefix, string s_Tag, string s_Key, string s_Value)
            {
                this.s_Prefix = s_Prefix;
                this.s_Tag = s_Tag;
                this.s_Key = s_Key;
                this.s_Value = s_Value;
            }

            public string s_Key { get; }
            public string s_Prefix { get; }
            public string s_Tag { get; }
            public string s_Value { get; }
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
    public sealed class Structured : TextInterpreterBase<Structured>
    {
        /// <summary>Gets the Prefix field value.</summary>
        public string? Prefix { get; init; }
        /// <summary>Gets the Tag field value.</summary>
        public string? Tag { get; init; }
        /// <summary>Gets the Key field value.</summary>
        public string? Key { get; init; }
        /// <summary>Gets the Value field value.</summary>
        public string? Value { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Structured";

        /// <inheritdoc/>
        public override Structured ParseAt(ReadOnlySpan<char> data, int offset)
        {
            ParsePosition = offset;
            SetCurrentField(null);
            SetCurrentField("Prefix");
            var _prefix = ReadChars(data, 2, fieldName: "Prefix");
            RecordParsedField("Prefix", _prefix);
            SetCurrentField("Tag");
            var _tag = ReadBetween(data, "[", "]", nested: true, fieldName: "Tag");
            RecordParsedField("Tag", _tag);
            SetCurrentField("Key");
            var _key = ReadUntil(data, "=", fieldName: "Key");
            RecordParsedField("Key", _key);
            SetCurrentField("Value");
            var _value = ReadRest(data, fieldName: "Value");
            RecordParsedField("Value", _value);
            return new Structured
            {
                Prefix = _prefix,
                Tag = _tag,
                Key = _key,
                Value = _value
            };
        }
    }
}
