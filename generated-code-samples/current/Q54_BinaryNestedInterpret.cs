/*
raw query string

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

/*
logical plan representation string

MultiStatement
  Project [f.Content as f.Content, v.Id as v.Id, v.Position as v.Position]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#Vertex(f.Content) as v]
  Project [v.Id as v.Id, v.Position.X as X, v.Position.Y as Y]
    CteRef [fv as fv]
*/

/*
physical plan representation string

PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, v.Id as v.Id, v.Position as v.Position]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#Vertex(f.Content) as v]
  PhysicalProject [v.Id as v.Id, v.Position.X as X, v.Position.Y as Y]
    PhysicalCteRef [fv as fv]
*/

/*
intermediate representation

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
      v.Id: byte <- field v_Id
      X: short <- field X
      Y: short <- field Y

  Body
    CtePhase [cte0]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [Vertex.Interpret(f.Content) -> vRows]
      ScalarForEach [v in vRows]
        AppendShape [result <- ResultShape0(v.Id: v.Id, X: v.Position.X, Y: v.Position.Y)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("v.Id", typeof(byte), 0),
            new Column("X", typeof(short), 1),
            new Column("Y", typeof(short), 2)
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.v_Id, __musoqShapeRow.X, __musoqShapeRow.Y);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                var __fSchema = provider.GetSchema("#test");
                var fRowsSource = __fSchema.GetRowSource<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>("files", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var fRows = fRowsSource.Chunks;
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
                                    __musoqFinalShapeRows.Add(new ResultShape0(v.Id, v.Position.X, v.Position.Y));
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
                                    __musoqFinalShapeRows.Add(new ResultShape0(v.Id, v.Position.X, v.Position.Y));
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
                            __musoqFinalShapeRows.Add(new ResultShape0(v.Id, v.Position.X, v.Position.Y));
                        }
                    }
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(byte __value0, short __value1, short __value2)
            {
                v_Id = __value0;
                X = __value1;
                Y = __value2;
            }

            public override int Count => 3;
            public short X { get; private set; }
            public short Y { get; private set; }
            public byte v_Id { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        v_Id = (byte)value;
                        break;
                    case 1:
                        X = (short)value;
                        break;
                    case 2:
                        Y = (short)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "v.Id" => true,
                "v_Id" => true,
                "Id" => true,
                "X" => true,
                "Y" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)v_Id,
                1 => (object)X,
                2 => (object)Y,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "v.Id" => (object)v_Id,
                "v_Id" => (object)v_Id,
                "Id" => (object)v_Id,
                "X" => (object)X,
                "Y" => (object)Y,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(byte v_Id, short X, short Y)
            {
                this.v_Id = v_Id;
                this.X = X;
                this.Y = Y;
            }

            public short X { get; }
            public short Y { get; }
            public byte v_Id { get; }
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
    public sealed class Point : BytesInterpreterBase<Point>
    {
        /// <summary>Gets the X field value.</summary>
        public short X { get; init; }
        /// <summary>Gets the Y field value.</summary>
        public short Y { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Point";

        /// <inheritdoc/>
        public override Point InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            ParsePosition = offset;
            BitOffset = 0;
            var _x = ReadInt16Le(data);
            var _y = ReadInt16Le(data);
            return new Point
            {
                X = _x,
                Y = _y
            };
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'Vertex'.
    /// </summary>
    public sealed class Vertex : BytesInterpreterBase<Vertex>
    {
        /// <summary>Gets the Id field value.</summary>
        public byte Id { get; init; }
        /// <summary>Gets the Position field value.</summary>
        public Point Position { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Vertex";

        /// <inheritdoc/>
        public override Vertex InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            ParsePosition = offset;
            BitOffset = 0;
            var _id = ReadByte(data);
            var __position_interpreter = new Point();
            var _position = __position_interpreter.InterpretAt(data, ParsePosition);
            ParsePosition = __position_interpreter.BytesConsumed;
            return new Vertex
            {
                Id = _id,
                Position = _position
            };
        }
    }
}
