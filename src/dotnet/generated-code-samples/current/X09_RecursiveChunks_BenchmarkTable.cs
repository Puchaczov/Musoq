// === Parsed Query ===
/*
binary Header {
    Kind: byte,
    Value: int le,
    Count: byte,
    Tail: byte[Count]
};
with recursive walk (Bytes, Off, Used, Error) as (select r.Payload, 0, 0, null
from bench.stream() r
union all select w.Bytes, w.Off + w.Used, p.BytesConsumed, p.ErrorField
from walk w
cross apply PartialInterpret<Header>(FromBase64(ToBase64(w.Bytes, w.Off + w.Used, Length(w.Bytes) - w.Off - w.Used))) p
where w.Off + w.Used < Length(w.Bytes) and w.Error is null) select Off as Id, Used as Consumed, Error
from walk
order by Id, Consumed
*/

// === Logical Plan ===
/*
Cte
  Definition [walk]
    RecursiveCte [walk] [All]
      Anchor
        MultiStatement
          Project [r.Payload as Bytes, 0 as Off, 0 as Used, NULL as Error]
            SchemaScan [#bench.stream() as r]
      RecursiveMember
        MultiStatement
          Project [w.Bytes as w.Bytes, w.Off as w.Off, w.Used as w.Used, w.Error as w.Error, p.ErrorField as p.ErrorField, p.BytesConsumed as p.BytesConsumed]
            Apply [Cross]
              CteRef [walk as w]
              InterpretSource [#Header(FromBase64(ToBase64(w.Bytes, (w.Off + w.Used), ((Length(w.Bytes) - w.Off) - w.Used)))) as p]
          Project [w.Bytes as Bytes, (w.Off + w.Used) as w.Off + w.Used, p.BytesConsumed as BytesConsumed, p.ErrorField as ErrorField]
            Filter [(((w.Off + w.Used) < Length(w.Bytes)) AND w.Error IS NULL)]
              CteRef [wp as wp]
  Query
    MultiStatement
      Sort [walk.Off, walk.Used]
        Project [walk.Off as Id, walk.Used as Consumed, walk.Error as Error]
          CteRef [walk as walk]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [walk]
    PhysicalRecursiveCte [walk] [All]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [r.Payload as Bytes, 0 as Off, 0 as Used, NULL as Error]
            PhysicalSchemaScan [#bench.stream() as r]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [w.Bytes as w.Bytes, w.Off as w.Off, w.Used as w.Used, w.Error as w.Error, p.ErrorField as p.ErrorField, p.BytesConsumed as p.BytesConsumed]
            PhysicalNestedLoopApply [Cross] [guards: PreApplyRight: ((w.Off + w.Used) < Length(w.Bytes)), PreApplyRight: w.Error IS NULL]
              PhysicalCteRef [walk as w]
              PhysicalInterpretSource [#Header(FromBase64(ToBase64(w.Bytes, (w.Off + w.Used), ((Length(w.Bytes) - w.Off) - w.Used)))) as p]
          PhysicalProject [w.Bytes as Bytes, (w.Off + w.Used) as w.Off + w.Used, p.BytesConsumed as BytesConsumed, p.ErrorField as ErrorField]
            PhysicalFilter [(((w.Off + w.Used) < Length(w.Bytes)) AND w.Error IS NULL)]
              PhysicalCteRef [wp as wp]
  Query
    PhysicalMultiStatement
      PhysicalSort [walk.Off, walk.Used]
        PhysicalProject [walk.Off as Id, walk.Used as Consumed, walk.Error as Error]
          PhysicalCteRef [walk as walk]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: BenchmarkSampleRow]
      Payload: byte[] <- property Payload
    Generated [Cte0Row0]
      Bytes: byte[] <- field Bytes
      Off: int <- field Off
      Used: int <- field Used
      Error: string <- field Error
    TableRow [w]
      Bytes: byte[] <- field Bytes
      Off: int <- field Off
      Used: int <- field Used
      Error: string <- field Error
    SourceEntity [p: object]
      ParsedFields: object <- property ParsedFields
      ErrorField: string <- property ErrorField
      ErrorMessage: string <- property ErrorMessage
      BytesConsumed: int <- property BytesConsumed
      CompletedFields: IReadOnlySet<string> <- property CompletedFields
      IsSuccess: bool <- property IsSuccess
      BitOffset: int <- property BitOffset
    TableRow [walk]
      Bytes: byte[] <- field Bytes
      Off: int <- field Off
      Used: int <- field Used
      Error: string <- field Error
    Generated [ResultRow0]
      Id: int <- field Id
      Consumed: int <- field Consumed
      Error: string <- field Error

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    RecursiveCte [walk; result cte0; frontiers cte0CurrentFrontier, cte0NextFrontier; identity none; max iterations 100002; max rows 10000000; max snapshot rows 10000000]
      Anchor
        SourceScan [r: BenchmarkSampleRow] -> cte0CurrentFrontier_rRows
        ChunkedForEach [r in cte0CurrentFrontier_rRows]
          RecursiveAppend [cte0CurrentFrontier <- Cte0Row0(Bytes: r.Payload, Off: 0, Used: 0, Error: NULL); identity none; guard cte0.Count + cte0CurrentFrontier.Count < 10000000]
      RecursiveMember
        ForEach [w in cte0CurrentFrontier]
          CreateObject [__compiled5LibraryBase0: LibraryBase]
          ContinueIf [NOT (((w.Off + w.Used) < Length(w.Bytes)) = TRUE)]
          ContinueIf [NOT w.Error IS NULL]
          InterpretSource [Header.PartialInterpret(FromBase64(ToBase64(w.Bytes, (w.Off + w.Used), ((Length(w.Bytes) - w.Off) - w.Used)))) -> cte0NextFrontier_pRows]
          ScalarForEach [p in cte0NextFrontier_pRows]
            RecursiveAppend [cte0NextFrontier <- Cte0Row0(Bytes: w.Bytes, Off: (w.Off + w.Used), Used: p.BytesConsumed, Error: p.ErrorField); identity none; guard cte0.Count + cte0NextFrontier.Count < 10000000]
    PhaseBoundary [Where:cte0]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [From]
    ForEach [walk in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Id: walk.Off, Consumed: walk.Used, Error: walk.Error)]
    PhaseBoundary [Select]
    SortOwnedBuffer [result by Id ASC, Consumed ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_X09_RecursiveChunks_BenchmarkTable
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Consumed", typeof(int), 1),
            new Column("Error", typeof(string), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Payload", typeof(byte[]), 0) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Id, __musoqShapeRow.Consumed, __musoqShapeRow.Error);
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
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.From);
                    var cte0 = new List<Cte0Row0>();
                    var cte0CurrentFrontier = new List<Cte0Row0>();
                    var cte0NextFrontier = new List<Cte0Row0>();
                    int __cte0Iteration = 0;
                    int __cte0CancellationCounter = 0;
                    var __cte0CurrentFrontier_rSchema = provider.GetSchema("#bench");
                    var cte0CurrentFrontier_rRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BenchmarkSampleRow>.Open(__cte0CurrentFrontier_rSchema, "stream", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                    var cte0CurrentFrontier_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BenchmarkSampleRow>(cte0CurrentFrontier_rRowsSource.Chunks, __musoqProgressContext, "r:1") : cte0CurrentFrontier_rRowsSource.Chunks;
                    foreach (var rChunk in cte0CurrentFrontier_rRows)
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
                                    if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                    {
                                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("walk", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                    }

                                    cte0CurrentFrontier.Add(new Cte0Row0(r.Payload, 0, 0, null));
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
                                    if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                    {
                                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("walk", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                    }

                                    cte0CurrentFrontier.Add(new Cte0Row0(r.Payload, 0, 0, null));
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
                            if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                            {
                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("walk", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                            }

                            cte0CurrentFrontier.Add(new Cte0Row0(r.Payload, 0, 0, null));
                        }
                    }

                    cte0.AddRange(cte0CurrentFrontier);
                    while (cte0CurrentFrontier.Count > 0)
                    {
                        if ((__cte0Iteration & 63) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        if (__cte0Iteration >= 100002)
                        {
                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("walk", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 100002);
                        }

                        __cte0Iteration++;
                        cte0NextFrontier.Clear();
                        for (int cte0CurrentFrontierIndex = 0; cte0CurrentFrontierIndex < cte0CurrentFrontier.Count; ++cte0CurrentFrontierIndex)
                        {
                            if (cte0CurrentFrontierIndex != 0 && (cte0CurrentFrontierIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Cte0Row0 w = (Cte0Row0)cte0CurrentFrontier[cte0CurrentFrontierIndex];
                            var __compiled5LibraryBase0 = new Musoq.Plugins.LibraryBase();
                            if ((!(Operators.SqlCompare<int, int?>((w.Off + w.Used), (int?)__compiled5LibraryBase0.Length<byte>(w.Bytes), (int __sqlLeft, int? __sqlRight) => (__sqlLeft < __sqlRight)) == true)))
                            {
                                continue;
                            }

                            if ((!(w.Error == null)))
                            {
                                continue;
                            }

                            var _interpreter_Header = new Musoq.Generated.Interpreters.Header();
                            var cte0NextFrontier_pRows = _interpreter_Header.PartialInterpret((byte[])__compiled5LibraryBase0.FromBase64((string)__compiled5LibraryBase0.ToBase64(w.Bytes, (w.Off + w.Used), (int?)((int?)((int?)__compiled5LibraryBase0.Length<byte>(w.Bytes) - w.Off) - w.Used))));
                            if (cte0NextFrontier_pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = cte0NextFrontier_pRows;
                                ++__cte0CancellationCounter;
                                if ((__cte0CancellationCounter & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                if (cte0.Count + cte0NextFrontier.Count >= 10000000)
                                {
                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("walk", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                }

                                cte0NextFrontier.Add(new Cte0Row0(w.Bytes, (w.Off + w.Used), p.BytesConsumed, p.ErrorField));
                            }
                        }

                        cte0.AddRange(cte0NextFrontier);
                        var __cte0FrontierSwap = cte0CurrentFrontier;
                        cte0CurrentFrontier = cte0NextFrontier;
                        cte0NextFrontier = __cte0FrontierSwap;
                    }

                    OnPhaseChanged("compiled:cte0", QueryPhase.Where);
                    OnPhaseChanged("compiled:cte0", QueryPhase.Select);
                    _cteRowResults.Slot0 = cte0;
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
                }

                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.From);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 walk = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(walk.Off, walk.Used, walk.Error) { __musoqSequence = result.Count });
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
                    if (comparison != 0)
                        return comparison;
                    comparison = left.Consumed.CompareTo(right.Consumed);
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

        private readonly struct Cte0Row0
        {
            public Cte0Row0(byte[] Bytes, int Off, int Used, string Error)
            {
                this.Bytes = Bytes;
                this.Off = Off;
                this.Used = Used;
                this.Error = Error;
            }

            public byte[] Bytes { get; }
            public int Off { get; }
            public int Used { get; }
            public string Error { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, int __value1, string __value2)
            {
                Id = __value0;
                Consumed = __value1;
                Error = __value2;
            }

            public int Consumed { get; private set; }
            public override int Count => 3;
            public string Error { get; private set; }
            public int Id { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Consumed = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Error = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Consumed),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Error),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Consumed" => true,
                "Error" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Consumed, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Error, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id, int Consumed, string Error)
            {
                this.Id = Id;
                this.Consumed = Consumed;
                this.Error = Error;
            }

            public int Consumed { get; }
            public string Error { get; }
            public int Id { get; }
            public int __musoqSequence { get; init; }
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
