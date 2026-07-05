/*
raw query string

SELECT City, Sum(Amount::Decimal) as TotalAmount
                  FROM #features.items()
                  WHERE Population::Int32 > 0
                  GROUP BY City
                  HAVING Sum(Amount::Decimal) > '10.00'::Decimal
*/

/*
logical plan representation string

MultiStatement
  Project [ko3iko.City as ko3iko.City, AggRef(ko3iko.Sum(ko3iko.Amount::Decimal)) as ko3iko.Sum(ko3iko.Amount::Decimal)]
    Having [(AggRef(ko3iko.Sum(ko3iko.Amount::Decimal)) > '10.00'::Decimal)]
      Aggregate [keys: City] [aggs: Sum(Amount::Decimal)]
        Filter [(ko3iko.Population::Int32 > 0)]
          SchemaScan [#features.items() as ko3iko]
  Project [ko3iko.City as City, ko3iko.Sum(ko3iko.Amount::Decimal) as TotalAmount]
    CteRef [ko3ikoScore as ko3ikoScore]
*/

/*
physical plan representation string

PhysicalMultiStatement
  PhysicalProject [ko3iko.City as ko3iko.City, AggRef(ko3iko.Sum(ko3iko.Amount::Decimal)) as ko3iko.Sum(ko3iko.Amount::Decimal)]
    PhysicalHaving [(AggRef(ko3iko.Sum(ko3iko.Amount::Decimal)) > '10.00'::Decimal)]
      PhysicalSingleKeyAggregate [key: City (String)] [aggs: Sum(Amount::Decimal)]
        PhysicalFilter [(ko3iko.Population::Int32 > 0)]
          PhysicalSchemaScan [#features.items() as ko3iko] [pushdown: (ko3iko.Population::Int32 > 0)]
  PhysicalProject [ko3iko.City as City, ko3iko.Sum(ko3iko.Amount::Decimal) as TotalAmount]
    PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: RuntimeV2CastGroupingFeatureEntity]
      City: string <- property City
      Population: string <- property Population
      Amount: string <- property Amount
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 1]
    Generated [ResultRow0]
      City: string <- field City
      TotalAmount: decimal? <- field TotalAmount

  Body
    SourceScan [ko3iko: RuntimeV2CastGroupingFeatureEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateSingleKeyAggregateContext [groups: string -> ResultAggregateGroup]
    ChunkedForEach [ko3iko in ko3ikoRows]
      If [(ko3iko.Population::Int32 > 0)]
        Let [amount: string = ko3iko.Amount]
        Let [amountDecimal: decimal? = amount::Decimal]
        GetOrAddSingleKeyAggregateGroup [group = groups[ko3iko.City] by City; typed: ResultAggregateGroup]
        TypedAggregateSet [Set(group.__agg0, amountDecimal)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      If [(ko3iko.Sum(ko3iko.Amount::Decimal) > '10.00'::Decimal)]
        AppendShape [result <- ResultShape0(City: finalGroup.City, TotalAmount: ko3iko.Sum(ko3iko.Amount::Decimal))]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q152_RuntimeV2CastAggregateGrouping
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("City", typeof(string), 0),
            new Column("TotalAmount", typeof(decimal?), 1)
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
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.City, __musoqShapeRow.TotalAmount);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.Where);
            OnPhaseChanged("compiled", QueryPhase.GroupBy);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                var __ko3ikoSchema = provider.GetSchema("#features");
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity>("items", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = ko3ikoRowsSource.Chunks;
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<string, ResultAggregateGroup>();
                ResultAggregateGroup nullGroup = null;
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
                                if ((global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt32(ko3iko.Population) > 0))
                                {
                                    UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, ko3iko);
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
                                if ((global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt32(ko3iko.Population) > 0))
                                {
                                    UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, ko3iko);
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
                        if ((global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt32(ko3iko.Population) > 0))
                        {
                            UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, ko3iko);
                        }
                    }
                }

                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    if (((finalGroup.__agg0.HasValue ? (decimal?)finalGroup.__agg0.Value : null) > global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToDecimal("10.00")))
                    {
                        __musoqFinalShapeRows.Add(new ResultShape0(finalGroup.__key0, finalGroup.__agg0.HasValue ? (decimal?)finalGroup.__agg0.Value : null));
                    }
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void UpdateGroupsAggregates(List<ResultAggregateGroup> groupsToFinalize, Dictionary<string, ResultAggregateGroup> groups, ref ResultAggregateGroup nullGroup, Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity ko3iko)
        {
            string amount = ko3iko.Amount;
            decimal? amountDecimal = global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToDecimal(amount);
            string groupKey = ko3iko.City;
            ResultAggregateGroup group = null;
            if (groupKey != null)
            {
                ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var groupExists);
                if (!groupExists)
                {
                    groupRef = new ResultAggregateGroup(groupKey);
                    groupsToFinalize.Add(groupRef);
                }

                group = groupRef;
            }
            else
            {
                if (nullGroup == null)
                {
                    nullGroup = new ResultAggregateGroup(null);
                    groupsToFinalize.Add(nullGroup);
                }

                group = nullGroup;
            }

            {
                var __agg0Input = (decimal?)amountDecimal;
                if (__agg0Input.HasValue)
                {
                    var __agg0Current = __agg0Input.GetValueOrDefault();
                    group.__agg0.Value = group.__agg0.HasValue ? checked(group.__agg0.Value + __agg0Current) : __agg0Current;
                    group.__agg0.HasValue = true;
                }
            }
        }

        private sealed class ResultAggregateGroup
        {
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg0;
            public readonly string __key0;
            public ResultAggregateGroup(string __key0)
            {
                this.__key0 = __key0;
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, decimal? __value1)
            {
                City = __value0;
                TotalAmount = __value1;
            }

            public string City { get; private set; }
            public override int Count => 2;
            public decimal? TotalAmount { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        City = (string)value;
                        break;
                    case 1:
                        TotalAmount = (decimal?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "City" => true,
                "TotalAmount" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)City,
                1 => (object)TotalAmount,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "City" => (object)City,
                "TotalAmount" => (object)TotalAmount,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string City, decimal? TotalAmount)
            {
                this.City = City;
                this.TotalAmount = TotalAmount;
            }

            public string City { get; }
            public decimal? TotalAmount { get; }
        }
    }
}
