# Character vs String Return Type Fix

## Issue
The user reported that character access (`Name[0]`) should return `char` type, not `string`. The current implementation converts characters to strings for "SQL compatibility", but this is not desired.

## Current Behavior
- `select Self.Name[0] from table` returns `string` ("K")
- Test expects `char` ('K')
- Test failure: "Expected:<K (System.Char)>. Actual:<K (System.String)>."

## Root Cause
Two places in the code are causing this:

1. **AccessObjectArrayNode.ReturnType** (line 61-62):
   ```csharp
   if (ColumnType == typeof(string))
   {
       // String character access returns string for SQL compatibility
       return typeof(string);
   }
   ```
   Should return `typeof(char)` instead.

2. **ToCSharpRewriteTreeVisitor** (lines 945-949 and 1010-1014):
   ```csharp
   var toStringCall = SyntaxFactory.InvocationExpression(
       SyntaxFactory.MemberAccessExpression(
           SyntaxKind.SimpleMemberAccessExpression,
           SyntaxFactory.ParenthesizedExpression(characterAccess),
           SyntaxFactory.IdentifierName("ToString")));
   ```
   Should NOT call `.ToString()` on the character.

## Required Changes
1. Change `AccessObjectArrayNode.ReturnType` to return `typeof(char)` for string character access
2. Remove `.ToString()` calls in `ToCSharpRewriteTreeVisitor` for character access
3. Update any tests that incorrectly expect string instead of char

## Expected Results After Fix
- `select Self.Name[0] from table` should return `char` ('K')
- All character access tests should pass
- WHERE clause comparisons like `Name[0] = 'd'` should still work (char to char comparison)