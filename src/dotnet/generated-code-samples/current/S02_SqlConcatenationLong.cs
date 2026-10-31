// === Parsed Query ===
/*
select Name || City || Name || City || Name || City || Name || City as A from #A.entities()
*/

// === Logical Plan ===
/*
MultiStatement
  Project [(((((((ko3iko.Name || ko3iko.City) || ko3iko.Name) || ko3iko.City) || ko3iko.Name) || ko3iko.City) || ko3iko.Name) || ko3iko.City) as A]
    SchemaScan [#A.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [(((((((ko3iko.Name || ko3iko.City) || ko3iko.Name) || ko3iko.City) || ko3iko.Name) || ko3iko.City) || ko3iko.Name) || ko3iko.City) as A]
    PhysicalSchemaScan [#A.entities() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
      City: string <- property City
    Generated [ResultRow0]
      A: string <- field A

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [name: string = ko3iko.Name]
      Let [city: string = ko3iko.City]
      AppendShape [result <- ResultRow0(A: (name || city || name || city || name || city || name || city))]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_S02_SqlConcatenationLong
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
            new Column("A", typeof(string), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("City", typeof(string), 1) });
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
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#A");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__ko3ikoSchema, "entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in ko3ikoRows)
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
                                string name = ko3iko.Name;
                                string city = ko3iko.City;
                                yield return new ResultRow0((((string)name)is var __musoqConcat0_0 && ((string)city)is var __musoqConcat0_1 && ((string)name)is var __musoqConcat0_2 && ((string)city)is var __musoqConcat0_3 && ((string)name)is var __musoqConcat0_4 && ((string)city)is var __musoqConcat0_5 && ((string)name)is var __musoqConcat0_6 && ((string)city)is var __musoqConcat0_7 && __musoqConcat0_0 != null && __musoqConcat0_1 != null && __musoqConcat0_2 != null && __musoqConcat0_3 != null && __musoqConcat0_4 != null && __musoqConcat0_5 != null && __musoqConcat0_6 != null && __musoqConcat0_7 != null ? global::System.String.Create(checked(__musoqConcat0_0.Length + __musoqConcat0_1.Length + __musoqConcat0_2.Length + __musoqConcat0_3.Length + __musoqConcat0_4.Length + __musoqConcat0_5.Length + __musoqConcat0_6.Length + __musoqConcat0_7.Length), (__musoqConcat0_0, __musoqConcat0_1, __musoqConcat0_2, __musoqConcat0_3, __musoqConcat0_4, __musoqConcat0_5, __musoqConcat0_6, __musoqConcat0_7), __MusoqQueryConcat.Write8) : (string)null));
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
                                string name = ko3iko.Name;
                                string city = ko3iko.City;
                                yield return new ResultRow0((((string)name)is var __musoqConcat1_0 && ((string)city)is var __musoqConcat1_1 && ((string)name)is var __musoqConcat1_2 && ((string)city)is var __musoqConcat1_3 && ((string)name)is var __musoqConcat1_4 && ((string)city)is var __musoqConcat1_5 && ((string)name)is var __musoqConcat1_6 && ((string)city)is var __musoqConcat1_7 && __musoqConcat1_0 != null && __musoqConcat1_1 != null && __musoqConcat1_2 != null && __musoqConcat1_3 != null && __musoqConcat1_4 != null && __musoqConcat1_5 != null && __musoqConcat1_6 != null && __musoqConcat1_7 != null ? global::System.String.Create(checked(__musoqConcat1_0.Length + __musoqConcat1_1.Length + __musoqConcat1_2.Length + __musoqConcat1_3.Length + __musoqConcat1_4.Length + __musoqConcat1_5.Length + __musoqConcat1_6.Length + __musoqConcat1_7.Length), (__musoqConcat1_0, __musoqConcat1_1, __musoqConcat1_2, __musoqConcat1_3, __musoqConcat1_4, __musoqConcat1_5, __musoqConcat1_6, __musoqConcat1_7), __MusoqQueryConcat.Write8) : (string)null));
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
                        string name = ko3iko.Name;
                        string city = ko3iko.City;
                        yield return new ResultRow0((((string)name)is var __musoqConcat2_0 && ((string)city)is var __musoqConcat2_1 && ((string)name)is var __musoqConcat2_2 && ((string)city)is var __musoqConcat2_3 && ((string)name)is var __musoqConcat2_4 && ((string)city)is var __musoqConcat2_5 && ((string)name)is var __musoqConcat2_6 && ((string)city)is var __musoqConcat2_7 && __musoqConcat2_0 != null && __musoqConcat2_1 != null && __musoqConcat2_2 != null && __musoqConcat2_3 != null && __musoqConcat2_4 != null && __musoqConcat2_5 != null && __musoqConcat2_6 != null && __musoqConcat2_7 != null ? global::System.String.Create(checked(__musoqConcat2_0.Length + __musoqConcat2_1.Length + __musoqConcat2_2.Length + __musoqConcat2_3.Length + __musoqConcat2_4.Length + __musoqConcat2_5.Length + __musoqConcat2_6.Length + __musoqConcat2_7.Length), (__musoqConcat2_0, __musoqConcat2_1, __musoqConcat2_2, __musoqConcat2_3, __musoqConcat2_4, __musoqConcat2_5, __musoqConcat2_6, __musoqConcat2_7), __MusoqQueryConcat.Write8) : (string)null));
                    }
                }
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
            public ResultRow0(string __value0)
            {
                A = __value0;
            }

            public string A { get; private set; }
            public override int Count => 1;

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.A = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.A),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "A" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.A, ref visitor);
            }
        }

        internal static class __MusoqQueryConcat
        {
            internal static readonly global::System.Buffers.SpanAction<char, (string, string, string, string, string, string, string, string)> Write8 = Write8Core;
            static __MusoqQueryConcat()
            {
            }

            [global::System.Runtime.CompilerServices.ModuleInitializer]
            internal static void Initialize()
            {
                _ = Write8;
            }

            private static void Write8Core(global::System.Span<char> destination, (string, string, string, string, string, string, string, string) state)
            {
                global::System.MemoryExtensions.AsSpan(state.Item1).CopyTo(destination);
                destination = destination.Slice(state.Item1.Length);
                global::System.MemoryExtensions.AsSpan(state.Item2).CopyTo(destination);
                destination = destination.Slice(state.Item2.Length);
                global::System.MemoryExtensions.AsSpan(state.Item3).CopyTo(destination);
                destination = destination.Slice(state.Item3.Length);
                global::System.MemoryExtensions.AsSpan(state.Item4).CopyTo(destination);
                destination = destination.Slice(state.Item4.Length);
                global::System.MemoryExtensions.AsSpan(state.Item5).CopyTo(destination);
                destination = destination.Slice(state.Item5.Length);
                global::System.MemoryExtensions.AsSpan(state.Item6).CopyTo(destination);
                destination = destination.Slice(state.Item6.Length);
                global::System.MemoryExtensions.AsSpan(state.Item7).CopyTo(destination);
                destination = destination.Slice(state.Item7.Length);
                global::System.MemoryExtensions.AsSpan(state.Item8).CopyTo(destination);
            }
        }
    }
}
