using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static IEnumerable<StatementSyntax> CreateWindowAggregateBoundedPrefixDeclarations(
        ExecutionWindowAggregateKernel kernel,
        string partitionCountName)
    {
        var size = SyntaxFactory.BinaryExpression(
            SyntaxKind.AddExpression,
            SyntaxFactory.IdentifierName(partitionCountName),
            CreateIntLiteral(1));

        if (RequiresWindowAggregateSumPrefix(kernel))
            foreach (var statement in CreateWindowAggregateBoundedPrefixDeclaration(kernel, typeof(decimal)))
                yield return statement;

        if (RequiresWindowAggregateCountPrefix(kernel))
            foreach (var statement in CreateWindowAggregateBoundedPrefixDeclaration(kernel, typeof(int)))
                yield return statement;

        IEnumerable<StatementSyntax> CreateWindowAggregateBoundedPrefixDeclaration(
            ExecutionWindowAggregateKernel declarationKernel,
            Type elementType)
        {
            var prefixName = elementType == typeof(decimal)
                ? CreateWindowAggregatePrefixSumName(declarationKernel)
                : CreateWindowAggregatePrefixCountName(declarationKernel);
            yield return CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                prefixName,
                CreateArrayPoolRentExpression(elementType, size));
            yield return CreateWindowAggregatePrefixDefaultAssignment(prefixName, elementType);
        }
    }

    private static IEnumerable<StatementSyntax> CreateWindowAggregateBoundedPrefixReturnStatements(
        ExecutionWindowAggregateKernel kernel)
    {
        if (RequiresWindowAggregateSumPrefix(kernel))
            yield return CreateArrayPoolReturnStatement(typeof(decimal), CreateWindowAggregatePrefixSumName(kernel));

        if (RequiresWindowAggregateCountPrefix(kernel))
            yield return CreateArrayPoolReturnStatement(typeof(int), CreateWindowAggregatePrefixCountName(kernel));
    }

    private static IEnumerable<StatementSyntax> CreateWindowAggregateBoundedMinMaxDequeDeclarations(
        ExecutionWindowAggregateKernel kernel,
        string partitionCountName)
    {
        var valueType = CreateWindowAggregateMinMaxValueType(kernel);

        yield return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            CreateWindowAggregateDequeValuesName(kernel),
            CreateArrayPoolRentExpression(valueType, SyntaxFactory.IdentifierName(partitionCountName)));
        yield return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            CreateWindowAggregateDequeIndicesName(kernel),
            CreateArrayPoolRentExpression(typeof(int), SyntaxFactory.IdentifierName(partitionCountName)));
        yield return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            CreateWindowAggregateDequeHeadName(kernel),
            CreateIntLiteral(0));
        yield return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            CreateWindowAggregateDequeTailName(kernel),
            CreateIntLiteral(0));
        yield return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            CreateWindowAggregateDequeFrameEndName(kernel),
            SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, CreateIntLiteral(1)));
    }

    private static IEnumerable<StatementSyntax> CreateWindowAggregateBoundedMinMaxDequeReturnStatements(
        ExecutionWindowAggregateKernel kernel)
    {
        yield return CreateArrayPoolReturnStatement(
            CreateWindowAggregateMinMaxValueType(kernel),
            CreateWindowAggregateDequeValuesName(kernel));
        yield return CreateArrayPoolReturnStatement(typeof(int), CreateWindowAggregateDequeIndicesName(kernel));
    }

    private static StatementSyntax CreateWindowAggregatePrefixDefaultAssignment(string prefixName, Type elementType)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateElementAccess(SyntaxFactory.IdentifierName(prefixName), CreateIntLiteral(0)),
                SyntaxFactory.DefaultExpression(CreateTypeSyntax(elementType))));
    }
}
