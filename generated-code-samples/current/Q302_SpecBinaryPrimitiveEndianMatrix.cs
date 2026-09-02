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
  Project [p.LittleInt as p.LittleInt, p.BigInt as p.BigInt, p.LittleShort as p.LittleShort, p.BigShort as p.BigShort, p.LittleFloat as p.LittleFloat, p.LittleDouble as p.LittleDouble]
    CteRef [fp as fp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, p.LittleInt as p.LittleInt, p.BigInt as p.BigInt, p.LittleShort as p.LittleShort, p.BigShort as p.BigShort, p.LittleFloat as p.LittleFloat, p.LittleDouble as p.LittleDouble]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#PrimitiveMatrix(f.Content) as p]
  PhysicalProject [p.LittleInt as p.LittleInt, p.BigInt as p.BigInt, p.LittleShort as p.LittleShort, p.BigShort as p.BigShort, p.LittleFloat as p.LittleFloat, p.LittleDouble as p.LittleDouble]
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
      p.LittleInt: int <- field p_LittleInt
      p.BigInt: int <- field p_BigInt
      p.LittleShort: short <- field p_LittleShort
      p.BigShort: short <- field p_BigShort
      p.LittleFloat: float <- field p_LittleFloat
      p.LittleDouble: double <- field p_LittleDouble

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [PrimitiveMatrix.Interpret(f.Content) -> pRows]
      ScalarForEach [p in pRows]
        AppendShape [result <- ResultShape0(p.LittleInt: p.LittleInt, p.BigInt: p.BigInt, p.LittleShort: p.LittleShort, p.BigShort: p.BigShort, p.LittleFloat: p.LittleFloat, p.LittleDouble: p.LittleDouble)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("p.LittleInt", typeof(int), 0),
            new Column("p.BigInt", typeof(int), 1),
            new Column("p.LittleShort", typeof(short), 2),
            new Column("p.BigShort", typeof(short), 3),
            new Column("p.LittleFloat", typeof(float), 4),
            new Column("p.LittleDouble", typeof(double), 5)
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
        public event QueryProgressEventHandler QueryProgress;
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.p_LittleInt, __musoqShapeRow.p_BigInt, __musoqShapeRow.p_LittleShort, __musoqShapeRow.p_BigShort, __musoqShapeRow.p_LittleFloat, __musoqShapeRow.p_LittleDouble);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    var __fSchema = provider.GetSchema("#test");
                    var fRowsSource = __fSchema.GetRowSource<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>("files", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), Array.Empty<object>());
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
                                        yield return new ResultShape0(p.LittleInt, p.BigInt, p.LittleShort, p.BigShort, p.LittleFloat, p.LittleDouble);
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
                                        yield return new ResultShape0(p.LittleInt, p.BigInt, p.LittleShort, p.BigShort, p.LittleFloat, p.LittleDouble);
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
                                yield return new ResultShape0(p.LittleInt, p.BigInt, p.LittleShort, p.BigShort, p.LittleFloat, p.LittleDouble);
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
                p_LittleInt = __value0;
                p_BigInt = __value1;
                p_LittleShort = __value2;
                p_BigShort = __value3;
                p_LittleFloat = __value4;
                p_LittleDouble = __value5;
            }

            public override int Count => 6;
            public int p_BigInt { get; private set; }
            public short p_BigShort { get; private set; }
            public double p_LittleDouble { get; private set; }
            public float p_LittleFloat { get; private set; }
            public int p_LittleInt { get; private set; }
            public short p_LittleShort { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        p_LittleInt = (int)value;
                        break;
                    case 1:
                        p_BigInt = (int)value;
                        break;
                    case 2:
                        p_LittleShort = (short)value;
                        break;
                    case 3:
                        p_BigShort = (short)value;
                        break;
                    case 4:
                        p_LittleFloat = (float)value;
                        break;
                    case 5:
                        p_LittleDouble = (double)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "p.LittleInt" => true,
                "p_LittleInt" => true,
                "LittleInt" => true,
                "p.BigInt" => true,
                "p_BigInt" => true,
                "BigInt" => true,
                "p.LittleShort" => true,
                "p_LittleShort" => true,
                "LittleShort" => true,
                "p.BigShort" => true,
                "p_BigShort" => true,
                "BigShort" => true,
                "p.LittleFloat" => true,
                "p_LittleFloat" => true,
                "LittleFloat" => true,
                "p.LittleDouble" => true,
                "p_LittleDouble" => true,
                "LittleDouble" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)p_LittleInt,
                1 => (object)p_BigInt,
                2 => (object)p_LittleShort,
                3 => (object)p_BigShort,
                4 => (object)p_LittleFloat,
                5 => (object)p_LittleDouble,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "p.LittleInt" => (object)p_LittleInt,
                "p_LittleInt" => (object)p_LittleInt,
                "LittleInt" => (object)p_LittleInt,
                "p.BigInt" => (object)p_BigInt,
                "p_BigInt" => (object)p_BigInt,
                "BigInt" => (object)p_BigInt,
                "p.LittleShort" => (object)p_LittleShort,
                "p_LittleShort" => (object)p_LittleShort,
                "LittleShort" => (object)p_LittleShort,
                "p.BigShort" => (object)p_BigShort,
                "p_BigShort" => (object)p_BigShort,
                "BigShort" => (object)p_BigShort,
                "p.LittleFloat" => (object)p_LittleFloat,
                "p_LittleFloat" => (object)p_LittleFloat,
                "LittleFloat" => (object)p_LittleFloat,
                "p.LittleDouble" => (object)p_LittleDouble,
                "p_LittleDouble" => (object)p_LittleDouble,
                "LittleDouble" => (object)p_LittleDouble,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int p_LittleInt, int p_BigInt, short p_LittleShort, short p_BigShort, float p_LittleFloat, double p_LittleDouble)
            {
                this.p_LittleInt = p_LittleInt;
                this.p_BigInt = p_BigInt;
                this.p_LittleShort = p_LittleShort;
                this.p_BigShort = p_BigShort;
                this.p_LittleFloat = p_LittleFloat;
                this.p_LittleDouble = p_LittleDouble;
            }

            public int p_BigInt { get; }
            public short p_BigShort { get; }
            public double p_LittleDouble { get; }
            public float p_LittleFloat { get; }
            public int p_LittleInt { get; }
            public short p_LittleShort { get; }
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
    public sealed class PrimitiveMatrix : BytesInterpreterBase<PrimitiveMatrix>
    {
        /// <summary>Gets the LittleInt field value.</summary>
        public int LittleInt { get; init; }
        /// <summary>Gets the BigInt field value.</summary>
        public int BigInt { get; init; }
        /// <summary>Gets the LittleShort field value.</summary>
        public short LittleShort { get; init; }
        /// <summary>Gets the BigShort field value.</summary>
        public short BigShort { get; init; }
        /// <summary>Gets the LittleFloat field value.</summary>
        public float LittleFloat { get; init; }
        /// <summary>Gets the LittleDouble field value.</summary>
        public double LittleDouble { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "PrimitiveMatrix";

        /// <inheritdoc/>
        public override PrimitiveMatrix InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("LittleInt");
            var _littleInt = ReadInt32Le(data);
            RecordParsedField("LittleInt", _littleInt);
            SetCurrentField("BigInt");
            var _bigInt = ReadInt32Be(data);
            RecordParsedField("BigInt", _bigInt);
            SetCurrentField("LittleShort");
            var _littleShort = ReadInt16Le(data);
            RecordParsedField("LittleShort", _littleShort);
            SetCurrentField("BigShort");
            var _bigShort = ReadInt16Be(data);
            RecordParsedField("BigShort", _bigShort);
            SetCurrentField("LittleFloat");
            var _littleFloat = ReadSingleLe(data);
            RecordParsedField("LittleFloat", _littleFloat);
            SetCurrentField("LittleDouble");
            var _littleDouble = ReadDoubleLe(data);
            RecordParsedField("LittleDouble", _littleDouble);
            return new PrimitiveMatrix
            {
                LittleInt = _littleInt,
                BigInt = _bigInt,
                LittleShort = _littleShort,
                BigShort = _bigShort,
                LittleFloat = _littleFloat,
                LittleDouble = _littleDouble
            };
        }
    }
}
