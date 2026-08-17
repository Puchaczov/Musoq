using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private LocalDeclarationStatementSyntax RenderLet(
        ExecutionLet let,
        ExecutionRenderContext context)
    {
        return CreateLocalDeclaration(
            CreateVariableTypeSyntax(let.Variable),
            let.Variable.Name,
            RenderExpression(let.Value, context));
    }

    private ExpressionStatementSyntax RenderAssign(ExecutionAssign assign)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateIdentifierName(assign.Variable.Name),
                RenderExpression(assign.Value)));
    }

    private static LocalDeclarationStatementSyntax RenderCreateBooleanArray(ExecutionCreateBooleanArray createArray)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.ArrayType(CreateTypeSyntax(typeof(bool)))
                .WithRankSpecifiers(SyntaxFactory.SingletonList(
                    SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                        SyntaxFactory.OmittedArraySizeExpression())))),
            createArray.Array.Name,
            SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.ArrayType(CreateTypeSyntax(typeof(bool)))
                    .WithRankSpecifiers(SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            CreateBufferCountExpression(createArray.LengthSource)))))));
    }

    private ExpressionStatementSyntax RenderArrayAssign(ExecutionArrayAssign arrayAssign)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateElementAccess(
                    SyntaxFactory.IdentifierName(arrayAssign.Array.Name),
                    RenderExpression(arrayAssign.Index)),
                SyntaxFactory.CastExpression(
                    CreateTypeSyntax(arrayAssign.ElementType),
                    SyntaxFactory.ParenthesizedExpression(RenderExpression(arrayAssign.Value)))));
    }

    private LocalDeclarationStatementSyntax RenderAdaptExpando(ExecutionAdaptExpando adapt)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            adapt.Target.Name,
            CreateExpandoAdapterCreation(adapt.Source.Name, adapt.Shape));
    }

    private static LocalDeclarationStatementSyntax RenderCreateObject(ExecutionCreateObject createObject)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            createObject.Target.Name,
            SyntaxFactory.ObjectCreationExpression(CreateTypeSyntax(createObject.Target.Type))
                .WithArgumentList(SyntaxFactory.ArgumentList()));
    }

    private ObjectCreationExpressionSyntax CreateExpandoAdapterCreation(string resolverName, ExpandoAdapterShape shape)
    {
        var values = shape.Fields
            .Select<FieldBinding, ExpressionSyntax>(field => RenderDynamicResolverRead(resolverName, shape.RuntimeType.RequireClrType(), field))
            .ToArray();

        return CreateObjectCreation(shape.TypeName, values);
    }

    private ConditionalExpressionSyntax RenderDynamicResolverRead(string resolverName, Type runtimeType, FieldBinding field)
    {
        var key = CreateStringLiteral(GetExpandoKey(field));
        var source = CreateDynamicDictionarySource(resolverName, runtimeType);
        var valueName = CreateIdentifierName(
            $"__dynamicValue{_dynamicResolverValueSequence++}_{field.OutputIndex}");
        var value = valueName;
        var valueDeclaration = SyntaxFactory.DeclarationExpression(
            SyntaxFactory.IdentifierName("var"),
            SyntaxFactory.SingleVariableDesignation(valueName.Identifier));
        var tryGetValue = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    source,
                    SyntaxFactory.IdentifierName("TryGetValue")))
            .WithArgumentList(SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(
                [
                    SyntaxFactory.Argument(key),
                    SyntaxFactory.Argument(valueDeclaration)
                        .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))
                ])));

        if (field.Type.RequireClrType() == typeof(object) || DynamicEntityBoundary.IsDynamicMetaObjectProvider(field.Type.RequireClrType()))
        {
            return SyntaxFactory.ConditionalExpression(
                tryGetValue,
                value,
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
        }

        return SyntaxFactory.ConditionalExpression(
            tryGetValue,
            SyntaxFactory.CastExpression(CreateTypeSyntax(field.Type), value),
            SyntaxFactory.DefaultExpression(CreateTypeSyntax(field.Type)));
    }

    private static ExpressionSyntax CreateDynamicDictionarySource(string resolverName, Type runtimeType)
    {
        var source = SyntaxFactory.IdentifierName(resolverName);

        if (CanUseDictionaryMembers(runtimeType))
            return source;

        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.CastExpression(
                CreateTypeSyntax(DynamicEntityBoundary.StringObjectDictionaryType),
                source));
    }

    private static bool CanUseDictionaryMembers(Type runtimeType)
    {
        return DynamicEntityBoundary.IsStringObjectDictionaryContext(runtimeType);
    }

    private static string GetExpandoKey(FieldBinding field)
    {
        return field.AccessStrategy is ExpandoDictionaryAccess expando
            ? expando.Key
            : field.Name;
    }

    private IfStatementSyntax RenderIf(
        ExecutionIf branch,
        ExecutionRenderContext context)
    {
        return StatementEmitter.CreateIf(RenderExpression(branch.Condition, context), RenderBlock(branch.Body, context));
    }

    private IfStatementSyntax RenderContinueIf(
        ExecutionContinueIf continueIf,
        ExecutionRenderContext context)
    {
        return StatementEmitter.CreateIf(
            RenderExpression(continueIf.Condition, context),
            StatementEmitter.CreateBlock(SyntaxFactory.ContinueStatement()));
    }
}
