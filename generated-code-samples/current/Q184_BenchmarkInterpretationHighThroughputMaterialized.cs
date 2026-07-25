// === Parsed Query ===
/*
binary TinyHeader {
                Id: int le
            };
            select h.Id
            from #test.files() f
            cross apply Interpret<TinyHeader>(f.Content) h
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, h.Id as h.Id]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#TinyHeader(f.Content) as h]
  Project [h.Id as h.Id]
    CteRef [fh as fh]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, h.Id as h.Id]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#TinyHeader(f.Content) as h]
  PhysicalProject [h.Id as h.Id]
    PhysicalCteRef [fh as fh]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [h: object]
      Id: int <- property Id
    Generated [ResultRow0]
      h.Id: int <- field h_Id

  Body
    CtePhase [cte0]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [TinyHeader.Interpret(f.Content) -> hRows]
      ScalarForEach [h in hRows]
        AppendShape [result <- ResultShape0(h.Id: h.Id)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q184_BenchmarkInterpretationHighThroughputMaterialized
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
            new Column("h.Id", typeof(int), 0)
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
            var __musoqMaterializedTable = QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
            _ = __musoqMaterializedTable.Count;
            return __musoqMaterializedTable;
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.h_Id);
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
                                var _interpreter_TinyHeader = new Musoq.Generated.Interpreters.TinyHeader();
                                var hRows = _interpreter_TinyHeader.Interpret(f.Content);
                                if (hRows != null)
                                {
                                    token.ThrowIfCancellationRequested();
                                    var h = hRows;
                                    __musoqFinalShapeRows.Add(new ResultShape0(h.Id));
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
                                var _interpreter_TinyHeader = new Musoq.Generated.Interpreters.TinyHeader();
                                var hRows = _interpreter_TinyHeader.Interpret(f.Content);
                                if (hRows != null)
                                {
                                    token.ThrowIfCancellationRequested();
                                    var h = hRows;
                                    __musoqFinalShapeRows.Add(new ResultShape0(h.Id));
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
                        var _interpreter_TinyHeader = new Musoq.Generated.Interpreters.TinyHeader();
                        var hRows = _interpreter_TinyHeader.Interpret(f.Content);
                        if (hRows != null)
                        {
                            token.ThrowIfCancellationRequested();
                            var h = hRows;
                            __musoqFinalShapeRows.Add(new ResultShape0(h.Id));
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
            public ResultRow0(int __value0)
            {
                h_Id = __value0;
            }

            public override int Count => 1;
            public int h_Id { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        h_Id = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "h.Id" => true,
                "h_Id" => true,
                "Id" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)h_Id,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "h.Id" => (object)h_Id,
                "h_Id" => (object)h_Id,
                "Id" => (object)h_Id,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int h_Id)
            {
                this.h_Id = h_Id;
            }

            public int h_Id { get; }
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
    /// Generated interpreter for binary schema 'TinyHeader'.
    /// </summary>
    public sealed class TinyHeader : BytesInterpreterBase<TinyHeader>
    {
        /// <summary>Gets the Id field value.</summary>
        public int Id { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "TinyHeader";

        /// <inheritdoc/>
        public override TinyHeader InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            ParsePosition = offset;
            BitOffset = 0;
            var _id = ReadInt32Le(data);
            return new TinyHeader
            {
                Id = _id
            };
        }
    }
}
