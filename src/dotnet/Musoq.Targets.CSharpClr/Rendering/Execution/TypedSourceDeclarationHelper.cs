using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Execution;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Targets.CSharpClr;

internal static class TypedSourceDeclarationHelper
{
    public static IReadOnlyList<StatementSyntax> Create(
        ExecutionSourceScan sourceScan,
        string schemaVariableName,
        ExpressionSyntax[] arguments,
        ExpressionSyntax runtimeContext,
        Type sourceClrType,
        Type rowType,
        string typedRowSourceName,
        string sourceContextName)
    {
        var statements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), sourceContextName, runtimeContext)
        };
        var constructorArguments = arguments.ToList();
        if (sourceScan.Binding.SourceConstructionSupportsContext)
            constructorArguments.Add(SyntaxFactory.IdentifierName(sourceContextName));

        var sourceExpression = SyntaxFactory.ObjectCreationExpression(CreateTypeSyntax(sourceClrType))
            .WithArgumentList(CreateArgumentList(constructorArguments.ToArray()));
        var openTypedName = SyntaxFactory.GenericName(nameof(DataSourceLifecycle.OpenTypedRowSource))
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SeparatedList<TypeSyntax>([
                    CreateTypeSyntax(sourceClrType),
                    CreateTypeSyntax(rowType)])));
        var typedInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(DataSourceLifecycle)),
                    openTypedName))
            .WithArgumentList(CreateArgumentList(
                SyntaxFactory.IdentifierName(schemaVariableName),
                CreateStringLiteral(sourceScan.Binding.MethodName),
                SyntaxFactory.IdentifierName($"{typedRowSourceName}Instance"),
                SyntaxFactory.IdentifierName(sourceContextName),
                CreateStringLiteral(sourceScan.Binding.SchemaName),
                CreateStringLiteral(sourceScan.Source.Name),
                CreateStringLiteral(sourceScan.Binding.RuntimeContextId)));

        statements.Add(SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(CreateTypeSyntax(typeof(RowSource<>).MakeGenericType(rowType)))
                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(typedRowSourceName)))));
        statements.Add(CreateOpenBoundary(
            SyntaxFactory.Block(
                CreateLocalDeclaration(CreateTypeSyntax(sourceClrType), $"{typedRowSourceName}Instance", sourceExpression),
                SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(typedRowSourceName),
                    typedInvocation))),
            sourceScan.Binding.SchemaName,
            sourceScan.Binding.MethodName,
            sourceScan.Source.Name,
            sourceScan.Binding.RuntimeContextId));
        return statements;
    }

    private static TryStatementSyntax CreateOpenBoundary(
        BlockSyntax body,
        string schemaName,
        string sourceName,
        string alias,
        string sourceContextId)
    {
        var lifecycleExceptionType = SyntaxFactory.ParseTypeName(
            "global::Musoq.Schema.Exceptions.DataSourceLifecycleException");
        var cancellationCatch = SyntaxFactory.CatchClause()
            .WithDeclaration(SyntaxFactory.CatchDeclaration(
                SyntaxFactory.ParseTypeName(nameof(OperationCanceledException))))
            .WithBlock(SyntaxFactory.Block(SyntaxFactory.ThrowStatement()));
        var lifecycleCatch = SyntaxFactory.CatchClause()
            .WithDeclaration(SyntaxFactory.CatchDeclaration(lifecycleExceptionType))
            .WithBlock(SyntaxFactory.Block(SyntaxFactory.ThrowStatement()));
        var exceptionName = SyntaxFactory.Identifier("exception");
        var openFailure = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    lifecycleExceptionType,
                    SyntaxFactory.IdentifierName("ForOpen")))
            .WithArgumentList(CreateArgumentList(
                CreateStringLiteral(schemaName),
                CreateStringLiteral(sourceName),
                CreateStringLiteral(alias),
                CreateStringLiteral(sourceContextId),
                SyntaxFactory.IdentifierName(exceptionName)));
        var generalCatch = SyntaxFactory.CatchClause()
            .WithDeclaration(SyntaxFactory.CatchDeclaration(
                SyntaxFactory.ParseTypeName(nameof(Exception)),
                exceptionName))
            .WithBlock(SyntaxFactory.Block(SyntaxFactory.ThrowStatement(openFailure)));
        return SyntaxFactory.TryStatement(
            body,
            SyntaxFactory.List([cancellationCatch, lifecycleCatch, generalCatch]),
            null);
    }
}
