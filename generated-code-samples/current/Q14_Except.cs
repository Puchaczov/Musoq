// === Parsed Query ===
/*
select Name from #A.entities() except (Name) select Name from #A.entities()
*/

// === Logical Plan ===
/*
SetOp [Except]
  MultiStatement
    Project [ko3iko.Name as Name]
      SchemaScan [#A.entities() as ko3iko]
  MultiStatement
    Project [vo04qt.Name as Name]
      SchemaScan [#A.entities() as vo04qt]
*/

// === Physical Plan ===
/*
PhysicalSetOp [Except]
  PhysicalMultiStatement
    PhysicalProject [ko3iko.Name as Name]
      PhysicalSchemaScan [#A.entities() as ko3iko]
  PhysicalMultiStatement
    PhysicalProject [vo04qt.Name as Name]
      PhysicalSchemaScan [#A.entities() as vo04qt]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
    Generated [LeftRow0]
      Name: string <- field Name
    SourceEntity [vo04qt: BasicEntity]
      Name: string <- property Name
    Generated [RightRow0]
      Name: string <- field Name

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:left]
    PhaseBoundary [From:left]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> left_ko3ikoRows
    CreateRowBuffer [left: List<LeftRow0>]
    PhaseBoundary [Select:left]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in left_ko3ikoRows]
      AppendRowBuffer [left <- LeftRow0(Name: ko3iko.Name)]
    PhaseBoundary [End:left]
    PhaseBoundary [Begin:right]
    PhaseBoundary [From:right]
    SourceScan [vo04qt: BasicEntity] -> right_vo04qtRows
    CreateRowBuffer [right: List<RightRow0>]
    PhaseBoundary [Select:right]
    ChunkedForEach [vo04qt in right_vo04qtRows]
      AppendRowBuffer [right <- RightRow0(Name: vo04qt.Name)]
    PhaseBoundary [End:right]
    SetOperation [result = left Except right, HashSet]
    ReturnDeferredTable [result: LeftRow0 <- LeftShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q14_Except
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
        private static readonly Column[] __columns_compiled_left_1 = new Column[]
        {
            new Column("Name", typeof(string), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0) });
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
            return QueryRows.DeferredTable<LeftRow0>("result", __columns_compiled_left_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<LeftRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new LeftRow0(__musoqShapeRow.Name);
            }
        }

        private IEnumerable<LeftShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<LeftShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled:left", QueryPhase.Begin);
                OnPhaseChanged("compiled:left", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __left_ko3ikoSchema = provider.GetSchema("#A");
                var left_ko3ikoRowsSource = __left_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var left_ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(left_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : left_ko3ikoRowsSource.Chunks;
                var left = new List<LeftRow0>();
                OnPhaseChanged("compiled:left", QueryPhase.Select);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in left_ko3ikoRows)
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
                                left.Add(new LeftRow0(ko3iko.Name));
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
                                left.Add(new LeftRow0(ko3iko.Name));
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
                        left.Add(new LeftRow0(ko3iko.Name));
                    }
                }

                OnPhaseChanged("compiled:left", QueryPhase.End);
                OnPhaseChanged("compiled:right", QueryPhase.Begin);
                OnPhaseChanged("compiled:right", QueryPhase.From);
                var __right_vo04qtSchema = provider.GetSchema("#A");
                var right_vo04qtRowsSource = __right_vo04qtSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("vo04qt:2", sourceExecutionPlans["vo04qt:2"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["vo04qt:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                var right_vo04qtRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(right_vo04qtRowsSource.Chunks, __musoqProgressContext, "vo04qt:2") : right_vo04qtRowsSource.Chunks;
                var right = new List<RightRow0>();
                OnPhaseChanged("compiled:right", QueryPhase.Select);
                foreach (var vo04qtChunk in right_vo04qtRows)
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
                                right.Add(new RightRow0(vo04qt.Name));
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
                                right.Add(new RightRow0(vo04qt.Name));
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
                        right.Add(new RightRow0(vo04qt.Name));
                    }
                }

                OnPhaseChanged("compiled:right", QueryPhase.End);
                var resultRightKeys = new HashSet<string>(right.Count);
                foreach (var resultRightRow in right)
                {
                    resultRightKeys.Add((string)resultRightRow.Name);
                }

                foreach (var resultLeftRow in left)
                {
                    if (!resultRightKeys.Contains((string)resultLeftRow.Name))
                    {
                        __musoqFinalShapeRows.Add(new LeftShape0((string)resultLeftRow.Name));
                    }
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

        private sealed class LeftRow0 : Row
        {
            public LeftRow0(string __value0)
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

        private sealed class LeftShape0
        {
            public LeftShape0(string Name)
            {
                this.Name = Name;
            }

            public string Name { get; }
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
