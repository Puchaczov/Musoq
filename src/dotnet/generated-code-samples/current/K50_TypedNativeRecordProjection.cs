// === Parsed Query ===
/*
select (Position: Position, Detail: (Value: Value), Items: array { Position, Position + 1 }) as Payload from #schema.first() order by Position
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [ko3iko.Position]
    Project [(Position: ko3iko.Position, Detail: (Value: ko3iko.Value), Items: array { ko3iko.Position, (ko3iko.Position + 1) }) as Payload]
      SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [ko3iko.Position]
    PhysicalProject [(Position: ko3iko.Position, Detail: (Value: ko3iko.Value), Items: array { ko3iko.Position, (ko3iko.Position + 1) }) as Payload]
      PhysicalSchemaScan [#schema.first() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: NativeRecordInput]
      Position: int <- property Position
      Value: int? <- property Value
    GeneratedRecord [ResultRow0WithSortKeys]
      Payload: StructuralValue <- field Payload
      __sortKey0: int <- field __sortKey0
      __ordinal: int <- field __ordinal
    Generated [ResultRow0]
      Payload: StructuralValue <- field Payload

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: NativeRecordInput] -> ko3ikoRows
    CreateRecordList [resultOrderRecords: ResultRow0WithSortKeys]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [position: int = ko3iko.Position]
      AppendRecord [resultOrderRecords <- ResultRow0WithSortKeys(Payload: (Position: ko3iko.Position, Detail: (Value: ko3iko.Value), Items: array { ko3iko.Position, (ko3iko.Position + 1) }), __sortKey0: position)]
    OrderRecordList [resultOrderRecords: ResultRow0WithSortKeys by __sortKey0 ASC]
    MaterializeRecordListToShapeRows [resultOrderRecords -> result: ResultShape0 fields 0]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K50_TypedNativeRecordProjection
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
            new Column("Payload", typeof(__musoqStructuralCarrier_0_root), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Value", typeof(int?), 1) });
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
                yield return new ResultRow0(__musoqShapeRow.Payload);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#schema");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionBoundaryQueryTests.NativeRecordInput>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionBoundaryQueryTests.NativeRecordInput>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var resultOrderRecords = new List<ResultRow0WithSortKeys>();
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionBoundaryQueryTests.NativeRecordInput> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionBoundaryQueryTests.NativeRecordInput[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                int position = ko3iko.Position;
                                resultOrderRecords.Add(new ResultRow0WithSortKeys(new __musoqStructuralCarrier_0_root(ko3iko.Position, new __musoqStructuralCarrier_0_1(ko3iko.Value, 1UL), new int[] { ko3iko.Position, (ko3iko.Position + 1) }, 7UL), position, resultOrderRecords.Count));
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionBoundaryQueryTests.NativeRecordInput> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                int position = ko3iko.Position;
                                resultOrderRecords.Add(new ResultRow0WithSortKeys(new __musoqStructuralCarrier_0_root(ko3iko.Position, new __musoqStructuralCarrier_0_1(ko3iko.Value, 1UL), new int[] { ko3iko.Position, (ko3iko.Position + 1) }, 7UL), position, resultOrderRecords.Count));
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
                        int position = ko3iko.Position;
                        resultOrderRecords.Add(new ResultRow0WithSortKeys(new __musoqStructuralCarrier_0_root(ko3iko.Position, new __musoqStructuralCarrier_0_1(ko3iko.Value, 1UL), new int[] { ko3iko.Position, (ko3iko.Position + 1) }, 7UL), position, resultOrderRecords.Count));
                    }
                }

                resultOrderRecords.Sort(ResultRow0WithSortKeysComparer.Instance);
                foreach (var resultRecord in resultOrderRecords)
                {
                    __musoqFinalShapeRows.Add(new ResultShape0(resultRecord.Payload));
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
            public ResultRow0(__musoqStructuralCarrier_0_root __value0)
            {
                Payload = __value0;
            }

            public override int Count => 1;
            public __musoqStructuralCarrier_0_root Payload { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, __musoqStructuralCarrier_0_root>(this.Payload = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<__musoqStructuralCarrier_0_root, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, __musoqStructuralCarrier_0_root>(this.Payload),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Payload" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Payload, ref visitor);
            }
        }

        private sealed class ResultRow0WithSortKeys
        {
            public ResultRow0WithSortKeys(__musoqStructuralCarrier_0_root Payload, int __sortKey0, int __ordinal)
            {
                this.Payload = Payload;
                this.__sortKey0 = __sortKey0;
                this.__ordinal = __ordinal;
            }

            public __musoqStructuralCarrier_0_root Payload { get; }
            public int __ordinal { get; }
            public int __sortKey0 { get; }
        }

        private sealed class ResultRow0WithSortKeysComparer : IComparer<ResultRow0WithSortKeys>
        {
            public static readonly ResultRow0WithSortKeysComparer Instance = new ResultRow0WithSortKeysComparer();
            public int Compare(ResultRow0WithSortKeys left, ResultRow0WithSortKeys right)
            {
                var comparison = left.__sortKey0.CompareTo(right.__sortKey0);
                if (comparison != 0)
                    return comparison;
                return left.__ordinal.CompareTo(right.__ordinal);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(__musoqStructuralCarrier_0_root Payload)
            {
                this.Payload = Payload;
            }

            public __musoqStructuralCarrier_0_root Payload { get; }
        }

        [global::System.Text.Json.Serialization.JsonConverter(typeof(__musoqStructuralCarrier_0_1JsonConverter))]
        private readonly struct __musoqStructuralCarrier_0_1
        {
            public readonly int? F0;
            public readonly ulong P0;
            public __musoqStructuralCarrier_0_1(int? f0, ulong p0)
            {
                F0 = f0;
                P0 = p0;
            }
        }

        private sealed class __musoqStructuralCarrier_0_1JsonConverter : global::System.Text.Json.Serialization.JsonConverter<__musoqStructuralCarrier_0_1>
        {
            public __musoqStructuralCarrier_0_1JsonConverter()
            {
            }

            public override __musoqStructuralCarrier_0_1 Read(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
            {
                if (reader.TokenType != global::System.Text.Json.JsonTokenType.StartObject)
                    throw new global::System.Text.Json.JsonException("A native record requires a JSON object.");
                int? field0 = default;
                ulong p0 = 0;
                while (reader.Read())
                {
                    if (reader.TokenType == global::System.Text.Json.JsonTokenType.EndObject)
                    {
                        if ((p0 & 1UL) == 0)
                            throw new global::System.Text.Json.JsonException("A required native field is missing.");
                        return new __musoqStructuralCarrier_0_1(field0, p0);
                    }

                    if (reader.TokenType != global::System.Text.Json.JsonTokenType.PropertyName)
                        throw new global::System.Text.Json.JsonException("A native record requires named fields.");
                    if (reader.ValueTextEquals("Value"))
                    {
                        if ((p0 & 1UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field0 = global::System.Text.Json.JsonSerializer.Deserialize<int?>(ref reader, options)!;
                        p0 |= 1UL;
                        continue;
                    }

                    throw new global::System.Text.Json.JsonException("The native record contains an undeclared field.");
                }

                throw new global::System.Text.Json.JsonException("The native record is incomplete.");
            }

            public override void Write(global::System.Text.Json.Utf8JsonWriter writer, __musoqStructuralCarrier_0_1 value, global::System.Text.Json.JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                if ((value.P0 & 1UL) != 0)
                {
                    writer.WritePropertyName("Value");
                    global::System.Text.Json.JsonSerializer.Serialize<int?>(writer, value.F0, options);
                }

                writer.WriteEndObject();
            }
        }

        [global::System.Text.Json.Serialization.JsonConverter(typeof(__musoqStructuralCarrier_0_rootJsonConverter))]
        private readonly struct __musoqStructuralCarrier_0_root
        {
            public readonly int F0;
            public readonly __musoqStructuralCarrier_0_1 F1;
            public readonly int[] F2;
            public readonly ulong P0;
            public __musoqStructuralCarrier_0_root(int f0, __musoqStructuralCarrier_0_1 f1, int[] f2, ulong p0)
            {
                F0 = f0;
                F1 = f1;
                F2 = f2;
                P0 = p0;
            }
        }

        private sealed class __musoqStructuralCarrier_0_rootJsonConverter : global::System.Text.Json.Serialization.JsonConverter<__musoqStructuralCarrier_0_root>
        {
            public __musoqStructuralCarrier_0_rootJsonConverter()
            {
            }

            public override __musoqStructuralCarrier_0_root Read(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
            {
                if (reader.TokenType != global::System.Text.Json.JsonTokenType.StartObject)
                    throw new global::System.Text.Json.JsonException("A native record requires a JSON object.");
                int field0 = default;
                __musoqStructuralCarrier_0_1 field1 = default;
                int[] field2 = default;
                ulong p0 = 0;
                while (reader.Read())
                {
                    if (reader.TokenType == global::System.Text.Json.JsonTokenType.EndObject)
                    {
                        if ((p0 & 1UL) == 0)
                            throw new global::System.Text.Json.JsonException("A required native field is missing.");
                        if ((p0 & 2UL) == 0)
                            throw new global::System.Text.Json.JsonException("A required native field is missing.");
                        if ((p0 & 4UL) == 0)
                            throw new global::System.Text.Json.JsonException("A required native field is missing.");
                        return new __musoqStructuralCarrier_0_root(field0, field1, field2, p0);
                    }

                    if (reader.TokenType != global::System.Text.Json.JsonTokenType.PropertyName)
                        throw new global::System.Text.Json.JsonException("A native record requires named fields.");
                    if (reader.ValueTextEquals("Position"))
                    {
                        if ((p0 & 1UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field0 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 1UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("Detail"))
                    {
                        if ((p0 & 2UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field1 = global::System.Text.Json.JsonSerializer.Deserialize<__musoqStructuralCarrier_0_1>(ref reader, options)!;
                        p0 |= 2UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("Items"))
                    {
                        if ((p0 & 4UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field2 = global::System.Text.Json.JsonSerializer.Deserialize<int[]>(ref reader, options)!;
                        p0 |= 4UL;
                        continue;
                    }

                    throw new global::System.Text.Json.JsonException("The native record contains an undeclared field.");
                }

                throw new global::System.Text.Json.JsonException("The native record is incomplete.");
            }

            public override void Write(global::System.Text.Json.Utf8JsonWriter writer, __musoqStructuralCarrier_0_root value, global::System.Text.Json.JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                if ((value.P0 & 1UL) != 0)
                {
                    writer.WritePropertyName("Position");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F0, options);
                }

                if ((value.P0 & 2UL) != 0)
                {
                    writer.WritePropertyName("Detail");
                    global::System.Text.Json.JsonSerializer.Serialize<__musoqStructuralCarrier_0_1>(writer, value.F1, options);
                }

                if ((value.P0 & 4UL) != 0)
                {
                    writer.WritePropertyName("Items");
                    global::System.Text.Json.JsonSerializer.Serialize<int[]>(writer, value.F2, options);
                }

                writer.WriteEndObject();
            }
        }
    }
}
