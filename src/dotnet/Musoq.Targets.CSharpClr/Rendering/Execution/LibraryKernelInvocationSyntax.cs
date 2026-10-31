using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

internal static class LibraryKernelInvocationSyntax
{
    internal static string CreateLibraryWindowStateName(ExecutionWindowAggregateKernel kernel)
        => $"{kernel.Results.Name}State";

    internal static ExpressionSyntax CreateLibraryWindowResult(ExecutionWindowAggregateKernel kernel)
        => Get(kernel.Descriptor.LibraryKernel!.GetMethod.RequireClrMethod(), CreateLibraryWindowStateName(kernel));

    public static ExpressionSyntax Get(MethodInfo operation, string state)
        => Invoke(operation, [StateArgument(state, SyntaxKind.InKeyword)]);

    public static IEnumerable<StatementSyntax> Initialize(ExecutionInitializeAggregateState initialize)
    {
        var type = CreateTypeSyntax(initialize.Target.Type);
        yield return CreateLocalDeclaration(type, initialize.Target.Name,
            SyntaxFactory.PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression,
                SyntaxFactory.DefaultExpression(type)));
        yield return SyntaxFactory.ExpressionStatement(Reset(initialize.ResetMethod.RequireClrMethod(),
            SyntaxFactory.IdentifierName(initialize.Target.Name)));
    }

    public static InvocationExpressionSyntax Reset(MethodInfo operation, ExpressionSyntax state)
        => Invoke(operation, [SyntaxFactory.Argument(state).WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.RefKeyword))]);

    public static StatementSyntax Set(ExecutionAggregateKernelOperations kernel, string state,
        IReadOnlyList<ExpressionSyntax> values)
    {
        var arguments = new List<ArgumentSyntax> { StateArgument(state, SyntaxKind.RefKeyword) };
        for (var index = 0; index < kernel.ArgumentTypes.Count; index++)
        {
            var type = CreateTypeSyntax(kernel.ArgumentTypes[index]);
            arguments.Add(SyntaxFactory.Argument(SyntaxFactory.CastExpression(type,
                SyntaxFactory.ParenthesizedExpression(values[index]))));
        }
        return SyntaxFactory.ExpressionStatement(Invoke(kernel.SetMethod.RequireClrMethod(), arguments));
    }

    private static ArgumentSyntax StateArgument(string state, SyntaxKind modifier)
        => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(state))
            .WithRefKindKeyword(SyntaxFactory.Token(modifier));

    private static InvocationExpressionSyntax Invoke(MethodInfo operation, IEnumerable<ArgumentSyntax> arguments)
        => SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.ParseTypeName(EvaluationHelper.GetCastableType(operation.DeclaringType!)),
                SyntaxFactory.IdentifierName(operation.Name)))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
}
