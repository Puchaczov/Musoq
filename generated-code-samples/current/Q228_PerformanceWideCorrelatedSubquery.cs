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
      Project [1 as _sq_1_key, b.Name as _sq_1_corr_0, b.City as _sq_1_corr_1, b.Country as _sq_1_corr_2, b.Population as _sq_1_corr_3, b.Month as _sq_1_corr_4, b.Money as _sq_1_corr_5, b.Id as _sq_1_corr_6, b.NullableValue as _sq_1_corr_7]
        SchemaScan [#B.entities() as b]
  Definition [_sq_2]
    MultiStatement
      Project [1 as _sq_2_key, b.Name as _sq_2_corr_0, b.City as _sq_2_corr_1, b.Country as _sq_2_corr_2, b.Population as _sq_2_corr_3, b.Month as _sq_2_corr_4, b.Money as _sq_2_corr_5, b.Id as _sq_2_corr_6, b.NullableValue as _sq_2_corr_7]
        SchemaScan [#B.entities() as b]
  Definition [_sq_3]
    MultiStatement
      Project [b.NullableValue as b.NullableValue, b.Id as b.Id, b.Money as b.Money, b.Month as b.Month, b.Population as b.Population, b.Country as b.Country, b.City as b.City, b.Name as b.Name, AggRef(b.__CorrelatedScalarSubqueryValue(b.City)) as b.__CorrelatedScalarSubqueryValue(b.City)]
        Aggregate [keys: b.Name, b.City, b.Country, b.Population, b.Month, b.Money, b.Id, b.NullableValue] [aggs: __CorrelatedScalarSubqueryValue(City)]
          SchemaScan [#B.entities() as b]
      Project [b.Name as _sq_3_corr_0, b.City as _sq_3_corr_1, b.Country as _sq_3_corr_2, b.Population as _sq_3_corr_3, b.Month as _sq_3_corr_4, b.Money as _sq_3_corr_5, b.Id as _sq_3_corr_6, b.NullableValue as _sq_3_corr_7, b.__CorrelatedScalarSubqueryValue(b.City) as _sq_3_value]
        CteRef [bScore as bScore]
  Query
    MultiStatement
      Project [a.Name as a.Name, a.City as a.City, a.Country as a.Country, a.Population as a.Population, a.Money as a.Money, a.Month as a.Month, a.Id as a.Id, a.NullableValue as a.NullableValue, _sq_1._sq_1_key as _sq_1._sq_1_key, _sq_1._sq_1_corr_0 as _sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1 as _sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2 as _sq_1._sq_1_corr_2, _sq_1._sq_1_corr_3 as _sq_1._sq_1_corr_3, _sq_1._sq_1_corr_4 as _sq_1._sq_1_corr_4, _sq_1._sq_1_corr_5 as _sq_1._sq_1_corr_5, _sq_1._sq_1_corr_6 as _sq_1._sq_1_corr_6, _sq_1._sq_1_corr_7 as _sq_1._sq_1_corr_7]
        Join [LeftMark] [((1 = _sq_1._sq_1_key) AND ((((((((_sq_1._sq_1_corr_0 = a.Name) AND (_sq_1._sq_1_corr_1 = a.City)) AND (_sq_1._sq_1_corr_2 = a.Country)) AND (_sq_1._sq_1_corr_3 = a.Population)) AND (_sq_1._sq_1_corr_4 = a.Month)) AND (_sq_1._sq_1_corr_5 = a.Money)) AND (_sq_1._sq_1_corr_6 = a.Id)) AND (_sq_1._sq_1_corr_7 = a.NullableValue)))]
          SchemaScan [#A.entities() as a]
          CteRef [_sq_1 as _sq_1]
      Project [a_sq_1.a.Name as a.Name, a_sq_1.a.City as a.City, a_sq_1.a.Country as a.Country, a_sq_1.a.Population as a.Population, a_sq_1.a.Money as a.Money, a_sq_1.a.Month as a.Month, a_sq_1.a.Id as a.Id, a_sq_1.a.NullableValue as a.NullableValue, a_sq_1._sq_1._sq_1_key as _sq_1._sq_1_key, a_sq_1._sq_1._sq_1_corr_0 as _sq_1._sq_1_corr_0, a_sq_1._sq_1._sq_1_corr_1 as _sq_1._sq_1_corr_1, a_sq_1._sq_1._sq_1_corr_2 as _sq_1._sq_1_corr_2, a_sq_1._sq_1._sq_1_corr_3 as _sq_1._sq_1_corr_3, a_sq_1._sq_1._sq_1_corr_4 as _sq_1._sq_1_corr_4, a_sq_1._sq_1._sq_1_corr_5 as _sq_1._sq_1_corr_5, a_sq_1._sq_1._sq_1_corr_6 as _sq_1._sq_1_corr_6, a_sq_1._sq_1._sq_1_corr_7 as _sq_1._sq_1_corr_7, _sq_2._sq_2_key as _sq_2._sq_2_key, _sq_2._sq_2_corr_0 as _sq_2._sq_2_corr_0, _sq_2._sq_2_corr_1 as _sq_2._sq_2_corr_1, _sq_2._sq_2_corr_2 as _sq_2._sq_2_corr_2, _sq_2._sq_2_corr_3 as _sq_2._sq_2_corr_3, _sq_2._sq_2_corr_4 as _sq_2._sq_2_corr_4, _sq_2._sq_2_corr_5 as _sq_2._sq_2_corr_5, _sq_2._sq_2_corr_6 as _sq_2._sq_2_corr_6, _sq_2._sq_2_corr_7 as _sq_2._sq_2_corr_7]
        Join [LeftMark] [((1 = _sq_2._sq_2_key) AND ((((((((_sq_2._sq_2_corr_0 = a_sq_1.a.Name) AND (_sq_2._sq_2_corr_1 = a_sq_1.a.City)) AND (_sq_2._sq_2_corr_2 = a_sq_1.a.Country)) AND (_sq_2._sq_2_corr_3 = a_sq_1.a.Population)) AND (_sq_2._sq_2_corr_4 = a_sq_1.a.Month)) AND (_sq_2._sq_2_corr_5 = a_sq_1.a.Money)) AND (_sq_2._sq_2_corr_6 = a_sq_1.a.Id)) AND (_sq_2._sq_2_corr_7 = a_sq_1.a.NullableValue)))]
          CteRef [a_sq_1 as a_sq_1]
          CteRef [_sq_2 as _sq_2]
      Project [a_sq_1_sq_2.a.Name as a.Name, a_sq_1_sq_2.a.City as a.City, a_sq_1_sq_2.a.Country as a.Country, a_sq_1_sq_2.a.Population as a.Population, a_sq_1_sq_2.a.Money as a.Money, a_sq_1_sq_2.a.Month as a.Month, a_sq_1_sq_2.a.Id as a.Id, a_sq_1_sq_2.a.NullableValue as a.NullableValue, a_sq_1_sq_2._sq_1._sq_1_key as _sq_1._sq_1_key, a_sq_1_sq_2._sq_1._sq_1_corr_0 as _sq_1._sq_1_corr_0, a_sq_1_sq_2._sq_1._sq_1_corr_1 as _sq_1._sq_1_corr_1, a_sq_1_sq_2._sq_1._sq_1_corr_2 as _sq_1._sq_1_corr_2, a_sq_1_sq_2._sq_1._sq_1_corr_3 as _sq_1._sq_1_corr_3, a_sq_1_sq_2._sq_1._sq_1_corr_4 as _sq_1._sq_1_corr_4, a_sq_1_sq_2._sq_1._sq_1_corr_5 as _sq_1._sq_1_corr_5, a_sq_1_sq_2._sq_1._sq_1_corr_6 as _sq_1._sq_1_corr_6, a_sq_1_sq_2._sq_1._sq_1_corr_7 as _sq_1._sq_1_corr_7, a_sq_1_sq_2._sq_2._sq_2_key as _sq_2._sq_2_key, a_sq_1_sq_2._sq_2._sq_2_corr_0 as _sq_2._sq_2_corr_0, a_sq_1_sq_2._sq_2._sq_2_corr_1 as _sq_2._sq_2_corr_1, a_sq_1_sq_2._sq_2._sq_2_corr_2 as _sq_2._sq_2_corr_2, a_sq_1_sq_2._sq_2._sq_2_corr_3 as _sq_2._sq_2_corr_3, a_sq_1_sq_2._sq_2._sq_2_corr_4 as _sq_2._sq_2_corr_4, a_sq_1_sq_2._sq_2._sq_2_corr_5 as _sq_2._sq_2_corr_5, a_sq_1_sq_2._sq_2._sq_2_corr_6 as _sq_2._sq_2_corr_6, a_sq_1_sq_2._sq_2._sq_2_corr_7 as _sq_2._sq_2_corr_7, _sq_3._sq_3_corr_0 as _sq_3_corr_0, _sq_3._sq_3_corr_1 as _sq_3_corr_1, _sq_3._sq_3_corr_2 as _sq_3_corr_2, _sq_3._sq_3_corr_3 as _sq_3_corr_3, _sq_3._sq_3_corr_4 as _sq_3_corr_4, _sq_3._sq_3_corr_5 as _sq_3_corr_5, _sq_3._sq_3_corr_6 as _sq_3_corr_6, _sq_3._sq_3_corr_7 as _sq_3_corr_7, _sq_3._sq_3_value as _sq_3_value]
        Join [LeftSingle] [((((((((_sq_3._sq_3_corr_0 = a_sq_1_sq_2.a.Name) AND (_sq_3._sq_3_corr_1 = a_sq_1_sq_2.a.City)) AND (_sq_3._sq_3_corr_2 = a_sq_1_sq_2.a.Country)) AND (_sq_3._sq_3_corr_3 = a_sq_1_sq_2.a.Population)) AND (_sq_3._sq_3_corr_4 = a_sq_1_sq_2.a.Month)) AND (_sq_3._sq_3_corr_5 = a_sq_1_sq_2.a.Money)) AND (_sq_3._sq_3_corr_6 = a_sq_1_sq_2.a.Id)) AND (_sq_3._sq_3_corr_7 = a_sq_1_sq_2.a.NullableValue))]
          CteRef [a_sq_1_sq_2 as a_sq_1_sq_2]
          CteRef [_sq_3 as _sq_3]
      Sort [a.Name]
        Project [a.Name as a.Name, CASE WHEN _sq_1._sq_1_key IS NOT NULL THEN 'Y' ELSE 'N' END as ExistsResult, CASE WHEN _sq_2._sq_2_key IS NULL THEN 'Y' ELSE 'N' END as NotExistsResult, __CorrelatedScalarSubqueryResult(_sq_3._sq_3_value) as Lookup]
          CteRef [a_sq_1_sq_2_sq_3 as a_sq_1_sq_2_sq_3]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [_sq_1]
    PhysicalMultiStatement
      PhysicalProject [1 as _sq_1_key, b.Name as _sq_1_corr_0, b.City as _sq_1_corr_1, b.Country as _sq_1_corr_2, b.Population as _sq_1_corr_3, b.Month as _sq_1_corr_4, b.Money as _sq_1_corr_5, b.Id as _sq_1_corr_6, b.NullableValue as _sq_1_corr_7]
        PhysicalSchemaScan [#B.entities() as b]
  Definition [_sq_2]
    PhysicalMultiStatement
      PhysicalProject [1 as _sq_2_key, b.Name as _sq_2_corr_0, b.City as _sq_2_corr_1, b.Country as _sq_2_corr_2, b.Population as _sq_2_corr_3, b.Month as _sq_2_corr_4, b.Money as _sq_2_corr_5, b.Id as _sq_2_corr_6, b.NullableValue as _sq_2_corr_7]
        PhysicalSchemaScan [#B.entities() as b]
  Definition [_sq_3]
    PhysicalMultiStatement
      PhysicalProject [b.NullableValue as b.NullableValue, b.Id as b.Id, b.Money as b.Money, b.Month as b.Month, b.Population as b.Population, b.Country as b.Country, b.City as b.City, b.Name as b.Name, AggRef(b.__CorrelatedScalarSubqueryValue(b.City)) as b.__CorrelatedScalarSubqueryValue(b.City)]
        PhysicalValueTupleAggregate [keys: b.Name, b.City, b.Country, b.Population, b.Month, b.Money, b.Id, b.NullableValue] [aggs: __CorrelatedScalarSubqueryValue(City)]
          PhysicalSchemaScan [#B.entities() as b]
      PhysicalProject [b.Name as _sq_3_corr_0, b.City as _sq_3_corr_1, b.Country as _sq_3_corr_2, b.Population as _sq_3_corr_3, b.Month as _sq_3_corr_4, b.Money as _sq_3_corr_5, b.Id as _sq_3_corr_6, b.NullableValue as _sq_3_corr_7, b.__CorrelatedScalarSubqueryValue(b.City) as _sq_3_value]
        PhysicalCteRef [bScore as bScore]
  Query
    PhysicalMultiStatement
      PhysicalProject [a.Name as a.Name, a.City as a.City, a.Country as a.Country, a.Population as a.Population, a.Money as a.Money, a.Month as a.Month, a.Id as a.Id, a.NullableValue as a.NullableValue, _sq_1._sq_1_key as _sq_1._sq_1_key, _sq_1._sq_1_corr_0 as _sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1 as _sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2 as _sq_1._sq_1_corr_2, _sq_1._sq_1_corr_3 as _sq_1._sq_1_corr_3, _sq_1._sq_1_corr_4 as _sq_1._sq_1_corr_4, _sq_1._sq_1_corr_5 as _sq_1._sq_1_corr_5, _sq_1._sq_1_corr_6 as _sq_1._sq_1_corr_6, _sq_1._sq_1_corr_7 as _sq_1._sq_1_corr_7]
        PhysicalHashJoin [LeftMark] [build: _sq_1._sq_1_key, _sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2, _sq_1._sq_1_corr_3, _sq_1._sq_1_corr_4, _sq_1._sq_1_corr_5, _sq_1._sq_1_corr_6, _sq_1._sq_1_corr_7] [probe: 1, a.Name, a.City, a.Country, a.Population, a.Month, a.Money, a.Id, a.NullableValue]
          PhysicalSchemaScan [#A.entities() as a]
          PhysicalCteRef [_sq_1 as _sq_1]
      PhysicalProject [a_sq_1.a.Name as a.Name, a_sq_1.a.City as a.City, a_sq_1.a.Country as a.Country, a_sq_1.a.Population as a.Population, a_sq_1.a.Money as a.Money, a_sq_1.a.Month as a.Month, a_sq_1.a.Id as a.Id, a_sq_1.a.NullableValue as a.NullableValue, a_sq_1._sq_1._sq_1_key as _sq_1._sq_1_key, a_sq_1._sq_1._sq_1_corr_0 as _sq_1._sq_1_corr_0, a_sq_1._sq_1._sq_1_corr_1 as _sq_1._sq_1_corr_1, a_sq_1._sq_1._sq_1_corr_2 as _sq_1._sq_1_corr_2, a_sq_1._sq_1._sq_1_corr_3 as _sq_1._sq_1_corr_3, a_sq_1._sq_1._sq_1_corr_4 as _sq_1._sq_1_corr_4, a_sq_1._sq_1._sq_1_corr_5 as _sq_1._sq_1_corr_5, a_sq_1._sq_1._sq_1_corr_6 as _sq_1._sq_1_corr_6, a_sq_1._sq_1._sq_1_corr_7 as _sq_1._sq_1_corr_7, _sq_2._sq_2_key as _sq_2._sq_2_key, _sq_2._sq_2_corr_0 as _sq_2._sq_2_corr_0, _sq_2._sq_2_corr_1 as _sq_2._sq_2_corr_1, _sq_2._sq_2_corr_2 as _sq_2._sq_2_corr_2, _sq_2._sq_2_corr_3 as _sq_2._sq_2_corr_3, _sq_2._sq_2_corr_4 as _sq_2._sq_2_corr_4, _sq_2._sq_2_corr_5 as _sq_2._sq_2_corr_5, _sq_2._sq_2_corr_6 as _sq_2._sq_2_corr_6, _sq_2._sq_2_corr_7 as _sq_2._sq_2_corr_7]
        PhysicalHashJoin [LeftMark] [build: _sq_2._sq_2_key, _sq_2._sq_2_corr_0, _sq_2._sq_2_corr_1, _sq_2._sq_2_corr_2, _sq_2._sq_2_corr_3, _sq_2._sq_2_corr_4, _sq_2._sq_2_corr_5, _sq_2._sq_2_corr_6, _sq_2._sq_2_corr_7] [probe: 1, a_sq_1.a.Name, a_sq_1.a.City, a_sq_1.a.Country, a_sq_1.a.Population, a_sq_1.a.Month, a_sq_1.a.Money, a_sq_1.a.Id, a_sq_1.a.NullableValue]
          PhysicalCteRef [a_sq_1 as a_sq_1]
          PhysicalCteRef [_sq_2 as _sq_2]
      PhysicalProject [a_sq_1_sq_2.a.Name as a.Name, a_sq_1_sq_2.a.City as a.City, a_sq_1_sq_2.a.Country as a.Country, a_sq_1_sq_2.a.Population as a.Population, a_sq_1_sq_2.a.Money as a.Money, a_sq_1_sq_2.a.Month as a.Month, a_sq_1_sq_2.a.Id as a.Id, a_sq_1_sq_2.a.NullableValue as a.NullableValue, a_sq_1_sq_2._sq_1._sq_1_key as _sq_1._sq_1_key, a_sq_1_sq_2._sq_1._sq_1_corr_0 as _sq_1._sq_1_corr_0, a_sq_1_sq_2._sq_1._sq_1_corr_1 as _sq_1._sq_1_corr_1, a_sq_1_sq_2._sq_1._sq_1_corr_2 as _sq_1._sq_1_corr_2, a_sq_1_sq_2._sq_1._sq_1_corr_3 as _sq_1._sq_1_corr_3, a_sq_1_sq_2._sq_1._sq_1_corr_4 as _sq_1._sq_1_corr_4, a_sq_1_sq_2._sq_1._sq_1_corr_5 as _sq_1._sq_1_corr_5, a_sq_1_sq_2._sq_1._sq_1_corr_6 as _sq_1._sq_1_corr_6, a_sq_1_sq_2._sq_1._sq_1_corr_7 as _sq_1._sq_1_corr_7, a_sq_1_sq_2._sq_2._sq_2_key as _sq_2._sq_2_key, a_sq_1_sq_2._sq_2._sq_2_corr_0 as _sq_2._sq_2_corr_0, a_sq_1_sq_2._sq_2._sq_2_corr_1 as _sq_2._sq_2_corr_1, a_sq_1_sq_2._sq_2._sq_2_corr_2 as _sq_2._sq_2_corr_2, a_sq_1_sq_2._sq_2._sq_2_corr_3 as _sq_2._sq_2_corr_3, a_sq_1_sq_2._sq_2._sq_2_corr_4 as _sq_2._sq_2_corr_4, a_sq_1_sq_2._sq_2._sq_2_corr_5 as _sq_2._sq_2_corr_5, a_sq_1_sq_2._sq_2._sq_2_corr_6 as _sq_2._sq_2_corr_6, a_sq_1_sq_2._sq_2._sq_2_corr_7 as _sq_2._sq_2_corr_7, _sq_3._sq_3_corr_0 as _sq_3_corr_0, _sq_3._sq_3_corr_1 as _sq_3_corr_1, _sq_3._sq_3_corr_2 as _sq_3_corr_2, _sq_3._sq_3_corr_3 as _sq_3_corr_3, _sq_3._sq_3_corr_4 as _sq_3_corr_4, _sq_3._sq_3_corr_5 as _sq_3_corr_5, _sq_3._sq_3_corr_6 as _sq_3_corr_6, _sq_3._sq_3_corr_7 as _sq_3_corr_7, _sq_3._sq_3_value as _sq_3_value]
        PhysicalHashJoin [LeftSingle] [build: _sq_3._sq_3_corr_0, _sq_3._sq_3_corr_1, _sq_3._sq_3_corr_2, _sq_3._sq_3_corr_3, _sq_3._sq_3_corr_4, _sq_3._sq_3_corr_5, _sq_3._sq_3_corr_6, _sq_3._sq_3_corr_7] [probe: a_sq_1_sq_2.a.Name, a_sq_1_sq_2.a.City, a_sq_1_sq_2.a.Country, a_sq_1_sq_2.a.Population, a_sq_1_sq_2.a.Month, a_sq_1_sq_2.a.Money, a_sq_1_sq_2.a.Id, a_sq_1_sq_2.a.NullableValue]
          PhysicalCteRef [a_sq_1_sq_2 as a_sq_1_sq_2]
          PhysicalCteRef [_sq_3 as _sq_3]
      PhysicalSort [a.Name]
        PhysicalProject [a.Name as a.Name, CASE WHEN _sq_1._sq_1_key IS NOT NULL THEN 'Y' ELSE 'N' END as ExistsResult, CASE WHEN _sq_2._sq_2_key IS NULL THEN 'Y' ELSE 'N' END as NotExistsResult, __CorrelatedScalarSubqueryResult(_sq_3._sq_3_value) as Lookup]
          PhysicalCteRef [a_sq_1_sq_2_sq_3 as a_sq_1_sq_2_sq_3]
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
    AggregateGroup [Cte2AggregateGroup; keys: 8; typed aggs: 1]
    Generated [Cte2Row0]
      _sq_3_corr_0: string <- field _sq_3_corr_0
      _sq_3_corr_1: string <- field _sq_3_corr_1
      _sq_3_corr_2: string <- field _sq_3_corr_2
      _sq_3_corr_3: decimal <- field _sq_3_corr_3
      _sq_3_corr_4: string <- field _sq_3_corr_4
      _sq_3_corr_5: decimal <- field _sq_3_corr_5
      _sq_3_corr_6: int <- field _sq_3_corr_6
      _sq_3_corr_7: int? <- field _sq_3_corr_7
      _sq_3_value: CorrelatedScalarSubqueryResult<string> <- field _sq_3_value
    SourceEntity [a: BasicEntity]
      Name: string <- property Name
      City: string <- property City
      Country: string <- property Country
      Population: decimal <- property Population
      Money: decimal <- property Money
      Month: string <- property Month
      Id: int <- property Id
      NullableValue: int? <- property NullableValue
    SourceEntity [b: BasicEntity]
      Name: string <- property Name
      City: string <- property City
      Country: string <- property Country
      Population: decimal <- property Population
      Money: decimal <- property Money
      Month: string <- property Month
      Id: int <- property Id
      NullableValue: int? <- property NullableValue
    HashPayload [_sq_1HashPayload0]
      _sq_1_key: int <- field _sq_1_key
      _sq_1_corr_0: string <- field _sq_1_corr_0
      _sq_1_corr_1: string <- field _sq_1_corr_1
      _sq_1_corr_2: string <- field _sq_1_corr_2
      _sq_1_corr_3: decimal <- field _sq_1_corr_3
      _sq_1_corr_4: string <- field _sq_1_corr_4
      _sq_1_corr_5: decimal <- field _sq_1_corr_5
      _sq_1_corr_6: int <- field _sq_1_corr_6
      _sq_1_corr_7: int? <- field _sq_1_corr_7
    TableRow [_sq_1]
      _sq_1_key: int <- field _sq_1_key
      _sq_1_corr_0: string <- field _sq_1_corr_0
      _sq_1_corr_1: string <- field _sq_1_corr_1
      _sq_1_corr_2: string <- field _sq_1_corr_2
      _sq_1_corr_3: decimal <- field _sq_1_corr_3
      _sq_1_corr_4: string <- field _sq_1_corr_4
      _sq_1_corr_5: decimal <- field _sq_1_corr_5
      _sq_1_corr_6: int <- field _sq_1_corr_6
      _sq_1_corr_7: int? <- field _sq_1_corr_7
    Generated [Statement0Row0]
      a.Name: string <- field a_Name
      a.City: string <- field a_City
      a.Country: string <- field a_Country
      a.Population: decimal <- field a_Population
      a.Money: decimal <- field a_Money
      a.Month: string <- field a_Month
      a.Id: int <- field a_Id
      a.NullableValue: int? <- field a_NullableValue
      _sq_1._sq_1_key: int? <- field _sq_1__sq_1_key
      _sq_1._sq_1_corr_0: string <- field _sq_1__sq_1_corr_0
      _sq_1._sq_1_corr_1: string <- field _sq_1__sq_1_corr_1
      _sq_1._sq_1_corr_2: string <- field _sq_1__sq_1_corr_2
      _sq_1._sq_1_corr_3: decimal? <- field _sq_1__sq_1_corr_3
      _sq_1._sq_1_corr_4: string <- field _sq_1__sq_1_corr_4
      _sq_1._sq_1_corr_5: decimal? <- field _sq_1__sq_1_corr_5
      _sq_1._sq_1_corr_6: int? <- field _sq_1__sq_1_corr_6
      _sq_1._sq_1_corr_7: int? <- field _sq_1__sq_1_corr_7
    TableRow [a_sq_1]
      a.Name: string <- field a_Name
      a.City: string <- field a_City
      a.Country: string <- field a_Country
      a.Population: decimal <- field a_Population
      a.Money: decimal <- field a_Money
      a.Month: string <- field a_Month
      a.Id: int <- field a_Id
      a.NullableValue: int? <- field a_NullableValue
      _sq_1._sq_1_key: int? <- field _sq_1__sq_1_key
      _sq_1._sq_1_corr_0: string <- field _sq_1__sq_1_corr_0
      _sq_1._sq_1_corr_1: string <- field _sq_1__sq_1_corr_1
      _sq_1._sq_1_corr_2: string <- field _sq_1__sq_1_corr_2
      _sq_1._sq_1_corr_3: decimal? <- field _sq_1__sq_1_corr_3
      _sq_1._sq_1_corr_4: string <- field _sq_1__sq_1_corr_4
      _sq_1._sq_1_corr_5: decimal? <- field _sq_1__sq_1_corr_5
      _sq_1._sq_1_corr_6: int? <- field _sq_1__sq_1_corr_6
      _sq_1._sq_1_corr_7: int? <- field _sq_1__sq_1_corr_7
    SourceEntity [b: BasicEntity]
      Name: string <- property Name
      City: string <- property City
      Country: string <- property Country
      Population: decimal <- property Population
      Money: decimal <- property Money
      Month: string <- property Month
      Id: int <- property Id
      NullableValue: int? <- property NullableValue
    HashPayload [_sq_2HashPayload0]
      _sq_2_key: int <- field _sq_2_key
      _sq_2_corr_0: string <- field _sq_2_corr_0
      _sq_2_corr_1: string <- field _sq_2_corr_1
      _sq_2_corr_2: string <- field _sq_2_corr_2
      _sq_2_corr_3: decimal <- field _sq_2_corr_3
      _sq_2_corr_4: string <- field _sq_2_corr_4
      _sq_2_corr_5: decimal <- field _sq_2_corr_5
      _sq_2_corr_6: int <- field _sq_2_corr_6
      _sq_2_corr_7: int? <- field _sq_2_corr_7
    TableRow [_sq_2]
      _sq_2_key: int <- field _sq_2_key
      _sq_2_corr_0: string <- field _sq_2_corr_0
      _sq_2_corr_1: string <- field _sq_2_corr_1
      _sq_2_corr_2: string <- field _sq_2_corr_2
      _sq_2_corr_3: decimal <- field _sq_2_corr_3
      _sq_2_corr_4: string <- field _sq_2_corr_4
      _sq_2_corr_5: decimal <- field _sq_2_corr_5
      _sq_2_corr_6: int <- field _sq_2_corr_6
      _sq_2_corr_7: int? <- field _sq_2_corr_7
    Generated [Statement1Row0]
      a.Name: string <- field a_Name
      a.City: string <- field a_City
      a.Country: string <- field a_Country
      a.Population: decimal <- field a_Population
      a.Money: decimal <- field a_Money
      a.Month: string <- field a_Month
      a.Id: int <- field a_Id
      a.NullableValue: int? <- field a_NullableValue
      _sq_1._sq_1_key: int? <- field _sq_1__sq_1_key
      _sq_1._sq_1_corr_0: string <- field _sq_1__sq_1_corr_0
      _sq_1._sq_1_corr_1: string <- field _sq_1__sq_1_corr_1
      _sq_1._sq_1_corr_2: string <- field _sq_1__sq_1_corr_2
      _sq_1._sq_1_corr_3: decimal? <- field _sq_1__sq_1_corr_3
      _sq_1._sq_1_corr_4: string <- field _sq_1__sq_1_corr_4
      _sq_1._sq_1_corr_5: decimal? <- field _sq_1__sq_1_corr_5
      _sq_1._sq_1_corr_6: int? <- field _sq_1__sq_1_corr_6
      _sq_1._sq_1_corr_7: int? <- field _sq_1__sq_1_corr_7
      _sq_2._sq_2_key: int? <- field _sq_2__sq_2_key
      _sq_2._sq_2_corr_0: string <- field _sq_2__sq_2_corr_0
      _sq_2._sq_2_corr_1: string <- field _sq_2__sq_2_corr_1
      _sq_2._sq_2_corr_2: string <- field _sq_2__sq_2_corr_2
      _sq_2._sq_2_corr_3: decimal? <- field _sq_2__sq_2_corr_3
      _sq_2._sq_2_corr_4: string <- field _sq_2__sq_2_corr_4
      _sq_2._sq_2_corr_5: decimal? <- field _sq_2__sq_2_corr_5
      _sq_2._sq_2_corr_6: int? <- field _sq_2__sq_2_corr_6
      _sq_2._sq_2_corr_7: int? <- field _sq_2__sq_2_corr_7
    TableRow [a_sq_1_sq_2]
      a.Name: string <- field a_Name
      a.City: string <- field a_City
      a.Country: string <- field a_Country
      a.Population: decimal <- field a_Population
      a.Money: decimal <- field a_Money
      a.Month: string <- field a_Month
      a.Id: int <- field a_Id
      a.NullableValue: int? <- field a_NullableValue
      _sq_1._sq_1_key: int? <- field _sq_1__sq_1_key
      _sq_1._sq_1_corr_0: string <- field _sq_1__sq_1_corr_0
      _sq_1._sq_1_corr_1: string <- field _sq_1__sq_1_corr_1
      _sq_1._sq_1_corr_2: string <- field _sq_1__sq_1_corr_2
      _sq_1._sq_1_corr_3: decimal? <- field _sq_1__sq_1_corr_3
      _sq_1._sq_1_corr_4: string <- field _sq_1__sq_1_corr_4
      _sq_1._sq_1_corr_5: decimal? <- field _sq_1__sq_1_corr_5
      _sq_1._sq_1_corr_6: int? <- field _sq_1__sq_1_corr_6
      _sq_1._sq_1_corr_7: int? <- field _sq_1__sq_1_corr_7
      _sq_2._sq_2_key: int? <- field _sq_2__sq_2_key
      _sq_2._sq_2_corr_0: string <- field _sq_2__sq_2_corr_0
      _sq_2._sq_2_corr_1: string <- field _sq_2__sq_2_corr_1
      _sq_2._sq_2_corr_2: string <- field _sq_2__sq_2_corr_2
      _sq_2._sq_2_corr_3: decimal? <- field _sq_2__sq_2_corr_3
      _sq_2._sq_2_corr_4: string <- field _sq_2__sq_2_corr_4
      _sq_2._sq_2_corr_5: decimal? <- field _sq_2__sq_2_corr_5
      _sq_2._sq_2_corr_6: int? <- field _sq_2__sq_2_corr_6
      _sq_2._sq_2_corr_7: int? <- field _sq_2__sq_2_corr_7
    TableRow [_sq_3]
      _sq_3_corr_0: string <- field _sq_3_corr_0
      _sq_3_corr_1: string <- field _sq_3_corr_1
      _sq_3_corr_2: string <- field _sq_3_corr_2
      _sq_3_corr_3: decimal <- field _sq_3_corr_3
      _sq_3_corr_4: string <- field _sq_3_corr_4
      _sq_3_corr_5: decimal <- field _sq_3_corr_5
      _sq_3_corr_6: int <- field _sq_3_corr_6
      _sq_3_corr_7: int? <- field _sq_3_corr_7
      _sq_3_value: CorrelatedScalarSubqueryResult<string> <- field _sq_3_value
    Generated [Statement2Row0]
      a.Name: string <- field a_Name
      a.City: string <- field a_City
      a.Country: string <- field a_Country
      a.Population: decimal <- field a_Population
      a.Money: decimal <- field a_Money
      a.Month: string <- field a_Month
      a.Id: int <- field a_Id
      a.NullableValue: int? <- field a_NullableValue
      _sq_1._sq_1_key: int? <- field _sq_1__sq_1_key
      _sq_1._sq_1_corr_0: string <- field _sq_1__sq_1_corr_0
      _sq_1._sq_1_corr_1: string <- field _sq_1__sq_1_corr_1
      _sq_1._sq_1_corr_2: string <- field _sq_1__sq_1_corr_2
      _sq_1._sq_1_corr_3: decimal? <- field _sq_1__sq_1_corr_3
      _sq_1._sq_1_corr_4: string <- field _sq_1__sq_1_corr_4
      _sq_1._sq_1_corr_5: decimal? <- field _sq_1__sq_1_corr_5
      _sq_1._sq_1_corr_6: int? <- field _sq_1__sq_1_corr_6
      _sq_1._sq_1_corr_7: int? <- field _sq_1__sq_1_corr_7
      _sq_2._sq_2_key: int? <- field _sq_2__sq_2_key
      _sq_2._sq_2_corr_0: string <- field _sq_2__sq_2_corr_0
      _sq_2._sq_2_corr_1: string <- field _sq_2__sq_2_corr_1
      _sq_2._sq_2_corr_2: string <- field _sq_2__sq_2_corr_2
      _sq_2._sq_2_corr_3: decimal? <- field _sq_2__sq_2_corr_3
      _sq_2._sq_2_corr_4: string <- field _sq_2__sq_2_corr_4
      _sq_2._sq_2_corr_5: decimal? <- field _sq_2__sq_2_corr_5
      _sq_2._sq_2_corr_6: int? <- field _sq_2__sq_2_corr_6
      _sq_2._sq_2_corr_7: int? <- field _sq_2__sq_2_corr_7
      _sq_3_corr_0: string <- field _sq_3_corr_0
      _sq_3_corr_1: string <- field _sq_3_corr_1
      _sq_3_corr_2: string <- field _sq_3_corr_2
      _sq_3_corr_3: decimal? <- field _sq_3_corr_3
      _sq_3_corr_4: string <- field _sq_3_corr_4
      _sq_3_corr_5: decimal? <- field _sq_3_corr_5
      _sq_3_corr_6: int? <- field _sq_3_corr_6
      _sq_3_corr_7: int? <- field _sq_3_corr_7
      _sq_3_value: CorrelatedScalarSubqueryResult<string> <- field _sq_3_value
    TableRow [a_sq_1_sq_2_sq_3]
      a.Name: string <- field a_Name
      a.City: string <- field a_City
      a.Country: string <- field a_Country
      a.Population: decimal <- field a_Population
      a.Money: decimal <- field a_Money
      a.Month: string <- field a_Month
      a.Id: int <- field a_Id
      a.NullableValue: int? <- field a_NullableValue
      _sq_1._sq_1_key: int? <- field _sq_1__sq_1_key
      _sq_1._sq_1_corr_0: string <- field _sq_1__sq_1_corr_0
      _sq_1._sq_1_corr_1: string <- field _sq_1__sq_1_corr_1
      _sq_1._sq_1_corr_2: string <- field _sq_1__sq_1_corr_2
      _sq_1._sq_1_corr_3: decimal? <- field _sq_1__sq_1_corr_3
      _sq_1._sq_1_corr_4: string <- field _sq_1__sq_1_corr_4
      _sq_1._sq_1_corr_5: decimal? <- field _sq_1__sq_1_corr_5
      _sq_1._sq_1_corr_6: int? <- field _sq_1__sq_1_corr_6
      _sq_1._sq_1_corr_7: int? <- field _sq_1__sq_1_corr_7
      _sq_2._sq_2_key: int? <- field _sq_2__sq_2_key
      _sq_2._sq_2_corr_0: string <- field _sq_2__sq_2_corr_0
      _sq_2._sq_2_corr_1: string <- field _sq_2__sq_2_corr_1
      _sq_2._sq_2_corr_2: string <- field _sq_2__sq_2_corr_2
      _sq_2._sq_2_corr_3: decimal? <- field _sq_2__sq_2_corr_3
      _sq_2._sq_2_corr_4: string <- field _sq_2__sq_2_corr_4
      _sq_2._sq_2_corr_5: decimal? <- field _sq_2__sq_2_corr_5
      _sq_2._sq_2_corr_6: int? <- field _sq_2__sq_2_corr_6
      _sq_2._sq_2_corr_7: int? <- field _sq_2__sq_2_corr_7
      _sq_3_corr_0: string <- field _sq_3_corr_0
      _sq_3_corr_1: string <- field _sq_3_corr_1
      _sq_3_corr_2: string <- field _sq_3_corr_2
      _sq_3_corr_3: decimal? <- field _sq_3_corr_3
      _sq_3_corr_4: string <- field _sq_3_corr_4
      _sq_3_corr_5: decimal? <- field _sq_3_corr_5
      _sq_3_corr_6: int? <- field _sq_3_corr_6
      _sq_3_corr_7: int? <- field _sq_3_corr_7
      _sq_3_value: CorrelatedScalarSubqueryResult<string> <- field _sq_3_value
    Generated [ResultRow0]
      a.Name: string <- field a_Name
      ExistsResult: string <- field ExistsResult
      NotExistsResult: string <- field NotExistsResult
      Lookup: string <- field Lookup

  Body
    CtePhase [cte0]
    CtePhase [cte1]
    SourceScan [b: BasicEntity] -> cte2_bRows
    CreateTable [cte2: Cte2Row0]
    CreateValueTupleAggregateContext [cte2Groups: (string, string, string, decimal, string, decimal, int, int?) -> Cte2AggregateGroup]
    ChunkedForEach [b in cte2_bRows]
      Let [city: string = b.City]
      GetOrAddValueTupleAggregateGroup [cte2Group = cte2Groups[(b.Name, city, b.Country, b.Population, b.Month, b.Money, b.Id, b.NullableValue)] by b.Name, b.City, b.Country, b.Population, b.Month, b.Money, b.Id, b.NullableValue; typed: Cte2AggregateGroup]
      TypedAggregateSet [Set(cte2Group.__agg0, city)]
    EnsureCapacity [cte2 <- cte2GroupsToFinalize.Count]
    ForEach [cte2FinalGroup in cte2GroupsToFinalize]
      AppendRow [cte2 <- Cte2Row0(_sq_3_corr_0: cte2FinalGroup.b.Name, _sq_3_corr_1: cte2FinalGroup.b.City, _sq_3_corr_2: cte2FinalGroup.b.Country, _sq_3_corr_3: cte2FinalGroup.b.Population, _sq_3_corr_4: cte2FinalGroup.b.Month, _sq_3_corr_5: cte2FinalGroup.b.Money, _sq_3_corr_6: cte2FinalGroup.b.Id, _sq_3_corr_7: cte2FinalGroup.b.NullableValue, _sq_3_value: b.__CorrelatedScalarSubqueryValue(b.City))]
    StoreTable [cte2 -> _cteRowResults.Slot2: List<Cte2Row0>]
    SourceScan [a: BasicEntity] -> statement0_aRows
    SourceScan [b: BasicEntity] -> cte0_bRows
    CreateTable [statement0: Statement0Row0]
    CreateHash [statement0_sq_1Hash: object -> Row]
    ChunkedForEach [b in cte0_bRows]
      CreateHashPayload [_sq_1 <- _sq_1HashPayload0(_sq_1_key: 1, _sq_1_corr_0: b.Name, _sq_1_corr_1: b.City, _sq_1_corr_2: b.Country, _sq_1_corr_3: b.Population, _sq_1_corr_4: b.Month, _sq_1_corr_5: b.Money, _sq_1_corr_6: b.Id, _sq_1_corr_7: b.NullableValue)]
      HashAdd [statement0_sq_1Hash[CreateNullableHashJoinKey(_sq_1._sq_1_key, _sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2, _sq_1._sq_1_corr_3, _sq_1._sq_1_corr_4, _sq_1._sq_1_corr_5, _sq_1._sq_1_corr_6, _sq_1._sq_1_corr_7)] += _sq_1]
    ChunkedForEach [a in statement0_aRows]
      HashProbe [statement0_sq_1Hash[CreateNullableHashJoinKey(1, a.Name, a.City, a.Country, a.Population, a.Month, a.Money, a.Id, a.NullableValue)] -> statement0_sq_1HashMatches] [match: statement0_sq_1HashHasMatch]
        ForEach [_sq_1 in statement0_sq_1HashMatches]
          Assign [statement0_sq_1HashHasMatch = TRUE]
          AppendRow [statement0 <- Statement0Row0(a.Name: a.Name, a.City: a.City, a.Country: a.Country, a.Population: a.Population, a.Money: a.Money, a.Month: a.Month, a.Id: a.Id, a.NullableValue: a.NullableValue, _sq_1._sq_1_key: _sq_1._sq_1_key, _sq_1._sq_1_corr_0: _sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1: _sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2: _sq_1._sq_1_corr_2, _sq_1._sq_1_corr_3: _sq_1._sq_1_corr_3, _sq_1._sq_1_corr_4: _sq_1._sq_1_corr_4, _sq_1._sq_1_corr_5: _sq_1._sq_1_corr_5, _sq_1._sq_1_corr_6: _sq_1._sq_1_corr_6, _sq_1._sq_1_corr_7: _sq_1._sq_1_corr_7)]
          Break
      HashProbeNoMatch
        AppendRow [statement0 <- Statement0Row0(a.Name: a.Name, a.City: a.City, a.Country: a.Country, a.Population: a.Population, a.Money: a.Money, a.Month: a.Month, a.Id: a.Id, a.NullableValue: a.NullableValue, _sq_1._sq_1_key: NULL, _sq_1._sq_1_corr_0: NULL, _sq_1._sq_1_corr_1: NULL, _sq_1._sq_1_corr_2: NULL, _sq_1._sq_1_corr_3: NULL, _sq_1._sq_1_corr_4: NULL, _sq_1._sq_1_corr_5: NULL, _sq_1._sq_1_corr_6: NULL, _sq_1._sq_1_corr_7: NULL)]
    StoreTable [statement0 -> _cteRowResults.Slot3: List<Statement0Row0>]
    SourceScan [b: BasicEntity] -> cte1_bRows
    CreateTable [statement1: Statement1Row0]
    CreateHash [statement1_sq_2Hash: object -> Row]
    ChunkedForEach [b in cte1_bRows]
      CreateHashPayload [_sq_2 <- _sq_2HashPayload0(_sq_2_key: 1, _sq_2_corr_0: b.Name, _sq_2_corr_1: b.City, _sq_2_corr_2: b.Country, _sq_2_corr_3: b.Population, _sq_2_corr_4: b.Month, _sq_2_corr_5: b.Money, _sq_2_corr_6: b.Id, _sq_2_corr_7: b.NullableValue)]
      HashAdd [statement1_sq_2Hash[CreateNullableHashJoinKey(_sq_2._sq_2_key, _sq_2._sq_2_corr_0, _sq_2._sq_2_corr_1, _sq_2._sq_2_corr_2, _sq_2._sq_2_corr_3, _sq_2._sq_2_corr_4, _sq_2._sq_2_corr_5, _sq_2._sq_2_corr_6, _sq_2._sq_2_corr_7)] += _sq_2]
    ForEach [a_sq_1 in _cteRowResults.Slot3]
      HashProbe [statement1_sq_2Hash[CreateNullableHashJoinKey(1, a_sq_1.a.Name, a_sq_1.a.City, a_sq_1.a.Country, a_sq_1.a.Population, a_sq_1.a.Month, a_sq_1.a.Money, a_sq_1.a.Id, a_sq_1.a.NullableValue)] -> statement1_sq_2HashMatches] [match: statement1_sq_2HashHasMatch]
        ForEach [_sq_2 in statement1_sq_2HashMatches]
          Assign [statement1_sq_2HashHasMatch = TRUE]
          AppendRow [statement1 <- Statement1Row0(a.Name: a_sq_1.a.Name, a.City: a_sq_1.a.City, a.Country: a_sq_1.a.Country, a.Population: a_sq_1.a.Population, a.Money: a_sq_1.a.Money, a.Month: a_sq_1.a.Month, a.Id: a_sq_1.a.Id, a.NullableValue: a_sq_1.a.NullableValue, _sq_1._sq_1_key: a_sq_1._sq_1._sq_1_key, _sq_1._sq_1_corr_0: a_sq_1._sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1: a_sq_1._sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2: a_sq_1._sq_1._sq_1_corr_2, _sq_1._sq_1_corr_3: a_sq_1._sq_1._sq_1_corr_3, _sq_1._sq_1_corr_4: a_sq_1._sq_1._sq_1_corr_4, _sq_1._sq_1_corr_5: a_sq_1._sq_1._sq_1_corr_5, _sq_1._sq_1_corr_6: a_sq_1._sq_1._sq_1_corr_6, _sq_1._sq_1_corr_7: a_sq_1._sq_1._sq_1_corr_7, _sq_2._sq_2_key: _sq_2._sq_2_key, _sq_2._sq_2_corr_0: _sq_2._sq_2_corr_0, _sq_2._sq_2_corr_1: _sq_2._sq_2_corr_1, _sq_2._sq_2_corr_2: _sq_2._sq_2_corr_2, _sq_2._sq_2_corr_3: _sq_2._sq_2_corr_3, _sq_2._sq_2_corr_4: _sq_2._sq_2_corr_4, _sq_2._sq_2_corr_5: _sq_2._sq_2_corr_5, _sq_2._sq_2_corr_6: _sq_2._sq_2_corr_6, _sq_2._sq_2_corr_7: _sq_2._sq_2_corr_7)]
          Break
      HashProbeNoMatch
        AppendRow [statement1 <- Statement1Row0(a.Name: a_sq_1.a.Name, a.City: a_sq_1.a.City, a.Country: a_sq_1.a.Country, a.Population: a_sq_1.a.Population, a.Money: a_sq_1.a.Money, a.Month: a_sq_1.a.Month, a.Id: a_sq_1.a.Id, a.NullableValue: a_sq_1.a.NullableValue, _sq_1._sq_1_key: a_sq_1._sq_1._sq_1_key, _sq_1._sq_1_corr_0: a_sq_1._sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1: a_sq_1._sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2: a_sq_1._sq_1._sq_1_corr_2, _sq_1._sq_1_corr_3: a_sq_1._sq_1._sq_1_corr_3, _sq_1._sq_1_corr_4: a_sq_1._sq_1._sq_1_corr_4, _sq_1._sq_1_corr_5: a_sq_1._sq_1._sq_1_corr_5, _sq_1._sq_1_corr_6: a_sq_1._sq_1._sq_1_corr_6, _sq_1._sq_1_corr_7: a_sq_1._sq_1._sq_1_corr_7, _sq_2._sq_2_key: NULL, _sq_2._sq_2_corr_0: NULL, _sq_2._sq_2_corr_1: NULL, _sq_2._sq_2_corr_2: NULL, _sq_2._sq_2_corr_3: NULL, _sq_2._sq_2_corr_4: NULL, _sq_2._sq_2_corr_5: NULL, _sq_2._sq_2_corr_6: NULL, _sq_2._sq_2_corr_7: NULL)]
    StoreTable [statement1 -> _cteRowResults.Slot4: List<Statement1Row0>]
    CreateTable [statement2: Statement2Row0]
    CreateObject [statement2_sq_3HashEmptyState: State<string>]
    Let [statement2_sq_3HashEmptyValue: CorrelatedScalarSubqueryResult<string> = Get(statement2_sq_3HashEmptyState)]
    CreateHash [statement2_sq_3Hash: object -> Row; capacity: _cteRowResults.Slot2.Count]
    ForEach [_sq_3 in _cteRowResults.Slot2]
      HashAdd [statement2_sq_3Hash[CreateNullableHashJoinKey(_sq_3._sq_3_corr_0, _sq_3._sq_3_corr_1, _sq_3._sq_3_corr_2, _sq_3._sq_3_corr_3, _sq_3._sq_3_corr_4, _sq_3._sq_3_corr_5, _sq_3._sq_3_corr_6, _sq_3._sq_3_corr_7)] += _sq_3]
    ForEach [a_sq_1_sq_2 in _cteRowResults.Slot4]
      HashProbe [statement2_sq_3Hash[CreateNullableHashJoinKey(a_sq_1_sq_2.a.Name, a_sq_1_sq_2.a.City, a_sq_1_sq_2.a.Country, a_sq_1_sq_2.a.Population, a_sq_1_sq_2.a.Month, a_sq_1_sq_2.a.Money, a_sq_1_sq_2.a.Id, a_sq_1_sq_2.a.NullableValue)] -> statement2_sq_3HashMatches] [match: statement2_sq_3HashHasMatch]
        ForEach [_sq_3 in statement2_sq_3HashMatches]
          Assign [statement2_sq_3HashHasMatch = TRUE]
          AppendRow [statement2 <- Statement2Row0(a.Name: a_sq_1_sq_2.a.Name, a.City: a_sq_1_sq_2.a.City, a.Country: a_sq_1_sq_2.a.Country, a.Population: a_sq_1_sq_2.a.Population, a.Money: a_sq_1_sq_2.a.Money, a.Month: a_sq_1_sq_2.a.Month, a.Id: a_sq_1_sq_2.a.Id, a.NullableValue: a_sq_1_sq_2.a.NullableValue, _sq_1._sq_1_key: a_sq_1_sq_2._sq_1._sq_1_key, _sq_1._sq_1_corr_0: a_sq_1_sq_2._sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1: a_sq_1_sq_2._sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2: a_sq_1_sq_2._sq_1._sq_1_corr_2, _sq_1._sq_1_corr_3: a_sq_1_sq_2._sq_1._sq_1_corr_3, _sq_1._sq_1_corr_4: a_sq_1_sq_2._sq_1._sq_1_corr_4, _sq_1._sq_1_corr_5: a_sq_1_sq_2._sq_1._sq_1_corr_5, _sq_1._sq_1_corr_6: a_sq_1_sq_2._sq_1._sq_1_corr_6, _sq_1._sq_1_corr_7: a_sq_1_sq_2._sq_1._sq_1_corr_7, _sq_2._sq_2_key: a_sq_1_sq_2._sq_2._sq_2_key, _sq_2._sq_2_corr_0: a_sq_1_sq_2._sq_2._sq_2_corr_0, _sq_2._sq_2_corr_1: a_sq_1_sq_2._sq_2._sq_2_corr_1, _sq_2._sq_2_corr_2: a_sq_1_sq_2._sq_2._sq_2_corr_2, _sq_2._sq_2_corr_3: a_sq_1_sq_2._sq_2._sq_2_corr_3, _sq_2._sq_2_corr_4: a_sq_1_sq_2._sq_2._sq_2_corr_4, _sq_2._sq_2_corr_5: a_sq_1_sq_2._sq_2._sq_2_corr_5, _sq_2._sq_2_corr_6: a_sq_1_sq_2._sq_2._sq_2_corr_6, _sq_2._sq_2_corr_7: a_sq_1_sq_2._sq_2._sq_2_corr_7, _sq_3_corr_0: _sq_3._sq_3_corr_0, _sq_3_corr_1: _sq_3._sq_3_corr_1, _sq_3_corr_2: _sq_3._sq_3_corr_2, _sq_3_corr_3: _sq_3._sq_3_corr_3, _sq_3_corr_4: _sq_3._sq_3_corr_4, _sq_3_corr_5: _sq_3._sq_3_corr_5, _sq_3_corr_6: _sq_3._sq_3_corr_6, _sq_3_corr_7: _sq_3._sq_3_corr_7, _sq_3_value: _sq_3._sq_3_value)]
      HashProbeNoMatch
        AppendRow [statement2 <- Statement2Row0(a.Name: a_sq_1_sq_2.a.Name, a.City: a_sq_1_sq_2.a.City, a.Country: a_sq_1_sq_2.a.Country, a.Population: a_sq_1_sq_2.a.Population, a.Money: a_sq_1_sq_2.a.Money, a.Month: a_sq_1_sq_2.a.Month, a.Id: a_sq_1_sq_2.a.Id, a.NullableValue: a_sq_1_sq_2.a.NullableValue, _sq_1._sq_1_key: a_sq_1_sq_2._sq_1._sq_1_key, _sq_1._sq_1_corr_0: a_sq_1_sq_2._sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1: a_sq_1_sq_2._sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2: a_sq_1_sq_2._sq_1._sq_1_corr_2, _sq_1._sq_1_corr_3: a_sq_1_sq_2._sq_1._sq_1_corr_3, _sq_1._sq_1_corr_4: a_sq_1_sq_2._sq_1._sq_1_corr_4, _sq_1._sq_1_corr_5: a_sq_1_sq_2._sq_1._sq_1_corr_5, _sq_1._sq_1_corr_6: a_sq_1_sq_2._sq_1._sq_1_corr_6, _sq_1._sq_1_corr_7: a_sq_1_sq_2._sq_1._sq_1_corr_7, _sq_2._sq_2_key: a_sq_1_sq_2._sq_2._sq_2_key, _sq_2._sq_2_corr_0: a_sq_1_sq_2._sq_2._sq_2_corr_0, _sq_2._sq_2_corr_1: a_sq_1_sq_2._sq_2._sq_2_corr_1, _sq_2._sq_2_corr_2: a_sq_1_sq_2._sq_2._sq_2_corr_2, _sq_2._sq_2_corr_3: a_sq_1_sq_2._sq_2._sq_2_corr_3, _sq_2._sq_2_corr_4: a_sq_1_sq_2._sq_2._sq_2_corr_4, _sq_2._sq_2_corr_5: a_sq_1_sq_2._sq_2._sq_2_corr_5, _sq_2._sq_2_corr_6: a_sq_1_sq_2._sq_2._sq_2_corr_6, _sq_2._sq_2_corr_7: a_sq_1_sq_2._sq_2._sq_2_corr_7, _sq_3_corr_0: NULL, _sq_3_corr_1: NULL, _sq_3_corr_2: NULL, _sq_3_corr_3: NULL, _sq_3_corr_4: NULL, _sq_3_corr_5: NULL, _sq_3_corr_6: NULL, _sq_3_corr_7: NULL, _sq_3_value: statement2_sq_3HashEmptyValue)]
    StoreTable [statement2 -> _cteRowResults.Slot5: List<Statement2Row0>]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [a_sq_1_sq_2_sq_3 in _cteRowResults.Slot5]
      AppendShape [result <- ResultShape0(a.Name: a_sq_1_sq_2_sq_3.a.Name, ExistsResult: CASE WHEN a_sq_1_sq_2_sq_3._sq_1._sq_1_key IS NOT NULL THEN 'Y' ELSE 'N' END, NotExistsResult: CASE WHEN a_sq_1_sq_2_sq_3._sq_2._sq_2_key IS NULL THEN 'Y' ELSE 'N' END, Lookup: __CorrelatedScalarSubqueryResult(a_sq_1_sq_2_sq_3._sq_3_value))]
    SortShapeRows [result -> resultSorted by a.Name ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_cte2_1 = new Column[]
        {
            new Column("_sq_3_corr_0", typeof(string), 0),
            new Column("_sq_3_corr_1", typeof(string), 1),
            new Column("_sq_3_corr_2", typeof(string), 2),
            new Column("_sq_3_corr_3", typeof(decimal), 3),
            new Column("_sq_3_corr_4", typeof(string), 4),
            new Column("_sq_3_corr_5", typeof(decimal), 5),
            new Column("_sq_3_corr_6", typeof(int), 6),
            new Column("_sq_3_corr_7", typeof(int?), 7),
            new Column("_sq_3_value", typeof(Musoq.Plugins.CorrelatedScalarSubqueryResult<string>), 8)
        };
        private static readonly Column[] __columns_compiled_result_5 = new Column[]
        {
            new Column("a.Name", typeof(string), 0),
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
            new Column("_sq_1._sq_1_key", typeof(int?), 8),
            new Column("_sq_1._sq_1_corr_0", typeof(string), 9),
            new Column("_sq_1._sq_1_corr_1", typeof(string), 10),
            new Column("_sq_1._sq_1_corr_2", typeof(string), 11),
            new Column("_sq_1._sq_1_corr_3", typeof(decimal?), 12),
            new Column("_sq_1._sq_1_corr_4", typeof(string), 13),
            new Column("_sq_1._sq_1_corr_5", typeof(decimal?), 14),
            new Column("_sq_1._sq_1_corr_6", typeof(int?), 15),
            new Column("_sq_1._sq_1_corr_7", typeof(int?), 16)
        };
        private static readonly Column[] __columns_compiled_statement1_3 = new Column[]
        {
            new Column("a.Name", typeof(string), 0),
            new Column("a.City", typeof(string), 1),
            new Column("a.Country", typeof(string), 2),
            new Column("a.Population", typeof(decimal), 3),
            new Column("a.Money", typeof(decimal), 4),
            new Column("a.Month", typeof(string), 5),
            new Column("a.Id", typeof(int), 6),
            new Column("a.NullableValue", typeof(int?), 7),
            new Column("_sq_1._sq_1_key", typeof(int?), 8),
            new Column("_sq_1._sq_1_corr_0", typeof(string), 9),
            new Column("_sq_1._sq_1_corr_1", typeof(string), 10),
            new Column("_sq_1._sq_1_corr_2", typeof(string), 11),
            new Column("_sq_1._sq_1_corr_3", typeof(decimal?), 12),
            new Column("_sq_1._sq_1_corr_4", typeof(string), 13),
            new Column("_sq_1._sq_1_corr_5", typeof(decimal?), 14),
            new Column("_sq_1._sq_1_corr_6", typeof(int?), 15),
            new Column("_sq_1._sq_1_corr_7", typeof(int?), 16),
            new Column("_sq_2._sq_2_key", typeof(int?), 17),
            new Column("_sq_2._sq_2_corr_0", typeof(string), 18),
            new Column("_sq_2._sq_2_corr_1", typeof(string), 19),
            new Column("_sq_2._sq_2_corr_2", typeof(string), 20),
            new Column("_sq_2._sq_2_corr_3", typeof(decimal?), 21),
            new Column("_sq_2._sq_2_corr_4", typeof(string), 22),
            new Column("_sq_2._sq_2_corr_5", typeof(decimal?), 23),
            new Column("_sq_2._sq_2_corr_6", typeof(int?), 24),
            new Column("_sq_2._sq_2_corr_7", typeof(int?), 25)
        };
        private static readonly Column[] __columns_compiled_statement2_4 = new Column[]
        {
            new Column("a.Name", typeof(string), 0),
            new Column("a.City", typeof(string), 1),
            new Column("a.Country", typeof(string), 2),
            new Column("a.Population", typeof(decimal), 3),
            new Column("a.Money", typeof(decimal), 4),
            new Column("a.Month", typeof(string), 5),
            new Column("a.Id", typeof(int), 6),
            new Column("a.NullableValue", typeof(int?), 7),
            new Column("_sq_1._sq_1_key", typeof(int?), 8),
            new Column("_sq_1._sq_1_corr_0", typeof(string), 9),
            new Column("_sq_1._sq_1_corr_1", typeof(string), 10),
            new Column("_sq_1._sq_1_corr_2", typeof(string), 11),
            new Column("_sq_1._sq_1_corr_3", typeof(decimal?), 12),
            new Column("_sq_1._sq_1_corr_4", typeof(string), 13),
            new Column("_sq_1._sq_1_corr_5", typeof(decimal?), 14),
            new Column("_sq_1._sq_1_corr_6", typeof(int?), 15),
            new Column("_sq_1._sq_1_corr_7", typeof(int?), 16),
            new Column("_sq_2._sq_2_key", typeof(int?), 17),
            new Column("_sq_2._sq_2_corr_0", typeof(string), 18),
            new Column("_sq_2._sq_2_corr_1", typeof(string), 19),
            new Column("_sq_2._sq_2_corr_2", typeof(string), 20),
            new Column("_sq_2._sq_2_corr_3", typeof(decimal?), 21),
            new Column("_sq_2._sq_2_corr_4", typeof(string), 22),
            new Column("_sq_2._sq_2_corr_5", typeof(decimal?), 23),
            new Column("_sq_2._sq_2_corr_6", typeof(int?), 24),
            new Column("_sq_2._sq_2_corr_7", typeof(int?), 25),
            new Column("_sq_3_corr_0", typeof(string), 26),
            new Column("_sq_3_corr_1", typeof(string), 27),
            new Column("_sq_3_corr_2", typeof(string), 28),
            new Column("_sq_3_corr_3", typeof(decimal?), 29),
            new Column("_sq_3_corr_4", typeof(string), 30),
            new Column("_sq_3_corr_5", typeof(decimal?), 31),
            new Column("_sq_3_corr_6", typeof(int?), 32),
            new Column("_sq_3_corr_7", typeof(int?), 33),
            new Column("_sq_3_value", typeof(Musoq.Plugins.CorrelatedScalarSubqueryResult<string>), 34)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("City", typeof(string), 11), new Column("Country", typeof(string), 12), new Column("Population", typeof(decimal), 13), new Column("Money", typeof(decimal), 15), new Column("Month", typeof(string), 16), new Column("Id", typeof(int), 18), new Column("NullableValue", typeof(int?), 19) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_5, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.a_Name, __musoqShapeRow.ExistsResult, __musoqShapeRow.NotExistsResult, __musoqShapeRow.Lookup);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.GroupBy);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            OnPhaseChanged("compiled:cte2", QueryPhase.Begin);
            OnPhaseChanged("compiled:cte3", QueryPhase.Begin);
            OnPhaseChanged("compiled:cte4", QueryPhase.Begin);
            OnPhaseChanged("compiled:cte5", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                _cteRowResults.Slot2 = BuildCte2(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults);
                _cteRowResults.Slot3 = BuildCte3(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults);
                _cteRowResults.Slot4 = BuildCte4(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults);
                _cteRowResults.Slot5 = BuildCte5(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults);
                var result = new List<ResultShape0>();
                var __storedTable5Rows = _cteRowResults.Slot5;
                for (int __storedTable5Index = 0; __storedTable5Index < __storedTable5Rows.Count; ++__storedTable5Index)
                {
                    if ((__storedTable5Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement2Row0 a_sq_1_sq_2_sq_3 = __storedTable5Rows[__storedTable5Index];
                    result.Add(new ResultShape0(a_sq_1_sq_2_sq_3.a_Name, (a_sq_1_sq_2_sq_3._sq_1__sq_1_key != null) ? (string)"Y" : (string)"N", (a_sq_1_sq_2_sq_3._sq_2__sq_2_key == null) ? (string)"Y" : (string)"N", (string)Musoq.Plugins.CorrelatedScalarSubqueryResultExtractor.GetValue(a_sq_1_sq_2_sq_3._sq_3_value)));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.a_Name, right.a_Name);
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
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
                OnPhaseChanged("compiled:cte2", QueryPhase.End);
                OnPhaseChanged("compiled:cte3", QueryPhase.End);
                OnPhaseChanged("compiled:cte4", QueryPhase.End);
                OnPhaseChanged("compiled:cte5", QueryPhase.End);
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte2Row0> BuildCte2(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, CteRowResults _cteRowResults)
        {
            var __cte2_bSchema = provider.GetSchema("#B");
            var cte2_bRowsSource = __cte2_bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("b:4", sourceExecutionPlans["b:4"], token, __schemaColumns_compiled_b_0, sourceRuntimeSettingsBySourceContextId["b:4"], logger, OnDataSourceProgress), Array.Empty<object>());
            var cte2_bRows = cte2_bRowsSource.Chunks;
            var cte2 = new List<Cte2Row0>();
            var cte2GroupsToFinalize = new List<Cte2AggregateGroup>();
            var cte2Groups = new Dictionary<(string, string, string, decimal, string, decimal, int, int?), Cte2AggregateGroup>();
            foreach (var bChunk in cte2_bRows)
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
                            string city = b.City;
                            string groupKey0 = b.Name;
                            string groupKey1 = city;
                            string groupKey2 = b.Country;
                            decimal groupKey3 = b.Population;
                            string groupKey4 = b.Month;
                            decimal groupKey5 = b.Money;
                            int groupKey6 = b.Id;
                            int? groupKey7 = b.NullableValue;
                            ref var cte2GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte2Groups, (groupKey0, groupKey1, groupKey2, groupKey3, groupKey4, groupKey5, groupKey6, groupKey7), out var cte2GroupExists);
                            if (!cte2GroupExists)
                            {
                                cte2GroupRef = new Cte2AggregateGroup(groupKey0, groupKey1, groupKey2, groupKey3, groupKey4, groupKey5, groupKey6, groupKey7);
                                cte2GroupsToFinalize.Add(cte2GroupRef);
                            }

                            Cte2AggregateGroup cte2Group = cte2GroupRef;
                            Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.Set(ref cte2Group.__agg0, (string)city);
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
                            string city = b.City;
                            string groupKey0 = b.Name;
                            string groupKey1 = city;
                            string groupKey2 = b.Country;
                            decimal groupKey3 = b.Population;
                            string groupKey4 = b.Month;
                            decimal groupKey5 = b.Money;
                            int groupKey6 = b.Id;
                            int? groupKey7 = b.NullableValue;
                            ref var cte2GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte2Groups, (groupKey0, groupKey1, groupKey2, groupKey3, groupKey4, groupKey5, groupKey6, groupKey7), out var cte2GroupExists);
                            if (!cte2GroupExists)
                            {
                                cte2GroupRef = new Cte2AggregateGroup(groupKey0, groupKey1, groupKey2, groupKey3, groupKey4, groupKey5, groupKey6, groupKey7);
                                cte2GroupsToFinalize.Add(cte2GroupRef);
                            }

                            Cte2AggregateGroup cte2Group = cte2GroupRef;
                            Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.Set(ref cte2Group.__agg0, (string)city);
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
                    string city = b.City;
                    string groupKey0 = b.Name;
                    string groupKey1 = city;
                    string groupKey2 = b.Country;
                    decimal groupKey3 = b.Population;
                    string groupKey4 = b.Month;
                    decimal groupKey5 = b.Money;
                    int groupKey6 = b.Id;
                    int? groupKey7 = b.NullableValue;
                    ref var cte2GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte2Groups, (groupKey0, groupKey1, groupKey2, groupKey3, groupKey4, groupKey5, groupKey6, groupKey7), out var cte2GroupExists);
                    if (!cte2GroupExists)
                    {
                        cte2GroupRef = new Cte2AggregateGroup(groupKey0, groupKey1, groupKey2, groupKey3, groupKey4, groupKey5, groupKey6, groupKey7);
                        cte2GroupsToFinalize.Add(cte2GroupRef);
                    }

                    Cte2AggregateGroup cte2Group = cte2GroupRef;
                    Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.Set(ref cte2Group.__agg0, (string)city);
                }
            }

            cte2.EnsureCapacity(cte2GroupsToFinalize.Count);
            foreach (var cte2FinalGroup in cte2GroupsToFinalize)
            {
                token.ThrowIfCancellationRequested();
                cte2.Add(new Cte2Row0(cte2FinalGroup.__key0, cte2FinalGroup.__key1, cte2FinalGroup.__key2, cte2FinalGroup.__key3, cte2FinalGroup.__key4, cte2FinalGroup.__key5, cte2FinalGroup.__key6, cte2FinalGroup.__key7, Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.Get(in cte2FinalGroup.__agg0)));
            }

            return cte2;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement0Row0> BuildCte3(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, CteRowResults _cteRowResults)
        {
            var __statement0_aSchema = provider.GetSchema("#A");
            var statement0_aRowsSource = __statement0_aSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("a:4", sourceExecutionPlans["a:4"], token, __schemaColumns_compiled_b_0, sourceRuntimeSettingsBySourceContextId["a:4"], logger, OnDataSourceProgress), Array.Empty<object>());
            var statement0_aRows = statement0_aRowsSource.Chunks;
            var __cte0_bSchema = provider.GetSchema("#B");
            var cte0_bRowsSource = __cte0_bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("b:2", sourceExecutionPlans["b:2"], token, __schemaColumns_compiled_b_0, sourceRuntimeSettingsBySourceContextId["b:2"], logger, OnDataSourceProgress), Array.Empty<object>());
            var cte0_bRows = cte0_bRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            var statement0_sq_1Hash = new Dictionary<object, HashJoinBucket<_sq_1HashPayload0>>();
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
                            _sq_1HashPayload0 _sq_1 = new _sq_1HashPayload0(1, b.Name, b.City, b.Country, b.Population, b.Month, b.Money, b.Id, b.NullableValue);
                            object key = (object)Musoq.Evaluator.Helpers.EvaluationHelper.CreateNullableHashJoinKey(_sq_1._sq_1_key, _sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2, _sq_1._sq_1_corr_3, _sq_1._sq_1_corr_4, _sq_1._sq_1_corr_5, _sq_1._sq_1_corr_6, _sq_1._sq_1_corr_7);
                            if (key == null)
                                continue;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0_sq_1Hash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<_sq_1HashPayload0>(_sq_1);
                                }
                                else
                                {
                                    matches.Add(_sq_1);
                                }
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
                            _sq_1HashPayload0 _sq_1 = new _sq_1HashPayload0(1, b.Name, b.City, b.Country, b.Population, b.Month, b.Money, b.Id, b.NullableValue);
                            object key = (object)Musoq.Evaluator.Helpers.EvaluationHelper.CreateNullableHashJoinKey(_sq_1._sq_1_key, _sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2, _sq_1._sq_1_corr_3, _sq_1._sq_1_corr_4, _sq_1._sq_1_corr_5, _sq_1._sq_1_corr_6, _sq_1._sq_1_corr_7);
                            if (key == null)
                                continue;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0_sq_1Hash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<_sq_1HashPayload0>(_sq_1);
                                }
                                else
                                {
                                    matches.Add(_sq_1);
                                }
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
                    _sq_1HashPayload0 _sq_1 = new _sq_1HashPayload0(1, b.Name, b.City, b.Country, b.Population, b.Month, b.Money, b.Id, b.NullableValue);
                    object key = (object)Musoq.Evaluator.Helpers.EvaluationHelper.CreateNullableHashJoinKey(_sq_1._sq_1_key, _sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2, _sq_1._sq_1_corr_3, _sq_1._sq_1_corr_4, _sq_1._sq_1_corr_5, _sq_1._sq_1_corr_6, _sq_1._sq_1_corr_7);
                    if (key == null)
                        continue;
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0_sq_1Hash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<_sq_1HashPayload0>(_sq_1);
                        }
                        else
                        {
                            matches.Add(_sq_1);
                        }
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
                            object key = (object)Musoq.Evaluator.Helpers.EvaluationHelper.CreateNullableHashJoinKey(1, a.Name, a.City, a.Country, a.Population, a.Month, a.Money, a.Id, a.NullableValue);
                            if (key != null && statement0_sq_1Hash.TryGetValue(key, out var statement0_sq_1HashMatches))
                            {
                                foreach (var _sq_1 in statement0_sq_1HashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0_sq_1HashHasMatch = true;
                                    statement0.Add(new Statement0Row0(a.Name, a.City, a.Country, a.Population, a.Money, a.Month, a.Id, a.NullableValue, _sq_1._sq_1_key, _sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2, _sq_1._sq_1_corr_3, _sq_1._sq_1_corr_4, _sq_1._sq_1_corr_5, _sq_1._sq_1_corr_6, _sq_1._sq_1_corr_7));
                                    break;
                                }
                            }

                            if (!statement0_sq_1HashHasMatch)
                            {
                                statement0.Add(new Statement0Row0(a.Name, a.City, a.Country, a.Population, a.Money, a.Month, a.Id, a.NullableValue, null, null, null, null, null, null, null, null, null));
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
                            object key = (object)Musoq.Evaluator.Helpers.EvaluationHelper.CreateNullableHashJoinKey(1, a.Name, a.City, a.Country, a.Population, a.Month, a.Money, a.Id, a.NullableValue);
                            if (key != null && statement0_sq_1Hash.TryGetValue(key, out var statement0_sq_1HashMatches))
                            {
                                foreach (var _sq_1 in statement0_sq_1HashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0_sq_1HashHasMatch = true;
                                    statement0.Add(new Statement0Row0(a.Name, a.City, a.Country, a.Population, a.Money, a.Month, a.Id, a.NullableValue, _sq_1._sq_1_key, _sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2, _sq_1._sq_1_corr_3, _sq_1._sq_1_corr_4, _sq_1._sq_1_corr_5, _sq_1._sq_1_corr_6, _sq_1._sq_1_corr_7));
                                    break;
                                }
                            }

                            if (!statement0_sq_1HashHasMatch)
                            {
                                statement0.Add(new Statement0Row0(a.Name, a.City, a.Country, a.Population, a.Money, a.Month, a.Id, a.NullableValue, null, null, null, null, null, null, null, null, null));
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
                    object key = (object)Musoq.Evaluator.Helpers.EvaluationHelper.CreateNullableHashJoinKey(1, a.Name, a.City, a.Country, a.Population, a.Month, a.Money, a.Id, a.NullableValue);
                    if (key != null && statement0_sq_1Hash.TryGetValue(key, out var statement0_sq_1HashMatches))
                    {
                        foreach (var _sq_1 in statement0_sq_1HashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            statement0_sq_1HashHasMatch = true;
                            statement0.Add(new Statement0Row0(a.Name, a.City, a.Country, a.Population, a.Money, a.Month, a.Id, a.NullableValue, _sq_1._sq_1_key, _sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2, _sq_1._sq_1_corr_3, _sq_1._sq_1_corr_4, _sq_1._sq_1_corr_5, _sq_1._sq_1_corr_6, _sq_1._sq_1_corr_7));
                            break;
                        }
                    }

                    if (!statement0_sq_1HashHasMatch)
                    {
                        statement0.Add(new Statement0Row0(a.Name, a.City, a.Country, a.Population, a.Money, a.Month, a.Id, a.NullableValue, null, null, null, null, null, null, null, null, null));
                    }
                }
            }

            return statement0;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement1Row0> BuildCte4(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, CteRowResults _cteRowResults)
        {
            var __cte1_bSchema = provider.GetSchema("#B");
            var cte1_bRowsSource = __cte1_bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("b:3", sourceExecutionPlans["b:3"], token, __schemaColumns_compiled_b_0, sourceRuntimeSettingsBySourceContextId["b:3"], logger, OnDataSourceProgress), Array.Empty<object>());
            var cte1_bRows = cte1_bRowsSource.Chunks;
            var statement1 = new List<Statement1Row0>(_cteRowResults.Slot3.Count);
            var statement1_sq_2Hash = new Dictionary<object, HashJoinBucket<_sq_2HashPayload0>>();
            foreach (var bChunk in cte1_bRows)
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
                            _sq_2HashPayload0 _sq_2 = new _sq_2HashPayload0(1, b.Name, b.City, b.Country, b.Population, b.Month, b.Money, b.Id, b.NullableValue);
                            object key = (object)Musoq.Evaluator.Helpers.EvaluationHelper.CreateNullableHashJoinKey(_sq_2._sq_2_key, _sq_2._sq_2_corr_0, _sq_2._sq_2_corr_1, _sq_2._sq_2_corr_2, _sq_2._sq_2_corr_3, _sq_2._sq_2_corr_4, _sq_2._sq_2_corr_5, _sq_2._sq_2_corr_6, _sq_2._sq_2_corr_7);
                            if (key == null)
                                continue;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement1_sq_2Hash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<_sq_2HashPayload0>(_sq_2);
                                }
                                else
                                {
                                    matches.Add(_sq_2);
                                }
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
                            _sq_2HashPayload0 _sq_2 = new _sq_2HashPayload0(1, b.Name, b.City, b.Country, b.Population, b.Month, b.Money, b.Id, b.NullableValue);
                            object key = (object)Musoq.Evaluator.Helpers.EvaluationHelper.CreateNullableHashJoinKey(_sq_2._sq_2_key, _sq_2._sq_2_corr_0, _sq_2._sq_2_corr_1, _sq_2._sq_2_corr_2, _sq_2._sq_2_corr_3, _sq_2._sq_2_corr_4, _sq_2._sq_2_corr_5, _sq_2._sq_2_corr_6, _sq_2._sq_2_corr_7);
                            if (key == null)
                                continue;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement1_sq_2Hash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<_sq_2HashPayload0>(_sq_2);
                                }
                                else
                                {
                                    matches.Add(_sq_2);
                                }
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
                    _sq_2HashPayload0 _sq_2 = new _sq_2HashPayload0(1, b.Name, b.City, b.Country, b.Population, b.Month, b.Money, b.Id, b.NullableValue);
                    object key = (object)Musoq.Evaluator.Helpers.EvaluationHelper.CreateNullableHashJoinKey(_sq_2._sq_2_key, _sq_2._sq_2_corr_0, _sq_2._sq_2_corr_1, _sq_2._sq_2_corr_2, _sq_2._sq_2_corr_3, _sq_2._sq_2_corr_4, _sq_2._sq_2_corr_5, _sq_2._sq_2_corr_6, _sq_2._sq_2_corr_7);
                    if (key == null)
                        continue;
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement1_sq_2Hash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<_sq_2HashPayload0>(_sq_2);
                        }
                        else
                        {
                            matches.Add(_sq_2);
                        }
                    }
                }
            }

            var __storedTable3Rows = _cteRowResults.Slot3;
            for (int __storedTable3Index = 0; __storedTable3Index < __storedTable3Rows.Count; ++__storedTable3Index)
            {
                if ((__storedTable3Index & 1023) == 0)
                {
                    token.ThrowIfCancellationRequested();
                }

                Statement0Row0 a_sq_1 = __storedTable3Rows[__storedTable3Index];
                bool statement1_sq_2HashHasMatch = false;
                object key = (object)Musoq.Evaluator.Helpers.EvaluationHelper.CreateNullableHashJoinKey(1, a_sq_1.a_Name, a_sq_1.a_City, a_sq_1.a_Country, a_sq_1.a_Population, a_sq_1.a_Month, a_sq_1.a_Money, a_sq_1.a_Id, a_sq_1.a_NullableValue);
                if (key != null && statement1_sq_2Hash.TryGetValue(key, out var statement1_sq_2HashMatches))
                {
                    foreach (var _sq_2 in statement1_sq_2HashMatches)
                    {
                        token.ThrowIfCancellationRequested();
                        statement1_sq_2HashHasMatch = true;
                        statement1.Add(new Statement1Row0(a_sq_1.a_Name, a_sq_1.a_City, a_sq_1.a_Country, a_sq_1.a_Population, a_sq_1.a_Money, a_sq_1.a_Month, a_sq_1.a_Id, a_sq_1.a_NullableValue, a_sq_1._sq_1__sq_1_key, a_sq_1._sq_1__sq_1_corr_0, a_sq_1._sq_1__sq_1_corr_1, a_sq_1._sq_1__sq_1_corr_2, a_sq_1._sq_1__sq_1_corr_3, a_sq_1._sq_1__sq_1_corr_4, a_sq_1._sq_1__sq_1_corr_5, a_sq_1._sq_1__sq_1_corr_6, a_sq_1._sq_1__sq_1_corr_7, _sq_2._sq_2_key, _sq_2._sq_2_corr_0, _sq_2._sq_2_corr_1, _sq_2._sq_2_corr_2, _sq_2._sq_2_corr_3, _sq_2._sq_2_corr_4, _sq_2._sq_2_corr_5, _sq_2._sq_2_corr_6, _sq_2._sq_2_corr_7));
                        break;
                    }
                }

                if (!statement1_sq_2HashHasMatch)
                {
                    statement1.Add(new Statement1Row0(a_sq_1.a_Name, a_sq_1.a_City, a_sq_1.a_Country, a_sq_1.a_Population, a_sq_1.a_Money, a_sq_1.a_Month, a_sq_1.a_Id, a_sq_1.a_NullableValue, a_sq_1._sq_1__sq_1_key, a_sq_1._sq_1__sq_1_corr_0, a_sq_1._sq_1__sq_1_corr_1, a_sq_1._sq_1__sq_1_corr_2, a_sq_1._sq_1__sq_1_corr_3, a_sq_1._sq_1__sq_1_corr_4, a_sq_1._sq_1__sq_1_corr_5, a_sq_1._sq_1__sq_1_corr_6, a_sq_1._sq_1__sq_1_corr_7, null, null, null, null, null, null, null, null, null));
                }
            }

            return statement1;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement2Row0> BuildCte5(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, CteRowResults _cteRowResults)
        {
            var statement2 = new List<Statement2Row0>(_cteRowResults.Slot4.Count);
            var statement2_sq_3HashEmptyState = new Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.State();
            Musoq.Plugins.CorrelatedScalarSubqueryResult<string> statement2_sq_3HashEmptyValue = (Musoq.Plugins.CorrelatedScalarSubqueryResult<string>)Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.Get(statement2_sq_3HashEmptyState);
            var statement2_sq_3Hash = new Dictionary<object, HashJoinBucket<Cte2Row0>>(_cteRowResults.Slot2.Count);
            var __storedTable2Rows = _cteRowResults.Slot2;
            for (int __storedTable2Index = 0; __storedTable2Index < __storedTable2Rows.Count; ++__storedTable2Index)
            {
                if ((__storedTable2Index & 1023) == 0)
                {
                    token.ThrowIfCancellationRequested();
                }

                Cte2Row0 _sq_3 = __storedTable2Rows[__storedTable2Index];
                object key = (object)Musoq.Evaluator.Helpers.EvaluationHelper.CreateNullableHashJoinKey(_sq_3._sq_3_corr_0, _sq_3._sq_3_corr_1, _sq_3._sq_3_corr_2, _sq_3._sq_3_corr_3, _sq_3._sq_3_corr_4, _sq_3._sq_3_corr_5, _sq_3._sq_3_corr_6, _sq_3._sq_3_corr_7);
                if (key == null)
                    continue;
                {
                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement2_sq_3Hash, key, out var matchesExists);
                    if (!matchesExists)
                    {
                        matches = new HashJoinBucket<Cte2Row0>(_sq_3);
                    }
                    else
                    {
                        matches.Add(_sq_3);
                    }
                }
            }

            var __storedTable4Rows = _cteRowResults.Slot4;
            for (int __storedTable4Index = 0; __storedTable4Index < __storedTable4Rows.Count; ++__storedTable4Index)
            {
                if ((__storedTable4Index & 1023) == 0)
                {
                    token.ThrowIfCancellationRequested();
                }

                Statement1Row0 a_sq_1_sq_2 = __storedTable4Rows[__storedTable4Index];
                bool statement2_sq_3HashHasMatch = false;
                object key = (object)Musoq.Evaluator.Helpers.EvaluationHelper.CreateNullableHashJoinKey(a_sq_1_sq_2.a_Name, a_sq_1_sq_2.a_City, a_sq_1_sq_2.a_Country, a_sq_1_sq_2.a_Population, a_sq_1_sq_2.a_Month, a_sq_1_sq_2.a_Money, a_sq_1_sq_2.a_Id, a_sq_1_sq_2.a_NullableValue);
                if (key != null && statement2_sq_3Hash.TryGetValue(key, out var statement2_sq_3HashMatches))
                {
                    foreach (var _sq_3 in statement2_sq_3HashMatches)
                    {
                        token.ThrowIfCancellationRequested();
                        statement2_sq_3HashHasMatch = true;
                        statement2.Add(new Statement2Row0(a_sq_1_sq_2.a_Name, a_sq_1_sq_2.a_City, a_sq_1_sq_2.a_Country, a_sq_1_sq_2.a_Population, a_sq_1_sq_2.a_Money, a_sq_1_sq_2.a_Month, a_sq_1_sq_2.a_Id, a_sq_1_sq_2.a_NullableValue, a_sq_1_sq_2._sq_1__sq_1_key, a_sq_1_sq_2._sq_1__sq_1_corr_0, a_sq_1_sq_2._sq_1__sq_1_corr_1, a_sq_1_sq_2._sq_1__sq_1_corr_2, a_sq_1_sq_2._sq_1__sq_1_corr_3, a_sq_1_sq_2._sq_1__sq_1_corr_4, a_sq_1_sq_2._sq_1__sq_1_corr_5, a_sq_1_sq_2._sq_1__sq_1_corr_6, a_sq_1_sq_2._sq_1__sq_1_corr_7, a_sq_1_sq_2._sq_2__sq_2_key, a_sq_1_sq_2._sq_2__sq_2_corr_0, a_sq_1_sq_2._sq_2__sq_2_corr_1, a_sq_1_sq_2._sq_2__sq_2_corr_2, a_sq_1_sq_2._sq_2__sq_2_corr_3, a_sq_1_sq_2._sq_2__sq_2_corr_4, a_sq_1_sq_2._sq_2__sq_2_corr_5, a_sq_1_sq_2._sq_2__sq_2_corr_6, a_sq_1_sq_2._sq_2__sq_2_corr_7, _sq_3._sq_3_corr_0, _sq_3._sq_3_corr_1, _sq_3._sq_3_corr_2, _sq_3._sq_3_corr_3, _sq_3._sq_3_corr_4, _sq_3._sq_3_corr_5, _sq_3._sq_3_corr_6, _sq_3._sq_3_corr_7, _sq_3._sq_3_value));
                    }
                }

                if (!statement2_sq_3HashHasMatch)
                {
                    statement2.Add(new Statement2Row0(a_sq_1_sq_2.a_Name, a_sq_1_sq_2.a_City, a_sq_1_sq_2.a_Country, a_sq_1_sq_2.a_Population, a_sq_1_sq_2.a_Money, a_sq_1_sq_2.a_Month, a_sq_1_sq_2.a_Id, a_sq_1_sq_2.a_NullableValue, a_sq_1_sq_2._sq_1__sq_1_key, a_sq_1_sq_2._sq_1__sq_1_corr_0, a_sq_1_sq_2._sq_1__sq_1_corr_1, a_sq_1_sq_2._sq_1__sq_1_corr_2, a_sq_1_sq_2._sq_1__sq_1_corr_3, a_sq_1_sq_2._sq_1__sq_1_corr_4, a_sq_1_sq_2._sq_1__sq_1_corr_5, a_sq_1_sq_2._sq_1__sq_1_corr_6, a_sq_1_sq_2._sq_1__sq_1_corr_7, a_sq_1_sq_2._sq_2__sq_2_key, a_sq_1_sq_2._sq_2__sq_2_corr_0, a_sq_1_sq_2._sq_2__sq_2_corr_1, a_sq_1_sq_2._sq_2__sq_2_corr_2, a_sq_1_sq_2._sq_2__sq_2_corr_3, a_sq_1_sq_2._sq_2__sq_2_corr_4, a_sq_1_sq_2._sq_2__sq_2_corr_5, a_sq_1_sq_2._sq_2__sq_2_corr_6, a_sq_1_sq_2._sq_2__sq_2_corr_7, null, null, null, null, null, null, null, null, statement2_sq_3HashEmptyValue));
                }
            }

            return statement2;
        }

        private sealed class Cte2AggregateGroup
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
            public Cte2AggregateGroup(string __key0, string __key1, string __key2, decimal __key3, string __key4, decimal __key5, int __key6, int? __key7)
            {
                this.__key0 = __key0;
                this.__key1 = __key1;
                this.__key2 = __key2;
                this.__key3 = __key3;
                this.__key4 = __key4;
                this.__key5 = __key5;
                this.__key6 = __key6;
                this.__key7 = __key7;
            }

            public void MergeFrom(Cte2AggregateGroup source)
            {
                Musoq.Plugins.CorrelatedScalarSubqueryAggregateKernel<string>.Merge(ref this.__agg0, in source.__agg0);
            }
        }

        private sealed class Cte2Row0
        {
            public Cte2Row0(string __value0, string __value1, string __value2, decimal __value3, string __value4, decimal __value5, int __value6, int? __value7, Musoq.Plugins.CorrelatedScalarSubqueryResult<string> __value8)
            {
                _sq_3_corr_0 = __value0;
                _sq_3_corr_1 = __value1;
                _sq_3_corr_2 = __value2;
                _sq_3_corr_3 = __value3;
                _sq_3_corr_4 = __value4;
                _sq_3_corr_5 = __value5;
                _sq_3_corr_6 = __value6;
                _sq_3_corr_7 = __value7;
                _sq_3_value = __value8;
            }

            public string _sq_3_corr_0 { get; }
            public string _sq_3_corr_1 { get; }
            public string _sq_3_corr_2 { get; }
            public decimal _sq_3_corr_3 { get; }
            public string _sq_3_corr_4 { get; }
            public decimal _sq_3_corr_5 { get; }
            public int _sq_3_corr_6 { get; }
            public int? _sq_3_corr_7 { get; }
            public Musoq.Plugins.CorrelatedScalarSubqueryResult<string> _sq_3_value { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte2Row0> Slot2;
            public List<Statement0Row0> Slot3;
            public List<Statement1Row0> Slot4;
            public List<Statement2Row0> Slot5;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1, string __value2, string __value3)
            {
                a_Name = __value0;
                ExistsResult = __value1;
                NotExistsResult = __value2;
                Lookup = __value3;
            }

            public override int Count => 4;
            public string ExistsResult { get; private set; }
            public string Lookup { get; private set; }
            public string NotExistsResult { get; private set; }
            public string a_Name { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        a_Name = (string)value;
                        break;
                    case 1:
                        ExistsResult = (string)value;
                        break;
                    case 2:
                        NotExistsResult = (string)value;
                        break;
                    case 3:
                        Lookup = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "a.Name" => true,
                "a_Name" => true,
                "Name" => true,
                "ExistsResult" => true,
                "NotExistsResult" => true,
                "Lookup" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)a_Name,
                1 => (object)ExistsResult,
                2 => (object)NotExistsResult,
                3 => (object)Lookup,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "a.Name" => (object)a_Name,
                "a_Name" => (object)a_Name,
                "Name" => (object)a_Name,
                "ExistsResult" => (object)ExistsResult,
                "NotExistsResult" => (object)NotExistsResult,
                "Lookup" => (object)Lookup,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string a_Name, string ExistsResult, string NotExistsResult, string Lookup)
            {
                this.a_Name = a_Name;
                this.ExistsResult = ExistsResult;
                this.NotExistsResult = NotExistsResult;
                this.Lookup = Lookup;
            }

            public string ExistsResult { get; }
            public string Lookup { get; }
            public string NotExistsResult { get; }
            public string a_Name { get; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(string __value0, string __value1, string __value2, decimal __value3, decimal __value4, string __value5, int __value6, int? __value7, int? __value8, string __value9, string __value10, string __value11, decimal? __value12, string __value13, decimal? __value14, int? __value15, int? __value16)
            {
                a_Name = __value0;
                a_City = __value1;
                a_Country = __value2;
                a_Population = __value3;
                a_Money = __value4;
                a_Month = __value5;
                a_Id = __value6;
                a_NullableValue = __value7;
                _sq_1__sq_1_key = __value8;
                _sq_1__sq_1_corr_0 = __value9;
                _sq_1__sq_1_corr_1 = __value10;
                _sq_1__sq_1_corr_2 = __value11;
                _sq_1__sq_1_corr_3 = __value12;
                _sq_1__sq_1_corr_4 = __value13;
                _sq_1__sq_1_corr_5 = __value14;
                _sq_1__sq_1_corr_6 = __value15;
                _sq_1__sq_1_corr_7 = __value16;
            }

            public string _sq_1__sq_1_corr_0 { get; }
            public string _sq_1__sq_1_corr_1 { get; }
            public string _sq_1__sq_1_corr_2 { get; }
            public decimal? _sq_1__sq_1_corr_3 { get; }
            public string _sq_1__sq_1_corr_4 { get; }
            public decimal? _sq_1__sq_1_corr_5 { get; }
            public int? _sq_1__sq_1_corr_6 { get; }
            public int? _sq_1__sq_1_corr_7 { get; }
            public int? _sq_1__sq_1_key { get; }
            public string a_City { get; }
            public string a_Country { get; }
            public int a_Id { get; }
            public decimal a_Money { get; }
            public string a_Month { get; }
            public string a_Name { get; }
            public int? a_NullableValue { get; }
            public decimal a_Population { get; }
        }

        private sealed class Statement1Row0
        {
            public Statement1Row0(string __value0, string __value1, string __value2, decimal __value3, decimal __value4, string __value5, int __value6, int? __value7, int? __value8, string __value9, string __value10, string __value11, decimal? __value12, string __value13, decimal? __value14, int? __value15, int? __value16, int? __value17, string __value18, string __value19, string __value20, decimal? __value21, string __value22, decimal? __value23, int? __value24, int? __value25)
            {
                a_Name = __value0;
                a_City = __value1;
                a_Country = __value2;
                a_Population = __value3;
                a_Money = __value4;
                a_Month = __value5;
                a_Id = __value6;
                a_NullableValue = __value7;
                _sq_1__sq_1_key = __value8;
                _sq_1__sq_1_corr_0 = __value9;
                _sq_1__sq_1_corr_1 = __value10;
                _sq_1__sq_1_corr_2 = __value11;
                _sq_1__sq_1_corr_3 = __value12;
                _sq_1__sq_1_corr_4 = __value13;
                _sq_1__sq_1_corr_5 = __value14;
                _sq_1__sq_1_corr_6 = __value15;
                _sq_1__sq_1_corr_7 = __value16;
                _sq_2__sq_2_key = __value17;
                _sq_2__sq_2_corr_0 = __value18;
                _sq_2__sq_2_corr_1 = __value19;
                _sq_2__sq_2_corr_2 = __value20;
                _sq_2__sq_2_corr_3 = __value21;
                _sq_2__sq_2_corr_4 = __value22;
                _sq_2__sq_2_corr_5 = __value23;
                _sq_2__sq_2_corr_6 = __value24;
                _sq_2__sq_2_corr_7 = __value25;
            }

            public string _sq_1__sq_1_corr_0 { get; }
            public string _sq_1__sq_1_corr_1 { get; }
            public string _sq_1__sq_1_corr_2 { get; }
            public decimal? _sq_1__sq_1_corr_3 { get; }
            public string _sq_1__sq_1_corr_4 { get; }
            public decimal? _sq_1__sq_1_corr_5 { get; }
            public int? _sq_1__sq_1_corr_6 { get; }
            public int? _sq_1__sq_1_corr_7 { get; }
            public int? _sq_1__sq_1_key { get; }
            public string _sq_2__sq_2_corr_0 { get; }
            public string _sq_2__sq_2_corr_1 { get; }
            public string _sq_2__sq_2_corr_2 { get; }
            public decimal? _sq_2__sq_2_corr_3 { get; }
            public string _sq_2__sq_2_corr_4 { get; }
            public decimal? _sq_2__sq_2_corr_5 { get; }
            public int? _sq_2__sq_2_corr_6 { get; }
            public int? _sq_2__sq_2_corr_7 { get; }
            public int? _sq_2__sq_2_key { get; }
            public string a_City { get; }
            public string a_Country { get; }
            public int a_Id { get; }
            public decimal a_Money { get; }
            public string a_Month { get; }
            public string a_Name { get; }
            public int? a_NullableValue { get; }
            public decimal a_Population { get; }
        }

        private sealed class Statement2Row0
        {
            public Statement2Row0(string __value0, string __value1, string __value2, decimal __value3, decimal __value4, string __value5, int __value6, int? __value7, int? __value8, string __value9, string __value10, string __value11, decimal? __value12, string __value13, decimal? __value14, int? __value15, int? __value16, int? __value17, string __value18, string __value19, string __value20, decimal? __value21, string __value22, decimal? __value23, int? __value24, int? __value25, string __value26, string __value27, string __value28, decimal? __value29, string __value30, decimal? __value31, int? __value32, int? __value33, Musoq.Plugins.CorrelatedScalarSubqueryResult<string> __value34)
            {
                a_Name = __value0;
                a_City = __value1;
                a_Country = __value2;
                a_Population = __value3;
                a_Money = __value4;
                a_Month = __value5;
                a_Id = __value6;
                a_NullableValue = __value7;
                _sq_1__sq_1_key = __value8;
                _sq_1__sq_1_corr_0 = __value9;
                _sq_1__sq_1_corr_1 = __value10;
                _sq_1__sq_1_corr_2 = __value11;
                _sq_1__sq_1_corr_3 = __value12;
                _sq_1__sq_1_corr_4 = __value13;
                _sq_1__sq_1_corr_5 = __value14;
                _sq_1__sq_1_corr_6 = __value15;
                _sq_1__sq_1_corr_7 = __value16;
                _sq_2__sq_2_key = __value17;
                _sq_2__sq_2_corr_0 = __value18;
                _sq_2__sq_2_corr_1 = __value19;
                _sq_2__sq_2_corr_2 = __value20;
                _sq_2__sq_2_corr_3 = __value21;
                _sq_2__sq_2_corr_4 = __value22;
                _sq_2__sq_2_corr_5 = __value23;
                _sq_2__sq_2_corr_6 = __value24;
                _sq_2__sq_2_corr_7 = __value25;
                _sq_3_corr_0 = __value26;
                _sq_3_corr_1 = __value27;
                _sq_3_corr_2 = __value28;
                _sq_3_corr_3 = __value29;
                _sq_3_corr_4 = __value30;
                _sq_3_corr_5 = __value31;
                _sq_3_corr_6 = __value32;
                _sq_3_corr_7 = __value33;
                _sq_3_value = __value34;
            }

            public string _sq_1__sq_1_corr_0 { get; }
            public string _sq_1__sq_1_corr_1 { get; }
            public string _sq_1__sq_1_corr_2 { get; }
            public decimal? _sq_1__sq_1_corr_3 { get; }
            public string _sq_1__sq_1_corr_4 { get; }
            public decimal? _sq_1__sq_1_corr_5 { get; }
            public int? _sq_1__sq_1_corr_6 { get; }
            public int? _sq_1__sq_1_corr_7 { get; }
            public int? _sq_1__sq_1_key { get; }
            public string _sq_2__sq_2_corr_0 { get; }
            public string _sq_2__sq_2_corr_1 { get; }
            public string _sq_2__sq_2_corr_2 { get; }
            public decimal? _sq_2__sq_2_corr_3 { get; }
            public string _sq_2__sq_2_corr_4 { get; }
            public decimal? _sq_2__sq_2_corr_5 { get; }
            public int? _sq_2__sq_2_corr_6 { get; }
            public int? _sq_2__sq_2_corr_7 { get; }
            public int? _sq_2__sq_2_key { get; }
            public string _sq_3_corr_0 { get; }
            public string _sq_3_corr_1 { get; }
            public string _sq_3_corr_2 { get; }
            public decimal? _sq_3_corr_3 { get; }
            public string _sq_3_corr_4 { get; }
            public decimal? _sq_3_corr_5 { get; }
            public int? _sq_3_corr_6 { get; }
            public int? _sq_3_corr_7 { get; }
            public Musoq.Plugins.CorrelatedScalarSubqueryResult<string> _sq_3_value { get; }
            public string a_City { get; }
            public string a_Country { get; }
            public int a_Id { get; }
            public decimal a_Money { get; }
            public string a_Month { get; }
            public string a_Name { get; }
            public int? a_NullableValue { get; }
            public decimal a_Population { get; }
        }

        private readonly struct _sq_1HashPayload0
        {
            public readonly int _sq_1_key;
            public readonly string _sq_1_corr_0;
            public readonly string _sq_1_corr_1;
            public readonly string _sq_1_corr_2;
            public readonly decimal _sq_1_corr_3;
            public readonly string _sq_1_corr_4;
            public readonly decimal _sq_1_corr_5;
            public readonly int _sq_1_corr_6;
            public readonly int? _sq_1_corr_7;
            public _sq_1HashPayload0(int _sq_1_key, string _sq_1_corr_0, string _sq_1_corr_1, string _sq_1_corr_2, decimal _sq_1_corr_3, string _sq_1_corr_4, decimal _sq_1_corr_5, int _sq_1_corr_6, int? _sq_1_corr_7)
            {
                this._sq_1_key = _sq_1_key;
                this._sq_1_corr_0 = _sq_1_corr_0;
                this._sq_1_corr_1 = _sq_1_corr_1;
                this._sq_1_corr_2 = _sq_1_corr_2;
                this._sq_1_corr_3 = _sq_1_corr_3;
                this._sq_1_corr_4 = _sq_1_corr_4;
                this._sq_1_corr_5 = _sq_1_corr_5;
                this._sq_1_corr_6 = _sq_1_corr_6;
                this._sq_1_corr_7 = _sq_1_corr_7;
            }
        }

        private readonly struct _sq_2HashPayload0
        {
            public readonly int _sq_2_key;
            public readonly string _sq_2_corr_0;
            public readonly string _sq_2_corr_1;
            public readonly string _sq_2_corr_2;
            public readonly decimal _sq_2_corr_3;
            public readonly string _sq_2_corr_4;
            public readonly decimal _sq_2_corr_5;
            public readonly int _sq_2_corr_6;
            public readonly int? _sq_2_corr_7;
            public _sq_2HashPayload0(int _sq_2_key, string _sq_2_corr_0, string _sq_2_corr_1, string _sq_2_corr_2, decimal _sq_2_corr_3, string _sq_2_corr_4, decimal _sq_2_corr_5, int _sq_2_corr_6, int? _sq_2_corr_7)
            {
                this._sq_2_key = _sq_2_key;
                this._sq_2_corr_0 = _sq_2_corr_0;
                this._sq_2_corr_1 = _sq_2_corr_1;
                this._sq_2_corr_2 = _sq_2_corr_2;
                this._sq_2_corr_3 = _sq_2_corr_3;
                this._sq_2_corr_4 = _sq_2_corr_4;
                this._sq_2_corr_5 = _sq_2_corr_5;
                this._sq_2_corr_6 = _sq_2_corr_6;
                this._sq_2_corr_7 = _sq_2_corr_7;
            }
        }
    }
}
