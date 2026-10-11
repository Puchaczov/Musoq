// === Parsed Query ===
/*
select i.Name, s.Value from #apply.items() i cross apply i.JustReturnArrayOfString() s where s.Value rlike i.Name
*/

// === Logical Plan ===
/*
MultiStatement
  Project [i.Name as i.Name, s.Value as s.Value]
    Apply [Cross]
      SchemaScan [#apply.items() as i]
      AccessMethodSource [JustReturnArrayOfString() as s] [apply: Cross] [type: String[]]
  Project [i.Name as Name, s.Value as Value]
    Filter [s.Value RLIKE i.Name]
      CteRef [is as is]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [i.Name as i.Name, s.Value as s.Value]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#apply.items() as i]
      PhysicalAccessMethodSource [JustReturnArrayOfString() as s] [apply: Cross] [type: String[]]
  PhysicalProject [i.Name as Name, s.Value as Value]
    PhysicalFilter [s.Value RLIKE i.Name]
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
    Generated [ResultRow0]
      Name: string <- field Name
      Value: string <- field Value

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    SourceScan [i: GeneratedApplySampleEntity] -> iRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateObject [__resultLibrary0: Library]
    ChunkedForEach [i in iRows]
      Let [__rlikeMatcher0: PreparedRLikeMatcher = PREPARE_RLIKE(i.Name, strategy=runtime-classified, anchors=runtime, literal-span=runtime, fallback=runtime)]
      EnumerableSource [JustReturnArrayOfString() -> sRows]
      ChunkedForEach [s in sRows]
        Let [value: string = s.Value]
        If [PREPARED_RLIKE(value, __rlikeMatcher0)]
          AppendShape [result <- ResultRow0(Name: i.Name, Value: value)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q374_CorrelatedApplyRLike
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("Value", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_i_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return __musoqShapeRow;
            }
        }

        private IEnumerable<ResultRow0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                Musoq.Evaluator.Tests.Schema.Basic.Library __resultLibrary0 = default!;
                try
                {
                    OnPhaseChanged("compiled", QueryPhase.Where);
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    var __iSchema = provider.GetSchema("#apply");
                    var iRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>.Open(__iSchema, "items", new SourceExecutionContext("i:1", sourceExecutionPlans["i:1"], token, __schemaColumns_compiled_i_0, sourceRuntimeSettingsBySourceContextId["i:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                    var iRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Generated.GeneratedApplySampleEntity>(iRowsSource.Chunks, __musoqProgressContext, "i:1") : iRowsSource.Chunks;
                    __resultLibrary0 = new Musoq.Evaluator.Tests.Schema.Basic.Library();
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
                                    Musoq.Evaluator.PreparedRLikeMatcher __rlikeMatcher0 = Operators.PrepareRLike(i.Name, TimeSpan.FromTicks(20000000L));
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
                                                    string value = s;
                                                    if (Operators.RLikePrepared(value, __rlikeMatcher0))
                                                    {
                                                        __musoqFinalShapeRows.Add(new ResultRow0(i.Name, value));
                                                    }
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
                                                    string value = s;
                                                    if (Operators.RLikePrepared(value, __rlikeMatcher0))
                                                    {
                                                        __musoqFinalShapeRows.Add(new ResultRow0(i.Name, value));
                                                    }
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
                                            string value = s;
                                            if (Operators.RLikePrepared(value, __rlikeMatcher0))
                                            {
                                                __musoqFinalShapeRows.Add(new ResultRow0(i.Name, value));
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
                                    Musoq.Evaluator.PreparedRLikeMatcher __rlikeMatcher0 = Operators.PrepareRLike(i.Name, TimeSpan.FromTicks(20000000L));
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
                                                    string value = s;
                                                    if (Operators.RLikePrepared(value, __rlikeMatcher0))
                                                    {
                                                        __musoqFinalShapeRows.Add(new ResultRow0(i.Name, value));
                                                    }
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
                                                    string value = s;
                                                    if (Operators.RLikePrepared(value, __rlikeMatcher0))
                                                    {
                                                        __musoqFinalShapeRows.Add(new ResultRow0(i.Name, value));
                                                    }
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
                                            string value = s;
                                            if (Operators.RLikePrepared(value, __rlikeMatcher0))
                                            {
                                                __musoqFinalShapeRows.Add(new ResultRow0(i.Name, value));
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
                            Musoq.Evaluator.PreparedRLikeMatcher __rlikeMatcher0 = Operators.PrepareRLike(i.Name, TimeSpan.FromTicks(20000000L));
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
                                            string value = s;
                                            if (Operators.RLikePrepared(value, __rlikeMatcher0))
                                            {
                                                __musoqFinalShapeRows.Add(new ResultRow0(i.Name, value));
                                            }
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
                                            string value = s;
                                            if (Operators.RLikePrepared(value, __rlikeMatcher0))
                                            {
                                                __musoqFinalShapeRows.Add(new ResultRow0(i.Name, value));
                                            }
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
                                    string value = s;
                                    if (Operators.RLikePrepared(value, __rlikeMatcher0))
                                    {
                                        __musoqFinalShapeRows.Add(new ResultRow0(i.Name, value));
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                Name = __value0;
                Value = __value1;
            }

            public override int Count => 2;
            public string Name { get; private set; }
            public string Value { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "Value" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Value, ref visitor);
            }
        }
    }
}
