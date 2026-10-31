// === Parsed Query ===
/*
select Position, Floating from #schema.first() where Floating > Amount order by Position
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [ko3iko.Position]
    Project [ko3iko.Position as Position, ko3iko.Floating as Floating]
      Filter [(ToDecimal(ko3iko.Floating) > ToDecimal(ko3iko.Amount))]
        SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [ko3iko.Position]
    PhysicalProject [ko3iko.Position as Position, ko3iko.Floating as Floating]
      PhysicalFilter [(ToDecimal(ko3iko.Floating) > ToDecimal(ko3iko.Amount))]
        PhysicalSchemaScan [#schema.first() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: LibraryExecutionEntity]
      Position: int <- property Position
      Amount: decimal? <- property Amount
      Floating: double? <- property Floating
    GeneratedRecord [ResultRow0WithSortKeys]
      Position: int <- field Position
      Floating: double? <- field Floating
      __ordinal: int <- field __ordinal
    Generated [ResultRow0]
      Position: int <- field Position
      Floating: double? <- field Floating

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: LibraryExecutionEntity] -> ko3ikoRows
    CreateRecordList [resultOrderRecords: ResultRow0WithSortKeys]
    CreateObject [__resultOrderRecordsLibraryBase0: LibraryBase]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [floating: double? = ko3iko.Floating]
      If [(ToDecimal(floating) > ToDecimal(ko3iko.Amount))]
        AppendRecord [resultOrderRecords <- ResultRow0WithSortKeys(Position: ko3iko.Position, Floating: floating)]
    OrderRecordList [resultOrderRecords: ResultRow0WithSortKeys by Position ASC]
    TransferOwnedRows [resultOrderRecords -> result: ResultRow0WithSortKeys]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0WithSortKeys]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K80_NativeFloatingDecimalComparison
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
            new Column("Floating", typeof(double?), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Amount", typeof(decimal?), 1), new Column("Floating", typeof(double?), 2) });
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
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Floating);
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
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var resultOrderRecords = new List<ResultRow0WithSortKeys>();
                var __resultOrderRecordsLibraryBase0 = new Musoq.Plugins.LibraryBase();
                OnPhaseChanged("compiled", QueryPhase.Where);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                double? floating = ko3iko.Floating;
                                if (((Operators.SqlCompare<decimal?, decimal?>((decimal?)__resultOrderRecordsLibraryBase0.ToDecimal(floating), (decimal?)ko3iko.Amount, (decimal? __sqlLeft, decimal? __sqlRight) => (__sqlLeft > __sqlRight)))) == true)
                                {
                                    resultOrderRecords.Add(new ResultRow0WithSortKeys(ko3iko.Position, floating, resultOrderRecords.Count));
                                }
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                double? floating = ko3iko.Floating;
                                if (((Operators.SqlCompare<decimal?, decimal?>((decimal?)__resultOrderRecordsLibraryBase0.ToDecimal(floating), (decimal?)ko3iko.Amount, (decimal? __sqlLeft, decimal? __sqlRight) => (__sqlLeft > __sqlRight)))) == true)
                                {
                                    resultOrderRecords.Add(new ResultRow0WithSortKeys(ko3iko.Position, floating, resultOrderRecords.Count));
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
                        double? floating = ko3iko.Floating;
                        if (((Operators.SqlCompare<decimal?, decimal?>((decimal?)__resultOrderRecordsLibraryBase0.ToDecimal(floating), (decimal?)ko3iko.Amount, (decimal? __sqlLeft, decimal? __sqlRight) => (__sqlLeft > __sqlRight)))) == true)
                        {
                            resultOrderRecords.Add(new ResultRow0WithSortKeys(ko3iko.Position, floating, resultOrderRecords.Count));
                        }
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
            public ResultRow0(int __value0, double? __value1)
            {
                Position = __value0;
                Floating = __value1;
            }

            public override int Count => 2;
            public double? Floating { get; private set; }
            public int Position { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double?>(this.Floating = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<double?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double?>(this.Floating),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "Floating" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Floating, ref visitor);
            }
        }

        private sealed class ResultRow0WithSortKeys
        {
            public ResultRow0WithSortKeys(int Position, double? Floating, int __ordinal)
            {
                this.Position = Position;
                this.Floating = Floating;
                this.__ordinal = __ordinal;
            }

            public double? Floating { get; }
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
