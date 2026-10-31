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
      Project [l.Id as l.Id, l.Name as l.Name, l.Population as l.Population, r.Id as r.Id]
        Apply [Cross]
          SchemaScan [#A.entities() as l]
          SchemaScan [#B.entities() as r]
      Project [l.Id as Id, l.Name as Name, l.Population as Population]
        Filter [(l.Id = r.Id)]
          CteRef [lr as lr]
  Definition [Joined]
    MultiStatement
      Project [e.Name as e.Name, e.Population as e.Population, r.Name as r.Name]
        Join [Inner] [(e.Name = r.Name)]
          CteRef [Expanded as e]
          SchemaScan [#B.entities() as r]
      Project [e.Name as Name, e.Population as Population]
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
      PhysicalProject [l.Id as l.Id, l.Name as l.Name, l.Population as l.Population, r.Id as r.Id]
        PhysicalNestedLoopApply [Cross]
          PhysicalSchemaScan [#A.entities() as l]
          PhysicalSchemaScan [#B.entities() as r]
      PhysicalProject [l.Name as Name, l.Population as Population]
        PhysicalFilter [(l.Id = r.Id)]
          PhysicalCteRef [lr as lr]
  Definition [Joined]
    PhysicalMultiStatement
      PhysicalProject [e.Name as e.Name, e.Population as e.Population]
        PhysicalHashJoin [Inner] [build: e.Name] [probe: r.Name]
          PhysicalCteRef [Expanded as e]
          PhysicalSchemaScan [#B.entities() as r]
      PhysicalProject [e.Name as Name, e.Population as Population]
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
      Id: int <- property Id
      Name: string <- property Name
      Population: decimal <- property Population
    SourceEntity [r: BasicEntity]
      Id: int <- property Id
      Name: string <- property Name
    Generated [Cte0Row0]
      Name: string <- field Name
      Population: decimal <- field Population
    TableRow [e]
      Name: string <- field Name
      Population: decimal <- field Population
    SourceEntity [r: BasicEntity]
      Id: int <- property Id
      Name: string <- property Name
    Generated [Cte1Row0]
      Name: string <- field Name
      Population: decimal <- field Population
    TableRow [Joined]
      Name: string <- field Name
      Population: decimal <- field Population
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
        If [(l.Id = r.Id)]
          AppendRow [cte0 <- Cte0Row0(Name: l.Name, Population: l.Population)]
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
      HashAdd [cte1EHash[e.Name] += e]
    ChunkedForEach [r in cte1_rRows]
      HashProbe [cte1EHash[r.Name] -> cte1EHashMatches]
        ForEach [e in cte1EHashMatches]
          AppendRow [cte1 <- Cte1Row0(Name: e.Name, Population: e.Population)]
    PhaseBoundary [End:cte2]
    PhaseBoundary [Select:cte1]
    StoreTable [cte1 -> _cteRowResults.Slot1: List<Cte1Row0>]
    PhaseBoundary [End:cte1]
    PhaseBoundary [Begin:left]
    CreateRowBuffer [left: List<LeftRow0>]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [leftGroups: string -> LeftAggregateGroup]
    PhaseBoundary [From:left]
    ForEach [Joined in _cteRowResults.Slot1]
      GetOrAddSingleKeyAggregateGroup [leftGroup = leftGroups[Joined.Name] by Name; typed: LeftAggregateGroup]
      Let [population: decimal = Joined.Population]
      TypedAggregateSet [Set(leftGroup.__agg0, population)]
    PhaseBoundary [GroupBy:left]
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
    ChunkedForEach [nnr1je in right_nnr1jeRows]
      GetOrAddSingleKeyAggregateGroup [rightGroup = rightGroups[nnr1je.Name] by Name; typed: RightAggregateGroup]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("Population", typeof(decimal), 1)
        };
        private static readonly Column[] __columns_compiled_left_3 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("Total", typeof(decimal?), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_l_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Name", typeof(string), 1), new Column("Population", typeof(decimal), 2) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_nnr1je_4 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("Population", typeof(decimal), 1) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_2 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Name", typeof(string), 1) });
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
            return QueryRows.DeferredTable<LeftRow0>("result", __columns_compiled_left_3, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<LeftShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled:left", QueryPhase.Begin);
                var left = new List<LeftRow0>();
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var leftGroupsToFinalize = new List<AggregateGroup0>();
                var leftGroups = new Dictionary<string, AggregateGroup0>();
                AggregateGroup0 leftNullGroup = null;
                OnPhaseChanged("compiled:left", QueryPhase.From);
                var __storedTable1Rows = _cteRowResults.Slot1;
                for (int __storedTable1Index = 0; __storedTable1Index < __storedTable1Rows.Count; ++__storedTable1Index)
                {
                    if ((__storedTable1Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte1Row0 Joined = __storedTable1Rows[__storedTable1Index];
                    string groupKey = Joined.Name;
                    AggregateGroup0 leftGroup = null;
                    if (groupKey != null)
                    {
                        ref var leftGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(leftGroups, groupKey, out var leftGroupExists);
                        if (!leftGroupExists)
                        {
                            leftGroupRef = new AggregateGroup0(groupKey);
                            leftGroupsToFinalize.Add(leftGroupRef);
                        }

                        leftGroup = leftGroupRef;
                    }
                    else
                    {
                        if (leftNullGroup == null)
                        {
                            leftNullGroup = new AggregateGroup0(null);
                            leftGroupsToFinalize.Add(leftNullGroup);
                        }

                        leftGroup = leftNullGroup;
                    }

                    decimal population = Joined.Population;
                    {
                        var __agg0Input = (decimal?)population;
                        if (__agg0Input.HasValue)
                        {
                            var __agg0Current = __agg0Input.GetValueOrDefault();
                            leftGroup.__agg0.Value = leftGroup.__agg0.HasValue ? checked(leftGroup.__agg0.Value + __agg0Current) : __agg0Current;
                            leftGroup.__agg0.HasValue = true;
                        }
                    }
                }

                OnPhaseChanged("compiled:left", QueryPhase.GroupBy);
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
                var right_nnr1jeRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__right_nnr1jeSchema, "entities", new SourceExecutionContext("nnr1je:4", sourceExecutionPlans["nnr1je:4"], token, __schemaColumns_compiled_nnr1je_4, sourceRuntimeSettingsBySourceContextId["nnr1je:4"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var right_nnr1jeRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(right_nnr1jeRowsSource.Chunks, __musoqProgressContext, "nnr1je:4") : right_nnr1jeRowsSource.Chunks;
                var right = new List<RightRow0>();
                OnPhaseChanged("compiled:right", QueryPhase.GroupBy);
                var rightGroupsToFinalize = new List<AggregateGroup0>();
                var rightGroups = new Dictionary<string, AggregateGroup0>();
                AggregateGroup0 rightNullGroup = null;
                foreach (var nnr1jeChunk in right_nnr1jeRows)
                {
                    if (nnr1jeChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> nnr1jeChunkView)
                    {
                        if (nnr1jeChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] nnr1jeChunkViewArray)
                        {
                            int nnr1jeChunkViewOffset = nnr1jeChunkView.Offset;
                            for (int nnr1jeIndex = 0, nnr1jeIndexCount = nnr1jeChunkView.Count; nnr1jeIndex < nnr1jeIndexCount; ++nnr1jeIndex)
                            {
                                if ((nnr1jeIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var nnr1je = nnr1jeChunkViewArray[nnr1jeChunkViewOffset + nnr1jeIndex];
                                string groupKey = nnr1je.Name;
                                AggregateGroup0 rightGroup = null;
                                if (groupKey != null)
                                {
                                    ref var rightGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(rightGroups, groupKey, out var rightGroupExists);
                                    if (!rightGroupExists)
                                    {
                                        rightGroupRef = new AggregateGroup0(groupKey);
                                        rightGroupsToFinalize.Add(rightGroupRef);
                                    }

                                    rightGroup = rightGroupRef;
                                }
                                else
                                {
                                    if (rightNullGroup == null)
                                    {
                                        rightNullGroup = new AggregateGroup0(null);
                                        rightGroupsToFinalize.Add(rightNullGroup);
                                    }

                                    rightGroup = rightNullGroup;
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

                            continue;
                        }

                        if (nnr1jeChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> nnr1jeChunkViewList)
                        {
                            int nnr1jeChunkViewOffset = nnr1jeChunkView.Offset;
                            for (int nnr1jeIndex = 0, nnr1jeIndexCount = nnr1jeChunkView.Count; nnr1jeIndex < nnr1jeIndexCount; ++nnr1jeIndex)
                            {
                                if ((nnr1jeIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var nnr1je = nnr1jeChunkViewList[nnr1jeChunkViewOffset + nnr1jeIndex];
                                string groupKey = nnr1je.Name;
                                AggregateGroup0 rightGroup = null;
                                if (groupKey != null)
                                {
                                    ref var rightGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(rightGroups, groupKey, out var rightGroupExists);
                                    if (!rightGroupExists)
                                    {
                                        rightGroupRef = new AggregateGroup0(groupKey);
                                        rightGroupsToFinalize.Add(rightGroupRef);
                                    }

                                    rightGroup = rightGroupRef;
                                }
                                else
                                {
                                    if (rightNullGroup == null)
                                    {
                                        rightNullGroup = new AggregateGroup0(null);
                                        rightGroupsToFinalize.Add(rightNullGroup);
                                    }

                                    rightGroup = rightNullGroup;
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

                            continue;
                        }
                    }

                    for (int nnr1jeIndex = 0, nnr1jeIndexCount = nnr1jeChunk.Count; nnr1jeIndex < nnr1jeIndexCount; ++nnr1jeIndex)
                    {
                        if ((nnr1jeIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var nnr1je = nnr1jeChunk[nnr1jeIndex];
                        string groupKey = nnr1je.Name;
                        AggregateGroup0 rightGroup = null;
                        if (groupKey != null)
                        {
                            ref var rightGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(rightGroups, groupKey, out var rightGroupExists);
                            if (!rightGroupExists)
                            {
                                rightGroupRef = new AggregateGroup0(groupKey);
                                rightGroupsToFinalize.Add(rightGroupRef);
                            }

                            rightGroup = rightGroupRef;
                        }
                        else
                        {
                            if (rightNullGroup == null)
                            {
                                rightNullGroup = new AggregateGroup0(null);
                                rightGroupsToFinalize.Add(rightNullGroup);
                            }

                            rightGroup = rightNullGroup;
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
                var cte0_lRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__cte0_lSchema, "entities", new SourceExecutionContext("l:1", sourceExecutionPlans["l:1"], token, __schemaColumns_compiled_l_0, sourceRuntimeSettingsBySourceContextId["l:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
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
                                var cte0_rRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__cte0_rSchema, "entities", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_2, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
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
                                                if ((l.Id == r.Id))
                                                {
                                                    cte0.Add(new Cte0Row0(l.Name, l.Population));
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
                                                if ((l.Id == r.Id))
                                                {
                                                    cte0.Add(new Cte0Row0(l.Name, l.Population));
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
                                        if ((l.Id == r.Id))
                                        {
                                            cte0.Add(new Cte0Row0(l.Name, l.Population));
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
                                var cte0_rRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__cte0_rSchema, "entities", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_2, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
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
                                                if ((l.Id == r.Id))
                                                {
                                                    cte0.Add(new Cte0Row0(l.Name, l.Population));
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
                                                if ((l.Id == r.Id))
                                                {
                                                    cte0.Add(new Cte0Row0(l.Name, l.Population));
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
                                        if ((l.Id == r.Id))
                                        {
                                            cte0.Add(new Cte0Row0(l.Name, l.Population));
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
                        var cte0_rRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__cte0_rSchema, "entities", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_2, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
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
                                        if ((l.Id == r.Id))
                                        {
                                            cte0.Add(new Cte0Row0(l.Name, l.Population));
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
                                        if ((l.Id == r.Id))
                                        {
                                            cte0.Add(new Cte0Row0(l.Name, l.Population));
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
                                if ((l.Id == r.Id))
                                {
                                    cte0.Add(new Cte0Row0(l.Name, l.Population));
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
                var cte1_rRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__cte1_rSchema, "entities", new SourceExecutionContext("r:2", sourceExecutionPlans["r:2"], token, __schemaColumns_compiled_r_2, sourceRuntimeSettingsBySourceContextId["r:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
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
                    string key = e.Name;
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
                                        cte1.Add(new Cte1Row0(e.Name, e.Population));
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
                                        cte1.Add(new Cte1Row0(e.Name, e.Population));
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
                                cte1.Add(new Cte1Row0(e.Name, e.Population));
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

        private sealed class AggregateGroup0
        {
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg0;
            public readonly string __key0;
            public AggregateGroup0(string __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg0);
            }

            public void MergeFrom(AggregateGroup0 source)
            {
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
            }
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(string __value0, decimal __value1)
            {
                Name = __value0;
                Population = __value1;
            }

            public string Name { get; }
            public decimal Population { get; }
        }

        private sealed class Cte1Row0
        {
            public Cte1Row0(string __value0, decimal __value1)
            {
                Name = __value0;
                Population = __value1;
            }

            public string Name { get; }
            public decimal Population { get; }
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

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Total" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
            }
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

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Total" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
            }
        }
    }
}
