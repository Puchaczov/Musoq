using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

internal static class ScriptParameterSyntaxFactory
{
    public static ExpressionSyntax CreateDefinitionsInitializer(
        IReadOnlyList<ScriptParameterDefinition>? definitions)
    {
        if (definitions == null || definitions.Count == 0)
            return SyntaxFactory.ParseExpression("Array.Empty<ScriptParameterDefinition>()");

        return SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.ArrayType(SyntaxFactory.IdentifierName(nameof(ScriptParameterDefinition)))
                    .WithRankSpecifiers(SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.OmittedArraySizeExpression())))))
            .WithInitializer(SyntaxFactory.InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                SyntaxFactory.SeparatedList(definitions
                    .Select(definition => (ExpressionSyntax)CreateDefinitionCreation(definition)))));
    }

    public static ExpressionSyntax CreateContractsInitializer(
        IReadOnlyList<ScriptParameterDefinition>? definitions)
    {
        if (definitions == null || definitions.Count == 0)
            return SyntaxFactory.ParseExpression("Array.Empty<ScriptParameterContract>()");

        return SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.ArrayType(SyntaxFactory.IdentifierName(nameof(ScriptParameterContract)))
                    .WithRankSpecifiers(SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.OmittedArraySizeExpression())))))
            .WithInitializer(SyntaxFactory.InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                SyntaxFactory.SeparatedList(definitions
                    .Select(definition => (ExpressionSyntax)CreateContractCreation(definition.Contract)))));
    }

    public static ExpressionSyntax CreateDefaultArgumentExpression(
        Type parameterType,
        object? defaultValue)
    {
        return defaultValue == null
            ? SyntaxFactory.DefaultExpression(CreateTypeSyntax(parameterType))
            : CreateDefaultValueExpression(defaultValue);
    }

    public static TypeSyntax CreateTypeSyntax(Type type)
    {
        return SyntaxFactory.ParseTypeName(EvaluationHelper.GetCastableType(type));
    }

    private static ObjectCreationExpressionSyntax CreateDefinitionCreation(ScriptParameterDefinition definition)
    {
        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(nameof(ScriptParameterDefinition)))
            .WithArgumentList(CreateArgumentList(CreateContractCreation(definition.Contract)));
    }

    private static ObjectCreationExpressionSyntax CreateContractCreation(ScriptParameterContract contract)
    {
        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(nameof(ScriptParameterContract)))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([
                SyntaxFactory.Argument(CreateStringLiteral(contract.Name)),
                SyntaxFactory.Argument(CreateStringLiteral(contract.DeclaredTypeName)),
                SyntaxFactory.Argument(CreateStringLiteral(contract.CanonicalTypeName)),
                SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(CreateTypeSyntax(contract.ClrType))),
                SyntaxFactory.Argument(contract.IsNullable
                    ? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                    : SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)),
                SyntaxFactory.Argument(contract.IsCollection
                    ? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                    : SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)),
                SyntaxFactory.Argument(contract.ElementClrType == null
                    ? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                    : SyntaxFactory.TypeOfExpression(CreateTypeSyntax(contract.ElementClrType))),
                SyntaxFactory.Argument(contract.ElementCanonicalTypeName == null
                    ? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                    : CreateStringLiteral(contract.ElementCanonicalTypeName)),
                SyntaxFactory.Argument(contract.HasDefaultValue
                    ? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                    : SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)),
                SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(ScriptParameterDefaultKind)),
                    SyntaxFactory.IdentifierName(contract.DefaultKind.ToString()))),
                SyntaxFactory.Argument(contract.DefaultValue == null
                    ? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                    : CreateDefaultValueExpression(contract.DefaultValue))
            ])));
    }

    private static ExpressionSyntax CreateDefaultValueExpression(object value)
    {
        return value switch
        {
            string text => CreateStringLiteral(text),
            bool flag => SyntaxFactory.LiteralExpression(
                flag ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression),
            char character => SyntaxFactory.LiteralExpression(
                SyntaxKind.CharacterLiteralExpression,
                SyntaxFactory.Literal(character)),
            byte number => SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(number)),
            sbyte number => SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(number)),
            short number => SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(number)),
            ushort number => SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(number)),
            int number => SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(number)),
            uint number => SyntaxFactory.ParseExpression($"{number.ToString(CultureInfo.InvariantCulture)}u"),
            long number => SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(number)),
            ulong number => SyntaxFactory.ParseExpression($"{number.ToString(CultureInfo.InvariantCulture)}ul"),
            float number => SyntaxFactory.ParseExpression($"{number.ToString(CultureInfo.InvariantCulture)}f"),
            double number => SyntaxFactory.ParseExpression($"{number.ToString(CultureInfo.InvariantCulture)}d"),
            decimal number => SyntaxFactory.ParseExpression($"{number.ToString(CultureInfo.InvariantCulture)}m"),
            Guid guid => SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(nameof(Guid)))
                .WithArgumentList(CreateArgumentList(CreateStringLiteral(guid.ToString("D", CultureInfo.InvariantCulture)))),
            DateTime dateTime => SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(nameof(DateTime)))
                .WithArgumentList(CreateArgumentList(
                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(dateTime.Ticks)),
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(nameof(DateTimeKind)),
                        SyntaxFactory.IdentifierName(dateTime.Kind.ToString())))),
            DateTimeOffset dateTimeOffset => SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(nameof(DateTimeOffset)))
                .WithArgumentList(CreateArgumentList(
                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(dateTimeOffset.Ticks)),
                    SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(nameof(TimeSpan)))
                        .WithArgumentList(CreateArgumentList(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(dateTimeOffset.Offset.Ticks)))))),
            TimeSpan timeSpan => SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(nameof(TimeSpan)))
                .WithArgumentList(CreateArgumentList(
                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(timeSpan.Ticks)))),
            _ => throw new NotSupportedException(
                $"Script parameter default value type '{value.GetType().Name}' is not supported.")
        };
    }

    private static LiteralExpressionSyntax CreateStringLiteral(string value)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value));
    }

    private static ArgumentListSyntax CreateArgumentList(params ExpressionSyntax[] expressions)
    {
        return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(expressions.Select(SyntaxFactory.Argument)));
    }
}
