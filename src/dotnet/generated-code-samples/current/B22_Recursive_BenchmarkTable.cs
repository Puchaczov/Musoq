// === Parsed Query ===
/*
with recursive walk as (select r.Id as Id
from bench.rows() r
where r.Id = 0
union all select e.Id
from walk w
inner join bench.rows() e on e.Id = w.Id + 1) select Id
from walk
order by Id
*/

// === Logical Plan ===
/*
Cte
  Definition [walk]
    RecursiveCte [walk] [All]
      Anchor
        MultiStatement
          Project [r.Id as Id]
            Filter [(r.Id = 0)]
              SchemaScan [#bench.rows() as r]
      RecursiveMember
        MultiStatement
          Project [w.Id as w.Id, e.Id as e.Id]
            Join [Inner] [(e.Id = (w.Id + 1))]
              CteRef [walk as w]
              SchemaScan [#bench.rows() as e]
          Project [e.Id as Id]
            CteRef [we as we]
  Query
    MultiStatement
      Sort [walk.Id]
        Project [walk.Id as Id]
          CteRef [walk as walk]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [walk]
    PhysicalRecursiveCte [walk] [All]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [r.Id as Id]
            PhysicalFilter [(r.Id = 0)]
              PhysicalSchemaScan [#bench.rows() as r] [pushdown: (r.Id = 0)]
      Invariant [__recursive_walk_invariant_0; HashIndex; fields Id]
        PhysicalSchemaScan [#bench.rows() as e]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [w.Id as w.Id, e.Id as e.Id]
            PhysicalHashJoin [Inner] [build: e.Id] [probe: (w.Id + 1)]
              PhysicalCteRef [walk as w]
              PhysicalCteRef [__recursive_walk_invariant_0 as e]
          PhysicalProject [e.Id as Id]
            PhysicalCteRef [we as we]
  Query
    PhysicalMultiStatement
      PhysicalSort [walk.Id]
        PhysicalProject [walk.Id as Id]
          PhysicalCteRef [walk as walk]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: BenchmarkSampleRow]
      Id: int <- property Id
    Generated [Cte0Row0]
      Id: int <- field Id
    SourceEntity [e: BenchmarkSampleRow]
      Id: int <- property Id
    Generated [Cte0Invariant0Row0]
      Id: int <- field Id
    TableRow [w]
      Id: int <- field Id
    TableRow [walk]
      Id: int <- field Id
    Generated [ResultRow0]
      Id: int <- field Id

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    RecursiveCte [walk; result cte0; frontiers cte0CurrentFrontier, cte0NextFrontier; identity none; max iterations 100000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        SourceScan [r: BenchmarkSampleRow] -> cte0CurrentFrontier_rRows
        ChunkedForEach [r in cte0CurrentFrontier_rRows]
          If [(r.Id = 0)]
            RecursiveAppend [cte0CurrentFrontier <- Cte0Row0(Id: r.Id); identity none; guard cte0.Count + cte0CurrentFrontier.Count < 10000000]
      InvariantSetup
        CreateHash [cte0Invariant0Hash: int -> Row]
        SourceScan [e: BenchmarkSampleRow] -> cte0Invariant0_eRows
        ChunkedForEach [e in cte0Invariant0_eRows]
          ScopedBlock
            RecursiveSnapshotGuard [__cte0SnapshotRows < 10000000; walk]
            CreateGeneratedRow [cte0Invariant0Row <- Cte0Invariant0Row0(Id: e.Id)]
            HashAdd [cte0Invariant0Hash[cte0Invariant0Row.Id] += cte0Invariant0Row]
      RecursiveMember
        ForEach [w in cte0CurrentFrontier]
          HashProbe [cte0Invariant0Hash[(w.Id + 1)] -> cte0Invariant0HashMatches]
            ForEach [e in cte0Invariant0HashMatches]
              RecursiveAppend [cte0NextFrontier <- Cte0Row0(Id: e.Id); identity none; guard cte0.Count + cte0NextFrontier.Count < 10000000]
    PhaseBoundary [Where:cte0]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [From]
    ForEach [walk in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Id: walk.Id)]
    PhaseBoundary [Select]
    SortShapeRows [result -> resultSorted by Id ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B22_Recursive_BenchmarkTable
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
            new Column("Id", typeof(int), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0) });
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
                yield return new ResultRow0(__musoqShapeRow.Id);
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
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.From);
                    var cte0 = new List<Cte0Row0>();
                    var cte0CurrentFrontier = new List<Cte0Row0>();
                    var cte0NextFrontier = new List<Cte0Row0>();
                    int __cte0SnapshotRows = 0;
                    int __cte0Iteration = 0;
                    int __cte0CancellationCounter = 0;
                    var __cte0CurrentFrontier_rSchema = provider.GetSchema("#bench");
                    var cte0CurrentFrontier_rRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BenchmarkSampleRow>.Open(__cte0CurrentFrontier_rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                    var cte0CurrentFrontier_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BenchmarkSampleRow>(cte0CurrentFrontier_rRowsSource.Chunks, __musoqProgressContext, "r:1") : cte0CurrentFrontier_rRowsSource.Chunks;
                    foreach (var rChunk in cte0CurrentFrontier_rRows)
                    {
                        if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.BenchmarkSampleRow> rChunkView)
                        {
                            if (rChunkView.Source is Musoq.Evaluator.Tests.BenchmarkSampleRow[] rChunkViewArray)
                            {
                                int rChunkViewOffset = rChunkView.Offset;
                                for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                                {
                                    if ((rIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var r = rChunkViewArray[rChunkViewOffset + rIndex];
                                    if ((r.Id == 0))
                                    {
                                        if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                        {
                                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("walk", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                        }

                                        cte0CurrentFrontier.Add(new Cte0Row0(r.Id));
                                    }
                                }

                                continue;
                            }

                            if (rChunkView.Source is List<Musoq.Evaluator.Tests.BenchmarkSampleRow> rChunkViewList)
                            {
                                int rChunkViewOffset = rChunkView.Offset;
                                for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                                {
                                    if ((rIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var r = rChunkViewList[rChunkViewOffset + rIndex];
                                    if ((r.Id == 0))
                                    {
                                        if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                        {
                                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("walk", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                        }

                                        cte0CurrentFrontier.Add(new Cte0Row0(r.Id));
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
                            if ((r.Id == 0))
                            {
                                if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                {
                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("walk", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                }

                                cte0CurrentFrontier.Add(new Cte0Row0(r.Id));
                            }
                        }
                    }

                    cte0.AddRange(cte0CurrentFrontier);
                    if (cte0CurrentFrontier.Count > 0)
                    {
                        var cte0Invariant0Hash = new Dictionary<int, HashJoinBucket<Cte0Invariant0Row0>>();
                        var __cte0Invariant0_eSchema = provider.GetSchema("#bench");
                        var cte0Invariant0_eRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BenchmarkSampleRow>.Open(__cte0Invariant0_eSchema, "rows", new SourceExecutionContext("e:2", sourceExecutionPlans["e:2"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["e:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                        var cte0Invariant0_eRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BenchmarkSampleRow>(cte0Invariant0_eRowsSource.Chunks, __musoqProgressContext, "e:2") : cte0Invariant0_eRowsSource.Chunks;
                        foreach (var eChunk in cte0Invariant0_eRows)
                        {
                            if (eChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.BenchmarkSampleRow> eChunkView)
                            {
                                if (eChunkView.Source is Musoq.Evaluator.Tests.BenchmarkSampleRow[] eChunkViewArray)
                                {
                                    int eChunkViewOffset = eChunkView.Offset;
                                    for (int eIndex = 0, eIndexCount = eChunkView.Count; eIndex < eIndexCount; ++eIndex)
                                    {
                                        if ((eIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var e = eChunkViewArray[eChunkViewOffset + eIndex];
                                        {
                                            if (__cte0SnapshotRows >= 10000000)
                                            {
                                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("walk", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 10000000);
                                            }

                                            __cte0SnapshotRows++;
                                            Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.Id);
                                            int cte0Invariant0HashKey = cte0Invariant0Row.Id;
                                            {
                                                ref var cte0Invariant0HashMatches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Invariant0Hash, cte0Invariant0HashKey, out var cte0Invariant0HashMatchesExists);
                                                if (!cte0Invariant0HashMatchesExists)
                                                {
                                                    cte0Invariant0HashMatches = new HashJoinBucket<Cte0Invariant0Row0>(cte0Invariant0Row);
                                                }
                                                else
                                                {
                                                    cte0Invariant0HashMatches.Add(cte0Invariant0Row);
                                                }
                                            }
                                        }
                                    }

                                    continue;
                                }

                                if (eChunkView.Source is List<Musoq.Evaluator.Tests.BenchmarkSampleRow> eChunkViewList)
                                {
                                    int eChunkViewOffset = eChunkView.Offset;
                                    for (int eIndex = 0, eIndexCount = eChunkView.Count; eIndex < eIndexCount; ++eIndex)
                                    {
                                        if ((eIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var e = eChunkViewList[eChunkViewOffset + eIndex];
                                        {
                                            if (__cte0SnapshotRows >= 10000000)
                                            {
                                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("walk", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 10000000);
                                            }

                                            __cte0SnapshotRows++;
                                            Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.Id);
                                            int cte0Invariant0HashKey = cte0Invariant0Row.Id;
                                            {
                                                ref var cte0Invariant0HashMatches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Invariant0Hash, cte0Invariant0HashKey, out var cte0Invariant0HashMatchesExists);
                                                if (!cte0Invariant0HashMatchesExists)
                                                {
                                                    cte0Invariant0HashMatches = new HashJoinBucket<Cte0Invariant0Row0>(cte0Invariant0Row);
                                                }
                                                else
                                                {
                                                    cte0Invariant0HashMatches.Add(cte0Invariant0Row);
                                                }
                                            }
                                        }
                                    }

                                    continue;
                                }
                            }

                            for (int eIndex = 0, eIndexCount = eChunk.Count; eIndex < eIndexCount; ++eIndex)
                            {
                                if ((eIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var e = eChunk[eIndex];
                                {
                                    if (__cte0SnapshotRows >= 10000000)
                                    {
                                        throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("walk", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded, 10000000);
                                    }

                                    __cte0SnapshotRows++;
                                    Cte0Invariant0Row0 cte0Invariant0Row = new Cte0Invariant0Row0(e.Id);
                                    int cte0Invariant0HashKey = cte0Invariant0Row.Id;
                                    {
                                        ref var cte0Invariant0HashMatches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Invariant0Hash, cte0Invariant0HashKey, out var cte0Invariant0HashMatchesExists);
                                        if (!cte0Invariant0HashMatchesExists)
                                        {
                                            cte0Invariant0HashMatches = new HashJoinBucket<Cte0Invariant0Row0>(cte0Invariant0Row);
                                        }
                                        else
                                        {
                                            cte0Invariant0HashMatches.Add(cte0Invariant0Row);
                                        }
                                    }
                                }
                            }
                        }

                        while (cte0CurrentFrontier.Count > 0)
                        {
                            if ((__cte0Iteration & 63) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            if (__cte0Iteration >= 100000)
                            {
                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("walk", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 100000);
                            }

                            __cte0Iteration++;
                            cte0NextFrontier.Clear();
                            for (int cte0CurrentFrontierIndex = 0; cte0CurrentFrontierIndex < cte0CurrentFrontier.Count; ++cte0CurrentFrontierIndex)
                            {
                                if (cte0CurrentFrontierIndex != 0 && (cte0CurrentFrontierIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                Cte0Row0 w = (Cte0Row0)cte0CurrentFrontier[cte0CurrentFrontierIndex];
                                int key = (w.Id + 1);
                                if (cte0Invariant0Hash.TryGetValue(key, out var cte0Invariant0HashMatches))
                                {
                                    foreach (var e in cte0Invariant0HashMatches)
                                    {
                                        ++__cte0CancellationCounter;
                                        if ((__cte0CancellationCounter & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        if (cte0.Count + cte0NextFrontier.Count >= 10000000)
                                        {
                                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("walk", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                        }

                                        cte0NextFrontier.Add(new Cte0Row0(e.Id));
                                    }
                                }
                            }

                            cte0.AddRange(cte0NextFrontier);
                            var __cte0FrontierSwap = cte0CurrentFrontier;
                            cte0CurrentFrontier = cte0NextFrontier;
                            cte0NextFrontier = __cte0FrontierSwap;
                        }
                    }

                    OnPhaseChanged("compiled:cte0", QueryPhase.Where);
                    OnPhaseChanged("compiled:cte0", QueryPhase.Select);
                    _cteRowResults.Slot0 = cte0;
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
                }

                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.From);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 walk = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(walk.Id));
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
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

        private readonly struct Cte0Invariant0Row0
        {
            public Cte0Invariant0Row0(int Id)
            {
                this.Id = Id;
            }

            public int Id { get; }
        }

        private readonly struct Cte0Row0
        {
            public Cte0Row0(int Id)
            {
                this.Id = Id;
            }

            public int Id { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0)
            {
                Id = __value0;
            }

            public override int Count => 1;
            public int Id { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id)
            {
                this.Id = Id;
            }

            public int Id { get; }
        }
    }
}
