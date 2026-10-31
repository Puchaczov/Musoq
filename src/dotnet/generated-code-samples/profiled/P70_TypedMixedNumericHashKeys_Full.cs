// === Parsed Query ===
/*
select a.Position as LeftPosition, b.Position as RightPosition from #schema.first() a inner join #schema.first() b on a.IntKey = b.LongKey and a.Label = b.Label order by LeftPosition, RightPosition
*/

// === Logical Plan ===
/*
MultiStatement
  Project [a.Position as a.Position, a.IntKey as a.IntKey, a.Label as a.Label, b.Position as b.Position, b.LongKey as b.LongKey, b.Label as b.Label]
    Join [Inner] [((a.IntKey = b.LongKey) AND (a.Label = b.Label))]
      SchemaScan [#schema.first() as a]
      SchemaScan [#schema.first() as b]
  Sort [a.Position, b.Position]
    Project [a.Position as LeftPosition, b.Position as RightPosition]
      CteRef [ab as ab]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.Position as a.Position, b.Position as b.Position]
    PhysicalHashJoin [Inner] [build: b.Label] [probe: a.Label] [residual: (a.IntKey = b.LongKey)]
      PhysicalSchemaScan [#schema.first() as a]
      PhysicalSchemaScan [#schema.first() as b]
  PhysicalSort [a.Position, b.Position]
    PhysicalProject [a.Position as LeftPosition, b.Position as RightPosition]
      PhysicalCteRef [ab as ab]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: MixedInput]
      Position: int <- property Position
      IntKey: int? <- property IntKey
      Label: string <- property Label
    SourceEntity [b: MixedInput]
      Position: int <- property Position
      LongKey: long? <- property LongKey
      Label: string <- property Label
    Generated [Statement0Row0]
      a.Position: int <- field a_Position
      b.Position: int <- field b_Position
    TableRow [ab]
      a.Position: int <- field a_Position
      b.Position: int <- field b_Position
    Generated [ResultRow0]
      LeftPosition: int <- field LeftPosition
      RightPosition: int <- field RightPosition

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [a: MixedInput] -> statement0_aRows
    SourceScan [b: MixedInput] -> statement0_bRows
    CreateTable [statement0: Statement0Row0]
    CreateHash [statement0BHash: string -> MixedInput]
    PhaseBoundary [Select]
    ChunkedForEach [b in statement0_bRows]
      HashAdd [statement0BHash[b.Label] += b]
    ChunkedForEach [a in statement0_aRows]
      HashProbe [statement0BHash[a.Label] -> statement0BHashMatches]
        ForEach [b in statement0BHashMatches]
          If [(a.IntKey = b.LongKey)]
            AppendRow [statement0 <- Statement0Row0(a.Position: a.Position, b.Position: b.Position)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [ab in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(LeftPosition: ab.a.Position, RightPosition: ab.b.Position)]
    SortOwnedBuffer [result by LeftPosition ASC, RightPosition ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P70_TypedMixedNumericHashKeys_Full
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
            new Column("LeftPosition", typeof(int), 0),
            new Column("RightPosition", typeof(int), 1)
        };
        private static readonly Column[] __columns_compiled_statement0_2 = new Column[]
        {
            new Column("a.Position", typeof(int), 0),
            new Column("b.Position", typeof(int), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("IntKey", typeof(int?), 1), new Column("Label", typeof(string), 2) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("LongKey", typeof(long?), 1), new Column("Label", typeof(string), 2) });
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
                yield return new ResultRow0(__musoqShapeRow.LeftPosition, __musoqShapeRow.RightPosition);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.LeftPosition, __musoqShapeRow.RightPosition);
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
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 ab = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(ab.a_Position, ab.b_Position) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.LeftPosition.CompareTo(right.LeftPosition);
                    if (comparison != 0)
                        return comparison;
                    comparison = left.RightPosition.CompareTo(right.RightPosition);
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
                    var __op21Handle = profileRecorder?.GetOperatorHandle("op21", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op22Handle = profileRecorder?.GetOperatorHandle("op22", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op27Handle = profileRecorder?.GetOperatorHandle("op27", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op36Handle = profileRecorder?.GetOperatorHandle("op36", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op37Handle = profileRecorder?.GetOperatorHandle("op37", "ForEach") ?? OperatorProfileHandle.None;
                    var __op38Handle = profileRecorder?.GetOperatorHandle("op38", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op39Handle = profileRecorder?.GetOperatorHandle("op39", "SortOwnedBuffer") ?? OperatorProfileHandle.None;
                    long __op38OutputRows = 0L;
                    var __op21Scope = profileRecorder?.BeginOperatorValue(__op21Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op21Scope.Dispose();
                    var __op22Scope = profileRecorder?.BeginOperatorValue(__op22Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op22Scope.Dispose();
                    var __op27Scope = profileRecorder?.BeginOperatorValue(__op27Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op27Scope.Dispose();
                    _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, profileRecorder, _cteRowResults);
                    var __op36Scope = profileRecorder?.BeginOperatorValue(__op36Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>();
                    __op36Scope.Dispose();
                    long __op37InputRows = 0L;
                    long __op37OutputRows = 0L;
                    var __op37Scope = profileRecorder?.BeginOperatorValue(__op37Handle) ?? OperatorProfileValueScope.None;
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
                            __op37InputRows++;
                            __op37OutputRows++;
                            result.Add(new ResultShape0(ab.a_Position, ab.b_Position) { __musoqSequence = result.Count });
                            __op38OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op37Scope.AddInputRows(__op37InputRows);
                        __op37Scope.AddOutputRows(__op37OutputRows);
                        __op37Scope.Dispose();
                    }

                    var __op39Scope = profileRecorder?.BeginOperatorValue(__op39Handle) ?? OperatorProfileValueScope.None;
                    __op39Scope.AddInputRows(result.Count);
                    result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                    {
                        var comparison = left.LeftPosition.CompareTo(right.LeftPosition);
                        if (comparison != 0)
                            return comparison;
                        comparison = left.RightPosition.CompareTo(right.RightPosition);
                        if (comparison != 0)
                            return comparison;
                        return left.__musoqSequence.CompareTo(right.__musoqSequence);
                    }));
                    __op39Scope.AddOutputRows(result.Count);
                    __op39Scope.Dispose();
                    if (__op38OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op38Handle, __op38OutputRows);
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
            var statement0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>.Open(__statement0_aSchema, "first", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks;
            var __statement0_bSchema = provider.GetSchema("#schema");
            var statement0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>.Open(__statement0_bSchema, "first", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_b_1, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            var statement0BHash = new Dictionary<string, HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>>();
            foreach (var bChunk in statement0_bRows)
            {
                if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput> bChunkView)
                {
                    if (bChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput[] bChunkViewArray)
                    {
                        int bChunkViewOffset = bChunkView.Offset;
                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                        {
                            if ((bIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var b = bChunkViewArray[bChunkViewOffset + bIndex];
                            string key = b.Label;
                            if (key == null)
                                continue;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>(b);
                                }
                                else
                                {
                                    matches.Add(b);
                                }
                            }
                        }

                        continue;
                    }

                    if (bChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput> bChunkViewList)
                    {
                        int bChunkViewOffset = bChunkView.Offset;
                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                        {
                            if ((bIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var b = bChunkViewList[bChunkViewOffset + bIndex];
                            string key = b.Label;
                            if (key == null)
                                continue;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>(b);
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
                    string key = b.Label;
                    if (key == null)
                        continue;
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>(b);
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
                if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput> aChunkView)
                {
                    if (aChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput[] aChunkViewArray)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewArray[aChunkViewOffset + aIndex];
                            string key = a.Label;
                            if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    if ((Operators.SqlCompare<int?, long?>(a.IntKey, b.LongKey, (int? __sqlLeft, long? __sqlRight) => (__sqlLeft == __sqlRight))) == true)
                                    {
                                        statement0.Add(new Statement0Row0(a.Position, b.Position));
                                    }
                                }
                            }
                        }

                        continue;
                    }

                    if (aChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput> aChunkViewList)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewList[aChunkViewOffset + aIndex];
                            string key = a.Label;
                            if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    if ((Operators.SqlCompare<int?, long?>(a.IntKey, b.LongKey, (int? __sqlLeft, long? __sqlRight) => (__sqlLeft == __sqlRight))) == true)
                                    {
                                        statement0.Add(new Statement0Row0(a.Position, b.Position));
                                    }
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
                    string key = a.Label;
                    if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                    {
                        foreach (var b in statement0BHashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            if ((Operators.SqlCompare<int?, long?>(a.IntKey, b.LongKey, (int? __sqlLeft, long? __sqlRight) => (__sqlLeft == __sqlRight))) == true)
                            {
                                statement0.Add(new Statement0Row0(a.Position, b.Position));
                            }
                        }
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
                var __op23Handle = profileRecorder?.GetOperatorHandle("op23", "SourceScan") ?? OperatorProfileHandle.None;
                var __op24Handle = profileRecorder?.GetOperatorHandle("op24", "SourceScan") ?? OperatorProfileHandle.None;
                var __op25Handle = profileRecorder?.GetOperatorHandle("op25", "CreateTable") ?? OperatorProfileHandle.None;
                var __op26Handle = profileRecorder?.GetOperatorHandle("op26", "CreateHash") ?? OperatorProfileHandle.None;
                var __op28Handle = profileRecorder?.GetOperatorHandle("op28", "ChunkedForEach") ?? OperatorProfileHandle.None;
                var __op29Handle = profileRecorder?.GetOperatorHandle("op29", "HashAdd") ?? OperatorProfileHandle.None;
                var __op30Handle = profileRecorder?.GetOperatorHandle("op30", "ChunkedForEach") ?? OperatorProfileHandle.None;
                var __op31Handle = profileRecorder?.GetOperatorHandle("op31", "HashProbe") ?? OperatorProfileHandle.None;
                var __op32Handle = profileRecorder?.GetOperatorHandle("op32", "ForEach") ?? OperatorProfileHandle.None;
                var __op34Handle = profileRecorder?.GetOperatorHandle("op34", "AppendRow") ?? OperatorProfileHandle.None;
                long __op29OutputRows = 0L;
                long __op31InputRows = 0L;
                long __op32InputRows = 0L;
                long __op32OutputRows = 0L;
                long __op34OutputRows = 0L;
                var __op23Scope = profileRecorder?.BeginOperatorValue(__op23Handle) ?? OperatorProfileValueScope.None;
                var __statement0_aSchema = provider.GetSchema("#schema");
                var statement0_aRowsProfile = profileRecorder?.CreateSourceRecorder("a");
                var statement0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>.Open(__statement0_aSchema, "first", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress, statement0_aRowsProfile == null ? SourceDiagnostics.None : statement0_aRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                var statement0_aRows = statement0_aRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks, statement0_aRowsProfile);
                __op23Scope.Dispose();
                var __op24Scope = profileRecorder?.BeginOperatorValue(__op24Handle) ?? OperatorProfileValueScope.None;
                var __statement0_bSchema = provider.GetSchema("#schema");
                var statement0_bRowsProfile = profileRecorder?.CreateSourceRecorder("b");
                var statement0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>.Open(__statement0_bSchema, "first", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_b_1, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress, statement0_bRowsProfile == null ? SourceDiagnostics.None : statement0_bRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                var statement0_bRows = statement0_bRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks, statement0_bRowsProfile);
                __op24Scope.Dispose();
                var __op25Scope = profileRecorder?.BeginOperatorValue(__op25Handle) ?? OperatorProfileValueScope.None;
                var statement0 = new List<Statement0Row0>();
                __op25Scope.Dispose();
                var __op26Scope = profileRecorder?.BeginOperatorValue(__op26Handle) ?? OperatorProfileValueScope.None;
                var statement0BHash = new Dictionary<string, HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>>();
                __op26Scope.Dispose();
                long __op28InputRows = 0L;
                long __op28OutputRows = 0L;
                var __op28Scope = profileRecorder?.BeginOperatorValue(__op28Handle) ?? OperatorProfileValueScope.None;
                try
                {
                    foreach (var bChunk in statement0_bRows)
                    {
                        if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput> bChunkView)
                        {
                            if (bChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput[] bChunkViewArray)
                            {
                                int bChunkViewOffset = bChunkView.Offset;
                                for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                {
                                    if ((bIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var b = bChunkViewArray[bChunkViewOffset + bIndex];
                                    __op28InputRows++;
                                    __op28OutputRows++;
                                    string key = b.Label;
                                    if (key == null)
                                        continue;
                                    {
                                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                        if (!matchesExists)
                                        {
                                            matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>(b);
                                        }
                                        else
                                        {
                                            matches.Add(b);
                                        }
                                    }

                                    __op29OutputRows += 1;
                                }

                                continue;
                            }

                            if (bChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput> bChunkViewList)
                            {
                                int bChunkViewOffset = bChunkView.Offset;
                                for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                {
                                    if ((bIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var b = bChunkViewList[bChunkViewOffset + bIndex];
                                    __op28InputRows++;
                                    __op28OutputRows++;
                                    string key = b.Label;
                                    if (key == null)
                                        continue;
                                    {
                                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                        if (!matchesExists)
                                        {
                                            matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>(b);
                                        }
                                        else
                                        {
                                            matches.Add(b);
                                        }
                                    }

                                    __op29OutputRows += 1;
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
                            __op28InputRows++;
                            __op28OutputRows++;
                            string key = b.Label;
                            if (key == null)
                                continue;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput>(b);
                                }
                                else
                                {
                                    matches.Add(b);
                                }
                            }

                            __op29OutputRows += 1;
                        }
                    }
                }
                finally
                {
                    __op28Scope.AddInputRows(__op28InputRows);
                    __op28Scope.AddOutputRows(__op28OutputRows);
                    __op28Scope.Dispose();
                }

                long __op30InputRows = 0L;
                long __op30OutputRows = 0L;
                var __op30Scope = profileRecorder?.BeginOperatorValue(__op30Handle) ?? OperatorProfileValueScope.None;
                try
                {
                    foreach (var aChunk in statement0_aRows)
                    {
                        if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput> aChunkView)
                        {
                            if (aChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput[] aChunkViewArray)
                            {
                                int aChunkViewOffset = aChunkView.Offset;
                                for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                                {
                                    if ((aIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var a = aChunkViewArray[aChunkViewOffset + aIndex];
                                    __op30InputRows++;
                                    __op30OutputRows++;
                                    __op31InputRows += 1;
                                    string key = a.Label;
                                    if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                                    {
                                        foreach (var b in statement0BHashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            __op32InputRows++;
                                            __op32OutputRows++;
                                            if ((Operators.SqlCompare<int?, long?>(a.IntKey, b.LongKey, (int? __sqlLeft, long? __sqlRight) => (__sqlLeft == __sqlRight))) == true)
                                            {
                                                statement0.Add(new Statement0Row0(a.Position, b.Position));
                                                __op34OutputRows += 1;
                                            }
                                        }
                                    }
                                }

                                continue;
                            }

                            if (aChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionMixedKeyQueryTests.MixedInput> aChunkViewList)
                            {
                                int aChunkViewOffset = aChunkView.Offset;
                                for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                                {
                                    if ((aIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var a = aChunkViewList[aChunkViewOffset + aIndex];
                                    __op30InputRows++;
                                    __op30OutputRows++;
                                    __op31InputRows += 1;
                                    string key = a.Label;
                                    if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                                    {
                                        foreach (var b in statement0BHashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            __op32InputRows++;
                                            __op32OutputRows++;
                                            if ((Operators.SqlCompare<int?, long?>(a.IntKey, b.LongKey, (int? __sqlLeft, long? __sqlRight) => (__sqlLeft == __sqlRight))) == true)
                                            {
                                                statement0.Add(new Statement0Row0(a.Position, b.Position));
                                                __op34OutputRows += 1;
                                            }
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
                            __op30InputRows++;
                            __op30OutputRows++;
                            __op31InputRows += 1;
                            string key = a.Label;
                            if (key != null && statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    __op32InputRows++;
                                    __op32OutputRows++;
                                    if ((Operators.SqlCompare<int?, long?>(a.IntKey, b.LongKey, (int? __sqlLeft, long? __sqlRight) => (__sqlLeft == __sqlRight))) == true)
                                    {
                                        statement0.Add(new Statement0Row0(a.Position, b.Position));
                                        __op34OutputRows += 1;
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    __op30Scope.AddInputRows(__op30InputRows);
                    __op30Scope.AddOutputRows(__op30OutputRows);
                    __op30Scope.Dispose();
                }

                if (__op29OutputRows > 0L)
                    profileRecorder?.AddOperatorOutputRows(__op29Handle, __op29OutputRows);
                if (__op31InputRows > 0L)
                    profileRecorder?.AddOperatorInputRows(__op31Handle, __op31InputRows);
                if (__op32InputRows > 0L)
                    profileRecorder?.AddOperatorInputRows(__op32Handle, __op32InputRows);
                if (__op32OutputRows > 0L)
                    profileRecorder?.AddOperatorOutputRows(__op32Handle, __op32OutputRows);
                if (__op34OutputRows > 0L)
                    profileRecorder?.AddOperatorOutputRows(__op34Handle, __op34OutputRows);
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
            public ResultRow0(int __value0, int __value1)
            {
                LeftPosition = __value0;
                RightPosition = __value1;
            }

            public override int Count => 2;
            public int LeftPosition { get; private set; }
            public int RightPosition { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.LeftPosition = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.RightPosition = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.LeftPosition),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.RightPosition),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "LeftPosition" => true,
                "RightPosition" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.LeftPosition, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.RightPosition, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int LeftPosition, int RightPosition)
            {
                this.LeftPosition = LeftPosition;
                this.RightPosition = RightPosition;
            }

            public int LeftPosition { get; }
            public int RightPosition { get; }
            public int __musoqSequence { get; init; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(int __value0, int __value1)
            {
                a_Position = __value0;
                b_Position = __value1;
            }

            public int a_Position { get; }
            public int b_Position { get; }
        }
    }
}
