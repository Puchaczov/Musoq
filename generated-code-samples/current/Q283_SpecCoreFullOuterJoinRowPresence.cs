// === Parsed Query ===
/*
select a.Id, b.Id from #A.entities() a full outer join #B.entities() b on a.Id = b.Id
*/

// === Logical Plan ===
/*
MultiStatement
  Project [a.Id as a.Id, b.Id as b.Id]
    Join [FullOuter] [(a.Id = b.Id)]
      SchemaScan [#A.entities() as a]
      SchemaScan [#B.entities() as b]
  Project [a.Id as a.Id, b.Id as b.Id]
    CteRef [ab as ab]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.Id as a.Id, b.Id as b.Id]
    PhysicalHashJoin [FullOuter] [build: b.Id] [probe: a.Id]
      PhysicalSchemaScan [#A.entities() as a]
      PhysicalSchemaScan [#B.entities() as b]
  PhysicalProject [a.Id as a.Id, b.Id as b.Id]
    PhysicalCteRef [ab as ab]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: BasicEntity]
      Id: int <- property Id
    SourceEntity [b: BasicEntity]
      Id: int <- property Id
    Generated [ResultRow0]
      a.Id: int? <- field a_Id
      b.Id: int? <- field b_Id

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [a: BasicEntity] -> aRows
    SourceScan [b: BasicEntity] -> bRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    MaterializeChunked [bRows -> bRowsBuffer]
    CreateHash [bHash: int? -> IndexedHashJoinRow<BasicEntity>; capacity: bRowsBuffer.Count]
    CreateBooleanArray [bHashBuildMatched <- bRowsBuffer.Count]
    ForEachIndexed [bIndex, b in bRowsBuffer]
      Let [bIndexed: IndexedHashJoinRow<BasicEntity> = IndexedHashRow(b, bIndex)]
      HashAdd [bHash[b.Id] += bIndexed]
    ChunkedForEach [a in aRows]
      HashProbe [bHash[a.Id] -> bHashMatches] [match: bHashHasMatch]
        ForEach [bIndexed in bHashMatches]
          Let [b: BasicEntity = bIndexed.Row]
          Let [bIndex: int = bIndexed.Index]
          Assign [bHashHasMatch = TRUE]
          ArrayAssign [bHashBuildMatched[bIndex] = TRUE]
          AppendShape [result <- ResultShape0(a.Id: a.Id, b.Id: b.Id)]
      HashProbeNoMatch
        AppendShape [result <- ResultShape0(a.Id: a.Id, b.Id: NULL)]
    ForEachIndexed [bIndex, b in bRowsBuffer]
      If [NOT bHashBuildMatched[bIndex]]
        AppendShape [result <- ResultShape0(a.Id: NULL, b.Id: b.Id)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q283_SpecCoreFullOuterJoinRowPresence
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("a.Id", typeof(int?), 0),
            new Column("b.Id", typeof(int?), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 18) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.a_Id, __musoqShapeRow.b_Id);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    var __aSchema = provider.GetSchema("#A");
                    var aRowsSource = __aSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                    var aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(aRowsSource.Chunks, __musoqProgressContext, "a:1") : aRowsSource.Chunks;
                    var __bSchema = provider.GetSchema("#B");
                    var bRowsSource = __bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                    var bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(bRowsSource.Chunks, __musoqProgressContext, "b:1") : bRowsSource.Chunks;
                    var bRowsBuffer = EvaluationHelper.MaterializeChunkedRows(bRows);
                    var bHash = new Dictionary<int?, HashJoinBucket<Musoq.Evaluator.Helpers.IndexedHashJoinRow<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>>>(bRowsBuffer.Count);
                    bool[] bHashBuildMatched = new bool[bRowsBuffer.Count];
                    for (int bIndex = 0; bIndex < bRowsBuffer.Count; ++bIndex)
                    {
                        if ((bIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        Musoq.Evaluator.Tests.Schema.Basic.BasicEntity b = bRowsBuffer[bIndex];
                        Musoq.Evaluator.Helpers.IndexedHashJoinRow<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> bIndexed = new Musoq.Evaluator.Helpers.IndexedHashJoinRow<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(b, bIndex);
                        int? key = b.Id;
                        if (key == null)
                            continue;
                        {
                            ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(bHash, key, out var matchesExists);
                            if (!matchesExists)
                            {
                                matches = new HashJoinBucket<Musoq.Evaluator.Helpers.IndexedHashJoinRow<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>>(bIndexed);
                            }
                            else
                            {
                                matches.Add(bIndexed);
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
                                    bool bHashHasMatch = false;
                                    int? key = a.Id;
                                    if (key != null && bHash.TryGetValue(key, out var bHashMatches))
                                    {
                                        foreach (var bIndexed in bHashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            Musoq.Evaluator.Tests.Schema.Basic.BasicEntity b = bIndexed.Row;
                                            int bIndex = bIndexed.Index;
                                            bHashHasMatch = true;
                                            bHashBuildMatched[bIndex] = (bool)true;
                                            __musoqFinalShapeRows.Add(new ResultShape0(a.Id, b.Id));
                                        }
                                    }

                                    if (!bHashHasMatch)
                                    {
                                        __musoqFinalShapeRows.Add(new ResultShape0(a.Id, null));
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
                                    bool bHashHasMatch = false;
                                    int? key = a.Id;
                                    if (key != null && bHash.TryGetValue(key, out var bHashMatches))
                                    {
                                        foreach (var bIndexed in bHashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            Musoq.Evaluator.Tests.Schema.Basic.BasicEntity b = bIndexed.Row;
                                            int bIndex = bIndexed.Index;
                                            bHashHasMatch = true;
                                            bHashBuildMatched[bIndex] = (bool)true;
                                            __musoqFinalShapeRows.Add(new ResultShape0(a.Id, b.Id));
                                        }
                                    }

                                    if (!bHashHasMatch)
                                    {
                                        __musoqFinalShapeRows.Add(new ResultShape0(a.Id, null));
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
                            bool bHashHasMatch = false;
                            int? key = a.Id;
                            if (key != null && bHash.TryGetValue(key, out var bHashMatches))
                            {
                                foreach (var bIndexed in bHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    Musoq.Evaluator.Tests.Schema.Basic.BasicEntity b = bIndexed.Row;
                                    int bIndex = bIndexed.Index;
                                    bHashHasMatch = true;
                                    bHashBuildMatched[bIndex] = (bool)true;
                                    __musoqFinalShapeRows.Add(new ResultShape0(a.Id, b.Id));
                                }
                            }

                            if (!bHashHasMatch)
                            {
                                __musoqFinalShapeRows.Add(new ResultShape0(a.Id, null));
                            }
                        }
                    }

                    for (int bIndex = 0; bIndex < bRowsBuffer.Count; ++bIndex)
                    {
                        if ((bIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        Musoq.Evaluator.Tests.Schema.Basic.BasicEntity b = bRowsBuffer[bIndex];
                        if ((!(bool)SafeArrayAccess.GetIndexedElement(bHashBuildMatched, bIndex, typeof(bool))))
                        {
                            __musoqFinalShapeRows.Add(new ResultShape0(null, b.Id));
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
            public ResultRow0(int? __value0, int? __value1)
            {
                a_Id = __value0;
                b_Id = __value1;
            }

            public override int Count => 2;
            public int? a_Id { get; private set; }
            public int? b_Id { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        a_Id = (int?)value;
                        break;
                    case 1:
                        b_Id = (int?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "a.Id" => true,
                "a_Id" => true,
                "b.Id" => true,
                "b_Id" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)a_Id,
                1 => (object)b_Id,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "a.Id" => (object)a_Id,
                "a_Id" => (object)a_Id,
                "b.Id" => (object)b_Id,
                "b_Id" => (object)b_Id,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int? a_Id, int? b_Id)
            {
                this.a_Id = a_Id;
                this.b_Id = b_Id;
            }

            public int? a_Id { get; }
            public int? b_Id { get; }
        }
    }
}
