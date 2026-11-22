// === Parsed Query ===
/*
select r.Id as Id, Lag(d.Amount) over (order by r.Id) as Previous, Sum(d.Amount) over (order by r.Id) as Total
from bench.rows() r asof
left join bench.lookup() d on r.Time >= d.Time tie break by d.Id desc
order by r.Id
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.Id as r.Id, r.Time as r.Time, d.Id as d.Id, d.Amount as d.Amount, d.Time as d.Time]
    Join [AsofLeft] [(r.Time >= d.Time)] [tie: d.Id DESC]
      SchemaScan [#bench.rows() as r]
      SchemaScan [#bench.lookup() as d]
  Sort [r.Id]
    Project [r.Id as Id, WindowRef(0) as Previous, WindowRef(1) as Total]
      Window [Lag(idx:0; order: r.Id; args: d.Amount), Sum(idx:1; order: r.Id; args: d.Amount)]
        CteRef [rd as rd]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.Id as r.Id, d.Amount as d.Amount]
    PhysicalNestedLoopJoin [AsofLeft] [(r.Time >= d.Time)] [tie: d.Id DESC]
      PhysicalSchemaScan [#bench.rows() as r]
      PhysicalSchemaScan [#bench.lookup() as d]
  PhysicalSort [r.Id]
    PhysicalProject [r.Id as Id, WindowRef(0) as Previous, WindowRef(1) as Total]
      PhysicalWindow [Lag(idx:0; order: r.Id; args: d.Amount), Sum(idx:1; order: r.Id; args: d.Amount)]
        PhysicalMaterialize
          PhysicalCteRef [rd as rd]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Id: int <- property Id
      Time: DateTime <- property Time
    SourceEntity [d: FixtureRow]
      Id: int <- property Id
      Amount: decimal <- property Amount
      Time: DateTime <- property Time
    Generated [Statement0Row0]
      r.Id: int <- field r_Id
      d.Amount: decimal? <- field d_Amount
    TableRow [rd]
      r.Id: int <- field r_Id
      d.Amount: decimal? <- field d_Amount
    Generated [ResultRow0]
      Id: int <- field Id
      Previous: decimal? <- field Previous
      Total: decimal? <- field Total

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: FixtureRow] -> statement0_rRows
    SourceScan [d: FixtureRow] -> statement0_dRows
    CreateTable [statement0: Statement0Row0]
    CreateAsOfIndex [statement0AsOfIndex <- statement0_dRows by dCandidate.Time; tie dCandidate.Id DESC]
    PhaseBoundary [Select]
    ChunkedForEach [r in statement0_rRows]
      AsOfProbe [d <- statement0_dRows using statement0AsOfIndex where r.Time >= dCandidate.Time tie break by dCandidate.Id DESC]
        AppendRow [statement0 <- Statement0Row0(r.Id: r.Id, d.Amount: d.Amount)]
      AsOfProbeNoMatch
        AppendRow [statement0 <- Statement0Row0(r.Id: r.Id, d.Amount: NULL)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    Materialize [_cteRowResults.Slot0 -> resultWindowRows]
    ComputeLagWindow [resultLags0 <- resultWindowRows value rd.d.Amount order by rd.r.Id ASC frame range between unbounded preceding and current row args 1, NULL]
    ComputeSumWindowKernel[BoundedRows] [resultSums1 <- resultWindowRows value rd.d.Amount order by rd.r.Id ASC frame range between unbounded preceding and current row]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, rd in resultWindowRows]
      AppendShape [result <- ResultShape0(Id: rd.r.Id, Previous: resultLags0[windowIndex], Total: resultSums1[windowIndex])]
    SortOwnedBuffer [result by Id ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultShape0 from owned buffer result]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_X04_TemporalAnalytics_BenchmarkTable
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IContextTableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Time", typeof(DateTime), 1) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_d_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Amount", typeof(decimal), 1), new Column("Time", typeof(DateTime), 2) });
        private static readonly Column[] __columns_compiled_statement0_2 = new Column[]
        {
            new Column("r.Id", typeof(int), 0),
            new Column("d.Amount", typeof(decimal?), 1)
        };
        private static readonly Column[] __columns_compiled_result_3 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Previous", typeof(decimal?), 1),
            new Column("Total", typeof(decimal?), 2)
        };
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public ILogger Logger { get; set; }
        public TypedParameterSnapshot TypedParameters { get; set; } = TypedParameterSnapshot.Empty;
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();

        public event QueryPhaseEventHandler PhaseChanged;
        public event DataSourceEventHandler DataSourceProgress;
        public event QueryProgressEventHandler QueryProgress;
        public Table Run(CancellationToken token)
        {
            var queryContext = new QueryRunContext(token, TypedParameters, PhaseChanged, DataSourceProgress, this, queryId: "compiled", provider: Provider, sourceRuntimeSettingsBySourceContextId: SourceRuntimeSettingsBySourceContextId, sourceRuntimeSettingDescriptionsBySourceContextId: SourceRuntimeSettingDescriptionsBySourceContextId, sourceExecutionPlans: SourceExecutionPlans, logger: Logger, queryProgress: QueryProgress);
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            var token = queryContext.CancellationToken;
            var __musoqProgressContext = queryContext.QueryProgress == null ? null : queryContext;
            Action<string, QueryPhase> OnPhaseChanged = queryContext.NotifyPhaseChanged;
            DataSourceEventHandler OnDataSourceProgress = queryContext.NotifyDataSourceProgress;
            QueryProgressEventHandler OnQueryProgress = queryContext.QueryProgress;
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(queryContext.TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var resultWindowRows = EvaluationHelper.MaterializeGeneratedRows<Statement0Row0>(_cteRowResults.Slot0);
                var resultLags0OrderKeys = new WindowResultLags0OrderKeysKey[resultWindowRows.Count];
                var resultLags0Values = new decimal? [resultWindowRows.Count];
                var resultLags0Argument0 = new int[resultWindowRows.Count];
                var resultLags0Argument1 = new decimal? [resultWindowRows.Count];
                for (int resultLags0ExtractionIndex = 0; resultLags0ExtractionIndex < resultWindowRows.Count; ++resultLags0ExtractionIndex)
                {
                    Statement0Row0 rd = resultWindowRows[resultLags0ExtractionIndex];
                    resultLags0OrderKeys[resultLags0ExtractionIndex] = new WindowResultLags0OrderKeysKey(rd.r_Id);
                    resultLags0Values[resultLags0ExtractionIndex] = (decimal?)((decimal?)rd.d_Amount);
                    resultLags0Argument0[resultLags0ExtractionIndex] = (int)((int)1);
                    resultLags0Argument1[resultLags0ExtractionIndex] = (decimal?)((decimal?)null);
                }

                var resultLags0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultLags0Partitions, resultLags0OrderKeys, false);
                var resultLags0 = new decimal? [resultWindowRows.Count];
                for (int resultLags0Partition = 0; resultLags0Partition < resultLags0Partitions.PartitionCount; ++resultLags0Partition)
                {
                    var resultLags0Start = resultLags0Partitions.GetStart(resultLags0Partition);
                    var resultLags0Count = resultLags0Partitions.GetLength(resultLags0Partition);
                    var resultLags0Indices = resultLags0Partitions.Indices;
                    for (int resultLags0Position = 0; resultLags0Position < resultLags0Count; ++resultLags0Position)
                    {
                        if ((resultLags0Position & 1023) == 0)
                            token.ThrowIfCancellationRequested();
                        var resultLags0Current = resultLags0Indices[resultLags0Start + resultLags0Position];
                        var resultLags0FrameStart = 0;
                        var resultLags0FrameEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultLags0OrderKeys, resultLags0Indices, resultLags0Start, resultLags0Count, resultLags0Position);
                        var resultLags0Context = new Musoq.Plugins.WindowFrameContext<decimal?>(resultLags0Values, new System.ReadOnlySpan<int>(resultLags0Indices, resultLags0Start, resultLags0Count), resultLags0Position, resultLags0FrameStart, resultLags0FrameEnd, token);
                        resultLags0[resultLags0Current] = Musoq.Plugins.LagKernel<decimal>.Get(in resultLags0Context, resultLags0Argument0[resultLags0Current], resultLags0Argument1[resultLags0Current]);
                    }
                }

                var resultSums1 = new decimal? [resultWindowRows.Count];
                for (int resultSums1PartitionSetIndex = 0; resultSums1PartitionSetIndex < resultLags0Partitions.PartitionCount; ++resultSums1PartitionSetIndex)
                {
                    var resultSums1PartitionStart = resultLags0Partitions.GetStart(resultSums1PartitionSetIndex);
                    var resultSums1PartitionCount = resultLags0Partitions.GetLength(resultSums1PartitionSetIndex);
                    var resultSums1PartitionIndices = resultLags0Partitions.Indices;
                    Musoq.Plugins.SumAggregateKernel<decimal>.State resultSums1State = default(Musoq.Plugins.SumAggregateKernel<decimal>.State)!;
                    Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref resultSums1State);
                    for (int resultSums1PartitionIndex = 0; resultSums1PartitionIndex < resultSums1PartitionCount;)
                    {
                        var resultSums1PeerEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultLags0OrderKeys, resultSums1PartitionIndices, resultSums1PartitionStart, resultSums1PartitionCount, resultSums1PartitionIndex);
                        for (int resultSums1PeerIndex = resultSums1PartitionIndex; resultSums1PeerIndex <= resultSums1PeerEnd; ++resultSums1PeerIndex)
                        {
                            var resultSums1CurrentIndex = resultSums1PartitionIndices[resultSums1PartitionStart + resultSums1PeerIndex];
                            Statement0Row0 rd = resultWindowRows[resultSums1CurrentIndex];
                            Musoq.Plugins.SumAggregateKernel<decimal>.Set(ref resultSums1State, (decimal?)(rd.d_Amount));
                        }

                        var resultSums1PeerValue = Musoq.Plugins.SumAggregateKernel<decimal>.Get(in resultSums1State);
                        for (int resultSums1PeerIndex = resultSums1PartitionIndex; resultSums1PeerIndex <= resultSums1PeerEnd; ++resultSums1PeerIndex)
                        {
                            var resultSums1CurrentIndex = resultSums1PartitionIndices[resultSums1PartitionStart + resultSums1PeerIndex];
                            resultSums1[resultSums1CurrentIndex] = resultSums1PeerValue;
                        }

                        resultSums1PartitionIndex = resultSums1PeerEnd + 1;
                    }
                }

                var result = new List<ResultShape0>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 rd = resultWindowRows[windowIndex];
                    result.Add(new ResultShape0(rd.r_Id, (decimal?)resultLags0[windowIndex], (decimal?)resultSums1[windowIndex]) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
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

        private IEnumerable<ResultShape0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, queryContext))
            {
                yield return __musoqShapeRow;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnPhaseChanged(string queryId, QueryPhase phase)
        {
            PhaseChanged?.Invoke(this, new QueryPhaseEventArgs(queryId, phase));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnDataSourceProgress(object sender, DataSourceEventArgs e)
        {
            DataSourceProgress?.Invoke(this, e);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            var __statement0_rSchema = provider.GetSchema("#bench");
            var statement0_rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__statement0_rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
            var statement0_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(statement0_rRowsSource.Chunks, __musoqProgressContext, "r:1") : statement0_rRowsSource.Chunks;
            var __statement0_dSchema = provider.GetSchema("#bench");
            var statement0_dRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__statement0_dSchema, "lookup", new SourceExecutionContext("d:1", sourceExecutionPlans["d:1"], token, __schemaColumns_compiled_d_1, sourceRuntimeSettingsBySourceContextId["d:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["d:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
            var statement0_dRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(statement0_dRowsSource.Chunks, __musoqProgressContext, "d:1") : statement0_dRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            var statement0AsOfIndex = EvaluationHelper.CreateAsOfIndex<Musoq.Benchmarks.Scenarios.FixtureRow, DateTime?, bool, int?>(statement0_dRows, null, (dCandidate) => dCandidate.Time, Musoq.Evaluator.IR.Expressions.BinaryOpKind.GreaterOrEqual, (dCandidate) => dCandidate.Id, true, Musoq.Evaluator.IR.Bindings.NullOrdering.Default);
            foreach (var rChunk in statement0_rRows)
            {
                if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.FixtureRow> rChunkView)
                {
                    if (rChunkView.Source is Musoq.Benchmarks.Scenarios.FixtureRow[] rChunkViewArray)
                    {
                        int rChunkViewOffset = rChunkView.Offset;
                        for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                        {
                            if ((rIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var r = rChunkViewArray[rChunkViewOffset + rIndex];
                            {
                                var dEquality = new NativeEqualityKey<bool>(true, true ? false : default(bool));
                                var d = statement0AsOfIndex.Find(dEquality, (DateTime?)(r.Time));
                                if (d != null)
                                {
                                    statement0.Add(new Statement0Row0(r.Id, d.Amount));
                                }
                                else
                                {
                                    statement0.Add(new Statement0Row0(r.Id, null));
                                }
                            }
                        }

                        continue;
                    }

                    if (rChunkView.Source is List<Musoq.Benchmarks.Scenarios.FixtureRow> rChunkViewList)
                    {
                        int rChunkViewOffset = rChunkView.Offset;
                        for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                        {
                            if ((rIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var r = rChunkViewList[rChunkViewOffset + rIndex];
                            {
                                var dEquality = new NativeEqualityKey<bool>(true, true ? false : default(bool));
                                var d = statement0AsOfIndex.Find(dEquality, (DateTime?)(r.Time));
                                if (d != null)
                                {
                                    statement0.Add(new Statement0Row0(r.Id, d.Amount));
                                }
                                else
                                {
                                    statement0.Add(new Statement0Row0(r.Id, null));
                                }
                            }
                        }

                        continue;
                    }
                }

                for (int rIndex = 0, rIndexCount = rChunk.Count; rIndex < rIndexCount; ++rIndex)
                {
                    if ((rIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var r = rChunk[rIndex];
                    {
                        var dEquality = new NativeEqualityKey<bool>(true, true ? false : default(bool));
                        var d = statement0AsOfIndex.Find(dEquality, (DateTime?)(r.Time));
                        if (d != null)
                        {
                            statement0.Add(new Statement0Row0(r.Id, d.Amount));
                        }
                        else
                        {
                            statement0.Add(new Statement0Row0(r.Id, null));
                        }
                    }
                }
            }

            return statement0;
        }

        private sealed class Statement0Row0
        {
            public int r_Id { get; }
            public decimal? d_Amount { get; }

            public Statement0Row0(int __value0, decimal? __value1)
            {
                r_Id = __value0;
                d_Amount = __value1;
            }
        }

        private sealed class ResultShape0 : Row
        {
            public ResultShape0(int Id, decimal? Previous, decimal? Total)
            {
                this.Id = Id;
                this.Previous = Previous;
                this.Total = Total;
            }

            public int Id { get; private set; }
            public decimal? Previous { get; private set; }
            public decimal? Total { get; private set; }
            public override int Count => 3;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Previous, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Total, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Previous),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Previous" => true,
                "Total" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Previous = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public int __musoqSequence { get; init; }
        }

        private sealed class CteRowResults
        {
            public List<Statement0Row0> Slot0;
        }

        private readonly struct WindowResultLags0OrderKeysKey : System.IEquatable<WindowResultLags0OrderKeysKey>, System.IComparable<WindowResultLags0OrderKeysKey>
        {
            private readonly int _value0;
            public WindowResultLags0OrderKeysKey(int value0)
            {
                _value0 = value0;
            }

            public int RawValues => _value0;

            public int CompareTo(WindowResultLags0OrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultLags0OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultLags0OrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0);
                return hash.ToHashCode();
            }

            private static int CompareValue0(int left, int right)
            {
                var comparison = left.CompareTo(right);
                return comparison;
            }
        }
    }
}
