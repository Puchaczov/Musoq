using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;
using Musoq.Parser.Diagnostics;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IReadOnlyList<StatementSyntax> RenderRecursiveCte(
        ExecutionRecursiveCte recursiveCte,
        ExecutionRenderContext context)
    {
        var session = context.Session;
        var previousTypedBuffers = session.TypedRowBufferVariables;
        var recursiveTypedBuffers = new Dictionary<string, GeneratedRowShape>(previousTypedBuffers, StringComparer.Ordinal)
        {
            [recursiveCte.Result.Name] = recursiveCte.RowShape,
            [recursiveCte.CurrentFrontier.Name] = recursiveCte.RowShape,
            [recursiveCte.NextFrontier.Name] = recursiveCte.RowShape
        };
        foreach (var createTable in ExecutionIrAnalysis.FlattenNodes(recursiveCte.InvariantSetup)
                     .OfType<ExecutionCreateTable>()
                     .Where(static createTable => createTable.RowShape.EmitAsValueType))
        {
            recursiveTypedBuffers[createTable.Table.Name] = createTable.RowShape;
        }

        session.TypedRowBufferVariables = recursiveTypedBuffers;

        try
        {
            var statements = new List<StatementSyntax>();
            statements.Add(CreateRecursiveRowListDeclaration(recursiveCte.Result, recursiveCte.RowShape));
            statements.Add(CreateRecursiveRowListDeclaration(recursiveCte.CurrentFrontier, recursiveCte.RowShape));
            statements.Add(CreateRecursiveRowListDeclaration(recursiveCte.NextFrontier, recursiveCte.RowShape));
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
                recursiveCte.SnapshotRows.Name,
                CreateIntLiteral(0)));
            if (recursiveCte.Seen != null)
                statements.Add(RecursiveCteIdentitySyntaxFactory.CreateSeenDeclaration(recursiveCte));

            var iterationName = $"__{recursiveCte.Result.Name}Iteration";
            var cancellationCounterName = $"__{recursiveCte.Result.Name}CancellationCounter";
            var recursiveProfileDescriptor = IsOperatorProfilingEnabledFor(context) &&
                                             session.OperatorCatalog.TryGetDescriptor(
                                                 recursiveCte,
                                                 out var profileDescriptor)
                ? profileDescriptor
                : null;
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
                iterationName,
                CreateIntLiteral(0)));
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
                cancellationCounterName,
                CreateIntLiteral(0)));
            statements.AddRange(RenderBlock(recursiveCte.Anchor, context).Statements);
            statements.Add(CreateInvocationStatement(
                recursiveCte.Result.Name,
                nameof(List<object>.AddRange),
                SyntaxFactory.IdentifierName(recursiveCte.CurrentFrontier.Name)));

            var loopStatements = new List<StatementSyntax>
            {
                CreatePeriodicCancellationCheck(iterationName, mask: 63),
                CreateRecursiveLimitGuard(
                    recursiveCte.Name,
                    iterationName,
                    recursiveCte.MaxIterations,
                    DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded),
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.PostfixUnaryExpression(
                        SyntaxKind.PostIncrementExpression,
                        SyntaxFactory.IdentifierName(iterationName))),
                CreateInvocationStatement(recursiveCte.NextFrontier.Name, nameof(List<object>.Clear)),
            };
            if (recursiveProfileDescriptor != null)
            {
                loopStatements.Add(OperatorProfileCounterFacts.CreateCounterRowsStatement(
                    OperatorProfileCounterFacts.CreateInputRowsVariableName(recursiveProfileDescriptor),
                    CreateRecursiveCountExpression(recursiveCte.CurrentFrontier.Name)));
            }

            var previousSkipInitialCancellationCheck = session.SkipInitialLoopCancellationCheck;
            var previousRecursiveCancellationCounter = session.RecursiveCteCancellationCounterName;
            try
            {
                session.SkipInitialLoopCancellationCheck = true;
                session.RecursiveCteCancellationCounterName = cancellationCounterName;
                loopStatements.AddRange(RenderBlock(recursiveCte.RecursiveMember, context).Statements);
            }
            finally
            {
                session.SkipInitialLoopCancellationCheck = previousSkipInitialCancellationCheck;
                session.RecursiveCteCancellationCounterName = previousRecursiveCancellationCounter;
            }
            if (recursiveProfileDescriptor != null)
            {
                loopStatements.Add(OperatorProfileCounterFacts.CreateCounterRowsStatement(
                    OperatorProfileCounterFacts.CreateOutputRowsVariableName(recursiveProfileDescriptor),
                    CreateRecursiveCountExpression(recursiveCte.NextFrontier.Name)));
            }

            loopStatements.Add(CreateInvocationStatement(
                recursiveCte.Result.Name,
                nameof(List<object>.AddRange),
                SyntaxFactory.IdentifierName(recursiveCte.NextFrontier.Name)));

            var swapName = $"__{recursiveCte.Result.Name}FrontierSwap";
            loopStatements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                swapName,
                SyntaxFactory.IdentifierName(recursiveCte.CurrentFrontier.Name)));
            loopStatements.Add(CreateAssignment(recursiveCte.CurrentFrontier.Name, recursiveCte.NextFrontier.Name));
            loopStatements.Add(CreateAssignment(recursiveCte.NextFrontier.Name, swapName));

            var fixedPointLoop = SyntaxFactory.WhileStatement(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.GreaterThanExpression,
                    CreateRecursiveCountExpression(recursiveCte.CurrentFrontier.Name),
                    CreateIntLiteral(0)),
                StatementEmitter.CreateBlock(loopStatements));
            if (recursiveCte.InvariantSetup.Nodes.Count == 0)
            {
                statements.Add(fixedPointLoop);
            }
            else
            {
                var recursiveStatements = new List<StatementSyntax>();
                recursiveStatements.AddRange(RenderBlock(recursiveCte.InvariantSetup, context).Statements);
                recursiveStatements.Add(fixedPointLoop);
                statements.Add(SyntaxFactory.IfStatement(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.GreaterThanExpression,
                        CreateRecursiveCountExpression(recursiveCte.CurrentFrontier.Name),
                        CreateIntLiteral(0)),
                    StatementEmitter.CreateBlock(recursiveStatements)));
            }

            return statements;
        }
        finally
        {
            session.TypedRowBufferVariables = previousTypedBuffers;
        }
    }

    private static IfStatementSyntax CreateRecursiveRowLimitGuard(ExecutionRecursiveCteAppend append)
    {
        var acceptedCount = SyntaxFactory.BinaryExpression(
            SyntaxKind.AddExpression,
            CreateRecursiveCountExpression(append.Result.Name),
            CreateRecursiveCountExpression(append.Frontier.Name));
        return CreateRecursiveLimitGuard(
            append.Name,
            acceptedCount,
            append.MaxRows,
            DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded);
    }

    private static IEnumerable<StatementSyntax> RenderRecursiveCteSnapshotRowGuard(
        ExecutionRecursiveCteSnapshotRowGuard guard)
    {
        yield return CreateRecursiveLimitGuard(
            guard.Name,
            guard.Counter.Name,
            guard.MaxRows,
            DiagnosticCode.MQ7009_RecursiveCteSnapshotLimitExceeded);
        yield return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.PostfixUnaryExpression(
                SyntaxKind.PostIncrementExpression,
                SyntaxFactory.IdentifierName(guard.Counter.Name)));
    }

    private static LocalDeclarationStatementSyntax CreateRecursiveRowListDeclaration(
        ExecutionVariable variable,
        GeneratedRowShape rowShape)
    {
        var listType = CreateListTypeSyntax(rowShape.TypeName);
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            variable.Name,
            SyntaxFactory.ObjectCreationExpression(listType)
                .WithArgumentList(SyntaxFactory.ArgumentList()));
    }

    private static IfStatementSyntax CreateRecursiveLimitGuard(
        string cteName,
        string counterName,
        int limit,
        DiagnosticCode code)
    {
        return CreateRecursiveLimitGuard(
            cteName,
            SyntaxFactory.IdentifierName(counterName),
            limit,
            code);
    }

    private static IfStatementSyntax CreateRecursiveLimitGuard(
        string cteName,
        ExpressionSyntax counter,
        int limit,
        DiagnosticCode code)
    {
        var condition = SyntaxFactory.BinaryExpression(
            SyntaxKind.GreaterThanOrEqualExpression,
            counter,
            CreateIntLiteral(limit));
        var diagnosticCode = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.ParseName("global::Musoq.Parser.Diagnostics.DiagnosticCode"),
            SyntaxFactory.IdentifierName(code.ToString()));
        var exception = SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.ParseTypeName(
                    "global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException"))
            .WithArgumentList(CreateArgumentList(
                CreateStringLiteral(cteName),
                diagnosticCode,
                CreateIntLiteral(limit)));

        return SyntaxFactory.IfStatement(
            condition,
            StatementEmitter.CreateBlock(SyntaxFactory.ThrowStatement(exception)));
    }

    private static ExpressionStatementSyntax CreateAssignment(string target, string value)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(target),
                SyntaxFactory.IdentifierName(value)));
    }

    private static ExpressionSyntax CreateRecursiveCountExpression(string variableName)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(variableName),
            SyntaxFactory.IdentifierName(nameof(List<object>.Count)));
    }

}
