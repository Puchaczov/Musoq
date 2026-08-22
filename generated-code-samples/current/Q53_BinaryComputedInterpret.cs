// === Parsed Query ===
/*
binary Rectangle {
                Width: int le,
                Height: int le,
                Area: Width * Height
            };
            select
                r.Width,
                r.Height,
                r.Area
            from #test.files() f
            cross apply Interpret<Rectangle>(f.Content) r
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, r.Width as r.Width, r.Height as r.Height, r.Area as r.Area]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#Rectangle(f.Content) as r]
  Project [r.Width as r.Width, r.Height as r.Height, r.Area as r.Area]
    CteRef [fr as fr]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, r.Width as r.Width, r.Height as r.Height, r.Area as r.Area]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#Rectangle(f.Content) as r]
  PhysicalProject [r.Width as r.Width, r.Height as r.Height, r.Area as r.Area]
    PhysicalCteRef [fr as fr]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [r: object]
      Width: int <- property Width
      Height: int <- property Height
      Area: object <- property Area
    Generated [ResultRow0]
      r.Width: int <- field r_Width
      r.Height: int <- field r_Height
      r.Area: int <- field r_Area

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [Rectangle.Interpret(f.Content) -> rRows]
      ScalarForEach [r in rRows]
        AppendShape [result <- ResultShape0(r.Width: r.Width, r.Height: r.Height, r.Area: r.Area)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q53_BinaryComputedInterpret
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
            new Column("r.Width", typeof(int), 0),
            new Column("r.Height", typeof(int), 1),
            new Column("r.Area", typeof(int), 2)
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
                yield return new ResultRow0(__musoqShapeRow.r_Width, __musoqShapeRow.r_Height, __musoqShapeRow.r_Area);
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
                                    var _interpreter_Rectangle = new Musoq.Generated.Interpreters.Rectangle();
                                    var rRows = _interpreter_Rectangle.Interpret(f.Content);
                                    if (rRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var r = rRows;
                                        yield return new ResultShape0(r.Width, r.Height, r.Area);
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
                                    var _interpreter_Rectangle = new Musoq.Generated.Interpreters.Rectangle();
                                    var rRows = _interpreter_Rectangle.Interpret(f.Content);
                                    if (rRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var r = rRows;
                                        yield return new ResultShape0(r.Width, r.Height, r.Area);
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
                            var _interpreter_Rectangle = new Musoq.Generated.Interpreters.Rectangle();
                            var rRows = _interpreter_Rectangle.Interpret(f.Content);
                            if (rRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var r = rRows;
                                yield return new ResultShape0(r.Width, r.Height, r.Area);
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
            public ResultRow0(int __value0, int __value1, int __value2)
            {
                r_Width = __value0;
                r_Height = __value1;
                r_Area = __value2;
            }

            public override int Count => 3;
            public int r_Area { get; private set; }
            public int r_Height { get; private set; }
            public int r_Width { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        r_Width = (int)value;
                        break;
                    case 1:
                        r_Height = (int)value;
                        break;
                    case 2:
                        r_Area = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "r.Width" => true,
                "r_Width" => true,
                "Width" => true,
                "r.Height" => true,
                "r_Height" => true,
                "Height" => true,
                "r.Area" => true,
                "r_Area" => true,
                "Area" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)r_Width,
                1 => (object)r_Height,
                2 => (object)r_Area,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "r.Width" => (object)r_Width,
                "r_Width" => (object)r_Width,
                "Width" => (object)r_Width,
                "r.Height" => (object)r_Height,
                "r_Height" => (object)r_Height,
                "Height" => (object)r_Height,
                "r.Area" => (object)r_Area,
                "r_Area" => (object)r_Area,
                "Area" => (object)r_Area,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int r_Width, int r_Height, int r_Area)
            {
                this.r_Width = r_Width;
                this.r_Height = r_Height;
                this.r_Area = r_Area;
            }

            public int r_Area { get; }
            public int r_Height { get; }
            public int r_Width { get; }
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
    /// Generated interpreter for binary schema 'Rectangle'.
    /// </summary>
    public sealed class Rectangle : BytesInterpreterBase<Rectangle>
    {
        /// <summary>Gets the Width field value.</summary>
        public int Width { get; init; }
        /// <summary>Gets the Height field value.</summary>
        public int Height { get; init; }
        /// <summary>Gets the Area field value.</summary>
        public int Area { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Rectangle";

        /// <inheritdoc/>
        public override Rectangle InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            ParsePosition = offset;
            BitOffset = 0;
            var _width = ReadInt32Le(data);
            var _height = ReadInt32Le(data);
            var _area = (int)((_width * _height));
            return new Rectangle
            {
                Width = _width,
                Height = _height,
                Area = _area
            };
        }
    }
}
