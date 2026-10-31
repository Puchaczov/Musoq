// === Parsed Query ===
/*
with rightCte as (select d.Team as Team, d.Name as Name, d.Score as Score from #dynamic.all() d) select l.Name as LeftName, r.Name as RightName from #dynamic.all() l asof join rightCte r on l.Team = r.Team and l.Score >= r.Score
*/

// === Logical Plan ===
/*
Cte
  Definition [rightCte]
    MultiStatement
      Project [d.Team as Team, d.Name as Name, d.Score as Score]
        SchemaScan [#dynamic.all() as d]
  Query
    MultiStatement
      Project [l.Team as l.Team, l.Name as l.Name, l.Score as l.Score, r.Team as r.Team, r.Name as r.Name, r.Score as r.Score]
        Join [AsofInner] [((l.Team = r.Team) AND (l.Score >= r.Score))]
          SchemaScan [#dynamic.all() as l]
          CteRef [rightCte as r]
      Project [l.Name as LeftName, r.Name as RightName]
        CteRef [lr as lr]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [rightCte]
    PhysicalMultiStatement
      PhysicalProject [d.Team as Team, d.Name as Name, d.Score as Score]
        PhysicalSchemaScan [#dynamic.all() as d] [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=6CE20F4D259D3B79D13AB2D9B2DEFCA5236BC4E894D4B74115E4B3F88F7F8AFB]
  Query
    PhysicalMultiStatement
      PhysicalProject [l.Name as l.Name, r.Name as r.Name]
        PhysicalNestedLoopJoin [AsofInner] [((l.Team = r.Team) AND (l.Score >= r.Score))]
          PhysicalSchemaScan [#dynamic.all() as l] [query-row:SealedClass;lifetime=EscapesScan;shape=6CE20F4D259D3B79D13AB2D9B2DEFCA5236BC4E894D4B74115E4B3F88F7F8AFB]
          PhysicalCteRef [rightCte as r]
      PhysicalProject [l.Name as LeftName, r.Name as RightName]
        PhysicalCteRef [lr as lr]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    Generated [QueryRow_6CE20F4D259D_S]
      Team: string <- field Field0
      Name: string <- field Field1
      Score: int <- field Field2
    Generated [Cte0Row0]
      Team: string <- field Team
      Name: string <- field Name
      Score: int <- field Score
    Generated [QueryRow_6CE20F4D259D_C]
      Team: string <- field Field0
      Name: string <- field Field1
      Score: int <- field Field2
    TableRow [r]
      Team: string <- field Team
      Name: string <- field Name
      Score: int <- field Score
    Generated [ResultRow0]
      LeftName: string <- field LeftName
      RightName: string <- field RightName

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    SourceScan [d: QueryRow_6CE20F4D259D_S] -> cte0_dRows [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=6CE20F4D259D3B79D13AB2D9B2DEFCA5236BC4E894D4B74115E4B3F88F7F8AFB]
    CreateTable [cte0: Cte0Row0]
    PhaseBoundary [Select:cte0]
    ChunkedForEach [d in cte0_dRows]
      AppendRow [cte0 <- Cte0Row0(Team: d.Team, Name: d.Name, Score: d.Score)]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    SourceScan [l: QueryRow_6CE20F4D259D_C] -> lRows [query-row:SealedClass;lifetime=EscapesScan;shape=6CE20F4D259D3B79D13AB2D9B2DEFCA5236BC4E894D4B74115E4B3F88F7F8AFB]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateAsOfIndex [resultAsOfIndex <- _cteRowResults.Slot0 by rCandidate.Team, rCandidate.Score]
    ChunkedForEach [l in lRows]
      AsOfProbe [r <- _cteRowResults.Slot0 using resultAsOfIndex where l.Team = rCandidate.Team and l.Score >= rCandidate.Score]
        AppendShape [result <- ResultRow0(LeftName: l.Name, RightName: r.Name)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q67_DynamicCteBackedAsOfJoin
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
            new Column("Team", typeof(string), 0),
            new Column("Name", typeof(string), 1),
            new Column("Score", typeof(int), 2)
        };
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("LeftName", typeof(string), 0),
            new Column("RightName", typeof(string), 1)
        };
        private static readonly QueryRowShape __queryRowShape_6CE20F4D259D = new QueryRowShape(new QueryRowField[] { new QueryRowField(0, 0, "Team", typeof(string), true), new QueryRowField(1, 1, "Name", typeof(string), true), new QueryRowField(2, 2, "Score", typeof(int), false) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_d_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Team", typeof(string), 0), new Column("Name", typeof(string), 1), new Column("Score", typeof(int), 2) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return __musoqShapeRow;
            }
        }

        private IEnumerable<ResultRow0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
                try
                {
                    var __lSchema = provider.GetSchema("#dynamic");
                    var __lSchemaQueryRows = __lSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#dynamic.all' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape 6CE20F4D259D3B79D13AB2D9B2DEFCA5236BC4E894D4B74115E4B3F88F7F8AFB).");
                    var lRowsSource = QuerySourceInvocation<QueryRow_6CE20F4D259D_C, QueryRowMaterializer_6CE20F4D259D_C>.Open(__lSchemaQueryRows, "all", new QueryScopedRowSourceRequest(new SourceExecutionContext("l:2", sourceExecutionPlans["l:2"], token, __schemaColumns_compiled_d_0, sourceRuntimeSettingsBySourceContextId["l:2"], logger, OnDataSourceProgress), __queryRowShape_6CE20F4D259D), global::Musoq.Schema.SourceArguments.Empty);
                    var lRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_6CE20F4D259D_C>(lRowsSource.Chunks, __musoqProgressContext, "l:2") : lRowsSource.Chunks;
                    var resultAsOfIndex = EvaluationHelper.CreateAsOfIndex<Cte0Row0, int, string>(_cteRowResults.Slot0, (rCandidate) =>
                    {
                        var __asOfEqualityPart0 = rCandidate.Team;
                        var __asOfEquality = new NativeEqualityKey<string>(true && __asOfEqualityPart0 != null, true && __asOfEqualityPart0 != null ? (string)__asOfEqualityPart0 : default(string));
                        return __asOfEquality;
                    }, (rCandidate) => rCandidate.Score, Musoq.Evaluator.IR.Expressions.BinaryOpKind.GreaterOrEqual);
                    foreach (var lChunk in lRows)
                    {
                        if (lChunk is global::Musoq.Schema.DataSources.RowChunk<QueryRow_6CE20F4D259D_C> lChunkView)
                        {
                            if (lChunkView.Source is QueryRow_6CE20F4D259D_C[] lChunkViewArray)
                            {
                                int lChunkViewOffset = lChunkView.Offset;
                                for (int lIndex = 0, lIndexCount = lChunkView.Count; lIndex < lIndexCount; ++lIndex)
                                {
                                    if ((lIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var l = lChunkViewArray[lChunkViewOffset + lIndex];
                                    {
                                        var rEqualityPart0 = l.Field0;
                                        var rEquality = new NativeEqualityKey<string>(true && rEqualityPart0 != null, true && rEqualityPart0 != null ? (string)rEqualityPart0 : default(string));
                                        var r = resultAsOfIndex.Find(rEquality, l.Field2);
                                        if (r != null)
                                        {
                                            __musoqFinalShapeRows.Add(new ResultRow0(l.Field1, r.Name));
                                        }
                                    }
                                }

                                continue;
                            }

                            if (lChunkView.Source is List<QueryRow_6CE20F4D259D_C> lChunkViewList)
                            {
                                int lChunkViewOffset = lChunkView.Offset;
                                for (int lIndex = 0, lIndexCount = lChunkView.Count; lIndex < lIndexCount; ++lIndex)
                                {
                                    if ((lIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var l = lChunkViewList[lChunkViewOffset + lIndex];
                                    {
                                        var rEqualityPart0 = l.Field0;
                                        var rEquality = new NativeEqualityKey<string>(true && rEqualityPart0 != null, true && rEqualityPart0 != null ? (string)rEqualityPart0 : default(string));
                                        var r = resultAsOfIndex.Find(rEquality, l.Field2);
                                        if (r != null)
                                        {
                                            __musoqFinalShapeRows.Add(new ResultRow0(l.Field1, r.Name));
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
                            {
                                var rEqualityPart0 = l.Field0;
                                var rEquality = new NativeEqualityKey<string>(true && rEqualityPart0 != null, true && rEqualityPart0 != null ? (string)rEqualityPart0 : default(string));
                                var r = resultAsOfIndex.Find(rEquality, l.Field2);
                                if (r != null)
                                {
                                    __musoqFinalShapeRows.Add(new ResultRow0(l.Field1, r.Name));
                                }
                            }
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte1", QueryPhase.End);
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
                var __cte0_dSchema = provider.GetSchema("#dynamic");
                var __cte0_dSchemaQueryRows = __cte0_dSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#dynamic.all' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape 6CE20F4D259D3B79D13AB2D9B2DEFCA5236BC4E894D4B74115E4B3F88F7F8AFB).");
                var cte0_dRowsSource = QuerySourceInvocation<QueryRow_6CE20F4D259D_S, QueryRowMaterializer_6CE20F4D259D_S>.Open(__cte0_dSchemaQueryRows, "all", new QueryScopedRowSourceRequest(new SourceExecutionContext("d:1", sourceExecutionPlans["d:1"], token, __schemaColumns_compiled_d_0, sourceRuntimeSettingsBySourceContextId["d:1"], logger, OnDataSourceProgress), __queryRowShape_6CE20F4D259D), global::Musoq.Schema.SourceArguments.Empty);
                var cte0_dRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_6CE20F4D259D_S>(cte0_dRowsSource.Chunks, __musoqProgressContext, "d:1") : cte0_dRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                foreach (var dChunk in cte0_dRows)
                {
                    if (dChunk is global::Musoq.Schema.DataSources.RowChunk<QueryRow_6CE20F4D259D_S> dChunkView)
                    {
                        if (dChunkView.Source is QueryRow_6CE20F4D259D_S[] dChunkViewArray)
                        {
                            int dChunkViewOffset = dChunkView.Offset;
                            for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                            {
                                if ((dIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var d = dChunkViewArray[dChunkViewOffset + dIndex];
                                cte0.Add(new Cte0Row0(d.Field0, d.Field1, d.Field2));
                            }

                            continue;
                        }

                        if (dChunkView.Source is List<QueryRow_6CE20F4D259D_S> dChunkViewList)
                        {
                            int dChunkViewOffset = dChunkView.Offset;
                            for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                            {
                                if ((dIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var d = dChunkViewList[dChunkViewOffset + dIndex];
                                cte0.Add(new Cte0Row0(d.Field0, d.Field1, d.Field2));
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
                        cte0.Add(new Cte0Row0(d.Field0, d.Field1, d.Field2));
                    }
                }

                return cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(string __value0, string __value1, int __value2)
            {
                Team = __value0;
                Name = __value1;
                Score = __value2;
            }

            public string Name { get; }
            public int Score { get; }
            public string Team { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private readonly struct QueryRowMaterializer_6CE20F4D259D_C : IQueryRowMaterializer<QueryRow_6CE20F4D259D_C>
        {
            public static QueryRow_6CE20F4D259D_C Materialize<TReader>(scoped ref TReader reader)
                where TReader : IQuerySourceFieldReader, allows ref struct => new QueryRow_6CE20F4D259D_C(reader.Read<string>(0), reader.Read<string>(1), reader.Read<int>(2));
        }

        private readonly struct QueryRowMaterializer_6CE20F4D259D_S : IQueryRowMaterializer<QueryRow_6CE20F4D259D_S>
        {
            public static QueryRow_6CE20F4D259D_S Materialize<TReader>(scoped ref TReader reader)
                where TReader : IQuerySourceFieldReader, allows ref struct => new QueryRow_6CE20F4D259D_S(reader.Read<string>(0), reader.Read<string>(1), reader.Read<int>(2));
        }

        private sealed class QueryRow_6CE20F4D259D_C
        {
            public QueryRow_6CE20F4D259D_C(string __value0, string __value1, int __value2)
            {
                Field0 = __value0;
                Field1 = __value1;
                Field2 = __value2;
            }

            public string Field0 { get; }
            public string Field1 { get; }
            public int Field2 { get; }
        }

        private readonly struct QueryRow_6CE20F4D259D_S
        {
            public QueryRow_6CE20F4D259D_S(string Field0, string Field1, int Field2)
            {
                this.Field0 = Field0;
                this.Field1 = Field1;
                this.Field2 = Field2;
            }

            public string Field0 { get; }
            public string Field1 { get; }
            public int Field2 { get; }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                LeftName = __value0;
                RightName = __value1;
            }

            public override int Count => 2;
            public string LeftName { get; private set; }
            public string RightName { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.LeftName = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.RightName = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.LeftName),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.RightName),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "LeftName" => true,
                "RightName" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.LeftName, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.RightName, ref visitor);
            }
        }
    }
}
