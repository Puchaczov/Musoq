// === Parsed Query ===
/*
select s.Value as Text from #apply.items() i cross apply i.JustReturnArrayOfString() s where i.Name = 'left'
*/

// === Logical Plan ===
/*
MultiStatement
  Project [i.Name as i.Name, s.Value as s.Value]
    Apply [Cross]
      SchemaScan [#apply.items() as i]
      AccessMethodSource [JustReturnArrayOfString() as s] [apply: Cross] [type: String[]]
  Project [s.Value as Text]
    Filter [(i.Name = 'left')]
      CteRef [is as is]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [i.Name as i.Name, s.Value as s.Value]
    PhysicalNestedLoopApply [Cross] [guards: PreApplyRight: (i.Name = 'left')]
      PhysicalSchemaScan [#apply.items() as i] [pushdown: (i.Name = 'left')]
      PhysicalAccessMethodSource [JustReturnArrayOfString() as s] [apply: Cross] [type: String[]]
  PhysicalProject [s.Value as Text]
    PhysicalFilter [(i.Name = 'left')]
      PhysicalCteRef [is as is]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [i: GeneratedApplySampleEntity]
      Name: string <- property Name
    SourceEntity [s: string]
      Value: string <- direct scalar value
    Generated [Statement0Row0]
      i.Name: string <- field i_Name
      s.Value: string <- field s_Value
    TableRow [is]
      i.Name: string <- field i_Name
      s.Value: string <- field s_Value
    Generated [ResultRow0]
      Text: string <- field Text

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [i: GeneratedApplySampleEntity] -> statement0_iRows
    CreateTable [statement0: Statement0Row0]
    CreateObject [__statement0Library0: Library]
    PhaseBoundary [Where]
    ChunkedForEach [i in statement0_iRows]
      Let [iName: string = i.Name]
      ContinueIf [NOT ((i.Name = 'left') = TRUE)]
      EnumerableSource [JustReturnArrayOfString() -> statement0_sRows]
      ChunkedForEach [s in statement0_sRows]
        AppendRow [statement0 <- Statement0Row0(i.Name: iName, s.Value: s.Value)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ForEach [is in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Text: is.s.Value)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q247_CrossApplyMethodWhereLeftGuard
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
            new Column("Text", typeof(string), 0)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("i.Name", typeof(string), 0),
            new Column("s.Value", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_i_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Text);
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
                OnPhaseChanged("compiled", QueryPhase.Where);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled", QueryPhase.Select);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 @is = __storedTable0Rows[__storedTable0Index];
                    __musoqFinalShapeRows.Add(new ResultShape0(@is.s_Value));
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
            var __statement0Library0 = new Musoq.Evaluator.Tests.Schema.Basic.Library();
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
                            string iName = i.Name;
                            if ((!(Operators.SqlCompare<string, string>(i.Name, "left", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) == true)))
                            {
                                continue;
                            }

                            var statement0_sRows = EvaluationHelper.ConvertEnumerableOutputToChunks<string>((string[])__statement0Library0.JustReturnArrayOfString());
                            foreach (var sChunk in statement0_sRows)
                            {
                                if (sChunk is global::Musoq.Schema.DataSources.RowChunk<string> sChunkView)
                                {
                                    if (sChunkView.Source is string[] sChunkViewArray)
                                    {
                                        int sChunkViewOffset = sChunkView.Offset;
                                        for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                                        {
                                            if ((sIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var s = sChunkViewArray[sChunkViewOffset + sIndex];
                                            statement0.Add(new Statement0Row0(iName, s));
                                        }

                                        continue;
                                    }

                                    if (sChunkView.Source is List<string> sChunkViewList)
                                    {
                                        int sChunkViewOffset = sChunkView.Offset;
                                        for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                                        {
                                            if ((sIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var s = sChunkViewList[sChunkViewOffset + sIndex];
                                            statement0.Add(new Statement0Row0(iName, s));
                                        }

                                        continue;
                                    }
                                }

                                for (int sIndex = 0, sIndexCount = sChunk.Count; sIndex < sIndexCount; ++sIndex)
                                {
                                    if ((sIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var s = sChunk[sIndex];
                                    statement0.Add(new Statement0Row0(iName, s));
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
                            string iName = i.Name;
                            if ((!(Operators.SqlCompare<string, string>(i.Name, "left", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) == true)))
                            {
                                continue;
                            }

                            var statement0_sRows = EvaluationHelper.ConvertEnumerableOutputToChunks<string>((string[])__statement0Library0.JustReturnArrayOfString());
                            foreach (var sChunk in statement0_sRows)
                            {
                                if (sChunk is global::Musoq.Schema.DataSources.RowChunk<string> sChunkView)
                                {
                                    if (sChunkView.Source is string[] sChunkViewArray)
                                    {
                                        int sChunkViewOffset = sChunkView.Offset;
                                        for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                                        {
                                            if ((sIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var s = sChunkViewArray[sChunkViewOffset + sIndex];
                                            statement0.Add(new Statement0Row0(iName, s));
                                        }

                                        continue;
                                    }

                                    if (sChunkView.Source is List<string> sChunkViewList)
                                    {
                                        int sChunkViewOffset = sChunkView.Offset;
                                        for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                                        {
                                            if ((sIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var s = sChunkViewList[sChunkViewOffset + sIndex];
                                            statement0.Add(new Statement0Row0(iName, s));
                                        }

                                        continue;
                                    }
                                }

                                for (int sIndex = 0, sIndexCount = sChunk.Count; sIndex < sIndexCount; ++sIndex)
                                {
                                    if ((sIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var s = sChunk[sIndex];
                                    statement0.Add(new Statement0Row0(iName, s));
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
                    string iName = i.Name;
                    if ((!(Operators.SqlCompare<string, string>(i.Name, "left", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) == true)))
                    {
                        continue;
                    }

                    var statement0_sRows = EvaluationHelper.ConvertEnumerableOutputToChunks<string>((string[])__statement0Library0.JustReturnArrayOfString());
                    foreach (var sChunk in statement0_sRows)
                    {
                        if (sChunk is global::Musoq.Schema.DataSources.RowChunk<string> sChunkView)
                        {
                            if (sChunkView.Source is string[] sChunkViewArray)
                            {
                                int sChunkViewOffset = sChunkView.Offset;
                                for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                                {
                                    if ((sIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var s = sChunkViewArray[sChunkViewOffset + sIndex];
                                    statement0.Add(new Statement0Row0(iName, s));
                                }

                                continue;
                            }

                            if (sChunkView.Source is List<string> sChunkViewList)
                            {
                                int sChunkViewOffset = sChunkView.Offset;
                                for (int sIndex = 0, sIndexCount = sChunkView.Count; sIndex < sIndexCount; ++sIndex)
                                {
                                    if ((sIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var s = sChunkViewList[sChunkViewOffset + sIndex];
                                    statement0.Add(new Statement0Row0(iName, s));
                                }

                                continue;
                            }
                        }

                        for (int sIndex = 0, sIndexCount = sChunk.Count; sIndex < sIndexCount; ++sIndex)
                        {
                            if ((sIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var s = sChunk[sIndex];
                            statement0.Add(new Statement0Row0(iName, s));
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
            public ResultRow0(string __value0)
            {
                Text = __value0;
            }

            public override int Count => 1;
            public string Text { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Text = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Text" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Text,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Text" => (object)Text,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Text)
            {
                this.Text = Text;
            }

            public string Text { get; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(string __value0, string __value1)
            {
                i_Name = __value0;
                s_Value = __value1;
            }

            public string i_Name { get; }
            public string s_Value { get; }
        }
    }
}
