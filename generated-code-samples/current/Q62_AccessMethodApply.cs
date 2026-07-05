/*
raw query string

select i.Name as Name, s.Value as Text from #apply.items() i cross apply i.JustReturnArrayOfString() s
*/

/*
logical plan representation string

MultiStatement
  Project [i.Name as i.Name, s.Value as s.Value]
    Apply [Cross]
      SchemaScan [#apply.items() as i]
      AccessMethodSource [JustReturnArrayOfString() as s] [apply: Cross] [type: String[]]
  Project [i.Name as Name, s.Value as Text]
    CteRef [is as is]
*/

/*
physical plan representation string

PhysicalMultiStatement
  PhysicalProject [i.Name as i.Name, s.Value as s.Value]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#apply.items() as i]
      PhysicalAccessMethodSource [JustReturnArrayOfString() as s] [apply: Cross] [type: String[]]
  PhysicalProject [i.Name as Name, s.Value as Text]
    PhysicalCteRef [is as is]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    SourceEntity [i: GeneratedApplySampleEntity]
      Name: string <- property Name
    SourceEntity [s: string]
      Value: string <- direct scalar value
    Generated [ResultRow0]
      Name: string <- field Name
      Text: string <- field Text

  Body
    CtePhase [cte0]
    SourceScan [i: GeneratedApplySampleEntity] -> iRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateObject [__resultLibrary0: Library]
    ChunkedForEach [i in iRows]
      EnumerableSource [JustReturnArrayOfString() -> sRows]
      ChunkedForEach [s in sRows]
        AppendShape [result <- ResultShape0(Name: i.Name, Text: s.Value)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q62_AccessMethodApply
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("Text", typeof(string), 1)
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
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.Text);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                var __iSchema = provider.GetSchema("#apply");
                var iRowsSource = __iSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>("items", new SourceExecutionContext("i:1", sourceExecutionPlans["i:1"], token, __schemaColumns_compiled_i_0, sourceRuntimeSettingsBySourceContextId["i:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var iRows = iRowsSource.Chunks;
                var __resultLibrary0 = new Musoq.Evaluator.Tests.Schema.Basic.Library();
                foreach (var iChunk in iRows)
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
                                var sRows = EvaluationHelper.ConvertEnumerableOutputToChunks<string>((string[])__resultLibrary0.JustReturnArrayOfString());
                                foreach (var sChunk in sRows)
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
                                                __musoqFinalShapeRows.Add(new ResultShape0(i.Name, s));
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
                                                __musoqFinalShapeRows.Add(new ResultShape0(i.Name, s));
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
                                        __musoqFinalShapeRows.Add(new ResultShape0(i.Name, s));
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
                                var sRows = EvaluationHelper.ConvertEnumerableOutputToChunks<string>((string[])__resultLibrary0.JustReturnArrayOfString());
                                foreach (var sChunk in sRows)
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
                                                __musoqFinalShapeRows.Add(new ResultShape0(i.Name, s));
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
                                                __musoqFinalShapeRows.Add(new ResultShape0(i.Name, s));
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
                                        __musoqFinalShapeRows.Add(new ResultShape0(i.Name, s));
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
                        var sRows = EvaluationHelper.ConvertEnumerableOutputToChunks<string>((string[])__resultLibrary0.JustReturnArrayOfString());
                        foreach (var sChunk in sRows)
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
                                        __musoqFinalShapeRows.Add(new ResultShape0(i.Name, s));
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
                                        __musoqFinalShapeRows.Add(new ResultShape0(i.Name, s));
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
                                __musoqFinalShapeRows.Add(new ResultShape0(i.Name, s));
                            }
                        }
                    }
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                Name = __value0;
                Text = __value1;
            }

            public override int Count => 2;
            public string Name { get; private set; }
            public string Text { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    case 1:
                        Text = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Text" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                1 => (object)Text,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                "Text" => (object)Text,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Name, string Text)
            {
                this.Name = Name;
                this.Text = Text;
            }

            public string Name { get; }
            public string Text { get; }
        }
    }
}
