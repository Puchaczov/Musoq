// === Parsed Query ===
/*
param(
    input: (
        Id: string,
        Pattern: string,
        Mode: string = 'regex'
    )[] = array { (Id: 'todo', Pattern: 'TODO') }
)

select m.PatternId, m.MatchText
from #inputs.match('TODO', patterns: $input) m
*/

// === Logical Plan ===
/*
MultiStatement
  Project [m.PatternId as PatternId, m.MatchText as MatchText]
    SchemaScan [#inputs.match('TODO', <IReadOnlyList`1>$input) as m]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [m.PatternId as PatternId, m.MatchText as MatchText]
    PhysicalSchemaScan [#inputs.match('TODO', <IReadOnlyList`1>$input) as m]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [m: PatternMatchRow]
      PatternId: string <- property PatternId
      MatchText: string <- property MatchText
    Generated [ResultRow0]
      PatternId: string <- field PatternId
      MatchText: string <- field MatchText

  Body
    PhaseBoundary [Begin]
    Let [__musoqStructural_m_0: IReadOnlyList<PatternInput> = convert<sequence<Musoq.Examples.DataSources.StructuredInputs.PatternInput>>($input)]
    PhaseBoundary [From]
    SourceScan [m: PatternMatchRow] -> mRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [m in mRows]
      AppendShape [result <- ResultRow0(PatternId: m.PatternId, MatchText: m.MatchText)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q360_StructuredTypedParameterSnapshot
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
            new Column("PatternId", typeof(string), 0),
            new Column("MatchText", typeof(string), 1)
        };
        private static readonly string[] __musoqDecodeTypedJson_0_element_fields = new string[]
        {
            "Id",
            "Pattern",
            "Mode"
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_m_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("PatternId", typeof(string), 0), new Column("MatchText", typeof(string), 1) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = new ScriptParameterContract[]
        {
            new ScriptParameterContract("input", "(Id: string, Pattern: string, Mode: string = 'regex')[]", "(Id: string, Pattern: string, Mode: string = 'regex')[]", typeof(Musoq.Schema.StructuralInputs.StructuralValue[]), false, true, typeof(Musoq.Schema.StructuralInputs.StructuralValue), "(Id: string, Pattern: string, Mode: string = 'regex')", true, ScriptParameterDefaultKind.Structured, StructuralValue.FromCollection(new StructuralValue[] { StructuralValue.FromRecord(new KeyValuePair<string, StructuralValue>[] { new KeyValuePair<string, StructuralValue>("Id", StructuralValue.FromScalar("todo")), new KeyValuePair<string, StructuralValue>("Pattern", StructuralValue.FromScalar("TODO")), new KeyValuePair<string, StructuralValue>("Mode", StructuralValue.FromScalar("regex")) }) }), StructuralTypeDescriptor.Collection(typeof(Musoq.Schema.StructuralInputs.StructuralValue[]), StructuralTypeDescriptor.Record(typeof(Musoq.Schema.StructuralInputs.StructuralValue), new StructuralFieldDescriptor[] { new StructuralFieldDescriptor("Id", StructuralTypeDescriptor.Scalar(typeof(string), true), true, null), new StructuralFieldDescriptor("Pattern", StructuralTypeDescriptor.Scalar(typeof(string), true), true, null), new StructuralFieldDescriptor("Mode", StructuralTypeDescriptor.Scalar(typeof(string), true), false, StructuralDefaultDescriptor.Create("regex", "'regex'")) }, false), false))
        };
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = new ScriptParameterDefinition[]
        {
            new ScriptParameterDefinition(new ScriptParameterContract("input", "(Id: string, Pattern: string, Mode: string = 'regex')[]", "(Id: string, Pattern: string, Mode: string = 'regex')[]", typeof(Musoq.Schema.StructuralInputs.StructuralValue[]), false, true, typeof(Musoq.Schema.StructuralInputs.StructuralValue), "(Id: string, Pattern: string, Mode: string = 'regex')", true, ScriptParameterDefaultKind.Structured, StructuralValue.FromCollection(new StructuralValue[] { StructuralValue.FromRecord(new KeyValuePair<string, StructuralValue>[] { new KeyValuePair<string, StructuralValue>("Id", StructuralValue.FromScalar("todo")), new KeyValuePair<string, StructuralValue>("Pattern", StructuralValue.FromScalar("TODO")), new KeyValuePair<string, StructuralValue>("Mode", StructuralValue.FromScalar("regex")) }) }), StructuralTypeDescriptor.Collection(typeof(Musoq.Schema.StructuralInputs.StructuralValue[]), StructuralTypeDescriptor.Record(typeof(Musoq.Schema.StructuralInputs.StructuralValue), new StructuralFieldDescriptor[] { new StructuralFieldDescriptor("Id", StructuralTypeDescriptor.Scalar(typeof(string), true), true, null), new StructuralFieldDescriptor("Pattern", StructuralTypeDescriptor.Scalar(typeof(string), true), true, null), new StructuralFieldDescriptor("Mode", StructuralTypeDescriptor.Scalar(typeof(string), true), false, StructuralDefaultDescriptor.Create("regex", "'regex'")) }, false), false)))
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
                IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.PatternInput> __musoqStructural_m_0 = __musoqStructuralAdapt_0_691781520(paramInput);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __mSchema = provider.GetSchema("#inputs");
                var mRowsSourceContext = new SourceExecutionContext("m:1", sourceExecutionPlans["m:1"], token, __schemaColumns_compiled_m_0, sourceRuntimeSettingsBySourceContextId["m:1"], logger, OnDataSourceProgress);
                Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow> mRowsSource;
                try
                {
                    Musoq.Examples.DataSources.StructuredInputs.MatchSource mRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.MatchSource(__musoqCheckStructural_d97fc6504a38a2bf("TODO", token), __musoqCheckStructural_ecdf7e4fe85289c5(__musoqStructural_m_0, token), mRowsSourceContext);
                    mRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.MatchSource, Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow>(__mSchema, "match", mRowsSourceInstance, mRowsSourceContext, "#inputs", "m", "m:1");
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
                    throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "match", "m", "m:1", exception);
                }

                var mRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow>(mRowsSource.Chunks, __musoqProgressContext, "m:1") : mRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var mChunk in mRows)
                {
                    if (mChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow> mChunkView)
                    {
                        if (mChunkView.Source is Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow[] mChunkViewArray)
                        {
                            int mChunkViewOffset = mChunkView.Offset;
                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                            {
                                if ((mIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var m = mChunkViewArray[mChunkViewOffset + mIndex];
                                yield return new ResultRow0(m.PatternId, m.MatchText);
                            }

                            continue;
                        }

                        if (mChunkView.Source is List<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow> mChunkViewList)
                        {
                            int mChunkViewOffset = mChunkView.Offset;
                            for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                            {
                                if ((mIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var m = mChunkViewList[mChunkViewOffset + mIndex];
                                yield return new ResultRow0(m.PatternId, m.MatchText);
                            }

                            continue;
                        }
                    }

                    for (int mIndex = 0, mIndexCount = mChunk.Count; mIndex < mIndexCount; ++mIndex)
                    {
                        if ((mIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var m = mChunk[mIndex];
                        yield return new ResultRow0(m.PatternId, m.MatchText);
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

        private static __musoqStructuralCarrier_0_a[] __musoqBindTypedJson_0(ExecutionState executionState, string name, StructuralParameterCaptureState state)
        {
            try
            {
                if (!executionState.TypedParameters.Contains(name))
                {
                    state.ReserveMetrics(new StructuralInputMetrics(3, 5L, 26L), 1);
                    return new __musoqStructuralCarrier_0_a[]
                    {
                        new __musoqStructuralCarrier_0_a("todo", "TODO", "regex", 7UL)
                    };
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
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.TypeMismatch(name, typeof(__musoqStructuralCarrier_0_a[]), typeof(System.Text.Json.JsonElement), exception);
            }
        }

        private static string __musoqCheckStructural_d97fc6504a38a2bf(string value, System.Threading.CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            long __musoqStructuralNodes = 0L;
            long __musoqStructuralStrings = 0L;
            int __musoqStructuralMaxDepth = 0;
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (1 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 1;
            if (value != null)
                __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)value.Length * 2L));
            global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("inline", "m.argument[0]", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "m:1");
            return value;
        }

        private static IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.PatternInput> __musoqCheckStructural_ecdf7e4fe85289c5(IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.PatternInput> value, System.Threading.CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            long __musoqStructuralNodes = 0L;
            long __musoqStructuralStrings = 0L;
            int __musoqStructuralMaxDepth = 0;
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (1 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 1;
            if (value != null)
            {
                var __musoqStructural_collection1 = (System.Collections.Generic.IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.PatternInput>)value;
                var __musoqStructural_count2 = __musoqStructural_collection1.Count;
                for (var __musoqStructural_index0 = 0; __musoqStructural_index0 < __musoqStructural_count2; __musoqStructural_index0++)
                {
                    if ((__musoqStructural_index0 & 1023) == 0)
                        token.ThrowIfCancellationRequested();
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (2 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 2;
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (3 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 3;
                    if (__musoqStructural_collection1[__musoqStructural_index0].Id != null)
                        __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)__musoqStructural_collection1[__musoqStructural_index0].Id.Length * 2L));
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (3 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 3;
                    if (__musoqStructural_collection1[__musoqStructural_index0].Pattern != null)
                        __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)__musoqStructural_collection1[__musoqStructural_index0].Pattern.Length * 2L));
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (3 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 3;
                    if (__musoqStructural_collection1[__musoqStructural_index0].Mode != null)
                        __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long)__musoqStructural_collection1[__musoqStructural_index0].Mode.Length * 2L));
                }
            }

            global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("parameter", "$input", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "m:1");
            return value;
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
            var(present0, input0) = TypedParameterJsonReader.FindField(value, "Id");
            if (!present0)
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.MissingRequired(path + ".Id");
            string field0 = present0 ? __musoqDecodeTypedJson_0_element_0(input0, state, depth + 1, path + ".Id") : default(string);
            var(present1, input1) = TypedParameterJsonReader.FindField(value, "Pattern");
            if (!present1)
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.MissingRequired(path + ".Pattern");
            string field1 = present1 ? __musoqDecodeTypedJson_0_element_1(input1, state, depth + 1, path + ".Pattern") : default(string);
            var(present2, input2) = TypedParameterJsonReader.FindField(value, "Mode");
            if (!present2)
                state.ReserveMetrics(new StructuralInputMetrics(1, 1L, 10L), depth + 1);
            string field2 = present2 ? __musoqDecodeTypedJson_0_element_2(input2, state, depth + 1, path + ".Mode") : "regex";
            return new __musoqStructuralCarrier_0_a(field0, field1, field2, (present0 ? 1UL : 0UL) | (present1 ? 2UL : 0UL) | (true ? 4UL : 0UL));
        }

        private static string __musoqDecodeTypedJson_0_element_0(System.Text.Json.JsonElement value, StructuralParameterCaptureState state, int depth, string path)
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

        private static string __musoqDecodeTypedJson_0_element_1(System.Text.Json.JsonElement value, StructuralParameterCaptureState state, int depth, string path)
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

        private static string __musoqDecodeTypedJson_0_element_2(System.Text.Json.JsonElement value, StructuralParameterCaptureState state, int depth, string path)
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

        private static IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.PatternInput> __musoqStructuralAdapt_0_691781520(__musoqStructuralCarrier_0_a[] source)
        {
            var result = new Musoq.Examples.DataSources.StructuredInputs.PatternInput[source.Length];
            for (var index = 0; index < source.Length; index++)
            {
                result[index] = new Musoq.Examples.DataSources.StructuredInputs.PatternInput(((source[index].P0 & 1UL) != 0UL) ? source[index].F0 : default(string), ((source[index].P0 & 2UL) != 0UL) ? source[index].F1 : default(string), ((source[index].P0 & 4UL) != 0UL) ? source[index].F2 : "literal");
            }

            return result;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                PatternId = __value0;
                MatchText = __value1;
            }

            public override int Count => 2;
            public string MatchText { get; private set; }
            public string PatternId { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.PatternId = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.MatchText = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.PatternId),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.MatchText),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "PatternId" => true,
                "MatchText" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.PatternId, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.MatchText, ref visitor);
            }
        }

        [global::System.Text.Json.Serialization.JsonConverter(typeof(__musoqStructuralCarrier_0_aJsonConverter))]
        private readonly struct __musoqStructuralCarrier_0_a
        {
            public readonly string F0;
            public readonly string F1;
            public readonly string F2;
            public readonly ulong P0;
            public __musoqStructuralCarrier_0_a(string f0, string f1, string f2, ulong p0)
            {
                F0 = f0;
                F1 = f1;
                F2 = f2;
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
                        return new __musoqStructuralCarrier_0_a(field0, field1, field2, p0);
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

            public override void Write(global::System.Text.Json.Utf8JsonWriter writer, __musoqStructuralCarrier_0_a value, global::System.Text.Json.JsonSerializerOptions options)
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
