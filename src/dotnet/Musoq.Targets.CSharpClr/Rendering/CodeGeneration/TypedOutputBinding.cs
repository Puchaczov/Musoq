using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.TypedOutput;

namespace Musoq.Targets.CSharpClr;

internal sealed class TypedOutputBinding
{
    private readonly TypedOutputBindingPlan _plan;

    private TypedOutputBinding(TypedOutputBindingPlan plan)
    {
        _plan = plan;
    }

    public Type OutputType => _plan.OutputType;

    public static TypedOutputBinding Create(Type outputType, IReadOnlyList<ExecutionColumnMetadataField> columns)
    {
        ArgumentNullException.ThrowIfNull(outputType);
        ArgumentNullException.ThrowIfNull(columns);

        return new TypedOutputBinding(TypedOutputBinder.Create(
            outputType,
            columns
                .Select(static column => new TypedOutputColumn(column.Name, column.Index, column.Type.RequireClrType()))
                .ToArray()));
    }

    public ExpressionSyntax CreateOutputExpression(string rowVariableName)
    {
        if (_plan.Constructor != null)
        {
            return SyntaxFactory.ObjectCreationExpression(CreateTypeSyntax(OutputType))
                .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                    _plan.ConstructorBindings.Select(binding => SyntaxFactory.Argument(CreateRowValueRead(rowVariableName, binding.Column, binding.TargetType))))));
        }

        return SyntaxFactory.ObjectCreationExpression(CreateTypeSyntax(OutputType))
            .WithInitializer(SyntaxFactory.InitializerExpression(
                SyntaxKind.ObjectInitializerExpression,
                SyntaxFactory.SeparatedList<ExpressionSyntax>(
                    _plan.MemberBindings.Select(binding =>
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                        ExecutionSyntaxFactory.CreateIdentifierName(binding.MemberName),
                        CreateRowValueRead(rowVariableName, binding.Column, binding.TargetType))))));
    }

    public ExpressionSyntax CreateOutputExpression(IReadOnlyList<ExpressionSyntax> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (_plan.Constructor != null)
        {
            return SyntaxFactory.ObjectCreationExpression(CreateTypeSyntax(OutputType))
                .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                    _plan.ConstructorBindings.Select(binding => SyntaxFactory.Argument(
                        CreateTypedValueExpression(values, binding.Column, binding.TargetType))))));
        }

        return SyntaxFactory.ObjectCreationExpression(CreateTypeSyntax(OutputType))
            .WithInitializer(SyntaxFactory.InitializerExpression(
                SyntaxKind.ObjectInitializerExpression,
                SyntaxFactory.SeparatedList<ExpressionSyntax>(
                    _plan.MemberBindings.Select(binding =>
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            ExecutionSyntaxFactory.CreateIdentifierName(binding.MemberName),
                            CreateTypedValueExpression(values, binding.Column, binding.TargetType))))));
    }

    private static ExpressionSyntax CreateRowValueRead(
        string rowVariableName,
        TypedOutputColumn column,
        Type targetType)
    {
        var value = SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName(rowVariableName))
            .WithArgumentList(SyntaxFactory.BracketedArgumentList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(column.Index))))));

        return targetType == typeof(object)
            ? value
            : SyntaxFactory.CastExpression(CreateTypeSyntax(targetType), value);
    }

    private static ExpressionSyntax CreateTypedValueExpression(
        IReadOnlyList<ExpressionSyntax> values,
        TypedOutputColumn column,
        Type targetType)
    {
        if ((uint)column.Index >= (uint)values.Count)
            throw new InvalidOperationException($"Typed output column '{column.Name}' index {column.Index} is outside projected values.");

        var value = values[column.Index];
        return targetType == typeof(object)
            ? value
            : SyntaxFactory.CastExpression(CreateTypeSyntax(targetType), value);
    }

    private static TypeSyntax CreateTypeSyntax(Type type)
    {
        return SyntaxFactory.ParseTypeName(EvaluationHelper.GetCastableType(type));
    }
}
