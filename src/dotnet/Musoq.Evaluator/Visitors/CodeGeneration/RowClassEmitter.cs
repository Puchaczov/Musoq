using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
        var (fields, constructorParams, constructorBody, valuesInit, fieldNames) = GenerateFieldsAndConstructorParams(node);
        var (contextParams, contextBody) = GenerateContextParams(scope);

        constructorParams.AddRange(contextParams);
        constructorBody.AddRange(contextBody);

        var constructor = CreateConstructor(className, constructorParams, constructorBody);
        var indexer = CreateIndexer(node.Fields.Length, i => fieldNames[i]);
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
        List<StatementSyntax> ConstructorBody, List<ExpressionSyntax> ValuesInit, List<string> FieldNames)
        GenerateFieldsAndConstructorParams(SelectNode node)
    {
        var fields = new List<MemberDeclarationSyntax>();
        var constructorParams = new List<ParameterSyntax>();
        var constructorBody = new List<StatementSyntax>();
        var valuesInit = new List<ExpressionSyntax>();
        var fieldNames = new List<string>();
        var usedNames = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < node.Fields.Length; i++)
        {
            var fieldName = SanitizeFieldName(node.Fields[i].FieldName, i, usedNames);
            var type = node.Fields[i].Expression.ReturnType;
            var typeSyntax = SyntaxFactory.ParseTypeName(EvaluationHelper.GetCastableType(type));

            fields.Add(CreatePublicField(fieldName, typeSyntax, node.Fields[i].FieldName));

            var paramName = DeriveParameterName(fieldName);
            constructorParams.Add(CreateParameter(paramName, typeSyntax));
            constructorBody.Add(CreateFieldAssignment(fieldName, paramName));
            valuesInit.Add(SyntaxFactory.IdentifierName(fieldName));
            fieldNames.Add(fieldName);
        }

        return (fields, constructorParams, constructorBody, valuesInit, fieldNames);
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

    private static readonly HashSet<string> CSharpKeywords =
    [
        "abstract", "as", "async", "await", "base", "bool", "break", "byte", "case", "catch", "char",
        "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double",
        "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "var", "virtual",
        "void", "volatile", "while"
    ];

    private static readonly HashSet<string> ReservedRowMembers =
    [
        "Contexts", "Values", "Count", "_values"
    ];

    private static string SanitizeFieldName(string columnName, int index, HashSet<string> usedNames)
    {
        var sb = new StringBuilder(columnName.Length);

        foreach (var ch in columnName)
        {
            if (char.IsLetterOrDigit(ch))
                sb.Append(ch);
            else if (ch is '_')
                sb.Append(ch);
            else if (sb.Length > 0 && sb[sb.Length - 1] != '_')
                sb.Append('_');
        }

        if (sb.Length == 0 || char.IsDigit(sb[0]))
            sb.Insert(0, '_');

        var candidate = sb.ToString();

        if (CSharpKeywords.Contains(candidate) || ReservedRowMembers.Contains(candidate))
            candidate = $"_{candidate}";

        if (!usedNames.Add(candidate))
        {
            candidate = $"{candidate}_{index}";
            usedNames.Add(candidate);
        }

        return candidate;
    }

    private static string DeriveParameterName(string fieldName)
    {
        if (fieldName.Length == 0)
            return "p";

        var lowered = $"{char.ToLower(fieldName[0], CultureInfo.InvariantCulture)}{fieldName.Substring(1)}";

        if (lowered == fieldName || CSharpKeywords.Contains(lowered))
            return $"p_{fieldName}";

        return lowered;
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

    private static IndexerDeclarationSyntax CreateIndexer(int fieldCount, Func<int, string> fieldNameForIndex)
    {
        var arms = new List<SwitchExpressionArmSyntax>();

        for (var i = 0; i < fieldCount; i++)
            arms.Add(
                SyntaxFactory.SwitchExpressionArm(
                    SyntaxFactory.ConstantPattern(
                        SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(i))),
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                        SyntaxFactory.IdentifierName(fieldNameForIndex(i)))));

        arms.Add(
            SyntaxFactory.SwitchExpressionArm(
                SyntaxFactory.DiscardPattern(),
                SyntaxFactory.ThrowExpression(
                    SyntaxFactory.ObjectCreationExpression(SyntaxHelper.IndexOutOfRangeExceptionTypeSyntax)
                        .WithArgumentList(SyntaxFactory.ArgumentList()))));

        var switchExpression = SyntaxFactory.SwitchExpression(
            SyntaxFactory.IdentifierName("index"),
            SyntaxFactory.SeparatedList(arms));

        return SyntaxFactory
            .IndexerDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)))
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.OverrideKeyword)))
            .WithParameterList(SyntaxFactory.BracketedParameterList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("index"))
                    .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword))))))
            .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(switchExpression))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
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
