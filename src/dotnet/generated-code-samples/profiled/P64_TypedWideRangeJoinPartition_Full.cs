// === Parsed Query ===
/*
select a.Position as FirstPosition, b.Position as SecondPosition
from #schema.first() a inner join #schema.first() b
    on a.K1 = b.K1 and a.K2 = b.K2 and a.K3 = b.K3
    and a.K4 = b.K4 and a.K5 = b.K5 and a.K6 = b.K6
    and a.K7 = b.K7 and a.K8 = b.K8 and a.K9 = b.K9
    and a.Amount > b.Amount
order by FirstPosition, SecondPosition
*/

// === Logical Plan ===
/*
MultiStatement
  Project [a.Position as a.Position, a.Amount as a.Amount, a.K1 as a.K1, a.K2 as a.K2, a.K3 as a.K3, a.K4 as a.K4, a.K5 as a.K5, a.K6 as a.K6, a.K7 as a.K7, a.K8 as a.K8, a.K9 as a.K9, b.Position as b.Position, b.Amount as b.Amount, b.K1 as b.K1, b.K2 as b.K2, b.K3 as b.K3, b.K4 as b.K4, b.K5 as b.K5, b.K6 as b.K6, b.K7 as b.K7, b.K8 as b.K8, b.K9 as b.K9]
    Join [Inner] [((((((((((a.K1 = b.K1) AND (a.K2 = b.K2)) AND (a.K3 = b.K3)) AND (a.K4 = b.K4)) AND (a.K5 = b.K5)) AND (a.K6 = b.K6)) AND (a.K7 = b.K7)) AND (a.K8 = b.K8)) AND (a.K9 = b.K9)) AND (a.Amount > b.Amount))]
      SchemaScan [#schema.first() as a]
      SchemaScan [#schema.first() as b]
  Sort [a.Position, b.Position]
    Project [a.Position as FirstPosition, b.Position as SecondPosition]
      CteRef [ab as ab]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.Position as a.Position, b.Position as b.Position]
    PhysicalSortMergeJoin [Inner] [left: a.Amount] [right: b.Amount] [op: >] [partitions: a.K1 = b.K1, a.K2 = b.K2, a.K3 = b.K3, a.K4 = b.K4, a.K5 = b.K5, a.K6 = b.K6, a.K7 = b.K7, a.K8 = b.K8, a.K9 = b.K9] [residual: ((((((((((a.K1 = b.K1) AND (a.K2 = b.K2)) AND (a.K3 = b.K3)) AND (a.K4 = b.K4)) AND (a.K5 = b.K5)) AND (a.K6 = b.K6)) AND (a.K7 = b.K7)) AND (a.K8 = b.K8)) AND (a.K9 = b.K9)) AND (a.Amount > b.Amount))]
      PhysicalSchemaScan [#schema.first() as a]
      PhysicalSchemaScan [#schema.first() as b]
  PhysicalSort [a.Position, b.Position]
    PhysicalProject [a.Position as FirstPosition, b.Position as SecondPosition]
      PhysicalCteRef [ab as ab]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: WideRangeInput]
      Position: int <- property Position
      Amount: decimal <- property Amount
      K1: int <- property K1
      K2: int <- property K2
      K3: int <- property K3
      K4: int <- property K4
      K5: int <- property K5
      K6: int <- property K6
      K7: int <- property K7
      K8: int <- property K8
      K9: string <- property K9
    SourceEntity [b: WideRangeInput]
      Position: int <- property Position
      Amount: decimal <- property Amount
      K1: int <- property K1
      K2: int <- property K2
      K3: int <- property K3
      K4: int <- property K4
      K5: int <- property K5
      K6: int <- property K6
      K7: int <- property K7
      K8: int <- property K8
      K9: string <- property K9
    Generated [Statement0Row0]
      a.Position: int <- field a_Position
      b.Position: int <- field b_Position
    TableRow [ab]
      a.Position: int <- field a_Position
      b.Position: int <- field b_Position
    Generated [ResultRow0]
      FirstPosition: int <- field FirstPosition
      SecondPosition: int <- field SecondPosition

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [a: WideRangeInput] -> statement0_aRows
    SourceScan [b: WideRangeInput] -> statement0_bRows
    CreateTable [statement0: Statement0Row0]
    CreateRangeIndex [statement0RangeIndex <- statement0_bRows by bRangeCandidate.K1, bRangeCandidate.K2, bRangeCandidate.K3, bRangeCandidate.K4, bRangeCandidate.K5, bRangeCandidate.K6, bRangeCandidate.K7, bRangeCandidate.K8, bRangeCandidate.K9, bRangeCandidate.Amount >]
    PhaseBoundary [Select]
    ChunkedForEach [a in statement0_aRows]
      RangeProbe [b <- statement0RangeIndex where a.K1 = bRangeCandidate.K1 and a.K2 = bRangeCandidate.K2 and a.K3 = bRangeCandidate.K3 and a.K4 = bRangeCandidate.K4 and a.K5 = bRangeCandidate.K5 and a.K6 = bRangeCandidate.K6 and a.K7 = bRangeCandidate.K7 and a.K8 = bRangeCandidate.K8 and a.K9 = bRangeCandidate.K9 and a.Amount]
        If [((((((((((a.K1 = b.K1) AND (a.K2 = b.K2)) AND (a.K3 = b.K3)) AND (a.K4 = b.K4)) AND (a.K5 = b.K5)) AND (a.K6 = b.K6)) AND (a.K7 = b.K7)) AND (a.K8 = b.K8)) AND (a.K9 = b.K9)) AND (a.Amount > b.Amount))]
          AppendRow [statement0 <- Statement0Row0(a.Position: a.Position, b.Position: b.Position)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [ab in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(FirstPosition: ab.a.Position, SecondPosition: ab.b.Position)]
    SortOwnedBuffer [result by FirstPosition ASC, SecondPosition ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P64_TypedWideRangeJoinPartition_Full
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("FirstPosition", typeof(int), 0),
            new Column("SecondPosition", typeof(int), 1)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("a.Position", typeof(int), 0),
            new Column("b.Position", typeof(int), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Amount", typeof(decimal), 1), new Column("K1", typeof(int), 2), new Column("K2", typeof(int), 3), new Column("K3", typeof(int), 4), new Column("K4", typeof(int), 5), new Column("K5", typeof(int), 6), new Column("K6", typeof(int), 7), new Column("K7", typeof(int), 8), new Column("K8", typeof(int), 9), new Column("K9", typeof(string), 10) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            ArgumentNullException.ThrowIfNull(profileRecorder);
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0_Profiled(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken, profileRecorder), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.FirstPosition, __musoqShapeRow.SecondPosition);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.FirstPosition, __musoqShapeRow.SecondPosition);
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
                    var comparison = left.FirstPosition.CompareTo(right.FirstPosition);
                    if (comparison != 0)
                        return comparison;
                    comparison = left.SecondPosition.CompareTo(right.SecondPosition);
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
                    var __op37Handle = profileRecorder?.GetOperatorHandle("op37", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op38Handle = profileRecorder?.GetOperatorHandle("op38", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op43Handle = profileRecorder?.GetOperatorHandle("op43", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op49Handle = profileRecorder?.GetOperatorHandle("op49", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op50Handle = profileRecorder?.GetOperatorHandle("op50", "ForEach") ?? OperatorProfileHandle.None;
                    var __op51Handle = profileRecorder?.GetOperatorHandle("op51", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op52Handle = profileRecorder?.GetOperatorHandle("op52", "SortOwnedBuffer") ?? OperatorProfileHandle.None;
                    long __op51OutputRows = 0L;
                    var __op37Scope = profileRecorder?.BeginOperatorValue(__op37Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op37Scope.Dispose();
                    var __op38Scope = profileRecorder?.BeginOperatorValue(__op38Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op38Scope.Dispose();
                    var __op43Scope = profileRecorder?.BeginOperatorValue(__op43Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op43Scope.Dispose();
                    _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, profileRecorder, _cteRowResults);
                    var __op49Scope = profileRecorder?.BeginOperatorValue(__op49Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>();
                    __op49Scope.Dispose();
                    long __op50InputRows = 0L;
                    long __op50OutputRows = 0L;
                    var __op50Scope = profileRecorder?.BeginOperatorValue(__op50Handle) ?? OperatorProfileValueScope.None;
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
                            __op50InputRows++;
                            __op50OutputRows++;
                            result.Add(new ResultShape0(ab.a_Position, ab.b_Position) { __musoqSequence = result.Count });
                            __op51OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op50Scope.AddInputRows(__op50InputRows);
                        __op50Scope.AddOutputRows(__op50OutputRows);
                        __op50Scope.Dispose();
                    }

                    var __op52Scope = profileRecorder?.BeginOperatorValue(__op52Handle) ?? OperatorProfileValueScope.None;
                    __op52Scope.AddInputRows(result.Count);
                    result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                    {
                        var comparison = left.FirstPosition.CompareTo(right.FirstPosition);
                        if (comparison != 0)
                            return comparison;
                        comparison = left.SecondPosition.CompareTo(right.SecondPosition);
                        if (comparison != 0)
                            return comparison;
                        return left.__musoqSequence.CompareTo(right.__musoqSequence);
                    }));
                    __op52Scope.AddOutputRows(result.Count);
                    __op52Scope.Dispose();
                    if (__op51OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op51Handle, __op51OutputRows);
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
            var statement0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput>.Open(__statement0_aSchema, "first", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks;
            var __statement0_bSchema = provider.GetSchema("#schema");
            var statement0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput>.Open(__statement0_bSchema, "first", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            var statement0RangeIndex = EvaluationHelper.CreateRangeJoinIndex<Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput, ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, string>>?, decimal>(statement0_bRows, (bRangeCandidate) => bRangeCandidate.K9 == null ? default(ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, string>>?) : (bRangeCandidate.K1, bRangeCandidate.K2, bRangeCandidate.K3, bRangeCandidate.K4, bRangeCandidate.K5, bRangeCandidate.K6, bRangeCandidate.K7, bRangeCandidate.K8, bRangeCandidate.K9), (bRangeCandidate) => bRangeCandidate.Amount, Musoq.Evaluator.IR.Expressions.BinaryOpKind.GreaterThan);
            foreach (var aChunk in statement0_aRows)
            {
                if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput> aChunkView)
                {
                    if (aChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput[] aChunkViewArray)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewArray[aChunkViewOffset + aIndex];
                            foreach (var b in statement0RangeIndex.Find<ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, string>>?>(a.K9 == null ? default(ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, string>>?) : (a.K1, a.K2, a.K3, a.K4, a.K5, a.K6, a.K7, a.K8, a.K9), a.Amount))
                            {
                                if (((((bool?)((((bool?)((((((((a.K1 == b.K1) && (a.K2 == b.K2)) && (a.K3 == b.K3)) && (a.K4 == b.K4)) && (a.K5 == b.K5)) && (a.K6 == b.K6)) && (a.K7 == b.K7)) && (a.K8 == b.K8))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<string, string>(a.K9, b.K9, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(a.K9, b.K9, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null

                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)(a.Amount > b.Amount),
                                    _ => (bool?)(a.Amount > b.Amount) & (bool?)null

                                })) == true)
                                {
                                    statement0.Add(new Statement0Row0(a.Position, b.Position));
                                }
                            }
                        }

                        continue;
                    }

                    if (aChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput> aChunkViewList)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewList[aChunkViewOffset + aIndex];
                            foreach (var b in statement0RangeIndex.Find<ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, string>>?>(a.K9 == null ? default(ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, string>>?) : (a.K1, a.K2, a.K3, a.K4, a.K5, a.K6, a.K7, a.K8, a.K9), a.Amount))
                            {
                                if (((((bool?)((((bool?)((((((((a.K1 == b.K1) && (a.K2 == b.K2)) && (a.K3 == b.K3)) && (a.K4 == b.K4)) && (a.K5 == b.K5)) && (a.K6 == b.K6)) && (a.K7 == b.K7)) && (a.K8 == b.K8))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<string, string>(a.K9, b.K9, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(a.K9, b.K9, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null

                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)(a.Amount > b.Amount),
                                    _ => (bool?)(a.Amount > b.Amount) & (bool?)null

                                })) == true)
                                {
                                    statement0.Add(new Statement0Row0(a.Position, b.Position));
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
                    foreach (var b in statement0RangeIndex.Find<ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, string>>?>(a.K9 == null ? default(ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, string>>?) : (a.K1, a.K2, a.K3, a.K4, a.K5, a.K6, a.K7, a.K8, a.K9), a.Amount))
                    {
                        if (((((bool?)((((bool?)((((((((a.K1 == b.K1) && (a.K2 == b.K2)) && (a.K3 == b.K3)) && (a.K4 == b.K4)) && (a.K5 == b.K5)) && (a.K6 == b.K6)) && (a.K7 == b.K7)) && (a.K8 == b.K8))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)Operators.SqlCompare<string, string>(a.K9, b.K9, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                            _ => (bool?)Operators.SqlCompare<string, string>(a.K9, b.K9, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null

                        }))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)(a.Amount > b.Amount),
                            _ => (bool?)(a.Amount > b.Amount) & (bool?)null

                        })) == true)
                        {
                            statement0.Add(new Statement0Row0(a.Position, b.Position));
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
                var __op39Handle = profileRecorder?.GetOperatorHandle("op39", "SourceScan") ?? OperatorProfileHandle.None;
                var __op40Handle = profileRecorder?.GetOperatorHandle("op40", "SourceScan") ?? OperatorProfileHandle.None;
                var __op41Handle = profileRecorder?.GetOperatorHandle("op41", "CreateTable") ?? OperatorProfileHandle.None;
                var __op42Handle = profileRecorder?.GetOperatorHandle("op42", "CreateRangeIndex") ?? OperatorProfileHandle.None;
                var __op44Handle = profileRecorder?.GetOperatorHandle("op44", "ChunkedForEach") ?? OperatorProfileHandle.None;
                var __op45Handle = profileRecorder?.GetOperatorHandle("op45", "RangeProbe") ?? OperatorProfileHandle.None;
                var __op47Handle = profileRecorder?.GetOperatorHandle("op47", "AppendRow") ?? OperatorProfileHandle.None;
                long __op47OutputRows = 0L;
                var __op39Scope = profileRecorder?.BeginOperatorValue(__op39Handle) ?? OperatorProfileValueScope.None;
                var __statement0_aSchema = provider.GetSchema("#schema");
                var statement0_aRowsProfile = profileRecorder?.CreateSourceRecorder("a");
                var statement0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput>.Open(__statement0_aSchema, "first", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress, statement0_aRowsProfile == null ? SourceDiagnostics.None : statement0_aRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                var statement0_aRows = statement0_aRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks, statement0_aRowsProfile);
                __op39Scope.Dispose();
                var __op40Scope = profileRecorder?.BeginOperatorValue(__op40Handle) ?? OperatorProfileValueScope.None;
                var __statement0_bSchema = provider.GetSchema("#schema");
                var statement0_bRowsProfile = profileRecorder?.CreateSourceRecorder("b");
                var statement0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput>.Open(__statement0_bSchema, "first", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress, statement0_bRowsProfile == null ? SourceDiagnostics.None : statement0_bRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                var statement0_bRows = statement0_bRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks, statement0_bRowsProfile);
                __op40Scope.Dispose();
                var __op41Scope = profileRecorder?.BeginOperatorValue(__op41Handle) ?? OperatorProfileValueScope.None;
                var statement0 = new List<Statement0Row0>();
                __op41Scope.Dispose();
                var __op42Scope = profileRecorder?.BeginOperatorValue(__op42Handle) ?? OperatorProfileValueScope.None;
                var statement0RangeIndex = EvaluationHelper.CreateRangeJoinIndex<Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput, ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, string>>?, decimal>(statement0_bRows, (bRangeCandidate) => bRangeCandidate.K9 == null ? default(ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, string>>?) : (bRangeCandidate.K1, bRangeCandidate.K2, bRangeCandidate.K3, bRangeCandidate.K4, bRangeCandidate.K5, bRangeCandidate.K6, bRangeCandidate.K7, bRangeCandidate.K8, bRangeCandidate.K9), (bRangeCandidate) => bRangeCandidate.Amount, Musoq.Evaluator.IR.Expressions.BinaryOpKind.GreaterThan);
                __op42Scope.Dispose();
                long __op44InputRows = 0L;
                long __op44OutputRows = 0L;
                var __op44Scope = profileRecorder?.BeginOperatorValue(__op44Handle) ?? OperatorProfileValueScope.None;
                try
                {
                    foreach (var aChunk in statement0_aRows)
                    {
                        if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput> aChunkView)
                        {
                            if (aChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput[] aChunkViewArray)
                            {
                                int aChunkViewOffset = aChunkView.Offset;
                                for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                                {
                                    if ((aIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var a = aChunkViewArray[aChunkViewOffset + aIndex];
                                    __op44InputRows++;
                                    __op44OutputRows++;
                                    var __op45Scope = profileRecorder?.BeginOperatorValue(__op45Handle) ?? OperatorProfileValueScope.None;
                                    try
                                    {
                                        foreach (var b in statement0RangeIndex.Find<ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, string>>?>(a.K9 == null ? default(ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, string>>?) : (a.K1, a.K2, a.K3, a.K4, a.K5, a.K6, a.K7, a.K8, a.K9), a.Amount))
                                        {
                                            if (((((bool?)((((bool?)((((((((a.K1 == b.K1) && (a.K2 == b.K2)) && (a.K3 == b.K3)) && (a.K4 == b.K4)) && (a.K5 == b.K5)) && (a.K6 == b.K6)) && (a.K7 == b.K7)) && (a.K8 == b.K8))) switch
                                            {
                                                false => (bool?)false,
                                                true => (bool?)Operators.SqlCompare<string, string>(a.K9, b.K9, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                                _ => (bool?)Operators.SqlCompare<string, string>(a.K9, b.K9, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null

                                            }))) switch
                                            {
                                                false => (bool?)false,
                                                true => (bool?)(a.Amount > b.Amount),
                                                _ => (bool?)(a.Amount > b.Amount) & (bool?)null

                                            })) == true)
                                            {
                                                statement0.Add(new Statement0Row0(a.Position, b.Position));
                                                __op47OutputRows += 1;
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        __op45Scope.Dispose();
                                    }
                                }

                                continue;
                            }

                            if (aChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionWideRangeKeyQueryTests.WideRangeInput> aChunkViewList)
                            {
                                int aChunkViewOffset = aChunkView.Offset;
                                for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                                {
                                    if ((aIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var a = aChunkViewList[aChunkViewOffset + aIndex];
                                    __op44InputRows++;
                                    __op44OutputRows++;
                                    var __op45Scope = profileRecorder?.BeginOperatorValue(__op45Handle) ?? OperatorProfileValueScope.None;
                                    try
                                    {
                                        foreach (var b in statement0RangeIndex.Find<ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, string>>?>(a.K9 == null ? default(ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, string>>?) : (a.K1, a.K2, a.K3, a.K4, a.K5, a.K6, a.K7, a.K8, a.K9), a.Amount))
                                        {
                                            if (((((bool?)((((bool?)((((((((a.K1 == b.K1) && (a.K2 == b.K2)) && (a.K3 == b.K3)) && (a.K4 == b.K4)) && (a.K5 == b.K5)) && (a.K6 == b.K6)) && (a.K7 == b.K7)) && (a.K8 == b.K8))) switch
                                            {
                                                false => (bool?)false,
                                                true => (bool?)Operators.SqlCompare<string, string>(a.K9, b.K9, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                                _ => (bool?)Operators.SqlCompare<string, string>(a.K9, b.K9, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null

                                            }))) switch
                                            {
                                                false => (bool?)false,
                                                true => (bool?)(a.Amount > b.Amount),
                                                _ => (bool?)(a.Amount > b.Amount) & (bool?)null

                                            })) == true)
                                            {
                                                statement0.Add(new Statement0Row0(a.Position, b.Position));
                                                __op47OutputRows += 1;
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        __op45Scope.Dispose();
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
                            __op44InputRows++;
                            __op44OutputRows++;
                            var __op45Scope = profileRecorder?.BeginOperatorValue(__op45Handle) ?? OperatorProfileValueScope.None;
                            try
                            {
                                foreach (var b in statement0RangeIndex.Find<ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, string>>?>(a.K9 == null ? default(ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, string>>?) : (a.K1, a.K2, a.K3, a.K4, a.K5, a.K6, a.K7, a.K8, a.K9), a.Amount))
                                {
                                    if (((((bool?)((((bool?)((((((((a.K1 == b.K1) && (a.K2 == b.K2)) && (a.K3 == b.K3)) && (a.K4 == b.K4)) && (a.K5 == b.K5)) && (a.K6 == b.K6)) && (a.K7 == b.K7)) && (a.K8 == b.K8))) switch
                                    {
                                        false => (bool?)false,
                                        true => (bool?)Operators.SqlCompare<string, string>(a.K9, b.K9, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                        _ => (bool?)Operators.SqlCompare<string, string>(a.K9, b.K9, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null

                                    }))) switch
                                    {
                                        false => (bool?)false,
                                        true => (bool?)(a.Amount > b.Amount),
                                        _ => (bool?)(a.Amount > b.Amount) & (bool?)null

                                    })) == true)
                                    {
                                        statement0.Add(new Statement0Row0(a.Position, b.Position));
                                        __op47OutputRows += 1;
                                    }
                                }
                            }
                            finally
                            {
                                __op45Scope.Dispose();
                            }
                        }
                    }
                }
                finally
                {
                    __op44Scope.AddInputRows(__op44InputRows);
                    __op44Scope.AddOutputRows(__op44OutputRows);
                    __op44Scope.Dispose();
                }

                if (__op47OutputRows > 0L)
                    profileRecorder?.AddOperatorOutputRows(__op47Handle, __op47OutputRows);
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
                FirstPosition = __value0;
                SecondPosition = __value1;
            }

            public override int Count => 2;
            public int FirstPosition { get; private set; }
            public int SecondPosition { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.FirstPosition = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.SecondPosition = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.FirstPosition),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.SecondPosition),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "FirstPosition" => true,
                "SecondPosition" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.FirstPosition, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.SecondPosition, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int FirstPosition, int SecondPosition)
            {
                this.FirstPosition = FirstPosition;
                this.SecondPosition = SecondPosition;
            }

            public int FirstPosition { get; }
            public int SecondPosition { get; }
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
