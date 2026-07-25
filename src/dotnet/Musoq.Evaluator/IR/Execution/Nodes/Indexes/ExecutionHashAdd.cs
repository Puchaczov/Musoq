using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionHashAdd(
    ExecutionVariable Hash,
    ExecutionExpression Key,
    ExecutionVariable Row,
    ExecutionTypeRef KeyType,
    ExecutionTypeRef RowType,
    string? GeneratedRowTypeName = null,
    ExecutionVariable? PrecomputedKey = null,
    string? KeyVariableName = null,
    string? BucketVariableName = null,
    ExecutionKeyBuildNullHandling NullHandling = ExecutionKeyBuildNullHandling.Continue) : ExecutionNode
{
    internal ExecutionHashAdd(
        ExecutionVariable hash,
        ExecutionExpression key,
        ExecutionVariable row,
        Type keyType,
        Type rowType,
        string? generatedRowTypeName = null,
        ExecutionVariable? precomputedKey = null,
        string? keyVariableName = null,
        string? bucketVariableName = null,
        ExecutionKeyBuildNullHandling nullHandling = ExecutionKeyBuildNullHandling.Continue)
        : this(
            hash,
            key,
            row,
            ExecutionClrBindingFactory.FromClr(keyType),
            ExecutionClrBindingFactory.FromClr(rowType),
            generatedRowTypeName,
            precomputedKey,
            keyVariableName,
            bucketVariableName,
            nullHandling)
    {
    }
}
