using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class LoopOperatorProfilingStatementFactory
{
    public static BlockSyntax CreateBody(
        bool isEnabled,
        ExecutionPlanOperatorCatalog catalog,
        ExecutionNode loop,
        IEnumerable<StatementSyntax> statements) =>
        StatementEmitter.CreateBlock(
        [
            ..Create(isEnabled, catalog, loop),
            ..statements
        ]);

    public static IReadOnlyList<StatementSyntax> Create(
        bool isEnabled,
        ExecutionPlanOperatorCatalog catalog,
        ExecutionNode loop) =>
        isEnabled && catalog.TryGetDescriptor(loop, out var descriptor)
            ? Create(descriptor.Id)
            : [];

    public static IReadOnlyList<StatementSyntax> Create(string operatorId) =>
    [
        CreateIncrementStatement(OperatorProfileCounterFacts.CreateInputRowsVariableName(operatorId)),
        CreateIncrementStatement(OperatorProfileCounterFacts.CreateOutputRowsVariableName(operatorId))
    ];

    private static StatementSyntax CreateIncrementStatement(string variableName)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.PostfixUnaryExpression(
                SyntaxKind.PostIncrementExpression,
                SyntaxFactory.IdentifierName(variableName)));
    }
}
