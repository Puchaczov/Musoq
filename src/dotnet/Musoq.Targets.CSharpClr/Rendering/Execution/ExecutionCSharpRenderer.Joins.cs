using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tables;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderParallelFilterProjectLoop(
        ExecutionParallelFilterProjectLoop parallelProject,
        ExecutionRenderContext context)
    {
        if (context.Session.FinalShapeYieldSink is { } sink &&
            string.Equals(parallelProject.AppendRow.Table.Name, sink.TableName, StringComparison.Ordinal))
        {
            return RenderFinalShapeParallelFilterProjectLoop(parallelProject, sink, context);
        }

        var arguments = new List<ExpressionSyntax>
        {
            SyntaxFactory.IdentifierName(parallelProject.AppendRow.Table.Name),
            RenderExpression(parallelProject.SourceRows, context),
            SyntaxFactory.IdentifierName("token")
        };
        AddProfileRecorderArgument(arguments);
        arguments.AddRange(CollectParallelFilterProjectCaptures(parallelProject)
            .Select(CreateCapturedLocalArgument));

        return
        [
            SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName(CreateParallelFilterProjectFunctionName(parallelProject, context)))
                .WithArgumentList(CreateArgumentList(arguments)))
        ];
    }

    private MethodDeclarationSyntax CreateParallelFilterProjectFunction(
        ExecutionParallelFilterProjectLoop parallelProject,
        ExecutionRenderContext context)
    {
        var captures = CollectParallelFilterProjectCaptures(parallelProject);
        var rowsParameterName = CreateParallelFilterProjectRowsParameterName(parallelProject);
        var previousProfileRecorderInScope = context.Session.ProfileRecorderInScope;
        context.Session.ProfileRecorderInScope = IsInstrumentationEnabled;

        try
        {
            return SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                    CreateParallelFilterProjectFunctionName(parallelProject, context))
                .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
                .WithModifiers(SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .WithParameterList(CreateParallelFilterProjectParameterList(parallelProject, rowsParameterName, captures))
                .WithBody(CreateProfiledHelperBody(CreateParallelFilterProjectStatements(
                    parallelProject,
                    new ExecutionVariableRead(new ExecutionVariable(
                        rowsParameterName,
                        typeof(object),
                        parallelProject.Source.GeneratedRowTypeName)),
                    context),
                    context));
        }
        finally
        {
            context.Session.ProfileRecorderInScope = previousProfileRecorderInScope;
        }
    }

    private ParameterListSyntax CreateParallelFilterProjectParameterList(
        ExecutionParallelFilterProjectLoop parallelProject,
        string rowsParameterName,
        IReadOnlyList<CapturedLocal> captures)
    {
        var parameters = new List<ParameterSyntax>
        {
            CreateParameter(parallelProject.AppendRow.Table.Name, CreateTypeSyntax(typeof(Table))),
            CreateParameter(rowsParameterName, CreateParallelFilterProjectRowsParameterType(parallelProject)),
            CreateParameter("token", CreateTypeSyntax(typeof(CancellationToken)))
        };

        AddProfileRecorderParameter(parameters);
        parameters.AddRange(captures.Select(CreateCapturedLocalParameter));

        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }

    private IReadOnlyList<StatementSyntax> CreateParallelFilterProjectStatements(
        ExecutionParallelFilterProjectLoop parallelProject,
        ExecutionExpression sourceRows,
        ExecutionRenderContext context)
    {
        var parallelRowsName = $"{parallelProject.AppendRow.Table.Name}ParallelProjectRows";
        var projectedRowsName = $"{parallelProject.AppendRow.Table.Name}ParallelProjectedRows";
        var parallelRowsDeclaration = CreateParallelProjectionRowsDeclaration(parallelProject, parallelRowsName, sourceRows, context);
        var projectedRowsDeclaration = CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            projectedRowsName,
            CreateParallelProjectionInvocation(parallelProject, parallelRowsName, context));
        var appendProjectedRows = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                        SyntaxFactory.IdentifierName(nameof(EvaluationHelper.AddRowsDirect))))
                .WithArgumentList(CreateArgumentList(
                    SyntaxFactory.IdentifierName(parallelProject.AppendRow.Table.Name),
                    SyntaxFactory.IdentifierName(projectedRowsName))));

        return
        [
            parallelRowsDeclaration,
            projectedRowsDeclaration,
            appendProjectedRows
        ];
    }

    private static TypeSyntax CreateParallelFilterProjectRowsParameterType(ExecutionParallelFilterProjectLoop parallelProject)
    {
        var sourceType = CreateVariableTypeSyntax(parallelProject.Source);
        return ExecutionRowStreams.IsChunked(parallelProject.SourceRows)
            ? CreateEnumerableTypeSyntax(CreateReadOnlyListTypeSyntax(sourceType))
            : CreateEnumerableTypeSyntax(sourceType);
    }

    private CapturedLocal[] CollectParallelFilterProjectCaptures(
        ExecutionParallelFilterProjectLoop parallelProject)
    {
        var excludedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            parallelProject.Source.Name,
            parallelProject.AppendRow.Table.Name,
            CreateParallelFilterProjectRowsParameterName(parallelProject),
            "token"
        };
        AddProfileRecorderExcludedName(excludedNames);

        foreach (var variableName in CollectDeclaredVariableNames(parallelProject.ProjectionBody))
            excludedNames.Add(variableName);

        var captures = new Dictionary<string, CapturedLocal>(StringComparer.Ordinal);
        AddHelperCaptures(parallelProject.ProjectionBody, excludedNames, captures);
        return captures.Values.ToArray();
    }

    private static string CreateParallelFilterProjectRowsParameterName(ExecutionParallelFilterProjectLoop parallelProject)
    {
        return CreateIdentifierCandidate($"{parallelProject.Source.Name}Rows", 0);
    }

    private LocalDeclarationStatementSyntax CreateParallelProjectionRowsDeclaration(
        ExecutionParallelFilterProjectLoop parallelProject,
        string parallelRowsName,
        ExecutionExpression sourceRows,
        ExecutionRenderContext context)
    {
        var initializer = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    SyntaxFactory.GenericName(nameof(EvaluationHelper.GetParallelProjectionRowsOrEmpty))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            CreateVariableTypeSyntax(parallelProject.Source))))))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Argument(RenderExpression(sourceRows, context)),
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(parallelProject.Threshold)))
            ])));

        return CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), parallelRowsName, initializer);
    }

    private InvocationExpressionSyntax CreateParallelProjectionInvocation(
        ExecutionParallelFilterProjectLoop parallelProject,
        string parallelRowsName,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    SyntaxFactory.GenericName(nameof(EvaluationHelper.ProjectRowsParallel))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(
                        [
                            CreateVariableTypeSyntax(parallelProject.Source),
                            SyntaxFactory.ParseTypeName(parallelProject.AppendRow.RowShape.TypeName)
                        ])))))
            .WithArgumentList(CreateArgumentList(
                SyntaxFactory.IdentifierName(parallelRowsName),
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(parallelProject.MaxDegreeOfParallelism)),
                CreateParallelProjectionProjector(parallelProject, context),
                SyntaxFactory.IdentifierName("token")));
    }

    private ParenthesizedLambdaExpressionSyntax CreateParallelProjectionProjector(
        ExecutionParallelFilterProjectLoop parallelProject,
        ExecutionRenderContext context)
    {
        return CreateParallelProjectionProjector(
            parallelProject,
            appendRow => CreateGeneratedRowCreation(appendRow, context),
            context);
    }

    private ParenthesizedLambdaExpressionSyntax CreateParallelProjectionProjector(
        ExecutionParallelFilterProjectLoop parallelProject,
        Func<ExecutionAppendRow, ExpressionSyntax> createProjection,
        ExecutionRenderContext context)
    {
        return CreateOptionalProjectionProjector(
            parallelProject.ProjectionBody,
            parallelProject.Source,
            createProjection,
            context);
    }

    private ParenthesizedLambdaExpressionSyntax CreateOptionalProjectionProjector(
        ExecutionBlock projectionBody,
        ExecutionVariable source,
        Func<ExecutionAppendRow, ExpressionSyntax> createProjection,
        ExecutionRenderContext context)
    {
        var statements = CreateParallelProjectionProjectorStatements(
            projectionBody,
            createProjection,
            context).ToList();
        statements.Add(SyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));

        return SyntaxFactory.ParenthesizedLambdaExpression()
            .WithParameterList(CreateParallelSourceParameterList(source))
            .WithBlock(StatementEmitter.CreateBlock(statements));
    }

    private IEnumerable<StatementSyntax> CreateParallelProjectionProjectorStatements(
        ExecutionBlock block,
        Func<ExecutionAppendRow, ExpressionSyntax> createProjection,
        ExecutionRenderContext context)
    {
        foreach (var node in block.Nodes)
        {
            foreach (var statement in CreateParallelProjectionProjectorStatements(node, createProjection, context))
                yield return statement;
        }
    }

    private IEnumerable<StatementSyntax> CreateParallelProjectionProjectorStatements(
        ExecutionNode node,
        Func<ExecutionAppendRow, ExpressionSyntax> createProjection,
        ExecutionRenderContext context)
    {
        switch (node)
        {
            case ExecutionLet let:
                yield return RenderLet(let, context);
                break;
            case ExecutionIf branch:
                yield return SyntaxFactory.IfStatement(
                    RenderExpression(branch.Condition, context),
                    StatementEmitter.CreateBlock(CreateParallelProjectionProjectorStatements(branch.Body, createProjection, context)));
                break;
            case ExecutionAppendRow appendRow:
                yield return SyntaxFactory.ReturnStatement(createProjection(appendRow));
                break;
            default:
                throw new InvalidOperationException(
                    $"Parallel filter/project projector cannot render node '{node.GetType().Name}'.");
        }
    }

    private static ParameterListSyntax CreateParallelSourceParameterList(ExecutionVariable source)
    {
        return SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier(EscapeIdentifier(source.Name)))));
    }

}
