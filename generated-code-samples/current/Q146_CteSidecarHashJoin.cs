// === Parsed Query ===
/*
with indexed as (select Id, Name from #A.entities()) select b.Name, i.Name from #B.entities() b inner join indexed i on b.Id = i.Id
*/

// === Logical Plan ===
/*
Cte
  Definition [indexed]
    MultiStatement
      Project [ko3iko.Id as Id, ko3iko.Name as Name]
        SchemaScan [#A.entities() as ko3iko]
  Query
    MultiStatement
      Project [b.Name as b.Name, b.Id as b.Id, i.Id as i.Id, i.Name as i.Name]
        Join [Inner] [(b.Id = i.Id)]
          SchemaScan [#B.entities() as b]
          CteRef [indexed as i]
      Project [b.Name as b.Name, i.Name as i.Name]
        CteRef [bi as bi]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [indexed]
    PhysicalMultiStatement
      PhysicalProject [ko3iko.Id as Id, ko3iko.Name as Name]
        PhysicalSchemaScan [#A.entities() as ko3iko]
  Query
    PhysicalMultiStatement
      PhysicalProject [b.Name as b.Name, b.Id as b.Id, i.Id as i.Id, i.Name as i.Name]
        PhysicalHashJoin [Inner] [build: i.Id] [probe: b.Id]
          PhysicalSchemaScan [#B.entities() as b]
          PhysicalCteRef [indexed as i]
      PhysicalProject [b.Name as b.Name, i.Name as i.Name]
        PhysicalCteRef [bi as bi]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
      Id: int <- property Id
    HashPayload [Cte0HashPayload0]
      Id: int <- field Id
      Name: string <- field Name
    SourceEntity [b: BasicEntity]
      Name: string <- property Name
      Id: int <- property Id
    HashPayload [Cte0HashPayload0]
      Id: int <- field Id
      Name: string <- field Name
    TableRow [i]
      Id: int <- field Id
      Name: string <- field Name
    Generated [ResultRow0]
      b.Name: string <- field b_Name
      i.Name: string <- field i_Name

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    SourceScan [ko3iko: BasicEntity] -> cte0_ko3ikoRows
    CreateHash [cte0HashSidecar0Id: int -> Row]
    ChunkedForEach [ko3iko in cte0_ko3ikoRows]
      CreateHashPayload [cte0SidecarPayload0 <- Cte0HashPayload0(Id: ko3iko.Id, Name: ko3iko.Name)]
      HashAdd [cte0HashSidecar0Id[ko3iko.Id] += cte0SidecarPayload0]
    StoreCteIndex [cte0HashSidecar0Id -> _cteIndexResults.Slot0 Hash]
    PhaseBoundary [Select:cte0]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    SourceScan [b: BasicEntity] -> bRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    LoadCteIndex [iHash <- _cteIndexResults.Slot0 Hash: int]
    ChunkedForEach [b in bRows]
      HashProbe [iHash[b.Id] -> iHashMatches]
        ForEach [i in iHashMatches]
          AppendShape [result <- ResultShape0(b.Name: b.Name, i.Name: i.Name)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q146_CteSidecarHashJoin
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
            new Column("b.Name", typeof(string), 0),
            new Column("i.Name", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("Id", typeof(int), 18) });
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
                yield return new ResultRow0(__musoqShapeRow.b_Name, __musoqShapeRow.i_Name);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _cteIndexResults = new CteIndexResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.From);
                    var __cte0_ko3ikoSchema = provider.GetSchema("#A");
                    var cte0_ko3ikoRowsSource = __cte0_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                    var cte0_ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : cte0_ko3ikoRowsSource.Chunks;
                    var cte0HashSidecar0Id = new Dictionary<int, HashJoinBucket<Cte0HashPayload0>>();
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
                                    Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(ko3iko.Id, ko3iko.Name);
                                    int cte0HashSidecar0IdKey0 = ko3iko.Id;
                                    {
                                        ref var cte0HashSidecar0IdBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0HashSidecar0Id, cte0HashSidecar0IdKey0, out var cte0HashSidecar0IdBucket0Exists);
                                        if (!cte0HashSidecar0IdBucket0Exists)
                                        {
                                            cte0HashSidecar0IdBucket0 = new HashJoinBucket<Cte0HashPayload0>(cte0SidecarPayload0);
                                        }
                                        else
                                        {
                                            cte0HashSidecar0IdBucket0.Add(cte0SidecarPayload0);
                                        }
                                    }
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
                                    Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(ko3iko.Id, ko3iko.Name);
                                    int cte0HashSidecar0IdKey0 = ko3iko.Id;
                                    {
                                        ref var cte0HashSidecar0IdBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0HashSidecar0Id, cte0HashSidecar0IdKey0, out var cte0HashSidecar0IdBucket0Exists);
                                        if (!cte0HashSidecar0IdBucket0Exists)
                                        {
                                            cte0HashSidecar0IdBucket0 = new HashJoinBucket<Cte0HashPayload0>(cte0SidecarPayload0);
                                        }
                                        else
                                        {
                                            cte0HashSidecar0IdBucket0.Add(cte0SidecarPayload0);
                                        }
                                    }
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
                            Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(ko3iko.Id, ko3iko.Name);
                            int cte0HashSidecar0IdKey0 = ko3iko.Id;
                            {
                                ref var cte0HashSidecar0IdBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0HashSidecar0Id, cte0HashSidecar0IdKey0, out var cte0HashSidecar0IdBucket0Exists);
                                if (!cte0HashSidecar0IdBucket0Exists)
                                {
                                    cte0HashSidecar0IdBucket0 = new HashJoinBucket<Cte0HashPayload0>(cte0SidecarPayload0);
                                }
                                else
                                {
                                    cte0HashSidecar0IdBucket0.Add(cte0SidecarPayload0);
                                }
                            }
                        }
                    }

                    _cteIndexResults.Slot0 = cte0HashSidecar0Id;
                    OnPhaseChanged("compiled:cte0", QueryPhase.Select);
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
                try
                {
                    var __bSchema = provider.GetSchema("#B");
                    var bRowsSource = __bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("b:2", sourceExecutionPlans["b:2"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["b:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                    var bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(bRowsSource.Chunks, __musoqProgressContext, "b:2") : bRowsSource.Chunks;
                    var iHash = _cteIndexResults.Slot0;
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
                                    if (iHash.TryGetValue(key, out var iHashMatches))
                                    {
                                        foreach (var i in iHashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            __musoqFinalShapeRows.Add(new ResultShape0(b.Name, i.Name));
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
                                    if (iHash.TryGetValue(key, out var iHashMatches))
                                    {
                                        foreach (var i in iHashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            __musoqFinalShapeRows.Add(new ResultShape0(b.Name, i.Name));
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
                            if (iHash.TryGetValue(key, out var iHashMatches))
                            {
                                foreach (var i in iHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    __musoqFinalShapeRows.Add(new ResultShape0(b.Name, i.Name));
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

        private readonly struct Cte0HashPayload0
        {
            public readonly int Id;
            public readonly string Name;
            public Cte0HashPayload0(int Id, string Name)
            {
                this.Id = Id;
                this.Name = Name;
            }
        }

        private sealed class CteIndexResults
        {
            public Dictionary<int, HashJoinBucket<Cte0HashPayload0>> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                b_Name = __value0;
                i_Name = __value1;
            }

            public override int Count => 2;
            public string b_Name { get; private set; }
            public string i_Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        b_Name = (string)value;
                        break;
                    case 1:
                        i_Name = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "b.Name" => true,
                "b_Name" => true,
                "i.Name" => true,
                "i_Name" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)b_Name,
                1 => (object)i_Name,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "b.Name" => (object)b_Name,
                "b_Name" => (object)b_Name,
                "i.Name" => (object)i_Name,
                "i_Name" => (object)i_Name,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string b_Name, string i_Name)
            {
                this.b_Name = b_Name;
                this.i_Name = i_Name;
            }

            public string b_Name { get; }
            public string i_Name { get; }
        }
    }
}
