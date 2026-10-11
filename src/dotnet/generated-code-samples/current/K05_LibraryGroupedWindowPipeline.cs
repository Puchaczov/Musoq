// === Parsed Query ===
/*
with totals as (
    select Partition, Sum(Amount) as Total from #schema.first() group by Partition
)
select Partition, Total,
       Sum(Total) over (order by Partition) as RunningTotal
from totals order by Partition
*/

// === Logical Plan ===
/*
Cte
  Definition [totals]
    MultiStatement
      Project [ko3iko.Partition as ko3iko.Partition, AggRef(ko3iko.Sum(ko3iko.Amount)) as ko3iko.Sum(ko3iko.Amount)]
        Aggregate [keys: Partition] [aggs: Sum(Amount)]
          SchemaScan [#schema.first() as ko3iko]
      Project [ko3iko.Partition as Partition, ko3iko.Sum(ko3iko.Amount) as Total]
        CteRef [ko3ikoScore as ko3ikoScore]
  Query
    MultiStatement
      Sort [totals.Partition]
        Project [totals.Partition as Partition, totals.Total as Total, WindowRef(0) as RunningTotal]
          Window [Sum(idx:0; order: totals.Partition; args: totals.Total)]
            CteRef [totals as totals]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [totals]
    PhysicalMultiStatement
      PhysicalProject [ko3iko.Partition as ko3iko.Partition, AggRef(ko3iko.Sum(ko3iko.Amount)) as ko3iko.Sum(ko3iko.Amount)]
        PhysicalSingleKeyAggregate [key: Partition (String)] [aggs: Sum(Amount)]
          PhysicalSchemaScan [#schema.first() as ko3iko]
      PhysicalProject [ko3iko.Partition as Partition, ko3iko.Sum(ko3iko.Amount) as Total]
        PhysicalCteRef [ko3ikoScore as ko3ikoScore]
  Query
    PhysicalMultiStatement
      PhysicalSort [totals.Partition]
        PhysicalProject [totals.Partition as Partition, totals.Total as Total, WindowRef(0) as RunningTotal]
          PhysicalWindow [Sum(idx:0; order: totals.Partition; args: totals.Total)]
            PhysicalMaterialize
              PhysicalCteRef [totals as totals]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: LibraryExecutionEntity]
      Partition: string <- property Partition
      Amount: decimal? <- property Amount
    AggregateGroup [Cte0AggregateGroup; keys: 1; typed aggs: 1]
    Generated [Cte0Row0]
      Partition: string <- field Partition
      Total: decimal? <- field Total
    TableRow [totals]
      Partition: string <- field Partition
      Total: decimal? <- field Total
    Generated [ResultRow0]
      Partition: string <- field Partition
      Total: decimal? <- field Total
      RunningTotal: decimal? <- field RunningTotal

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [From:cte0]
    SourceScan [ko3iko: LibraryExecutionEntity] -> cte0_ko3ikoRows
    CreateTable [cte0: Cte0Row0]
    PhaseBoundary [GroupBy:cte0]
    CreateSingleKeyAggregateContext [cte0Groups: string -> Cte0AggregateGroup]
    ChunkedForEach [ko3iko in cte0_ko3ikoRows]
      GetOrAddSingleKeyAggregateGroup [cte0Group = cte0Groups[ko3iko.Partition] by Partition; typed: Cte0AggregateGroup]
      Let [amount: decimal? = ko3iko.Amount]
      TypedAggregateSet [Set(cte0Group.__agg0, amount)]
    EnsureCapacity [cte0 <- cte0GroupsToFinalize.Count]
    PhaseBoundary [Select:cte0]
    ForEach [cte0FinalGroup in cte0GroupsToFinalize]
      AppendRow [cte0 <- Cte0Row0(Partition: cte0FinalGroup.Partition, Total: ko3iko.Sum(ko3iko.Amount))]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    Materialize [_cteRowResults.Slot0 -> resultWindowRows]
    ComputeSumWindowKernel[BoundedRows] [resultSums <- resultWindowRows value totals.Total order by totals.Partition ASC frame range between unbounded preceding and current row]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, totals in resultWindowRows]
      AppendShape [result <- ResultShape0(Partition: totals.Partition, Total: totals.Total, RunningTotal: resultSums[windowIndex])]
    SortShapeRows [result -> resultSorted by Partition ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K05_LibraryGroupedWindowPipeline
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
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("Partition", typeof(string), 0),
            new Column("Total", typeof(decimal?), 1)
        };
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Partition", typeof(string), 0),
            new Column("Total", typeof(decimal?), 1),
            new Column("RunningTotal", typeof(decimal?), 2)
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Partition, __musoqShapeRow.Total, __musoqShapeRow.RunningTotal);
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
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var resultWindowRows = EvaluationHelper.MaterializeGeneratedRows<Cte0Row0>(_cteRowResults.Slot0);
                var resultSumsOrderKeys = new WindowResultSumsOrderKeysKey[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Cte0Row0 totals = resultWindowRows[windowIndex];
                    resultSumsOrderKeys[windowIndex] = new WindowResultSumsOrderKeysKey(totals.Partition);
                }

                var resultSumsPartitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultSumsPartitions, resultSumsOrderKeys, false);
                var resultSums = new decimal? [resultWindowRows.Count];
                for (int resultSumsPartitionSetIndex = 0; resultSumsPartitionSetIndex < resultSumsPartitions.PartitionCount; ++resultSumsPartitionSetIndex)
                {
                    var resultSumsPartitionStart = resultSumsPartitions.GetStart(resultSumsPartitionSetIndex);
                    var resultSumsPartitionCount = resultSumsPartitions.GetLength(resultSumsPartitionSetIndex);
                    var resultSumsPartitionIndices = resultSumsPartitions.Indices;
                    Musoq.Plugins.SumAggregateKernel<decimal>.State resultSumsState = default(Musoq.Plugins.SumAggregateKernel<decimal>.State)!;
                    Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref resultSumsState);
                    for (int resultSumsPartitionIndex = 0; resultSumsPartitionIndex < resultSumsPartitionCount;)
                    {
                        var resultSumsPeerEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultSumsOrderKeys, resultSumsPartitionIndices, resultSumsPartitionStart, resultSumsPartitionCount, resultSumsPartitionIndex);
                        for (int resultSumsPeerIndex = resultSumsPartitionIndex; resultSumsPeerIndex <= resultSumsPeerEnd; ++resultSumsPeerIndex)
                        {
                            var resultSumsCurrentIndex = resultSumsPartitionIndices[resultSumsPartitionStart + resultSumsPeerIndex];
                            Cte0Row0 totals = resultWindowRows[resultSumsCurrentIndex];
                            Musoq.Plugins.SumAggregateKernel<decimal>.Set(ref resultSumsState, (decimal?)totals.Total);
                        }

                        var resultSumsPeerValue = Musoq.Plugins.SumAggregateKernel<decimal>.Get(in resultSumsState);
                        for (int resultSumsPeerIndex = resultSumsPartitionIndex; resultSumsPeerIndex <= resultSumsPeerEnd; ++resultSumsPeerIndex)
                        {
                            var resultSumsCurrentIndex = resultSumsPartitionIndices[resultSumsPartitionStart + resultSumsPeerIndex];
                            resultSums[resultSumsCurrentIndex] = resultSumsPeerValue;
                        }

                        resultSumsPartitionIndex = resultSumsPeerEnd + 1;
                    }
                }

                var result = new List<ResultShape0>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 totals = resultWindowRows[windowIndex];
                    result.Add(new ResultShape0(totals.Partition, totals.Total, (decimal?)resultSums[windowIndex]));
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                var __cte0_ko3ikoSchema = provider.GetSchema("#schema");
                var cte0_ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__cte0_ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var cte0_ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(cte0_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : cte0_ko3ikoRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                var cte0GroupsToFinalize = new List<Cte0AggregateGroup>();
                var cte0Groups = new Dictionary<string, Cte0AggregateGroup>();
                Cte0AggregateGroup cte0NullGroup = null;
                foreach (var ko3ikoChunk in cte0_ko3ikoRows)
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
                                Cte0AggregateGroup cte0Group = null;
                                if (groupKey != null)
                                {
                                    ref var cte0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Groups, groupKey, out var cte0GroupExists);
                                    if (!cte0GroupExists)
                                    {
                                        cte0GroupRef = new Cte0AggregateGroup(groupKey);
                                        cte0GroupsToFinalize.Add(cte0GroupRef);
                                    }

                                    cte0Group = cte0GroupRef;
                                }
                                else
                                {
                                    if (cte0NullGroup == null)
                                    {
                                        cte0NullGroup = new Cte0AggregateGroup(null);
                                        cte0GroupsToFinalize.Add(cte0NullGroup);
                                    }

                                    cte0Group = cte0NullGroup;
                                }

                                decimal? amount = ko3iko.Amount;
                                {
                                    var __agg0Input = (decimal?)amount;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        cte0Group.__agg0.Value = cte0Group.__agg0.HasValue ? checked(cte0Group.__agg0.Value + __agg0Current) : __agg0Current;
                                        cte0Group.__agg0.HasValue = true;
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
                                Cte0AggregateGroup cte0Group = null;
                                if (groupKey != null)
                                {
                                    ref var cte0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Groups, groupKey, out var cte0GroupExists);
                                    if (!cte0GroupExists)
                                    {
                                        cte0GroupRef = new Cte0AggregateGroup(groupKey);
                                        cte0GroupsToFinalize.Add(cte0GroupRef);
                                    }

                                    cte0Group = cte0GroupRef;
                                }
                                else
                                {
                                    if (cte0NullGroup == null)
                                    {
                                        cte0NullGroup = new Cte0AggregateGroup(null);
                                        cte0GroupsToFinalize.Add(cte0NullGroup);
                                    }

                                    cte0Group = cte0NullGroup;
                                }

                                decimal? amount = ko3iko.Amount;
                                {
                                    var __agg0Input = (decimal?)amount;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        cte0Group.__agg0.Value = cte0Group.__agg0.HasValue ? checked(cte0Group.__agg0.Value + __agg0Current) : __agg0Current;
                                        cte0Group.__agg0.HasValue = true;
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
                        Cte0AggregateGroup cte0Group = null;
                        if (groupKey != null)
                        {
                            ref var cte0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Groups, groupKey, out var cte0GroupExists);
                            if (!cte0GroupExists)
                            {
                                cte0GroupRef = new Cte0AggregateGroup(groupKey);
                                cte0GroupsToFinalize.Add(cte0GroupRef);
                            }

                            cte0Group = cte0GroupRef;
                        }
                        else
                        {
                            if (cte0NullGroup == null)
                            {
                                cte0NullGroup = new Cte0AggregateGroup(null);
                                cte0GroupsToFinalize.Add(cte0NullGroup);
                            }

                            cte0Group = cte0NullGroup;
                        }

                        decimal? amount = ko3iko.Amount;
                        {
                            var __agg0Input = (decimal?)amount;
                            if (__agg0Input.HasValue)
                            {
                                var __agg0Current = __agg0Input.GetValueOrDefault();
                                cte0Group.__agg0.Value = cte0Group.__agg0.HasValue ? checked(cte0Group.__agg0.Value + __agg0Current) : __agg0Current;
                                cte0Group.__agg0.HasValue = true;
                            }
                        }
                    }
                }

                cte0.EnsureCapacity(cte0GroupsToFinalize.Count);
                foreach (var cte0FinalGroup in cte0GroupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    cte0.Add(new Cte0Row0(cte0FinalGroup.__key0, cte0FinalGroup.__agg0.HasValue ? (decimal?)cte0FinalGroup.__agg0.Value : null));
                }

                return cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        private sealed class Cte0AggregateGroup
        {
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg0;
            public readonly string __key0;
            public Cte0AggregateGroup(string __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg0);
            }

            public void MergeFrom(Cte0AggregateGroup source)
            {
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
            }
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(string __value0, decimal? __value1)
            {
                Partition = __value0;
                Total = __value1;
            }

            public string Partition { get; }
            public decimal? Total { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, decimal? __value1, decimal? __value2)
            {
                Partition = __value0;
                Total = __value1;
                RunningTotal = __value2;
            }

            public override int Count => 3;
            public string Partition { get; private set; }
            public decimal? RunningTotal { get; private set; }
            public decimal? Total { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Partition = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.RunningTotal = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Partition),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.RunningTotal),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Partition" => true,
                "Total" => true,
                "RunningTotal" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Partition, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.RunningTotal, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Partition, decimal? Total, decimal? RunningTotal)
            {
                this.Partition = Partition;
                this.Total = Total;
                this.RunningTotal = RunningTotal;
            }

            public string Partition { get; }
            public decimal? RunningTotal { get; }
            public decimal? Total { get; }
        }

        private readonly struct WindowResultSumsOrderKeysKey : System.IEquatable<WindowResultSumsOrderKeysKey>, System.IComparable<WindowResultSumsOrderKeysKey>
        {
            private readonly string _value0;
            public WindowResultSumsOrderKeysKey(string value0)
            {
                _value0 = value0;
            }

            public string RawValues => _value0;

            public int CompareTo(WindowResultSumsOrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultSumsOrderKeysKey other)
            {
                return System.String.Equals(_value0, other._value0, System.StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultSumsOrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0, System.StringComparer.Ordinal);
                return hash.ToHashCode();
            }

            private static int CompareValue0(string left, string right)
            {
                if (left == null)
                    return right == null ? 0 : -1;
                if (right == null)
                    return 1;
                var comparison = System.String.CompareOrdinal(left, right);
                return comparison;
            }
        }
    }
}
