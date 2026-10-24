// === Parsed Query ===
/*
binary Inner { X: byte, Y: byte } binary Outer { Items: Inner[2] } with c as (select s.Items[1] as Item from #test.files() f cross apply Interpret<Outer>(f.Content) s) select c.Item.X as X, c.Item.Y as Y from c c
*/

// === Logical Plan ===
/*
Cte
  Definition [c]
    MultiStatement
      Project [f.Content as f.Content, s.Items as s.Items]
        Apply [Cross]
          SchemaScan [#test.files() as f]
          InterpretSource [#Outer(f.Content) as s]
      Project [s.Items[1] as Item]
        CteRef [fs as fs]
  Query
    MultiStatement
      Project [c.Item.X as X, c.Item.Y as Y]
        CteRef [c as c]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [c]
    PhysicalMultiStatement
      PhysicalProject [f.Content as f.Content, s.Items as s.Items]
        PhysicalNestedLoopApply [Cross]
          PhysicalSchemaScan [#test.files() as f]
          PhysicalInterpretSource [#Outer(f.Content) as s]
      PhysicalProject [s.Items[1] as Item]
        PhysicalCteRef [fs as fs]
  Query
    PhysicalMultiStatement
      PhysicalProject [c.Item.X as X, c.Item.Y as Y]
        PhysicalCteRef [c as c]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [f: BinaryEntity]
      Content: byte[] <- property Content
    SourceEntity [s: object]
      Items: object[] <- property Items
    Generated [Cte0Row0]
      Item: object <- field Item_
    TableRow [c]
      Item: object <- field Item_
    Generated [ResultRow0]
      X: byte <- field X
      Y: byte <- field Y

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    PhaseBoundary [Begin:cte1]
    SourceScan [f: BinaryEntity] -> cte0_fRows
    CreateTable [cte0: Cte0Row0]
    ChunkedForEach [f in cte0_fRows]
      InterpretSource [Outer.Interpret(f.Content) -> cte0_sRows]
      ScalarForEach [s in cte0_sRows]
        AppendRow [cte0 <- Cte0Row0(Item: GetArrayElement(s.Items, 1))]
    PhaseBoundary [End:cte1]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [From]
    ForEach [c in _cteRowResults.Slot0]
      AppendShape [result <- ResultRow0(X: c.Item.X, Y: c.Item.Y)]
    PhaseBoundary [Select]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_I05_InterpretedIndexedCteShape
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
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("Item", typeof(Musoq.Generated.Interpreters.Inner), 0)
        };
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("X", typeof(byte), 0),
            new Column("Y", typeof(byte), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_f_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Content", typeof(byte[]), 0) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 c = __storedTable0Rows[__storedTable0Index];
                    __musoqFinalShapeRows.Add(new ResultRow0(c.Item_.X, c.Item_.Y));
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
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
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                var __cte0_fSchema = provider.GetSchema("#test");
                var cte0_fRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>.Open(__cte0_fSchema, "files", new SourceExecutionContext("f:1", sourceExecutionPlans["f:1"], token, __schemaColumns_compiled_f_0, sourceRuntimeSettingsBySourceContextId["f:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var cte0_fRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity>(cte0_fRowsSource.Chunks, __musoqProgressContext, "f:1") : cte0_fRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                foreach (var fChunk in cte0_fRows)
                {
                    if (fChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity> fChunkView)
                    {
                        if (fChunkView.Source is Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity[] fChunkViewArray)
                        {
                            int fChunkViewOffset = fChunkView.Offset;
                            for (int fIndex = 0, fIndexCount = fChunkView.Count; fIndex < fIndexCount; ++fIndex)
                            {
                                if ((fIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var f = fChunkViewArray[fChunkViewOffset + fIndex];
                                var _interpreter_Outer = new Musoq.Generated.Interpreters.Outer();
                                var cte0_sRows = _interpreter_Outer.Interpret(f.Content);
                                if (cte0_sRows != null)
                                {
                                    token.ThrowIfCancellationRequested();
                                    var s = cte0_sRows;
                                    cte0.Add(new Cte0Row0(Musoq.Evaluator.Helpers.SafeArrayAccess.GetArrayElement<Musoq.Generated.Interpreters.Inner>(s.Items, 1)));
                                }
                            }

                            continue;
                        }

                        if (fChunkView.Source is List<Musoq.Evaluator.Tests.BinaryOrTextualEvaluatorTestBase.BinaryEntity> fChunkViewList)
                        {
                            int fChunkViewOffset = fChunkView.Offset;
                            for (int fIndex = 0, fIndexCount = fChunkView.Count; fIndex < fIndexCount; ++fIndex)
                            {
                                if ((fIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var f = fChunkViewList[fChunkViewOffset + fIndex];
                                var _interpreter_Outer = new Musoq.Generated.Interpreters.Outer();
                                var cte0_sRows = _interpreter_Outer.Interpret(f.Content);
                                if (cte0_sRows != null)
                                {
                                    token.ThrowIfCancellationRequested();
                                    var s = cte0_sRows;
                                    cte0.Add(new Cte0Row0(Musoq.Evaluator.Helpers.SafeArrayAccess.GetArrayElement<Musoq.Generated.Interpreters.Inner>(s.Items, 1)));
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
                        var _interpreter_Outer = new Musoq.Generated.Interpreters.Outer();
                        var cte0_sRows = _interpreter_Outer.Interpret(f.Content);
                        if (cte0_sRows != null)
                        {
                            token.ThrowIfCancellationRequested();
                            var s = cte0_sRows;
                            cte0.Add(new Cte0Row0(Musoq.Evaluator.Helpers.SafeArrayAccess.GetArrayElement<Musoq.Generated.Interpreters.Inner>(s.Items, 1)));
                        }
                    }
                }

                return cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(Musoq.Generated.Interpreters.Inner __value0)
            {
                Item_ = __value0;
            }

            public Musoq.Generated.Interpreters.Inner Item_ { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(byte __value0, byte __value1)
            {
                X = __value0;
                Y = __value1;
            }

            public override int Count => 2;
            public byte X { get; private set; }
            public byte Y { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.X = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Y = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<byte, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.X),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, byte>(this.Y),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "X" => true,
                "Y" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.X, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Y, ref visitor);
            }
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
    /// Generated interpreter for binary schema 'Inner'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Inner : BytesInterpreterBase<Inner>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Inner value, System.Collections.Generic.IReadOnlySet<string> completedFields)
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

            if (IsFieldComplete("Y"))
            {
                writer.WritePropertyName("Y");
                var fieldValue = this.Y;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the X field value.</summary>
        public byte X { get; set; }
        /// <summary>Gets the Y field value.</summary>
        public byte Y { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Inner";

        /// <inheritdoc/>
        public override Inner InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new Inner();
            RecordParsedValue(__parsedValue);
            SetCurrentField("X");
            var _x = ReadByte(data);
            __parsedValue.X = _x;
            RecordParsedField("X");
            SetCurrentField("Y");
            var _y = ReadByte(data);
            __parsedValue.Y = _y;
            RecordParsedField("Y");
            return __parsedValue;
        }
    }

    /// <summary>
    /// Generated interpreter for binary schema 'Outer'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Outer : BytesInterpreterBase<Outer>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Outer value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Items"))
            {
                writer.WritePropertyName("Items");
                var fieldValue = this.Items;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Items field value.</summary>
        public Inner[] Items { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Outer";

        /// <inheritdoc/>
        public override Outer InterpretAt(ReadOnlySpan<byte> data, int offset)
        {
            InitializeParsePosition(data, offset);
            SetCurrentField(null);
            var __parsedValue = new Outer();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Items");
            var __items_list = new System.Collections.Generic.List<Inner>();
            var __items_count = 2;
            for (int __items_i = 0; __items_i < __items_count; __items_i++)
            {
                var ___items_elem_interpreter = new Inner();
                var __items_elem = InterpretNested(___items_elem_interpreter, data, "Items");
                __items_list.Add(__items_elem);
            }

            var _items = __items_list.ToArray();
            __parsedValue.Items = _items;
            RecordParsedField("Items");
            return __parsedValue;
        }
    }
}
