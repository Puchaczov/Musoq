// === Parsed Query ===
/*
text KeyValue {
                        Key: until '=',
                        Value: rest
                    };
                    select t.Key, t.Value, p.ErrorField, p.ErrorMessage, p.BytesConsumed
                    from #test.lines() f
                    outer apply TryParse<KeyValue>(f.Line) t
                    cross apply PartialParse<KeyValue>(f.Line) p
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Line as f.Line, t.Key as t.Key, t.Value as t.Value]
    Apply [Outer]
      SchemaScan [#test.lines() as f]
      InterpretSource [#KeyValue(f.Line) as t]
  Project [ft.f.Line as f.Line, ft.t.Key as t.Key, ft.t.Value as t.Value, p.ErrorField as p.ErrorField, p.ErrorMessage as p.ErrorMessage, p.BytesConsumed as p.BytesConsumed]
    Apply [Cross]
      CteRef [ft as ft]
      InterpretSource [#KeyValue(ft.f.Line) as p]
  Project [t.Key as Key, t.Value as Value, p.ErrorField as ErrorField, p.ErrorMessage as ErrorMessage, p.BytesConsumed as BytesConsumed]
    CteRef [ftp as ftp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Line as f.Line, t.Key as t.Key, t.Value as t.Value]
    PhysicalNestedLoopApply [Outer]
      PhysicalSchemaScan [#test.lines() as f]
      PhysicalInterpretSource [#KeyValue(f.Line) as t]
  PhysicalProject [ft.t.Key as t.Key, ft.t.Value as t.Value, p.ErrorField as p.ErrorField, p.ErrorMessage as p.ErrorMessage, p.BytesConsumed as p.BytesConsumed]
    PhysicalNestedLoopApply [Cross]
      PhysicalCteRef [ft as ft]
      PhysicalInterpretSource [#KeyValue(ft.f.Line) as p]
  PhysicalProject [t.Key as Key, t.Value as Value, p.ErrorField as ErrorField, p.ErrorMessage as ErrorMessage, p.BytesConsumed as BytesConsumed]
    PhysicalCteRef [ftp as ftp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: TextEntity]
      Line: string <- property Line
    SourceEntity [t: object]
      Key: string <- property Key
      Value: string <- property Value
    Generated [Statement0Row0]
      f.Line: string <- field f_Line
      t.Key: string <- field t_Key
      t.Value: string <- field t_Value
    TableRow [ft]
      f.Line: string <- field f_Line
      t.Key: string <- field t_Key
      t.Value: string <- field t_Value
    SourceEntity [p: object]
      ParsedFields: object <- property ParsedFields
      ErrorField: string <- property ErrorField
      ErrorMessage: string <- property ErrorMessage
      BytesConsumed: int <- property BytesConsumed
      CompletedFields: IReadOnlySet<string> <- property CompletedFields
      IsSuccess: bool <- property IsSuccess
      BitOffset: int <- property BitOffset
    Generated [ResultRow0]
      Key: string <- field Key
      Value: string <- field Value
      ErrorField: string <- field ErrorField
      ErrorMessage: string <- field ErrorMessage
      BytesConsumed: int <- field BytesConsumed

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [f: TextEntity] -> statement0_fRows
    CreateTable [statement0: Statement0Row0]
    ChunkedForEach [f in statement0_fRows]
      Let [fLine: string = f.Line]
      InterpretSource [KeyValue.TryParse(f.Line) -> statement0_tRows]
      Let [tHasMatch: bool = FALSE]
      ScalarForEach [t in statement0_tRows]
        Assign [tHasMatch = TRUE]
        AppendRow [statement0 <- Statement0Row0(f.Line: fLine, t.Key: t.Key, t.Value: t.Value)]
      If [NOT tHasMatch]
        AppendRow [statement0 <- Statement0Row0(f.Line: fLine, t.Key: NULL, t.Value: NULL)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    ForEach [ft in _cteRowResults.Slot0]
      Let [fttKey: string = ft.t.Key]
      Let [fttValue: string = ft.t.Value]
      InterpretSource [KeyValue.PartialParse(ft.f.Line) -> pRows]
      ScalarForEach [p in pRows]
        AppendShape [result <- ResultRow0(Key: fttKey, Value: fttValue, ErrorField: p.ErrorField, ErrorMessage: p.ErrorMessage, BytesConsumed: p.BytesConsumed)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q318_SpecTextTryParseAndPartialParse
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
            new Column("Key", typeof(string), 0),
            new Column("Value", typeof(string), 1),
            new Column("ErrorField", typeof(string), 2),
            new Column("ErrorMessage", typeof(string), 3),
            new Column("BytesConsumed", typeof(int), 4)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("f.Line", typeof(string), 0),
            new Column("t.Key", typeof(string), 1),
            new Column("t.Value", typeof(string), 2)
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
            var __musoqMaterializedTable = QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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

                        Statement0Row0 ft = __storedTable0Rows[__storedTable0Index];
                        string fttKey = ft.t_Key;
                        string fttValue = ft.t_Value;
                        var _interpreter_KeyValue = new Musoq.Generated.Interpreters.KeyValue();
                        var pRows = _interpreter_KeyValue.PartialParse(ft.f_Line);
                        if (pRows != null)
                        {
                            token.ThrowIfCancellationRequested();
                            var p = pRows;
                            __musoqFinalShapeRows.Add(new ResultRow0(fttKey, fttValue, p.ErrorField, p.ErrorMessage, p.BytesConsumed));
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
            var statement0_fRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity>.Open(__statement0_fSchema, "lines", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_fRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity>(statement0_fRowsSource.Chunks, __musoqProgressContext, "f:1") : statement0_fRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            foreach (var fChunk in statement0_fRows)
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
                            string fLine = f.Line;
                            var _interpreter_KeyValue = new Musoq.Generated.Interpreters.KeyValue();
                            Musoq.Generated.Interpreters.KeyValue? statement0_tRows = null;
                            try
                            {
                                statement0_tRows = _interpreter_KeyValue.Parse(f.Line);
                            }
                            catch (global::Musoq.Schema.Interpreters.ParseException)
                            {
                                statement0_tRows = null;
                            }

                            bool tHasMatch = false;
                            if (statement0_tRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var t = statement0_tRows;
                                tHasMatch = true;
                                statement0.Add(new Statement0Row0(fLine, t.Key, t.Value));
                            }

                            if ((!tHasMatch))
                            {
                                statement0.Add(new Statement0Row0(fLine, null, null));
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
                            string fLine = f.Line;
                            var _interpreter_KeyValue = new Musoq.Generated.Interpreters.KeyValue();
                            Musoq.Generated.Interpreters.KeyValue? statement0_tRows = null;
                            try
                            {
                                statement0_tRows = _interpreter_KeyValue.Parse(f.Line);
                            }
                            catch (global::Musoq.Schema.Interpreters.ParseException)
                            {
                                statement0_tRows = null;
                            }

                            bool tHasMatch = false;
                            if (statement0_tRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var t = statement0_tRows;
                                tHasMatch = true;
                                statement0.Add(new Statement0Row0(fLine, t.Key, t.Value));
                            }

                            if ((!tHasMatch))
                            {
                                statement0.Add(new Statement0Row0(fLine, null, null));
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
                    string fLine = f.Line;
                    var _interpreter_KeyValue = new Musoq.Generated.Interpreters.KeyValue();
                    Musoq.Generated.Interpreters.KeyValue? statement0_tRows = null;
                    try
                    {
                        statement0_tRows = _interpreter_KeyValue.Parse(f.Line);
                    }
                    catch (global::Musoq.Schema.Interpreters.ParseException)
                    {
                        statement0_tRows = null;
                    }

                    bool tHasMatch = false;
                    if (statement0_tRows != null)
                    {
                        token.ThrowIfCancellationRequested();
                        var t = statement0_tRows;
                        tHasMatch = true;
                        statement0.Add(new Statement0Row0(fLine, t.Key, t.Value));
                    }

                    if ((!tHasMatch))
                    {
                        statement0.Add(new Statement0Row0(fLine, null, null));
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
            public ResultRow0(string __value0, string __value1, string __value2, string __value3, int __value4)
            {
                Key = __value0;
                Value = __value1;
                ErrorField = __value2;
                ErrorMessage = __value3;
                BytesConsumed = __value4;
            }

            public int BytesConsumed { get; private set; }
            public override int Count => 5;
            public string ErrorField { get; private set; }
            public string ErrorMessage { get; private set; }
            public string Key { get; private set; }
            public string Value { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Key = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.ErrorField = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.ErrorMessage = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.BytesConsumed = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Key),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Value),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.ErrorField),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.ErrorMessage),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.BytesConsumed),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Key" => true,
                "Value" => true,
                "ErrorField" => true,
                "ErrorMessage" => true,
                "BytesConsumed" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Key, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Value, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.ErrorField, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.ErrorMessage, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.BytesConsumed, ref visitor);
            }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(string __value0, string __value1, string __value2)
            {
                f_Line = __value0;
                t_Key = __value1;
                t_Value = __value2;
            }

            public string f_Line { get; }
            public string t_Key { get; }
            public string t_Value { get; }
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
}
