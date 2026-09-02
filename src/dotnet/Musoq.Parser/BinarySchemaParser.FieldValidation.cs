using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private FieldValueValidationNode? ComposeOptionalFieldValueValidation(TypeAnnotationNode typeAnnotation, string fieldName)
    {
        var kind = MatchValidationKeyword();
        if (kind is null)
            return null;

        var keywordSpan = Current.Span;
        Consume(Current.TokenType);

        var validation = kind == FieldValueValidationKind.OneOf
            ? ComposeOneOfValidation(typeAnnotation, fieldName, keywordSpan)
            : ComposeConstValidation(kind.Value, typeAnnotation, fieldName, keywordSpan);

        if (MatchValidationKeyword() is not null)
            throw InvalidFieldValueValidation(
                $"Field '{fieldName}' may only declare a single value validation modifier.",
                Current.Span);

        return validation;
    }

    private FieldValueValidationKind? MatchValidationKeyword() => MatchValidationKeyword(Current);

    private static FieldValueValidationKind? MatchValidationKeyword(Token? token)
    {
        if (token is null || token.TokenType is not (TokenType.Identifier or TokenType.Word))
            return null;

        return token.Value.ToLowerInvariant() switch
        {
            "const" => FieldValueValidationKind.Const,
            "magic" => FieldValueValidationKind.Magic,
            "oneof" => FieldValueValidationKind.OneOf,
            _ => null
        };
    }

    private void EnsureFieldValueValidationOrdering(string fieldName)
    {
        if (MatchValidationKeyword() is null)
            return;

        throw InvalidFieldValueValidation(
            $"Field '{fieldName}' value validation must appear immediately after the type/repeat annotation and before 'at', 'when', or 'check'.",
            Current.Span);
    }

    private FieldValueValidationNode ComposeConstValidation(
        FieldValueValidationKind kind,
        TypeAnnotationNode typeAnnotation,
        string fieldName,
        TextSpan keywordSpan)
    {
        if (Current.TokenType == TokenType.LeftSquareBracket)
        {
            EnsureByteListCompatible(typeAnnotation, fieldName, keywordSpan);
            var bytes = ComposeByteList(fieldName);
            return new FieldValueValidationNode(kind, bytes, true);
        }

        EnsureScalarCompatible(typeAnnotation, fieldName, keywordSpan);
        var value = ComposeValidationScalar(fieldName);
        EnsureScalarValueCompatible(typeAnnotation, value, fieldName, keywordSpan);
        return new FieldValueValidationNode(kind, [value], false);
    }

    private FieldValueValidationNode ComposeOneOfValidation(
        TypeAnnotationNode typeAnnotation,
        string fieldName,
        TextSpan keywordSpan)
    {
        EnsureScalarCompatible(typeAnnotation, fieldName, keywordSpan);

        if (Current.TokenType != TokenType.LeftSquareBracket)
            throw InvalidFieldValueValidation(
                $"Field '{fieldName}' 'oneOf' requires a bracketed list of values.",
                Current.Span);

        var values = ComposeBracketedLiterals(fieldName);

        if (values.Count == 0)
            throw InvalidFieldValueValidation(
                $"Field '{fieldName}' 'oneOf' requires at least one value.",
                keywordSpan);

        foreach (var value in values)
            EnsureScalarValueCompatible(typeAnnotation, value, fieldName, keywordSpan);

        return new FieldValueValidationNode(FieldValueValidationKind.OneOf, values, false);
    }
}
