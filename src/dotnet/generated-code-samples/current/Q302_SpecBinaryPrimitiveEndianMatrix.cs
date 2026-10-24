// === Parsed Query ===
/*
binary PrimitiveMatrix {
                        LittleInt: int le,
                        BigInt: int be,
                        LittleShort: short le,
                        BigShort: short be,
                        LittleFloat: float le,
                        LittleDouble: double le
                    };
                    select p.LittleInt, p.BigInt, p.LittleShort, p.BigShort, p.LittleFloat, p.LittleDouble
                    from #test.files() f
                    cross apply Interpret<PrimitiveMatrix>(f.Content) p
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, p.LittleInt as p.LittleInt, p.BigInt as p.BigInt, p.LittleShort as p.LittleShort, p.BigShort as p.BigShort, p.LittleFloat as p.LittleFloat, p.LittleDouble as p.LittleDouble]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#PrimitiveMatrix(f.Content) as p]
  Project [p.LittleInt as LittleInt, p.BigInt as BigInt, p.LittleShort as LittleShort, p.BigShort as BigShort, p.LittleFloat as LittleFloat, p.LittleDouble as LittleDouble]
    CteRef [fp as fp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, p.LittleInt as p.LittleInt, p.BigInt as p.BigInt, p.LittleShort as p.LittleShort, p.BigShort as p.BigShort, p.LittleFloat as p.LittleFloat, p.LittleDouble as p.LittleDouble]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#PrimitiveMatrix(f.Content) as p]
  PhysicalProject [p.LittleInt as LittleInt, p.BigInt as BigInt, p.LittleShort as LittleShort, p.BigShort as BigShort, p.LittleFloat as LittleFloat, p.LittleDouble as LittleDouble]
    PhysicalCteRef [fp as fp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [p: object]
      LittleInt: int <- property LittleInt
      BigInt: int <- property BigInt
      LittleShort: short <- property LittleShort
      BigShort: short <- property BigShort
      LittleFloat: float <- property LittleFloat
      LittleDouble: double <- property LittleDouble
    Generated [ResultRow0]
      LittleInt: int <- field LittleInt
      BigInt: int <- field BigInt
      LittleShort: short <- field LittleShort
      BigShort: short <- field BigShort
      LittleFloat: float <- field LittleFloat
      LittleDouble: double <- field LittleDouble

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [PrimitiveMatrix.Interpret(f.Content) -> pRows]
      ScalarForEach [p in pRows]
        AppendShape [result <- ResultRow0(LittleInt: p.LittleInt, BigInt: p.BigInt, LittleShort: p.LittleShort, BigShort: p.BigShort, LittleFloat: p.LittleFloat, LittleDouble: p.LittleDouble)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q302_SpecBinaryPrimitiveEndianMatrix
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
            new Column("LittleInt", typeof(int), 0),
            new Column("BigInt", typeof(int), 1),
            new Column("LittleShort", typeof(short), 2),
            new Column("BigShort", typeof(short), 3),
            new Column("LittleFloat", typeof(float), 4),
            new Column("LittleDouble", typeof(double), 5)
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
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    var __fSchema = provider.GetSchema("#test");
                    var fRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>.Open(__fSchema, "files", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                    var fRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>(fRowsSource.Chunks, __musoqProgressContext, "f:1") : fRowsSource.Chunks;
                    foreach (var fChunk in fRows)
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
                                    var _interpreter_PrimitiveMatrix = new Musoq.Generated.Interpreters.PrimitiveMatrix();
                                    var pRows = _interpreter_PrimitiveMatrix.Interpret(f.Content);
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        yield return new ResultRow0(p.LittleInt, p.BigInt, p.LittleShort, p.BigShort, p.LittleFloat, p.LittleDouble);
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
                                    var _interpreter_PrimitiveMatrix = new Musoq.Generated.Interpreters.PrimitiveMatrix();
                                    var pRows = _interpreter_PrimitiveMatrix.Interpret(f.Content);
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        yield return new ResultRow0(p.LittleInt, p.BigInt, p.LittleShort, p.BigShort, p.LittleFloat, p.LittleDouble);
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
                            var _interpreter_PrimitiveMatrix = new Musoq.Generated.Interpreters.PrimitiveMatrix();
                            var pRows = _interpreter_PrimitiveMatrix.Interpret(f.Content);
                            if (pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = pRows;
                                yield return new ResultRow0(p.LittleInt, p.BigInt, p.LittleShort, p.BigShort, p.LittleFloat, p.LittleDouble);
                            }
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
                }
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, int __value1, short __value2, short __value3, float __value4, double __value5)
            {
                LittleInt = __value0;
                BigInt = __value1;
                LittleShort = __value2;
                BigShort = __value3;
                LittleFloat = __value4;
                LittleDouble = __value5;
            }

            public int BigInt { get; private set; }
            public short BigShort { get; private set; }
            public override int Count => 6;
            public double LittleDouble { get; private set; }
            public float LittleFloat { get; private set; }
            public int LittleInt { get; private set; }
            public short LittleShort { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.LittleInt = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.BigInt = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short>(this.LittleShort = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<short, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short>(this.BigShort = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<short, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, float>(this.LittleFloat = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<float, T>(value)),
                    5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double>(this.LittleDouble = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<double, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.LittleInt),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.BigInt),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short>(this.LittleShort),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short>(this.BigShort),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, float>(this.LittleFloat),
                5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double>(this.LittleDouble),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "LittleInt" => true,
                "BigInt" => true,
                "LittleShort" => true,
                "BigShort" => true,
                "LittleFloat" => true,
                "LittleDouble" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.LittleInt, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.BigInt, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.LittleShort, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.BigShort, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.LittleFloat, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(5, this.LittleDouble, ref visitor);
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
    /// Generated interpreter for binary schema 'PrimitiveMatrix'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class PrimitiveMatrix : BytesInterpreterBase<PrimitiveMatrix>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(PrimitiveMatrix value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("LittleInt"))
            {
                writer.WritePropertyName("LittleInt");
                var fieldValue = this.LittleInt;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("BigInt"))
            {
                writer.WritePropertyName("BigInt");
                var fieldValue = this.BigInt;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("LittleShort"))
            {
                writer.WritePropertyName("LittleShort");
                var fieldValue = this.LittleShort;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("BigShort"))
            {
                writer.WritePropertyName("BigShort");
                var fieldValue = this.BigShort;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("LittleFloat"))
            {
                writer.WritePropertyName("LittleFloat");
                var fieldValue = this.LittleFloat;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("LittleDouble"))
            {
                writer.WritePropertyName("LittleDouble");
                var fieldValue = this.LittleDouble;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the LittleInt field value.</summary>
        public int LittleInt { get; set; }
        /// <summary>Gets the BigInt field value.</summary>
        public int BigInt { get; set; }
        /// <summary>Gets the LittleShort field value.</summary>
        public short LittleShort { get; set; }
        /// <summary>Gets the BigShort field value.</summary>
        public short BigShort { get; set; }
        /// <summary>Gets the LittleFloat field value.</summary>
        public float LittleFloat { get; set; }
        /// <summary>Gets the LittleDouble field value.</summary>
        public double LittleDouble { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "PrimitiveMatrix";

        /// <inheritdoc/>
        public override PrimitiveMatrix InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new PrimitiveMatrix();
            RecordParsedValue(__parsedValue);
            SetCurrentField("LittleInt");
            var _littleInt = ReadInt32Le(data);
            __parsedValue.LittleInt = _littleInt;
            RecordParsedField("LittleInt");
            SetCurrentField("BigInt");
            var _bigInt = ReadInt32Be(data);
            __parsedValue.BigInt = _bigInt;
            RecordParsedField("BigInt");
            SetCurrentField("LittleShort");
            var _littleShort = ReadInt16Le(data);
            __parsedValue.LittleShort = _littleShort;
            RecordParsedField("LittleShort");
            SetCurrentField("BigShort");
            var _bigShort = ReadInt16Be(data);
            __parsedValue.BigShort = _bigShort;
            RecordParsedField("BigShort");
            SetCurrentField("LittleFloat");
            var _littleFloat = ReadSingleLe(data);
            __parsedValue.LittleFloat = _littleFloat;
            RecordParsedField("LittleFloat");
            SetCurrentField("LittleDouble");
            var _littleDouble = ReadDoubleLe(data);
            __parsedValue.LittleDouble = _littleDouble;
            RecordParsedField("LittleDouble");
            return __parsedValue;
        }
    }
}
