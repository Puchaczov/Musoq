using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static void AddSharedBoundariesDeclaration(
        ExecutionWindowAggregateKernel kernel,
        List<StatementSyntax> statements
    )
    {
        if (kernel.BoundariesOwner != kernel.Results)
            return;
        foreach (var suffix in new[] { "FrameStarts", "FrameEnds" })
            statements.Add(
                CreateLocalDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    kernel.Results.Name + suffix,
                    CreateSizedArrayCreation(typeof(int), CreateBufferCountExpression(kernel.Buffer))
                )
            );
    }

    private static LocalDeclarationStatementSyntax CreateFrameBoundDeclaration(
        ExecutionWindowAggregateKernel kernel,
        string local,
        string suffix,
        string index,
        ExpressionSyntax resolve
    )
    {
        if (kernel.BoundariesOwner is { } owner)
        {
            var slot = CreateElementAccess(
                SyntaxFactory.IdentifierName(owner.Name + suffix),
                SyntaxFactory.IdentifierName(index)
            );
            resolve =
                owner == kernel.Results
                    ? SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, slot, resolve)
                    : slot;
        }
        return CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), local, resolve);
    }

    private static StatementSyntax CreateCopyPreviousPeerResult(
        ExecutionWindowAggregateKernel kernel,
        string indices,
        string start,
        string position,
        string outputIndex
    )
    {
        var current = SyntaxFactory.IdentifierName(outputIndex);
        var previous = SyntaxFactory.IdentifierName(kernel.Results.Name + "PreviousPeer");
        var one = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1));
        var zero = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0));
        var previousPosition = SyntaxFactory.BinaryExpression(
            SyntaxKind.SubtractExpression,
            SyntaxFactory.BinaryExpression(
                SyntaxKind.AddExpression,
                SyntaxFactory.IdentifierName(start),
                SyntaxFactory.IdentifierName(position)
            ),
            one
        );
        var raw = SyntaxFactory.IdentifierName(WindowRangeFrameSyntax.GetAggregateRangeKeysName(kernel));
        var sameKey = SyntaxFactory.BinaryExpression(
            SyntaxKind.EqualsExpression,
            CreateElementAccess(raw, previous),
            CreateElementAccess(raw, current)
        );
        var copy = CreateWindowResultAssignment(
            kernel.Results.Name,
            current,
            CreateElementAccess(SyntaxFactory.IdentifierName(kernel.Results.Name), previous)
        );
        return SyntaxFactory.IfStatement(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.GreaterThanExpression,
                SyntaxFactory.IdentifierName(position),
                zero
            ),
            StatementEmitter.CreateBlock(
                CreateLocalDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    previous.Identifier.ValueText,
                    CreateElementAccess(SyntaxFactory.IdentifierName(indices), previousPosition)
                ),
                SyntaxFactory.IfStatement(
                    sameKey,
                    StatementEmitter.CreateBlock(copy, SyntaxFactory.ContinueStatement())
                )
            )
        );
    }
}
