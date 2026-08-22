// === Parsed Query ===
/*
select i.Name, n.Value as Number, n.Ordinal as NumberOrdinal from #apply.items() i cross apply i.Numbers n with ordinality order by i.Name, n.Ordinal
*/

// === Logical Plan ===
/*
MultiStatement
  Project [i.Name as i.Name, i.Numbers as i.Numbers, n.Value as n.Value, n.Ordinal as n.Ordinal]
    Apply [Cross, with ordinality]
      SchemaScan [#apply.items() as i]
      PropertySource [i.Numbers as n] [apply: Cross] [type: Int32[]]
  Sort [i.Name, n.Ordinal]
    Project [i.Name as i.Name, n.Value as Number, n.Ordinal as NumberOrdinal]
      CteRef [in as in]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [i.Name as i.Name, i.Numbers as i.Numbers, n.Value as n.Value, n.Ordinal as n.Ordinal]
    PhysicalNestedLoopApply [Cross, with ordinality]
      PhysicalSchemaScan [#apply.items() as i]
      PhysicalPropertySource [i.Numbers as n] [apply: Cross] [type: Int32[]]
  PhysicalSort [i.Name, n.Ordinal]
    PhysicalProject [i.Name as i.Name, n.Value as Number, n.Ordinal as NumberOrdinal]
      PhysicalCteRef [in as in]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [i: GeneratedApplySampleEntity]
      Name: string <- property Name
      Numbers: int[] <- property Numbers
    SourceEntity [n: PrimitiveTypeEntity<int>]
      Value: int <- property Value
      Ordinal: int <- apply ordinality nOrdinal
    Generated [Statement0Row0]
      i.Name: string <- field i_Name
      i.Numbers: int[] <- field i_Numbers
      n.Value: int <- field n_Value
      n.Ordinal: int <- field n_Ordinal
    TableRow [in]
      i.Name: string <- field i_Name
      i.Numbers: int[] <- field i_Numbers
      n.Value: int <- field n_Value
      n.Ordinal: int <- field n_Ordinal
    Generated [ResultRow0]
      i.Name: string <- field i_Name
      Number: int <- field Number
      NumberOrdinal: int <- field NumberOrdinal

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [i: GeneratedApplySampleEntity] -> statement0_iRows
    CreateTable [statement0: Statement0Row0]
    PhaseBoundary [Select]
    ChunkedForEach [i in statement0_iRows]
      EnumerableSource [i.Numbers -> statement0_nRows]
      ChunkedForEachWithOrdinality [nOrdinal, n in statement0_nRows]
        AppendRow [statement0 <- Statement0Row0(i.Name: i.Name, i.Numbers: i.Numbers, n.Value: n.Value, n.Ordinal: n.Ordinal)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [in in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(i.Name: in.i.Name, Number: in.n.Value, NumberOrdinal: in.n.Ordinal)]
    SortShapeRows [result -> resultSorted by i.Name ASC, NumberOrdinal ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q173_ApplyWithOrdinality
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("i.Name", typeof(string), 0),
            new Column("Number", typeof(int), 1),
            new Column("NumberOrdinal", typeof(int), 2)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("i.Name", typeof(string), 0),
            new Column("i.Numbers", typeof(int[]), 1),
            new Column("n.Value", typeof(int), 2),
            new Column("n.Ordinal", typeof(int), 3)
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
                yield return new ResultRow0(__musoqShapeRow.i_Name, __musoqShapeRow.Number, __musoqShapeRow.NumberOrdinal);
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

                    Statement0Row0 @in = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(@in.i_Name, @in.n_Value, @in.n_Ordinal));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.i_Name, right.i_Name);
                    if (comparison != 0)
                        return comparison;
                    comparison = left.NumberOrdinal.CompareTo(right.NumberOrdinal);
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
            var __statement0_iSchema = provider.GetSchema("#apply");
            var statement0_iRowsSource = __statement0_iSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>("items", new SourceExecutionContext("i:1", sourceExecutionPlans["i:1"], token, __schemaColumns_compiled_i_0, sourceRuntimeSettingsBySourceContextId["i:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var statement0_iRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>(statement0_iRowsSource.Chunks, __musoqProgressContext, "i:1") : statement0_iRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
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
                            var statement0_nRows = EvaluationHelper.ConvertScalarEnumerableToTypedChunks<int>(i.Numbers);
                            {
                                int nOrdinal = 0;
                                foreach (var nChunk in statement0_nRows)
                                {
                                    if (nChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Plugins.PrimitiveTypeEntity<int>> nChunkView)
                                    {
                                        if (nChunkView.Source is Musoq.Plugins.PrimitiveTypeEntity<int>[] nChunkViewArray)
                                        {
                                            int nChunkViewOffset = nChunkView.Offset;
                                            for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                                            {
                                                if ((nIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                var n = nChunkViewArray[nChunkViewOffset + nIndex];
                                                statement0.Add(new Statement0Row0(i.Name, i.Numbers, n.Value, nOrdinal));
                                                ++nOrdinal;
                                            }

                                            continue;
                                        }

                                        if (nChunkView.Source is List<Musoq.Plugins.PrimitiveTypeEntity<int>> nChunkViewList)
                                        {
                                            int nChunkViewOffset = nChunkView.Offset;
                                            for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                                            {
                                                if ((nIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                var n = nChunkViewList[nChunkViewOffset + nIndex];
                                                statement0.Add(new Statement0Row0(i.Name, i.Numbers, n.Value, nOrdinal));
                                                ++nOrdinal;
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
                                        statement0.Add(new Statement0Row0(i.Name, i.Numbers, n.Value, nOrdinal));
                                        ++nOrdinal;
                                    }
                                }
                            }
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
                            var statement0_nRows = EvaluationHelper.ConvertScalarEnumerableToTypedChunks<int>(i.Numbers);
                            {
                                int nOrdinal = 0;
                                foreach (var nChunk in statement0_nRows)
                                {
                                    if (nChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Plugins.PrimitiveTypeEntity<int>> nChunkView)
                                    {
                                        if (nChunkView.Source is Musoq.Plugins.PrimitiveTypeEntity<int>[] nChunkViewArray)
                                        {
                                            int nChunkViewOffset = nChunkView.Offset;
                                            for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                                            {
                                                if ((nIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                var n = nChunkViewArray[nChunkViewOffset + nIndex];
                                                statement0.Add(new Statement0Row0(i.Name, i.Numbers, n.Value, nOrdinal));
                                                ++nOrdinal;
                                            }

                                            continue;
                                        }

                                        if (nChunkView.Source is List<Musoq.Plugins.PrimitiveTypeEntity<int>> nChunkViewList)
                                        {
                                            int nChunkViewOffset = nChunkView.Offset;
                                            for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                                            {
                                                if ((nIndex & 1023) == 0)
                                                {
                                                    token.ThrowIfCancellationRequested();
                                                }

                                                var n = nChunkViewList[nChunkViewOffset + nIndex];
                                                statement0.Add(new Statement0Row0(i.Name, i.Numbers, n.Value, nOrdinal));
                                                ++nOrdinal;
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
                                        statement0.Add(new Statement0Row0(i.Name, i.Numbers, n.Value, nOrdinal));
                                        ++nOrdinal;
                                    }
                                }
                            }
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
                    var statement0_nRows = EvaluationHelper.ConvertScalarEnumerableToTypedChunks<int>(i.Numbers);
                    {
                        int nOrdinal = 0;
                        foreach (var nChunk in statement0_nRows)
                        {
                            if (nChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Plugins.PrimitiveTypeEntity<int>> nChunkView)
                            {
                                if (nChunkView.Source is Musoq.Plugins.PrimitiveTypeEntity<int>[] nChunkViewArray)
                                {
                                    int nChunkViewOffset = nChunkView.Offset;
                                    for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                                    {
                                        if ((nIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var n = nChunkViewArray[nChunkViewOffset + nIndex];
                                        statement0.Add(new Statement0Row0(i.Name, i.Numbers, n.Value, nOrdinal));
                                        ++nOrdinal;
                                    }

                                    continue;
                                }

                                if (nChunkView.Source is List<Musoq.Plugins.PrimitiveTypeEntity<int>> nChunkViewList)
                                {
                                    int nChunkViewOffset = nChunkView.Offset;
                                    for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                                    {
                                        if ((nIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var n = nChunkViewList[nChunkViewOffset + nIndex];
                                        statement0.Add(new Statement0Row0(i.Name, i.Numbers, n.Value, nOrdinal));
                                        ++nOrdinal;
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
                                statement0.Add(new Statement0Row0(i.Name, i.Numbers, n.Value, nOrdinal));
                                ++nOrdinal;
                            }
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
            public ResultRow0(string __value0, int __value1, int __value2)
            {
                i_Name = __value0;
                Number = __value1;
                NumberOrdinal = __value2;
            }

            public override int Count => 3;
            public int Number { get; private set; }
            public int NumberOrdinal { get; private set; }
            public string i_Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        i_Name = (string)value;
                        break;
                    case 1:
                        Number = (int)value;
                        break;
                    case 2:
                        NumberOrdinal = (int)value;
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
                "Number" => true,
                "NumberOrdinal" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)i_Name,
                1 => (object)Number,
                2 => (object)NumberOrdinal,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "i.Name" => (object)i_Name,
                "i_Name" => (object)i_Name,
                "Name" => (object)i_Name,
                "Number" => (object)Number,
                "NumberOrdinal" => (object)NumberOrdinal,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string i_Name, int Number, int NumberOrdinal)
            {
                this.i_Name = i_Name;
                this.Number = Number;
                this.NumberOrdinal = NumberOrdinal;
            }

            public int Number { get; }
            public int NumberOrdinal { get; }
            public string i_Name { get; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(string __value0, int[] __value1, int __value2, int __value3)
            {
                i_Name = __value0;
                i_Numbers = __value1;
                n_Value = __value2;
                n_Ordinal = __value3;
            }

            public string i_Name { get; }
            public int[] i_Numbers { get; }
            public int n_Ordinal { get; }
            public int n_Value { get; }
        }
    }
}
