// === Parsed Query ===
/*
param(offset: int, options: (Enabled: bool, Codes: int[], Window: (Before: int, After: int = 3)) =
      (Enabled: true, Codes: array { 10 }, Window: (Before: 2)));
select Enabled, Before + $offset as Before, After from #inputs.configure(options: $options)
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Enabled as Enabled, (ko3iko.Before + $offset) as Before, ko3iko.After as After]
    SchemaScan [#inputs.configure(<OptionsInput>$options) as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Enabled as Enabled, (ko3iko.Before + $offset) as Before, ko3iko.After as After]
    PhysicalSchemaScan [#inputs.configure(<OptionsInput>$options) as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: ConfigureRow]
      Enabled: bool <- property Enabled
      Before: int <- property Before
      After: int <- property After
    Generated [ResultRow0]
      Enabled: bool <- field Enabled
      Before: int <- field Before
      After: int <- field After

  Body
    PhaseBoundary [Begin]
    Let [__musoqStructural_ko3iko_0: OptionsInput = convert<Musoq.Examples.DataSources.StructuredInputs.OptionsInput>($options)]
    PhaseBoundary [From]
    SourceScan [ko3iko: ConfigureRow] -> ko3ikoRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      AppendShape [result <- ResultRow0(Enabled: ko3iko.Enabled, Before: (ko3iko.Before + $offset), After: ko3iko.After)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K12_TypedStructuredDeclarationDefaults
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
            new Column("Enabled", typeof(bool), 0),
            new Column("Before", typeof(int), 1),
            new Column("After", typeof(int), 2)
        };
        private static readonly string[] __musoqDecodeTypedJson_1_2_fields = new string[]
        {
            "Before",
            "After"
        };
        private static readonly string[] __musoqDecodeTypedJson_1__fields = new string[]
        {
            "Enabled",
            "Codes",
            "window"
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Enabled", typeof(bool), 0), new Column("Before", typeof(int), 1), new Column("After", typeof(int), 2) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = new ScriptParameterContract[]
        {
            new ScriptParameterContract("offset", "int", "int", typeof(int), false, false, null, null, false, ScriptParameterDefaultKind.None, null),
            new ScriptParameterContract("options", "(Enabled: bool, Codes: int[], window: (Before: int, After: int = 3))", "(Enabled: bool, Codes: int[], window: (Before: int, After: int = 3))", typeof(Musoq.Schema.StructuralInputs.StructuralValue), false, false, null, null, true, ScriptParameterDefaultKind.Structured, StructuralValue.FromRecord(new KeyValuePair<string, StructuralValue>[] { new KeyValuePair<string, StructuralValue>("Enabled", StructuralValue.FromScalar(true)), new KeyValuePair<string, StructuralValue>("Codes", StructuralValue.FromCollection(new StructuralValue[] { StructuralValue.FromScalar(10) })), new KeyValuePair<string, StructuralValue>("window", StructuralValue.FromRecord(new KeyValuePair<string, StructuralValue>[] { new KeyValuePair<string, StructuralValue>("Before", StructuralValue.FromScalar(2)), new KeyValuePair<string, StructuralValue>("After", StructuralValue.FromScalar(3)) })) }), StructuralTypeDescriptor.Record(typeof(Musoq.Schema.StructuralInputs.StructuralValue), new StructuralFieldDescriptor[] { new StructuralFieldDescriptor("Enabled", StructuralTypeDescriptor.Scalar(typeof(bool), false), true, null), new StructuralFieldDescriptor("Codes", StructuralTypeDescriptor.Collection(typeof(int[]), StructuralTypeDescriptor.Scalar(typeof(int), false), false), true, null), new StructuralFieldDescriptor("window", StructuralTypeDescriptor.Record(typeof(Musoq.Schema.StructuralInputs.StructuralValue), new StructuralFieldDescriptor[] { new StructuralFieldDescriptor("Before", StructuralTypeDescriptor.Scalar(typeof(int), false), true, null), new StructuralFieldDescriptor("After", StructuralTypeDescriptor.Scalar(typeof(int), false), false, StructuralDefaultDescriptor.Create(3, "3")) }, false), true, null) }, false))
        };
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = new ScriptParameterDefinition[]
        {
            new ScriptParameterDefinition(new ScriptParameterContract("offset", "int", "int", typeof(int), false, false, null, null, false, ScriptParameterDefaultKind.None, null)),
            new ScriptParameterDefinition(new ScriptParameterContract("options", "(Enabled: bool, Codes: int[], window: (Before: int, After: int = 3))", "(Enabled: bool, Codes: int[], window: (Before: int, After: int = 3))", typeof(Musoq.Schema.StructuralInputs.StructuralValue), false, false, null, null, true, ScriptParameterDefaultKind.Structured, StructuralValue.FromRecord(new KeyValuePair<string, StructuralValue>[] { new KeyValuePair<string, StructuralValue>("Enabled", StructuralValue.FromScalar(true)), new KeyValuePair<string, StructuralValue>("Codes", StructuralValue.FromCollection(new StructuralValue[] { StructuralValue.FromScalar(10) })), new KeyValuePair<string, StructuralValue>("window", StructuralValue.FromRecord(new KeyValuePair<string, StructuralValue>[] { new KeyValuePair<string, StructuralValue>("Before", StructuralValue.FromScalar(2)), new KeyValuePair<string, StructuralValue>("After", StructuralValue.FromScalar(3)) })) }), StructuralTypeDescriptor.Record(typeof(Musoq.Schema.StructuralInputs.StructuralValue), new StructuralFieldDescriptor[] { new StructuralFieldDescriptor("Enabled", StructuralTypeDescriptor.Scalar(typeof(bool), false), true, null), new StructuralFieldDescriptor("Codes", StructuralTypeDescriptor.Collection(typeof(int[]), StructuralTypeDescriptor.Scalar(typeof(int), false), false), true, null), new StructuralFieldDescriptor("window", StructuralTypeDescriptor.Record(typeof(Musoq.Schema.StructuralInputs.StructuralValue), new StructuralFieldDescriptor[] { new StructuralFieldDescriptor("Before", StructuralTypeDescriptor.Scalar(typeof(int), false), true, null), new StructuralFieldDescriptor("After", StructuralTypeDescriptor.Scalar(typeof(int), false), false, StructuralDefaultDescriptor.Create(3, "3")) }, false), true, null) }, false)))
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
                var paramOffset = ScriptParameterBinder.GetRequired<int>(__musoqExecutionState, "offset");
                var paramOptions = __musoqBindTypedJson_1(__musoqExecutionState, "options", __musoqTypedParameterCaptureState);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, new string[] { "offset", "options" });
                OnPhaseChanged("compiled", QueryPhase.Begin);
                Musoq.Examples.DataSources.StructuredInputs.OptionsInput __musoqStructural_ko3iko_0 = __musoqStructuralAdapt_1_2323394117(paramOptions);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#inputs");
                var ko3ikoRowsSourceContext = new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress);
                Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.ConfigureRow> ko3ikoRowsSource;
                try
                {
                    Musoq.Examples.DataSources.StructuredInputs.ConfigureSource ko3ikoRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.ConfigureSource(__musoqCheckStructural_c104dc5577b3ebd0(__musoqStructural_ko3iko_0, token), ko3ikoRowsSourceContext);
                    ko3ikoRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.ConfigureSource, Musoq.Examples.DataSources.StructuredInputs.ConfigureRow>(__ko3ikoSchema, "configure", ko3ikoRowsSourceInstance, ko3ikoRowsSourceContext, "#inputs", "ko3iko", "ko3iko:1");
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
                    throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "configure", "ko3iko", "ko3iko:1", exception);
                }

                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.ConfigureRow>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Examples.DataSources.StructuredInputs.ConfigureRow> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Examples.DataSources.StructuredInputs.ConfigureRow[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                yield return new ResultRow0(ko3iko.Enabled, (ko3iko.Before + paramOffset), ko3iko.After);
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Examples.DataSources.StructuredInputs.ConfigureRow> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                yield return new ResultRow0(ko3iko.Enabled, (ko3iko.Before + paramOffset), ko3iko.After);
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
                        yield return new ResultRow0(ko3iko.Enabled, (ko3iko.Before + paramOffset), ko3iko.After);
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

        private static __musoqStructuralCarrier_1_root __musoqBindTypedJson_1(ExecutionState executionState, string name, StructuralParameterCaptureState state)
        {
            try
            {
                if (!executionState.TypedParameters.Contains(name))
                {
                    state.ReserveMetrics(new StructuralInputMetrics(3, 7L, 0L), 1);
                    return new __musoqStructuralCarrier_1_root(true, new int[] { 10 }, new __musoqStructuralCarrier_1_2(2, 3, 3UL), 7UL);
                }

                return __musoqDecodeTypedJson_1_(executionState.TypedParameters.GetRequired<System.Text.Json.JsonElement>(name), state, 1, "$" + name);
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
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.TypeMismatch(name, typeof(__musoqStructuralCarrier_1_root), typeof(System.Text.Json.JsonElement), exception);
            }
        }

        private static Musoq.Examples.DataSources.StructuredInputs.OptionsInput __musoqCheckStructural_c104dc5577b3ebd0(Musoq.Examples.DataSources.StructuredInputs.OptionsInput value, System.Threading.CancellationToken token)
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
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (2 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 2;
            if (value.Codes != null)
            {
                var __musoqStructural_collection1 = value.Codes;
                var __musoqStructural_count2 = __musoqStructural_collection1.Length;
                for (var __musoqStructural_index0 = 0; __musoqStructural_index0 < __musoqStructural_count2; __musoqStructural_index0++)
                {
                    if ((__musoqStructural_index0 & 1023) == 0)
                        token.ThrowIfCancellationRequested();
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (3 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 3;
                }
            }

            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (2 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 2;
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (3 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 3;
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (3 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 3;
            global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("parameter", "$options", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "ko3iko:1");
            return value;
        }

        private static __musoqStructuralCarrier_1_root __musoqDecodeTypedJson_1_(System.Text.Json.JsonElement value, StructuralParameterCaptureState state, int depth, string path)
        {
            TypedParameterJsonReader.RequireRecord(value, state, depth, path);
            TypedParameterJsonReader.ValidateFields(value, __musoqDecodeTypedJson_1__fields, path);
            var(present0, input0) = TypedParameterJsonReader.FindField(value, "Enabled");
            if (!present0)
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.MissingRequired(path + ".Enabled");
            bool field0 = present0 ? __musoqDecodeTypedJson_1_0(input0, state, depth + 1, path + ".Enabled") : default(bool);
            var(present1, input1) = TypedParameterJsonReader.FindField(value, "Codes");
            if (!present1)
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.MissingRequired(path + ".Codes");
            int[] field1 = present1 ? __musoqDecodeTypedJson_1_1(input1, state, depth + 1, path + ".Codes") : default(int[]);
            var(present2, input2) = TypedParameterJsonReader.FindField(value, "window");
            if (!present2)
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.MissingRequired(path + ".window");
            __musoqStructuralCarrier_1_2 field2 = present2 ? __musoqDecodeTypedJson_1_2(input2, state, depth + 1, path + ".window") : default(__musoqStructuralCarrier_1_2);
            return new __musoqStructuralCarrier_1_root(field0, field1, field2, (present0 ? 1UL : 0UL) | (present1 ? 2UL : 0UL) | (present2 ? 4UL : 0UL));
        }

        private static bool __musoqDecodeTypedJson_1_0(System.Text.Json.JsonElement value, StructuralParameterCaptureState state, int depth, string path)
        {
            state.ReserveNode(depth);
            if (value.ValueKind == System.Text.Json.JsonValueKind.Null)
            {
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.NullNotAllowed(path, typeof(bool));
            }

            var result = value.GetBoolean();
            return result;
        }

        private static int[] __musoqDecodeTypedJson_1_1(System.Text.Json.JsonElement value, StructuralParameterCaptureState state, int depth, string path)
        {
            state.ReserveNode(depth);
            if (value.ValueKind != System.Text.Json.JsonValueKind.Array)
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.TypeMismatch(path, typeof(int[]), typeof(System.Text.Json.JsonElement));
            var count = value.GetArrayLength();
            state.EnsureCollectionLowerBound(count);
            var result = new int[count];
            for (var index = 0; index < count; index++)
                result[index] = __musoqDecodeTypedJson_1_1_element(value[index], state, depth + 1, path + "[" + index + "]");
            return result;
        }

        private static int __musoqDecodeTypedJson_1_1_element(System.Text.Json.JsonElement value, StructuralParameterCaptureState state, int depth, string path)
        {
            state.ReserveNode(depth);
            if (value.ValueKind == System.Text.Json.JsonValueKind.Null)
            {
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.NullNotAllowed(path, typeof(int));
            }

            var result = value.GetInt32();
            return result;
        }

        private static __musoqStructuralCarrier_1_2 __musoqDecodeTypedJson_1_2(System.Text.Json.JsonElement value, StructuralParameterCaptureState state, int depth, string path)
        {
            TypedParameterJsonReader.RequireRecord(value, state, depth, path);
            TypedParameterJsonReader.ValidateFields(value, __musoqDecodeTypedJson_1_2_fields, path);
            var(present0, input0) = TypedParameterJsonReader.FindField(value, "Before");
            if (!present0)
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.MissingRequired(path + ".Before");
            int field0 = present0 ? __musoqDecodeTypedJson_1_2_0(input0, state, depth + 1, path + ".Before") : default(int);
            var(present1, input1) = TypedParameterJsonReader.FindField(value, "After");
            if (!present1)
                state.ReserveMetrics(new StructuralInputMetrics(1, 1L, 0L), depth + 1);
            int field1 = present1 ? __musoqDecodeTypedJson_1_2_1(input1, state, depth + 1, path + ".After") : 3;
            return new __musoqStructuralCarrier_1_2(field0, field1, (present0 ? 1UL : 0UL) | (true ? 2UL : 0UL));
        }

        private static int __musoqDecodeTypedJson_1_2_0(System.Text.Json.JsonElement value, StructuralParameterCaptureState state, int depth, string path)
        {
            state.ReserveNode(depth);
            if (value.ValueKind == System.Text.Json.JsonValueKind.Null)
            {
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.NullNotAllowed(path, typeof(int));
            }

            var result = value.GetInt32();
            return result;
        }

        private static int __musoqDecodeTypedJson_1_2_1(System.Text.Json.JsonElement value, StructuralParameterCaptureState state, int depth, string path)
        {
            state.ReserveNode(depth);
            if (value.ValueKind == System.Text.Json.JsonValueKind.Null)
            {
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.NullNotAllowed(path, typeof(int));
            }

            var result = value.GetInt32();
            return result;
        }

        private static Musoq.Examples.DataSources.StructuredInputs.OptionsInput __musoqStructuralAdapt_1_2323394117(__musoqStructuralCarrier_1_root source)
        {
            return new Musoq.Examples.DataSources.StructuredInputs.OptionsInput(((source.P0 & 1UL) != 0UL) ? source.F0 : default(bool), ((source.P0 & 2UL) != 0UL) ? source.F1 : default(int[]), (((source.P0 & 4UL) != 0UL) ? new Musoq.Examples.DataSources.StructuredInputs.WindowInput(((source.F2.P0 & 1UL) != 0UL) ? source.F2.F0 : default(int), ((source.F2.P0 & 2UL) != 0UL) ? source.F2.F1 : default(int)) : default(Musoq.Examples.DataSources.StructuredInputs.WindowInput)));
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(bool __value0, int __value1, int __value2)
            {
                Enabled = __value0;
                Before = __value1;
                After = __value2;
            }

            public int After { get; private set; }
            public int Before { get; private set; }
            public override int Count => 3;
            public bool Enabled { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.Enabled = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<bool, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Before = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.After = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool>(this.Enabled),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Before),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.After),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Enabled" => true,
                "Before" => true,
                "After" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Enabled, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Before, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.After, ref visitor);
            }
        }

        [global::System.Text.Json.Serialization.JsonConverter(typeof(__musoqStructuralCarrier_1_2JsonConverter))]
        private readonly struct __musoqStructuralCarrier_1_2
        {
            public readonly int F0;
            public readonly int F1;
            public readonly ulong P0;
            public __musoqStructuralCarrier_1_2(int f0, int f1, ulong p0)
            {
                F0 = f0;
                F1 = f1;
                P0 = p0;
            }
        }

        private sealed class __musoqStructuralCarrier_1_2JsonConverter : global::System.Text.Json.Serialization.JsonConverter<__musoqStructuralCarrier_1_2>
        {
            public __musoqStructuralCarrier_1_2JsonConverter()
            {
            }

            public override __musoqStructuralCarrier_1_2 Read(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
            {
                if (reader.TokenType != global::System.Text.Json.JsonTokenType.StartObject)
                    throw new global::System.Text.Json.JsonException("A native record requires a JSON object.");
                int field0 = default;
                int field1 = default;
                ulong p0 = 0;
                while (reader.Read())
                {
                    if (reader.TokenType == global::System.Text.Json.JsonTokenType.EndObject)
                    {
                        if ((p0 & 1UL) == 0)
                            throw new global::System.Text.Json.JsonException("A required native field is missing.");
                        return new __musoqStructuralCarrier_1_2(field0, field1, p0);
                    }

                    if (reader.TokenType != global::System.Text.Json.JsonTokenType.PropertyName)
                        throw new global::System.Text.Json.JsonException("A native record requires named fields.");
                    if (reader.ValueTextEquals("Before"))
                    {
                        if ((p0 & 1UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field0 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 1UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("After"))
                    {
                        if ((p0 & 2UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field1 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 2UL;
                        continue;
                    }

                    throw new global::System.Text.Json.JsonException("The native record contains an undeclared field.");
                }

                throw new global::System.Text.Json.JsonException("The native record is incomplete.");
            }

            public override void Write(global::System.Text.Json.Utf8JsonWriter writer, __musoqStructuralCarrier_1_2 value, global::System.Text.Json.JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                if ((value.P0 & 1UL) != 0)
                {
                    writer.WritePropertyName("Before");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F0, options);
                }

                if ((value.P0 & 2UL) != 0)
                {
                    writer.WritePropertyName("After");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F1, options);
                }

                writer.WriteEndObject();
            }
        }

        [global::System.Text.Json.Serialization.JsonConverter(typeof(__musoqStructuralCarrier_1_rootJsonConverter))]
        private readonly struct __musoqStructuralCarrier_1_root
        {
            public readonly bool F0;
            public readonly int[] F1;
            public readonly __musoqStructuralCarrier_1_2 F2;
            public readonly ulong P0;
            public __musoqStructuralCarrier_1_root(bool f0, int[] f1, __musoqStructuralCarrier_1_2 f2, ulong p0)
            {
                F0 = f0;
                F1 = f1;
                F2 = f2;
                P0 = p0;
            }
        }

        private sealed class __musoqStructuralCarrier_1_rootJsonConverter : global::System.Text.Json.Serialization.JsonConverter<__musoqStructuralCarrier_1_root>
        {
            public __musoqStructuralCarrier_1_rootJsonConverter()
            {
            }

            public override __musoqStructuralCarrier_1_root Read(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
            {
                if (reader.TokenType != global::System.Text.Json.JsonTokenType.StartObject)
                    throw new global::System.Text.Json.JsonException("A native record requires a JSON object.");
                bool field0 = default;
                int[] field1 = default;
                __musoqStructuralCarrier_1_2 field2 = default;
                ulong p0 = 0;
                while (reader.Read())
                {
                    if (reader.TokenType == global::System.Text.Json.JsonTokenType.EndObject)
                    {
                        if ((p0 & 1UL) == 0)
                            throw new global::System.Text.Json.JsonException("A required native field is missing.");
                        if ((p0 & 2UL) == 0)
                            throw new global::System.Text.Json.JsonException("A required native field is missing.");
                        if ((p0 & 4UL) == 0)
                            throw new global::System.Text.Json.JsonException("A required native field is missing.");
                        return new __musoqStructuralCarrier_1_root(field0, field1, field2, p0);
                    }

                    if (reader.TokenType != global::System.Text.Json.JsonTokenType.PropertyName)
                        throw new global::System.Text.Json.JsonException("A native record requires named fields.");
                    if (reader.ValueTextEquals("Enabled"))
                    {
                        if ((p0 & 1UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field0 = global::System.Text.Json.JsonSerializer.Deserialize<bool>(ref reader, options)!;
                        p0 |= 1UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("Codes"))
                    {
                        if ((p0 & 2UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field1 = global::System.Text.Json.JsonSerializer.Deserialize<int[]>(ref reader, options)!;
                        p0 |= 2UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("window"))
                    {
                        if ((p0 & 4UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field2 = global::System.Text.Json.JsonSerializer.Deserialize<__musoqStructuralCarrier_1_2>(ref reader, options)!;
                        p0 |= 4UL;
                        continue;
                    }

                    throw new global::System.Text.Json.JsonException("The native record contains an undeclared field.");
                }

                throw new global::System.Text.Json.JsonException("The native record is incomplete.");
            }

            public override void Write(global::System.Text.Json.Utf8JsonWriter writer, __musoqStructuralCarrier_1_root value, global::System.Text.Json.JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                if ((value.P0 & 1UL) != 0)
                {
                    writer.WritePropertyName("Enabled");
                    global::System.Text.Json.JsonSerializer.Serialize<bool>(writer, value.F0, options);
                }

                if ((value.P0 & 2UL) != 0)
                {
                    writer.WritePropertyName("Codes");
                    global::System.Text.Json.JsonSerializer.Serialize<int[]>(writer, value.F1, options);
                }

                if ((value.P0 & 4UL) != 0)
                {
                    writer.WritePropertyName("window");
                    global::System.Text.Json.JsonSerializer.Serialize<__musoqStructuralCarrier_1_2>(writer, value.F2, options);
                }

                writer.WriteEndObject();
            }
        }
    }
}
