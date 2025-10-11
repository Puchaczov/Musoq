# Invalid Query Error Handling Tests

This document describes the comprehensive test coverage for invalid SQL queries and the meaningful error messages they produce.

## Overview

The Musoq query engine includes robust error handling to provide clear, actionable error messages when queries are invalid. We have created comprehensive test suites to validate that all error scenarios produce meaningful error messages.

## Test Files

### 1. Parser-Level Syntax Tests
**File:** `Musoq.Parser.Tests/InvalidQuerySyntaxTests.cs`
**Test Count:** 21 tests
**Purpose:** Validate that syntax errors in SQL queries are caught during parsing and produce meaningful error messages.

#### Test Coverage

| Test Name | Invalid Query Pattern | Error Validated |
|-----------|----------------------|-----------------|
| `MissingFromClause_ShouldThrowMeaningfulError` | `select 1, 2, 3` | Missing FROM clause |
| `MissingSelectKeyword_ShouldThrowMeaningfulError` | `from #some.table()` | Missing SELECT keyword |
| `InvalidJoinWithoutOnClause_ShouldThrowMeaningfulError` | `select a.Name from #some.a() a inner join #some.b() b` | JOIN without ON condition |
| `UnclosedParenthesesInSelectList_ShouldThrowMeaningfulError` | `select (1 + 2 from #some.a()` | Unclosed parentheses |
| `UnclosedParenthesesInFunctionCall_ShouldThrowMeaningfulError` | `select ABS(5 from #some.a()` | Unclosed function call |
| `InvalidWhereClauseMissingCondition_ShouldThrowMeaningfulError` | `select Name from #some.a() where` | WHERE without condition |
| `InvalidGroupByMissingColumn_ShouldThrowMeaningfulError` | `select Name from #some.a() group by` | GROUP BY without column |
| `InvalidOrderByMissingColumn_ShouldThrowMeaningfulError` | `select Name from #some.a() order by` | ORDER BY without column |
| `InvalidCaseWhenMissingThen_ShouldThrowMeaningfulError` | `select case when 1 = 1 else 0 end from #some.a()` | CASE without THEN |
| `MultipleCommasInSelectList_ShouldThrowMeaningfulError` | `select 1,, 2 from #some.a()` | Multiple consecutive commas |
| `TrailingCommaInGroupBy_ShouldThrowMeaningfulError` | `select Name, Count(*) from #some.a() group by Name,` | Trailing comma |
| `InvalidHavingWithoutGroupBy_ShouldThrowMeaningfulError` | `select Name from #some.a() having Count(*) > 5` | HAVING without GROUP BY |
| `InvalidUnionMissingSideOfQuery_ShouldThrowMeaningfulError` | `select 1 from #some.a() union` | Incomplete UNION |
| `InvalidExceptMissingSideOfQuery_ShouldThrowMeaningfulError` | `select 1 from #some.a() except (Column)` | Incomplete EXCEPT |
| `InvalidTableReferenceSyntax_ShouldThrowMeaningfulError` | `select Name from #some.` | Incomplete table reference |
| `InvalidCTESyntaxMissingAsKeyword_ShouldThrowMeaningfulError` | `with cte (select 1) select * from cte` | CTE without AS |
| `InvalidSelectListSyntax_ShouldThrowMeaningfulError` | `select * * from #some.a()` | Invalid SELECT syntax |
| `InvalidSubqueryMissingParentheses_ShouldThrowMeaningfulError` | `select Name from select 1 from #some.a()` | Subquery without parentheses |
| `DoubleNegationWithoutParentheses_ShouldThrowMeaningfulError` | `select --5 from #some.a()` | Double negation syntax |
| `InvalidInOperatorWithoutList_ShouldThrowMeaningfulError` | `select Name from #some.a() where Id in` | IN without list |
| `InvalidBetweenOperatorMissingAnd_ShouldThrowMeaningfulError` | `select Name from #some.a() where Id between 1 5` | BETWEEN without AND |

### 2. Evaluator-Level Semantic/Runtime Tests
**File:** `Musoq.Evaluator.Tests/InvalidQueryEvaluationTests.cs`
**Test Count:** 20 tests
**Purpose:** Validate that semantic and runtime errors produce meaningful error messages with specific details about the problem.

#### Test Coverage

| Test Name | Invalid Query Pattern | Error Validated |
|-----------|----------------------|-----------------|
| `NonExistentColumn_ShouldThrowMeaningfulError` | `select NonExistentColumn from #A.Entities()` | `UnknownColumnOrAliasException` - mentions column name |
| `NonExistentFunction_ShouldThrowMeaningfulError` | `select NonExistentFunction(Name) from #A.Entities()` | Method resolution error - mentions function name |
| `AmbiguousColumnInJoin_ShouldThrowMeaningfulError` | `select Name from #A.Entities() a inner join #B.Entities() b on a.Name = b.Name` | `AmbiguousColumnException` - mentions ambiguous column |
| `InvalidPropertyAccessOnNull_ShouldThrowMeaningfulError` | `select Self.Other.SomeProperty from #A.Entities()` | Property access error - mentions property name |
| `WrongNumberOfFunctionArguments_ShouldThrowMeaningfulError` | `select Concat(Name) from #A.Entities()` | Method resolution error - argument count mismatch |
| `AggregateWithoutGroupByInvalidContext_ShouldThrowMeaningfulError` | `select Name, Count(*) from #A.Entities()` | Aggregate function misuse error |
| `InvalidColumnInGroupBy_ShouldThrowMeaningfulError` | `select Name from #A.Entities() group by NonExistentColumn` | Unknown column in GROUP BY |
| `InvalidColumnInHaving_ShouldThrowMeaningfulError` | `select Name from #A.Entities() group by Name having NonExistentColumn > 5` | Unknown column in HAVING |
| `InvalidColumnInOrderBy_ShouldThrowMeaningfulError` | `select Name from #A.Entities() order by NonExistentColumn` | Unknown column in ORDER BY |
| `SetOperatorColumnCountMismatch_ShouldThrowMeaningfulError` | `select Name from #A.Entities() union (Name) select Name, City from #B.Entities()` | Column count mismatch in set operation |
| `InvalidAliasReference_ShouldThrowMeaningfulError` | `select Name from #A.Entities() where NonExistentAlias = 'test'` | Unknown alias reference |
| `DivisionByZeroLiteral_ShouldThrowMeaningfulError` | `select 10 / 0 from #A.Entities()` | Division by zero error |
| `InvalidTypeInArithmeticOperation_ShouldThrowMeaningfulError` | `select Name + 10 from #A.Entities()` | Type mismatch in arithmetic |
| `InvalidComparisonBetweenIncompatibleTypes_ShouldThrowMeaningfulError` | `select Name from #A.Entities() where Name > 100` | Type mismatch in comparison |
| `NonExistentSchemaTable_ShouldThrowMeaningfulError` | `select Name from #NonExistent.table()` | Schema not found - mentions schema name |
| `InvalidJoinConditionType_ShouldThrowMeaningfulError` | `select a.Name from #A.Entities() a inner join #B.Entities() b on a.Name + b.Name` | Invalid join condition type |
| `SelfJoinWithoutAlias_ShouldThrowMeaningfulError` | `select Name from #A.Entities() inner join #A.Entities() on Name = Name` | Self-join requires aliases |
| `InvalidCastSyntax_ShouldThrowMeaningfulError` | `select ToInt(Name) from #A.Entities()` | Invalid cast/conversion |
| `NestedAggregatesNotAllowed_ShouldThrowMeaningfulError` | `select Count(Sum(Population)) from #A.Entities() group by City` | Nested aggregate functions |
| `InvalidCTEReference_ShouldThrowMeaningfulError` | `with cte as (select Name from #A.Entities()) select * from NonExistentCTE` | CTE reference not found |

## Error Message Quality

All tests validate that:

1. **An exception is thrown** - Invalid queries do not silently fail
2. **Error messages are non-empty** - Every error has a message
3. **Error messages are meaningful** - Messages contain:
   - The specific element that caused the error (column name, function name, etc.)
   - Context about where the error occurred
   - Guidance about what went wrong

## Example Error Messages

### Parser Errors
```
SyntaxException: "Expected token is FROM but received <end of query>"
Query Part: "select 1 form #some."
```

### Evaluator Errors
```
UnknownColumnOrAliasException: "Column or Alias NonExistentColumn could not be found."

CannotResolveMethodException: "Method NonExistentFunction with argument types String cannot be resolved"

AmbiguousColumnException: "Column 'Name' is ambiguous. It exists in multiple tables."
```

## Test Results

- **Parser Tests:** 181 total (21 new invalid query tests)
- **Evaluator Tests:** 1,498 total (20 new invalid query tests)
- **All tests passing:** ✅ 100% pass rate

## Usage

Run the invalid query tests:

```bash
# Run all Parser invalid query tests
dotnet test Musoq.Parser.Tests --filter "FullyQualifiedName~InvalidQuerySyntaxTests"

# Run all Evaluator invalid query tests
dotnet test Musoq.Evaluator.Tests --filter "FullyQualifiedName~InvalidQueryEvaluationTests"

# Run both test suites
dotnet test Musoq.Parser.Tests Musoq.Evaluator.Tests --filter "FullyQualifiedName~InvalidQuery"
```

## Benefits

1. **Developer Experience:** Clear error messages help developers quickly identify and fix query issues
2. **Regression Protection:** Tests ensure error messages remain helpful as the codebase evolves
3. **Documentation:** Test names and patterns serve as documentation of invalid query scenarios
4. **Code Quality:** Forces the engine to handle edge cases gracefully

## Future Enhancements

Potential areas for additional test coverage:
- Schema-level validation errors (method signature mismatches, etc.)
- Performance-related errors (query too complex, timeout scenarios)
- Data source connection errors
- Permission/authorization errors
- More complex nested query error scenarios
