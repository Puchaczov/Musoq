using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Tables;

namespace Musoq.Targets.CSharpClr;

/// <summary>
/// Stateless Roslyn syntax-construction primitives shared across the
/// C# target renderer partials. Extracted from the renderer
/// god-class so syntax fabrication has a single cohesive home and the renderer
/// itself stays focused on orchestration. Members are surfaced to the renderer
/// through a <c>global using static</c> import.
/// </summary>
internal static class ExecutionSyntaxFactory
{
    internal static TypeSyntax CreateGroupDictionaryTypeSyntax(Type keyType, TypeSyntax groupType)
    {
        return SyntaxFactory.ParseTypeName($"Dictionary<{EvaluationHelper.GetCastableType(keyType)}, {groupType}>");
    }

    internal static TypeSyntax CreateGroupDictionaryTypeSyntax(ExecutionTypeRef keyType, TypeSyntax groupType) =>
        CreateGroupDictionaryTypeSyntax(keyType.RequireClrType(), groupType);

    internal static TypeSyntax CreateValueTupleGroupDictionaryTypeSyntax(
        IReadOnlyList<Type> keyTypes,
        int keyCount,
        TypeSyntax groupType)
    {
        return SyntaxFactory.ParseTypeName($"Dictionary<{CreateValueTupleTypeName(keyTypes, keyCount)}, {groupType}>");
    }

    internal static TypeSyntax CreateValueTupleGroupDictionaryTypeSyntax(
        IReadOnlyList<ExecutionTypeRef> keyTypes,
        int keyCount,
        TypeSyntax groupType) =>
        CreateValueTupleGroupDictionaryTypeSyntax(keyTypes.RequireClrTypes(), keyCount, groupType);

    internal static string CreateValueTupleTypeName(IReadOnlyList<Type> keyTypes, int keyCount)
    {
        if (keyCount == 1)
            return $"ValueTuple<{EvaluationHelper.GetCastableType(keyTypes[0])}>";

        return $"({string.Join(", ", keyTypes.Take(keyCount).Select(EvaluationHelper.GetCastableType))})";
    }

    internal static string CreateGroupKeyVariableName(int index)
    {
        return $"groupKey{index.ToString(CultureInfo.InvariantCulture)}";
    }

    internal static ExpressionSyntax[] CreateAggregateGroupDefaultKeyArguments(AggregateGroupShape shape)
    {
        return shape.Keys
            .Select(static key => (ExpressionSyntax)SyntaxFactory.DefaultExpression(CreateTypeSyntax(key.Type)))
            .ToArray();
    }

    internal static ExpressionSyntax[] CreateAggregateGroupKeyArguments(
        AggregateGroupShape shape,
        int knownKeyCount)
    {
        return shape.Keys
            .Select((key, index) => index < knownKeyCount
                ? (ExpressionSyntax)SyntaxFactory.IdentifierName(CreateGroupKeyVariableName(index))
                : SyntaxFactory.DefaultExpression(CreateTypeSyntax(key.Type)))
            .ToArray();
    }

    internal static TypeSyntax CreateListTypeSyntax(Type rowType)
    {
        return SyntaxFactory.ParseTypeName($"List<{EvaluationHelper.GetCastableType(rowType)}>");
    }

    internal static TypeSyntax CreateListTypeSyntax(string rowTypeName)
    {
        return SyntaxFactory.ParseTypeName($"List<{rowTypeName}>");
    }

    internal static TypeSyntax CreateListTypeSyntax(TypeSyntax rowType)
    {
        return SyntaxFactory.ParseTypeName($"List<{rowType}>");
    }

    internal static bool CanBeNull(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
    }

    internal static bool CanBeNull(ExecutionTypeRef type) => CanBeNull(type.RequireClrType());

    internal static ObjectCreationExpressionSyntax CreateColumnCreation(FieldBinding field)
    {
        return CreateObjectCreation(
            "Column",
            CreateStringLiteral(field.Name),
            SyntaxFactory.TypeOfExpression(CreateTypeOfTypeSyntax(field.ColumnType)),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(field.OutputIndex)));
    }

    internal static ExpressionSyntax CreateColumnCreation(ExecutionColumnMetadataField field)
    {
        return CreateObjectCreation(
            nameof(Column),
            CreateStringLiteral(field.Name),
            SyntaxFactory.TypeOfExpression(CreateTypeOfTypeSyntax(field.Type)),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(field.Index)));
    }

    internal static ExpressionSyntax CreateSchemaColumnCreation(ExecutionColumnMetadataField field)
    {
        if (field.ReadModifiers.Count == 0)
            return CreateColumnCreation(field);

        return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.ParseTypeName("global::Musoq.Schema.DataSources.SchemaColumn"))
            .WithArgumentList(CreateArgumentList(
                CreateStringLiteral(field.Name),
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(field.Index)),
                SyntaxFactory.TypeOfExpression(CreateTypeOfTypeSyntax(field.Type)),
                CSharpReadModifierMetadata.CreateDictionaryCreation(field.ReadModifiers)));
    }

    internal static LocalDeclarationStatementSyntax CreateLocalDeclaration(
        TypeSyntax type,
        string variableName,
        ExpressionSyntax initializer)
    {
        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(type)
                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(EscapeIdentifier(variableName))
                        .WithInitializer(SyntaxFactory.EqualsValueClause(initializer)))));
    }

    internal static ObjectCreationExpressionSyntax CreateObjectCreation(
        string typeName,
        params ExpressionSyntax[] arguments)
    {
        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(typeName))
            .WithArgumentList(CreateArgumentList(arguments));
    }

    internal static ArrayCreationExpressionSyntax CreateArrayCreation(
        string elementTypeName,
        IEnumerable<ExpressionSyntax> expressions)
    {
        return SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName(elementTypeName))
                    .WithRankSpecifiers(SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.OmittedArraySizeExpression())))))
            .WithInitializer(SyntaxFactory.InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                SyntaxFactory.SeparatedList(expressions)));
    }

    internal static ArrayCreationExpressionSyntax CreateArrayCreation(
        Type elementType,
        IEnumerable<ExpressionSyntax> expressions)
    {
        return SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.ArrayType(CreateTypeSyntax(elementType))
                    .WithRankSpecifiers(SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.OmittedArraySizeExpression())))))
            .WithInitializer(SyntaxFactory.InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                SyntaxFactory.SeparatedList(expressions)));
    }

    internal static ArrayCreationExpressionSyntax CreateArrayCreation(
        ExecutionTypeRef elementType,
        IEnumerable<ExpressionSyntax> expressions) =>
        CreateArrayCreation(elementType.RequireClrType(), expressions);

    internal static ElementAccessExpressionSyntax CreateElementAccess(
        ExpressionSyntax expression,
        ExpressionSyntax indexExpression)
    {
        return SyntaxFactory.ElementAccessExpression(expression)
            .WithArgumentList(SyntaxFactory.BracketedArgumentList(
                SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(indexExpression))));
    }

    internal static ArgumentListSyntax CreateArgumentList(params ExpressionSyntax[] expressions)
    {
        return CreateArgumentList((IEnumerable<ExpressionSyntax>)expressions);
    }

    internal static ArgumentListSyntax CreateArgumentList(IEnumerable<ExpressionSyntax> expressions)
    {
        return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(expressions.Select(SyntaxFactory.Argument)));
    }

    internal static LiteralExpressionSyntax CreateStringLiteral(string value)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value));
    }

    internal static TypeSyntax CreateTypeSyntax(Type type)
    {
        if (DynamicEntityBoundary.IsDynamicMetaObjectProvider(type))
            return SyntaxFactory.IdentifierName("dynamic");

        return SyntaxFactory.ParseTypeName(EvaluationHelper.GetCastableType(type));
    }

    internal static TypeSyntax CreateTypeSyntax(ExecutionTypeRef type) =>
        CreateTypeSyntax(type.RequireClrType());

    internal static ExpressionSyntax CreateArrayPoolRentExpression(Type elementType, ExpressionSyntax minimumLength)
    {
        var source = minimumLength.NormalizeWhitespace().ToFullString();
        return SyntaxFactory.ParseExpression(
            $"System.Buffers.ArrayPool<{EvaluationHelper.GetCastableType(elementType)}>.Shared.Rent({source})");
    }

    internal static StatementSyntax CreateArrayPoolReturnStatement(Type elementType, string arrayName)
    {
        var clearArray = elementType.IsValueType ? "false" : "true";
        return SyntaxFactory.ParseStatement(
            $"System.Buffers.ArrayPool<{EvaluationHelper.GetCastableType(elementType)}>.Shared.Return({arrayName}, {clearArray});");
    }

    internal static TypeSyntax CreateVariableTypeSyntax(ExecutionVariable variable)
    {
        return string.IsNullOrWhiteSpace(variable.GeneratedRowTypeName)
            ? CreateTypeSyntax(variable.Type.RequireClrType())
            : SyntaxFactory.ParseTypeName(variable.GeneratedRowTypeName);
    }

    internal static TypeSyntax CreateTypeOfTypeSyntax(Type type)
    {
        return DynamicEntityBoundary.IsDynamicMetaObjectProvider(type)
            ? CreateTypeSyntax(typeof(object))
            : CreateTypeSyntax(type);
    }

    internal static TypeSyntax CreateTypeOfTypeSyntax(ExecutionTypeRef type) =>
        CreateTypeOfTypeSyntax(type.RequireClrType());

    internal static IdentifierNameSyntax CreateIdentifierName(string identifier)
    {
        return SyntaxFactory.IdentifierName(EscapeIdentifier(identifier));
    }

    internal static string EscapeIdentifier(string identifier)
    {
        return SyntaxFacts.GetKeywordKind(identifier) == SyntaxKind.None
            ? identifier
            : $"@{identifier}";
    }

    internal static SyntaxKind GetBinaryExpressionKind(BinaryOpKind kind)
    {
        return kind switch
        {
            BinaryOpKind.Add or BinaryOpKind.StringConcatenate => SyntaxKind.AddExpression,
            BinaryOpKind.Subtract => SyntaxKind.SubtractExpression,
            BinaryOpKind.Multiply => SyntaxKind.MultiplyExpression,
            BinaryOpKind.Divide => SyntaxKind.DivideExpression,
            BinaryOpKind.Modulo => SyntaxKind.ModuloExpression,
            BinaryOpKind.And => SyntaxKind.LogicalAndExpression,
            BinaryOpKind.Or => SyntaxKind.LogicalOrExpression,
            BinaryOpKind.Equal => SyntaxKind.EqualsExpression,
            BinaryOpKind.NotEqual or BinaryOpKind.IsDistinctFrom => SyntaxKind.NotEqualsExpression,
            BinaryOpKind.IsNotDistinctFrom => SyntaxKind.EqualsExpression,
            BinaryOpKind.GreaterThan => SyntaxKind.GreaterThanExpression,
            BinaryOpKind.LessThan => SyntaxKind.LessThanExpression,
            BinaryOpKind.GreaterOrEqual => SyntaxKind.GreaterThanOrEqualExpression,
            BinaryOpKind.LessOrEqual => SyntaxKind.LessThanOrEqualExpression,
            BinaryOpKind.BitwiseAnd => SyntaxKind.BitwiseAndExpression,
            BinaryOpKind.BitwiseOr => SyntaxKind.BitwiseOrExpression,
            BinaryOpKind.BitwiseXor => SyntaxKind.ExclusiveOrExpression,
            BinaryOpKind.LeftShift => SyntaxKind.LeftShiftExpression,
            BinaryOpKind.RightShift => SyntaxKind.RightShiftExpression,
            _ => throw UnsupportedShape.Of($"Binary operator '{kind}'", "the C# backend")
        };
    }

    internal static SyntaxKind GetUnaryExpressionKind(UnaryOpKind kind)
    {
        return kind switch
        {
            UnaryOpKind.Not => SyntaxKind.LogicalNotExpression,
            UnaryOpKind.Negate => SyntaxKind.UnaryMinusExpression,
            _ => throw UnsupportedShape.Of($"Unary operator '{kind}'", "the C# backend")
        };
    }
}
