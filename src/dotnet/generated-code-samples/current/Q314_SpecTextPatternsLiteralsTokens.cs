// === Parsed Query ===
/*
text Patterned {
                        Ip: pattern '\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}',
                        Separator: literal ':',
                        User: token,
                        _: whitespace+,
                        Message: rest
                    };
                    select p.Ip, p.User, p.Message
                    from #test.lines() f
                    cross apply Parse<Patterned>(f.Line) p
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Line as f.Line, p.Ip as p.Ip, p.User as p.User, p.Message as p.Message]
    Apply [Cross]
      SchemaScan [#test.lines() as f]
      InterpretSource [#Patterned(f.Line) as p]
  Project [p.Ip as Ip, p.User as User, p.Message as Message]
    CteRef [fp as fp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [p.Ip as p.Ip, p.User as p.User, p.Message as p.Message]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.lines() as f]
      PhysicalInterpretSource [#Patterned(f.Line) as p]
  PhysicalProject [p.Ip as Ip, p.User as User, p.Message as Message]
    PhysicalCteRef [fp as fp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: TextEntity]
      Line: string <- property Line
    SourceEntity [p: object]
      Ip: string <- property Ip
      Separator: string <- property Separator
      User: string <- property User
      Message: string <- property Message
    Generated [ResultRow0]
      Ip: string <- field Ip
      User: string <- field User
      Message: string <- field Message

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: TextEntity] -> fRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [Patterned.Parse(f.Line) -> pRows]
      ScalarForEach [p in pRows]
        AppendShape [result <- ResultRow0(Ip: p.Ip, User: p.User, Message: p.Message)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q314_SpecTextPatternsLiteralsTokens
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
            new Column("Ip", typeof(string), 0),
            new Column("User", typeof(string), 1),
            new Column("Message", typeof(string), 2)
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
                    var fRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity>.Open(__fSchema, "lines", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["f:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
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
                                    var _interpreter_Patterned = new Musoq.Generated.Interpreters.Patterned();
                                    var pRows = global::Musoq.Schema.Interpreters.InterpretationStatus.ParseText(_interpreter_Patterned, f.Line);
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        yield return new ResultRow0(p.Ip, p.User, p.Message);
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
                                    var _interpreter_Patterned = new Musoq.Generated.Interpreters.Patterned();
                                    var pRows = global::Musoq.Schema.Interpreters.InterpretationStatus.ParseText(_interpreter_Patterned, f.Line);
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        yield return new ResultRow0(p.Ip, p.User, p.Message);
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
                            var _interpreter_Patterned = new Musoq.Generated.Interpreters.Patterned();
                            var pRows = global::Musoq.Schema.Interpreters.InterpretationStatus.ParseText(_interpreter_Patterned, f.Line);
                            if (pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = pRows;
                                yield return new ResultRow0(p.Ip, p.User, p.Message);
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
            public ResultRow0(string __value0, string __value1, string __value2)
            {
                Ip = __value0;
                User = __value1;
                Message = __value2;
            }

            public override int Count => 3;
            public string Ip { get; private set; }
            public string Message { get; private set; }
            public string User { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Ip = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.User = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Message = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Ip),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.User),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Message),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Ip" => true,
                "User" => true,
                "Message" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Ip, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.User, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Message, ref visitor);
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
    /// Generated interpreter for text schema 'Patterned'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Patterned : TextInterpreterBase<Patterned>, ITextInterpreterStatus<Patterned>, IParserCompletionLayoutSource, global::Musoq.Plugins.IJsonPayload
    {
        private static readonly global::Musoq.Schema.Interpreters.ParserCompletionLayout __musoqCompletionLayout = new(new string[] { "Ip", "Separator", "User", "Message" });
        global::Musoq.Schema.Interpreters.ParserCompletionLayout global::Musoq.Schema.Interpreters.IParserCompletionLayoutSource.CompletionLayout => __musoqCompletionLayout;

        private static readonly global::Musoq.Schema.Interpreters.PreparedTextPattern __musoqPattern0 = new(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}", (global::Musoq.Schema.Interpreters.TextPatternModifiers)0);
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Patterned value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Ip"))
            {
                writer.WritePropertyName("Ip");
                var fieldValue = this.Ip;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Separator"))
            {
                writer.WritePropertyName("Separator");
                var fieldValue = this.Separator;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("User"))
            {
                writer.WritePropertyName("User");
                var fieldValue = this.User;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Message"))
            {
                writer.WritePropertyName("Message");
                var fieldValue = this.Message;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Ip field value.</summary>
        public string? Ip { get; set; }
        /// <summary>Gets the Separator field value.</summary>
        public string? Separator { get; set; }
        /// <summary>Gets the User field value.</summary>
        public string? User { get; set; }
        /// <summary>Gets the Message field value.</summary>
        public string? Message { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Patterned";

        public override Patterned ParseAt(ReadOnlySpan<char> data, int offset)
        {
            var input = new global::Musoq.Schema.Interpreters.TextParseInput(data);
            return ((global::Musoq.Schema.Interpreters.ITextInterpreterStatus<Patterned>)this).ReadAt(ref input, offset).GetValueOrThrow();
        }

        global::Musoq.Schema.Interpreters.ParseAttempt<Patterned> global::Musoq.Schema.Interpreters.ITextInterpreterStatus<Patterned>.ReadAt(ref global::Musoq.Schema.Interpreters.TextParseInput __musoqTextInput, int offset)
        {
            var data = __musoqTextInput.Text;
            Patterned? __parsedValue = null;
            try
            {
                ParsePosition = offset;
                SetCurrentField(null);
                __parsedValue = new Patterned();
                RecordParsedValue(__parsedValue);
                SetCurrentField("Ip");
                var __musoqReadResult0 = base.ReadPreparedPatternStatus(data, __musoqPattern0, "Ip");
                if (!__musoqReadResult0.Succeeded)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Patterned>.Failed(__parsedValue, __musoqReadResult0.Failure!);
                }

                var _ip = __musoqReadResult0.Value!;
                __parsedValue.Ip = _ip;
                base.RecordParsedFieldSlot(0);
                SetCurrentField("Separator");
                var __musoqReadResult1 = base.ExpectLiteralStatus(ref __musoqTextInput, ":", fieldName: "Separator");
                if (!__musoqReadResult1.Succeeded)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Patterned>.Failed(__parsedValue, __musoqReadResult1.Failure!);
                }

                var _separator = __musoqReadResult1.Value!;
                __parsedValue.Separator = _separator;
                base.RecordParsedFieldSlot(1);
                SetCurrentField("User");
                var __musoqReadResult2 = base.ReadTokenStatus(data, fieldName: "User");
                if (!__musoqReadResult2.Succeeded)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Patterned>.Failed(__parsedValue, __musoqReadResult2.Failure!);
                }

                var _user = __musoqReadResult2.Value!;
                __parsedValue.User = _user;
                base.RecordParsedFieldSlot(2);
                SetCurrentField("_");
                if (base.SkipWhitespaceStatus(data, true, fieldName: "_")is { } __musoqReadFailure3)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Patterned>.Failed(__parsedValue, __musoqReadFailure3);
                }

                SetCurrentField("Message");
                var __musoqReadResult4 = base.ReadRestStatus(data, fieldName: "Message");
                if (!__musoqReadResult4.Succeeded)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Patterned>.Failed(__parsedValue, __musoqReadResult4.Failure!);
                }

                var _message = __musoqReadResult4.Value!;
                __parsedValue.Message = _message;
                base.RecordParsedFieldSlot(3);
                return global::Musoq.Schema.Interpreters.ParseAttempt<Patterned>.Success(__parsedValue);
            }
            catch (global::Musoq.Schema.Interpreters.ParseException __parseFailure)
            {
                return global::Musoq.Schema.Interpreters.ParseAttempt<Patterned>.Failed(__parsedValue, global::Musoq.Schema.Interpreters.ParseFailure.FromException(__parseFailure));
            }
        }
    }
}
