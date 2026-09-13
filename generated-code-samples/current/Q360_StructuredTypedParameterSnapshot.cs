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
  Project [m.PatternId as m.PatternId, m.MatchText as m.MatchText]
    SchemaScan [#inputs.match('TODO', <IReadOnlyList`1>$input) as m]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [m.PatternId as m.PatternId, m.MatchText as m.MatchText]
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
      m.PatternId: string <- field m_PatternId
      m.MatchText: string <- field m_MatchText

  Body
    PhaseBoundary [Begin]
    Let [__musoqStructural_m_0: IReadOnlyList<PatternInput> = convert<sequence<Musoq.Examples.DataSources.StructuredInputs.PatternInput>>($input)]
    PhaseBoundary [From]
    SourceScan [m: PatternMatchRow] -> mRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [m in mRows]
      AppendShape [result <- ResultShape0(m.PatternId: m.PatternId, m.MatchText: m.MatchText)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, IStructuralParameterSnapshotProvider
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("m.PatternId", typeof(string), 0),
            new Column("m.MatchText", typeof(string), 1)
        };
        private static readonly string[] __musoqDeclaredParameterNames = new string[]
        {
            "input"
        };
        private static readonly string[] __musoqStructuralParameterFields_0_a = new[]
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
        public IDictionary<string, System.Object> Parameters { get; } = new Dictionary<string, System.Object>(StringComparer.Ordinal);
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }

        public event DataSourceEventHandler DataSourceProgress;
        public event QueryPhaseEventHandler PhaseChanged;
        public event QueryProgressEventHandler QueryProgress;
        public IReadOnlyDictionary<string, object?> CaptureParameterSnapshot(IReadOnlyDictionary<string, object?> supplied, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(supplied);
            cancellationToken.ThrowIfCancellationRequested();
            var __musoqCaptureState = new StructuralParameterCaptureState(cancellationToken);
            var __musoqSnapshot = new Dictionary<string, object?>(StringComparer.Ordinal);
            if (supplied.TryGetValue("input", out var __musoqRawParameter_0))
            {
                try
                {
                    __musoqSnapshot["input"] = __musoqCaptureStructuralParameter_0_root(__musoqRawParameter_0, __musoqCaptureState, 1, "$input");
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
                    throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.TypeMismatch("input", typeof(Musoq.Schema.StructuralInputs.StructuralValue[]), __musoqRawParameter_0, exception);
                }
            }
            else
            {
                __musoqCaptureState.ReserveMetrics(new StructuralInputMetrics(3, 5L, 26L), 1);
                __musoqSnapshot["input"] = new __musoqStructuralCarrier_0_a[]
                {
                    new __musoqStructuralCarrier_0_a("todo", "TODO", "regex", 7UL)
                };
            }

            ScriptParameterBinder.ValidateNoUnknownParameters(supplied, __musoqDeclaredParameterNames);
            return new System.Collections.ObjectModel.ReadOnlyDictionary<string, object?>(__musoqSnapshot);
        }

        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.m_PatternId, __musoqShapeRow.m_MatchText);
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
                var paramInput = ScriptParameterBinder.GetOptional<__musoqStructuralCarrier_0_a[]>(__musoqExecutionState.Parameters, "input", default(__musoqStructuralCarrier_0_a[]));
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, new string[] { "input" });
                OnPhaseChanged("compiled", QueryPhase.Begin);
                IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.PatternInput> __musoqStructural_m_0 = __musoqStructuralAdapt_0_691781520(paramInput);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __mSchema = provider.GetSchema("#inputs");
                var mRowsSourceContext = new SourceExecutionContext("m:1", sourceExecutionPlans["m:1"], token, __schemaColumns_compiled_m_0, sourceRuntimeSettingsBySourceContextId["m:1"], logger, OnDataSourceProgress);
                Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.PatternMatchRow> mRowsSource;
                try
                {
                    Musoq.Examples.DataSources.StructuredInputs.MatchSource mRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.MatchSource("TODO", __musoqCheckStructural_ecdf7e4fe85289c5(__musoqStructural_m_0, token), mRowsSourceContext);
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
                                yield return new ResultShape0(m.PatternId, m.MatchText);
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
                                yield return new ResultShape0(m.PatternId, m.MatchText);
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
                        yield return new ResultShape0(m.PatternId, m.MatchText);
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

        private static __musoqStructuralCarrier_0_a __musoqCaptureStructuralParameter_0_a(object? rawValue, StructuralParameterCaptureState state, int depth, string path)
        {
            state.ReserveNode(depth);
            if (rawValue == null || rawValue is StructuralValue { Kind: StructuralTypeKind.Scalar, Scalar: null })
            {
                throw new InvalidOperationException("A non-null structural record was expected.");
            }

            state.Enter(rawValue, path);
            try
            {
                StructuralParameterCaptureRuntime.ValidateRecord(rawValue, __musoqStructuralParameterFields_0_a, path);
                var __presentField0 = StructuralParameterCaptureRuntime.TryGetRecordField(rawValue, "Id", out var __rawField0);
                string __field0;
                if (__presentField0)
                {
                    __field0 = StructuralParameterCaptureRuntime.ReadScalar<string>(__rawField0, path + ".Id", true, state, depth + 1);
                }
                else
                {
                    throw new InvalidOperationException("Structural value '" + path + "' is missing required field 'Id'.");
                }

                var __presentField1 = StructuralParameterCaptureRuntime.TryGetRecordField(rawValue, "Pattern", out var __rawField1);
                string __field1;
                if (__presentField1)
                {
                    __field1 = StructuralParameterCaptureRuntime.ReadScalar<string>(__rawField1, path + ".Pattern", true, state, depth + 1);
                }
                else
                {
                    throw new InvalidOperationException("Structural value '" + path + "' is missing required field 'Pattern'.");
                }

                var __presentField2 = StructuralParameterCaptureRuntime.TryGetRecordField(rawValue, "Mode", out var __rawField2);
                string __field2;
                if (__presentField2)
                {
                    __field2 = StructuralParameterCaptureRuntime.ReadScalar<string>(__rawField2, path + ".Mode", true, state, depth + 1);
                }
                else
                {
                    state.ReserveMetrics(new StructuralInputMetrics(1, 1L, 10L), depth + 1);
                    __field2 = "regex";
                    __presentField2 = true;
                }

                return new __musoqStructuralCarrier_0_a(__field0, __field1, __field2, (__presentField0 ? 1UL : 0UL) | (__presentField1 ? 2UL : 0UL) | (__presentField2 ? 4UL : 0UL));
            }
            finally
            {
                state.Exit(rawValue);
            }
        }

        private static __musoqStructuralCarrier_0_a[] __musoqCaptureStructuralParameter_0_root(object? rawValue, StructuralParameterCaptureState state, int depth, string path)
        {
            state.ReserveNode(depth);
            if (rawValue == null || rawValue is StructuralValue { Kind: StructuralTypeKind.Scalar, Scalar: null })
            {
                throw new InvalidOperationException("A non-null structural collection was expected.");
            }

            state.Enter(rawValue, path);
            try
            {
                var count = StructuralParameterCaptureRuntime.GetCollectionCount<__musoqStructuralCarrier_0_a>(rawValue, path);
                state.EnsureCollectionLowerBound(count);
                var result = new __musoqStructuralCarrier_0_a[count];
                if (rawValue is __musoqStructuralCarrier_0_a[] typedArray)
                {
                    for (var index = 0; index < count; index++)
                    {
                        if ((index & 1023) == 0)
                            state.CancellationToken.ThrowIfCancellationRequested();
                        result[index] = __musoqCaptureStructuralParameter_0_a(typedArray[index], state, depth + 1, path + "[" + index + "]");
                    }
                }
                else if (rawValue is IReadOnlyList<__musoqStructuralCarrier_0_a> typedList)
                {
                    for (var index = 0; index < count; index++)
                    {
                        if ((index & 1023) == 0)
                            state.CancellationToken.ThrowIfCancellationRequested();
                        result[index] = __musoqCaptureStructuralParameter_0_a(typedList[index], state, depth + 1, path + "[" + index + "]");
                    }
                }
                else if (rawValue is IReadOnlyList<object?> referenceList)
                {
                    for (var index = 0; index < count; index++)
                    {
                        if ((index & 1023) == 0)
                            state.CancellationToken.ThrowIfCancellationRequested();
                        result[index] = __musoqCaptureStructuralParameter_0_a(referenceList[index], state, depth + 1, path + "[" + index + "]");
                    }
                }
                else if (rawValue is StructuralValue { Kind: StructuralTypeKind.Collection } structural)
                {
                    for (var index = 0; index < count; index++)
                    {
                        if ((index & 1023) == 0)
                            state.CancellationToken.ThrowIfCancellationRequested();
                        result[index] = __musoqCaptureStructuralParameter_0_a(structural.Elements[index], state, depth + 1, path + "[" + index + "]");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Structural collection does not expose a supported indexed representation.");
                }

                return result;
            }
            finally
            {
                state.Exit(rawValue);
            }
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
                m_PatternId = __value0;
                m_MatchText = __value1;
            }

            public override int Count => 2;
            public string m_MatchText { get; private set; }
            public string m_PatternId { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        m_PatternId = (string)value;
                        break;
                    case 1:
                        m_MatchText = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "m.PatternId" => true,
                "m_PatternId" => true,
                "PatternId" => true,
                "m.MatchText" => true,
                "m_MatchText" => true,
                "MatchText" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)m_PatternId,
                1 => (object)m_MatchText,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "m.PatternId" => (object)m_PatternId,
                "m_PatternId" => (object)m_PatternId,
                "PatternId" => (object)m_PatternId,
                "m.MatchText" => (object)m_MatchText,
                "m_MatchText" => (object)m_MatchText,
                "MatchText" => (object)m_MatchText,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string m_PatternId, string m_MatchText)
            {
                this.m_PatternId = m_PatternId;
                this.m_MatchText = m_MatchText;
            }

            public string m_MatchText { get; }
            public string m_PatternId { get; }
        }

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
    }
}
