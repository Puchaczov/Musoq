// === Parsed Query ===
/*
text Entry { Level: until '|', Number: until '|', Label: rest }; select r.Id as Id, p.Level as Level, p.Number::int as Number, Length(p.Label) as LabelLength from perf.rows() r cross apply Parse<Entry>(r.Text) p
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.Id as r.Id, r.Text as r.Text, p.Level as p.Level, p.Number as p.Number, p.Label as p.Label]
    Apply [Cross]
      SchemaScan [#perf.rows() as r]
      InterpretSource [#Entry(r.Text) as p]
  Project [r.Id as Id, p.Level as Level, p.Number::Int32 as Number, Length(p.Label) as LabelLength]
    CteRef [rp as rp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.Id as r.Id, p.Level as p.Level, p.Number as p.Number, p.Label as p.Label]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#perf.rows() as r]
      PhysicalInterpretSource [#Entry(r.Text) as p]
  PhysicalProject [r.Id as Id, p.Level as Level, p.Number::Int32 as Number, Length(p.Label) as LabelLength]
    PhysicalCteRef [rp as rp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: PerformanceRow]
      Id: int <- property Id
      Text: string <- property Text
    SourceEntity [p: object]
      Level: string <- property Level
      Number: string <- property Number
      Label: string <- property Label
    Generated [ResultRow0]
      Id: int <- field Id
      Level: string <- field Level
      Number: int? <- field Number
      LabelLength: int? <- field LabelLength

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [r: PerformanceRow] -> rRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateObject [__resultLibraryBase0: LibraryBase]
    ChunkedForEach [r in rRows]
      Let [rId: int = r.Id]
      InterpretSource [Entry.Parse(r.Text) -> pRows]
      ScalarForEach [p in pRows]
        AppendShape [result <- ResultRow0(Id: rId, Level: p.Level, Number: p.Number::Int32, LabelLength: Length(p.Label))]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B30_Text1KiB_BenchmarkTable
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IContextTableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Text", typeof(string), 1) });
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Level", typeof(string), 1),
            new Column("Number", typeof(int?), 2),
            new Column("LabelLength", typeof(int?), 3)
        };
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public ILogger Logger { get; set; }
        public TypedParameterSnapshot TypedParameters { get; set; } = TypedParameterSnapshot.Empty;
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();

        public event QueryPhaseEventHandler PhaseChanged;
        public event DataSourceEventHandler DataSourceProgress;
        public event QueryProgressEventHandler QueryProgress;
        public Table Run(CancellationToken token)
        {
            var queryContext = new QueryRunContext(token, TypedParameters, PhaseChanged, DataSourceProgress, this, queryId: "compiled", provider: Provider, sourceRuntimeSettingsBySourceContextId: SourceRuntimeSettingsBySourceContextId, sourceRuntimeSettingDescriptionsBySourceContextId: SourceRuntimeSettingDescriptionsBySourceContextId, sourceExecutionPlans: SourceExecutionPlans, logger: Logger, queryProgress: QueryProgress);
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        private IEnumerable<ResultRow0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            var token = queryContext.CancellationToken;
            var __musoqProgressContext = queryContext.QueryProgress == null ? null : queryContext;
            Action<string, QueryPhase> OnPhaseChanged = queryContext.NotifyPhaseChanged;
            DataSourceEventHandler OnDataSourceProgress = queryContext.NotifyDataSourceProgress;
            QueryProgressEventHandler OnQueryProgress = queryContext.QueryProgress;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(queryContext.TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                Musoq.Plugins.LibraryBase __resultLibraryBase0 = default!;
                try
                {
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    var __rSchema = provider.GetSchema("#perf");
                    var rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.PerformanceRow>.Open(__rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                    var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.PerformanceRow>(rRowsSource.Chunks, __musoqProgressContext, "r:1") : rRowsSource.Chunks;
                    __resultLibraryBase0 = new Musoq.Plugins.LibraryBase();
                    foreach (var rChunk in rRows)
                    {
                        if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.PerformanceRow> rChunkView)
                        {
                            if (rChunkView.Source is Musoq.Benchmarks.Scenarios.PerformanceRow[] rChunkViewArray)
                            {
                                int rChunkViewOffset = rChunkView.Offset;
                                for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                                {
                                    if ((rIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var r = rChunkViewArray[rChunkViewOffset + rIndex];
                                    int rId = r.Id;
                                    var _interpreter_Entry = new Musoq.Generated.Interpreters.Entry();
                                    var pRows = global::Musoq.Schema.Interpreters.InterpretationStatus.ParseText(_interpreter_Entry, r.Text);
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        yield return new ResultRow0(rId, p.Level, global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt32(p.Number), (int?)__resultLibraryBase0.Length<char>(p.Label));
                                    }
                                }

                                continue;
                            }

                            if (rChunkView.Source is List<Musoq.Benchmarks.Scenarios.PerformanceRow> rChunkViewList)
                            {
                                int rChunkViewOffset = rChunkView.Offset;
                                for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                                {
                                    if ((rIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var r = rChunkViewList[rChunkViewOffset + rIndex];
                                    int rId = r.Id;
                                    var _interpreter_Entry = new Musoq.Generated.Interpreters.Entry();
                                    var pRows = global::Musoq.Schema.Interpreters.InterpretationStatus.ParseText(_interpreter_Entry, r.Text);
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        yield return new ResultRow0(rId, p.Level, global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt32(p.Number), (int?)__resultLibraryBase0.Length<char>(p.Label));
                                    }
                                }

                                continue;
                            }
                        }

                        for (int rIndex = 0, rIndexCount = rChunk.Count; rIndex < rIndexCount; ++rIndex)
                        {
                            if ((rIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var r = rChunk[rIndex];
                            int rId = r.Id;
                            var _interpreter_Entry = new Musoq.Generated.Interpreters.Entry();
                            var pRows = global::Musoq.Schema.Interpreters.InterpretationStatus.ParseText(_interpreter_Entry, r.Text);
                            if (pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = pRows;
                                yield return new ResultRow0(rId, p.Level, global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt32(p.Number), (int?)__resultLibraryBase0.Length<char>(p.Label));
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

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, queryContext))
            {
                yield return __musoqShapeRow;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnPhaseChanged(string queryId, QueryPhase phase)
        {
            PhaseChanged?.Invoke(this, new QueryPhaseEventArgs(queryId, phase));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnDataSourceProgress(object sender, DataSourceEventArgs e)
        {
            DataSourceProgress?.Invoke(this, e);
        }

        private sealed class ResultRow0 : Row
        {
            public int Id { get; private set; }
            public string Level { get; private set; }
            public int? Number { get; private set; }
            public int? LabelLength { get; private set; }

            public ResultRow0(int __value0, string __value1, int? __value2, int? __value3)
            {
                Id = __value0;
                Level = __value1;
                Number = __value2;
                LabelLength = __value3;
            }

            public override int Count => 4;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Level, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Number, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.LabelLength, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Level),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Number),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.LabelLength),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Level" => true,
                "Number" => true,
                "LabelLength" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Level = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Number = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.LabelLength = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
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
    /// Generated interpreter for text schema 'Entry'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Entry : TextInterpreterBase<Entry>, ITextInterpreterStatus<Entry>, IParserCompletionLayoutSource, global::Musoq.Plugins.IJsonPayload
    {
        private static readonly global::Musoq.Schema.Interpreters.ParserCompletionLayout __musoqCompletionLayout = new(new string[] { "Level", "Number", "Label" });
        global::Musoq.Schema.Interpreters.ParserCompletionLayout global::Musoq.Schema.Interpreters.IParserCompletionLayoutSource.CompletionLayout => __musoqCompletionLayout;

        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Entry value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Level"))
            {
                writer.WritePropertyName("Level");
                var fieldValue = this.Level;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Number"))
            {
                writer.WritePropertyName("Number");
                var fieldValue = this.Number;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Label"))
            {
                writer.WritePropertyName("Label");
                var fieldValue = this.Label;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Level field value.</summary>
        public string? Level { get; set; }
        /// <summary>Gets the Number field value.</summary>
        public string? Number { get; set; }
        /// <summary>Gets the Label field value.</summary>
        public string? Label { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Entry";

        public override Entry ParseAt(ReadOnlySpan<char> data, int offset)
        {
            var input = new global::Musoq.Schema.Interpreters.TextParseInput(data);
            return ((global::Musoq.Schema.Interpreters.ITextInterpreterStatus<Entry>)this).ReadAt(ref input, offset).GetValueOrThrow();
        }

        global::Musoq.Schema.Interpreters.ParseAttempt<Entry> global::Musoq.Schema.Interpreters.ITextInterpreterStatus<Entry>.ReadAt(ref global::Musoq.Schema.Interpreters.TextParseInput __musoqTextInput, int offset)
        {
            var data = __musoqTextInput.Text;
            Entry? __parsedValue = null;
            try
            {
                ParsePosition = offset;
                SetCurrentField(null);
                __parsedValue = new Entry();
                RecordParsedValue(__parsedValue);
                SetCurrentField("Level");
                string _level;
                var __musoqRemainingText2 = (uint)ParsePosition <= (uint)data.Length ? data[ParsePosition..] : default;
                var __musoqDelimiterIndex3 = __musoqRemainingText2.IndexOf('\u007C');
                if (__musoqDelimiterIndex3 >= 0)
                {
                    _level = __musoqRemainingText2[..__musoqDelimiterIndex3].ToString();
                    ParsePosition += __musoqDelimiterIndex3 + 1;
                }
                else
                {
                    var __musoqReadResult1 = base.ReadUntilStatus(data, "|", fieldName: "Level");
                    if (!__musoqReadResult1.Succeeded)
                    {
                        return global::Musoq.Schema.Interpreters.ParseAttempt<Entry>.Failed(__parsedValue, __musoqReadResult1.Failure!);
                    }

                    var __musoqDelimitedFallback0 = __musoqReadResult1.Value!;
                    _level = __musoqDelimitedFallback0;
                }

                __parsedValue.Level = _level;
                base.RecordParsedFieldSlot(0);
                SetCurrentField("Number");
                string _number;
                var __musoqRemainingText6 = (uint)ParsePosition <= (uint)data.Length ? data[ParsePosition..] : default;
                var __musoqDelimiterIndex7 = __musoqRemainingText6.IndexOf('\u007C');
                if (__musoqDelimiterIndex7 >= 0)
                {
                    _number = __musoqRemainingText6[..__musoqDelimiterIndex7].ToString();
                    ParsePosition += __musoqDelimiterIndex7 + 1;
                }
                else
                {
                    var __musoqReadResult5 = base.ReadUntilStatus(data, "|", fieldName: "Number");
                    if (!__musoqReadResult5.Succeeded)
                    {
                        return global::Musoq.Schema.Interpreters.ParseAttempt<Entry>.Failed(__parsedValue, __musoqReadResult5.Failure!);
                    }

                    var __musoqDelimitedFallback4 = __musoqReadResult5.Value!;
                    _number = __musoqDelimitedFallback4;
                }

                __parsedValue.Number = _number;
                base.RecordParsedFieldSlot(1);
                SetCurrentField("Label");
                string _label;
                if ((uint)ParsePosition <= (uint)data.Length)
                {
                    _label = data[ParsePosition..].ToString();
                    ParsePosition = data.Length;
                }
                else
                {
                    var __musoqReadResult9 = base.ReadRestStatus(data, fieldName: "Label");
                    if (!__musoqReadResult9.Succeeded)
                    {
                        return global::Musoq.Schema.Interpreters.ParseAttempt<Entry>.Failed(__parsedValue, __musoqReadResult9.Failure!);
                    }

                    var __musoqDelimitedFallback8 = __musoqReadResult9.Value!;
                    _label = __musoqDelimitedFallback8;
                }

                __parsedValue.Label = _label;
                base.RecordParsedFieldSlot(2);
                return global::Musoq.Schema.Interpreters.ParseAttempt<Entry>.Success(__parsedValue);
            }
            catch (global::Musoq.Schema.Interpreters.ParseException __parseFailure)
            {
                return global::Musoq.Schema.Interpreters.ParseAttempt<Entry>.Failed(__parsedValue, global::Musoq.Schema.Interpreters.ParseFailure.FromException(__parseFailure));
            }
        }
    }
}
