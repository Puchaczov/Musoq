// === Parsed Query ===
/*
let flag: bool = true
                            let code: char = 'x'
                            let limit: int? = null
                            let id: guid = '2ffcf6fa-3369-4300-946a-bb131a037985'
                            let created: datetime = '2024-01-02T03:04:05.0000000Z'
                            let elapsed: timespan = '01:30:00'
                            select $flag, $code, $limit, $id, $created, $elapsed
                            from #A.entities()
*/

// === Logical Plan ===
/*
MultiStatement
  Project [$flag as $flag, $code as $code, $limit as $limit, $id as $id, $created as $created, $elapsed as $elapsed]
    SchemaScan [#A.entities() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [$flag as $flag, $code as $code, $limit as $limit, $id as $id, $created as $created, $elapsed as $elapsed]
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
      Self: BasicEntity <- property Self
      Money: decimal <- property Money
      Month: string <- property Month
      Time: DateTime <- property Time
      Id: int <- property Id
      NullableValue: int? <- property NullableValue
      Array: int[] <- property Array
      Other: BasicEntity <- property Other
      Dictionary: Dictionary<string, string> <- property Dictionary
      Children: BasicEntity[] <- property Children
    Generated [ResultRow0]
      $flag: bool <- field _flag
      $code: char <- field _code
      $limit: int? <- field _limit
      $id: Guid <- field _id
      $created: DateTime <- field _created
      $elapsed: TimeSpan <- field _elapsed

  Body
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [ko3iko in ko3ikoRows]
      AppendShape [result <- ResultShape0($flag: $flag, $code: $code, $limit: $limit, $id: $id, $created: $created, $elapsed: $elapsed)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q131_ScriptVariablePrimitiveValues
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
            new Column("$flag", typeof(bool), 0),
            new Column("$code", typeof(char), 1),
            new Column("$limit", typeof(int?), 2),
            new Column("$id", typeof(Guid), 3),
            new Column("$created", typeof(DateTime), 4),
            new Column("$elapsed", typeof(TimeSpan), 5)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("City", typeof(string), 11), new Column("Country", typeof(string), 12), new Column("Population", typeof(decimal), 13), new Column("Self", typeof(Musoq.Evaluator.Tests.Schema.Basic.BasicEntity), 14), new Column("Money", typeof(decimal), 15), new Column("Month", typeof(string), 16), new Column("Time", typeof(DateTime), 17), new Column("Id", typeof(int), 18), new Column("NullableValue", typeof(int?), 19), new Column("Array", typeof(int[]), 20), new Column("Other", typeof(Musoq.Evaluator.Tests.Schema.Basic.BasicEntity), 21), new Column("Dictionary", typeof(Dictionary<string, string>), 22), new Column("Children", typeof(Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[]), 23) });
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
            const bool letFlag = true;
            const char letCode = 'x';
            int? letLimit = default(int?);
            Guid letId = new Guid("2ffcf6fa-3369-4300-946a-bb131a037985");
            DateTime letCreated = new DateTime(638397614450000000L, DateTimeKind.Utc);
            TimeSpan letElapsed = new TimeSpan(54000000000L);
            var __ko3ikoSchema = provider.GetSchema("#A");
            var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var ko3ikoRows = ko3ikoRowsSource.Chunks;
            var __musoqTableSourceRows = ko3ikoRows;
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectRowsSerial<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity, ResultRow0>(__musoqTableSourceRows, (ko3iko) => true, (ko3iko) => new ResultRow0(letFlag, letCode, letLimit, letId, letCreated, letElapsed), token), token, onCompleted: () =>
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
            public ResultRow0(bool __value0, char __value1, int? __value2, Guid __value3, DateTime __value4, TimeSpan __value5)
            {
                _flag = __value0;
                _code = __value1;
                _limit = __value2;
                _id = __value3;
                _created = __value4;
                _elapsed = __value5;
            }

            public override int Count => 6;
            public char _code { get; private set; }
            public DateTime _created { get; private set; }
            public TimeSpan _elapsed { get; private set; }
            public bool _flag { get; private set; }
            public Guid _id { get; private set; }
            public int? _limit { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        _flag = (bool)value;
                        break;
                    case 1:
                        _code = (char)value;
                        break;
                    case 2:
                        _limit = (int?)value;
                        break;
                    case 3:
                        _id = (Guid)value;
                        break;
                    case 4:
                        _created = (DateTime)value;
                        break;
                    case 5:
                        _elapsed = (TimeSpan)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "$flag" => true,
                "_flag" => true,
                "$code" => true,
                "_code" => true,
                "$limit" => true,
                "_limit" => true,
                "$id" => true,
                "_id" => true,
                "$created" => true,
                "_created" => true,
                "$elapsed" => true,
                "_elapsed" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)_flag,
                1 => (object)_code,
                2 => (object)_limit,
                3 => (object)_id,
                4 => (object)_created,
                5 => (object)_elapsed,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "$flag" => (object)_flag,
                "_flag" => (object)_flag,
                "$code" => (object)_code,
                "_code" => (object)_code,
                "$limit" => (object)_limit,
                "_limit" => (object)_limit,
                "$id" => (object)_id,
                "_id" => (object)_id,
                "$created" => (object)_created,
                "_created" => (object)_created,
                "$elapsed" => (object)_elapsed,
                "_elapsed" => (object)_elapsed,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(bool _flag, char _code, int? _limit, Guid _id, DateTime _created, TimeSpan _elapsed)
            {
                this._flag = _flag;
                this._code = _code;
                this._limit = _limit;
                this._id = _id;
                this._created = _created;
                this._elapsed = _elapsed;
            }

            public char _code { get; }
            public DateTime _created { get; }
            public TimeSpan _elapsed { get; }
            public bool _flag { get; }
            public Guid _id { get; }
            public int? _limit { get; }
        }
    }
}
