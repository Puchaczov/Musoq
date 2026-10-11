// === Parsed Query ===
/*
select Position, RowNumber() over (order by Value) as Ordinal, Rank() over (order by Value) as Rank from #schema.first() order by Position
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [ko3iko.Position]
    Project [ko3iko.Position as Position, WindowRef(0) as Ordinal, WindowRef(1) as Rank]
      Window [RowNumber(idx:0; order: ko3iko.Value), Rank(idx:1; order: ko3iko.Value)]
        SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [ko3iko.Position]
    PhysicalProject [ko3iko.Position as Position, WindowRef(0) as Ordinal, WindowRef(1) as Rank]
      PhysicalWindow [RowNumber(idx:0; order: ko3iko.Value), Rank(idx:1; order: ko3iko.Value)]
        PhysicalMaterialize
          PhysicalSchemaScan [#schema.first() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: NumericRow<int>]
      Position: int <- property Position
      Value: int? <- property Value
    Generated [ResultRow0]
      Position: int <- field Position
      Ordinal: long <- field Ordinal
      Rank: long <- field Rank

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: NumericRow<int>] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    WindowKernelPlan [hash partition/per-partition sort; kernels 2; ranking|resultWindowRows|resultRowNumbers0Partitions|resultRowNumbers0Partitions||resultRowNumbers0OrderKeys]
      ComputeRowNumberWindow [resultRowNumbers0 <- resultWindowRows order by ko3iko.Value ASC]
      ComputeRankWindow [resultRanks1 <- resultWindowRows order by ko3iko.Value ASC]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultShape0(Position: ko3iko.Position, Ordinal: resultRowNumbers0[windowIndex], Rank: resultRanks1[windowIndex])]
    SortShapeRows [result -> resultSorted by Position ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K70_CatalogFusedRanking
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
            new Column("Ordinal", typeof(long), 1),
            new Column("Rank", typeof(long), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Value", typeof(int?), 1) });
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
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Ordinal, __musoqShapeRow.Rank);
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
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.NumericRow<int>>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.NumericRow<int>>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                var resultRowNumbers0OrderKeys = new WindowResultRowNumbers0OrderKeysKey[resultWindowRows.Count];
                ExtractResultRowNumbers0WindowKeys(resultWindowRows, resultRowNumbers0OrderKeys);
                var resultRowNumbers0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultRowNumbers0Partitions, resultRowNumbers0OrderKeys, false);
                var resultRowNumbers0 = new long[resultWindowRows.Count];
                var resultRanks1 = new long[resultWindowRows.Count];
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
                            resultRowNumbers0[resultRowNumbers0CurrentIndex] = Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.ShiftedOrdinalKernel.Get(in resultRowNumbers0Context);
                            var resultRanks1Context = new Musoq.Plugins.WindowRankingContext(resultRowNumbers0PeerIndex, resultRowNumbers0PeerStart, resultRowNumbers0PeerEnd, resultRowNumbers0DenseRank, resultRowNumbers0WindowPlanPartitionCount, token);
                            resultRanks1[resultRowNumbers0CurrentIndex] = Musoq.Plugins.RankKernel.Get(in resultRanks1Context);
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

                    Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.NumericRow<int> ko3iko = resultWindowRows[windowIndex];
                    result.Add(new ResultShape0(ko3iko.Position, (long)resultRowNumbers0[windowIndex], (long)resultRanks1[windowIndex]));
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
        private static void ExtractResultRowNumbers0WindowKeys(IReadOnlyList<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.NumericRow<int>> resultWindowRows, WindowResultRowNumbers0OrderKeysKey[] resultRowNumbers0OrderKeys)
        {
            for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
            {
                Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.NumericRow<int> ko3iko = resultWindowRows[windowIndex];
                resultRowNumbers0OrderKeys[windowIndex] = new WindowResultRowNumbers0OrderKeysKey(ko3iko.Value);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, long __value1, long __value2)
            {
                Position = __value0;
                Ordinal = __value1;
                Rank = __value2;
            }

            public override int Count => 3;
            public long Ordinal { get; private set; }
            public int Position { get; private set; }
            public long Rank { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Ordinal = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Rank = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Ordinal),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Rank),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "Ordinal" => true,
                "Rank" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Ordinal, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Rank, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Position, long Ordinal, long Rank)
            {
                this.Position = Position;
                this.Ordinal = Ordinal;
                this.Rank = Rank;
            }

            public long Ordinal { get; }
            public int Position { get; }
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
