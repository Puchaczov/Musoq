// === Parsed Query ===
/*
text Arm { Payload: rest }; text Packet { Variant: switch { _ => Arm } }; select p.Variant as Variant from #schema.first() s cross apply Parse<Packet>(s.Line) p
*/

// === Logical Plan ===
/*
MultiStatement
  Project [s.Line as s.Line, p.Variant as p.Variant]
    Apply [Cross]
      SchemaScan [#schema.first() as s]
      InterpretSource [#Packet(s.Line) as p]
  Project [p.Variant as Variant]
    CteRef [sp as sp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [s.Line as s.Line, p.Variant as p.Variant]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#schema.first() as s]
      PhysicalInterpretSource [#Packet(s.Line) as p]
  PhysicalProject [p.Variant as Variant]
    PhysicalCteRef [sp as sp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [s: SwitchLine]
      Line: string <- property Line
    SourceEntity [p: object]
      Variant: object <- property Variant
    Generated [ResultRow0]
      Variant: object <- field Variant

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [s: SwitchLine] -> sRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    ChunkedForEach [s in sRows]
      InterpretSource [Packet.Parse(s.Line) -> pRows]
      ScalarForEach [p in pRows]
        AppendShape [result <- ResultRow0(Variant: p.Variant)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
// === SYNTAX TREE 0 ===
namespace GeneratedSample_P57_TypedTextSwitchResult_Full
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Musoq.Schema;
    using Musoq.Schema.Diagnostics;
    using Musoq.Schema.Optimization;
    using Musoq.Evaluator;
    using Musoq.Evaluator.Diagnostics;
    using Musoq.Evaluator.Tables;
    using Musoq.Evaluator.Helpers;
    using Musoq.Evaluator.Runtime;
    using Musoq.Schema.DataSources;
    using System.Linq;

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable, IProfiledRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Variant", typeof(Musoq.Generated.Interpreters.Arm), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_s_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Line", typeof(string), 0) });
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
            var __musoqMaterializedTable = QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
            _ = __musoqMaterializedTable.Count;
            return __musoqMaterializedTable;
        }

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            ArgumentNullException.ThrowIfNull(profileRecorder);
            var __musoqMaterializedTable = QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0_Profiled(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken, profileRecorder), token);
            _ = __musoqMaterializedTable.Count;
            return __musoqMaterializedTable;
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return __musoqShapeRow;
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ProfiledOperatorEnumerable<ResultRow0>.Create(ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder), profileRecorder, profileRecorder?.GetCurrentOperatorScopeDepth() ?? 0))
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
                    var __sSchema = provider.GetSchema("#schema");
                    var sRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.SwitchLine>.Open(__sSchema, "first", new SourceExecutionContext("s:1", sourceExecutionPlans["s:1"], token, __schemaColumns_compiled_s_0, sourceRuntimeSettingsBySourceContextId["s:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                    var sRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.SwitchLine>(sRowsSource.Chunks, __musoqProgressContext, "s:1") : sRowsSource.Chunks;
                    foreach (var sChunk in sRows)
                    {
                        if (sChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.SwitchLine> sChunkView)
                        {
                            if (sChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.SwitchLine[] sChunkViewArray)
                            {
                                int sChunkViewOffset = sChunkView.Offset;
                                for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                                {
                                    if ((sIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var s = sChunkViewArray[sChunkViewOffset + sIndex];
                                    var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                                    var pRows = _interpreter_Packet.Parse(s.Line);
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        yield return new ResultRow0(p.Variant);
                                    }
                                }

                                continue;
                            }

                            if (sChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.SwitchLine> sChunkViewList)
                            {
                                int sChunkViewOffset = sChunkView.Offset;
                                for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                                {
                                    if ((sIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var s = sChunkViewList[sChunkViewOffset + sIndex];
                                    var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                                    var pRows = _interpreter_Packet.Parse(s.Line);
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        yield return new ResultRow0(p.Variant);
                                    }
                                }

                                continue;
                            }
                        }

                        for (int sIndex = 0, sIndexCount = sChunk.Count; sIndex < sIndexCount; ++sIndex)
                        {
                            if ((sIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var s = sChunk[sIndex];
                            var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                            var pRows = _interpreter_Packet.Parse(s.Line);
                            if (pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = pRows;
                                yield return new ResultRow0(p.Variant);
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

        private IEnumerable<ResultRow0> ComputeShapeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __op10Handle = profileRecorder?.GetOperatorHandle("op10", "PhaseBoundary") ?? OperatorProfileHandle.None;
                var __op11Handle = profileRecorder?.GetOperatorHandle("op11", "PhaseBoundary") ?? OperatorProfileHandle.None;
                var __op12Handle = profileRecorder?.GetOperatorHandle("op12", "PhaseBoundary") ?? OperatorProfileHandle.None;
                var __op13Handle = profileRecorder?.GetOperatorHandle("op13", "PhaseBoundary") ?? OperatorProfileHandle.None;
                var __op14Handle = profileRecorder?.GetOperatorHandle("op14", "SourceScan") ?? OperatorProfileHandle.None;
                var __op16Handle = profileRecorder?.GetOperatorHandle("op16", "ChunkedForEach") ?? OperatorProfileHandle.None;
                var __op17Handle = profileRecorder?.GetOperatorHandle("op17", "InterpretSource") ?? OperatorProfileHandle.None;
                var __op18Handle = profileRecorder?.GetOperatorHandle("op18", "ScalarForEach") ?? OperatorProfileHandle.None;
                var __op19Handle = profileRecorder?.GetOperatorHandle("op19", "AppendShape") ?? OperatorProfileHandle.None;
                var __op20Handle = profileRecorder?.GetOperatorHandle("op20", "PhaseBoundary") ?? OperatorProfileHandle.None;
                long __op19OutputRows = 0L;
                var __op10Scope = profileRecorder?.BeginOperatorValue(__op10Handle) ?? OperatorProfileValueScope.None;
                OnPhaseChanged("compiled", QueryPhase.Begin);
                __op10Scope.Dispose();
                var __op11Scope = profileRecorder?.BeginOperatorValue(__op11Handle) ?? OperatorProfileValueScope.None;
                OnPhaseChanged("compiled", QueryPhase.From);
                __op11Scope.Dispose();
                var __op12Scope = profileRecorder?.BeginOperatorValue(__op12Handle) ?? OperatorProfileValueScope.None;
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                __op12Scope.Dispose();
                try
                {
                    var __op13Scope = profileRecorder?.BeginOperatorValue(__op13Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op13Scope.Dispose();
                    var __op14Scope = profileRecorder?.BeginOperatorValue(__op14Handle) ?? OperatorProfileValueScope.None;
                    var __sSchema = provider.GetSchema("#schema");
                    var sRowsProfile = profileRecorder?.CreateSourceRecorder("s");
                    var sRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.SwitchLine>.Open(__sSchema, "first", new SourceExecutionContext("s:1", sourceExecutionPlans["s:1"], token, __schemaColumns_compiled_s_0, sourceRuntimeSettingsBySourceContextId["s:1"], logger, OnDataSourceProgress, sRowsProfile == null ? SourceDiagnostics.None : sRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                    var sRows = sRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.SwitchLine>(sRowsSource.Chunks, __musoqProgressContext, "s:1") : sRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.SwitchLine>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.SwitchLine>(sRowsSource.Chunks, __musoqProgressContext, "s:1") : sRowsSource.Chunks, sRowsProfile);
                    __op14Scope.Dispose();
                    long __op16InputRows = 0L;
                    long __op16OutputRows = 0L;
                    var __op16Scope = profileRecorder?.BeginOperatorValue(__op16Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        foreach (var sChunk in sRows)
                        {
                            if (sChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.SwitchLine> sChunkView)
                            {
                                if (sChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.SwitchLine[] sChunkViewArray)
                                {
                                    int sChunkViewOffset = sChunkView.Offset;
                                    for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                                    {
                                        if ((sIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var s = sChunkViewArray[sChunkViewOffset + sIndex];
                                        __op16InputRows++;
                                        __op16OutputRows++;
                                        var __op17Scope = profileRecorder?.BeginOperatorValue(__op17Handle) ?? OperatorProfileValueScope.None;
                                        var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                                        var pRows = _interpreter_Packet.Parse(s.Line);
                                        __op17Scope.Dispose();
                                        long __op18InputRows = 0L;
                                        long __op18OutputRows = 0L;
                                        var __op18Scope = profileRecorder?.BeginOperatorValue(__op18Handle) ?? OperatorProfileValueScope.None;
                                        try
                                        {
                                            if (pRows != null)
                                            {
                                                token.ThrowIfCancellationRequested();
                                                var p = pRows;
                                                __op18InputRows++;
                                                __op18OutputRows++;
                                                yield return new ResultRow0(p.Variant);
                                                __op19OutputRows += 1;
                                            }
                                        }
                                        finally
                                        {
                                            __op18Scope.AddInputRows(__op18InputRows);
                                            __op18Scope.AddOutputRows(__op18OutputRows);
                                            __op18Scope.Dispose();
                                        }
                                    }

                                    continue;
                                }

                                if (sChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.SwitchLine> sChunkViewList)
                                {
                                    int sChunkViewOffset = sChunkView.Offset;
                                    for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                                    {
                                        if ((sIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var s = sChunkViewList[sChunkViewOffset + sIndex];
                                        __op16InputRows++;
                                        __op16OutputRows++;
                                        var __op17Scope = profileRecorder?.BeginOperatorValue(__op17Handle) ?? OperatorProfileValueScope.None;
                                        var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                                        var pRows = _interpreter_Packet.Parse(s.Line);
                                        __op17Scope.Dispose();
                                        long __op18InputRows = 0L;
                                        long __op18OutputRows = 0L;
                                        var __op18Scope = profileRecorder?.BeginOperatorValue(__op18Handle) ?? OperatorProfileValueScope.None;
                                        try
                                        {
                                            if (pRows != null)
                                            {
                                                token.ThrowIfCancellationRequested();
                                                var p = pRows;
                                                __op18InputRows++;
                                                __op18OutputRows++;
                                                yield return new ResultRow0(p.Variant);
                                                __op19OutputRows += 1;
                                            }
                                        }
                                        finally
                                        {
                                            __op18Scope.AddInputRows(__op18InputRows);
                                            __op18Scope.AddOutputRows(__op18OutputRows);
                                            __op18Scope.Dispose();
                                        }
                                    }

                                    continue;
                                }
                            }

                            for (int sIndex = 0, sIndexCount = sChunk.Count; sIndex < sIndexCount; ++sIndex)
                            {
                                if ((sIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var s = sChunk[sIndex];
                                __op16InputRows++;
                                __op16OutputRows++;
                                var __op17Scope = profileRecorder?.BeginOperatorValue(__op17Handle) ?? OperatorProfileValueScope.None;
                                var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                                var pRows = _interpreter_Packet.Parse(s.Line);
                                __op17Scope.Dispose();
                                long __op18InputRows = 0L;
                                long __op18OutputRows = 0L;
                                var __op18Scope = profileRecorder?.BeginOperatorValue(__op18Handle) ?? OperatorProfileValueScope.None;
                                try
                                {
                                    if (pRows != null)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        var p = pRows;
                                        __op18InputRows++;
                                        __op18OutputRows++;
                                        yield return new ResultRow0(p.Variant);
                                        __op19OutputRows += 1;
                                    }
                                }
                                finally
                                {
                                    __op18Scope.AddInputRows(__op18InputRows);
                                    __op18Scope.AddOutputRows(__op18OutputRows);
                                    __op18Scope.Dispose();
                                }
                            }
                        }
                    }
                    finally
                    {
                        __op16Scope.AddInputRows(__op16InputRows);
                        __op16Scope.AddOutputRows(__op16OutputRows);
                        __op16Scope.Dispose();
                    }
                }
                finally
                {
                    var __op20Scope = profileRecorder?.BeginOperatorValue(__op20Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
                    __op20Scope.Dispose();
                }

                if (__op19OutputRows > 0L)
                    profileRecorder?.AddOperatorOutputRows(__op19Handle, __op19OutputRows);
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
            public ResultRow0(Musoq.Generated.Interpreters.Arm __value0)
            {
                Variant = __value0;
            }

            public override int Count => 1;
            public Musoq.Generated.Interpreters.Arm Variant { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, Musoq.Generated.Interpreters.Arm>(this.Variant = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<Musoq.Generated.Interpreters.Arm, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, Musoq.Generated.Interpreters.Arm>(this.Variant),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Variant" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Variant, ref visitor);
            }
        }
    }
} // === SYNTAX TREE 1 ===
// <auto-generated>
// This code was generated by Musoq Interpretation Schema code generator.
// Do not modify this file directly.
// </auto-generated>
#nullable enable
using  System ;  using  System . Buffers . Binary ;  using  System . Collections . Generic ;  using  System . Dynamic ;  using  Musoq . Schema . Interpreters ; 

namespace Musoq.Generated.Interpreters
{
    /// <summary>
    /// Generated interpreter for text schema 'Arm'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Arm : TextInterpreterBase<Arm>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Arm value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Payload"))
            {
                writer.WritePropertyName("Payload");
                var fieldValue = this.Payload;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Payload field value.</summary>
        public string? Payload { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Arm";

        /// <inheritdoc/>
        public override Arm ParseAt(ReadOnlySpan<char> data, int offset)
        {
            ParsePosition = offset;
            SetCurrentField(null);
            var __parsedValue = new Arm();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Payload");
            var _payload = ReadRest(data, fieldName: "Payload");
            __parsedValue.Payload = _payload;
            RecordParsedField("Payload");
            return __parsedValue;
        }
    }

    /// <summary>
    /// Generated interpreter for text schema 'Packet'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Packet : TextInterpreterBase<Packet>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Packet value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Variant"))
            {
                writer.WritePropertyName("Variant");
                var fieldValue = this.Variant;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Variant field value.</summary>
        public Arm? Variant { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Packet";

        /// <inheritdoc/>
        public override Packet ParseAt(ReadOnlySpan<char> data, int offset)
        {
            ParsePosition = offset;
            SetCurrentField(null);
            var __parsedValue = new Packet();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Variant");
            Arm? _variant = default;
            {
                var _interp_variant_selected = new Arm();
                var _result_variant_selected = ParseNested(_interp_variant_selected, data, "Variant");
                var _variant_selected = _result_variant_selected;
                _variant = _variant_selected;
            }

            __parsedValue.Variant = _variant;
            RecordParsedField("Variant");
            return __parsedValue;
        }
    }
}
