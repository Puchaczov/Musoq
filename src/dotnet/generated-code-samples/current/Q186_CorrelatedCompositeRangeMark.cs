// === Parsed Query ===
/*
SELECT a.City,
                     CASE WHEN EXISTS (
                         SELECT b.City
                         FROM #B.entities() b
                         WHERE b.Country = a.Country
                           AND b.City = a.City
                           AND b.Population < a.Population
                     ) THEN 'Y' ELSE 'N' END AS HasEarlierPeer
              FROM #A.entities() a
*/

// === Logical Plan ===
/*
MultiStatement
  Project [a.City as City, CASE WHEN ApplyExists[Subquery0](a.Country, a.City, a.Population; MultiStatement
  Project [b.City as City]
    Filter [(((b.Country = outer[0]) AND (b.City = outer[1])) AND (b.Population < outer[2]))]
      SchemaScan [#B.entities() as b]) THEN 'Y' ELSE 'N' END as HasEarlierPeer]
    SchemaScan [#A.entities() as a]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.City as City, CASE WHEN ApplyExists[Subquery0](a.Country, a.City, a.Population; MultiStatement
  Project [b.City as City]
    Filter [(((b.Country = outer[0]) AND (b.City = outer[1])) AND (b.Population < outer[2]))]
      SchemaScan [#B.entities() as b]) THEN 'Y' ELSE 'N' END as HasEarlierPeer]
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
      HasEarlierPeer: string <- field HasEarlierPeer

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [a: BasicEntity] -> aRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [a in aRows]
      Let [city: string = a.City]
      AppendShape [result <- ResultRow0(City: city, HasEarlierPeer: CASE WHEN ApplyExists[Subquery0](a.Country, a.City, a.Population) THEN 'Y' ELSE 'N' END)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]

  Subquery
ExecutionPlan [Subquery0]
  Shapes
    SourceEntity [b: BasicEntity]
      City: string <- property City
      Country: string <- property Country
      Population: decimal <- property Population
    Generated [Subquery0ResultRow0]
      __existsValue: int <- field __existsValue

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [b: BasicEntity] -> bRows
    CreateShapeRows [result: Subquery0ResultRow0 from Subquery0ResultRow0]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [b in bRows]
      If [(((b.Country = __outer0) AND (b.City = __outer1)) AND (b.Population < __outer2))]
        AppendShape [result <- Subquery0ResultRow0(__existsValue: 1)]
    ReturnDeferredTable [result: Subquery0ResultRow0 <- Subquery0ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q186_CorrelatedCompositeRangeMark
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
            new Column("__existsValue", typeof(int), 0)
        };
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("City", typeof(string), 0),
            new Column("HasEarlierPeer", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_Subquery0_b_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 0), new Column("Country", typeof(string), 1), new Column("Population", typeof(decimal), 2) });
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
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __aSchema = provider.GetSchema("#A");
                var aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__aSchema, "entities", new SourceExecutionContext("a:2", sourceExecutionPlans["a:2"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(aRowsSource.Chunks, __musoqProgressContext, "a:2") : aRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Select);
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
                                string city = a.City;
                                yield return new ResultRow0(city, global::Musoq.Evaluator.Runtime.SubqueryExecution.Exists<Subquery0ResultRow0>(__ExecuteSubquery0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, a.Country, a.City, a.Population), token) ? (string)"Y" : (string)"N");
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
                                string city = a.City;
                                yield return new ResultRow0(city, global::Musoq.Evaluator.Runtime.SubqueryExecution.Exists<Subquery0ResultRow0>(__ExecuteSubquery0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, a.Country, a.City, a.Population), token) ? (string)"Y" : (string)"N");
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
                        string city = a.City;
                        yield return new ResultRow0(city, global::Musoq.Evaluator.Runtime.SubqueryExecution.Exists<Subquery0ResultRow0>(__ExecuteSubquery0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, a.Country, a.City, a.Population), token) ? (string)"Y" : (string)"N");
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

        private static IEnumerable<Subquery0ResultRow0> __ExecuteSubquery0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, string __outer0, string __outer1, decimal __outer2)
        {
            try
            {
                OnPhaseChanged("Subquery0", QueryPhase.Begin);
                OnPhaseChanged("Subquery0", QueryPhase.From);
                var __bSchema = provider.GetSchema("#B");
                var bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__bSchema, "entities", new SourceExecutionContext("b:2", sourceExecutionPlans["b:2"], token, __schemaColumns_Subquery0_b_0, sourceRuntimeSettingsBySourceContextId["b:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(bRowsSource.Chunks, __musoqProgressContext, "b:2") : bRowsSource.Chunks;
                OnPhaseChanged("Subquery0", QueryPhase.Where);
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
                                if (((((bool?)((((bool?)(Operators.SqlCompare<string, string>(b.Country, __outer0, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<string, string>(b.City, __outer1, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(b.City, __outer1, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null

                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)(b.Population < __outer2),
                                    _ => (bool?)(b.Population < __outer2) & (bool?)null

                                })) == true)
                                {
                                    yield return new Subquery0ResultRow0(1);
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
                                if (((((bool?)((((bool?)(Operators.SqlCompare<string, string>(b.Country, __outer0, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<string, string>(b.City, __outer1, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(b.City, __outer1, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null

                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)(b.Population < __outer2),
                                    _ => (bool?)(b.Population < __outer2) & (bool?)null

                                })) == true)
                                {
                                    yield return new Subquery0ResultRow0(1);
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
                        if (((((bool?)((((bool?)(Operators.SqlCompare<string, string>(b.Country, __outer0, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)Operators.SqlCompare<string, string>(b.City, __outer1, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                            _ => (bool?)Operators.SqlCompare<string, string>(b.City, __outer1, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null

                        }))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)(b.Population < __outer2),
                            _ => (bool?)(b.Population < __outer2) & (bool?)null

                        })) == true)
                        {
                            yield return new Subquery0ResultRow0(1);
                        }
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                City = __value0;
                HasEarlierPeer = __value1;
            }

            public string City { get; private set; }
            public override int Count => 2;
            public string HasEarlierPeer { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.HasEarlierPeer = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.HasEarlierPeer),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "City" => true,
                "HasEarlierPeer" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.City, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.HasEarlierPeer, ref visitor);
            }
        }

        private sealed class Subquery0ResultRow0 : Row
        {
            public Subquery0ResultRow0(int __value0)
            {
                __existsValue = __value0;
            }

            public override int Count => 1;
            public int __existsValue { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.__existsValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.__existsValue),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "__existsValue" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.__existsValue, ref visitor);
            }
        }
    }
}
