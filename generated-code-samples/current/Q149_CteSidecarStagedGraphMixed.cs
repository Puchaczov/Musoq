// === Parsed Query ===
/*
with raw as (select Id, Name, City, Country, Population from #A.entities()), names as (select Id, Name from raw), cities as (select Id, City from raw), eligible as (select Id from raw where Population > 0), joined as (select b.Id, n.Name, c.City from #B.entities() b inner join names n on b.Id = n.Id inner join cities c on b.Id = c.Id) select j.Id, j.Name, j.City from joined j semi join eligible e on j.Id = e.Id
*/

// === Logical Plan ===
/*
Cte
  Definition [raw]
    MultiStatement
      Project [ko3iko.Id as Id, ko3iko.Name as Name, ko3iko.City as City, ko3iko.Country as Country, ko3iko.Population as Population]
        SchemaScan [#A.entities() as ko3iko]
  Definition [names]
    MultiStatement
      Project [raw.Id as Id, raw.Name as Name]
        CteRef [raw as raw]
  Definition [cities]
    MultiStatement
      Project [raw.Id as Id, raw.City as City]
        CteRef [raw as raw]
  Definition [eligible]
    MultiStatement
      Project [raw.Id as Id]
        Filter [(raw.Population > 0)]
          CteRef [raw as raw]
  Definition [joined]
    MultiStatement
      Project [b.Id as b.Id, n.Id as n.Id, n.Name as n.Name]
        Join [Inner] [(b.Id = n.Id)]
          SchemaScan [#B.entities() as b]
          CteRef [names as n]
      Project [bn.b.Id as b.Id, bn.n.Id as n.Id, bn.n.Name as n.Name, c.Id as Id, c.City as City]
        Join [Inner] [(bn.b.Id = c.Id)]
          CteRef [bn as bn]
          CteRef [cities as c]
      Project [b.Id as b.Id, n.Name as n.Name, c.City as c.City]
        CteRef [bnc as bnc]
  Query
    MultiStatement
      Project [j.Id as j.Id, j.Name as j.Name, j.City as j.City]
        Join [LeftSemi] [(j.Id = e.Id)]
          CteRef [joined as j]
          CteRef [eligible as e]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [raw]
    PhysicalMultiStatement
      PhysicalProject [ko3iko.Id as Id, ko3iko.Name as Name, ko3iko.City as City, ko3iko.Country as Country, ko3iko.Population as Population]
        PhysicalSchemaScan [#A.entities() as ko3iko]
  Definition [names]
    PhysicalMultiStatement
      PhysicalProject [raw.Id as Id, raw.Name as Name]
        PhysicalCteRef [raw as raw]
  Definition [cities]
    PhysicalMultiStatement
      PhysicalProject [raw.Id as Id, raw.City as City]
        PhysicalCteRef [raw as raw]
  Definition [eligible]
    PhysicalMultiStatement
      PhysicalProject [raw.Id as Id]
        PhysicalFilter [(raw.Population > 0)]
          PhysicalCteRef [raw as raw]
  Definition [joined]
    PhysicalMultiStatement
      PhysicalProject [b.Id as b.Id, n.Id as n.Id, n.Name as n.Name]
        PhysicalHashJoin [Inner] [build: n.Id] [probe: b.Id]
          PhysicalSchemaScan [#B.entities() as b]
          PhysicalCteRef [names as n]
      PhysicalProject [bn.b.Id as b.Id, bn.n.Id as n.Id, bn.n.Name as n.Name, c.Id as Id, c.City as City]
        PhysicalHashJoin [Inner] [build: c.Id] [probe: bn.b.Id]
          PhysicalCteRef [bn as bn]
          PhysicalCteRef [cities as c]
      PhysicalProject [b.Id as b.Id, n.Name as n.Name, c.City as c.City]
        PhysicalCteRef [bnc as bnc]
  Query
    PhysicalMultiStatement
      PhysicalProject [j.Id as j.Id, j.Name as j.Name, j.City as j.City]
        PhysicalHashJoin [LeftSemi] [build: e.Id] [probe: j.Id]
          PhysicalCteRef [joined as j]
          PhysicalCteRef [eligible as e]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
      City: string <- property City
      Population: decimal <- property Population
      Id: int <- property Id
    TableRow [raw]
      Id: int <- field Id
      Name: string <- field Name
      City: string <- field City
      Population: decimal <- field Population
    HashPayload [Cte1HashPayload0]
      Id: int <- field Id
      Name: string <- field Name
    TableRow [raw]
      Id: int <- field Id
      Name: string <- field Name
      City: string <- field City
      Population: decimal <- field Population
    HashPayload [Cte2HashPayload1]
      Id: int <- field Id
      City: string <- field City
    TableRow [raw]
      Id: int <- field Id
      Name: string <- field Name
      City: string <- field City
      Population: decimal <- field Population
    SourceEntity [b: BasicEntity]
      Id: int <- property Id
    HashPayload [Cte1HashPayload0]
      Id: int <- field Id
      Name: string <- field Name
    TableRow [n]
      Id: int <- field Id
      Name: string <- field Name
    HashPayload [Cte2HashPayload1]
      Id: int <- field Id
      City: string <- field City
    TableRow [c]
      Id: int <- field Id
      City: string <- field City
    Generated [ResultRow0]
      j.Id: int <- field j_Id
      j.Name: string <- field j_Name
      j.City: string <- field j_City

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> cte0_ko3ikoRows
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [End:cte0]
    FusedCteProducer [cte1 -> sidecar-only, cte2 -> sidecar-only, cte3 -> sidecar-only]
      PhaseBoundary [Begin:cte1]
      PhaseBoundary [Begin:cte2]
      PhaseBoundary [Begin:cte3]
      CreateHash [cte1HashSidecar0Id: int -> Row]
      CreateHash [cte2HashSidecar1Id: int -> Row]
      CreateKeySet [cte3KeySetSidecar2Id: int]
      ChunkedForEach [ko3iko in cte0_ko3ikoRows]
        CreateHashPayload [cte1SidecarPayload0 <- Cte1HashPayload0(Id: ko3iko.Id, Name: ko3iko.Name)]
        HashAdd [cte1HashSidecar0Id[ko3iko.Id] += cte1SidecarPayload0]
        CreateHashPayload [cte2SidecarPayload0 <- Cte2HashPayload1(Id: ko3iko.Id, City: ko3iko.City)]
        HashAdd [cte2HashSidecar1Id[ko3iko.Id] += cte2SidecarPayload0]
        If [(ko3iko.Population > 0)]
          KeySetAdd [cte3KeySetSidecar2Id += ko3iko.Id]
      StoreCteIndex [cte1HashSidecar0Id -> _cteIndexResults.Slot0 Hash]
      StoreCteIndex [cte2HashSidecar1Id -> _cteIndexResults.Slot1 Hash]
      StoreCteIndex [cte3KeySetSidecar2Id -> _cteIndexResults.Slot2 KeySet]
      PhaseBoundary [End:cte3]
      PhaseBoundary [End:cte2]
      PhaseBoundary [End:cte1]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte4]
    SourceScan [b: BasicEntity] -> bRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    LoadCteIndex [bnNHash <- _cteIndexResults.Slot0 Hash: int]
    LoadCteIndex [bncCHash <- _cteIndexResults.Slot1 Hash: int]
    LoadCteIndex [eKeys <- _cteIndexResults.Slot2 KeySet: int]
    ChunkedForEach [b in bRows]
      KeySetProbe [eKeys[b.Id]]
        HashProbe [bnNHash[b.Id] -> bnNHashMatches]
          ForEach [n in bnNHashMatches]
            HashProbe [bncCHash[b.Id] -> bncCHashMatches]
              ForEach [c in bncCHashMatches]
                AppendShape [result <- ResultShape0(j.Id: b.Id, j.Name: n.Name, j.City: c.City)]
    PhaseBoundary [End:cte4]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q149_CteSidecarStagedGraphMixed
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("j.Id", typeof(int), 0),
            new Column("j.Name", typeof(string), 1),
            new Column("j.City", typeof(string), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 18) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("City", typeof(string), 11), new Column("Population", typeof(decimal), 13), new Column("Id", typeof(int), 18) });
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
                yield return new ResultRow0(__musoqShapeRow.j_Id, __musoqShapeRow.j_Name, __musoqShapeRow.j_City);
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
                var __cte0_ko3ikoSchema = provider.GetSchema("#A");
                var cte0_ko3ikoRowsSource = __cte0_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte0_ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : cte0_ko3ikoRowsSource.Chunks;
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
                }

                OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled:cte2", QueryPhase.Begin);
                    try
                    {
                        OnPhaseChanged("compiled:cte3", QueryPhase.Begin);
                        try
                        {
                            var cte1HashSidecar0Id = new Dictionary<int, HashJoinBucket<Cte1HashPayload0>>();
                            var cte2HashSidecar1Id = new Dictionary<int, HashJoinBucket<Cte2HashPayload1>>();
                            var cte3KeySetSidecar2Id = new HashSet<int>();
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
                                            Cte1HashPayload0 cte1SidecarPayload0 = new Cte1HashPayload0(ko3iko.Id, ko3iko.Name);
                                            int cte1HashSidecar0IdKey0 = ko3iko.Id;
                                            {
                                                ref var cte1HashSidecar0IdBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1HashSidecar0Id, cte1HashSidecar0IdKey0, out var cte1HashSidecar0IdBucket0Exists);
                                                if (!cte1HashSidecar0IdBucket0Exists)
                                                {
                                                    cte1HashSidecar0IdBucket0 = new HashJoinBucket<Cte1HashPayload0>(cte1SidecarPayload0);
                                                }
                                                else
                                                {
                                                    cte1HashSidecar0IdBucket0.Add(cte1SidecarPayload0);
                                                }
                                            }

                                            Cte2HashPayload1 cte2SidecarPayload0 = new Cte2HashPayload1(ko3iko.Id, ko3iko.City);
                                            int cte2HashSidecar1IdKey0 = ko3iko.Id;
                                            {
                                                ref var cte2HashSidecar1IdBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte2HashSidecar1Id, cte2HashSidecar1IdKey0, out var cte2HashSidecar1IdBucket0Exists);
                                                if (!cte2HashSidecar1IdBucket0Exists)
                                                {
                                                    cte2HashSidecar1IdBucket0 = new HashJoinBucket<Cte2HashPayload1>(cte2SidecarPayload0);
                                                }
                                                else
                                                {
                                                    cte2HashSidecar1IdBucket0.Add(cte2SidecarPayload0);
                                                }
                                            }

                                            if ((ko3iko.Population > 0))
                                            {
                                                int cte3KeySetSidecar2IdKey0 = ko3iko.Id;
                                                cte3KeySetSidecar2Id.Add(cte3KeySetSidecar2IdKey0);
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
                                            Cte1HashPayload0 cte1SidecarPayload0 = new Cte1HashPayload0(ko3iko.Id, ko3iko.Name);
                                            int cte1HashSidecar0IdKey0 = ko3iko.Id;
                                            {
                                                ref var cte1HashSidecar0IdBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1HashSidecar0Id, cte1HashSidecar0IdKey0, out var cte1HashSidecar0IdBucket0Exists);
                                                if (!cte1HashSidecar0IdBucket0Exists)
                                                {
                                                    cte1HashSidecar0IdBucket0 = new HashJoinBucket<Cte1HashPayload0>(cte1SidecarPayload0);
                                                }
                                                else
                                                {
                                                    cte1HashSidecar0IdBucket0.Add(cte1SidecarPayload0);
                                                }
                                            }

                                            Cte2HashPayload1 cte2SidecarPayload0 = new Cte2HashPayload1(ko3iko.Id, ko3iko.City);
                                            int cte2HashSidecar1IdKey0 = ko3iko.Id;
                                            {
                                                ref var cte2HashSidecar1IdBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte2HashSidecar1Id, cte2HashSidecar1IdKey0, out var cte2HashSidecar1IdBucket0Exists);
                                                if (!cte2HashSidecar1IdBucket0Exists)
                                                {
                                                    cte2HashSidecar1IdBucket0 = new HashJoinBucket<Cte2HashPayload1>(cte2SidecarPayload0);
                                                }
                                                else
                                                {
                                                    cte2HashSidecar1IdBucket0.Add(cte2SidecarPayload0);
                                                }
                                            }

                                            if ((ko3iko.Population > 0))
                                            {
                                                int cte3KeySetSidecar2IdKey0 = ko3iko.Id;
                                                cte3KeySetSidecar2Id.Add(cte3KeySetSidecar2IdKey0);
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
                                    Cte1HashPayload0 cte1SidecarPayload0 = new Cte1HashPayload0(ko3iko.Id, ko3iko.Name);
                                    int cte1HashSidecar0IdKey0 = ko3iko.Id;
                                    {
                                        ref var cte1HashSidecar0IdBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1HashSidecar0Id, cte1HashSidecar0IdKey0, out var cte1HashSidecar0IdBucket0Exists);
                                        if (!cte1HashSidecar0IdBucket0Exists)
                                        {
                                            cte1HashSidecar0IdBucket0 = new HashJoinBucket<Cte1HashPayload0>(cte1SidecarPayload0);
                                        }
                                        else
                                        {
                                            cte1HashSidecar0IdBucket0.Add(cte1SidecarPayload0);
                                        }
                                    }

                                    Cte2HashPayload1 cte2SidecarPayload0 = new Cte2HashPayload1(ko3iko.Id, ko3iko.City);
                                    int cte2HashSidecar1IdKey0 = ko3iko.Id;
                                    {
                                        ref var cte2HashSidecar1IdBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte2HashSidecar1Id, cte2HashSidecar1IdKey0, out var cte2HashSidecar1IdBucket0Exists);
                                        if (!cte2HashSidecar1IdBucket0Exists)
                                        {
                                            cte2HashSidecar1IdBucket0 = new HashJoinBucket<Cte2HashPayload1>(cte2SidecarPayload0);
                                        }
                                        else
                                        {
                                            cte2HashSidecar1IdBucket0.Add(cte2SidecarPayload0);
                                        }
                                    }

                                    if ((ko3iko.Population > 0))
                                    {
                                        int cte3KeySetSidecar2IdKey0 = ko3iko.Id;
                                        cte3KeySetSidecar2Id.Add(cte3KeySetSidecar2IdKey0);
                                    }
                                }
                            }

                            _cteIndexResults.Slot0 = cte1HashSidecar0Id;
                            _cteIndexResults.Slot1 = cte2HashSidecar1Id;
                            _cteIndexResults.Slot2 = cte3KeySetSidecar2Id;
                        }
                        finally
                        {
                            OnPhaseChanged("compiled:cte3", QueryPhase.End);
                        }
                    }
                    finally
                    {
                        OnPhaseChanged("compiled:cte2", QueryPhase.End);
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte1", QueryPhase.End);
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte4", QueryPhase.Begin);
                try
                {
                    var __bSchema = provider.GetSchema("#B");
                    var bRowsSource = __bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("b:5", sourceExecutionPlans["b:5"], token, __schemaColumns_compiled_b_1, sourceRuntimeSettingsBySourceContextId["b:5"], logger, OnDataSourceProgress), Array.Empty<object>());
                    var bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(bRowsSource.Chunks, __musoqProgressContext, "b:5") : bRowsSource.Chunks;
                    var bnNHash = _cteIndexResults.Slot0;
                    var bncCHash = _cteIndexResults.Slot1;
                    var eKeys = _cteIndexResults.Slot2;
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
                                    int eKeysKey = b.Id;
                                    if (eKeys.Contains(eKeysKey))
                                    {
                                        int bnNHashKey = b.Id;
                                        if (bnNHash.TryGetValue(bnNHashKey, out var bnNHashMatches))
                                        {
                                            foreach (var n in bnNHashMatches)
                                            {
                                                token.ThrowIfCancellationRequested();
                                                int bncCHashKey = b.Id;
                                                if (bncCHash.TryGetValue(bncCHashKey, out var bncCHashMatches))
                                                {
                                                    foreach (var c in bncCHashMatches)
                                                    {
                                                        token.ThrowIfCancellationRequested();
                                                        __musoqFinalShapeRows.Add(new ResultShape0(b.Id, n.Name, c.City));
                                                    }
                                                }
                                            }
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
                                    int eKeysKey = b.Id;
                                    if (eKeys.Contains(eKeysKey))
                                    {
                                        int bnNHashKey = b.Id;
                                        if (bnNHash.TryGetValue(bnNHashKey, out var bnNHashMatches))
                                        {
                                            foreach (var n in bnNHashMatches)
                                            {
                                                token.ThrowIfCancellationRequested();
                                                int bncCHashKey = b.Id;
                                                if (bncCHash.TryGetValue(bncCHashKey, out var bncCHashMatches))
                                                {
                                                    foreach (var c in bncCHashMatches)
                                                    {
                                                        token.ThrowIfCancellationRequested();
                                                        __musoqFinalShapeRows.Add(new ResultShape0(b.Id, n.Name, c.City));
                                                    }
                                                }
                                            }
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
                            int eKeysKey = b.Id;
                            if (eKeys.Contains(eKeysKey))
                            {
                                int bnNHashKey = b.Id;
                                if (bnNHash.TryGetValue(bnNHashKey, out var bnNHashMatches))
                                {
                                    foreach (var n in bnNHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        int bncCHashKey = b.Id;
                                        if (bncCHash.TryGetValue(bncCHashKey, out var bncCHashMatches))
                                        {
                                            foreach (var c in bncCHashMatches)
                                            {
                                                token.ThrowIfCancellationRequested();
                                                __musoqFinalShapeRows.Add(new ResultShape0(b.Id, n.Name, c.City));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte4", QueryPhase.End);
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

        private readonly struct Cte1HashPayload0
        {
            public readonly int Id;
            public readonly string Name;
            public Cte1HashPayload0(int Id, string Name)
            {
                this.Id = Id;
                this.Name = Name;
            }
        }

        private readonly struct Cte2HashPayload1
        {
            public readonly int Id;
            public readonly string City;
            public Cte2HashPayload1(int Id, string City)
            {
                this.Id = Id;
                this.City = City;
            }
        }

        private sealed class CteIndexResults
        {
            public Dictionary<int, HashJoinBucket<Cte1HashPayload0>> Slot0;
            public Dictionary<int, HashJoinBucket<Cte2HashPayload1>> Slot1;
            public HashSet<int> Slot2;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, string __value1, string __value2)
            {
                j_Id = __value0;
                j_Name = __value1;
                j_City = __value2;
            }

            public override int Count => 3;
            public string j_City { get; private set; }
            public int j_Id { get; private set; }
            public string j_Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        j_Id = (int)value;
                        break;
                    case 1:
                        j_Name = (string)value;
                        break;
                    case 2:
                        j_City = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "j.Id" => true,
                "j_Id" => true,
                "Id" => true,
                "j.Name" => true,
                "j_Name" => true,
                "Name" => true,
                "j.City" => true,
                "j_City" => true,
                "City" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)j_Id,
                1 => (object)j_Name,
                2 => (object)j_City,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "j.Id" => (object)j_Id,
                "j_Id" => (object)j_Id,
                "Id" => (object)j_Id,
                "j.Name" => (object)j_Name,
                "j_Name" => (object)j_Name,
                "Name" => (object)j_Name,
                "j.City" => (object)j_City,
                "j_City" => (object)j_City,
                "City" => (object)j_City,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int j_Id, string j_Name, string j_City)
            {
                this.j_Id = j_Id;
                this.j_Name = j_Name;
                this.j_City = j_City;
            }

            public string j_City { get; }
            public int j_Id { get; }
            public string j_Name { get; }
        }
    }
}
