// === Parsed Query ===
/*
select a.Name, b.Country from #A.entities() a inner join #A.entities() b on a.Id = b.Id where a.Population > 0
*/

// === Logical Plan ===
/*
MultiStatement
  Project [a.Name as a.Name, a.Population as a.Population, a.Id as a.Id, b.Country as b.Country, b.Id as b.Id]
    Join [Inner] [(a.Id = b.Id)]
      SchemaScan [#A.entities() as a]
      SchemaScan [#A.entities() as b]
  Project [a.Name as Name, b.Country as Country]
    Filter [(a.Population > 0)]
      CteRef [ab as ab]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.Name as a.Name, a.Population as a.Population, b.Country as b.Country]
    PhysicalHashJoin [Inner] [build: b.Id] [probe: a.Id]
      PhysicalFilter [(a.Population > 0)]
        PhysicalSchemaScan [#A.entities() as a] [pushdown: (a.Population > 0)]
      PhysicalSchemaScan [#A.entities() as b]
  PhysicalProject [a.Name as Name, b.Country as Country]
    PhysicalFilter [(a.Population > 0)]
      PhysicalCteRef [ab as ab]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: BasicEntity]
      Name: string <- property Name
      Population: decimal <- property Population
      Id: int <- property Id
    SourceEntity [b: BasicEntity]
      Country: string <- property Country
      Id: int <- property Id
    Generated [ResultRow0]
      Name: string <- field Name
      Country: string <- field Country

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    SourceScan [a: BasicEntity] -> aRows
    MaterializeFilteredChunked [aRows where (a.Population > 0) -> aPrefilteredRows]
    SourceScan [b: BasicEntity] -> bRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateHash [bHash: int -> BasicEntity]
    ChunkedForEach [b in bRows]
      HashAdd [bHash[b.Id] += b]
    ForEach [a in aPrefilteredRows]
      HashProbe [bHash[a.Id] -> bHashMatches]
        ForEach [b in bHashMatches]
          If [(a.Population > 0)]
            AppendShape [result <- ResultRow0(Name: a.Name, Country: b.Country)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q34_InnerJoinWithWhere
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("Country", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("Population", typeof(decimal), 1), new Column("Id", typeof(int), 2) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Country", typeof(string), 0), new Column("Id", typeof(int), 1) });
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
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled", QueryPhase.Where);
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    var __aSchema = provider.GetSchema("#A");
                    var aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__aSchema, "entities", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                    var aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(aRowsSource.Chunks, __musoqProgressContext, "a:1") : aRowsSource.Chunks;
                    var aPrefilteredRows = new List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>();
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
                                        if ((a.Population > 0))
                                        {
                                            aPrefilteredRows.Add(a);
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
                                        if ((a.Population > 0))
                                        {
                                            aPrefilteredRows.Add(a);
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
                                if ((a.Population > 0))
                                {
                                    aPrefilteredRows.Add(a);
                                }
                            }
                        }
                    }

                    var __bSchema = provider.GetSchema("#A");
                    var bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__bSchema, "entities", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_b_1, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                    var bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(bRowsSource.Chunks, __musoqProgressContext, "b:1") : bRowsSource.Chunks;
                    var bHash = new Dictionary<int, HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>>();
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
                                    int key = b.Id;
                                    {
                                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(bHash, key, out var matchesExists);
                                        if (!matchesExists)
                                        {
                                            matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(b);
                                        }
                                        else
                                        {
                                            matches.Add(b);
                                        }
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
                                    int key = b.Id;
                                    {
                                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(bHash, key, out var matchesExists);
                                        if (!matchesExists)
                                        {
                                            matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(b);
                                        }
                                        else
                                        {
                                            matches.Add(b);
                                        }
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
                            int key = b.Id;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(bHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(b);
                                }
                                else
                                {
                                    matches.Add(b);
                                }
                            }
                        }
                    }

                    foreach (var a in aPrefilteredRows)
                    {
                        token.ThrowIfCancellationRequested();
                        int key = a.Id;
                        if (bHash.TryGetValue(key, out var bHashMatches))
                        {
                            foreach (var b in bHashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                if ((a.Population > 0))
                                {
                                    __musoqFinalShapeRows.Add(new ResultRow0(a.Name, b.Country));
                                }
                            }
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                Name = __value0;
                Country = __value1;
            }

            public override int Count => 2;
            public string Country { get; private set; }
            public string Name { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Country = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Country),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Country" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Country, ref visitor);
            }
        }
    }
}
