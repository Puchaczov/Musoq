// === Parsed Query ===
/*
select s.Value
from #inputs.strict(values: array { 1, 2 }) s
*/

// === Logical Plan ===
/*
MultiStatement
  Project [s.Value as s.Value]
    SchemaScan [#inputs.strict(array { 1, 2 }) as s]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [s.Value as s.Value]
    PhysicalSchemaScan [#inputs.strict(array { 1, 2 }) as s]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [s: StrictLimitRow]
      Value: int <- property Value
    Generated [ResultRow0]
      s.Value: int <- field s_Value

  Body
    PhaseBoundary [Begin]
    Let [__musoqStructural_s_0: IReadOnlyList<int> = array { 1, 2 }]
    PhaseBoundary [From]
    SourceScan [s: StrictLimitRow] -> sRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [s in sRows]
      AppendShape [result <- ResultShape0(s.Value: s.Value)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q363_StructuredStrictReceiverDemand
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("s.Value", typeof(int), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_s_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Value", typeof(int), 0) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public IDictionary<string, System.Object> Parameters { get; } = new Dictionary<string, System.Object>(StringComparer.Ordinal);
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }

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
                yield return new ResultRow0(__musoqShapeRow.s_Value);
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
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                IReadOnlyList<int> __musoqStructural_s_0 = new int[]
                {
                    1,
                    2
                };
                OnPhaseChanged("compiled", QueryPhase.From);
                var __sSchema = provider.GetSchema("#inputs");
                var sRowsSourceContext = new SourceExecutionContext("s:1", sourceExecutionPlans["s:1"], token, __schemaColumns_compiled_s_0, sourceRuntimeSettingsBySourceContextId["s:1"], logger, OnDataSourceProgress);
                Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.StrictLimitRow> sRowsSource;
                try
                {
                    Musoq.Examples.DataSources.StructuredInputs.StrictLimitSource sRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.StrictLimitSource(__musoqCheckStructural_e76b1fbbf5574933(__musoqStructural_s_0, token), sRowsSourceContext);
                    sRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.StrictLimitSource, Musoq.Examples.DataSources.StructuredInputs.StrictLimitRow>(__sSchema, "strict", sRowsSourceInstance, sRowsSourceContext, "#inputs", "s", "s:1");
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
                    throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "strict", "s", "s:1", exception);
                }

                var sRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.StrictLimitRow>(sRowsSource.Chunks, __musoqProgressContext, "s:1") : sRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var sChunk in sRows)
                {
                    if (sChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Examples.DataSources.StructuredInputs.StrictLimitRow> sChunkView)
                    {
                        if (sChunkView.Source is Musoq.Examples.DataSources.StructuredInputs.StrictLimitRow[] sChunkViewArray)
                        {
                            int sChunkViewOffset = sChunkView.Offset;
                            for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                            {
                                if ((sIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var s = sChunkViewArray[sChunkViewOffset + sIndex];
                                yield return new ResultShape0(s.Value);
                            }

                            continue;
                        }

                        if (sChunkView.Source is List<Musoq.Examples.DataSources.StructuredInputs.StrictLimitRow> sChunkViewList)
                        {
                            int sChunkViewOffset = sChunkView.Offset;
                            for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                            {
                                if ((sIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var s = sChunkViewList[sChunkViewOffset + sIndex];
                                yield return new ResultShape0(s.Value);
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
                        yield return new ResultShape0(s.Value);
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

        private static IReadOnlyList<int> __musoqCheckStructural_e76b1fbbf5574933(IReadOnlyList<int> value, System.Threading.CancellationToken token)
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
                var __musoqStructural_collection1 = (System.Collections.Generic.IReadOnlyList<int>)value;
                var __musoqStructural_count2 = __musoqStructural_collection1.Count;
                for (var __musoqStructural_index0 = 0; __musoqStructural_index0 < __musoqStructural_count2; __musoqStructural_index0++)
                {
                    if ((__musoqStructural_index0 & 1023) == 0)
                        token.ThrowIfCancellationRequested();
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (2 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 2;
                }
            }

            global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("inline", "s.argument[0]", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 8, 3L, 1024L, token, "s:1");
            return value;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0)
            {
                s_Value = __value0;
            }

            public override int Count => 1;
            public int s_Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        s_Value = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "s.Value" => true,
                "s_Value" => true,
                "Value" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)s_Value,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "s.Value" => (object)s_Value,
                "s_Value" => (object)s_Value,
                "Value" => (object)s_Value,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int s_Value)
            {
                this.s_Value = s_Value;
            }

            public int s_Value { get; }
        }
    }
}
