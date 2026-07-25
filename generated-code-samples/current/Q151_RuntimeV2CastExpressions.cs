// === Parsed Query ===
/*
SELECT (Quantity + 1)::Int64 as QuantityNext,
                         Population::Int32::String as PopulationText,
                         CreatedAt::DateTimeOffset as CreatedOffset
                  FROM #features.items()
                  WHERE Population::Int32 > 1000
                  ORDER BY Amount::Decimal
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [ko3iko.Amount::Decimal]
    Project [(ko3iko.Quantity + 1)::Int64 as QuantityNext, ko3iko.Population::Int32::String as PopulationText, ko3iko.CreatedAt::DateTimeOffset as CreatedOffset]
      Filter [(ko3iko.Population::Int32 > 1000)]
        SchemaScan [#features.items() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [ko3iko.Amount::Decimal]
    PhysicalProject [(ko3iko.Quantity + 1)::Int64 as QuantityNext, ko3iko.Population::Int32::String as PopulationText, ko3iko.CreatedAt::DateTimeOffset as CreatedOffset]
      PhysicalFilter [(ko3iko.Population::Int32 > 1000)]
        PhysicalSchemaScan [#features.items() as ko3iko] [pushdown: (ko3iko.Population::Int32 > 1000)]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: RuntimeV2CastGroupingFeatureEntity]
      Population: string <- property Population
      Amount: string <- property Amount
      CreatedAt: string <- property CreatedAt
      Quantity: int <- property Quantity
    GeneratedRecord [ResultRow0WithSortKeys]
      QuantityNext: long? <- field QuantityNext
      PopulationText: string <- field PopulationText
      CreatedOffset: DateTimeOffset? <- field CreatedOffset
      __sortKey0: decimal? <- field __sortKey0
      __ordinal: int <- field __ordinal
    Generated [ResultRow0]
      QuantityNext: long? <- field QuantityNext
      PopulationText: string <- field PopulationText
      CreatedOffset: DateTimeOffset? <- field CreatedOffset

  Body
    SourceScan [ko3iko: RuntimeV2CastGroupingFeatureEntity] -> ko3ikoRows
    CreateRecordList [resultOrderRecords: ResultRow0WithSortKeys]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [population: string = ko3iko.Population]
      Let [populationInt32: int? = population::Int32]
      If [(populationInt32 > 1000)]
        AppendRecord [resultOrderRecords <- ResultRow0WithSortKeys(QuantityNext: (ko3iko.Quantity + 1)::Int64, PopulationText: populationInt32::String, CreatedOffset: ko3iko.CreatedAt::DateTimeOffset, __sortKey0: ko3iko.Amount::Decimal)]
    OrderRecordList [resultOrderRecords: ResultRow0WithSortKeys by __sortKey0 ASC]
    MaterializeRecordListToShapeRows [resultOrderRecords -> result: ResultShape0 fields 0, 1, 2]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q151_RuntimeV2CastExpressions
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
            new Column("QuantityNext", typeof(long?), 0),
            new Column("PopulationText", typeof(string), 1),
            new Column("CreatedOffset", typeof(DateTimeOffset?), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Population", typeof(string), 2), new Column("Amount", typeof(string), 3), new Column("CreatedAt", typeof(string), 5), new Column("Quantity", typeof(int), 6) });
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
                yield return new ResultRow0(__musoqShapeRow.QuantityNext, __musoqShapeRow.PopulationText, __musoqShapeRow.CreatedOffset);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.Where);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                var __ko3ikoSchema = provider.GetSchema("#features");
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity>("items", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = ko3ikoRowsSource.Chunks;
                var resultOrderRecords = new List<ResultRow0WithSortKeys>();
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                string population = ko3iko.Population;
                                int? populationInt32 = global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt32(population);
                                if ((populationInt32 > 1000))
                                {
                                    resultOrderRecords.Add(new ResultRow0WithSortKeys(global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt64((ko3iko.Quantity + 1)), global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToString(populationInt32), global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToDateTimeOffset(ko3iko.CreatedAt), global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToDecimal(ko3iko.Amount), resultOrderRecords.Count));
                                }
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                string population = ko3iko.Population;
                                int? populationInt32 = global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt32(population);
                                if ((populationInt32 > 1000))
                                {
                                    resultOrderRecords.Add(new ResultRow0WithSortKeys(global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt64((ko3iko.Quantity + 1)), global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToString(populationInt32), global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToDateTimeOffset(ko3iko.CreatedAt), global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToDecimal(ko3iko.Amount), resultOrderRecords.Count));
                                }
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
                        string population = ko3iko.Population;
                        int? populationInt32 = global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt32(population);
                        if ((populationInt32 > 1000))
                        {
                            resultOrderRecords.Add(new ResultRow0WithSortKeys(global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt64((ko3iko.Quantity + 1)), global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToString(populationInt32), global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToDateTimeOffset(ko3iko.CreatedAt), global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToDecimal(ko3iko.Amount), resultOrderRecords.Count));
                        }
                    }
                }

                resultOrderRecords.Sort(ResultRow0WithSortKeysComparer.Instance);
                foreach (var resultRecord in resultOrderRecords)
                {
                    __musoqFinalShapeRows.Add(new ResultShape0(resultRecord.QuantityNext, resultRecord.PopulationText, resultRecord.CreatedOffset));
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
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
            public ResultRow0(long? __value0, string __value1, DateTimeOffset? __value2)
            {
                QuantityNext = __value0;
                PopulationText = __value1;
                CreatedOffset = __value2;
            }

            public override int Count => 3;
            public DateTimeOffset? CreatedOffset { get; private set; }
            public string PopulationText { get; private set; }
            public long? QuantityNext { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        QuantityNext = (long?)value;
                        break;
                    case 1:
                        PopulationText = (string)value;
                        break;
                    case 2:
                        CreatedOffset = (DateTimeOffset?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "QuantityNext" => true,
                "PopulationText" => true,
                "CreatedOffset" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)QuantityNext,
                1 => (object)PopulationText,
                2 => (object)CreatedOffset,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "QuantityNext" => (object)QuantityNext,
                "PopulationText" => (object)PopulationText,
                "CreatedOffset" => (object)CreatedOffset,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultRow0WithSortKeys
        {
            public ResultRow0WithSortKeys(long? QuantityNext, string PopulationText, DateTimeOffset? CreatedOffset, decimal? __sortKey0, int __ordinal)
            {
                this.QuantityNext = QuantityNext;
                this.PopulationText = PopulationText;
                this.CreatedOffset = CreatedOffset;
                this.__sortKey0 = __sortKey0;
                this.__ordinal = __ordinal;
            }

            public DateTimeOffset? CreatedOffset { get; }
            public string PopulationText { get; }
            public long? QuantityNext { get; }
            public int __ordinal { get; }
            public decimal? __sortKey0 { get; }
        }

        private sealed class ResultRow0WithSortKeysComparer : IComparer<ResultRow0WithSortKeys>
        {
            public static readonly ResultRow0WithSortKeysComparer Instance = new ResultRow0WithSortKeysComparer();
            public int Compare(ResultRow0WithSortKeys left, ResultRow0WithSortKeys right)
            {
                var comparison = Nullable.Compare(left.__sortKey0, right.__sortKey0);
                if (comparison != 0)
                    return comparison;
                return left.__ordinal.CompareTo(right.__ordinal);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(long? QuantityNext, string PopulationText, DateTimeOffset? CreatedOffset)
            {
                this.QuantityNext = QuantityNext;
                this.PopulationText = PopulationText;
                this.CreatedOffset = CreatedOffset;
            }

            public DateTimeOffset? CreatedOffset { get; }
            public string PopulationText { get; }
            public long? QuantityNext { get; }
        }
    }
}
