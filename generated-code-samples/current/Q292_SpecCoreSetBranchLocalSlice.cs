// === Parsed Query ===
/*
with sliced as (select Name from #A.entities() order by Name take 1) select sliced.Name as Label from sliced union all select Name from #B.entities() order by Label desc
*/

// === Logical Plan ===
/*
Cte
  Definition [sliced]
    MultiStatement
      Take [1]
        Sort [ko3iko.Name]
          Project [ko3iko.Name as Name]
            SchemaScan [#A.entities() as ko3iko]
  Query
    Sort [Label DESC]
      SetOp [UnionAll]
        MultiStatement
          Project [sliced.Name as Label]
            CteRef [sliced as sliced]
        MultiStatement
          Project [gougbq.Name as Name]
            SchemaScan [#B.entities() as gougbq]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [sliced]
    PhysicalMultiStatement
      PhysicalTopN [1] [ko3iko.Name]
        PhysicalProject [ko3iko.Name as Name]
          PhysicalSchemaScan [#A.entities() as ko3iko]
  Query
    PhysicalSort [Label DESC]
      PhysicalSetOp [UnionAll]
        PhysicalMultiStatement
          PhysicalProject [sliced.Name as Label]
            PhysicalCteRef [sliced as sliced]
        PhysicalMultiStatement
          PhysicalProject [gougbq.Name as Name]
            PhysicalSchemaScan [#B.entities() as gougbq]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
    Generated [Cte0Row0]
      Name: string <- field Name
    TableRow [sliced]
      Name: string <- field Name
    SourceEntity [gougbq: BasicEntity]
      Name: string <- property Name
    Generated [ResultRow0]
      Label: string <- field Label

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [From:cte0]
    SourceScan [ko3iko: BasicEntity] -> cte0_ko3ikoRows
    CreateTable [cte0: Cte0Row0]
    PhaseBoundary [Select:cte0]
    ChunkedForEach [ko3iko in cte0_ko3ikoRows]
      AppendRow [cte0 <- Cte0Row0(Name: ko3iko.Name)]
    TopNTable [cte0 -> cte0TopN by Name ASC, 1]
    StoreTable [cte0TopN -> _tableResults[0]]
    PhaseBoundary [End:cte0]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Begin:left]
    PhaseBoundary [From:left]
    ForEach [sliced in CastGeneratedRows<Cte0Row0>(_tableResults[0].Rows)]
      AppendShape [result <- ResultShape0(Label: sliced.Name)]
    PhaseBoundary [Select:left]
    PhaseBoundary [End:left]
    PhaseBoundary [Begin:right]
    PhaseBoundary [From:right]
    SourceScan [gougbq: BasicEntity] -> right_gougbqRows
    PhaseBoundary [Select:right]
    ChunkedForEach [gougbq in right_gougbqRows]
      AppendShape [result <- ResultShape0(Name: gougbq.Name)]
    PhaseBoundary [End:right]
    SortShapeRows [result -> resultSorted by Label DESC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q292_SpecCoreSetBranchLocalSlice
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
            new Column("Name", typeof(string), 0)
        };
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Label", typeof(string), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Label);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _tableResults = new Musoq.Evaluator.Tables.Table[1];
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                Musoq.Evaluator.Tables.Table cte0 = null!;
                try
                {
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    OnPhaseChanged("compiled:cte0", QueryPhase.From);
                    var __cte0_ko3ikoSchema = provider.GetSchema("#A");
                    var cte0_ko3ikoRowsSource = __cte0_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                    var cte0_ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : cte0_ko3ikoRowsSource.Chunks;
                    cte0 = new Table("cte0", __columns_compiled_cte0_1);
                    OnPhaseChanged("compiled:cte0", QueryPhase.Select);
                    foreach (var ko3ikoChunk in cte0_ko3ikoRows)
                    {
                        if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkView)
                        {
                            if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] ko3ikoChunkViewArray)
                            {
                                int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                                for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                                {
                                    if ((ko3ikoIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                    cte0.AddDirect(new Cte0Row0(ko3iko.Name));
                                }

                                continue;
                            }

                            if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkViewList)
                            {
                                int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                                for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                                {
                                    if ((ko3ikoIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                    cte0.AddDirect(new Cte0Row0(ko3iko.Name));
                                }

                                continue;
                            }
                        }

                        for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunk.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                        {
                            if ((ko3ikoIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var ko3iko = ko3ikoChunk[ko3ikoIndex];
                            cte0.AddDirect(new Cte0Row0(ko3iko.Name));
                        }
                    }

                    var cte0TopNRows = EvaluationHelper.CastGeneratedRows<Cte0Row0>(cte0.Rows).OrderBy((row) => row, Cte0Row0OrderBy_0AComparer.Instance).Take(1);
                    var cte0TopN = new Table("cte0TopN", __columns_compiled_cte0_1);
                    cte0TopN.EnsureCapacity(Math.Min(cte0.Count, 1));
                    foreach (var copiedRow in cte0TopNRows)
                    {
                        cte0TopN.AddDirect(copiedRow);
                    }

                    _tableResults[0] = cte0TopN;
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
                }

                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled:left", QueryPhase.Begin);
                OnPhaseChanged("compiled:left", QueryPhase.From);
                var __storedTable0Rows = EvaluationHelper.CastGeneratedRows<Cte0Row0>(_tableResults[0].Rows);
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 sliced = (Cte0Row0)__storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(sliced.Name));
                }

                OnPhaseChanged("compiled:left", QueryPhase.Select);
                OnPhaseChanged("compiled:left", QueryPhase.End);
                OnPhaseChanged("compiled:right", QueryPhase.Begin);
                OnPhaseChanged("compiled:right", QueryPhase.From);
                var __right_gougbqSchema = provider.GetSchema("#B");
                var right_gougbqRowsSource = __right_gougbqSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("gougbq:3", sourceExecutionPlans["gougbq:3"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["gougbq:3"], logger, OnDataSourceProgress), Array.Empty<object>());
                var right_gougbqRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(right_gougbqRowsSource.Chunks, __musoqProgressContext, "gougbq:3") : right_gougbqRowsSource.Chunks;
                OnPhaseChanged("compiled:right", QueryPhase.Select);
                foreach (var gougbqChunk in right_gougbqRows)
                {
                    if (gougbqChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> gougbqChunkView)
                    {
                        if (gougbqChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] gougbqChunkViewArray)
                        {
                            int gougbqChunkViewOffset = gougbqChunkView.Offset;
                            for (int gougbqIndex = 0, gougbqIndexCount = gougbqChunkView.Count; gougbqIndex < gougbqIndexCount; ++gougbqIndex)
                            {
                                if ((gougbqIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var gougbq = gougbqChunkViewArray[gougbqChunkViewOffset + gougbqIndex];
                                result.Add(new ResultShape0(gougbq.Name));
                            }

                            continue;
                        }

                        if (gougbqChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> gougbqChunkViewList)
                        {
                            int gougbqChunkViewOffset = gougbqChunkView.Offset;
                            for (int gougbqIndex = 0, gougbqIndexCount = gougbqChunkView.Count; gougbqIndex < gougbqIndexCount; ++gougbqIndex)
                            {
                                if ((gougbqIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var gougbq = gougbqChunkViewList[gougbqChunkViewOffset + gougbqIndex];
                                result.Add(new ResultShape0(gougbq.Name));
                            }

                            continue;
                        }
                    }

                    for (int gougbqIndex = 0, gougbqIndexCount = gougbqChunk.Count; gougbqIndex < gougbqIndexCount; ++gougbqIndex)
                    {
                        if ((gougbqIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var gougbq = gougbqChunk[gougbqIndex];
                        result.Add(new ResultShape0(gougbq.Name));
                    }
                }

                OnPhaseChanged("compiled:right", QueryPhase.End);
                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.Label, right.Label);
                    comparison = -comparison;
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

        private sealed class Cte0Row0 : Row
        {
            public Cte0Row0(string __value0)
            {
                Name = __value0;
            }

            public override int Count => 1;
            public string Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class Cte0Row0OrderBy_0AComparer : IComparer<Cte0Row0>
        {
            public static readonly Cte0Row0OrderBy_0AComparer Instance = new Cte0Row0OrderBy_0AComparer();
            public int Compare(Cte0Row0 left, Cte0Row0 right)
            {
                var comparison = StringComparer.Ordinal.Compare(left.Name, right.Name);
                if (comparison != 0)
                    return comparison;
                return 0;
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0)
            {
                Label = __value0;
            }

            public override int Count => 1;
            public string Label { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Label = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Label" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Label,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Label" => (object)Label,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Label)
            {
                this.Label = Label;
            }

            public string Label { get; }
        }
    }
}
