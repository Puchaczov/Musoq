// === Parsed Query ===
/*
with names as (select Id, Name from #A.entities()), cities as (select Id, City from #A.entities()), countries as (select Id, Country from #A.entities()) select b.Name, n.Name, c.City, co.Country from #B.entities() b inner join names n on b.Id = n.Id inner join cities c on b.Id = c.Id inner join countries co on b.Id = co.Id
*/

// === Logical Plan ===
/*
Cte
  Definition [names]
    MultiStatement
      Project [ko3iko.Id as Id, ko3iko.Name as Name]
        SchemaScan [#A.entities() as ko3iko]
  Definition [cities]
    MultiStatement
      Project [vo04qt.Id as Id, vo04qt.City as City]
        SchemaScan [#A.entities() as vo04qt]
  Definition [countries]
    MultiStatement
      Project [gougbq.Id as Id, gougbq.Country as Country]
        SchemaScan [#A.entities() as gougbq]
  Query
    MultiStatement
      Project [b.Name as b.Name, b.Id as b.Id, n.Id as n.Id, n.Name as n.Name]
        Join [Inner] [(b.Id = n.Id)]
          SchemaScan [#B.entities() as b]
          CteRef [names as n]
      Project [bn.b.Name as b.Name, bn.b.Id as b.Id, bn.n.Id as n.Id, bn.n.Name as n.Name, c.Id as c.Id, c.City as c.City]
        Join [Inner] [(bn.b.Id = c.Id)]
          CteRef [bn as bn]
          CteRef [cities as c]
      Project [bnc.b.Name as b.Name, bnc.b.Id as b.Id, bnc.n.Id as n.Id, bnc.n.Name as n.Name, bnc.c.Id as c.Id, bnc.c.City as c.City, co.Id as Id, co.Country as Country]
        Join [Inner] [(bnc.b.Id = co.Id)]
          CteRef [bnc as bnc]
          CteRef [countries as co]
      Project [b.Name as b.Name, n.Name as n.Name, c.City as c.City, co.Country as co.Country]
        CteRef [bncco as bncco]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [names]
    PhysicalMultiStatement
      PhysicalProject [ko3iko.Id as Id, ko3iko.Name as Name]
        PhysicalSchemaScan [#A.entities() as ko3iko]
  Definition [cities]
    PhysicalMultiStatement
      PhysicalProject [vo04qt.Id as Id, vo04qt.City as City]
        PhysicalSchemaScan [#A.entities() as vo04qt]
  Definition [countries]
    PhysicalMultiStatement
      PhysicalProject [gougbq.Id as Id, gougbq.Country as Country]
        PhysicalSchemaScan [#A.entities() as gougbq]
  Query
    PhysicalMultiStatement
      PhysicalProject [b.Name as b.Name, b.Id as b.Id, n.Id as n.Id, n.Name as n.Name]
        PhysicalHashJoin [Inner] [build: n.Id] [probe: b.Id]
          PhysicalSchemaScan [#B.entities() as b]
          PhysicalCteRef [names as n]
      PhysicalProject [bn.b.Name as b.Name, bn.b.Id as b.Id, bn.n.Id as n.Id, bn.n.Name as n.Name, c.Id as c.Id, c.City as c.City]
        PhysicalHashJoin [Inner] [build: c.Id] [probe: bn.b.Id]
          PhysicalCteRef [bn as bn]
          PhysicalCteRef [cities as c]
      PhysicalProject [bnc.b.Name as b.Name, bnc.b.Id as b.Id, bnc.n.Id as n.Id, bnc.n.Name as n.Name, bnc.c.Id as c.Id, bnc.c.City as c.City, co.Id as Id, co.Country as Country]
        PhysicalHashJoin [Inner] [build: co.Id] [probe: bnc.b.Id]
          PhysicalCteRef [bnc as bnc]
          PhysicalCteRef [countries as co]
      PhysicalProject [b.Name as b.Name, n.Name as n.Name, c.City as c.City, co.Country as co.Country]
        PhysicalCteRef [bncco as bncco]
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
    SourceEntity [vo04qt: BasicEntity]
      City: string <- property City
      Id: int <- property Id
    HashPayload [Cte1HashPayload1]
      Id: int <- field Id
      City: string <- field City
    SourceEntity [gougbq: BasicEntity]
      Country: string <- property Country
      Id: int <- property Id
    HashPayload [Cte2HashPayload2]
      Id: int <- field Id
      Country: string <- field Country
    SourceEntity [b: BasicEntity]
      Name: string <- property Name
      Id: int <- property Id
    HashPayload [Cte0HashPayload0]
      Id: int <- field Id
      Name: string <- field Name
    TableRow [n]
      Id: int <- field Id
      Name: string <- field Name
    HashPayload [Cte1HashPayload1]
      Id: int <- field Id
      City: string <- field City
    TableRow [c]
      Id: int <- field Id
      City: string <- field City
    HashPayload [Cte2HashPayload2]
      Id: int <- field Id
      Country: string <- field Country
    TableRow [co]
      Id: int <- field Id
      Country: string <- field Country
    Generated [ResultRow0]
      b.Name: string <- field b_Name
      n.Name: string <- field n_Name
      c.City: string <- field c_City
      co.Country: string <- field co_Country

  Body
    ParallelBlock [cte-level-0, tasks 3, maxDegree 3]
      ParallelTask [names -> __parallelCteLevel0Task0Result]
        SourceScan [ko3iko: BasicEntity] -> cte0_ko3ikoRows
        CreateHash [cte0HashSidecar0Id: int -> Row]
        ChunkedForEach [ko3iko in cte0_ko3ikoRows]
          CreateHashPayload [cte0SidecarPayload0 <- Cte0HashPayload0(Id: ko3iko.Id, Name: ko3iko.Name)]
          HashAdd [cte0HashSidecar0Id[ko3iko.Id] += cte0SidecarPayload0]
        StoreCteIndex [cte0HashSidecar0Id -> _cteIndexResults.Slot0 Hash]
      ParallelTask [cities -> __parallelCteLevel0Task1Result]
        SourceScan [vo04qt: BasicEntity] -> cte1_vo04qtRows
        CreateHash [cte1HashSidecar1Id: int -> Row]
        ChunkedForEach [vo04qt in cte1_vo04qtRows]
          CreateHashPayload [cte1SidecarPayload0 <- Cte1HashPayload1(Id: vo04qt.Id, City: vo04qt.City)]
          HashAdd [cte1HashSidecar1Id[vo04qt.Id] += cte1SidecarPayload0]
        StoreCteIndex [cte1HashSidecar1Id -> _cteIndexResults.Slot1 Hash]
      ParallelTask [countries -> __parallelCteLevel0Task2Result]
        SourceScan [gougbq: BasicEntity] -> cte2_gougbqRows
        CreateHash [cte2HashSidecar2Id: int -> Row]
        ChunkedForEach [gougbq in cte2_gougbqRows]
          CreateHashPayload [cte2SidecarPayload0 <- Cte2HashPayload2(Id: gougbq.Id, Country: gougbq.Country)]
          HashAdd [cte2HashSidecar2Id[gougbq.Id] += cte2SidecarPayload0]
        StoreCteIndex [cte2HashSidecar2Id -> _cteIndexResults.Slot2 Hash]
      ParallelMerge
    SourceScan [b: BasicEntity] -> bRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    LoadCteIndex [bnNHash <- _cteIndexResults.Slot0 Hash: int]
    LoadCteIndex [bncCHash <- _cteIndexResults.Slot1 Hash: int]
    LoadCteIndex [bnccoCoHash <- _cteIndexResults.Slot2 Hash: int]
    ChunkedForEach [b in bRows]
      HashProbe [bnNHash[b.Id] -> bnNHashMatches]
        ForEach [n in bnNHashMatches]
          HashProbe [bncCHash[b.Id] -> bncCHashMatches]
            ForEach [c in bncCHashMatches]
              HashProbe [bnccoCoHash[b.Id] -> bnccoCoHashMatches]
                ForEach [co in bnccoCoHashMatches]
                  AppendShape [result <- ResultShape0(b.Name: b.Name, n.Name: n.Name, c.City: c.City, co.Country: co.Country)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q148_CteSidecarFanoutThreeHashes
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_3 = new Column[]
        {
            new Column("b.Name", typeof(string), 0),
            new Column("n.Name", typeof(string), 1),
            new Column("c.City", typeof(string), 2),
            new Column("co.Country", typeof(string), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_gougbq_2 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Country", typeof(string), 12), new Column("Id", typeof(int), 18) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("Id", typeof(int), 18) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_vo04qt_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 11), new Column("Id", typeof(int), 18) });
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
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.b_Name, __musoqShapeRow.n_Name, __musoqShapeRow.c_City, __musoqShapeRow.co_Country);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var _cteIndexResults = new CteIndexResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                object __parallelCteLevel0Task0Result = null;
                object __parallelCteLevel0Task1Result = null;
                object __parallelCteLevel0Task2Result = null;
                var cteLevel0Runner = new CteLevel0Runner(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, OnPhaseChanged, _cteIndexResults);
                Parallel.Invoke(new ParallelOptions() { CancellationToken = token, MaxDegreeOfParallelism = 3 }, cteLevel0Runner.RunCteLevel0Task0, cteLevel0Runner.RunCteLevel0Task1, cteLevel0Runner.RunCteLevel0Task2);
                token.ThrowIfCancellationRequested();
                __parallelCteLevel0Task0Result = cteLevel0Runner.Task0Result;
                __parallelCteLevel0Task1Result = cteLevel0Runner.Task1Result;
                __parallelCteLevel0Task2Result = cteLevel0Runner.Task2Result;
                var __bSchema = provider.GetSchema("#B");
                var bRowsSource = __bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("b:4", sourceExecutionPlans["b:4"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["b:4"], logger, OnDataSourceProgress), Array.Empty<object>());
                var bRows = bRowsSource.Chunks;
                var bnNHash = _cteIndexResults.Slot0;
                var bncCHash = _cteIndexResults.Slot1;
                var bnccoCoHash = _cteIndexResults.Slot2;
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
                                                int bnccoCoHashKey = b.Id;
                                                if (bnccoCoHash.TryGetValue(bnccoCoHashKey, out var bnccoCoHashMatches))
                                                {
                                                    foreach (var co in bnccoCoHashMatches)
                                                    {
                                                        token.ThrowIfCancellationRequested();
                                                        __musoqFinalShapeRows.Add(new ResultShape0(b.Name, n.Name, c.City, co.Country));
                                                    }
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
                                                int bnccoCoHashKey = b.Id;
                                                if (bnccoCoHash.TryGetValue(bnccoCoHashKey, out var bnccoCoHashMatches))
                                                {
                                                    foreach (var co in bnccoCoHashMatches)
                                                    {
                                                        token.ThrowIfCancellationRequested();
                                                        __musoqFinalShapeRows.Add(new ResultShape0(b.Name, n.Name, c.City, co.Country));
                                                    }
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
                                        int bnccoCoHashKey = b.Id;
                                        if (bnccoCoHash.TryGetValue(bnccoCoHashKey, out var bnccoCoHashMatches))
                                        {
                                            foreach (var co in bnccoCoHashMatches)
                                            {
                                                token.ThrowIfCancellationRequested();
                                                __musoqFinalShapeRows.Add(new ResultShape0(b.Name, n.Name, c.City, co.Country));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
                OnPhaseChanged("compiled", QueryPhase.End);
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
        private static object BuildCteLevel0Task0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Action<string, QueryPhase> OnPhaseChanged, CteIndexResults _cteIndexResults)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                object __parallelCteLevel0Task0Result = null;
                token.ThrowIfCancellationRequested();
                var __cte0_ko3ikoSchema = provider.GetSchema("#A");
                var cte0_ko3ikoRowsSource = __cte0_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte0_ko3ikoRows = cte0_ko3ikoRowsSource.Chunks;
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
                return __parallelCteLevel0Task0Result;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static object BuildCteLevel0Task1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Action<string, QueryPhase> OnPhaseChanged, CteIndexResults _cteIndexResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                object __parallelCteLevel0Task1Result = null;
                token.ThrowIfCancellationRequested();
                var __cte1_vo04qtSchema = provider.GetSchema("#A");
                var cte1_vo04qtRowsSource = __cte1_vo04qtSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("vo04qt:2", sourceExecutionPlans["vo04qt:2"], token, __schemaColumns_compiled_vo04qt_1, sourceRuntimeSettingsBySourceContextId["vo04qt:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte1_vo04qtRows = cte1_vo04qtRowsSource.Chunks;
                var cte1HashSidecar1Id = new Dictionary<int, HashJoinBucket<Cte1HashPayload1>>();
                foreach (var vo04qtChunk in cte1_vo04qtRows)
                {
                    if (vo04qtChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> vo04qtChunkView)
                    {
                        if (vo04qtChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] vo04qtChunkViewArray)
                        {
                            int vo04qtChunkViewOffset = vo04qtChunkView.Offset;
                            for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunkView.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                            {
                                if ((vo04qtIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var vo04qt = vo04qtChunkViewArray[vo04qtChunkViewOffset + vo04qtIndex];
                                Cte1HashPayload1 cte1SidecarPayload0 = new Cte1HashPayload1(vo04qt.Id, vo04qt.City);
                                int cte1HashSidecar1IdKey0 = vo04qt.Id;
                                {
                                    ref var cte1HashSidecar1IdBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1HashSidecar1Id, cte1HashSidecar1IdKey0, out var cte1HashSidecar1IdBucket0Exists);
                                    if (!cte1HashSidecar1IdBucket0Exists)
                                    {
                                        cte1HashSidecar1IdBucket0 = new HashJoinBucket<Cte1HashPayload1>(cte1SidecarPayload0);
                                    }
                                    else
                                    {
                                        cte1HashSidecar1IdBucket0.Add(cte1SidecarPayload0);
                                    }
                                }
                            }

                            continue;
                        }

                        if (vo04qtChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> vo04qtChunkViewList)
                        {
                            int vo04qtChunkViewOffset = vo04qtChunkView.Offset;
                            for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunkView.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                            {
                                if ((vo04qtIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var vo04qt = vo04qtChunkViewList[vo04qtChunkViewOffset + vo04qtIndex];
                                Cte1HashPayload1 cte1SidecarPayload0 = new Cte1HashPayload1(vo04qt.Id, vo04qt.City);
                                int cte1HashSidecar1IdKey0 = vo04qt.Id;
                                {
                                    ref var cte1HashSidecar1IdBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1HashSidecar1Id, cte1HashSidecar1IdKey0, out var cte1HashSidecar1IdBucket0Exists);
                                    if (!cte1HashSidecar1IdBucket0Exists)
                                    {
                                        cte1HashSidecar1IdBucket0 = new HashJoinBucket<Cte1HashPayload1>(cte1SidecarPayload0);
                                    }
                                    else
                                    {
                                        cte1HashSidecar1IdBucket0.Add(cte1SidecarPayload0);
                                    }
                                }
                            }

                            continue;
                        }
                    }

                    for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunk.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                    {
                        if ((vo04qtIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var vo04qt = vo04qtChunk[vo04qtIndex];
                        Cte1HashPayload1 cte1SidecarPayload0 = new Cte1HashPayload1(vo04qt.Id, vo04qt.City);
                        int cte1HashSidecar1IdKey0 = vo04qt.Id;
                        {
                            ref var cte1HashSidecar1IdBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1HashSidecar1Id, cte1HashSidecar1IdKey0, out var cte1HashSidecar1IdBucket0Exists);
                            if (!cte1HashSidecar1IdBucket0Exists)
                            {
                                cte1HashSidecar1IdBucket0 = new HashJoinBucket<Cte1HashPayload1>(cte1SidecarPayload0);
                            }
                            else
                            {
                                cte1HashSidecar1IdBucket0.Add(cte1SidecarPayload0);
                            }
                        }
                    }
                }

                _cteIndexResults.Slot1 = cte1HashSidecar1Id;
                return __parallelCteLevel0Task1Result;
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static object BuildCteLevel0Task2(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Action<string, QueryPhase> OnPhaseChanged, CteIndexResults _cteIndexResults)
        {
            OnPhaseChanged("compiled:cte2", QueryPhase.Begin);
            try
            {
                object __parallelCteLevel0Task2Result = null;
                token.ThrowIfCancellationRequested();
                var __cte2_gougbqSchema = provider.GetSchema("#A");
                var cte2_gougbqRowsSource = __cte2_gougbqSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("gougbq:3", sourceExecutionPlans["gougbq:3"], token, __schemaColumns_compiled_gougbq_2, sourceRuntimeSettingsBySourceContextId["gougbq:3"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte2_gougbqRows = cte2_gougbqRowsSource.Chunks;
                var cte2HashSidecar2Id = new Dictionary<int, HashJoinBucket<Cte2HashPayload2>>();
                foreach (var gougbqChunk in cte2_gougbqRows)
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
                                Cte2HashPayload2 cte2SidecarPayload0 = new Cte2HashPayload2(gougbq.Id, gougbq.Country);
                                int cte2HashSidecar2IdKey0 = gougbq.Id;
                                {
                                    ref var cte2HashSidecar2IdBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte2HashSidecar2Id, cte2HashSidecar2IdKey0, out var cte2HashSidecar2IdBucket0Exists);
                                    if (!cte2HashSidecar2IdBucket0Exists)
                                    {
                                        cte2HashSidecar2IdBucket0 = new HashJoinBucket<Cte2HashPayload2>(cte2SidecarPayload0);
                                    }
                                    else
                                    {
                                        cte2HashSidecar2IdBucket0.Add(cte2SidecarPayload0);
                                    }
                                }
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
                                Cte2HashPayload2 cte2SidecarPayload0 = new Cte2HashPayload2(gougbq.Id, gougbq.Country);
                                int cte2HashSidecar2IdKey0 = gougbq.Id;
                                {
                                    ref var cte2HashSidecar2IdBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte2HashSidecar2Id, cte2HashSidecar2IdKey0, out var cte2HashSidecar2IdBucket0Exists);
                                    if (!cte2HashSidecar2IdBucket0Exists)
                                    {
                                        cte2HashSidecar2IdBucket0 = new HashJoinBucket<Cte2HashPayload2>(cte2SidecarPayload0);
                                    }
                                    else
                                    {
                                        cte2HashSidecar2IdBucket0.Add(cte2SidecarPayload0);
                                    }
                                }
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
                        Cte2HashPayload2 cte2SidecarPayload0 = new Cte2HashPayload2(gougbq.Id, gougbq.Country);
                        int cte2HashSidecar2IdKey0 = gougbq.Id;
                        {
                            ref var cte2HashSidecar2IdBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte2HashSidecar2Id, cte2HashSidecar2IdKey0, out var cte2HashSidecar2IdBucket0Exists);
                            if (!cte2HashSidecar2IdBucket0Exists)
                            {
                                cte2HashSidecar2IdBucket0 = new HashJoinBucket<Cte2HashPayload2>(cte2SidecarPayload0);
                            }
                            else
                            {
                                cte2HashSidecar2IdBucket0.Add(cte2SidecarPayload0);
                            }
                        }
                    }
                }

                _cteIndexResults.Slot2 = cte2HashSidecar2Id;
                return __parallelCteLevel0Task2Result;
            }
            finally
            {
                OnPhaseChanged("compiled:cte2", QueryPhase.End);
            }
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

        private readonly struct Cte1HashPayload1
        {
            public readonly int Id;
            public readonly string City;
            public Cte1HashPayload1(int Id, string City)
            {
                this.Id = Id;
                this.City = City;
            }
        }

        private readonly struct Cte2HashPayload2
        {
            public readonly int Id;
            public readonly string Country;
            public Cte2HashPayload2(int Id, string Country)
            {
                this.Id = Id;
                this.Country = Country;
            }
        }

        private sealed class CteIndexResults
        {
            public Dictionary<int, HashJoinBucket<Cte0HashPayload0>> Slot0;
            public Dictionary<int, HashJoinBucket<Cte1HashPayload1>> Slot1;
            public Dictionary<int, HashJoinBucket<Cte2HashPayload2>> Slot2;
        }

        private sealed class CteLevel0Runner
        {
            private readonly CteIndexResults _cteIndexResults;
            private readonly Microsoft.Extensions.Logging.ILogger _logger;
            private readonly Musoq.Schema.DataSourceEventHandler _onDataSourceProgress;
            private readonly Action<string, QueryPhase> _onPhaseChanged;
            private readonly Musoq.Schema.ISchemaProvider _provider;
            private readonly IReadOnlyDictionary<string, SourceExecutionPlan> _sourceExecutionPlans;
            private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _sourceRuntimeSettingsBySourceContextId;
            private readonly CancellationToken _token;
            public CteLevel0Runner(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Action<string, QueryPhase> OnPhaseChanged, CteIndexResults _cteIndexResults)
            {
                _provider = provider;
                _sourceRuntimeSettingsBySourceContextId = sourceRuntimeSettingsBySourceContextId;
                _sourceExecutionPlans = sourceExecutionPlans;
                _logger = logger;
                _token = token;
                _onDataSourceProgress = OnDataSourceProgress;
                _onPhaseChanged = OnPhaseChanged;
                this._cteIndexResults = _cteIndexResults;
            }

            public object Task0Result { get; private set; }
            public object Task1Result { get; private set; }
            public object Task2Result { get; private set; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void RunCteLevel0Task0()
            {
                Task0Result = BuildCteLevel0Task0(_provider, _sourceRuntimeSettingsBySourceContextId, _sourceExecutionPlans, _logger, _token, _onDataSourceProgress, _onPhaseChanged, _cteIndexResults);
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void RunCteLevel0Task1()
            {
                Task1Result = BuildCteLevel0Task1(_provider, _sourceRuntimeSettingsBySourceContextId, _sourceExecutionPlans, _logger, _token, _onDataSourceProgress, _onPhaseChanged, _cteIndexResults);
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void RunCteLevel0Task2()
            {
                Task2Result = BuildCteLevel0Task2(_provider, _sourceRuntimeSettingsBySourceContextId, _sourceExecutionPlans, _logger, _token, _onDataSourceProgress, _onPhaseChanged, _cteIndexResults);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1, string __value2, string __value3)
            {
                b_Name = __value0;
                n_Name = __value1;
                c_City = __value2;
                co_Country = __value3;
            }

            public override int Count => 4;
            public string b_Name { get; private set; }
            public string c_City { get; private set; }
            public string co_Country { get; private set; }
            public string n_Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        b_Name = (string)value;
                        break;
                    case 1:
                        n_Name = (string)value;
                        break;
                    case 2:
                        c_City = (string)value;
                        break;
                    case 3:
                        co_Country = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "b.Name" => true,
                "b_Name" => true,
                "n.Name" => true,
                "n_Name" => true,
                "c.City" => true,
                "c_City" => true,
                "City" => true,
                "co.Country" => true,
                "co_Country" => true,
                "Country" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)b_Name,
                1 => (object)n_Name,
                2 => (object)c_City,
                3 => (object)co_Country,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "b.Name" => (object)b_Name,
                "b_Name" => (object)b_Name,
                "n.Name" => (object)n_Name,
                "n_Name" => (object)n_Name,
                "c.City" => (object)c_City,
                "c_City" => (object)c_City,
                "City" => (object)c_City,
                "co.Country" => (object)co_Country,
                "co_Country" => (object)co_Country,
                "Country" => (object)co_Country,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string b_Name, string n_Name, string c_City, string co_Country)
            {
                this.b_Name = b_Name;
                this.n_Name = n_Name;
                this.c_City = c_City;
                this.co_Country = co_Country;
            }

            public string b_Name { get; }
            public string c_City { get; }
            public string co_Country { get; }
            public string n_Name { get; }
        }
    }
}
