// === Parsed Query ===
/*
SELECT City as c,
                         Population::Int32 as pop,
                         Count(*) as cnt,
                         Sum(Amount::Decimal) as total
                  FROM #features.items()
                  WHERE pop > 0
                  GROUP BY ALL
                  HAVING cnt > 1 AND total > '10.00'::Decimal
                  ORDER BY c
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Population::Int32 as ko3iko.Population::Int32, ko3iko.City as ko3iko.City, AggRef(ko3iko.Sum(ko3iko.Amount::Decimal)) as ko3iko.Sum(ko3iko.Amount::Decimal), AggRef(ko3iko.Count(*)) as ko3iko.Count(*)]
    Having [((AggRef(ko3iko.Count(*)) > 1) AND (AggRef(ko3iko.Sum(ko3iko.Amount::Decimal)) > '10.00'::Decimal))]
      Aggregate [keys: ko3iko.City, ko3iko.Population::Int32] [aggs: Sum(Amount::Decimal), Count(*)]
        Filter [(ko3iko.Population::Int32 > 0)]
          SchemaScan [#features.items() as ko3iko]
  Sort [ko3iko.City]
    Project [ko3iko.City as c, ko3iko.Population::Int32 as pop, ko3iko.Count(*) as cnt, ko3iko.Sum(ko3iko.Amount::Decimal) as total]
      CteRef [ko3ikoScore as ko3ikoScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Population::Int32 as ko3iko.Population::Int32, ko3iko.City as ko3iko.City, AggRef(ko3iko.Sum(ko3iko.Amount::Decimal)) as ko3iko.Sum(ko3iko.Amount::Decimal), AggRef(ko3iko.Count(*)) as ko3iko.Count(*)]
    PhysicalHaving [((AggRef(ko3iko.Count(*)) > 1) AND (AggRef(ko3iko.Sum(ko3iko.Amount::Decimal)) > '10.00'::Decimal))]
      PhysicalValueTupleAggregate [keys: ko3iko.City, ko3iko.Population::Int32] [aggs: Sum(Amount::Decimal), Count(*)]
        PhysicalFilter [(ko3iko.Population::Int32 > 0)]
          PhysicalSchemaScan [#features.items() as ko3iko] [pushdown: (ko3iko.Population::Int32 > 0)]
  PhysicalSort [ko3iko.City]
    PhysicalProject [ko3iko.City as c, ko3iko.Population::Int32 as pop, ko3iko.Count(*) as cnt, ko3iko.Sum(ko3iko.Amount::Decimal) as total]
      PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: RuntimeV2CastGroupingFeatureEntity]
      City: string <- property City
      Population: string <- property Population
      Amount: string <- property Amount
    AggregateGroup [ResultAggregateGroup; keys: 2; typed aggs: 2]
    Generated [ResultRow0]
      c: string <- field c
      pop: int? <- field pop
      cnt: long <- field cnt
      total: decimal? <- field total

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: RuntimeV2CastGroupingFeatureEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [GroupBy]
    CreateValueTupleAggregateContext [groups: (string, int?) -> ResultAggregateGroup]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [population: string = ko3iko.Population]
      Let [populationInt32: int? = population::Int32]
      If [(populationInt32 > 0)]
        Let [amount: string = ko3iko.Amount]
        Let [amountDecimal: decimal? = amount::Decimal]
        GetOrAddValueTupleAggregateGroup [group = groups[(ko3iko.City, populationInt32)] by ko3iko.City, ko3iko.Population::Int32; typed: ResultAggregateGroup]
        TypedAggregateSet [Set(group.__agg0, amountDecimal)]
        TypedAggregateSet [Set(group.__agg1)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      If [((ko3iko.Count(*) > 1) AND (ko3iko.Sum(ko3iko.Amount::Decimal) > '10.00'::Decimal))]
        AppendShape [result <- ResultShape0(c: finalGroup.ko3iko.City, pop: finalGroup.ko3iko.Population::Int32, cnt: ko3iko.Count(*), total: ko3iko.Sum(ko3iko.Amount::Decimal))]
    SortShapeRows [result -> resultSorted by c ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q158_RuntimeV2CombinedGrouping
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
            new Column("c", typeof(string), 0),
            new Column("pop", typeof(int?), 1),
            new Column("cnt", typeof(long), 2),
            new Column("total", typeof(decimal?), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 0), new Column("Population", typeof(string), 2), new Column("Amount", typeof(string), 3) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.c, __musoqShapeRow.pop, __musoqShapeRow.cnt, __musoqShapeRow.total);
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
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#features");
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity>("items", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<(string, int?), ResultAggregateGroup>();
                OnPhaseChanged("compiled", QueryPhase.Where);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                string population = ko3iko.Population;
                                int? populationInt32 = global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt32(population);
                                if ((populationInt32 > 0))
                                {
                                    string amount = ko3iko.Amount;
                                    decimal? amountDecimal = global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToDecimal(amount);
                                    string groupKey0 = ko3iko.City;
                                    int? groupKey1 = populationInt32;
                                    ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, (groupKey0, groupKey1), out var groupExists);
                                    if (!groupExists)
                                    {
                                        groupRef = new ResultAggregateGroup(groupKey0, groupKey1);
                                        groupsToFinalize.Add(groupRef);
                                    }

                                    ResultAggregateGroup group = groupRef;
                                    {
                                        var __agg0Input = (decimal?)amountDecimal;
                                        if (__agg0Input.HasValue)
                                        {
                                            var __agg0Current = __agg0Input.GetValueOrDefault();
                                            group.__agg0.Value = group.__agg0.HasValue ? checked(group.__agg0.Value + __agg0Current) : __agg0Current;
                                            group.__agg0.HasValue = true;
                                        }
                                    }

                                    group.__agg1.Count = checked(group.__agg1.Count + 1L);
                                }
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                string population = ko3iko.Population;
                                int? populationInt32 = global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt32(population);
                                if ((populationInt32 > 0))
                                {
                                    string amount = ko3iko.Amount;
                                    decimal? amountDecimal = global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToDecimal(amount);
                                    string groupKey0 = ko3iko.City;
                                    int? groupKey1 = populationInt32;
                                    ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, (groupKey0, groupKey1), out var groupExists);
                                    if (!groupExists)
                                    {
                                        groupRef = new ResultAggregateGroup(groupKey0, groupKey1);
                                        groupsToFinalize.Add(groupRef);
                                    }

                                    ResultAggregateGroup group = groupRef;
                                    {
                                        var __agg0Input = (decimal?)amountDecimal;
                                        if (__agg0Input.HasValue)
                                        {
                                            var __agg0Current = __agg0Input.GetValueOrDefault();
                                            group.__agg0.Value = group.__agg0.HasValue ? checked(group.__agg0.Value + __agg0Current) : __agg0Current;
                                            group.__agg0.HasValue = true;
                                        }
                                    }

                                    group.__agg1.Count = checked(group.__agg1.Count + 1L);
                                }
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
                        string population = ko3iko.Population;
                        int? populationInt32 = global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt32(population);
                        if ((populationInt32 > 0))
                        {
                            string amount = ko3iko.Amount;
                            decimal? amountDecimal = global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToDecimal(amount);
                            string groupKey0 = ko3iko.City;
                            int? groupKey1 = populationInt32;
                            ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, (groupKey0, groupKey1), out var groupExists);
                            if (!groupExists)
                            {
                                groupRef = new ResultAggregateGroup(groupKey0, groupKey1);
                                groupsToFinalize.Add(groupRef);
                            }

                            ResultAggregateGroup group = groupRef;
                            {
                                var __agg0Input = (decimal?)amountDecimal;
                                if (__agg0Input.HasValue)
                                {
                                    var __agg0Current = __agg0Input.GetValueOrDefault();
                                    group.__agg0.Value = group.__agg0.HasValue ? checked(group.__agg0.Value + __agg0Current) : __agg0Current;
                                    group.__agg0.HasValue = true;
                                }
                            }

                            group.__agg1.Count = checked(group.__agg1.Count + 1L);
                        }
                    }
                }

                result.EnsureCapacity(groupsToFinalize.Count);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    if (((finalGroup.__agg1.Count > 1) && ((finalGroup.__agg0.HasValue ? (decimal?)finalGroup.__agg0.Value : null) > global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToDecimal("10.00"))))
                    {
                        result.Add(new ResultShape0(finalGroup.__key0, finalGroup.__key1, finalGroup.__agg1.Count, finalGroup.__agg0.HasValue ? (decimal?)finalGroup.__agg0.Value : null));
                    }
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.c, right.c);
                    if (comparison != 0)
                        return comparison;
                    return 0;
                }));
                foreach (var resultSortedRowsRow in resultSortedRows)
                {
                    __musoqFinalShapeRows.Add(resultSortedRowsRow);
                }

                return __musoqFinalShapeRows;
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

        private sealed class ResultAggregateGroup
        {
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg0;
            public Musoq.Plugins.CountAllAggregateKernel.State __agg1;
            public readonly string __key0;
            public readonly int? __key1;
            public ResultAggregateGroup(string __key0, int? __key1)
            {
                this.__key0 = __key0;
                this.__key1 = __key1;
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.CountAllAggregateKernel.Merge(ref this.__agg1, in source.__agg1);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, int? __value1, long __value2, decimal? __value3)
            {
                c = __value0;
                pop = __value1;
                cnt = __value2;
                total = __value3;
            }

            public override int Count => 4;
            public string c { get; private set; }
            public long cnt { get; private set; }
            public int? pop { get; private set; }
            public decimal? total { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        c = (string)value;
                        break;
                    case 1:
                        pop = (int?)value;
                        break;
                    case 2:
                        cnt = (long)value;
                        break;
                    case 3:
                        total = (decimal?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "c" => true,
                "pop" => true,
                "cnt" => true,
                "total" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)c,
                1 => (object)pop,
                2 => (object)cnt,
                3 => (object)total,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "c" => (object)c,
                "pop" => (object)pop,
                "cnt" => (object)cnt,
                "total" => (object)total,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string c, int? pop, long cnt, decimal? total)
            {
                this.c = c;
                this.pop = pop;
                this.cnt = cnt;
                this.total = total;
            }

            public string c { get; }
            public long cnt { get; }
            public int? pop { get; }
            public decimal? total { get; }
        }
    }
}
