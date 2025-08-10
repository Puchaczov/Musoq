# Window Function currentRowStats Scope Issue Debug

## Problem
The test `Convert_WindowFunctionTypeInference_ShouldWork` fails with:
```
(26,44): error CS0103: Nazwa „currentRowStats" nie istnieje w bieżącym kontekście
```

## Analysis
The error occurs when RANK() OVER is used in a CASE expression. The `currentRowStats` variable is referenced but not in scope.

## Root Cause
Window functions in complex expressions (like CASE statements) generate references to `currentRowStats` but the variable declaration scope is limited to simple SELECT clauses.

## Issue Location
The problem is in the `ToCSharpRewriteTreeVisitor.cs` where `currentRowStats` is declared at the SELECT level but CASE expressions are processed separately.

## Next Steps
1. Ensure currentRowStats is declared at a higher scope level
2. Or modify window function injection to work without currentRowStats variable
3. Or use alternative approach for complex expressions

Current time: $(date)