// === Parsed Query ===
/*
with recursive cycle (Id) as (select Id from values {{ Id: 1 }} seed union (Id) select (case when c.Id = 1 then 2 else 1 end) from cycle c) select Id from cycle order by Id
*/

// === Logical Plan ===
/*
Cte
  Definition [cycle]
    RecursiveCte [cycle] [Keyed: Id]
      Anchor
        MultiStatement
          Project [seed.Id as Id]
            ValuesScan [1 rows as seed]
      RecursiveMember
        MultiStatement
          Project [CASE WHEN (c.Id = 1) THEN 2 ELSE 1 END as case when c.Id = 1 then 2 else 1 end]
            CteRef [cycle as c]
  Query
    MultiStatement
      Sort [cycle.Id]
        Project [cycle.Id as Id]
          CteRef [cycle as cycle]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [cycle]
    PhysicalRecursiveCte [cycle] [Keyed: Id]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [seed.Id as Id]
            PhysicalValuesScan [1 rows as seed]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [CASE WHEN (c.Id = 1) THEN 2 ELSE 1 END as case when c.Id = 1 then 2 else 1 end]
            PhysicalCteRef [cycle as c]
  Query
    PhysicalMultiStatement
      PhysicalSort [cycle.Id]
        PhysicalProject [cycle.Id as Id]
          PhysicalCteRef [cycle as cycle]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Id: int <- field Id
    Generated [Cte0Row0]
      Id: int <- field Id
    TableRow [c]
      Id: int <- field Id
    TableRow [cycle]
      Id: int <- field Id
    Generated [ResultRow0]
      Id: int <- field Id

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    RecursiveCte [cycle; result cte0; frontiers cte0CurrentFrontier, cte0NextFrontier; identity Keyed via cte0Seen (Id); max iterations 1000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        CreateValuesRows [cte0CurrentFrontier_seedRows: seedValues0C8F87F6Row0 x 1]
        ForEach [seed in cte0CurrentFrontier_seedRows]
          RecursiveAppend [cte0CurrentFrontier <- Cte0Row0(Id: seed.Id); identity cte0Seen (Id); guard cte0.Count + cte0CurrentFrontier.Count < 10000000]
      RecursiveMember
        ForEach [c in cte0CurrentFrontier]
          RecursiveAppend [cte0NextFrontier <- Cte0Row0(Id: CASE WHEN (c.Id = 1) THEN 2 ELSE 1 END); identity cte0Seen (Id); guard cte0.Count + cte0NextFrontier.Count < 10000000]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [From]
    ForEach [cycle in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Id: cycle.Id)]
    PhaseBoundary [Select]
    SortShapeRows [result -> resultSorted by Id ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P11_RecursiveKeyedUnion_Full
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, IProfiledRunnable
    {
        private static readonly Column[] __columns_compiled_result_0 = new Column[]
        {
            new Column("Id", typeof(int), 0)
        };
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_0, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            ArgumentNullException.ThrowIfNull(profileRecorder);
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_0, (queryToken) => ComputeRows_compiled_0_Profiled(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken, profileRecorder), token);
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
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.From);
                    var cte0 = new List<Cte0Row0>();
                    var cte0CurrentFrontier = new List<Cte0Row0>();
                    var cte0NextFrontier = new List<Cte0Row0>();
                    var cte0Seen = new HashSet<int>();
                    int __cte0Iteration = 0;
                    int __cte0CancellationCounter = 0;
                    seedValues0C8F87F6Row0[] cte0CurrentFrontier_seedRows = new seedValues0C8F87F6Row0[]
                    {
                        new seedValues0C8F87F6Row0(1)
                    };
                    foreach (var seed in cte0CurrentFrontier_seedRows)
                    {
                        token.ThrowIfCancellationRequested();
                        var __cte0CurrentFrontierCandidate0 = seed.Id;
                        if (cte0Seen.Add(__cte0CurrentFrontierCandidate0))
                        {
                            if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                            {
                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("cycle", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                            }

                            cte0CurrentFrontier.Add(new Cte0Row0(__cte0CurrentFrontierCandidate0));
                        }
                    }

                    cte0.AddRange(cte0CurrentFrontier);
                    while (cte0CurrentFrontier.Count > 0)
                    {
                        if ((__cte0Iteration & 63) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        if (__cte0Iteration >= 1000)
                        {
                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("cycle", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 1000);
                        }

                        __cte0Iteration++;
                        cte0NextFrontier.Clear();
                        for (int cte0CurrentFrontierIndex = 0; cte0CurrentFrontierIndex < cte0CurrentFrontier.Count; ++cte0CurrentFrontierIndex)
                        {
                            if (cte0CurrentFrontierIndex != 0 && (cte0CurrentFrontierIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Cte0Row0 c = (Cte0Row0)cte0CurrentFrontier[cte0CurrentFrontierIndex];
                            ++__cte0CancellationCounter;
                            if ((__cte0CancellationCounter & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var __cte0NextFrontierCandidate0 = ((c.Id == 1) ? (int)2 : (int)1);
                            if (cte0Seen.Add(__cte0NextFrontierCandidate0))
                            {
                                if (cte0.Count + cte0NextFrontier.Count >= 10000000)
                                {
                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("cycle", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                }

                                cte0NextFrontier.Add(new Cte0Row0(__cte0NextFrontierCandidate0));
                            }
                        }

                        cte0.AddRange(cte0NextFrontier);
                        var __cte0FrontierSwap = cte0CurrentFrontier;
                        cte0CurrentFrontier = cte0NextFrontier;
                        cte0NextFrontier = __cte0FrontierSwap;
                    }

                    OnPhaseChanged("compiled:cte0", QueryPhase.Select);
                    _cteRowResults.Slot0 = cte0;
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
                }

                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.From);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 cycle = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(cycle.Id));
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
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
                    var __musoqExecutionState = ExecutionState.Capture(Parameters);
                    ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                    var __musoqFinalShapeRows = new List<ResultShape0>();
                    var __op14Handle = profileRecorder?.GetOperatorHandle("op14", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op15Handle = profileRecorder?.GetOperatorHandle("op15", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op16Handle = profileRecorder?.GetOperatorHandle("op16", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op17Handle = profileRecorder?.GetOperatorHandle("op17", "RecursiveCte") ?? OperatorProfileHandle.None;
                    var __op19Handle = profileRecorder?.GetOperatorHandle("op19", "CreateValuesRows") ?? OperatorProfileHandle.None;
                    var __op20Handle = profileRecorder?.GetOperatorHandle("op20", "ForEach") ?? OperatorProfileHandle.None;
                    var __op21Handle = profileRecorder?.GetOperatorHandle("op21", "RecursiveAppend") ?? OperatorProfileHandle.None;
                    var __op23Handle = profileRecorder?.GetOperatorHandle("op23", "ForEach") ?? OperatorProfileHandle.None;
                    var __op24Handle = profileRecorder?.GetOperatorHandle("op24", "RecursiveAppend") ?? OperatorProfileHandle.None;
                    var __op25Handle = profileRecorder?.GetOperatorHandle("op25", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op26Handle = profileRecorder?.GetOperatorHandle("op26", "StoreTable") ?? OperatorProfileHandle.None;
                    var __op27Handle = profileRecorder?.GetOperatorHandle("op27", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op28Handle = profileRecorder?.GetOperatorHandle("op28", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op29Handle = profileRecorder?.GetOperatorHandle("op29", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op30Handle = profileRecorder?.GetOperatorHandle("op30", "ForEach") ?? OperatorProfileHandle.None;
                    var __op31Handle = profileRecorder?.GetOperatorHandle("op31", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op32Handle = profileRecorder?.GetOperatorHandle("op32", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op33Handle = profileRecorder?.GetOperatorHandle("op33", "SortShapeRows") ?? OperatorProfileHandle.None;
                    long __op21InputRows = 0L;
                    long __op21OutputRows = 0L;
                    long __op24InputRows = 0L;
                    long __op24OutputRows = 0L;
                    long __op31OutputRows = 0L;
                    var __op14Scope = profileRecorder?.BeginOperatorValue(__op14Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op14Scope.Dispose();
                    var __op15Scope = profileRecorder?.BeginOperatorValue(__op15Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                    __op15Scope.Dispose();
                    try
                    {
                        var __op16Scope = profileRecorder?.BeginOperatorValue(__op16Handle) ?? OperatorProfileValueScope.None;
                        OnPhaseChanged("compiled:cte0", QueryPhase.From);
                        __op16Scope.Dispose();
                        long __op17InputRows = 0L;
                        long __op17OutputRows = 0L;
                        var __op17Scope = profileRecorder?.BeginOperatorValue(__op17Handle) ?? OperatorProfileValueScope.None;
                        List<Cte0Row0> cte0;
                        try
                        {
                            cte0 = new List<Cte0Row0>();
                            var cte0CurrentFrontier = new List<Cte0Row0>();
                            var cte0NextFrontier = new List<Cte0Row0>();
                            var cte0Seen = new HashSet<int>();
                            int __cte0Iteration = 0;
                            int __cte0CancellationCounter = 0;
                            var __op19Scope = profileRecorder?.BeginOperatorValue(__op19Handle) ?? OperatorProfileValueScope.None;
                            seedValues0C8F87F6Row0[] cte0CurrentFrontier_seedRows = new seedValues0C8F87F6Row0[]
                            {
                                new seedValues0C8F87F6Row0(1)
                            };
                            __op19Scope.AddOutputRows(cte0CurrentFrontier_seedRows.Length);
                            __op19Scope.Dispose();
                            long __op20InputRows = 0L;
                            long __op20OutputRows = 0L;
                            var __op20Scope = profileRecorder?.BeginOperatorValue(__op20Handle) ?? OperatorProfileValueScope.None;
                            try
                            {
                                foreach (var seed in cte0CurrentFrontier_seedRows)
                                {
                                    token.ThrowIfCancellationRequested();
                                    __op20InputRows++;
                                    __op20OutputRows++;
                                    __op21InputRows += 1;
                                    var __cte0CurrentFrontierCandidate0 = seed.Id;
                                    if (cte0Seen.Add(__cte0CurrentFrontierCandidate0))
                                    {
                                        if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                                        {
                                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("cycle", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                        }

                                        cte0CurrentFrontier.Add(new Cte0Row0(__cte0CurrentFrontierCandidate0));
                                        __op21OutputRows += 1;
                                    }
                                }
                            }
                            finally
                            {
                                __op20Scope.AddInputRows(__op20InputRows);
                                __op20Scope.AddOutputRows(__op20OutputRows);
                                __op20Scope.Dispose();
                            }

                            cte0.AddRange(cte0CurrentFrontier);
                            while (cte0CurrentFrontier.Count > 0)
                            {
                                if ((__cte0Iteration & 63) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                if (__cte0Iteration >= 1000)
                                {
                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("cycle", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 1000);
                                }

                                __cte0Iteration++;
                                cte0NextFrontier.Clear();
                                __op17InputRows += cte0CurrentFrontier.Count;
                                long __op23InputRows = 0L;
                                long __op23OutputRows = 0L;
                                var __op23Scope = profileRecorder?.BeginOperatorValue(__op23Handle) ?? OperatorProfileValueScope.None;
                                try
                                {
                                    for (int cte0CurrentFrontierIndex = 0; cte0CurrentFrontierIndex < cte0CurrentFrontier.Count; ++cte0CurrentFrontierIndex)
                                    {
                                        if (cte0CurrentFrontierIndex != 0 && (cte0CurrentFrontierIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        Cte0Row0 c = (Cte0Row0)cte0CurrentFrontier[cte0CurrentFrontierIndex];
                                        __op23InputRows++;
                                        __op23OutputRows++;
                                        __op24InputRows += 1;
                                        ++__cte0CancellationCounter;
                                        if ((__cte0CancellationCounter & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var __cte0NextFrontierCandidate0 = ((c.Id == 1) ? (int)2 : (int)1);
                                        if (cte0Seen.Add(__cte0NextFrontierCandidate0))
                                        {
                                            if (cte0.Count + cte0NextFrontier.Count >= 10000000)
                                            {
                                                throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("cycle", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                            }

                                            cte0NextFrontier.Add(new Cte0Row0(__cte0NextFrontierCandidate0));
                                            __op24OutputRows += 1;
                                        }
                                    }
                                }
                                finally
                                {
                                    __op23Scope.AddInputRows(__op23InputRows);
                                    __op23Scope.AddOutputRows(__op23OutputRows);
                                    __op23Scope.Dispose();
                                }

                                __op17OutputRows += cte0NextFrontier.Count;
                                cte0.AddRange(cte0NextFrontier);
                                var __cte0FrontierSwap = cte0CurrentFrontier;
                                cte0CurrentFrontier = cte0NextFrontier;
                                cte0NextFrontier = __cte0FrontierSwap;
                            }
                        }
                        finally
                        {
                            __op17Scope.AddInputRows(__op17InputRows);
                            __op17Scope.AddOutputRows(__op17OutputRows);
                            __op17Scope.Dispose();
                        }

                        var __op25Scope = profileRecorder?.BeginOperatorValue(__op25Handle) ?? OperatorProfileValueScope.None;
                        OnPhaseChanged("compiled:cte0", QueryPhase.Select);
                        __op25Scope.Dispose();
                        var __op26Scope = profileRecorder?.BeginOperatorValue(__op26Handle) ?? OperatorProfileValueScope.None;
                        try
                        {
                            _cteRowResults.Slot0 = cte0;
                            __op26Scope.AddOutputRows(cte0.Count);
                        }
                        finally
                        {
                            __op26Scope.Dispose();
                        }
                    }
                    finally
                    {
                        var __op27Scope = profileRecorder?.BeginOperatorValue(__op27Handle) ?? OperatorProfileValueScope.None;
                        OnPhaseChanged("compiled:cte0", QueryPhase.End);
                        __op27Scope.Dispose();
                    }

                    var __op28Scope = profileRecorder?.BeginOperatorValue(__op28Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>();
                    __op28Scope.Dispose();
                    var __op29Scope = profileRecorder?.BeginOperatorValue(__op29Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op29Scope.Dispose();
                    long __op30InputRows = 0L;
                    long __op30OutputRows = 0L;
                    var __op30Scope = profileRecorder?.BeginOperatorValue(__op30Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        var __storedTable0Rows = _cteRowResults.Slot0;
                        for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                        {
                            if ((__storedTable0Index & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Cte0Row0 cycle = __storedTable0Rows[__storedTable0Index];
                            __op30InputRows++;
                            __op30OutputRows++;
                            result.Add(new ResultShape0(cycle.Id));
                            __op31OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op30Scope.AddInputRows(__op30InputRows);
                        __op30Scope.AddOutputRows(__op30OutputRows);
                        __op30Scope.Dispose();
                    }

                    var __op32Scope = profileRecorder?.BeginOperatorValue(__op32Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op32Scope.Dispose();
                    var __op33Scope = profileRecorder?.BeginOperatorValue(__op33Handle) ?? OperatorProfileValueScope.None;
                    __op33Scope.AddInputRows(result.Count);
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

                    __op33Scope.AddOutputRows(__musoqFinalShapeRows.Count);
                    __op33Scope.Dispose();
                    if (__op21InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op21Handle, __op21InputRows);
                    if (__op21OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op21Handle, __op21OutputRows);
                    if (__op24InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op24Handle, __op24InputRows);
                    if (__op24OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op24Handle, __op24OutputRows);
                    if (__op31OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op31Handle, __op31OutputRows);
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

        private readonly struct Cte0Row0
        {
            public Cte0Row0(int Id)
            {
                this.Id = Id;
            }

            public int Id { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0)
            {
                Id = __value0;
            }

            public override int Count => 1;
            public int Id { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Id = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Id,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Id" => (object)Id,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id)
            {
                this.Id = Id;
            }

            public int Id { get; }
        }

        private sealed class seedValues0C8F87F6Row0 : Row
        {
            public seedValues0C8F87F6Row0(int __value0)
            {
                Id = __value0;
            }

            public override int Count => 1;
            public int Id { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Id = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Id,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Id" => (object)Id,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
