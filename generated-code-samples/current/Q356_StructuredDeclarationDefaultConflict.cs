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
  Project [d.Id as d.Id, d.Mode as d.Mode]
    SchemaScan [#inputs.defaultconflict(<PatternInput>$input) as d]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [d.Id as d.Id, d.Mode as d.Mode]
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
      d.Id: string <- field d_Id
      d.Mode: string <- field d_Mode

  Body
    PhaseBoundary [Begin]
    Let [__musoqStructural_d_0: PatternInput = convert<Musoq.Examples.DataSources.StructuredInputs.PatternInput>($input)]
    PhaseBoundary [From]
    SourceScan [d: DefaultConflictRow] -> dRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [d in dRows]
      AppendShape [result <- ResultShape0(d.Id: d.Id, d.Mode: d.Mode)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, IStructuralParameterSnapshotProvider
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("d.Id", typeof(string), 0),
            new Column("d.Mode", typeof(string), 1)
        };
        private static readonly string[] __musoqDeclaredParameterNames = new string[]
        {
            "input"
        };
        private static readonly string[] __musoqStructuralParameterFields_0_root = new[]
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
                    throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.TypeMismatch("input", typeof(Musoq.Schema.StructuralInputs.StructuralValue), __musoqRawParameter_0, exception);
                }
            }
            else
            {
                __musoqCaptureState.ReserveMetrics(new StructuralInputMetrics(2, 4L, 26L), 1);
                __musoqSnapshot["input"] = new __musoqStructuralCarrier_0_root("todo", "TODO", "regex", 7UL);
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
                yield return new ResultRow0(__musoqShapeRow.d_Id, __musoqShapeRow.d_Mode);
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
                var paramInput = ScriptParameterBinder.GetOptional<__musoqStructuralCarrier_0_root>(__musoqExecutionState.Parameters, "input", default(__musoqStructuralCarrier_0_root));
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, new string[] { "input" });
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
                                yield return new ResultShape0(d.Id, d.Mode);
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
                                yield return new ResultShape0(d.Id, d.Mode);
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
                        yield return new ResultShape0(d.Id, d.Mode);
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

        private static __musoqStructuralCarrier_0_root __musoqCaptureStructuralParameter_0_root(object? rawValue, StructuralParameterCaptureState state, int depth, string path)
        {
            state.ReserveNode(depth);
            if (rawValue == null || rawValue is StructuralValue { Kind: StructuralTypeKind.Scalar, Scalar: null })
            {
                throw new InvalidOperationException("A non-null structural record was expected.");
            }

            state.Enter(rawValue, path);
            try
            {
                StructuralParameterCaptureRuntime.ValidateRecord(rawValue, __musoqStructuralParameterFields_0_root, path);
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

                return new __musoqStructuralCarrier_0_root(__field0, __field1, __field2, (__presentField0 ? 1UL : 0UL) | (__presentField1 ? 2UL : 0UL) | (__presentField2 ? 4UL : 0UL));
            }
            finally
            {
                state.Exit(rawValue);
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

        private static Musoq.Examples.DataSources.StructuredInputs.PatternInput __musoqStructuralAdapt_0_919131913(__musoqStructuralCarrier_0_root source)
        {
            return new Musoq.Examples.DataSources.StructuredInputs.PatternInput(((source.P0 & 1UL) != 0UL) ? source.F0 : default(string), ((source.P0 & 2UL) != 0UL) ? source.F1 : default(string), ((source.P0 & 4UL) != 0UL) ? source.F2 : "literal");
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                d_Id = __value0;
                d_Mode = __value1;
            }

            public override int Count => 2;
            public string d_Id { get; private set; }
            public string d_Mode { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        d_Id = (string)value;
                        break;
                    case 1:
                        d_Mode = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "d.Id" => true,
                "d_Id" => true,
                "Id" => true,
                "d.Mode" => true,
                "d_Mode" => true,
                "Mode" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)d_Id,
                1 => (object)d_Mode,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "d.Id" => (object)d_Id,
                "d_Id" => (object)d_Id,
                "Id" => (object)d_Id,
                "d.Mode" => (object)d_Mode,
                "d_Mode" => (object)d_Mode,
                "Mode" => (object)d_Mode,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string d_Id, string d_Mode)
            {
                this.d_Id = d_Id;
                this.d_Mode = d_Mode;
            }

            public string d_Id { get; }
            public string d_Mode { get; }
        }

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
    }
}
