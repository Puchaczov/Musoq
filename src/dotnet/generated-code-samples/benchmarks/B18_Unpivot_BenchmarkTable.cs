// === Parsed Query ===
/*
unpivot bench.rows() r on Label in (r.Id as Original, r.Key as Grouped) using Value keep r.Id as Id order by Id, Label
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [__unpivot.Id, __unpivot.Label]
    Project [__unpivot.Id as Id, __unpivot.Label as Label, __unpivot.Value as Value]
      Unpivot [name: Label; value: Value; entries: r.Id as Original, r.Key as Grouped; keep: r.Id as Id] as __unpivot
        SchemaScan [#bench.rows() as r]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [__unpivot.Id, __unpivot.Label]
    PhysicalProject [__unpivot.Id as Id, __unpivot.Label as Label, __unpivot.Value as Value]
      PhysicalUnpivot [name: Label; value: Value; entries: r.Id as Original, r.Key as Grouped; keep: r.Id as Id] as __unpivot
        PhysicalSchemaScan [#bench.rows() as r]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Id: int <- property Id
      Key: int <- property Key
    UnknownShape [ValuesRowShape]
      Id: int <- field Id
      Label: string <- field Label
      Value: int <- field Value
    Generated [ResultRow0]
      Id: int <- field Id
      Label: string <- field Label
      Value: int <- field Value

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: FixtureRow] -> rRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [r in rRows]
      ScopedBlock
        Let [id: int = r.Id]
        CreateGeneratedRow [__unpivot <- __unpivotUnpivotCF7986C5Row0(Id: id, Label: 'Original', Value: id)]
        AppendShape [result <- ResultShape0(Id: __unpivot.Id, Label: __unpivot.Label, Value: __unpivot.Value)]
      ScopedBlock
        CreateGeneratedRow [__unpivot <- __unpivotUnpivotCF7986C5Row0(Id: r.Id, Label: 'Grouped', Value: r.Key)]
        AppendShape [result <- ResultShape0(Id: __unpivot.Id, Label: __unpivot.Label, Value: __unpivot.Value)]
    SortOwnedBuffer [result by Id ASC, Label ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultShape0 from owned buffer result]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B18_Unpivot_BenchmarkTable
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
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Key", typeof(int), 1) });
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Label", typeof(string), 1),
            new Column("Value", typeof(int), 2)
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
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
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
                var __musoqExecutionState = ExecutionState.Capture(queryContext.TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __rSchema = provider.GetSchema("#bench");
                var rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(rRowsSource.Chunks, __musoqProgressContext, "r:1") : rRowsSource.Chunks;
                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var rChunk in rRows)
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
                                {
                                    int id = r.Id;
                                    __unpivotUnpivotCF7986C5Row0 __unpivot = new __unpivotUnpivotCF7986C5Row0(id, "Original", id);
                                    result.Add(new ResultShape0(__unpivot.Id, __unpivot.Label, __unpivot.Value) { __musoqSequence = result.Count });
                                }

                                {
                                    __unpivotUnpivotCF7986C5Row0 __unpivot = new __unpivotUnpivotCF7986C5Row0(r.Id, "Grouped", r.Key);
                                    result.Add(new ResultShape0(__unpivot.Id, __unpivot.Label, __unpivot.Value) { __musoqSequence = result.Count });
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
                                {
                                    int id = r.Id;
                                    __unpivotUnpivotCF7986C5Row0 __unpivot = new __unpivotUnpivotCF7986C5Row0(id, "Original", id);
                                    result.Add(new ResultShape0(__unpivot.Id, __unpivot.Label, __unpivot.Value) { __musoqSequence = result.Count });
                                }

                                {
                                    __unpivotUnpivotCF7986C5Row0 __unpivot = new __unpivotUnpivotCF7986C5Row0(r.Id, "Grouped", r.Key);
                                    result.Add(new ResultShape0(__unpivot.Id, __unpivot.Label, __unpivot.Value) { __musoqSequence = result.Count });
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
                        {
                            int id = r.Id;
                            __unpivotUnpivotCF7986C5Row0 __unpivot = new __unpivotUnpivotCF7986C5Row0(id, "Original", id);
                            result.Add(new ResultShape0(__unpivot.Id, __unpivot.Label, __unpivot.Value) { __musoqSequence = result.Count });
                        }

                        {
                            __unpivotUnpivotCF7986C5Row0 __unpivot = new __unpivotUnpivotCF7986C5Row0(r.Id, "Grouped", r.Key);
                            result.Add(new ResultShape0(__unpivot.Id, __unpivot.Label, __unpivot.Value) { __musoqSequence = result.Count });
                        }
                    }
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
                    if (comparison != 0)
                        return comparison;
                    comparison = StringComparer.Ordinal.Compare(left.Label, right.Label);
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

        private sealed class __unpivotUnpivotCF7986C5Row0 : Row
        {
            public int Id { get; private set; }
            public string Label { get; private set; }
            public int Value { get; private set; }

            public __unpivotUnpivotCF7986C5Row0(int __value0, string __value1, int __value2)
            {
                Id = __value0;
                Label = __value1;
                Value = __value2;
            }

            public override int Count => 3;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Label, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Value, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Label),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Label" => true,
                "Value" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Label = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }
        }

        private sealed class ResultShape0 : Row
        {
            public ResultShape0(int Id, string Label, int Value)
            {
                this.Id = Id;
                this.Label = Label;
                this.Value = Value;
            }

            public int Id { get; private set; }
            public string Label { get; private set; }
            public int Value { get; private set; }
            public override int Count => 3;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Label, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Value, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Label),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Label" => true,
                "Value" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Label = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public int __musoqSequence { get; init; }
        }
    }
}
