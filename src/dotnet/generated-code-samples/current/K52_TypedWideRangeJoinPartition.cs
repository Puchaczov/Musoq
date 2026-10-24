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
  PhysicalProject [a.Position as a.Position, a.Amount as a.Amount, a.K1 as a.K1, a.K2 as a.K2, a.K3 as a.K3, a.K4 as a.K4, a.K5 as a.K5, a.K6 as a.K6, a.K7 as a.K7, a.K8 as a.K8, a.K9 as a.K9, b.Position as b.Position, b.Amount as b.Amount, b.K1 as b.K1, b.K2 as b.K2, b.K3 as b.K3, b.K4 as b.K4, b.K5 as b.K5, b.K6 as b.K6, b.K7 as b.K7, b.K8 as b.K8, b.K9 as b.K9]
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
      a.Amount: decimal <- field a_Amount
      a.K1: int <- field a_K1
      a.K2: int <- field a_K2
      a.K3: int <- field a_K3
      a.K4: int <- field a_K4
      a.K5: int <- field a_K5
      a.K6: int <- field a_K6
      a.K7: int <- field a_K7
      a.K8: int <- field a_K8
      a.K9: string <- field a_K9
      b.Position: int <- field b_Position
      b.Amount: decimal <- field b_Amount
      b.K1: int <- field b_K1
      b.K2: int <- field b_K2
      b.K3: int <- field b_K3
      b.K4: int <- field b_K4
      b.K5: int <- field b_K5
      b.K6: int <- field b_K6
      b.K7: int <- field b_K7
      b.K8: int <- field b_K8
      b.K9: string <- field b_K9
    TableRow [ab]
      a.Position: int <- field a_Position
      a.Amount: decimal <- field a_Amount
      a.K1: int <- field a_K1
      a.K2: int <- field a_K2
      a.K3: int <- field a_K3
      a.K4: int <- field a_K4
      a.K5: int <- field a_K5
      a.K6: int <- field a_K6
      a.K7: int <- field a_K7
      a.K8: int <- field a_K8
      a.K9: string <- field a_K9
      b.Position: int <- field b_Position
      b.Amount: decimal <- field b_Amount
      b.K1: int <- field b_K1
      b.K2: int <- field b_K2
      b.K3: int <- field b_K3
      b.K4: int <- field b_K4
      b.K5: int <- field b_K5
      b.K6: int <- field b_K6
      b.K7: int <- field b_K7
      b.K8: int <- field b_K8
      b.K9: string <- field b_K9
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
        Let [k1: int = a.K1]
        Let [k11: int = b.K1]
        Let [k2: int = a.K2]
        Let [k21: int = b.K2]
        Let [k3: int = a.K3]
        Let [k31: int = b.K3]
        Let [k4: int = a.K4]
        Let [k41: int = b.K4]
        Let [k5: int = a.K5]
        Let [k51: int = b.K5]
        Let [k6: int = a.K6]
        Let [k61: int = b.K6]
        Let [k7: int = a.K7]
        Let [k71: int = b.K7]
        Let [k8: int = a.K8]
        Let [k81: int = b.K8]
        Let [k9: string = a.K9]
        Let [k91: string = b.K9]
        Let [amount: decimal = a.Amount]
        Let [amount1: decimal = b.Amount]
        If [((((((((((k1 = k11) AND (k2 = k21)) AND (k3 = k31)) AND (k4 = k41)) AND (k5 = k51)) AND (k6 = k61)) AND (k7 = k71)) AND (k8 = k81)) AND (k9 = k91)) AND (amount > amount1))]
          AppendRow [statement0 <- Statement0Row0(a.Position: a.Position, a.Amount: amount, a.K1: k1, a.K2: k2, a.K3: k3, a.K4: k4, a.K5: k5, a.K6: k6, a.K7: k7, a.K8: k8, a.K9: k9, b.Position: b.Position, b.Amount: amount1, b.K1: k11, b.K2: k21, b.K3: k31, b.K4: k41, b.K5: k51, b.K6: k61, b.K7: k71, b.K8: k81, b.K9: k91)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [ab in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(FirstPosition: ab.a.Position, SecondPosition: ab.b.Position)]
    SortShapeRows [result -> resultSorted by FirstPosition ASC, SecondPosition ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K52_TypedWideRangeJoinPartition
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("FirstPosition", typeof(int), 0),
            new Column("SecondPosition", typeof(int), 1)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("a.Position", typeof(int), 0),
            new Column("a.Amount", typeof(decimal), 1),
            new Column("a.K1", typeof(int), 2),
            new Column("a.K2", typeof(int), 3),
            new Column("a.K3", typeof(int), 4),
            new Column("a.K4", typeof(int), 5),
            new Column("a.K5", typeof(int), 6),
            new Column("a.K6", typeof(int), 7),
            new Column("a.K7", typeof(int), 8),
            new Column("a.K8", typeof(int), 9),
            new Column("a.K9", typeof(string), 10),
            new Column("b.Position", typeof(int), 11),
            new Column("b.Amount", typeof(decimal), 12),
            new Column("b.K1", typeof(int), 13),
            new Column("b.K2", typeof(int), 14),
            new Column("b.K3", typeof(int), 15),
            new Column("b.K4", typeof(int), 16),
            new Column("b.K5", typeof(int), 17),
            new Column("b.K6", typeof(int), 18),
            new Column("b.K7", typeof(int), 19),
            new Column("b.K8", typeof(int), 20),
            new Column("b.K9", typeof(string), 21)
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

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
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
                    var comparison = left.FirstPosition.CompareTo(right.FirstPosition);
                    if (comparison != 0)
                        return comparison;
                    comparison = left.SecondPosition.CompareTo(right.SecondPosition);
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
                                int k1 = a.K1;
                                int k11 = b.K1;
                                int k2 = a.K2;
                                int k21 = b.K2;
                                int k3 = a.K3;
                                int k31 = b.K3;
                                int k4 = a.K4;
                                int k41 = b.K4;
                                int k5 = a.K5;
                                int k51 = b.K5;
                                int k6 = a.K6;
                                int k61 = b.K6;
                                int k7 = a.K7;
                                int k71 = b.K7;
                                int k8 = a.K8;
                                int k81 = b.K8;
                                string k9 = a.K9;
                                string k91 = b.K9;
                                decimal amount = a.Amount;
                                decimal amount1 = b.Amount;
                                if (((((bool?)((((bool?)((((((((k1 == k11) && (k2 == k21)) && (k3 == k31)) && (k4 == k41)) && (k5 == k51)) && (k6 == k61)) && (k7 == k71)) && (k8 == k81))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<string, string>(k9, k91, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(k9, k91, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null

                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)(amount > amount1),
                                    _ => (bool?)(amount > amount1) & (bool?)null

                                })) == true)
                                {
                                    statement0.Add(new Statement0Row0(a.Position, amount, k1, k2, k3, k4, k5, k6, k7, k8, k9, b.Position, amount1, k11, k21, k31, k41, k51, k61, k71, k81, k91));
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
                                int k1 = a.K1;
                                int k11 = b.K1;
                                int k2 = a.K2;
                                int k21 = b.K2;
                                int k3 = a.K3;
                                int k31 = b.K3;
                                int k4 = a.K4;
                                int k41 = b.K4;
                                int k5 = a.K5;
                                int k51 = b.K5;
                                int k6 = a.K6;
                                int k61 = b.K6;
                                int k7 = a.K7;
                                int k71 = b.K7;
                                int k8 = a.K8;
                                int k81 = b.K8;
                                string k9 = a.K9;
                                string k91 = b.K9;
                                decimal amount = a.Amount;
                                decimal amount1 = b.Amount;
                                if (((((bool?)((((bool?)((((((((k1 == k11) && (k2 == k21)) && (k3 == k31)) && (k4 == k41)) && (k5 == k51)) && (k6 == k61)) && (k7 == k71)) && (k8 == k81))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<string, string>(k9, k91, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(k9, k91, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null

                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)(amount > amount1),
                                    _ => (bool?)(amount > amount1) & (bool?)null

                                })) == true)
                                {
                                    statement0.Add(new Statement0Row0(a.Position, amount, k1, k2, k3, k4, k5, k6, k7, k8, k9, b.Position, amount1, k11, k21, k31, k41, k51, k61, k71, k81, k91));
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
                        int k1 = a.K1;
                        int k11 = b.K1;
                        int k2 = a.K2;
                        int k21 = b.K2;
                        int k3 = a.K3;
                        int k31 = b.K3;
                        int k4 = a.K4;
                        int k41 = b.K4;
                        int k5 = a.K5;
                        int k51 = b.K5;
                        int k6 = a.K6;
                        int k61 = b.K6;
                        int k7 = a.K7;
                        int k71 = b.K7;
                        int k8 = a.K8;
                        int k81 = b.K8;
                        string k9 = a.K9;
                        string k91 = b.K9;
                        decimal amount = a.Amount;
                        decimal amount1 = b.Amount;
                        if (((((bool?)((((bool?)((((((((k1 == k11) && (k2 == k21)) && (k3 == k31)) && (k4 == k41)) && (k5 == k51)) && (k6 == k61)) && (k7 == k71)) && (k8 == k81))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)Operators.SqlCompare<string, string>(k9, k91, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                            _ => (bool?)Operators.SqlCompare<string, string>(k9, k91, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null

                        }))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)(amount > amount1),
                            _ => (bool?)(amount > amount1) & (bool?)null

                        })) == true)
                        {
                            statement0.Add(new Statement0Row0(a.Position, amount, k1, k2, k3, k4, k5, k6, k7, k8, k9, b.Position, amount1, k11, k21, k31, k41, k51, k61, k71, k81, k91));
                        }
                    }
                }
            }

            return statement0;
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
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(int __value0, decimal __value1, int __value2, int __value3, int __value4, int __value5, int __value6, int __value7, int __value8, int __value9, string __value10, int __value11, decimal __value12, int __value13, int __value14, int __value15, int __value16, int __value17, int __value18, int __value19, int __value20, string __value21)
            {
                a_Position = __value0;
                a_Amount = __value1;
                a_K1 = __value2;
                a_K2 = __value3;
                a_K3 = __value4;
                a_K4 = __value5;
                a_K5 = __value6;
                a_K6 = __value7;
                a_K7 = __value8;
                a_K8 = __value9;
                a_K9 = __value10;
                b_Position = __value11;
                b_Amount = __value12;
                b_K1 = __value13;
                b_K2 = __value14;
                b_K3 = __value15;
                b_K4 = __value16;
                b_K5 = __value17;
                b_K6 = __value18;
                b_K7 = __value19;
                b_K8 = __value20;
                b_K9 = __value21;
            }

            public decimal a_Amount { get; }
            public int a_K1 { get; }
            public int a_K2 { get; }
            public int a_K3 { get; }
            public int a_K4 { get; }
            public int a_K5 { get; }
            public int a_K6 { get; }
            public int a_K7 { get; }
            public int a_K8 { get; }
            public string a_K9 { get; }
            public int a_Position { get; }
            public decimal b_Amount { get; }
            public int b_K1 { get; }
            public int b_K2 { get; }
            public int b_K3 { get; }
            public int b_K4 { get; }
            public int b_K5 { get; }
            public int b_K6 { get; }
            public int b_K7 { get; }
            public int b_K8 { get; }
            public string b_K9 { get; }
            public int b_Position { get; }
        }
    }
}
