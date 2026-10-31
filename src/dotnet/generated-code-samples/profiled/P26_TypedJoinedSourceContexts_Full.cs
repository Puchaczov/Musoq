// === Parsed Query ===
/*
select a.Position, b.SourcePosition() as OtherPosition, b.Amount
from #schema.first() a left outer join #schema.first() b
    on a.Position = b.Position and b.Position > 2 order by a.Position
*/

// === Logical Plan ===
/*
MultiStatement
  Project [a.Position as a.Position, b.Position as b.Position, b.Amount as b.Amount]
    Join [LeftOuter] [((a.Position = b.Position) AND (b.Position > 2))]
      SchemaScan [#schema.first() as a]
      SchemaScan [#schema.first() as b]
  Sort [a.Position]
    Project [a.Position as Position, SourcePosition() as OtherPosition, b.Amount as Amount]
      CteRef [ab as ab]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.Position as a.Position, b.Amount as b.Amount]
    PhysicalHashJoin [LeftOuter] [build: b.Position] [probe: a.Position] [residual: (b.Position > 2)]
      PhysicalSchemaScan [#schema.first() as a]
      PhysicalSchemaScan [#schema.first() as b]
  PhysicalSort [a.Position]
    PhysicalProject [a.Position as Position, SourcePosition() as OtherPosition, b.Amount as Amount]
      PhysicalCteRef [ab as ab]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: LibraryExecutionEntity]
      Position: int <- property Position
    SourceEntity [b: LibraryExecutionEntity]
      Position: int <- property Position
      Amount: decimal? <- property Amount
    Generated [Statement0Row0]
      a.Position: int <- field a_Position
      b.Amount: decimal? <- field b_Amount
    TableRow [ab]
      a.Position: int <- field a_Position
      b.Amount: decimal? <- field b_Amount
    Generated [ResultRow0]
      Position: int <- field Position
      OtherPosition: int? <- field OtherPosition
      Amount: decimal? <- field Amount

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [a: LibraryExecutionEntity] -> statement0_aRows
    SourceScan [b: LibraryExecutionEntity] -> statement0_bRows
    CreateTable [statement0: Statement0Row0]
    CreateHash [statement0BHash: int? -> LibraryExecutionEntity]
    PhaseBoundary [Select]
    ChunkedForEach [b in statement0_bRows]
      HashAdd [statement0BHash[b.Position] += b]
    ChunkedForEach [a in statement0_aRows]
      HashProbe [statement0BHash[a.Position] -> statement0BHashMatches] [match: statement0BHashHasMatch]
        ForEach [b in statement0BHashMatches]
          If [(b.Position > 2)]
            Assign [statement0BHashHasMatch = TRUE]
            AppendRow [statement0 <- Statement0Row0(a.Position: a.Position, b.Amount: b.Amount)]
      HashProbeNoMatch
        AppendRow [statement0 <- Statement0Row0(a.Position: a.Position, b.Amount: NULL)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateObject [__resultLibraryExecutionLibrary0: LibraryExecutionLibrary]
    ForEach [ab in _cteRowResults.Slot0]
      Let [b1: LibraryExecutionEntity = ab.b]
      AppendShape [result <- ResultShape0(Position: ab.a.Position, OtherPosition: CASE WHEN b1 IS NULL THEN NULL ELSE SourcePosition() END, Amount: ab.b.Amount)]
    SortOwnedBuffer [result by Position ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P26_TypedJoinedSourceContexts_Full
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
            new Column("Position", typeof(int), 0),
            new Column("OtherPosition", typeof(int?), 1),
            new Column("Amount", typeof(decimal?), 2)
        };
        private static readonly Column[] __columns_compiled_statement0_2 = new Column[]
        {
            new Column("a.Position", typeof(int), 0),
            new Column("b.Amount", typeof(decimal?), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Amount", typeof(decimal?), 1) });
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
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.OtherPosition, __musoqShapeRow.Amount);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.OtherPosition, __musoqShapeRow.Amount);
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
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var result = new List<ResultShape0>();
                var __resultLibraryExecutionLibrary0 = new Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary();
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 ab = __storedTable0Rows[__storedTable0Index];
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity b1 = (Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity)ab.__rightContext;
                    result.Add(new ResultShape0(ab.a_Position, (b1 == null) ? (int?)null : (int?)(int)__resultLibraryExecutionLibrary0.SourcePosition((Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity)b1), ab.b_Amount) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Position.CompareTo(right.Position);
                    if (comparison != 0)
                        return comparison;
                    return left.__musoqSequence.CompareTo(right.__musoqSequence);
                }));
                return result;
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
                    var __op19Handle = profileRecorder?.GetOperatorHandle("op19", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op20Handle = profileRecorder?.GetOperatorHandle("op20", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op25Handle = profileRecorder?.GetOperatorHandle("op25", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op37Handle = profileRecorder?.GetOperatorHandle("op37", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op38Handle = profileRecorder?.GetOperatorHandle("op38", "CreateObject") ?? OperatorProfileHandle.None;
                    var __op39Handle = profileRecorder?.GetOperatorHandle("op39", "ForEach") ?? OperatorProfileHandle.None;
                    var __op41Handle = profileRecorder?.GetOperatorHandle("op41", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op42Handle = profileRecorder?.GetOperatorHandle("op42", "SortOwnedBuffer") ?? OperatorProfileHandle.None;
                    long __op41OutputRows = 0L;
                    var __op19Scope = profileRecorder?.BeginOperatorValue(__op19Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op19Scope.Dispose();
                    var __op20Scope = profileRecorder?.BeginOperatorValue(__op20Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op20Scope.Dispose();
                    var __op25Scope = profileRecorder?.BeginOperatorValue(__op25Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op25Scope.Dispose();
                    _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, profileRecorder, _cteRowResults);
                    var __op37Scope = profileRecorder?.BeginOperatorValue(__op37Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>();
                    __op37Scope.Dispose();
                    var __op38Scope = profileRecorder?.BeginOperatorValue(__op38Handle) ?? OperatorProfileValueScope.None;
                    var __resultLibraryExecutionLibrary0 = new Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary();
                    __op38Scope.Dispose();
                    long __op39InputRows = 0L;
                    long __op39OutputRows = 0L;
                    var __op39Scope = profileRecorder?.BeginOperatorValue(__op39Handle) ?? OperatorProfileValueScope.None;
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
                            __op39InputRows++;
                            __op39OutputRows++;
                            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity b1 = (Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity)ab.__rightContext;
                            result.Add(new ResultShape0(ab.a_Position, (b1 == null) ? (int?)null : (int?)(int)__resultLibraryExecutionLibrary0.SourcePosition((Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity)b1), ab.b_Amount) { __musoqSequence = result.Count });
                            __op41OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op39Scope.AddInputRows(__op39InputRows);
                        __op39Scope.AddOutputRows(__op39OutputRows);
                        __op39Scope.Dispose();
                    }

                    var __op42Scope = profileRecorder?.BeginOperatorValue(__op42Handle) ?? OperatorProfileValueScope.None;
                    __op42Scope.AddInputRows(result.Count);
                    result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                    {
                        var comparison = left.Position.CompareTo(right.Position);
                        if (comparison != 0)
                            return comparison;
                        return left.__musoqSequence.CompareTo(right.__musoqSequence);
                    }));
                    __op42Scope.AddOutputRows(result.Count);
                    __op42Scope.Dispose();
                    if (__op41OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op41Handle, __op41OutputRows);
                    return result;
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
            var statement0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__statement0_aSchema, "first", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks;
            var __statement0_bSchema = provider.GetSchema("#schema");
            var statement0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__statement0_bSchema, "first", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_b_1, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            var statement0BHash = new Dictionary<int?, HashJoinBucket<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>>();
            foreach (var bChunk in statement0_bRows)
            {
                if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> bChunkView)
                {
                    if (bChunkView.Source is Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity[] bChunkViewArray)
                    {
                        int bChunkViewOffset = bChunkView.Offset;
                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                        {
                            if ((bIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var b = bChunkViewArray[bChunkViewOffset + bIndex];
                            int? key = b.Position;
                            if (key == null)
                                continue;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(b);
                                }
                                else
                                {
                                    matches.Add(b);
                                }
                            }
                        }

                        continue;
                    }

                    if (bChunkView.Source is List<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> bChunkViewList)
                    {
                        int bChunkViewOffset = bChunkView.Offset;
                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                        {
                            if ((bIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var b = bChunkViewList[bChunkViewOffset + bIndex];
                            int? key = b.Position;
                            if (key == null)
                                continue;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(b);
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
                    int? key = b.Position;
                    if (key == null)
                        continue;
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(b);
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
                if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> aChunkView)
                {
                    if (aChunkView.Source is Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity[] aChunkViewArray)
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
                            int? key = a.Position;
                            if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    if ((Operators.SqlCompare<int?, int>(b.Position, 2, (int? __sqlLeft, int __sqlRight) => (__sqlLeft > __sqlRight))) == true)
                                    {
                                        statement0BHashHasMatch = true;
                                        statement0.Add(new Statement0Row0(a.Position, b.Amount, a, b));
                                    }
                                }
                            }

                            if (!statement0BHashHasMatch)
                            {
                                statement0.Add(new Statement0Row0(a.Position, null, a, null));
                            }
                        }

                        continue;
                    }

                    if (aChunkView.Source is List<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> aChunkViewList)
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
                            int? key = a.Position;
                            if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    if ((Operators.SqlCompare<int?, int>(b.Position, 2, (int? __sqlLeft, int __sqlRight) => (__sqlLeft > __sqlRight))) == true)
                                    {
                                        statement0BHashHasMatch = true;
                                        statement0.Add(new Statement0Row0(a.Position, b.Amount, a, b));
                                    }
                                }
                            }

                            if (!statement0BHashHasMatch)
                            {
                                statement0.Add(new Statement0Row0(a.Position, null, a, null));
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
                    int? key = a.Position;
                    if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                    {
                        foreach (var b in statement0BHashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            if ((Operators.SqlCompare<int?, int>(b.Position, 2, (int? __sqlLeft, int __sqlRight) => (__sqlLeft > __sqlRight))) == true)
                            {
                                statement0BHashHasMatch = true;
                                statement0.Add(new Statement0Row0(a.Position, b.Amount, a, b));
                            }
                        }
                    }

                    if (!statement0BHashHasMatch)
                    {
                        statement0.Add(new Statement0Row0(a.Position, null, a, null));
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
                var __op26Handle = profileRecorder?.GetOperatorHandle("op26", "ChunkedForEach") ?? OperatorProfileHandle.None;
                var __op27Handle = profileRecorder?.GetOperatorHandle("op27", "HashAdd") ?? OperatorProfileHandle.None;
                var __op28Handle = profileRecorder?.GetOperatorHandle("op28", "ChunkedForEach") ?? OperatorProfileHandle.None;
                var __op29Handle = profileRecorder?.GetOperatorHandle("op29", "HashProbe") ?? OperatorProfileHandle.None;
                var __op30Handle = profileRecorder?.GetOperatorHandle("op30", "ForEach") ?? OperatorProfileHandle.None;
                var __op33Handle = profileRecorder?.GetOperatorHandle("op33", "AppendRow") ?? OperatorProfileHandle.None;
                var __op35Handle = profileRecorder?.GetOperatorHandle("op35", "AppendRow") ?? OperatorProfileHandle.None;
                long __op27OutputRows = 0L;
                long __op29InputRows = 0L;
                long __op30InputRows = 0L;
                long __op30OutputRows = 0L;
                long __op33OutputRows = 0L;
                long __op35OutputRows = 0L;
                var __op21Scope = profileRecorder?.BeginOperatorValue(__op21Handle) ?? OperatorProfileValueScope.None;
                var __statement0_aSchema = provider.GetSchema("#schema");
                var statement0_aRowsProfile = profileRecorder?.CreateSourceRecorder("a");
                var statement0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__statement0_aSchema, "first", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress, statement0_aRowsProfile == null ? SourceDiagnostics.None : statement0_aRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                var statement0_aRows = statement0_aRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks, statement0_aRowsProfile);
                __op21Scope.Dispose();
                var __op22Scope = profileRecorder?.BeginOperatorValue(__op22Handle) ?? OperatorProfileValueScope.None;
                var __statement0_bSchema = provider.GetSchema("#schema");
                var statement0_bRowsProfile = profileRecorder?.CreateSourceRecorder("b");
                var statement0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__statement0_bSchema, "first", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_b_1, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress, statement0_bRowsProfile == null ? SourceDiagnostics.None : statement0_bRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                var statement0_bRows = statement0_bRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks, statement0_bRowsProfile);
                __op22Scope.Dispose();
                var __op23Scope = profileRecorder?.BeginOperatorValue(__op23Handle) ?? OperatorProfileValueScope.None;
                var statement0 = new List<Statement0Row0>();
                __op23Scope.Dispose();
                var __op24Scope = profileRecorder?.BeginOperatorValue(__op24Handle) ?? OperatorProfileValueScope.None;
                var statement0BHash = new Dictionary<int?, HashJoinBucket<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>>();
                __op24Scope.Dispose();
                long __op26InputRows = 0L;
                long __op26OutputRows = 0L;
                var __op26Scope = profileRecorder?.BeginOperatorValue(__op26Handle) ?? OperatorProfileValueScope.None;
                try
                {
                    foreach (var bChunk in statement0_bRows)
                    {
                        if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> bChunkView)
                        {
                            if (bChunkView.Source is Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity[] bChunkViewArray)
                            {
                                int bChunkViewOffset = bChunkView.Offset;
                                for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                {
                                    if ((bIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var b = bChunkViewArray[bChunkViewOffset + bIndex];
                                    __op26InputRows++;
                                    __op26OutputRows++;
                                    int? key = b.Position;
                                    if (key == null)
                                        continue;
                                    {
                                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                        if (!matchesExists)
                                        {
                                            matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(b);
                                        }
                                        else
                                        {
                                            matches.Add(b);
                                        }
                                    }

                                    __op27OutputRows += 1;
                                }

                                continue;
                            }

                            if (bChunkView.Source is List<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> bChunkViewList)
                            {
                                int bChunkViewOffset = bChunkView.Offset;
                                for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                {
                                    if ((bIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var b = bChunkViewList[bChunkViewOffset + bIndex];
                                    __op26InputRows++;
                                    __op26OutputRows++;
                                    int? key = b.Position;
                                    if (key == null)
                                        continue;
                                    {
                                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                        if (!matchesExists)
                                        {
                                            matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(b);
                                        }
                                        else
                                        {
                                            matches.Add(b);
                                        }
                                    }

                                    __op27OutputRows += 1;
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
                            __op26InputRows++;
                            __op26OutputRows++;
                            int? key = b.Position;
                            if (key == null)
                                continue;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(b);
                                }
                                else
                                {
                                    matches.Add(b);
                                }
                            }

                            __op27OutputRows += 1;
                        }
                    }
                }
                finally
                {
                    __op26Scope.AddInputRows(__op26InputRows);
                    __op26Scope.AddOutputRows(__op26OutputRows);
                    __op26Scope.Dispose();
                }

                long __op28InputRows = 0L;
                long __op28OutputRows = 0L;
                var __op28Scope = profileRecorder?.BeginOperatorValue(__op28Handle) ?? OperatorProfileValueScope.None;
                try
                {
                    foreach (var aChunk in statement0_aRows)
                    {
                        if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> aChunkView)
                        {
                            if (aChunkView.Source is Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity[] aChunkViewArray)
                            {
                                int aChunkViewOffset = aChunkView.Offset;
                                for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                                {
                                    if ((aIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var a = aChunkViewArray[aChunkViewOffset + aIndex];
                                    __op28InputRows++;
                                    __op28OutputRows++;
                                    __op29InputRows += 1;
                                    bool statement0BHashHasMatch = false;
                                    int? key = a.Position;
                                    if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                                    {
                                        foreach (var b in statement0BHashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            __op30InputRows++;
                                            __op30OutputRows++;
                                            if ((Operators.SqlCompare<int?, int>(b.Position, 2, (int? __sqlLeft, int __sqlRight) => (__sqlLeft > __sqlRight))) == true)
                                            {
                                                statement0BHashHasMatch = true;
                                                statement0.Add(new Statement0Row0(a.Position, b.Amount, a, b));
                                                __op33OutputRows += 1;
                                            }
                                        }
                                    }

                                    if (!statement0BHashHasMatch)
                                    {
                                        statement0.Add(new Statement0Row0(a.Position, null, a, null));
                                        __op35OutputRows += 1;
                                    }
                                }

                                continue;
                            }

                            if (aChunkView.Source is List<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> aChunkViewList)
                            {
                                int aChunkViewOffset = aChunkView.Offset;
                                for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                                {
                                    if ((aIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var a = aChunkViewList[aChunkViewOffset + aIndex];
                                    __op28InputRows++;
                                    __op28OutputRows++;
                                    __op29InputRows += 1;
                                    bool statement0BHashHasMatch = false;
                                    int? key = a.Position;
                                    if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                                    {
                                        foreach (var b in statement0BHashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            __op30InputRows++;
                                            __op30OutputRows++;
                                            if ((Operators.SqlCompare<int?, int>(b.Position, 2, (int? __sqlLeft, int __sqlRight) => (__sqlLeft > __sqlRight))) == true)
                                            {
                                                statement0BHashHasMatch = true;
                                                statement0.Add(new Statement0Row0(a.Position, b.Amount, a, b));
                                                __op33OutputRows += 1;
                                            }
                                        }
                                    }

                                    if (!statement0BHashHasMatch)
                                    {
                                        statement0.Add(new Statement0Row0(a.Position, null, a, null));
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
                            __op28InputRows++;
                            __op28OutputRows++;
                            __op29InputRows += 1;
                            bool statement0BHashHasMatch = false;
                            int? key = a.Position;
                            if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    __op30InputRows++;
                                    __op30OutputRows++;
                                    if ((Operators.SqlCompare<int?, int>(b.Position, 2, (int? __sqlLeft, int __sqlRight) => (__sqlLeft > __sqlRight))) == true)
                                    {
                                        statement0BHashHasMatch = true;
                                        statement0.Add(new Statement0Row0(a.Position, b.Amount, a, b));
                                        __op33OutputRows += 1;
                                    }
                                }
                            }

                            if (!statement0BHashHasMatch)
                            {
                                statement0.Add(new Statement0Row0(a.Position, null, a, null));
                                __op35OutputRows += 1;
                            }
                        }
                    }
                }
                finally
                {
                    __op28Scope.AddInputRows(__op28InputRows);
                    __op28Scope.AddOutputRows(__op28OutputRows);
                    __op28Scope.Dispose();
                }

                if (__op27OutputRows > 0L)
                    profileRecorder?.AddOperatorOutputRows(__op27Handle, __op27OutputRows);
                if (__op29InputRows > 0L)
                    profileRecorder?.AddOperatorInputRows(__op29Handle, __op29InputRows);
                if (__op30InputRows > 0L)
                    profileRecorder?.AddOperatorInputRows(__op30Handle, __op30InputRows);
                if (__op30OutputRows > 0L)
                    profileRecorder?.AddOperatorOutputRows(__op30Handle, __op30OutputRows);
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
            public ResultRow0(int __value0, int? __value1, decimal? __value2)
            {
                Position = __value0;
                OtherPosition = __value1;
                Amount = __value2;
            }

            public decimal? Amount { get; private set; }
            public override int Count => 3;
            public int? OtherPosition { get; private set; }
            public int Position { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.OtherPosition = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Amount = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.OtherPosition),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Amount),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "OtherPosition" => true,
                "Amount" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.OtherPosition, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Amount, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Position, int? OtherPosition, decimal? Amount)
            {
                this.Position = Position;
                this.OtherPosition = OtherPosition;
                this.Amount = Amount;
            }

            public decimal? Amount { get; }
            public int? OtherPosition { get; }
            public int Position { get; }
            public int __musoqSequence { get; init; }
        }

        private sealed class Statement0Row0
        {
            public readonly Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity __leftContext;
            public readonly Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity __rightContext;
            public Statement0Row0(int __value0, decimal? __value1, Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity __leftContext, Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity __rightContext)
            {
                a_Position = __value0;
                b_Amount = __value1;
                this.__leftContext = __leftContext;
                this.__rightContext = __rightContext;
            }

            public int a_Position { get; }
            public decimal? b_Amount { get; }
        }
    }
}
