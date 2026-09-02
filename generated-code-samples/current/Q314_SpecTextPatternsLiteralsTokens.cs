// === Parsed Query ===
/*
text Patterned {
                        Ip: pattern '\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}',
                        Separator: literal ':',
                        User: token,
                        _: whitespace+,
                        Message: rest
                    };
                    select p.Ip, p.User, p.Message
                    from #test.lines() f
                    cross apply Parse<Patterned>(f.Line) p
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Line as f.Line, p.Ip as p.Ip, p.User as p.User, p.Message as p.Message]
    Apply [Cross]
      SchemaScan [#test.lines() as f]
      InterpretSource [#Patterned(f.Line) as p]
  Project [p.Ip as p.Ip, p.User as p.User, p.Message as p.Message]
    CteRef [fp as fp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Line as f.Line, p.Ip as p.Ip, p.User as p.User, p.Message as p.Message]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.lines() as f]
      PhysicalInterpretSource [#Patterned(f.Line) as p]
  PhysicalProject [p.Ip as p.Ip, p.User as p.User, p.Message as p.Message]
    PhysicalCteRef [fp as fp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: TextEntity]
      Line: string <- property Line
    SourceEntity [p: object]
      Ip: string <- property Ip
      Separator: string <- property Separator
      User: string <- property User
      Message: string <- property Message
    Generated [ResultRow0]
      p.Ip: string <- field p_Ip
      p.User: string <- field p_User
      p.Message: string <- field p_Message

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: TextEntity] -> fRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [Patterned.Parse(f.Line) -> pRows]
      ScalarForEach [p in pRows]
        AppendShape [result <- ResultShape0(p.Ip: p.Ip, p.User: p.User, p.Message: p.Message)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q314_SpecTextPatternsLiteralsTokens
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
            new Column("p.Ip", typeof(string), 0),
            new Column("p.User", typeof(string), 1),
            new Column("p.Message", typeof(string), 2)
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
                yield return new ResultRow0(__musoqShapeRow.p_Ip, __musoqShapeRow.p_User, __musoqShapeRow.p_Message);
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
                                    var _interpreter_Patterned = new Musoq.Generated.Interpreters.Patterned();
                                    var pRows = _interpreter_Patterned.Parse(f.Line);
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        yield return new ResultShape0(p.Ip, p.User, p.Message);
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
                                    var _interpreter_Patterned = new Musoq.Generated.Interpreters.Patterned();
                                    var pRows = _interpreter_Patterned.Parse(f.Line);
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        yield return new ResultShape0(p.Ip, p.User, p.Message);
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
                            var _interpreter_Patterned = new Musoq.Generated.Interpreters.Patterned();
                            var pRows = _interpreter_Patterned.Parse(f.Line);
                            if (pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = pRows;
                                yield return new ResultShape0(p.Ip, p.User, p.Message);
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
                p_Ip = __value0;
                p_User = __value1;
                p_Message = __value2;
            }

            public override int Count => 3;
            public string p_Ip { get; private set; }
            public string p_Message { get; private set; }
            public string p_User { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        p_Ip = (string)value;
                        break;
                    case 1:
                        p_User = (string)value;
                        break;
                    case 2:
                        p_Message = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "p.Ip" => true,
                "p_Ip" => true,
                "Ip" => true,
                "p.User" => true,
                "p_User" => true,
                "User" => true,
                "p.Message" => true,
                "p_Message" => true,
                "Message" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)p_Ip,
                1 => (object)p_User,
                2 => (object)p_Message,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "p.Ip" => (object)p_Ip,
                "p_Ip" => (object)p_Ip,
                "Ip" => (object)p_Ip,
                "p.User" => (object)p_User,
                "p_User" => (object)p_User,
                "User" => (object)p_User,
                "p.Message" => (object)p_Message,
                "p_Message" => (object)p_Message,
                "Message" => (object)p_Message,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string p_Ip, string p_User, string p_Message)
            {
                this.p_Ip = p_Ip;
                this.p_User = p_User;
                this.p_Message = p_Message;
            }

            public string p_Ip { get; }
            public string p_Message { get; }
            public string p_User { get; }
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
    /// Generated interpreter for text schema 'Patterned'.
    /// </summary>
    public sealed class Patterned : TextInterpreterBase<Patterned>
    {
        /// <summary>Gets the Ip field value.</summary>
        public string? Ip { get; init; }
        /// <summary>Gets the Separator field value.</summary>
        public string? Separator { get; init; }
        /// <summary>Gets the User field value.</summary>
        public string? User { get; init; }
        /// <summary>Gets the Message field value.</summary>
        public string? Message { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Patterned";

        /// <inheritdoc/>
        public override Patterned ParseAt(ReadOnlySpan<char> data, int offset)
        {
            ParsePosition = offset;
            SetCurrentField(null);
            SetCurrentField("Ip");
            var _ip = ReadPattern(data, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}", fieldName: "Ip");
            RecordParsedField("Ip", _ip);
            SetCurrentField("Separator");
            var _separator = ExpectLiteral(data, ":", fieldName: "Separator");
            RecordParsedField("Separator", _separator);
            SetCurrentField("User");
            var _user = ReadToken(data, fieldName: "User");
            RecordParsedField("User", _user);
            SetCurrentField("_");
            SkipWhitespace(data, true, fieldName: "_");
            SetCurrentField("Message");
            var _message = ReadRest(data, fieldName: "Message");
            RecordParsedField("Message", _message);
            return new Patterned
            {
                Ip = _ip,
                Separator = _separator,
                User = _user,
                Message = _message
            };
        }
    }
}
