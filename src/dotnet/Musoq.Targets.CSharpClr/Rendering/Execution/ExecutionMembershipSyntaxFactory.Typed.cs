using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Execution;
using static Musoq.Targets.CSharpClr.ExecutionSyntaxFactory;

namespace Musoq.Targets.CSharpClr;

internal sealed partial class ExecutionMembershipSyntaxFactory
{
    private ExpressionSyntax RenderTypedMembership(ExecutionInCheck membership)
    {
        var binding = membership.TypedBinding
            ?? throw UnsupportedShape.Of("Membership requires a concretely bound comparison plan.");
        var operandName = $"__membership{variableSequence}Operand";
        var members = Enumerable.Range(0, membership.Values.Count)
            .Select(index => $"__membership{variableSequence}Member{index}").ToArray();
        var context = new MembershipRenderingContext(membership, binding, operandName, members);
        var body = binding.EvaluationMode == ExecutionMembershipEvaluationMode.Eager
            ? RenderEagerMembers(context, 0)
            : RenderMembers(context, 0);
        return ExecutionValueCaptureSyntax.Capture(RenderTypedInput(membership.Expression, binding.OperandType), operandName, body);
    }

    private ExpressionSyntax RenderEagerMembers(MembershipRenderingContext context, int index)
    {
        if (index == context.Members.Length)
            return context.Binding.Comparisons.Select((comparison, slot) => RenderMemberEquality(comparison, context.Operand, context.Members[slot]))
                .Aggregate((left, right) => SyntaxFactory.BinaryExpression(SyntaxKind.LogicalOrExpression,
                    SyntaxFactory.ParenthesizedExpression(left), SyntaxFactory.ParenthesizedExpression(right)));
        var remaining = RenderEagerMembers(context, index + 1);
        return ExecutionValueCaptureSyntax.Capture(RenderTypedInput(context.Membership.Values[index], context.Binding.MemberTypes[index]), context.Members[index], remaining);
    }

    private ExpressionSyntax RenderMembers(MembershipRenderingContext context, int index)
    {
        if (index == context.Members.Length)
            return RenderNoMatch(context.Membership.UsesSqlNullSemantics, context.Operand, context.Members);
        var value = RenderTypedInput(context.Membership.Values[index], context.Binding.MemberTypes[index]);
        var comparison = RenderMemberEquality(context.Binding.Comparisons[index], context.Operand, context.Members[index]);
        if (context.Membership.UsesSqlNullSemantics)
            comparison = SyntaxFactory.ParseExpression($"({context.Operand} is not null && {context.Members[index]} is not null && ({comparison}))");
        var success = SyntaxFactory.ParseExpression(context.Membership.UsesSqlNullSemantics ? "(bool?)true" : "true");
        var remaining = RenderMembers(context, index + 1);
        var match = SyntaxFactory.SwitchExpression(value).WithArms(SyntaxFactory.SeparatedList(
        [
            SyntaxFactory.SwitchExpressionArm(CreateCapturePattern(context.Members[index]), success)
                .WithWhenClause(SyntaxFactory.WhenClause(comparison)),
            SyntaxFactory.SwitchExpressionArm(CreateCapturePattern(context.Members[index]), remaining)
        ]));
        return SyntaxFactory.ParenthesizedExpression(match);
    }

    private ExpressionSyntax RenderTypedInput(ExecutionExpression value, ExecutionTypeRef type)
        => SyntaxFactory.ParenthesizedExpression(SyntaxFactory.CastExpression(CreateTypeSyntax(type),
            SyntaxFactory.ParenthesizedExpression(renderExpression(value))));

    private static ExpressionSyntax RenderMemberEquality(ExecutionMembershipComparison comparison,
        string operand, string member)
    {
        if (comparison.Kind == ExecutionMembershipEqualityKind.NeverMatch)
            return SyntaxFactory.ParseExpression($"({operand} is null && {member} is null)");
        var type = CreateTypeSyntax(comparison.ComparisonType).ToString();
        if (comparison.Kind == ExecutionMembershipEqualityKind.NumericEquality)
            return SyntaxFactory.ParseExpression($"(({type}){operand} == ({type}){member})");
        return SyntaxFactory.ParseExpression($"global::System.Collections.Generic.EqualityComparer<{type}>.Default.Equals(({type}){operand}, ({type}){member})");
    }

    private static ExpressionSyntax RenderNoMatch(bool sqlSemantics, string operand, IReadOnlyList<string> members)
    {
        if (!sqlSemantics)
            return SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
        var unknown = string.Join(" || ", new[] { operand }.Concat(members).Select(name => $"{name} is null"));
        return SyntaxFactory.ParseExpression($"({unknown}) ? (bool?)null : false");
    }

    private static VarPatternSyntax CreateCapturePattern(string name)
        => SyntaxFactory.VarPattern(SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(name)));

    private sealed record MembershipRenderingContext(ExecutionInCheck Membership,
        ExecutionMembershipBinding Binding, string Operand, string[] Members);
}
