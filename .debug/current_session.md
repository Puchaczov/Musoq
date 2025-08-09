# Debug Session - Window Functions OVER Clause Investigation

## Critical Discovery
- **BASIC functions work**: `RANK()` works in both evaluator and converter ✅
- **OVER clause functions fail**: `RANK() OVER()` fails in both evaluator and converter with "From node is null" ❌

## Error Pattern
The error occurs consistently in `BuildMetadataAndInferTypesVisitor.Visit(QueryNode node)` at line 1154 when trying to pop FromNode from the stack.

## Root Cause Analysis
The issue is in the visitor pattern stack management during metadata building. When window functions with OVER clauses are processed, they interfere with the normal stack state expected by the QueryNode visitor.

### Key Investigation Points:

1. **WindowFunctionNode Creation**: Parser creates WindowFunctionNode instead of AccessMethodNode when OVER clause is detected
2. **Traverse Visitor Processing**: WindowFunctionNode.Arguments gets processed twice:
   - Once in `Visit(ArgsListNode)` (line 602) 
   - Once in WindowFunctionNode visitor logic
3. **Stack State Corruption**: The double processing or WindowSpecification processing corrupts the stack

### WindowSpecification Notes:
The traverse visitor has commented out PARTITION BY and ORDER BY processing:
```
// node.PartitionBy?.Accept(this);  // COMMENTED OUT: This would push extra nodes to stack
// node.OrderBy?.Accept(this);      // COMMENTED OUT: This would push extra OrderByNode to stack
```

This indicates previous attempts to fix stack management issues.

## Next Steps:
1. Fix the stack management in WindowFunctionNode visitor
2. Ensure WindowSpecification processing doesn't interfere with main query stack
3. Test the fix against all window function variants