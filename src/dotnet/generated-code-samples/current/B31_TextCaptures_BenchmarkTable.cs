// === Parsed Query ===
/*
text Captured {
    Fields: pattern r'(?<Level>[A-Z]+)\|(?<Number>[0-9]+)\|(?<Label>.*)' capture (Level, Number, Label)
};
select r.Id as Id, p.Fields.Label as Label
from bench.rows() r
cross apply Parse<Captured>(r.Text) p
order by r.Id
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.Id as r.Id, r.Text as r.Text, p.Fields as p.Fields]
    Apply [Cross]
      SchemaScan [#bench.rows() as r]
      InterpretSource [#Captured(r.Text) as p]
  Sort [r.Id]
    Project [r.Id as Id, p.Fields.Label as Label]
      CteRef [rp as rp]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.Id as r.Id, p.Fields as p.Fields]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#bench.rows() as r]
      PhysicalInterpretSource [#Captured(r.Text) as p]
  PhysicalSort [r.Id]
    PhysicalProject [r.Id as Id, p.Fields.Label as Label]
      PhysicalCteRef [rp as rp]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: BenchmarkSampleRow]
      Id: int <- property Id
      Text: string <- property Text
    SourceEntity [p: object]
      Fields: object <- property Fields
    Generated [Statement0Row0]
      r.Id: int <- field r_Id
      p.Fields: object <- field p_Fields
    TableRow [rp]
      r.Id: int <- field r_Id
      p.Fields: object <- field p_Fields
    Generated [ResultRow0]
      Id: int <- field Id
      Label: string <- field Label

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: BenchmarkSampleRow] -> statement0_rRows
    CreateTable [statement0: Statement0Row0]
    PhaseBoundary [Select]
    ChunkedForEach [r in statement0_rRows]
      Let [rId: int = r.Id]
      InterpretSource [Captured.Parse(r.Text) -> statement0_pRows]
      ScalarForEach [p in statement0_pRows]
        AppendRow [statement0 <- Statement0Row0(r.Id: rId, p.Fields: p.Fields)]
    StoreTable [statement0 -> _cteRowResults.Slot0: List<Statement0Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [rp in _cteRowResults.Slot0]
      AppendShape [result <- ResultShape0(Id: rp.r.Id, Label: rp.p.Fields?.Label)]
    SortOwnedBuffer [result by Id ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B31_TextCaptures_BenchmarkTable
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Label", typeof(string), 1)
        };
        private static readonly Column[] __columns_compiled_statement0_1 = new Column[]
        {
            new Column("r.Id", typeof(int), 0),
            new Column("p.Fields", typeof(Musoq.Generated.Interpreters.Captured.CaptureResult_Fields), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Text", typeof(string), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Id, __musoqShapeRow.Label);
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
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
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

                    Statement0Row0 rp = __storedTable0Rows[__storedTable0Index];
                    result.Add(new ResultShape0(rp.r_Id, rp.p_Fields?.Label) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
                    if (comparison != 0)
                        return comparison;
                    return left.__musoqSequence.CompareTo(right.__musoqSequence);
                }));
                return result;
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
            var __statement0_rSchema = provider.GetSchema("#bench");
            var statement0_rRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BenchmarkSampleRow>.Open(__statement0_rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var statement0_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BenchmarkSampleRow>(statement0_rRowsSource.Chunks, __musoqProgressContext, "r:1") : statement0_rRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            foreach (var rChunk in statement0_rRows)
            {
                if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.BenchmarkSampleRow> rChunkView)
                {
                    if (rChunkView.Source is Musoq.Evaluator.Tests.BenchmarkSampleRow[] rChunkViewArray)
                    {
                        int rChunkViewOffset = rChunkView.Offset;
                        for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                        {
                            if ((rIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var r = rChunkViewArray[rChunkViewOffset + rIndex];
                            int rId = r.Id;
                            var _interpreter_Captured = new Musoq.Generated.Interpreters.Captured();
                            var statement0_pRows = _interpreter_Captured.Parse(r.Text);
                            if (statement0_pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = statement0_pRows;
                                statement0.Add(new Statement0Row0(rId, p.Fields));
                            }
                        }

                        continue;
                    }

                    if (rChunkView.Source is List<Musoq.Evaluator.Tests.BenchmarkSampleRow> rChunkViewList)
                    {
                        int rChunkViewOffset = rChunkView.Offset;
                        for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                        {
                            if ((rIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var r = rChunkViewList[rChunkViewOffset + rIndex];
                            int rId = r.Id;
                            var _interpreter_Captured = new Musoq.Generated.Interpreters.Captured();
                            var statement0_pRows = _interpreter_Captured.Parse(r.Text);
                            if (statement0_pRows != null)
                            {
                                token.ThrowIfCancellationRequested();
                                var p = statement0_pRows;
                                statement0.Add(new Statement0Row0(rId, p.Fields));
                            }
                        }

                        continue;
                    }
                }

                for (int rIndex = 0, rIndexCount = rChunk.Count; rIndex < rIndexCount; ++rIndex)
                {
                    if ((rIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var r = rChunk[rIndex];
                    int rId = r.Id;
                    var _interpreter_Captured = new Musoq.Generated.Interpreters.Captured();
                    var statement0_pRows = _interpreter_Captured.Parse(r.Text);
                    if (statement0_pRows != null)
                    {
                        token.ThrowIfCancellationRequested();
                        var p = statement0_pRows;
                        statement0.Add(new Statement0Row0(rId, p.Fields));
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
            public ResultRow0(int __value0, string __value1)
            {
                Id = __value0;
                Label = __value1;
            }

            public override int Count => 2;
            public int Id { get; private set; }
            public string Label { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Label = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Label),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Label" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Label, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id, string Label)
            {
                this.Id = Id;
                this.Label = Label;
            }

            public int Id { get; }
            public string Label { get; }
            public int __musoqSequence { get; init; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(int __value0, Musoq.Generated.Interpreters.Captured.CaptureResult_Fields __value1)
            {
                r_Id = __value0;
                p_Fields = __value1;
            }

            public Musoq.Generated.Interpreters.Captured.CaptureResult_Fields p_Fields { get; }
            public int r_Id { get; }
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
    /// Generated interpreter for text schema 'Captured'.
    /// </summary>
    [global::System.Text.Json.Serialization.JsonConverter(typeof(global::Musoq.Plugins.JsonPayloadConverterFactory))]
    public sealed class Captured : TextInterpreterBase<Captured>, global::Musoq.Plugins.IJsonPayload
    {
        private System.Collections.Generic.IReadOnlySet<string>? __completedFieldNames;
        public bool IsFieldComplete(string name) => __completedFieldNames?.Contains(name) ?? true;
        protected override void AttachPartialFieldNames(Captured value, System.Collections.Generic.IReadOnlySet<string> completedFields)
        {
            value.__completedFieldNames = completedFields;
        }

        public void WriteJson(global::System.Text.Json.Utf8JsonWriter writer, global::System.Text.Json.JsonSerializerOptions? options = null)
        {
            writer.WriteStartObject();
            if (IsFieldComplete("Fields"))
            {
                writer.WritePropertyName("Fields");
                var fieldValue = this.Fields;
                global::Musoq.Plugins.JsonPayloadWriter.Write(writer, in fieldValue, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>Gets the Fields capture result.</summary>
        public CaptureResult_Fields? Fields { get; set; }
        /// <inheritdoc/>
        public override string SchemaName => "Captured";

        /// <inheritdoc/>
        public override Captured ParseAt(ReadOnlySpan<char> data, int offset)
        {
            ParsePosition = offset;
            SetCurrentField(null);
            var __parsedValue = new Captured();
            RecordParsedValue(__parsedValue);
            SetCurrentField("Fields");
            var _match__fields = ReadPatternMatch(data, @"(?<Level>[A-Z]+)\|(?<Number>[0-9]+)\|(?<Label>.*)", fieldName: "Fields");
            var _fields = _match__fields.Success ? new CaptureResult_Fields
            {
                Level = ApplyModifiers(_match__fields.Groups["Level"].Value),
                Number = ApplyModifiers(_match__fields.Groups["Number"].Value),
                Label = ApplyModifiers(_match__fields.Groups["Label"].Value)
            }

            : null;
            __parsedValue.Fields = _fields;
            RecordParsedField("Fields");
            return __parsedValue;
        }

        public sealed class CaptureResult_Fields
        {
            public string? Level { get; set; }
            public string? Number { get; set; }
            public string? Label { get; set; }
        }
    }
}
