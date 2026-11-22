// === Parsed Query ===
/*
with totals as (select r.Key as Id, Sum(r.Amount) as Total
from bench.rows() r
group by r.Key) select a.Id as Id, a.Total + b.Total as Total
from totals a
inner join totals b on a.Id = b.Id
order by a.Id
*/

// === Logical Plan ===
/*
Cte
  Definition [totals]
    MultiStatement
      Project [r.Key as r.Key, AggRef(r.Sum(r.Amount)) as r.Sum(r.Amount)]
        Aggregate [keys: r.Key] [aggs: Sum(Amount)]
          SchemaScan [#bench.rows() as r]
      Project [r.Key as Id, r.Sum(r.Amount) as Total]
        CteRef [rScore as rScore]
  Query
    MultiStatement
      Project [a.Id as a.Id, a.Total as a.Total, b.Id as b.Id, b.Total as b.Total]
        Join [Inner] [(a.Id = b.Id)]
          CteRef [totals as a]
          CteRef [totals as b]
      Sort [a.Id]
        Project [a.Id as Id, (a.Total + b.Total) as Total]
          CteRef [ab as ab]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [totals]
    PhysicalMultiStatement
      PhysicalProject [r.Key as r.Key, AggRef(r.Sum(r.Amount)) as r.Sum(r.Amount)]
        PhysicalSingleKeyAggregate [key: r.Key (Int32)] [aggs: Sum(Amount)]
          PhysicalSchemaScan [#bench.rows() as r]
      PhysicalProject [r.Key as Id, r.Sum(r.Amount) as Total]
        PhysicalCteRef [rScore as rScore]
  Query
    PhysicalMultiStatement
      PhysicalProject [a.Id as a.Id, a.Total as a.Total, b.Total as b.Total]
        PhysicalHashJoin [Inner] [build: b.Id] [probe: a.Id]
          PhysicalCteRef [totals as a]
          PhysicalCteRef [totals as b]
      PhysicalSort [a.Id]
        PhysicalProject [a.Id as Id, (a.Total + b.Total) as Total]
          PhysicalCteRef [ab as ab]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Key: int <- property Key
      Amount: decimal <- property Amount
    AggregateGroup [Cte0AggregateGroup; keys: 1; typed aggs: 1]
    Generated [Cte0Row0]
      Id: int <- field Id
      Total: decimal? <- field Total
    TableRow [a]
      Id: int <- field Id
      Total: decimal? <- field Total
    TableRow [b]
      Id: int <- field Id
      Total: decimal? <- field Total
    Generated [Statement0Row0]
      a.Id: int <- field a_Id
      a.Total: decimal? <- field a_Total
      b.Total: decimal? <- field b_Total
    TableRow [ab]
      a.Id: int <- field a_Id
      a.Total: decimal? <- field a_Total
      b.Total: decimal? <- field b_Total
    Generated [ResultRow0]
      Id: int <- field Id
      Total: decimal? <- field Total

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [From:cte0]
    SourceScan [r: FixtureRow] -> cte0_rRows
    CreateTable [cte0: Cte0Row0]
    PhaseBoundary [GroupBy:cte0]
    CreateSingleKeyAggregateContext [cte0Groups: int -> Cte0AggregateGroup]
    ChunkedForEach [r in cte0_rRows]
      GetOrAddSingleKeyAggregateGroup [cte0Group = cte0Groups[r.Key] by r.Key; typed: Cte0AggregateGroup]
      Let [amount: decimal = r.Amount]
      TypedAggregateSet [Set(cte0Group.__agg0, amount)]
    EnsureCapacity [cte0 <- cte0GroupsToFinalize.Count]
    PhaseBoundary [Select:cte0]
    ForEach [cte0FinalGroup in cte0GroupsToFinalize]
      AppendRow [cte0 <- Cte0Row0(Id: cte0FinalGroup.r.Key, Total: r.Sum(r.Amount))]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Begin:cte1]
    CreateTable [statement0: Statement0Row0]
    CreateHash [statement0BHash: int -> Row; capacity: _cteRowResults.Slot0.Count]
    ForEach [b in _cteRowResults.Slot0]
      HashAdd [statement0BHash[b.Id] += b]
    ForEach [a in _cteRowResults.Slot0]
      HashProbe [statement0BHash[a.Id] -> statement0BHashMatches]
        ForEach [b in statement0BHashMatches]
          AppendRow [statement0 <- Statement0Row0(a.Id: a.Id, a.Total: a.Total, b.Total: b.Total)]
    StoreTable [statement0 -> _cteRowResults.Slot1: List<Statement0Row0>]
    PhaseBoundary [End:cte1]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [ab in _cteRowResults.Slot1]
      AppendShape [result <- ResultShape0(Id: ab.a.Id, Total: (ab.a.Total + ab.b.Total))]
    SortOwnedBuffer [result by Id ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultShape0 from owned buffer result]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B21_CteReuse_BenchmarkTable
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
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Key", typeof(int), 0), new Column("Amount", typeof(decimal), 1) });
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Total", typeof(decimal?), 1)
        };
        private static readonly Column[] __columns_compiled_statement0_2 = new Column[]
        {
            new Column("a.Id", typeof(int), 0),
            new Column("a.Total", typeof(decimal?), 1),
            new Column("b.Total", typeof(decimal?), 2)
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
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_cte0_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_cte0_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
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
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var result = new List<ResultShape0>();
                var __storedTable1Rows = _cteRowResults.Slot1;
                for (int __storedTable1Index = 0; __storedTable1Index < __storedTable1Rows.Count; ++__storedTable1Index)
                {
                    if ((__storedTable1Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 ab = __storedTable1Rows[__storedTable1Index];
                    result.Add(new ResultShape0(ab.a_Id, (decimal?)(ab.a_Total + ab.b_Total)) { __musoqSequence = result.Count });
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
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                var __cte0_rSchema = provider.GetSchema("#bench");
                var cte0_rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__cte0_rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var cte0_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(cte0_rRowsSource.Chunks, __musoqProgressContext, "r:1") : cte0_rRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                var cte0GroupsToFinalize = new List<Cte0AggregateGroup>();
                var cte0Groups = new Dictionary<int, Cte0AggregateGroup>();
                foreach (var rChunk in cte0_rRows)
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
                                int groupKey = r.Key;
                                ref var cte0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Groups, groupKey, out var cte0GroupExists);
                                if (!cte0GroupExists)
                                {
                                    cte0GroupRef = new Cte0AggregateGroup(groupKey);
                                    cte0GroupsToFinalize.Add(cte0GroupRef);
                                }

                                Cte0AggregateGroup cte0Group = cte0GroupRef;
                                decimal amount = r.Amount;
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
                                int groupKey = r.Key;
                                ref var cte0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Groups, groupKey, out var cte0GroupExists);
                                if (!cte0GroupExists)
                                {
                                    cte0GroupRef = new Cte0AggregateGroup(groupKey);
                                    cte0GroupsToFinalize.Add(cte0GroupRef);
                                }

                                Cte0AggregateGroup cte0Group = cte0GroupRef;
                                decimal amount = r.Amount;
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

                    for (int rIndex = 0, rIndexCount = rChunk.Count; rIndex < rIndexCount; ++rIndex)
                    {
                        if ((rIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var r = rChunk[rIndex];
                        int groupKey = r.Key;
                        ref var cte0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Groups, groupKey, out var cte0GroupExists);
                        if (!cte0GroupExists)
                        {
                            cte0GroupRef = new Cte0AggregateGroup(groupKey);
                            cte0GroupsToFinalize.Add(cte0GroupRef);
                        }

                        Cte0AggregateGroup cte0Group = cte0GroupRef;
                        decimal amount = r.Amount;
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
                    cte0.Add(new Cte0Row0(cte0FinalGroup.__key0, (cte0FinalGroup.__agg0.HasValue ? (decimal?)cte0FinalGroup.__agg0.Value : null)));
                }

                return cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement0Row0> BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                var statement0 = new List<Statement0Row0>(_cteRowResults.Slot0.Count);
                var statement0BHash = new Dictionary<int, HashJoinBucket<Cte0Row0>>(_cteRowResults.Slot0.Count);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 b = __storedTable0Rows[__storedTable0Index];
                    int key = b.Id;
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<Cte0Row0>(b);
                        }
                        else
                        {
                            matches.Add(b);
                        }
                    }
                }

                var __storedTable0Rows1 = _cteRowResults.Slot0;
                for (int __storedTable0Rows1Index = 0; __storedTable0Rows1Index < __storedTable0Rows1.Count; ++__storedTable0Rows1Index)
                {
                    if ((__storedTable0Rows1Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 a = __storedTable0Rows1[__storedTable0Rows1Index];
                    int key = a.Id;
                    if (statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                    {
                        foreach (var b in statement0BHashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            statement0.Add(new Statement0Row0(a.Id, a.Total, b.Total));
                        }
                    }
                }

                return statement0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }
        }

        private sealed class Cte0AggregateGroup
        {
            public readonly int __key0;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg0;
            public Cte0AggregateGroup(int __key0)
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
            public int Id { get; }
            public decimal? Total { get; }

            public Cte0Row0(int __value0, decimal? __value1)
            {
                Id = __value0;
                Total = __value1;
            }
        }

        private sealed class Statement0Row0
        {
            public int a_Id { get; }
            public decimal? a_Total { get; }
            public decimal? b_Total { get; }

            public Statement0Row0(int __value0, decimal? __value1, decimal? __value2)
            {
                a_Id = __value0;
                a_Total = __value1;
                b_Total = __value2;
            }
        }

        private sealed class ResultShape0 : Row
        {
            public ResultShape0(int Id, decimal? Total)
            {
                this.Id = Id;
                this.Total = Total;
            }

            public int Id { get; private set; }
            public decimal? Total { get; private set; }
            public override int Count => 2;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Total" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public int __musoqSequence { get; init; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
            public List<Statement0Row0> Slot1;
        }
    }
}
