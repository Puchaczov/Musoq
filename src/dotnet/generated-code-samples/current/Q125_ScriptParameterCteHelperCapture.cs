// === Parsed Query ===
/*
param(country: string)
              with filtered as (
                  select Name, City, $country as RequestedCountry
                  from #A.entities()
                  where Country = $country
              )
              select l.Name, r.RequestedCountry
              from filtered l
              inner join filtered r on l.Name = r.Name
*/

// === Logical Plan ===
/*
Cte
  Definition [filtered]
    MultiStatement
      Project [ko3iko.Name as Name, ko3iko.City as City, $country as RequestedCountry]
        Filter [(ko3iko.Country = $country)]
          SchemaScan [#A.entities() as ko3iko]
  Query
    MultiStatement
      Project [l.Name as l.Name, r.Name as r.Name, r.RequestedCountry as r.RequestedCountry]
        Join [Inner] [(l.Name = r.Name)]
          CteRef [filtered as l]
          CteRef [filtered as r]
      Project [l.Name as Name, r.RequestedCountry as RequestedCountry]
        CteRef [lr as lr]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [filtered]
    PhysicalMultiStatement
      PhysicalProject [ko3iko.Name as Name, $country as RequestedCountry]
        PhysicalFilter [(ko3iko.Country = $country)]
          PhysicalSchemaScan [#A.entities() as ko3iko] [pushdown: (ko3iko.Country = $country)]
  Query
    PhysicalMultiStatement
      PhysicalProject [l.Name as l.Name, r.Name as r.Name, r.RequestedCountry as r.RequestedCountry]
        PhysicalHashJoin [Inner] [build: r.Name] [probe: l.Name]
          PhysicalCteRef [filtered as l]
          PhysicalCteRef [filtered as r]
      PhysicalProject [l.Name as Name, r.RequestedCountry as RequestedCountry]
        PhysicalCteRef [lr as lr]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
      City: string <- property City
      Country: string <- property Country
    Generated [Cte0Row0]
      Name: string <- field Name
      RequestedCountry: string <- field RequestedCountry
    HashPayload [Cte0HashPayload0]
      Name: string <- field Name
      RequestedCountry: string <- field RequestedCountry
    TableRow [l]
      Name: string <- field Name
      RequestedCountry: string <- field RequestedCountry
    HashPayload [Cte0HashPayload0]
      Name: string <- field Name
      RequestedCountry: string <- field RequestedCountry
    TableRow [r]
      Name: string <- field Name
      RequestedCountry: string <- field RequestedCountry
    Generated [ResultRow0]
      Name: string <- field Name
      RequestedCountry: string <- field RequestedCountry

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    SourceScan [ko3iko: BasicEntity] -> cte0_ko3ikoRows
    CreateTable [cte0: Cte0Row0]
    CreateHash [cte0HashSidecar0Name: string -> Row]
    PhaseBoundary [Where:cte0]
    ChunkedForEach [ko3iko in cte0_ko3ikoRows]
      If [(ko3iko.Country = $country)]
        CreateGeneratedRow [cte0SidecarRow0 <- Cte0Row0(Name: ko3iko.Name, RequestedCountry: $country)]
        AppendExistingRow [cte0 <- cte0SidecarRow0]
        CreateHashPayload [cte0SidecarPayload0 <- Cte0HashPayload0(Name: ko3iko.Name, RequestedCountry: $country)]
        HashAdd [cte0HashSidecar0Name[ko3iko.Name] += cte0SidecarPayload0]
    StoreCteIndex [cte0HashSidecar0Name -> _cteIndexResults.Slot0 Hash]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    LoadCteIndex [rHash <- _cteIndexResults.Slot0 Hash: string]
    ForEach [l in _cteRowResults.Slot0]
      HashProbe [rHash[l.Name] -> rHashMatches]
        ForEach [r in rHashMatches]
          AppendShape [result <- ResultRow0(Name: l.Name, RequestedCountry: r.RequestedCountry)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q125_ScriptParameterCteHelperCapture
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
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("RequestedCountry", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("City", typeof(string), 1), new Column("Country", typeof(string), 2) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = new ScriptParameterContract[]
        {
            new ScriptParameterContract("country", "string", "string", typeof(string), false, false, null, null, false, ScriptParameterDefaultKind.None, null)
        };
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = new ScriptParameterDefinition[]
        {
            new ScriptParameterDefinition(new ScriptParameterContract("country", "string", "string", typeof(string), false, false, null, null, false, ScriptParameterDefaultKind.None, null))
        };
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_cte0_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                var _cteRowResults = new CteRowResults();
                var _cteIndexResults = new CteIndexResults();
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                var paramCountry = ScriptParameterBinder.GetRequired<string>(__musoqExecutionState, "country");
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, new string[] { "country" });
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults, _cteIndexResults, paramCountry);
                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
                try
                {
                    __musoqFinalShapeRows.EnsureCapacity(_cteRowResults.Slot0.Count);
                    var rHash = _cteIndexResults.Slot0;
                    var __storedTable0Rows = _cteRowResults.Slot0;
                    for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                    {
                        if ((__storedTable0Index & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        Cte0Row0 l = __storedTable0Rows[__storedTable0Index];
                        string key = l.Name;
                        if (key != null && rHash.TryGetValue(key, out var rHashMatches))
                        {
                            foreach (var r in rHashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                __musoqFinalShapeRows.Add(new ResultRow0(l.Name, r.RequestedCountry));
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults, string paramCountry)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                var __cte0_ko3ikoSchema = provider.GetSchema("#A");
                var cte0_ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__cte0_ko3ikoSchema, "entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var cte0_ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : cte0_ko3ikoRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                var cte0HashSidecar0Name = new Dictionary<string, HashJoinBucket<Cte0HashPayload0>>();
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
                                if ((Operators.SqlCompare<string, string>(ko3iko.Country, paramCountry, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight))) == true)
                                {
                                    Cte0Row0 cte0SidecarRow0 = new Cte0Row0(ko3iko.Name, paramCountry);
                                    cte0.Add(cte0SidecarRow0);
                                    Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(ko3iko.Name, paramCountry);
                                    string cte0HashSidecar0NameKey0 = ko3iko.Name;
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
                                if ((Operators.SqlCompare<string, string>(ko3iko.Country, paramCountry, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight))) == true)
                                {
                                    Cte0Row0 cte0SidecarRow0 = new Cte0Row0(ko3iko.Name, paramCountry);
                                    cte0.Add(cte0SidecarRow0);
                                    Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(ko3iko.Name, paramCountry);
                                    string cte0HashSidecar0NameKey0 = ko3iko.Name;
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
                        if ((Operators.SqlCompare<string, string>(ko3iko.Country, paramCountry, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight))) == true)
                        {
                            Cte0Row0 cte0SidecarRow0 = new Cte0Row0(ko3iko.Name, paramCountry);
                            cte0.Add(cte0SidecarRow0);
                            Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(ko3iko.Name, paramCountry);
                            string cte0HashSidecar0NameKey0 = ko3iko.Name;
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
                return cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        private readonly struct Cte0HashPayload0
        {
            public readonly string Name;
            public readonly string RequestedCountry;
            public Cte0HashPayload0(string Name, string RequestedCountry)
            {
                this.Name = Name;
                this.RequestedCountry = RequestedCountry;
            }
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(string __value0, string __value1)
            {
                Name = __value0;
                RequestedCountry = __value1;
            }

            public string Name { get; }
            public string RequestedCountry { get; }
        }

        private sealed class CteIndexResults
        {
            public Dictionary<string, HashJoinBucket<Cte0HashPayload0>> Slot0;
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                Name = __value0;
                RequestedCountry = __value1;
            }

            public override int Count => 2;
            public string Name { get; private set; }
            public string RequestedCountry { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.RequestedCountry = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.RequestedCountry),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "RequestedCountry" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.RequestedCountry, ref visitor);
            }
        }
    }
}
