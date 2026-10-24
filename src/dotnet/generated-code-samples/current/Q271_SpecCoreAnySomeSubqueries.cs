// === Parsed Query ===
/*
select a.City, a.Country = ANY (select b.Country from #B.entities() b) as AnyMatch, a.Population > SOME (select c.Population from #C.entities() c) as SomeMatch from #A.entities() a
*/

// === Logical Plan ===
/*
MultiStatement
  Project [a.City as City, ApplyAny<Equal>[Subquery0](a.Country; MultiStatement
  Project [b.Country as Country]
    SchemaScan [#B.entities() as b]) as AnyMatch, ApplyAny<GreaterThan>[Subquery1](a.Population; MultiStatement
  Project [c.Population as Population]
    SchemaScan [#C.entities() as c]) as SomeMatch]
    SchemaScan [#A.entities() as a]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.City as City, ApplyAny<Equal>[Subquery0](a.Country; MultiStatement
  Project [b.Country as Country]
    SchemaScan [#B.entities() as b]) as AnyMatch, ApplyAny<GreaterThan>[Subquery1](a.Population; MultiStatement
  Project [c.Population as Population]
    SchemaScan [#C.entities() as c]) as SomeMatch]
    PhysicalSchemaScan [#A.entities() as a]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: BasicEntity]
      City: string <- property City
      Country: string <- property Country
      Population: decimal <- property Population
    Generated [ResultRow0]
      City: string <- field City
      AnyMatch: bool? <- field AnyMatch
      SomeMatch: bool? <- field SomeMatch

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [a: BasicEntity] -> aRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [a in aRows]
      AppendShape [result <- ResultRow0(City: a.City, AnyMatch: ApplyAny<Equal>(a.Country)[Subquery0](), SomeMatch: ApplyAny<GreaterThan>(a.Population)[Subquery1]())]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]

  Subquery
ExecutionPlan [Subquery0]
  Shapes
    SourceEntity [b: BasicEntity]
      Country: string <- property Country
    Generated [Subquery0ResultRow0]
      Country: string <- field Country

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [b: BasicEntity] -> bRows
    CreateShapeRows [result: Subquery0ResultRow0 from Subquery0ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [b in bRows]
      AppendShape [result <- Subquery0ResultRow0(Country: b.Country)]
    ReturnDeferredTable [result: Subquery0ResultRow0 <- Subquery0ResultRow0]

  Subquery
ExecutionPlan [Subquery1]
  Shapes
    SourceEntity [c: BasicEntity]
      Population: decimal <- property Population
    Generated [Subquery1ResultRow0]
      Population: decimal <- field Population

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [c: BasicEntity] -> cRows
    CreateShapeRows [result: Subquery1ResultRow0 from Subquery1ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [c in cRows]
      AppendShape [result <- Subquery1ResultRow0(Population: c.Population)]
    ReturnDeferredTable [result: Subquery1ResultRow0 <- Subquery1ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q271_SpecCoreAnySomeSubqueries
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
        private static readonly Column[] __columns_Subquery0_result_1 = new Column[]
        {
            new Column("Country", typeof(string), 0)
        };
        private static readonly Column[] __columns_Subquery1_result_1 = new Column[]
        {
            new Column("Population", typeof(decimal), 0)
        };
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("City", typeof(string), 0),
            new Column("AnyMatch", typeof(bool?), 1),
            new Column("SomeMatch", typeof(bool?), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_Subquery0_b_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Country", typeof(string), 0) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_Subquery1_c_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Population", typeof(decimal), 0) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 0), new Column("Country", typeof(string), 1), new Column("Population", typeof(decimal), 2) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
            ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
            this.OnPhaseChanged("compiled", QueryPhase.Begin);
            this.OnPhaseChanged("compiled", QueryPhase.From);
            var __aSchema = provider.GetSchema("#A");
            var aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__aSchema, "entities", new SourceExecutionContext("a:3", sourceExecutionPlans["a:3"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:3"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(aRowsSource.Chunks, __musoqProgressContext, "a:3") : aRowsSource.Chunks;
            var __musoqTableSourceRows = aRows;
            this.OnPhaseChanged("compiled", QueryPhase.Select);
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectRowsSerial<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity, ResultRow0>(__musoqTableSourceRows, (a) => true, (a) => new ResultRow0(a.City, global::Musoq.Evaluator.Runtime.SubqueryExecution.Any<Subquery0ResultRow0, string, string>(a.Country, __ExecuteSubquery0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged), static row => (string)row.Country, static (left, right) => Operators.SqlCompare<string, string>(left, right, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)), token), global::Musoq.Evaluator.Runtime.SubqueryExecution.Any<Subquery1ResultRow0, decimal, decimal>(a.Population, __ExecuteSubquery1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged), static row => (decimal)row.Population, static (left, right) => (left > right), token)), token), token, onCompleted: () =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }, onException: (Exception _) =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }, onDisposed: () =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            });
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

        private static IEnumerable<Subquery0ResultRow0> __ExecuteSubquery0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged)
        {
            try
            {
                OnPhaseChanged("Subquery0", QueryPhase.Begin);
                OnPhaseChanged("Subquery0", QueryPhase.From);
                var __bSchema = provider.GetSchema("#B");
                var bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__bSchema, "entities", new SourceExecutionContext("b:2", sourceExecutionPlans["b:2"], token, __schemaColumns_Subquery0_b_0, sourceRuntimeSettingsBySourceContextId["b:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(bRowsSource.Chunks, __musoqProgressContext, "b:2") : bRowsSource.Chunks;
                OnPhaseChanged("Subquery0", QueryPhase.Select);
                foreach (var bChunk in bRows)
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
                                yield return new Subquery0ResultRow0(b.Country);
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
                                yield return new Subquery0ResultRow0(b.Country);
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
                        yield return new Subquery0ResultRow0(b.Country);
                    }
                }
            }
            finally
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("Subquery0", QueryPhase.End);
                }
            }
        }

        private static IEnumerable<Subquery1ResultRow0> __ExecuteSubquery1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged)
        {
            try
            {
                OnPhaseChanged("Subquery1", QueryPhase.Begin);
                OnPhaseChanged("Subquery1", QueryPhase.From);
                var __cSchema = provider.GetSchema("#C");
                var cRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__cSchema, "entities", new SourceExecutionContext("c:3", sourceExecutionPlans["c:3"], token, __schemaColumns_Subquery1_c_0, sourceRuntimeSettingsBySourceContextId["c:3"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var cRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cRowsSource.Chunks, __musoqProgressContext, "c:3") : cRowsSource.Chunks;
                OnPhaseChanged("Subquery1", QueryPhase.Select);
                foreach (var cChunk in cRows)
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
                                yield return new Subquery1ResultRow0(c.Population);
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
                                yield return new Subquery1ResultRow0(c.Population);
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
                        yield return new Subquery1ResultRow0(c.Population);
                    }
                }
            }
            finally
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("Subquery1", QueryPhase.End);
                }
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, bool? __value1, bool? __value2)
            {
                City = __value0;
                AnyMatch = __value1;
                SomeMatch = __value2;
            }

            public bool? AnyMatch { get; private set; }
            public string City { get; private set; }
            public override int Count => 3;
            public bool? SomeMatch { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool?>(this.AnyMatch = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<bool?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool?>(this.SomeMatch = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<bool?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool?>(this.AnyMatch),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, bool?>(this.SomeMatch),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "City" => true,
                "AnyMatch" => true,
                "SomeMatch" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.City, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.AnyMatch, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.SomeMatch, ref visitor);
            }
        }

        private sealed class Subquery0ResultRow0 : Row
        {
            public Subquery0ResultRow0(string __value0)
            {
                Country = __value0;
            }

            public override int Count => 1;
            public string Country { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Country = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Country),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Country" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Country, ref visitor);
            }
        }

        private sealed class Subquery1ResultRow0 : Row
        {
            public Subquery1ResultRow0(decimal __value0)
            {
                Population = __value0;
            }

            public override int Count => 1;
            public decimal Population { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Population = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Population),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Population" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Population, ref visitor);
            }
        }
    }
}
