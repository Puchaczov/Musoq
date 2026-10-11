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
      Project [a.Name as Name, a.Population as Population, r.Name as Name, r.Population as Population]
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
      PhysicalProject [a.Name as Name, a.Population as Population, r.Name as Name, r.Population as Population]
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
      Name: string <- field Name
      Population: decimal <- field Population
      Name: string <- field Name_1
      Population: decimal <- field Population_1

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
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateAsOfIndex [resultAsOfIndex <- _cteRowResults.Slot0 by rCandidate.Population]
    ChunkedForEach [a in aRows]
      AsOfProbe [r <- _cteRowResults.Slot0 using resultAsOfIndex where a.Population >= rCandidate.Population]
        AppendShape [result <- ResultRow0(Name: a.Name, Population: a.Population, Name: r.Name, Population: r.Population)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("Population", typeof(decimal), 1)
        };
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("Population", typeof(decimal), 1),
            new Column("Name", typeof(string), 2),
            new Column("Population", typeof(decimal), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_e_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("Population", typeof(decimal), 1) });
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
                    var __aSchema = provider.GetSchema("#A");
                    var aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__aSchema, "entities", new SourceExecutionContext("a:2", sourceExecutionPlans["a:2"], token, __schemaColumns_compiled_e_0, sourceRuntimeSettingsBySourceContextId["a:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                    var aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(aRowsSource.Chunks, __musoqProgressContext, "a:2") : aRowsSource.Chunks;
                    var resultAsOfIndex = EvaluationHelper.CreateAsOfIndex<Cte0Row0, decimal, bool>(_cteRowResults.Slot0, null, (rCandidate) => rCandidate.Population, Musoq.Evaluator.IR.Expressions.BinaryOpKind.GreaterOrEqual);
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
                                        var rEquality = new NativeEqualityKey<bool>(true, true ? false : default(bool));
                                        var r = resultAsOfIndex.Find(rEquality, a.Population);
                                        if (r != null)
                                        {
                                            __musoqFinalShapeRows.Add(new ResultRow0(a.Name, a.Population, r.Name, r.Population));
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
                                        var rEquality = new NativeEqualityKey<bool>(true, true ? false : default(bool));
                                        var r = resultAsOfIndex.Find(rEquality, a.Population);
                                        if (r != null)
                                        {
                                            __musoqFinalShapeRows.Add(new ResultRow0(a.Name, a.Population, r.Name, r.Population));
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
                                var rEquality = new NativeEqualityKey<bool>(true, true ? false : default(bool));
                                var r = resultAsOfIndex.Find(rEquality, a.Population);
                                if (r != null)
                                {
                                    __musoqFinalShapeRows.Add(new ResultRow0(a.Name, a.Population, r.Name, r.Population));
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
                var cte0_eRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__cte0_eSchema, "entities", new SourceExecutionContext("e:1", sourceExecutionPlans["e:1"], token, __schemaColumns_compiled_e_0, sourceRuntimeSettingsBySourceContextId["e:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
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
                Name = __value0;
                Population = __value1;
                Name_1 = __value2;
                Population_1 = __value3;
            }

            public override int Count => 4;
            public string Name { get; private set; }
            public string Name_1 { get; private set; }
            public decimal Population { get; private set; }
            public decimal Population_1 { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Population = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name_1 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Population_1 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Population),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name_1),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Population_1),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Population" => true,
                "Name_1" => true,
                "Population_1" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Population, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Name_1, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Population_1, ref visitor);
            }
        }
    }
}
