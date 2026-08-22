// === Parsed Query ===
/*
let country: string = 'Poland'
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
      Project [l.Name as l.Name, r.RequestedCountry as r.RequestedCountry]
        CteRef [lr as lr]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [filtered]
    PhysicalMultiStatement
      PhysicalProject [ko3iko.Name as Name, ko3iko.City as City, $country as RequestedCountry]
        PhysicalFilter [(ko3iko.Country = $country)]
          PhysicalSchemaScan [#A.entities() as ko3iko] [pushdown: (ko3iko.Country = $country)]
  Query
    PhysicalMultiStatement
      PhysicalProject [l.Name as l.Name, r.Name as r.Name, r.RequestedCountry as r.RequestedCountry]
        PhysicalHashJoin [Inner] [build: r.Name] [probe: l.Name]
          PhysicalCteRef [filtered as l]
          PhysicalCteRef [filtered as r]
      PhysicalProject [l.Name as l.Name, r.RequestedCountry as r.RequestedCountry]
        PhysicalCteRef [lr as lr]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
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
      l.Name: string <- field l_Name
      r.RequestedCountry: string <- field r_RequestedCountry

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
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    LoadCteIndex [rHash <- _cteIndexResults.Slot0 Hash: string]
    ForEach [l in _cteRowResults.Slot0]
      HashProbe [rHash[l.Name] -> rHashMatches]
        ForEach [r in rHashMatches]
          AppendShape [result <- ResultShape0(l.Name: l.Name, r.RequestedCountry: r.RequestedCountry)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q135_ScriptVariableCteHelperCapture
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
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("RequestedCountry", typeof(string), 1)
        };
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("l.Name", typeof(string), 0),
            new Column("r.RequestedCountry", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("Country", typeof(string), 12) });
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
                yield return new ResultRow0(__musoqShapeRow.l_Name, __musoqShapeRow.r_RequestedCountry);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _cteRowResults = new CteRowResults();
                var _cteIndexResults = new CteIndexResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                const string letCountry = "Poland";
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults, _cteIndexResults, letCountry);
                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
                try
                {
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
                                __musoqFinalShapeRows.Add(new ResultShape0(l.Name, r.RequestedCountry));
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
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults, string letCountry)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                var __cte0_ko3ikoSchema = provider.GetSchema("#A");
                var cte0_ko3ikoRowsSource = __cte0_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
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
                                if ((ko3iko.Country == letCountry))
                                {
                                    Cte0Row0 cte0SidecarRow0 = new Cte0Row0(ko3iko.Name, letCountry);
                                    cte0.Add(cte0SidecarRow0);
                                    Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(ko3iko.Name, letCountry);
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
                                if ((ko3iko.Country == letCountry))
                                {
                                    Cte0Row0 cte0SidecarRow0 = new Cte0Row0(ko3iko.Name, letCountry);
                                    cte0.Add(cte0SidecarRow0);
                                    Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(ko3iko.Name, letCountry);
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
                        if ((ko3iko.Country == letCountry))
                        {
                            Cte0Row0 cte0SidecarRow0 = new Cte0Row0(ko3iko.Name, letCountry);
                            cte0.Add(cte0SidecarRow0);
                            Cte0HashPayload0 cte0SidecarPayload0 = new Cte0HashPayload0(ko3iko.Name, letCountry);
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
                l_Name = __value0;
                r_RequestedCountry = __value1;
            }

            public override int Count => 2;
            public string l_Name { get; private set; }
            public string r_RequestedCountry { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        l_Name = (string)value;
                        break;
                    case 1:
                        r_RequestedCountry = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "l.Name" => true,
                "l_Name" => true,
                "Name" => true,
                "r.RequestedCountry" => true,
                "r_RequestedCountry" => true,
                "RequestedCountry" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)l_Name,
                1 => (object)r_RequestedCountry,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "l.Name" => (object)l_Name,
                "l_Name" => (object)l_Name,
                "Name" => (object)l_Name,
                "r.RequestedCountry" => (object)r_RequestedCountry,
                "r_RequestedCountry" => (object)r_RequestedCountry,
                "RequestedCountry" => (object)r_RequestedCountry,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string l_Name, string r_RequestedCountry)
            {
                this.l_Name = l_Name;
                this.r_RequestedCountry = r_RequestedCountry;
            }

            public string l_Name { get; }
            public string r_RequestedCountry { get; }
        }
    }
}
