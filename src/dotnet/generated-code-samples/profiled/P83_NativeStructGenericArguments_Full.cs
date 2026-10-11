// === Parsed Query ===
/*
param(items: (Value: int)[]) select StructElements(NativeEcho($items)) as Length from #schema.first()
*/

// === Logical Plan ===
/*
MultiStatement
  Project [StructElements(NativeEcho($items)) as Length]
    SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [StructElements(NativeEcho($items)) as Length]
    PhysicalSchemaScan [#schema.first() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: Input]
      Position: int <- property Position
      Label: string <- property Label
    Generated [ResultRow0]
      Length: int <- field Length

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: Input] -> ko3ikoRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateObject [__resultGenericLibrary0: GenericLibrary]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      AppendShape [result <- ResultRow0(Length: StructElements(NativeEcho($items)))]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P83_NativeStructGenericArguments_Full
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Musoq.Schema;
    using Musoq.Schema.StructuralInputs;
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
            new Column("Length", typeof(int), 0)
        };
        private static readonly string[] __musoqDecodeTypedJson_0_element_fields = new string[]
        {
            "Value"
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Label", typeof(string), 1) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = new ScriptParameterContract[]
        {
            new ScriptParameterContract("items", "(Value: int)[]", "(Value: int)[]", typeof(Musoq.Schema.StructuralInputs.StructuralValue[]), false, true, typeof(Musoq.Schema.StructuralInputs.StructuralValue), "(Value: int)", false, ScriptParameterDefaultKind.None, null, StructuralTypeDescriptor.Collection(typeof(Musoq.Schema.StructuralInputs.StructuralValue[]), StructuralTypeDescriptor.Record(typeof(Musoq.Schema.StructuralInputs.StructuralValue), new StructuralFieldDescriptor[] { new StructuralFieldDescriptor("Value", StructuralTypeDescriptor.Scalar(typeof(int), false), true, null) }, false), false))
        };
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = new ScriptParameterDefinition[]
        {
            new ScriptParameterDefinition(new ScriptParameterContract("items", "(Value: int)[]", "(Value: int)[]", typeof(Musoq.Schema.StructuralInputs.StructuralValue[]), false, true, typeof(Musoq.Schema.StructuralInputs.StructuralValue), "(Value: int)", false, ScriptParameterDefaultKind.None, null, StructuralTypeDescriptor.Collection(typeof(Musoq.Schema.StructuralInputs.StructuralValue[]), StructuralTypeDescriptor.Record(typeof(Musoq.Schema.StructuralInputs.StructuralValue), new StructuralFieldDescriptor[] { new StructuralFieldDescriptor("Value", StructuralTypeDescriptor.Scalar(typeof(int), false), true, null) }, false), false)))
        };
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

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            ArgumentNullException.ThrowIfNull(profileRecorder);
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0_Profiled(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken, profileRecorder), token);
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
                var __musoqTypedParameterCaptureState = new StructuralParameterCaptureState(token);
                var paramItems = __musoqBindTypedJson_0(__musoqExecutionState, "items", __musoqTypedParameterCaptureState);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, new string[] { "items" });
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#schema");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTests.Input>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTests.Input>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var __resultGenericLibrary0 = new Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTests.GenericLibrary();
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTests.Input> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTests.Input[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                yield return new ResultRow0((int)__resultGenericLibrary0.StructElements<__musoqStructuralCarrier_0_a>(__resultGenericLibrary0.NativeEcho<__musoqStructuralCarrier_0_a>(paramItems)));
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTests.Input> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                yield return new ResultRow0((int)__resultGenericLibrary0.StructElements<__musoqStructuralCarrier_0_a>(__resultGenericLibrary0.NativeEcho<__musoqStructuralCarrier_0_a>(paramItems)));
                            }

                            continue;
                        }
                    }

                    for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunk.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                    {
                        if ((ko3ikoIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var ko3iko = ko3ikoChunk[ko3ikoIndex];
                        yield return new ResultRow0((int)__resultGenericLibrary0.StructElements<__musoqStructuralCarrier_0_a>(__resultGenericLibrary0.NativeEcho<__musoqStructuralCarrier_0_a>(paramItems)));
                    }
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
                var __musoqTypedParameterCaptureState = new StructuralParameterCaptureState(token);
                var paramItems = __musoqBindTypedJson_0(__musoqExecutionState, "items", __musoqTypedParameterCaptureState);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, new string[] { "items" });
                var __op9Handle = profileRecorder?.GetOperatorHandle("op9", "PhaseBoundary") ?? OperatorProfileHandle.None;
                var __op10Handle = profileRecorder?.GetOperatorHandle("op10", "PhaseBoundary") ?? OperatorProfileHandle.None;
                var __op11Handle = profileRecorder?.GetOperatorHandle("op11", "SourceScan") ?? OperatorProfileHandle.None;
                var __op13Handle = profileRecorder?.GetOperatorHandle("op13", "CreateObject") ?? OperatorProfileHandle.None;
                var __op14Handle = profileRecorder?.GetOperatorHandle("op14", "PhaseBoundary") ?? OperatorProfileHandle.None;
                var __op15Handle = profileRecorder?.GetOperatorHandle("op15", "ChunkedForEach") ?? OperatorProfileHandle.None;
                var __op16Handle = profileRecorder?.GetOperatorHandle("op16", "AppendShape") ?? OperatorProfileHandle.None;
                long __op16OutputRows = 0L;
                var __op9Scope = profileRecorder?.BeginOperatorValue(__op9Handle) ?? OperatorProfileValueScope.None;
                OnPhaseChanged("compiled", QueryPhase.Begin);
                __op9Scope.Dispose();
                var __op10Scope = profileRecorder?.BeginOperatorValue(__op10Handle) ?? OperatorProfileValueScope.None;
                OnPhaseChanged("compiled", QueryPhase.From);
                __op10Scope.Dispose();
                var __op11Scope = profileRecorder?.BeginOperatorValue(__op11Handle) ?? OperatorProfileValueScope.None;
                var __ko3ikoSchema = provider.GetSchema("#schema");
                var ko3ikoRowsProfile = profileRecorder?.CreateSourceRecorder("ko3iko");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTests.Input>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress, ko3ikoRowsProfile == null ? SourceDiagnostics.None : ko3ikoRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = ko3ikoRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTests.Input>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTests.Input>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTests.Input>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks, ko3ikoRowsProfile);
                __op11Scope.Dispose();
                var __op13Scope = profileRecorder?.BeginOperatorValue(__op13Handle) ?? OperatorProfileValueScope.None;
                var __resultGenericLibrary0 = new Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTests.GenericLibrary();
                __op13Scope.Dispose();
                var __op14Scope = profileRecorder?.BeginOperatorValue(__op14Handle) ?? OperatorProfileValueScope.None;
                OnPhaseChanged("compiled", QueryPhase.Select);
                __op14Scope.Dispose();
                long __op15InputRows = 0L;
                long __op15OutputRows = 0L;
                var __op15Scope = profileRecorder?.BeginOperatorValue(__op15Handle) ?? OperatorProfileValueScope.None;
                try
                {
                    foreach (var ko3ikoChunk in ko3ikoRows)
                    {
                        if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTests.Input> ko3ikoChunkView)
                        {
                            if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTests.Input[] ko3ikoChunkViewArray)
                            {
                                int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                                for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                                {
                                    if ((ko3ikoIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                    __op15InputRows++;
                                    __op15OutputRows++;
                                    yield return new ResultRow0((int)__resultGenericLibrary0.StructElements<__musoqStructuralCarrier_0_a>(__resultGenericLibrary0.NativeEcho<__musoqStructuralCarrier_0_a>(paramItems)));
                                    __op16OutputRows += 1;
                                }

                                continue;
                            }

                            if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTests.Input> ko3ikoChunkViewList)
                            {
                                int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                                for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                                {
                                    if ((ko3ikoIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                    __op15InputRows++;
                                    __op15OutputRows++;
                                    yield return new ResultRow0((int)__resultGenericLibrary0.StructElements<__musoqStructuralCarrier_0_a>(__resultGenericLibrary0.NativeEcho<__musoqStructuralCarrier_0_a>(paramItems)));
                                    __op16OutputRows += 1;
                                }

                                continue;
                            }
                        }

                        for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunk.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                        {
                            if ((ko3ikoIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var ko3iko = ko3ikoChunk[ko3ikoIndex];
                            __op15InputRows++;
                            __op15OutputRows++;
                            yield return new ResultRow0((int)__resultGenericLibrary0.StructElements<__musoqStructuralCarrier_0_a>(__resultGenericLibrary0.NativeEcho<__musoqStructuralCarrier_0_a>(paramItems)));
                            __op16OutputRows += 1;
                        }
                    }
                }
                finally
                {
                    __op15Scope.AddInputRows(__op15InputRows);
                    __op15Scope.AddOutputRows(__op15OutputRows);
                    __op15Scope.Dispose();
                }

                if (__op16OutputRows > 0L)
                    profileRecorder?.AddOperatorOutputRows(__op16Handle, __op16OutputRows);
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

        private static __musoqStructuralCarrier_0_a[] __musoqBindTypedJson_0(ExecutionState executionState, string name, StructuralParameterCaptureState state)
        {
            try
            {
                return __musoqDecodeTypedJson_0_(executionState.TypedParameters.GetRequired<System.Text.Json.JsonElement>(name), state, 1, "$" + name);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.TypeMismatch(name, typeof(__musoqStructuralCarrier_0_a[]), typeof(System.Text.Json.JsonElement), exception);
            }
        }

        private static __musoqStructuralCarrier_0_a[] __musoqDecodeTypedJson_0_(System.Text.Json.JsonElement value, StructuralParameterCaptureState state, int depth, string path)
        {
            state.ReserveNode(depth);
            if (value.ValueKind != System.Text.Json.JsonValueKind.Array)
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.TypeMismatch(path, typeof(__musoqStructuralCarrier_0_a[]), typeof(System.Text.Json.JsonElement));
            var count = value.GetArrayLength();
            state.EnsureCollectionLowerBound(count);
            var result = new __musoqStructuralCarrier_0_a[count];
            for (var index = 0; index < count; index++)
                result[index] = __musoqDecodeTypedJson_0_element(value[index], state, depth + 1, path + "[" + index + "]");
            return result;
        }

        private static __musoqStructuralCarrier_0_a __musoqDecodeTypedJson_0_element(System.Text.Json.JsonElement value, StructuralParameterCaptureState state, int depth, string path)
        {
            TypedParameterJsonReader.RequireRecord(value, state, depth, path);
            TypedParameterJsonReader.ValidateFields(value, __musoqDecodeTypedJson_0_element_fields, path);
            var(present0, input0) = TypedParameterJsonReader.FindField(value, "Value");
            if (!present0)
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.MissingRequired(path + ".Value");
            int field0 = present0 ? __musoqDecodeTypedJson_0_element_0(input0, state, depth + 1, path + ".Value") : default(int);
            return new __musoqStructuralCarrier_0_a(field0, present0 ? 1UL : 0UL);
        }

        private static int __musoqDecodeTypedJson_0_element_0(System.Text.Json.JsonElement value, StructuralParameterCaptureState state, int depth, string path)
        {
            state.ReserveNode(depth);
            if (value.ValueKind == System.Text.Json.JsonValueKind.Null)
            {
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.NullNotAllowed(path, typeof(int));
            }

            var result = value.GetInt32();
            return result;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0)
            {
                Length = __value0;
            }

            public override int Count => 1;
            public int Length { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Length = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Length),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Length" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Length, ref visitor);
            }
        }

        [global::System.Text.Json.Serialization.JsonConverter(typeof(__musoqStructuralCarrier_0_aJsonConverter))]
        private readonly struct __musoqStructuralCarrier_0_a
        {
            public readonly int F0;
            public readonly ulong P0;
            public __musoqStructuralCarrier_0_a(int f0, ulong p0)
            {
                F0 = f0;
                P0 = p0;
            }
        }

        private sealed class __musoqStructuralCarrier_0_aJsonConverter : global::System.Text.Json.Serialization.JsonConverter<__musoqStructuralCarrier_0_a>
        {
            public __musoqStructuralCarrier_0_aJsonConverter()
            {
            }

            public override __musoqStructuralCarrier_0_a Read(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
            {
                if (reader.TokenType != global::System.Text.Json.JsonTokenType.StartObject)
                    throw new global::System.Text.Json.JsonException("A native record requires a JSON object.");
                int field0 = default;
                ulong p0 = 0;
                while (reader.Read())
                {
                    if (reader.TokenType == global::System.Text.Json.JsonTokenType.EndObject)
                    {
                        if ((p0 & 1UL) == 0)
                            throw new global::System.Text.Json.JsonException("A required native field is missing.");
                        return new __musoqStructuralCarrier_0_a(field0, p0);
                    }

                    if (reader.TokenType != global::System.Text.Json.JsonTokenType.PropertyName)
                        throw new global::System.Text.Json.JsonException("A native record requires named fields.");
                    if (reader.ValueTextEquals("Value"))
                    {
                        if ((p0 & 1UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field0 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 1UL;
                        continue;
                    }

                    throw new global::System.Text.Json.JsonException("The native record contains an undeclared field.");
                }

                throw new global::System.Text.Json.JsonException("The native record is incomplete.");
            }

            public override void Write(global::System.Text.Json.Utf8JsonWriter writer, __musoqStructuralCarrier_0_a value, global::System.Text.Json.JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                if ((value.P0 & 1UL) != 0)
                {
                    writer.WritePropertyName("Value");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F0, options);
                }

                writer.WriteEndObject();
            }
        }
    }
}
