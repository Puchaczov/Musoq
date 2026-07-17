using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Plugins;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{

    private ExpressionSyntax RenderMethodCall(ExecutionMethodCall methodCall, ExecutionRenderContext context)
    {
        if (methodCall.Cache != null)
            return RenderCachedMethodCall(methodCall, context);

        if (TryRenderMethodCallWithoutTarget(methodCall, context, out var targetlessInvocation))
            return targetlessInvocation;

        var targetExpression = methodCall.Target != null
            ? SyntaxFactory.IdentifierName(methodCall.Target.Name)
            : methodCall.Method.IsStatic
                ? CreateTypeSyntax(methodCall.Method.RequireClrMethod().DeclaringType!)
                : throw new NotSupportedException(CreateMissingMethodTargetMessage(methodCall));
        var methodName = CreateMethodNameSyntax(methodCall.Method.RequireClrMethod());
        var arguments = CreateMethodInvocationArguments(methodCall, context);

        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    targetExpression,
                    methodName))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));

        return CastIfNeeded(invocation, methodCall.ReturnType);
    }

    private bool TryRenderMethodCallWithoutTarget(
        ExecutionMethodCall methodCall,
        ExecutionRenderContext context,
        [NotNullWhen(true)] out ExpressionSyntax? expression)
    {
        expression = null;

        if (!ExecutionMethodTargetReuse.CanRenderWithoutTarget(methodCall))
            return false;

        expression = methodCall.Method.MethodName switch
        {
            nameof(LibraryBase.Contains) => RenderLibraryBaseStringPredicate(methodCall, nameof(string.Contains), context),
            nameof(LibraryBase.StartsWith) => RenderLibraryBaseStringPredicate(methodCall, nameof(string.StartsWith), context),
            nameof(LibraryBase.ToDecimal) => RenderLibraryBaseNumericToDecimal(methodCall, context),
            nameof(LibraryBase.__CorrelatedScalarSubqueryResult) =>
                RenderCorrelatedScalarSubqueryResultAccessor(methodCall, context),
            _ when ExecutionMethodTargetReuse.CanRenderPerInvocation(methodCall) => RenderPerInvocationMethodCall(methodCall, context),
            _ => null
        };

        return expression != null;
    }

    private ParenthesizedExpressionSyntax RenderLibraryBaseStringPredicate(
        ExecutionMethodCall methodCall,
        string methodName,
        ExecutionRenderContext context)
    {
        var value = RenderExpression(methodCall.Arguments[0], context);
        var pattern = RenderExpression(methodCall.Arguments[1], context);
        var nullLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
        var nullableBoolType = CreateTypeSyntax(typeof(bool?));

        var hasNullArgument = SyntaxFactory.BinaryExpression(
            SyntaxKind.LogicalOrExpression,
            SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression,
                value,
                nullLiteral),
            SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression,
                pattern,
                nullLiteral));

        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    value,
                    SyntaxFactory.IdentifierName(methodName)))
            .WithArgumentList(CreateArgumentList(
                pattern,
                CreateOrdinalIgnoreCaseStringComparisonExpression()));

        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.ConditionalExpression(
                SyntaxFactory.ParenthesizedExpression(hasNullArgument),
                SyntaxFactory.CastExpression(nullableBoolType, nullLiteral),
                invocation));
    }

    private ParenthesizedExpressionSyntax RenderLibraryBaseNumericToDecimal(
        ExecutionMethodCall methodCall,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.CastExpression(
                CreateTypeSyntax(typeof(decimal?)),
                RenderExpression(methodCall.Arguments[0], context)));
    }

    private ExpressionSyntax RenderCorrelatedScalarSubqueryResultAccessor(
        ExecutionMethodCall methodCall,
        ExecutionRenderContext context)
    {
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    CreateTypeSyntax(typeof(CorrelatedScalarSubqueryResultExtractor)),
                    SyntaxFactory.IdentifierName(nameof(CorrelatedScalarSubqueryResultExtractor.GetValue))))
            .WithArgumentList(SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(CreateMethodInvocationArguments(methodCall, context))));

        return CastIfNeeded(invocation, methodCall.ReturnType);
    }

    private static ExpressionSyntax CreateOptionalParameterDefaultExpression(ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue)
            return RenderLiteral(parameter.DefaultValue);

        return SyntaxFactory.DefaultExpression(CreateTypeSyntax(parameter.ParameterType));
    }

    private static ExpressionSyntax CastIfNeeded(ExpressionSyntax expression, Type targetType)
    {
        return targetType == typeof(void)
            ? expression
            : SyntaxFactory.CastExpression(CreateTypeSyntax(targetType), expression);
    }

    private static ExpressionSyntax CastIfNeeded(ExpressionSyntax expression, ExecutionTypeRef targetType) =>
        CastIfNeeded(expression, targetType.RequireClrType());

    private static CastExpressionSyntax CreateObjectCastExpression(ExpressionSyntax expression)
    {
        return SyntaxFactory.CastExpression(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
            expression);
    }

}
