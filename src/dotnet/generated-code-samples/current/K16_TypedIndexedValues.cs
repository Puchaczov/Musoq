// === Parsed Query ===
/*
select Position, Numbers[-1] as Last, Numbers[8] as Missing,
    Amounts[1] as NullableAmount, Costs['present'] as Cost, Costs['missing'] as MissingCost
from #schema.first() order by Position
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [ko3iko.Position]
    Project [ko3iko.Position as Position, ko3iko.Numbers[-1] as Last, ko3iko.Numbers[8] as Missing, ko3iko.Amounts[1] as NullableAmount, ko3iko.Costs['present'] as Cost, ko3iko.Costs['missing'] as MissingCost]
      SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [ko3iko.Position]
    PhysicalProject [ko3iko.Position as Position, ko3iko.Numbers[-1] as Last, ko3iko.Numbers[8] as Missing, ko3iko.Amounts[1] as NullableAmount, ko3iko.Costs['present'] as Cost, ko3iko.Costs['missing'] as MissingCost]
      PhysicalSchemaScan [#schema.first() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: IndexedEntity]
      Position: int <- property Position
      Numbers: int[] <- property Numbers
      Amounts: decimal?[] <- property Amounts
      Costs: Dictionary<string, decimal> <- property Costs
    GeneratedRecord [ResultRow0WithSortKeys]
      Position: int <- field Position
      Last: int <- field Last
      Missing: int <- field Missing
      NullableAmount: decimal? <- field NullableAmount
      Cost: decimal <- field Cost
      MissingCost: decimal <- field MissingCost
      __ordinal: int <- field __ordinal
    Generated [ResultRow0]
      Position: int <- field Position
      Last: int <- field Last
      Missing: int <- field Missing
      NullableAmount: decimal? <- field NullableAmount
      Cost: decimal <- field Cost
      MissingCost: decimal <- field MissingCost

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: IndexedEntity] -> ko3ikoRows
    CreateRecordList [resultOrderRecords: ResultRow0WithSortKeys]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [numbers: int[] = ko3iko.Numbers]
      Let [costs: Dictionary<string, decimal> = ko3iko.Costs]
      AppendRecord [resultOrderRecords <- ResultRow0WithSortKeys(Position: ko3iko.Position, Last: GetTypedReadOnlyListElement(numbers, -1), Missing: GetTypedReadOnlyListElement(numbers, 8), NullableAmount: GetTypedReadOnlyListElement(ko3iko.Amounts, 1), Cost: GetTypedDictionaryValue(costs, 'present'), MissingCost: GetTypedDictionaryValue(costs, 'missing'))]
    OrderRecordList [resultOrderRecords: ResultRow0WithSortKeys by Position ASC]
    TransferOwnedRows [resultOrderRecords -> result: ResultRow0WithSortKeys]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0WithSortKeys]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K16_TypedIndexedValues
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
            new Column("Position", typeof(int), 0),
            new Column("Last", typeof(int), 1),
            new Column("Missing", typeof(int), 2),
            new Column("NullableAmount", typeof(decimal?), 3),
            new Column("Cost", typeof(decimal), 4),
            new Column("MissingCost", typeof(decimal), 5)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Numbers", typeof(int[]), 1), new Column("Amounts", typeof(decimal? []), 2), new Column("Costs", typeof(Dictionary<string, decimal>), 3) });
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
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Last, __musoqShapeRow.Missing, __musoqShapeRow.NullableAmount, __musoqShapeRow.Cost, __musoqShapeRow.MissingCost);
            }
        }

        private IEnumerable<ResultRow0WithSortKeys> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
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
                var __ko3ikoSchema = provider.GetSchema("#schema");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionRelationalQueryTests.IndexedEntity>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionRelationalQueryTests.IndexedEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var resultOrderRecords = new List<ResultRow0WithSortKeys>();
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionRelationalQueryTests.IndexedEntity> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionRelationalQueryTests.IndexedEntity[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                int[] numbers = ko3iko.Numbers;
                                Dictionary<string, decimal> costs = ko3iko.Costs;
                                resultOrderRecords.Add(new ResultRow0WithSortKeys(ko3iko.Position, (int)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<int[], int>(numbers, -1), (int)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<int[], int>(numbers, 8), (decimal?)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<decimal? [], decimal?>(ko3iko.Amounts, 1), (decimal)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedDictionaryValue<Dictionary<string, decimal>, string, decimal>(costs, "present"), (decimal)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedDictionaryValue<Dictionary<string, decimal>, string, decimal>(costs, "missing"), resultOrderRecords.Count));
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionRelationalQueryTests.IndexedEntity> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                int[] numbers = ko3iko.Numbers;
                                Dictionary<string, decimal> costs = ko3iko.Costs;
                                resultOrderRecords.Add(new ResultRow0WithSortKeys(ko3iko.Position, (int)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<int[], int>(numbers, -1), (int)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<int[], int>(numbers, 8), (decimal?)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<decimal? [], decimal?>(ko3iko.Amounts, 1), (decimal)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedDictionaryValue<Dictionary<string, decimal>, string, decimal>(costs, "present"), (decimal)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedDictionaryValue<Dictionary<string, decimal>, string, decimal>(costs, "missing"), resultOrderRecords.Count));
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
                        int[] numbers = ko3iko.Numbers;
                        Dictionary<string, decimal> costs = ko3iko.Costs;
                        resultOrderRecords.Add(new ResultRow0WithSortKeys(ko3iko.Position, (int)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<int[], int>(numbers, -1), (int)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<int[], int>(numbers, 8), (decimal?)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedReadOnlyListElement<decimal? [], decimal?>(ko3iko.Amounts, 1), (decimal)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedDictionaryValue<Dictionary<string, decimal>, string, decimal>(costs, "present"), (decimal)Musoq.Evaluator.Helpers.SafeArrayAccess.GetTypedDictionaryValue<Dictionary<string, decimal>, string, decimal>(costs, "missing"), resultOrderRecords.Count));
                    }
                }

                resultOrderRecords.Sort(ResultRow0WithSortKeysComparer.Instance);
                return resultOrderRecords;
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
            public ResultRow0(int __value0, int __value1, int __value2, decimal? __value3, decimal __value4, decimal __value5)
            {
                Position = __value0;
                Last = __value1;
                Missing = __value2;
                NullableAmount = __value3;
                Cost = __value4;
                MissingCost = __value5;
            }

            public decimal Cost { get; private set; }
            public override int Count => 6;
            public int Last { get; private set; }
            public int Missing { get; private set; }
            public decimal MissingCost { get; private set; }
            public decimal? NullableAmount { get; private set; }
            public int Position { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Last = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Missing = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.NullableAmount = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Cost = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.MissingCost = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Last),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Missing),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.NullableAmount),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Cost),
                5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.MissingCost),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "Last" => true,
                "Missing" => true,
                "NullableAmount" => true,
                "Cost" => true,
                "MissingCost" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Last, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Missing, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.NullableAmount, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.Cost, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(5, this.MissingCost, ref visitor);
            }
        }

        private sealed class ResultRow0WithSortKeys
        {
            public ResultRow0WithSortKeys(int Position, int Last, int Missing, decimal? NullableAmount, decimal Cost, decimal MissingCost, int __ordinal)
            {
                this.Position = Position;
                this.Last = Last;
                this.Missing = Missing;
                this.NullableAmount = NullableAmount;
                this.Cost = Cost;
                this.MissingCost = MissingCost;
                this.__ordinal = __ordinal;
            }

            public decimal Cost { get; }
            public int Last { get; }
            public int Missing { get; }
            public decimal MissingCost { get; }
            public decimal? NullableAmount { get; }
            public int Position { get; }
            public int __ordinal { get; }
        }

        private sealed class ResultRow0WithSortKeysComparer : IComparer<ResultRow0WithSortKeys>
        {
            public static readonly ResultRow0WithSortKeysComparer Instance = new ResultRow0WithSortKeysComparer();
            public int Compare(ResultRow0WithSortKeys left, ResultRow0WithSortKeys right)
            {
                var comparison = left.Position.CompareTo(right.Position);
                if (comparison != 0)
                    return comparison;
                return left.__ordinal.CompareTo(right.__ordinal);
            }
        }
    }
}
