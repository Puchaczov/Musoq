// === Parsed Query ===
/*
select r.Id as Id, Sum(r.Amount) over (order by r.Id) as Total, Sum(r.Amount) over (order by r.Id range between 1 preceding and current row) as Sliding
from bench.rows() r
inner join bench.twice() d on r.Key = d.Key
order by r.Id
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.Id as r.Id, r.Key as r.Key, r.Amount as r.Amount, d.Key as d.Key]
    Join [Inner] [(r.Key = d.Key)]
      SchemaScan [#bench.rows() as r]
      SchemaScan [#bench.twice() as d]
  Sort [r.Id]
    Project [r.Id as Id, WindowRef(0) as Total, WindowRef(1) as Sliding]
      Window [Sum(idx:0; order: r.Id; args: r.Amount), Sum(idx:1; order: r.Id; args: r.Amount; frame: range between 1 preceding and current row)]
        CteRef [rd as rd]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.Id as r.Id, r.Amount as r.Amount]
    PhysicalHashJoin [Inner] [build: d.Key] [probe: r.Key]
      PhysicalSchemaScan [#bench.rows() as r]
      PhysicalSchemaScan [#bench.twice() as d]
  PhysicalSort [r.Id]
    PhysicalProject [r.Id as Id, WindowRef(0) as Total, WindowRef(1) as Sliding]
      PhysicalWindow [Sum(idx:0; order: r.Id; args: r.Amount), Sum(idx:1; order: r.Id; args: r.Amount; frame: range between 1 preceding and current row)]
        PhysicalMaterialize
          PhysicalCteRef [rd as rd]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Id: int <- property Id
      Key: int <- property Key
      Amount: decimal <- property Amount
    SourceEntity [d: FixtureRow]
      Key: int <- property Key
    Generated [Statement0Row0]
      r.Id: int <- field r_Id
      r.Amount: decimal <- field r_Amount
    TableRow [rd]
      r.Id: int <- field r_Id
      r.Amount: decimal <- field r_Amount
    Generated [ResultRow0]
      Id: int <- field Id
      Total: decimal? <- field Total
      Sliding: decimal? <- field Sliding
  StoredTableRepresentations
    [0] GeneratedColumnarBuffer; row=Statement0Row0; ownership=ConstructFresh; lifetime=Execution

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: FixtureRow] -> statement0_rRows
    SourceScan [d: FixtureRow] -> statement0_dRows
    CreateTable [statement0: Statement0Row0]
    CreateHash [statement0DHash: int -> FixtureRow]
    PhaseBoundary [Select]
    ChunkedForEach [d in statement0_dRows]
      HashAdd [statement0DHash[d.Key] += d]
    ChunkedForEach [r in statement0_rRows]
      HashProbe [statement0DHash[r.Key] -> statement0DHashMatches]
        ForEach [d in statement0DHashMatches]
          AppendRow [statement0 <- Statement0Row0(r.Id: r.Id, r.Amount: r.Amount)]
    StoreTable [statement0 -> _cteRowResults.Slot0: GeneratedColumnarBuffer<Statement0Row0>]
    Materialize [_cteRowResults.Slot0 -> resultWindowRows]
    ComputeSumWindowKernel[BoundedRows] [resultSums0 <- resultWindowRows value rd.r.Amount order by rd.r.Id ASC frame range between unbounded preceding and current row]
    ComputeSumWindowKernel[BoundedRows] [resultSums1 <- resultWindowRows value rd.r.Amount order by rd.r.Id ASC frame range between 1 preceding and current row] [reuse peer frame]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, rd in resultWindowRows]
      AppendShape [result <- ResultShape0(Id: rd.r.Id, Total: resultSums0[windowIndex], Sliding: resultSums1[windowIndex])]
    SortOwnedBuffer [result by Id ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultShape0 from owned buffer result]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_X02_JoinWindow_BenchmarkTable
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
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Key", typeof(int), 1), new Column("Amount", typeof(decimal), 2) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_d_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Key", typeof(int), 0) });
        private static readonly Column[] __columns_compiled_statement0_2 = new Column[]
        {
            new Column("r.Id", typeof(int), 0),
            new Column("r.Amount", typeof(decimal), 1)
        };
        private static readonly Column[] __columns_compiled_result_3 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Total", typeof(decimal?), 1),
            new Column("Sliding", typeof(decimal?), 2)
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
                var resultWindowRows = _cteRowResults.Slot0;
                var resultSums0OrderKeys = new WindowResultSums0OrderKeysKey[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Statement0Row0 rd = resultWindowRows[windowIndex];
                    resultSums0OrderKeys[windowIndex] = new WindowResultSums0OrderKeysKey(rd.r_Id);
                }

                var resultSums0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultSums0Partitions, resultSums0OrderKeys, false);
                var resultSums0 = new decimal? [resultWindowRows.Count];
                for (int resultSums0PartitionSetIndex = 0; resultSums0PartitionSetIndex < resultSums0Partitions.PartitionCount; ++resultSums0PartitionSetIndex)
                {
                    var resultSums0PartitionStart = resultSums0Partitions.GetStart(resultSums0PartitionSetIndex);
                    var resultSums0PartitionCount = resultSums0Partitions.GetLength(resultSums0PartitionSetIndex);
                    var resultSums0PartitionIndices = resultSums0Partitions.Indices;
                    Musoq.Plugins.SumAggregateKernel<decimal>.State resultSums0State = default(Musoq.Plugins.SumAggregateKernel<decimal>.State)!;
                    Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref resultSums0State);
                    for (int resultSums0PartitionIndex = 0; resultSums0PartitionIndex < resultSums0PartitionCount;)
                    {
                        var resultSums0PeerEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultSums0OrderKeys, resultSums0PartitionIndices, resultSums0PartitionStart, resultSums0PartitionCount, resultSums0PartitionIndex);
                        for (int resultSums0PeerIndex = resultSums0PartitionIndex; resultSums0PeerIndex <= resultSums0PeerEnd; ++resultSums0PeerIndex)
                        {
                            var resultSums0CurrentIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0PeerIndex];
                            Statement0Row0 rd = resultWindowRows[resultSums0CurrentIndex];
                            Musoq.Plugins.SumAggregateKernel<decimal>.Set(ref resultSums0State, (decimal?)(rd.r_Amount));
                        }

                        var resultSums0PeerValue = Musoq.Plugins.SumAggregateKernel<decimal>.Get(in resultSums0State);
                        for (int resultSums0PeerIndex = resultSums0PartitionIndex; resultSums0PeerIndex <= resultSums0PeerEnd; ++resultSums0PeerIndex)
                        {
                            var resultSums0CurrentIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0PeerIndex];
                            resultSums0[resultSums0CurrentIndex] = resultSums0PeerValue;
                        }

                        resultSums0PartitionIndex = resultSums0PeerEnd + 1;
                    }
                }

                var resultSums1RangeKeys = new int[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Statement0Row0 rd = resultWindowRows[windowIndex];
                    resultSums1RangeKeys[windowIndex] = (int)(resultSums0OrderKeys[windowIndex].RawValues);
                }

                var resultSums1 = new decimal? [resultWindowRows.Count];
                var resultSums1BatchValues = new decimal? [resultWindowRows.Count];
                for (int resultSums1CaptureIndex = 0; resultSums1CaptureIndex < resultWindowRows.Count; ++resultSums1CaptureIndex)
                {
                    Statement0Row0 rd = resultWindowRows[resultSums1CaptureIndex];
                    resultSums1BatchValues[resultSums1CaptureIndex] = (decimal?)(rd.r_Amount);
                }

                for (int resultSums1PartitionSetIndex = 0; resultSums1PartitionSetIndex < resultSums0Partitions.PartitionCount; ++resultSums1PartitionSetIndex)
                {
                    var resultSums1PartitionStart = resultSums0Partitions.GetStart(resultSums1PartitionSetIndex);
                    var resultSums1PartitionCount = resultSums0Partitions.GetLength(resultSums1PartitionSetIndex);
                    var resultSums1PartitionIndices = resultSums0Partitions.Indices;
                    Musoq.Plugins.SumAggregateKernel<decimal>.State resultSums1State = default(Musoq.Plugins.SumAggregateKernel<decimal>.State)!;
                    Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref resultSums1State);
                    for (int resultSums1PartitionIndex = 0; resultSums1PartitionIndex < resultSums1PartitionCount; ++resultSums1PartitionIndex)
                    {
                        var resultSums1OutputIndex = resultSums1PartitionIndices[resultSums1PartitionStart + resultSums1PartitionIndex];
                        if (resultSums1PartitionIndex > 0)
                        {
                            var resultSums1PreviousPeer = resultSums1PartitionIndices[resultSums1PartitionStart + resultSums1PartitionIndex - 1];
                            if (resultSums1RangeKeys[resultSums1PreviousPeer] == resultSums1RangeKeys[resultSums1OutputIndex])
                            {
                                resultSums1[resultSums1OutputIndex] = resultSums1[resultSums1PreviousPeer];
                                continue;
                            }
                        }

                        var resultSums1FrameStart = WindowFunctionHelpers.ResolveRangeFrameStart(resultSums1RangeKeys, resultSums0OrderKeys, resultSums1PartitionIndices, resultSums1PartitionStart, resultSums1PartitionCount, resultSums1PartitionIndex, -1, false, true);
                        var resultSums1FrameEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultSums0OrderKeys, resultSums1PartitionIndices, resultSums1PartitionStart, resultSums1PartitionCount, resultSums1PartitionIndex);
                        Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref resultSums1State);
                        for (var resultSums1FrameIndex = Math.Max(0, resultSums1FrameStart); resultSums1FrameIndex <= resultSums1FrameEnd && resultSums1FrameIndex < resultSums1PartitionCount; resultSums1FrameIndex++)
                        {
                            var resultSums1CurrentIndex = resultSums1PartitionIndices[resultSums1PartitionStart + resultSums1FrameIndex];
                            Musoq.Plugins.SumAggregateKernel<decimal>.Set(ref resultSums1State, (decimal?)(resultSums1BatchValues[resultSums1CurrentIndex]));
                        }

                        resultSums1[resultSums1OutputIndex] = Musoq.Plugins.SumAggregateKernel<decimal>.Get(in resultSums1State);
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
                    result.Add(new ResultShape0(rd.r_Id, (decimal?)resultSums0[windowIndex], (decimal?)resultSums1[windowIndex]) { __musoqSequence = result.Count });
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
        private static StoredTableColumnBuffer0 BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            var __statement0_rSchema = provider.GetSchema("#bench");
            var statement0_rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__statement0_rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
            var statement0_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(statement0_rRowsSource.Chunks, __musoqProgressContext, "r:1") : statement0_rRowsSource.Chunks;
            var __statement0_dSchema = provider.GetSchema("#bench");
            var statement0_dRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__statement0_dSchema, "twice", new SourceExecutionContext("d:1", sourceExecutionPlans["d:1"], token, __schemaColumns_compiled_d_1, sourceRuntimeSettingsBySourceContextId["d:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["d:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
            var statement0_dRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(statement0_dRowsSource.Chunks, __musoqProgressContext, "d:1") : statement0_dRowsSource.Chunks;
            var statement0 = new StoredTableColumnBuffer0();
            var statement0DHash = new Dictionary<int, HashJoinBucket<Musoq.Benchmarks.Scenarios.FixtureRow>>();
            foreach (var dChunk in statement0_dRows)
            {
                if (dChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.FixtureRow> dChunkView)
                {
                    if (dChunkView.Source is Musoq.Benchmarks.Scenarios.FixtureRow[] dChunkViewArray)
                    {
                        int dChunkViewOffset = dChunkView.Offset;
                        for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                        {
                            if ((dIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var d = dChunkViewArray[dChunkViewOffset + dIndex];
                            int key = d.Key;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0DHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Benchmarks.Scenarios.FixtureRow>(d);
                                }
                                else
                                {
                                    matches.Add(d);
                                }
                            }
                        }

                        continue;
                    }

                    if (dChunkView.Source is List<Musoq.Benchmarks.Scenarios.FixtureRow> dChunkViewList)
                    {
                        int dChunkViewOffset = dChunkView.Offset;
                        for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                        {
                            if ((dIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var d = dChunkViewList[dChunkViewOffset + dIndex];
                            int key = d.Key;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0DHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Benchmarks.Scenarios.FixtureRow>(d);
                                }
                                else
                                {
                                    matches.Add(d);
                                }
                            }
                        }

                        continue;
                    }
                }

                for (int dIndex = 0, dIndexCount = dChunk.Count; dIndex < dIndexCount; ++dIndex)
                {
                    if ((dIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var d = dChunk[dIndex];
                    int key = d.Key;
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0DHash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<Musoq.Benchmarks.Scenarios.FixtureRow>(d);
                        }
                        else
                        {
                            matches.Add(d);
                        }
                    }
                }
            }

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
                            int key = r.Key;
                            if (statement0DHash.TryGetValue(key, out var statement0DHashMatches))
                            {
                                foreach (var d in statement0DHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0.Add(r.Id, r.Amount);
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
                            int key = r.Key;
                            if (statement0DHash.TryGetValue(key, out var statement0DHashMatches))
                            {
                                foreach (var d in statement0DHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0.Add(r.Id, r.Amount);
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
                    int key = r.Key;
                    if (statement0DHash.TryGetValue(key, out var statement0DHashMatches))
                    {
                        foreach (var d in statement0DHashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            statement0.Add(r.Id, r.Amount);
                        }
                    }
                }
            }

            return statement0;
        }

        private readonly struct Statement0Row0
        {
            private readonly StoredTableColumnBuffer0 __buffer;
            private readonly int __index;
            public Statement0Row0(StoredTableColumnBuffer0 buffer, int index)
            {
                this.__buffer = buffer;
                this.__index = index;
            }

            public int r_Id => __buffer.Column0[__index];
            public decimal r_Amount => __buffer.Column1[__index];
        }

        private sealed class ResultShape0 : Row
        {
            public ResultShape0(int Id, decimal? Total, decimal? Sliding)
            {
                this.Id = Id;
                this.Total = Total;
                this.Sliding = Sliding;
            }

            public int Id { get; private set; }
            public decimal? Total { get; private set; }
            public decimal? Sliding { get; private set; }
            public override int Count => 3;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Sliding, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Sliding),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Total" => true,
                "Sliding" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Sliding = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public int __musoqSequence { get; init; }
        }

        private sealed class CteRowResults
        {
            public StoredTableColumnBuffer0 Slot0;
        }

        private sealed class StoredTableColumn<T>
        {
            private const int ChunkSize = 1024;
            private T[][] _chunks = Array.Empty<T[]>();
            private int _chunkCount;
            private int _count;
            public StoredTableColumn(int capacity = 0)
            {
                EnsureCapacity(capacity);
            }

            public int Count => _count;

            public T this[int index] => _chunks[index >> 10][index & (ChunkSize - 1)];
            public void Add(T value)
            {
                var chunkIndex = _count >> 10;
                if (chunkIndex == _chunkCount)
                    AddChunk();
                _chunks[chunkIndex][_count & (ChunkSize - 1)] = value;
                _count++;
            }

            public void EnsureCapacity(int capacity)
            {
                if (capacity <= 0)
                    return;
                var requiredChunks = ((capacity - 1) / ChunkSize) + 1;
                while (_chunkCount < requiredChunks)
                    AddChunk();
            }

            private void AddChunk()
            {
                if (_chunkCount == _chunks.Length)
                    Array.Resize(ref _chunks, _chunkCount == 0 ? 4 : _chunkCount * 2);
                _chunks[_chunkCount++] = new T[ChunkSize];
            }
        }

        private sealed class StoredTableColumnBuffer0
        {
            public readonly StoredTableColumn<int> Column0;
            public readonly StoredTableColumn<decimal> Column1;
            public StoredTableColumnBuffer0(int capacity = 0)
            {
                Column0 = new StoredTableColumn<int>(capacity);
                Column1 = new StoredTableColumn<decimal>(capacity);
            }

            public int Count => Column0.Count;

            public Statement0Row0 this[int index] => new Statement0Row0(this, index);
            public void Add(int value0, decimal value1)
            {
                Column0.Add(value0);
                Column1.Add(value1);
            }

            public void EnsureCapacity(int capacity)
            {
                Column0.EnsureCapacity(capacity);
                Column1.EnsureCapacity(capacity);
            }
        }

        private readonly struct WindowResultSums0OrderKeysKey : System.IEquatable<WindowResultSums0OrderKeysKey>, System.IComparable<WindowResultSums0OrderKeysKey>
        {
            private readonly int _value0;
            public WindowResultSums0OrderKeysKey(int value0)
            {
                _value0 = value0;
            }

            public int RawValues => _value0;

            public int CompareTo(WindowResultSums0OrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultSums0OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultSums0OrderKeysKey other && Equals(other);
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
