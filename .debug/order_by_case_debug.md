# ORDER BY CASE Expression Debug Session

## Problem
ORDER BY CASE expressions fail with: `currentRowStats` doesn't exist in current context
Error at line 43, column 104 in generated code.

## Query being tested
```sql
select City from #A.Entities() order by case when Money > 0 then Money else 0d end
```

## What we've tried
1. **Extended QueryStats injection to TransformingQuery contexts** - Fixed RowNumber GROUP BY issues ✅
2. **Added CaseWhen to supported contexts** - Still failed ❌
3. **Made injection logic consistent between function calls and case methods** - Still failed ❌
4. **Disabled QueryStats injection entirely for ORDER BY contexts** - Still failed ❌

## Key insights
- The error persists even when we disable QueryStats injection for ORDER BY
- This suggests the issue is not in our injection logic but somewhere else
- The error is consistently at line 43, column 104
- Some function call is hardcoded to use `currentRowStats` regardless of injection settings

## Next steps
- Investigate what function at line 43, column 104 is causing this
- Check if there are hardcoded references to `currentRowStats` in generated code
- Look for functions that bypass the injection mechanism