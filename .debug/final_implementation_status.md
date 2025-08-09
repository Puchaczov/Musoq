# String Character Index Access - Current Analysis Status

## Current State: Tests Failing with Same Error

Both `FirstLetterOfColumnTest` (Name[0]) and `FirstLetterOfColumnTest2` (f.Name[0]) are failing with:
```
InvalidOperationException: Cannot generate code for array access AccessObjectArrayNode - no parent expression available
```

This error occurs in `ToCSharpRewriteTreeVisitor.Visit(AccessObjectArrayNode)` at line 985, indicating that:
1. The `AccessObjectArrayNode` has `IsColumnAccess = false` 
2. It's trying to use property-based access logic but has no parent expression on the stack

## Root Cause Analysis

The issue is that the transformation logic in `BuildMetadataAndInferTypesTraverseVisitor` is not properly converting `AccessObjectArrayNode` instances to column access nodes (with `IsColumnAccess = true`).

### What Should Happen:
1. **Direct access (`Name[0]`)**: Should be transformed to `AccessObjectArrayNode(token, typeof(string))` with `IsColumnAccess = true`
2. **Aliased access (`f.Name[0]`)**: Should be transformed to `AccessObjectArrayNode(token, typeof(string), "f")` with `IsColumnAccess = true`

### Current Implementation Issues:
1. The `IsDirectColumnAccess()` method may not be detecting the column correctly
2. The table context lookup via `MetaAttributes.ProcessedQueryId` might not be working
3. The transformation may not be happening due to visitor execution order

## Next Steps

Need to debug why the transformation is not occurring:
1. Add debug output to verify if `IsDirectColumnAccess()` returns true
2. Check if `TransformToColumnAccessNode()` is being called
3. Verify if the enhanced node has `IsColumnAccess = true`
4. Confirm that the enhanced node reaches the C# code generation stage