// === Parsed Query ===
/*
with p as (select Name as Name from #A.entities()), q as (select Name as Name from #B.entities()) select p.Name, q.Name from p inner join q on p.Name = q.Name
*/

// === Logical Plan ===
/*
Cte
  Definition [p]
    MultiStatement
      Project [ko3iko.Name as Name]
        SchemaScan [#A.entities() as ko3iko]
  Definition [q]
    MultiStatement
      Project [vo04qt.Name as Name]
        SchemaScan [#B.entities() as vo04qt]
  Query
    MultiStatement
      Project [p.Name as p.Name, q.Name as q.Name]
        Join [Inner] [(p.Name = q.Name)]
          CteRef [p as p]
          CteRef [q as q]
      Project [p.Name as p.Name, q.Name as q.Name]
        CteRef [pq as pq]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [p]
    PhysicalMultiStatement
      PhysicalProject [ko3iko.Name as Name]
        PhysicalSchemaScan [#A.entities() as ko3iko]
  Definition [q]
    PhysicalMultiStatement
      PhysicalProject [vo04qt.Name as Name]
        PhysicalSchemaScan [#B.entities() as vo04qt]
  Query
    PhysicalMultiStatement
      PhysicalProject [p.Name as p.Name, q.Name as q.Name]
        PhysicalHashJoin [Inner] [build: q.Name] [probe: p.Name]
          PhysicalCteRef [p as p]
          PhysicalCteRef [q as q]
      PhysicalProject [p.Name as p.Name, q.Name as q.Name]
        PhysicalCteRef [pq as pq]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
    Generated [Cte0Row0]
      Name: string <- field Name
    SourceEntity [vo04qt: BasicEntity]
      Name: string <- property Name
    HashPayload [Cte1HashPayload0]
      Name: string <- field Name
    TableRow [p]
      Name: string <- field Name
    HashPayload [Cte1HashPayload0]
      Name: string <- field Name
    TableRow [q]
      Name: string <- field Name
    Generated [ResultRow0]
      p.Name: string <- field p_Name
      q.Name: string <- field q_Name

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    ParallelBlock [cte-level-0, tasks 2, maxDegree 2]
      ParallelTask [p -> __parallelCteLevel0Task0Result]
        PhaseBoundary [Begin:cte0]
        PhaseBoundary [From:cte0]
        SourceScan [ko3iko: BasicEntity] -> cte0_ko3ikoRows
        CreateTable [cte0: Cte0Row0]
        PhaseBoundary [Select:cte0]
        ChunkedForEach [ko3iko in cte0_ko3ikoRows]
          AppendRow [cte0 <- Cte0Row0(Name: ko3iko.Name)]
        Assign [__parallelCteLevel0Task0Result = cte0]
        PhaseBoundary [End:cte0]
      ParallelTask [q -> __parallelCteLevel0Task1Result]
        PhaseBoundary [Begin:cte1]
        PhaseBoundary [From:cte1]
        SourceScan [vo04qt: BasicEntity] -> cte1_vo04qtRows
        CreateHash [cte1HashSidecar0Name: string -> Row]
        ChunkedForEach [vo04qt in cte1_vo04qtRows]
          CreateHashPayload [cte1SidecarPayload0 <- Cte1HashPayload0(Name: vo04qt.Name)]
          HashAdd [cte1HashSidecar0Name[vo04qt.Name] += cte1SidecarPayload0]
        StoreCteIndex [cte1HashSidecar0Name -> _cteIndexResults.Slot0 Hash]
        PhaseBoundary [Select:cte1]
        PhaseBoundary [End:cte1]
      ParallelMerge
        StoreTable [__parallelCteLevel0Task0Result -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte2]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    LoadCteIndex [qHash <- _cteIndexResults.Slot0 Hash: string]
    ForEach [p in _cteRowResults.Slot0]
      HashProbe [qHash[p.Name] -> qHashMatches]
        ForEach [q in qHashMatches]
          AppendShape [result <- ResultShape0(p.Name: p.Name, q.Name: q.Name)]
    PhaseBoundary [End:cte2]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q82_ParallelIndependentCtes
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
            new Column("Name", typeof(string), 0)
        };
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("p.Name", typeof(string), 0),
            new Column("q.Name", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10) });
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
                yield return new ResultRow0(__musoqShapeRow.p_Name, __musoqShapeRow.q_Name);
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
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                List<Cte0Row0> __parallelCteLevel0Task0Result = null;
                object __parallelCteLevel0Task1Result = null;
                var cteLevel0Runner = new CteLevel0Runner(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults, _cteIndexResults);
                Parallel.Invoke(new ParallelOptions() { CancellationToken = token, MaxDegreeOfParallelism = 2 }, cteLevel0Runner.RunCteLevel0Task0, cteLevel0Runner.RunCteLevel0Task1);
                token.ThrowIfCancellationRequested();
                __parallelCteLevel0Task0Result = cteLevel0Runner.Task0Result;
                __parallelCteLevel0Task1Result = cteLevel0Runner.Task1Result;
                _cteRowResults.Slot0 = __parallelCteLevel0Task0Result;
                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte2", QueryPhase.Begin);
                try
                {
                    var qHash = _cteIndexResults.Slot0;
                    var __storedTable0Rows = _cteRowResults.Slot0;
                    for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                    {
                        if ((__storedTable0Index & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        Cte0Row0 p = __storedTable0Rows[__storedTable0Index];
                        string key = p.Name;
                        if (key != null && qHash.TryGetValue(key, out var qHashMatches))
                        {
                            foreach (var q in qHashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                __musoqFinalShapeRows.Add(new ResultShape0(p.Name, q.Name));
                            }
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte2", QueryPhase.End);
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
        private static List<Cte0Row0> BuildCteLevel0Task0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults)
        {
            List<Cte0Row0> __parallelCteLevel0Task0Result = null;
            token.ThrowIfCancellationRequested();
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            List<Cte0Row0> cte0 = null!;
            try
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.From);
                var __cte0_ko3ikoSchema = provider.GetSchema("#A");
                var cte0_ko3ikoRowsSource = __cte0_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte0_ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : cte0_ko3ikoRowsSource.Chunks;
                cte0 = new List<Cte0Row0>();
                OnPhaseChanged("compiled:cte0", QueryPhase.Select);
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
                                cte0.Add(new Cte0Row0(ko3iko.Name));
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
                                cte0.Add(new Cte0Row0(ko3iko.Name));
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
                        cte0.Add(new Cte0Row0(ko3iko.Name));
                    }
                }

                __parallelCteLevel0Task0Result = cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }

            return __parallelCteLevel0Task0Result;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static object BuildCteLevel0Task1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults)
        {
            object __parallelCteLevel0Task1Result = null;
            token.ThrowIfCancellationRequested();
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.From);
                var __cte1_vo04qtSchema = provider.GetSchema("#B");
                var cte1_vo04qtRowsSource = __cte1_vo04qtSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("vo04qt:2", sourceExecutionPlans["vo04qt:2"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["vo04qt:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte1_vo04qtRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte1_vo04qtRowsSource.Chunks, __musoqProgressContext, "vo04qt:2") : cte1_vo04qtRowsSource.Chunks;
                var cte1HashSidecar0Name = new Dictionary<string, HashJoinBucket<Cte1HashPayload0>>();
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
                                Cte1HashPayload0 cte1SidecarPayload0 = new Cte1HashPayload0(vo04qt.Name);
                                string cte1HashSidecar0NameKey0 = vo04qt.Name;
                                if (cte1HashSidecar0NameKey0 != null)
                                {
                                    {
                                        ref var cte1HashSidecar0NameBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1HashSidecar0Name, cte1HashSidecar0NameKey0, out var cte1HashSidecar0NameBucket0Exists);
                                        if (!cte1HashSidecar0NameBucket0Exists)
                                        {
                                            cte1HashSidecar0NameBucket0 = new HashJoinBucket<Cte1HashPayload0>(cte1SidecarPayload0);
                                        }
                                        else
                                        {
                                            cte1HashSidecar0NameBucket0.Add(cte1SidecarPayload0);
                                        }
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
                                Cte1HashPayload0 cte1SidecarPayload0 = new Cte1HashPayload0(vo04qt.Name);
                                string cte1HashSidecar0NameKey0 = vo04qt.Name;
                                if (cte1HashSidecar0NameKey0 != null)
                                {
                                    {
                                        ref var cte1HashSidecar0NameBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1HashSidecar0Name, cte1HashSidecar0NameKey0, out var cte1HashSidecar0NameBucket0Exists);
                                        if (!cte1HashSidecar0NameBucket0Exists)
                                        {
                                            cte1HashSidecar0NameBucket0 = new HashJoinBucket<Cte1HashPayload0>(cte1SidecarPayload0);
                                        }
                                        else
                                        {
                                            cte1HashSidecar0NameBucket0.Add(cte1SidecarPayload0);
                                        }
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
                        Cte1HashPayload0 cte1SidecarPayload0 = new Cte1HashPayload0(vo04qt.Name);
                        string cte1HashSidecar0NameKey0 = vo04qt.Name;
                        if (cte1HashSidecar0NameKey0 != null)
                        {
                            {
                                ref var cte1HashSidecar0NameBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1HashSidecar0Name, cte1HashSidecar0NameKey0, out var cte1HashSidecar0NameBucket0Exists);
                                if (!cte1HashSidecar0NameBucket0Exists)
                                {
                                    cte1HashSidecar0NameBucket0 = new HashJoinBucket<Cte1HashPayload0>(cte1SidecarPayload0);
                                }
                                else
                                {
                                    cte1HashSidecar0NameBucket0.Add(cte1SidecarPayload0);
                                }
                            }
                        }
                    }
                }

                _cteIndexResults.Slot0 = cte1HashSidecar0Name;
                OnPhaseChanged("compiled:cte1", QueryPhase.Select);
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }

            return __parallelCteLevel0Task1Result;
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(string __value0)
            {
                Name = __value0;
            }

            public string Name { get; }
        }

        private readonly struct Cte1HashPayload0
        {
            public readonly string Name;
            public Cte1HashPayload0(string Name)
            {
                this.Name = Name;
            }
        }

        private sealed class CteIndexResults
        {
            public Dictionary<string, HashJoinBucket<Cte1HashPayload0>> Slot0;
        }

        private sealed class CteLevel0Runner
        {
            private readonly CteIndexResults _cteIndexResults;
            private readonly CteRowResults _cteRowResults;
            private readonly Microsoft.Extensions.Logging.ILogger _logger;
            private readonly QueryRunContext? _musoqProgressContext;
            private readonly Musoq.Schema.DataSourceEventHandler _onDataSourceProgress;
            private readonly Action<string, QueryPhase> _onPhaseChanged;
            private readonly Musoq.Evaluator.QueryProgressEventHandler _onQueryProgress;
            private readonly Musoq.Schema.ISchemaProvider _provider;
            private readonly IReadOnlyDictionary<string, SourceExecutionPlan> _sourceExecutionPlans;
            private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _sourceRuntimeSettingsBySourceContextId;
            private readonly CancellationToken _token;
            public CteLevel0Runner(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults)
            {
                _provider = provider;
                _sourceRuntimeSettingsBySourceContextId = sourceRuntimeSettingsBySourceContextId;
                _sourceExecutionPlans = sourceExecutionPlans;
                _logger = logger;
                _token = token;
                _musoqProgressContext = __musoqProgressContext;
                _onDataSourceProgress = OnDataSourceProgress;
                _onQueryProgress = OnQueryProgress;
                _onPhaseChanged = OnPhaseChanged;
                this._cteRowResults = _cteRowResults;
                this._cteIndexResults = _cteIndexResults;
            }

            public List<Cte0Row0> Task0Result { get; private set; }
            public object Task1Result { get; private set; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void RunCteLevel0Task0()
            {
                Task0Result = BuildCteLevel0Task0(_provider, _sourceRuntimeSettingsBySourceContextId, _sourceExecutionPlans, _logger, _token, _musoqProgressContext, _onDataSourceProgress, _onQueryProgress, _onPhaseChanged, _cteRowResults, _cteIndexResults);
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void RunCteLevel0Task1()
            {
                Task1Result = BuildCteLevel0Task1(_provider, _sourceRuntimeSettingsBySourceContextId, _sourceExecutionPlans, _logger, _token, _musoqProgressContext, _onDataSourceProgress, _onQueryProgress, _onPhaseChanged, _cteRowResults, _cteIndexResults);
            }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                p_Name = __value0;
                q_Name = __value1;
            }

            public override int Count => 2;
            public string p_Name { get; private set; }
            public string q_Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        p_Name = (string)value;
                        break;
                    case 1:
                        q_Name = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "p.Name" => true,
                "p_Name" => true,
                "q.Name" => true,
                "q_Name" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)p_Name,
                1 => (object)q_Name,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "p.Name" => (object)p_Name,
                "p_Name" => (object)p_Name,
                "q.Name" => (object)q_Name,
                "q_Name" => (object)q_Name,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string p_Name, string q_Name)
            {
                this.p_Name = p_Name;
                this.q_Name = q_Name;
            }

            public string p_Name { get; }
            public string q_Name { get; }
        }
    }
}
