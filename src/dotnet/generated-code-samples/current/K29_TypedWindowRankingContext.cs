// === Parsed Query ===
/*
select Position, RowNumber() over (partition by Partition order by Integral) as PositionRank,
    Rank() over (partition by Partition order by Integral) as Rank,
    DenseRank() over (partition by Partition order by Integral) as Dense,
    PercentRank() over (partition by Partition order by Integral) as Percent,
    CumeDist() over (partition by Partition order by Integral) as Distribution
from #schema.first() order by Position
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [ko3iko.Position]
    Project [ko3iko.Position as Position, WindowRef(0) as PositionRank, WindowRef(1) as Rank, WindowRef(2) as Dense, WindowRef(3) as Percent, WindowRef(4) as Distribution]
      Window [RowNumber(idx:0; partition: ko3iko.Partition; order: ko3iko.Integral), Rank(idx:1; partition: ko3iko.Partition; order: ko3iko.Integral), DenseRank(idx:2; partition: ko3iko.Partition; order: ko3iko.Integral), PercentRank(idx:3; partition: ko3iko.Partition; order: ko3iko.Integral), CumeDist(idx:4; partition: ko3iko.Partition; order: ko3iko.Integral)]
        SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [ko3iko.Position]
    PhysicalProject [ko3iko.Position as Position, WindowRef(0) as PositionRank, WindowRef(1) as Rank, WindowRef(2) as Dense, WindowRef(3) as Percent, WindowRef(4) as Distribution]
      PhysicalWindow [RowNumber(idx:0; partition: ko3iko.Partition; order: ko3iko.Integral), Rank(idx:1; partition: ko3iko.Partition; order: ko3iko.Integral), DenseRank(idx:2; partition: ko3iko.Partition; order: ko3iko.Integral), PercentRank(idx:3; partition: ko3iko.Partition; order: ko3iko.Integral), CumeDist(idx:4; partition: ko3iko.Partition; order: ko3iko.Integral)]
        PhysicalMaterialize
          PhysicalSchemaScan [#schema.first() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: LibraryExecutionEntity]
      Position: int <- property Position
      Partition: string <- property Partition
      Integral: int? <- property Integral
    Generated [ResultRow0]
      Position: int <- field Position
      PositionRank: long <- field PositionRank
      Rank: long <- field Rank
      Dense: long <- field Dense
      Percent: double <- field Percent
      Distribution: double <- field Distribution

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: LibraryExecutionEntity] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    WindowKernelPlan [hash partition/per-partition sort; kernels 5; ranking|resultWindowRows|resultRowNumbers0Partitions|resultRowNumbers0Partitions|resultRowNumbers0PartitionKeys|resultRowNumbers0OrderKeys]
      ComputeRowNumberWindow [resultRowNumbers0 <- resultWindowRows partition by ko3iko.Partition order by ko3iko.Integral ASC]
      ComputeRankWindow [resultRanks1 <- resultWindowRows partition by ko3iko.Partition order by ko3iko.Integral ASC]
      ComputeDenseRankWindow [resultDenseRanks2 <- resultWindowRows partition by ko3iko.Partition order by ko3iko.Integral ASC]
      ComputePercentRankWindow [resultPercentRanks3 <- resultWindowRows partition by ko3iko.Partition order by ko3iko.Integral ASC]
      ComputeCumeDistWindow [resultCumeDists4 <- resultWindowRows partition by ko3iko.Partition order by ko3iko.Integral ASC]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultShape0(Position: ko3iko.Position, PositionRank: resultRowNumbers0[windowIndex], Rank: resultRanks1[windowIndex], Dense: resultDenseRanks2[windowIndex], Percent: resultPercentRanks3[windowIndex], Distribution: resultCumeDists4[windowIndex])]
    SortShapeRows [result -> resultSorted by Position ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K29_TypedWindowRankingContext
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
            new Column("Position", typeof(int), 0),
            new Column("PositionRank", typeof(long), 1),
            new Column("Rank", typeof(long), 2),
            new Column("Dense", typeof(long), 3),
            new Column("Percent", typeof(double), 4),
            new Column("Distribution", typeof(double), 5)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Partition", typeof(string), 1), new Column("Integral", typeof(int?), 2) });
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
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.PositionRank, __musoqShapeRow.Rank, __musoqShapeRow.Dense, __musoqShapeRow.Percent, __musoqShapeRow.Distribution);
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
                var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                var resultRowNumbers0PartitionKeys = new string[resultWindowRows.Count];
                var resultRowNumbers0OrderKeys = new WindowResultRowNumbers0OrderKeysKey[resultWindowRows.Count];
                ExtractResultRowNumbers0WindowKeys(resultWindowRows, resultRowNumbers0PartitionKeys, resultRowNumbers0OrderKeys);
                var resultRowNumbers0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count, resultRowNumbers0PartitionKeys);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultRowNumbers0Partitions, resultRowNumbers0OrderKeys, false);
                var resultRowNumbers0 = new long[resultWindowRows.Count];
                var resultRanks1 = new long[resultWindowRows.Count];
                var resultDenseRanks2 = new long[resultWindowRows.Count];
                var resultPercentRanks3 = new double[resultWindowRows.Count];
                var resultCumeDists4 = new double[resultWindowRows.Count];
                for (int resultRowNumbers0WindowPlanPartitionSetIndex = 0; resultRowNumbers0WindowPlanPartitionSetIndex < resultRowNumbers0Partitions.PartitionCount; ++resultRowNumbers0WindowPlanPartitionSetIndex)
                {
                    var resultRowNumbers0WindowPlanPartitionStart = resultRowNumbers0Partitions.GetStart(resultRowNumbers0WindowPlanPartitionSetIndex);
                    var resultRowNumbers0WindowPlanPartitionCount = resultRowNumbers0Partitions.GetLength(resultRowNumbers0WindowPlanPartitionSetIndex);
                    var resultRowNumbers0WindowPlanPartitionIndices = resultRowNumbers0Partitions.Indices;
                    long resultRowNumbers0DenseRank = 1L;
                    for (int resultRowNumbers0PeerStart = 0; resultRowNumbers0PeerStart < resultRowNumbers0WindowPlanPartitionCount;)
                    {
                        var resultRowNumbers0CurrentIndex = resultRowNumbers0WindowPlanPartitionIndices[resultRowNumbers0WindowPlanPartitionStart + resultRowNumbers0PeerStart];
                        var resultRowNumbers0PeerEnd = resultRowNumbers0PeerStart;
                        while (resultRowNumbers0PeerEnd + 1 < resultRowNumbers0WindowPlanPartitionCount)
                        {
                            var resultRowNumbers0CandidateIndex = resultRowNumbers0WindowPlanPartitionIndices[resultRowNumbers0WindowPlanPartitionStart + resultRowNumbers0PeerEnd + 1];
                            if (!resultRowNumbers0OrderKeys[resultRowNumbers0CurrentIndex].PeerEquals(resultRowNumbers0OrderKeys[resultRowNumbers0CandidateIndex]))
                                break;
                            resultRowNumbers0PeerEnd++;
                        }

                        for (int resultRowNumbers0PeerIndex = resultRowNumbers0PeerStart; resultRowNumbers0PeerIndex <= resultRowNumbers0PeerEnd; ++resultRowNumbers0PeerIndex)
                        {
                            resultRowNumbers0CurrentIndex = resultRowNumbers0WindowPlanPartitionIndices[resultRowNumbers0WindowPlanPartitionStart + resultRowNumbers0PeerIndex];
                            var resultRowNumbers0Context = new Musoq.Plugins.WindowRankingContext(resultRowNumbers0PeerIndex, resultRowNumbers0PeerStart, resultRowNumbers0PeerEnd, resultRowNumbers0DenseRank, resultRowNumbers0WindowPlanPartitionCount, token);
                            resultRowNumbers0[resultRowNumbers0CurrentIndex] = Musoq.Plugins.RowNumberKernel.Get(in resultRowNumbers0Context);
                            var resultRanks1Context = new Musoq.Plugins.WindowRankingContext(resultRowNumbers0PeerIndex, resultRowNumbers0PeerStart, resultRowNumbers0PeerEnd, resultRowNumbers0DenseRank, resultRowNumbers0WindowPlanPartitionCount, token);
                            resultRanks1[resultRowNumbers0CurrentIndex] = Musoq.Plugins.RankKernel.Get(in resultRanks1Context);
                            var resultDenseRanks2Context = new Musoq.Plugins.WindowRankingContext(resultRowNumbers0PeerIndex, resultRowNumbers0PeerStart, resultRowNumbers0PeerEnd, resultRowNumbers0DenseRank, resultRowNumbers0WindowPlanPartitionCount, token);
                            resultDenseRanks2[resultRowNumbers0CurrentIndex] = Musoq.Plugins.DenseRankKernel.Get(in resultDenseRanks2Context);
                            var resultPercentRanks3Context = new Musoq.Plugins.WindowRankingContext(resultRowNumbers0PeerIndex, resultRowNumbers0PeerStart, resultRowNumbers0PeerEnd, resultRowNumbers0DenseRank, resultRowNumbers0WindowPlanPartitionCount, token);
                            resultPercentRanks3[resultRowNumbers0CurrentIndex] = Musoq.Plugins.PercentRankKernel.Get(in resultPercentRanks3Context);
                            var resultCumeDists4Context = new Musoq.Plugins.WindowRankingContext(resultRowNumbers0PeerIndex, resultRowNumbers0PeerStart, resultRowNumbers0PeerEnd, resultRowNumbers0DenseRank, resultRowNumbers0WindowPlanPartitionCount, token);
                            resultCumeDists4[resultRowNumbers0CurrentIndex] = Musoq.Plugins.CumeDistKernel.Get(in resultCumeDists4Context);
                        }

                        resultRowNumbers0DenseRank++;
                        resultRowNumbers0PeerStart = resultRowNumbers0PeerEnd + 1;
                    }
                }

                var result = new List<ResultShape0>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                    result.Add(new ResultShape0(ko3iko.Position, (long)resultRowNumbers0[windowIndex], (long)resultRanks1[windowIndex], (long)resultDenseRanks2[windowIndex], (double)resultPercentRanks3[windowIndex], (double)resultCumeDists4[windowIndex]));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Position.CompareTo(right.Position);
                    if (comparison != 0)
                        return comparison;
                    return 0;
                }));
                foreach (var resultSortedRowsRow in resultSortedRows)
                {
                    __musoqFinalShapeRows.Add(resultSortedRowsRow);
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
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
        private static void ExtractResultRowNumbers0WindowKeys(IReadOnlyList<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> resultWindowRows, string[] resultRowNumbers0PartitionKeys, WindowResultRowNumbers0OrderKeysKey[] resultRowNumbers0OrderKeys)
        {
            for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
            {
                Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                resultRowNumbers0PartitionKeys[windowIndex] = (string)ko3iko.Partition;
                resultRowNumbers0OrderKeys[windowIndex] = new WindowResultRowNumbers0OrderKeysKey(ko3iko.Integral);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, long __value1, long __value2, long __value3, double __value4, double __value5)
            {
                Position = __value0;
                PositionRank = __value1;
                Rank = __value2;
                Dense = __value3;
                Percent = __value4;
                Distribution = __value5;
            }

            public override int Count => 6;
            public long Dense { get; private set; }
            public double Distribution { get; private set; }
            public double Percent { get; private set; }
            public int Position { get; private set; }
            public long PositionRank { get; private set; }
            public long Rank { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.PositionRank = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Rank = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Dense = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double>(this.Percent = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<double, T>(value)),
                    5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double>(this.Distribution = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<double, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.PositionRank),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Rank),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Dense),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double>(this.Percent),
                5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double>(this.Distribution),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "PositionRank" => true,
                "Rank" => true,
                "Dense" => true,
                "Percent" => true,
                "Distribution" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.PositionRank, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Rank, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Dense, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.Percent, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(5, this.Distribution, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Position, long PositionRank, long Rank, long Dense, double Percent, double Distribution)
            {
                this.Position = Position;
                this.PositionRank = PositionRank;
                this.Rank = Rank;
                this.Dense = Dense;
                this.Percent = Percent;
                this.Distribution = Distribution;
            }

            public long Dense { get; }
            public double Distribution { get; }
            public double Percent { get; }
            public int Position { get; }
            public long PositionRank { get; }
            public long Rank { get; }
        }

        private readonly struct WindowResultRowNumbers0OrderKeysKey : System.IEquatable<WindowResultRowNumbers0OrderKeysKey>, System.IComparable<WindowResultRowNumbers0OrderKeysKey>
        {
            private readonly int? _value0;
            public WindowResultRowNumbers0OrderKeysKey(int? value0)
            {
                _value0 = value0;
            }

            public int? RawValues => _value0;

            public int CompareTo(WindowResultRowNumbers0OrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool PeerEquals(WindowResultRowNumbers0OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int?>.Default.Equals(_value0, other._value0);
            }

            public bool Equals(WindowResultRowNumbers0OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int?>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultRowNumbers0OrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0);
                return hash.ToHashCode();
            }

            private static int CompareValue0(int? left, int? right)
            {
                if (!left.HasValue)
                    return !right.HasValue ? 0 : -1;
                if (!right.HasValue)
                    return 1;
                var comparison = left.Value.CompareTo(right.Value);
                return comparison;
            }
        }
    }
}
