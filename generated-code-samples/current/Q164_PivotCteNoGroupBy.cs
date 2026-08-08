// === Parsed Query ===
/*
with p as (
                      pivot #A.entities()
                      on Month in ('Jan' as Jan, 'Feb' as Feb)
                      using Sum(Money) as Sales
                  )
                  select Jan, Feb from p
*/

// === Logical Plan ===
/*
Cte
  Definition [p]
    MultiStatement
      Project [1 as 1, AggRef(ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb')) as ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb'), AggRef(ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan')) as ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan')]
        Aggregate [keys: 1] [aggs: Sum(Money) filter (where Month = 'Feb'), Sum(Money) filter (where Month = 'Jan')]
          SchemaScan [#A.entities() as ko3iko]
      Project [ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan') as Jan, ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb') as Feb]
        CteRef [ko3ikoScore as ko3ikoScore]
  Query
    MultiStatement
      Project [p.Jan as Jan, p.Feb as Feb]
        CteRef [p as p]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [p]
    PhysicalMultiStatement
      PhysicalProject [1 as 1, AggRef(ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb')) as ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb'), AggRef(ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan')) as ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan')]
        PhysicalSingleKeyAggregate [key: 1 (Int16)] [aggs: Sum(Money) filter (where Month = 'Feb'), Sum(Money) filter (where Month = 'Jan')]
          PhysicalSchemaScan [#A.entities() as ko3iko]
      PhysicalProject [ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan') as Jan, ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb') as Feb]
        PhysicalCteRef [ko3ikoScore as ko3ikoScore]
  Query
    PhysicalMultiStatement
      PhysicalProject [p.Jan as Jan, p.Feb as Feb]
        PhysicalCteRef [p as p]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Money: decimal <- property Money
      Month: string <- property Month
    AggregateGroup [Cte0AggregateGroup; keys: 0; typed aggs: 2]
    Generated [Cte0Row0]
      Jan: decimal? <- field Jan
      Feb: decimal? <- field Feb
    TableRow [p]
      Jan: decimal? <- field Jan
      Feb: decimal? <- field Feb
    Generated [ResultRow0]
      Jan: decimal? <- field Jan
      Feb: decimal? <- field Feb

  Body
    SourceScan [ko3iko: BasicEntity] -> cte0_ko3ikoRows
    CreateTable [cte0: Cte0Row0]
    CreateAggregateContext [cte0RootGroup, cte0Group, cte0GroupsToFinalize; typed: Cte0AggregateGroup]
    ChunkedForEach [ko3iko in cte0_ko3ikoRows]
      Let [money: decimal = ko3iko.Money]
      EnsureAggregateGroup [cte0Group; typed: Cte0AggregateGroup]
      TypedAggregateSet [Set(cte0Group.__agg0, money) filter (ko3iko.Month = 'Feb')]
      TypedAggregateSet [Set(cte0Group.__agg1, money) filter (ko3iko.Month = 'Jan')]
    EnsureCapacity [cte0 <- cte0GroupsToFinalize.Count]
    ForEach [cte0FinalGroup in cte0GroupsToFinalize]
      AppendRow [cte0 <- Cte0Row0(Jan: ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan'), Feb: ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb'))]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [p in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Jan: p.Jan, Feb: p.Feb)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q164_PivotCteNoGroupBy
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
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("Jan", typeof(decimal?), 0),
            new Column("Feb", typeof(decimal?), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Money", typeof(decimal), 15), new Column("Month", typeof(string), 16) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_cte0_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Jan, __musoqShapeRow.Feb);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.GroupBy);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 p = __storedTable0Rows[__storedTable0Index];
                    __musoqFinalShapeRows.Add(new ResultShape0(p.Jan, p.Feb));
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, CteRowResults _cteRowResults)
        {
            var __cte0_ko3ikoSchema = provider.GetSchema("#A");
            var cte0_ko3ikoRowsSource = __cte0_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var cte0_ko3ikoRows = cte0_ko3ikoRowsSource.Chunks;
            var cte0 = new List<Cte0Row0>();
            var cte0GroupsToFinalize = new List<Cte0AggregateGroup>();
            Cte0AggregateGroup cte0Group = new Cte0AggregateGroup();
            cte0GroupsToFinalize.Add(cte0Group);
            foreach (var ko3ikoChunk in cte0_ko3ikoRows)
            {
                if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkView)
                {
                    if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] ko3ikoChunkViewArray)
                    {
                        int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                        for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                        {
                            if ((ko3ikoIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                            decimal money = ko3iko.Money;
                            if (cte0Group == null)
                            {
                                cte0Group = new Cte0AggregateGroup();
                                cte0GroupsToFinalize.Add(cte0Group);
                            }

                            if ((ko3iko.Month == "Feb"))
                            {
                                {
                                    var __agg0Input = (decimal?)money;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        cte0Group.__agg0.Value = cte0Group.__agg0.HasValue ? checked(cte0Group.__agg0.Value + __agg0Current) : __agg0Current;
                                        cte0Group.__agg0.HasValue = true;
                                    }
                                }
                            }

                            if ((ko3iko.Month == "Jan"))
                            {
                                {
                                    var __agg1Input = (decimal?)money;
                                    if (__agg1Input.HasValue)
                                    {
                                        var __agg1Current = __agg1Input.GetValueOrDefault();
                                        cte0Group.__agg1.Value = cte0Group.__agg1.HasValue ? checked(cte0Group.__agg1.Value + __agg1Current) : __agg1Current;
                                        cte0Group.__agg1.HasValue = true;
                                    }
                                }
                            }
                        }

                        continue;
                    }

                    if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkViewList)
                    {
                        int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                        for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                        {
                            if ((ko3ikoIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                            decimal money = ko3iko.Money;
                            if (cte0Group == null)
                            {
                                cte0Group = new Cte0AggregateGroup();
                                cte0GroupsToFinalize.Add(cte0Group);
                            }

                            if ((ko3iko.Month == "Feb"))
                            {
                                {
                                    var __agg0Input = (decimal?)money;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        cte0Group.__agg0.Value = cte0Group.__agg0.HasValue ? checked(cte0Group.__agg0.Value + __agg0Current) : __agg0Current;
                                        cte0Group.__agg0.HasValue = true;
                                    }
                                }
                            }

                            if ((ko3iko.Month == "Jan"))
                            {
                                {
                                    var __agg1Input = (decimal?)money;
                                    if (__agg1Input.HasValue)
                                    {
                                        var __agg1Current = __agg1Input.GetValueOrDefault();
                                        cte0Group.__agg1.Value = cte0Group.__agg1.HasValue ? checked(cte0Group.__agg1.Value + __agg1Current) : __agg1Current;
                                        cte0Group.__agg1.HasValue = true;
                                    }
                                }
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
                    decimal money = ko3iko.Money;
                    if (cte0Group == null)
                    {
                        cte0Group = new Cte0AggregateGroup();
                        cte0GroupsToFinalize.Add(cte0Group);
                    }

                    if ((ko3iko.Month == "Feb"))
                    {
                        {
                            var __agg0Input = (decimal?)money;
                            if (__agg0Input.HasValue)
                            {
                                var __agg0Current = __agg0Input.GetValueOrDefault();
                                cte0Group.__agg0.Value = cte0Group.__agg0.HasValue ? checked(cte0Group.__agg0.Value + __agg0Current) : __agg0Current;
                                cte0Group.__agg0.HasValue = true;
                            }
                        }
                    }

                    if ((ko3iko.Month == "Jan"))
                    {
                        {
                            var __agg1Input = (decimal?)money;
                            if (__agg1Input.HasValue)
                            {
                                var __agg1Current = __agg1Input.GetValueOrDefault();
                                cte0Group.__agg1.Value = cte0Group.__agg1.HasValue ? checked(cte0Group.__agg1.Value + __agg1Current) : __agg1Current;
                                cte0Group.__agg1.HasValue = true;
                            }
                        }
                    }
                }
            }

            cte0.EnsureCapacity(cte0GroupsToFinalize.Count);
            foreach (var cte0FinalGroup in cte0GroupsToFinalize)
            {
                token.ThrowIfCancellationRequested();
                cte0.Add(new Cte0Row0(cte0FinalGroup.__agg1.HasValue ? (decimal?)cte0FinalGroup.__agg1.Value : null, cte0FinalGroup.__agg0.HasValue ? (decimal?)cte0FinalGroup.__agg0.Value : null));
            }

            return cte0;
        }

        private sealed class Cte0AggregateGroup
        {
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg0;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg1;
            public Cte0AggregateGroup()
            {
            }

            public void MergeFrom(Cte0AggregateGroup source)
            {
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg1, in source.__agg1);
            }
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(decimal? __value0, decimal? __value1)
            {
                Jan = __value0;
                Feb = __value1;
            }

            public decimal? Feb { get; }
            public decimal? Jan { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(decimal? __value0, decimal? __value1)
            {
                Jan = __value0;
                Feb = __value1;
            }

            public override int Count => 2;
            public decimal? Feb { get; private set; }
            public decimal? Jan { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Jan = (decimal?)value;
                        break;
                    case 1:
                        Feb = (decimal?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Jan" => true,
                "Feb" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Jan,
                1 => (object)Feb,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Jan" => (object)Jan,
                "Feb" => (object)Feb,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(decimal? Jan, decimal? Feb)
            {
                this.Jan = Jan;
                this.Feb = Feb;
            }

            public decimal? Feb { get; }
            public decimal? Jan { get; }
        }
    }
}
