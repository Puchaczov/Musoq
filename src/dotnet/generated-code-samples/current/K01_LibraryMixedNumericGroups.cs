// === Parsed Query ===
/*
select Partition, Sum(Integral) as Integers, Sum(Amount) as Decimals,
       Sum(Floating) as Doubles, Count(Amount) as Values
from #schema.first() group by Partition order by Partition
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Partition as ko3iko.Partition, AggRef(ko3iko.Count(ko3iko.Amount)) as ko3iko.Count(ko3iko.Amount), AggRef(ko3iko.Sum(ko3iko.Floating)) as ko3iko.Sum(ko3iko.Floating), AggRef(ko3iko.Sum(ko3iko.Amount)) as ko3iko.Sum(ko3iko.Amount), AggRef(ko3iko.Sum(ko3iko.Integral)) as ko3iko.Sum(ko3iko.Integral)]
    Aggregate [keys: Partition] [aggs: Count(Amount), Sum(Floating), Sum(Amount), Sum(Integral)]
      SchemaScan [#schema.first() as ko3iko]
  Sort [ko3iko.Partition]
    Project [ko3iko.Partition as Partition, ko3iko.Sum(ko3iko.Integral) as Integers, ko3iko.Sum(ko3iko.Amount) as Decimals, ko3iko.Sum(ko3iko.Floating) as Doubles, ko3iko.Count(ko3iko.Amount) as Values]
      CteRef [ko3ikoScore as ko3ikoScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Partition as ko3iko.Partition, AggRef(ko3iko.Count(ko3iko.Amount)) as ko3iko.Count(ko3iko.Amount), AggRef(ko3iko.Sum(ko3iko.Floating)) as ko3iko.Sum(ko3iko.Floating), AggRef(ko3iko.Sum(ko3iko.Amount)) as ko3iko.Sum(ko3iko.Amount), AggRef(ko3iko.Sum(ko3iko.Integral)) as ko3iko.Sum(ko3iko.Integral)]
    PhysicalSingleKeyAggregate [key: Partition (String)] [aggs: Count(Amount), Sum(Floating), Sum(Amount), Sum(Integral)]
      PhysicalSchemaScan [#schema.first() as ko3iko]
  PhysicalSort [ko3iko.Partition]
    PhysicalProject [ko3iko.Partition as Partition, ko3iko.Sum(ko3iko.Integral) as Integers, ko3iko.Sum(ko3iko.Amount) as Decimals, ko3iko.Sum(ko3iko.Floating) as Doubles, ko3iko.Count(ko3iko.Amount) as Values]
      PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: LibraryExecutionEntity]
      Partition: string <- property Partition
      Integral: int? <- property Integral
      Amount: decimal? <- property Amount
      Floating: double? <- property Floating
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 4]
    Generated [ResultRow0]
      Partition: string <- field Partition
      Integers: int? <- field Integers
      Decimals: decimal? <- field Decimals
      Doubles: double? <- field Doubles
      Values: long <- field Values

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: LibraryExecutionEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [groups: string -> ResultAggregateGroup]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      GetOrAddSingleKeyAggregateGroup [group = groups[ko3iko.Partition] by Partition; typed: ResultAggregateGroup]
      Let [amount: decimal? = ko3iko.Amount]
      TypedAggregateSet [Set(group.__agg0, amount)]
      Let [floating: double? = ko3iko.Floating]
      TypedAggregateSet [Set(group.__agg1, floating)]
      Let [amount1: decimal? = ko3iko.Amount]
      TypedAggregateSet [Set(group.__agg2, amount1)]
      Let [integral: int? = ko3iko.Integral]
      TypedAggregateSet [Set(group.__agg3, integral)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(Partition: finalGroup.Partition, Integers: ko3iko.Sum(ko3iko.Integral), Decimals: ko3iko.Sum(ko3iko.Amount), Doubles: ko3iko.Sum(ko3iko.Floating), Values: ko3iko.Count(ko3iko.Amount))]
    SortOwnedBuffer [result by Partition ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K01_LibraryMixedNumericGroups
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
            new Column("Partition", typeof(string), 0),
            new Column("Integers", typeof(int?), 1),
            new Column("Decimals", typeof(decimal?), 2),
            new Column("Doubles", typeof(double?), 3),
            new Column("Values", typeof(long), 4)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Partition", typeof(string), 0), new Column("Integral", typeof(int?), 1), new Column("Amount", typeof(decimal?), 2), new Column("Floating", typeof(double?), 3) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Partition, __musoqShapeRow.Integers, __musoqShapeRow.Decimals, __musoqShapeRow.Doubles, __musoqShapeRow.Values);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#schema");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<string, ResultAggregateGroup>();
                ResultAggregateGroup nullGroup = null;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                string groupKey = ko3iko.Partition;
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

                                decimal? amount = ko3iko.Amount;
                                if (((decimal?)amount).HasValue)
                                {
                                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                }

                                double? floating = ko3iko.Floating;
                                {
                                    var __agg1Input = (double?)floating;
                                    if (__agg1Input.HasValue)
                                    {
                                        var __agg1Current = __agg1Input.GetValueOrDefault();
                                        group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                        group.__agg1.HasValue = true;
                                    }
                                }

                                decimal? amount1 = ko3iko.Amount;
                                {
                                    var __agg2Input = (decimal?)amount1;
                                    if (__agg2Input.HasValue)
                                    {
                                        var __agg2Current = __agg2Input.GetValueOrDefault();
                                        group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                        group.__agg2.HasValue = true;
                                    }
                                }

                                int? integral = ko3iko.Integral;
                                {
                                    var __agg3Input = (int?)integral;
                                    if (__agg3Input.HasValue)
                                    {
                                        var __agg3Current = __agg3Input.GetValueOrDefault();
                                        group.__agg3.Value = group.__agg3.HasValue ? checked(group.__agg3.Value + __agg3Current) : __agg3Current;
                                        group.__agg3.HasValue = true;
                                    }
                                }
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                string groupKey = ko3iko.Partition;
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

                                decimal? amount = ko3iko.Amount;
                                if (((decimal?)amount).HasValue)
                                {
                                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                }

                                double? floating = ko3iko.Floating;
                                {
                                    var __agg1Input = (double?)floating;
                                    if (__agg1Input.HasValue)
                                    {
                                        var __agg1Current = __agg1Input.GetValueOrDefault();
                                        group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                        group.__agg1.HasValue = true;
                                    }
                                }

                                decimal? amount1 = ko3iko.Amount;
                                {
                                    var __agg2Input = (decimal?)amount1;
                                    if (__agg2Input.HasValue)
                                    {
                                        var __agg2Current = __agg2Input.GetValueOrDefault();
                                        group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                        group.__agg2.HasValue = true;
                                    }
                                }

                                int? integral = ko3iko.Integral;
                                {
                                    var __agg3Input = (int?)integral;
                                    if (__agg3Input.HasValue)
                                    {
                                        var __agg3Current = __agg3Input.GetValueOrDefault();
                                        group.__agg3.Value = group.__agg3.HasValue ? checked(group.__agg3.Value + __agg3Current) : __agg3Current;
                                        group.__agg3.HasValue = true;
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
                        string groupKey = ko3iko.Partition;
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

                        decimal? amount = ko3iko.Amount;
                        if (((decimal?)amount).HasValue)
                        {
                            group.__agg0.Count = checked(group.__agg0.Count + 1L);
                        }

                        double? floating = ko3iko.Floating;
                        {
                            var __agg1Input = (double?)floating;
                            if (__agg1Input.HasValue)
                            {
                                var __agg1Current = __agg1Input.GetValueOrDefault();
                                group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                group.__agg1.HasValue = true;
                            }
                        }

                        decimal? amount1 = ko3iko.Amount;
                        {
                            var __agg2Input = (decimal?)amount1;
                            if (__agg2Input.HasValue)
                            {
                                var __agg2Current = __agg2Input.GetValueOrDefault();
                                group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                group.__agg2.HasValue = true;
                            }
                        }

                        int? integral = ko3iko.Integral;
                        {
                            var __agg3Input = (int?)integral;
                            if (__agg3Input.HasValue)
                            {
                                var __agg3Current = __agg3Input.GetValueOrDefault();
                                group.__agg3.Value = group.__agg3.HasValue ? checked(group.__agg3.Value + __agg3Current) : __agg3Current;
                                group.__agg3.HasValue = true;
                            }
                        }
                    }
                }

                result.EnsureCapacity(groupsToFinalize.Count);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    result.Add(new ResultShape0(finalGroup.__key0, finalGroup.__agg3.HasValue ? (int?)finalGroup.__agg3.Value : null, finalGroup.__agg2.HasValue ? (decimal?)finalGroup.__agg2.Value : null, finalGroup.__agg1.HasValue ? (double?)finalGroup.__agg1.Value : null, finalGroup.__agg0.Count) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.Partition, right.Partition);
                    if (comparison != 0)
                        return comparison;
                    return left.__musoqSequence.CompareTo(right.__musoqSequence);
                }));
                return result;
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
            public Musoq.Plugins.CountNullableAggregateKernel<decimal>.State __agg0;
            public Musoq.Plugins.SumAggregateKernel<double>.State __agg1;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg2;
            public Musoq.Plugins.SumAggregateKernel<int>.State __agg3;
            public readonly string __key0;
            public ResultAggregateGroup(string __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.CountNullableAggregateKernel<decimal>.Reset(ref this.__agg0);
                Musoq.Plugins.SumAggregateKernel<double>.Reset(ref this.__agg1);
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg2);
                Musoq.Plugins.SumAggregateKernel<int>.Reset(ref this.__agg3);
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.CountNullableAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.SumAggregateKernel<double>.Merge(ref this.__agg1, in source.__agg1);
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg2, in source.__agg2);
                Musoq.Plugins.SumAggregateKernel<int>.Merge(ref this.__agg3, in source.__agg3);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, int? __value1, decimal? __value2, double? __value3, long __value4)
            {
                Partition = __value0;
                Integers = __value1;
                Decimals = __value2;
                Doubles = __value3;
                Values = __value4;
            }

            public override int Count => 5;
            public decimal? Decimals { get; private set; }
            public double? Doubles { get; private set; }
            public int? Integers { get; private set; }
            public string Partition { get; private set; }
            public long Values { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Partition = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Integers = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Decimals = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double?>(this.Doubles = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<double?, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Values = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Partition),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Integers),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Decimals),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double?>(this.Doubles),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Values),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Partition" => true,
                "Integers" => true,
                "Decimals" => true,
                "Doubles" => true,
                "Values" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Partition, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Integers, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Decimals, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Doubles, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.Values, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Partition, int? Integers, decimal? Decimals, double? Doubles, long Values)
            {
                this.Partition = Partition;
                this.Integers = Integers;
                this.Decimals = Decimals;
                this.Doubles = Doubles;
                this.Values = Values;
            }

            public decimal? Decimals { get; }
            public double? Doubles { get; }
            public int? Integers { get; }
            public string Partition { get; }
            public long Values { get; }
            public int __musoqSequence { get; init; }
        }
    }
}
