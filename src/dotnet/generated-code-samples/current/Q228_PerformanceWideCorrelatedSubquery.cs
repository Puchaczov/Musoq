// === Parsed Query ===
/*
SELECT a.Name,
       CASE WHEN EXISTS (
           SELECT b.City FROM #B.entities() b
           WHERE b.Name = a.Name
             AND b.City = a.City
             AND b.Country = a.Country
             AND b.Population = a.Population
             AND b.Month = a.Month
             AND b.Money = a.Money
             AND b.Id = a.Id
             AND b.NullableValue = a.NullableValue
       ) THEN 'Y' ELSE 'N' END AS ExistsResult,
       CASE WHEN NOT EXISTS (
           SELECT b.City FROM #B.entities() b
           WHERE b.Name = a.Name
             AND b.City = a.City
             AND b.Country = a.Country
             AND b.Population = a.Population
             AND b.Month = a.Month
             AND b.Money = a.Money
             AND b.Id = a.Id
             AND b.NullableValue = a.NullableValue
       ) THEN 'Y' ELSE 'N' END AS NotExistsResult,
       (
           SELECT b.City FROM #B.entities() b
           WHERE b.Name = a.Name
             AND b.City = a.City
             AND b.Country = a.Country
             AND b.Population = a.Population
             AND b.Month = a.Month
             AND b.Money = a.Money
             AND b.Id = a.Id
             AND b.NullableValue = a.NullableValue
       ) AS Lookup
FROM #A.entities() a
ORDER BY a.Name
*/

// === Logical Plan ===
/*
Cte
  Definition [_sq_1]
    MultiStatement
      Project [b.NullableValue as b.NullableValue, b.Id as b.Id, b.Money as b.Money, b.Month as b.Month, b.Population as b.Population, b.Country as b.Country, b.City as b.City, b.Name as b.Name, AggRef(b.__CorrelatedScalarSubqueryValue(b.City)) as b.__CorrelatedScalarSubqueryValue(b.City)]
        Aggregate [keys: b.Name, b.City, b.Country, b.Population, b.Month, b.Money, b.Id, b.NullableValue] [aggs: __CorrelatedScalarSubqueryValue(City)]
          SchemaScan [#B.entities() as b]
      Project [b.Name as _sq_1_corr_0, b.City as _sq_1_corr_1, b.Country as _sq_1_corr_2, b.Population as _sq_1_corr_3, b.Month as _sq_1_corr_4, b.Money as _sq_1_corr_5, b.Id as _sq_1_corr_6, b.NullableValue as _sq_1_corr_7, b.__CorrelatedScalarSubqueryValue(b.City) as _sq_1_value]
        CteRef [bScore as bScore]
  Query
    MultiStatement
      Project [a.Name as a.Name, a.City as a.City, a.Country as a.Country, a.Population as a.Population, a.Money as a.Money, a.Month as a.Month, a.Id as a.Id, a.NullableValue as a.NullableValue, _sq_1._sq_1_corr_0 as _sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1 as _sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2 as _sq_1._sq_1_corr_2, _sq_1._sq_1_corr_3 as _sq_1._sq_1_corr_3, _sq_1._sq_1_corr_4 as _sq_1._sq_1_corr_4, _sq_1._sq_1_corr_5 as _sq_1._sq_1_corr_5, _sq_1._sq_1_corr_6 as _sq_1._sq_1_corr_6, _sq_1._sq_1_corr_7 as _sq_1._sq_1_corr_7, _sq_1._sq_1_value as _sq_1._sq_1_value]
        Join [LeftSingle] [((((((((_sq_1._sq_1_corr_0 = a.Name) AND (_sq_1._sq_1_corr_1 = a.City)) AND (_sq_1._sq_1_corr_2 = a.Country)) AND (_sq_1._sq_1_corr_3 = a.Population)) AND (_sq_1._sq_1_corr_4 = a.Month)) AND (_sq_1._sq_1_corr_5 = a.Money)) AND (_sq_1._sq_1_corr_6 = a.Id)) AND (_sq_1._sq_1_corr_7 = a.NullableValue))]
          SchemaScan [#A.entities() as a]
          CteRef [_sq_1 as _sq_1]
      Sort [a.Name]
        Project [a.Name as Name, CASE WHEN ApplyExists[Subquery0](a.Name, a.City, a.Country, a.Population, a.Month, a.Money, a.Id, a.NullableValue; MultiStatement
  Project [b.City as City]
    Filter [((((((((b.Name = outer[0]) AND (b.City = outer[1])) AND (b.Country = outer[2])) AND (b.Population = outer[3])) AND (b.Month = outer[4])) AND (b.Money = outer[5])) AND (b.Id = outer[6])) AND (b.NullableValue = outer[7]))]
      SchemaScan [#B.entities() as b]) THEN 'Y' ELSE 'N' END as ExistsResult, CASE WHEN NOT ApplyExists[Subquery1](a.Name, a.City, a.Country, a.Population, a.Month, a.Money, a.Id, a.NullableValue; MultiStatement
  Project [b.City as City]
    Filter [((((((((b.Name = outer[0]) AND (b.City = outer[1])) AND (b.Country = outer[2])) AND (b.Population = outer[3])) AND (b.Month = outer[4])) AND (b.Money = outer[5])) AND (b.Id = outer[6])) AND (b.NullableValue = outer[7]))]
      SchemaScan [#B.entities() as b]) THEN 'Y' ELSE 'N' END as NotExistsResult, __CorrelatedScalarSubqueryResult(_sq_1._sq_1_value) as Lookup]
          CteRef [a_sq_1 as a_sq_1]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [_sq_1]
    PhysicalMultiStatement
      PhysicalProject [b.NullableValue as b.NullableValue, b.Id as b.Id, b.Money as b.Money, b.Month as b.Month, b.Population as b.Population, b.Country as b.Country, b.City as b.City, b.Name as b.Name, AggRef(b.__CorrelatedScalarSubqueryValue(b.City)) as b.__CorrelatedScalarSubqueryValue(b.City)]
        PhysicalValueTupleAggregate [keys: b.Name, b.City, b.Country, b.Population, b.Month, b.Money, b.Id, b.NullableValue] [aggs: __CorrelatedScalarSubqueryValue(City)]
          PhysicalSchemaScan [#B.entities() as b]
      PhysicalProject [b.Name as _sq_1_corr_0, b.City as _sq_1_corr_1, b.Country as _sq_1_corr_2, b.Population as _sq_1_corr_3, b.Month as _sq_1_corr_4, b.Money as _sq_1_corr_5, b.Id as _sq_1_corr_6, b.NullableValue as _sq_1_corr_7, b.__CorrelatedScalarSubqueryValue(b.City) as _sq_1_value]
        PhysicalCteRef [bScore as bScore]
  Query
    PhysicalMultiStatement
      PhysicalProject [a.Name as a.Name, a.City as a.City, a.Country as a.Country, a.Population as a.Population, a.Money as a.Money, a.Month as a.Month, a.Id as a.Id, a.NullableValue as a.NullableValue, _sq_1._sq_1_value as _sq_1._sq_1_value]
        PhysicalHashJoin [LeftSingle] [build: _sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2, _sq_1._sq_1_corr_3, _sq_1._sq_1_corr_4, _sq_1._sq_1_corr_5, _sq_1._sq_1_corr_6, _sq_1._sq_1_corr_7] [probe: a.Name, a.City, a.Country, a.Population, a.Month, a.Money, a.Id, a.NullableValue]
          PhysicalSchemaScan [#A.entities() as a]
          PhysicalCteRef [_sq_1 as _sq_1]
      PhysicalSort [a.Name]
        PhysicalProject [a.Name as Name, CASE WHEN ApplyExists[Subquery0](a.Name, a.City, a.Country, a.Population, a.Month, a.Money, a.Id, a.NullableValue; MultiStatement
  Project [b.City as City]
    Filter [((((((((b.Name = outer[0]) AND (b.City = outer[1])) AND (b.Country = outer[2])) AND (b.Population = outer[3])) AND (b.Month = outer[4])) AND (b.Money = outer[5])) AND (b.Id = outer[6])) AND (b.NullableValue = outer[7]))]
      SchemaScan [#B.entities() as b]) THEN 'Y' ELSE 'N' END as ExistsResult, CASE WHEN NOT ApplyExists[Subquery1](a.Name, a.City, a.Country, a.Population, a.Month, a.Money, a.Id, a.NullableValue; MultiStatement
  Project [b.City as City]
    Filter [((((((((b.Name = outer[0]) AND (b.City = outer[1])) AND (b.Country = outer[2])) AND (b.Population = outer[3])) AND (b.Month = outer[4])) AND (b.Money = outer[5])) AND (b.Id = outer[6])) AND (b.NullableValue = outer[7]))]
      SchemaScan [#B.entities() as b]) THEN 'Y' ELSE 'N' END as NotExistsResult, __CorrelatedScalarSubqueryResult(_sq_1._sq_1_value) as Lookup]
          PhysicalCteRef [a_sq_1 as a_sq_1]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [b: BasicEntity]
      Name: string <- property Name
      City: string <- property City
      Country: string <- property Country
      Population: decimal <- property Population
      Money: decimal <- property Money
      Month: string <- property Month
      Id: int <- property Id
      NullableValue: int? <- property NullableValue
    AggregateGroup [Cte0AggregateGroup; keys: 8; typed aggs: 1]
    Generated [Cte0Row0]
      _sq_1_corr_0: string <- field _sq_1_corr_0
      _sq_1_corr_1: string <- field _sq_1_corr_1
      _sq_1_corr_2: string <- field _sq_1_corr_2
      _sq_1_corr_3: decimal <- field _sq_1_corr_3
      _sq_1_corr_4: string <- field _sq_1_corr_4
      _sq_1_corr_5: decimal <- field _sq_1_corr_5
      _sq_1_corr_6: int <- field _sq_1_corr_6
      _sq_1_corr_7: int? <- field _sq_1_corr_7
      _sq_1_value: CorrelatedScalarSubqueryResult<string> <- field _sq_1_value
    SourceEntity [a: BasicEntity]
      Name: string <- property Name
      City: string <- property City
      Country: string <- property Country
      Population: decimal <- property Population
      Money: decimal <- property Money
      Month: string <- property Month
      Id: int <- property Id
      NullableValue: int? <- property NullableValue
    TableRow [_sq_1]
      _sq_1_corr_0: string <- field _sq_1_corr_0
      _sq_1_corr_1: string <- field _sq_1_corr_1
      _sq_1_corr_2: string <- field _sq_1_corr_2
      _sq_1_corr_3: decimal <- field _sq_1_corr_3
      _sq_1_corr_4: string <- field _sq_1_corr_4
      _sq_1_corr_5: decimal <- field _sq_1_corr_5
      _sq_1_corr_6: int <- field _sq_1_corr_6
      _sq_1_corr_7: int? <- field _sq_1_corr_7
      _sq_1_value: CorrelatedScalarSubqueryResult<string> <- field _sq_1_value
    Generated [Statement0Row0]
      a.Name: string <- field a_Name
      a.City: string <- field a_City
      a.Country: string <- field a_Country
      a.Population: decimal <- field a_Population
      a.Money: decimal <- field a_Money
      a.Month: string <- field a_Month
      a.Id: int <- field a_Id
      a.NullableValue: int? <- field a_NullableValue
      _sq_1._sq_1_value: CorrelatedScalarSubqueryResult<string> <- field _sq_1__sq_1_value
    TableRow [a_sq_1]
      a.Name: string <- field a_Name
      a.City: string <- field a_City
      a.Country: string <- field a_Country
      a.Population: decimal <- field a_Population
      a.Money: decimal <- field a_Money
      a.Month: string <- field a_Month
      a.Id: int <- field a_Id
      a.NullableValue: int? <- field a_NullableValue
      _sq_1._sq_1_value: CorrelatedScalarSubqueryResult<string> <- field _sq_1__sq_1_value
    Generated [ResultRow0]
      Name: string <- field Name
      ExistsResult: string <- field ExistsResult
      NotExistsResult: string <- field NotExistsResult
      Lookup: string <- field Lookup

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [From:cte0]
    SourceScan [b: BasicEntity] -> cte0_bRows
    CreateTable [cte0: Cte0Row0]
    PhaseBoundary [GroupBy:cte0]
    CreateValueTupleAggregateContext [cte0Groups: (string, string, string, decimal, string, decimal, int, int?) -> Cte0AggregateGroup]
    ChunkedForEach [b in cte0_bRows]
      GetOrAddValueTupleAggregateGroup [cte0Group = cte0Groups[(b.Name, b.City, b.Country, b.Population, b.Month, b.Money, b.Id, b.NullableValue)] by b.Name, b.City, b.Country, b.Population, b.Month, b.Money, b.Id, b.NullableValue; typed: Cte0AggregateGroup]
      Let [city: string = b.City]
      TypedAggregateSet [Set(cte0Group.__agg0, city)]
    EnsureCapacity [cte0 <- cte0GroupsToFinalize.Count]
    PhaseBoundary [Select:cte0]
    ForEach [cte0FinalGroup in cte0GroupsToFinalize]
      AppendRow [cte0 <- Cte0Row0(_sq_1_corr_0: cte0FinalGroup.b.Name, _sq_1_corr_1: cte0FinalGroup.b.City, _sq_1_corr_2: cte0FinalGroup.b.Country, _sq_1_corr_3: cte0FinalGroup.b.Population, _sq_1_corr_4: cte0FinalGroup.b.Month, _sq_1_corr_5: cte0FinalGroup.b.Money, _sq_1_corr_6: cte0FinalGroup.b.Id, _sq_1_corr_7: cte0FinalGroup.b.NullableValue, _sq_1_value: b.__CorrelatedScalarSubqueryValue(b.City))]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Begin:cte1]
    SourceScan [a: BasicEntity] -> statement0_aRows
    CreateTable [statement0: Statement0Row0]
    InitializeAggregateState [statement0_sq_1HashEmptyState: State<string> via Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel`1<string>.Reset]
    Let [statement0_sq_1HashEmptyValue: CorrelatedScalarSubqueryResult<string> = Get(statement0_sq_1HashEmptyState)]
    CreateHash [statement0_sq_1Hash: ValueTuple<string, string, string, decimal?, string, decimal?, int?, ValueTuple<int?>> -> Row; capacity: _cteRowResults.Slot0.Count]
    ForEach [_sq_1 in _cteRowResults.Slot0]
      HashAdd [statement0_sq_1Hash[(_sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2, _sq_1._sq_1_corr_3, _sq_1._sq_1_corr_4, _sq_1._sq_1_corr_5, _sq_1._sq_1_corr_6, _sq_1._sq_1_corr_7)] += _sq_1]
    ChunkedForEach [a in statement0_aRows]
      HashProbe [statement0_sq_1Hash[(a.Name, a.City, a.Country, a.Population, a.Month, a.Money, a.Id, a.NullableValue)] -> statement0_sq_1HashMatches] [match: statement0_sq_1HashHasMatch]
        ForEach [_sq_1 in statement0_sq_1HashMatches]
          Assign [statement0_sq_1HashHasMatch = TRUE]
          AppendRow [statement0 <- Statement0Row0(a.Name: a.Name, a.City: a.City, a.Country: a.Country, a.Population: a.Population, a.Money: a.Money, a.Month: a.Month, a.Id: a.Id, a.NullableValue: a.NullableValue, _sq_1._sq_1_value: _sq_1._sq_1_value)]
      HashProbeNoMatch
        AppendRow [statement0 <- Statement0Row0(a.Name: a.Name, a.City: a.City, a.Country: a.Country, a.Population: a.Population, a.Money: a.Money, a.Month: a.Month, a.Id: a.Id, a.NullableValue: a.NullableValue, _sq_1._sq_1_value: statement0_sq_1HashEmptyValue)]
    StoreTable [statement0 -> _cteRowResults.Slot1: List<Statement0Row0>]
    PhaseBoundary [End:cte1]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [a_sq_1 in _cteRowResults.Slot1]
      Let [a_Name: string = a_sq_1.a.Name]
      Let [a_City: string = a_sq_1.a.City]
      Let [a_Country: string = a_sq_1.a.Country]
      Let [a_Population: decimal = a_sq_1.a.Population]
      Let [a_Month: string = a_sq_1.a.Month]
      Let [a_Money: decimal = a_sq_1.a.Money]
      Let [a_Id: int = a_sq_1.a.Id]
      Let [a_NullableValue: int? = a_sq_1.a.NullableValue]
      AppendShape [result <- ResultShape0(Name: a_Name, ExistsResult: CASE WHEN ApplyExists[Subquery0](a_sq_1.a.Name, a_sq_1.a.City, a_sq_1.a.Country, a_sq_1.a.Population, a_sq_1.a.Month, a_sq_1.a.Money, a_sq_1.a.Id, a_sq_1.a.NullableValue) THEN 'Y' ELSE 'N' END, NotExistsResult: CASE WHEN NOT ApplyExists[Subquery1](a_sq_1.a.Name, a_sq_1.a.City, a_sq_1.a.Country, a_sq_1.a.Population, a_sq_1.a.Month, a_sq_1.a.Money, a_sq_1.a.Id, a_sq_1.a.NullableValue) THEN 'Y' ELSE 'N' END, Lookup: __CorrelatedScalarSubqueryResult(a_sq_1._sq_1._sq_1_value))]
    SortOwnedBuffer [result by Name ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]

  Subquery
ExecutionPlan [Subquery0]
  Shapes
    SourceEntity [b: BasicEntity]
      Name: string <- property Name
      City: string <- property City
      Country: string <- property Country
      Population: decimal <- property Population
      Money: decimal <- property Money
      Month: string <- property Month
      Id: int <- property Id
      NullableValue: int? <- property NullableValue
    Generated [Subquery0ResultRow0]
      __existsValue: int <- field __existsValue

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [b: BasicEntity] -> bRows
    CreateShapeRows [result: Subquery0ResultRow0 from Subquery0ResultRow0]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [b in bRows]
      If [((((((((b.Name = __outer0) AND (b.City = __outer1)) AND (b.Country = __outer2)) AND (b.Population = __outer3)) AND (b.Month = __outer4)) AND (b.Money = __outer5)) AND (b.Id = __outer6)) AND (b.NullableValue = __outer7))]
        AppendShape [result <- Subquery0ResultRow0(__existsValue: 1)]
    ReturnDeferredTable [result: Subquery0ResultRow0 <- Subquery0ResultRow0]

  Subquery
ExecutionPlan [Subquery1]
  Shapes
    SourceEntity [b: BasicEntity]
      Name: string <- property Name
      City: string <- property City
      Country: string <- property Country
      Population: decimal <- property Population
      Money: decimal <- property Money
      Month: string <- property Month
      Id: int <- property Id
      NullableValue: int? <- property NullableValue
    Generated [Subquery1ResultRow0]
      __existsValue: int <- field __existsValue

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [b: BasicEntity] -> bRows
    CreateShapeRows [result: Subquery1ResultRow0 from Subquery1ResultRow0]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [b in bRows]
      If [((((((((b.Name = __outer0) AND (b.City = __outer1)) AND (b.Country = __outer2)) AND (b.Population = __outer3)) AND (b.Month = __outer4)) AND (b.Money = __outer5)) AND (b.Id = __outer6)) AND (b.NullableValue = __outer7))]
        AppendShape [result <- Subquery1ResultRow0(__existsValue: 1)]
    ReturnDeferredTable [result: Subquery1ResultRow0 <- Subquery1ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q228_PerformanceWideCorrelatedSubquery
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
        private static readonly Column[] __columns_Subquery0_result_1 = new Column[]
        {
            new Column("__existsValue", typeof(int), 0)
        };
        private static readonly Column[] __columns_Subquery1_result_1 = new Column[]
        {
            new Column("__existsValue", typeof(int), 0)
        };
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("_sq_1_corr_0", typeof(string), 0),
            new Column("_sq_1_corr_1", typeof(string), 1),
            new Column("_sq_1_corr_2", typeof(string), 2),
            new Column("_sq_1_corr_3", typeof(decimal), 3),
            new Column("_sq_1_corr_4", typeof(string), 4),
            new Column("_sq_1_corr_5", typeof(decimal), 5),
            new Column("_sq_1_corr_6", typeof(int), 6),
            new Column("_sq_1_corr_7", typeof(int?), 7),
            new Column("_sq_1_value", typeof(Musoq.Plugins.CorrelatedScalarSubqueryResult<string>), 8)
        };
        private static readonly Column[] __columns_compiled_result_3 = new Column[]
        {
            new Column("Name", typeof(string), 0),
            new Column("ExistsResult", typeof(string), 1),
            new Column("NotExistsResult", typeof(string), 2),
            new Column("Lookup", typeof(string), 3)
        };
        private static readonly Column[] __columns_compiled_statement0_2 = new Column[]
        {
            new Column("a.Name", typeof(string), 0),
            new Column("a.City", typeof(string), 1),
            new Column("a.Country", typeof(string), 2),
            new Column("a.Population", typeof(decimal), 3),
            new Column("a.Money", typeof(decimal), 4),
            new Column("a.Month", typeof(string), 5),
            new Column("a.Id", typeof(int), 6),
            new Column("a.NullableValue", typeof(int?), 7),
            new Column("_sq_1._sq_1_value", typeof(Musoq.Plugins.CorrelatedScalarSubqueryResult<string>), 8)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_Subquery0_b_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("City", typeof(string), 1), new Column("Country", typeof(string), 2), new Column("Population", typeof(decimal), 3), new Column("Money", typeof(decimal), 4), new Column("Month", typeof(string), 5), new Column("Id", typeof(int), 6), new Column("NullableValue", typeof(int?), 7) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_Subquery1_b_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("City", typeof(string), 1), new Column("Country", typeof(string), 2), new Column("Population", typeof(decimal), 3), new Column("Money", typeof(decimal), 4), new Column("Month", typeof(string), 5), new Column("Id", typeof(int), 6), new Column("NullableValue", typeof(int?), 7) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("City", typeof(string), 1), new Column("Country", typeof(string), 2), new Column("Population", typeof(decimal), 3), new Column("Money", typeof(decimal), 4), new Column("Month", typeof(string), 5), new Column("Id", typeof(int), 6), new Column("NullableValue", typeof(int?), 7) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Name, __musoqShapeRow.ExistsResult, __musoqShapeRow.NotExistsResult, __musoqShapeRow.Lookup);
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
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var result = new List<ResultShape0>();
                var __storedTable1Rows = _cteRowResults.Slot1;
                for (int __storedTable1Index = 0; __storedTable1Index < __storedTable1Rows.Count; ++__storedTable1Index)
                {
                    if ((__storedTable1Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 a_sq_1 = __storedTable1Rows[__storedTable1Index];
                    string a_Name = a_sq_1.a_Name;
                    string a_City = a_sq_1.a_City;
                    string a_Country = a_sq_1.a_Country;
                    decimal a_Population = a_sq_1.a_Population;
                    string a_Month = a_sq_1.a_Month;
                    decimal a_Money = a_sq_1.a_Money;
                    int a_Id = a_sq_1.a_Id;
                    int? a_NullableValue = a_sq_1.a_NullableValue;
                    result.Add(new ResultShape0(a_Name, global::Musoq.Evaluator.Runtime.SubqueryExecution.Exists<Subquery0ResultRow0>(__ExecuteSubquery0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, a_sq_1.a_Name, a_sq_1.a_City, a_sq_1.a_Country, a_sq_1.a_Population, a_sq_1.a_Month, a_sq_1.a_Money, a_sq_1.a_Id, a_sq_1.a_NullableValue), token) ? (string)"Y" : (string)"N", (!global::Musoq.Evaluator.Runtime.SubqueryExecution.Exists<Subquery1ResultRow0>(__ExecuteSubquery1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, a_sq_1.a_Name, a_sq_1.a_City, a_sq_1.a_Country, a_sq_1.a_Population, a_sq_1.a_Month, a_sq_1.a_Money, a_sq_1.a_Id, a_sq_1.a_NullableValue), token)) ? (string)"Y" : (string)"N", (string)Musoq.Plugins.CorrelatedScalarSubqueryResultExtractor.GetValue(a_sq_1._sq_1__sq_1_value)) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.Name, right.Name);
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
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                var __cte0_bSchema = provider.GetSchema("#B");
                var cte0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__cte0_bSchema, "entities", new SourceExecutionContext("b:4", sourceExecutionPlans["b:4"], token, __schemaColumns_compiled_b_0, sourceRuntimeSettingsBySourceContextId["b:4"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var cte0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0_bRowsSource.Chunks, __musoqProgressContext, "b:4") : cte0_bRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                var cte0GroupsToFinalize = new List<Cte0AggregateGroup>();
                var cte0Groups = new Dictionary<(string, string, string, decimal, string, decimal, int, int?), Cte0AggregateGroup>();
                foreach (var bChunk in cte0_bRows)
                {
                    if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> bChunkView)
                    {
                        if (bChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] bChunkViewArray)
                        {
                            int bChunkViewOffset = bChunkView.Offset;
                            for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                            {
                                if ((bIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var b = bChunkViewArray[bChunkViewOffset + bIndex];
                                string groupKey0 = b.Name;
                                string groupKey1 = b.City;
                                string groupKey2 = b.Country;
                                decimal groupKey3 = b.Population;
                                string groupKey4 = b.Month;
                                decimal groupKey5 = b.Money;
                                int groupKey6 = b.Id;
                                int? groupKey7 = b.NullableValue;
                                ref var cte0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Groups, (groupKey0, groupKey1, groupKey2, groupKey3, groupKey4, groupKey5, groupKey6, groupKey7), out var cte0GroupExists);
                                if (!cte0GroupExists)
                                {
                                    cte0GroupRef = new Cte0AggregateGroup(groupKey0, groupKey1, groupKey2, groupKey3, groupKey4, groupKey5, groupKey6, groupKey7);
                                    cte0GroupsToFinalize.Add(cte0GroupRef);
                                }

                                Cte0AggregateGroup cte0Group = cte0GroupRef;
                                string city = b.City;
                                Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.Set(ref cte0Group.__agg0, (string)city);
                            }

                            continue;
                        }

                        if (bChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> bChunkViewList)
                        {
                            int bChunkViewOffset = bChunkView.Offset;
                            for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                            {
                                if ((bIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var b = bChunkViewList[bChunkViewOffset + bIndex];
                                string groupKey0 = b.Name;
                                string groupKey1 = b.City;
                                string groupKey2 = b.Country;
                                decimal groupKey3 = b.Population;
                                string groupKey4 = b.Month;
                                decimal groupKey5 = b.Money;
                                int groupKey6 = b.Id;
                                int? groupKey7 = b.NullableValue;
                                ref var cte0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Groups, (groupKey0, groupKey1, groupKey2, groupKey3, groupKey4, groupKey5, groupKey6, groupKey7), out var cte0GroupExists);
                                if (!cte0GroupExists)
                                {
                                    cte0GroupRef = new Cte0AggregateGroup(groupKey0, groupKey1, groupKey2, groupKey3, groupKey4, groupKey5, groupKey6, groupKey7);
                                    cte0GroupsToFinalize.Add(cte0GroupRef);
                                }

                                Cte0AggregateGroup cte0Group = cte0GroupRef;
                                string city = b.City;
                                Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.Set(ref cte0Group.__agg0, (string)city);
                            }

                            continue;
                        }
                    }

                    for (int bIndex = 0, bIndexCount = bChunk.Count; bIndex < bIndexCount; ++bIndex)
                    {
                        if ((bIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var b = bChunk[bIndex];
                        string groupKey0 = b.Name;
                        string groupKey1 = b.City;
                        string groupKey2 = b.Country;
                        decimal groupKey3 = b.Population;
                        string groupKey4 = b.Month;
                        decimal groupKey5 = b.Money;
                        int groupKey6 = b.Id;
                        int? groupKey7 = b.NullableValue;
                        ref var cte0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Groups, (groupKey0, groupKey1, groupKey2, groupKey3, groupKey4, groupKey5, groupKey6, groupKey7), out var cte0GroupExists);
                        if (!cte0GroupExists)
                        {
                            cte0GroupRef = new Cte0AggregateGroup(groupKey0, groupKey1, groupKey2, groupKey3, groupKey4, groupKey5, groupKey6, groupKey7);
                            cte0GroupsToFinalize.Add(cte0GroupRef);
                        }

                        Cte0AggregateGroup cte0Group = cte0GroupRef;
                        string city = b.City;
                        Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.Set(ref cte0Group.__agg0, (string)city);
                    }
                }

                cte0.EnsureCapacity(cte0GroupsToFinalize.Count);
                foreach (var cte0FinalGroup in cte0GroupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    cte0.Add(new Cte0Row0(cte0FinalGroup.__key0, cte0FinalGroup.__key1, cte0FinalGroup.__key2, cte0FinalGroup.__key3, cte0FinalGroup.__key4, cte0FinalGroup.__key5, cte0FinalGroup.__key6, cte0FinalGroup.__key7, Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.Get(in cte0FinalGroup.__agg0)));
                }

                return cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement0Row0> BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                var __statement0_aSchema = provider.GetSchema("#A");
                var statement0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__statement0_aSchema, "entities", new SourceExecutionContext("a:4", sourceExecutionPlans["a:4"], token, __schemaColumns_compiled_b_0, sourceRuntimeSettingsBySourceContextId["a:4"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var statement0_aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:4") : statement0_aRowsSource.Chunks;
                var statement0 = new List<Statement0Row0>();
                Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.State statement0_sq_1HashEmptyState = default(Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.State)!;
                Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.Reset(ref statement0_sq_1HashEmptyState);
                Musoq.Plugins.CorrelatedScalarSubqueryResult<string> statement0_sq_1HashEmptyValue = (Musoq.Plugins.CorrelatedScalarSubqueryResult<string>)Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.Get(statement0_sq_1HashEmptyState);
                var statement0_sq_1Hash = new Dictionary<ValueTuple<string, string, string, decimal?, string, decimal?, int?, ValueTuple<int?>>, HashJoinBucket<Cte0Row0>>(_cteRowResults.Slot0.Count);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 _sq_1 = __storedTable0Rows[__storedTable0Index];
                    var key0 = _sq_1._sq_1_corr_0;
                    var key1 = _sq_1._sq_1_corr_1;
                    var key2 = _sq_1._sq_1_corr_2;
                    var key3 = _sq_1._sq_1_corr_3;
                    var key4 = _sq_1._sq_1_corr_4;
                    var key5 = _sq_1._sq_1_corr_5;
                    var key6 = _sq_1._sq_1_corr_6;
                    var key7 = _sq_1._sq_1_corr_7;
                    if (key0 == null || key1 == null || key2 == null || key3 == null || key4 == null || key5 == null || key6 == null || key7 == null)
                        continue;
                    var key = (key0, key1, key2, key3, key4, key5, key6, key7);
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0_sq_1Hash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<Cte0Row0>(_sq_1);
                        }
                        else
                        {
                            matches.Add(_sq_1);
                        }
                    }
                }

                foreach (var aChunk in statement0_aRows)
                {
                    if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> aChunkView)
                    {
                        if (aChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] aChunkViewArray)
                        {
                            int aChunkViewOffset = aChunkView.Offset;
                            for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                            {
                                if ((aIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var a = aChunkViewArray[aChunkViewOffset + aIndex];
                                bool statement0_sq_1HashHasMatch = false;
                                var key0 = a.Name;
                                var key1 = a.City;
                                var key2 = a.Country;
                                var key3 = a.Population;
                                var key4 = a.Month;
                                var key5 = a.Money;
                                var key6 = a.Id;
                                var key7 = a.NullableValue;
                                var key = (key0, key1, key2, key3, key4, key5, key6, key7);
                                if (key0 != null && key1 != null && key2 != null && key4 != null && key7 != null && statement0_sq_1Hash.TryGetValue(key, out var statement0_sq_1HashMatches))
                                {
                                    foreach (var _sq_1 in statement0_sq_1HashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        statement0_sq_1HashHasMatch = true;
                                        statement0.Add(new Statement0Row0(a.Name, a.City, a.Country, a.Population, a.Money, a.Month, a.Id, a.NullableValue, _sq_1._sq_1_value));
                                    }
                                }

                                if (!statement0_sq_1HashHasMatch)
                                {
                                    statement0.Add(new Statement0Row0(a.Name, a.City, a.Country, a.Population, a.Money, a.Month, a.Id, a.NullableValue, statement0_sq_1HashEmptyValue));
                                }
                            }

                            continue;
                        }

                        if (aChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> aChunkViewList)
                        {
                            int aChunkViewOffset = aChunkView.Offset;
                            for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                            {
                                if ((aIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var a = aChunkViewList[aChunkViewOffset + aIndex];
                                bool statement0_sq_1HashHasMatch = false;
                                var key0 = a.Name;
                                var key1 = a.City;
                                var key2 = a.Country;
                                var key3 = a.Population;
                                var key4 = a.Month;
                                var key5 = a.Money;
                                var key6 = a.Id;
                                var key7 = a.NullableValue;
                                var key = (key0, key1, key2, key3, key4, key5, key6, key7);
                                if (key0 != null && key1 != null && key2 != null && key4 != null && key7 != null && statement0_sq_1Hash.TryGetValue(key, out var statement0_sq_1HashMatches))
                                {
                                    foreach (var _sq_1 in statement0_sq_1HashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        statement0_sq_1HashHasMatch = true;
                                        statement0.Add(new Statement0Row0(a.Name, a.City, a.Country, a.Population, a.Money, a.Month, a.Id, a.NullableValue, _sq_1._sq_1_value));
                                    }
                                }

                                if (!statement0_sq_1HashHasMatch)
                                {
                                    statement0.Add(new Statement0Row0(a.Name, a.City, a.Country, a.Population, a.Money, a.Month, a.Id, a.NullableValue, statement0_sq_1HashEmptyValue));
                                }
                            }

                            continue;
                        }
                    }

                    for (int aIndex = 0, aIndexCount = aChunk.Count; aIndex < aIndexCount; ++aIndex)
                    {
                        if ((aIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var a = aChunk[aIndex];
                        bool statement0_sq_1HashHasMatch = false;
                        var key0 = a.Name;
                        var key1 = a.City;
                        var key2 = a.Country;
                        var key3 = a.Population;
                        var key4 = a.Month;
                        var key5 = a.Money;
                        var key6 = a.Id;
                        var key7 = a.NullableValue;
                        var key = (key0, key1, key2, key3, key4, key5, key6, key7);
                        if (key0 != null && key1 != null && key2 != null && key4 != null && key7 != null && statement0_sq_1Hash.TryGetValue(key, out var statement0_sq_1HashMatches))
                        {
                            foreach (var _sq_1 in statement0_sq_1HashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                statement0_sq_1HashHasMatch = true;
                                statement0.Add(new Statement0Row0(a.Name, a.City, a.Country, a.Population, a.Money, a.Month, a.Id, a.NullableValue, _sq_1._sq_1_value));
                            }
                        }

                        if (!statement0_sq_1HashHasMatch)
                        {
                            statement0.Add(new Statement0Row0(a.Name, a.City, a.Country, a.Population, a.Money, a.Month, a.Id, a.NullableValue, statement0_sq_1HashEmptyValue));
                        }
                    }
                }

                return statement0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }
        }

        private static IEnumerable<Subquery0ResultRow0> __ExecuteSubquery0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, string __outer0, string __outer1, string __outer2, decimal __outer3, string __outer4, decimal __outer5, int __outer6, int? __outer7)
        {
            try
            {
                OnPhaseChanged("Subquery0", QueryPhase.Begin);
                OnPhaseChanged("Subquery0", QueryPhase.From);
                var __bSchema = provider.GetSchema("#B");
                var bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__bSchema, "entities", new SourceExecutionContext("b:2", sourceExecutionPlans["b:2"], token, __schemaColumns_Subquery0_b_0, sourceRuntimeSettingsBySourceContextId["b:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(bRowsSource.Chunks, __musoqProgressContext, "b:2") : bRowsSource.Chunks;
                OnPhaseChanged("Subquery0", QueryPhase.Where);
                OnPhaseChanged("Subquery0", QueryPhase.Select);
                foreach (var bChunk in bRows)
                {
                    if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> bChunkView)
                    {
                        if (bChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] bChunkViewArray)
                        {
                            int bChunkViewOffset = bChunkView.Offset;
                            for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                            {
                                if ((bIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var b = bChunkViewArray[bChunkViewOffset + bIndex];
                                if (((((bool?)((((bool?)((((bool?)((((bool?)((((bool?)((((bool?)((((bool?)(Operators.SqlCompare<string, string>(b.Name, __outer0, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<string, string>(b.City, __outer1, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(b.City, __outer1, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<string, string>(b.Country, __outer2, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(b.Country, __outer2, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)(b.Population == __outer3),
                                    _ => (bool?)(b.Population == __outer3) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<string, string>(b.Month, __outer4, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(b.Month, __outer4, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)(b.Money == __outer5),
                                    _ => (bool?)(b.Money == __outer5) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)(b.Id == __outer6),
                                    _ => (bool?)(b.Id == __outer6) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<int?, int?>(b.NullableValue, __outer7, (int? __sqlLeft, int? __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<int?, int?>(b.NullableValue, __outer7, (int? __sqlLeft, int? __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                                })) == true)
                                {
                                    yield return new Subquery0ResultRow0(1);
                                }
                            }

                            continue;
                        }

                        if (bChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> bChunkViewList)
                        {
                            int bChunkViewOffset = bChunkView.Offset;
                            for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                            {
                                if ((bIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var b = bChunkViewList[bChunkViewOffset + bIndex];
                                if (((((bool?)((((bool?)((((bool?)((((bool?)((((bool?)((((bool?)((((bool?)(Operators.SqlCompare<string, string>(b.Name, __outer0, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<string, string>(b.City, __outer1, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(b.City, __outer1, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<string, string>(b.Country, __outer2, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(b.Country, __outer2, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)(b.Population == __outer3),
                                    _ => (bool?)(b.Population == __outer3) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<string, string>(b.Month, __outer4, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(b.Month, __outer4, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)(b.Money == __outer5),
                                    _ => (bool?)(b.Money == __outer5) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)(b.Id == __outer6),
                                    _ => (bool?)(b.Id == __outer6) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<int?, int?>(b.NullableValue, __outer7, (int? __sqlLeft, int? __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<int?, int?>(b.NullableValue, __outer7, (int? __sqlLeft, int? __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                                })) == true)
                                {
                                    yield return new Subquery0ResultRow0(1);
                                }
                            }

                            continue;
                        }
                    }

                    for (int bIndex = 0, bIndexCount = bChunk.Count; bIndex < bIndexCount; ++bIndex)
                    {
                        if ((bIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var b = bChunk[bIndex];
                        if (((((bool?)((((bool?)((((bool?)((((bool?)((((bool?)((((bool?)((((bool?)(Operators.SqlCompare<string, string>(b.Name, __outer0, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)Operators.SqlCompare<string, string>(b.City, __outer1, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                            _ => (bool?)Operators.SqlCompare<string, string>(b.City, __outer1, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                        }))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)Operators.SqlCompare<string, string>(b.Country, __outer2, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                            _ => (bool?)Operators.SqlCompare<string, string>(b.Country, __outer2, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                        }))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)(b.Population == __outer3),
                            _ => (bool?)(b.Population == __outer3) & (bool?)null
                        }))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)Operators.SqlCompare<string, string>(b.Month, __outer4, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                            _ => (bool?)Operators.SqlCompare<string, string>(b.Month, __outer4, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                        }))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)(b.Money == __outer5),
                            _ => (bool?)(b.Money == __outer5) & (bool?)null
                        }))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)(b.Id == __outer6),
                            _ => (bool?)(b.Id == __outer6) & (bool?)null
                        }))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)Operators.SqlCompare<int?, int?>(b.NullableValue, __outer7, (int? __sqlLeft, int? __sqlRight) => (__sqlLeft == __sqlRight)),
                            _ => (bool?)Operators.SqlCompare<int?, int?>(b.NullableValue, __outer7, (int? __sqlLeft, int? __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                        })) == true)
                        {
                            yield return new Subquery0ResultRow0(1);
                        }
                    }
                }
            }
            finally
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("Subquery0", QueryPhase.End);
                }
            }
        }

        private static IEnumerable<Subquery1ResultRow0> __ExecuteSubquery1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, string __outer0, string __outer1, string __outer2, decimal __outer3, string __outer4, decimal __outer5, int __outer6, int? __outer7)
        {
            try
            {
                OnPhaseChanged("Subquery1", QueryPhase.Begin);
                OnPhaseChanged("Subquery1", QueryPhase.From);
                var __bSchema = provider.GetSchema("#B");
                var bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__bSchema, "entities", new SourceExecutionContext("b:3", sourceExecutionPlans["b:3"], token, __schemaColumns_Subquery1_b_0, sourceRuntimeSettingsBySourceContextId["b:3"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(bRowsSource.Chunks, __musoqProgressContext, "b:3") : bRowsSource.Chunks;
                OnPhaseChanged("Subquery1", QueryPhase.Where);
                OnPhaseChanged("Subquery1", QueryPhase.Select);
                foreach (var bChunk in bRows)
                {
                    if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> bChunkView)
                    {
                        if (bChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] bChunkViewArray)
                        {
                            int bChunkViewOffset = bChunkView.Offset;
                            for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                            {
                                if ((bIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var b = bChunkViewArray[bChunkViewOffset + bIndex];
                                if (((((bool?)((((bool?)((((bool?)((((bool?)((((bool?)((((bool?)((((bool?)(Operators.SqlCompare<string, string>(b.Name, __outer0, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<string, string>(b.City, __outer1, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(b.City, __outer1, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<string, string>(b.Country, __outer2, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(b.Country, __outer2, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)(b.Population == __outer3),
                                    _ => (bool?)(b.Population == __outer3) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<string, string>(b.Month, __outer4, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(b.Month, __outer4, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)(b.Money == __outer5),
                                    _ => (bool?)(b.Money == __outer5) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)(b.Id == __outer6),
                                    _ => (bool?)(b.Id == __outer6) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<int?, int?>(b.NullableValue, __outer7, (int? __sqlLeft, int? __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<int?, int?>(b.NullableValue, __outer7, (int? __sqlLeft, int? __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                                })) == true)
                                {
                                    yield return new Subquery1ResultRow0(1);
                                }
                            }

                            continue;
                        }

                        if (bChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> bChunkViewList)
                        {
                            int bChunkViewOffset = bChunkView.Offset;
                            for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                            {
                                if ((bIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var b = bChunkViewList[bChunkViewOffset + bIndex];
                                if (((((bool?)((((bool?)((((bool?)((((bool?)((((bool?)((((bool?)((((bool?)(Operators.SqlCompare<string, string>(b.Name, __outer0, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<string, string>(b.City, __outer1, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(b.City, __outer1, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<string, string>(b.Country, __outer2, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(b.Country, __outer2, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)(b.Population == __outer3),
                                    _ => (bool?)(b.Population == __outer3) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<string, string>(b.Month, __outer4, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<string, string>(b.Month, __outer4, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)(b.Money == __outer5),
                                    _ => (bool?)(b.Money == __outer5) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)(b.Id == __outer6),
                                    _ => (bool?)(b.Id == __outer6) & (bool?)null
                                }))) switch
                                {
                                    false => (bool?)false,
                                    true => (bool?)Operators.SqlCompare<int?, int?>(b.NullableValue, __outer7, (int? __sqlLeft, int? __sqlRight) => (__sqlLeft == __sqlRight)),
                                    _ => (bool?)Operators.SqlCompare<int?, int?>(b.NullableValue, __outer7, (int? __sqlLeft, int? __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                                })) == true)
                                {
                                    yield return new Subquery1ResultRow0(1);
                                }
                            }

                            continue;
                        }
                    }

                    for (int bIndex = 0, bIndexCount = bChunk.Count; bIndex < bIndexCount; ++bIndex)
                    {
                        if ((bIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var b = bChunk[bIndex];
                        if (((((bool?)((((bool?)((((bool?)((((bool?)((((bool?)((((bool?)((((bool?)(Operators.SqlCompare<string, string>(b.Name, __outer0, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)Operators.SqlCompare<string, string>(b.City, __outer1, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                            _ => (bool?)Operators.SqlCompare<string, string>(b.City, __outer1, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                        }))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)Operators.SqlCompare<string, string>(b.Country, __outer2, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                            _ => (bool?)Operators.SqlCompare<string, string>(b.Country, __outer2, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                        }))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)(b.Population == __outer3),
                            _ => (bool?)(b.Population == __outer3) & (bool?)null
                        }))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)Operators.SqlCompare<string, string>(b.Month, __outer4, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)),
                            _ => (bool?)Operators.SqlCompare<string, string>(b.Month, __outer4, (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                        }))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)(b.Money == __outer5),
                            _ => (bool?)(b.Money == __outer5) & (bool?)null
                        }))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)(b.Id == __outer6),
                            _ => (bool?)(b.Id == __outer6) & (bool?)null
                        }))) switch
                        {
                            false => (bool?)false,
                            true => (bool?)Operators.SqlCompare<int?, int?>(b.NullableValue, __outer7, (int? __sqlLeft, int? __sqlRight) => (__sqlLeft == __sqlRight)),
                            _ => (bool?)Operators.SqlCompare<int?, int?>(b.NullableValue, __outer7, (int? __sqlLeft, int? __sqlRight) => (__sqlLeft == __sqlRight)) & (bool?)null
                        })) == true)
                        {
                            yield return new Subquery1ResultRow0(1);
                        }
                    }
                }
            }
            finally
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("Subquery1", QueryPhase.End);
                }
            }
        }

        private sealed class Cte0AggregateGroup
        {
            public Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.State __agg0;
            public readonly string __key0;
            public readonly string __key1;
            public readonly string __key2;
            public readonly decimal __key3;
            public readonly string __key4;
            public readonly decimal __key5;
            public readonly int __key6;
            public readonly int? __key7;
            public Cte0AggregateGroup(string __key0, string __key1, string __key2, decimal __key3, string __key4, decimal __key5, int __key6, int? __key7)
            {
                this.__key0 = __key0;
                this.__key1 = __key1;
                this.__key2 = __key2;
                this.__key3 = __key3;
                this.__key4 = __key4;
                this.__key5 = __key5;
                this.__key6 = __key6;
                this.__key7 = __key7;
                Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.Reset(ref this.__agg0);
            }

            public void MergeFrom(Cte0AggregateGroup source)
            {
                Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.Merge(ref this.__agg0, in source.__agg0);
            }
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(string __value0, string __value1, string __value2, decimal __value3, string __value4, decimal __value5, int __value6, int? __value7, Musoq.Plugins.CorrelatedScalarSubqueryResult<string> __value8)
            {
                _sq_1_corr_0 = __value0;
                _sq_1_corr_1 = __value1;
                _sq_1_corr_2 = __value2;
                _sq_1_corr_3 = __value3;
                _sq_1_corr_4 = __value4;
                _sq_1_corr_5 = __value5;
                _sq_1_corr_6 = __value6;
                _sq_1_corr_7 = __value7;
                _sq_1_value = __value8;
            }

            public string _sq_1_corr_0 { get; }
            public string _sq_1_corr_1 { get; }
            public string _sq_1_corr_2 { get; }
            public decimal _sq_1_corr_3 { get; }
            public string _sq_1_corr_4 { get; }
            public decimal _sq_1_corr_5 { get; }
            public int _sq_1_corr_6 { get; }
            public int? _sq_1_corr_7 { get; }
            public Musoq.Plugins.CorrelatedScalarSubqueryResult<string> _sq_1_value { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
            public List<Statement0Row0> Slot1;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1, string __value2, string __value3)
            {
                Name = __value0;
                ExistsResult = __value1;
                NotExistsResult = __value2;
                Lookup = __value3;
            }

            public override int Count => 4;
            public string ExistsResult { get; private set; }
            public string Lookup { get; private set; }
            public string Name { get; private set; }
            public string NotExistsResult { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.ExistsResult = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.NotExistsResult = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Lookup = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Name),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.ExistsResult),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.NotExistsResult),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Lookup),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Name" => true,
                "ExistsResult" => true,
                "NotExistsResult" => true,
                "Lookup" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Name, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.ExistsResult, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.NotExistsResult, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Lookup, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Name, string ExistsResult, string NotExistsResult, string Lookup)
            {
                this.Name = Name;
                this.ExistsResult = ExistsResult;
                this.NotExistsResult = NotExistsResult;
                this.Lookup = Lookup;
            }

            public string ExistsResult { get; }
            public string Lookup { get; }
            public string Name { get; }
            public string NotExistsResult { get; }
            public int __musoqSequence { get; init; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(string __value0, string __value1, string __value2, decimal __value3, decimal __value4, string __value5, int __value6, int? __value7, Musoq.Plugins.CorrelatedScalarSubqueryResult<string> __value8)
            {
                a_Name = __value0;
                a_City = __value1;
                a_Country = __value2;
                a_Population = __value3;
                a_Money = __value4;
                a_Month = __value5;
                a_Id = __value6;
                a_NullableValue = __value7;
                _sq_1__sq_1_value = __value8;
            }

            public Musoq.Plugins.CorrelatedScalarSubqueryResult<string> _sq_1__sq_1_value { get; }
            public string a_City { get; }
            public string a_Country { get; }
            public int a_Id { get; }
            public decimal a_Money { get; }
            public string a_Month { get; }
            public string a_Name { get; }
            public int? a_NullableValue { get; }
            public decimal a_Population { get; }
        }

        private sealed class Subquery0ResultRow0 : Row
        {
            public Subquery0ResultRow0(int __value0)
            {
                __existsValue = __value0;
            }

            public override int Count => 1;
            public int __existsValue { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.__existsValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.__existsValue),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "__existsValue" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.__existsValue, ref visitor);
            }
        }

        private sealed class Subquery1ResultRow0 : Row
        {
            public Subquery1ResultRow0(int __value0)
            {
                __existsValue = __value0;
            }

            public override int Count => 1;
            public int __existsValue { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.__existsValue = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.__existsValue),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "__existsValue" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.__existsValue, ref visitor);
            }
        }
    }
}
