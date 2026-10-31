// === Parsed Query ===
/*
select a.Id, Sum(a.Amount) over (order by a.Id) as Total, RowNumber() over (order by a.Id) as Number from #typedrows.rows() a inner join #typedrows.rows() b on a.Id = b.Id order by a.Id
*/

// === Logical Plan ===
/*
MultiStatement
  Project [a.Id as a.Id, a.Amount as a.Amount, b.Id as b.Id]
    Join [Inner] [(a.Id = b.Id)]
      SchemaScan [#typedrows.rows() as a]
      SchemaScan [#typedrows.rows() as b]
  Sort [a.Id]
    Project [a.Id as Id, WindowRef(0) as Total, WindowRef(1) as Number]
      Window [Sum(idx:0; order: a.Id; args: a.Amount), RowNumber(idx:1; order: a.Id)]
        CteRef [ab as ab]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.Id as a.Id, a.Amount as a.Amount]
    PhysicalHashJoin [Inner] [build: b.Id] [probe: a.Id]
      PhysicalSchemaScan [#typedrows.rows() as a] [query-row:SealedClass;lifetime=EscapesScan;shape=896769F4889215AC88BDFADE9351576307B6F8F655D2248082FAD5BDA158FBCB]
      PhysicalSchemaScan [#typedrows.rows() as b] [query-row:SealedClass;lifetime=EscapesScan;shape=3D4EDDB8B07DA3CD9EFD8F267D363EC943ADE8917A3F2B49DF8EE4CBED47227B]
  PhysicalSort [a.Id]
    PhysicalProject [a.Id as Id, WindowRef(0) as Total, WindowRef(1) as Number]
      PhysicalWindow [Sum(idx:0; order: a.Id; args: a.Amount), RowNumber(idx:1; order: a.Id)]
        PhysicalMaterialize
          PhysicalCteRef [ab as ab]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    Generated [QueryRow_896769F48892_C]
      Id: int <- field Field0
      Amount: decimal? <- field Field1
    Generated [QueryRow_3D4EDDB8B07D_C]
      Id: int <- field Field0
    Generated [Statement0Row0]
      a.Id: int <- field a_Id
      a.Amount: decimal? <- field a_Amount
    TableRow [ab]
      a.Id: int <- field a_Id
      a.Amount: decimal? <- field a_Amount
    Generated [ResultRow0]
      Id: int <- field Id
      Total: decimal? <- field Total
      Number: long <- field Number

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [a: QueryRow_896769F48892_C] -> statement0_aRows [query-row:SealedClass;lifetime=EscapesScan;shape=896769F4889215AC88BDFADE9351576307B6F8F655D2248082FAD5BDA158FBCB]
    SourceScan [b: QueryRow_3D4EDDB8B07D_C] -> statement0_bRows [query-row:SealedClass;lifetime=EscapesScan;shape=3D4EDDB8B07DA3CD9EFD8F267D363EC943ADE8917A3F2B49DF8EE4CBED47227B]
    CreateTable [statement0: Statement0Row0]
    CreateHash [statement0BHash: int -> object]
    PhaseBoundary [Select]
    ChunkedForEach [b in statement0_bRows]
      HashAdd [statement0BHash[b.Id] += b]
    ChunkedForEach [a in statement0_aRows]
      HashProbe [statement0BHash[a.Id] -> statement0BHashMatches]
        ForEach [b in statement0BHashMatches]
          AppendRow [statement0 <- Statement0Row0(a.Id: a.Id, a.Amount: a.Amount)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    Materialize [_cteRowResults.Slot0 -> resultWindowRows]
    ComputeSumWindowKernel[BoundedRows] [resultSums0 <- resultWindowRows value ab.a.Amount order by ab.a.Id ASC frame range between unbounded preceding and current row]
    ComputeRowNumberWindow [resultRowNumbers1 <- resultWindowRows order by ab.a.Id ASC]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, ab in resultWindowRows]
      AppendShape [result <- ResultShape0(Id: ab.a.Id, Total: resultSums0[windowIndex], Number: resultRowNumbers1[windowIndex])]
    SortOwnedBuffer [result by Id ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P51_TypedProviderJoinedWindow_Full
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
            new Column("Id", typeof(int), 0),
            new Column("Total", typeof(decimal?), 1),
            new Column("Number", typeof(long), 2)
        };
        private static readonly Column[] __columns_compiled_statement0_2 = new Column[]
        {
            new Column("a.Id", typeof(int), 0),
            new Column("a.Amount", typeof(decimal?), 1)
        };
        private static readonly QueryRowShape __queryRowShape_3D4EDDB8B07D = new QueryRowShape(new QueryRowField[] { new QueryRowField(0, 0, "Id", typeof(int), false) });
        private static readonly QueryRowShape __queryRowShape_896769F48892 = new QueryRowShape(new QueryRowField[] { new QueryRowField(0, 0, "Id", typeof(int), false), new QueryRowField(1, 1, "Amount", typeof(decimal?), true) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Amount", typeof(decimal?), 1) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0) });
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
                yield return new ResultRow0(__musoqShapeRow.Id, __musoqShapeRow.Total, __musoqShapeRow.Number);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.Id, __musoqShapeRow.Total, __musoqShapeRow.Number);
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
                var resultWindowRows = EvaluationHelper.MaterializeGeneratedRows<Statement0Row0>(_cteRowResults.Slot0);
                var resultSums0OrderKeys = new WindowResultSums0OrderKeysKey[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Statement0Row0 ab = resultWindowRows[windowIndex];
                    resultSums0OrderKeys[windowIndex] = new WindowResultSums0OrderKeysKey(ab.a_Id);
                }

                var resultSums0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultSums0Partitions, resultSums0OrderKeys, false);
                var resultSums0 = new decimal? [resultWindowRows.Count];
                for (int resultSums0PartitionSetIndex = 0; resultSums0PartitionSetIndex < resultSums0Partitions.PartitionCount; ++resultSums0PartitionSetIndex)
                {
                    var resultSums0PartitionStart = resultSums0Partitions.GetStart(resultSums0PartitionSetIndex);
                    var resultSums0PartitionCount = resultSums0Partitions.GetLength(resultSums0PartitionSetIndex);
                    var resultSums0PartitionIndices = resultSums0Partitions.Indices;
                    Musoq.Plugins.SumAggregateKernel<decimal>.State resultSums0State = default(Musoq.Plugins.SumAggregateKernel<decimal>.State)!;
                    Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref resultSums0State);
                    for (int resultSums0PartitionIndex = 0; resultSums0PartitionIndex < resultSums0PartitionCount;)
                    {
                        var resultSums0PeerEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultSums0OrderKeys, resultSums0PartitionIndices, resultSums0PartitionStart, resultSums0PartitionCount, resultSums0PartitionIndex);
                        for (int resultSums0PeerIndex = resultSums0PartitionIndex; resultSums0PeerIndex <= resultSums0PeerEnd; ++resultSums0PeerIndex)
                        {
                            var resultSums0CurrentIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0PeerIndex];
                            Statement0Row0 ab = resultWindowRows[resultSums0CurrentIndex];
                            Musoq.Plugins.SumAggregateKernel<decimal>.Set(ref resultSums0State, (decimal?)ab.a_Amount);
                        }

                        var resultSums0PeerValue = Musoq.Plugins.SumAggregateKernel<decimal>.Get(in resultSums0State);
                        for (int resultSums0PeerIndex = resultSums0PartitionIndex; resultSums0PeerIndex <= resultSums0PeerEnd; ++resultSums0PeerIndex)
                        {
                            var resultSums0CurrentIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0PeerIndex];
                            resultSums0[resultSums0CurrentIndex] = resultSums0PeerValue;
                        }

                        resultSums0PartitionIndex = resultSums0PeerEnd + 1;
                    }
                }

                var resultRowNumbers1 = new long[resultWindowRows.Count];
                for (int resultRowNumbers1PartitionSetIndex = 0; resultRowNumbers1PartitionSetIndex < resultSums0Partitions.PartitionCount; ++resultRowNumbers1PartitionSetIndex)
                {
                    var resultRowNumbers1PartitionStart = resultSums0Partitions.GetStart(resultRowNumbers1PartitionSetIndex);
                    var resultRowNumbers1PartitionCount = resultSums0Partitions.GetLength(resultRowNumbers1PartitionSetIndex);
                    var resultRowNumbers1PartitionIndices = resultSums0Partitions.Indices;
                    var resultRowNumbers1PartitionLimit = resultRowNumbers1PartitionCount;
                    for (int resultRowNumbers1PartitionIndex = 0; resultRowNumbers1PartitionIndex < resultRowNumbers1PartitionLimit; ++resultRowNumbers1PartitionIndex)
                    {
                        var resultRowNumbers1CurrentIndex = resultRowNumbers1PartitionIndices[resultRowNumbers1PartitionStart + resultRowNumbers1PartitionIndex];
                        var resultRowNumbers1Context = new Musoq.Plugins.WindowRankingContext(resultRowNumbers1PartitionIndex, 0, 0, 0L, resultRowNumbers1PartitionCount, token);
                        resultRowNumbers1[resultRowNumbers1CurrentIndex] = Musoq.Plugins.RowNumberKernel.Get(in resultRowNumbers1Context);
                    }
                }

                var result = new List<ResultShape0>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 ab = resultWindowRows[windowIndex];
                    result.Add(new ResultShape0(ab.a_Id, (decimal?)resultSums0[windowIndex], (long)resultRowNumbers1[windowIndex]) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
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
                    var __op33Handle = profileRecorder?.GetOperatorHandle("op33", "Materialize") ?? OperatorProfileHandle.None;
                    var __op34Handle = profileRecorder?.GetOperatorHandle("op34", "ComputeSumWindowKernel") ?? OperatorProfileHandle.None;
                    var __op35Handle = profileRecorder?.GetOperatorHandle("op35", "ComputeRowNumberWindow") ?? OperatorProfileHandle.None;
                    var __op36Handle = profileRecorder?.GetOperatorHandle("op36", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op37Handle = profileRecorder?.GetOperatorHandle("op37", "ForEachIndexed") ?? OperatorProfileHandle.None;
                    var __op38Handle = profileRecorder?.GetOperatorHandle("op38", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op39Handle = profileRecorder?.GetOperatorHandle("op39", "SortOwnedBuffer") ?? OperatorProfileHandle.None;
                    long __op38OutputRows = 0L;
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
                    var __op33Scope = profileRecorder?.BeginOperatorValue(__op33Handle) ?? OperatorProfileValueScope.None;
                    var resultWindowRows = EvaluationHelper.MaterializeGeneratedRows<Statement0Row0>(_cteRowResults.Slot0);
                    __op33Scope.AddOutputRows(resultWindowRows.Count);
                    __op33Scope.Dispose();
                    var __op34Scope = profileRecorder?.BeginOperatorValue(__op34Handle) ?? OperatorProfileValueScope.None;
                    var resultSums0OrderKeys = new WindowResultSums0OrderKeysKey[resultWindowRows.Count];
                    for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                    {
                        Statement0Row0 ab = resultWindowRows[windowIndex];
                        resultSums0OrderKeys[windowIndex] = new WindowResultSums0OrderKeysKey(ab.a_Id);
                    }

                    var resultSums0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                    WindowFunctionHelpers.SortStructPartitionSetInPlace(resultSums0Partitions, resultSums0OrderKeys, false);
                    var resultSums0 = new decimal? [resultWindowRows.Count];
                    for (int resultSums0PartitionSetIndex = 0; resultSums0PartitionSetIndex < resultSums0Partitions.PartitionCount; ++resultSums0PartitionSetIndex)
                    {
                        var resultSums0PartitionStart = resultSums0Partitions.GetStart(resultSums0PartitionSetIndex);
                        var resultSums0PartitionCount = resultSums0Partitions.GetLength(resultSums0PartitionSetIndex);
                        var resultSums0PartitionIndices = resultSums0Partitions.Indices;
                        Musoq.Plugins.SumAggregateKernel<decimal>.State resultSums0State = default(Musoq.Plugins.SumAggregateKernel<decimal>.State)!;
                        Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref resultSums0State);
                        for (int resultSums0PartitionIndex = 0; resultSums0PartitionIndex < resultSums0PartitionCount;)
                        {
                            var resultSums0PeerEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultSums0OrderKeys, resultSums0PartitionIndices, resultSums0PartitionStart, resultSums0PartitionCount, resultSums0PartitionIndex);
                            for (int resultSums0PeerIndex = resultSums0PartitionIndex; resultSums0PeerIndex <= resultSums0PeerEnd; ++resultSums0PeerIndex)
                            {
                                var resultSums0CurrentIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0PeerIndex];
                                Statement0Row0 ab = resultWindowRows[resultSums0CurrentIndex];
                                Musoq.Plugins.SumAggregateKernel<decimal>.Set(ref resultSums0State, (decimal?)ab.a_Amount);
                            }

                            var resultSums0PeerValue = Musoq.Plugins.SumAggregateKernel<decimal>.Get(in resultSums0State);
                            for (int resultSums0PeerIndex = resultSums0PartitionIndex; resultSums0PeerIndex <= resultSums0PeerEnd; ++resultSums0PeerIndex)
                            {
                                var resultSums0CurrentIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0PeerIndex];
                                resultSums0[resultSums0CurrentIndex] = resultSums0PeerValue;
                            }

                            resultSums0PartitionIndex = resultSums0PeerEnd + 1;
                        }
                    }

                    __op34Scope.Dispose();
                    var __op35Scope = profileRecorder?.BeginOperatorValue(__op35Handle) ?? OperatorProfileValueScope.None;
                    var resultRowNumbers1 = new long[resultWindowRows.Count];
                    for (int resultRowNumbers1PartitionSetIndex = 0; resultRowNumbers1PartitionSetIndex < resultSums0Partitions.PartitionCount; ++resultRowNumbers1PartitionSetIndex)
                    {
                        var resultRowNumbers1PartitionStart = resultSums0Partitions.GetStart(resultRowNumbers1PartitionSetIndex);
                        var resultRowNumbers1PartitionCount = resultSums0Partitions.GetLength(resultRowNumbers1PartitionSetIndex);
                        var resultRowNumbers1PartitionIndices = resultSums0Partitions.Indices;
                        var resultRowNumbers1PartitionLimit = resultRowNumbers1PartitionCount;
                        for (int resultRowNumbers1PartitionIndex = 0; resultRowNumbers1PartitionIndex < resultRowNumbers1PartitionLimit; ++resultRowNumbers1PartitionIndex)
                        {
                            var resultRowNumbers1CurrentIndex = resultRowNumbers1PartitionIndices[resultRowNumbers1PartitionStart + resultRowNumbers1PartitionIndex];
                            var resultRowNumbers1Context = new Musoq.Plugins.WindowRankingContext(resultRowNumbers1PartitionIndex, 0, 0, 0L, resultRowNumbers1PartitionCount, token);
                            resultRowNumbers1[resultRowNumbers1CurrentIndex] = Musoq.Plugins.RowNumberKernel.Get(in resultRowNumbers1Context);
                        }
                    }

                    __op35Scope.Dispose();
                    var __op36Scope = profileRecorder?.BeginOperatorValue(__op36Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>(resultWindowRows.Count);
                    __op36Scope.Dispose();
                    long __op37InputRows = 0L;
                    long __op37OutputRows = 0L;
                    var __op37Scope = profileRecorder?.BeginOperatorValue(__op37Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                        {
                            if ((windowIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Statement0Row0 ab = resultWindowRows[windowIndex];
                            __op37InputRows++;
                            __op37OutputRows++;
                            result.Add(new ResultShape0(ab.a_Id, (decimal?)resultSums0[windowIndex], (long)resultRowNumbers1[windowIndex]) { __musoqSequence = result.Count });
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
                        var comparison = left.Id.CompareTo(right.Id);
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
            var __statement0_aSchema = provider.GetSchema("#typedrows");
            var __statement0_aSchemaQueryRows = __statement0_aSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#typedrows.rows' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape 896769F4889215AC88BDFADE9351576307B6F8F655D2248082FAD5BDA158FBCB).");
            var statement0_aRowsSource = QuerySourceInvocation<QueryRow_896769F48892_C, QueryRowMaterializer_896769F48892_C>.Open(__statement0_aSchemaQueryRows, "rows", new QueryScopedRowSourceRequest(new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), __queryRowShape_896769F48892), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_896769F48892_C>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks;
            var __statement0_bSchema = provider.GetSchema("#typedrows");
            var __statement0_bSchemaQueryRows = __statement0_bSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#typedrows.rows' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape 3D4EDDB8B07DA3CD9EFD8F267D363EC943ADE8917A3F2B49DF8EE4CBED47227B).");
            var statement0_bRowsSource = QuerySourceInvocation<QueryRow_3D4EDDB8B07D_C, QueryRowMaterializer_3D4EDDB8B07D_C>.Open(__statement0_bSchemaQueryRows, "rows", new QueryScopedRowSourceRequest(new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_b_1, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), __queryRowShape_3D4EDDB8B07D), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_3D4EDDB8B07D_C>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            var statement0BHash = new Dictionary<int, HashJoinBucket<QueryRow_3D4EDDB8B07D_C>>();
            foreach (var bChunk in statement0_bRows)
            {
                if (bChunk is global::Musoq.Schema.DataSources.RowChunk<QueryRow_3D4EDDB8B07D_C> bChunkView)
                {
                    if (bChunkView.Source is QueryRow_3D4EDDB8B07D_C[] bChunkViewArray)
                    {
                        int bChunkViewOffset = bChunkView.Offset;
                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                        {
                            if ((bIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var b = bChunkViewArray[bChunkViewOffset + bIndex];
                            int key = b.Field0;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<QueryRow_3D4EDDB8B07D_C>(b);
                                }
                                else
                                {
                                    matches.Add(b);
                                }
                            }
                        }

                        continue;
                    }

                    if (bChunkView.Source is List<QueryRow_3D4EDDB8B07D_C> bChunkViewList)
                    {
                        int bChunkViewOffset = bChunkView.Offset;
                        for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                        {
                            if ((bIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var b = bChunkViewList[bChunkViewOffset + bIndex];
                            int key = b.Field0;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<QueryRow_3D4EDDB8B07D_C>(b);
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
                    int key = b.Field0;
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<QueryRow_3D4EDDB8B07D_C>(b);
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
                if (aChunk is global::Musoq.Schema.DataSources.RowChunk<QueryRow_896769F48892_C> aChunkView)
                {
                    if (aChunkView.Source is QueryRow_896769F48892_C[] aChunkViewArray)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewArray[aChunkViewOffset + aIndex];
                            int key = a.Field0;
                            if (statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0.Add(new Statement0Row0(a.Field0, a.Field1));
                                }
                            }
                        }

                        continue;
                    }

                    if (aChunkView.Source is List<QueryRow_896769F48892_C> aChunkViewList)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewList[aChunkViewOffset + aIndex];
                            int key = a.Field0;
                            if (statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0.Add(new Statement0Row0(a.Field0, a.Field1));
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
                    int key = a.Field0;
                    if (statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                    {
                        foreach (var b in statement0BHashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            statement0.Add(new Statement0Row0(a.Field0, a.Field1));
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
                var __op21Handle = profileRecorder?.GetOperatorHandle("op21", "SourceScan") ?? OperatorProfileHandle.None;
                var __op22Handle = profileRecorder?.GetOperatorHandle("op22", "SourceScan") ?? OperatorProfileHandle.None;
                var __op23Handle = profileRecorder?.GetOperatorHandle("op23", "CreateTable") ?? OperatorProfileHandle.None;
                var __op24Handle = profileRecorder?.GetOperatorHandle("op24", "CreateHash") ?? OperatorProfileHandle.None;
                var __op26Handle = profileRecorder?.GetOperatorHandle("op26", "ChunkedForEach") ?? OperatorProfileHandle.None;
                var __op27Handle = profileRecorder?.GetOperatorHandle("op27", "HashAdd") ?? OperatorProfileHandle.None;
                var __op28Handle = profileRecorder?.GetOperatorHandle("op28", "ChunkedForEach") ?? OperatorProfileHandle.None;
                var __op29Handle = profileRecorder?.GetOperatorHandle("op29", "HashProbe") ?? OperatorProfileHandle.None;
                var __op30Handle = profileRecorder?.GetOperatorHandle("op30", "ForEach") ?? OperatorProfileHandle.None;
                var __op31Handle = profileRecorder?.GetOperatorHandle("op31", "AppendRow") ?? OperatorProfileHandle.None;
                long __op27OutputRows = 0L;
                long __op29InputRows = 0L;
                long __op30InputRows = 0L;
                long __op30OutputRows = 0L;
                long __op31OutputRows = 0L;
                var __op21Scope = profileRecorder?.BeginOperatorValue(__op21Handle) ?? OperatorProfileValueScope.None;
                var __statement0_aSchema = provider.GetSchema("#typedrows");
                var statement0_aRowsProfile = profileRecorder?.CreateSourceRecorder("a");
                var __statement0_aSchemaQueryRows = __statement0_aSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#typedrows.rows' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape 896769F4889215AC88BDFADE9351576307B6F8F655D2248082FAD5BDA158FBCB).");
                var statement0_aRowsSource = QuerySourceInvocation<QueryRow_896769F48892_C, QueryRowMaterializer_896769F48892_C>.Open(__statement0_aSchemaQueryRows, "rows", new QueryScopedRowSourceRequest(new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress, statement0_aRowsProfile == null ? SourceDiagnostics.None : statement0_aRowsProfile.CreateDiagnostics()), __queryRowShape_896769F48892), global::Musoq.Schema.SourceArguments.Empty);
                var statement0_aRows = statement0_aRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_896769F48892_C>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks : ProfiledChunkedEnumerable<QueryRow_896769F48892_C>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_896769F48892_C>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks, statement0_aRowsProfile);
                __op21Scope.Dispose();
                var __op22Scope = profileRecorder?.BeginOperatorValue(__op22Handle) ?? OperatorProfileValueScope.None;
                var __statement0_bSchema = provider.GetSchema("#typedrows");
                var statement0_bRowsProfile = profileRecorder?.CreateSourceRecorder("b");
                var __statement0_bSchemaQueryRows = __statement0_bSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#typedrows.rows' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape 3D4EDDB8B07DA3CD9EFD8F267D363EC943ADE8917A3F2B49DF8EE4CBED47227B).");
                var statement0_bRowsSource = QuerySourceInvocation<QueryRow_3D4EDDB8B07D_C, QueryRowMaterializer_3D4EDDB8B07D_C>.Open(__statement0_bSchemaQueryRows, "rows", new QueryScopedRowSourceRequest(new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_b_1, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress, statement0_bRowsProfile == null ? SourceDiagnostics.None : statement0_bRowsProfile.CreateDiagnostics()), __queryRowShape_3D4EDDB8B07D), global::Musoq.Schema.SourceArguments.Empty);
                var statement0_bRows = statement0_bRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_3D4EDDB8B07D_C>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks : ProfiledChunkedEnumerable<QueryRow_3D4EDDB8B07D_C>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_3D4EDDB8B07D_C>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks, statement0_bRowsProfile);
                __op22Scope.Dispose();
                var __op23Scope = profileRecorder?.BeginOperatorValue(__op23Handle) ?? OperatorProfileValueScope.None;
                var statement0 = new List<Statement0Row0>();
                __op23Scope.Dispose();
                var __op24Scope = profileRecorder?.BeginOperatorValue(__op24Handle) ?? OperatorProfileValueScope.None;
                var statement0BHash = new Dictionary<int, HashJoinBucket<QueryRow_3D4EDDB8B07D_C>>();
                __op24Scope.Dispose();
                long __op26InputRows = 0L;
                long __op26OutputRows = 0L;
                var __op26Scope = profileRecorder?.BeginOperatorValue(__op26Handle) ?? OperatorProfileValueScope.None;
                try
                {
                    foreach (var bChunk in statement0_bRows)
                    {
                        if (bChunk is global::Musoq.Schema.DataSources.RowChunk<QueryRow_3D4EDDB8B07D_C> bChunkView)
                        {
                            if (bChunkView.Source is QueryRow_3D4EDDB8B07D_C[] bChunkViewArray)
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
                                    int key = b.Field0;
                                    {
                                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                        if (!matchesExists)
                                        {
                                            matches = new HashJoinBucket<QueryRow_3D4EDDB8B07D_C>(b);
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

                            if (bChunkView.Source is List<QueryRow_3D4EDDB8B07D_C> bChunkViewList)
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
                                    int key = b.Field0;
                                    {
                                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                        if (!matchesExists)
                                        {
                                            matches = new HashJoinBucket<QueryRow_3D4EDDB8B07D_C>(b);
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
                            int key = b.Field0;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0BHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<QueryRow_3D4EDDB8B07D_C>(b);
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
                        if (aChunk is global::Musoq.Schema.DataSources.RowChunk<QueryRow_896769F48892_C> aChunkView)
                        {
                            if (aChunkView.Source is QueryRow_896769F48892_C[] aChunkViewArray)
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
                                    int key = a.Field0;
                                    if (statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                                    {
                                        foreach (var b in statement0BHashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            __op30InputRows++;
                                            __op30OutputRows++;
                                            statement0.Add(new Statement0Row0(a.Field0, a.Field1));
                                            __op31OutputRows += 1;
                                        }
                                    }
                                }

                                continue;
                            }

                            if (aChunkView.Source is List<QueryRow_896769F48892_C> aChunkViewList)
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
                                    int key = a.Field0;
                                    if (statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                                    {
                                        foreach (var b in statement0BHashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            __op30InputRows++;
                                            __op30OutputRows++;
                                            statement0.Add(new Statement0Row0(a.Field0, a.Field1));
                                            __op31OutputRows += 1;
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
                            __op28InputRows++;
                            __op28OutputRows++;
                            __op29InputRows += 1;
                            int key = a.Field0;
                            if (statement0BHash.TryGetValue(key, out var statement0BHashMatches))
                            {
                                foreach (var b in statement0BHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    __op30InputRows++;
                                    __op30OutputRows++;
                                    statement0.Add(new Statement0Row0(a.Field0, a.Field1));
                                    __op31OutputRows += 1;
                                }
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
                if (__op31OutputRows > 0L)
                    profileRecorder?.AddOperatorOutputRows(__op31Handle, __op31OutputRows);
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

        private readonly struct QueryRowMaterializer_3D4EDDB8B07D_C : IQueryRowMaterializer<QueryRow_3D4EDDB8B07D_C>
        {
            public static QueryRow_3D4EDDB8B07D_C Materialize<TReader>(scoped ref TReader reader)
                where TReader : IQuerySourceFieldReader, allows ref struct => new QueryRow_3D4EDDB8B07D_C(reader.Read<int>(0));
        }

        private readonly struct QueryRowMaterializer_896769F48892_C : IQueryRowMaterializer<QueryRow_896769F48892_C>
        {
            public static QueryRow_896769F48892_C Materialize<TReader>(scoped ref TReader reader)
                where TReader : IQuerySourceFieldReader, allows ref struct => new QueryRow_896769F48892_C(reader.Read<int>(0), reader.Read<decimal?>(1));
        }

        private sealed class QueryRow_3D4EDDB8B07D_C
        {
            public QueryRow_3D4EDDB8B07D_C(int __value0)
            {
                Field0 = __value0;
            }

            public int Field0 { get; }
        }

        private sealed class QueryRow_896769F48892_C
        {
            public QueryRow_896769F48892_C(int __value0, decimal? __value1)
            {
                Field0 = __value0;
                Field1 = __value1;
            }

            public int Field0 { get; }
            public decimal? Field1 { get; }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, decimal? __value1, long __value2)
            {
                Id = __value0;
                Total = __value1;
                Number = __value2;
            }

            public override int Count => 3;
            public int Id { get; private set; }
            public long Number { get; private set; }
            public decimal? Total { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Number = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Number),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Total" => true,
                "Number" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Number, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id, decimal? Total, long Number)
            {
                this.Id = Id;
                this.Total = Total;
                this.Number = Number;
            }

            public int Id { get; }
            public long Number { get; }
            public decimal? Total { get; }
            public int __musoqSequence { get; init; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(int __value0, decimal? __value1)
            {
                a_Id = __value0;
                a_Amount = __value1;
            }

            public decimal? a_Amount { get; }
            public int a_Id { get; }
        }

        private readonly struct WindowResultSums0OrderKeysKey : System.IEquatable<WindowResultSums0OrderKeysKey>, System.IComparable<WindowResultSums0OrderKeysKey>
        {
            private readonly int _value0;
            public WindowResultSums0OrderKeysKey(int value0)
            {
                _value0 = value0;
            }

            public int RawValues => _value0;

            public int CompareTo(WindowResultSums0OrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultSums0OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultSums0OrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0);
                return hash.ToHashCode();
            }

            private static int CompareValue0(int left, int right)
            {
                var comparison = left.CompareTo(right);
                return comparison;
            }
        }
    }
}
