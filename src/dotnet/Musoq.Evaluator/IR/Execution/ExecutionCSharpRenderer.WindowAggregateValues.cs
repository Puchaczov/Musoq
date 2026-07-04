using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> CreateWindowAggregateAccumulateStatements(
        ExecutionWindowAggregateKernel kernel)
    {
        IReadOnlyList<StatementSyntax> statements = Nullable.GetUnderlyingType(kernel.Descriptor.InputType) != null
            ? CreateNullableWindowAggregateAccumulateStatements(kernel)
            : [.. CreateNonNullableWindowAggregateAccumulateStatements(kernel)];

        if (kernel.FilterPredicate == null)
            return statements;

        return
        [
            StatementEmitter.CreateIf(
                CreateBooleanCondition(RenderExpression(kernel.FilterPredicate), kernel.FilterPredicate.ReturnType),
                StatementEmitter.CreateBlock(statements))
        ];
    }

    private IEnumerable<StatementSyntax> CreateNonNullableWindowAggregateAccumulateStatements(
        ExecutionWindowAggregateKernel kernel)
    {
        switch (kernel.Descriptor.Function)
        {
            case ExecutionWindowAggregateFunction.Sum:
                yield return CreateAddAssignStatement(
                    CreateWindowAggregateSumName(kernel),
                    CreateWindowAggregateDecimalValue(kernel));
                break;
            case ExecutionWindowAggregateFunction.Count:
                if (!kernel.Descriptor.InputType.IsValueType)
                {
                    var valueName = CreateWindowAggregateValueName(kernel);
                    yield return CreateLocalDeclaration(
                        SyntaxFactory.IdentifierName("var"),
                        valueName,
                        RenderExpression(kernel.Value));
                    yield return StatementEmitter.CreateIf(
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.NotEqualsExpression,
                            SyntaxFactory.IdentifierName(valueName),
                            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                        StatementEmitter.CreateBlock(
                            CreatePreIncrementStatement(CreateWindowAggregateCountName(kernel))));
                }
                else
                {
                    yield return CreateDiscardAssignment(CastIfNeeded(RenderExpression(kernel.Value), kernel.Descriptor.InputType));
                    yield return CreatePreIncrementStatement(CreateWindowAggregateCountName(kernel));
                }
                break;
            case ExecutionWindowAggregateFunction.Avg:
                yield return CreateAddAssignStatement(
                    CreateWindowAggregateSumName(kernel),
                    CreateWindowAggregateDecimalValue(kernel));
                yield return CreatePreIncrementStatement(CreateWindowAggregateCountName(kernel));
                break;
            case ExecutionWindowAggregateFunction.Min:
            case ExecutionWindowAggregateFunction.Max:
                foreach (var statement in CreateWindowAggregateMinMaxAccumulateStatements(kernel))
                    yield return statement;
                break;
            default:
                throw new NotSupportedException(
                    $"Window aggregate kernel {kernel.Descriptor.Function} is not supported.");
        }
    }

    private List<StatementSyntax> CreateNullableWindowAggregateAccumulateStatements(
        ExecutionWindowAggregateKernel kernel)
    {
        var valueName = CreateWindowAggregateValueName(kernel);
        var statements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                valueName,
                RenderExpression(kernel.Value))
        };

        var valueRead = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(valueName),
            SyntaxFactory.IdentifierName(nameof(Nullable<>.Value)));
        var hasValueCheck = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(valueName),
            SyntaxFactory.IdentifierName(nameof(Nullable<>.HasValue)));

        var body = kernel.Descriptor.Function switch
        {
            ExecutionWindowAggregateFunction.Sum => StatementEmitter.CreateBlock(
                CreateAddAssignStatement(
                    CreateWindowAggregateSumName(kernel),
                    CastIfNeeded(valueRead, typeof(decimal)))),
            ExecutionWindowAggregateFunction.Count => StatementEmitter.CreateBlock(
                CreatePreIncrementStatement(CreateWindowAggregateCountName(kernel))),
            ExecutionWindowAggregateFunction.Avg => StatementEmitter.CreateBlock(
                CreateAddAssignStatement(
                    CreateWindowAggregateSumName(kernel),
                    CastIfNeeded(valueRead, typeof(decimal))),
                CreatePreIncrementStatement(CreateWindowAggregateCountName(kernel))),
            ExecutionWindowAggregateFunction.Min or ExecutionWindowAggregateFunction.Max => StatementEmitter.CreateBlock(
                CreateWindowAggregateMinMaxUpdateStatement(kernel, valueRead)),
            _ => throw new NotSupportedException(
                $"Window aggregate kernel {kernel.Descriptor.Function} is not supported.")
        };

        statements.Add(SyntaxFactory.IfStatement(hasValueCheck, body));
        return statements;
    }

    private static ExpressionSyntax CreateWindowAggregateValueExpression(ExecutionWindowAggregateKernel kernel)
    {
        return kernel.Descriptor.Function switch
        {
            ExecutionWindowAggregateFunction.Sum => SyntaxFactory.IdentifierName(CreateWindowAggregateSumName(kernel)),
            ExecutionWindowAggregateFunction.Count => SyntaxFactory.IdentifierName(CreateWindowAggregateCountName(kernel)),
            ExecutionWindowAggregateFunction.Avg => SyntaxFactory.ConditionalExpression(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.GreaterThanExpression,
                    SyntaxFactory.IdentifierName(CreateWindowAggregateCountName(kernel)),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))),
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.DivideExpression,
                    SyntaxFactory.IdentifierName(CreateWindowAggregateSumName(kernel)),
                    SyntaxFactory.IdentifierName(CreateWindowAggregateCountName(kernel))),
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0m))),
            ExecutionWindowAggregateFunction.Min or ExecutionWindowAggregateFunction.Max => SyntaxFactory.ConditionalExpression(
                SyntaxFactory.IdentifierName(CreateWindowAggregateHasValueName(kernel)),
                CastIfNeeded(
                    SyntaxFactory.IdentifierName(CreateWindowAggregateCurrentName(kernel)),
                    kernel.Descriptor.ResultType),
                SyntaxFactory.DefaultExpression(CreateTypeSyntax(kernel.Descriptor.ResultType))),
            _ => throw new NotSupportedException(
                $"Window aggregate kernel {kernel.Descriptor.Function} is not supported.")
        };
    }

    private IEnumerable<StatementSyntax> CreateWindowAggregateMinMaxAccumulateStatements(
        ExecutionWindowAggregateKernel kernel)
    {
        var valueName = CreateWindowAggregateValueName(kernel);

        yield return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            valueName,
            RenderExpression(kernel.Value));
        yield return CreateWindowAggregateMinMaxUpdateStatement(
            kernel,
            CreateWindowAggregateMinMaxValueReadExpression(valueName, kernel.Descriptor.InputType));
    }

    private static StatementSyntax CreateWindowAggregateMinMaxUpdateStatement(
        ExecutionWindowAggregateKernel kernel,
        ExpressionSyntax value)
    {
        var comparisonKind = kernel.Descriptor.Function == ExecutionWindowAggregateFunction.Min
            ? SyntaxKind.LessThanExpression
            : SyntaxKind.GreaterThanExpression;
        var currentName = CreateWindowAggregateCurrentName(kernel);
        var hasValueName = CreateWindowAggregateHasValueName(kernel);
        var shouldUpdate = SyntaxFactory.BinaryExpression(
            SyntaxKind.LogicalOrExpression,
            SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                SyntaxFactory.IdentifierName(hasValueName)),
            SyntaxFactory.BinaryExpression(
                comparisonKind,
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            value,
                            SyntaxFactory.IdentifierName(nameof(IComparable<int>.CompareTo))))
                    .WithArgumentList(CreateArgumentList(SyntaxFactory.IdentifierName(currentName))),
                CreateIntLiteral(0)));

        return StatementEmitter.CreateIf(
            shouldUpdate,
            StatementEmitter.CreateBlock(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(currentName),
                        value)),
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(hasValueName),
                        CreateBooleanLiteral(true)))));
    }

    private static ExpressionSyntax CreateWindowAggregateMinMaxValueReadExpression(
        string valueName,
        Type inputType)
    {
        return Nullable.GetUnderlyingType(inputType) != null
            ? SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(valueName),
                SyntaxFactory.IdentifierName(nameof(Nullable<>.Value)))
            : SyntaxFactory.IdentifierName(valueName);
    }

    private ExpressionSyntax CreateWindowAggregateDecimalValue(ExecutionWindowAggregateKernel kernel)
    {
        return CastIfNeeded(RenderExpression(kernel.Value), typeof(decimal));
    }

    private static ExpressionStatementSyntax CreateAddAssignStatement(
        string variableName,
        ExpressionSyntax value)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.AddAssignmentExpression,
                SyntaxFactory.IdentifierName(variableName),
                value));
    }

    private static ExpressionStatementSyntax CreateDiscardAssignment(ExpressionSyntax value)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName("_"),
                value));
    }

    private static ExpressionStatementSyntax CreatePreIncrementStatement(string variableName)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.PreIncrementExpression,
                SyntaxFactory.IdentifierName(variableName)));
    }

    private static string CreateWindowAggregateSumName(ExecutionWindowAggregateKernel kernel)
    {
        return $"{kernel.Results.Name}Sum";
    }

    private static string CreateWindowAggregateCountName(ExecutionWindowAggregateKernel kernel)
    {
        return $"{kernel.Results.Name}Count";
    }

    private static string CreateWindowAggregateValueName(ExecutionWindowAggregateKernel kernel)
    {
        return $"{kernel.Results.Name}Value";
    }

    private static string CreateWindowAggregateCurrentName(ExecutionWindowAggregateKernel kernel)
    {
        return $"{kernel.Results.Name}Current";
    }

    private static string CreateWindowAggregateHasValueName(ExecutionWindowAggregateKernel kernel)
    {
        return $"{kernel.Results.Name}HasValue";
    }

    private static string CreateWindowAggregatePrefixSumName(ExecutionWindowAggregateKernel kernel)
    {
        return $"{kernel.Results.Name}PrefixSum";
    }

    private static string CreateWindowAggregatePrefixCountName(ExecutionWindowAggregateKernel kernel)
    {
        return $"{kernel.Results.Name}PrefixCount";
    }

    private static bool IsMinMaxWindowAggregate(ExecutionWindowAggregateKernel kernel)
    {
        return kernel.Descriptor.Function is ExecutionWindowAggregateFunction.Min
            or ExecutionWindowAggregateFunction.Max;
    }

    private static Type CreateWindowAggregateMinMaxValueType(ExecutionWindowAggregateKernel kernel)
    {
        return Nullable.GetUnderlyingType(kernel.Descriptor.InputType) ?? kernel.Descriptor.InputType;
    }
}
