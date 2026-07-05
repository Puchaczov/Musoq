/*
raw query string

select * replace (Population * 2 as Population) rename (Name as EntityName, Population as WeightedPopulation) from #A.entities()
*/

/*
logical plan representation string

MultiStatement
  Project [ko3iko.Name as EntityName, ko3iko.City as City, ko3iko.Country as Country, (ko3iko.Population * 2) as WeightedPopulation, ko3iko.Money as Money, ko3iko.Month as Month, ko3iko.Time as Time, ko3iko.Id as Id, ko3iko.NullableValue as NullableValue]
    SchemaScan [#A.entities() as ko3iko]
*/

/*
physical plan representation string

PhysicalMultiStatement
  PhysicalProject [ko3iko.Name as EntityName, ko3iko.City as City, ko3iko.Country as Country, (ko3iko.Population * 2) as WeightedPopulation, ko3iko.Money as Money, ko3iko.Month as Month, ko3iko.Time as Time, ko3iko.Id as Id, ko3iko.NullableValue as NullableValue]
    PhysicalSchemaScan [#A.entities() as ko3iko]
*/

/*
intermediate representation

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
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [ko3iko in ko3ikoRows]
      AppendShape [result <- ResultShape0(EntityName: ko3iko.Name, City: ko3iko.City, Country: ko3iko.Country, WeightedPopulation: (ko3iko.Population * 2), Money: ko3iko.Money, Month: ko3iko.Month, Time: ko3iko.Time, Id: ko3iko.Id, NullableValue: ko3iko.NullableValue)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable
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
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("City", typeof(string), 11), new Column("Country", typeof(string), 12), new Column("Population", typeof(decimal), 13), new Column("Money", typeof(decimal), 15), new Column("Month", typeof(string), 16), new Column("Time", typeof(DateTime), 17), new Column("Id", typeof(int), 18), new Column("NullableValue", typeof(int?), 19) });
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
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.Select);
            var __musoqExecutionState = ExecutionState.Capture(Parameters);
            ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
            var __ko3ikoSchema = provider.GetSchema("#A");
            var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var ko3ikoRows = ko3ikoRowsSource.Chunks;
            var __musoqTableSourceRows = ko3ikoRows;
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectRowsSerial<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity, ResultRow0>(__musoqTableSourceRows, (ko3iko) => true, (ko3iko) => new ResultRow0(ko3iko.Name, ko3iko.City, ko3iko.Country, (ko3iko.Population * 2), ko3iko.Money, ko3iko.Month, ko3iko.Time, ko3iko.Id, ko3iko.NullableValue), token), token, onCompleted: () =>
            {
                OnPhaseChanged("compiled", QueryPhase.End);
            }, onDisposed: () =>
            {
                OnPhaseChanged("compiled", QueryPhase.End);
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

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        EntityName = (string)value;
                        break;
                    case 1:
                        City = (string)value;
                        break;
                    case 2:
                        Country = (string)value;
                        break;
                    case 3:
                        WeightedPopulation = (decimal)value;
                        break;
                    case 4:
                        Money = (decimal)value;
                        break;
                    case 5:
                        Month = (string)value;
                        break;
                    case 6:
                        Time = (DateTime)value;
                        break;
                    case 7:
                        Id = (int)value;
                        break;
                    case 8:
                        NullableValue = (int?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

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
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)EntityName,
                1 => (object)City,
                2 => (object)Country,
                3 => (object)WeightedPopulation,
                4 => (object)Money,
                5 => (object)Month,
                6 => (object)Time,
                7 => (object)Id,
                8 => (object)NullableValue,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "EntityName" => (object)EntityName,
                "City" => (object)City,
                "Country" => (object)Country,
                "WeightedPopulation" => (object)WeightedPopulation,
                "Money" => (object)Money,
                "Month" => (object)Month,
                "Time" => (object)Time,
                "Id" => (object)Id,
                "NullableValue" => (object)NullableValue,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string EntityName, string City, string Country, decimal WeightedPopulation, decimal Money, string Month, DateTime Time, int Id, int? NullableValue)
            {
                this.EntityName = EntityName;
                this.City = City;
                this.Country = Country;
                this.WeightedPopulation = WeightedPopulation;
                this.Money = Money;
                this.Month = Month;
                this.Time = Time;
                this.Id = Id;
                this.NullableValue = NullableValue;
            }

            public string City { get; }
            public string Country { get; }
            public string EntityName { get; }
            public int Id { get; }
            public decimal Money { get; }
            public string Month { get; }
            public int? NullableValue { get; }
            public DateTime Time { get; }
            public decimal WeightedPopulation { get; }
        }
    }
}
