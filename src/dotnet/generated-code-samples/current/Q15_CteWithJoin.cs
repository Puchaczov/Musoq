// === Parsed Query ===
/*
with cte as (select Name, City, Population from #A.entities() where Population > 0) select c.Name, a.Country from cte c inner join #A.entities() a on c.Name = a.Name
*/

// === Logical Plan ===
/*
Cte
  Definition [cte]
    MultiStatement
      Project [ko3iko.Name as Name, ko3iko.City as City, ko3iko.Population as Population]
        Filter [(ko3iko.Population > 0)]
          SchemaScan [#A.entities() as ko3iko]
  Query
    MultiStatement
      Project [c.Name as c.Name, a.Name as a.Name, a.Country as a.Country]
        Join [Inner] [(c.Name = a.Name)]
          CteRef [cte as c]
          SchemaScan [#A.entities() as a]
      Project [c.Name as Name, a.Country as Country]
        CteRef [ca as ca]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [cte]
    PhysicalMultiStatement
      PhysicalProject [ko3iko.Name as Name]
        PhysicalFilter [(ko3iko.Population > 0)]
          PhysicalSchemaScan [#A.entities() as ko3iko] [pushdown: (ko3iko.Population > 0)]
  Query
    PhysicalMultiStatement
      PhysicalProject [c.Name as c.Name, a.Name as a.Name, a.Country as a.Country]
        PhysicalHashJoin [Inner] [build: c.Name] [probe: a.Name]
          PhysicalCteRef [cte as c]
          PhysicalSchemaScan [#A.entities() as a]
      PhysicalProject [c.Name as Name, a.Country as Country]
        PhysicalCteRef [ca as ca]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
      City: string <- property City
      Population: decimal <- property Population
    HashPayload [Cte0HashPayload0]
      Name: string <- field Name
    HashPayload [Cte0HashPayload0]
      Name: string <- field Name
    TableRow [c]
      Name: string <- field Name
    SourceEntity [a: BasicEntity]
      Name: string <- property Name
      Country: string <- property Country
    Generated [ResultRow0]
      Name: string <- field Name
      Country: string <- field Country

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    SourceScan [ko3iko: BasicEntity] -> cte0_ko3ikoRows
    CreateHash [cte0HashSidecar0Name: string -> Row]
    PhaseBoundary [Where:cte0]
    ChunkedForEach [ko3iko in cte0_ko3ikoRows]
      If [(ko3iko.Population > 0)]
        Let [name: string = ko3iko.Name]
        CreateHashPayload [cte0SidecarPayload0 <- Cte0HashPayload0(Name: name)]
        HashAdd [cte0HashSidecar0Name[name] += cte0SidecarPayload0]
    StoreCteIndex [cte0HashSidecar0Name -> _cteIndexResults.Slot0 Hash]
    PhaseBoundary [Select:cte0]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    SourceScan [a: BasicEntity] -> aRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    LoadCteIndex [cHash <- _cteIndexResults.Slot0 Hash: string]
    ChunkedForEach [a in aRows]
      HashProbe [cHash[a.Name] -> cHashMatches]
        ForEach [c in cHashMatches]
          AppendShape [result <- ResultRow0(Name: c.Name, Country: a.Country)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q15_CteWithJoin
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
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("Country", typeof(string), 1) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("City", typeof(string), 1), new Column("Population", typeof(decimal), 2) });
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
                var _cteIndexResults = new CteIndexResults();
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.From);
                    var __cte0_ko3ikoSchema = provider.GetSchema("#A");
                    var cte0_ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__cte0_ko3ikoSchema, "entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                    var cte0_ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : cte0_ko3ikoRowsSource.Chunks;
                    var cte0HashSidecar0Name = new Dictionary<string, HashJoinBucket<Cte0HashPayload0>>();
                    OnPhaseChanged("compiled:cte0", QueryPhase.Where);
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
                                    if ((ko3iko.Population > 0))
                                    {
                                        string name = ko3iko.Name;
                                        Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(name);
                                        string cte0HashSidecar0NameKey0 = name;
                                        if (cte0HashSidecar0NameKey0 != null)
                                        {
                                            {
                                                ref var cte0HashSidecar0NameBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0HashSidecar0Name, cte0HashSidecar0NameKey0, out var cte0HashSidecar0NameBucket0Exists);
                                                if (!cte0HashSidecar0NameBucket0Exists)
                                                {
                                                    cte0HashSidecar0NameBucket0 = new HashJoinBucket<Cte0HashPayload0>(cte0SidecarPayload0);
                                                }
                                                else
                                                {
                                                    cte0HashSidecar0NameBucket0.Add(cte0SidecarPayload0);
                                                }
                                            }
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
                                    if ((ko3iko.Population > 0))
                                    {
                                        string name = ko3iko.Name;
                                        Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(name);
                                        string cte0HashSidecar0NameKey0 = name;
                                        if (cte0HashSidecar0NameKey0 != null)
                                        {
                                            {
                                                ref var cte0HashSidecar0NameBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0HashSidecar0Name, cte0HashSidecar0NameKey0, out var cte0HashSidecar0NameBucket0Exists);
                                                if (!cte0HashSidecar0NameBucket0Exists)
                                                {
                                                    cte0HashSidecar0NameBucket0 = new HashJoinBucket<Cte0HashPayload0>(cte0SidecarPayload0);
                                                }
                                                else
                                                {
                                                    cte0HashSidecar0NameBucket0.Add(cte0SidecarPayload0);
                                                }
                                            }
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
                            if ((ko3iko.Population > 0))
                            {
                                string name = ko3iko.Name;
                                Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(name);
                                string cte0HashSidecar0NameKey0 = name;
                                if (cte0HashSidecar0NameKey0 != null)
                                {
                                    {
                                        ref var cte0HashSidecar0NameBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0HashSidecar0Name, cte0HashSidecar0NameKey0, out var cte0HashSidecar0NameBucket0Exists);
                                        if (!cte0HashSidecar0NameBucket0Exists)
                                        {
                                            cte0HashSidecar0NameBucket0 = new HashJoinBucket<Cte0HashPayload0>(cte0SidecarPayload0);
                                        }
                                        else
                                        {
                                            cte0HashSidecar0NameBucket0.Add(cte0SidecarPayload0);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    _cteIndexResults.Slot0 = cte0HashSidecar0Name;
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
                    var __aSchema = provider.GetSchema("#A");
                    var aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__aSchema, "entities", new SourceExecutionContext("a:2", sourceExecutionPlans["a:2"], token, __schemaColumns_compiled_a_1, sourceRuntimeSettingsBySourceContextId["a:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                    var aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(aRowsSource.Chunks, __musoqProgressContext, "a:2") : aRowsSource.Chunks;
                    var cHash = _cteIndexResults.Slot0;
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
                                    string key = a.Name;
                                    if (key != null && cHash.TryGetValue(key, out var cHashMatches))
                                    {
                                        foreach (var c in cHashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            __musoqFinalShapeRows.Add(new ResultRow0(c.Name, a.Country));
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
                                    string key = a.Name;
                                    if (key != null && cHash.TryGetValue(key, out var cHashMatches))
                                    {
                                        foreach (var c in cHashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            __musoqFinalShapeRows.Add(new ResultRow0(c.Name, a.Country));
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
                            string key = a.Name;
                            if (key != null && cHash.TryGetValue(key, out var cHashMatches))
                            {
                                foreach (var c in cHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    __musoqFinalShapeRows.Add(new ResultRow0(c.Name, a.Country));
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
            public readonly string Name;
            public Cte0HashPayload0(string Name)
            {
                this.Name = Name;
            }
        }

        private sealed class CteIndexResults
        {
            public Dictionary<string, HashJoinBucket<Cte0HashPayload0>> Slot0;
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
