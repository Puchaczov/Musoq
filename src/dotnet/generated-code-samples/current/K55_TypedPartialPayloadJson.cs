// === Parsed Query ===
/*
binary Packet { Amount: int le, Currency: byte }; select s.Position as Position, ToJson(p.ParsedFields) as Json from #schema.first() s cross apply PartialInterpret<Packet>(s.Data) p order by s.Position
*/

// === Logical Plan ===
/*
MultiStatement
  Project [s.Position as s.Position, s.Data as s.Data, p.ParsedFields as p.ParsedFields]
    Apply [Cross]
      SchemaScan [#schema.first() as s]
      InterpretSource [#Packet(s.Data) as p]
  Sort [s.Position]
    Project [s.Position as Position, ToJson(p.ParsedFields) as Json]
      CteRef [sp as sp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [s.Position as s.Position, s.Data as s.Data, p.ParsedFields as p.ParsedFields]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#schema.first() as s]
      PhysicalInterpretSource [#Packet(s.Data) as p]
  PhysicalSort [s.Position]
    PhysicalProject [s.Position as Position, ToJson(p.ParsedFields) as Json]
      PhysicalCteRef [sp as sp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [s: PartialBytes]
      Position: int <- property Position
      Data: byte[] <- property Data
    SourceEntity [p: object]
      ParsedFields: object <- property ParsedFields
      ErrorField: string <- property ErrorField
      ErrorMessage: string <- property ErrorMessage
      BytesConsumed: int <- property BytesConsumed
    Generated [Statement0Row0]
      s.Position: int <- field s_Position
      s.Data: byte[] <- field s_Data
      p.ParsedFields: object <- field p_ParsedFields
    TableRow [sp]
      s.Position: int <- field s_Position
      s.Data: byte[] <- field s_Data
      p.ParsedFields: object <- field p_ParsedFields
    Generated [ResultRow0]
      Position: int <- field Position
      Json: string <- field Json

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [s: PartialBytes] -> statement0_sRows
    CreateTable [statement0: Statement0Row0]
    PhaseBoundary [Select]
    ChunkedForEach [s in statement0_sRows]
      Let [sPosition: int = s.Position]
      InterpretSource [Packet.PartialInterpret(s.Data) -> statement0_pRows]
      ChunkedForEach [p in statement0_pRows]
        AppendRow [statement0 <- Statement0Row0(s.Position: sPosition, s.Data: s.Data, p.ParsedFields: p.ParsedFields)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateObject [__resultLibraryBase0: LibraryBase]
    ForEach [sp in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Position: sp.s.Position, Json: ToJson(sp.p.ParsedFields))]
    SortShapeRows [result -> resultSorted by Position ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
// === SYNTAX TREE 0 ===
namespace GeneratedSample_K55_TypedPartialPayloadJson
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
            new Column("Position", typeof(int), 0),
            new Column("Json", typeof(string), 1)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("s.Position", typeof(int), 0),
            new Column("s.Data", typeof(byte[]), 1),
            new Column("p.ParsedFields", typeof(Musoq.Generated.Interpreters.Packet), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_s_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Data", typeof(byte[]), 1) });
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
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Json);
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
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var result = new List<ResultShape0>();
                var __resultLibraryBase0 = new Musoq.Plugins.LibraryBase();
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 sp = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(sp.s_Position, (string)__resultLibraryBase0.ToJson<Musoq.Generated.Interpreters.Packet>(sp.p_ParsedFields)));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Position.CompareTo(right.Position);
                    if (comparison != 0)
                        return comparison;
                    return 0;
                }));
                foreach (var resultSortedRowsRow in resultSortedRows)
                {
                    __musoqFinalShapeRows.Add(resultSortedRowsRow);
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
            var __statement0_sSchema = provider.GetSchema("#schema");
            var statement0_sRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.PartialBytes>.Open(__statement0_sSchema, "first", new SourceExecutionContext("s:1", sourceExecutionPlans["s:1"], token, __schemaColumns_compiled_s_0, sourceRuntimeSettingsBySourceContextId["s:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_sRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.PartialBytes>(statement0_sRowsSource.Chunks, __musoqProgressContext, "s:1") : statement0_sRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            foreach (var sChunk in statement0_sRows)
            {
                if (sChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.PartialBytes> sChunkView)
                {
                    if (sChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.PartialBytes[] sChunkViewArray)
                    {
                        int sChunkViewOffset = sChunkView.Offset;
                        for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                        {
                            if ((sIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var s = sChunkViewArray[sChunkViewOffset + sIndex];
                            int sPosition = s.Position;
                            var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                            var statement0_pRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Schema.Interpreters.PartialInterpretResult<Musoq.Generated.Interpreters.Packet>>(EvaluationHelper.WrapScalarForCrossApply<Musoq.Schema.Interpreters.PartialInterpretResult<Musoq.Generated.Interpreters.Packet>>(_interpreter_Packet.PartialInterpret(s.Data)));
                            foreach (var pChunk in statement0_pRows)
                            {
                                for (int pIndex = 0, pIndexCount = pChunk.Count; pIndex < pIndexCount; ++pIndex)
                                {
                                    if ((pIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var p = pChunk[pIndex];
                                    statement0.Add(new Statement0Row0(sPosition, s.Data, p.ParsedFields));
                                }
                            }
                        }

                        continue;
                    }

                    if (sChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.PartialBytes> sChunkViewList)
                    {
                        int sChunkViewOffset = sChunkView.Offset;
                        for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                        {
                            if ((sIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var s = sChunkViewList[sChunkViewOffset + sIndex];
                            int sPosition = s.Position;
                            var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                            var statement0_pRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Schema.Interpreters.PartialInterpretResult<Musoq.Generated.Interpreters.Packet>>(EvaluationHelper.WrapScalarForCrossApply<Musoq.Schema.Interpreters.PartialInterpretResult<Musoq.Generated.Interpreters.Packet>>(_interpreter_Packet.PartialInterpret(s.Data)));
                            foreach (var pChunk in statement0_pRows)
                            {
                                for (int pIndex = 0, pIndexCount = pChunk.Count; pIndex < pIndexCount; ++pIndex)
                                {
                                    if ((pIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var p = pChunk[pIndex];
                                    statement0.Add(new Statement0Row0(sPosition, s.Data, p.ParsedFields));
                                }
                            }
                        }

                        continue;
                    }
                }

                for (int sIndex = 0, sIndexCount = sChunk.Count; sIndex < sIndexCount; ++sIndex)
                {
                    if ((sIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var s = sChunk[sIndex];
                    int sPosition = s.Position;
                    var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                    var statement0_pRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Schema.Interpreters.PartialInterpretResult<Musoq.Generated.Interpreters.Packet>>(EvaluationHelper.WrapScalarForCrossApply<Musoq.Schema.Interpreters.PartialInterpretResult<Musoq.Generated.Interpreters.Packet>>(_interpreter_Packet.PartialInterpret(s.Data)));
                    foreach (var pChunk in statement0_pRows)
                    {
                        for (int pIndex = 0, pIndexCount = pChunk.Count; pIndex < pIndexCount; ++pIndex)
                        {
                            if ((pIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var p = pChunk[pIndex];
                            statement0.Add(new Statement0Row0(sPosition, s.Data, p.ParsedFields));
                        }
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
            public ResultRow0(int __value0, string __value1)
            {
                Position = __value0;
                Json = __value1;
            }

            public override int Count => 2;
            public string Json { get; private set; }
            public int Position { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Json = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Json),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "Json" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Json, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Position, string Json)
            {
                this.Position = Position;
                this.Json = Json;
            }

            public string Json { get; }
            public int Position { get; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(int __value0, byte[] __value1, Musoq.Generated.Interpreters.Packet __value2)
            {
                s_Position = __value0;
                s_Data = __value1;
                p_ParsedFields = __value2;
            }

            public Musoq.Generated.Interpreters.Packet p_ParsedFields { get; }
            public byte[] s_Data { get; }
            public int s_Position { get; }
        }
    }
} // === SYNTAX TREE 1 ===
// <auto-generated>
// This code was generated by Musoq Interpretation Schema code generator.
// Do not modify this file directly.
// </auto-generated>
#nullable enable
using  System ;  using  System . Buffers . Binary ;  using  System . Collections . Generic ;  using  System . Dynamic ;  using  Musoq . Schema . Interpreters ; 

namespace Musoq.Generated.Interpreters
{
    /// <summary>
    /// Generated interpreter for binary schema 'Packet'.
    /// </summary>
    public sealed class Packet : BytesInterpreterBase<Packet>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Packet value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Amount"))
            {
                writer.WritePropertyName("Amount");
                var fieldValue = this.Amount;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Currency"))
            {
                writer.WritePropertyName("Currency");
                var fieldValue = this.Currency;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Amount field value.</summary>
        public int Amount { get; set; }
        /// <summary>Gets the Currency field value.</summary>
        public byte Currency { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Packet";

        /// <inheritdoc/>
        public override Packet InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new Packet();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Amount");
            var _amount = ReadInt32Le(data);
            __parsedValue.Amount = _amount;
            RecordParsedField("Amount");
            SetCurrentField("Currency");
            var _currency = ReadByte(data);
            __parsedValue.Currency = _currency;
            RecordParsedField("Currency");
            return __parsedValue;
        }
    }
}
