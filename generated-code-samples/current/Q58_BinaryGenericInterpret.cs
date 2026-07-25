// === Parsed Query ===
/*
binary GenericItem {
                Value: byte
            };
            binary LengthPrefixed<T> {
                Count: byte,
                Data: T[Count]
            };
            binary GenericContainer {
                Items: LengthPrefixed<GenericItem>
            };
            select
                d.Value as ItemValue
            from #test.files() f
            cross apply Interpret<GenericContainer>(f.Content) c
            cross apply c.Items.Data d
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, c.Items as c.Items]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#GenericContainer(f.Content) as c]
  Project [fc.f.Content as f.Content, fc.c.Items as c.Items, d.Value as d.Value]
    Apply [Cross]
      CteRef [fc as fc]
      PropertySource [fc.c.Items.Data as d] [apply: Cross] [type: Object[]]
  Project [d.Value as ItemValue]
    CteRef [fcd as fcd]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, c.Items as c.Items]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#GenericContainer(f.Content) as c]
  PhysicalProject [fc.f.Content as f.Content, fc.c.Items as c.Items, d.Value as d.Value]
    PhysicalNestedLoopApply [Cross]
      PhysicalCteRef [fc as fc]
      PhysicalPropertySource [fc.c.Items.Data as d] [apply: Cross] [type: Object[]]
  PhysicalProject [d.Value as ItemValue]
    PhysicalCteRef [fcd as fcd]
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
    SourceEntity [d: object]
      Value: byte <- property Value
    Generated [ResultRow0]
      ItemValue: byte <- field ItemValue

  Body
    SourceScan [f: BinaryEntity] -> statement0_fRows
    CreateTable [statement0: Statement0Row0]
    ChunkedForEach [f in statement0_fRows]
      InterpretSource [GenericContainer.Interpret(f.Content) -> statement0_cRows]
      ScalarForEach [c in statement0_cRows]
        AppendRow [statement0 <- Statement0Row0(f.Content: f.Content, c.Items: c.Items)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CtePhase [cte1]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [fc in _cteRowResults.Slot0]
      EnumerableSource [fc.c.Items.Data -> dRows]
      ChunkedForEach [d in dRows]
        AppendShape [result <- ResultShape0(ItemValue: d.Value)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q58_BinaryGenericInterpret
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("ItemValue", typeof(byte), 0)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("f.Content", typeof(byte[]), 0),
            new Column("c.Items", typeof(object), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_f_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Content", typeof(byte[]), 1) });
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
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.ItemValue);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 fc = __storedTable0Rows[__storedTable0Index];
                    var dRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Generated.Interpreters.GenericItem>((Musoq.Generated.Interpreters.GenericItem[])(object[])EvaluationHelper.GetNestedValue(fc[1], "Data"));
                    foreach (var dChunk in dRows)
                    {
                        for (int dIndex = 0, dIndexCount = dChunk.Count; dIndex < dIndexCount; ++dIndex)
                        {
                            if ((dIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var d = dChunk[dIndex];
                            __musoqFinalShapeRows.Add(new ResultShape0(d.Value));
                        }
                    }
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
                OnPhaseChanged("compiled", QueryPhase.End);
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
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, CteRowResults _cteRowResults)
        {
            var __statement0_fSchema = provider.GetSchema("#test");
            var statement0_fRowsSource = __statement0_fSchema.GetRowSource<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>("files", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var statement0_fRows = statement0_fRowsSource.Chunks;
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
                            var _interpreter_GenericContainer = new Musoq.Generated.Interpreters.GenericContainer();
                            var statement0_cRows = _interpreter_GenericContainer.Interpret(f.Content);
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
                            var _interpreter_GenericContainer = new Musoq.Generated.Interpreters.GenericContainer();
                            var statement0_cRows = _interpreter_GenericContainer.Interpret(f.Content);
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
                    var _interpreter_GenericContainer = new Musoq.Generated.Interpreters.GenericContainer();
                    var statement0_cRows = _interpreter_GenericContainer.Interpret(f.Content);
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
            public ResultRow0(byte __value0)
            {
                ItemValue = __value0;
            }

            public override int Count => 1;
            public byte ItemValue { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        ItemValue = (byte)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "ItemValue" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)ItemValue,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "ItemValue" => (object)ItemValue,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(byte ItemValue)
            {
                this.ItemValue = ItemValue;
            }

            public byte ItemValue { get; }
        }

        private sealed class Statement0Row0 : Row
        {
            public Statement0Row0(byte[] __value0, object __value1)
            {
                f_Content = __value0;
                c_Items = __value1;
            }

            public override int Count => 2;
            public object c_Items { get; private set; }
            public byte[] f_Content { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        f_Content = (byte[])value;
                        break;
                    case 1:
                        c_Items = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "f.Content" => true,
                "f_Content" => true,
                "Content" => true,
                "c.Items" => true,
                "c_Items" => true,
                "Items" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)f_Content,
                1 => (object)c_Items,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "f.Content" => (object)f_Content,
                "f_Content" => (object)f_Content,
                "Content" => (object)f_Content,
                "c.Items" => (object)c_Items,
                "c_Items" => (object)c_Items,
                "Items" => (object)c_Items,
                _ => throw new KeyNotFoundException(name)};
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
    /// Generated interpreter for binary schema 'GenericItem'.
    /// </summary>
    public sealed class GenericItem : BytesInterpreterBase<GenericItem>
    {
        /// <summary>Gets the Value field value.</summary>
        public byte Value { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "GenericItem";

        /// <inheritdoc/>
        public override GenericItem InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            ParsePosition = offset;
            BitOffset = 0;
            var _value = ReadByte(data);
            return new GenericItem
            {
                Value = _value
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
            ParsePosition = offset;
            BitOffset = 0;
            var _count = ReadByte(data);
            var __data_list = new System.Collections.Generic.List<T>();
            for (int __data_i = 0; __data_i < (int)_count; __data_i++)
            {
                var __data_elemInterpreter = new T();
                var _elem = __data_elemInterpreter.InterpretAt(data, ParsePosition);
                ParsePosition = __data_elemInterpreter.BytesConsumed;
                __data_list.Add(_elem);
            }

            var _data = __data_list.ToArray();
            return new LengthPrefixed<T>
            {
                Count = _count,
                Data = _data
            };
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'GenericContainer'.
    /// </summary>
    public sealed class GenericContainer : BytesInterpreterBase<GenericContainer>
    {
        /// <summary>Gets the Items field value.</summary>
        public LengthPrefixed<GenericItem> Items { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "GenericContainer";

        /// <inheritdoc/>
        public override GenericContainer InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            ParsePosition = offset;
            BitOffset = 0;
            var __items_interpreter = new LengthPrefixed<GenericItem>();
            var _items = __items_interpreter.InterpretAt(data, ParsePosition);
            ParsePosition = __items_interpreter.BytesConsumed;
            return new GenericContainer
            {
                Items = _items
            };
        }
    }
}
