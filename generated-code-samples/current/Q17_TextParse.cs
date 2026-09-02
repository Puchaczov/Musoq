// === Parsed Query ===
/*
text LogLine {
                Timestamp: until ' ',
                Level: until ' ',
                Message: rest
            };
            select
                l.Timestamp,
                l.Level,
                l.Message
            from #test.lines() f
            cross apply Parse<LogLine>(f.Line) l
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Line as f.Line, l.Timestamp as l.Timestamp, l.Level as l.Level, l.Message as l.Message]
    Apply [Cross]
      SchemaScan [#test.lines() as f]
      InterpretSource [#LogLine(f.Line) as l]
  Project [l.Timestamp as l.Timestamp, l.Level as l.Level, l.Message as l.Message]
    CteRef [fl as fl]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Line as f.Line, l.Timestamp as l.Timestamp, l.Level as l.Level, l.Message as l.Message]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.lines() as f]
      PhysicalInterpretSource [#LogLine(f.Line) as l]
  PhysicalProject [l.Timestamp as l.Timestamp, l.Level as l.Level, l.Message as l.Message]
    PhysicalCteRef [fl as fl]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: TextEntity]
      Line: string <- property Line
    SourceEntity [l: object]
      Timestamp: string <- property Timestamp
      Level: string <- property Level
      Message: string <- property Message
    Generated [ResultRow0]
      l.Timestamp: string <- field l_Timestamp
      l.Level: string <- field l_Level
      l.Message: string <- field l_Message

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: TextEntity] -> fRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [LogLine.Parse(f.Line) -> lRows]
      ScalarForEach [l in lRows]
        AppendShape [result <- ResultShape0(l.Timestamp: l.Timestamp, l.Level: l.Level, l.Message: l.Message)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q17_TextParse
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
            new Column("l.Timestamp", typeof(string), 0),
            new Column("l.Level", typeof(string), 1),
            new Column("l.Message", typeof(string), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_f_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Line", typeof(string), 1) });
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
                yield return new ResultRow0(__musoqShapeRow.l_Timestamp, __musoqShapeRow.l_Level, __musoqShapeRow.l_Message);
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
                    var fRowsSource = __fSchema.GetRowSource<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity>("lines", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                    var fRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity>(fRowsSource.Chunks, __musoqProgressContext, "f:1") : fRowsSource.Chunks;
                    foreach (var fChunk in fRows)
                    {
                        if (fChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity> fChunkView)
                        {
                            if (fChunkView.Source is Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity[] fChunkViewArray)
                            {
                                int fChunkViewOffset = fChunkView.Offset;
                                for (int fIndex = 0, fIndexCount = fChunkView.Count; fIndex < fIndexCount; ++fIndex)
                                {
                                    if ((fIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var f = fChunkViewArray[fChunkViewOffset + fIndex];
                                    var _interpreter_LogLine = new Musoq.Generated.Interpreters.LogLine();
                                    var lRows = _interpreter_LogLine.Parse(f.Line);
                                    if (lRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var l = lRows;
                                        yield return new ResultShape0(l.Timestamp, l.Level, l.Message);
                                    }
                                }

                                continue;
                            }

                            if (fChunkView.Source is List<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity> fChunkViewList)
                            {
                                int fChunkViewOffset = fChunkView.Offset;
                                for (int fIndex = 0, fIndexCount = fChunkView.Count; fIndex < fIndexCount; ++fIndex)
                                {
                                    if ((fIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var f = fChunkViewList[fChunkViewOffset + fIndex];
                                    var _interpreter_LogLine = new Musoq.Generated.Interpreters.LogLine();
                                    var lRows = _interpreter_LogLine.Parse(f.Line);
                                    if (lRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var l = lRows;
                                        yield return new ResultShape0(l.Timestamp, l.Level, l.Message);
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
                            var _interpreter_LogLine = new Musoq.Generated.Interpreters.LogLine();
                            var lRows = _interpreter_LogLine.Parse(f.Line);
                            if (lRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var l = lRows;
                                yield return new ResultShape0(l.Timestamp, l.Level, l.Message);
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
            public ResultRow0(string __value0, string __value1, string __value2)
            {
                l_Timestamp = __value0;
                l_Level = __value1;
                l_Message = __value2;
            }

            public override int Count => 3;
            public string l_Level { get; private set; }
            public string l_Message { get; private set; }
            public string l_Timestamp { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        l_Timestamp = (string)value;
                        break;
                    case 1:
                        l_Level = (string)value;
                        break;
                    case 2:
                        l_Message = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "l.Timestamp" => true,
                "l_Timestamp" => true,
                "Timestamp" => true,
                "l.Level" => true,
                "l_Level" => true,
                "Level" => true,
                "l.Message" => true,
                "l_Message" => true,
                "Message" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)l_Timestamp,
                1 => (object)l_Level,
                2 => (object)l_Message,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "l.Timestamp" => (object)l_Timestamp,
                "l_Timestamp" => (object)l_Timestamp,
                "Timestamp" => (object)l_Timestamp,
                "l.Level" => (object)l_Level,
                "l_Level" => (object)l_Level,
                "Level" => (object)l_Level,
                "l.Message" => (object)l_Message,
                "l_Message" => (object)l_Message,
                "Message" => (object)l_Message,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string l_Timestamp, string l_Level, string l_Message)
            {
                this.l_Timestamp = l_Timestamp;
                this.l_Level = l_Level;
                this.l_Message = l_Message;
            }

            public string l_Level { get; }
            public string l_Message { get; }
            public string l_Timestamp { get; }
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
    /// Generated interpreter for text schema 'LogLine'.
    /// </summary>
    public sealed class LogLine : TextInterpreterBase<LogLine>
    {
        /// <summary>Gets the Timestamp field value.</summary>
        public string? Timestamp { get; init; }
        /// <summary>Gets the Level field value.</summary>
        public string? Level { get; init; }
        /// <summary>Gets the Message field value.</summary>
        public string? Message { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "LogLine";

        /// <inheritdoc/>
        public override LogLine ParseAt(ReadOnlySpan<char> data, int offset)
        {
            ParsePosition = offset;
            SetCurrentField(null);
            SetCurrentField("Timestamp");
            var _timestamp = ReadUntil(data, " ", fieldName: "Timestamp");
            RecordParsedField("Timestamp", _timestamp);
            SetCurrentField("Level");
            var _level = ReadUntil(data, " ", fieldName: "Level");
            RecordParsedField("Level", _level);
            SetCurrentField("Message");
            var _message = ReadRest(data, fieldName: "Message");
            RecordParsedField("Message", _message);
            return new LogLine
            {
                Timestamp = _timestamp,
                Level = _level,
                Message = _message
            };
        }
    }
}
