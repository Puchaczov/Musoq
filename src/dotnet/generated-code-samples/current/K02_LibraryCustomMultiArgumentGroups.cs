// === Parsed Query ===
/*
select Partition, PositiveTotal(Amount) as Positive,
       ScaledTotal(Amount, 2) as Scaled, Sum(Amount) as Total
from #schema.first() group by Partition order by Partition
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Partition as ko3iko.Partition, AggRef(ko3iko.Sum(ko3iko.Amount)) as ko3iko.Sum(ko3iko.Amount), AggRef(ko3iko.ScaledTotal(ko3iko.Amount, 2)) as ko3iko.ScaledTotal(ko3iko.Amount, 2), AggRef(ko3iko.PositiveTotal(ko3iko.Amount)) as ko3iko.PositiveTotal(ko3iko.Amount)]
    Aggregate [keys: Partition] [aggs: Sum(Amount), ScaledTotal(Amount, 2), PositiveTotal(Amount)]
      SchemaScan [#schema.first() as ko3iko]
  Sort [ko3iko.Partition]
    Project [ko3iko.Partition as Partition, ko3iko.PositiveTotal(ko3iko.Amount) as Positive, ko3iko.ScaledTotal(ko3iko.Amount, 2) as Scaled, ko3iko.Sum(ko3iko.Amount) as Total]
      CteRef [ko3ikoScore as ko3ikoScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Partition as ko3iko.Partition, AggRef(ko3iko.Sum(ko3iko.Amount)) as ko3iko.Sum(ko3iko.Amount), AggRef(ko3iko.ScaledTotal(ko3iko.Amount, 2)) as ko3iko.ScaledTotal(ko3iko.Amount, 2), AggRef(ko3iko.PositiveTotal(ko3iko.Amount)) as ko3iko.PositiveTotal(ko3iko.Amount)]
    PhysicalSingleKeyAggregate [key: Partition (String)] [aggs: Sum(Amount), ScaledTotal(Amount, 2), PositiveTotal(Amount)]
      PhysicalSchemaScan [#schema.first() as ko3iko]
  PhysicalSort [ko3iko.Partition]
    PhysicalProject [ko3iko.Partition as Partition, ko3iko.PositiveTotal(ko3iko.Amount) as Positive, ko3iko.ScaledTotal(ko3iko.Amount, 2) as Scaled, ko3iko.Sum(ko3iko.Amount) as Total]
      PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: LibraryExecutionEntity]
      Partition: string <- property Partition
      Amount: decimal? <- property Amount
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 3]
    Generated [ResultRow0]
      Partition: string <- field Partition
      Positive: decimal? <- field Positive
      Scaled: decimal? <- field Scaled
      Total: decimal? <- field Total

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
      Let [amount1: decimal? = ko3iko.Amount]
      TypedAggregateSet [Set(group.__agg1, (amount1, 2))]
      Let [amount2: decimal? = ko3iko.Amount]
      TypedAggregateSet [Set(group.__agg2, amount2)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(Partition: finalGroup.Partition, Positive: ko3iko.PositiveTotal(ko3iko.Amount), Scaled: ko3iko.ScaledTotal(ko3iko.Amount, 2), Total: ko3iko.Sum(ko3iko.Amount))]
    SortShapeRows [result -> resultSorted by Partition ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K02_LibraryCustomMultiArgumentGroups
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
            new Column("Positive", typeof(decimal?), 1),
            new Column("Scaled", typeof(decimal?), 2),
            new Column("Total", typeof(decimal?), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Partition", typeof(string), 0), new Column("Amount", typeof(decimal?), 1) });
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
                yield return new ResultRow0(__musoqShapeRow.Partition, __musoqShapeRow.Positive, __musoqShapeRow.Scaled, __musoqShapeRow.Total);
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
                var __musoqFinalShapeRows = new List<ResultShape0>();
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
                                {
                                    var __agg0Input = (decimal?)amount;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        group.__agg0.Value = group.__agg0.HasValue ? checked(group.__agg0.Value + __agg0Current) : __agg0Current;
                                        group.__agg0.HasValue = true;
                                    }
                                }

                                decimal? amount1 = ko3iko.Amount;
                                Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.ScaledTotalKernel.Set(ref group.__agg1, (decimal?)amount1, (int)2);
                                decimal? amount2 = ko3iko.Amount;
                                Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.PositiveTotalKernel.Set(ref group.__agg2, (decimal?)amount2);
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
                                {
                                    var __agg0Input = (decimal?)amount;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        group.__agg0.Value = group.__agg0.HasValue ? checked(group.__agg0.Value + __agg0Current) : __agg0Current;
                                        group.__agg0.HasValue = true;
                                    }
                                }

                                decimal? amount1 = ko3iko.Amount;
                                Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.ScaledTotalKernel.Set(ref group.__agg1, (decimal?)amount1, (int)2);
                                decimal? amount2 = ko3iko.Amount;
                                Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.PositiveTotalKernel.Set(ref group.__agg2, (decimal?)amount2);
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
                        {
                            var __agg0Input = (decimal?)amount;
                            if (__agg0Input.HasValue)
                            {
                                var __agg0Current = __agg0Input.GetValueOrDefault();
                                group.__agg0.Value = group.__agg0.HasValue ? checked(group.__agg0.Value + __agg0Current) : __agg0Current;
                                group.__agg0.HasValue = true;
                            }
                        }

                        decimal? amount1 = ko3iko.Amount;
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.ScaledTotalKernel.Set(ref group.__agg1, (decimal?)amount1, (int)2);
                        decimal? amount2 = ko3iko.Amount;
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.PositiveTotalKernel.Set(ref group.__agg2, (decimal?)amount2);
                    }
                }

                result.EnsureCapacity(groupsToFinalize.Count);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    result.Add(new ResultShape0(finalGroup.__key0, Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.PositiveTotalKernel.Get(in finalGroup.__agg2), Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.ScaledTotalKernel.Get(in finalGroup.__agg1), finalGroup.__agg0.HasValue ? (decimal?)finalGroup.__agg0.Value : null));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.Partition, right.Partition);
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
            public Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.ScaledTotalKernel.State __agg1;
            public Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.PositiveTotalKernel.State __agg2;
            public readonly string __key0;
            public ResultAggregateGroup(string __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg0);
                Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.ScaledTotalKernel.Reset(ref this.__agg1);
                Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.PositiveTotalKernel.Reset(ref this.__agg2);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, decimal? __value1, decimal? __value2, decimal? __value3)
            {
                Partition = __value0;
                Positive = __value1;
                Scaled = __value2;
                Total = __value3;
            }

            public override int Count => 4;
            public string Partition { get; private set; }
            public decimal? Positive { get; private set; }
            public decimal? Scaled { get; private set; }
            public decimal? Total { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Partition = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Positive = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Scaled = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Partition),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Positive),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Scaled),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Partition" => true,
                "Positive" => true,
                "Scaled" => true,
                "Total" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Partition, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Positive, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Scaled, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Total, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Partition, decimal? Positive, decimal? Scaled, decimal? Total)
            {
                this.Partition = Partition;
                this.Positive = Positive;
                this.Scaled = Scaled;
                this.Total = Total;
            }

            public string Partition { get; }
            public decimal? Positive { get; }
            public decimal? Scaled { get; }
            public decimal? Total { get; }
        }
    }
}
