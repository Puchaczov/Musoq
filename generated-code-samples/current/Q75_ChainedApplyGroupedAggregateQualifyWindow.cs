// === Parsed Query ===
/*
select i.Name as Name, Avg(n.Value) as ValueAvg, Min(n.Value) as ValueMin, Max(n.Value) as ValueMax from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m group by i.Name having Max(n.Value) >= 2 qualify RowNumber() over (order by Avg(n.Value) desc, Min(n.Value), Max(n.Value) desc) <= 1 order by Name
*/

// === Logical Plan ===
/*
MultiStatement
  Project [i.Name as i.Name, AggRef(inm.Max(n.Value)) as inm.Max(n.Value), AggRef(inm.Min(n.Value)) as inm.Min(n.Value), AggRef(inm.Avg(n.Value)) as inm.Avg(n.Value)]
    Having [(AggRef(inm.Max(n.Value)) >= 2)]
      Aggregate [keys: i.Name] [aggs: Max(Value), Min(Value), Avg(Value)]
        Apply [Cross]
          Apply [Cross]
            SchemaScan [#apply.items() as i]
            PropertySource [i.Numbers as n] [apply: Cross] [type: Int32[]]
          PropertySource [i.Numbers as m] [apply: Cross] [type: Int32[]]
  Sort [i.Name]
    Project [i.Name as Name, inm.Avg(n.Value) as ValueAvg, inm.Min(n.Value) as ValueMin, inm.Max(n.Value) as ValueMax]
      Qualify [(WindowRef(0) <= 1)]
        Window [RowNumber(idx:0; order: Avg(inm.Avg(n.Value)) DESC, Min(inm.Min(n.Value)), Max(inm.Max(n.Value)) DESC)]
          CteRef [inmScore as inmScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [i.Name as i.Name, AggRef(inm.Max(n.Value)) as inm.Max(n.Value), AggRef(inm.Min(n.Value)) as inm.Min(n.Value), AggRef(inm.Avg(n.Value)) as inm.Avg(n.Value)]
    PhysicalHaving [(AggRef(inm.Max(n.Value)) >= 2)]
      PhysicalSingleKeyAggregate [key: i.Name (String)] [aggs: Max(Value), Min(Value), Avg(Value)]
        PhysicalNestedLoopApply [Cross]
          PhysicalNestedLoopApply [Cross]
            PhysicalSchemaScan [#apply.items() as i]
            PhysicalPropertySource [i.Numbers as n] [apply: Cross] [type: Int32[]]
          PhysicalPropertySource [i.Numbers as m] [apply: Cross] [type: Int32[]]
  PhysicalSort [i.Name]
    PhysicalProject [i.Name as Name, inm.Avg(n.Value) as ValueAvg, inm.Min(n.Value) as ValueMin, inm.Max(n.Value) as ValueMax]
      PhysicalQualify [(WindowRef(0) <= 1)]
        PhysicalWindow [RowNumber(idx:0; order: Avg(inm.Avg(n.Value)) DESC, Min(inm.Min(n.Value)), Max(inm.Max(n.Value)) DESC)]
          PhysicalMaterialize
            PhysicalCteRef [inmScore as inmScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [i: GeneratedApplySampleEntity]
      Name: string <- property Name
      Numbers: int[] <- property Numbers
    SourceEntity [n: int]
      Value: int <- direct scalar value
    SourceEntity [m: int]
      Value: int <- direct scalar value
    AggregateGroup [Statement0AggregateGroup; keys: 1; typed aggs: 3]
    Generated [Statement0Row0]
      i.Name: string <- field i_Name
      inm.Max(n.Value): int? <- field inm_Max_n_Value_
      inm.Min(n.Value): int? <- field inm_Min_n_Value_
      inm.Avg(n.Value): int? <- field inm_Avg_n_Value_
    TableRow [inmScore]
      i.Name: string <- field i_Name
      inm.Max(n.Value): int? <- field inm_Max_n_Value_
      inm.Min(n.Value): int? <- field inm_Min_n_Value_
      inm.Avg(n.Value): int? <- field inm_Avg_n_Value_
    Generated [ResultRow0]
      Name: string <- field Name
      ValueAvg: int? <- field ValueAvg
      ValueMin: int? <- field ValueMin
      ValueMax: int? <- field ValueMax

  Body
    SourceScan [i: GeneratedApplySampleEntity] -> statement0_iRows
    CreateTable [statement0: Statement0Row0]
    CreateSingleKeyAggregateContext [statement0Groups: string -> Statement0AggregateGroup]
    ChunkedForEach [i in statement0_iRows]
      EnumerableSource [i.Numbers -> statement0_nRows]
      ChunkedForEach [n in statement0_nRows]
        EnumerableSource [i.Numbers -> statement0_mRows]
        ChunkedForEach [m in statement0_mRows]
          Let [value: int = n.Value]
          GetOrAddSingleKeyAggregateGroup [statement0Group = statement0Groups[i.Name] by i.Name; typed: Statement0AggregateGroup]
          TypedAggregateSet [Set(statement0Group.__agg0, value)]
          TypedAggregateSet [Set(statement0Group.__agg1, value)]
          TypedAggregateSet [Set(statement0Group.__agg2, value)]
    EnsureCapacity [statement0 <- statement0GroupsToFinalize.Count]
    ForEach [statement0FinalGroup in statement0GroupsToFinalize]
      If [(inm.Max(n.Value) >= 2)]
        AppendRow [statement0 <- Statement0Row0(i.Name: statement0FinalGroup.i.Name, inm.Max(n.Value): inm.Max(n.Value), inm.Min(n.Value): inm.Min(n.Value), inm.Avg(n.Value): inm.Avg(n.Value))]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    Materialize [_cteRowResults.Slot0 -> resultWindowRows]
    ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows order by inmScore.inm.Avg(n.Value) DESC, inmScore.inm.Min(n.Value) ASC, inmScore.inm.Max(n.Value) DESC qualify <= 1]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, inmScore in resultWindowRows]
      If [((resultRowNumbers[windowIndex] > 0) AND (resultRowNumbers[windowIndex] <= 1))]
        AppendShape [result <- ResultShape0(Name: inmScore.i.Name, ValueAvg: inmScore.inm.Avg(n.Value), ValueMin: inmScore.inm.Min(n.Value), ValueMax: inmScore.inm.Max(n.Value))]
    SortShapeRows [result -> resultSorted by Name ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q75_ChainedApplyGroupedAggregateQualifyWindow
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("ValueAvg", typeof(int?), 1),
            new Column("ValueMin", typeof(int?), 2),
            new Column("ValueMax", typeof(int?), 3)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("i.Name", typeof(string), 0),
            new Column("inm.Max(n.Value)", typeof(int?), 1),
            new Column("inm.Min(n.Value)", typeof(int?), 2),
            new Column("inm.Avg(n.Value)", typeof(int?), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_i_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("Numbers", typeof(int[]), 1) });
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
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.ValueAvg, __musoqShapeRow.ValueMin, __musoqShapeRow.ValueMax);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.Where);
            OnPhaseChanged("compiled", QueryPhase.GroupBy);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults);
                var resultWindowRows = EvaluationHelper.MaterializeGeneratedRows<Statement0Row0>(_cteRowResults.Slot0);
                var resultRowNumbersOrderKeys = new WindowResultRowNumbersOrderKeysKey[resultWindowRows.Count];
                ExtractResultRowNumbersWindowKeys(resultWindowRows, resultRowNumbersOrderKeys);
                var resultRowNumbersPartitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count, null);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultRowNumbersPartitions, resultRowNumbersOrderKeys, false);
                var resultRowNumbers = new long[resultWindowRows.Count];
                for (int resultRowNumbersPartitionSetIndex = 0; resultRowNumbersPartitionSetIndex < resultRowNumbersPartitions.PartitionCount; ++resultRowNumbersPartitionSetIndex)
                {
                    var resultRowNumbersPartitionStart = resultRowNumbersPartitions.GetStart(resultRowNumbersPartitionSetIndex);
                    var resultRowNumbersPartitionCount = resultRowNumbersPartitions.GetLength(resultRowNumbersPartitionSetIndex);
                    var resultRowNumbersPartitionIndices = resultRowNumbersPartitions.Indices;
                    var resultRowNumbersPartitionLimit = (int)System.Math.Min((long)resultRowNumbersPartitionCount, 1L);
                    for (int resultRowNumbersPartitionIndex = 0; resultRowNumbersPartitionIndex < resultRowNumbersPartitionLimit; ++resultRowNumbersPartitionIndex)
                    {
                        var resultRowNumbersCurrentIndex = resultRowNumbersPartitionIndices[resultRowNumbersPartitionStart + resultRowNumbersPartitionIndex];
                        resultRowNumbers[resultRowNumbersCurrentIndex] = resultRowNumbersPartitionIndex + 1L;
                    }
                }

                var result = new List<ResultShape0>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 inmScore = resultWindowRows[windowIndex];
                    if ((((long)resultRowNumbers[windowIndex] > 0L) && ((long)resultRowNumbers[windowIndex] <= 1)))
                    {
                        result.Add(new ResultShape0(inmScore.i_Name, inmScore.inm_Avg_n_Value_, inmScore.inm_Min_n_Value_, inmScore.inm_Max_n_Value_));
                    }
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.Name, right.Name);
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
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
                OnPhaseChanged("compiled", QueryPhase.End);
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
        private static List<Statement0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, CteRowResults _cteRowResults)
        {
            var __statement0_iSchema = provider.GetSchema("#apply");
            var statement0_iRowsSource = __statement0_iSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>("items", new SourceExecutionContext("i:1", sourceExecutionPlans["i:1"], token, __schemaColumns_compiled_i_0, sourceRuntimeSettingsBySourceContextId["i:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var statement0_iRows = statement0_iRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            var statement0GroupsToFinalize = new List<Statement0AggregateGroup>();
            var statement0Groups = new Dictionary<string, Statement0AggregateGroup>();
            Statement0AggregateGroup statement0NullGroup = null;
            foreach (var iChunk in statement0_iRows)
            {
                if (iChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity> iChunkView)
                {
                    if (iChunkView.Source is Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity[] iChunkViewArray)
                    {
                        int iChunkViewOffset = iChunkView.Offset;
                        for (int iIndex = 0, iIndexCount = iChunkView.Count; iIndex < iIndexCount; ++iIndex)
                        {
                            if ((iIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var i = iChunkViewArray[iChunkViewOffset + iIndex];
                            TraverseStatement0NRows(token, i, statement0GroupsToFinalize, statement0Groups, ref statement0NullGroup);
                        }

                        continue;
                    }

                    if (iChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity> iChunkViewList)
                    {
                        int iChunkViewOffset = iChunkView.Offset;
                        for (int iIndex = 0, iIndexCount = iChunkView.Count; iIndex < iIndexCount; ++iIndex)
                        {
                            if ((iIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var i = iChunkViewList[iChunkViewOffset + iIndex];
                            TraverseStatement0NRows(token, i, statement0GroupsToFinalize, statement0Groups, ref statement0NullGroup);
                        }

                        continue;
                    }
                }

                for (int iIndex = 0, iIndexCount = iChunk.Count; iIndex < iIndexCount; ++iIndex)
                {
                    if ((iIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var i = iChunk[iIndex];
                    TraverseStatement0NRows(token, i, statement0GroupsToFinalize, statement0Groups, ref statement0NullGroup);
                }
            }

            statement0.EnsureCapacity(statement0GroupsToFinalize.Count);
            foreach (var statement0FinalGroup in statement0GroupsToFinalize)
            {
                token.ThrowIfCancellationRequested();
                if (((statement0FinalGroup.__agg0.HasValue ? (int?)statement0FinalGroup.__agg0.Value : null) >= 2))
                {
                    statement0.Add(new Statement0Row0(statement0FinalGroup.__key0, statement0FinalGroup.__agg0.HasValue ? (int?)statement0FinalGroup.__agg0.Value : null, statement0FinalGroup.__agg1.HasValue ? (int?)statement0FinalGroup.__agg1.Value : null, statement0FinalGroup.__agg2.HasValue ? (int?)statement0FinalGroup.__agg2.Sum / int.CreateChecked(statement0FinalGroup.__agg2.Count) : null));
                }
            }

            return statement0;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void ExtractResultRowNumbersWindowKeys(IReadOnlyList<Statement0Row0> resultWindowRows, WindowResultRowNumbersOrderKeysKey[] resultRowNumbersOrderKeys)
        {
            for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
            {
                Statement0Row0 inmScore = resultWindowRows[windowIndex];
                resultRowNumbersOrderKeys[windowIndex] = new WindowResultRowNumbersOrderKeysKey(inmScore.inm_Avg_n_Value_, inmScore.inm_Min_n_Value_, inmScore.inm_Max_n_Value_);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void TraverseStatement0MRows(CancellationToken token, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i, int n, List<Statement0AggregateGroup> statement0GroupsToFinalize, Dictionary<string, Statement0AggregateGroup> statement0Groups, ref Statement0AggregateGroup statement0NullGroup)
        {
            token.ThrowIfCancellationRequested();
            var statement0_mRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
            foreach (var mChunk in statement0_mRows)
            {
                if (mChunk is global::Musoq.Schema.DataSources.RowChunk<int> mChunkView)
                {
                    if (mChunkView.Source is int[] mChunkViewArray)
                    {
                        int mChunkViewOffset = mChunkView.Offset;
                        for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                        {
                            if ((mIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var m = mChunkViewArray[mChunkViewOffset + mIndex];
                            UpdateStatement0GroupsAggregates(statement0GroupsToFinalize, statement0Groups, ref statement0NullGroup, n, i);
                        }

                        continue;
                    }

                    if (mChunkView.Source is List<int> mChunkViewList)
                    {
                        int mChunkViewOffset = mChunkView.Offset;
                        for (int mIndex = 0, mIndexCount = mChunkView.Count; mIndex < mIndexCount; ++mIndex)
                        {
                            if ((mIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var m = mChunkViewList[mChunkViewOffset + mIndex];
                            UpdateStatement0GroupsAggregates(statement0GroupsToFinalize, statement0Groups, ref statement0NullGroup, n, i);
                        }

                        continue;
                    }
                }

                for (int mIndex = 0, mIndexCount = mChunk.Count; mIndex < mIndexCount; ++mIndex)
                {
                    if ((mIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var m = mChunk[mIndex];
                    UpdateStatement0GroupsAggregates(statement0GroupsToFinalize, statement0Groups, ref statement0NullGroup, n, i);
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void TraverseStatement0NRows(CancellationToken token, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i, List<Statement0AggregateGroup> statement0GroupsToFinalize, Dictionary<string, Statement0AggregateGroup> statement0Groups, ref Statement0AggregateGroup statement0NullGroup)
        {
            token.ThrowIfCancellationRequested();
            var statement0_nRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
            foreach (var nChunk in statement0_nRows)
            {
                if (nChunk is global::Musoq.Schema.DataSources.RowChunk<int> nChunkView)
                {
                    if (nChunkView.Source is int[] nChunkViewArray)
                    {
                        int nChunkViewOffset = nChunkView.Offset;
                        for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                        {
                            if ((nIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var n = nChunkViewArray[nChunkViewOffset + nIndex];
                            TraverseStatement0MRows(token, i, n, statement0GroupsToFinalize, statement0Groups, ref statement0NullGroup);
                        }

                        continue;
                    }

                    if (nChunkView.Source is List<int> nChunkViewList)
                    {
                        int nChunkViewOffset = nChunkView.Offset;
                        for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                        {
                            if ((nIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var n = nChunkViewList[nChunkViewOffset + nIndex];
                            TraverseStatement0MRows(token, i, n, statement0GroupsToFinalize, statement0Groups, ref statement0NullGroup);
                        }

                        continue;
                    }
                }

                for (int nIndex = 0, nIndexCount = nChunk.Count; nIndex < nIndexCount; ++nIndex)
                {
                    if ((nIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var n = nChunk[nIndex];
                    TraverseStatement0MRows(token, i, n, statement0GroupsToFinalize, statement0Groups, ref statement0NullGroup);
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void UpdateStatement0GroupsAggregates(List<Statement0AggregateGroup> statement0GroupsToFinalize, Dictionary<string, Statement0AggregateGroup> statement0Groups, ref Statement0AggregateGroup statement0NullGroup, int n, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i)
        {
            int value = n;
            string groupKey = i.Name;
            Statement0AggregateGroup statement0Group = null;
            if (groupKey != null)
            {
                ref var statement0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0Groups, groupKey, out var statement0GroupExists);
                if (!statement0GroupExists)
                {
                    statement0GroupRef = new Statement0AggregateGroup(groupKey);
                    statement0GroupsToFinalize.Add(statement0GroupRef);
                }

                statement0Group = statement0GroupRef;
            }
            else
            {
                if (statement0NullGroup == null)
                {
                    statement0NullGroup = new Statement0AggregateGroup(null);
                    statement0GroupsToFinalize.Add(statement0NullGroup);
                }

                statement0Group = statement0NullGroup;
            }

            {
                var __agg0Input = (int?)value;
                if (__agg0Input.HasValue)
                {
                    var __agg0Current = __agg0Input.GetValueOrDefault();
                    if (!statement0Group.__agg0.HasValue || __agg0Current > statement0Group.__agg0.Value)
                    {
                        statement0Group.__agg0.Value = __agg0Current;
                    }

                    statement0Group.__agg0.HasValue = true;
                    if (!statement0Group.__agg1.HasValue || __agg0Current < statement0Group.__agg1.Value)
                    {
                        statement0Group.__agg1.Value = __agg0Current;
                    }

                    statement0Group.__agg1.HasValue = true;
                    statement0Group.__agg2.Sum = statement0Group.__agg2.HasValue ? checked(statement0Group.__agg2.Sum + __agg0Current) : __agg0Current;
                    statement0Group.__agg2.Count = checked(statement0Group.__agg2.Count + 1L);
                    statement0Group.__agg2.HasValue = true;
                }
            }
        }

        private sealed class CteRowResults
        {
            public List<Statement0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, int? __value1, int? __value2, int? __value3)
            {
                Name = __value0;
                ValueAvg = __value1;
                ValueMin = __value2;
                ValueMax = __value3;
            }

            public override int Count => 4;
            public string Name { get; private set; }
            public int? ValueAvg { get; private set; }
            public int? ValueMax { get; private set; }
            public int? ValueMin { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        ValueAvg = (int?)value;
                        break;
                    case 2:
                        ValueMin = (int?)value;
                        break;
                    case 3:
                        ValueMax = (int?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "ValueAvg" => true,
                "ValueMin" => true,
                "ValueMax" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)ValueAvg,
                2 => (object)ValueMin,
                3 => (object)ValueMax,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "ValueAvg" => (object)ValueAvg,
                "ValueMin" => (object)ValueMin,
                "ValueMax" => (object)ValueMax,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Name, int? ValueAvg, int? ValueMin, int? ValueMax)
            {
                this.Name = Name;
                this.ValueAvg = ValueAvg;
                this.ValueMin = ValueMin;
                this.ValueMax = ValueMax;
            }

            public string Name { get; }
            public int? ValueAvg { get; }
            public int? ValueMax { get; }
            public int? ValueMin { get; }
        }

        private sealed class Statement0AggregateGroup
        {
            public Musoq.Plugins.MaxAggregateKernel<int>.State __agg0;
            public Musoq.Plugins.MinAggregateKernel<int>.State __agg1;
            public Musoq.Plugins.AvgAggregateKernel<int>.State __agg2;
            public readonly string __key0;
            public Statement0AggregateGroup(string __key0)
            {
                this.__key0 = __key0;
            }

            public void MergeFrom(Statement0AggregateGroup source)
            {
                Musoq.Plugins.MaxAggregateKernel<int>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.MinAggregateKernel<int>.Merge(ref this.__agg1, in source.__agg1);
                Musoq.Plugins.AvgAggregateKernel<int>.Merge(ref this.__agg2, in source.__agg2);
            }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(string __value0, int? __value1, int? __value2, int? __value3)
            {
                i_Name = __value0;
                inm_Max_n_Value_ = __value1;
                inm_Min_n_Value_ = __value2;
                inm_Avg_n_Value_ = __value3;
            }

            public string i_Name { get; }
            public int? inm_Avg_n_Value_ { get; }
            public int? inm_Max_n_Value_ { get; }
            public int? inm_Min_n_Value_ { get; }
        }

        private readonly struct WindowResultRowNumbersOrderKeysKey : System.IEquatable<WindowResultRowNumbersOrderKeysKey>, System.IComparable<WindowResultRowNumbersOrderKeysKey>
        {
            private readonly int? _value0;
            private readonly int? _value1;
            private readonly int? _value2;
            public WindowResultRowNumbersOrderKeysKey(int? value0, int? value1, int? value2)
            {
                _value0 = value0;
                _value1 = value1;
                _value2 = value2;
            }

            public int CompareTo(WindowResultRowNumbersOrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                var comparison1 = CompareValue1(_value1, other._value1);
                if (comparison1 != 0)
                    return comparison1;
                var comparison2 = CompareValue2(_value2, other._value2);
                if (comparison2 != 0)
                    return comparison2;
                return 0;
            }

            public bool Equals(WindowResultRowNumbersOrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int?>.Default.Equals(_value0, other._value0) && System.Collections.Generic.EqualityComparer<int?>.Default.Equals(_value1, other._value1) && System.Collections.Generic.EqualityComparer<int?>.Default.Equals(_value2, other._value2);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultRowNumbersOrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0);
                hash.Add(_value1);
                hash.Add(_value2);
                return hash.ToHashCode();
            }

            private static int CompareValue0(int? left, int? right)
            {
                if (!left.HasValue)
                    return !right.HasValue ? 0 : 1;
                if (!right.HasValue)
                    return -1;
                var comparison = left.Value.CompareTo(right.Value);
                return -comparison;
            }

            private static int CompareValue1(int? left, int? right)
            {
                if (!left.HasValue)
                    return !right.HasValue ? 0 : -1;
                if (!right.HasValue)
                    return 1;
                var comparison = left.Value.CompareTo(right.Value);
                return comparison;
            }

            private static int CompareValue2(int? left, int? right)
            {
                if (!left.HasValue)
                    return !right.HasValue ? 0 : 1;
                if (!right.HasValue)
                    return -1;
                var comparison = left.Value.CompareTo(right.Value);
                return -comparison;
            }
        }
    }
}
