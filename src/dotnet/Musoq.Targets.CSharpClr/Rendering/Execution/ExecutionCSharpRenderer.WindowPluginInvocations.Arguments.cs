using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Plugins;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private StatementSyntax? CreateTypedPluginArgumentsStatement(
        ExecutionComputePluginWindow plugin,
        string functionName)
    {
        if (plugin.Arguments.Count == 0)
            return null;

        var argumentTypes = plugin.Arguments.Select(static argument => argument.ReturnType.RequireClrType()).ToArray();
        var interfaceType = CreatePluginWindowArgumentsType(argumentTypes);
        var target = SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.CastExpression(
                interfaceType,
                SyntaxFactory.IdentifierName(functionName)));
        var arguments = plugin.Arguments.Select(argument =>
            SyntaxFactory.CastExpression(
                CreateTypeSyntax(argument.ReturnType),
                SyntaxFactory.ParenthesizedExpression(RenderExpression(argument))));

        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        target,
                        SyntaxFactory.IdentifierName(nameof(IWindowFunctionArguments<int>.SetArguments))))
                .WithArgumentList(CreateArgumentList(arguments)));
    }

    private static TypeSyntax CreatePluginWindowArgumentsType(IReadOnlyList<Type> argumentTypes)
    {
        return SyntaxFactory.ParseTypeName(
            $"Musoq.Plugins.IWindowFunctionArguments<{string.Join(", ", argumentTypes.Select(EvaluationHelper.GetCastableType))}>");
    }

    private static CastExpressionSyntax CreateIntCastExpression(ExpressionSyntax expression)
    {
        return SyntaxFactory.CastExpression(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
            SyntaxFactory.ParenthesizedExpression(expression));
    }

    private static ExpressionSyntax CreatePartitionKeysArgument(ExecutionVariable? partitionKeys)
    {
        return partitionKeys == null
            ? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
            : SyntaxFactory.IdentifierName(partitionKeys.Name);
    }
}
