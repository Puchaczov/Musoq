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
  Project [c.Version as c.Version, c.Data.Key as c.Data.Key, c.Data.Value as c.Data.Value, c.Checksum as c.Checksum]
    CteRef [fc as fc]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, c.Version as c.Version, c.Data as c.Data, c.Checksum as c.Checksum]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#Config(f.Content) as c]
  PhysicalProject [c.Version as c.Version, c.Data.Key as c.Data.Key, c.Data.Value as c.Data.Value, c.Checksum as c.Checksum]
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
      c.Version: byte <- field c_Version
      c.Data.Key: string <- field c_Data_Key
      c.Data.Value: string <- field c_Data_Value
      c.Checksum: byte <- field c_Checksum

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [Config.Interpret(f.Content) -> cRows]
      ScalarForEach [c in cRows]
        AppendShape [result <- ResultShape0(c.Version: c.Version, c.Data.Key: c.Data.Key, c.Data.Value: c.Data.Value, c.Checksum: c.Checksum)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("c.Version", typeof(byte), 0),
            new Column("c.Data.Key", typeof(string), 1),
            new Column("c.Data.Value", typeof(string), 2),
            new Column("c.Checksum", typeof(byte), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_f_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Content", typeof(byte[]), 0) });
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
                yield return new ResultRow0(__musoqShapeRow.c_Version, __musoqShapeRow.c_Data_Key, __musoqShapeRow.c_Data_Value, __musoqShapeRow.c_Checksum);
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
                                    var _interpreter_Config = new Musoq.Generated.Interpreters.Config();
                                    var cRows = _interpreter_Config.Interpret(f.Content);
                                    if (cRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var c = cRows;
                                        yield return new ResultShape0(c.Version, ((Musoq.Generated.Interpreters.KeyValue)c.Data).Key, ((Musoq.Generated.Interpreters.KeyValue)c.Data).Value, c.Checksum);
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
                                        yield return new ResultShape0(c.Version, ((Musoq.Generated.Interpreters.KeyValue)c.Data).Key, ((Musoq.Generated.Interpreters.KeyValue)c.Data).Value, c.Checksum);
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
                                yield return new ResultShape0(c.Version, ((Musoq.Generated.Interpreters.KeyValue)c.Data).Key, ((Musoq.Generated.Interpreters.KeyValue)c.Data).Value, c.Checksum);
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
                c_Version = __value0;
                c_Data_Key = __value1;
                c_Data_Value = __value2;
                c_Checksum = __value3;
            }

            public override int Count => 4;
            public byte c_Checksum { get; private set; }
            public string c_Data_Key { get; private set; }
            public string c_Data_Value { get; private set; }
            public byte c_Version { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        c_Version = (byte)value;
                        break;
                    case 1:
                        c_Data_Key = (string)value;
                        break;
                    case 2:
                        c_Data_Value = (string)value;
                        break;
                    case 3:
                        c_Checksum = (byte)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "c.Version" => true,
                "c_Version" => true,
                "Version" => true,
                "c.Data.Key" => true,
                "c_Data_Key" => true,
                "Key" => true,
                "c.Data.Value" => true,
                "c_Data_Value" => true,
                "Value" => true,
                "c.Checksum" => true,
                "c_Checksum" => true,
                "Checksum" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)c_Version,
                1 => (object)c_Data_Key,
                2 => (object)c_Data_Value,
                3 => (object)c_Checksum,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "c.Version" => (object)c_Version,
                "c_Version" => (object)c_Version,
                "Version" => (object)c_Version,
                "c.Data.Key" => (object)c_Data_Key,
                "c_Data_Key" => (object)c_Data_Key,
                "Key" => (object)c_Data_Key,
                "c.Data.Value" => (object)c_Data_Value,
                "c_Data_Value" => (object)c_Data_Value,
                "Value" => (object)c_Data_Value,
                "c.Checksum" => (object)c_Checksum,
                "c_Checksum" => (object)c_Checksum,
                "Checksum" => (object)c_Checksum,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(byte c_Version, string c_Data_Key, string c_Data_Value, byte c_Checksum)
            {
                this.c_Version = c_Version;
                this.c_Data_Key = c_Data_Key;
                this.c_Data_Value = c_Data_Value;
                this.c_Checksum = c_Checksum;
            }

            public byte c_Checksum { get; }
            public string c_Data_Key { get; }
            public string c_Data_Value { get; }
            public byte c_Version { get; }
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
    public sealed class KeyValue : TextInterpreterBase<KeyValue>
    {
        /// <summary>Gets the Key field value.</summary>
        public string? Key { get; init; }
        /// <summary>Gets the Value field value.</summary>
        public string? Value { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "KeyValue";

        /// <inheritdoc/>
        public override KeyValue ParseAt(ReadOnlySpan<char> data, int offset)
        {
            ParsePosition = offset;
            SetCurrentField(null);
            SetCurrentField("Key");
            var _key = ReadUntil(data, ":", fieldName: "Key");
            RecordParsedField("Key", _key);
            SetCurrentField("Value");
            var _value = ReadRest(data, trim: true, fieldName: "Value");
            RecordParsedField("Value", _value);
            return new KeyValue
            {
                Key = _key,
                Value = _value
            };
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'Config'.
    /// </summary>
    public sealed class Config : BytesInterpreterBase<Config>
    {
        /// <summary>Gets the Version field value.</summary>
        public byte Version { get; init; }
        /// <summary>Gets the Data field value.</summary>
        public KeyValue Data { get; init; }
        /// <summary>Gets the Checksum field value.</summary>
        public byte Checksum { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Config";

        /// <inheritdoc/>
        public override Config InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("Version");
            var _version = ReadByte(data);
            RecordParsedField("Version", _version);
            SetCurrentField("Data");
            var _data_raw = ReadString(data, 20, System.Text.Encoding.ASCII).Trim();
            var _data_textInterpreter = new KeyValue();
            var _data = ParseNested(_data_textInterpreter, _data_raw, "Data");
            RecordParsedField("Data", _data);
            SetCurrentField("Checksum");
            var _checksum = ReadByte(data);
            RecordParsedField("Checksum", _checksum);
            return new Config
            {
                Version = _version,
                Data = _data,
                Checksum = _checksum
            };
        }
    }
}
