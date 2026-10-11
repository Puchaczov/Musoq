// === Parsed Query ===
/*
select a.Id as Id from #schema.first() a left join #schema.first() b on b.Id = a.Id + 1 where b.Label contains ('match', null) order by a.Id
*/

// === Logical Plan ===
/*
MultiStatement
  Project [a.Id as a.Id, b.Id as b.Id, b.Label as b.Label]
    Join [LeftOuter] [(b.Id = (a.Id + 1))]
      SchemaScan [#schema.first() as a]
      SchemaScan [#schema.first() as b]
  Sort [a.Id]
    Project [a.Id as Id]
      Filter [b.Label IN ('match', NULL)]
        CteRef [ab as ab]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.Id as a.Id, b.Id as b.Id, b.Label as b.Label]
    PhysicalHashJoin [LeftOuter] [build: b.Id] [probe: (a.Id + 1)]
      PhysicalSchemaScan [#schema.first() as a]
      PhysicalSchemaScan [#schema.first() as b]
  PhysicalSort [a.Id]
    PhysicalProject [a.Id as Id]
      PhysicalFilter [b.Label IN ('match', NULL)]
        PhysicalCteRef [ab as ab]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: OuterMembershipRow]
      Id: int <- property Id
    SourceEntity [b: OuterMembershipRow]
      Id: int <- property Id
      Label: string <- property Label
    Generated [Statement0Row0]
      a.Id: int <- field a_Id
      b.Id: int? <- field b_Id
      b.Label: string <- field b_Label
    TableRow [ab]
      a.Id: int <- field a_Id
      b.Id: int? <- field b_Id
      b.Label: string <- field b_Label
    Generated [ResultRow0]
      Id: int <- field Id

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [a: OuterMembershipRow] -> statement0_aRows
    SourceScan [b: OuterMembershipRow] -> statement0_bRows
    CreateTable [statement0: Statement0Row0]
    CreateHash [statement0BHash: int? -> OuterMembershipRow]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [b in statement0_bRows]
      HashAdd [statement0BHash[b.Id] += b]
    ChunkedForEach [a in statement0_aRows]
      HashProbe [statement0BHash[(a.Id + 1)] -> statement0BHashMatches] [match: statement0BHashHasMatch]
        ForEach [b in statement0BHashMatches]
          Assign [statement0BHashHasMatch = TRUE]
          AppendRow [statement0 <- Statement0Row0(a.Id: a.Id, b.Id: b.Id, b.Label: b.Label)]
      HashProbeNoMatch
        AppendRow [statement0 <- Statement0Row0(a.Id: a.Id, b.Id: NULL, b.Label: NULL)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [ab in _cteRowResults.Slot0]
      If [ab.b.Label IN ('match', NULL)]
        AppendShape [result <- ResultShape0(Id: ab.a.Id)]
    SortShapeRows [result -> resultSorted by Id ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P96_NativeOuterJoinConstantMembership_Full
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Musoq.Schema;
    using Musoq.Schema.Diagnostics;
    using Musoq.Schema.Optimization;
    using Musoq.Evaluator;
    using Musoq.Evaluator.Diagnostics;
    using Musoq.Evaluator.Tables;
    using Musoq.Evaluator.Helpers;
    using Musoq.Evaluator.Runtime;
    using Musoq.Schema.DataSources;
    using System.Linq;

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable, IProfiledRunnable
    {
        private static readonly Column[] __columns_compiled_result_3 = new Column[]
        {
            new Column("Id", typeof(int), 0)
        };
        private static readonly Column[] __columns_compiled_statement0_2 = new Column[]
        {
            new Column("a.Id", typeof(int), 0),
            new Column("b.Id", typeof(int?), 1),
            new Column("b.Label", typeof(string), 2)
        };
        private static readonly string[] __inSet_compiled_0 = new string[]
        {
            "match",
            null
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Label", typeof(string), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            ArgumentNullException.ThrowIfNull(profileRecorder);
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0_Profiled(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken, profileRecorder), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Id);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.Id);
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
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Where);
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var result = new List<ResultShape0>();
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 ab = __storedTable0Rows[__storedTable0Index];
                    if ((Array.IndexOf(__inSet_compiled_0, ab.b_Label) >= 0))
                    {
                        result.Add(new ResultShape0(ab.a_Id));
                    }
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
                    if (comparison != 0)
                        return comparison;
                    return 0;
                }));
                foreach (var resultSortedRowsRow in resultSortedRows)
                {
                    __musoqFinalShapeRows.Add(resultSortedRowsRow);
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

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            var __profileScopeDepth = profileRecorder?.GetCurrentOperatorScopeDepth() ?? 0;
            try
            {
                QueryProgressEventHandler OnQueryProgress = QueryProgress;
                var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
                Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
                try
                {
                    var _cteRowResults = new CteRowResults();
                    var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                    ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                    var __musoqFinalShapeRows = new List<ResultShape0>();
                    var __op19Handle = profileRecorder?.GetOperatorHandle("op19", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op20Handle = profileRecorder?.GetOperatorHandle("op20", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op25Handle = profileRecorder?.GetOperatorHandle("op25", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op26Handle = profileRecorder?.GetOperatorHandle("op26", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op37Handle = profileRecorder?.GetOperatorHandle("op37", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op38Handle = profileRecorder?.GetOperatorHandle("op38", "ForEach") ?? OperatorProfileHandle.None;
                    var __op40Handle = profileRecorder?.GetOperatorHandle("op40", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op41Handle = profileRecorder?.GetOperatorHandle("op41", "SortShapeRows") ?? OperatorProfileHandle.None;
                    long __op40OutputRows = 0L;
                    var __op19Scope = profileRecorder?.BeginOperatorValue(__op19Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op19Scope.Dispose();
                    var __op20Scope = profileRecorder?.BeginOperatorValue(__op20Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op20Scope.Dispose();
                    var __op25Scope = profileRecorder?.BeginOperatorValue(__op25Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Where);
                    __op25Scope.Dispose();
                    var __op26Scope = profileRecorder?.BeginOperatorValue(__op26Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op26Scope.Dispose();
                    _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, profileRecorder, _cteRowResults);
                    var __op37Scope = profileRecorder?.BeginOperatorValue(__op37Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>();
                    __op37Scope.Dispose();
                    long __op38InputRows = 0L;
                    long __op38OutputRows = 0L;
                    var __op38Scope = profileRecorder?.BeginOperatorValue(__op38Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        var __storedTable0Rows = _cteRowResults.Slot0;
                        for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                        {
                            if ((__storedTable0Index & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Statement0Row0 ab = __storedTable0Rows[__storedTable0Index];
                            __op38InputRows++;
                            __op38OutputRows++;
                            if ((Array.IndexOf(__inSet_compiled_0, ab.b_Label) >= 0))
                            {
                                result.Add(new ResultShape0(ab.a_Id));
                                __op40OutputRows += 1;
                            }
                        }
                    }
                    finally
                    {
                        __op38Scope.AddInputRows(__op38InputRows);
                        __op38Scope.AddOutputRows(__op38OutputRows);
                        __op38Scope.Dispose();
                    }

                    var __op41Scope = profileRecorder?.BeginOperatorValue(__op41Handle) ?? OperatorProfileValueScope.None;
                    __op41Scope.AddInputRows(result.Count);
                    var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                    {
                        var comparison = left.Id.CompareTo(right.Id);
                        if (comparison != 0)
                            return comparison;
                        return 0;
                    }));
                    foreach (var resultSortedRowsRow in resultSortedRows)
                    {
                        __musoqFinalShapeRows.Add(resultSortedRowsRow);
                    }

                    __op41Scope.AddOutputRows(__musoqFinalShapeRows.Count);
                    __op41Scope.Dispose();
                    if (__op40OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op40Handle, __op40OutputRows);
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
            catch (Exception __profileException)when (profileRecorder != null && profileRecorder.RecordActiveOperatorException(__profileException, __profileScopeDepth))
            {
                profileRecorder.DisposeActiveOperatorScopes(__profileScopeDepth);
                throw;
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
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            var __statement0_aSchema = provider.GetSchema("#schema");
            var statement0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>.Open(__statement0_aSchema, "first", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks;
            var __statement0_bSchema = provider.GetSchema("#schema");
            var statement0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>.Open(__statement0_bSchema, "first", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_b_1, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            var statement0BHash = new Dictionary<int?, HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>>();
            foreach (var bChunk in statement0_bRows)
            {
                if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow> bChunkView)
                {
                    if (bChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow[] bChunkViewArray)
                    {
                        int bChunkViewOffset = bChunkView.Offset;
                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                        {
                            if ((bIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var b = bChunkViewArray[bChunkViewOffset + bIndex];
                            int? key = b.Id;
                            if (key == null)
                                continue;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>(b);
                                }
                                else
                                {
                                    matches.Add(b);
                                }
                            }
                        }

                        continue;
                    }

                    if (bChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow> bChunkViewList)
                    {
                        int bChunkViewOffset = bChunkView.Offset;
                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                        {
                            if ((bIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var b = bChunkViewList[bChunkViewOffset + bIndex];
                            int? key = b.Id;
                            if (key == null)
                                continue;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>(b);
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
                    int? key = b.Id;
                    if (key == null)
                        continue;
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>(b);
                        }
                        else
                        {
                            matches.Add(b);
                        }
                    }
                }
            }

            foreach (var aChunk in statement0_aRows)
            {
                if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow> aChunkView)
                {
                    if (aChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow[] aChunkViewArray)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewArray[aChunkViewOffset + aIndex];
                            bool statement0BHashHasMatch = false;
                            int? key = (a.Id + 1);
                            if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0BHashHasMatch = true;
                                    statement0.Add(new Statement0Row0(a.Id, b.Id, b.Label));
                                }
                            }

                            if (!statement0BHashHasMatch)
                            {
                                statement0.Add(new Statement0Row0(a.Id, null, null));
                            }
                        }

                        continue;
                    }

                    if (aChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow> aChunkViewList)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewList[aChunkViewOffset + aIndex];
                            bool statement0BHashHasMatch = false;
                            int? key = (a.Id + 1);
                            if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0BHashHasMatch = true;
                                    statement0.Add(new Statement0Row0(a.Id, b.Id, b.Label));
                                }
                            }

                            if (!statement0BHashHasMatch)
                            {
                                statement0.Add(new Statement0Row0(a.Id, null, null));
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
                    bool statement0BHashHasMatch = false;
                    int? key = (a.Id + 1);
                    if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                    {
                        foreach (var b in statement0BHashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            statement0BHashHasMatch = true;
                            statement0.Add(new Statement0Row0(a.Id, b.Id, b.Label));
                        }
                    }

                    if (!statement0BHashHasMatch)
                    {
                        statement0.Add(new Statement0Row0(a.Id, null, null));
                    }
                }
            }

            return statement0;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, Musoq.Evaluator.Diagnostics.QueryProfileRecorder profileRecorder, CteRowResults _cteRowResults)
        {
            var __profileScopeDepth = profileRecorder?.GetCurrentOperatorScopeDepth() ?? 0;
            try
            {
                var __op21Handle = profileRecorder?.GetOperatorHandle("op21", "SourceScan") ?? OperatorProfileHandle.None;
                var __op22Handle = profileRecorder?.GetOperatorHandle("op22", "SourceScan") ?? OperatorProfileHandle.None;
                var __op23Handle = profileRecorder?.GetOperatorHandle("op23", "CreateTable") ?? OperatorProfileHandle.None;
                var __op24Handle = profileRecorder?.GetOperatorHandle("op24", "CreateHash") ?? OperatorProfileHandle.None;
                var __op27Handle = profileRecorder?.GetOperatorHandle("op27", "ChunkedForEach") ?? OperatorProfileHandle.None;
                var __op28Handle = profileRecorder?.GetOperatorHandle("op28", "HashAdd") ?? OperatorProfileHandle.None;
                var __op29Handle = profileRecorder?.GetOperatorHandle("op29", "ChunkedForEach") ?? OperatorProfileHandle.None;
                var __op30Handle = profileRecorder?.GetOperatorHandle("op30", "HashProbe") ?? OperatorProfileHandle.None;
                var __op31Handle = profileRecorder?.GetOperatorHandle("op31", "ForEach") ?? OperatorProfileHandle.None;
                var __op33Handle = profileRecorder?.GetOperatorHandle("op33", "AppendRow") ?? OperatorProfileHandle.None;
                var __op35Handle = profileRecorder?.GetOperatorHandle("op35", "AppendRow") ?? OperatorProfileHandle.None;
                long __op28OutputRows = 0L;
                long __op30InputRows = 0L;
                long __op31InputRows = 0L;
                long __op31OutputRows = 0L;
                long __op33OutputRows = 0L;
                long __op35OutputRows = 0L;
                var __op21Scope = profileRecorder?.BeginOperatorValue(__op21Handle) ?? OperatorProfileValueScope.None;
                var __statement0_aSchema = provider.GetSchema("#schema");
                var statement0_aRowsProfile = profileRecorder?.CreateSourceRecorder("a");
                var statement0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>.Open(__statement0_aSchema, "first", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress, statement0_aRowsProfile == null ? SourceDiagnostics.None : statement0_aRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                var statement0_aRows = statement0_aRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks, statement0_aRowsProfile);
                __op21Scope.Dispose();
                var __op22Scope = profileRecorder?.BeginOperatorValue(__op22Handle) ?? OperatorProfileValueScope.None;
                var __statement0_bSchema = provider.GetSchema("#schema");
                var statement0_bRowsProfile = profileRecorder?.CreateSourceRecorder("b");
                var statement0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>.Open(__statement0_bSchema, "first", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_b_1, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress, statement0_bRowsProfile == null ? SourceDiagnostics.None : statement0_bRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                var statement0_bRows = statement0_bRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks, statement0_bRowsProfile);
                __op22Scope.Dispose();
                var __op23Scope = profileRecorder?.BeginOperatorValue(__op23Handle) ?? OperatorProfileValueScope.None;
                var statement0 = new List<Statement0Row0>();
                __op23Scope.Dispose();
                var __op24Scope = profileRecorder?.BeginOperatorValue(__op24Handle) ?? OperatorProfileValueScope.None;
                var statement0BHash = new Dictionary<int?, HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>>();
                __op24Scope.Dispose();
                long __op27InputRows = 0L;
                long __op27OutputRows = 0L;
                var __op27Scope = profileRecorder?.BeginOperatorValue(__op27Handle) ?? OperatorProfileValueScope.None;
                try
                {
                    foreach (var bChunk in statement0_bRows)
                    {
                        if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow> bChunkView)
                        {
                            if (bChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow[] bChunkViewArray)
                            {
                                int bChunkViewOffset = bChunkView.Offset;
                                for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                {
                                    if ((bIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var b = bChunkViewArray[bChunkViewOffset + bIndex];
                                    __op27InputRows++;
                                    __op27OutputRows++;
                                    int? key = b.Id;
                                    if (key == null)
                                        continue;
                                    {
                                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                        if (!matchesExists)
                                        {
                                            matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>(b);
                                        }
                                        else
                                        {
                                            matches.Add(b);
                                        }
                                    }

                                    __op28OutputRows += 1;
                                }

                                continue;
                            }

                            if (bChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow> bChunkViewList)
                            {
                                int bChunkViewOffset = bChunkView.Offset;
                                for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                {
                                    if ((bIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var b = bChunkViewList[bChunkViewOffset + bIndex];
                                    __op27InputRows++;
                                    __op27OutputRows++;
                                    int? key = b.Id;
                                    if (key == null)
                                        continue;
                                    {
                                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                        if (!matchesExists)
                                        {
                                            matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>(b);
                                        }
                                        else
                                        {
                                            matches.Add(b);
                                        }
                                    }

                                    __op28OutputRows += 1;
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
                            __op27InputRows++;
                            __op27OutputRows++;
                            int? key = b.Id;
                            if (key == null)
                                continue;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow>(b);
                                }
                                else
                                {
                                    matches.Add(b);
                                }
                            }

                            __op28OutputRows += 1;
                        }
                    }
                }
                finally
                {
                    __op27Scope.AddInputRows(__op27InputRows);
                    __op27Scope.AddOutputRows(__op27OutputRows);
                    __op27Scope.Dispose();
                }

                long __op29InputRows = 0L;
                long __op29OutputRows = 0L;
                var __op29Scope = profileRecorder?.BeginOperatorValue(__op29Handle) ?? OperatorProfileValueScope.None;
                try
                {
                    foreach (var aChunk in statement0_aRows)
                    {
                        if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow> aChunkView)
                        {
                            if (aChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow[] aChunkViewArray)
                            {
                                int aChunkViewOffset = aChunkView.Offset;
                                for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                                {
                                    if ((aIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var a = aChunkViewArray[aChunkViewOffset + aIndex];
                                    __op29InputRows++;
                                    __op29OutputRows++;
                                    __op30InputRows += 1;
                                    bool statement0BHashHasMatch = false;
                                    int? key = (a.Id + 1);
                                    if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                                    {
                                        foreach (var b in statement0BHashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            __op31InputRows++;
                                            __op31OutputRows++;
                                            statement0BHashHasMatch = true;
                                            statement0.Add(new Statement0Row0(a.Id, b.Id, b.Label));
                                            __op33OutputRows += 1;
                                        }
                                    }

                                    if (!statement0BHashHasMatch)
                                    {
                                        statement0.Add(new Statement0Row0(a.Id, null, null));
                                        __op35OutputRows += 1;
                                    }
                                }

                                continue;
                            }

                            if (aChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionProviderQueryTests.OuterMembershipRow> aChunkViewList)
                            {
                                int aChunkViewOffset = aChunkView.Offset;
                                for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                                {
                                    if ((aIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var a = aChunkViewList[aChunkViewOffset + aIndex];
                                    __op29InputRows++;
                                    __op29OutputRows++;
                                    __op30InputRows += 1;
                                    bool statement0BHashHasMatch = false;
                                    int? key = (a.Id + 1);
                                    if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                                    {
                                        foreach (var b in statement0BHashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            __op31InputRows++;
                                            __op31OutputRows++;
                                            statement0BHashHasMatch = true;
                                            statement0.Add(new Statement0Row0(a.Id, b.Id, b.Label));
                                            __op33OutputRows += 1;
                                        }
                                    }

                                    if (!statement0BHashHasMatch)
                                    {
                                        statement0.Add(new Statement0Row0(a.Id, null, null));
                                        __op35OutputRows += 1;
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
                            __op29InputRows++;
                            __op29OutputRows++;
                            __op30InputRows += 1;
                            bool statement0BHashHasMatch = false;
                            int? key = (a.Id + 1);
                            if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    __op31InputRows++;
                                    __op31OutputRows++;
                                    statement0BHashHasMatch = true;
                                    statement0.Add(new Statement0Row0(a.Id, b.Id, b.Label));
                                    __op33OutputRows += 1;
                                }
                            }

                            if (!statement0BHashHasMatch)
                            {
                                statement0.Add(new Statement0Row0(a.Id, null, null));
                                __op35OutputRows += 1;
                            }
                        }
                    }
                }
                finally
                {
                    __op29Scope.AddInputRows(__op29InputRows);
                    __op29Scope.AddOutputRows(__op29OutputRows);
                    __op29Scope.Dispose();
                }

                if (__op28OutputRows > 0L)
                    profileRecorder?.AddOperatorOutputRows(__op28Handle, __op28OutputRows);
                if (__op30InputRows > 0L)
                    profileRecorder?.AddOperatorInputRows(__op30Handle, __op30InputRows);
                if (__op31InputRows > 0L)
                    profileRecorder?.AddOperatorInputRows(__op31Handle, __op31InputRows);
                if (__op31OutputRows > 0L)
                    profileRecorder?.AddOperatorOutputRows(__op31Handle, __op31OutputRows);
                if (__op33OutputRows > 0L)
                    profileRecorder?.AddOperatorOutputRows(__op33Handle, __op33OutputRows);
                if (__op35OutputRows > 0L)
                    profileRecorder?.AddOperatorOutputRows(__op35Handle, __op35OutputRows);
                return statement0;
            }
            catch (Exception __profileException)when (profileRecorder != null && profileRecorder.RecordActiveOperatorException(__profileException, __profileScopeDepth))
            {
                profileRecorder.DisposeActiveOperatorScopes(__profileScopeDepth);
                throw;
            }
        }

        private sealed class CteRowResults
        {
            public List<Statement0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0)
            {
                Id = __value0;
            }

            public override int Count => 1;
            public int Id { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id)
            {
                this.Id = Id;
            }

            public int Id { get; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(int __value0, int? __value1, string __value2)
            {
                a_Id = __value0;
                b_Id = __value1;
                b_Label = __value2;
            }

            public int a_Id { get; }
            public int? b_Id { get; }
            public string b_Label { get; }
        }
    }
}
