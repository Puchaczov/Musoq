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
      inm.Avg(n.Value): decimal? <- field inm_Avg_n_Value_
    TableRow [inmScore]
      i.Name: string <- field i_Name
      inm.Max(n.Value): int? <- field inm_Max_n_Value_
      inm.Min(n.Value): int? <- field inm_Min_n_Value_
      inm.Avg(n.Value): decimal? <- field inm_Avg_n_Value_
    Generated [ResultRow0]
      Name: string <- field Name
      ValueAvg: decimal? <- field ValueAvg
      ValueMin: int? <- field ValueMin
      ValueMax: int? <- field ValueMax

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [i: GeneratedApplySampleEntity] -> statement0_iRows
    CreateTable [statement0: Statement0Row0]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [statement0Groups: string -> Statement0AggregateGroup]
    PhaseBoundary [Select]
    ChunkedForEach [i in statement0_iRows]
      EnumerableSource [i.Numbers -> statement0_nRows]
      ForEach [n in statement0_nRows]
        Let [nValue: int = n.Value]
        EnumerableSource [i.Numbers -> statement0_mRows]
        ForEach [m in statement0_mRows]
          GetOrAddSingleKeyAggregateGroup [statement0Group = statement0Groups[i.Name] by i.Name; typed: Statement0AggregateGroup]
          TypedAggregateSet [Set(statement0Group.__agg0, nValue)]
          TypedAggregateSet [Set(statement0Group.__agg1, nValue)]
          TypedAggregateSet [Set(statement0Group.__agg2, nValue)]
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
    SortOwnedBuffer [result by Name ASC, stable sequence __musoqSequence]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("ValueAvg", typeof(decimal?), 1),
            new Column("ValueMin", typeof(int?), 2),
            new Column("ValueMax", typeof(int?), 3)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("i.Name", typeof(string), 0),
            new Column("inm.Max(n.Value)", typeof(int?), 1),
            new Column("inm.Min(n.Value)", typeof(int?), 2),
            new Column("inm.Avg(n.Value)", typeof(decimal?), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_i_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("Numbers", typeof(int[]), 1) });
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
                yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.ValueAvg, __musoqShapeRow.ValueMin, __musoqShapeRow.ValueMax);
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
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var resultWindowRows = EvaluationHelper.MaterializeGeneratedRows<Statement0Row0>(_cteRowResults.Slot0);
                var resultRowNumbersOrderKeys = new WindowResultRowNumbersOrderKeysKey[resultWindowRows.Count];
                ExtractResultRowNumbersWindowKeys(resultWindowRows, resultRowNumbersOrderKeys);
                var resultRowNumbersPartitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
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
                        var resultRowNumbersContext = new Musoq.Plugins.WindowRankingContext(resultRowNumbersPartitionIndex, 0, 0, 0L, resultRowNumbersPartitionCount, token);
                        resultRowNumbers[resultRowNumbersCurrentIndex] = Musoq.Plugins.RowNumberKernel.Get(in resultRowNumbersContext);
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
                        result.Add(new ResultShape0(inmScore.i_Name, inmScore.inm_Avg_n_Value_, inmScore.inm_Min_n_Value_, inmScore.inm_Max_n_Value_) { __musoqSequence = result.Count });
                    }
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.Name, right.Name);
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
            var __statement0_iSchema = provider.GetSchema("#apply");
            var statement0_iRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>.Open(__statement0_iSchema, "items", new SourceExecutionContext("i:1", sourceExecutionPlans["i:1"], token, __schemaColumns_compiled_i_0, sourceRuntimeSettingsBySourceContextId["i:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_iRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>(statement0_iRowsSource.Chunks, __musoqProgressContext, "i:1") : statement0_iRowsSource.Chunks;
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
                if (((Operators.SqlCompare<int?, int>(statement0FinalGroup.__agg0.HasValue ? (int?)statement0FinalGroup.__agg0.Value : null, 2, (int? __sqlLeft, int __sqlRight) => (__sqlLeft >= __sqlRight)))) == true)
                {
                    statement0.Add(new Statement0Row0(statement0FinalGroup.__key0, statement0FinalGroup.__agg0.HasValue ? (int?)statement0FinalGroup.__agg0.Value : null, statement0FinalGroup.__agg1.HasValue ? (int?)statement0FinalGroup.__agg1.Value : null, Musoq.Plugins.AverageKernel<int, decimal>.Get(in statement0FinalGroup.__agg2)));
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
        private static void TraverseStatement0MRows(CancellationToken token, int n, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i, List<Statement0AggregateGroup> statement0GroupsToFinalize, Dictionary<string, Statement0AggregateGroup> statement0Groups, ref Statement0AggregateGroup statement0NullGroup)
        {
            token.ThrowIfCancellationRequested();
            int nValue = n;
            var statement0_mRows = (IEnumerable<int>)i.Numbers ?? Array.Empty<int>();
            var mIndexRows = statement0_mRows;
            if (mIndexRows is int[] mIndexArray)
                for (int mIndex = 0, mIndexCount = mIndexArray.Length; mIndex < mIndexCount; ++mIndex)
                {
                    if ((mIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    int m = mIndexArray[mIndex];
                    UpdateStatement0GroupsAggregates(statement0GroupsToFinalize, statement0Groups, ref statement0NullGroup, nValue, i);
                }
            else if (mIndexRows is List<int> mIndexConcreteList)
                for (int mIndex = 0, mIndexCount = mIndexConcreteList.Count; mIndex < mIndexCount; ++mIndex)
                {
                    if ((mIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    int m = mIndexConcreteList[mIndex];
                    UpdateStatement0GroupsAggregates(statement0GroupsToFinalize, statement0Groups, ref statement0NullGroup, nValue, i);
                }
            else
            {
                int mIndex = 0;
                foreach (var m in mIndexRows)
                {
                    token.ThrowIfCancellationRequested();
                    UpdateStatement0GroupsAggregates(statement0GroupsToFinalize, statement0Groups, ref statement0NullGroup, nValue, i);
                    ++mIndex;
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void TraverseStatement0NRows(CancellationToken token, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i, List<Statement0AggregateGroup> statement0GroupsToFinalize, Dictionary<string, Statement0AggregateGroup> statement0Groups, ref Statement0AggregateGroup statement0NullGroup)
        {
            token.ThrowIfCancellationRequested();
            var statement0_nRows = (IEnumerable<int>)i.Numbers ?? Array.Empty<int>();
            var nIndexRows = statement0_nRows;
            if (nIndexRows is int[] nIndexArray)
                for (int nIndex = 0, nIndexCount = nIndexArray.Length; nIndex < nIndexCount; ++nIndex)
                {
                    if ((nIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    int n = nIndexArray[nIndex];
                    TraverseStatement0MRows(token, n, i, statement0GroupsToFinalize, statement0Groups, ref statement0NullGroup);
                }
            else if (nIndexRows is List<int> nIndexConcreteList)
                for (int nIndex = 0, nIndexCount = nIndexConcreteList.Count; nIndex < nIndexCount; ++nIndex)
                {
                    if ((nIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    int n = nIndexConcreteList[nIndex];
                    TraverseStatement0MRows(token, n, i, statement0GroupsToFinalize, statement0Groups, ref statement0NullGroup);
                }
            else
            {
                int nIndex = 0;
                foreach (var n in nIndexRows)
                {
                    token.ThrowIfCancellationRequested();
                    TraverseStatement0MRows(token, n, i, statement0GroupsToFinalize, statement0Groups, ref statement0NullGroup);
                    ++nIndex;
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void UpdateStatement0GroupsAggregates(List<Statement0AggregateGroup> statement0GroupsToFinalize, Dictionary<string, Statement0AggregateGroup> statement0Groups, ref Statement0AggregateGroup statement0NullGroup, int nValue, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i)
        {
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
                var __agg0Input = (int?)nValue;
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
                }
            }

            Musoq.Plugins.AverageKernel<int, decimal>.Set(ref statement0Group.__agg2, (int?)nValue);
        }

        private sealed class CteRowResults
        {
            public List<Statement0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, decimal? __value1, int? __value2, int? __value3)
            {
                Name = __value0;
                ValueAvg = __value1;
                ValueMin = __value2;
                ValueMax = __value3;
            }

            public override int Count => 4;
            public string Name { get; private set; }
            public decimal? ValueAvg { get; private set; }
            public int? ValueMax { get; private set; }
            public int? ValueMin { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.ValueAvg = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.ValueMin = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.ValueMax = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.ValueAvg),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.ValueMin),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.ValueMax),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "ValueAvg" => true,
                "ValueMin" => true,
                "ValueMax" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.ValueAvg, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.ValueMin, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.ValueMax, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Name, decimal? ValueAvg, int? ValueMin, int? ValueMax)
            {
                this.Name = Name;
                this.ValueAvg = ValueAvg;
                this.ValueMin = ValueMin;
                this.ValueMax = ValueMax;
            }

            public string Name { get; }
            public decimal? ValueAvg { get; }
            public int? ValueMax { get; }
            public int? ValueMin { get; }
            public int __musoqSequence { get; init; }
        }

        private sealed class Statement0AggregateGroup
        {
            public Musoq.Plugins.MaxAggregateKernel<int>.State __agg0;
            public Musoq.Plugins.MinAggregateKernel<int>.State __agg1;
            public Musoq.Plugins.AverageKernel<int, decimal>.State __agg2;
            public readonly string __key0;
            public Statement0AggregateGroup(string __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.MaxAggregateKernel<int>.Reset(ref this.__agg0);
                Musoq.Plugins.MinAggregateKernel<int>.Reset(ref this.__agg1);
                Musoq.Plugins.AverageKernel<int, decimal>.Reset(ref this.__agg2);
            }

            public void MergeFrom(Statement0AggregateGroup source)
            {
                Musoq.Plugins.MaxAggregateKernel<int>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.MinAggregateKernel<int>.Merge(ref this.__agg1, in source.__agg1);
                Musoq.Plugins.AverageKernel<int, decimal>.Merge(ref this.__agg2, in source.__agg2);
            }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(string __value0, int? __value1, int? __value2, decimal? __value3)
            {
                i_Name = __value0;
                inm_Max_n_Value_ = __value1;
                inm_Min_n_Value_ = __value2;
                inm_Avg_n_Value_ = __value3;
            }

            public string i_Name { get; }
            public decimal? inm_Avg_n_Value_ { get; }
            public int? inm_Max_n_Value_ { get; }
            public int? inm_Min_n_Value_ { get; }
        }

        private readonly struct WindowResultRowNumbersOrderKeysKey : System.IEquatable<WindowResultRowNumbersOrderKeysKey>, System.IComparable<WindowResultRowNumbersOrderKeysKey>
        {
            private readonly decimal? _value0;
            private readonly int? _value1;
            private readonly int? _value2;
            public WindowResultRowNumbersOrderKeysKey(decimal? value0, int? value1, int? value2)
            {
                _value0 = value0;
                _value1 = value1;
                _value2 = value2;
            }

            public ValueTuple<decimal?, int?, int?> RawValues => (_value0, _value1, _value2);

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
                return System.Collections.Generic.EqualityComparer<decimal?>.Default.Equals(_value0, other._value0) && System.Collections.Generic.EqualityComparer<int?>.Default.Equals(_value1, other._value1) && System.Collections.Generic.EqualityComparer<int?>.Default.Equals(_value2, other._value2);
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

            private static int CompareValue0(decimal? left, decimal? right)
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
