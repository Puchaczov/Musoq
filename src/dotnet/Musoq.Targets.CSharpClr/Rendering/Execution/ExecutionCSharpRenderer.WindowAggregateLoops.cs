using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private ForStatementSyntax CreateWindowAggregateRunningLoop(
        ExecutionWindowAggregateKernel kernel,
        string partitionIndicesName,
        string partitionStartName,
        string partitionCountName)
    {
        var partitionIndexName = $"{kernel.Results.Name}PartitionIndex";
        var currentIndexName = $"{kernel.Results.Name}CurrentIndex";
        var currentIndex = new ExecutionVariable(currentIndexName, typeof(int));
        var body = new List<StatementSyntax>
        {
            CreateStreamingCurrentIndexDeclaration(
                partitionIndicesName,
                partitionStartName,
                partitionIndexName,
                currentIndexName)
        };

        body.AddRange(CreateIndexedItemDeclarations(
            kernel.Item,
            kernel.Buffer,
            currentIndex,
            kernel.RowAccessMode));
        body.AddRange(CreateWindowAggregateAccumulateStatements(kernel));
        body.Add(CreateWindowResultAssignment(
            kernel.Results.Name,
            SyntaxFactory.IdentifierName(currentIndexName),
            CreateWindowAggregateValueExpression(kernel)));

        return CreatePartitionIndexedForLoop(
            partitionIndexName,
            partitionCountName,
            StatementEmitter.CreateBlock(body));
    }

    private ForStatementSyntax CreateWindowAggregateAccumulationLoop(
        ExecutionWindowAggregateKernel kernel,
        string partitionIndicesName,
        string partitionStartName,
        string partitionCountName)
    {
        var partitionIndexName = $"{kernel.Results.Name}PartitionIndex";
        var currentIndexName = $"{kernel.Results.Name}CurrentIndex";
        var currentIndex = new ExecutionVariable(currentIndexName, typeof(int));
        var body = new List<StatementSyntax>
        {
            CreateStreamingCurrentIndexDeclaration(
                partitionIndicesName,
                partitionStartName,
                partitionIndexName,
                currentIndexName)
        };

        body.AddRange(CreateIndexedItemDeclarations(
            kernel.Item,
            kernel.Buffer,
            currentIndex,
            kernel.RowAccessMode));
        body.AddRange(CreateWindowAggregateAccumulateStatements(kernel));

        return CreatePartitionIndexedForLoop(
            partitionIndexName,
            partitionCountName,
            StatementEmitter.CreateBlock(body));
    }

    private static ForStatementSyntax CreateWindowAggregateWholePartitionAssignmentLoop(
        ExecutionWindowAggregateKernel kernel,
        string partitionIndicesName,
        string partitionStartName,
        string partitionCountName)
    {
        var partitionIndexName = $"{kernel.Results.Name}PartitionIndex";
        var currentIndexName = $"{kernel.Results.Name}CurrentIndex";

        return CreatePartitionIndexedForLoop(
            partitionIndexName,
            partitionCountName,
            StatementEmitter.CreateBlock(
                CreateStreamingCurrentIndexDeclaration(
                    partitionIndicesName,
                    partitionStartName,
                    partitionIndexName,
                    currentIndexName),
                CreateWindowResultAssignment(
                    kernel.Results.Name,
                    SyntaxFactory.IdentifierName(currentIndexName),
                    SyntaxFactory.IdentifierName($"{kernel.Results.Name}FinalValue"))));
    }

    private ForStatementSyntax CreateWindowAggregateBoundedPrefixLoop(
        ExecutionWindowAggregateKernel kernel,
        string partitionIndicesName,
        string partitionStartName,
        string partitionCountName)
    {
        var partitionIndexName = $"{kernel.Results.Name}PartitionIndex";
        var currentIndexName = $"{kernel.Results.Name}CurrentIndex";
        var currentIndex = new ExecutionVariable(currentIndexName, typeof(int));
        var body = new List<StatementSyntax>
        {
            CreateStreamingCurrentIndexDeclaration(
                partitionIndicesName,
                partitionStartName,
                partitionIndexName,
                currentIndexName)
        };

        body.AddRange(CreateIndexedItemDeclarations(
            kernel.Item,
            kernel.Buffer,
            currentIndex,
            kernel.RowAccessMode));
        body.AddRange(CreateWindowAggregateBoundedPrefixStepStatements(kernel, partitionIndexName));

        return CreatePartitionIndexedForLoop(
            partitionIndexName,
            partitionCountName,
            StatementEmitter.CreateBlock(body));
    }

    private List<StatementSyntax> CreateWindowAggregateBoundedPrefixStepStatements(
        ExecutionWindowAggregateKernel kernel,
        string partitionIndexName)
    {
        var valueName = CreateWindowAggregateValueName(kernel);
        var statements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                valueName,
                RenderExpression(kernel.Value))
        };
        var valuePresent = CreateWindowAggregateValuePresentExpression(valueName, kernel.Descriptor.InputType.RequireClrType());
        if (kernel.FilterPredicate != null)
        {
            valuePresent = SyntaxFactory.BinaryExpression(
                SyntaxKind.LogicalAndExpression,
                CreateBooleanCondition(RenderExpression(kernel.FilterPredicate), kernel.FilterPredicate.ReturnType),
                valuePresent);
        }

        if (RequiresWindowAggregateSumPrefix(kernel))
        {
            statements.Add(CreateWindowAggregatePrefixAssignment(
                CreateWindowAggregatePrefixSumName(kernel),
                partitionIndexName,
                CreateWindowAggregateNextPrefixValue(
                    CreateWindowAggregatePrefixSumName(kernel),
                    partitionIndexName,
                    CreateWindowAggregateDecimalValueRead(valueName, kernel.Descriptor.InputType.RequireClrType()),
                    valuePresent)));
        }

        if (RequiresWindowAggregateCountPrefix(kernel))
        {
            statements.Add(CreateWindowAggregatePrefixAssignment(
                CreateWindowAggregatePrefixCountName(kernel),
                partitionIndexName,
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.AddExpression,
                    CreateWindowAggregatePrefixCurrentAccess(
                        CreateWindowAggregatePrefixCountName(kernel),
                        partitionIndexName),
                    CreateWindowAggregateCountIncrement(valuePresent))));
        }

        return statements;
    }

    private static ExpressionSyntax CreateWindowAggregateNextPrefixValue(
        string prefixName,
        string partitionIndexName,
        ExpressionSyntax value,
        ExpressionSyntax valuePresent)
    {
        var current = CreateWindowAggregatePrefixCurrentAccess(prefixName, partitionIndexName);
        var accumulated = SyntaxFactory.BinaryExpression(
            SyntaxKind.AddExpression,
            current,
            value);

        return IsTrueLiteral(valuePresent)
            ? accumulated
            : SyntaxFactory.ConditionalExpression(valuePresent, accumulated, current);
    }

    private static ExpressionSyntax CreateWindowAggregateCountIncrement(ExpressionSyntax valuePresent)
    {
        return IsTrueLiteral(valuePresent)
            ? CreateIntLiteral(1)
            : SyntaxFactory.ParenthesizedExpression(
                SyntaxFactory.ConditionalExpression(valuePresent, CreateIntLiteral(1), CreateIntLiteral(0)));
    }

    private static ExpressionSyntax CreateWindowAggregateDecimalValueRead(string valueName, Type inputType)
    {
        var value = Nullable.GetUnderlyingType(inputType) != null
            ? SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(valueName),
                SyntaxFactory.IdentifierName(nameof(Nullable<>.Value)))
            : (ExpressionSyntax)SyntaxFactory.IdentifierName(valueName);

        return CastIfNeeded(value, typeof(decimal));
    }

    private static ExpressionSyntax CreateWindowAggregateValuePresentExpression(string valueName, Type inputType)
    {
        return CreateWindowAggregateValuePresentExpression(SyntaxFactory.IdentifierName(valueName), inputType);
    }

    private static ExpressionSyntax CreateWindowAggregateValuePresentExpression(ExpressionSyntax value, Type inputType)
    {
        if (Nullable.GetUnderlyingType(inputType) != null)
        {
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                value,
                SyntaxFactory.IdentifierName(nameof(Nullable<>.HasValue)));
        }

        if (!inputType.IsValueType)
        {
            return SyntaxFactory.BinaryExpression(
                SyntaxKind.NotEqualsExpression,
                value,
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
        }

        return CreateBooleanLiteral(true);
    }

}
