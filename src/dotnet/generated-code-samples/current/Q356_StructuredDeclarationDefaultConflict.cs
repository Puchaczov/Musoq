// === Parsed Query ===
/*
param(input: (
    Id: string,
    Pattern: string,
    Mode: string = 'regex'
) = (Id: 'todo', Pattern: 'TODO'))

select d.Id, d.Mode
from #inputs.defaultconflict(input: $input) d
*/

// === Logical Plan ===
/*
MultiStatement
  Project [d.Id as Id, d.Mode as Mode]
    SchemaScan [#inputs.defaultconflict(<PatternInput>$input) as d]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [d.Id as Id, d.Mode as Mode]
    PhysicalSchemaScan [#inputs.defaultconflict(<PatternInput>$input) as d]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [d: DefaultConflictRow]
      Id: string <- property Id
      Mode: string <- property Mode
    Generated [ResultRow0]
      Id: string <- field Id
      Mode: string <- field Mode

  Body
    PhaseBoundary [Begin]
    Let [__musoqStructural_d_0: PatternInput = convert<Musoq.Examples.DataSources.StructuredInputs.PatternInput>($input)]
    PhaseBoundary [From]
    SourceScan [d: DefaultConflictRow] -> dRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [d in dRows]
      AppendShape [result <- ResultRow0(Id: d.Id, Mode: d.Mode)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q356_StructuredDeclarationDefaultConflict
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
            new Column("Id", typeof(string), 0),
            new Column("Mode", typeof(string), 1)
        };
        private static readonly string[] __musoqDecodeTypedJson_0__fields = new string[]
        {
            "Id",
            "Pattern",
            "Mode"
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_d_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(string), 0), new Column("Mode", typeof(string), 1) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = new ScriptParameterContract[]
        {
            new ScriptParameterContract("input", "(Id: string, Pattern: string, Mode: string = 'regex')", "(Id: string, Pattern: string, Mode: string = 'regex')", typeof(Musoq.Schema.StructuralInputs.StructuralValue), false, false, null, null, true, ScriptParameterDefaultKind.Structured, StructuralValue.FromRecord(new KeyValuePair<string, StructuralValue>[] { new KeyValuePair<string, StructuralValue>("Id", StructuralValue.FromScalar("todo")), new KeyValuePair<string, StructuralValue>("Pattern", StructuralValue.FromScalar("TODO")), new KeyValuePair<string, StructuralValue>("Mode", StructuralValue.FromScalar("regex")) }), StructuralTypeDescriptor.Record(typeof(Musoq.Schema.StructuralInputs.StructuralValue), new StructuralFieldDescriptor[] { new StructuralFieldDescriptor("Id", StructuralTypeDescriptor.Scalar(typeof(string), true), true, null), new StructuralFieldDescriptor("Pattern", StructuralTypeDescriptor.Scalar(typeof(string), true), true, null), new StructuralFieldDescriptor("Mode", StructuralTypeDescriptor.Scalar(typeof(string), true), false, StructuralDefaultDescriptor.Create("regex", "'regex'")) }, false))
        };
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = new ScriptParameterDefinition[]
        {
            new ScriptParameterDefinition(new ScriptParameterContract("input", "(Id: string, Pattern: string, Mode: string = 'regex')", "(Id: string, Pattern: string, Mode: string = 'regex')", typeof(Musoq.Schema.StructuralInputs.StructuralValue), false, false, null, null, true, ScriptParameterDefaultKind.Structured, StructuralValue.FromRecord(new KeyValuePair<string, StructuralValue>[] { new KeyValuePair<string, StructuralValue>("Id", StructuralValue.FromScalar("todo")), new KeyValuePair<string, StructuralValue>("Pattern", StructuralValue.FromScalar("TODO")), new KeyValuePair<string, StructuralValue>("Mode", StructuralValue.FromScalar("regex")) }), StructuralTypeDescriptor.Record(typeof(Musoq.Schema.StructuralInputs.StructuralValue), new StructuralFieldDescriptor[] { new StructuralFieldDescriptor("Id", StructuralTypeDescriptor.Scalar(typeof(string), true), true, null), new StructuralFieldDescriptor("Pattern", StructuralTypeDescriptor.Scalar(typeof(string), true), true, null), new StructuralFieldDescriptor("Mode", StructuralTypeDescriptor.Scalar(typeof(string), true), false, StructuralDefaultDescriptor.Create("regex", "'regex'")) }, false)))
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
                var __musoqTypedParameterCaptureState = new StructuralParameterCaptureState(token);
                var paramInput = __musoqBindTypedJson_0(__musoqExecutionState, "input", __musoqTypedParameterCaptureState);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, new string[] { "input" });
                OnPhaseChanged("compiled", QueryPhase.Begin);
                Musoq.Examples.DataSources.StructuredInputs.PatternInput __musoqStructural_d_0 = __musoqStructuralAdapt_0_919131913(paramInput);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __dSchema = provider.GetSchema("#inputs");
                var dRowsSourceContext = new SourceExecutionContext("d:1", sourceExecutionPlans["d:1"], token, __schemaColumns_compiled_d_0, sourceRuntimeSettingsBySourceContextId["d:1"], logger, OnDataSourceProgress);
                Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.DefaultConflictRow> dRowsSource;
                try
                {
                    Musoq.Examples.DataSources.StructuredInputs.DefaultConflictSource dRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.DefaultConflictSource(__musoqCheckStructural_f2b4c3cc7af6ecaa(__musoqStructural_d_0, token), dRowsSourceContext);
                    dRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.DefaultConflictSource, Musoq.Examples.DataSources.StructuredInputs.DefaultConflictRow>(__dSchema, "defaultconflict", dRowsSourceInstance, dRowsSourceContext, "#inputs", "d", "d:1");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (global::Musoq.Schema.Exceptions.DataSourceLifecycleException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "defaultconflict", "d", "d:1", exception);
                }

                var dRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.DefaultConflictRow>(dRowsSource.Chunks, __musoqProgressContext, "d:1") : dRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var dChunk in dRows)
                {
                    if (dChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Examples.DataSources.StructuredInputs.DefaultConflictRow> dChunkView)
                    {
                        if (dChunkView.Source is Musoq.Examples.DataSources.StructuredInputs.DefaultConflictRow[] dChunkViewArray)
                        {
                            int dChunkViewOffset = dChunkView.Offset;
                            for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                            {
                                if ((dIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var d = dChunkViewArray[dChunkViewOffset + dIndex];
                                yield return new ResultRow0(d.Id, d.Mode);
                            }

                            continue;
                        }

                        if (dChunkView.Source is List<Musoq.Examples.DataSources.StructuredInputs.DefaultConflictRow> dChunkViewList)
                        {
                            int dChunkViewOffset = dChunkView.Offset;
                            for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                            {
                                if ((dIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var d = dChunkViewList[dChunkViewOffset + dIndex];
                                yield return new ResultRow0(d.Id, d.Mode);
                            }

                            continue;
                        }
                    }

                    for (int dIndex = 0, dIndexCount = dChunk.Count; dIndex < dIndexCount; ++dIndex)
                    {
                        if ((dIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var d = dChunk[dIndex];
                        yield return new ResultRow0(d.Id, d.Mode);
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

        private static __musoqStructuralCarrier_0_root __musoqBindTypedJson_0(ExecutionState executionState, string name, StructuralParameterCaptureState state)
        {
            try
            {
                if (!executionState.TypedParameters.Contains(name))
                {
                    state.ReserveMetrics(new StructuralInputMetrics(2, 4L, 26L), 1);
                    return new __musoqStructuralCarrier_0_root("todo", "TODO", "regex", 7UL);
                }

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
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.TypeMismatch(name, typeof(__musoqStructuralCarrier_0_root), typeof(System.Text.Json.JsonElement), exception);
            }
        }

        private static Musoq.Examples.DataSources.StructuredInputs.PatternInput __musoqCheckStructural_f2b4c3cc7af6ecaa(Musoq.Examples.DataSources.StructuredInputs.PatternInput value, System.Threading.CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            long __musoqStructuralNodes = 0L;
            long __musoqStructuralStrings = 0L;
            int __musoqStructuralMaxDepth = 0;
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (1 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 1;
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (2 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 2;
            if (value.Id != null)
                __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)value.Id.Length * 2L));
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (2 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 2;
            if (value.Pattern != null)
                __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)value.Pattern.Length * 2L));
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (2 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 2;
            if (value.Mode != null)
                __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)value.Mode.Length * 2L));
            global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("parameter", "$input", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "d:1");
            return value;
        }

        private static __musoqStructuralCarrier_0_root __musoqDecodeTypedJson_0_(System.Text.Json.JsonElement value, StructuralParameterCaptureState state, int depth, string path)
        {
            TypedParameterJsonReader.RequireRecord(value, state, depth, path);
            TypedParameterJsonReader.ValidateFields(value, __musoqDecodeTypedJson_0__fields, path);
            var(present0, input0) = TypedParameterJsonReader.FindField(value, "Id");
            if (!present0)
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.MissingRequired(path + ".Id");
            string field0 = present0 ? __musoqDecodeTypedJson_0_0(input0, state, depth + 1, path + ".Id") : default(string);
            var(present1, input1) = TypedParameterJsonReader.FindField(value, "Pattern");
            if (!present1)
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.MissingRequired(path + ".Pattern");
            string field1 = present1 ? __musoqDecodeTypedJson_0_1(input1, state, depth + 1, path + ".Pattern") : default(string);
            var(present2, input2) = TypedParameterJsonReader.FindField(value, "Mode");
            if (!present2)
                state.ReserveMetrics(new StructuralInputMetrics(1, 1L, 10L), depth + 1);
            string field2 = present2 ? __musoqDecodeTypedJson_0_2(input2, state, depth + 1, path + ".Mode") : "regex";
            return new __musoqStructuralCarrier_0_root(field0, field1, field2, (present0 ? 1UL : 0UL) | (present1 ? 2UL : 0UL) | (true ? 4UL : 0UL));
        }

        private static string __musoqDecodeTypedJson_0_0(System.Text.Json.JsonElement value, StructuralParameterCaptureState state, int depth, string path)
        {
            state.ReserveNode(depth);
            if (value.ValueKind == System.Text.Json.JsonValueKind.Null)
            {
                return default(string);
            }

            var result = value.GetString()!;
            state.ReserveString(result);
            return result;
        }

        private static string __musoqDecodeTypedJson_0_1(System.Text.Json.JsonElement value, StructuralParameterCaptureState state, int depth, string path)
        {
            state.ReserveNode(depth);
            if (value.ValueKind == System.Text.Json.JsonValueKind.Null)
            {
                return default(string);
            }

            var result = value.GetString()!;
            state.ReserveString(result);
            return result;
        }

        private static string __musoqDecodeTypedJson_0_2(System.Text.Json.JsonElement value, StructuralParameterCaptureState state, int depth, string path)
        {
            state.ReserveNode(depth);
            if (value.ValueKind == System.Text.Json.JsonValueKind.Null)
            {
                return default(string);
            }

            var result = value.GetString()!;
            state.ReserveString(result);
            return result;
        }

        private static Musoq.Examples.DataSources.StructuredInputs.PatternInput __musoqStructuralAdapt_0_919131913(__musoqStructuralCarrier_0_root source)
        {
            return new Musoq.Examples.DataSources.StructuredInputs.PatternInput(((source.P0 & 1UL) != 0UL) ? source.F0 : default(string), ((source.P0 & 2UL) != 0UL) ? source.F1 : default(string), ((source.P0 & 4UL) != 0UL) ? source.F2 : "literal");
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                Id = __value0;
                Mode = __value1;
            }

            public override int Count => 2;
            public string Id { get; private set; }
            public string Mode { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Mode = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Mode),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Mode" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Mode, ref visitor);
            }
        }

        [global::System.Text.Json.Serialization.JsonConverter(typeof(__musoqStructuralCarrier_0_rootJsonConverter))]
        private readonly struct __musoqStructuralCarrier_0_root
        {
            public readonly string F0;
            public readonly string F1;
            public readonly string F2;
            public readonly ulong P0;
            public __musoqStructuralCarrier_0_root(string f0, string f1, string f2, ulong p0)
            {
                F0 = f0;
                F1 = f1;
                F2 = f2;
                P0 = p0;
            }
        }

        private sealed class __musoqStructuralCarrier_0_rootJsonConverter : global::System.Text.Json.Serialization.JsonConverter<__musoqStructuralCarrier_0_root>
        {
            public __musoqStructuralCarrier_0_rootJsonConverter()
            {
            }

            public override __musoqStructuralCarrier_0_root Read(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
            {
                if (reader.TokenType != global::System.Text.Json.JsonTokenType.StartObject)
                    throw new global::System.Text.Json.JsonException("A native record requires a JSON object.");
                string field0 = default;
                string field1 = default;
                string field2 = default;
                ulong p0 = 0;
                while (reader.Read())
                {
                    if (reader.TokenType == global::System.Text.Json.JsonTokenType.EndObject)
                    {
                        if ((p0 & 1UL) == 0)
                            throw new global::System.Text.Json.JsonException("A required native field is missing.");
                        if ((p0 & 2UL) == 0)
                            throw new global::System.Text.Json.JsonException("A required native field is missing.");
                        return new __musoqStructuralCarrier_0_root(field0, field1, field2, p0);
                    }

                    if (reader.TokenType != global::System.Text.Json.JsonTokenType.PropertyName)
                        throw new global::System.Text.Json.JsonException("A native record requires named fields.");
                    if (reader.ValueTextEquals("Id"))
                    {
                        if ((p0 & 1UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field0 = global::System.Text.Json.JsonSerializer.Deserialize<string>(ref reader, options)!;
                        p0 |= 1UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("Pattern"))
                    {
                        if ((p0 & 2UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field1 = global::System.Text.Json.JsonSerializer.Deserialize<string>(ref reader, options)!;
                        p0 |= 2UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("Mode"))
                    {
                        if ((p0 & 4UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field2 = global::System.Text.Json.JsonSerializer.Deserialize<string>(ref reader, options)!;
                        p0 |= 4UL;
                        continue;
                    }

                    throw new global::System.Text.Json.JsonException("The native record contains an undeclared field.");
                }

                throw new global::System.Text.Json.JsonException("The native record is incomplete.");
            }

            public override void Write(global::System.Text.Json.Utf8JsonWriter writer, __musoqStructuralCarrier_0_root value, global::System.Text.Json.JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                if ((value.P0 & 1UL) != 0)
                {
                    writer.WritePropertyName("Id");
                    global::System.Text.Json.JsonSerializer.Serialize<string>(writer, value.F0, options);
                }

                if ((value.P0 & 2UL) != 0)
                {
                    writer.WritePropertyName("Pattern");
                    global::System.Text.Json.JsonSerializer.Serialize<string>(writer, value.F1, options);
                }

                if ((value.P0 & 4UL) != 0)
                {
                    writer.WritePropertyName("Mode");
                    global::System.Text.Json.JsonSerializer.Serialize<string>(writer, value.F2, options);
                }

                writer.WriteEndObject();
            }
        }
    }
}
