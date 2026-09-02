// === Parsed Query ===
/*
select Count(Name), Sum(Population), Min(Population), Max(Population) from #A.entities()
*/

// === Logical Plan ===
/*
MultiStatement
  Project [1 as 1, AggRef(ko3iko.Max(ko3iko.Population)) as ko3iko.Max(ko3iko.Population), AggRef(ko3iko.Min(ko3iko.Population)) as ko3iko.Min(ko3iko.Population), AggRef(ko3iko.Sum(ko3iko.Population)) as ko3iko.Sum(ko3iko.Population), AggRef(ko3iko.Count(ko3iko.Name)) as ko3iko.Count(ko3iko.Name)]
    Aggregate [keys: 1] [aggs: Max(Population), Min(Population), Sum(Population), Count(Name)]
      SchemaScan [#A.entities() as ko3iko]
  Project [ko3iko.Count(ko3iko.Name) as Count(Name), ko3iko.Sum(ko3iko.Population) as Sum(Population), ko3iko.Min(ko3iko.Population) as Min(Population), ko3iko.Max(ko3iko.Population) as Max(Population)]
    CteRef [ko3ikoScore as ko3ikoScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [1 as 1, AggRef(ko3iko.Max(ko3iko.Population)) as ko3iko.Max(ko3iko.Population), AggRef(ko3iko.Min(ko3iko.Population)) as ko3iko.Min(ko3iko.Population), AggRef(ko3iko.Sum(ko3iko.Population)) as ko3iko.Sum(ko3iko.Population), AggRef(ko3iko.Count(ko3iko.Name)) as ko3iko.Count(ko3iko.Name)]
    PhysicalSingleKeyAggregate [key: 1 (Int16)] [aggs: Max(Population), Min(Population), Sum(Population), Count(Name)]
      PhysicalSchemaScan [#A.entities() as ko3iko]
  PhysicalProject [ko3iko.Count(ko3iko.Name) as Count(Name), ko3iko.Sum(ko3iko.Population) as Sum(Population), ko3iko.Min(ko3iko.Population) as Min(Population), ko3iko.Max(ko3iko.Population) as Max(Population)]
    PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
      Population: decimal <- property Population
    AggregateGroup [ResultAggregateGroup; keys: 0; typed aggs: 4]
    Generated [ResultRow0]
      Count(Name): long <- field Count_Name_
      Sum(Population): decimal? <- field Sum_Population_
      Min(Population): decimal? <- field Min_Population_
      Max(Population): decimal? <- field Max_Population_

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateAggregateContext [rootGroup, group, groupsToFinalize; typed: ResultAggregateGroup]
    ChunkedForEach [ko3iko in ko3ikoRows]
      EnsureAggregateGroup [group; typed: ResultAggregateGroup]
      Let [population: decimal = ko3iko.Population]
      TypedAggregateSet [Set(group.__agg0, population)]
      Let [population1: decimal = ko3iko.Population]
      TypedAggregateSet [Set(group.__agg1, population1)]
      Let [population2: decimal = ko3iko.Population]
      TypedAggregateSet [Set(group.__agg2, population2)]
      Let [name: string = ko3iko.Name]
      TypedAggregateSet [Set(group.__agg3, name)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    PhaseBoundary [Select]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(Count(Name): ko3iko.Count(ko3iko.Name), Sum(Population): ko3iko.Sum(ko3iko.Population), Min(Population): ko3iko.Min(ko3iko.Population), Max(Population): ko3iko.Max(ko3iko.Population))]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q24_AggregateNoGroupBy
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
            new Column("Count(Name)", typeof(long), 0),
            new Column("Sum(Population)", typeof(decimal?), 1),
            new Column("Min(Population)", typeof(decimal?), 2),
            new Column("Max(Population)", typeof(decimal?), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("Population", typeof(decimal), 13) });
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
                yield return new ResultRow0(__musoqShapeRow.Count_Name_, __musoqShapeRow.Sum_Population_, __musoqShapeRow.Min_Population_, __musoqShapeRow.Max_Population_);
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
                var __ko3ikoSchema = provider.GetSchema("#A");
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var groupsToFinalize = new List<ResultAggregateGroup>();
                ResultAggregateGroup group = new ResultAggregateGroup();
                groupsToFinalize.Add(group);
                foreach (var ko3ikoChunk in ko3ikoRows)
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
                                if (group == null)
                                {
                                    group = new ResultAggregateGroup();
                                    groupsToFinalize.Add(group);
                                }

                                decimal population = ko3iko.Population;
                                {
                                    var __agg0Input = (decimal?)population;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        if (!group.__agg0.HasValue || __agg0Current > group.__agg0.Value)
                                        {
                                            group.__agg0.Value = __agg0Current;
                                        }

                                        group.__agg0.HasValue = true;
                                    }
                                }

                                decimal population1 = ko3iko.Population;
                                {
                                    var __agg1Input = (decimal?)population1;
                                    if (__agg1Input.HasValue)
                                    {
                                        var __agg1Current = __agg1Input.GetValueOrDefault();
                                        if (!group.__agg1.HasValue || __agg1Current < group.__agg1.Value)
                                        {
                                            group.__agg1.Value = __agg1Current;
                                        }

                                        group.__agg1.HasValue = true;
                                    }
                                }

                                decimal population2 = ko3iko.Population;
                                {
                                    var __agg2Input = (decimal?)population2;
                                    if (__agg2Input.HasValue)
                                    {
                                        var __agg2Current = __agg2Input.GetValueOrDefault();
                                        group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                        group.__agg2.HasValue = true;
                                    }
                                }

                                string name = ko3iko.Name;
                                if ((string)name != null)
                                {
                                    group.__agg3.Count = checked(group.__agg3.Count + 1L);
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
                                if (group == null)
                                {
                                    group = new ResultAggregateGroup();
                                    groupsToFinalize.Add(group);
                                }

                                decimal population = ko3iko.Population;
                                {
                                    var __agg0Input = (decimal?)population;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        if (!group.__agg0.HasValue || __agg0Current > group.__agg0.Value)
                                        {
                                            group.__agg0.Value = __agg0Current;
                                        }

                                        group.__agg0.HasValue = true;
                                    }
                                }

                                decimal population1 = ko3iko.Population;
                                {
                                    var __agg1Input = (decimal?)population1;
                                    if (__agg1Input.HasValue)
                                    {
                                        var __agg1Current = __agg1Input.GetValueOrDefault();
                                        if (!group.__agg1.HasValue || __agg1Current < group.__agg1.Value)
                                        {
                                            group.__agg1.Value = __agg1Current;
                                        }

                                        group.__agg1.HasValue = true;
                                    }
                                }

                                decimal population2 = ko3iko.Population;
                                {
                                    var __agg2Input = (decimal?)population2;
                                    if (__agg2Input.HasValue)
                                    {
                                        var __agg2Current = __agg2Input.GetValueOrDefault();
                                        group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                        group.__agg2.HasValue = true;
                                    }
                                }

                                string name = ko3iko.Name;
                                if ((string)name != null)
                                {
                                    group.__agg3.Count = checked(group.__agg3.Count + 1L);
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
                        if (group == null)
                        {
                            group = new ResultAggregateGroup();
                            groupsToFinalize.Add(group);
                        }

                        decimal population = ko3iko.Population;
                        {
                            var __agg0Input = (decimal?)population;
                            if (__agg0Input.HasValue)
                            {
                                var __agg0Current = __agg0Input.GetValueOrDefault();
                                if (!group.__agg0.HasValue || __agg0Current > group.__agg0.Value)
                                {
                                    group.__agg0.Value = __agg0Current;
                                }

                                group.__agg0.HasValue = true;
                            }
                        }

                        decimal population1 = ko3iko.Population;
                        {
                            var __agg1Input = (decimal?)population1;
                            if (__agg1Input.HasValue)
                            {
                                var __agg1Current = __agg1Input.GetValueOrDefault();
                                if (!group.__agg1.HasValue || __agg1Current < group.__agg1.Value)
                                {
                                    group.__agg1.Value = __agg1Current;
                                }

                                group.__agg1.HasValue = true;
                            }
                        }

                        decimal population2 = ko3iko.Population;
                        {
                            var __agg2Input = (decimal?)population2;
                            if (__agg2Input.HasValue)
                            {
                                var __agg2Current = __agg2Input.GetValueOrDefault();
                                group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                group.__agg2.HasValue = true;
                            }
                        }

                        string name = ko3iko.Name;
                        if ((string)name != null)
                        {
                            group.__agg3.Count = checked(group.__agg3.Count + 1L);
                        }
                    }
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    __musoqFinalShapeRows.Add(new ResultShape0(finalGroup.__agg3.Count, finalGroup.__agg2.HasValue ? (decimal?)finalGroup.__agg2.Value : null, finalGroup.__agg1.HasValue ? (decimal?)finalGroup.__agg1.Value : null, finalGroup.__agg0.HasValue ? (decimal?)finalGroup.__agg0.Value : null));
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
            public Musoq.Plugins.MaxAggregateKernel<decimal>.State __agg0;
            public Musoq.Plugins.MinAggregateKernel<decimal>.State __agg1;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg2;
            public Musoq.Plugins.CountReferenceAggregateKernel<string>.State __agg3;
            public ResultAggregateGroup()
            {
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.MaxAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.MinAggregateKernel<decimal>.Merge(ref this.__agg1, in source.__agg1);
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg2, in source.__agg2);
                Musoq.Plugins.CountReferenceAggregateKernel<string>.Merge(ref this.__agg3, in source.__agg3);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(long __value0, decimal? __value1, decimal? __value2, decimal? __value3)
            {
                Count_Name_ = __value0;
                Sum_Population_ = __value1;
                Min_Population_ = __value2;
                Max_Population_ = __value3;
            }

            public override int Count => 4;
            public long Count_Name_ { get; private set; }
            public decimal? Max_Population_ { get; private set; }
            public decimal? Min_Population_ { get; private set; }
            public decimal? Sum_Population_ { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Count_Name_ = (long)value;
                        break;
                    case 1:
                        Sum_Population_ = (decimal?)value;
                        break;
                    case 2:
                        Min_Population_ = (decimal?)value;
                        break;
                    case 3:
                        Max_Population_ = (decimal?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Count(Name)" => true,
                "Count_Name_" => true,
                "Sum(Population)" => true,
                "Sum_Population_" => true,
                "Min(Population)" => true,
                "Min_Population_" => true,
                "Max(Population)" => true,
                "Max_Population_" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Count_Name_,
                1 => (object)Sum_Population_,
                2 => (object)Min_Population_,
                3 => (object)Max_Population_,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Count(Name)" => (object)Count_Name_,
                "Count_Name_" => (object)Count_Name_,
                "Sum(Population)" => (object)Sum_Population_,
                "Sum_Population_" => (object)Sum_Population_,
                "Min(Population)" => (object)Min_Population_,
                "Min_Population_" => (object)Min_Population_,
                "Max(Population)" => (object)Max_Population_,
                "Max_Population_" => (object)Max_Population_,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(long Count_Name_, decimal? Sum_Population_, decimal? Min_Population_, decimal? Max_Population_)
            {
                this.Count_Name_ = Count_Name_;
                this.Sum_Population_ = Sum_Population_;
                this.Min_Population_ = Min_Population_;
                this.Max_Population_ = Max_Population_;
            }

            public long Count_Name_ { get; }
            public decimal? Max_Population_ { get; }
            public decimal? Min_Population_ { get; }
            public decimal? Sum_Population_ { get; }
        }
    }
}
