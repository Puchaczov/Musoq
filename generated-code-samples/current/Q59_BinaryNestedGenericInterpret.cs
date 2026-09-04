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
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [fc in _cteRowResults.Slot0]
      EnumerableSource [fc.c.Items.Data -> pRows]
      ChunkedForEach [p in pRows]
        AppendShape [result <- ResultShape0(LeftValue: p.LeftItem.Value, RightValue: p.RightItem.Value)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("LeftValue", typeof(byte), 0),
            new Column("RightValue", typeof(short), 1)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("f.Content", typeof(byte[]), 0),
            new Column("c.Items", typeof(object), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_f_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Content", typeof(byte[]), 0) });
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
                yield return new ResultRow0(__musoqShapeRow.LeftValue, __musoqShapeRow.RightValue);
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
                                __musoqFinalShapeRows.Add(new ResultShape0(((Musoq.Generated.Interpreters.ByteValue)p.LeftItem).Value, ((Musoq.Generated.Interpreters.ShortValue)p.RightItem).Value));
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
            var statement0_fRowsSource = __statement0_fSchema.GetRowSource<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>("files", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), Array.Empty<object>());
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

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        LeftValue = (byte)value;
                        break;
                    case 1:
                        RightValue = (short)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "LeftValue" => true,
                "RightValue" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)LeftValue,
                1 => (object)RightValue,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "LeftValue" => (object)LeftValue,
                "RightValue" => (object)RightValue,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(byte LeftValue, short RightValue)
            {
                this.LeftValue = LeftValue;
                this.RightValue = RightValue;
            }

            public byte LeftValue { get; }
            public short RightValue { get; }
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
    public sealed class ByteValue : BytesInterpreterBase<ByteValue>
    {
        /// <summary>Gets the Value field value.</summary>
        public byte Value { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "ByteValue";

        /// <inheritdoc/>
        public override ByteValue InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("Value");
            var _value = ReadByte(data);
            RecordParsedField("Value", _value);
            return new ByteValue
            {
                Value = _value
            };
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'ShortValue'.
    /// </summary>
    public sealed class ShortValue : BytesInterpreterBase<ShortValue>
    {
        /// <summary>Gets the Value field value.</summary>
        public short Value { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "ShortValue";

        /// <inheritdoc/>
        public override ShortValue InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("Value");
            var _value = ReadInt16Le(data);
            RecordParsedField("Value", _value);
            return new ShortValue
            {
                Value = _value
            };
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'Pair'.
    /// This is a generic schema with type parameters: T, U.
    /// </summary>
    public sealed class Pair<T, U> : BytesInterpreterBase<Pair<T, U>> where T : IBytesInterpreter<T>, new()
        where U : IBytesInterpreter<U>, new()
    {
        /// <summary>Gets the LeftItem field value.</summary>
        public T LeftItem { get; init; }
        /// <summary>Gets the RightItem field value.</summary>
        public U RightItem { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Pair";

        /// <inheritdoc/>
        public override Pair<T, U> InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("LeftItem");
            var __leftItem_interpreter = new T();
            var _leftItem = InterpretNested(__leftItem_interpreter, data, "LeftItem");
            RecordParsedField("LeftItem", _leftItem);
            SetCurrentField("RightItem");
            var __rightItem_interpreter = new U();
            var _rightItem = InterpretNested(__rightItem_interpreter, data, "RightItem");
            RecordParsedField("RightItem", _rightItem);
            return new Pair<T, U>
            {
                LeftItem = _leftItem,
                RightItem = _rightItem
            };
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'LengthPrefixed'.
    /// This is a generic schema with type parameters: T.
    /// </summary>
    public sealed class LengthPrefixed<T> : BytesInterpreterBase<LengthPrefixed<T>> where T : IBytesInterpreter<T>, new()
    {
        /// <summary>Gets the Count field value.</summary>
        public byte Count { get; init; }
        /// <summary>Gets the Data field value.</summary>
        public T[] Data { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "LengthPrefixed";

        /// <inheritdoc/>
        public override LengthPrefixed<T> InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("Count");
            var _count = ReadByte(data);
            RecordParsedField("Count", _count);
            SetCurrentField("Data");
            var __data_list = new System.Collections.Generic.List<T>();
            for (int __data_i = 0; __data_i < (int)_count; __data_i++)
            {
                var __data_elemInterpreter = new T();
                var _elem = InterpretNested(__data_elemInterpreter, data, "Data");
                __data_list.Add(_elem);
            }

            var _data = __data_list.ToArray();
            RecordParsedField("Data", _data);
            return new LengthPrefixed<T>
            {
                Count = _count,
                Data = _data
            };
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'NestedGenericContainer'.
    /// </summary>
    public sealed class NestedGenericContainer : BytesInterpreterBase<NestedGenericContainer>
    {
        /// <summary>Gets the Items field value.</summary>
        public LengthPrefixed<Pair<ByteValue, ShortValue>> Items { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "NestedGenericContainer";

        /// <inheritdoc/>
        public override NestedGenericContainer InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("Items");
            var __items_interpreter = new LengthPrefixed<Pair<ByteValue, ShortValue>>();
            var _items = InterpretNested(__items_interpreter, data, "Items");
            RecordParsedField("Items", _items);
            return new NestedGenericContainer
            {
                Items = _items
            };
        }
    }
}
