using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Plugins.Attributes;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private ExpressionSyntax CreateInjectedMethodArgument(
        ExecutionMethodCall methodCall,
        ParameterInfo parameter,
        InjectTypeAttribute injectAttribute)
    {
        if (injectAttribute is InjectQueryStatsAttribute)
        {
            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(StatsVariableName),
                    SyntaxFactory.IdentifierName(nameof(AmendableQueryStats.IncrementRowNumber))));
        }

        if (injectAttribute.GetType().Name is nameof(InjectSpecificSourceAttribute) or "InjectSourceAttribute")
        {
            if (methodCall.InjectedSource != null)
                return CastIfNeeded(RenderExpression(methodCall.InjectedSource), parameter.ParameterType);

            if (string.IsNullOrWhiteSpace(methodCall.Alias))
                throw new NotSupportedException("Method source injection requires a source alias.");

            return CastIfNeeded(CreateIdentifierName(methodCall.Alias), parameter.ParameterType);
        }

        throw UnsupportedShape.Of($"Method injection {injectAttribute.GetType().Name}", "the Execution IR backend");
    }
}
