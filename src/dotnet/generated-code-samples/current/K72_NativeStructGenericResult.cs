// === Parsed Query ===
/*
param(items: (Value: int)[]) with echoed as (select StructEcho($items) as Echo from #schema.first()) select e.Echo as Echo from echoed e
*/

// === Logical Plan ===
/*
Cte
  Definition [echoed]
    MultiStatement
      Project [StructEcho($items) as Echo]
        SchemaScan [#schema.first() as ko3iko]
  Query
    MultiStatement
      Project [e.Echo as Echo]
        CteRef [echoed as e]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [echoed]
    PhysicalMultiStatement
      PhysicalProject [StructEcho($items) as Echo]
        PhysicalSchemaScan [#schema.first() as ko3iko]
  Query
    PhysicalMultiStatement
      PhysicalProject [e.Echo as Echo]
        PhysicalCteRef [echoed as e]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: Input]
      Position: int <- property Position
      Label: string <- property Label
    Generated [ResultRow0]
      Echo: IReadOnlyList<GeneratedRecordConstraintWitness> <- field Echo

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [From:cte0]
    SourceScan [ko3iko: Input] -> ko3ikoRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select:cte0]
    CreateObject [__resultGenericLibrary0: GenericLibrary]
    ChunkedForEach [ko3iko in ko3ikoRows]
      AppendShape [result <- ResultRow0(Echo: StructEcho($items))]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K72_NativeStructGenericResult
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Musoq.Schema;
    using Musoq.Schema.StructuralInputs;
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
            new Column("Echo", typeof(System.Collections.Generic.IReadOnlyList<__musoqStructuralCarrier_0_a>), 0)
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

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
            var __musoqTypedParameterCaptureState = new StructuralParameterCaptureState(token);
            var paramItems = __musoqBindTypedJson_0(__musoqExecutionState, "items", __musoqTypedParameterCaptureState);
            ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, new string[] { "items" });
            this.OnPhaseChanged("compiled", QueryPhase.Begin);
            this.OnPhaseChanged("compiled", QueryPhase.From);
            this.OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            this.OnPhaseChanged("compiled:cte0", QueryPhase.From);
            var __ko3ikoSchema = provider.GetSchema("#schema");
            var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTests.Input>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTests.Input>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
            var __resultGenericLibrary0 = new Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTests.GenericLibrary();
            var __musoqTableSourceRows = ko3ikoRows;
            this.OnPhaseChanged("compiled", QueryPhase.Select);
            this.OnPhaseChanged("compiled:cte0", QueryPhase.Select);
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectRowsSerial<Musoq.Evaluator.Tests.LibraryExecutionNativeGenericQueryTests.Input, ResultRow0>(__musoqTableSourceRows, (ko3iko) => true, (ko3iko) => new ResultRow0(__resultGenericLibrary0.StructEcho<__musoqStructuralCarrier_0_a>(paramItems)), token), token, onCompleted: () =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }, onException: (Exception _) =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }, onDisposed: () =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            });
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
            public ResultRow0(System.Collections.Generic.IReadOnlyList<__musoqStructuralCarrier_0_a> __value0)
            {
                Echo = __value0;
            }

            public override int Count => 1;
            public System.Collections.Generic.IReadOnlyList<__musoqStructuralCarrier_0_a> Echo { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, System.Collections.Generic.IReadOnlyList<__musoqStructuralCarrier_0_a>>(this.Echo = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<System.Collections.Generic.IReadOnlyList<__musoqStructuralCarrier_0_a>, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, System.Collections.Generic.IReadOnlyList<__musoqStructuralCarrier_0_a>>(this.Echo),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Echo" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Echo, ref visitor);
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
