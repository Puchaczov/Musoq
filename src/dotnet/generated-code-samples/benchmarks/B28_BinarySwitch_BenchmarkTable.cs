// === Parsed Query ===
/*
binary Body { Value: int le }; binary Tagged { Kind: byte, Body: switch Kind { 0 => Known: Body, _ => Raw: byte[4] } }; select r.Id as Id, p.Body.Case as Kind
from bench.rows() r
cross apply Interpret<Tagged>(r.Payload) p
order by r.Id
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.Id as r.Id, r.Payload as r.Payload, p.Body as p.Body]
    Apply [Cross]
      SchemaScan [#bench.rows() as r]
      InterpretSource [#Tagged(r.Payload) as p]
  Sort [r.Id]
    Project [r.Id as Id, p.Body.Case as Kind]
      CteRef [rp as rp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.Id as r.Id, p.Body as p.Body]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#bench.rows() as r]
      PhysicalInterpretSource [#Tagged(r.Payload) as p]
  PhysicalSort [r.Id]
    PhysicalProject [r.Id as Id, p.Body.Case as Kind]
      PhysicalCteRef [rp as rp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Id: int <- property Id
      Payload: byte[] <- property Payload
    SourceEntity [p: object]
      Kind: byte <- property Kind
      Body: ExpandoObject <- property Body
    Generated [Statement0Row0]
      r.Id: int <- field r_Id
      p.Body: ExpandoObject <- field p_Body
    TableRow [rp]
      r.Id: int <- field r_Id
      p.Body: ExpandoObject <- field p_Body
    Generated [ResultRow0]
      Id: int <- field Id
      Kind: string <- field Kind

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: FixtureRow] -> statement0_rRows
    CreateTable [statement0: Statement0Row0]
    PhaseBoundary [Select]
    ChunkedForEach [r in statement0_rRows]
      Let [rId: int = r.Id]
      InterpretSource [Tagged.Interpret(r.Payload) -> statement0_pRows]
      ScalarForEach [p in statement0_pRows]
        AppendRow [statement0 <- Statement0Row0(r.Id: rId, p.Body: p.Body)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [rp in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Id: rp.r.Id, Kind: rp.p.Body.Case)]
    SortOwnedBuffer [result by Id ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultShape0 from owned buffer result]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B28_BinarySwitch_BenchmarkTable
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IContextTableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Payload", typeof(byte[]), 1) });
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("r.Id", typeof(int), 0),
            new Column("p.Body", typeof(Musoq.Generated.Interpreters.Switch_Body), 1)
        };
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Kind", typeof(string), 1)
        };
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public ILogger Logger { get; set; }
        public TypedParameterSnapshot TypedParameters { get; set; } = TypedParameterSnapshot.Empty;
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();

        public event QueryPhaseEventHandler PhaseChanged;
        public event DataSourceEventHandler DataSourceProgress;
        public event QueryProgressEventHandler QueryProgress;
        public Table Run(CancellationToken token)
        {
            var queryContext = new QueryRunContext(token, TypedParameters, PhaseChanged, DataSourceProgress, this, queryId: "compiled", provider: Provider, sourceRuntimeSettingsBySourceContextId: SourceRuntimeSettingsBySourceContextId, sourceRuntimeSettingDescriptionsBySourceContextId: SourceRuntimeSettingDescriptionsBySourceContextId, sourceExecutionPlans: SourceExecutionPlans, logger: Logger, queryProgress: QueryProgress);
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            var token = queryContext.CancellationToken;
            var __musoqProgressContext = queryContext.QueryProgress == null ? null : queryContext;
            Action<string, QueryPhase> OnPhaseChanged = queryContext.NotifyPhaseChanged;
            DataSourceEventHandler OnDataSourceProgress = queryContext.NotifyDataSourceProgress;
            QueryProgressEventHandler OnQueryProgress = queryContext.QueryProgress;
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(queryContext.TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var result = new List<ResultShape0>();
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 rp = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(rp.r_Id, rp.p_Body.Case) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
                    if (comparison != 0)
                        return comparison;
                    return left.__musoqSequence.CompareTo(right.__musoqSequence);
                }));
                return result;
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

        private IEnumerable<ResultShape0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, queryContext))
            {
                yield return __musoqShapeRow;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnPhaseChanged(string queryId, QueryPhase phase)
        {
            PhaseChanged?.Invoke(this, new QueryPhaseEventArgs(queryId, phase));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnDataSourceProgress(object sender, DataSourceEventArgs e)
        {
            DataSourceProgress?.Invoke(this, e);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            var __statement0_rSchema = provider.GetSchema("#bench");
            var statement0_rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__statement0_rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
            var statement0_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(statement0_rRowsSource.Chunks, __musoqProgressContext, "r:1") : statement0_rRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            foreach (var rChunk in statement0_rRows)
            {
                if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.FixtureRow> rChunkView)
                {
                    if (rChunkView.Source is Musoq.Benchmarks.Scenarios.FixtureRow[] rChunkViewArray)
                    {
                        int rChunkViewOffset = rChunkView.Offset;
                        for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                        {
                            if ((rIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var r = rChunkViewArray[rChunkViewOffset + rIndex];
                            int rId = r.Id;
                            var _interpreter_Tagged = new Musoq.Generated.Interpreters.Tagged();
                            var statement0_pRows = _interpreter_Tagged.Interpret(r.Payload);
                            if (statement0_pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = statement0_pRows;
                                statement0.Add(new Statement0Row0(rId, p.Body));
                            }
                        }

                        continue;
                    }

                    if (rChunkView.Source is List<Musoq.Benchmarks.Scenarios.FixtureRow> rChunkViewList)
                    {
                        int rChunkViewOffset = rChunkView.Offset;
                        for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                        {
                            if ((rIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var r = rChunkViewList[rChunkViewOffset + rIndex];
                            int rId = r.Id;
                            var _interpreter_Tagged = new Musoq.Generated.Interpreters.Tagged();
                            var statement0_pRows = _interpreter_Tagged.Interpret(r.Payload);
                            if (statement0_pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = statement0_pRows;
                                statement0.Add(new Statement0Row0(rId, p.Body));
                            }
                        }

                        continue;
                    }
                }

                for (int rIndex = 0, rIndexCount = rChunk.Count; rIndex < rIndexCount; ++rIndex)
                {
                    if ((rIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var r = rChunk[rIndex];
                    int rId = r.Id;
                    var _interpreter_Tagged = new Musoq.Generated.Interpreters.Tagged();
                    var statement0_pRows = _interpreter_Tagged.Interpret(r.Payload);
                    if (statement0_pRows != null)
                    {
                        token.ThrowIfCancellationRequested();
                        var p = statement0_pRows;
                        statement0.Add(new Statement0Row0(rId, p.Body));
                    }
                }
            }

            return statement0;
        }

        private sealed class Statement0Row0
        {
            public int r_Id { get; }
            public Musoq.Generated.Interpreters.Switch_Body p_Body { get; }

            public Statement0Row0(int __value0, Musoq.Generated.Interpreters.Switch_Body __value1)
            {
                r_Id = __value0;
                p_Body = __value1;
            }
        }

        private sealed class ResultShape0 : Row
        {
            public ResultShape0(int Id, string Kind)
            {
                this.Id = Id;
                this.Kind = Kind;
            }

            public int Id { get; private set; }
            public string Kind { get; private set; }
            public override int Count => 2;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Kind, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Kind),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Kind" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Kind = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public int __musoqSequence { get; init; }
        }

        private sealed class CteRowResults
        {
            public List<Statement0Row0> Slot0;
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
    /// Generated interpreter for binary schema 'Body'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Body : BytesInterpreterBase<Body>, IBytesInterpreterStatus<Body>, IParserCompletionLayoutSource, global::Musoq.Plugins.IJsonPayload
    {
        public int Value { get; set; }

        private static readonly global::Musoq.Schema.Interpreters.ParserCompletionLayout __musoqCompletionLayout = new(new string[] { "Value" });
        global::Musoq.Schema.Interpreters.ParserCompletionLayout global::Musoq.Schema.Interpreters.IParserCompletionLayoutSource.CompletionLayout => __musoqCompletionLayout;

        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Body value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Value"))
            {
                writer.WritePropertyName("Value");
                var fieldValue = this.Value;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        public override string SchemaName => "Body";

        public override Body InterpretAt(ReadOnlySpan<byte> data, int offset) => ((global::Musoq.Schema.Interpreters.IBytesInterpreterStatus<Body>)this).ReadAt(data, offset).GetValueOrThrow();
        global::Musoq.Schema.Interpreters.ParseAttempt<Body> global::Musoq.Schema.Interpreters.IBytesInterpreterStatus<Body>.ReadAt(ReadOnlySpan<byte> data, int offset)
        {
            Body? __parsedValue = null;
            try
            {
                if (base.InitializeParsePositionStatus(data, offset)is { } __musoqReadFailure0)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Body>.Failed(__parsedValue, __musoqReadFailure0);
                }

                SetCurrentField(null);
                __parsedValue = new Body();
                RecordParsedValue(__parsedValue);
                SetCurrentField("Value");
                if (base.PrepareByteInputStatus(data, 4)is { } __musoqReadFailure1)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Body>.Failed(__parsedValue, __musoqReadFailure1);
                }

                var _field5_Value = global::System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(base.TakePreparedBytes(data, 4));
                __parsedValue.Value = _field5_Value;
                base.RecordParsedFieldSlot(0);
                return global::Musoq.Schema.Interpreters.ParseAttempt<Body>.Success(__parsedValue);
            }
            catch (global::Musoq.Schema.Interpreters.ParseException __parseFailure)
            {
                return global::Musoq.Schema.Interpreters.ParseAttempt<Body>.Failed(__parsedValue, global::Musoq.Schema.Interpreters.ParseFailure.FromException(__parseFailure));
            }
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'Tagged'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Tagged : BytesInterpreterBase<Tagged>, IBytesInterpreterStatus<Tagged>, IParserCompletionLayoutSource, global::Musoq.Plugins.IJsonPayload
    {
        public byte Kind { get; set; }
        public Switch_Body Body { get; set; }

        private static readonly global::Musoq.Schema.Interpreters.ParserCompletionLayout __musoqCompletionLayout = new(new string[] { "Kind", "Body" });
        global::Musoq.Schema.Interpreters.ParserCompletionLayout global::Musoq.Schema.Interpreters.IParserCompletionLayoutSource.CompletionLayout => __musoqCompletionLayout;

        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Tagged value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Kind"))
            {
                writer.WritePropertyName("Kind");
                var fieldValue = this.Kind;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Body"))
            {
                writer.WritePropertyName("Body");
                var fieldValue = this.Body;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        public override string SchemaName => "Tagged";

        public override Tagged InterpretAt(ReadOnlySpan<byte> data, int offset) => ((global::Musoq.Schema.Interpreters.IBytesInterpreterStatus<Tagged>)this).ReadAt(data, offset).GetValueOrThrow();
        global::Musoq.Schema.Interpreters.ParseAttempt<Tagged> global::Musoq.Schema.Interpreters.IBytesInterpreterStatus<Tagged>.ReadAt(ReadOnlySpan<byte> data, int offset)
        {
            Tagged? __parsedValue = null;
            try
            {
                if (base.InitializeParsePositionStatus(data, offset)is { } __musoqReadFailure0)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Tagged>.Failed(__parsedValue, __musoqReadFailure0);
                }

                SetCurrentField(null);
                __parsedValue = new Tagged();
                RecordParsedValue(__parsedValue);
                SetCurrentField("Kind");
                if (base.PrepareByteInputStatus(data, 1)is { } __musoqReadFailure1)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Tagged>.Failed(__parsedValue, __musoqReadFailure1);
                }

                var _field4_Kind = base.TakePreparedBytes(data, 1)[0];
                __parsedValue.Kind = _field4_Kind;
                base.RecordParsedFieldSlot(0);
                SetCurrentField("Body");
                string _field4_Body_case = null;
                Body _field4_Body_Known = default;
                byte[] _field4_Body_Raw = default;
                if (_field4_Kind == 0)
                {
                    var __field4_Body_Known_value_interpreter = new Body();
                    var __musoqReadResult2 = base.InterpretNestedStatus(__field4_Body_Known_value_interpreter, data, "Body");
                    if (!__musoqReadResult2.Succeeded)
                    {
                        return global::Musoq.Schema.Interpreters.ParseAttempt<Tagged>.Failed(__parsedValue, __musoqReadResult2.Failure!);
                    }

                    var _field4_Body_Known_value = __musoqReadResult2.Value!;
                    _field4_Body_Known = _field4_Body_Known_value;
                    _field4_Body_case = "Known";
                }
                else
                {
                    var __musoqReadSize3 = 4;
                    if (base.PrepareByteArrayStatus(data, __musoqReadSize3)is { } __musoqReadFailure4)
                    {
                        return global::Musoq.Schema.Interpreters.ParseAttempt<Tagged>.Failed(__parsedValue, __musoqReadFailure4);
                    }

                    var _field4_Body_Raw_value = base.ReadPreparedByteArray(data, __musoqReadSize3);
                    _field4_Body_Raw = _field4_Body_Raw_value;
                    _field4_Body_case = "Raw";
                }

                var _field4_Body = new Switch_Body
                {
                    Case = _field4_Body_case,
                    Known = _field4_Body_Known,
                    Raw = _field4_Body_Raw
                };
                __parsedValue.Body = _field4_Body;
                base.RecordParsedFieldSlot(1);
                return global::Musoq.Schema.Interpreters.ParseAttempt<Tagged>.Success(__parsedValue);
            }
            catch (global::Musoq.Schema.Interpreters.ParseException __parseFailure)
            {
                return global::Musoq.Schema.Interpreters.ParseAttempt<Tagged>.Failed(__parsedValue, global::Musoq.Schema.Interpreters.ParseFailure.FromException(__parseFailure));
            }
        }
    }

    /// <summary>
    /// Generated tagged-union value for binary switch 'Switch_Body'.
    /// </summary>
    public sealed class Switch_Body
    {
        /// <summary>Gets the selected branch alias.</summary>
        public string Case { get; init; }
        /// <summary>Gets the 'Known' branch value; non-null only when selected.</summary>
        public Body Known { get; init; }
        /// <summary>Gets the 'Raw' branch value; non-null only when selected.</summary>
        public byte[] Raw { get; init; }
    }
}
