// === Parsed Query ===
/*
select Name as Label from #A.entities() union () select Name from #B.entities() union all select Name from #C.entities() order by Label desc skip 1 take 3
*/

// === Logical Plan ===
/*
Take [3]
  Skip [1]
    Sort [Label DESC]
      SetOp [UnionAll]
        SetOp [Union]
          MultiStatement
            Project [ko3iko.Name as Label]
              SchemaScan [#A.entities() as ko3iko]
          MultiStatement
            Project [vo04qt.Name as Name]
              SchemaScan [#B.entities() as vo04qt]
        MultiStatement
          Project [gougbq.Name as Name]
            SchemaScan [#C.entities() as gougbq]
*/

// === Physical Plan ===
/*
PhysicalTopOffset [skip 1, take 3] [Label DESC]
  PhysicalSetOp [UnionAll]
    PhysicalSetOp [Union]
      PhysicalMultiStatement
        PhysicalProject [ko3iko.Name as Label]
          PhysicalSchemaScan [#A.entities() as ko3iko]
      PhysicalMultiStatement
        PhysicalProject [vo04qt.Name as Name]
          PhysicalSchemaScan [#B.entities() as vo04qt]
    PhysicalMultiStatement
      PhysicalProject [gougbq.Name as Name]
        PhysicalSchemaScan [#C.entities() as gougbq]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
    Generated [LeftLeftRow0]
      Label: string <- field Label
    SourceEntity [vo04qt: BasicEntity]
      Name: string <- property Name
    Generated [LeftRightRow0]
      Name: string <- field Name
    SourceEntity [gougbq: BasicEntity]
      Name: string <- property Name
    Generated [RightRow0]
      Name: string <- field Name

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:left]
    PhaseBoundary [Begin:left]
    PhaseBoundary [From:left]
    PhaseBoundary [From:left]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> leftLeft_ko3ikoRows
    CreateRowBuffer [leftLeft: List<LeftLeftRow0>]
    PhaseBoundary [Select:left]
    PhaseBoundary [Select:left]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in leftLeft_ko3ikoRows]
      AppendRowBuffer [leftLeft <- LeftLeftRow0(Label: ko3iko.Name)]
    PhaseBoundary [End:left]
    PhaseBoundary [Begin:right]
    PhaseBoundary [From:right]
    SourceScan [vo04qt: BasicEntity] -> leftRight_vo04qtRows
    CreateRowBuffer [leftRight: List<LeftRightRow0>]
    PhaseBoundary [Select:right]
    ChunkedForEach [vo04qt in leftRight_vo04qtRows]
      AppendRowBuffer [leftRight <- LeftRightRow0(Name: vo04qt.Name)]
    PhaseBoundary [End:right]
    SetOperation [left = leftLeft Union leftRight, HashSet]
    PhaseBoundary [End:left]
    PhaseBoundary [Begin:right]
    PhaseBoundary [From:right]
    SourceScan [gougbq: BasicEntity] -> right_gougbqRows
    CreateRowBuffer [right: List<RightRow0>]
    PhaseBoundary [Select:right]
    ChunkedForEach [gougbq in right_gougbqRows]
      AppendRowBuffer [right <- RightRow0(Name: gougbq.Name)]
    PhaseBoundary [End:right]
    SetOperation [result = left UnionAll right, AppendLoop]
    TopOffsetRowBuffer [result -> resultTopOffset by Label DESC, skip 1, take 3, BoundedHeap]
    ReturnDeferredTable [resultTopOffset: LeftLeftRow0 <- LeftLeftShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q291_SpecCoreSetResultModifiers
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
        private static readonly Column[] __columns_compiled_leftLeft_1 = new Column[]
        {
            new Column("Label", typeof(string), 0)
        };
        private static readonly Column[] __columns_compiled_leftRight_2 = new Column[]
        {
            new Column("Name", typeof(string), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10) });
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
            return QueryRows.DeferredTable<LeftLeftRow0>("resultTopOffset", __columns_compiled_leftLeft_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<LeftLeftRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new LeftLeftRow0(__musoqShapeRow.Label);
            }
        }

        private IEnumerable<LeftLeftShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<LeftLeftShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled:left", QueryPhase.Begin);
                OnPhaseChanged("compiled:left", QueryPhase.Begin);
                OnPhaseChanged("compiled:left", QueryPhase.From);
                OnPhaseChanged("compiled:left", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __leftLeft_ko3ikoSchema = provider.GetSchema("#A");
                var leftLeft_ko3ikoRowsSource = __leftLeft_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var leftLeft_ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(leftLeft_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : leftLeft_ko3ikoRowsSource.Chunks;
                var leftLeft = new List<LeftLeftRow0>();
                OnPhaseChanged("compiled:left", QueryPhase.Select);
                OnPhaseChanged("compiled:left", QueryPhase.Select);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in leftLeft_ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                leftLeft.Add(new LeftLeftRow0(ko3iko.Name));
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                leftLeft.Add(new LeftLeftRow0(ko3iko.Name));
                            }

                            continue;
                        }
                    }

                    for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunk.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                    {
                        if ((ko3ikoIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var ko3iko = ko3ikoChunk[ko3ikoIndex];
                        leftLeft.Add(new LeftLeftRow0(ko3iko.Name));
                    }
                }

                OnPhaseChanged("compiled:left", QueryPhase.End);
                OnPhaseChanged("compiled:right", QueryPhase.Begin);
                OnPhaseChanged("compiled:right", QueryPhase.From);
                var __leftRight_vo04qtSchema = provider.GetSchema("#B");
                var leftRight_vo04qtRowsSource = __leftRight_vo04qtSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("vo04qt:2", sourceExecutionPlans["vo04qt:2"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["vo04qt:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                var leftRight_vo04qtRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(leftRight_vo04qtRowsSource.Chunks, __musoqProgressContext, "vo04qt:2") : leftRight_vo04qtRowsSource.Chunks;
                var leftRight = new List<LeftRightRow0>();
                OnPhaseChanged("compiled:right", QueryPhase.Select);
                foreach (var vo04qtChunk in leftRight_vo04qtRows)
                {
                    if (vo04qtChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> vo04qtChunkView)
                    {
                        if (vo04qtChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] vo04qtChunkViewArray)
                        {
                            int vo04qtChunkViewOffset = vo04qtChunkView.Offset;
                            for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunkView.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                            {
                                if ((vo04qtIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var vo04qt = vo04qtChunkViewArray[vo04qtChunkViewOffset + vo04qtIndex];
                                leftRight.Add(new LeftRightRow0(vo04qt.Name));
                            }

                            continue;
                        }

                        if (vo04qtChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> vo04qtChunkViewList)
                        {
                            int vo04qtChunkViewOffset = vo04qtChunkView.Offset;
                            for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunkView.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                            {
                                if ((vo04qtIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var vo04qt = vo04qtChunkViewList[vo04qtChunkViewOffset + vo04qtIndex];
                                leftRight.Add(new LeftRightRow0(vo04qt.Name));
                            }

                            continue;
                        }
                    }

                    for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunk.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                    {
                        if ((vo04qtIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var vo04qt = vo04qtChunk[vo04qtIndex];
                        leftRight.Add(new LeftRightRow0(vo04qt.Name));
                    }
                }

                OnPhaseChanged("compiled:right", QueryPhase.End);
                var left = new List<LeftLeftRow0>();
                var leftKeys = new HashSet<string>(leftLeft.Count + leftRight.Count);
                foreach (var leftLeftRow in leftLeft)
                {
                    if (leftKeys.Add((string)leftLeftRow.Label))
                    {
                        left.Add(leftLeftRow);
                    }
                }

                foreach (var leftRightRow in leftRight)
                {
                    if (leftKeys.Add((string)leftRightRow.Name))
                    {
                        left.Add(new LeftLeftRow0((string)leftRightRow.Name));
                    }
                }

                OnPhaseChanged("compiled:left", QueryPhase.End);
                OnPhaseChanged("compiled:right", QueryPhase.Begin);
                OnPhaseChanged("compiled:right", QueryPhase.From);
                var __right_gougbqSchema = provider.GetSchema("#C");
                var right_gougbqRowsSource = __right_gougbqSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("gougbq:3", sourceExecutionPlans["gougbq:3"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["gougbq:3"], logger, OnDataSourceProgress), Array.Empty<object>());
                var right_gougbqRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(right_gougbqRowsSource.Chunks, __musoqProgressContext, "gougbq:3") : right_gougbqRowsSource.Chunks;
                var right = new List<RightRow0>();
                OnPhaseChanged("compiled:right", QueryPhase.Select);
                foreach (var gougbqChunk in right_gougbqRows)
                {
                    if (gougbqChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> gougbqChunkView)
                    {
                        if (gougbqChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] gougbqChunkViewArray)
                        {
                            int gougbqChunkViewOffset = gougbqChunkView.Offset;
                            for (int gougbqIndex = 0, gougbqIndexCount = gougbqChunkView.Count; gougbqIndex < gougbqIndexCount; ++gougbqIndex)
                            {
                                if ((gougbqIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var gougbq = gougbqChunkViewArray[gougbqChunkViewOffset + gougbqIndex];
                                right.Add(new RightRow0(gougbq.Name));
                            }

                            continue;
                        }

                        if (gougbqChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> gougbqChunkViewList)
                        {
                            int gougbqChunkViewOffset = gougbqChunkView.Offset;
                            for (int gougbqIndex = 0, gougbqIndexCount = gougbqChunkView.Count; gougbqIndex < gougbqIndexCount; ++gougbqIndex)
                            {
                                if ((gougbqIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var gougbq = gougbqChunkViewList[gougbqChunkViewOffset + gougbqIndex];
                                right.Add(new RightRow0(gougbq.Name));
                            }

                            continue;
                        }
                    }

                    for (int gougbqIndex = 0, gougbqIndexCount = gougbqChunk.Count; gougbqIndex < gougbqIndexCount; ++gougbqIndex)
                    {
                        if ((gougbqIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var gougbq = gougbqChunk[gougbqIndex];
                        right.Add(new RightRow0(gougbq.Name));
                    }
                }

                OnPhaseChanged("compiled:right", QueryPhase.End);
                var result = new List<LeftLeftShape0>(left.Count + right.Count);
                foreach (var resultLeftRow in left)
                {
                    result.Add(new LeftLeftShape0((string)resultLeftRow.Label));
                }

                foreach (var resultRightRow in right)
                {
                    result.Add(new LeftLeftShape0((string)resultRightRow.Name));
                }

                var resultTopOffsetRows = EvaluationHelper.SelectTopOffsetRecords(result, 1, 3, Comparer<LeftLeftShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.Label, right.Label);
                    comparison = -comparison;
                    if (comparison != 0)
                        return comparison;
                    return 0;
                }));
                foreach (var resultTopOffsetRowsRow in resultTopOffsetRows)
                {
                    __musoqFinalShapeRows.Add(resultTopOffsetRowsRow);
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

        private sealed class LeftLeftRow0 : Row
        {
            public LeftLeftRow0(string __value0)
            {
                Label = __value0;
            }

            public override int Count => 1;
            public string Label { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Label = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Label" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Label,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Label" => (object)Label,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class LeftLeftShape0
        {
            public LeftLeftShape0(string Label)
            {
                this.Label = Label;
            }

            public string Label { get; }
        }

        private sealed class LeftRightRow0 : Row
        {
            public LeftRightRow0(string __value0)
            {
                Name = __value0;
            }

            public override int Count => 1;
            public string Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class RightRow0 : Row
        {
            public RightRow0(string __value0)
            {
                Name = __value0;
            }

            public override int Count => 1;
            public string Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Name = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Name,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Name" => (object)Name,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
