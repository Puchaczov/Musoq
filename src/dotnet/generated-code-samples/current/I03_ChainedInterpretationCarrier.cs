// === Parsed Query ===
/*
binary SampleReading { Score: int le } select batch.Label, reading.Score from #fixture.batches('demo', false) batch cross apply #fixture.samples(batch.Key) sample cross apply Interpret<SampleReading>(sample.ReadPayload()) reading
*/

// === Logical Plan ===
/*
MultiStatement
  Project [batch.Label as batch.Label, batch.Key as batch.Key]
    Apply [Cross]
      SchemaScan [#fixture.batches('demo', FALSE) as batch]
      SchemaScan [#fixture.samples(batch.Key) as sample]
  Project [batchsample.batch.Label as batch.Label, batchsample.batch.Key as batch.Key, reading.Score as reading.Score]
    Apply [Cross]
      CteRef [batchsample as batchsample]
      InterpretSource [#SampleReading(ReadPayload()) as reading]
  Project [batch.Label as Label, reading.Score as Score]
    CteRef [batchsamplereading as batchsamplereading]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [batch.Label as batch.Label, batch.Key as batch.Key]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#fixture.batches('demo', FALSE) as batch]
      PhysicalSchemaScan [#fixture.samples(batch.Key) as sample]
  PhysicalProject [batchsample.batch.Label as batch.Label, batchsample.batch.Key as batch.Key, reading.Score as reading.Score]
    PhysicalNestedLoopApply [Cross]
      PhysicalCteRef [batchsample as batchsample]
      PhysicalInterpretSource [#SampleReading(ReadPayload()) as reading]
  PhysicalProject [batch.Label as Label, reading.Score as Score]
    PhysicalCteRef [batchsamplereading as batchsamplereading]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [batch: ApplyCarrierBatch]
      Label: string <- property Label
      Key: string <- property Key
    SourceEntity [sample: ApplyCarrierSample]
      Key: string <- property Key
      Payload: byte[] <- property Payload
    Generated [Statement0Row0]
      batch.Label: string <- field batch_Label
      batch.Key: string <- field batch_Key
    TableRow [batchsample]
      batch.Label: string <- field batch_Label
      batch.Key: string <- field batch_Key
    SourceEntity [reading: object]
      Score: int <- property Score
    Generated [ResultRow0]
      Label: string <- field Label
      Score: int <- field Score

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [batch: ApplyCarrierBatch] -> statement0_batchRows
    CreateTable [statement0: Statement0Row0]
    ChunkedForEach [batch in statement0_batchRows]
      Let [batchKey: string = batch.Key]
      Let [batchLabel: string = batch.Label]
      SourceScan [sample: ApplyCarrierSample] -> statement0_sampleRows
      ChunkedForEach [sample in statement0_sampleRows]
        AppendRow [statement0 <- Statement0Row0(batch.Label: batchLabel, batch.Key: batchKey)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateObject [__resultApplyCarrierLibrary0: ApplyCarrierLibrary]
    ForEach [batchsample in _cteRowResults.Slot0]
      Let [batchsamplebatchLabel: string = batchsample.batch.Label]
      InterpretSource [SampleReading.Interpret(ReadPayload()) -> readingRows]
      ScalarForEach [reading in readingRows]
        AppendShape [result <- ResultRow0(Label: batchsamplebatchLabel, Score: reading.Score)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_I03_ChainedInterpretationCarrier
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
        private static readonly Column[] __columns_compiled_result_3 = new Column[]
        {
            new Column("Label", typeof(string), 0),
            new Column("Score", typeof(int), 1)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("batch.Label", typeof(string), 0),
            new Column("batch.Key", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_batch_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Label", typeof(string), 0), new Column("Key", typeof(string), 1) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_sample_2 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Key", typeof(string), 0), new Column("Payload", typeof(byte[]), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
                Musoq.Tests.Common.ApplyCarrierLibrary __resultApplyCarrierLibrary0 = default!;
                try
                {
                    __resultApplyCarrierLibrary0 = new Musoq.Tests.Common.ApplyCarrierLibrary();
                    var __storedTable0Rows = _cteRowResults.Slot0;
                    for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                    {
                        if ((__storedTable0Index & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        Statement0Row0 batchsample = __storedTable0Rows[__storedTable0Index];
                        string batchsamplebatchLabel = batchsample.batch_Label;
                        var _interpreter_SampleReading = new Musoq.Generated.Interpreters.SampleReading();
                        var readingRows = _interpreter_SampleReading.Interpret((byte[])__resultApplyCarrierLibrary0.ReadPayload((Musoq.Tests.Common.ApplyCarrierSample)(Musoq.Tests.Common.ApplyCarrierSample)batchsample.__rightContext));
                        if (readingRows != null)
                        {
                            token.ThrowIfCancellationRequested();
                            var reading = readingRows;
                            __musoqFinalShapeRows.Add(new ResultRow0(batchsamplebatchLabel, reading.Score));
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte1", QueryPhase.End);
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
            var __statement0_batchSchema = provider.GetSchema("#fixture");
            var statement0_batchRowsSource = SourceInvocation<Musoq.Tests.Common.ApplyCarrierBatch>.Open(__statement0_batchSchema, "batches", new SourceExecutionContext("batch:1", sourceExecutionPlans["batch:1"], token, __schemaColumns_compiled_batch_0, sourceRuntimeSettingsBySourceContextId["batch:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Create((string)"demo", global::Musoq.Schema.SourceArguments.Create((bool)false, global::Musoq.Schema.SourceArguments.Empty)));
            var statement0_batchRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Tests.Common.ApplyCarrierBatch>(statement0_batchRowsSource.Chunks, __musoqProgressContext, "batch:1") : statement0_batchRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            foreach (var batchChunk in statement0_batchRows)
            {
                if (batchChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Tests.Common.ApplyCarrierBatch> batchChunkView)
                {
                    if (batchChunkView.Source is Musoq.Tests.Common.ApplyCarrierBatch[] batchChunkViewArray)
                    {
                        int batchChunkViewOffset = batchChunkView.Offset;
                        for (int batchIndex = 0, batchIndexCount = batchChunkView.Count; batchIndex < batchIndexCount; ++batchIndex)
                        {
                            if ((batchIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var batch = batchChunkViewArray[batchChunkViewOffset + batchIndex];
                            string batchKey = batch.Key;
                            string batchLabel = batch.Label;
                            var __statement0_sampleSchema = provider.GetSchema("#fixture");
                            var statement0_sampleRowsSource = SourceInvocation<Musoq.Tests.Common.ApplyCarrierSample>.Open(__statement0_sampleSchema, "samples", new SourceExecutionContext("sample:1", sourceExecutionPlans["sample:1"], token, __schemaColumns_compiled_sample_2, sourceRuntimeSettingsBySourceContextId["sample:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Create(batch.Key, global::Musoq.Schema.SourceArguments.Empty));
                            var statement0_sampleRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Tests.Common.ApplyCarrierSample>(statement0_sampleRowsSource.Chunks, __musoqProgressContext, "sample:1") : statement0_sampleRowsSource.Chunks;
                            foreach (var sampleChunk in statement0_sampleRows)
                            {
                                if (sampleChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Tests.Common.ApplyCarrierSample> sampleChunkView)
                                {
                                    if (sampleChunkView.Source is Musoq.Tests.Common.ApplyCarrierSample[] sampleChunkViewArray)
                                    {
                                        int sampleChunkViewOffset = sampleChunkView.Offset;
                                        for (int sampleIndex = 0, sampleIndexCount = sampleChunkView.Count; sampleIndex < sampleIndexCount; ++sampleIndex)
                                        {
                                            if ((sampleIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var sample = sampleChunkViewArray[sampleChunkViewOffset + sampleIndex];
                                            statement0.Add(new Statement0Row0(batchLabel, batchKey, batch, sample));
                                        }

                                        continue;
                                    }

                                    if (sampleChunkView.Source is List<Musoq.Tests.Common.ApplyCarrierSample> sampleChunkViewList)
                                    {
                                        int sampleChunkViewOffset = sampleChunkView.Offset;
                                        for (int sampleIndex = 0, sampleIndexCount = sampleChunkView.Count; sampleIndex < sampleIndexCount; ++sampleIndex)
                                        {
                                            if ((sampleIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var sample = sampleChunkViewList[sampleChunkViewOffset + sampleIndex];
                                            statement0.Add(new Statement0Row0(batchLabel, batchKey, batch, sample));
                                        }

                                        continue;
                                    }
                                }

                                for (int sampleIndex = 0, sampleIndexCount = sampleChunk.Count; sampleIndex < sampleIndexCount; ++sampleIndex)
                                {
                                    if ((sampleIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var sample = sampleChunk[sampleIndex];
                                    statement0.Add(new Statement0Row0(batchLabel, batchKey, batch, sample));
                                }
                            }
                        }

                        continue;
                    }

                    if (batchChunkView.Source is List<Musoq.Tests.Common.ApplyCarrierBatch> batchChunkViewList)
                    {
                        int batchChunkViewOffset = batchChunkView.Offset;
                        for (int batchIndex = 0, batchIndexCount = batchChunkView.Count; batchIndex < batchIndexCount; ++batchIndex)
                        {
                            if ((batchIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var batch = batchChunkViewList[batchChunkViewOffset + batchIndex];
                            string batchKey = batch.Key;
                            string batchLabel = batch.Label;
                            var __statement0_sampleSchema = provider.GetSchema("#fixture");
                            var statement0_sampleRowsSource = SourceInvocation<Musoq.Tests.Common.ApplyCarrierSample>.Open(__statement0_sampleSchema, "samples", new SourceExecutionContext("sample:1", sourceExecutionPlans["sample:1"], token, __schemaColumns_compiled_sample_2, sourceRuntimeSettingsBySourceContextId["sample:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Create(batch.Key, global::Musoq.Schema.SourceArguments.Empty));
                            var statement0_sampleRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Tests.Common.ApplyCarrierSample>(statement0_sampleRowsSource.Chunks, __musoqProgressContext, "sample:1") : statement0_sampleRowsSource.Chunks;
                            foreach (var sampleChunk in statement0_sampleRows)
                            {
                                if (sampleChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Tests.Common.ApplyCarrierSample> sampleChunkView)
                                {
                                    if (sampleChunkView.Source is Musoq.Tests.Common.ApplyCarrierSample[] sampleChunkViewArray)
                                    {
                                        int sampleChunkViewOffset = sampleChunkView.Offset;
                                        for (int sampleIndex = 0, sampleIndexCount = sampleChunkView.Count; sampleIndex < sampleIndexCount; ++sampleIndex)
                                        {
                                            if ((sampleIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var sample = sampleChunkViewArray[sampleChunkViewOffset + sampleIndex];
                                            statement0.Add(new Statement0Row0(batchLabel, batchKey, batch, sample));
                                        }

                                        continue;
                                    }

                                    if (sampleChunkView.Source is List<Musoq.Tests.Common.ApplyCarrierSample> sampleChunkViewList)
                                    {
                                        int sampleChunkViewOffset = sampleChunkView.Offset;
                                        for (int sampleIndex = 0, sampleIndexCount = sampleChunkView.Count; sampleIndex < sampleIndexCount; ++sampleIndex)
                                        {
                                            if ((sampleIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var sample = sampleChunkViewList[sampleChunkViewOffset + sampleIndex];
                                            statement0.Add(new Statement0Row0(batchLabel, batchKey, batch, sample));
                                        }

                                        continue;
                                    }
                                }

                                for (int sampleIndex = 0, sampleIndexCount = sampleChunk.Count; sampleIndex < sampleIndexCount; ++sampleIndex)
                                {
                                    if ((sampleIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var sample = sampleChunk[sampleIndex];
                                    statement0.Add(new Statement0Row0(batchLabel, batchKey, batch, sample));
                                }
                            }
                        }

                        continue;
                    }
                }

                for (int batchIndex = 0, batchIndexCount = batchChunk.Count; batchIndex < batchIndexCount; ++batchIndex)
                {
                    if ((batchIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var batch = batchChunk[batchIndex];
                    string batchKey = batch.Key;
                    string batchLabel = batch.Label;
                    var __statement0_sampleSchema = provider.GetSchema("#fixture");
                    var statement0_sampleRowsSource = SourceInvocation<Musoq.Tests.Common.ApplyCarrierSample>.Open(__statement0_sampleSchema, "samples", new SourceExecutionContext("sample:1", sourceExecutionPlans["sample:1"], token, __schemaColumns_compiled_sample_2, sourceRuntimeSettingsBySourceContextId["sample:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Create(batch.Key, global::Musoq.Schema.SourceArguments.Empty));
                    var statement0_sampleRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Tests.Common.ApplyCarrierSample>(statement0_sampleRowsSource.Chunks, __musoqProgressContext, "sample:1") : statement0_sampleRowsSource.Chunks;
                    foreach (var sampleChunk in statement0_sampleRows)
                    {
                        if (sampleChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Tests.Common.ApplyCarrierSample> sampleChunkView)
                        {
                            if (sampleChunkView.Source is Musoq.Tests.Common.ApplyCarrierSample[] sampleChunkViewArray)
                            {
                                int sampleChunkViewOffset = sampleChunkView.Offset;
                                for (int sampleIndex = 0, sampleIndexCount = sampleChunkView.Count; sampleIndex < sampleIndexCount; ++sampleIndex)
                                {
                                    if ((sampleIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var sample = sampleChunkViewArray[sampleChunkViewOffset + sampleIndex];
                                    statement0.Add(new Statement0Row0(batchLabel, batchKey, batch, sample));
                                }

                                continue;
                            }

                            if (sampleChunkView.Source is List<Musoq.Tests.Common.ApplyCarrierSample> sampleChunkViewList)
                            {
                                int sampleChunkViewOffset = sampleChunkView.Offset;
                                for (int sampleIndex = 0, sampleIndexCount = sampleChunkView.Count; sampleIndex < sampleIndexCount; ++sampleIndex)
                                {
                                    if ((sampleIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var sample = sampleChunkViewList[sampleChunkViewOffset + sampleIndex];
                                    statement0.Add(new Statement0Row0(batchLabel, batchKey, batch, sample));
                                }

                                continue;
                            }
                        }

                        for (int sampleIndex = 0, sampleIndexCount = sampleChunk.Count; sampleIndex < sampleIndexCount; ++sampleIndex)
                        {
                            if ((sampleIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var sample = sampleChunk[sampleIndex];
                            statement0.Add(new Statement0Row0(batchLabel, batchKey, batch, sample));
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
            public ResultRow0(string __value0, int __value1)
            {
                Label = __value0;
                Score = __value1;
            }

            public override int Count => 2;
            public string Label { get; private set; }
            public int Score { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Label = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Score = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Label),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Score),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Label" => true,
                "Score" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Label, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Score, ref visitor);
            }
        }

        private sealed class Statement0Row0
        {
            public readonly Musoq.Tests.Common.ApplyCarrierBatch __leftContext;
            public readonly Musoq.Tests.Common.ApplyCarrierSample __rightContext;
            public Statement0Row0(string __value0, string __value1, Musoq.Tests.Common.ApplyCarrierBatch __leftContext, Musoq.Tests.Common.ApplyCarrierSample __rightContext)
            {
                batch_Label = __value0;
                batch_Key = __value1;
                this.__leftContext = __leftContext;
                this.__rightContext = __rightContext;
            }

            public string batch_Key { get; }
            public string batch_Label { get; }
        }
    }
}

// === SyntaxTree:  ===
// <auto-generated>
// This code was generated by Musoq Interpretation Schema code generator.
// Do not modify this file directly.
// </auto-generated>
#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Dynamic;
using Musoq.Schema.Interpreters;

namespace Musoq.Generated.Interpreters
{
    /// <summary>
    /// Generated interpreter for binary schema 'SampleReading'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class SampleReading : BytesInterpreterBase<SampleReading>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(SampleReading value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Score"))
            {
                writer.WritePropertyName("Score");
                var fieldValue = this.Score;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Score field value.</summary>
        public int Score { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "SampleReading";

        /// <inheritdoc/>
        public override SampleReading InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new SampleReading();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Score");
            var _score = ReadInt32Le(data);
            __parsedValue.Score = _score;
            RecordParsedField("Score");
            return __parsedValue;
        }
    }
}
