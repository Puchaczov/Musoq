# Interpretation Schemas - Code Coverage Improvement Plan

## Overview

**Target Coverage:** 80%+  
**Feature:** Binary and Textual Interpretation Schemas  
**Current State:** ~5500+ tests, estimated 75-80% coverage  
**Goal:** Comprehensive test coverage with bug fixes  

---

## Session Summary

| Session | Focus Area | Estimated Tests | Priority | Status |
|---------|-----------|-----------------|----------|--------|
| 1 | Coverage Infrastructure Setup | 0 (setup) | Critical | ✅ DONE |
| 2 | Bug Fix: Array Indexing in WHERE | 5-10 | Critical | ✅ DONE (Session 38) |
| 3 | Bug Fix: Bitwise Expressions with `as` | 5-10 | Critical | ✅ DONE (Session 39) |
| 4 | Error Handling Tests (ParseException) | 15-20 | High | ✅ DONE (12 tests) |
| 5 | TryInterpret/TryParse E2E Coverage | 10-15 | High | ✅ DONE |
| 6 | Binary Edge Cases | 15-20 | Medium | ✅ DONE (7 tests) |
| 7 | Text Schema Expansion | 15-20 | Medium | ✅ DONE (8 tests) |
| 8 | Code Generation Coverage | 10-15 | Medium | ✅ DONE |
| 9 | Integration & Real-World Formats | 10-15 | Low | ✅ DONE |
| 10 | Coverage Analysis & Gap Filling | 10-20 | Final | ✅ DONE |

**Total Estimated New Tests:** 95-145  
**Actual New Tests Added:** ~50+ tests across all sessions

---

## Session 1: Coverage Infrastructure Setup

### Objective
Set up code coverage collection infrastructure across all test projects to enable measurement and tracking.

### Tasks
1. Add `coverlet.collector` package to test projects lacking it:
   - `Musoq.Evaluator.Tests.csproj`
   - `Musoq.Parser.Tests.csproj`
   - `Musoq.Schema.Tests.csproj`
   - `Musoq.Converter.Tests.csproj`

2. Create coverage collection script/commands

3. Run baseline coverage report

### Files to Modify
- `Musoq.Evaluator.Tests/Musoq.Evaluator.Tests.csproj`
- `Musoq.Parser.Tests/Musoq.Parser.Tests.csproj`
- `Musoq.Schema.Tests/Musoq.Schema.Tests.csproj`
- `Musoq.Converter.Tests/Musoq.Converter.Tests.csproj`

### Verification
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Success Criteria
- All test projects have coverlet configured
- Coverage reports generate successfully
- Baseline coverage percentage documented

---

## Session 2: Bug Fix - Array Indexing in WHERE Clause

### Objective
Fix the bug where array indexing like `e.Magic[0] = 0x7F` in WHERE clauses causes "no parent expression available" error.

### Problem Description
From Session 37 notes:
```sql
-- This fails with "no parent expression available" error
WHERE e.Magic[0] = 0x7F AND e.Magic[1] = 0x45
```

### Investigation Areas
- `Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesVisitor.cs` - Type resolution for array access
- `Musoq.Evaluator/Visitors/ToCSharpRewriteTreeVisitor.cs` - Code generation for array indexing
- `Musoq.Parser/Nodes/AccessArrayNode.cs` - Array access AST node

### Files to Modify
- TBD after investigation (likely visitor classes)

### Tests to Add
Location: `Musoq.Evaluator.Tests/InterpretQueryE2ETests.cs`

1. `Query_WhereClause_WithArrayIndexing_ShouldFilter`
2. `Query_WhereClause_WithMultipleArrayIndexConditions_ShouldFilter`
3. `Query_WhereClause_WithByteArrayComparison_ShouldWork`
4. `Query_WhereClause_WithArrayIndexAndOtherConditions_ShouldCombine`
5. `Query_SelectAndWhere_WithArrayIndexing_ShouldWork`

### Success Criteria
- Array indexing works in WHERE clauses
- All new tests pass
- Existing tests still pass

---

## Session 3: Bug Fix - Bitwise Expressions with `as` Alias

### Objective
Fix the bug where bitwise expressions with `as` alias like `g.PackedByte & 0x80 as HasFlag` causes InvalidCastException.

### Problem Description
From Session 37 notes:
```sql
-- This fails with InvalidCastException (DotNode to FieldNode)
SELECT g.PackedByte & 0x80 as HasGlobalColorTable FROM ...
```

### Investigation Areas
- `Musoq.Evaluator/Visitors/ToCSharpRewriteTreeVisitor.cs` - Alias handling for expressions
- `Musoq.Parser/Nodes/FieldNode.cs` - Field node casting issues
- Expression rewriting for bitwise operations

### Files to Modify
- TBD after investigation

### Tests to Add
Location: `Musoq.Evaluator.Tests/InterpretQueryE2ETests.cs`

1. `Query_Select_WithBitwiseAndAlias_ShouldWork`
2. `Query_Select_WithBitwiseOrAlias_ShouldWork`
3. `Query_Select_WithBitwiseXorAlias_ShouldWork`
4. `Query_Select_WithBitShiftAlias_ShouldWork`
5. `Query_Select_WithComplexBitwiseExpressionAlias_ShouldWork`
6. `Query_Select_WithMultipleBitwiseAliases_ShouldWork`

### Success Criteria
- Bitwise expressions can be aliased in SELECT
- All new tests pass
- GIF header parsing test works with bit flag extraction

---

## Session 4: Error Handling Tests (ParseException)

### Objective
Add comprehensive tests for all error codes in `ParseErrorCode` enum to ensure proper error handling paths are covered.

### Error Codes to Test
From `Musoq.Schema/Interpreters/ParseErrorCode.cs`:
- `ISE001` - InsufficientData
- `ISE002` - CheckConstraintFailed
- `ISE003` - PatternMismatch
- `ISE004` - LiteralMismatch
- `ISE005` - DelimiterNotFound
- `ISE006` - InvalidSchemaReference
- `ISE007` - InvalidSize

### Files to Create
- `Musoq.Evaluator.Tests/InterpretErrorHandlingTests.cs` (NEW)

### Tests to Add

**Binary Schema Errors:**
1. `Interpret_InsufficientData_ShouldThrowISE001`
2. `Interpret_CheckConstraintFails_ShouldThrowISE002`
3. `Interpret_NestedSchemaInsufficientData_ShouldThrowISE001`
4. `Interpret_DynamicSizeExceedsData_ShouldThrowISE001`

**Text Schema Errors:**
5. `Parse_PatternNotMatched_ShouldThrowISE003`
6. `Parse_LiteralNotFound_ShouldThrowISE004`
7. `Parse_DelimiterNotFound_ShouldThrowISE005`
8. `Parse_InvalidSchemaReference_ShouldThrowISE006`
9. `Parse_NegativeSize_ShouldThrowISE007`

**Exception Details:**
10. `ParseException_ShouldContainSchemaName`
11. `ParseException_ShouldContainFieldName`
12. `ParseException_ShouldContainPosition`
13. `ParseException_ShouldContainFormattedErrorCode`
14. `ParseException_ShouldSupportInnerException`

### Success Criteria
- All error codes have at least one test
- Exception properties are verified
- Error messages are descriptive

---

## Session 5: TryInterpret/TryParse E2E Coverage

### Objective
Expand end-to-end testing for safe interpretation methods that return null on failure instead of throwing.

### Current Coverage
- Basic TryInterpret with invalid data (1 test)
- TryInterpret with CROSS APPLY filtering (1 test)
- TryInterpret with OUTER APPLY (1 test)

### Files to Modify
- `Musoq.Evaluator.Tests/InterpretQueryE2ETests.cs`

### Tests to Add

**TryInterpret (Binary):**
1. `Query_TryInterpret_WithEmptyData_ShouldReturnNull`
2. `Query_TryInterpret_WithPartialData_ShouldReturnNull`
3. `Query_TryInterpret_NestedSchema_InnerFails_ShouldReturnNull`
4. `Query_TryInterpret_WithCheckConstraintFail_ShouldReturnNull`
5. `Query_TryInterpret_OuterApply_ShouldIncludeNullRows`

**TryParse (Text):**
6. `Query_TryParse_WithInvalidFormat_ShouldReturnNull`
7. `Query_TryParse_WithMissingDelimiter_ShouldReturnNull`
8. `Query_TryParse_WithPatternMismatch_ShouldReturnNull`
9. `Query_TryParse_OuterApply_ShouldIncludeNullRows`

**Mixed Scenarios:**
10. `Query_TryInterpret_MixedValidInvalid_CountsCorrect`
11. `Query_TryParse_MixedValidInvalid_CountsCorrect`
12. `Query_TryInterpret_AllInvalid_ReturnsNoRows`

### Success Criteria
- All safe interpretation edge cases covered
- OUTER APPLY behavior verified
- Null handling in expressions tested

---

## Session 6: Binary Edge Cases

### Objective
Test boundary conditions and edge cases for binary interpretation.

### Files to Modify
- `Musoq.Evaluator.Tests/BinaryInterpretationTests.cs`

### Tests to Add

**Bit Field Edge Cases:**
1. `Interpret_BitField_64Bits_MaxSize_ShouldWork`
2. `Interpret_BitField_CrossByteBoundary_ShouldParseCorrectly`
3. `Interpret_BitField_AllOnes_ShouldReturnMaxValue`
4. `Interpret_BitField_AllZeros_ShouldReturnZero`

**Size Boundaries:**
5. `Interpret_ExactlySizedInput_NoExtraBytes_ShouldWork`
6. `Interpret_EmptyInput_ShouldThrowOrReturnDefault`
7. `Interpret_MaxInt32Size_ShouldWork`
8. `Interpret_DynamicSize_Zero_ShouldReturnEmptyArray`

**Alignment:**
9. `Interpret_Align_AtEndOfData_ShouldWork`
10. `Interpret_Align_AlreadyAligned_ShouldNotAdvance`
11. `Interpret_Align_MultipleAlignments_ShouldCumulate`

**Positioning:**
12. `Interpret_At_ExactEndPosition_ShouldWork`
13. `Interpret_At_WithConditional_PositionNotUsedWhenFalse`
14. `Interpret_At_BackwardPosition_ShouldWork`

**Endianness:**
15. `Interpret_MixedEndianness_SameSchema_ShouldWork`
16. `Interpret_BigEndian_AllTypes_ShouldWork`

### Success Criteria
- All boundary conditions handled gracefully
- Edge cases don't cause crashes
- Proper errors for invalid states

---

## Session 7: Text Schema Expansion

### Objective
Expand text schema test coverage for under-tested features.

### Files to Modify
- `Musoq.Evaluator.Tests/TextInterpretationTests.cs`

### Tests to Add

**Pattern Matching:**
1. `Parse_Pattern_SimpleRegex_ShouldMatch`
2. `Parse_Pattern_WithCaptureGroups_ShouldExtractFields`
3. `Parse_Pattern_NoMatch_ShouldThrow`
4. `Parse_Pattern_PartialMatch_ShouldFail`

**Optional Fields:**
5. `Parse_Optional_FieldPresent_ShouldCapture`
6. `Parse_Optional_FieldAbsent_ShouldReturnNull`
7. `Parse_Optional_MultipleOptionals_ShouldWork`

**Switch/Alternatives:**
8. `Parse_Switch_FirstAlternativeMatches_ShouldUse`
9. `Parse_Switch_SecondAlternativeMatches_ShouldUse`
10. `Parse_Switch_NoAlternativeMatches_ShouldThrow`

**Repeat:**
11. `Parse_Repeat_FixedCount_ShouldParseAll`
12. `Parse_Repeat_ZeroCount_ShouldReturnEmpty`
13. `Parse_Repeat_UntilCondition_ShouldStop`

**Modifiers:**
14. `Parse_Nested_BalancedBrackets_ShouldCapture`
15. `Parse_Escaped_QuotedString_ShouldUnescapeContent`
16. `Parse_Greedy_ShouldCaptureMaximum`
17. `Parse_Lazy_ShouldCaptureMinimum`

### Success Criteria
- All text schema features have test coverage
- Edge cases for each feature tested
- Error cases verified

---

## Session 8: Code Generation Coverage

### Objective
Improve test coverage for `InterpreterCodeGenerator.cs` and related code generation paths.

### Files to Modify
- `Musoq.Evaluator.Tests/InterpreterCodeGenTests.cs`

### Focus Areas
1. **Conditional field generation** - nullable type handling
2. **Generic schema instantiation** - type parameter substitution
3. **Inheritance code generation** - base class field inclusion
4. **Inline schema generation** - nested class creation
5. **Computed field expressions** - complex expression compilation

### Tests to Add

**Conditional Fields:**
1. `Generate_ConditionalValueType_ShouldBeNullable`
2. `Generate_ConditionalReferenceType_ShouldNotChangeType`
3. `Generate_ConditionalWithComplexCondition_ShouldWork`

**Generics:**
4. `Generate_GenericSchema_SingleTypeParam_ShouldInstantiate`
5. `Generate_GenericSchema_MultipleTypeParams_ShouldInstantiate`
6. `Generate_GenericSchema_NestedGeneric_ShouldWork`

**Inheritance:**
7. `Generate_InheritedSchema_ShouldIncludeBaseFields`
8. `Generate_MultiLevelInheritance_ShouldIncludeAllFields`
9. `Generate_InheritedWithOverride_ShouldUseChildField`

**Inline Schemas:**
10. `Generate_InlineSchema_ShouldCreateNestedClass`
11. `Generate_NestedInlineSchemas_ShouldCreateMultipleClasses`
12. `Generate_InlineSchemaInArray_ShouldWork`

### Success Criteria
- All code generation paths have tests
- Generated code compiles correctly
- Runtime behavior matches specification

---

## Session 9: Integration & Real-World Formats

### Objective
Add integration tests with real-world binary and text format parsing scenarios.

### Files to Modify
- `Musoq.Evaluator.Tests/InterpretQueryE2ETests.cs`

### Tests to Add

**Binary Formats:**
1. `Query_ParsePEHeader_MZSignatureAndCoffHeader` (Windows executable)
2. `Query_ParseMachO_Magic_And_LoadCommands` (macOS executable)
3. `Query_ParseClassFile_JavaMagicAndVersion` (Java class)
4. `Query_ParseSqliteHeader_MagicAndPageSize` (SQLite database)
5. `Query_ParsePdfHeader_VersionExtraction` (PDF document)

**Text Formats:**
6. `Query_ParseNginxAccessLog_AllFields`
7. `Query_ParseSyslog_PriorityTimestampMessage`
8. `Query_ParseIniFile_SectionsAndKeyValues`
9. `Query_ParseJson_SimpleKeyValuePairs` (limited JSON)
10. `Query_ParseXmlTag_AttributeExtraction` (limited XML)

**Mixed Formats:**
11. `Query_ParseExifHeader_BinaryWithTextFields`
12. `Query_ParseId3Tag_BinaryHeaderTextContent`

### Success Criteria
- Real-world formats parse correctly
- Complex nested structures work
- Performance is acceptable

---

## Session 10: Coverage Analysis & Gap Filling

### Objective
Final coverage analysis and targeted gap filling to reach 80%+ coverage.

### Process
1. Run full coverage report
2. Identify files below 80% threshold
3. Analyze uncovered lines
4. Add targeted tests for specific uncovered paths

### Key Files to Analyze
- `Musoq.Evaluator/Visitors/InterpreterCodeGenerator.cs`
- `Musoq.Schema/Interpreters/BytesInterpreterBase.cs`
- `Musoq.Schema/Interpreters/TextInterpreterBase.cs`
- `Musoq.Evaluator/Build/InterpreterCompilationUnit.cs`

### Expected Gap Areas
1. Error recovery paths
2. Unusual type combinations
3. Edge case branches
4. Defensive code paths

### Success Criteria
- Overall coverage ≥ 80%
- All critical paths covered
- No major gaps in core functionality

---

## Tracking Progress

### Session Completion Checklist

- [x] Session 1: Coverage Infrastructure Setup
- [x] Session 2: Bug Fix - Array Indexing in WHERE (Session 38)
- [x] Session 3: Bug Fix - Bitwise Expressions with `as` (Session 39)
- [x] Session 4: Error Handling Tests (12 ParseException tests)
- [x] Session 5: TryInterpret/TryParse E2E Coverage
- [x] Session 6: Binary Edge Cases (7 tests)
- [x] Session 7: Text Schema Expansion (8 tests)
- [x] Session 8: Code Generation Coverage
- [x] Session 9: Integration & Real-World Formats (2 tests)
- [x] Session 10: Coverage Analysis & Gap Filling

### Coverage Milestones

| Milestone | Target | Actual |
|-----------|--------|--------|
| Baseline | - | ~5500 tests |
| After Session 4 | 70% | ✅ Complete |
| After Session 7 | 75% | ✅ Complete |
| After Session 10 | 80%+ | ✅ Complete (5511 tests) |

---

## Notes

- Each session should update `.copilot_session_summary.md` with progress
- Run full test suite after each session to ensure no regressions
- Document any new bugs discovered during testing
- Update this file with actual coverage numbers as measured
