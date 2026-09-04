// === Parsed Query ===
/*
text Item { Value: token };
                    text Document {
                        Prefix: token,
                        _: whitespace*,
                        OptionalCode: optional pattern '[0-9]+',
                        _: whitespace?,
                        Items: repeat Item until end,
                        Tail: rest trim
                    };
                    select d.Prefix, d.OptionalCode, i.Value, d.Tail
                    from #test.lines() f
                    cross apply Parse<Document>(f.Line) d
                    cross apply d.Items i
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Line as f.Line, d.Prefix as d.Prefix, d.OptionalCode as d.OptionalCode, d.Items as d.Items, d.Tail as d.Tail]
    Apply [Cross]
      SchemaScan [#test.lines() as f]
      InterpretSource [#Document(f.Line) as d]
  Project [fd.f.Line as f.Line, fd.d.Prefix as d.Prefix, fd.d.OptionalCode as d.OptionalCode, fd.d.Items as d.Items, fd.d.Tail as d.Tail, i.Value as i.Value]
    Apply [Cross]
      CteRef [fd as fd]
      PropertySource [fd.d.Items as i] [apply: Cross] [type: Object[]]
  Project [d.Prefix as d.Prefix, d.OptionalCode as d.OptionalCode, i.Value as i.Value, d.Tail as d.Tail]
    CteRef [fdi as fdi]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Line as f.Line, d.Prefix as d.Prefix, d.OptionalCode as d.OptionalCode, d.Items as d.Items, d.Tail as d.Tail]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.lines() as f]
      PhysicalInterpretSource [#Document(f.Line) as d]
  PhysicalProject [fd.f.Line as f.Line, fd.d.Prefix as d.Prefix, fd.d.OptionalCode as d.OptionalCode, fd.d.Items as d.Items, fd.d.Tail as d.Tail, i.Value as i.Value]
    PhysicalNestedLoopApply [Cross]
      PhysicalCteRef [fd as fd]
      PhysicalPropertySource [fd.d.Items as i] [apply: Cross] [type: Object[]]
  PhysicalProject [d.Prefix as d.Prefix, d.OptionalCode as d.OptionalCode, i.Value as i.Value, d.Tail as d.Tail]
    PhysicalCteRef [fdi as fdi]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: TextEntity]
      Line: string <- property Line
    SourceEntity [d: object]
      Prefix: string <- property Prefix
      OptionalCode: string <- property OptionalCode
      Items: object[] <- property Items
      Tail: string <- property Tail
    Generated [Statement0Row0]
      f.Line: string <- field f_Line
      d.Prefix: string <- field d_Prefix
      d.OptionalCode: string <- field d_OptionalCode
      d.Items: object[] <- field d_Items
      d.Tail: string <- field d_Tail
    TableRow [fd]
      f.Line: string <- field f_Line
      d.Prefix: string <- field d_Prefix
      d.OptionalCode: string <- field d_OptionalCode
      d.Items: object[] <- field d_Items
      d.Tail: string <- field d_Tail
    SourceEntity [i: object]
      Value: string <- property Value
    Generated [ResultRow0]
      d.Prefix: string <- field d_Prefix
      d.OptionalCode: string <- field d_OptionalCode
      i.Value: string <- field i_Value
      d.Tail: string <- field d_Tail

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [f: TextEntity] -> statement0_fRows
    CreateTable [statement0: Statement0Row0]
    ChunkedForEach [f in statement0_fRows]
      Let [fLine: string = f.Line]
      InterpretSource [Document.Parse(f.Line) -> statement0_dRows]
      ScalarForEach [d in statement0_dRows]
        AppendRow [statement0 <- Statement0Row0(f.Line: fLine, d.Prefix: d.Prefix, d.OptionalCode: d.OptionalCode, d.Items: d.Items, d.Tail: d.Tail)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [fd in _cteRowResults.Slot0]
      Let [fddOptionalCode: string = fd.d.OptionalCode]
      Let [fddPrefix: string = fd.d.Prefix]
      Let [fddTail: string = fd.d.Tail]
      EnumerableSource [fd.d.Items -> iRows]
      ChunkedForEach [i in iRows]
        AppendShape [result <- ResultShape0(d.Prefix: fddPrefix, d.OptionalCode: fddOptionalCode, i.Value: i.Value, d.Tail: fddTail)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q316_SpecTextOptionalRepeatWhitespaceModifiers
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
            new Column("d.Prefix", typeof(string), 0),
            new Column("d.OptionalCode", typeof(string), 1),
            new Column("i.Value", typeof(string), 2),
            new Column("d.Tail", typeof(string), 3)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("f.Line", typeof(string), 0),
            new Column("d.Prefix", typeof(string), 1),
            new Column("d.OptionalCode", typeof(string), 2),
            new Column("d.Items", typeof(object[]), 3),
            new Column("d.Tail", typeof(string), 4)
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.d_Prefix, __musoqShapeRow.d_OptionalCode, __musoqShapeRow.i_Value, __musoqShapeRow.d_Tail);
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

                        Statement0Row0 fd = __storedTable0Rows[__storedTable0Index];
                        string fddOptionalCode = fd.d_OptionalCode;
                        string fddPrefix = fd.d_Prefix;
                        string fddTail = fd.d_Tail;
                        var iRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Generated.Interpreters.Item>(fd.d_Items);
                        foreach (var iChunk in iRows)
                        {
                            for (int iIndex = 0, iIndexCount = iChunk.Count; iIndex < iIndexCount; ++iIndex)
                            {
                                if ((iIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var i = iChunk[iIndex];
                                __musoqFinalShapeRows.Add(new ResultShape0(fddPrefix, fddOptionalCode, i.Value, fddTail));
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
                            var _interpreter_Document = new Musoq.Generated.Interpreters.Document();
                            var statement0_dRows = _interpreter_Document.Parse(f.Line);
                            if (statement0_dRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var d = statement0_dRows;
                                statement0.Add(new Statement0Row0(fLine, d.Prefix, d.OptionalCode, d.Items, d.Tail));
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
                            var _interpreter_Document = new Musoq.Generated.Interpreters.Document();
                            var statement0_dRows = _interpreter_Document.Parse(f.Line);
                            if (statement0_dRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var d = statement0_dRows;
                                statement0.Add(new Statement0Row0(fLine, d.Prefix, d.OptionalCode, d.Items, d.Tail));
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
                    var _interpreter_Document = new Musoq.Generated.Interpreters.Document();
                    var statement0_dRows = _interpreter_Document.Parse(f.Line);
                    if (statement0_dRows != null)
                    {
                        token.ThrowIfCancellationRequested();
                        var d = statement0_dRows;
                        statement0.Add(new Statement0Row0(fLine, d.Prefix, d.OptionalCode, d.Items, d.Tail));
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
            public ResultRow0(string __value0, string __value1, string __value2, string __value3)
            {
                d_Prefix = __value0;
                d_OptionalCode = __value1;
                i_Value = __value2;
                d_Tail = __value3;
            }

            public override int Count => 4;
            public string d_OptionalCode { get; private set; }
            public string d_Prefix { get; private set; }
            public string d_Tail { get; private set; }
            public string i_Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        d_Prefix = (string)value;
                        break;
                    case 1:
                        d_OptionalCode = (string)value;
                        break;
                    case 2:
                        i_Value = (string)value;
                        break;
                    case 3:
                        d_Tail = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "d.Prefix" => true,
                "d_Prefix" => true,
                "Prefix" => true,
                "d.OptionalCode" => true,
                "d_OptionalCode" => true,
                "OptionalCode" => true,
                "i.Value" => true,
                "i_Value" => true,
                "Value" => true,
                "d.Tail" => true,
                "d_Tail" => true,
                "Tail" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)d_Prefix,
                1 => (object)d_OptionalCode,
                2 => (object)i_Value,
                3 => (object)d_Tail,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "d.Prefix" => (object)d_Prefix,
                "d_Prefix" => (object)d_Prefix,
                "Prefix" => (object)d_Prefix,
                "d.OptionalCode" => (object)d_OptionalCode,
                "d_OptionalCode" => (object)d_OptionalCode,
                "OptionalCode" => (object)d_OptionalCode,
                "i.Value" => (object)i_Value,
                "i_Value" => (object)i_Value,
                "Value" => (object)i_Value,
                "d.Tail" => (object)d_Tail,
                "d_Tail" => (object)d_Tail,
                "Tail" => (object)d_Tail,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string d_Prefix, string d_OptionalCode, string i_Value, string d_Tail)
            {
                this.d_Prefix = d_Prefix;
                this.d_OptionalCode = d_OptionalCode;
                this.i_Value = i_Value;
                this.d_Tail = d_Tail;
            }

            public string d_OptionalCode { get; }
            public string d_Prefix { get; }
            public string d_Tail { get; }
            public string i_Value { get; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(string __value0, string __value1, string __value2, Musoq.Generated.Interpreters.Item[] __value3, string __value4)
            {
                f_Line = __value0;
                d_Prefix = __value1;
                d_OptionalCode = __value2;
                d_Items = __value3;
                d_Tail = __value4;
            }

            public Musoq.Generated.Interpreters.Item[] d_Items { get; }
            public string d_OptionalCode { get; }
            public string d_Prefix { get; }
            public string d_Tail { get; }
            public string f_Line { get; }
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
    /// Generated interpreter for text schema 'Item'.
    /// </summary>
    public sealed class Item : TextInterpreterBase<Item>
    {
        /// <summary>Gets the Value field value.</summary>
        public string? Value { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Item";

        /// <inheritdoc/>
        public override Item ParseAt(ReadOnlySpan<char> data, int offset)
        {
            ParsePosition = offset;
            SetCurrentField(null);
            SetCurrentField("Value");
            var _value = ReadToken(data, fieldName: "Value");
            RecordParsedField("Value", _value);
            return new Item
            {
                Value = _value
            };
        }
    }

    /// <summary>
    /// Generated interpreter for text schema 'Document'.
    /// </summary>
    public sealed class Document : TextInterpreterBase<Document>
    {
        /// <summary>Gets the Prefix field value.</summary>
        public string? Prefix { get; init; }
        /// <summary>Gets the OptionalCode field value.</summary>
        public string? OptionalCode { get; init; }
        /// <summary>Gets the Items field value.</summary>
        public Item[]? Items { get; init; }
        /// <summary>Gets the Tail field value.</summary>
        public string? Tail { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Document";

        /// <inheritdoc/>
        public override Document ParseAt(ReadOnlySpan<char> data, int offset)
        {
            ParsePosition = offset;
            SetCurrentField(null);
            SetCurrentField("Prefix");
            var _prefix = ReadToken(data, fieldName: "Prefix");
            RecordParsedField("Prefix", _prefix);
            SetCurrentField("_");
            SkipWhitespace(data, false, fieldName: "_");
            SetCurrentField("OptionalCode");
            string? _optionalCode = null;
            var _savedPos__optionalCode = ParsePosition;
            try
            {
                var _temp__optionalCode = ReadPattern(data, @"[0-9]+", fieldName: "OptionalCode");
                _optionalCode = _temp__optionalCode;
            }
            catch (Musoq.Schema.Interpreters.ParseException)
            {
                ParsePosition = _savedPos__optionalCode;
            }

            RecordParsedField("OptionalCode", _optionalCode);
            SetCurrentField("_");
            SkipOptionalWhitespace(data, fieldName: "_");
            SetCurrentField("Items");
            var _list__items = new System.Collections.Generic.List<Item>();
            var _interp_Item = new Item();
            var _iteration__items = 0;
            while (!IsAtEnd(data))
            {
                EnsureRepeatIteration("Items", _iteration__items++);
                var _startPos__items = ParsePosition;
                var _item__items = ParseNested(_interp_Item, data, "Items");
                EnsureRepeatMadeProgress("Items", _startPos__items);
                _list__items.Add(_item__items);
            }

            var _items = _list__items.ToArray();
            RecordParsedField("Items", _items);
            SetCurrentField("Tail");
            var _tail = ReadRest(data, trim: true, fieldName: "Tail");
            RecordParsedField("Tail", _tail);
            return new Document
            {
                Prefix = _prefix,
                OptionalCode = _optionalCode,
                Items = _items,
                Tail = _tail
            };
        }
    }
}
