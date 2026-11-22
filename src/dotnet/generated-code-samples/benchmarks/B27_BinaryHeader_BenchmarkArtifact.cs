// === Parsed Query ===
/*
binary Header {
    Kind: byte,
    Value: int le,
    Count: byte,
    Tail: byte[Count]
};
select r.Id as Id, h.Value as Value, h.Kind as Kind
from bench.rows() r
cross apply Interpret<Header>(r.Payload) h
order by r.Id
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.Id as r.Id, r.Payload as r.Payload, h.Kind as h.Kind, h.Value as h.Value]
    Apply [Cross]
      SchemaScan [#bench.rows() as r]
      InterpretSource [#Header(r.Payload) as h]
  Sort [r.Id]
    Project [r.Id as Id, h.Value as Value, h.Kind as Kind]
      CteRef [rh as rh]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.Id as r.Id, h.Kind as h.Kind, h.Value as h.Value]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#bench.rows() as r]
      PhysicalInterpretSource [#Header(r.Payload) as h]
  PhysicalSort [r.Id]
    PhysicalProject [r.Id as Id, h.Value as Value, h.Kind as Kind]
      PhysicalCteRef [rh as rh]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Id: int <- property Id
      Payload: byte[] <- property Payload
    SourceEntity [h: object]
      Kind: byte <- property Kind
      Value: int <- property Value
      Count: byte <- property Count
      Tail: byte[] <- property Tail
    Generated [Statement0Row0]
      r.Id: int <- field r_Id
      h.Kind: byte <- field h_Kind
      h.Value: int <- field h_Value
    TableRow [rh]
      r.Id: int <- field r_Id
      h.Kind: byte <- field h_Kind
      h.Value: int <- field h_Value
    Generated [ResultRow0]
      Id: int <- field Id
      Value: int <- field Value
      Kind: byte <- field Kind

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: FixtureRow] -> statement0_rRows
    CreateTable [statement0: Statement0Row0]
    PhaseBoundary [Select]
    ChunkedForEach [r in statement0_rRows]
      Let [rId: int = r.Id]
      InterpretSource [Header.Interpret(r.Payload) -> statement0_hRows]
      ScalarForEach [h in statement0_hRows]
        AppendRow [statement0 <- Statement0Row0(r.Id: rId, h.Kind: h.Kind, h.Value: h.Value)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [rh in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Id: rh.r.Id, Value: rh.h.Value, Kind: rh.h.Kind)]
    SortOwnedBuffer [result by Id ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultShape0 from owned buffer result]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B27_BinaryHeader_BenchmarkArtifact
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Value", typeof(int), 1),
            new Column("Kind", typeof(byte), 2)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("r.Id", typeof(int), 0),
            new Column("h.Kind", typeof(byte), 1),
            new Column("h.Value", typeof(int), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Payload", typeof(byte[]), 1) });
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
            var queryContext = new QueryRunContext(token, TypedParameters, PhaseChanged, DataSourceProgress, this, queryId: "compiled", provider: Provider, sourceRuntimeSettingsBySourceContextId: SourceRuntimeSettingsBySourceContextId, sourceRuntimeSettingDescriptionsBySourceContextId: SourceRuntimeSettingDescriptionsBySourceContextId, sourceExecutionPlans: SourceExecutionPlans, logger: Logger, queryProgress: QueryProgress);
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        private IEnumerable<ResultShape0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, queryContext))
            {
                yield return __musoqShapeRow;
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            var token = queryContext.CancellationToken;
            var __musoqProgressContext = queryContext.QueryProgress == null ? null : queryContext;
            Action<string, QueryPhase> OnPhaseChanged = queryContext.NotifyPhaseChanged;
            DataSourceEventHandler OnDataSourceProgress = queryContext.NotifyDataSourceProgress;
            QueryProgressEventHandler OnQueryProgress = queryContext.QueryProgress;
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(queryContext.TypedParameters);
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

                    Statement0Row0 rh = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(rh.r_Id, rh.h_Value, rh.h_Kind) { __musoqSequence = result.Count });
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
            var statement0_rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__statement0_rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
            var statement0_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(statement0_rRowsSource.Chunks, __musoqProgressContext, "r:1") : statement0_rRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            foreach (var rChunk in statement0_rRows)
            {
                if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.FixtureRow> rChunkView)
                {
                    if (rChunkView.Source is Musoq.Benchmarks.Scenarios.FixtureRow[] rChunkViewArray)
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
                            var statement0_hRows = _interpreter_Header.Interpret(r.Payload);
                            if (statement0_hRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var h = statement0_hRows;
                                statement0.Add(new Statement0Row0(rId, h.Kind, h.Value));
                            }
                        }

                        continue;
                    }

                    if (rChunkView.Source is List<Musoq.Benchmarks.Scenarios.FixtureRow> rChunkViewList)
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
                            var statement0_hRows = _interpreter_Header.Interpret(r.Payload);
                            if (statement0_hRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var h = statement0_hRows;
                                statement0.Add(new Statement0Row0(rId, h.Kind, h.Value));
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
                    var statement0_hRows = _interpreter_Header.Interpret(r.Payload);
                    if (statement0_hRows != null)
                    {
                        token.ThrowIfCancellationRequested();
                        var h = statement0_hRows;
                        statement0.Add(new Statement0Row0(rId, h.Kind, h.Value));
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
            public ResultRow0(int __value0, int __value1, byte __value2)
            {
                Id = __value0;
                Value = __value1;
                Kind = __value2;
            }

            public override int Count => 3;
            public int Id { get; private set; }
            public byte Kind { get; private set; }
            public int Value { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Kind = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Kind),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Value" => true,
                "Kind" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Value, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Kind, ref visitor);
            }
        }

        private sealed class ResultShape0 : Row
        {
            public ResultShape0(int Id, int Value, byte Kind)
            {
                this.Id = Id;
                this.Value = Value;
                this.Kind = Kind;
            }

            public override int Count => 3;
            public int Id { get; private set; }
            public byte Kind { get; private set; }
            public int Value { get; private set; }
            public int __musoqSequence { get; init; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Kind = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Kind),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Value" => true,
                "Kind" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Value, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Kind, ref visitor);
            }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(int __value0, byte __value1, int __value2)
            {
                r_Id = __value0;
                h_Kind = __value1;
                h_Value = __value2;
            }

            public byte h_Kind { get; }
            public int h_Value { get; }
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
    public sealed class Header : BytesInterpreterBase<Header>, IBytesInterpreterStatus<Header>, IParserCompletionLayoutSource, global::Musoq.Plugins.IJsonPayload
    {
        private static readonly global::Musoq.Schema.Interpreters.ParserCompletionLayout __musoqCompletionLayout = new(new string[] { "Kind", "Value", "Count", "Tail" });
        global::Musoq.Schema.Interpreters.ParserCompletionLayout global::Musoq.Schema.Interpreters.IParserCompletionLayoutSource.CompletionLayout => __musoqCompletionLayout;

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

        public override Header InterpretAt(ReadOnlySpan<byte> data, int offset) => ((global::Musoq.Schema.Interpreters.IBytesInterpreterStatus<Header>)this).ReadAt(data, offset).GetValueOrThrow();
        global::Musoq.Schema.Interpreters.ParseAttempt<Header> global::Musoq.Schema.Interpreters.IBytesInterpreterStatus<Header>.ReadAt(ReadOnlySpan<byte> data, int offset)
        {
            Header? __parsedValue = null;
            try
            {
                if (base.InitializeParsePositionStatus(data, offset)is { } __musoqReadFailure0)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Header>.Failed(__parsedValue, __musoqReadFailure0);
                }

                SetCurrentField(null);
                __parsedValue = new Header();
                RecordParsedValue(__parsedValue);
                SetCurrentField("Kind");
                if (base.PrepareByteInputStatus(data, 1)is { } __musoqReadFailure1)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Header>.Failed(__parsedValue, __musoqReadFailure1);
                }

                var _kind = base.TakePreparedBytes(data, 1)[0];
                __parsedValue.Kind = _kind;
                base.RecordParsedFieldSlot(0);
                SetCurrentField("Value");
                if (base.PrepareByteInputStatus(data, 4)is { } __musoqReadFailure2)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Header>.Failed(__parsedValue, __musoqReadFailure2);
                }

                var _value = global::System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(base.TakePreparedBytes(data, 4));
                __parsedValue.Value = _value;
                base.RecordParsedFieldSlot(1);
                SetCurrentField("Count");
                if (base.PrepareByteInputStatus(data, 1)is { } __musoqReadFailure3)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Header>.Failed(__parsedValue, __musoqReadFailure3);
                }

                var _count = base.TakePreparedBytes(data, 1)[0];
                __parsedValue.Count = _count;
                base.RecordParsedFieldSlot(2);
                SetCurrentField("Tail");
                var __musoqReadResult5 = base.ResolveSizeValueStatus<byte>((byte?)(_count), ParseErrorCode.InvalidSize);
                if (!__musoqReadResult5.Succeeded)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Header>.Failed(__parsedValue, __musoqReadResult5.Failure!);
                }

                var __musoqReadSize4 = __musoqReadResult5.Value!;
                if (base.PrepareByteArrayStatus(data, __musoqReadSize4)is { } __musoqReadFailure6)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Header>.Failed(__parsedValue, __musoqReadFailure6);
                }

                var _tail = base.ReadPreparedByteArray(data, __musoqReadSize4);
                __parsedValue.Tail = _tail;
                base.RecordParsedFieldSlot(3);
                return global::Musoq.Schema.Interpreters.ParseAttempt<Header>.Success(__parsedValue);
            }
            catch (global::Musoq.Schema.Interpreters.ParseException __parseFailure)
            {
                return global::Musoq.Schema.Interpreters.ParseAttempt<Header>.Failed(__parsedValue, global::Musoq.Schema.Interpreters.ParseFailure.FromException(__parseFailure));
            }
        }
    }
}
