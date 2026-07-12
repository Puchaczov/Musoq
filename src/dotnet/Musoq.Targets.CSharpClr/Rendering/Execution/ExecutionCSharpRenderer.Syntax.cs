using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IdentifierNameSyntax CreateAggregateGroupType(
        AggregateGroupShape shape,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.IdentifierName(GetAggregateGroupTypeName(shape, context));
    }

    private ObjectCreationExpressionSyntax CreateAggregateGroupCreation(
        AggregateGroupShape shape,
        ExecutionRenderContext context,
        IReadOnlyList<ExpressionSyntax> owners,
        params ExpressionSyntax[] keys)
    {
        return CreateObjectCreation(GetAggregateGroupTypeName(shape, context), [..owners, ..keys]);
    }

    internal sealed record ConstantInSetField(string Name, ExecutionConstantInSet ConstantSet);

    internal sealed record StaticMetadataField(string Name, ExecutionColumnMetadata Metadata);
}
