// === Parsed Query ===
/*
select Scale(e.metric, l.factor) from #runtime.events() e inner join #runtime.lookup() l on e.runtimekey = l.id
*/

// === Logical Plan ===
/*
MultiStatement
  Project [e.RuntimeKey as e.RuntimeKey, e.Metric as e.Metric, l.Id as l.Id, l.Factor as l.Factor]
    Join [Inner] [(e.RuntimeKey = l.Id)]
      SchemaScan [#runtime.events() as e]
      SchemaScan [#runtime.lookup() as l]
  Project [Scale(e.Metric, l.Factor) as Scale(e.metric, l.factor)]
    CteRef [el as el]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [e.RuntimeKey as e.RuntimeKey, e.Metric as e.Metric, l.Id as l.Id, l.Factor as l.Factor]
    PhysicalHashJoin [Inner] [build: l.Id] [probe: e.RuntimeKey]
      PhysicalSchemaScan [#runtime.events() as e]
      PhysicalSchemaScan [#runtime.lookup() as l]
  PhysicalProject [Scale(e.Metric, l.Factor) as Scale(e.metric, l.factor)]
    PhysicalCteRef [el as el]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [e: RuntimeDynamicRow]
      Label: string <- property Label
      RuntimeKey: int <- runtime dynamic member "RuntimeKey"
      Enabled: bool <- runtime dynamic member "Enabled"
      Metric: double <- runtime dynamic member "Metric"
      Payload: string <- runtime dynamic member "Payload"
      Branch: RuntimeDynamicBranch <- runtime dynamic member "Branch"
      Branch.Measurement: double <- runtime dynamic member "Branch.Measurement"
      Branch.Raw: ulong <- runtime dynamic member "Branch.Raw"
      StaticBranch: RuntimeDynamicBranch <- property StaticBranch
      StaticBranch.Measurement: double <- runtime dynamic member "StaticBranch.Measurement"
      StaticBranch.Raw: ulong <- runtime dynamic member "StaticBranch.Raw"
    SourceEntity [l: RuntimeDynamicLookupRow]
      Id: int <- property Id
      Factor: double <- property Factor
    Generated [ResultRow0]
      Scale(e.metric, l.factor): double <- field Scale_e_metric__l_factor_

  Body
    CtePhase [cte0]
    SourceScan [e: RuntimeDynamicRow] -> eRows
    SourceScan [l: RuntimeDynamicLookupRow] -> lRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateHash [lHash: int -> RuntimeDynamicLookupRow]
    ChunkedForEach [l in lRows]
      HashAdd [lHash[l.Id] += l]
    CreateObject [__resultRuntimeDynamicLibrary0: RuntimeDynamicLibrary]
    ChunkedForEach [e in eRows]
      HashProbe [lHash[e.RuntimeKey] -> lHashMatches]
        ForEach [l in lHashMatches]
          AppendShape [result <- ResultShape0(Scale(e.metric, l.factor): Scale(e.Metric, l.Factor))]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q234_PublicDynamicJoinMethod
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Scale(e.metric, l.factor)", typeof(double), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_e_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Label", typeof(string), 0), new Column("RuntimeKey", typeof(int), 1), new Column("Enabled", typeof(bool), 2), new Column("Metric", typeof(double), 3), new Column("Payload", typeof(string), 4), new Column("Branch", typeof(Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicBranch), 5), new Column("Branch.Measurement", typeof(double), 6), new Column("Branch.Raw", typeof(ulong), 7), new Column("StaticBranch", typeof(Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicBranch), 8), new Column("StaticBranch.Measurement", typeof(double), 9), new Column("StaticBranch.Raw", typeof(ulong), 10) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_l_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Factor", typeof(double), 1) });
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
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Scale_e_metric__l_factor_);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                var __eSchema = provider.GetSchema("#runtime");
                var eRowsSource = __eSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicRow>("events", new SourceExecutionContext("e:1", sourceExecutionPlans["e:1"], token, __schemaColumns_compiled_e_0, sourceRuntimeSettingsBySourceContextId["e:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var eRows = eRowsSource.Chunks;
                var __lSchema = provider.GetSchema("#runtime");
                var lRowsSource = __lSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicLookupRow>("lookup", new SourceExecutionContext("l:1", sourceExecutionPlans["l:1"], token, __schemaColumns_compiled_l_1, sourceRuntimeSettingsBySourceContextId["l:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var lRows = lRowsSource.Chunks;
                var lHash = new Dictionary<int, HashJoinBucket<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicLookupRow>>();
                foreach (var lChunk in lRows)
                {
                    if (lChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicLookupRow> lChunkView)
                    {
                        if (lChunkView.Source is Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicLookupRow[] lChunkViewArray)
                        {
                            int lChunkViewOffset = lChunkView.Offset;
                            for (int lIndex = 0, lIndexCount = lChunkView.Count; lIndex < lIndexCount; ++lIndex)
                            {
                                if ((lIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var l = lChunkViewArray[lChunkViewOffset + lIndex];
                                int key = l.Id;
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(lHash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicLookupRow>(l);
                                    }
                                    else
                                    {
                                        matches.Add(l);
                                    }
                                }
                            }

                            continue;
                        }

                        if (lChunkView.Source is List<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicLookupRow> lChunkViewList)
                        {
                            int lChunkViewOffset = lChunkView.Offset;
                            for (int lIndex = 0, lIndexCount = lChunkView.Count; lIndex < lIndexCount; ++lIndex)
                            {
                                if ((lIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var l = lChunkViewList[lChunkViewOffset + lIndex];
                                int key = l.Id;
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(lHash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicLookupRow>(l);
                                    }
                                    else
                                    {
                                        matches.Add(l);
                                    }
                                }
                            }

                            continue;
                        }
                    }

                    for (int lIndex = 0, lIndexCount = lChunk.Count; lIndex < lIndexCount; ++lIndex)
                    {
                        if ((lIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var l = lChunk[lIndex];
                        int key = l.Id;
                        {
                            ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(lHash, key, out var matchesExists);
                            if (!matchesExists)
                            {
                                matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicLookupRow>(l);
                            }
                            else
                            {
                                matches.Add(l);
                            }
                        }
                    }
                }

                var __resultRuntimeDynamicLibrary0 = new Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicLibrary();
                foreach (var eChunk in eRows)
                {
                    if (eChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicRow> eChunkView)
                    {
                        if (eChunkView.Source is Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicRow[] eChunkViewArray)
                        {
                            int eChunkViewOffset = eChunkView.Offset;
                            for (int eIndex = 0, eIndexCount = eChunkView.Count; eIndex < eIndexCount; ++eIndex)
                            {
                                if ((eIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var e = eChunkViewArray[eChunkViewOffset + eIndex];
                                int key = (int)(object)((dynamic)e).RuntimeKey;
                                if (lHash.TryGetValue(key, out var lHashMatches))
                                {
                                    foreach (var l in lHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        __musoqFinalShapeRows.Add(new ResultShape0((double)__resultRuntimeDynamicLibrary0.Scale((double)(object)((dynamic)e).Metric, l.Factor)));
                                    }
                                }
                            }

                            continue;
                        }

                        if (eChunkView.Source is List<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicRow> eChunkViewList)
                        {
                            int eChunkViewOffset = eChunkView.Offset;
                            for (int eIndex = 0, eIndexCount = eChunkView.Count; eIndex < eIndexCount; ++eIndex)
                            {
                                if ((eIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var e = eChunkViewList[eChunkViewOffset + eIndex];
                                int key = (int)(object)((dynamic)e).RuntimeKey;
                                if (lHash.TryGetValue(key, out var lHashMatches))
                                {
                                    foreach (var l in lHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        __musoqFinalShapeRows.Add(new ResultShape0((double)__resultRuntimeDynamicLibrary0.Scale((double)(object)((dynamic)e).Metric, l.Factor)));
                                    }
                                }
                            }

                            continue;
                        }
                    }

                    for (int eIndex = 0, eIndexCount = eChunk.Count; eIndex < eIndexCount; ++eIndex)
                    {
                        if ((eIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var e = eChunk[eIndex];
                        int key = (int)(object)((dynamic)e).RuntimeKey;
                        if (lHash.TryGetValue(key, out var lHashMatches))
                        {
                            foreach (var l in lHashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                __musoqFinalShapeRows.Add(new ResultShape0((double)__resultRuntimeDynamicLibrary0.Scale((double)(object)((dynamic)e).Metric, l.Factor)));
                            }
                        }
                    }
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
                OnPhaseChanged("compiled", QueryPhase.End);
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(double __value0)
            {
                Scale_e_metric__l_factor_ = __value0;
            }

            public override int Count => 1;
            public double Scale_e_metric__l_factor_ { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Scale_e_metric__l_factor_ = (double)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Scale(e.metric, l.factor)" => true,
                "Scale_e_metric__l_factor_" => true,
                "factor)" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Scale_e_metric__l_factor_,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "Scale(e.metric, l.factor)" => (object)Scale_e_metric__l_factor_,
                "Scale_e_metric__l_factor_" => (object)Scale_e_metric__l_factor_,
                "factor)" => (object)Scale_e_metric__l_factor_,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(double Scale_e_metric__l_factor_)
            {
                this.Scale_e_metric__l_factor_ = Scale_e_metric__l_factor_;
            }

            public double Scale_e_metric__l_factor_ { get; }
        }
    }
}
