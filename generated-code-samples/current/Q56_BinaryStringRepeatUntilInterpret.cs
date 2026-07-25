// === Parsed Query ===
/*
binary StringRepeatPacket {
                Names: string[3] ascii repeat until Names = 'END'
            };
            select
                n.Value as Text
            from #test.files() f
            cross apply Interpret<StringRepeatPacket>(f.Content) p
            cross apply p.Names n
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, p.Names as p.Names]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#StringRepeatPacket(f.Content) as p]
  Project [fp.f.Content as f.Content, fp.p.Names as p.Names, n.Value as n.Value]
    Apply [Cross]
      CteRef [fp as fp]
      PropertySource [fp.p.Names as n] [apply: Cross] [type: String[]]
  Project [n.Value as Text]
    CteRef [fpn as fpn]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, p.Names as p.Names]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#StringRepeatPacket(f.Content) as p]
  PhysicalProject [fp.f.Content as f.Content, fp.p.Names as p.Names, n.Value as n.Value]
    PhysicalNestedLoopApply [Cross]
      PhysicalCteRef [fp as fp]
      PhysicalPropertySource [fp.p.Names as n] [apply: Cross] [type: String[]]
  PhysicalProject [n.Value as Text]
    PhysicalCteRef [fpn as fpn]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [p: object]
      Names: string[] <- property Names
    Generated [Statement0Row0]
      f.Content: byte[] <- field f_Content
      p.Names: string[] <- field p_Names
    TableRow [fp]
      f.Content: byte[] <- field f_Content
      p.Names: string[] <- field p_Names
    SourceEntity [n: string]
      Value: string <- direct scalar value
    Generated [ResultRow0]
      Text: string <- field Text

  Body
    SourceScan [f: BinaryEntity] -> statement0_fRows
    CreateTable [statement0: Statement0Row0]
    ChunkedForEach [f in statement0_fRows]
      InterpretSource [StringRepeatPacket.Interpret(f.Content) -> statement0_pRows]
      ScalarForEach [p in statement0_pRows]
        AppendRow [statement0 <- Statement0Row0(f.Content: f.Content, p.Names: p.Names)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CtePhase [cte1]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [fp in _cteRowResults.Slot0]
      EnumerableSource [fp.p.Names -> nRows]
      ChunkedForEach [n in nRows]
        AppendShape [result <- ResultShape0(Text: n.Value)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q56_BinaryStringRepeatUntilInterpret
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Text", typeof(string), 0)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("f.Content", typeof(byte[]), 0),
            new Column("p.Names", typeof(string[]), 1)
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Text);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 fp = __storedTable0Rows[__storedTable0Index];
                    var nRows = EvaluationHelper.ConvertEnumerableOutputToChunks<string>(fp.p_Names);
                    foreach (var nChunk in nRows)
                    {
                        if (nChunk is global::Musoq.Schema.DataSources.RowChunk<string> nChunkView)
                        {
                            if (nChunkView.Source is string[] nChunkViewArray)
                            {
                                int nChunkViewOffset = nChunkView.Offset;
                                for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                                {
                                    if ((nIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var n = nChunkViewArray[nChunkViewOffset + nIndex];
                                    __musoqFinalShapeRows.Add(new ResultShape0(n));
                                }

                                continue;
                            }

                            if (nChunkView.Source is List<string> nChunkViewList)
                            {
                                int nChunkViewOffset = nChunkView.Offset;
                                for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                                {
                                    if ((nIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var n = nChunkViewList[nChunkViewOffset + nIndex];
                                    __musoqFinalShapeRows.Add(new ResultShape0(n));
                                }

                                continue;
                            }
                        }

                        for (int nIndex = 0, nIndexCount = nChunk.Count; nIndex < nIndexCount; ++nIndex)
                        {
                            if ((nIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var n = nChunk[nIndex];
                            __musoqFinalShapeRows.Add(new ResultShape0(n));
                        }
                    }
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, CteRowResults _cteRowResults)
        {
            var __statement0_fSchema = provider.GetSchema("#test");
            var statement0_fRowsSource = __statement0_fSchema.GetRowSource<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>("files", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var statement0_fRows = statement0_fRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            foreach (var fChunk in statement0_fRows)
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
                            var _interpreter_StringRepeatPacket = new Musoq.Generated.Interpreters.StringRepeatPacket();
                            var statement0_pRows = _interpreter_StringRepeatPacket.Interpret(f.Content);
                            if (statement0_pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = statement0_pRows;
                                statement0.Add(new Statement0Row0(f.Content, p.Names));
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
                            var _interpreter_StringRepeatPacket = new Musoq.Generated.Interpreters.StringRepeatPacket();
                            var statement0_pRows = _interpreter_StringRepeatPacket.Interpret(f.Content);
                            if (statement0_pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = statement0_pRows;
                                statement0.Add(new Statement0Row0(f.Content, p.Names));
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
                    var _interpreter_StringRepeatPacket = new Musoq.Generated.Interpreters.StringRepeatPacket();
                    var statement0_pRows = _interpreter_StringRepeatPacket.Interpret(f.Content);
                    if (statement0_pRows != null)
                    {
                        token.ThrowIfCancellationRequested();
                        var p = statement0_pRows;
                        statement0.Add(new Statement0Row0(f.Content, p.Names));
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
            public ResultRow0(string __value0)
            {
                Text = __value0;
            }

            public override int Count => 1;
            public string Text { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Text = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Text" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Text,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "Text" => (object)Text,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Text)
            {
                this.Text = Text;
            }

            public string Text { get; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(byte[] __value0, string[] __value1)
            {
                f_Content = __value0;
                p_Names = __value1;
            }

            public byte[] f_Content { get; }
            public string[] p_Names { get; }
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
    /// Generated interpreter for binary schema 'StringRepeatPacket'.
    /// </summary>
    public sealed class StringRepeatPacket : BytesInterpreterBase<StringRepeatPacket>
    {
        /// <summary>Gets the Names field value.</summary>
        public string[] Names { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "StringRepeatPacket";

        /// <inheritdoc/>
        public override StringRepeatPacket InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            ParsePosition = offset;
            BitOffset = 0;
            var __names_list = new System.Collections.Generic.List<string>();
            string __names_lastElem;
            do
            {
                __names_lastElem = ReadString(data, 3, System.Text.Encoding.ASCII);
                __names_list.Add(__names_lastElem);
            }
            while (!((__names_lastElem == "END")));
            var _names = __names_list.ToArray();
            return new StringRepeatPacket
            {
                Names = _names
            };
        }
    }
}
