// === Parsed Query ===
/*
select i.Name, n.Value as FirstValue, m.Value as SecondValue, RowNumber() over (partition by i.Name order by n.Value, m.Value) as RowNo from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m order by i.Name, RowNo
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [i.Name, WindowRef(0)]
    Project [i.Name as i.Name, n.Value as FirstValue, m.Value as SecondValue, WindowRef(0) as RowNo]
      Window [RowNumber(idx:0; partition: i.Name; order: n.Value, m.Value)]
        Apply [Cross]
          Apply [Cross]
            SchemaScan [#apply.items() as i]
            PropertySource [i.Numbers as n] [apply: Cross] [type: Int32[]]
          PropertySource [i.Numbers as m] [apply: Cross] [type: Int32[]]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [i.Name, WindowRef(0)]
    PhysicalProject [i.Name as i.Name, n.Value as FirstValue, m.Value as SecondValue, WindowRef(0) as RowNo]
      PhysicalWindow [RowNumber(idx:0; partition: i.Name; order: n.Value, m.Value)]
        PhysicalMaterialize
          PhysicalNestedLoopApply [Cross]
            PhysicalNestedLoopApply [Cross]
              PhysicalSchemaScan [#apply.items() as i]
              PhysicalPropertySource [i.Numbers as n] [apply: Cross] [type: Int32[]]
            PhysicalPropertySource [i.Numbers as m] [apply: Cross] [type: Int32[]]
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
    Generated [apply_0_i_n_mRow0]
      i.Name: string <- field i_Name
      i.Numbers: int[] <- field i_Numbers
      n.Value: int <- field n_Value
      m.Value: int <- field m_Value
    TableRow [apply_0_i_n_m]
      i.Name: string <- field i_Name
      i.Numbers: int[] <- field i_Numbers
      n.Value: int <- field n_Value
      m.Value: int <- field m_Value
    Generated [ResultRow0]
      i.Name: string <- field i_Name
      FirstValue: int <- field FirstValue
      SecondValue: int <- field SecondValue
      RowNo: long <- field RowNo

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [i: GeneratedApplySampleEntity] -> apply_0_i_n_mTable_iRows
    CreateRowBuffer [apply_0_i_n_mTable: List<apply_0_i_n_mRow0>]
    PhaseBoundary [Select]
    ChunkedForEach [i in apply_0_i_n_mTable_iRows]
      EnumerableSource [i.Numbers -> apply_0_i_n_mTable_nRows]
      ChunkedForEach [n in apply_0_i_n_mTable_nRows]
        EnumerableSource [i.Numbers -> apply_0_i_n_mTable_mRows]
        ChunkedForEach [m in apply_0_i_n_mTable_mRows]
          AppendRowBuffer [apply_0_i_n_mTable <- apply_0_i_n_mRow0(i.Name: i.Name, i.Numbers: i.Numbers, n.Value: n.Value, m.Value: m.Value)]
    Materialize [apply_0_i_n_mTable -> resultWindowRows]
    ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows partition by apply_0_i_n_m.i.Name order by apply_0_i_n_m.n.Value ASC, apply_0_i_n_m.m.Value ASC]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, apply_0_i_n_m in resultWindowRows]
      AppendShape [result <- ResultShape0(i.Name: apply_0_i_n_m.i.Name, FirstValue: apply_0_i_n_m.n.Value, SecondValue: apply_0_i_n_m.m.Value, RowNo: resultRowNumbers[windowIndex])]
    SortShapeRows [result -> resultSorted by i.Name ASC, RowNo ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q68_ChainedApplyWindow
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_apply_0_i_n_mTable_1 = new Column[]
        {
            new Column("i.Name", typeof(string), 0),
            new Column("i.Numbers", typeof(int[]), 1),
            new Column("n.Value", typeof(int), 2),
            new Column("m.Value", typeof(int), 3)
        };
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("i.Name", typeof(string), 0),
            new Column("FirstValue", typeof(int), 1),
            new Column("SecondValue", typeof(int), 2),
            new Column("RowNo", typeof(long), 3)
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
        public event QueryProgressEventHandler QueryProgress;
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.i_Name, __musoqShapeRow.FirstValue, __musoqShapeRow.SecondValue, __musoqShapeRow.RowNo);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __apply_0_i_n_mTable_iSchema = provider.GetSchema("#apply");
                var apply_0_i_n_mTable_iRowsSource = __apply_0_i_n_mTable_iSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>("items", new SourceExecutionContext("i:1", sourceExecutionPlans["i:1"], token, __schemaColumns_compiled_i_0, sourceRuntimeSettingsBySourceContextId["i:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var apply_0_i_n_mTable_iRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>(apply_0_i_n_mTable_iRowsSource.Chunks, __musoqProgressContext, "i:1") : apply_0_i_n_mTable_iRowsSource.Chunks;
                var apply_0_i_n_mTable = new List<apply_0_i_n_mRow0>();
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var iChunk in apply_0_i_n_mTable_iRows)
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
                                TraverseApply0INMTableNRows(token, i, apply_0_i_n_mTable);
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
                                TraverseApply0INMTableNRows(token, i, apply_0_i_n_mTable);
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
                        TraverseApply0INMTableNRows(token, i, apply_0_i_n_mTable);
                    }
                }

                var resultWindowRows = EvaluationHelper.MaterializeGeneratedRows<apply_0_i_n_mRow0>(apply_0_i_n_mTable);
                var resultRowNumbersPartitionKeys = new string[resultWindowRows.Count];
                var resultRowNumbersOrderKeys = new WindowResultRowNumbersOrderKeysKey[resultWindowRows.Count];
                ExtractResultRowNumbersWindowKeys(resultWindowRows, resultRowNumbersPartitionKeys, resultRowNumbersOrderKeys);
                var resultRowNumbersPartitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count, resultRowNumbersPartitionKeys);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultRowNumbersPartitions, resultRowNumbersOrderKeys, false);
                var resultRowNumbers = new long[resultWindowRows.Count];
                for (int resultRowNumbersPartitionSetIndex = 0; resultRowNumbersPartitionSetIndex < resultRowNumbersPartitions.PartitionCount; ++resultRowNumbersPartitionSetIndex)
                {
                    var resultRowNumbersPartitionStart = resultRowNumbersPartitions.GetStart(resultRowNumbersPartitionSetIndex);
                    var resultRowNumbersPartitionCount = resultRowNumbersPartitions.GetLength(resultRowNumbersPartitionSetIndex);
                    var resultRowNumbersPartitionIndices = resultRowNumbersPartitions.Indices;
                    var resultRowNumbersPartitionLimit = resultRowNumbersPartitionCount;
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

                    apply_0_i_n_mRow0 apply_0_i_n_m = resultWindowRows[windowIndex];
                    result.Add(new ResultShape0(apply_0_i_n_m.i_Name, apply_0_i_n_m.n_Value, apply_0_i_n_m.m_Value, (long)resultRowNumbers[windowIndex]));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.i_Name, right.i_Name);
                    if (comparison != 0)
                        return comparison;
                    comparison = left.RowNo.CompareTo(right.RowNo);
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
        private static void ExtractResultRowNumbersWindowKeys(IReadOnlyList<apply_0_i_n_mRow0> resultWindowRows, string[] resultRowNumbersPartitionKeys, WindowResultRowNumbersOrderKeysKey[] resultRowNumbersOrderKeys)
        {
            for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
            {
                apply_0_i_n_mRow0 apply_0_i_n_m = resultWindowRows[windowIndex];
                resultRowNumbersPartitionKeys[windowIndex] = (string)apply_0_i_n_m.i_Name;
                resultRowNumbersOrderKeys[windowIndex] = new WindowResultRowNumbersOrderKeysKey(apply_0_i_n_m.n_Value, apply_0_i_n_m.m_Value);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void TraverseApply0INMTableNRows(CancellationToken token, Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity i, List<apply_0_i_n_mRow0> apply_0_i_n_mTable)
        {
            token.ThrowIfCancellationRequested();
            var apply_0_i_n_mTable_nRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
            foreach (var nChunk in apply_0_i_n_mTable_nRows)
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
                            var apply_0_i_n_mTable_mRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
                            foreach (var mChunk in apply_0_i_n_mTable_mRows)
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
                                            apply_0_i_n_mTable.Add(new apply_0_i_n_mRow0(i.Name, i.Numbers, n, m));
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
                                            apply_0_i_n_mTable.Add(new apply_0_i_n_mRow0(i.Name, i.Numbers, n, m));
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
                                    apply_0_i_n_mTable.Add(new apply_0_i_n_mRow0(i.Name, i.Numbers, n, m));
                                }
                            }
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
                            var apply_0_i_n_mTable_mRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
                            foreach (var mChunk in apply_0_i_n_mTable_mRows)
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
                                            apply_0_i_n_mTable.Add(new apply_0_i_n_mRow0(i.Name, i.Numbers, n, m));
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
                                            apply_0_i_n_mTable.Add(new apply_0_i_n_mRow0(i.Name, i.Numbers, n, m));
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
                                    apply_0_i_n_mTable.Add(new apply_0_i_n_mRow0(i.Name, i.Numbers, n, m));
                                }
                            }
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
                    var apply_0_i_n_mTable_mRows = EvaluationHelper.ConvertEnumerableOutputToChunks<int>(i.Numbers);
                    foreach (var mChunk in apply_0_i_n_mTable_mRows)
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
                                    apply_0_i_n_mTable.Add(new apply_0_i_n_mRow0(i.Name, i.Numbers, n, m));
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
                                    apply_0_i_n_mTable.Add(new apply_0_i_n_mRow0(i.Name, i.Numbers, n, m));
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
                            apply_0_i_n_mTable.Add(new apply_0_i_n_mRow0(i.Name, i.Numbers, n, m));
                        }
                    }
                }
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, int __value1, int __value2, long __value3)
            {
                i_Name = __value0;
                FirstValue = __value1;
                SecondValue = __value2;
                RowNo = __value3;
            }

            public override int Count => 4;
            public int FirstValue { get; private set; }
            public long RowNo { get; private set; }
            public int SecondValue { get; private set; }
            public string i_Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        i_Name = (string)value;
                        break;
                    case 1:
                        FirstValue = (int)value;
                        break;
                    case 2:
                        SecondValue = (int)value;
                        break;
                    case 3:
                        RowNo = (long)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "i.Name" => true,
                "i_Name" => true,
                "Name" => true,
                "FirstValue" => true,
                "SecondValue" => true,
                "RowNo" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)i_Name,
                1 => (object)FirstValue,
                2 => (object)SecondValue,
                3 => (object)RowNo,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "i.Name" => (object)i_Name,
                "i_Name" => (object)i_Name,
                "Name" => (object)i_Name,
                "FirstValue" => (object)FirstValue,
                "SecondValue" => (object)SecondValue,
                "RowNo" => (object)RowNo,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string i_Name, int FirstValue, int SecondValue, long RowNo)
            {
                this.i_Name = i_Name;
                this.FirstValue = FirstValue;
                this.SecondValue = SecondValue;
                this.RowNo = RowNo;
            }

            public int FirstValue { get; }
            public long RowNo { get; }
            public int SecondValue { get; }
            public string i_Name { get; }
        }

        private readonly struct WindowResultRowNumbersOrderKeysKey : System.IEquatable<WindowResultRowNumbersOrderKeysKey>, System.IComparable<WindowResultRowNumbersOrderKeysKey>
        {
            private readonly int _value0;
            private readonly int _value1;
            public WindowResultRowNumbersOrderKeysKey(int value0, int value1)
            {
                _value0 = value0;
                _value1 = value1;
            }

            public int CompareTo(WindowResultRowNumbersOrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                var comparison1 = CompareValue1(_value1, other._value1);
                if (comparison1 != 0)
                    return comparison1;
                return 0;
            }

            public bool Equals(WindowResultRowNumbersOrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0) && System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value1, other._value1);
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
                return hash.ToHashCode();
            }

            private static int CompareValue0(int left, int right)
            {
                var comparison = left.CompareTo(right);
                return comparison;
            }

            private static int CompareValue1(int left, int right)
            {
                var comparison = left.CompareTo(right);
                return comparison;
            }
        }

        private sealed class apply_0_i_n_mRow0 : Row
        {
            public apply_0_i_n_mRow0(string __value0, int[] __value1, int __value2, int __value3)
            {
                i_Name = __value0;
                i_Numbers = __value1;
                n_Value = __value2;
                m_Value = __value3;
            }

            public override int Count => 4;
            public string i_Name { get; private set; }
            public int[] i_Numbers { get; private set; }
            public int m_Value { get; private set; }
            public int n_Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        i_Name = (string)value;
                        break;
                    case 1:
                        i_Numbers = (int[])value;
                        break;
                    case 2:
                        n_Value = (int)value;
                        break;
                    case 3:
                        m_Value = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "i.Name" => true,
                "i_Name" => true,
                "Name" => true,
                "i.Numbers" => true,
                "i_Numbers" => true,
                "Numbers" => true,
                "n.Value" => true,
                "n_Value" => true,
                "m.Value" => true,
                "m_Value" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)i_Name,
                1 => (object)i_Numbers,
                2 => (object)n_Value,
                3 => (object)m_Value,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "i.Name" => (object)i_Name,
                "i_Name" => (object)i_Name,
                "Name" => (object)i_Name,
                "i.Numbers" => (object)i_Numbers,
                "i_Numbers" => (object)i_Numbers,
                "Numbers" => (object)i_Numbers,
                "n.Value" => (object)n_Value,
                "n_Value" => (object)n_Value,
                "m.Value" => (object)m_Value,
                "m_Value" => (object)m_Value,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
