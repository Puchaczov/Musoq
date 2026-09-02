// === Parsed Query ===
/*
select Country, Substring(City, IndexOf(City, ':')) as 'City', Count(City) as 'Count', Sum(Population) as 'Sum' from #A.Entities() group by Substring(City, IndexOf(City, ':')), Country
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Country as ko3iko.Country, Substring(ko3iko.City, IndexOf(ko3iko.City, ':')) as ko3iko.Substring(ko3iko.City, ko3iko.IndexOf(ko3iko.City, ':')), AggRef(ko3iko.Sum(ko3iko.Population)) as ko3iko.Sum(ko3iko.Population), AggRef(ko3iko.Count(ko3iko.City)) as ko3iko.Count(ko3iko.City)]
    Aggregate [keys: Substring(City, IndexOf(City, :)), Country] [aggs: Sum(Population), Count(City)]
      SchemaScan [#A.Entities() as ko3iko]
  Project [ko3iko.Country as Country, ko3iko.Substring(ko3iko.City, ko3iko.IndexOf(ko3iko.City, ':')) as City, ko3iko.Count(ko3iko.City) as Count, ko3iko.Sum(ko3iko.Population) as Sum]
    CteRef [ko3ikoScore as ko3ikoScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Country as ko3iko.Country, Substring(ko3iko.City, IndexOf(ko3iko.City, ':')) as ko3iko.Substring(ko3iko.City, ko3iko.IndexOf(ko3iko.City, ':')), AggRef(ko3iko.Sum(ko3iko.Population)) as ko3iko.Sum(ko3iko.Population), AggRef(ko3iko.Count(ko3iko.City)) as ko3iko.Count(ko3iko.City)]
    PhysicalValueTupleAggregate [keys: Substring(City, IndexOf(City, :)), Country] [aggs: Sum(Population), Count(City)]
      PhysicalSchemaScan [#A.Entities() as ko3iko]
  PhysicalProject [ko3iko.Country as Country, ko3iko.Substring(ko3iko.City, ko3iko.IndexOf(ko3iko.City, ':')) as City, ko3iko.Count(ko3iko.City) as Count, ko3iko.Sum(ko3iko.Population) as Sum]
    PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      City: string <- property City
      Country: string <- property Country
      Population: decimal <- property Population
    AggregateGroup [Statement0AggregateGroup; keys: 2; typed aggs: 2]
    Generated [Statement0Row0]
      ko3iko.Country: string <- field ko3iko_Country
      ko3iko.Substring(ko3iko.City, ko3iko.IndexOf(ko3iko.City, ':')): string <- field ko3iko_Substring_ko3iko_City__ko3iko_IndexOf_ko3iko_City_______
      ko3iko.Sum(ko3iko.Population): decimal? <- field ko3iko_Sum_ko3iko_Population_
      ko3iko.Count(ko3iko.City): long <- field ko3iko_Count_ko3iko_City_
    TableRow [ko3ikoScore]
      ko3iko.Country: string <- field ko3iko_Country
      ko3iko.Substring(ko3iko.City, ko3iko.IndexOf(ko3iko.City, ':')): string <- field ko3iko_Substring_ko3iko_City__ko3iko_IndexOf_ko3iko_City_______
      ko3iko.Sum(ko3iko.Population): decimal? <- field ko3iko_Sum_ko3iko_Population_
      ko3iko.Count(ko3iko.City): long <- field ko3iko_Count_ko3iko_City_
    Generated [ResultRow0]
      Country: string <- field Country
      City: string <- field City
      Count: long <- field Count_
      Sum: decimal? <- field Sum

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> statement0_ko3ikoRows
    CreateObject [__statement0LibraryBase0: LibraryBase]
    CreateTable [statement0: Statement0Row0]
    PhaseBoundary [GroupBy]
    CreateValueTupleAggregateContext [statement0Groups: (string, string) -> Statement0AggregateGroup]
    ChunkedForEach [ko3iko in statement0_ko3ikoRows]
      Let [city: string = ko3iko.City]
      GetOrAddValueTupleAggregateGroup [statement0Group = statement0Groups[(Substring(city, IndexOf(city, ':')), ko3iko.Country)] by Substring(City, IndexOf(City, :)), Country; typed: Statement0AggregateGroup]
      Let [population: decimal = ko3iko.Population]
      TypedAggregateSet [Set(statement0Group.__agg0, population)]
      Let [city1: string = ko3iko.City]
      TypedAggregateSet [Set(statement0Group.__agg1, city1)]
    EnsureCapacity [statement0 <- statement0GroupsToFinalize.Count]
    ForEach [statement0FinalGroup in statement0GroupsToFinalize]
      AppendRow [statement0 <- Statement0Row0(ko3iko.Country: statement0FinalGroup.Country, ko3iko.Substring(ko3iko.City, ko3iko.IndexOf(ko3iko.City, ':')): statement0FinalGroup.Substring(City, IndexOf(City, :)), ko3iko.Sum(ko3iko.Population): ko3iko.Sum(ko3iko.Population), ko3iko.Count(ko3iko.City): ko3iko.Count(ko3iko.City))]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ForEach [ko3ikoScore in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Country: ko3ikoScore.ko3iko.Country, City: ko3ikoScore.ko3iko.Substring(ko3iko.City, ko3iko.IndexOf(ko3iko.City, ':')), Count: ko3ikoScore.ko3iko.Count(ko3iko.City), Sum: ko3ikoScore.ko3iko.Sum(ko3iko.Population))]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q98_GroupByExpressionNoAlias
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Country", typeof(string), 0),
            new Column("City", typeof(string), 1),
            new Column("Count", typeof(long), 2),
            new Column("Sum", typeof(decimal?), 3)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("ko3iko.Country", typeof(string), 0),
            new Column("ko3iko.Substring(ko3iko.City, ko3iko.IndexOf(ko3iko.City, ':'))", typeof(string), 1),
            new Column("ko3iko.Sum(ko3iko.Population)", typeof(decimal?), 2),
            new Column("ko3iko.Count(ko3iko.City)", typeof(long), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 11), new Column("Country", typeof(string), 12), new Column("Population", typeof(decimal), 13) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Country, __musoqShapeRow.City, __musoqShapeRow.Count_, __musoqShapeRow.Sum);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled", QueryPhase.Select);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 ko3ikoScore = __storedTable0Rows[__storedTable0Index];
                    __musoqFinalShapeRows.Add(new ResultShape0(ko3ikoScore.ko3iko_Country, ko3ikoScore.ko3iko_Substring_ko3iko_City__ko3iko_IndexOf_ko3iko_City_______, ko3ikoScore.ko3iko_Count_ko3iko_City_, ko3ikoScore.ko3iko_Sum_ko3iko_Population_));
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            var __statement0_ko3ikoSchema = provider.GetSchema("#A");
            var statement0_ko3ikoRowsSource = __statement0_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("Entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var statement0_ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(statement0_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : statement0_ko3ikoRowsSource.Chunks;
            var __statement0LibraryBase0 = new Musoq.Plugins.LibraryBase();
            var statement0 = new List<Statement0Row0>();
            var statement0GroupsToFinalize = new List<Statement0AggregateGroup>();
            var statement0Groups = new Dictionary<(string, string), Statement0AggregateGroup>();
            foreach (var ko3ikoChunk in statement0_ko3ikoRows)
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
                            string city = ko3iko.City;
                            string groupKey0 = (string)__statement0LibraryBase0.Substring(city, (int?)__statement0LibraryBase0.IndexOf(city, ":"));
                            string groupKey1 = ko3iko.Country;
                            ref var statement0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0Groups, (groupKey0, groupKey1), out var statement0GroupExists);
                            if (!statement0GroupExists)
                            {
                                statement0GroupRef = new Statement0AggregateGroup(groupKey0, groupKey1);
                                statement0GroupsToFinalize.Add(statement0GroupRef);
                            }

                            Statement0AggregateGroup statement0Group = statement0GroupRef;
                            decimal population = ko3iko.Population;
                            {
                                var __agg0Input = (decimal?)population;
                                if (__agg0Input.HasValue)
                                {
                                    var __agg0Current = __agg0Input.GetValueOrDefault();
                                    statement0Group.__agg0.Value = statement0Group.__agg0.HasValue ? checked(statement0Group.__agg0.Value + __agg0Current) : __agg0Current;
                                    statement0Group.__agg0.HasValue = true;
                                }
                            }

                            string city1 = ko3iko.City;
                            if ((string)city1 != null)
                            {
                                statement0Group.__agg1.Count = checked(statement0Group.__agg1.Count + 1L);
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
                            string city = ko3iko.City;
                            string groupKey0 = (string)__statement0LibraryBase0.Substring(city, (int?)__statement0LibraryBase0.IndexOf(city, ":"));
                            string groupKey1 = ko3iko.Country;
                            ref var statement0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0Groups, (groupKey0, groupKey1), out var statement0GroupExists);
                            if (!statement0GroupExists)
                            {
                                statement0GroupRef = new Statement0AggregateGroup(groupKey0, groupKey1);
                                statement0GroupsToFinalize.Add(statement0GroupRef);
                            }

                            Statement0AggregateGroup statement0Group = statement0GroupRef;
                            decimal population = ko3iko.Population;
                            {
                                var __agg0Input = (decimal?)population;
                                if (__agg0Input.HasValue)
                                {
                                    var __agg0Current = __agg0Input.GetValueOrDefault();
                                    statement0Group.__agg0.Value = statement0Group.__agg0.HasValue ? checked(statement0Group.__agg0.Value + __agg0Current) : __agg0Current;
                                    statement0Group.__agg0.HasValue = true;
                                }
                            }

                            string city1 = ko3iko.City;
                            if ((string)city1 != null)
                            {
                                statement0Group.__agg1.Count = checked(statement0Group.__agg1.Count + 1L);
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
                    string city = ko3iko.City;
                    string groupKey0 = (string)__statement0LibraryBase0.Substring(city, (int?)__statement0LibraryBase0.IndexOf(city, ":"));
                    string groupKey1 = ko3iko.Country;
                    ref var statement0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0Groups, (groupKey0, groupKey1), out var statement0GroupExists);
                    if (!statement0GroupExists)
                    {
                        statement0GroupRef = new Statement0AggregateGroup(groupKey0, groupKey1);
                        statement0GroupsToFinalize.Add(statement0GroupRef);
                    }

                    Statement0AggregateGroup statement0Group = statement0GroupRef;
                    decimal population = ko3iko.Population;
                    {
                        var __agg0Input = (decimal?)population;
                        if (__agg0Input.HasValue)
                        {
                            var __agg0Current = __agg0Input.GetValueOrDefault();
                            statement0Group.__agg0.Value = statement0Group.__agg0.HasValue ? checked(statement0Group.__agg0.Value + __agg0Current) : __agg0Current;
                            statement0Group.__agg0.HasValue = true;
                        }
                    }

                    string city1 = ko3iko.City;
                    if ((string)city1 != null)
                    {
                        statement0Group.__agg1.Count = checked(statement0Group.__agg1.Count + 1L);
                    }
                }
            }

            statement0.EnsureCapacity(statement0GroupsToFinalize.Count);
            foreach (var statement0FinalGroup in statement0GroupsToFinalize)
            {
                token.ThrowIfCancellationRequested();
                statement0.Add(new Statement0Row0(statement0FinalGroup.__key1, statement0FinalGroup.__key0, statement0FinalGroup.__agg0.HasValue ? (decimal?)statement0FinalGroup.__agg0.Value : null, statement0FinalGroup.__agg1.Count));
            }

            return statement0;
        }

        private sealed class CteRowResults
        {
            public List<Statement0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1, long __value2, decimal? __value3)
            {
                Country = __value0;
                City = __value1;
                Count_ = __value2;
                Sum = __value3;
            }

            public string City { get; private set; }
            public override int Count => 4;
            public long Count_ { get; private set; }
            public string Country { get; private set; }
            public decimal? Sum { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Country = (string)value;
                        break;
                    case 1:
                        City = (string)value;
                        break;
                    case 2:
                        Count_ = (long)value;
                        break;
                    case 3:
                        Sum = (decimal?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Country" => true,
                "City" => true,
                "Count" => true,
                "Count_" => true,
                "Sum" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Country,
                1 => (object)City,
                2 => (object)Count_,
                3 => (object)Sum,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Country" => (object)Country,
                "City" => (object)City,
                "Count" => (object)Count_,
                "Count_" => (object)Count_,
                "Sum" => (object)Sum,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Country, string City, long Count_, decimal? Sum)
            {
                this.Country = Country;
                this.City = City;
                this.Count_ = Count_;
                this.Sum = Sum;
            }

            public string City { get; }
            public long Count_ { get; }
            public string Country { get; }
            public decimal? Sum { get; }
        }

        private sealed class Statement0AggregateGroup
        {
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg0;
            public Musoq.Plugins.CountReferenceAggregateKernel<string>.State __agg1;
            public readonly string __key0;
            public readonly string __key1;
            public Statement0AggregateGroup(string __key0, string __key1)
            {
                this.__key0 = __key0;
                this.__key1 = __key1;
            }

            public void MergeFrom(Statement0AggregateGroup source)
            {
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.CountReferenceAggregateKernel<string>.Merge(ref this.__agg1, in source.__agg1);
            }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(string __value0, string __value1, decimal? __value2, long __value3)
            {
                ko3iko_Country = __value0;
                ko3iko_Substring_ko3iko_City__ko3iko_IndexOf_ko3iko_City_______ = __value1;
                ko3iko_Sum_ko3iko_Population_ = __value2;
                ko3iko_Count_ko3iko_City_ = __value3;
            }

            public long ko3iko_Count_ko3iko_City_ { get; }
            public string ko3iko_Country { get; }
            public string ko3iko_Substring_ko3iko_City__ko3iko_IndexOf_ko3iko_City_______ { get; }
            public decimal? ko3iko_Sum_ko3iko_Population_ { get; }
        }
    }
}
