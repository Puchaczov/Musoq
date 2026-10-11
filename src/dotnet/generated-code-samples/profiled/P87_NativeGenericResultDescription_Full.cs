// === Parsed Query ===
/*
param(items: (Value: int)[]) desc query (select StructEcho($items) as Echo from #schema.first())
*/

// === Logical Plan ===
/*
DescQuery
*/

// === Physical Plan ===
/*
PhysicalDescQuery
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes

  Body
    PhaseBoundary [Begin]
    ReturnDesc [query Query]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P87_NativeGenericResultDescription_Full
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
    using Musoq.Schema.DataSources;
    using System.Linq;

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable, IProfiledRunnable, IMetadataOnlyRunnable
    {
        private static readonly string[] __musoqDecodeTypedJson_0_element_fields = new string[]
        {
            "Value"
        };
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
            return ComputeTable_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, token);
        }

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            ArgumentNullException.ThrowIfNull(profileRecorder);
            return ComputeTable_compiled_0_Profiled(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, token, profileRecorder);
        }

        private Table ComputeTable_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
            OnPhaseChanged("compiled", QueryPhase.Begin);
            try
            {
                __musoqProgressContext?.CompleteQueryProgress();
            }
            finally
            {
                OnPhaseChanged("compiled", QueryPhase.End);
            }

            return EvaluationHelper.GetQueryDescription(new global::Musoq.Schema.QueryColumnDescription[] { new global::Musoq.Schema.QueryColumnDescription("Echo", 0, "(Value: int32)[]") });
            OnPhaseChanged("compiled", QueryPhase.Select);
        }

        private Table ComputeTable_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            var __profileScopeDepth = profileRecorder?.GetCurrentOperatorScopeDepth() ?? 0;
            try
            {
                QueryProgressEventHandler OnQueryProgress = QueryProgress;
                var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
                Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                var __op4Handle = profileRecorder?.GetOperatorHandle("op4", "PhaseBoundary") ?? OperatorProfileHandle.None;
                var __op6Handle = profileRecorder?.GetOperatorHandle("op6", "PhaseBoundary") ?? OperatorProfileHandle.None;
                var __op4Scope = profileRecorder?.BeginOperatorValue(__op4Handle) ?? OperatorProfileValueScope.None;
                OnPhaseChanged("compiled", QueryPhase.Begin);
                __op4Scope.Dispose();
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }

                return EvaluationHelper.GetQueryDescription(new global::Musoq.Schema.QueryColumnDescription[] { new global::Musoq.Schema.QueryColumnDescription("Echo", 0, "(Value: int32)[]") });
                var __op6Scope = profileRecorder?.BeginOperatorValue(__op6Handle) ?? OperatorProfileValueScope.None;
                OnPhaseChanged("compiled", QueryPhase.Select);
                __op6Scope.Dispose();
            }
            catch (Exception __profileException)when (profileRecorder != null && profileRecorder.RecordActiveOperatorException(__profileException, __profileScopeDepth))
            {
                profileRecorder.DisposeActiveOperatorScopes(__profileScopeDepth);
                throw;
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
