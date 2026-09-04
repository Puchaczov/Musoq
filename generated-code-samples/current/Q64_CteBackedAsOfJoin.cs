// === Parsed Query ===
/*
with rightCte as (select e.Name as Name, e.Population as Population from #A.entities() e) select a.Name, a.Population, r.Name, r.Population from #A.entities() a asof join rightCte r on a.Population >= r.Population
*/

// === Logical Plan ===
/*
Cte
  Definition [rightCte]
    MultiStatement
      Project [e.Name as Name, e.Population as Population]
        SchemaScan [#A.entities() as e]
  Query
    MultiStatement
      Project [a.Name as a.Name, a.Population as a.Population, r.Name as r.Name, r.Population as r.Population]
        Join [AsofInner] [(a.Population >= r.Population)]
          SchemaScan [#A.entities() as a]
          CteRef [rightCte as r]
      Project [a.Name as a.Name, a.Population as a.Population, r.Name as r.Name, r.Population as r.Population]
        CteRef [ar as ar]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [rightCte]
    PhysicalMultiStatement
      PhysicalProject [e.Name as Name, e.Population as Population]
        PhysicalSchemaScan [#A.entities() as e]
  Query
    PhysicalMultiStatement
      PhysicalProject [a.Name as a.Name, a.Population as a.Population, r.Name as r.Name, r.Population as r.Population]
        PhysicalNestedLoopJoin [AsofInner] [(a.Population >= r.Population)]
          PhysicalSchemaScan [#A.entities() as a]
          PhysicalCteRef [rightCte as r]
      PhysicalProject [a.Name as a.Name, a.Population as a.Population, r.Name as r.Name, r.Population as r.Population]
        PhysicalCteRef [ar as ar]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [e: BasicEntity]
      Name: string <- property Name
      Population: decimal <- property Population
    Generated [Cte0Row0]
      Name: string <- field Name
      Population: decimal <- field Population
    SourceEntity [a: BasicEntity]
      Name: string <- property Name
      Population: decimal <- property Population
    TableRow [r]
      Name: string <- field Name
      Population: decimal <- field Population
    Generated [ResultRow0]
      a.Name: string <- field a_Name
      a.Population: decimal <- field a_Population
      r.Name: string <- field r_Name
      r.Population: decimal <- field r_Population

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    SourceScan [e: BasicEntity] -> cte0_eRows
    CreateTable [cte0: Cte0Row0]
    PhaseBoundary [Select:cte0]
    ChunkedForEach [e in cte0_eRows]
      AppendRow [cte0 <- Cte0Row0(Name: e.Name, Population: e.Population)]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    SourceScan [a: BasicEntity] -> aRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateAsOfIndex [resultAsOfIndex <- _cteRowResults.Slot0 by rCandidate.Population]
    ChunkedForEach [a in aRows]
      AsOfProbe [r <- _cteRowResults.Slot0 using resultAsOfIndex where a.Population >= rCandidate.Population]
        AppendShape [result <- ResultShape0(a.Name: a.Name, a.Population: a.Population, r.Name: r.Name, r.Population: r.Population)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q64_CteBackedAsOfJoin
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
            new Column("Name", typeof(string), 0),
            new Column("Population", typeof(decimal), 1)
        };
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("a.Name", typeof(string), 0),
            new Column("a.Population", typeof(decimal), 1),
            new Column("r.Name", typeof(string), 2),
            new Column("r.Population", typeof(decimal), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_e_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("Population", typeof(decimal), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.a_Name, __musoqShapeRow.a_Population, __musoqShapeRow.r_Name, __musoqShapeRow.r_Population);
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
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
                try
                {
                    var __aSchema = provider.GetSchema("#A");
                    var aRowsSource = __aSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("a:2", sourceExecutionPlans["a:2"], token, __schemaColumns_compiled_e_0, sourceRuntimeSettingsBySourceContextId["a:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                    var aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(aRowsSource.Chunks, __musoqProgressContext, "a:2") : aRowsSource.Chunks;
                    var resultAsOfIndex = EvaluationHelper.CreateAsOfIndex<Cte0Row0, decimal>(_cteRowResults.Slot0, null, (rCandidate) => rCandidate.Population, Musoq.Evaluator.IR.Expressions.BinaryOpKind.GreaterOrEqual);
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
                                    {
                                        var r = resultAsOfIndex.Find(null, a.Population);
                                        if (r != null)
                                        {
                                            __musoqFinalShapeRows.Add(new ResultShape0(a.Name, a.Population, r.Name, r.Population));
                                        }
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
                                    {
                                        var r = resultAsOfIndex.Find(null, a.Population);
                                        if (r != null)
                                        {
                                            __musoqFinalShapeRows.Add(new ResultShape0(a.Name, a.Population, r.Name, r.Population));
                                        }
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
                            {
                                var r = resultAsOfIndex.Find(null, a.Population);
                                if (r != null)
                                {
                                    __musoqFinalShapeRows.Add(new ResultShape0(a.Name, a.Population, r.Name, r.Population));
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
                var __cte0_eSchema = provider.GetSchema("#A");
                var cte0_eRowsSource = __cte0_eSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("e:1", sourceExecutionPlans["e:1"], token, __schemaColumns_compiled_e_0, sourceRuntimeSettingsBySourceContextId["e:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte0_eRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0_eRowsSource.Chunks, __musoqProgressContext, "e:1") : cte0_eRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                foreach (var eChunk in cte0_eRows)
                {
                    if (eChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> eChunkView)
                    {
                        if (eChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] eChunkViewArray)
                        {
                            int eChunkViewOffset = eChunkView.Offset;
                            for (int eIndex = 0, eIndexCount = eChunkView.Count; eIndex < eIndexCount; ++eIndex)
                            {
                                if ((eIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var e = eChunkViewArray[eChunkViewOffset + eIndex];
                                cte0.Add(new Cte0Row0(e.Name, e.Population));
                            }

                            continue;
                        }

                        if (eChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> eChunkViewList)
                        {
                            int eChunkViewOffset = eChunkView.Offset;
                            for (int eIndex = 0, eIndexCount = eChunkView.Count; eIndex < eIndexCount; ++eIndex)
                            {
                                if ((eIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var e = eChunkViewList[eChunkViewOffset + eIndex];
                                cte0.Add(new Cte0Row0(e.Name, e.Population));
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
                        cte0.Add(new Cte0Row0(e.Name, e.Population));
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
            public Cte0Row0(string __value0, decimal __value1)
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
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, decimal __value1, string __value2, decimal __value3)
            {
                a_Name = __value0;
                a_Population = __value1;
                r_Name = __value2;
                r_Population = __value3;
            }

            public override int Count => 4;
            public string a_Name { get; private set; }
            public decimal a_Population { get; private set; }
            public string r_Name { get; private set; }
            public decimal r_Population { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        a_Name = (string)value;
                        break;
                    case 1:
                        a_Population = (decimal)value;
                        break;
                    case 2:
                        r_Name = (string)value;
                        break;
                    case 3:
                        r_Population = (decimal)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "a.Name" => true,
                "a_Name" => true,
                "a.Population" => true,
                "a_Population" => true,
                "r.Name" => true,
                "r_Name" => true,
                "r.Population" => true,
                "r_Population" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)a_Name,
                1 => (object)a_Population,
                2 => (object)r_Name,
                3 => (object)r_Population,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "a.Name" => (object)a_Name,
                "a_Name" => (object)a_Name,
                "a.Population" => (object)a_Population,
                "a_Population" => (object)a_Population,
                "r.Name" => (object)r_Name,
                "r_Name" => (object)r_Name,
                "r.Population" => (object)r_Population,
                "r_Population" => (object)r_Population,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string a_Name, decimal a_Population, string r_Name, decimal r_Population)
            {
                this.a_Name = a_Name;
                this.a_Population = a_Population;
                this.r_Name = r_Name;
                this.r_Population = r_Population;
            }

            public string a_Name { get; }
            public decimal a_Population { get; }
            public string r_Name { get; }
            public decimal r_Population { get; }
        }
    }
}
