// === Parsed Query ===
/*
binary LoginPayload { UserId: int le };
                    binary DataPayload { Size: short le };
                    binary Packet {
                        Type: byte,
                        Length: byte,
                        Payload: switch Type {
                            1 => Login: LoginPayload,
                            2 => Data: DataPayload,
                            _ => Raw: byte[Length]
                        }
                    };
                    select p.Type, p.Payload.Case, p.Payload.Login.UserId, p.Payload.Data.Size, p.Payload.Raw
                    from #test.files() f
                    cross apply Interpret<Packet>(f.Content) p
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, p.Type as p.Type, p.Payload as p.Payload]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#Packet(f.Content) as p]
  Project [p.Type as p.Type, p.Payload.Case as p.Payload.Case, p.Payload.Login.UserId as p.Payload.Login.UserId, p.Payload.Data.Size as p.Payload.Data.Size, p.Payload.Raw as p.Payload.Raw]
    CteRef [fp as fp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, p.Type as p.Type, p.Payload as p.Payload]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#Packet(f.Content) as p]
  PhysicalProject [p.Type as p.Type, p.Payload.Case as p.Payload.Case, p.Payload.Login.UserId as p.Payload.Login.UserId, p.Payload.Data.Size as p.Payload.Data.Size, p.Payload.Raw as p.Payload.Raw]
    PhysicalCteRef [fp as fp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [p: object]
      Type: byte <- property Type
      Length: byte <- property Length
      Payload: ExpandoObject <- property Payload
    Generated [ResultRow0]
      p.Type: byte <- field p_Type
      p.Payload.Case: string <- field p_Payload_Case
      p.Payload.Login.UserId: int <- field p_Payload_Login_UserId
      p.Payload.Data.Size: short <- field p_Payload_Data_Size
      p.Payload.Raw: byte[] <- field p_Payload_Raw

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [Packet.Interpret(f.Content) -> pRows]
      ScalarForEach [p in pRows]
        AppendShape [result <- ResultShape0(p.Type: p.Type, p.Payload.Case: p.Payload.Case, p.Payload.Login.UserId: p.Payload.Login.UserId, p.Payload.Data.Size: p.Payload.Data.Size, p.Payload.Raw: p.Payload.Raw)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q307_SpecBinarySwitchTaggedUnion
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
            new Column("p.Type", typeof(byte), 0),
            new Column("p.Payload.Case", typeof(string), 1),
            new Column("p.Payload.Login.UserId", typeof(int), 2),
            new Column("p.Payload.Data.Size", typeof(short), 3),
            new Column("p.Payload.Raw", typeof(byte[]), 4)
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
            var __musoqMaterializedTable = QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
            _ = __musoqMaterializedTable.Count;
            return __musoqMaterializedTable;
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.p_Type, __musoqShapeRow.p_Payload_Case, __musoqShapeRow.p_Payload_Login_UserId, __musoqShapeRow.p_Payload_Data_Size, __musoqShapeRow.p_Payload_Raw);
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
                                    var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                                    var pRows = _interpreter_Packet.Interpret(f.Content);
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        yield return new ResultShape0(p.Type, ((Musoq.Generated.Interpreters.Switch_Payload)p.Payload).Case, ((Musoq.Generated.Interpreters.Switch_Payload)p.Payload).Login.UserId, ((Musoq.Generated.Interpreters.Switch_Payload)p.Payload).Data.Size, ((Musoq.Generated.Interpreters.Switch_Payload)p.Payload).Raw);
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
                                    var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                                    var pRows = _interpreter_Packet.Interpret(f.Content);
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        yield return new ResultShape0(p.Type, ((Musoq.Generated.Interpreters.Switch_Payload)p.Payload).Case, ((Musoq.Generated.Interpreters.Switch_Payload)p.Payload).Login.UserId, ((Musoq.Generated.Interpreters.Switch_Payload)p.Payload).Data.Size, ((Musoq.Generated.Interpreters.Switch_Payload)p.Payload).Raw);
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
                            var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                            var pRows = _interpreter_Packet.Interpret(f.Content);
                            if (pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = pRows;
                                yield return new ResultShape0(p.Type, ((Musoq.Generated.Interpreters.Switch_Payload)p.Payload).Case, ((Musoq.Generated.Interpreters.Switch_Payload)p.Payload).Login.UserId, ((Musoq.Generated.Interpreters.Switch_Payload)p.Payload).Data.Size, ((Musoq.Generated.Interpreters.Switch_Payload)p.Payload).Raw);
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
            public ResultRow0(byte __value0, string __value1, int __value2, short __value3, byte[] __value4)
            {
                p_Type = __value0;
                p_Payload_Case = __value1;
                p_Payload_Login_UserId = __value2;
                p_Payload_Data_Size = __value3;
                p_Payload_Raw = __value4;
            }

            public override int Count => 5;
            public string p_Payload_Case { get; private set; }
            public short p_Payload_Data_Size { get; private set; }
            public int p_Payload_Login_UserId { get; private set; }
            public byte[] p_Payload_Raw { get; private set; }
            public byte p_Type { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        p_Type = (byte)value;
                        break;
                    case 1:
                        p_Payload_Case = (string)value;
                        break;
                    case 2:
                        p_Payload_Login_UserId = (int)value;
                        break;
                    case 3:
                        p_Payload_Data_Size = (short)value;
                        break;
                    case 4:
                        p_Payload_Raw = (byte[])value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "p.Type" => true,
                "p_Type" => true,
                "Type" => true,
                "p.Payload.Case" => true,
                "p_Payload_Case" => true,
                "Case" => true,
                "p.Payload.Login.UserId" => true,
                "p_Payload_Login_UserId" => true,
                "UserId" => true,
                "p.Payload.Data.Size" => true,
                "p_Payload_Data_Size" => true,
                "Size" => true,
                "p.Payload.Raw" => true,
                "p_Payload_Raw" => true,
                "Raw" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)p_Type,
                1 => (object)p_Payload_Case,
                2 => (object)p_Payload_Login_UserId,
                3 => (object)p_Payload_Data_Size,
                4 => (object)p_Payload_Raw,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "p.Type" => (object)p_Type,
                "p_Type" => (object)p_Type,
                "Type" => (object)p_Type,
                "p.Payload.Case" => (object)p_Payload_Case,
                "p_Payload_Case" => (object)p_Payload_Case,
                "Case" => (object)p_Payload_Case,
                "p.Payload.Login.UserId" => (object)p_Payload_Login_UserId,
                "p_Payload_Login_UserId" => (object)p_Payload_Login_UserId,
                "UserId" => (object)p_Payload_Login_UserId,
                "p.Payload.Data.Size" => (object)p_Payload_Data_Size,
                "p_Payload_Data_Size" => (object)p_Payload_Data_Size,
                "Size" => (object)p_Payload_Data_Size,
                "p.Payload.Raw" => (object)p_Payload_Raw,
                "p_Payload_Raw" => (object)p_Payload_Raw,
                "Raw" => (object)p_Payload_Raw,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(byte p_Type, string p_Payload_Case, int p_Payload_Login_UserId, short p_Payload_Data_Size, byte[] p_Payload_Raw)
            {
                this.p_Type = p_Type;
                this.p_Payload_Case = p_Payload_Case;
                this.p_Payload_Login_UserId = p_Payload_Login_UserId;
                this.p_Payload_Data_Size = p_Payload_Data_Size;
                this.p_Payload_Raw = p_Payload_Raw;
            }

            public string p_Payload_Case { get; }
            public short p_Payload_Data_Size { get; }
            public int p_Payload_Login_UserId { get; }
            public byte[] p_Payload_Raw { get; }
            public byte p_Type { get; }
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
    /// Generated interpreter for binary schema 'LoginPayload'.
    /// </summary>
    public sealed class LoginPayload : BytesInterpreterBase<LoginPayload>
    {
        /// <summary>Gets the UserId field value.</summary>
        public int UserId { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "LoginPayload";

        /// <inheritdoc/>
        public override LoginPayload InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("UserId");
            var _userId = ReadInt32Le(data);
            RecordParsedField("UserId", _userId);
            return new LoginPayload
            {
                UserId = _userId
            };
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'DataPayload'.
    /// </summary>
    public sealed class DataPayload : BytesInterpreterBase<DataPayload>
    {
        /// <summary>Gets the Size field value.</summary>
        public short Size { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "DataPayload";

        /// <inheritdoc/>
        public override DataPayload InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("Size");
            var _size = ReadInt16Le(data);
            RecordParsedField("Size", _size);
            return new DataPayload
            {
                Size = _size
            };
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'Packet'.
    /// </summary>
    public sealed class Packet : BytesInterpreterBase<Packet>
    {
        /// <summary>Gets the Type field value.</summary>
        public byte Type { get; init; }
        /// <summary>Gets the Length field value.</summary>
        public byte Length { get; init; }
        /// <summary>Gets the Payload field value.</summary>
        public Switch_Payload Payload { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Packet";

        /// <inheritdoc/>
        public override Packet InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("Type");
            var _type = ReadByte(data);
            RecordParsedField("Type", _type);
            SetCurrentField("Length");
            var _length = ReadByte(data);
            RecordParsedField("Length", _length);
            SetCurrentField("Payload");
            string _payload_case = null;
            LoginPayload _payload_Login = default;
            DataPayload _payload_Data = default;
            byte[] _payload_Raw = default;
            if (_type == 1)
            {
                var _payload_Login_value_interpreter = new LoginPayload();
                var _payload_Login_value = InterpretNested(_payload_Login_value_interpreter, data, "Payload");
                _payload_Login = _payload_Login_value;
                _payload_case = "Login";
            }
            else if (_type == 2)
            {
                var _payload_Data_value_interpreter = new DataPayload();
                var _payload_Data_value = InterpretNested(_payload_Data_value_interpreter, data, "Payload");
                _payload_Data = _payload_Data_value;
                _payload_case = "Data";
            }
            else
            {
                var _payload_Raw_value = ReadBytes(data, (int)_length);
                _payload_Raw = _payload_Raw_value;
                _payload_case = "Raw";
            }

            var _payload = new Switch_Payload
            {
                Case = _payload_case,
                Login = _payload_Login,
                Data = _payload_Data,
                Raw = _payload_Raw
            };
            RecordParsedField("Payload", _payload);
            return new Packet
            {
                Type = _type,
                Length = _length,
                Payload = _payload
            };
        }
    }

    /// <summary>
    /// Generated tagged-union value for binary switch 'Switch_Payload'.
    /// </summary>
    public sealed class Switch_Payload
    {
        /// <summary>Gets the selected branch alias.</summary>
        public string Case { get; init; }
        /// <summary>Gets the 'Login' branch value; non-null only when selected.</summary>
        public LoginPayload Login { get; init; }
        /// <summary>Gets the 'Data' branch value; non-null only when selected.</summary>
        public DataPayload Data { get; init; }
        /// <summary>Gets the 'Raw' branch value; non-null only when selected.</summary>
        public byte[] Raw { get; init; }
    }
}
