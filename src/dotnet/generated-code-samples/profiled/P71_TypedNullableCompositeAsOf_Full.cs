// === Parsed Query ===
/*
select a.Position as LeftPosition, b.Position as RightPosition from #schema.first() a asof left join #schema.first() b on a.Partition = b.Partition and a.Label = b.Label and a.Key >= b.Key tie break by b.Position desc order by LeftPosition
*/

// === Logical Plan ===
/*
MultiStatement
  Project [a.Position as a.Position, a.Partition as a.Partition, a.Label as a.Label, a.Key as a.Key, b.Position as b.Position, b.Partition as b.Partition, b.Label as b.Label, b.Key as b.Key]
    Join [AsofLeft] [(((a.Partition = b.Partition) AND (a.Label = b.Label)) AND (a.Key >= b.Key))] [tie: b.Position DESC]
      SchemaScan [#schema.first() as a]
      SchemaScan [#schema.first() as b]
  Sort [a.Position]
    Project [a.Position as LeftPosition, b.Position as RightPosition]
      CteRef [ab as ab]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.Position as a.Position, a.Partition as a.Partition, a.Label as a.Label, a.Key as a.Key, b.Position as b.Position, b.Partition as b.Partition, b.Label as b.Label, b.Key as b.Key]
    PhysicalNestedLoopJoin [AsofLeft] [(((a.Partition = b.Partition) AND (a.Label = b.Label)) AND (a.Key >= b.Key))] [tie: b.Position DESC]
      PhysicalSchemaScan [#schema.first() as a]
      PhysicalSchemaScan [#schema.first() as b]
  PhysicalSort [a.Position]
    PhysicalProject [a.Position as LeftPosition, b.Position as RightPosition]
      PhysicalCteRef [ab as ab]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: AsOfInput]
      Position: int <- property Position
      Partition: int? <- property Partition
      Label: string <- property Label
      Key: decimal? <- property Key
    SourceEntity [b: AsOfInput]
      Position: int <- property Position
      Partition: int? <- property Partition
      Label: string <- property Label
      Key: decimal? <- property Key
    Generated [Statement0Row0]
      a.Position: int <- field a_Position
      a.Partition: int? <- field a_Partition
      a.Label: string <- field a_Label
      a.Key: decimal? <- field a_Key
      b.Position: int? <- field b_Position
      b.Partition: int? <- field b_Partition
      b.Label: string <- field b_Label
      b.Key: decimal? <- field b_Key
    TableRow [ab]
      a.Position: int <- field a_Position
      a.Partition: int? <- field a_Partition
      a.Label: string <- field a_Label
      a.Key: decimal? <- field a_Key
      b.Position: int? <- field b_Position
      b.Partition: int? <- field b_Partition
      b.Label: string <- field b_Label
      b.Key: decimal? <- field b_Key
    Generated [ResultRow0]
      LeftPosition: int <- field LeftPosition
      RightPosition: int? <- field RightPosition

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [a: AsOfInput] -> statement0_aRows
    SourceScan [b: AsOfInput] -> statement0_bRows
    CreateTable [statement0: Statement0Row0]
    CreateAsOfIndex [statement0AsOfIndex <- statement0_bRows by bCandidate.Partition, bCandidate.Label, bCandidate.Key; tie bCandidate.Position DESC]
    PhaseBoundary [Select]
    ChunkedForEach [a in statement0_aRows]
      AsOfProbe [b <- statement0_bRows using statement0AsOfIndex where a.Partition = bCandidate.Partition and a.Label = bCandidate.Label and a.Key >= bCandidate.Key tie break by bCandidate.Position DESC]
        AppendRow [statement0 <- Statement0Row0(a.Position: a.Position, a.Partition: a.Partition, a.Label: a.Label, a.Key: a.Key, b.Position: b.Position, b.Partition: b.Partition, b.Label: b.Label, b.Key: b.Key)]
      AsOfProbeNoMatch
        AppendRow [statement0 <- Statement0Row0(a.Position: a.Position, a.Partition: a.Partition, a.Label: a.Label, a.Key: a.Key, b.Position: NULL, b.Partition: NULL, b.Label: NULL, b.Key: NULL)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [ab in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(LeftPosition: ab.a.Position, RightPosition: ab.b.Position)]
    SortShapeRows [result -> resultSorted by LeftPosition ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P71_TypedNullableCompositeAsOf_Full
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
            new Column("LeftPosition", typeof(int), 0),
            new Column("RightPosition", typeof(int?), 1)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("a.Position", typeof(int), 0),
            new Column("a.Partition", typeof(int?), 1),
            new Column("a.Label", typeof(string), 2),
            new Column("a.Key", typeof(decimal?), 3),
            new Column("b.Position", typeof(int?), 4),
            new Column("b.Partition", typeof(int?), 5),
            new Column("b.Label", typeof(string), 6),
            new Column("b.Key", typeof(decimal?), 7)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Partition", typeof(int?), 1), new Column("Label", typeof(string), 2), new Column("Key", typeof(decimal?), 3) });
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
                var __musoqFinalShapeRows = new List<ResultShape0>();
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
                    result.Add(new ResultShape0(ab.a_Position, ab.b_Position));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.LeftPosition.CompareTo(right.LeftPosition);
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
                    var __op35Handle = profileRecorder?.GetOperatorHandle("op35", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op36Handle = profileRecorder?.GetOperatorHandle("op36", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op41Handle = profileRecorder?.GetOperatorHandle("op41", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op48Handle = profileRecorder?.GetOperatorHandle("op48", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op49Handle = profileRecorder?.GetOperatorHandle("op49", "ForEach") ?? OperatorProfileHandle.None;
                    var __op50Handle = profileRecorder?.GetOperatorHandle("op50", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op51Handle = profileRecorder?.GetOperatorHandle("op51", "SortShapeRows") ?? OperatorProfileHandle.None;
                    long __op50OutputRows = 0L;
                    var __op35Scope = profileRecorder?.BeginOperatorValue(__op35Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op35Scope.Dispose();
                    var __op36Scope = profileRecorder?.BeginOperatorValue(__op36Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op36Scope.Dispose();
                    var __op41Scope = profileRecorder?.BeginOperatorValue(__op41Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op41Scope.Dispose();
                    _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, profileRecorder, _cteRowResults);
                    var __op48Scope = profileRecorder?.BeginOperatorValue(__op48Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>();
                    __op48Scope.Dispose();
                    long __op49InputRows = 0L;
                    long __op49OutputRows = 0L;
                    var __op49Scope = profileRecorder?.BeginOperatorValue(__op49Handle) ?? OperatorProfileValueScope.None;
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
                            __op49InputRows++;
                            __op49OutputRows++;
                            result.Add(new ResultShape0(ab.a_Position, ab.b_Position));
                            __op50OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op49Scope.AddInputRows(__op49InputRows);
                        __op49Scope.AddOutputRows(__op49OutputRows);
                        __op49Scope.Dispose();
                    }

                    var __op51Scope = profileRecorder?.BeginOperatorValue(__op51Handle) ?? OperatorProfileValueScope.None;
                    __op51Scope.AddInputRows(result.Count);
                    var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                    {
                        var comparison = left.LeftPosition.CompareTo(right.LeftPosition);
                        if (comparison != 0)
                            return comparison;
                        return 0;
                    }));
                    foreach (var resultSortedRowsRow in resultSortedRows)
                    {
                        __musoqFinalShapeRows.Add(resultSortedRowsRow);
                    }

                    __op51Scope.AddOutputRows(__musoqFinalShapeRows.Count);
                    __op51Scope.Dispose();
                    if (__op50OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op50Handle, __op50OutputRows);
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
            var statement0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput>.Open(__statement0_aSchema, "first", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks;
            var __statement0_bSchema = provider.GetSchema("#schema");
            var statement0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput>.Open(__statement0_bSchema, "first", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            var statement0AsOfIndex = EvaluationHelper.CreateAsOfIndex<Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput, decimal?, ValueTuple<int, string>, int?>(statement0_bRows, (bCandidate) =>
            {
                var __asOfEqualityPart0 = bCandidate.Partition;
                var __asOfEqualityPart1 = bCandidate.Label;
                var __asOfEquality = new NativeEqualityKey<ValueTuple<int, string>>(true && __asOfEqualityPart0 != null && __asOfEqualityPart1 != null, true && __asOfEqualityPart0 != null && __asOfEqualityPart1 != null ? ((int)__asOfEqualityPart0.GetValueOrDefault(), (string)__asOfEqualityPart1) : default(ValueTuple<int, string>));
                return __asOfEquality;
            }, (bCandidate) => bCandidate.Key, Musoq.Evaluator.IR.Expressions.BinaryOpKind.GreaterOrEqual, (bCandidate) => bCandidate.Position, true, Musoq.Evaluator.IR.Bindings.NullOrdering.Default);
            foreach (var aChunk in statement0_aRows)
            {
                if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput> aChunkView)
                {
                    if (aChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput[] aChunkViewArray)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewArray[aChunkViewOffset + aIndex];
                            {
                                var bEqualityPart0 = a.Partition;
                                var bEqualityPart1 = a.Label;
                                var bEquality = new NativeEqualityKey<ValueTuple<int, string>>(true && bEqualityPart0 != null && bEqualityPart1 != null, true && bEqualityPart0 != null && bEqualityPart1 != null ? ((int)bEqualityPart0.GetValueOrDefault(), (string)bEqualityPart1) : default(ValueTuple<int, string>));
                                var b = statement0AsOfIndex.Find(bEquality, a.Key);
                                if (b != null)
                                {
                                    statement0.Add(new Statement0Row0(a.Position, a.Partition, a.Label, a.Key, b.Position, b.Partition, b.Label, b.Key));
                                }
                                else
                                {
                                    statement0.Add(new Statement0Row0(a.Position, a.Partition, a.Label, a.Key, null, null, null, null));
                                }
                            }
                        }

                        continue;
                    }

                    if (aChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput> aChunkViewList)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewList[aChunkViewOffset + aIndex];
                            {
                                var bEqualityPart0 = a.Partition;
                                var bEqualityPart1 = a.Label;
                                var bEquality = new NativeEqualityKey<ValueTuple<int, string>>(true && bEqualityPart0 != null && bEqualityPart1 != null, true && bEqualityPart0 != null && bEqualityPart1 != null ? ((int)bEqualityPart0.GetValueOrDefault(), (string)bEqualityPart1) : default(ValueTuple<int, string>));
                                var b = statement0AsOfIndex.Find(bEquality, a.Key);
                                if (b != null)
                                {
                                    statement0.Add(new Statement0Row0(a.Position, a.Partition, a.Label, a.Key, b.Position, b.Partition, b.Label, b.Key));
                                }
                                else
                                {
                                    statement0.Add(new Statement0Row0(a.Position, a.Partition, a.Label, a.Key, null, null, null, null));
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
                    {
                        var bEqualityPart0 = a.Partition;
                        var bEqualityPart1 = a.Label;
                        var bEquality = new NativeEqualityKey<ValueTuple<int, string>>(true && bEqualityPart0 != null && bEqualityPart1 != null, true && bEqualityPart0 != null && bEqualityPart1 != null ? ((int)bEqualityPart0.GetValueOrDefault(), (string)bEqualityPart1) : default(ValueTuple<int, string>));
                        var b = statement0AsOfIndex.Find(bEquality, a.Key);
                        if (b != null)
                        {
                            statement0.Add(new Statement0Row0(a.Position, a.Partition, a.Label, a.Key, b.Position, b.Partition, b.Label, b.Key));
                        }
                        else
                        {
                            statement0.Add(new Statement0Row0(a.Position, a.Partition, a.Label, a.Key, null, null, null, null));
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
                var __op37Handle = profileRecorder?.GetOperatorHandle("op37", "SourceScan") ?? OperatorProfileHandle.None;
                var __op38Handle = profileRecorder?.GetOperatorHandle("op38", "SourceScan") ?? OperatorProfileHandle.None;
                var __op39Handle = profileRecorder?.GetOperatorHandle("op39", "CreateTable") ?? OperatorProfileHandle.None;
                var __op40Handle = profileRecorder?.GetOperatorHandle("op40", "CreateAsOfIndex") ?? OperatorProfileHandle.None;
                var __op42Handle = profileRecorder?.GetOperatorHandle("op42", "ChunkedForEach") ?? OperatorProfileHandle.None;
                var __op43Handle = profileRecorder?.GetOperatorHandle("op43", "AsOfProbe") ?? OperatorProfileHandle.None;
                var __op44Handle = profileRecorder?.GetOperatorHandle("op44", "AppendRow") ?? OperatorProfileHandle.None;
                var __op46Handle = profileRecorder?.GetOperatorHandle("op46", "AppendRow") ?? OperatorProfileHandle.None;
                long __op44OutputRows = 0L;
                long __op46OutputRows = 0L;
                var __op37Scope = profileRecorder?.BeginOperatorValue(__op37Handle) ?? OperatorProfileValueScope.None;
                var __statement0_aSchema = provider.GetSchema("#schema");
                var statement0_aRowsProfile = profileRecorder?.CreateSourceRecorder("a");
                var statement0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput>.Open(__statement0_aSchema, "first", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress, statement0_aRowsProfile == null ? SourceDiagnostics.None : statement0_aRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                var statement0_aRows = statement0_aRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : statement0_aRowsSource.Chunks, statement0_aRowsProfile);
                __op37Scope.Dispose();
                var __op38Scope = profileRecorder?.BeginOperatorValue(__op38Handle) ?? OperatorProfileValueScope.None;
                var __statement0_bSchema = provider.GetSchema("#schema");
                var statement0_bRowsProfile = profileRecorder?.CreateSourceRecorder("b");
                var statement0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput>.Open(__statement0_bSchema, "first", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress, statement0_bRowsProfile == null ? SourceDiagnostics.None : statement0_bRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                var statement0_bRows = statement0_bRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput>(statement0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : statement0_bRowsSource.Chunks, statement0_bRowsProfile);
                __op38Scope.Dispose();
                var __op39Scope = profileRecorder?.BeginOperatorValue(__op39Handle) ?? OperatorProfileValueScope.None;
                var statement0 = new List<Statement0Row0>();
                __op39Scope.Dispose();
                var __op40Scope = profileRecorder?.BeginOperatorValue(__op40Handle) ?? OperatorProfileValueScope.None;
                var statement0AsOfIndex = EvaluationHelper.CreateAsOfIndex<Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput, decimal?, ValueTuple<int, string>, int?>(statement0_bRows, (bCandidate) =>
                {
                    var __asOfEqualityPart0 = bCandidate.Partition;
                    var __asOfEqualityPart1 = bCandidate.Label;
                    var __asOfEquality = new NativeEqualityKey<ValueTuple<int, string>>(true && __asOfEqualityPart0 != null && __asOfEqualityPart1 != null, true && __asOfEqualityPart0 != null && __asOfEqualityPart1 != null ? ((int)__asOfEqualityPart0.GetValueOrDefault(), (string)__asOfEqualityPart1) : default(ValueTuple<int, string>));
                    return __asOfEquality;
                }, (bCandidate) => bCandidate.Key, Musoq.Evaluator.IR.Expressions.BinaryOpKind.GreaterOrEqual, (bCandidate) => bCandidate.Position, true, Musoq.Evaluator.IR.Bindings.NullOrdering.Default);
                __op40Scope.Dispose();
                long __op42InputRows = 0L;
                long __op42OutputRows = 0L;
                var __op42Scope = profileRecorder?.BeginOperatorValue(__op42Handle) ?? OperatorProfileValueScope.None;
                try
                {
                    foreach (var aChunk in statement0_aRows)
                    {
                        if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput> aChunkView)
                        {
                            if (aChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput[] aChunkViewArray)
                            {
                                int aChunkViewOffset = aChunkView.Offset;
                                for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                                {
                                    if ((aIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var a = aChunkViewArray[aChunkViewOffset + aIndex];
                                    __op42InputRows++;
                                    __op42OutputRows++;
                                    var __op43Scope = profileRecorder?.BeginOperatorValue(__op43Handle) ?? OperatorProfileValueScope.None;
                                    try
                                    {
                                        {
                                            var bEqualityPart0 = a.Partition;
                                            var bEqualityPart1 = a.Label;
                                            var bEquality = new NativeEqualityKey<ValueTuple<int, string>>(true && bEqualityPart0 != null && bEqualityPart1 != null, true && bEqualityPart0 != null && bEqualityPart1 != null ? ((int)bEqualityPart0.GetValueOrDefault(), (string)bEqualityPart1) : default(ValueTuple<int, string>));
                                            var b = statement0AsOfIndex.Find(bEquality, a.Key);
                                            if (b != null)
                                            {
                                                statement0.Add(new Statement0Row0(a.Position, a.Partition, a.Label, a.Key, b.Position, b.Partition, b.Label, b.Key));
                                                __op44OutputRows += 1;
                                            }
                                            else
                                            {
                                                statement0.Add(new Statement0Row0(a.Position, a.Partition, a.Label, a.Key, null, null, null, null));
                                                __op46OutputRows += 1;
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        __op43Scope.Dispose();
                                    }
                                }

                                continue;
                            }

                            if (aChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionAsOfQueryTests.AsOfInput> aChunkViewList)
                            {
                                int aChunkViewOffset = aChunkView.Offset;
                                for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                                {
                                    if ((aIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var a = aChunkViewList[aChunkViewOffset + aIndex];
                                    __op42InputRows++;
                                    __op42OutputRows++;
                                    var __op43Scope = profileRecorder?.BeginOperatorValue(__op43Handle) ?? OperatorProfileValueScope.None;
                                    try
                                    {
                                        {
                                            var bEqualityPart0 = a.Partition;
                                            var bEqualityPart1 = a.Label;
                                            var bEquality = new NativeEqualityKey<ValueTuple<int, string>>(true && bEqualityPart0 != null && bEqualityPart1 != null, true && bEqualityPart0 != null && bEqualityPart1 != null ? ((int)bEqualityPart0.GetValueOrDefault(), (string)bEqualityPart1) : default(ValueTuple<int, string>));
                                            var b = statement0AsOfIndex.Find(bEquality, a.Key);
                                            if (b != null)
                                            {
                                                statement0.Add(new Statement0Row0(a.Position, a.Partition, a.Label, a.Key, b.Position, b.Partition, b.Label, b.Key));
                                                __op44OutputRows += 1;
                                            }
                                            else
                                            {
                                                statement0.Add(new Statement0Row0(a.Position, a.Partition, a.Label, a.Key, null, null, null, null));
                                                __op46OutputRows += 1;
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        __op43Scope.Dispose();
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
                            __op42InputRows++;
                            __op42OutputRows++;
                            var __op43Scope = profileRecorder?.BeginOperatorValue(__op43Handle) ?? OperatorProfileValueScope.None;
                            try
                            {
                                {
                                    var bEqualityPart0 = a.Partition;
                                    var bEqualityPart1 = a.Label;
                                    var bEquality = new NativeEqualityKey<ValueTuple<int, string>>(true && bEqualityPart0 != null && bEqualityPart1 != null, true && bEqualityPart0 != null && bEqualityPart1 != null ? ((int)bEqualityPart0.GetValueOrDefault(), (string)bEqualityPart1) : default(ValueTuple<int, string>));
                                    var b = statement0AsOfIndex.Find(bEquality, a.Key);
                                    if (b != null)
                                    {
                                        statement0.Add(new Statement0Row0(a.Position, a.Partition, a.Label, a.Key, b.Position, b.Partition, b.Label, b.Key));
                                        __op44OutputRows += 1;
                                    }
                                    else
                                    {
                                        statement0.Add(new Statement0Row0(a.Position, a.Partition, a.Label, a.Key, null, null, null, null));
                                        __op46OutputRows += 1;
                                    }
                                }
                            }
                            finally
                            {
                                __op43Scope.Dispose();
                            }
                        }
                    }
                }
                finally
                {
                    __op42Scope.AddInputRows(__op42InputRows);
                    __op42Scope.AddOutputRows(__op42OutputRows);
                    __op42Scope.Dispose();
                }

                if (__op44OutputRows > 0L)
                    profileRecorder?.AddOperatorOutputRows(__op44Handle, __op44OutputRows);
                if (__op46OutputRows > 0L)
                    profileRecorder?.AddOperatorOutputRows(__op46Handle, __op46OutputRows);
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
            public ResultRow0(int __value0, int? __value1)
            {
                LeftPosition = __value0;
                RightPosition = __value1;
            }

            public override int Count => 2;
            public int LeftPosition { get; private set; }
            public int? RightPosition { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.LeftPosition = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.RightPosition = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.LeftPosition),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.RightPosition),
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
            public ResultShape0(int LeftPosition, int? RightPosition)
            {
                this.LeftPosition = LeftPosition;
                this.RightPosition = RightPosition;
            }

            public int LeftPosition { get; }
            public int? RightPosition { get; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(int __value0, int? __value1, string __value2, decimal? __value3, int? __value4, int? __value5, string __value6, decimal? __value7)
            {
                a_Position = __value0;
                a_Partition = __value1;
                a_Label = __value2;
                a_Key = __value3;
                b_Position = __value4;
                b_Partition = __value5;
                b_Label = __value6;
                b_Key = __value7;
            }

            public decimal? a_Key { get; }
            public string a_Label { get; }
            public int? a_Partition { get; }
            public int a_Position { get; }
            public decimal? b_Key { get; }
            public string b_Label { get; }
            public int? b_Partition { get; }
            public int? b_Position { get; }
        }
    }
}
