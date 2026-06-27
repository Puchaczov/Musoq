using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IdentifierNameSyntax CreateAggregateGroupType(AggregateGroupShape shape)
    {
        return SyntaxFactory.IdentifierName(GetAggregateGroupTypeName(shape));
    }

    private ObjectCreationExpressionSyntax CreateAggregateGroupCreation(
        AggregateGroupShape shape,
        IReadOnlyList<ExpressionSyntax> owners,
        params ExpressionSyntax[] keys)
    {
        return CreateObjectCreation(GetAggregateGroupTypeName(shape), [..owners, ..keys]);
    }

    private sealed record ConstantInSetField(string Name, ExecutionConstantInSet ConstantSet);

    private sealed record StaticMetadataField(string Name, ExecutionColumnMetadata Metadata);
}
