// === Parsed Query ===
/*
binary Header {
    Kind: byte,
    Value: int le,
    Count: byte,
    Tail: byte[Count]
};
select r.Id as Id, p.ParsedFields.Kind as Kind, p.BytesConsumed as Consumed, p.ErrorField as Error
from bench.rows() r
cross apply PartialInterpret<Header>(r.Truncated) p
order by r.Id
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.Id as r.Id, r.Truncated as r.Truncated, p.ParsedFields as p.ParsedFields, p.ErrorField as p.ErrorField, p.BytesConsumed as p.BytesConsumed]
    Apply [Cross]
      SchemaScan [#bench.rows() as r]
      InterpretSource [#Header(r.Truncated) as p]
  Sort [r.Id]
    Project [r.Id as Id, p.ParsedFields.Kind as Kind, p.BytesConsumed as Consumed, p.ErrorField as Error]
      CteRef [rp as rp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.Id as r.Id, p.ParsedFields as p.ParsedFields, p.ErrorField as p.ErrorField, p.BytesConsumed as p.BytesConsumed]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#bench.rows() as r]
      PhysicalInterpretSource [#Header(r.Truncated) as p]
  PhysicalSort [r.Id]
    PhysicalProject [r.Id as Id, p.ParsedFields.Kind as Kind, p.BytesConsumed as Consumed, p.ErrorField as Error]
      PhysicalCteRef [rp as rp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: BenchmarkSampleRow]
      Id: int <- property Id
      Truncated: byte[] <- property Truncated
    SourceEntity [p: object]
      ParsedFields: object <- property ParsedFields
      ErrorField: string <- property ErrorField
      ErrorMessage: string <- property ErrorMessage
      BytesConsumed: int <- property BytesConsumed
      CompletedFields: IReadOnlySet<string> <- property CompletedFields
      IsSuccess: bool <- property IsSuccess
      BitOffset: int <- property BitOffset
    Generated [Statement0Row0]
      r.Id: int <- field r_Id
      p.ParsedFields: object <- field p_ParsedFields
      p.ErrorField: string <- field p_ErrorField
      p.BytesConsumed: int <- field p_BytesConsumed
    TableRow [rp]
      r.Id: int <- field r_Id
      p.ParsedFields: object <- field p_ParsedFields
      p.ErrorField: string <- field p_ErrorField
      p.BytesConsumed: int <- field p_BytesConsumed
    Generated [ResultRow0]
      Id: int <- field Id
      Kind: byte <- field Kind
      Consumed: int <- field Consumed
      Error: string <- field Error

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: BenchmarkSampleRow] -> statement0_rRows
    CreateTable [statement0: Statement0Row0]
    PhaseBoundary [Select]
    ChunkedForEach [r in statement0_rRows]
      Let [rId: int = r.Id]
      InterpretSource [Header.PartialInterpret(r.Truncated) -> statement0_pRows]
      ScalarForEach [p in statement0_pRows]
        AppendRow [statement0 <- Statement0Row0(r.Id: rId, p.ParsedFields: p.ParsedFields, p.ErrorField: p.ErrorField, p.BytesConsumed: p.BytesConsumed)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [rp in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Id: rp.r.Id, Kind: rp.p.ParsedFields.Kind, Consumed: rp.p.BytesConsumed, Error: rp.p.ErrorField)]
    SortOwnedBuffer [result by Id ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B32_Partial_BenchmarkTable
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
            new Column("Id", typeof(int), 0),
            new Column("Kind", typeof(byte), 1),
            new Column("Consumed", typeof(int), 2),
            new Column("Error", typeof(string), 3)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("r.Id", typeof(int), 0),
            new Column("p.ParsedFields", typeof(Musoq.Generated.Interpreters.Header), 1),
            new Column("p.ErrorField", typeof(string), 2),
            new Column("p.BytesConsumed", typeof(int), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Truncated", typeof(byte[]), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Id, __musoqShapeRow.Kind, __musoqShapeRow.Consumed, __musoqShapeRow.Error);
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
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var result = new List<ResultShape0>();
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 rp = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(rp.r_Id, rp.p_ParsedFields.Kind, rp.p_BytesConsumed, rp.p_ErrorField) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
                    if (comparison != 0)
                        return comparison;
                    return left.__musoqSequence.CompareTo(right.__musoqSequence);
                }));
                return result;
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
            var __statement0_rSchema = provider.GetSchema("#bench");
            var statement0_rRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BenchmarkSampleRow>.Open(__statement0_rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BenchmarkSampleRow>(statement0_rRowsSource.Chunks, __musoqProgressContext, "r:1") : statement0_rRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            foreach (var rChunk in statement0_rRows)
            {
                if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.BenchmarkSampleRow> rChunkView)
                {
                    if (rChunkView.Source is Musoq.Evaluator.Tests.BenchmarkSampleRow[] rChunkViewArray)
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
                            var _interpreter_Header = new Musoq.Generated.Interpreters.Header();
                            var statement0_pRows = _interpreter_Header.PartialInterpret(r.Truncated);
                            if (statement0_pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = statement0_pRows;
                                statement0.Add(new Statement0Row0(rId, p.ParsedFields, p.ErrorField, p.BytesConsumed));
                            }
                        }

                        continue;
                    }

                    if (rChunkView.Source is List<Musoq.Evaluator.Tests.BenchmarkSampleRow> rChunkViewList)
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
                            var _interpreter_Header = new Musoq.Generated.Interpreters.Header();
                            var statement0_pRows = _interpreter_Header.PartialInterpret(r.Truncated);
                            if (statement0_pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = statement0_pRows;
                                statement0.Add(new Statement0Row0(rId, p.ParsedFields, p.ErrorField, p.BytesConsumed));
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
                    var _interpreter_Header = new Musoq.Generated.Interpreters.Header();
                    var statement0_pRows = _interpreter_Header.PartialInterpret(r.Truncated);
                    if (statement0_pRows != null)
                    {
                        token.ThrowIfCancellationRequested();
                        var p = statement0_pRows;
                        statement0.Add(new Statement0Row0(rId, p.ParsedFields, p.ErrorField, p.BytesConsumed));
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
            public ResultRow0(int __value0, byte __value1, int __value2, string __value3)
            {
                Id = __value0;
                Kind = __value1;
                Consumed = __value2;
                Error = __value3;
            }

            public int Consumed { get; private set; }
            public override int Count => 4;
            public string Error { get; private set; }
            public int Id { get; private set; }
            public byte Kind { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Kind = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Consumed = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Error = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Kind),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Consumed),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Error),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Kind" => true,
                "Consumed" => true,
                "Error" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Kind, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Consumed, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Error, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id, byte Kind, int Consumed, string Error)
            {
                this.Id = Id;
                this.Kind = Kind;
                this.Consumed = Consumed;
                this.Error = Error;
            }

            public int Consumed { get; }
            public string Error { get; }
            public int Id { get; }
            public byte Kind { get; }
            public int __musoqSequence { get; init; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(int __value0, Musoq.Generated.Interpreters.Header __value1, string __value2, int __value3)
            {
                r_Id = __value0;
                p_ParsedFields = __value1;
                p_ErrorField = __value2;
                p_BytesConsumed = __value3;
            }

            public int p_BytesConsumed { get; }
            public string p_ErrorField { get; }
            public Musoq.Generated.Interpreters.Header p_ParsedFields { get; }
            public int r_Id { get; }
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
            if (IsFieldComplete("Kind"))
            {
                writer.WritePropertyName("Kind");
                var fieldValue = this.Kind;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Value"))
            {
                writer.WritePropertyName("Value");
                var fieldValue = this.Value;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Count"))
            {
                writer.WritePropertyName("Count");
                var fieldValue = this.Count;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Tail"))
            {
                writer.WritePropertyName("Tail");
                var fieldValue = this.Tail;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Kind field value.</summary>
        public byte Kind { get; set; }
        /// <summary>Gets the Value field value.</summary>
        public int Value { get; set; }
        /// <summary>Gets the Count field value.</summary>
        public byte Count { get; set; }
        /// <summary>Gets the Tail field value.</summary>
        public byte[] Tail { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Header";

        /// <inheritdoc/>
        public override Header InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new Header();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Kind");
            var _kind = ReadByte(data);
            __parsedValue.Kind = _kind;
            RecordParsedField("Kind");
            SetCurrentField("Value");
            var _value = ReadInt32Le(data);
            __parsedValue.Value = _value;
            RecordParsedField("Value");
            SetCurrentField("Count");
            var _count = ReadByte(data);
            __parsedValue.Count = _count;
            RecordParsedField("Count");
            SetCurrentField("Tail");
            var _tail = ReadBytes(data, ResolveExpressionSize<byte>(() => checked((byte?)(_count)), ParseErrorCode.InvalidSize));
            __parsedValue.Tail = _tail;
            RecordParsedField("Tail");
            return __parsedValue;
        }
    }
}
