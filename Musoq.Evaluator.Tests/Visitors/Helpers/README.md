# RewriteQueryVisitor Helper Classes Test Suite

This directory contains comprehensive unit tests for the helper classes that were extracted from the `RewriteQueryVisitor` during the refactoring effort.

## Test Structure

### BinaryOperationVisitorHelperTests (5 tests)
Tests for arithmetic operations helper:
- ✅ ProcessStarOperation_WhenTwoNodesOnStack_ShouldCreateStarNode
- ✅ ProcessFSlashOperation_WhenTwoNodesOnStack_ShouldCreateFSlashNode
- ✅ ProcessModuloOperation_WhenTwoNodesOnStack_ShouldCreateModuloNode
- ✅ ProcessAddOperation_WhenTwoNodesOnStack_ShouldCreateAddNode
- ✅ ProcessHyphenOperation_WhenTwoNodesOnStack_ShouldCreateHyphenNode

### ComparisonOperationVisitorHelperTests (7 tests)
Tests for comparison operations helper:
- ✅ ProcessEqualityOperation_WhenTwoNodesOnStack_ShouldCreateEqualityNode
- ✅ ProcessGreaterOrEqualOperation_WhenTwoNodesOnStack_ShouldCreateGreaterEqualNode
- ✅ ProcessLessOrEqualOperation_WhenTwoNodesOnStack_ShouldCreateLessEqualNode
- ✅ ProcessGreaterOperation_WhenTwoNodesOnStack_ShouldCreateGreaterNode
- ✅ ProcessLessOperation_WhenTwoNodesOnStack_ShouldCreateLessNode
- ✅ ProcessDiffOperation_WhenTwoNodesOnStack_ShouldCreateDiffNode
- ✅ ProcessLikeOperation_WhenTwoNodesOnStack_ShouldCreateLikeNode

### LogicalOperationVisitorHelperTests (10 tests)
Tests for logical operations helper:
- ✅ ProcessAndOperation_WhenTwoNodesOnStack_ShouldCreateAndNode
- ✅ ProcessOrOperation_WhenTwoNodesOnStack_ShouldCreateOrNode
- ✅ ProcessNotOperation_WhenOneNodeOnStack_ShouldCreateNotNode
- ✅ ProcessContainsOperation_WhenTwoNodesOnStack_ShouldCreateContainsNode
- ✅ ProcessIsNullOperation_WhenOneNodeOnStack_ShouldCreateIsNullNode
- ✅ ProcessInOperation_WhenTwoNodesOnStack_ShouldCreateOrChain
- ✅ ProcessAndOperation_WithNullableRewriter_ShouldApplyRewriter
- ✅ ProcessOrOperation_WithNullableRewriter_ShouldApplyRewriter
- ✅ ProcessInOperation_WithSingleValue_ShouldCreateSingleEquality
- ✅ ProcessInOperation_WithEmptyArgs_ShouldCreateBooleanFalse

### QueryRewriteUtilitiesTests (5 tests)
Tests for query rewrite utility functions:
- ✅ RewriteNullableBoolExpressions_WhenNodeIsNotNullableBool_ShouldReturnOriginalNode
- ✅ RewriteNullableBoolExpressions_WhenNodeIsBinaryNode_ShouldReturnOriginalNode
- ✅ RewriteFieldNameWithoutStringPrefixAndSuffix_WhenFieldHasQuotes_ShouldRemoveQuotes
- ✅ RewriteFieldNameWithoutStringPrefixAndSuffix_WhenFieldHasEscapedQuotes_ShouldUnescapeQuotes
- ✅ RewriteFieldNameWithoutStringPrefixAndSuffix_WhenNoQuotes_ShouldReturnOriginal

### FieldProcessingHelperTests (3 tests)
Tests for field processing operations:
- ✅ CreateFields_WhenOldFieldsProvided_ShouldCreateFieldsFromStack
- ✅ CreateFields_WhenStackHasFewerItems_ShouldHandleGracefully
- ✅ CreateFields_WhenEmptyOldFields_ShouldReturnEmptyArray

## Test Summary

**Total Tests: 27**
- ✅ All Passed: 27
- ❌ Failed: 0
- ⏭️ Skipped: 0

## Running the Tests

### Run all helper tests:
```bash
dotnet test Musoq.Evaluator.Tests --filter "FullyQualifiedName~HelperTests"
```

### Run specific helper class tests:
```bash
# Binary operations
dotnet test Musoq.Evaluator.Tests --filter "FullyQualifiedName~BinaryOperationVisitorHelperTests"

# Comparison operations  
dotnet test Musoq.Evaluator.Tests --filter "FullyQualifiedName~ComparisonOperationVisitorHelperTests"

# Logical operations
dotnet test Musoq.Evaluator.Tests --filter "FullyQualifiedName~LogicalOperationVisitorHelperTests"

# Query rewrite utilities
dotnet test Musoq.Evaluator.Tests --filter "FullyQualifiedName~QueryRewriteUtilitiesTests"

# Field processing
dotnet test Musoq.Evaluator.Tests --filter "FullyQualifiedName~FieldProcessingHelperTests"
```

## Test Coverage

These tests provide comprehensive coverage of the core functionality extracted from `RewriteQueryVisitor`:

1. **Node Creation Pattern Validation**: Tests verify that the correct AST nodes are created for each operation
2. **Stack Management**: Tests ensure proper pop/push behavior on the node stack
3. **Edge Case Handling**: Tests cover empty arguments, single values, and error conditions
4. **Function Parameter Handling**: Tests verify that nullable rewriter functions are properly applied
5. **String Processing**: Tests validate quote removal and escaping logic

## Benefits

- **Independent Testing**: Each helper class can now be tested in isolation
- **Regression Prevention**: Changes to helper classes are automatically validated
- **Documentation**: Tests serve as living documentation of expected behavior
- **Faster Feedback**: Smaller, focused tests run faster than integration tests
- **Easier Debugging**: Test failures point directly to the problematic helper method

## Future Enhancements

Potential areas for test expansion:
- Performance benchmarks for operations with large node stacks
- Error condition testing (e.g., malformed nodes, type mismatches)
- Integration tests with actual RewriteQueryVisitor usage
- Property-based testing for complex field processing scenarios