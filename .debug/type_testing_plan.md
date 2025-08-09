# Type Testing Plan for Generic Array Access

## Current Test Coverage
- String character access: `Name[0]` -> should return `char`
- Integer array access: `Array[2]` -> should return `int`
- Array operations: `Inc(Array[2])` -> should return `long`

## Missing Test Coverage - Need to Add
Based on BasicEntity properties, we should test:
1. **String indexing** (existing): `Name[0]` -> `char`
2. **Integer array indexing** (existing): `Array[2]` -> `int`
3. **Dictionary indexing**: `Dictionary["A"]` -> `string`
4. **Nested property access**: `Self.Array[1]` -> `int`
5. **Decimal indexing** (if arrays exist): hypothetical `DecimalArray[0]` -> `decimal`

## Architecture Issues to Fix
1. **AccessObjectArrayNode.ReturnType** - hardcoded `typeof(string)` and `typeof(char)`
2. **ToCSharpRewriteTreeVisitor** - assumes string casting
3. **BuildMetadataAndInferTypesVisitor** - only checks for `typeof(string)`
4. **TransformToColumnAccessNode** - hardcoded `typeof(string)`

## Test Creation Strategy
1. Create tests for each type scenario
2. Run tests to see current failures
3. Fix architecture to be generic
4. Validate all tests pass