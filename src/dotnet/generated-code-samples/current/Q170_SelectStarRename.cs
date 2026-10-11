// === Parsed Query ===
/*
select * replace (Population * 2 as Population) rename (Name as EntityName, Population as WeightedPopulation) from #A.entities()
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Name as EntityName, ko3iko.City as City, ko3iko.Country as Country, (ko3iko.Population * 2) as WeightedPopulation, ko3iko.Money as Money, ko3iko.Month as Month, ko3iko.Time as Time, ko3iko.Id as Id, ko3iko.NullableValue as NullableValue]
    SchemaScan [#A.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Name as EntityName, ko3iko.City as City, ko3iko.Country as Country, (ko3iko.Population * 2) as WeightedPopulation, ko3iko.Money as Money, ko3iko.Month as Month, ko3iko.Time as Time, ko3iko.Id as Id, ko3iko.NullableValue as NullableValue]
    PhysicalSchemaScan [#A.entities() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
      City: string <- property City
      Country: string <- property Country
      Population: decimal <- property Population
      Money: decimal <- property Money
      Month: string <- property Month
      Time: DateTime <- property Time
      Id: int <- property Id
      NullableValue: int? <- property NullableValue
    Generated [ResultRow0]
      EntityName: string <- field EntityName
      City: string <- field City
      Country: string <- field Country
      WeightedPopulation: decimal <- field WeightedPopulation
      Money: decimal <- field Money
      Month: string <- field Month
      Time: DateTime <- field Time
      Id: int <- field Id
      NullableValue: int? <- field NullableValue

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      AppendShape [result <- ResultRow0(EntityName: ko3iko.Name, City: ko3iko.City, Country: ko3iko.Country, WeightedPopulation: (ko3iko.Population * 2), Money: ko3iko.Money, Month: ko3iko.Month, Time: ko3iko.Time, Id: ko3iko.Id, NullableValue: ko3iko.NullableValue)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q170_SelectStarRename
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
            new Column("EntityName", typeof(string), 0),
            new Column("City", typeof(string), 1),
            new Column("Country", typeof(string), 2),
            new Column("WeightedPopulation", typeof(decimal), 3),
            new Column("Money", typeof(decimal), 4),
            new Column("Month", typeof(string), 5),
            new Column("Time", typeof(DateTime), 6),
            new Column("Id", typeof(int), 7),
            new Column("NullableValue", typeof(int?), 8)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("City", typeof(string), 1), new Column("Country", typeof(string), 2), new Column("Population", typeof(decimal), 3), new Column("Money", typeof(decimal), 4), new Column("Month", typeof(string), 5), new Column("Time", typeof(DateTime), 6), new Column("Id", typeof(int), 7), new Column("NullableValue", typeof(int?), 8) });
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
            var __ko3ikoSchema = provider.GetSchema("#A");
            var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__ko3ikoSchema, "entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
            var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
            var __musoqTableSourceRows = ko3ikoRows;
            this.OnPhaseChanged("compiled", QueryPhase.Select);
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectRowsSerial<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity, ResultRow0>(__musoqTableSourceRows, (ko3iko) => true, (ko3iko) => new ResultRow0(ko3iko.Name, ko3iko.City, ko3iko.Country, (ko3iko.Population * 2), ko3iko.Money, ko3iko.Month, ko3iko.Time, ko3iko.Id, ko3iko.NullableValue), token), token, onCompleted: () =>
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
            public ResultRow0(string __value0, string __value1, string __value2, decimal __value3, decimal __value4, string __value5, DateTime __value6, int __value7, int? __value8)
            {
                EntityName = __value0;
                City = __value1;
                Country = __value2;
                WeightedPopulation = __value3;
                Money = __value4;
                Month = __value5;
                Time = __value6;
                Id = __value7;
                NullableValue = __value8;
            }

            public string City { get; private set; }
            public override int Count => 9;
            public string Country { get; private set; }
            public string EntityName { get; private set; }
            public int Id { get; private set; }
            public decimal Money { get; private set; }
            public string Month { get; private set; }
            public int? NullableValue { get; private set; }
            public DateTime Time { get; private set; }
            public decimal WeightedPopulation { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.EntityName = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Country = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.WeightedPopulation = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Money = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Month = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, DateTime>(this.Time = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<DateTime, T>(value)),
                    7 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    8 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.NullableValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.EntityName),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Country),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.WeightedPopulation),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Money),
                5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Month),
                6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, DateTime>(this.Time),
                7 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                8 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.NullableValue),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "EntityName" => true,
                "City" => true,
                "Country" => true,
                "WeightedPopulation" => true,
                "Money" => true,
                "Month" => true,
                "Time" => true,
                "Id" => true,
                "NullableValue" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.EntityName, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.City, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Country, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.WeightedPopulation, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.Money, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(5, this.Month, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(6, this.Time, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(7, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(8, this.NullableValue, ref visitor);
            }
        }
    }
}
