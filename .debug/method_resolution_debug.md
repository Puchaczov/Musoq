# Method Resolution Debug Session

## Issue
Window function SUM with argument types [Decimal] cannot be resolved.

## Expected Behavior
The generic method `Sum<T>(T value, [InjectQueryStats] QueryStats info)` should be resolved and constructed as `Sum<Decimal>`.

## Debugging Steps
1. Check if Sum<T> method is found by TryResolveRawMethod
2. Check if TryConstructGenericMethod succeeds
3. Identify where the resolution fails

## Analysis Plan
- Add debug logging to the WindowFunctionNode visitor
- Test with simplified query to isolate the issue
- Compare with working non-generic methods (Rank, DenseRank)