// === Parsed Query ===
/*
let todo = (Id: 'todo', Pattern: 'TODO');
let fixme = (Id: 'fixme', Pattern: 'FIXME');
let patterns = array { $todo, (Id: 'issue', Pattern: 'ISSUE-[0-9]+', Mode: 'regex') };

select m.PatternId
from #inputs.match('TODO FIXME', patterns: $patterns) m
*/

// === Logical Plan ===
/*
MultiStatement
  Project [m.PatternId as PatternId]
    SchemaScan [#inputs.match('TODO FIXME', <IReadOnlyList`1>$patterns) as m]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [m.PatternId as PatternId]
    PhysicalSchemaScan [#inputs.match('TODO FIXME', <IReadOnlyList`1>$patterns) as m]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [m: PatternMatchRow]
      PatternId: string <- property PatternId
    Generated [ResultRow0]
      PatternId: string <- field PatternId

  Body
    PhaseBoundary [Begin]
    Let [__musoqStructural_m_0: IReadOnlyList<PatternInput> = convert<sequence<Musoq.Examples.DataSources.StructuredInputs.PatternInput>>($patterns)]
    PhaseBoundary [From]
    SourceScan [m: PatternMatchRow] -> mRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [m in mRows]
      AppendShape [result <- ResultRow0(PatternId: m.PatternId)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q335_StructuredInferredLetPresence
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("PatternId", typeof(string), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_m_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("PatternId", typeof(string), 0) });
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
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                __musoqStructuralCarrier_0_root letTodo = new __musoqStructuralCarrier_0_root("todo", "TODO", 3UL);
                __musoqStructuralCarrier_1_root letFixme = new __musoqStructuralCarrier_1_root("fixme", "FIXME", 3UL);
                __musoqStructuralCarrier_2_a[] letPatterns = new __musoqStructuralCarrier_2_a[]
                {
                    new __musoqStructuralCarrier_2_a("todo", default(string), "TODO", 5UL),
                    new __musoqStructuralCarrier_2_a("issue", "regex", "ISSUE-[0-9]+", 7UL)
                };
                OnPhaseChanged("compiled", QueryPhase.Begin);
                IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.PatternInput> __musoqStructural_m_0 = __musoqStructuralAdapt_2_691781520(letPatterns);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __mSchema = provider.GetSchema("#inputs");
                var mRowsSourceContext = new SourceExecutionContext("m:1", sourceExecutionPlans["m:1"], token, __schemaColumns_compiled_m_0, sourceRuntimeSettingsBySourceContextId["m:1"], logger, OnDataSourceProgress);
                Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow> mRowsSource;
                try
                {
                    Musoq.Examples.DataSources.StructuredInputs.MatchSource mRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.MatchSource(__musoqCheckStructural_d97fc6504a38a2bf("TODO FIXME", token), __musoqCheckStructural_9a1ccaa9c485de38(__musoqStructural_m_0, token), mRowsSourceContext);
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
                                yield return new ResultRow0(m.PatternId);
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
                                yield return new ResultRow0(m.PatternId);
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
                        yield return new ResultRow0(m.PatternId);
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

        private static IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.PatternInput> __musoqCheckStructural_9a1ccaa9c485de38(IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.PatternInput> value, System.Threading.CancellationToken token)
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

            global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("let", "$patterns", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "m:1");
            return value;
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

        private static IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.PatternInput> __musoqStructuralAdapt_2_691781520(__musoqStructuralCarrier_2_a[] source)
        {
            var result = new Musoq.Examples.DataSources.StructuredInputs.PatternInput[source.Length];
            for (var index = 0; index < source.Length; index++)
            {
                result[index] = new Musoq.Examples.DataSources.StructuredInputs.PatternInput(((source[index].P0 & 1UL) != 0UL) ? source[index].F0 : default(string), ((source[index].P0 & 4UL) != 0UL) ? source[index].F2 : default(string), ((source[index].P0 & 2UL) != 0UL) ? source[index].F1 : "literal");
            }

            return result;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0)
            {
                PatternId = __value0;
            }

            public override int Count => 1;
            public string PatternId { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.PatternId = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.PatternId),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "PatternId" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.PatternId, ref visitor);
            }
        }

        [global::System.Text.Json.Serialization.JsonConverter(typeof(__musoqStructuralCarrier_0_rootJsonConverter))]
        private readonly struct __musoqStructuralCarrier_0_root
        {
            public readonly string F0;
            public readonly string F1;
            public readonly ulong P0;
            public __musoqStructuralCarrier_0_root(string f0, string f1, ulong p0)
            {
                F0 = f0;
                F1 = f1;
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
                ulong p0 = 0;
                while (reader.Read())
                {
                    if (reader.TokenType == global::System.Text.Json.JsonTokenType.EndObject)
                    {
                        if ((p0 & 1UL) == 0)
                            throw new global::System.Text.Json.JsonException("A required native field is missing.");
                        if ((p0 & 2UL) == 0)
                            throw new global::System.Text.Json.JsonException("A required native field is missing.");
                        return new __musoqStructuralCarrier_0_root(field0, field1, p0);
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

                writer.WriteEndObject();
            }
        }

        [global::System.Text.Json.Serialization.JsonConverter(typeof(__musoqStructuralCarrier_1_rootJsonConverter))]
        private readonly struct __musoqStructuralCarrier_1_root
        {
            public readonly string F0;
            public readonly string F1;
            public readonly ulong P0;
            public __musoqStructuralCarrier_1_root(string f0, string f1, ulong p0)
            {
                F0 = f0;
                F1 = f1;
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
                string field0 = default;
                string field1 = default;
                ulong p0 = 0;
                while (reader.Read())
                {
                    if (reader.TokenType == global::System.Text.Json.JsonTokenType.EndObject)
                    {
                        if ((p0 & 1UL) == 0)
                            throw new global::System.Text.Json.JsonException("A required native field is missing.");
                        if ((p0 & 2UL) == 0)
                            throw new global::System.Text.Json.JsonException("A required native field is missing.");
                        return new __musoqStructuralCarrier_1_root(field0, field1, p0);
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

                    throw new global::System.Text.Json.JsonException("The native record contains an undeclared field.");
                }

                throw new global::System.Text.Json.JsonException("The native record is incomplete.");
            }

            public override void Write(global::System.Text.Json.Utf8JsonWriter writer, __musoqStructuralCarrier_1_root value, global::System.Text.Json.JsonSerializerOptions options)
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

                writer.WriteEndObject();
            }
        }

        [global::System.Text.Json.Serialization.JsonConverter(typeof(__musoqStructuralCarrier_2_aJsonConverter))]
        private readonly struct __musoqStructuralCarrier_2_a
        {
            public readonly string F0;
            public readonly string F1;
            public readonly string F2;
            public readonly ulong P0;
            public __musoqStructuralCarrier_2_a(string f0, string f1, string f2, ulong p0)
            {
                F0 = f0;
                F1 = f1;
                F2 = f2;
                P0 = p0;
            }
        }

        private sealed class __musoqStructuralCarrier_2_aJsonConverter : global::System.Text.Json.Serialization.JsonConverter<__musoqStructuralCarrier_2_a>
        {
            public __musoqStructuralCarrier_2_aJsonConverter()
            {
            }

            public override __musoqStructuralCarrier_2_a Read(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
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
                        if ((p0 & 4UL) == 0)
                            throw new global::System.Text.Json.JsonException("A required native field is missing.");
                        return new __musoqStructuralCarrier_2_a(field0, field1, field2, p0);
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

                    if (reader.ValueTextEquals("Mode"))
                    {
                        if ((p0 & 2UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field1 = global::System.Text.Json.JsonSerializer.Deserialize<string>(ref reader, options)!;
                        p0 |= 2UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("Pattern"))
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

            public override void Write(global::System.Text.Json.Utf8JsonWriter writer, __musoqStructuralCarrier_2_a value, global::System.Text.Json.JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                if ((value.P0 & 1UL) != 0)
                {
                    writer.WritePropertyName("Id");
                    global::System.Text.Json.JsonSerializer.Serialize<string>(writer, value.F0, options);
                }

                if ((value.P0 & 2UL) != 0)
                {
                    writer.WritePropertyName("Mode");
                    global::System.Text.Json.JsonSerializer.Serialize<string>(writer, value.F1, options);
                }

                if ((value.P0 & 4UL) != 0)
                {
                    writer.WritePropertyName("Pattern");
                    global::System.Text.Json.JsonSerializer.Serialize<string>(writer, value.F2, options);
                }

                writer.WriteEndObject();
            }
        }
    }
}
