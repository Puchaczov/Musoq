// === Parsed Query ===
/*
select a.Position,
       (select Sum(b.Amount) from #schema.first() b
        where b.Partition = a.Partition) as Total
from #schema.first() a order by a.Position
*/

// === Logical Plan ===
/*
Cte
  Definition [_sq_1]
    MultiStatement
      Project [b.Partition as b.Partition, AggRef(b.Sum(b.Amount)) as b.Sum(b.Amount)]
        Aggregate [keys: b.Partition] [aggs: Sum(Amount)]
          SchemaScan [#schema.first() as b]
      Project [b.Partition as _sq_1_corr_0, b.Sum(b.Amount) as _sq_1_value]
        CteRef [bScore as bScore]
  Query
    MultiStatement
      Project [a.Position as a.Position, a.Partition as a.Partition, _sq_1._sq_1_corr_0 as _sq_1._sq_1_corr_0, _sq_1._sq_1_value as _sq_1._sq_1_value]
        Join [LeftSingle] [(_sq_1._sq_1_corr_0 = a.Partition)]
          SchemaScan [#schema.first() as a]
          CteRef [_sq_1 as _sq_1]
      Sort [a.Position]
        Project [a.Position as Position, _sq_1._sq_1_value as Total]
          CteRef [a_sq_1 as a_sq_1]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [_sq_1]
    PhysicalMultiStatement
      PhysicalProject [b.Partition as b.Partition, AggRef(b.Sum(b.Amount)) as b.Sum(b.Amount)]
        PhysicalSingleKeyAggregate [key: b.Partition (String)] [aggs: Sum(Amount)]
          PhysicalSchemaScan [#schema.first() as b]
      PhysicalProject [b.Partition as _sq_1_corr_0, b.Sum(b.Amount) as _sq_1_value]
        PhysicalCteRef [bScore as bScore]
  Query
    PhysicalMultiStatement
      PhysicalProject [a.Position as a.Position, _sq_1._sq_1_value as _sq_1._sq_1_value]
        PhysicalHashJoin [LeftSingle] [build: _sq_1._sq_1_corr_0] [probe: a.Partition]
          PhysicalSchemaScan [#schema.first() as a]
          PhysicalCteRef [_sq_1 as _sq_1]
      PhysicalSort [a.Position]
        PhysicalProject [a.Position as Position, _sq_1._sq_1_value as Total]
          PhysicalCteRef [a_sq_1 as a_sq_1]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [b: LibraryExecutionEntity]
      Partition: string <- property Partition
      Amount: decimal? <- property Amount
    AggregateGroup [Cte0AggregateGroup; keys: 1; typed aggs: 1]
    Generated [Cte0Row0]
      _sq_1_corr_0: string <- field _sq_1_corr_0
      _sq_1_value: decimal? <- field _sq_1_value
    SourceEntity [a: LibraryExecutionEntity]
      Position: int <- property Position
      Partition: string <- property Partition
    TableRow [_sq_1]
      _sq_1_corr_0: string <- field _sq_1_corr_0
      _sq_1_value: decimal? <- field _sq_1_value
    Generated [Statement0Row0]
      a.Position: int <- field a_Position
      _sq_1._sq_1_value: decimal? <- field _sq_1__sq_1_value
    TableRow [a_sq_1]
      a.Position: int <- field a_Position
      _sq_1._sq_1_value: decimal? <- field _sq_1__sq_1_value
    Generated [ResultRow0]
      Position: int <- field Position
      Total: decimal? <- field Total

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [From:cte0]
    SourceScan [b: LibraryExecutionEntity] -> cte0_bRows
    CreateTable [cte0: Cte0Row0]
    PhaseBoundary [GroupBy:cte0]
    CreateSingleKeyAggregateContext [cte0Groups: string -> Cte0AggregateGroup]
    ChunkedForEach [b in cte0_bRows]
      GetOrAddSingleKeyAggregateGroup [cte0Group = cte0Groups[b.Partition] by b.Partition; typed: Cte0AggregateGroup]
      Let [amount: decimal? = b.Amount]
      TypedAggregateSet [Set(cte0Group.__agg0, amount)]
    EnsureCapacity [cte0 <- cte0GroupsToFinalize.Count]
    PhaseBoundary [Select:cte0]
    ForEach [cte0FinalGroup in cte0GroupsToFinalize]
      AppendRow [cte0 <- Cte0Row0(_sq_1_corr_0: cte0FinalGroup.b.Partition, _sq_1_value: b.Sum(b.Amount))]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Begin:cte1]
    SourceScan [a: LibraryExecutionEntity] -> statement0_aRows
    CreateTable [statement0: Statement0Row0]
    CreateHash [statement0_sq_1Hash: string -> Row; capacity: _cteRowResults.Slot0.Count]
    ForEach [_sq_1 in _cteRowResults.Slot0]
      HashAdd [statement0_sq_1Hash[_sq_1._sq_1_corr_0] += _sq_1]
    ChunkedForEach [a in statement0_aRows]
      HashProbe [statement0_sq_1Hash[a.Partition] -> statement0_sq_1HashMatches] [match: statement0_sq_1HashHasMatch]
        ForEach [_sq_1 in statement0_sq_1HashMatches]
          Assign [statement0_sq_1HashHasMatch = TRUE]
          AppendRow [statement0 <- Statement0Row0(a.Position: a.Position, _sq_1._sq_1_value: _sq_1._sq_1_value)]
      HashProbeNoMatch
        AppendRow [statement0 <- Statement0Row0(a.Position: a.Position, _sq_1._sq_1_value: NULL)]
    StoreTable [statement0 -> _cteRowResults.Slot1: List<Statement0Row0>]
    PhaseBoundary [End:cte1]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [a_sq_1 in _cteRowResults.Slot1]
      AppendShape [result <- ResultShape0(Position: a_sq_1.a.Position, Total: a_sq_1._sq_1._sq_1_value)]
    SortOwnedBuffer [result by Position ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K07_LibraryCorrelatedScalarAggregate
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
            new Column("_sq_1_corr_0", typeof(string), 0),
            new Column("_sq_1_value", typeof(decimal?), 1)
        };
        private static readonly Column[] __columns_compiled_result_4 = new Column[]
        {
            new Column("Position", typeof(int), 0),
            new Column("Total", typeof(decimal?), 1)
        };
        private static readonly Column[] __columns_compiled_statement0_3 = new Column[]
        {
            new Column("a.Position", typeof(int), 0),
            new Column("_sq_1._sq_1_value", typeof(decimal?), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_2 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Partition", typeof(string), 1) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Partition", typeof(string), 0), new Column("Amount", typeof(decimal?), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_4, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Total);
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

                    Statement0Row0 a_sq_1 = __storedTable1Rows[__storedTable1Index];
                    result.Add(new ResultShape0(a_sq_1.a_Position, a_sq_1._sq_1__sq_1_value) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Position.CompareTo(right.Position);
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                var __cte0_bSchema = provider.GetSchema("#schema");
                var cte0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__cte0_bSchema, "first", new SourceExecutionContext("b:2", sourceExecutionPlans["b:2"], token, __schemaColumns_compiled_b_0, sourceRuntimeSettingsBySourceContextId["b:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var cte0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(cte0_bRowsSource.Chunks, __musoqProgressContext, "b:2") : cte0_bRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                var cte0GroupsToFinalize = new List<Cte0AggregateGroup>();
                var cte0Groups = new Dictionary<string, Cte0AggregateGroup>();
                Cte0AggregateGroup cte0NullGroup = null;
                foreach (var bChunk in cte0_bRows)
                {
                    if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> bChunkView)
                    {
                        if (bChunkView.Source is Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity[] bChunkViewArray)
                        {
                            int bChunkViewOffset = bChunkView.Offset;
                            for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                            {
                                if ((bIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var b = bChunkViewArray[bChunkViewOffset + bIndex];
                                string groupKey = b.Partition;
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

                                decimal? amount = b.Amount;
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

                        if (bChunkView.Source is List<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> bChunkViewList)
                        {
                            int bChunkViewOffset = bChunkView.Offset;
                            for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                            {
                                if ((bIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var b = bChunkViewList[bChunkViewOffset + bIndex];
                                string groupKey = b.Partition;
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

                                decimal? amount = b.Amount;
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

                    for (int bIndex = 0, bIndexCount = bChunk.Count; bIndex < bIndexCount; ++bIndex)
                    {
                        if ((bIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var b = bChunk[bIndex];
                        string groupKey = b.Partition;
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

                        decimal? amount = b.Amount;
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement0Row0> BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                var __statement0_aSchema = provider.GetSchema("#schema");
                var statement0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__statement0_aSchema, "first", new SourceExecutionContext("a:2", sourceExecutionPlans["a:2"], token, __schemaColumns_compiled_a_2, sourceRuntimeSettingsBySourceContextId["a:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var statement0_aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:2") : statement0_aRowsSource.Chunks;
                var statement0 = new List<Statement0Row0>();
                var statement0_sq_1Hash = new Dictionary<string, HashJoinBucket<Cte0Row0>>(_cteRowResults.Slot0.Count);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 _sq_1 = __storedTable0Rows[__storedTable0Index];
                    string key = _sq_1._sq_1_corr_0;
                    if (key == null)
                        continue;
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0_sq_1Hash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<Cte0Row0>(_sq_1);
                        }
                        else
                        {
                            matches.Add(_sq_1);
                        }
                    }
                }

                foreach (var aChunk in statement0_aRows)
                {
                    if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> aChunkView)
                    {
                        if (aChunkView.Source is Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity[] aChunkViewArray)
                        {
                            int aChunkViewOffset = aChunkView.Offset;
                            for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                            {
                                if ((aIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var a = aChunkViewArray[aChunkViewOffset + aIndex];
                                bool statement0_sq_1HashHasMatch = false;
                                string key = a.Partition;
                                if (key != null && statement0_sq_1Hash.TryGetValue(key, out var statement0_sq_1HashMatches))
                                {
                                    foreach (var _sq_1 in statement0_sq_1HashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        statement0_sq_1HashHasMatch = true;
                                        statement0.Add(new Statement0Row0(a.Position, _sq_1._sq_1_value));
                                    }
                                }

                                if (!statement0_sq_1HashHasMatch)
                                {
                                    statement0.Add(new Statement0Row0(a.Position, null));
                                }
                            }

                            continue;
                        }

                        if (aChunkView.Source is List<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> aChunkViewList)
                        {
                            int aChunkViewOffset = aChunkView.Offset;
                            for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                            {
                                if ((aIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var a = aChunkViewList[aChunkViewOffset + aIndex];
                                bool statement0_sq_1HashHasMatch = false;
                                string key = a.Partition;
                                if (key != null && statement0_sq_1Hash.TryGetValue(key, out var statement0_sq_1HashMatches))
                                {
                                    foreach (var _sq_1 in statement0_sq_1HashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        statement0_sq_1HashHasMatch = true;
                                        statement0.Add(new Statement0Row0(a.Position, _sq_1._sq_1_value));
                                    }
                                }

                                if (!statement0_sq_1HashHasMatch)
                                {
                                    statement0.Add(new Statement0Row0(a.Position, null));
                                }
                            }

                            continue;
                        }
                    }

                    for (int aIndex = 0, aIndexCount = aChunk.Count; aIndex < aIndexCount; ++aIndex)
                    {
                        if ((aIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var a = aChunk[aIndex];
                        bool statement0_sq_1HashHasMatch = false;
                        string key = a.Partition;
                        if (key != null && statement0_sq_1Hash.TryGetValue(key, out var statement0_sq_1HashMatches))
                        {
                            foreach (var _sq_1 in statement0_sq_1HashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                statement0_sq_1HashHasMatch = true;
                                statement0.Add(new Statement0Row0(a.Position, _sq_1._sq_1_value));
                            }
                        }

                        if (!statement0_sq_1HashHasMatch)
                        {
                            statement0.Add(new Statement0Row0(a.Position, null));
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
                _sq_1_corr_0 = __value0;
                _sq_1_value = __value1;
            }

            public string _sq_1_corr_0 { get; }
            public decimal? _sq_1_value { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
            public List<Statement0Row0> Slot1;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, decimal? __value1)
            {
                Position = __value0;
                Total = __value1;
            }

            public override int Count => 2;
            public int Position { get; private set; }
            public decimal? Total { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "Total" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Position, decimal? Total)
            {
                this.Position = Position;
                this.Total = Total;
            }

            public int Position { get; }
            public decimal? Total { get; }
            public int __musoqSequence { get; init; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(int __value0, decimal? __value1)
            {
                a_Position = __value0;
                _sq_1__sq_1_value = __value1;
            }

            public decimal? _sq_1__sq_1_value { get; }
            public int a_Position { get; }
        }
    }
}
