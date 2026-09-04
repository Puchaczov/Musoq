// === Parsed Query ===
/*
SELECT a.City, (SELECT b.City FROM #B.entities() b WHERE b.Country = a.Country AND (b.City = 'KRAKOW' OR b.City = 'PARIS') UNION (City) SELECT c.City FROM #C.entities() c WHERE c.Country = a.Country) AS MatchCity FROM #A.entities() a
*/

// === Logical Plan ===
/*
Cte
  Definition [_sm_1]
    SetOp [Union]
      MultiStatement
        Project [b.Country as _sq_1_corr_0, b.City as _sm_1_value]
          Filter [((b.City = 'KRAKOW') OR (b.City = 'PARIS'))]
            SchemaScan [#B.entities() as b]
      MultiStatement
        Project [c.Country as _sq_1_corr_0, c.City as _sm_1_value]
          SchemaScan [#C.entities() as c]
  Definition [_sq_1]
    MultiStatement
      Project [_sm_1._sq_1_corr_0 as _sm_1._sq_1_corr_0, AggRef(_sm_1.__CorrelatedScalarSubqueryValue(_sm_1._sm_1_value)) as _sm_1.__CorrelatedScalarSubqueryValue(_sm_1._sm_1_value)]
        Aggregate [keys: _sm_1._sq_1_corr_0] [aggs: __CorrelatedScalarSubqueryValue(_sm_1_value)]
          CteRef [_sm_1 as _sm_1]
      Project [_sm_1._sq_1_corr_0 as _sq_1_corr_0, _sm_1.__CorrelatedScalarSubqueryValue(_sm_1._sm_1_value) as _sq_1_value]
        CteRef [_sm_1Score as _sm_1Score]
  Query
    MultiStatement
      Project [a.City as a.City, a.Country as a.Country, _sq_1._sq_1_corr_0 as _sq_1._sq_1_corr_0, _sq_1._sq_1_value as _sq_1._sq_1_value]
        Join [LeftSingle] [(_sq_1._sq_1_corr_0 = a.Country)]
          SchemaScan [#A.entities() as a]
          CteRef [_sq_1 as _sq_1]
      Project [a.City as a.City, __CorrelatedScalarSubqueryResult(_sq_1._sq_1_value) as MatchCity]
        CteRef [a_sq_1 as a_sq_1]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [_sm_1]
    PhysicalSetOp [Union]
      PhysicalMultiStatement
        PhysicalProject [b.Country as _sq_1_corr_0, b.City as _sm_1_value]
          PhysicalFilter [((b.City = 'KRAKOW') OR (b.City = 'PARIS'))]
            PhysicalSchemaScan [#B.entities() as b]
      PhysicalMultiStatement
        PhysicalProject [c.Country as _sq_1_corr_0, c.City as _sm_1_value]
          PhysicalSchemaScan [#C.entities() as c]
  Definition [_sq_1]
    PhysicalMultiStatement
      PhysicalProject [_sm_1._sq_1_corr_0 as _sm_1._sq_1_corr_0, AggRef(_sm_1.__CorrelatedScalarSubqueryValue(_sm_1._sm_1_value)) as _sm_1.__CorrelatedScalarSubqueryValue(_sm_1._sm_1_value)]
        PhysicalSingleKeyAggregate [key: _sm_1._sq_1_corr_0 (String)] [aggs: __CorrelatedScalarSubqueryValue(_sm_1_value)]
          PhysicalCteRef [_sm_1 as _sm_1]
      PhysicalProject [_sm_1._sq_1_corr_0 as _sq_1_corr_0, _sm_1.__CorrelatedScalarSubqueryValue(_sm_1._sm_1_value) as _sq_1_value]
        PhysicalCteRef [_sm_1Score as _sm_1Score]
  Query
    PhysicalMultiStatement
      PhysicalProject [a.City as a.City, a.Country as a.Country, _sq_1._sq_1_corr_0 as _sq_1._sq_1_corr_0, _sq_1._sq_1_value as _sq_1._sq_1_value]
        PhysicalHashJoin [LeftSingle] [build: _sq_1._sq_1_corr_0] [probe: a.Country]
          PhysicalSchemaScan [#A.entities() as a]
          PhysicalCteRef [_sq_1 as _sq_1]
      PhysicalProject [a.City as a.City, __CorrelatedScalarSubqueryResult(_sq_1._sq_1_value) as MatchCity]
        PhysicalCteRef [a_sq_1 as a_sq_1]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [b: BasicEntity]
      City: string <- property City
      Country: string <- property Country
    Generated [Cte0LeftRow0]
      _sq_1_corr_0: string <- field _sq_1_corr_0
      _sm_1_value: string <- field _sm_1_value
    SourceEntity [c: BasicEntity]
      City: string <- property City
      Country: string <- property Country
    Generated [Cte0RightRow0]
      _sq_1_corr_0: string <- field _sq_1_corr_0
      _sm_1_value: string <- field _sm_1_value
    TableRow [_sm_1]
      _sq_1_corr_0: string <- position 0
      _sm_1_value: string <- position 1
    AggregateGroup [Cte1AggregateGroup; keys: 1; typed aggs: 1]
    Generated [Cte1Row0]
      _sq_1_corr_0: string <- field _sq_1_corr_0
      _sq_1_value: CorrelatedScalarSubqueryResult<string> <- field _sq_1_value
    SourceEntity [a: BasicEntity]
      City: string <- property City
      Country: string <- property Country
    TableRow [_sq_1]
      _sq_1_corr_0: string <- field _sq_1_corr_0
      _sq_1_value: CorrelatedScalarSubqueryResult<string> <- field _sq_1_value
    Generated [ResultRow0]
      a.City: string <- field a_City
      MatchCity: string <- field MatchCity

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Begin:left]
    PhaseBoundary [From:left]
    PhaseBoundary [From:cte0]
    SourceScan [b: BasicEntity] -> cte0Left_bRows
    CreateTable [cte0Left: Cte0LeftRow0]
    PhaseBoundary [Where:left]
    PhaseBoundary [Select:left]
    PhaseBoundary [Where:cte0]
    PhaseBoundary [Select:cte0]
    ChunkedForEach [b in cte0Left_bRows]
      Let [city: string = b.City]
      If [((city = 'KRAKOW') OR (city = 'PARIS'))]
        AppendRow [cte0Left <- Cte0LeftRow0(_sq_1_corr_0: b.Country, _sm_1_value: city)]
    PhaseBoundary [End:left]
    PhaseBoundary [Begin:right]
    PhaseBoundary [From:right]
    SourceScan [c: BasicEntity] -> cte0Right_cRows
    CreateTable [cte0Right: Cte0RightRow0]
    PhaseBoundary [Select:right]
    ChunkedForEach [c in cte0Right_cRows]
      AppendRow [cte0Right <- Cte0RightRow0(_sq_1_corr_0: c.Country, _sm_1_value: c.City)]
    PhaseBoundary [End:right]
    SetOperation [cte0 = cte0Left Union cte0Right, HashSet]
    StoreTable [cte0 -> _tableResults[0]]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Begin:cte1]
    CreateTable [cte1: Cte1Row0]
    PhaseBoundary [GroupBy:cte1]
    CreateSingleKeyAggregateContext [cte1Groups: string -> Cte1AggregateGroup]
    ParallelSingleKeyAggregateLoop [_sm_1 in _tableResults[0].Rows by _sm_1._sq_1_corr_0; threshold 4096, sample 8192/6144, maxDegree 24, group Cte1AggregateGroup]
      ParallelAccumulate
        Let [_sm_1_value: string = _sm_1._sm_1_value]
        TypedAggregateSet [Set(cte1Group.__agg0, _sm_1_value)]
    EnsureCapacity [cte1 <- cte1GroupsToFinalize.Count]
    PhaseBoundary [Select:cte1]
    ForEach [cte1FinalGroup in cte1GroupsToFinalize]
      AppendRow [cte1 <- Cte1Row0(_sq_1_corr_0: cte1FinalGroup._sm_1._sq_1_corr_0, _sq_1_value: _sm_1.__CorrelatedScalarSubqueryValue(_sm_1._sm_1_value))]
    StoreTable [cte1 -> _tableResults[1]]
    PhaseBoundary [End:cte1]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte2]
    SourceScan [a: BasicEntity] -> aRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateObject [_sq_1HashEmptyState: State<string>]
    Let [_sq_1HashEmptyValue: CorrelatedScalarSubqueryResult<string> = Get(_sq_1HashEmptyState)]
    CreateHash [_sq_1Hash: string -> Row; capacity: _tableResults[1].Count]
    ForEach [_sq_1 in CastGeneratedRows<Cte1Row0>(_tableResults[1].Rows)]
      HashAdd [_sq_1Hash[_sq_1._sq_1_corr_0] += _sq_1]
    ChunkedForEach [a in aRows]
      HashProbe [_sq_1Hash[a.Country] -> _sq_1HashMatches] [match: _sq_1HashHasMatch]
        ForEach [_sq_1 in _sq_1HashMatches]
          Assign [_sq_1HashHasMatch = TRUE]
          AppendShape [result <- ResultShape0(a.City: a.City, MatchCity: __CorrelatedScalarSubqueryResult(_sq_1._sq_1_value))]
      HashProbeNoMatch
        AppendShape [result <- ResultShape0(a.City: a.City, MatchCity: __CorrelatedScalarSubqueryResult(_sq_1HashEmptyValue))]
    PhaseBoundary [End:cte2]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q273_SpecCoreCorrelatedScalarSetOperation
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
        private static readonly Column[] __columns_compiled_cte0Left_1 = new Column[]
        {
            new Column("_sq_1_corr_0", typeof(string), 0),
            new Column("_sm_1_value", typeof(string), 1)
        };
        private static readonly Column[] __columns_compiled_cte1_2 = new Column[]
        {
            new Column("_sq_1_corr_0", typeof(string), 0),
            new Column("_sq_1_value", typeof(Musoq.Plugins.CorrelatedScalarSubqueryResult<string>), 1)
        };
        private static readonly Column[] __columns_compiled_result_3 = new Column[]
        {
            new Column("a.City", typeof(string), 0),
            new Column("MatchCity", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 0), new Column("Country", typeof(string), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.a_City, __musoqShapeRow.MatchCity);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _tableResults = new Musoq.Evaluator.Tables.Table[2];
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                Musoq.Evaluator.Tables.Table cte0Left = null!;
                Musoq.Evaluator.Tables.Table cte0Right = null!;
                try
                {
                    OnPhaseChanged("compiled:left", QueryPhase.Begin);
                    OnPhaseChanged("compiled:left", QueryPhase.From);
                    OnPhaseChanged("compiled:cte0", QueryPhase.From);
                    var __cte0Left_bSchema = provider.GetSchema("#B");
                    var cte0Left_bRowsSource = __cte0Left_bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("b:2", sourceExecutionPlans["b:2"], token, __schemaColumns_compiled_b_0, sourceRuntimeSettingsBySourceContextId["b:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                    var cte0Left_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0Left_bRowsSource.Chunks, __musoqProgressContext, "b:2") : cte0Left_bRowsSource.Chunks;
                    cte0Left = new Table("cte0Left", __columns_compiled_cte0Left_1);
                    OnPhaseChanged("compiled:left", QueryPhase.Where);
                    OnPhaseChanged("compiled:left", QueryPhase.Select);
                    OnPhaseChanged("compiled:cte0", QueryPhase.Where);
                    OnPhaseChanged("compiled:cte0", QueryPhase.Select);
                    foreach (var bChunk in cte0Left_bRows)
                    {
                        if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> bChunkView)
                        {
                            if (bChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] bChunkViewArray)
                            {
                                int bChunkViewOffset = bChunkView.Offset;
                                for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                {
                                    if ((bIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var b = bChunkViewArray[bChunkViewOffset + bIndex];
                                    string city = b.City;
                                    if (((Operators.SqlCompare<string, string>(city, "KRAKOW", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) | Operators.SqlCompare<string, string>(city, "PARIS", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)))) == true)
                                    {
                                        cte0Left.AddDirect(new Cte0LeftRow0(b.Country, city));
                                    }
                                }

                                continue;
                            }

                            if (bChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> bChunkViewList)
                            {
                                int bChunkViewOffset = bChunkView.Offset;
                                for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                {
                                    if ((bIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var b = bChunkViewList[bChunkViewOffset + bIndex];
                                    string city = b.City;
                                    if (((Operators.SqlCompare<string, string>(city, "KRAKOW", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) | Operators.SqlCompare<string, string>(city, "PARIS", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)))) == true)
                                    {
                                        cte0Left.AddDirect(new Cte0LeftRow0(b.Country, city));
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
                            string city = b.City;
                            if (((Operators.SqlCompare<string, string>(city, "KRAKOW", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) | Operators.SqlCompare<string, string>(city, "PARIS", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)))) == true)
                            {
                                cte0Left.AddDirect(new Cte0LeftRow0(b.Country, city));
                            }
                        }
                    }

                    OnPhaseChanged("compiled:left", QueryPhase.End);
                    OnPhaseChanged("compiled:right", QueryPhase.Begin);
                    OnPhaseChanged("compiled:right", QueryPhase.From);
                    var __cte0Right_cSchema = provider.GetSchema("#C");
                    var cte0Right_cRowsSource = __cte0Right_cSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("c:3", sourceExecutionPlans["c:3"], token, __schemaColumns_compiled_b_0, sourceRuntimeSettingsBySourceContextId["c:3"], logger, OnDataSourceProgress), Array.Empty<object>());
                    var cte0Right_cRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0Right_cRowsSource.Chunks, __musoqProgressContext, "c:3") : cte0Right_cRowsSource.Chunks;
                    cte0Right = new Table("cte0Right", __columns_compiled_cte0Left_1);
                    OnPhaseChanged("compiled:right", QueryPhase.Select);
                    foreach (var cChunk in cte0Right_cRows)
                    {
                        if (cChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> cChunkView)
                        {
                            if (cChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] cChunkViewArray)
                            {
                                int cChunkViewOffset = cChunkView.Offset;
                                for (int cIndex = 0, cIndexCount = cChunkView.Count; cIndex < cIndexCount; ++cIndex)
                                {
                                    if ((cIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var c = cChunkViewArray[cChunkViewOffset + cIndex];
                                    cte0Right.AddDirect(new Cte0RightRow0(c.Country, c.City));
                                }

                                continue;
                            }

                            if (cChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> cChunkViewList)
                            {
                                int cChunkViewOffset = cChunkView.Offset;
                                for (int cIndex = 0, cIndexCount = cChunkView.Count; cIndex < cIndexCount; ++cIndex)
                                {
                                    if ((cIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var c = cChunkViewList[cChunkViewOffset + cIndex];
                                    cte0Right.AddDirect(new Cte0RightRow0(c.Country, c.City));
                                }

                                continue;
                            }
                        }

                        for (int cIndex = 0, cIndexCount = cChunk.Count; cIndex < cIndexCount; ++cIndex)
                        {
                            if ((cIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var c = cChunk[cIndex];
                            cte0Right.AddDirect(new Cte0RightRow0(c.Country, c.City));
                        }
                    }

                    OnPhaseChanged("compiled:right", QueryPhase.End);
                    var cte0 = new Table("cte0", __columns_compiled_cte0Left_1);
                    var cte0Keys = new HashSet<ValueTuple<string, string>>(cte0Left.Rows.Count + cte0Right.Rows.Count);
                    foreach (var cte0LeftRow in cte0Left.Rows)
                    {
                        if (cte0Keys.Add(((string)cte0LeftRow[0], (string)cte0LeftRow[1])))
                        {
                            cte0.AddDirect(cte0LeftRow);
                        }
                    }

                    foreach (var cte0RightRow in cte0Right.Rows)
                    {
                        if (cte0Keys.Add(((string)cte0RightRow[0], (string)cte0RightRow[1])))
                        {
                            cte0.AddDirect(cte0RightRow);
                        }
                    }

                    _tableResults[0] = cte0;
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
                }

                OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
                Musoq.Evaluator.Tables.Table cte1 = null!;
                try
                {
                    cte1 = new Table("cte1", __columns_compiled_cte1_2);
                    OnPhaseChanged("compiled:cte1", QueryPhase.GroupBy);
                    var cte1GroupsToFinalize = new List<Cte1AggregateGroup>();
                    var cte1Groups = new Dictionary<string, Cte1AggregateGroup>();
                    var cte1GroupsToFinalizeParallelRows = EvaluationHelper.GetParallelAggregationRowsOrEmpty<Musoq.Evaluator.Tables.Row>(_tableResults[0].Rows, 4096);
                    cte1GroupsToFinalize = ParallelSingleKeyAggregate_0(cte1GroupsToFinalizeParallelRows, 24, token);
                    cte1.EnsureCapacity(cte1GroupsToFinalize.Count);
                    OnPhaseChanged("compiled:cte1", QueryPhase.Select);
                    foreach (var cte1FinalGroup in cte1GroupsToFinalize)
                    {
                        token.ThrowIfCancellationRequested();
                        cte1.AddDirect(new Cte1Row0(cte1FinalGroup.__key0, Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.Get(in cte1FinalGroup.__agg0)));
                    }

                    _tableResults[1] = cte1;
                }
                finally
                {
                    OnPhaseChanged("compiled:cte1", QueryPhase.End);
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte2", QueryPhase.Begin);
                Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.State _sq_1HashEmptyState = default!;
                try
                {
                    var __aSchema = provider.GetSchema("#A");
                    var aRowsSource = __aSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("a:3", sourceExecutionPlans["a:3"], token, __schemaColumns_compiled_b_0, sourceRuntimeSettingsBySourceContextId["a:3"], logger, OnDataSourceProgress), Array.Empty<object>());
                    var aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(aRowsSource.Chunks, __musoqProgressContext, "a:3") : aRowsSource.Chunks;
                    _sq_1HashEmptyState = new Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.State();
                    Musoq.Plugins.CorrelatedScalarSubqueryResult<string> _sq_1HashEmptyValue = (Musoq.Plugins.CorrelatedScalarSubqueryResult<string>)Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.Get(_sq_1HashEmptyState);
                    var _sq_1Hash = new Dictionary<string, HashJoinBucket<Cte1Row0>>(_tableResults[1].Count);
                    var __storedTable1Rows = EvaluationHelper.CastGeneratedRows<Cte1Row0>(_tableResults[1].Rows);
                    for (int __storedTable1Index = 0; __storedTable1Index < __storedTable1Rows.Count; ++__storedTable1Index)
                    {
                        if ((__storedTable1Index & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        Cte1Row0 _sq_1 = (Cte1Row0)__storedTable1Rows[__storedTable1Index];
                        string key = _sq_1._sq_1_corr_0;
                        if (key == null)
                            continue;
                        {
                            ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(_sq_1Hash, key, out var matchesExists);
                            if (!matchesExists)
                            {
                                matches = new HashJoinBucket<Cte1Row0>(_sq_1);
                            }
                            else
                            {
                                matches.Add(_sq_1);
                            }
                        }
                    }

                    foreach (var aChunk in aRows)
                    {
                        if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> aChunkView)
                        {
                            if (aChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] aChunkViewArray)
                            {
                                int aChunkViewOffset = aChunkView.Offset;
                                for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                                {
                                    if ((aIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var a = aChunkViewArray[aChunkViewOffset + aIndex];
                                    bool _sq_1HashHasMatch = false;
                                    string key = a.Country;
                                    if (key != null && _sq_1Hash.TryGetValue(key, out var _sq_1HashMatches))
                                    {
                                        foreach (var _sq_1 in _sq_1HashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            _sq_1HashHasMatch = true;
                                            __musoqFinalShapeRows.Add(new ResultShape0(a.City, (string)Musoq.Plugins.CorrelatedScalarSubqueryResultExtractor.GetValue(_sq_1._sq_1_value)));
                                        }
                                    }

                                    if (!_sq_1HashHasMatch)
                                    {
                                        __musoqFinalShapeRows.Add(new ResultShape0(a.City, (string)Musoq.Plugins.CorrelatedScalarSubqueryResultExtractor.GetValue(_sq_1HashEmptyValue)));
                                    }
                                }

                                continue;
                            }

                            if (aChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> aChunkViewList)
                            {
                                int aChunkViewOffset = aChunkView.Offset;
                                for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                                {
                                    if ((aIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var a = aChunkViewList[aChunkViewOffset + aIndex];
                                    bool _sq_1HashHasMatch = false;
                                    string key = a.Country;
                                    if (key != null && _sq_1Hash.TryGetValue(key, out var _sq_1HashMatches))
                                    {
                                        foreach (var _sq_1 in _sq_1HashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            _sq_1HashHasMatch = true;
                                            __musoqFinalShapeRows.Add(new ResultShape0(a.City, (string)Musoq.Plugins.CorrelatedScalarSubqueryResultExtractor.GetValue(_sq_1._sq_1_value)));
                                        }
                                    }

                                    if (!_sq_1HashHasMatch)
                                    {
                                        __musoqFinalShapeRows.Add(new ResultShape0(a.City, (string)Musoq.Plugins.CorrelatedScalarSubqueryResultExtractor.GetValue(_sq_1HashEmptyValue)));
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
                            bool _sq_1HashHasMatch = false;
                            string key = a.Country;
                            if (key != null && _sq_1Hash.TryGetValue(key, out var _sq_1HashMatches))
                            {
                                foreach (var _sq_1 in _sq_1HashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    _sq_1HashHasMatch = true;
                                    __musoqFinalShapeRows.Add(new ResultShape0(a.City, (string)Musoq.Plugins.CorrelatedScalarSubqueryResultExtractor.GetValue(_sq_1._sq_1_value)));
                                }
                            }

                            if (!_sq_1HashHasMatch)
                            {
                                __musoqFinalShapeRows.Add(new ResultShape0(a.City, (string)Musoq.Plugins.CorrelatedScalarSubqueryResultExtractor.GetValue(_sq_1HashEmptyValue)));
                            }
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte2", QueryPhase.End);
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
        private static void ParallelSingleKeyAggregateShard_0(IReadOnlyList<Musoq.Evaluator.Tables.Row> rows, int workerCount, List<Cte1AggregateGroup>[] shards, CancellationToken cancellationToken, int shardIndex)
        {
            var start = rows.Count * shardIndex / workerCount;
            var end = rows.Count * (shardIndex + 1) / workerCount;
            var groups = new Dictionary<string, Cte1AggregateGroup>();
            var orderedGroups = new List<Cte1AggregateGroup>();
            Cte1AggregateGroup nullGroup = null;
            for (var index = start; index < end; index++)
            {
                if ((index & 1023) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                Musoq.Evaluator.Tables.Row _sm_1 = rows[index];
                string groupKey = (string)_sm_1[0];
                Cte1AggregateGroup cte1Group = null;
                if (groupKey != null)
                {
                    ref var cte1GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var cte1GroupExists);
                    if (!cte1GroupExists)
                    {
                        cte1GroupRef = new Cte1AggregateGroup(groupKey);
                        orderedGroups.Add(cte1GroupRef);
                    }

                    cte1Group = cte1GroupRef;
                }
                else
                {
                    if (nullGroup == null)
                    {
                        nullGroup = new Cte1AggregateGroup(groupKey);
                        orderedGroups.Add(nullGroup);
                    }

                    cte1Group = nullGroup;
                }

                string _sm_1_value = (string)_sm_1[1];
                Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.Set(ref cte1Group.__agg0, (string)_sm_1_value);
            }

            shards[shardIndex] = orderedGroups;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte1AggregateGroup> ParallelSingleKeyAggregate_0(IReadOnlyList<Musoq.Evaluator.Tables.Row> rows, int maxDegreeOfParallelism, CancellationToken cancellationToken)
        {
            if (rows.Count == 0)
            {
                return new List<Cte1AggregateGroup>();
            }

            var workerCount = Math.Min(Math.Max(1, maxDegreeOfParallelism), rows.Count);
            List<Cte1AggregateGroup>[] shards = new List<Cte1AggregateGroup>[workerCount];
            var options = new ParallelOptions()
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = workerCount
            };
            var worker = new ParallelSingleKeyAggregateWorker_0(rows, workerCount, shards, cancellationToken);
            Parallel.For(0, workerCount, options, worker.Run);
            var mergedGroups = new Dictionary<string, Cte1AggregateGroup>();
            var groupsToFinalize = new List<Cte1AggregateGroup>();
            Cte1AggregateGroup nullGroup = null;
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

        private sealed class Cte0LeftRow0 : Row
        {
            public Cte0LeftRow0(string __value0, string __value1)
            {
                _sq_1_corr_0 = __value0;
                _sm_1_value = __value1;
            }

            public override int Count => 2;
            public string _sm_1_value { get; private set; }
            public string _sq_1_corr_0 { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        _sq_1_corr_0 = (string)value;
                        break;
                    case 1:
                        _sm_1_value = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "_sq_1_corr_0" => true,
                "_sm_1_value" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)_sq_1_corr_0,
                1 => (object)_sm_1_value,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "_sq_1_corr_0" => (object)_sq_1_corr_0,
                "_sm_1_value" => (object)_sm_1_value,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class Cte0RightRow0 : Row
        {
            public Cte0RightRow0(string __value0, string __value1)
            {
                _sq_1_corr_0 = __value0;
                _sm_1_value = __value1;
            }

            public override int Count => 2;
            public string _sm_1_value { get; private set; }
            public string _sq_1_corr_0 { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        _sq_1_corr_0 = (string)value;
                        break;
                    case 1:
                        _sm_1_value = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "_sq_1_corr_0" => true,
                "_sm_1_value" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)_sq_1_corr_0,
                1 => (object)_sm_1_value,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "_sq_1_corr_0" => (object)_sq_1_corr_0,
                "_sm_1_value" => (object)_sm_1_value,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class Cte1AggregateGroup
        {
            public Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.State __agg0;
            public readonly string __key0;
            public Cte1AggregateGroup(string __key0)
            {
                this.__key0 = __key0;
            }

            public void MergeFrom(Cte1AggregateGroup source)
            {
                Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.Merge(ref this.__agg0, in source.__agg0);
            }
        }

        private sealed class Cte1Row0 : Row
        {
            public Cte1Row0(string __value0, Musoq.Plugins.CorrelatedScalarSubqueryResult<string> __value1)
            {
                _sq_1_corr_0 = __value0;
                _sq_1_value = __value1;
            }

            public override int Count => 2;
            public string _sq_1_corr_0 { get; private set; }
            public Musoq.Plugins.CorrelatedScalarSubqueryResult<string> _sq_1_value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        _sq_1_corr_0 = (string)value;
                        break;
                    case 1:
                        _sq_1_value = (Musoq.Plugins.CorrelatedScalarSubqueryResult<string>)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "_sq_1_corr_0" => true,
                "_sq_1_value" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)_sq_1_corr_0,
                1 => (object)_sq_1_value,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "_sq_1_corr_0" => (object)_sq_1_corr_0,
                "_sq_1_value" => (object)_sq_1_value,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ParallelSingleKeyAggregateWorker_0
        {
            private readonly CancellationToken _cancellationToken;
            private readonly IReadOnlyList<Musoq.Evaluator.Tables.Row> _rows;
            private readonly List<Cte1AggregateGroup>[] _shards;
            private readonly int _workerCount;
            public ParallelSingleKeyAggregateWorker_0(IReadOnlyList<Musoq.Evaluator.Tables.Row> rows, int workerCount, List<Cte1AggregateGroup>[] shards, CancellationToken cancellationToken)
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                a_City = __value0;
                MatchCity = __value1;
            }

            public override int Count => 2;
            public string MatchCity { get; private set; }
            public string a_City { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        a_City = (string)value;
                        break;
                    case 1:
                        MatchCity = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "a.City" => true,
                "a_City" => true,
                "City" => true,
                "MatchCity" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)a_City,
                1 => (object)MatchCity,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "a.City" => (object)a_City,
                "a_City" => (object)a_City,
                "City" => (object)a_City,
                "MatchCity" => (object)MatchCity,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string a_City, string MatchCity)
            {
                this.a_City = a_City;
                this.MatchCity = MatchCity;
            }

            public string MatchCity { get; }
            public string a_City { get; }
        }
    }
}
