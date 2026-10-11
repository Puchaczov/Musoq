// === Parsed Query ===
/*
SELECT Population::Int32 as PopulationInt,
                         Amount::Decimal as AmountDecimal,
                         Id::Guid as EntityGuid
                  FROM #features.items()
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Population::Int32 as PopulationInt, ko3iko.Amount::Decimal as AmountDecimal, ko3iko.Id::Guid as EntityGuid]
    SchemaScan [#features.items() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Population::Int32 as PopulationInt, ko3iko.Amount::Decimal as AmountDecimal, ko3iko.Id::Guid as EntityGuid]
    PhysicalSchemaScan [#features.items() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: RuntimeV2CastGroupingFeatureEntity]
      Population: string <- property Population
      Amount: string <- property Amount
      Id: string <- property Id
    Generated [ResultRow0]
      PopulationInt: int? <- field PopulationInt
      AmountDecimal: decimal? <- field AmountDecimal
      EntityGuid: Guid? <- field EntityGuid

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: RuntimeV2CastGroupingFeatureEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      AppendShape [result <- ResultRow0(PopulationInt: ko3iko.Population::Int32, AmountDecimal: ko3iko.Amount::Decimal, EntityGuid: ko3iko.Id::Guid)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q150_RuntimeV2CastProjection
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
            new Column("PopulationInt", typeof(int?), 0),
            new Column("AmountDecimal", typeof(decimal?), 1),
            new Column("EntityGuid", typeof(Guid?), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Population", typeof(string), 0), new Column("Amount", typeof(string), 1), new Column("Id", typeof(string), 2) });
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
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
            ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
            this.OnPhaseChanged("compiled", QueryPhase.Begin);
            this.OnPhaseChanged("compiled", QueryPhase.From);
            var __ko3ikoSchema = provider.GetSchema("#features");
            var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity>.Open(__ko3ikoSchema, "items", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
            var __musoqTableSourceRows = ko3ikoRows;
            this.OnPhaseChanged("compiled", QueryPhase.Select);
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectRowsSerial<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity, ResultRow0>(__musoqTableSourceRows, (ko3iko) => true, (ko3iko) => new ResultRow0(global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt32(ko3iko.Population), global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToDecimal(ko3iko.Amount), global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToGuid(ko3iko.Id)), token), token, onCompleted: () =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }, onException: (Exception _) =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }, onDisposed: () =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            });
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
            public ResultRow0(int? __value0, decimal? __value1, Guid? __value2)
            {
                PopulationInt = __value0;
                AmountDecimal = __value1;
                EntityGuid = __value2;
            }

            public decimal? AmountDecimal { get; private set; }
            public override int Count => 3;
            public Guid? EntityGuid { get; private set; }
            public int? PopulationInt { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.PopulationInt = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.AmountDecimal = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, Guid?>(this.EntityGuid = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<Guid?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.PopulationInt),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.AmountDecimal),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, Guid?>(this.EntityGuid),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "PopulationInt" => true,
                "AmountDecimal" => true,
                "EntityGuid" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.PopulationInt, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.AmountDecimal, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.EntityGuid, ref visitor);
            }
        }
    }
}
