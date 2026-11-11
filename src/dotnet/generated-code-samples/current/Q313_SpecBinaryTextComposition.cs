// === Parsed Query ===
/*
text KeyValue {
                        Key: until ':',
                        Value: rest trim
                    };
                    binary Config {
                        Version: byte,
                        Data: string[20] ascii trim as KeyValue,
                        Checksum: byte
                    };
                    select c.Version, c.Data.Key, c.Data.Value, c.Checksum
                    from #test.files() f
                    cross apply Interpret<Config>(f.Content) c
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, c.Version as c.Version, c.Data as c.Data, c.Checksum as c.Checksum]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#Config(f.Content) as c]
  Project [c.Version as Version, c.Data.Key as c.Data.Key, c.Data.Value as c.Data.Value, c.Checksum as Checksum]
    CteRef [fc as fc]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [c.Version as c.Version, c.Data as c.Data, c.Checksum as c.Checksum]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#Config(f.Content) as c]
  PhysicalProject [c.Version as Version, c.Data.Key as c.Data.Key, c.Data.Value as c.Data.Value, c.Checksum as Checksum]
    PhysicalCteRef [fc as fc]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [c: object]
      Version: byte <- property Version
      Data: object <- property Data
      Checksum: byte <- property Checksum
    Generated [ResultRow0]
      Version: byte <- field Version
      c.Data.Key: string <- field c_Data_Key
      c.Data.Value: string <- field c_Data_Value
      Checksum: byte <- field Checksum

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [Config.Interpret(f.Content) -> cRows]
      ScalarForEach [c in cRows]
        AppendShape [result <- ResultRow0(Version: c.Version, c.Data.Key: c.Data.Key, c.Data.Value: c.Data.Value, Checksum: c.Checksum)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q313_SpecBinaryTextComposition
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Version", typeof(byte), 0),
            new Column("c.Data.Key", typeof(string), 1),
            new Column("c.Data.Value", typeof(string), 2),
            new Column("Checksum", typeof(byte), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_f_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Content", typeof(byte[]), 0) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }
        public TypedParameterSnapshot TypedParameters { get; set; } = TypedParameterSnapshot.Empty;

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
                yield return __musoqShapeRow;
            }
        }

        private IEnumerable<ResultRow0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    var __fSchema = provider.GetSchema("#test");
                    var fRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>.Open(__fSchema, "files", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["f:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
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
                                    var _interpreter_Config = new Musoq.Generated.Interpreters.Config();
                                    var cRows = _interpreter_Config.Interpret(f.Content);
                                    if (cRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var c = cRows;
                                        yield return new ResultRow0(c.Version, ((Musoq.Generated.Interpreters.KeyValue)c.Data).Key, ((Musoq.Generated.Interpreters.KeyValue)c.Data).Value, c.Checksum);
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
                                    var _interpreter_Config = new Musoq.Generated.Interpreters.Config();
                                    var cRows = _interpreter_Config.Interpret(f.Content);
                                    if (cRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var c = cRows;
                                        yield return new ResultRow0(c.Version, ((Musoq.Generated.Interpreters.KeyValue)c.Data).Key, ((Musoq.Generated.Interpreters.KeyValue)c.Data).Value, c.Checksum);
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
                            var _interpreter_Config = new Musoq.Generated.Interpreters.Config();
                            var cRows = _interpreter_Config.Interpret(f.Content);
                            if (cRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var c = cRows;
                                yield return new ResultRow0(c.Version, ((Musoq.Generated.Interpreters.KeyValue)c.Data).Key, ((Musoq.Generated.Interpreters.KeyValue)c.Data).Value, c.Checksum);
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
            public ResultRow0(byte __value0, string __value1, string __value2, byte __value3)
            {
                Version = __value0;
                c_Data_Key = __value1;
                c_Data_Value = __value2;
                Checksum = __value3;
            }

            public byte Checksum { get; private set; }
            public override int Count => 4;
            public byte Version { get; private set; }
            public string c_Data_Key { get; private set; }
            public string c_Data_Value { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Version = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.c_Data_Key = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.c_Data_Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Checksum = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Version),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.c_Data_Key),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.c_Data_Value),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Checksum),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Version" => true,
                "c.Data.Key" => true,
                "c_Data_Key" => true,
                "Key" => true,
                "c.Data.Value" => true,
                "c_Data_Value" => true,
                "Value" => true,
                "Checksum" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Version, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.c_Data_Key, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.c_Data_Value, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Checksum, ref visitor);
            }
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
    /// Generated interpreter for text schema 'KeyValue'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class KeyValue : TextInterpreterBase<KeyValue>, ITextInterpreterStatus<KeyValue>, IParserCompletionLayoutSource, global::Musoq.Plugins.IJsonPayload
    {
        private static readonly global::Musoq.Schema.Interpreters.ParserCompletionLayout __musoqCompletionLayout = new(new string[] { "Key", "Value" });
        global::Musoq.Schema.Interpreters.ParserCompletionLayout global::Musoq.Schema.Interpreters.IParserCompletionLayoutSource.CompletionLayout => __musoqCompletionLayout;

        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(KeyValue value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Key"))
            {
                writer.WritePropertyName("Key");
                var fieldValue = this.Key;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Value"))
            {
                writer.WritePropertyName("Value");
                var fieldValue = this.Value;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Key field value.</summary>
        public string? Key { get; set; }
        /// <summary>Gets the Value field value.</summary>
        public string? Value { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "KeyValue";

        public override KeyValue ParseAt(ReadOnlySpan<char> data, int offset)
        {
            var input = new global::Musoq.Schema.Interpreters.TextParseInput(data);
            return ((global::Musoq.Schema.Interpreters.ITextInterpreterStatus<KeyValue>)this).ReadAt(ref input, offset).GetValueOrThrow();
        }

        global::Musoq.Schema.Interpreters.ParseAttempt<KeyValue> global::Musoq.Schema.Interpreters.ITextInterpreterStatus<KeyValue>.ReadAt(ref global::Musoq.Schema.Interpreters.TextParseInput __musoqTextInput, int offset)
        {
            var data = __musoqTextInput.Text;
            KeyValue? __parsedValue = null;
            try
            {
                ParsePosition = offset;
                SetCurrentField(null);
                __parsedValue = new KeyValue();
                RecordParsedValue(__parsedValue);
                SetCurrentField("Key");
                var __musoqReadResult0 = base.ReadUntilStatus(data, ":", fieldName: "Key");
                if (!__musoqReadResult0.Succeeded)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<KeyValue>.Failed(__parsedValue, __musoqReadResult0.Failure!);
                }

                var _key = __musoqReadResult0.Value!;
                __parsedValue.Key = _key;
                base.RecordParsedFieldSlot(0);
                SetCurrentField("Value");
                var __musoqReadResult1 = base.ReadRestStatus(data, trim: true, fieldName: "Value");
                if (!__musoqReadResult1.Succeeded)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<KeyValue>.Failed(__parsedValue, __musoqReadResult1.Failure!);
                }

                var _value = __musoqReadResult1.Value!;
                __parsedValue.Value = _value;
                base.RecordParsedFieldSlot(1);
                return global::Musoq.Schema.Interpreters.ParseAttempt<KeyValue>.Success(__parsedValue);
            }
            catch (global::Musoq.Schema.Interpreters.ParseException __parseFailure)
            {
                return global::Musoq.Schema.Interpreters.ParseAttempt<KeyValue>.Failed(__parsedValue, global::Musoq.Schema.Interpreters.ParseFailure.FromException(__parseFailure));
            }
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'Config'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Config : BytesInterpreterBase<Config>, IBytesInterpreterStatus<Config>, IParserCompletionLayoutSource, global::Musoq.Plugins.IJsonPayload
    {
        private static readonly global::Musoq.Schema.Interpreters.ParserCompletionLayout __musoqCompletionLayout = new(new string[] { "Version", "Data", "Checksum" });
        global::Musoq.Schema.Interpreters.ParserCompletionLayout global::Musoq.Schema.Interpreters.IParserCompletionLayoutSource.CompletionLayout => __musoqCompletionLayout;

        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Config value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Version"))
            {
                writer.WritePropertyName("Version");
                var fieldValue = this.Version;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Data"))
            {
                writer.WritePropertyName("Data");
                var fieldValue = this.Data;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            if (IsFieldComplete("Checksum"))
            {
                writer.WritePropertyName("Checksum");
                var fieldValue = this.Checksum;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Version field value.</summary>
        public byte Version { get; set; }
        /// <summary>Gets the Data field value.</summary>
        public KeyValue Data { get; set; }
        /// <summary>Gets the Checksum field value.</summary>
        public byte Checksum { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Config";

        public override Config InterpretAt(ReadOnlySpan<byte> data, int offset) => ((global::Musoq.Schema.Interpreters.IBytesInterpreterStatus<Config>)this).ReadAt(data, offset).GetValueOrThrow();
        global::Musoq.Schema.Interpreters.ParseAttempt<Config> global::Musoq.Schema.Interpreters.IBytesInterpreterStatus<Config>.ReadAt(ReadOnlySpan<byte> data, int offset)
        {
            Config? __parsedValue = null;
            try
            {
                if (base.InitializeParsePositionStatus(data, offset)is { } __musoqReadFailure0)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Config>.Failed(__parsedValue, __musoqReadFailure0);
                }

                SetCurrentField(null);
                __parsedValue = new Config();
                RecordParsedValue(__parsedValue);
                SetCurrentField("Version");
                if (base.PrepareByteInputStatus(data, 1)is { } __musoqReadFailure1)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Config>.Failed(__parsedValue, __musoqReadFailure1);
                }

                var _version = base.TakePreparedBytes(data, 1)[0];
                __parsedValue.Version = _version;
                base.RecordParsedFieldSlot(0);
                SetCurrentField("Data");
                var __musoqReadSize2 = 20;
                var __musoqReadEncoding3 = System.Text.Encoding.ASCII;
                if (base.PrepareStringStatus(data, __musoqReadSize2)is { } __musoqReadFailure4)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Config>.Failed(__parsedValue, __musoqReadFailure4);
                }

                var _data_raw = ReadString(data, __musoqReadSize2, __musoqReadEncoding3).Trim();
                var _data_textInterpreter = new KeyValue();
                var __musoqReadResult5 = base.ParseNestedStatus(_data_textInterpreter, _data_raw, "Data");
                if (!__musoqReadResult5.Succeeded)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Config>.Failed(__parsedValue, __musoqReadResult5.Failure!);
                }

                var _data = __musoqReadResult5.Value!;
                __parsedValue.Data = _data;
                base.RecordParsedFieldSlot(1);
                SetCurrentField("Checksum");
                if (base.PrepareByteInputStatus(data, 1)is { } __musoqReadFailure6)
                {
                    return global::Musoq.Schema.Interpreters.ParseAttempt<Config>.Failed(__parsedValue, __musoqReadFailure6);
                }

                var _checksum = base.TakePreparedBytes(data, 1)[0];
                __parsedValue.Checksum = _checksum;
                base.RecordParsedFieldSlot(2);
                return global::Musoq.Schema.Interpreters.ParseAttempt<Config>.Success(__parsedValue);
            }
            catch (global::Musoq.Schema.Interpreters.ParseException __parseFailure)
            {
                return global::Musoq.Schema.Interpreters.ParseAttempt<Config>.Failed(__parsedValue, global::Musoq.Schema.Interpreters.ParseFailure.FromException(__parseFailure));
            }
        }
    }
}
