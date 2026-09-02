using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Runtime;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IReadOnlyList<StatementSyntax> RenderFinalShapeParallelFilterProjectLoop(
        ExecutionParallelFilterProjectLoop parallelProject,
        FinalShapeYieldSink sink,
        ExecutionRenderContext context)
    {
        var parallelRowsName = $"{parallelProject.AppendRow.Table.Name}ParallelProjectRows";
        var projectedRowsName = $"{parallelProject.AppendRow.Table.Name}ParallelProjectedShapes";
        var projectedShapeName = "__musoqProjectedShape";
        var parallelRowsDeclaration = CreateParallelProjectionRowsDeclaration(
            parallelProject,
            parallelRowsName,
            parallelProject.SourceRows,
            context);
        var projectedRowsDeclaration = CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            projectedRowsName,
            CreateFinalShapeParallelProjectionInvocation(parallelProject, parallelRowsName, sink.ShapeTypeName, context));
        var appendProjectedShapes = StatementEmitter.CreateForeach(
            projectedShapeName,
            CreateQueryRowsFromShardsInvocation(projectedRowsName),
            StatementEmitter.CreateBlock(CreateFinalShapeOutputStatement(SyntaxFactory.IdentifierName(projectedShapeName), context)));

        return
        [
            parallelRowsDeclaration,
            projectedRowsDeclaration,
            appendProjectedShapes
        ];
    }

    private InvocationExpressionSyntax CreateFinalShapeParallelProjectionInvocation(
        ExecutionParallelFilterProjectLoop parallelProject,
        string parallelRowsName,
        string shapeTypeName,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(TypedProjectionRows)),
                    SyntaxFactory.GenericName(nameof(TypedProjectionRows.ProjectOptionalValuesParallel))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(
                        [
                            CreateVariableTypeSyntax(parallelProject.Source),
                            SyntaxFactory.ParseTypeName(shapeTypeName)
                        ])))))
            .WithArgumentList(CreateArgumentList(
                SyntaxFactory.IdentifierName(parallelRowsName),
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(parallelProject.MaxDegreeOfParallelism)),
                CreateFinalShapeParallelProjectionProjector(parallelProject, shapeTypeName, context),
                SyntaxFactory.IdentifierName("token")));
    }

    private static InvocationExpressionSyntax CreateQueryRowsFromShardsInvocation(string projectedRowsName)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(QueryRows)),
                    SyntaxFactory.IdentifierName(nameof(QueryRows.FromShards))))
            .WithArgumentList(CreateArgumentList(SyntaxFactory.IdentifierName(projectedRowsName)));
    }

    private ParenthesizedLambdaExpressionSyntax CreateFinalShapeParallelProjectionProjector(
        ExecutionParallelFilterProjectLoop parallelProject,
        string shapeTypeName,
        ExecutionRenderContext context)
    {
        return CreateParallelProjectionProjector(
            parallelProject,
            appendRow => CreateFinalShapeCreation(shapeTypeName, appendRow),
            context);
    }
}
