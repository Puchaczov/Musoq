// === Parsed Query ===
/*
select Customer, Sum(Amount) as Total, PositiveCount(Amount) as Positive, Avg(Quantity) as AverageQuantity from #schema.first() group by Customer order by Customer
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Customer as ko3iko.Customer, AggRef(ko3iko.Avg(ko3iko.Quantity)) as ko3iko.Avg(ko3iko.Quantity), AggRef(ko3iko.PositiveCount(ko3iko.Amount)) as ko3iko.PositiveCount(ko3iko.Amount), AggRef(ko3iko.Sum(ko3iko.Amount)) as ko3iko.Sum(ko3iko.Amount)]
    Aggregate [keys: Customer] [aggs: Avg(Quantity), PositiveCount(Amount), Sum(Amount)]
      SchemaScan [#schema.first() as ko3iko]
  Sort [ko3iko.Customer]
    Project [ko3iko.Customer as Customer, ko3iko.Sum(ko3iko.Amount) as Total, ko3iko.PositiveCount(ko3iko.Amount) as Positive, ko3iko.Avg(ko3iko.Quantity) as AverageQuantity]
      CteRef [ko3ikoScore as ko3ikoScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Customer as ko3iko.Customer, AggRef(ko3iko.Avg(ko3iko.Quantity)) as ko3iko.Avg(ko3iko.Quantity), AggRef(ko3iko.PositiveCount(ko3iko.Amount)) as ko3iko.PositiveCount(ko3iko.Amount), AggRef(ko3iko.Sum(ko3iko.Amount)) as ko3iko.Sum(ko3iko.Amount)]
    PhysicalSingleKeyAggregate [key: Customer (String)] [aggs: Avg(Quantity), PositiveCount(Amount), Sum(Amount)]
      PhysicalSchemaScan [#schema.first() as ko3iko]
  PhysicalSort [ko3iko.Customer]
    PhysicalProject [ko3iko.Customer as Customer, ko3iko.Sum(ko3iko.Amount) as Total, ko3iko.PositiveCount(ko3iko.Amount) as Positive, ko3iko.Avg(ko3iko.Quantity) as AverageQuantity]
      PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: Invoice]
      Customer: string <- property Customer
      Amount: decimal? <- property Amount
      Quantity: int? <- property Quantity
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 3]
    Generated [ResultRow0]
      Customer: string <- field Customer
      Total: decimal? <- field Total
      Positive: long <- field Positive
      AverageQuantity: decimal? <- field AverageQuantity

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: Invoice] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [groups: string -> ResultAggregateGroup]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      GetOrAddSingleKeyAggregateGroup [group = groups[ko3iko.Customer] by Customer; typed: ResultAggregateGroup]
      Let [quantity: int? = ko3iko.Quantity]
      TypedAggregateSet [Set(group.__agg0, quantity)]
      Let [amount: decimal? = ko3iko.Amount]
      TypedAggregateSet [Set(group.__agg1, amount)]
      Let [amount1: decimal? = ko3iko.Amount]
      TypedAggregateSet [Set(group.__agg2, amount1)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(Customer: finalGroup.Customer, Total: ko3iko.Sum(ko3iko.Amount), Positive: ko3iko.PositiveCount(ko3iko.Amount), AverageQuantity: ko3iko.Avg(ko3iko.Quantity))]
    SortShapeRows [result -> resultSorted by Customer ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K42_InvoiceLibraryGrouped
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
            new Column("Customer", typeof(string), 0),
            new Column("Total", typeof(decimal?), 1),
            new Column("Positive", typeof(long), 2),
            new Column("AverageQuantity", typeof(decimal?), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Customer", typeof(string), 0), new Column("Amount", typeof(decimal?), 1), new Column("Quantity", typeof(int?), 2) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Customer, __musoqShapeRow.Total, __musoqShapeRow.Positive, __musoqShapeRow.AverageQuantity);
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
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionDocumentationQueryTests.Invoice>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionDocumentationQueryTests.Invoice>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<string, ResultAggregateGroup>();
                ResultAggregateGroup nullGroup = null;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionDocumentationQueryTests.Invoice> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionDocumentationQueryTests.Invoice[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                string groupKey = ko3iko.Customer;
                                ResultAggregateGroup group = null;
                                if (groupKey != null)
                                {
                                    ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var groupExists);
                                    if (!groupExists)
                                    {
                                        groupRef = new ResultAggregateGroup(groupKey);
                                        groupsToFinalize.Add(groupRef);
                                    }

                                    group = groupRef;
                                }
                                else
                                {
                                    if (nullGroup == null)
                                    {
                                        nullGroup = new ResultAggregateGroup(null);
                                        groupsToFinalize.Add(nullGroup);
                                    }

                                    group = nullGroup;
                                }

                                int? quantity = ko3iko.Quantity;
                                Musoq.Plugins.AverageKernel<int, decimal>.Set(ref group.__agg0, (int?)quantity);
                                decimal? amount = ko3iko.Amount;
                                Musoq.TypedLibraryExamples.InvoiceLibrary.PositiveCountKernel.Set(ref group.__agg1, (decimal?)amount);
                                decimal? amount1 = ko3iko.Amount;
                                Musoq.TypedLibraryExamples.SumKernel<decimal>.Set(ref group.__agg2, (decimal?)amount1);
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionDocumentationQueryTests.Invoice> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                string groupKey = ko3iko.Customer;
                                ResultAggregateGroup group = null;
                                if (groupKey != null)
                                {
                                    ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var groupExists);
                                    if (!groupExists)
                                    {
                                        groupRef = new ResultAggregateGroup(groupKey);
                                        groupsToFinalize.Add(groupRef);
                                    }

                                    group = groupRef;
                                }
                                else
                                {
                                    if (nullGroup == null)
                                    {
                                        nullGroup = new ResultAggregateGroup(null);
                                        groupsToFinalize.Add(nullGroup);
                                    }

                                    group = nullGroup;
                                }

                                int? quantity = ko3iko.Quantity;
                                Musoq.Plugins.AverageKernel<int, decimal>.Set(ref group.__agg0, (int?)quantity);
                                decimal? amount = ko3iko.Amount;
                                Musoq.TypedLibraryExamples.InvoiceLibrary.PositiveCountKernel.Set(ref group.__agg1, (decimal?)amount);
                                decimal? amount1 = ko3iko.Amount;
                                Musoq.TypedLibraryExamples.SumKernel<decimal>.Set(ref group.__agg2, (decimal?)amount1);
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
                        string groupKey = ko3iko.Customer;
                        ResultAggregateGroup group = null;
                        if (groupKey != null)
                        {
                            ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var groupExists);
                            if (!groupExists)
                            {
                                groupRef = new ResultAggregateGroup(groupKey);
                                groupsToFinalize.Add(groupRef);
                            }

                            group = groupRef;
                        }
                        else
                        {
                            if (nullGroup == null)
                            {
                                nullGroup = new ResultAggregateGroup(null);
                                groupsToFinalize.Add(nullGroup);
                            }

                            group = nullGroup;
                        }

                        int? quantity = ko3iko.Quantity;
                        Musoq.Plugins.AverageKernel<int, decimal>.Set(ref group.__agg0, (int?)quantity);
                        decimal? amount = ko3iko.Amount;
                        Musoq.TypedLibraryExamples.InvoiceLibrary.PositiveCountKernel.Set(ref group.__agg1, (decimal?)amount);
                        decimal? amount1 = ko3iko.Amount;
                        Musoq.TypedLibraryExamples.SumKernel<decimal>.Set(ref group.__agg2, (decimal?)amount1);
                    }
                }

                result.EnsureCapacity(groupsToFinalize.Count);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    result.Add(new ResultShape0(finalGroup.__key0, Musoq.TypedLibraryExamples.SumKernel<decimal>.Get(in finalGroup.__agg2), Musoq.TypedLibraryExamples.InvoiceLibrary.PositiveCountKernel.Get(in finalGroup.__agg1), Musoq.Plugins.AverageKernel<int, decimal>.Get(in finalGroup.__agg0)));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.Customer, right.Customer);
                    if (comparison != 0)
                        return comparison;
                    return 0;
                }));
                foreach (var resultSortedRowsRow in resultSortedRows)
                {
                    __musoqFinalShapeRows.Add(resultSortedRowsRow);
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

        private sealed class ResultAggregateGroup
        {
            public Musoq.Plugins.AverageKernel<int, decimal>.State __agg0;
            public Musoq.TypedLibraryExamples.InvoiceLibrary.PositiveCountKernel.State __agg1;
            public Musoq.TypedLibraryExamples.SumKernel<decimal>.State __agg2;
            public readonly string __key0;
            public ResultAggregateGroup(string __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.AverageKernel<int, decimal>.Reset(ref this.__agg0);
                Musoq.TypedLibraryExamples.InvoiceLibrary.PositiveCountKernel.Reset(ref this.__agg1);
                Musoq.TypedLibraryExamples.SumKernel<decimal>.Reset(ref this.__agg2);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, decimal? __value1, long __value2, decimal? __value3)
            {
                Customer = __value0;
                Total = __value1;
                Positive = __value2;
                AverageQuantity = __value3;
            }

            public decimal? AverageQuantity { get; private set; }
            public override int Count => 4;
            public string Customer { get; private set; }
            public long Positive { get; private set; }
            public decimal? Total { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Customer = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Positive = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.AverageQuantity = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Customer),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Positive),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.AverageQuantity),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Customer" => true,
                "Total" => true,
                "Positive" => true,
                "AverageQuantity" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Customer, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Positive, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.AverageQuantity, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Customer, decimal? Total, long Positive, decimal? AverageQuantity)
            {
                this.Customer = Customer;
                this.Total = Total;
                this.Positive = Positive;
                this.AverageQuantity = AverageQuantity;
            }

            public decimal? AverageQuantity { get; }
            public string Customer { get; }
            public long Positive { get; }
            public decimal? Total { get; }
        }
    }
}
