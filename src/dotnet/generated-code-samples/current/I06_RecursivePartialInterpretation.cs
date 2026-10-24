// === Parsed Query ===
/*
binary SampleBlock { ValueCount: byte, Values: byte[ValueCount] } binary SampleBatch { Blocks: SampleBlock[2] } with recursive offs (Bytes, Off, Used) as (select f.Content, 0, 0 from #test.files() f union all select o.Bytes, o.Off + o.Used, p.BytesConsumed from offs o cross apply PartialInterpret<SampleBatch>(FromBase64(ToBase64(o.Bytes, o.Off + o.Used, Length(o.Bytes) - o.Off - o.Used))) p where o.Off + o.Used < Length(o.Bytes)) select o.Off, o.Used from offs o
*/

// === Logical Plan ===
/*
Cte
  Definition [offs]
    RecursiveCte [offs] [All]
      Anchor
        MultiStatement
          Project [f.Content as Bytes, 0 as Off, 0 as Used]
            SchemaScan [#test.files() as f]
      RecursiveMember
        MultiStatement
          Project [o.Bytes as o.Bytes, o.Off as o.Off, o.Used as o.Used, p.BytesConsumed as p.BytesConsumed]
            Apply [Cross]
              CteRef [offs as o]
              InterpretSource [#SampleBatch(FromBase64(ToBase64(o.Bytes, (o.Off + o.Used), ((Length(o.Bytes) - o.Off) - o.Used)))) as p]
          Project [o.Bytes as Bytes, (o.Off + o.Used) as o.Off + o.Used, p.BytesConsumed as BytesConsumed]
            Filter [((o.Off + o.Used) < Length(o.Bytes))]
              CteRef [op as op]
  Query
    MultiStatement
      Project [o.Off as Off, o.Used as Used]
        CteRef [offs as o]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [offs]
    PhysicalRecursiveCte [offs] [All]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [f.Content as Bytes, 0 as Off, 0 as Used]
            PhysicalSchemaScan [#test.files() as f]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [o.Bytes as o.Bytes, o.Off as o.Off, o.Used as o.Used, p.BytesConsumed as p.BytesConsumed]
            PhysicalNestedLoopApply [Cross] [guards: PreApplyRight: ((o.Off + o.Used) < Length(o.Bytes))]
              PhysicalCteRef [offs as o]
              PhysicalInterpretSource [#SampleBatch(FromBase64(ToBase64(o.Bytes, (o.Off + o.Used), ((Length(o.Bytes) - o.Off) - o.Used)))) as p]
          PhysicalProject [o.Bytes as Bytes, (o.Off + o.Used) as o.Off + o.Used, p.BytesConsumed as BytesConsumed]
            PhysicalFilter [((o.Off + o.Used) < Length(o.Bytes))]
              PhysicalCteRef [op as op]
  Query
    PhysicalMultiStatement
      PhysicalProject [o.Off as Off, o.Used as Used]
        PhysicalCteRef [offs as o]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    Generated [Cte0Row0]
      Bytes: byte[] <- field Bytes
      Off: int <- field Off
      Used: int <- field Used
    TableRow [o]
      Bytes: byte[] <- field Bytes
      Off: int <- field Off
      Used: int <- field Used
    SourceEntity [p: object]
      ParsedFields: object <- property ParsedFields
      ErrorField: string <- property ErrorField
      ErrorMessage: string <- property ErrorMessage
      BytesConsumed: int <- property BytesConsumed
      CompletedFields: IReadOnlySet<string> <- property CompletedFields
      IsSuccess: bool <- property IsSuccess
      BitOffset: int <- property BitOffset
    TableRow [o]
      Bytes: byte[] <- field Bytes
      Off: int <- field Off
      Used: int <- field Used
    Generated [ResultRow0]
      Off: int <- field Off
      Used: int <- field Used

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    RecursiveCte [offs; result cte0; frontiers cte0CurrentFrontier, cte0NextFrontier; identity none; max iterations 1000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        SourceScan [f: BinaryEntity] -> cte0CurrentFrontier_fRows
        ChunkedForEach [f in cte0CurrentFrontier_fRows]
          RecursiveAppend [cte0CurrentFrontier <- Cte0Row0(Bytes: f.Content, Off: 0, Used: 0); identity none; guard cte0.Count + cte0CurrentFrontier.Count < 10000000]
      RecursiveMember
        ForEach [o in cte0CurrentFrontier]
          CreateObject [__compiled5LibraryBase0: LibraryBase]
          ContinueIf [NOT (((o.Off + o.Used) < Length(o.Bytes)) = TRUE)]
          InterpretSource [SampleBatch.PartialInterpret(FromBase64(ToBase64(o.Bytes, (o.Off + o.Used), ((Length(o.Bytes) - o.Off) - o.Used)))) -> cte0NextFrontier_pRows]
          ScalarForEach [p in cte0NextFrontier_pRows]
            RecursiveAppend [cte0NextFrontier <- Cte0Row0(Bytes: o.Bytes, Off: (o.Off + o.Used), Used: p.BytesConsumed); identity none; guard cte0.Count + cte0NextFrontier.Count < 10000000]
    PhaseBoundary [Where:cte0]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [From]
    ForEach [o in _cteRowResults.Slot0]
      AppendShape [result <- ResultRow0(Off: o.Off, Used: o.Used)]
    PhaseBoundary [Select]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_I06_RecursivePartialInterpretation
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
            new Column("Off", typeof(int), 0),
            new Column("Used", typeof(int), 1)
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.From);
                    var cte0 = new List<Cte0Row0>();
                    var cte0CurrentFrontier = new List<Cte0Row0>();
                    var cte0NextFrontier = new List<Cte0Row0>();
                    int __cte0Iteration = 0;
                    int __cte0CancellationCounter = 0;
                    var __cte0CurrentFrontier_fSchema = provider.GetSchema("#test");
                    var cte0CurrentFrontier_fRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>.Open(__cte0CurrentFrontier_fSchema, "files", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                    var cte0CurrentFrontier_fRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>(cte0CurrentFrontier_fRowsSource.Chunks, __musoqProgressContext, "f:1") : cte0CurrentFrontier_fRowsSource.Chunks;
                    foreach (var fChunk in cte0CurrentFrontier_fRows)
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
                                    if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                    {
                                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("offs", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                    }

                                    cte0CurrentFrontier.Add(new Cte0Row0(f.Content, 0, 0));
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
                                    if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                    {
                                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("offs", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                    }

                                    cte0CurrentFrontier.Add(new Cte0Row0(f.Content, 0, 0));
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
                            if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                            {
                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("offs", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                            }

                            cte0CurrentFrontier.Add(new Cte0Row0(f.Content, 0, 0));
                        }
                    }

                    cte0.AddRange(cte0CurrentFrontier);
                    while (cte0CurrentFrontier.Count > 0)
                    {
                        if ((__cte0Iteration & 63) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        if (__cte0Iteration >= 1000)
                        {
                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("offs", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 1000);
                        }

                        __cte0Iteration++;
                        cte0NextFrontier.Clear();
                        for (int cte0CurrentFrontierIndex = 0; cte0CurrentFrontierIndex < cte0CurrentFrontier.Count; ++cte0CurrentFrontierIndex)
                        {
                            if (cte0CurrentFrontierIndex != 0 && (cte0CurrentFrontierIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Cte0Row0 o = (Cte0Row0)cte0CurrentFrontier[cte0CurrentFrontierIndex];
                            var __compiled5LibraryBase0 = new Musoq.Plugins.LibraryBase();
                            if ((!(Operators.SqlCompare<int, int?>((o.Off + o.Used), (int?)__compiled5LibraryBase0.Length<byte>(o.Bytes), (int __sqlLeft, int? __sqlRight) => (__sqlLeft < __sqlRight)) == true)))
                            {
                                continue;
                            }

                            var _interpreter_SampleBatch = new Musoq.Generated.Interpreters.SampleBatch();
                            var cte0NextFrontier_pRows = _interpreter_SampleBatch.PartialInterpret((byte[])__compiled5LibraryBase0.FromBase64((string)__compiled5LibraryBase0.ToBase64(o.Bytes, (o.Off + o.Used), (int?)((int?)((int?)__compiled5LibraryBase0.Length<byte>(o.Bytes) - o.Off) - o.Used))));
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
                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("offs", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                }

                                cte0NextFrontier.Add(new Cte0Row0(o.Bytes, (o.Off + o.Used), p.BytesConsumed));
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

                OnPhaseChanged("compiled", QueryPhase.From);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 o = __storedTable0Rows[__storedTable0Index];
                    __musoqFinalShapeRows.Add(new ResultRow0(o.Off, o.Used));
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
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

        private readonly struct Cte0Row0
        {
            public Cte0Row0(byte[] Bytes, int Off, int Used)
            {
                this.Bytes = Bytes;
                this.Off = Off;
                this.Used = Used;
            }

            public byte[] Bytes { get; }
            public int Off { get; }
            public int Used { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, int __value1)
            {
                Off = __value0;
                Used = __value1;
            }

            public override int Count => 2;
            public int Off { get; private set; }
            public int Used { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Off = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Used = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Off),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Used),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Off" => true,
                "Used" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Off, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Used, ref visitor);
            }
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
    /// Generated interpreter for binary schema 'SampleBlock'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class SampleBlock : BytesInterpreterBase<SampleBlock>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(SampleBlock value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("ValueCount"))
            {
                writer.WritePropertyName("ValueCount");
                var fieldValue = this.ValueCount;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Values"))
            {
                writer.WritePropertyName("Values");
                var fieldValue = this.Values;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the ValueCount field value.</summary>
        public byte ValueCount { get; set; }
        /// <summary>Gets the Values field value.</summary>
        public byte[] Values { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "SampleBlock";

        /// <inheritdoc/>
        public override SampleBlock InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new SampleBlock();
            RecordParsedValue(__parsedValue);
            SetCurrentField("ValueCount");
            var _valueCount = ReadByte(data);
            __parsedValue.ValueCount = _valueCount;
            RecordParsedField("ValueCount");
            SetCurrentField("Values");
            var _values = ReadBytes(data, ResolveExpressionSize<byte>(() => checked((byte?)(_valueCount)), ParseErrorCode.InvalidSize));
            __parsedValue.Values = _values;
            RecordParsedField("Values");
            return __parsedValue;
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'SampleBatch'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class SampleBatch : BytesInterpreterBase<SampleBatch>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(SampleBatch value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Blocks"))
            {
                writer.WritePropertyName("Blocks");
                var fieldValue = this.Blocks;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Blocks field value.</summary>
        public SampleBlock[] Blocks { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "SampleBatch";

        /// <inheritdoc/>
        public override SampleBatch InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new SampleBatch();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Blocks");
            var __blocks_list = new System.Collections.Generic.List<SampleBlock>();
            var __blocks_count = 2;
            for (int __blocks_i = 0; __blocks_i < __blocks_count; __blocks_i++)
            {
                var ___blocks_elem_interpreter = new SampleBlock();
                var __blocks_elem = InterpretNested(___blocks_elem_interpreter, data, "Blocks");
                __blocks_list.Add(__blocks_elem);
            }

            var _blocks = __blocks_list.ToArray();
            __parsedValue.Blocks = _blocks;
            RecordParsedField("Blocks");
            return __parsedValue;
        }
    }
}
