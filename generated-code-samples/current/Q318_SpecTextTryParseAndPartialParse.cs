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
  Project [t.Key as t.Key, t.Value as t.Value, p.ErrorField as p.ErrorField, p.ErrorMessage as p.ErrorMessage, p.BytesConsumed as p.BytesConsumed]
    CteRef [ftp as ftp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Line as f.Line, t.Key as t.Key, t.Value as t.Value]
    PhysicalNestedLoopApply [Outer]
      PhysicalSchemaScan [#test.lines() as f]
      PhysicalInterpretSource [#KeyValue(f.Line) as t]
  PhysicalProject [ft.f.Line as f.Line, ft.t.Key as t.Key, ft.t.Value as t.Value, p.ErrorField as p.ErrorField, p.ErrorMessage as p.ErrorMessage, p.BytesConsumed as p.BytesConsumed]
    PhysicalNestedLoopApply [Cross]
      PhysicalCteRef [ft as ft]
      PhysicalInterpretSource [#KeyValue(ft.f.Line) as p]
  PhysicalProject [t.Key as t.Key, t.Value as t.Value, p.ErrorField as p.ErrorField, p.ErrorMessage as p.ErrorMessage, p.BytesConsumed as p.BytesConsumed]
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
      ParsedFields: Dictionary<string, object> <- property ParsedFields
      ErrorField: string <- property ErrorField
      ErrorMessage: string <- property ErrorMessage
      BytesConsumed: int <- property BytesConsumed
    Generated [ResultRow0]
      t.Key: string <- field t_Key
      t.Value: string <- field t_Value
      p.ErrorField: string <- field p_ErrorField
      p.ErrorMessage: string <- field p_ErrorMessage
      p.BytesConsumed: int <- field p_BytesConsumed

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
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [ft in _cteRowResults.Slot0]
      Let [fttKey: string = ft.t.Key]
      Let [fttValue: string = ft.t.Value]
      InterpretSource [KeyValue.PartialParse(ft.f.Line) -> pRows]
      ChunkedForEach [p in pRows]
        AppendShape [result <- ResultShape0(t.Key: fttKey, t.Value: fttValue, p.ErrorField: p.ErrorField, p.ErrorMessage: p.ErrorMessage, p.BytesConsumed: p.BytesConsumed)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("t.Key", typeof(string), 0),
            new Column("t.Value", typeof(string), 1),
            new Column("p.ErrorField", typeof(string), 2),
            new Column("p.ErrorMessage", typeof(string), 3),
            new Column("p.BytesConsumed", typeof(int), 4)
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
            var __musoqMaterializedTable = QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
            _ = __musoqMaterializedTable.Count;
            return __musoqMaterializedTable;
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.t_Key, __musoqShapeRow.t_Value, __musoqShapeRow.p_ErrorField, __musoqShapeRow.p_ErrorMessage, __musoqShapeRow.p_BytesConsumed);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
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
                        var pRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Schema.Interpreters.PartialInterpretResult<Musoq.Generated.Interpreters.KeyValue>>(EvaluationHelper.WrapScalarForCrossApply<Musoq.Schema.Interpreters.PartialInterpretResult<Musoq.Generated.Interpreters.KeyValue>>(_interpreter_KeyValue.PartialParse(ft.f_Line)));
                        foreach (var pChunk in pRows)
                        {
                            for (int pIndex = 0, pIndexCount = pChunk.Count; pIndex < pIndexCount; ++pIndex)
                            {
                                if ((pIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var p = pChunk[pIndex];
                                __musoqFinalShapeRows.Add(new ResultShape0(fttKey, fttValue, p.ErrorField, p.ErrorMessage, p.BytesConsumed));
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
            var statement0_fRowsSource = __statement0_fSchema.GetRowSource<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity>("lines", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), Array.Empty<object>());
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
                            var statement0_tRows = new Func<Musoq.Generated.Interpreters.KeyValue?>(() =>
                            {
                                try
                                {
                                    return _interpreter_KeyValue.Parse(f.Line);
                                }
                                catch
                                {
                                    return null;
                                }
                            })();
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
                            var statement0_tRows = new Func<Musoq.Generated.Interpreters.KeyValue?>(() =>
                            {
                                try
                                {
                                    return _interpreter_KeyValue.Parse(f.Line);
                                }
                                catch
                                {
                                    return null;
                                }
                            })();
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
                    var statement0_tRows = new Func<Musoq.Generated.Interpreters.KeyValue?>(() =>
                    {
                        try
                        {
                            return _interpreter_KeyValue.Parse(f.Line);
                        }
                        catch
                        {
                            return null;
                        }
                    })();
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
                t_Key = __value0;
                t_Value = __value1;
                p_ErrorField = __value2;
                p_ErrorMessage = __value3;
                p_BytesConsumed = __value4;
            }

            public override int Count => 5;
            public int p_BytesConsumed { get; private set; }
            public string p_ErrorField { get; private set; }
            public string p_ErrorMessage { get; private set; }
            public string t_Key { get; private set; }
            public string t_Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        t_Key = (string)value;
                        break;
                    case 1:
                        t_Value = (string)value;
                        break;
                    case 2:
                        p_ErrorField = (string)value;
                        break;
                    case 3:
                        p_ErrorMessage = (string)value;
                        break;
                    case 4:
                        p_BytesConsumed = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "t.Key" => true,
                "t_Key" => true,
                "Key" => true,
                "t.Value" => true,
                "t_Value" => true,
                "Value" => true,
                "p.ErrorField" => true,
                "p_ErrorField" => true,
                "ErrorField" => true,
                "p.ErrorMessage" => true,
                "p_ErrorMessage" => true,
                "ErrorMessage" => true,
                "p.BytesConsumed" => true,
                "p_BytesConsumed" => true,
                "BytesConsumed" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)t_Key,
                1 => (object)t_Value,
                2 => (object)p_ErrorField,
                3 => (object)p_ErrorMessage,
                4 => (object)p_BytesConsumed,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "t.Key" => (object)t_Key,
                "t_Key" => (object)t_Key,
                "Key" => (object)t_Key,
                "t.Value" => (object)t_Value,
                "t_Value" => (object)t_Value,
                "Value" => (object)t_Value,
                "p.ErrorField" => (object)p_ErrorField,
                "p_ErrorField" => (object)p_ErrorField,
                "ErrorField" => (object)p_ErrorField,
                "p.ErrorMessage" => (object)p_ErrorMessage,
                "p_ErrorMessage" => (object)p_ErrorMessage,
                "ErrorMessage" => (object)p_ErrorMessage,
                "p.BytesConsumed" => (object)p_BytesConsumed,
                "p_BytesConsumed" => (object)p_BytesConsumed,
                "BytesConsumed" => (object)p_BytesConsumed,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string t_Key, string t_Value, string p_ErrorField, string p_ErrorMessage, int p_BytesConsumed)
            {
                this.t_Key = t_Key;
                this.t_Value = t_Value;
                this.p_ErrorField = p_ErrorField;
                this.p_ErrorMessage = p_ErrorMessage;
                this.p_BytesConsumed = p_BytesConsumed;
            }

            public int p_BytesConsumed { get; }
            public string p_ErrorField { get; }
            public string p_ErrorMessage { get; }
            public string t_Key { get; }
            public string t_Value { get; }
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
}
