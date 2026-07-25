// === Parsed Query ===
/*
binary OptionalPacket {
                HasValue: byte,
                Value: int le when HasValue = 1
            };
            select
                p.HasValue,
                p.Value
            from #test.files() f
            cross apply Interpret<OptionalPacket>(f.Content) p
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, p.HasValue as p.HasValue, p.Value as p.Value]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#OptionalPacket(f.Content) as p]
  Project [p.HasValue as p.HasValue, p.Value as p.Value]
    CteRef [fp as fp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, p.HasValue as p.HasValue, p.Value as p.Value]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#OptionalPacket(f.Content) as p]
  PhysicalProject [p.HasValue as p.HasValue, p.Value as p.Value]
    PhysicalCteRef [fp as fp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [p: object]
      HasValue: byte <- property HasValue
      Value: int? <- property Value
    Generated [ResultRow0]
      p.HasValue: byte <- field p_HasValue
      p.Value: int? <- field p_Value

  Body
    CtePhase [cte0]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [OptionalPacket.Interpret(f.Content) -> pRows]
      ScalarForEach [p in pRows]
        AppendShape [result <- ResultShape0(p.HasValue: p.HasValue, p.Value: p.Value)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q51_BinaryConditionalInterpret
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
            new Column("p.HasValue", typeof(byte), 0),
            new Column("p.Value", typeof(int?), 1)
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
                yield return new ResultRow0(__musoqShapeRow.p_HasValue, __musoqShapeRow.p_Value);
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
                                var _interpreter_OptionalPacket = new Musoq.Generated.Interpreters.OptionalPacket();
                                var pRows = _interpreter_OptionalPacket.Interpret(f.Content);
                                if (pRows != null)
                                {
                                    token.ThrowIfCancellationRequested();
                                    var p = pRows;
                                    __musoqFinalShapeRows.Add(new ResultShape0(p.HasValue, p.Value));
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
                                var _interpreter_OptionalPacket = new Musoq.Generated.Interpreters.OptionalPacket();
                                var pRows = _interpreter_OptionalPacket.Interpret(f.Content);
                                if (pRows != null)
                                {
                                    token.ThrowIfCancellationRequested();
                                    var p = pRows;
                                    __musoqFinalShapeRows.Add(new ResultShape0(p.HasValue, p.Value));
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
                        var _interpreter_OptionalPacket = new Musoq.Generated.Interpreters.OptionalPacket();
                        var pRows = _interpreter_OptionalPacket.Interpret(f.Content);
                        if (pRows != null)
                        {
                            token.ThrowIfCancellationRequested();
                            var p = pRows;
                            __musoqFinalShapeRows.Add(new ResultShape0(p.HasValue, p.Value));
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
            public ResultRow0(byte __value0, int? __value1)
            {
                p_HasValue = __value0;
                p_Value = __value1;
            }

            public override int Count => 2;
            public byte p_HasValue { get; private set; }
            public int? p_Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        p_HasValue = (byte)value;
                        break;
                    case 1:
                        p_Value = (int?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "p.HasValue" => true,
                "p_HasValue" => true,
                "HasValue" => true,
                "p.Value" => true,
                "p_Value" => true,
                "Value" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)p_HasValue,
                1 => (object)p_Value,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "p.HasValue" => (object)p_HasValue,
                "p_HasValue" => (object)p_HasValue,
                "HasValue" => (object)p_HasValue,
                "p.Value" => (object)p_Value,
                "p_Value" => (object)p_Value,
                "Value" => (object)p_Value,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(byte p_HasValue, int? p_Value)
            {
                this.p_HasValue = p_HasValue;
                this.p_Value = p_Value;
            }

            public byte p_HasValue { get; }
            public int? p_Value { get; }
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
    /// Generated interpreter for binary schema 'OptionalPacket'.
    /// </summary>
    public sealed class OptionalPacket : BytesInterpreterBase<OptionalPacket>
    {
        /// <summary>Gets the HasValue field value.</summary>
        public byte HasValue { get; init; }
        /// <summary>Gets the Value field value.</summary>
        public int? Value { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "OptionalPacket";

        /// <inheritdoc/>
        public override OptionalPacket InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            ParsePosition = offset;
            BitOffset = 0;
            var _hasValue = ReadByte(data);
            int? _value = null;
            if ((_hasValue == 1))
            {
                _value = ReadInt32Le(data);
            }

            return new OptionalPacket
            {
                HasValue = _hasValue,
                Value = _value
            };
        }
    }
}
