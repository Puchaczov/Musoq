// === Parsed Query ===
/*
binary BitsRepeatPacket {
                Flags: bits[1] repeat until Flags = 0
            };
            select
                f.Value as FlagValue
            from #test.files() file
            cross apply Interpret<BitsRepeatPacket>(file.Content) p
            cross apply p.Flags f
*/

// === Logical Plan ===
/*
MultiStatement
  Project [file.Content as file.Content, p.Flags as p.Flags]
    Apply [Cross]
      SchemaScan [#test.files() as file]
      InterpretSource [#BitsRepeatPacket(file.Content) as p]
  Project [filep.file.Content as file.Content, filep.p.Flags as p.Flags, f.Value as f.Value]
    Apply [Cross]
      CteRef [filep as filep]
      PropertySource [filep.p.Flags as f] [apply: Cross] [type: Byte[]]
  Project [f.Value as FlagValue]
    CteRef [filepf as filepf]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [file.Content as file.Content, p.Flags as p.Flags]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as file]
      PhysicalInterpretSource [#BitsRepeatPacket(file.Content) as p]
  PhysicalProject [filep.file.Content as file.Content, filep.p.Flags as p.Flags, f.Value as f.Value]
    PhysicalNestedLoopApply [Cross]
      PhysicalCteRef [filep as filep]
      PhysicalPropertySource [filep.p.Flags as f] [apply: Cross] [type: Byte[]]
  PhysicalProject [f.Value as FlagValue]
    PhysicalCteRef [filepf as filepf]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [file: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [p: object]
      Flags: byte[] <- property Flags
    Generated [Statement0Row0]
      file.Content: byte[] <- field file_Content
      p.Flags: byte[] <- field p_Flags
    TableRow [filep]
      file.Content: byte[] <- field file_Content
      p.Flags: byte[] <- field p_Flags
    SourceEntity [f: byte]
      Value: byte <- direct scalar value
    Generated [ResultRow0]
      FlagValue: byte <- field FlagValue

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [file: BinaryEntity] -> statement0_fileRows
    CreateTable [statement0: Statement0Row0]
    ChunkedForEach [file in statement0_fileRows]
      InterpretSource [BitsRepeatPacket.Interpret(file.Content) -> statement0_pRows]
      ScalarForEach [p in statement0_pRows]
        AppendRow [statement0 <- Statement0Row0(file.Content: file.Content, p.Flags: p.Flags)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [filep in _cteRowResults.Slot0]
      EnumerableSource [filep.p.Flags -> fRows]
      ChunkedForEach [f in fRows]
        AppendShape [result <- ResultShape0(FlagValue: f.Value)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q60_BinaryBitsRepeatUntilInterpret
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("FlagValue", typeof(byte), 0)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("file.Content", typeof(byte[]), 0),
            new Column("p.Flags", typeof(byte[]), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_file_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Content", typeof(byte[]), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.FlagValue);
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
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
                try
                {
                    var __storedTable0Rows = _cteRowResults.Slot0;
                    for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                    {
                        if ((__storedTable0Index & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        Statement0Row0 filep = __storedTable0Rows[__storedTable0Index];
                        var fRows = EvaluationHelper.ConvertEnumerableOutputToChunks<byte>(filep.p_Flags);
                        foreach (var fChunk in fRows)
                        {
                            if (fChunk is global::Musoq.Schema.DataSources.RowChunk<byte> fChunkView)
                            {
                                if (fChunkView.Source is byte[] fChunkViewArray)
                                {
                                    int fChunkViewOffset = fChunkView.Offset;
                                    for (int fIndex = 0, fIndexCount = fChunkView.Count; fIndex < fIndexCount; ++fIndex)
                                    {
                                        if ((fIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var f = fChunkViewArray[fChunkViewOffset + fIndex];
                                        __musoqFinalShapeRows.Add(new ResultShape0(f));
                                    }

                                    continue;
                                }

                                if (fChunkView.Source is List<byte> fChunkViewList)
                                {
                                    int fChunkViewOffset = fChunkView.Offset;
                                    for (int fIndex = 0, fIndexCount = fChunkView.Count; fIndex < fIndexCount; ++fIndex)
                                    {
                                        if ((fIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var f = fChunkViewList[fChunkViewOffset + fIndex];
                                        __musoqFinalShapeRows.Add(new ResultShape0(f));
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
                                __musoqFinalShapeRows.Add(new ResultShape0(f));
                            }
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte1", QueryPhase.End);
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
            var __statement0_fileSchema = provider.GetSchema("#test");
            var statement0_fileRowsSource = __statement0_fileSchema.GetRowSource<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>("files", new SourceExecutionContext("file:1", sourceExecutionPlans["file:1"], token, __schemaColumns_compiled_file_0, sourceRuntimeSettingsBySourceContextId["file:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var statement0_fileRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>(statement0_fileRowsSource.Chunks, __musoqProgressContext, "file:1") : statement0_fileRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            foreach (var fileChunk in statement0_fileRows)
            {
                if (fileChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity> fileChunkView)
                {
                    if (fileChunkView.Source is Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity[] fileChunkViewArray)
                    {
                        int fileChunkViewOffset = fileChunkView.Offset;
                        for (int fileIndex = 0, fileIndexCount = fileChunkView.Count; fileIndex < fileIndexCount; ++fileIndex)
                        {
                            if ((fileIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var file = fileChunkViewArray[fileChunkViewOffset + fileIndex];
                            var _interpreter_BitsRepeatPacket = new Musoq.Generated.Interpreters.BitsRepeatPacket();
                            var statement0_pRows = _interpreter_BitsRepeatPacket.Interpret(file.Content);
                            if (statement0_pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = statement0_pRows;
                                statement0.Add(new Statement0Row0(file.Content, p.Flags));
                            }
                        }

                        continue;
                    }

                    if (fileChunkView.Source is List<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity> fileChunkViewList)
                    {
                        int fileChunkViewOffset = fileChunkView.Offset;
                        for (int fileIndex = 0, fileIndexCount = fileChunkView.Count; fileIndex < fileIndexCount; ++fileIndex)
                        {
                            if ((fileIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var file = fileChunkViewList[fileChunkViewOffset + fileIndex];
                            var _interpreter_BitsRepeatPacket = new Musoq.Generated.Interpreters.BitsRepeatPacket();
                            var statement0_pRows = _interpreter_BitsRepeatPacket.Interpret(file.Content);
                            if (statement0_pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = statement0_pRows;
                                statement0.Add(new Statement0Row0(file.Content, p.Flags));
                            }
                        }

                        continue;
                    }
                }

                for (int fileIndex = 0, fileIndexCount = fileChunk.Count; fileIndex < fileIndexCount; ++fileIndex)
                {
                    if ((fileIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var file = fileChunk[fileIndex];
                    var _interpreter_BitsRepeatPacket = new Musoq.Generated.Interpreters.BitsRepeatPacket();
                    var statement0_pRows = _interpreter_BitsRepeatPacket.Interpret(file.Content);
                    if (statement0_pRows != null)
                    {
                        token.ThrowIfCancellationRequested();
                        var p = statement0_pRows;
                        statement0.Add(new Statement0Row0(file.Content, p.Flags));
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
            public ResultRow0(byte __value0)
            {
                FlagValue = __value0;
            }

            public override int Count => 1;
            public byte FlagValue { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        FlagValue = (byte)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "FlagValue" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)FlagValue,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "FlagValue" => (object)FlagValue,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(byte FlagValue)
            {
                this.FlagValue = FlagValue;
            }

            public byte FlagValue { get; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(byte[] __value0, byte[] __value1)
            {
                file_Content = __value0;
                p_Flags = __value1;
            }

            public byte[] file_Content { get; }
            public byte[] p_Flags { get; }
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
    /// Generated interpreter for binary schema 'BitsRepeatPacket'.
    /// </summary>
    public sealed class BitsRepeatPacket : BytesInterpreterBase<BitsRepeatPacket>
    {
        /// <summary>Gets the Flags field value.</summary>
        public byte[] Flags { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "BitsRepeatPacket";

        /// <inheritdoc/>
        public override BitsRepeatPacket InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("Flags");
            var __flags_list = new System.Collections.Generic.List<byte>();
            byte __flags_lastElem;
            var __flags_iteration = 0;
            do
            {
                EnsureRepeatIteration("Flags", __flags_iteration++);
                __flags_lastElem = (byte)ReadBits(data, 1);
                __flags_list.Add(__flags_lastElem);
            }
            while (!((__flags_lastElem == 0)));
            var _flags = __flags_list.ToArray();
            RecordParsedField("Flags", _flags);
            return new BitsRepeatPacket
            {
                Flags = _flags
            };
        }
    }
}
