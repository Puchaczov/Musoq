/*
raw query string

with rightCte as (select d.Team as Team, d.Name as Name, d.Score as Score from #dynamic.all() d) select l.Name as LeftName, r.Name as RightName from #dynamic.all() l asof join rightCte r on l.Team = r.Team and l.Score >= r.Score
*/

/*
logical plan representation string

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

/*
physical plan representation string

PhysicalCte
  Definition [rightCte]
    PhysicalMultiStatement
      PhysicalProject [d.Team as Team, d.Name as Name, d.Score as Score]
        PhysicalSchemaScan [#dynamic.all() as d]
  Query
    PhysicalMultiStatement
      PhysicalProject [l.Team as l.Team, l.Name as l.Name, l.Score as l.Score, r.Team as r.Team, r.Name as r.Name, r.Score as r.Score]
        PhysicalNestedLoopJoin [AsofInner] [((l.Team = r.Team) AND (l.Score >= r.Score))]
          PhysicalSchemaScan [#dynamic.all() as l]
          PhysicalCteRef [rightCte as r]
      PhysicalProject [l.Name as LeftName, r.Name as RightName]
        PhysicalCteRef [lr as lr]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    ExpandoAdapter [d: dDynamicRow0]
      Team: string <- expando key "Team"
      Name: string <- expando key "Name"
      Score: int <- expando key "Score"
    Generated [Cte0Row0]
      Team: string <- field Team
      Name: string <- field Name
      Score: int <- field Score
    ExpandoAdapter [l: lDynamicRow0]
      Team: string <- expando key "Team"
      Name: string <- expando key "Name"
      Score: int <- expando key "Score"
    TableRow [r]
      Team: string <- field Team
      Name: string <- field Name
      Score: int <- field Score
    Generated [ResultRow0]
      LeftName: string <- field LeftName
      RightName: string <- field RightName

  Body
    SourceScan [d: IReadOnlyDictionary<string, object>] -> cte0_dRows
    CreateTable [cte0: Cte0Row0]
    ChunkedForEach [dResolver in cte0_dRows]
      AdaptExpando [d: dDynamicRow0 <- dResolver]
      AppendRow [cte0 <- Cte0Row0(Team: d.Team, Name: d.Name, Score: d.Score)]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    CtePhase [cte1]
    SourceScan [l: IReadOnlyDictionary<string, object>] -> lRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateAsOfIndex [resultAsOfIndex <- _cteRowResults.Slot0 by rCandidate.Team, rCandidate.Score]
    ChunkedForEach [lResolver in lRows]
      AdaptExpando [l: lDynamicRow0 <- lResolver]
      AsOfProbe [r <- _cteRowResults.Slot0 using resultAsOfIndex where l.Team = rCandidate.Team and l.Score >= rCandidate.Score]
        AppendShape [result <- ResultShape0(LeftName: l.Name, RightName: r.Name)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable
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
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_d_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Team", typeof(string), 0), new Column("Name", typeof(string), 1), new Column("Score", typeof(int), 2) });
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
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.LeftName, __musoqShapeRow.RightName);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults);
                var __lSchema = provider.GetSchema("#dynamic");
                var lRowsSource = __lSchema.GetRowSource<IReadOnlyDictionary<string, object>>("all", new SourceExecutionContext("l:2", sourceExecutionPlans["l:2"], token, __schemaColumns_compiled_d_0, sourceRuntimeSettingsBySourceContextId["l:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                var lRows = lRowsSource.Chunks;
                var resultAsOfIndex = EvaluationHelper.CreateAsOfIndex<Cte0Row0, int>(_cteRowResults.Slot0, (rCandidate) => (object)rCandidate.Team, (rCandidate) => rCandidate.Score, Musoq.Evaluator.IR.Expressions.BinaryOpKind.GreaterOrEqual);
                foreach (var lResolverChunk in lRows)
                {
                    if (lResolverChunk is global::Musoq.Schema.DataSources.RowChunk<IReadOnlyDictionary<string, object>> lResolverChunkView)
                    {
                        if (lResolverChunkView.Source is IReadOnlyDictionary<string, object>[] lResolverChunkViewArray)
                        {
                            int lResolverChunkViewOffset = lResolverChunkView.Offset;
                            for (int lResolverIndex = 0, lResolverIndexCount = lResolverChunkView.Count; lResolverIndex < lResolverIndexCount; ++lResolverIndex)
                            {
                                if ((lResolverIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var lResolver = lResolverChunkViewArray[lResolverChunkViewOffset + lResolverIndex];
                                var l = new lDynamicRow0(lResolver.ContainsKey("Team") ? (string)lResolver["Team"] : default(string), lResolver.ContainsKey("Name") ? (string)lResolver["Name"] : default(string), lResolver.ContainsKey("Score") ? (int)lResolver["Score"] : default(int));
                                {
                                    var r = resultAsOfIndex.Find((object)l.Team, l.Score);
                                    if (r != null)
                                    {
                                        __musoqFinalShapeRows.Add(new ResultShape0(l.Name, r.Name));
                                    }
                                }
                            }

                            continue;
                        }

                        if (lResolverChunkView.Source is List<IReadOnlyDictionary<string, object>> lResolverChunkViewList)
                        {
                            int lResolverChunkViewOffset = lResolverChunkView.Offset;
                            for (int lResolverIndex = 0, lResolverIndexCount = lResolverChunkView.Count; lResolverIndex < lResolverIndexCount; ++lResolverIndex)
                            {
                                if ((lResolverIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var lResolver = lResolverChunkViewList[lResolverChunkViewOffset + lResolverIndex];
                                var l = new lDynamicRow0(lResolver.ContainsKey("Team") ? (string)lResolver["Team"] : default(string), lResolver.ContainsKey("Name") ? (string)lResolver["Name"] : default(string), lResolver.ContainsKey("Score") ? (int)lResolver["Score"] : default(int));
                                {
                                    var r = resultAsOfIndex.Find((object)l.Team, l.Score);
                                    if (r != null)
                                    {
                                        __musoqFinalShapeRows.Add(new ResultShape0(l.Name, r.Name));
                                    }
                                }
                            }

                            continue;
                        }
                    }

                    for (int lResolverIndex = 0, lResolverIndexCount = lResolverChunk.Count; lResolverIndex < lResolverIndexCount; ++lResolverIndex)
                    {
                        if ((lResolverIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var lResolver = lResolverChunk[lResolverIndex];
                        var l = new lDynamicRow0(lResolver.ContainsKey("Team") ? (string)lResolver["Team"] : default(string), lResolver.ContainsKey("Name") ? (string)lResolver["Name"] : default(string), lResolver.ContainsKey("Score") ? (int)lResolver["Score"] : default(int));
                        {
                            var r = resultAsOfIndex.Find((object)l.Team, l.Score);
                            if (r != null)
                            {
                                __musoqFinalShapeRows.Add(new ResultShape0(l.Name, r.Name));
                            }
                        }
                    }
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
                OnPhaseChanged("compiled", QueryPhase.End);
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
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, CteRowResults _cteRowResults)
        {
            var __cte0_dSchema = provider.GetSchema("#dynamic");
            var cte0_dRowsSource = __cte0_dSchema.GetRowSource<IReadOnlyDictionary<string, object>>("all", new SourceExecutionContext("d:1", sourceExecutionPlans["d:1"], token, __schemaColumns_compiled_d_0, sourceRuntimeSettingsBySourceContextId["d:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var cte0_dRows = cte0_dRowsSource.Chunks;
            var cte0 = new List<Cte0Row0>();
            foreach (var dResolverChunk in cte0_dRows)
            {
                if (dResolverChunk is global::Musoq.Schema.DataSources.RowChunk<IReadOnlyDictionary<string, object>> dResolverChunkView)
                {
                    if (dResolverChunkView.Source is IReadOnlyDictionary<string, object>[] dResolverChunkViewArray)
                    {
                        int dResolverChunkViewOffset = dResolverChunkView.Offset;
                        for (int dResolverIndex = 0, dResolverIndexCount = dResolverChunkView.Count; dResolverIndex < dResolverIndexCount; ++dResolverIndex)
                        {
                            if ((dResolverIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var dResolver = dResolverChunkViewArray[dResolverChunkViewOffset + dResolverIndex];
                            var d = new dDynamicRow0(dResolver.ContainsKey("Team") ? (string)dResolver["Team"] : default(string), dResolver.ContainsKey("Name") ? (string)dResolver["Name"] : default(string), dResolver.ContainsKey("Score") ? (int)dResolver["Score"] : default(int));
                            cte0.Add(new Cte0Row0(d.Team, d.Name, d.Score));
                        }

                        continue;
                    }

                    if (dResolverChunkView.Source is List<IReadOnlyDictionary<string, object>> dResolverChunkViewList)
                    {
                        int dResolverChunkViewOffset = dResolverChunkView.Offset;
                        for (int dResolverIndex = 0, dResolverIndexCount = dResolverChunkView.Count; dResolverIndex < dResolverIndexCount; ++dResolverIndex)
                        {
                            if ((dResolverIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var dResolver = dResolverChunkViewList[dResolverChunkViewOffset + dResolverIndex];
                            var d = new dDynamicRow0(dResolver.ContainsKey("Team") ? (string)dResolver["Team"] : default(string), dResolver.ContainsKey("Name") ? (string)dResolver["Name"] : default(string), dResolver.ContainsKey("Score") ? (int)dResolver["Score"] : default(int));
                            cte0.Add(new Cte0Row0(d.Team, d.Name, d.Score));
                        }

                        continue;
                    }
                }

                for (int dResolverIndex = 0, dResolverIndexCount = dResolverChunk.Count; dResolverIndex < dResolverIndexCount; ++dResolverIndex)
                {
                    if ((dResolverIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var dResolver = dResolverChunk[dResolverIndex];
                    var d = new dDynamicRow0(dResolver.ContainsKey("Team") ? (string)dResolver["Team"] : default(string), dResolver.ContainsKey("Name") ? (string)dResolver["Name"] : default(string), dResolver.ContainsKey("Score") ? (int)dResolver["Score"] : default(int));
                    cte0.Add(new Cte0Row0(d.Team, d.Name, d.Score));
                }
            }

            return cte0;
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

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        LeftName = (string)value;
                        break;
                    case 1:
                        RightName = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "LeftName" => true,
                "RightName" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)LeftName,
                1 => (object)RightName,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "LeftName" => (object)LeftName,
                "RightName" => (object)RightName,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string LeftName, string RightName)
            {
                this.LeftName = LeftName;
                this.RightName = RightName;
            }

            public string LeftName { get; }
            public string RightName { get; }
        }

        private sealed class dDynamicRow0
        {
            public dDynamicRow0(string Team, string Name, int Score)
            {
                this.Team = Team;
                this.Name = Name;
                this.Score = Score;
            }

            public string Name { get; }
            public int Score { get; }
            public string Team { get; }
        }

        private sealed class lDynamicRow0
        {
            public lDynamicRow0(string Team, string Name, int Score)
            {
                this.Team = Team;
                this.Name = Name;
                this.Score = Score;
            }

            public string Name { get; }
            public int Score { get; }
            public string Team { get; }
        }
    }
}
