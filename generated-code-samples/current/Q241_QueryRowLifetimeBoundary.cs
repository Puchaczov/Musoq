// === Parsed Query ===
/*
select l.Id as LeftId, r.Id as RightId from #queryrowsample.rows() l inner join #queryrowsample.rows() r on l.Id = r.Id
*/

// === Logical Plan ===
/*
MultiStatement
  Project [l.Id as l.Id, r.Id as r.Id]
    Join [Inner] [(l.Id = r.Id)]
      SchemaScan [#queryrowsample.rows() as l]
      SchemaScan [#queryrowsample.rows() as r]
  Project [l.Id as LeftId, r.Id as RightId]
    CteRef [lr as lr]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [l.Id as l.Id, r.Id as r.Id]
    PhysicalHashJoin [Inner] [build: r.Id] [probe: l.Id]
      PhysicalSchemaScan [#queryrowsample.rows() as l] [query-row:SealedClass;lifetime=EscapesScan;shape=4D941F6DC39F3C2653416E1578297526ECBFC21554FF5DF542D95CB50BE5B018]
      PhysicalSchemaScan [#queryrowsample.rows() as r] [query-row:SealedClass;lifetime=EscapesScan;shape=4D941F6DC39F3C2653416E1578297526ECBFC21554FF5DF542D95CB50BE5B018]
  PhysicalProject [l.Id as LeftId, r.Id as RightId]
    PhysicalCteRef [lr as lr]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    Generated [QueryRow_4D941F6DC39F_C]
      Id: int <- field Field0
    Generated [QueryRow_4D941F6DC39F_C]
      Id: int <- field Field0
    Generated [ResultRow0]
      LeftId: int <- field LeftId
      RightId: int <- field RightId

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [l: object] -> lRows [query-row:SealedClass;lifetime=EscapesScan;shape=4D941F6DC39F3C2653416E1578297526ECBFC21554FF5DF542D95CB50BE5B018]
    SourceScan [r: object] -> rRows [query-row:SealedClass;lifetime=EscapesScan;shape=4D941F6DC39F3C2653416E1578297526ECBFC21554FF5DF542D95CB50BE5B018]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateHash [rHash: int -> object]
    ChunkedForEach [r in rRows]
      HashAdd [rHash[r.Id] += r]
    ChunkedForEach [l in lRows]
      HashProbe [rHash[l.Id] -> rHashMatches]
        ForEach [r in rHashMatches]
          AppendShape [result <- ResultShape0(LeftId: l.Id, RightId: r.Id)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q241_QueryRowLifetimeBoundary
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
            new Column("LeftId", typeof(int), 0),
            new Column("RightId", typeof(int), 1)
        };
        private static readonly QueryRowShape __queryRowShape_4D941F6DC39F = new QueryRowShape(new QueryRowField[] { new QueryRowField(0, 0, "Id", typeof(int), false) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_l_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0) });
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
                yield return new ResultRow0(__musoqShapeRow.LeftId, __musoqShapeRow.RightId);
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
                    var __lSchema = provider.GetSchema("#queryrowsample");
                    var __lSchemaQueryRows = __lSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#queryrowsample.rows' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape 4D941F6DC39F3C2653416E1578297526ECBFC21554FF5DF542D95CB50BE5B018).");
                    var lRowsSource = __lSchemaQueryRows.GetQueryScopedRowSource<QueryRow_4D941F6DC39F_C, QueryRowMaterializer_4D941F6DC39F_C>("rows", new QueryScopedRowSourceRequest(new SourceExecutionContext("l:1", sourceExecutionPlans["l:1"], token, __schemaColumns_compiled_l_0, sourceRuntimeSettingsBySourceContextId["l:1"], logger, OnDataSourceProgress), __queryRowShape_4D941F6DC39F), Array.Empty<object>());
                    var lRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_4D941F6DC39F_C>(lRowsSource.Chunks, __musoqProgressContext, "l:1") : lRowsSource.Chunks;
                    var __rSchema = provider.GetSchema("#queryrowsample");
                    var __rSchemaQueryRows = __rSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#queryrowsample.rows' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape 4D941F6DC39F3C2653416E1578297526ECBFC21554FF5DF542D95CB50BE5B018).");
                    var rRowsSource = __rSchemaQueryRows.GetQueryScopedRowSource<QueryRow_4D941F6DC39F_C, QueryRowMaterializer_4D941F6DC39F_C>("rows", new QueryScopedRowSourceRequest(new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_l_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), __queryRowShape_4D941F6DC39F), Array.Empty<object>());
                    var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_4D941F6DC39F_C>(rRowsSource.Chunks, __musoqProgressContext, "r:1") : rRowsSource.Chunks;
                    var rHash = new Dictionary<int, HashJoinBucket<QueryRow_4D941F6DC39F_C>>();
                    foreach (var rChunk in rRows)
                    {
                        if (rChunk is global::Musoq.Schema.DataSources.RowChunk<QueryRow_4D941F6DC39F_C> rChunkView)
                        {
                            if (rChunkView.Source is QueryRow_4D941F6DC39F_C[] rChunkViewArray)
                            {
                                int rChunkViewOffset = rChunkView.Offset;
                                for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                                {
                                    if ((rIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var r = rChunkViewArray[rChunkViewOffset + rIndex];
                                    int key = r.Field0;
                                    {
                                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(rHash, key, out var matchesExists);
                                        if (!matchesExists)
                                        {
                                            matches = new HashJoinBucket<QueryRow_4D941F6DC39F_C>(r);
                                        }
                                        else
                                        {
                                            matches.Add(r);
                                        }
                                    }
                                }

                                continue;
                            }

                            if (rChunkView.Source is List<QueryRow_4D941F6DC39F_C> rChunkViewList)
                            {
                                int rChunkViewOffset = rChunkView.Offset;
                                for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                                {
                                    if ((rIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var r = rChunkViewList[rChunkViewOffset + rIndex];
                                    int key = r.Field0;
                                    {
                                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(rHash, key, out var matchesExists);
                                        if (!matchesExists)
                                        {
                                            matches = new HashJoinBucket<QueryRow_4D941F6DC39F_C>(r);
                                        }
                                        else
                                        {
                                            matches.Add(r);
                                        }
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
                            int key = r.Field0;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(rHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<QueryRow_4D941F6DC39F_C>(r);
                                }
                                else
                                {
                                    matches.Add(r);
                                }
                            }
                        }
                    }

                    foreach (var lChunk in lRows)
                    {
                        if (lChunk is global::Musoq.Schema.DataSources.RowChunk<QueryRow_4D941F6DC39F_C> lChunkView)
                        {
                            if (lChunkView.Source is QueryRow_4D941F6DC39F_C[] lChunkViewArray)
                            {
                                int lChunkViewOffset = lChunkView.Offset;
                                for (int lIndex = 0, lIndexCount = lChunkView.Count; lIndex < lIndexCount; ++lIndex)
                                {
                                    if ((lIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var l = lChunkViewArray[lChunkViewOffset + lIndex];
                                    int key = l.Field0;
                                    if (rHash.TryGetValue(key, out var rHashMatches))
                                    {
                                        foreach (var r in rHashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            __musoqFinalShapeRows.Add(new ResultShape0(l.Field0, r.Field0));
                                        }
                                    }
                                }

                                continue;
                            }

                            if (lChunkView.Source is List<QueryRow_4D941F6DC39F_C> lChunkViewList)
                            {
                                int lChunkViewOffset = lChunkView.Offset;
                                for (int lIndex = 0, lIndexCount = lChunkView.Count; lIndex < lIndexCount; ++lIndex)
                                {
                                    if ((lIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var l = lChunkViewList[lChunkViewOffset + lIndex];
                                    int key = l.Field0;
                                    if (rHash.TryGetValue(key, out var rHashMatches))
                                    {
                                        foreach (var r in rHashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            __musoqFinalShapeRows.Add(new ResultShape0(l.Field0, r.Field0));
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
                            int key = l.Field0;
                            if (rHash.TryGetValue(key, out var rHashMatches))
                            {
                                foreach (var r in rHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    __musoqFinalShapeRows.Add(new ResultShape0(l.Field0, r.Field0));
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

        private readonly struct QueryRowMaterializer_4D941F6DC39F_C : IQueryRowMaterializer<QueryRow_4D941F6DC39F_C>
        {
            public static QueryRow_4D941F6DC39F_C Materialize<TReader>(scoped ref TReader reader)
                where TReader : IQuerySourceFieldReader, allows ref struct => new QueryRow_4D941F6DC39F_C(reader.Read<int>(0));
        }

        private sealed class QueryRow_4D941F6DC39F_C
        {
            public QueryRow_4D941F6DC39F_C(int __value0)
            {
                Field0 = __value0;
            }

            public int Field0 { get; }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, int __value1)
            {
                LeftId = __value0;
                RightId = __value1;
            }

            public override int Count => 2;
            public int LeftId { get; private set; }
            public int RightId { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        LeftId = (int)value;
                        break;
                    case 1:
                        RightId = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "LeftId" => true,
                "RightId" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)LeftId,
                1 => (object)RightId,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "LeftId" => (object)LeftId,
                "RightId" => (object)RightId,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int LeftId, int RightId)
            {
                this.LeftId = LeftId;
                this.RightId = RightId;
            }

            public int LeftId { get; }
            public int RightId { get; }
        }
    }
}
