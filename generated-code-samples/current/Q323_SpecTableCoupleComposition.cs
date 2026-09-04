// === Parsed Query ===
/*
table Row {
                        Id: int,
                        Name: string,
                        Population: decimal
                    };
                    couple #A.entities with table Row as LeftRows;
                    couple #B.entities with table Row as RightRows;
                    with Expanded as (
                        select l.Id, l.Name, l.Population
                        from LeftRows() l
                        cross apply RightRows() r
                        where l.Id = r.Id
                    ), Joined as (
                        select e.Name, e.Population
                        from Expanded e
                        inner join RightRows() r on e.Name = r.Name
                    )
                    select Name, Sum(Population) as Total
                    from Joined
                    group by Name
                    union (Name)
                    select Name, Sum(Population) as Total
                    from LeftRows()
                    group by Name
*/

// === Logical Plan ===
/*
Cte
  Definition [Expanded]
    MultiStatement
      Project [l.Name as l.Name, l.Population as l.Population, l.Id as l.Id, r.Id as r.Id]
        Apply [Cross]
          SchemaScan [#A.entities() as l]
          SchemaScan [#B.entities() as r]
      Project [l.Id as l.Id, l.Name as l.Name, l.Population as l.Population]
        Filter [(l.Id = r.Id)]
          CteRef [lr as lr]
  Definition [Joined]
    MultiStatement
      Project [e.Name as e.Name, e.Population as e.Population, r.Name as r.Name]
        Join [Inner] [(e.Name = r.Name)]
          CteRef [Expanded as e]
          SchemaScan [#B.entities() as r]
      Project [e.Name as e.Name, e.Population as e.Population]
        CteRef [er as er]
  Query
    SetOp [Union]
      MultiStatement
        Project [Joined.Name as Joined.Name, AggRef(Joined.Sum(Joined.Population)) as Joined.Sum(Joined.Population)]
          Aggregate [keys: Name] [aggs: Sum(Population)]
            CteRef [Joined as Joined]
        Project [Joined.Name as Name, Joined.Sum(Joined.Population) as Total]
          CteRef [joinedScore as joinedScore]
      MultiStatement
        Project [nnr1je.Name as nnr1je.Name, AggRef(nnr1je.Sum(nnr1je.Population)) as nnr1je.Sum(nnr1je.Population)]
          Aggregate [keys: Name] [aggs: Sum(Population)]
            SchemaScan [#A.entities() as nnr1je]
        Project [nnr1je.Name as Name, nnr1je.Sum(nnr1je.Population) as Total]
          CteRef [nnr1jeScore as nnr1jeScore]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [Expanded]
    PhysicalMultiStatement
      PhysicalProject [l.Name as l.Name, l.Population as l.Population, l.Id as l.Id, r.Id as r.Id]
        PhysicalNestedLoopApply [Cross]
          PhysicalSchemaScan [#A.entities() as l]
          PhysicalSchemaScan [#B.entities() as r]
      PhysicalProject [l.Id as l.Id, l.Name as l.Name, l.Population as l.Population]
        PhysicalFilter [(l.Id = r.Id)]
          PhysicalCteRef [lr as lr]
  Definition [Joined]
    PhysicalMultiStatement
      PhysicalProject [e.Name as e.Name, e.Population as e.Population, r.Name as r.Name]
        PhysicalHashJoin [Inner] [build: e.Name] [probe: r.Name]
          PhysicalCteRef [Expanded as e]
          PhysicalSchemaScan [#B.entities() as r]
      PhysicalProject [e.Name as e.Name, e.Population as e.Population]
        PhysicalCteRef [er as er]
  Query
    PhysicalSetOp [Union]
      PhysicalMultiStatement
        PhysicalProject [Joined.Name as Joined.Name, AggRef(Joined.Sum(Joined.Population)) as Joined.Sum(Joined.Population)]
          PhysicalSingleKeyAggregate [key: Name (String)] [aggs: Sum(Population)]
            PhysicalCteRef [Joined as Joined]
        PhysicalProject [Joined.Name as Name, Joined.Sum(Joined.Population) as Total]
          PhysicalCteRef [joinedScore as joinedScore]
      PhysicalMultiStatement
        PhysicalProject [nnr1je.Name as nnr1je.Name, AggRef(nnr1je.Sum(nnr1je.Population)) as nnr1je.Sum(nnr1je.Population)]
          PhysicalSingleKeyAggregate [key: Name (String)] [aggs: Sum(Population)]
            PhysicalSchemaScan [#A.entities() as nnr1je]
        PhysicalProject [nnr1je.Name as Name, nnr1je.Sum(nnr1je.Population) as Total]
          PhysicalCteRef [nnr1jeScore as nnr1jeScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [l: BasicEntity]
      Name: string <- property Name
      Population: decimal <- property Population
      Id: int <- property Id
    SourceEntity [r: BasicEntity]
      Name: string <- property Name
      Id: int <- property Id
    Generated [Cte0Row0]
      l.Id: int <- field l_Id
      l.Name: string <- field l_Name
      l.Population: decimal <- field l_Population
    TableRow [e]
      l.Id: int <- field l_Id
      l.Name: string <- field l_Name
      l.Population: decimal <- field l_Population
    SourceEntity [r: BasicEntity]
      Name: string <- property Name
      Id: int <- property Id
    Generated [Cte1Row0]
      e.Name: string <- field e_Name
      e.Population: decimal <- field e_Population
    TableRow [Joined]
      e.Name: string <- field e_Name
      e.Population: decimal <- field e_Population
    AggregateGroup [LeftAggregateGroup; keys: 1; typed aggs: 1]
    Generated [LeftRow0]
      Name: string <- field Name
      Total: decimal? <- field Total
    SourceEntity [nnr1je: BasicEntity]
      Name: string <- property Name
      Population: decimal <- property Population
    AggregateGroup [RightAggregateGroup; keys: 1; typed aggs: 1]
    Generated [RightRow0]
      Name: string <- field Name
      Total: decimal? <- field Total

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte2]
    SourceScan [l: BasicEntity] -> cte0_lRows
    CreateTable [cte0: Cte0Row0]
    ChunkedForEach [l in cte0_lRows]
      SourceScan [r: BasicEntity] -> cte0_rRows
      ChunkedForEach [r in cte0_rRows]
        Let [id: int = l.Id]
        If [(id = r.Id)]
          AppendRow [cte0 <- Cte0Row0(l.Id: id, l.Name: l.Name, l.Population: l.Population)]
    PhaseBoundary [End:cte2]
    PhaseBoundary [Where:cte0]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Begin:cte1]
    PhaseBoundary [From:cte1]
    PhaseBoundary [Begin:cte2]
    SourceScan [r: BasicEntity] -> cte1_rRows
    CreateTable [cte1: Cte1Row0]
    CreateHash [cte1EHash: string -> Row; capacity: _cteRowResults.Slot0.Count]
    ForEach [e in _cteRowResults.Slot0]
      HashAdd [cte1EHash[e.l.Name] += e]
    ChunkedForEach [r in cte1_rRows]
      HashProbe [cte1EHash[r.Name] -> cte1EHashMatches]
        ForEach [e in cte1EHashMatches]
          AppendRow [cte1 <- Cte1Row0(e.Name: e.l.Name, e.Population: e.l.Population)]
    PhaseBoundary [End:cte2]
    PhaseBoundary [Select:cte1]
    StoreTable [cte1 -> _cteRowResults.Slot1: List<Cte1Row0>]
    PhaseBoundary [End:cte1]
    PhaseBoundary [Begin:left]
    CreateRowBuffer [left: List<LeftRow0>]
    PhaseBoundary [GroupBy:left]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [leftGroups: string -> LeftAggregateGroup]
    ParallelSingleKeyAggregateLoop [Joined in _cteRowResults.Slot1 by Joined.e.Name; threshold 4096, sample 8192/6144, maxDegree 24, group LeftAggregateGroup]
      ParallelAccumulate
        Let [e_Population: decimal = Joined.e.Population]
        TypedAggregateSet [Set(leftGroup.__agg0, e_Population)]
    EnsureRowBufferCapacity [left <- leftGroupsToFinalize.Count]
    PhaseBoundary [Select:left]
    ForEach [leftFinalGroup in leftGroupsToFinalize]
      AppendRowBuffer [left <- LeftRow0(Name: leftFinalGroup.Name, Total: Joined.Sum(Joined.Population))]
    PhaseBoundary [End:left]
    PhaseBoundary [Begin:right]
    PhaseBoundary [From:right]
    SourceScan [nnr1je: BasicEntity] -> right_nnr1jeRows
    CreateRowBuffer [right: List<RightRow0>]
    PhaseBoundary [GroupBy:right]
    CreateSingleKeyAggregateContext [rightGroups: string -> RightAggregateGroup]
    ParallelSingleKeyAggregateLoop [nnr1je in right_nnr1jeRows by nnr1je.Name; threshold 4096, sample 8192/6144, maxDegree 24, group RightAggregateGroup]
      ParallelAccumulate
        Let [population: decimal = nnr1je.Population]
        TypedAggregateSet [Set(rightGroup.__agg0, population)]
    EnsureRowBufferCapacity [right <- rightGroupsToFinalize.Count]
    PhaseBoundary [Select:right]
    ForEach [rightFinalGroup in rightGroupsToFinalize]
      AppendRowBuffer [right <- RightRow0(Name: rightFinalGroup.Name, Total: nnr1je.Sum(nnr1je.Population))]
    PhaseBoundary [End:right]
    SetOperation [result = left Union right, HashSet]
    ReturnDeferredTable [result: LeftRow0 <- LeftShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q323_SpecTableCoupleComposition
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("l.Id", typeof(int), 0),
            new Column("l.Name", typeof(string), 1),
            new Column("l.Population", typeof(decimal), 2)
        };
        private static readonly Column[] __columns_compiled_cte1_3 = new Column[]
        {
            new Column("e.Name", typeof(string), 0),
            new Column("e.Population", typeof(decimal), 1)
        };
        private static readonly Column[] __columns_compiled_left_4 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("Total", typeof(decimal?), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_l_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("Population", typeof(decimal), 1), new Column("Id", typeof(int), 2) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_nnr1je_5 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("Population", typeof(decimal), 1) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_2 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("Id", typeof(int), 1) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public IDictionary<string, System.Object> Parameters { get; } = new Dictionary<string, System.Object>(StringComparer.Ordinal);
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }

        public event DataSourceEventHandler DataSourceProgress;
        public event QueryPhaseEventHandler PhaseChanged;
        public event QueryProgressEventHandler QueryProgress;
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<LeftRow0>("result", __columns_compiled_left_4, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<LeftRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new LeftRow0(__musoqShapeRow.Name, __musoqShapeRow.Total);
            }
        }

        private IEnumerable<LeftShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<LeftShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled:left", QueryPhase.Begin);
                var left = new List<LeftRow0>();
                OnPhaseChanged("compiled:left", QueryPhase.GroupBy);
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var leftGroupsToFinalize = new List<AggregateGroup0>();
                var leftGroups = new Dictionary<string, AggregateGroup0>();
                var leftGroupsToFinalizeParallelRows = EvaluationHelper.GetParallelAggregationRowsOrEmpty<Cte1Row0>(_cteRowResults.Slot1, 4096);
                leftGroupsToFinalize = ParallelSingleKeyAggregate_0(leftGroupsToFinalizeParallelRows, 24, token);
                left.EnsureCapacity(leftGroupsToFinalize.Count);
                OnPhaseChanged("compiled:left", QueryPhase.Select);
                foreach (var leftFinalGroup in leftGroupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    left.Add(new LeftRow0(leftFinalGroup.__key0, leftFinalGroup.__agg0.HasValue ? (decimal?)leftFinalGroup.__agg0.Value : null));
                }

                OnPhaseChanged("compiled:left", QueryPhase.End);
                OnPhaseChanged("compiled:right", QueryPhase.Begin);
                OnPhaseChanged("compiled:right", QueryPhase.From);
                var __right_nnr1jeSchema = provider.GetSchema("#A");
                var right_nnr1jeRowsSource = __right_nnr1jeSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("nnr1je:4", sourceExecutionPlans["nnr1je:4"], token, __schemaColumns_compiled_nnr1je_5, sourceRuntimeSettingsBySourceContextId["nnr1je:4"], logger, OnDataSourceProgress), Array.Empty<object>());
                var right_nnr1jeRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(right_nnr1jeRowsSource.Chunks, __musoqProgressContext, "nnr1je:4") : right_nnr1jeRowsSource.Chunks;
                var right = new List<RightRow0>();
                OnPhaseChanged("compiled:right", QueryPhase.GroupBy);
                var rightGroupsToFinalize = new List<AggregateGroup0>();
                var rightGroups = new Dictionary<string, AggregateGroup0>();
                rightGroupsToFinalize = ParallelSingleKeyAggregate_1(right_nnr1jeRows, 24, token);
                right.EnsureCapacity(rightGroupsToFinalize.Count);
                OnPhaseChanged("compiled:right", QueryPhase.Select);
                foreach (var rightFinalGroup in rightGroupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    right.Add(new RightRow0(rightFinalGroup.__key0, rightFinalGroup.__agg0.HasValue ? (decimal?)rightFinalGroup.__agg0.Value : null));
                }

                OnPhaseChanged("compiled:right", QueryPhase.End);
                var resultKeys = new HashSet<string>(left.Count + right.Count);
                foreach (var resultLeftRow in left)
                {
                    if (resultKeys.Add((string)resultLeftRow.Name))
                    {
                        __musoqFinalShapeRows.Add(new LeftShape0((string)resultLeftRow.Name, (decimal?)resultLeftRow.Total));
                    }
                }

                foreach (var resultRightRow in right)
                {
                    if (resultKeys.Add((string)resultRightRow.Name))
                    {
                        __musoqFinalShapeRows.Add(new LeftShape0((string)resultRightRow.Name, (decimal?)resultRightRow.Total));
                    }
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
                var __cte0_lSchema = provider.GetSchema("#A");
                var cte0_lRowsSource = __cte0_lSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("l:1", sourceExecutionPlans["l:1"], token, __schemaColumns_compiled_l_0, sourceRuntimeSettingsBySourceContextId["l:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte0_lRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0_lRowsSource.Chunks, __musoqProgressContext, "l:1") : cte0_lRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                foreach (var lChunk in cte0_lRows)
                {
                    if (lChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> lChunkView)
                    {
                        if (lChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] lChunkViewArray)
                        {
                            int lChunkViewOffset = lChunkView.Offset;
                            for (int lIndex = 0, lIndexCount = lChunkView.Count; lIndex < lIndexCount; ++lIndex)
                            {
                                if ((lIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var l = lChunkViewArray[lChunkViewOffset + lIndex];
                                var __cte0_rSchema = provider.GetSchema("#B");
                                var cte0_rRowsSource = __cte0_rSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_2, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                                var cte0_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0_rRowsSource.Chunks, __musoqProgressContext, "r:1") : cte0_rRowsSource.Chunks;
                                foreach (var rChunk in cte0_rRows)
                                {
                                    if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> rChunkView)
                                    {
                                        if (rChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] rChunkViewArray)
                                        {
                                            int rChunkViewOffset = rChunkView.Offset;
                                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                                            {
                                                if ((rIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                var r = rChunkViewArray[rChunkViewOffset + rIndex];
                                                int id = l.Id;
                                                if ((id == r.Id))
                                                {
                                                    cte0.Add(new Cte0Row0(id, l.Name, l.Population));
                                                }
                                            }

                                            continue;
                                        }

                                        if (rChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> rChunkViewList)
                                        {
                                            int rChunkViewOffset = rChunkView.Offset;
                                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                                            {
                                                if ((rIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                var r = rChunkViewList[rChunkViewOffset + rIndex];
                                                int id = l.Id;
                                                if ((id == r.Id))
                                                {
                                                    cte0.Add(new Cte0Row0(id, l.Name, l.Population));
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
                                        int id = l.Id;
                                        if ((id == r.Id))
                                        {
                                            cte0.Add(new Cte0Row0(id, l.Name, l.Population));
                                        }
                                    }
                                }
                            }

                            continue;
                        }

                        if (lChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> lChunkViewList)
                        {
                            int lChunkViewOffset = lChunkView.Offset;
                            for (int lIndex = 0, lIndexCount = lChunkView.Count; lIndex < lIndexCount; ++lIndex)
                            {
                                if ((lIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var l = lChunkViewList[lChunkViewOffset + lIndex];
                                var __cte0_rSchema = provider.GetSchema("#B");
                                var cte0_rRowsSource = __cte0_rSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_2, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                                var cte0_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0_rRowsSource.Chunks, __musoqProgressContext, "r:1") : cte0_rRowsSource.Chunks;
                                foreach (var rChunk in cte0_rRows)
                                {
                                    if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> rChunkView)
                                    {
                                        if (rChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] rChunkViewArray)
                                        {
                                            int rChunkViewOffset = rChunkView.Offset;
                                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                                            {
                                                if ((rIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                var r = rChunkViewArray[rChunkViewOffset + rIndex];
                                                int id = l.Id;
                                                if ((id == r.Id))
                                                {
                                                    cte0.Add(new Cte0Row0(id, l.Name, l.Population));
                                                }
                                            }

                                            continue;
                                        }

                                        if (rChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> rChunkViewList)
                                        {
                                            int rChunkViewOffset = rChunkView.Offset;
                                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                                            {
                                                if ((rIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                var r = rChunkViewList[rChunkViewOffset + rIndex];
                                                int id = l.Id;
                                                if ((id == r.Id))
                                                {
                                                    cte0.Add(new Cte0Row0(id, l.Name, l.Population));
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
                                        int id = l.Id;
                                        if ((id == r.Id))
                                        {
                                            cte0.Add(new Cte0Row0(id, l.Name, l.Population));
                                        }
                                    }
                                }
                            }

                            continue;
                        }
                    }

                    for (int lIndex = 0, lIndexCount = lChunk.Count; lIndex < lIndexCount; ++lIndex)
                    {
                        if ((lIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var l = lChunk[lIndex];
                        var __cte0_rSchema = provider.GetSchema("#B");
                        var cte0_rRowsSource = __cte0_rSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_2, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                        var cte0_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0_rRowsSource.Chunks, __musoqProgressContext, "r:1") : cte0_rRowsSource.Chunks;
                        foreach (var rChunk in cte0_rRows)
                        {
                            if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> rChunkView)
                            {
                                if (rChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] rChunkViewArray)
                                {
                                    int rChunkViewOffset = rChunkView.Offset;
                                    for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                                    {
                                        if ((rIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var r = rChunkViewArray[rChunkViewOffset + rIndex];
                                        int id = l.Id;
                                        if ((id == r.Id))
                                        {
                                            cte0.Add(new Cte0Row0(id, l.Name, l.Population));
                                        }
                                    }

                                    continue;
                                }

                                if (rChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> rChunkViewList)
                                {
                                    int rChunkViewOffset = rChunkView.Offset;
                                    for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                                    {
                                        if ((rIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var r = rChunkViewList[rChunkViewOffset + rIndex];
                                        int id = l.Id;
                                        if ((id == r.Id))
                                        {
                                            cte0.Add(new Cte0Row0(id, l.Name, l.Population));
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
                                int id = l.Id;
                                if ((id == r.Id))
                                {
                                    cte0.Add(new Cte0Row0(id, l.Name, l.Population));
                                }
                            }
                        }
                    }
                }

                return cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte1Row0> BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                var __cte1_rSchema = provider.GetSchema("#B");
                var cte1_rRowsSource = __cte1_rSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("r:2", sourceExecutionPlans["r:2"], token, __schemaColumns_compiled_r_2, sourceRuntimeSettingsBySourceContextId["r:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte1_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte1_rRowsSource.Chunks, __musoqProgressContext, "r:2") : cte1_rRowsSource.Chunks;
                var cte1 = new List<Cte1Row0>();
                var cte1EHash = new Dictionary<string, HashJoinBucket<Cte0Row0>>(_cteRowResults.Slot0.Count);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 e = __storedTable0Rows[__storedTable0Index];
                    string key = e.l_Name;
                    if (key == null)
                        continue;
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1EHash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<Cte0Row0>(e);
                        }
                        else
                        {
                            matches.Add(e);
                        }
                    }
                }

                foreach (var rChunk in cte1_rRows)
                {
                    if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> rChunkView)
                    {
                        if (rChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] rChunkViewArray)
                        {
                            int rChunkViewOffset = rChunkView.Offset;
                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                            {
                                if ((rIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var r = rChunkViewArray[rChunkViewOffset + rIndex];
                                string key = r.Name;
                                if (key != null && cte1EHash.TryGetValue(key, out var cte1EHashMatches))
                                {
                                    foreach (var e in cte1EHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        cte1.Add(new Cte1Row0(e.l_Name, e.l_Population));
                                    }
                                }
                            }

                            continue;
                        }

                        if (rChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> rChunkViewList)
                        {
                            int rChunkViewOffset = rChunkView.Offset;
                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                            {
                                if ((rIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var r = rChunkViewList[rChunkViewOffset + rIndex];
                                string key = r.Name;
                                if (key != null && cte1EHash.TryGetValue(key, out var cte1EHashMatches))
                                {
                                    foreach (var e in cte1EHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        cte1.Add(new Cte1Row0(e.l_Name, e.l_Population));
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
                        string key = r.Name;
                        if (key != null && cte1EHash.TryGetValue(key, out var cte1EHashMatches))
                        {
                            foreach (var e in cte1EHashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                cte1.Add(new Cte1Row0(e.l_Name, e.l_Population));
                            }
                        }
                    }
                }

                return cte1;
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void ParallelSingleKeyAggregateChunk_1(IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> chunk, Dictionary<string, AggregateGroup0> groups, List<AggregateGroup0> orderedGroups, ref AggregateGroup0 nullGroup, CancellationToken cancellationToken)
        {
            for (var index = 0; index < chunk.Count; index++)
            {
                if ((index & 1023) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                Musoq.Evaluator.Tests.Schema.Basic.BasicEntity nnr1je = chunk[index];
                string groupKey = nnr1je.Name;
                AggregateGroup0 rightGroup = null;
                if (groupKey != null)
                {
                    ref var rightGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var rightGroupExists);
                    if (!rightGroupExists)
                    {
                        rightGroupRef = new AggregateGroup0(groupKey);
                        orderedGroups.Add(rightGroupRef);
                    }

                    rightGroup = rightGroupRef;
                }
                else
                {
                    if (nullGroup == null)
                    {
                        nullGroup = new AggregateGroup0(groupKey);
                        orderedGroups.Add(nullGroup);
                    }

                    rightGroup = nullGroup;
                }

                decimal population = nnr1je.Population;
                {
                    var __agg0Input = (decimal?)population;
                    if (__agg0Input.HasValue)
                    {
                        var __agg0Current = __agg0Input.GetValueOrDefault();
                        rightGroup.__agg0.Value = rightGroup.__agg0.HasValue ? checked(rightGroup.__agg0.Value + __agg0Current) : __agg0Current;
                        rightGroup.__agg0.HasValue = true;
                    }
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void ParallelSingleKeyAggregateShard_0(IReadOnlyList<Cte1Row0> rows, int workerCount, List<AggregateGroup0>[] shards, CancellationToken cancellationToken, int shardIndex)
        {
            var start = rows.Count * shardIndex / workerCount;
            var end = rows.Count * (shardIndex + 1) / workerCount;
            var groups = new Dictionary<string, AggregateGroup0>();
            var orderedGroups = new List<AggregateGroup0>();
            AggregateGroup0 nullGroup = null;
            for (var index = start; index < end; index++)
            {
                if ((index & 1023) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                Cte1Row0 Joined = rows[index];
                string groupKey = Joined.e_Name;
                AggregateGroup0 leftGroup = null;
                if (groupKey != null)
                {
                    ref var leftGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var leftGroupExists);
                    if (!leftGroupExists)
                    {
                        leftGroupRef = new AggregateGroup0(groupKey);
                        orderedGroups.Add(leftGroupRef);
                    }

                    leftGroup = leftGroupRef;
                }
                else
                {
                    if (nullGroup == null)
                    {
                        nullGroup = new AggregateGroup0(groupKey);
                        orderedGroups.Add(nullGroup);
                    }

                    leftGroup = nullGroup;
                }

                decimal e_Population = Joined.e_Population;
                {
                    var __agg0Input = (decimal?)e_Population;
                    if (__agg0Input.HasValue)
                    {
                        var __agg0Current = __agg0Input.GetValueOrDefault();
                        leftGroup.__agg0.Value = leftGroup.__agg0.HasValue ? checked(leftGroup.__agg0.Value + __agg0Current) : __agg0Current;
                        leftGroup.__agg0.HasValue = true;
                    }
                }
            }

            shards[shardIndex] = orderedGroups;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<AggregateGroup0> ParallelSingleKeyAggregate_0(IReadOnlyList<Cte1Row0> rows, int maxDegreeOfParallelism, CancellationToken cancellationToken)
        {
            if (rows.Count == 0)
            {
                return new List<AggregateGroup0>();
            }

            var workerCount = Math.Min(Math.Max(1, maxDegreeOfParallelism), rows.Count);
            List<AggregateGroup0>[] shards = new List<AggregateGroup0>[workerCount];
            var options = new ParallelOptions()
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = workerCount
            };
            var worker = new ParallelSingleKeyAggregateWorker_0(rows, workerCount, shards, cancellationToken);
            Parallel.For(0, workerCount, options, worker.Run);
            var mergedGroups = new Dictionary<string, AggregateGroup0>();
            var groupsToFinalize = new List<AggregateGroup0>();
            AggregateGroup0 nullGroup = null;
            foreach (var shard in shards)
            {
                foreach (var sourceGroup in shard)
                {
                    string groupKey = sourceGroup.__key0;
                    if (groupKey != null)
                    {
                        ref var mergedGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(mergedGroups, groupKey, out var mergedGroupExists);
                        if (!mergedGroupExists)
                        {
                            mergedGroupRef = sourceGroup;
                            groupsToFinalize.Add(sourceGroup);
                        }
                        else
                        {
                            mergedGroupRef.MergeFrom(sourceGroup);
                        }
                    }
                    else
                    {
                        if (nullGroup == null)
                        {
                            nullGroup = sourceGroup;
                            groupsToFinalize.Add(sourceGroup);
                        }
                        else
                        {
                            nullGroup.MergeFrom(sourceGroup);
                        }
                    }
                }
            }

            return groupsToFinalize;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<AggregateGroup0> ParallelSingleKeyAggregate_1(IEnumerable<IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>> rows, int maxDegreeOfParallelism, CancellationToken cancellationToken)
        {
            var workerCount = Math.Max(1, maxDegreeOfParallelism);
            var shards = new global::System.Collections.Concurrent.ConcurrentQueue<List<AggregateGroup0>>();
            var options = new ParallelOptions()
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = workerCount
            };
            Parallel.ForEach<IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>, ParallelSingleKeyAggregateChunkWorker_1>(rows, options, () => new ParallelSingleKeyAggregateChunkWorker_1(cancellationToken), (chunk, _, worker) =>
            {
                worker.ProcessChunk(chunk ?? Array.Empty<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>());
                return worker;
            }, worker =>
            {
                if (worker.OrderedGroups.Count != 0)
                {
                    shards.Enqueue(worker.OrderedGroups);
                }
            });
            var mergedGroups = new Dictionary<string, AggregateGroup0>();
            var groupsToFinalize = new List<AggregateGroup0>();
            AggregateGroup0 nullGroup = null;
            foreach (var shard in shards)
            {
                foreach (var sourceGroup in shard)
                {
                    string groupKey = sourceGroup.__key0;
                    if (groupKey != null)
                    {
                        ref var mergedGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(mergedGroups, groupKey, out var mergedGroupExists);
                        if (!mergedGroupExists)
                        {
                            mergedGroupRef = sourceGroup;
                            groupsToFinalize.Add(sourceGroup);
                        }
                        else
                        {
                            mergedGroupRef.MergeFrom(sourceGroup);
                        }
                    }
                    else
                    {
                        if (nullGroup == null)
                        {
                            nullGroup = sourceGroup;
                            groupsToFinalize.Add(sourceGroup);
                        }
                        else
                        {
                            nullGroup.MergeFrom(sourceGroup);
                        }
                    }
                }
            }

            return groupsToFinalize;
        }

        private sealed class AggregateGroup0
        {
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg0;
            public readonly string __key0;
            public AggregateGroup0(string __key0)
            {
                this.__key0 = __key0;
            }

            public void MergeFrom(AggregateGroup0 source)
            {
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
            }
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(int __value0, string __value1, decimal __value2)
            {
                l_Id = __value0;
                l_Name = __value1;
                l_Population = __value2;
            }

            public int l_Id { get; }
            public string l_Name { get; }
            public decimal l_Population { get; }
        }

        private sealed class Cte1Row0
        {
            public Cte1Row0(string __value0, decimal __value1)
            {
                e_Name = __value0;
                e_Population = __value1;
            }

            public string e_Name { get; }
            public decimal e_Population { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
            public List<Cte1Row0> Slot1;
        }

        private sealed class LeftRow0 : Row
        {
            public LeftRow0(string __value0, decimal? __value1)
            {
                Name = __value0;
                Total = __value1;
            }

            public override int Count => 2;
            public string Name { get; private set; }
            public decimal? Total { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        Total = (decimal?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Total" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)Total,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "Total" => (object)Total,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class LeftShape0
        {
            public LeftShape0(string Name, decimal? Total)
            {
                this.Name = Name;
                this.Total = Total;
            }

            public string Name { get; }
            public decimal? Total { get; }
        }

        private sealed class ParallelSingleKeyAggregateChunkWorker_1
        {
            private readonly CancellationToken _cancellationToken;
            private readonly Dictionary<string, AggregateGroup0> _groups = new Dictionary<string, AggregateGroup0>();
            private AggregateGroup0 _nullGroup;
            private readonly List<AggregateGroup0> _orderedGroups = new List<AggregateGroup0>();
            public ParallelSingleKeyAggregateChunkWorker_1(CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
            }

            public List<AggregateGroup0> OrderedGroups => _orderedGroups;

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void ProcessChunk(IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> chunk)
            {
                ParallelSingleKeyAggregateChunk_1(chunk, _groups, _orderedGroups, ref _nullGroup, _cancellationToken);
            }
        }

        private sealed class ParallelSingleKeyAggregateWorker_0
        {
            private readonly CancellationToken _cancellationToken;
            private readonly IReadOnlyList<Cte1Row0> _rows;
            private readonly List<AggregateGroup0>[] _shards;
            private readonly int _workerCount;
            public ParallelSingleKeyAggregateWorker_0(IReadOnlyList<Cte1Row0> rows, int workerCount, List<AggregateGroup0>[] shards, CancellationToken cancellationToken)
            {
                _rows = rows;
                _workerCount = workerCount;
                _shards = shards;
                _cancellationToken = cancellationToken;
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void Run(int shardIndex)
            {
                ParallelSingleKeyAggregateShard_0(_rows, _workerCount, _shards, _cancellationToken, shardIndex);
            }
        }

        private sealed class RightRow0 : Row
        {
            public RightRow0(string __value0, decimal? __value1)
            {
                Name = __value0;
                Total = __value1;
            }

            public override int Count => 2;
            public string Name { get; private set; }
            public decimal? Total { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        Total = (decimal?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Total" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)Total,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "Total" => (object)Total,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
