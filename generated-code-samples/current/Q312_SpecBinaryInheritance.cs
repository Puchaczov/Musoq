// === Parsed Query ===
/*
binary Base { Id: byte };
                    binary Child extends Base { Version: byte };
                    binary Grandchild extends Child { Flags: byte };
                    select g.Id, g.Version, g.Flags
                    from #test.files() f
                    cross apply Interpret<Grandchild>(f.Content) g
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Content as f.Content, g.Id as g.Id, g.Version as g.Version, g.Flags as g.Flags]
    Apply [Cross]
      SchemaScan [#test.files() as f]
      InterpretSource [#Grandchild(f.Content) as g]
  Project [g.Id as g.Id, g.Version as g.Version, g.Flags as g.Flags]
    CteRef [fg as fg]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [f.Content as f.Content, g.Id as g.Id, g.Version as g.Version, g.Flags as g.Flags]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.files() as f]
      PhysicalInterpretSource [#Grandchild(f.Content) as g]
  PhysicalProject [g.Id as g.Id, g.Version as g.Version, g.Flags as g.Flags]
    PhysicalCteRef [fg as fg]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [g: object]
      Id: byte <- property Id
      Version: byte <- property Version
      Flags: byte <- property Flags
    Generated [ResultRow0]
      g.Id: byte <- field g_Id
      g.Version: byte <- field g_Version
      g.Flags: byte <- field g_Flags

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [f: BinaryEntity] -> fRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [f in fRows]
      InterpretSource [Grandchild.Interpret(f.Content) -> gRows]
      ScalarForEach [g in gRows]
        AppendShape [result <- ResultShape0(g.Id: g.Id, g.Version: g.Version, g.Flags: g.Flags)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q312_SpecBinaryInheritance
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
            new Column("g.Id", typeof(byte), 0),
            new Column("g.Version", typeof(byte), 1),
            new Column("g.Flags", typeof(byte), 2)
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
                yield return new ResultRow0(__musoqShapeRow.g_Id, __musoqShapeRow.g_Version, __musoqShapeRow.g_Flags);
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
                                    var _interpreter_Grandchild = new Musoq.Generated.Interpreters.Grandchild();
                                    var gRows = _interpreter_Grandchild.Interpret(f.Content);
                                    if (gRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var g = gRows;
                                        yield return new ResultShape0(g.Id, g.Version, g.Flags);
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
                                    var _interpreter_Grandchild = new Musoq.Generated.Interpreters.Grandchild();
                                    var gRows = _interpreter_Grandchild.Interpret(f.Content);
                                    if (gRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var g = gRows;
                                        yield return new ResultShape0(g.Id, g.Version, g.Flags);
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
                            var _interpreter_Grandchild = new Musoq.Generated.Interpreters.Grandchild();
                            var gRows = _interpreter_Grandchild.Interpret(f.Content);
                            if (gRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var g = gRows;
                                yield return new ResultShape0(g.Id, g.Version, g.Flags);
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
            public ResultRow0(byte __value0, byte __value1, byte __value2)
            {
                g_Id = __value0;
                g_Version = __value1;
                g_Flags = __value2;
            }

            public override int Count => 3;
            public byte g_Flags { get; private set; }
            public byte g_Id { get; private set; }
            public byte g_Version { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        g_Id = (byte)value;
                        break;
                    case 1:
                        g_Version = (byte)value;
                        break;
                    case 2:
                        g_Flags = (byte)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "g.Id" => true,
                "g_Id" => true,
                "Id" => true,
                "g.Version" => true,
                "g_Version" => true,
                "Version" => true,
                "g.Flags" => true,
                "g_Flags" => true,
                "Flags" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)g_Id,
                1 => (object)g_Version,
                2 => (object)g_Flags,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "g.Id" => (object)g_Id,
                "g_Id" => (object)g_Id,
                "Id" => (object)g_Id,
                "g.Version" => (object)g_Version,
                "g_Version" => (object)g_Version,
                "Version" => (object)g_Version,
                "g.Flags" => (object)g_Flags,
                "g_Flags" => (object)g_Flags,
                "Flags" => (object)g_Flags,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(byte g_Id, byte g_Version, byte g_Flags)
            {
                this.g_Id = g_Id;
                this.g_Version = g_Version;
                this.g_Flags = g_Flags;
            }

            public byte g_Flags { get; }
            public byte g_Id { get; }
            public byte g_Version { get; }
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
    /// Generated interpreter for binary schema 'Base'.
    /// </summary>
    public sealed class Base : BytesInterpreterBase<Base>
    {
        /// <summary>Gets the Id field value.</summary>
        public byte Id { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Base";

        /// <inheritdoc/>
        public override Base InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("Id");
            var _id = ReadByte(data);
            RecordParsedField("Id", _id);
            return new Base
            {
                Id = _id
            };
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'Child'.
    /// Extends schema 'Base'.
    /// </summary>
    public sealed class Child : BytesInterpreterBase<Child>
    {
        /// <summary>Gets the Id field value.</summary>
        public byte Id { get; init; }
        /// <summary>Gets the Version field value.</summary>
        public byte Version { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Child";

        /// <inheritdoc/>
        public override Child InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("Id");
            var _id = ReadByte(data);
            RecordParsedField("Id", _id);
            SetCurrentField("Version");
            var _version = ReadByte(data);
            RecordParsedField("Version", _version);
            return new Child
            {
                Id = _id,
                Version = _version
            };
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'Grandchild'.
    /// Extends schema 'Child'.
    /// </summary>
    public sealed class Grandchild : BytesInterpreterBase<Grandchild>
    {
        /// <summary>Gets the Id field value.</summary>
        public byte Id { get; init; }
        /// <summary>Gets the Version field value.</summary>
        public byte Version { get; init; }
        /// <summary>Gets the Flags field value.</summary>
        public byte Flags { get; init; }
        /// <inheritdoc/>
        public override string SchemaName => "Grandchild";

        /// <inheritdoc/>
        public override Grandchild InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            SetCurrentField("Id");
            var _id = ReadByte(data);
            RecordParsedField("Id", _id);
            SetCurrentField("Version");
            var _version = ReadByte(data);
            RecordParsedField("Version", _version);
            SetCurrentField("Flags");
            var _flags = ReadByte(data);
            RecordParsedField("Flags", _flags);
            return new Grandchild
            {
                Id = _id,
                Version = _version,
                Flags = _flags
            };
        }
    }
}
