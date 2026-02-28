using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
///     Emits Row class declarations for query results.
/// </summary>
internal static class RowClassEmitter
{
    /// <summary>
    ///     Generates a Row class that holds the selected fields from a query.
    /// </summary>
    /// <param name="className">The name of the class to generate.</param>
    /// <param name="node">The select node containing field definitions.</param>
    /// <param name="scope">The scope containing metadata like contexts.</param>
    /// <returns>A class declaration syntax.</returns>
    public static MemberDeclarationSyntax GenerateRowClass(string className, SelectNode node, Scope scope)
    {
        var (fields, constructorParams, constructorBody, valuesInit) = GenerateFieldsAndConstructorParams(node);
        var (contextParams, contextBody) = GenerateContextParams(scope);

        constructorParams.AddRange(contextParams);
        constructorBody.AddRange(contextBody);

        var constructor = CreateConstructor(className, constructorParams, constructorBody);
        var indexer = CreateIndexer(node.Fields.Length);
        var countProp = CreateCountProperty(node.Fields.Length);
        var (valuesBackingField, valuesProp) = CreateValuesPropertyWithCache(valuesInit);
        var contextsProp = CreateContextsProperty();

        fields.Add(valuesBackingField);
        fields.Add(contextsProp);

        return SyntaxFactory.ClassDeclaration(className)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                SyntaxFactory.SimpleBaseType(SyntaxHelper.RowTypeSyntax))))
            .WithMembers(SyntaxFactory.List(fields.Concat([constructor, indexer, countProp, valuesProp])));
    }

    private static (List<MemberDeclarationSyntax> Fields, List<ParameterSyntax> ConstructorParams,
        List<StatementSyntax> ConstructorBody, List<ExpressionSyntax> ValuesInit)
        GenerateFieldsAndConstructorParams(SelectNode node)
    {
        var fields = new List<MemberDeclarationSyntax>();
        var constructorParams = new List<ParameterSyntax>();
        var constructorBody = new List<StatementSyntax>();
        var valuesInit = new List<ExpressionSyntax>();

        for (var i = 0; i < node.Fields.Length; i++)
        {
            var fieldName = $"Item{i}";
            var type = node.Fields[i].Expression.ReturnType;
            var typeSyntax = SyntaxFactory.ParseTypeName(EvaluationHelper.GetCastableType(type));

            fields.Add(CreatePublicField(fieldName, typeSyntax, node.Fields[i].FieldName));

            var paramName = $"item{i}";
            constructorParams.Add(CreateParameter(paramName, typeSyntax));
            constructorBody.Add(CreateFieldAssignment(fieldName, paramName));
            valuesInit.Add(SyntaxFactory.IdentifierName(fieldName));
        }

        return (fields, constructorParams, constructorBody, valuesInit);
    }

    private static (List<ParameterSyntax> Params, List<StatementSyntax> Body) GenerateContextParams(Scope scope)
    {
        var contexts = scope[MetaAttributes.Contexts].Split(',');
        var contextParams = new List<ParameterSyntax>();
        var contextExprs = new List<ExpressionSyntax>();

        for (var i = 0; i < contexts.Length; i++)
        {
            var paramName = $"context{i}";
            contextParams.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier(paramName))
                .WithType(SyntaxHelper.ObjectArrayTypeSyntax));
            contextExprs.Add(SyntaxFactory.IdentifierName(paramName));
        }

        ExpressionSyntax contextsExpression;
        if (contexts.Length == 1)
        {
            contextsExpression = SyntaxFactory.BinaryExpression(
                SyntaxKind.CoalesceExpression,
                contextExprs[0],
                SyntaxFactory.ArrayCreationExpression(
                    SyntaxFactory.ArrayType(
                            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)))
                        .WithRankSpecifiers(SyntaxFactory.SingletonList(
                            SyntaxFactory.ArrayRankSpecifier(
                                SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                    SyntaxFactory.OmittedArraySizeExpression())))),
                    SyntaxFactory.InitializerExpression(
                        SyntaxKind.ArrayInitializerExpression,
                        SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)))));
        }
        else
        {
            contextsExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper.FlattenContexts))),
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                    contextExprs.Select(SyntaxFactory.Argument))));
        }

        var contextBody = new List<StatementSyntax>
        {
            SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName("Contexts"),
                    contextsExpression))
        };

        return (contextParams, contextBody);
    }

    private static MemberDeclarationSyntax CreatePublicField(string fieldName, TypeSyntax typeSyntax, string comment)
    {
        var sanitizedComment = comment.Replace("\n", "\\n").Replace("\r", "\\r");
        return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(typeSyntax)
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(fieldName))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithTrailingTrivia(SyntaxFactory.Comment($"// {sanitizedComment}"));
    }

    private static ParameterSyntax CreateParameter(string name, TypeSyntax typeSyntax)
    {
        return SyntaxFactory.Parameter(SyntaxFactory.Identifier(name)).WithType(typeSyntax);
    }

    private static StatementSyntax CreateFieldAssignment(string fieldName, string paramName)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(fieldName),
                SyntaxFactory.IdentifierName(paramName)));
    }

    private static ConstructorDeclarationSyntax CreateConstructor(
        string className,
        List<ParameterSyntax> parameters,
        List<StatementSyntax> body)
    {
        return SyntaxFactory.ConstructorDeclaration(className)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)))
            .WithBody(StatementEmitter.CreateBlock(body.ToArray()));
    }

    private static IndexerDeclarationSyntax CreateIndexer(int fieldCount)
    {
        var switchSections = new List<SwitchSectionSyntax>();

        for (var i = 0; i < fieldCount; i++)
            switchSections.Add(
                SyntaxFactory.SwitchSection()
                    .WithLabels(SyntaxFactory.SingletonList<SwitchLabelSyntax>(
                        SyntaxFactory.CaseSwitchLabel(
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(i)))))
                    .WithStatements(SyntaxFactory.SingletonList<StatementSyntax>(
                        StatementEmitter.CreateReturn(SyntaxFactory.IdentifierName($"Item{i}")))));

        switchSections.Add(
            SyntaxFactory.SwitchSection()
                .WithLabels(SyntaxFactory.SingletonList<SwitchLabelSyntax>(SyntaxFactory.DefaultSwitchLabel()))
                .WithStatements(SyntaxFactory.SingletonList<StatementSyntax>(
                    StatementEmitter.CreateThrow(
                        SyntaxFactory.ObjectCreationExpression(SyntaxHelper.IndexOutOfRangeExceptionTypeSyntax)
                            .WithArgumentList(SyntaxFactory.ArgumentList())))));

        var switchStatement = SyntaxFactory.SwitchStatement(SyntaxFactory.IdentifierName("index"))
            .WithSections(SyntaxFactory.List(switchSections));

        var indexerBody = SyntaxFactory.Block(switchStatement);

        return SyntaxFactory
            .IndexerDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)))
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
            .WithParameterList(SyntaxFactory.BracketedParameterList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("index"))
                    .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword))))))
            .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithBody(indexerBody))));
    }

    private static PropertyDeclarationSyntax CreateCountProperty(int count)
    {
        return SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
                "Count")
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
            .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(count))))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private static (FieldDeclarationSyntax BackingField, PropertyDeclarationSyntax Property)
        CreateValuesPropertyWithCache(List<ExpressionSyntax> valuesInit)
    {
        var backingField = SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.NullableType(SyntaxHelper.ObjectArrayTypeSyntax))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator("_values"))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));

        var arrayCreation = SyntaxFactory.ArrayCreationExpression(
            SyntaxHelper.ObjectArrayTypeSyntax,
            SyntaxFactory.InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                SyntaxFactory.SeparatedList(valuesInit)));

        var coalescingAssignment = SyntaxFactory.AssignmentExpression(
            SyntaxKind.CoalesceAssignmentExpression,
            SyntaxFactory.IdentifierName("_values"),
            arrayCreation);

        var property = SyntaxFactory.PropertyDeclaration(SyntaxHelper.ObjectArrayTypeSyntax, "Values")
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
            .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(coalescingAssignment))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

        return (backingField, property);
    }

    private static PropertyDeclarationSyntax CreateContextsProperty()
    {
        return SyntaxFactory.PropertyDeclaration(SyntaxHelper.ObjectArrayTypeSyntax, "Contexts")
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
            .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))));
    }
}
