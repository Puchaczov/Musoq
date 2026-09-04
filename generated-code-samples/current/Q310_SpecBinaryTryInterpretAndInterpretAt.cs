// === Parsed Query ===
/*
binary Header { Magic: int le, Offset: int le };
                    binary Payload { Value: short le };
                    select h.Magic, h.Offset, p.Value
                    from #test.files() f
                    cross apply TryInterpret<Header>(f.Content) h
                    cross apply InterpretAt<Payload>(f.Content, h.Offset) p
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, h.Magic as h.Magic, h.Offset as h.Offset]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#Header(f.Content) as h]
  Project [fh.f.Content as f.Content, fh.h.Magic as h.Magic, fh.h.Offset as h.Offset, p.Value as p.Value]
    Apply [Cross]
      CteRef [fh as fh]
      InterpretSource [#Payload(fh.f.Content, fh.h.Offset) as p]
  Project [h.Magic as h.Magic, h.Offset as h.Offset, p.Value as p.Value]
    CteRef [fhp as fhp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, h.Magic as h.Magic, h.Offset as h.Offset]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#Header(f.Content) as h]
  PhysicalProject [fh.f.Content as f.Content, fh.h.Magic as h.Magic, fh.h.Offset as h.Offset, p.Value as p.Value]
    PhysicalNestedLoopApply [Cross]
      PhysicalCteRef [fh as fh]
      PhysicalInterpretSource [#Payload(fh.f.Content, fh.h.Offset) as p]
  PhysicalProject [h.Magic as h.Magic, h.Offset as h.Offset, p.Value as p.Value]
    PhysicalCteRef [fhp as fhp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [h: object]
      Magic: int <- property Magic
      Offset: int <- property Offset
    Generated [Statement0Row0]
      f.Content: byte[] <- field f_Content
      h.Magic: int <- field h_Magic
      h.Offset: int <- field h_Offset
    TableRow [fh]
      f.Content: byte[] <- field f_Content
      h.Magic: int <- field h_Magic
      h.Offset: int <- field h_Offset
    SourceEntity [p: object]
      Value: short <- property Value
    Generated [ResultRow0]
      h.Magic: int <- field h_Magic
      h.Offset: int <- field h_Offset
      p.Value: short <- field p_Value

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [f: BinaryEntity] -> statement0_fRows
    CreateTable [statement0: Statement0Row0]
    ChunkedForEach [f in statement0_fRows]
      InterpretSource [Header.TryInterpret(f.Content) -> statement0_hRows]
      ScalarForEach [h in statement0_hRows]
        AppendRow [statement0 <- Statement0Row0(f.Content: f.Content, h.Magic: h.Magic, h.Offset: h.Offset)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [fh in _cteRowResults.Slot0]
      Let [fhhMagic: int = fh.h.Magic]
      Let [fhhOffset: int = fh.h.Offset]
      InterpretSource [Payload.InterpretAt(fh.f.Content, fh.h.Offset) -> pRows]
      ScalarForEach [p in pRows]
        AppendShape [result <- ResultShape0(h.Magic: fhhMagic, h.Offset: fhhOffset, p.Value: p.Value)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q310_SpecBinaryTryInterpretAndInterpretAt
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
            new Column("h.Magic", typeof(int), 0),
            new Column("h.Offset", typeof(int), 1),
            new Column("p.Value", typeof(short), 2)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("f.Content", typeof(byte[]), 0),
            new Column("h.Magic", typeof(int), 1),
            new Column("h.Offset", typeof(int), 2)
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
            var __musoqMaterializedTable = QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
            _ = __musoqMaterializedTable.Count;
            return __musoqMaterializedTable;
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.h_Magic, __musoqShapeRow.h_Offset, __musoqShapeRow.p_Value);
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

                        Statement0Row0 fh = __storedTable0Rows[__storedTable0Index];
                        int fhhMagic = fh.h_Magic;
                        int fhhOffset = fh.h_Offset;
                        var _interpreter_Payload = new Musoq.Generated.Interpreters.Payload();
                        var pRows = _interpreter_Payload.InterpretAt(fh.f_Content, fh.h_Offset);
                        if (pRows != null)
                        {
                            token.ThrowIfCancellationRequested();
                            var p = pRows;
                            __musoqFinalShapeRows.Add(new ResultShape0(fhhMagic, fhhOffset, p.Value));
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
                            var _interpreter_Header = new Musoq.Generated.Interpreters.Header();
                            var statement0_hRows = new Func<Musoq.Generated.Interpreters.Header?>(() =>
                            {
                                try
                                {
                                    return _interpreter_Header.Interpret(f.Content);
                                }
                                catch
                                {
                                    return null;
                                }
                            })();
                            if (statement0_hRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var h = statement0_hRows;
                                statement0.Add(new Statement0Row0(f.Content, h.Magic, h.Offset));
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
                            var _interpreter_Header = new Musoq.Generated.Interpreters.Header();
                            var statement0_hRows = new Func<Musoq.Generated.Interpreters.Header?>(() =>
                            {
                                try
                                {
                                    return _interpreter_Header.Interpret(f.Content);
                                }
                                catch
                                {
                                    return null;
                                }
                            })();
                            if (statement0_hRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var h = statement0_hRows;
                                statement0.Add(new Statement0Row0(f.Content, h.Magic, h.Offset));
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
                    var _interpreter_Header = new Musoq.Generated.Interpreters.Header();
                    var statement0_hRows = new Func<Musoq.Generated.Interpreters.Header?>(() =>
                    {
                        try
                        {
                            return _interpreter_Header.Interpret(f.Content);
                        }
                        catch
                        {
                            return null;
                        }
                    })();
                    if (statement0_hRows != null)
                    {
                        token.ThrowIfCancellationRequested();
                        var h = statement0_hRows;
                        statement0.Add(new Statement0Row0(f.Content, h.Magic, h.Offset));
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
            public ResultRow0(int __value0, int __value1, short __value2)
            {
                h_Magic = __value0;
                h_Offset = __value1;
                p_Value = __value2;
            }

            public override int Count => 3;
            public int h_Magic { get; private set; }
            public int h_Offset { get; private set; }
            public short p_Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        h_Magic = (int)value;
                        break;
                    case 1:
                        h_Offset = (int)value;
                        break;
                    case 2:
                        p_Value = (short)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "h.Magic" => true,
                "h_Magic" => true,
                "Magic" => true,
                "h.Offset" => true,
                "h_Offset" => true,
                "Offset" => true,
                "p.Value" => true,
                "p_Value" => true,
                "Value" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)h_Magic,
                1 => (object)h_Offset,
                2 => (object)p_Value,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "h.Magic" => (object)h_Magic,
                "h_Magic" => (object)h_Magic,
                "Magic" => (object)h_Magic,
                "h.Offset" => (object)h_Offset,
                "h_Offset" => (object)h_Offset,
                "Offset" => (object)h_Offset,
                "p.Value" => (object)p_Value,
                "p_Value" => (object)p_Value,
                "Value" => (object)p_Value,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int h_Magic, int h_Offset, short p_Value)
            {
                this.h_Magic = h_Magic;
                this.h_Offset = h_Offset;
                this.p_Value = p_Value;
            }

            public int h_Magic { get; }
            public int h_Offset { get; }
            public short p_Value { get; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(byte[] __value0, int __value1, int __value2)
            {
                f_Content = __value0;
                h_Magic = __value1;
                h_Offset = __value2;
            }

            public byte[] f_Content { get; }
            public int h_Magic { get; }
            public int h_Offset { get; }
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
    /// Generated interpreter for binary schema 'Header'.
    /// </summary>
    public sealed class Header : BytesInterpreterBase<Header>
    {
        /// <summary>Gets the Magic field value.</summary>
        public int Magic { get; init; }
        /// <summary>Gets the Offset field value.</summary>
        public int Offset { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Header";

        /// <inheritdoc/>
        public override Header InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("Magic");
            var _magic = ReadInt32Le(data);
            RecordParsedField("Magic", _magic);
            SetCurrentField("Offset");
            var _offset = ReadInt32Le(data);
            RecordParsedField("Offset", _offset);
            return new Header
            {
                Magic = _magic,
                Offset = _offset
            };
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'Payload'.
    /// </summary>
    public sealed class Payload : BytesInterpreterBase<Payload>
    {
        /// <summary>Gets the Value field value.</summary>
        public short Value { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Payload";

        /// <inheritdoc/>
        public override Payload InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("Value");
            var _value = ReadInt16Le(data);
            RecordParsedField("Value", _value);
            return new Payload
            {
                Value = _value
            };
        }
    }
}
