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
  Project [h.Magic as Magic, h.Offset as Offset, p.Value as Value]
    CteRef [fhp as fhp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, h.Magic as h.Magic, h.Offset as h.Offset]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#Header(f.Content) as h]
  PhysicalProject [fh.h.Magic as h.Magic, fh.h.Offset as h.Offset, p.Value as p.Value]
    PhysicalNestedLoopApply [Cross]
      PhysicalCteRef [fh as fh]
      PhysicalInterpretSource [#Payload(fh.f.Content, fh.h.Offset) as p]
  PhysicalProject [h.Magic as Magic, h.Offset as Offset, p.Value as Value]
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
      Magic: int <- field Magic
      Offset: int <- field Offset
      Value: short <- field Value

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
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    ForEach [fh in _cteRowResults.Slot0]
      Let [fhhMagic: int = fh.h.Magic]
      Let [fhhOffset: int = fh.h.Offset]
      InterpretSource [Payload.InterpretAt(fh.f.Content, fh.h.Offset) -> pRows]
      ScalarForEach [p in pRows]
        AppendShape [result <- ResultRow0(Magic: fhhMagic, Offset: fhhOffset, Value: p.Value)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Magic", typeof(int), 0),
            new Column("Offset", typeof(int), 1),
            new Column("Value", typeof(short), 2)
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

                        Statement0Row0 fh = __storedTable0Rows[__storedTable0Index];
                        int fhhMagic = fh.h_Magic;
                        int fhhOffset = fh.h_Offset;
                        var _interpreter_Payload = new Musoq.Generated.Interpreters.Payload();
                        var pRows = _interpreter_Payload.InterpretAt(fh.f_Content, fh.h_Offset);
                        if (pRows != null)
                        {
                            token.ThrowIfCancellationRequested();
                            var p = pRows;
                            __musoqFinalShapeRows.Add(new ResultRow0(fhhMagic, fhhOffset, p.Value));
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
            var statement0_fRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>.Open(__statement0_fSchema, "files", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
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
                            Musoq.Generated.Interpreters.Header? statement0_hRows = null;
                            try
                            {
                                statement0_hRows = _interpreter_Header.Interpret(f.Content);
                            }
                            catch (global::Musoq.Schema.Interpreters.ParseException)
                            {
                                statement0_hRows = null;
                            }

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
                            Musoq.Generated.Interpreters.Header? statement0_hRows = null;
                            try
                            {
                                statement0_hRows = _interpreter_Header.Interpret(f.Content);
                            }
                            catch (global::Musoq.Schema.Interpreters.ParseException)
                            {
                                statement0_hRows = null;
                            }

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
                    Musoq.Generated.Interpreters.Header? statement0_hRows = null;
                    try
                    {
                        statement0_hRows = _interpreter_Header.Interpret(f.Content);
                    }
                    catch (global::Musoq.Schema.Interpreters.ParseException)
                    {
                        statement0_hRows = null;
                    }

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
                Magic = __value0;
                Offset = __value1;
                Value = __value2;
            }

            public override int Count => 3;
            public int Magic { get; private set; }
            public int Offset { get; private set; }
            public short Value { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Magic = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Offset = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<short, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Magic),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Offset),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short>(this.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Magic" => true,
                "Offset" => true,
                "Value" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Magic, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Offset, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Value, ref visitor);
            }
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
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Header : BytesInterpreterBase<Header>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Header value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Magic"))
            {
                writer.WritePropertyName("Magic");
                var fieldValue = this.Magic;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Offset"))
            {
                writer.WritePropertyName("Offset");
                var fieldValue = this.Offset;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Magic field value.</summary>
        public int Magic { get; set; }
        /// <summary>Gets the Offset field value.</summary>
        public int Offset { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Header";

        /// <inheritdoc/>
        public override Header InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new Header();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Magic");
            var _magic = ReadInt32Le(data);
            __parsedValue.Magic = _magic;
            RecordParsedField("Magic");
            SetCurrentField("Offset");
            var _offset = ReadInt32Le(data);
            __parsedValue.Offset = _offset;
            RecordParsedField("Offset");
            return __parsedValue;
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'Payload'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Payload : BytesInterpreterBase<Payload>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Payload value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Value"))
            {
                writer.WritePropertyName("Value");
                var fieldValue = this.Value;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Value field value.</summary>
        public short Value { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Payload";

        /// <inheritdoc/>
        public override Payload InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new Payload();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Value");
            var _value = ReadInt16Le(data);
            __parsedValue.Value = _value;
            RecordParsedField("Value");
            return __parsedValue;
        }
    }
}
