// === Parsed Query ===
/*
text Item { X: chars[1] } text Packet { Records: repeat Item until end } select Count(r.X) as N from #test.lines() f cross apply Parse<Packet>(f.Line) p cross apply p.Records r
*/

// === Logical Plan ===
/*
MultiStatement
  Project [f.Line as f.Line, p.Records as p.Records]
    Apply [Cross]
      SchemaScan [#test.lines() as f]
      InterpretSource [#Packet(f.Line) as p]
  Project [fp.f.Line as f.Line, fp.p.Records as p.Records, r.X as r.X]
    Apply [Cross]
      CteRef [fp as fp]
      PropertySource [fp.p.Records as r] [apply: Cross] [type: Object[]]
  Project [1 as 1, AggRef(fpr.Count(r.X)) as fpr.Count(r.X)]
    Aggregate [keys: 1] [aggs: Count(X)]
      CteRef [fpr as fpr]
  Project [fpr.Count(r.X) as N]
    CteRef [fprScore as fprScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [p.Records as p.Records]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#test.lines() as f]
      PhysicalInterpretSource [#Packet(f.Line) as p]
  PhysicalProject [r.X as r.X]
    PhysicalNestedLoopApply [Cross]
      PhysicalCteRef [fp as fp]
      PhysicalPropertySource [fp.p.Records as r] [apply: Cross] [type: Object[]]
  PhysicalProject [AggRef(fpr.Count(r.X)) as fpr.Count(r.X)]
    PhysicalSingleKeyAggregate [key: 1 (Int16)] [aggs: Count(X)]
      PhysicalCteRef [fpr as fpr]
  PhysicalProject [fpr.Count(r.X) as N]
    PhysicalCteRef [fprScore as fprScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: TextEntity]
      Line: string <- property Line
    SourceEntity [p: object]
      Records: object[] <- property Records
    Generated [Statement0Row0]
      p.Records: object[] <- field p_Records
    TableRow [fp]
      p.Records: object[] <- field p_Records
    SourceEntity [r: object]
      X: string <- property X
    Generated [Statement1Row0]
      r.X: string <- field r_X
    TableRow [fpr]
      r.X: string <- field r_X
    AggregateGroup [Statement2AggregateGroup; keys: 0; typed aggs: 1]
    Generated [Statement2Row0]
      fpr.Count(r.X): long <- field fpr_Count_r_X_
    TableRow [fprScore]
      fpr.Count(r.X): long <- field fpr_Count_r_X_
    Generated [ResultRow0]
      N: long <- field N

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [f: TextEntity] -> statement0_fRows
    CreateTable [statement0: Statement0Row0]
    ChunkedForEach [f in statement0_fRows]
      InterpretSource [Packet.Parse(f.Line) -> statement0_pRows]
      ScalarForEach [p in statement0_pRows]
        AppendRow [statement0 <- Statement0Row0(p.Records: p.Records)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateTable [statement1: Statement1Row0]
    ForEach [fp in _cteRowResults.Slot0]
      EnumerableSource [fp.p.Records -> statement1_rRows]
      ChunkedForEach [r in statement1_rRows]
        AppendRow [statement1 <- Statement1Row0(r.X: r.X)]
    StoreTable [statement1 -> _cteRowResults.Slot1: List<Statement1Row0>]
    CreateTable [statement2: Statement2Row0]
    CreateAggregateContext [statement2RootGroup, statement2Group, statement2GroupsToFinalize; typed: Statement2AggregateGroup]
    ForEach [fpr in _cteRowResults.Slot1]
      EnsureAggregateGroup [statement2Group; typed: Statement2AggregateGroup]
      Let [r_X: string = fpr.r.X]
      TypedAggregateSet [Set(statement2Group.__agg0, r_X)]
    EnsureCapacity [statement2 <- statement2GroupsToFinalize.Count]
    ForEach [statement2FinalGroup in statement2GroupsToFinalize]
      AppendRow [statement2 <- Statement2Row0(fpr.Count(r.X): fpr.Count(r.X))]
    StoreTable [statement2 -> _cteRowResults.Slot2: List<Statement2Row0>]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ForEach [fprScore in _cteRowResults.Slot2]
      AppendShape [result <- ResultRow0(N: fprScore.fpr.Count(r.X))]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_I02_ConfiguredTextRepeatLimit
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
        private static readonly Column[] __columns_compiled_result_4 = new Column[]
        {
            new Column("N", typeof(long), 0)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("p.Records", typeof(Musoq.Generated.Interpreters.Item[]), 0)
        };
        private static readonly Column[] __columns_compiled_statement1_2 = new Column[]
        {
            new Column("r.X", typeof(string), 0)
        };
        private static readonly Column[] __columns_compiled_statement2_3 = new Column[]
        {
            new Column("fpr.Count(r.X)", typeof(long), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_f_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Line", typeof(string), 0) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_4, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                _cteRowResults.Slot2 = BuildCte2(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled", QueryPhase.Select);
                var __storedTable2Rows = _cteRowResults.Slot2;
                for (int __storedTable2Index = 0; __storedTable2Index < __storedTable2Rows.Count; ++__storedTable2Index)
                {
                    if ((__storedTable2Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement2Row0 fprScore = __storedTable2Rows[__storedTable2Index];
                    __musoqFinalShapeRows.Add(new ResultRow0(fprScore.fpr_Count_r_X_));
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
            var __statement0_fSchema = provider.GetSchema("#test");
            var statement0_fRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity>.Open(__statement0_fSchema, "lines", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_fRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity>(statement0_fRowsSource.Chunks, __musoqProgressContext, "f:1") : statement0_fRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            foreach (var fChunk in statement0_fRows)
            {
                if (fChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity> fChunkView)
                {
                    if (fChunkView.Source is Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity[] fChunkViewArray)
                    {
                        int fChunkViewOffset = fChunkView.Offset;
                        for (int fIndex = 0, fIndexCount = fChunkView.Count; fIndex < fIndexCount; ++fIndex)
                        {
                            if ((fIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var f = fChunkViewArray[fChunkViewOffset + fIndex];
                            var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                            var statement0_pRows = _interpreter_Packet.Parse(f.Line);
                            if (statement0_pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = statement0_pRows;
                                statement0.Add(new Statement0Row0(p.Records));
                            }
                        }

                        continue;
                    }

                    if (fChunkView.Source is List<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.TextEntity> fChunkViewList)
                    {
                        int fChunkViewOffset = fChunkView.Offset;
                        for (int fIndex = 0, fIndexCount = fChunkView.Count; fIndex < fIndexCount; ++fIndex)
                        {
                            if ((fIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var f = fChunkViewList[fChunkViewOffset + fIndex];
                            var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                            var statement0_pRows = _interpreter_Packet.Parse(f.Line);
                            if (statement0_pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = statement0_pRows;
                                statement0.Add(new Statement0Row0(p.Records));
                            }
                        }

                        continue;
                    }
                }

                for (int fIndex = 0, fIndexCount = fChunk.Count; fIndex < fIndexCount; ++fIndex)
                {
                    if ((fIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var f = fChunk[fIndex];
                    var _interpreter_Packet = new Musoq.Generated.Interpreters.Packet();
                    var statement0_pRows = _interpreter_Packet.Parse(f.Line);
                    if (statement0_pRows != null)
                    {
                        token.ThrowIfCancellationRequested();
                        var p = statement0_pRows;
                        statement0.Add(new Statement0Row0(p.Records));
                    }
                }
            }

            return statement0;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement1Row0> BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            var statement1 = new List<Statement1Row0>();
            var __storedTable0Rows = _cteRowResults.Slot0;
            for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
            {
                if ((__storedTable0Index & 1023) == 0)
                {
                    token.ThrowIfCancellationRequested();
                }

                Statement0Row0 fp = __storedTable0Rows[__storedTable0Index];
                var statement1_rRows = EvaluationHelper.ConvertEnumerableOutputToChunks<Musoq.Generated.Interpreters.Item>(fp.p_Records);
                foreach (var rChunk in statement1_rRows)
                {
                    for (int rIndex = 0, rIndexCount = rChunk.Count; rIndex < rIndexCount; ++rIndex)
                    {
                        if ((rIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var r = rChunk[rIndex];
                        statement1.Add(new Statement1Row0(r.X));
                    }
                }
            }

            return statement1;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement2Row0> BuildCte2(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            var statement2 = new List<Statement2Row0>();
            var statement2GroupsToFinalize = new List<Statement2AggregateGroup>();
            Statement2AggregateGroup statement2Group = new Statement2AggregateGroup();
            statement2GroupsToFinalize.Add(statement2Group);
            var __storedTable1Rows = _cteRowResults.Slot1;
            for (int __storedTable1Index = 0; __storedTable1Index < __storedTable1Rows.Count; ++__storedTable1Index)
            {
                if ((__storedTable1Index & 1023) == 0)
                {
                    token.ThrowIfCancellationRequested();
                }

                Statement1Row0 fpr = __storedTable1Rows[__storedTable1Index];
                if (statement2Group == null)
                {
                    statement2Group = new Statement2AggregateGroup();
                    statement2GroupsToFinalize.Add(statement2Group);
                }

                string r_X = fpr.r_X;
                if ((string)r_X != null)
                {
                    statement2Group.__agg0.Count = checked(statement2Group.__agg0.Count + 1L);
                }
            }

            statement2.EnsureCapacity(statement2GroupsToFinalize.Count);
            foreach (var statement2FinalGroup in statement2GroupsToFinalize)
            {
                token.ThrowIfCancellationRequested();
                statement2.Add(new Statement2Row0(statement2FinalGroup.__agg0.Count));
            }

            return statement2;
        }

        private sealed class CteRowResults
        {
            public List<Statement0Row0> Slot0;
            public List<Statement1Row0> Slot1;
            public List<Statement2Row0> Slot2;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(long __value0)
            {
                N = __value0;
            }

            public override int Count => 1;
            public long N { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.N = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.N),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "N" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.N, ref visitor);
            }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(Musoq.Generated.Interpreters.Item[] __value0)
            {
                p_Records = __value0;
            }

            public Musoq.Generated.Interpreters.Item[] p_Records { get; }
        }

        private sealed class Statement1Row0
        {
            public Statement1Row0(string __value0)
            {
                r_X = __value0;
            }

            public string r_X { get; }
        }

        private sealed class Statement2AggregateGroup
        {
            public Musoq.Plugins.CountReferenceAggregateKernel<string>.State __agg0;
            public Statement2AggregateGroup()
            {
                Musoq.Plugins.CountReferenceAggregateKernel<string>.Reset(ref this.__agg0);
            }

            public void MergeFrom(Statement2AggregateGroup source)
            {
                Musoq.Plugins.CountReferenceAggregateKernel<string>.Merge(ref this.__agg0, in source.__agg0);
            }
        }

        private sealed class Statement2Row0
        {
            public Statement2Row0(long __value0)
            {
                fpr_Count_r_X_ = __value0;
            }

            public long fpr_Count_r_X_ { get; }
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
    /// Generated interpreter for text schema 'Item'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Item : TextInterpreterBase<Item>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Item value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("X"))
            {
                writer.WritePropertyName("X");
                var fieldValue = this.X;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the X field value.</summary>
        public string? X { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Item";

        /// <inheritdoc/>
        public override Item ParseAt(ReadOnlySpan<char> data, int offset)
        {
            ParsePosition = offset;
            SetCurrentField(null);
            var __parsedValue = new Item();
            RecordParsedValue(__parsedValue);
            SetCurrentField("X");
            var _x = ReadChars(data, 1, fieldName: "X");
            __parsedValue.X = _x;
            RecordParsedField("X");
            return __parsedValue;
        }
    }

    /// <summary>
    /// Generated interpreter for text schema 'Packet'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Packet : TextInterpreterBase<Packet>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Packet value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Records"))
            {
                writer.WritePropertyName("Records");
                var fieldValue = this.Records;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Records field value.</summary>
        public Item[]? Records { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Packet";

        /// <inheritdoc/>
        public override Packet ParseAt(ReadOnlySpan<char> data, int offset)
        {
            ParsePosition = offset;
            SetCurrentField(null);
            var __parsedValue = new Packet();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Records");
            var _list__records = new System.Collections.Generic.List<Item>();
            var _interp_Item = new Item();
            var _iteration__records = 0;
            while (!IsAtEnd(data))
            {
                EnsureRepeatIteration("Records", _iteration__records++, 10000);
                var _startPos__records = ParsePosition;
                var _item__records = ParseNested(_interp_Item, data, "Records");
                EnsureRepeatMadeProgress("Records", _startPos__records);
                _list__records.Add(_item__records);
            }

            var _records = _list__records.ToArray();
            __parsedValue.Records = _records;
            RecordParsedField("Records");
            return __parsedValue;
        }
    }
}
