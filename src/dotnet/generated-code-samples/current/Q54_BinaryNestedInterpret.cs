// === Parsed Query ===
/*
binary Point {
                X: short le,
                Y: short le
            };
            binary Vertex {
                Id: byte,
                Position: Point
            };
            select
                v.Id,
                v.Position.X as X,
                v.Position.Y as Y
            from #test.files() f
            cross apply Interpret<Vertex>(f.Content) v
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, v.Id as v.Id, v.Position as v.Position]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#Vertex(f.Content) as v]
  Project [v.Id as Id, v.Position.X as X, v.Position.Y as Y]
    CteRef [fv as fv]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [v.Id as v.Id, v.Position as v.Position]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#Vertex(f.Content) as v]
  PhysicalProject [v.Id as Id, v.Position.X as X, v.Position.Y as Y]
    PhysicalCteRef [fv as fv]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [v: object]
      Id: byte <- property Id
      Position: object <- property Position
      Position.X: short <- nested property Position.X
      Position.Y: short <- nested property Position.Y
    Generated [ResultRow0]
      Id: byte <- field Id
      X: short <- field X
      Y: short <- field Y

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [Vertex.Interpret(f.Content) -> vRows]
      ScalarForEach [v in vRows]
        AppendShape [result <- ResultRow0(Id: v.Id, X: v.Position.X, Y: v.Position.Y)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q54_BinaryNestedInterpret
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
            new Column("Id", typeof(byte), 0),
            new Column("X", typeof(short), 1),
            new Column("Y", typeof(short), 2)
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
                                    var _interpreter_Vertex = new Musoq.Generated.Interpreters.Vertex();
                                    var vRows = _interpreter_Vertex.Interpret(f.Content);
                                    if (vRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var v = vRows;
                                        yield return new ResultRow0(v.Id, v.Position.X, v.Position.Y);
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
                                    var _interpreter_Vertex = new Musoq.Generated.Interpreters.Vertex();
                                    var vRows = _interpreter_Vertex.Interpret(f.Content);
                                    if (vRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var v = vRows;
                                        yield return new ResultRow0(v.Id, v.Position.X, v.Position.Y);
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
                            var _interpreter_Vertex = new Musoq.Generated.Interpreters.Vertex();
                            var vRows = _interpreter_Vertex.Interpret(f.Content);
                            if (vRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var v = vRows;
                                yield return new ResultRow0(v.Id, v.Position.X, v.Position.Y);
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
            public ResultRow0(byte __value0, short __value1, short __value2)
            {
                Id = __value0;
                X = __value1;
                Y = __value2;
            }

            public override int Count => 3;
            public byte Id { get; private set; }
            public short X { get; private set; }
            public short Y { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short>(this.X = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<short, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short>(this.Y = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<short, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short>(this.X),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, short>(this.Y),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "X" => true,
                "Y" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.X, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Y, ref visitor);
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
    /// Generated interpreter for binary schema 'Point'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Point : BytesInterpreterBase<Point>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Point value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("X"))
            {
                writer.WritePropertyName("X");
                var fieldValue = this.X;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Y"))
            {
                writer.WritePropertyName("Y");
                var fieldValue = this.Y;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the X field value.</summary>
        public short X { get; set; }
        /// <summary>Gets the Y field value.</summary>
        public short Y { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Point";

        /// <inheritdoc/>
        public override Point InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new Point();
            RecordParsedValue(__parsedValue);
            SetCurrentField("X");
            var _x = ReadInt16Le(data);
            __parsedValue.X = _x;
            RecordParsedField("X");
            SetCurrentField("Y");
            var _y = ReadInt16Le(data);
            __parsedValue.Y = _y;
            RecordParsedField("Y");
            return __parsedValue;
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'Vertex'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Vertex : BytesInterpreterBase<Vertex>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Vertex value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Id"))
            {
                writer.WritePropertyName("Id");
                var fieldValue = this.Id;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Position"))
            {
                writer.WritePropertyName("Position");
                var fieldValue = this.Position;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Id field value.</summary>
        public byte Id { get; set; }
        /// <summary>Gets the Position field value.</summary>
        public Point Position { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Vertex";

        /// <inheritdoc/>
        public override Vertex InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new Vertex();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Id");
            var _id = ReadByte(data);
            __parsedValue.Id = _id;
            RecordParsedField("Id");
            SetCurrentField("Position");
            var __position_interpreter = new Point();
            var _position = InterpretNested(__position_interpreter, data, "Position");
            __parsedValue.Position = _position;
            RecordParsedField("Position");
            return __parsedValue;
        }
    }
}
