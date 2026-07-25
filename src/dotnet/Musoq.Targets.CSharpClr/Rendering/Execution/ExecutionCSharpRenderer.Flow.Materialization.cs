using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private LocalDeclarationStatementSyntax RenderMaterializeList(
        ExecutionMaterializeList materialize,
        ExecutionRenderContext context)
    {
        if (materialize.GeneratedRowShape != null)
        {
            return CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                materialize.Buffer.Name,
                CreateMaterializeGeneratedRowsInvocation(materialize.GeneratedRowShape, materialize.Source, context));
        }

        var materializeMethodName = IsListType(materialize.Buffer.Type.RequireClrType())
            ? nameof(EvaluationHelper.MaterializeRowsList)
            : nameof(EvaluationHelper.MaterializeRows);
        var materializeInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                SyntaxFactory.IdentifierName(materializeMethodName)))
            .WithArgumentList(CreateArgumentList(RenderExpression(materialize.Source, context)));

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            materialize.Buffer.Name,
            materializeInvocation);
    }

    private LocalDeclarationStatementSyntax RenderMaterializeChunkedList(
        ExecutionMaterializeList materialize,
        ExecutionRenderContext context)
    {
        if (materialize.GeneratedRowShape != null)
        {
            return CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                materialize.Buffer.Name,
                CreateMaterializeGeneratedChunkedRowsInvocation(materialize.GeneratedRowShape, materialize.Source, context));
        }

        var materializeMethodName = IsListType(materialize.Buffer.Type.RequireClrType())
            ? nameof(EvaluationHelper.MaterializeChunkedRowsList)
            : nameof(EvaluationHelper.MaterializeChunkedRows);
        var materializeInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                SyntaxFactory.IdentifierName(materializeMethodName)))
            .WithArgumentList(CreateArgumentList(RenderExpression(materialize.Source, context)));

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            materialize.Buffer.Name,
            materializeInvocation);
    }

    private static bool IsListType(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(List<>);
    }

    private IEnumerable<StatementSyntax> RenderMaterializeFilteredList(
        ExecutionMaterializeFilteredList materialize,
        ExecutionRenderContext context)
    {
        if (materialize.GeneratedRowShape != null)
        {
            yield return CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                materialize.Buffer.Name,
                CreateMaterializeFilteredGeneratedRowsInvocation(materialize, context));
            yield break;
        }

        var elementType = ResolveMaterializedElementType(materialize);
        var bufferDeclaration = CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            materialize.Buffer.Name,
            SyntaxFactory.ObjectCreationExpression(CreateListTypeSyntax(elementType))
                .WithArgumentList(SyntaxFactory.ArgumentList()));
        var loopVariableName = materialize.Item.Name;
        var loopStatements = new List<StatementSyntax>
        {
            StatementEmitter.CreateIf(
                RenderExpression(materialize.Predicate, context),
                StatementEmitter.CreateBlock(CreateListAddStatement(materialize.Buffer.Name, loopVariableName)))
        };

        var loop = StatementEmitter.CreateForeach(
            loopVariableName,
            RenderExpression(materialize.Source, context),
            StatementEmitter.CreateBlock(loopStatements));

        yield return bufferDeclaration;
        yield return loop;
    }

    private IEnumerable<StatementSyntax> RenderMaterializeFilteredChunkedList(
        ExecutionMaterializeFilteredList materialize,
        ExecutionRenderContext context)
    {
        if (materialize.GeneratedRowShape != null)
        {
            yield return CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                materialize.Buffer.Name,
                CreateMaterializeFilteredGeneratedChunkedRowsInvocation(materialize, context));
            yield break;
        }

        var elementType = materialize.Item.Type.RequireClrType();
        var bufferDeclaration = CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            materialize.Buffer.Name,
            SyntaxFactory.ObjectCreationExpression(CreateListTypeSyntax(elementType))
                .WithArgumentList(SyntaxFactory.ArgumentList()));
        var loopVariableName = materialize.Item.Name;
        var loopStatements = new List<StatementSyntax>
        {
            StatementEmitter.CreateIf(
                RenderExpression(materialize.Predicate, context),
                StatementEmitter.CreateBlock(CreateListAddStatement(materialize.Buffer.Name, loopVariableName)))
        };

        yield return bufferDeclaration;
        yield return CreateChunkedMaterializationLoop(
            materialize.Source,
            materialize.Item,
            StatementEmitter.CreateBlock(loopStatements),
            context);
    }

    private InvocationExpressionSyntax CreateMaterializeGeneratedRowsInvocation(
        GeneratedRowShape rowShape,
        ExecutionExpression source,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.InvocationExpression(CreateGenericEvaluationHelperMemberAccess(
                nameof(EvaluationHelper.MaterializeGeneratedRows),
                rowShape.TypeName))
            .WithArgumentList(CreateArgumentList(RenderExpression(source, context)));
    }

    private InvocationExpressionSyntax CreateMaterializeGeneratedChunkedRowsInvocation(
        GeneratedRowShape rowShape,
        ExecutionExpression source,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.InvocationExpression(CreateGenericEvaluationHelperMemberAccess(
                nameof(EvaluationHelper.MaterializeGeneratedChunkedRows),
                rowShape.TypeName))
            .WithArgumentList(CreateArgumentList(RenderExpression(source, context)));
    }

    private InvocationExpressionSyntax CreateMaterializeFilteredGeneratedRowsInvocation(
        ExecutionMaterializeFilteredList materialize,
        ExecutionRenderContext context)
    {
        var predicate = SyntaxFactory.SimpleLambdaExpression(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier(materialize.Item.Name)),
            RenderExpression(materialize.Predicate, context));

        return SyntaxFactory.InvocationExpression(CreateGenericEvaluationHelperMemberAccess(
                nameof(EvaluationHelper.MaterializeFilteredGeneratedRows),
                materialize.GeneratedRowShape!.TypeName))
            .WithArgumentList(CreateArgumentList(RenderExpression(materialize.Source, context), predicate));
    }

    private InvocationExpressionSyntax CreateMaterializeFilteredGeneratedChunkedRowsInvocation(
        ExecutionMaterializeFilteredList materialize,
        ExecutionRenderContext context)
    {
        var predicate = SyntaxFactory.SimpleLambdaExpression(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier(materialize.Item.Name)),
            RenderExpression(materialize.Predicate, context));

        return SyntaxFactory.InvocationExpression(CreateGenericEvaluationHelperMemberAccess(
                nameof(EvaluationHelper.MaterializeFilteredGeneratedChunkedRows),
                materialize.GeneratedRowShape!.TypeName))
            .WithArgumentList(CreateArgumentList(RenderExpression(materialize.Source, context), predicate));
    }

    private static MemberAccessExpressionSyntax CreateGenericEvaluationHelperMemberAccess(
        string methodName,
        string typeName)
    {
        var method = SyntaxFactory.GenericName(methodName)
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.ParseTypeName(typeName))));

        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
            method);
    }

    private IEnumerable<StatementSyntax> RenderMaterializeExpandoList(
        ExecutionMaterializeExpandoList materialize,
        ExecutionRenderContext context)
    {
        var bufferDeclaration = CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            materialize.Buffer.Name,
            SyntaxFactory.ObjectCreationExpression(CreateListTypeSyntax(materialize.Shape.TypeName))
                .WithArgumentList(SyntaxFactory.ArgumentList()));
        var resolverName = CreateExpandoResolverVariableName(materialize.Shape.Alias);
        var adapterDeclaration = CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            materialize.Shape.Alias,
            CreateExpandoAdapterCreation(resolverName, materialize.Shape));
        var addStatement = CreateListAddStatement(materialize.Buffer.Name, materialize.Shape.Alias);
        var loopStatements = new List<StatementSyntax> { adapterDeclaration };

        if (materialize.Predicate == null)
        {
            loopStatements.Add(addStatement);
        }
        else
        {
            loopStatements.Add(StatementEmitter.CreateIf(
                RenderExpression(materialize.Predicate, context),
                StatementEmitter.CreateBlock(addStatement)));
        }

        var loop = StatementEmitter.CreateForeach(
            resolverName,
            RenderExpression(materialize.Source, context),
            StatementEmitter.CreateBlock(loopStatements));

        return [bufferDeclaration, loop];
    }

    private IEnumerable<StatementSyntax> RenderMaterializeChunkedExpandoList(
        ExecutionMaterializeExpandoList materialize,
        ExecutionRenderContext context)
    {
        var bufferDeclaration = CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            materialize.Buffer.Name,
            SyntaxFactory.ObjectCreationExpression(CreateListTypeSyntax(materialize.Shape.TypeName))
                .WithArgumentList(SyntaxFactory.ArgumentList()));
        var resolverName = CreateExpandoResolverVariableName(materialize.Shape.Alias);
        var adapterDeclaration = CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            materialize.Shape.Alias,
            CreateExpandoAdapterCreation(resolverName, materialize.Shape));
        var addStatement = CreateListAddStatement(materialize.Buffer.Name, materialize.Shape.Alias);
        var loopStatements = new List<StatementSyntax> { adapterDeclaration };

        if (materialize.Predicate == null)
        {
            loopStatements.Add(addStatement);
        }
        else
        {
            loopStatements.Add(StatementEmitter.CreateIf(
                RenderExpression(materialize.Predicate, context),
                StatementEmitter.CreateBlock(addStatement)));
        }

        return
        [
            bufferDeclaration,
            CreateChunkedMaterializationLoop(
                materialize.Source,
                new ExecutionVariable(resolverName, materialize.Shape.RuntimeType),
                StatementEmitter.CreateBlock(loopStatements),
                context)
        ];
    }

    private StatementSyntax CreateChunkedMaterializationLoop(
        ExecutionExpression source,
        ExecutionVariable item,
        StatementSyntax body,
        ExecutionRenderContext context)
    {
        return CreateChunkedLoop(
            item,
            source,
            context,
            (itemAccessExpression, indexVariableName) =>
            [
                ..CreateChunkedLoopBodyPrefix(item, itemAccessExpression, indexVariableName, context),
                body
            ]);
    }

    private static Type ResolveMaterializedElementType(ExecutionMaterializeFilteredList materialize)
    {
        return materialize.Item.Type.RequireClrType();
    }

    private static string CreateExpandoResolverVariableName(string alias)
    {
        return $"{alias}Resolver";
    }

    private static ExpressionStatementSyntax CreateListAddStatement(string listName, string valueName)
    {
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(listName),
                    SyntaxFactory.IdentifierName("Add")))
            .WithArgumentList(CreateArgumentList(SyntaxFactory.IdentifierName(valueName)));

        return SyntaxFactory.ExpressionStatement(invocation);
    }
}
