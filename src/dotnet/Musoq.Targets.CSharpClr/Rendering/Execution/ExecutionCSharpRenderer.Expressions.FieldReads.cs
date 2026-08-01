using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tables;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{

    private ExpressionSyntax RenderFieldRead(ExecutionFieldRead fieldRead, ExecutionRenderContext context)
    {
        if (fieldRead.AccessStrategy is DirectScalarValueAccess)
            return RenderDirectScalarValueRead(fieldRead);

        if (fieldRead.AccessStrategy is ApplyOrdinalityAccess ordinality)
            return CreateIdentifierName(ordinality.VariableName);

        if (fieldRead.AccessStrategy is NestedClrPropertyAccess nestedProperty)
            return SyntaxFactory.ParseExpression(CreateNestedPropertyReadExpressionText(fieldRead, nestedProperty));

        if (fieldRead.AccessStrategy is GeneratedRowNestedAccess generatedNested)
            return RenderGeneratedRowNestedFieldRead(fieldRead, generatedNested, context);

        if (fieldRead.AccessStrategy is GeneratedDictionaryNestedAccess generatedDictionary)
            return RenderGeneratedDictionaryNestedFieldRead(fieldRead, generatedDictionary);

        if (fieldRead.AccessStrategy is NestedPositionalAccess nestedPositional)
            return NestedPositionalFieldReadRenderer.Render(fieldRead, nestedPositional);

        if (fieldRead.AccessStrategy is ReflectedMemberAccess)
            throw new InvalidOperationException(
                "Generated execution encountered a reflected member access after source policy validation.");

        if (fieldRead.AccessStrategy is ContextAccess contextAccess)
            return RenderContextFieldRead(fieldRead, contextAccess, context);

        if (fieldRead.AccessStrategy is GeneratedRowContextAccess generatedContext)
            return RenderGeneratedRowContextRead(fieldRead, generatedContext, context);

        if (fieldRead.AccessStrategy is GeneratedFieldAccess generatedField)
            return RenderGeneratedFieldRead(fieldRead, generatedField);

        if (fieldRead.AccessStrategy is GeneratedRowTypeAccess generatedRow)
            return RenderGeneratedRowTypeFieldRead(fieldRead, generatedRow);

        if (fieldRead.AccessStrategy is PositionalAccess positional)
            return RenderPositionalFieldRead(fieldRead, positional);

        if (RequiresParsedFieldRead(fieldRead))
            return SyntaxFactory.ParseExpression(CreateFieldReadExpressionText(fieldRead));

        if (string.IsNullOrWhiteSpace(fieldRead.Alias))
            return SyntaxFactory.IdentifierName(fieldRead.FieldName);

        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            CreateIdentifierName(fieldRead.Alias),
            CreateIdentifierName(fieldRead.FieldName));
    }

    private static IdentifierNameSyntax RenderDirectScalarValueRead(ExecutionFieldRead fieldRead)
    {
        if (string.IsNullOrWhiteSpace(fieldRead.Alias))
            throw new InvalidOperationException("Direct scalar value field reads require a source alias.");

        return CreateIdentifierName(fieldRead.Alias);
    }

    private ExpressionSyntax RenderGeneratedRowNestedFieldRead(
        ExecutionFieldRead fieldRead,
        GeneratedRowNestedAccess generatedNested,
        ExecutionRenderContext context)
    {
        if (string.IsNullOrWhiteSpace(fieldRead.Alias))
            throw new InvalidOperationException("Generated row nested field reads require a source alias.");

        ExpressionSyntax typedRow;
        if (generatedNested.ContextIndex is { } contextIndex)
        {
            var contextValue = TryCreateGeneratedRowContextStorageRead(
                fieldRead.Alias,
                new ContextAccess(contextIndex),
                context,
                out var generatedContextValue)
                ? generatedContextValue
                : CreateContextArrayElementRead(fieldRead.Alias, contextIndex);
            typedRow = SyntaxFactory.ParenthesizedExpression(SyntaxFactory.CastExpression(
                SyntaxFactory.ParseTypeName(generatedNested.TypeName),
                contextValue));
        }
        else
        {
            var sourceIsKnownGeneratedRow =
                context.Session.GeneratedRowVariableTypeNamesByName.TryGetValue(
                    fieldRead.Alias,
                    out var generatedRowTypeNames) &&
                generatedRowTypeNames.Contains(generatedNested.TypeName);
            typedRow = string.IsNullOrWhiteSpace(generatedNested.TypeName) ||
                       generatedNested.IsRowCarrier ||
                       sourceIsKnownGeneratedRow
                ? CreateIdentifierName(fieldRead.Alias)
                : SyntaxFactory.ParenthesizedExpression(SyntaxFactory.CastExpression(
                    SyntaxFactory.ParseTypeName(generatedNested.TypeName),
                    CreateIdentifierName(fieldRead.Alias)));
        }

        ExpressionSyntax sourceValue = generatedNested.FieldIndex is { } fieldIndex
            ? CreateElementAccess(
                typedRow,
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(fieldIndex)))
            : string.IsNullOrWhiteSpace(generatedNested.TypeName)
                ? SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    typedRow,
                    CreateIdentifierName(generatedNested.FieldName))
                : SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                typedRow,
                CreateIdentifierName(generatedNested.FieldName));

        if (!string.IsNullOrWhiteSpace(generatedNested.ValueTypeName))
        {
            sourceValue = SyntaxFactory.ParenthesizedExpression(
                SyntaxFactory.CastExpression(
                    SyntaxFactory.ParseTypeName(generatedNested.ValueTypeName),
                    sourceValue));
        }

        var separator = generatedNested.PropertyPath.StartsWith("[", StringComparison.Ordinal) ||
                         generatedNested.PropertyPath.StartsWith(".", StringComparison.Ordinal)
            ? string.Empty
            : ".";
        var value = SyntaxFactory.ParseExpression(
            $"{sourceValue}{separator}{generatedNested.PropertyPath}");

        if (generatedNested.IsRowCarrier ||
            !string.IsNullOrWhiteSpace(generatedNested.ValueTypeName) ||
            !string.IsNullOrWhiteSpace(fieldRead.GeneratedTypeName) ||
            fieldRead.ReturnType.RequireClrType() == typeof(object))
            return value;

        return SyntaxFactory.CastExpression(CreateTypeSyntax(fieldRead.ReturnType), value);
    }

    private static ExpressionSyntax RenderGeneratedDictionaryNestedFieldRead(
        ExecutionFieldRead fieldRead,
        GeneratedDictionaryNestedAccess generatedDictionary)
    {
        if (string.IsNullOrWhiteSpace(fieldRead.Alias))
            throw new InvalidOperationException("Generated dictionary nested field reads require a source alias.");

        ExpressionSyntax value = generatedDictionary.FieldIndex is { } fieldIndex
            ? CreateElementAccess(
                CreateIdentifierName(fieldRead.Alias),
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(fieldIndex)))
            : string.IsNullOrWhiteSpace(generatedDictionary.FieldName)
                ? CreateIdentifierName(fieldRead.Alias)
                : SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    CreateIdentifierName(fieldRead.Alias),
                    CreateIdentifierName(generatedDictionary.FieldName));

        foreach (var segment in generatedDictionary.PropertyPath
                     .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            value = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("GeneratedDictionaryAccess"),
                        SyntaxFactory.IdentifierName("GetValue")))
                .WithArgumentList(SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                    [
                        SyntaxFactory.Argument(value),
                        SyntaxFactory.Argument(CreateStringLiteral(segment))
                    ])));
        }

        if (fieldRead.ReturnType.RequireClrType() == typeof(object))
            return value;

        return SyntaxFactory.CastExpression(CreateTypeSyntax(fieldRead.ReturnType), value);
    }

    private static string CreateNestedPropertyReadExpressionText(
        ExecutionFieldRead fieldRead,
        NestedClrPropertyAccess nestedProperty)
    {
        if (string.IsNullOrWhiteSpace(fieldRead.Alias))
            throw new InvalidOperationException("Nested CLR property field reads require a source alias.");

        var separator = nestedProperty.PropertyPath.StartsWith('[')
            ? string.Empty
            : ".";
        return $"{EscapeIdentifier(fieldRead.Alias)}{separator}{nestedProperty.PropertyPath}";
    }

    private static ExpressionSyntax RenderPositionalFieldRead(
        ExecutionFieldRead fieldRead,
        PositionalAccess positional)
    {
        if (string.IsNullOrWhiteSpace(fieldRead.Alias))
            throw new InvalidOperationException("Positional field reads require a source alias.");

        var value = CreateElementAccess(
            CreateIdentifierName(fieldRead.Alias),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(positional.Index)));

        if (fieldRead.ReturnType.RequireClrType() == typeof(object))
            return value;

        return SyntaxFactory.CastExpression(CreateTypeSyntax(fieldRead.ReturnType), value);
    }

    private static MemberAccessExpressionSyntax RenderGeneratedRowTypeFieldRead(
        ExecutionFieldRead fieldRead,
        GeneratedRowTypeAccess generatedRow)
    {
        if (string.IsNullOrWhiteSpace(fieldRead.Alias))
            throw new InvalidOperationException("Generated row type field reads require a source alias.");

        var typedRow = SyntaxFactory.ParenthesizedExpression(SyntaxFactory.CastExpression(
            SyntaxFactory.IdentifierName(generatedRow.TypeName),
            CreateIdentifierName(fieldRead.Alias)));

        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            typedRow,
            CreateIdentifierName(generatedRow.FieldName));
    }

    private ExpressionSyntax RenderGeneratedRowContextRead(
        ExecutionFieldRead fieldRead,
        GeneratedRowContextAccess generatedContext,
        ExecutionRenderContext context)
    {
        if (string.IsNullOrWhiteSpace(fieldRead.Alias))
            throw new InvalidOperationException("Generated row context reads require a source alias.");

        var value = TryCreateGeneratedRowContextStorageRead(
            fieldRead.Alias,
            generatedContext,
            context,
            out var contextRead)
            ? contextRead
            : RenderContextFieldRead(fieldRead, new ContextAccess(generatedContext.Index), context);

        if (fieldRead.ReturnType.RequireClrType() == typeof(object))
            return value;

        return SyntaxFactory.CastExpression(CreateTypeSyntax(fieldRead.ReturnType), value);
    }

    private bool TryCreateGeneratedRowContextStorageRead(
        string alias,
        GeneratedRowContextAccess generatedContext,
        ExecutionRenderContext context,
        out ExpressionSyntax value)
    {
        value = null!;
        var row = CreateIdentifierName(alias);

        if (!context.Session.GeneratedRowConstructorUsagesByType.TryGetValue(
                generatedContext.TypeName,
                out var usedConstructors))
        {
            return false;
        }

        var constructors = GetGeneratedRowConstructors(usedConstructors);
        if (constructors.Length > 1)
        {
            value = CreateGeneratedRowContextArrayElementRead(row, "__contexts", generatedContext.Index);
            return true;
        }

        if (constructors.Length != 1)
            return false;

        return TryCreateGeneratedRowContextStorageRead(
            row,
            constructors[0],
            generatedContext.Index,
            out value);
    }

    private static bool TryCreateGeneratedRowContextStorageRead(
        ExpressionSyntax row,
        GeneratedRowContextConstructor constructor,
        int index,
        out ExpressionSyntax value)
    {
        value = null!;
        switch (constructor)
        {
            case GeneratedRowContextConstructor.ContextArray when index >= 0:
                value = CreateGeneratedRowContextArrayElementRead(row, "__contexts", index);
                return true;
            case GeneratedRowContextConstructor.SingleContext when index == 0:
                value = CreateGeneratedRowContextFieldRead(row, "__leftContext");
                return true;
            case GeneratedRowContextConstructor.SingleContexts when index >= 0:
                value = CreateGeneratedRowContextFieldRead(row, $"__context{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                return true;
            case GeneratedRowContextConstructor.TwoSingleContexts when index is 0 or 1:
                value = CreateGeneratedRowContextFieldRead(
                    row,
                    index == 0 ? "__leftContext" : "__rightContext");
                return true;
            default:
                return false;
        }
    }

    private static MemberAccessExpressionSyntax CreateGeneratedRowContextFieldRead(
        ExpressionSyntax row,
        string fieldName)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            row,
            CreateIdentifierName(fieldName));
    }

    private static ElementAccessExpressionSyntax CreateGeneratedRowContextArrayElementRead(
        ExpressionSyntax row,
        string fieldName,
        int index)
    {
        return CreateElementAccess(
            CreateGeneratedRowContextFieldRead(row, fieldName),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(index)));
    }

    private static MemberAccessExpressionSyntax RenderGeneratedFieldRead(
        ExecutionFieldRead fieldRead,
        GeneratedFieldAccess generatedField)
    {
        if (string.IsNullOrWhiteSpace(fieldRead.Alias))
            throw new InvalidOperationException("Generated field reads require a source alias.");

        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            CreateIdentifierName(fieldRead.Alias),
            CreateIdentifierName(generatedField.FieldName));
    }

    private static string CreateFieldReadExpressionText(ExecutionFieldRead fieldRead)
    {
        var fieldName = fieldRead.FieldName
            .Replace("['", "[\"", StringComparison.Ordinal)
            .Replace("']", "\"]", StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(fieldRead.Alias)
            ? fieldName
            : $"{EscapeIdentifier(fieldRead.Alias)}.{fieldName}";
    }

    private static bool RequiresParsedFieldRead(ExecutionFieldRead fieldRead)
    {
        return fieldRead.FieldName.Contains('.', StringComparison.Ordinal) || fieldRead.FieldName.Contains('[', StringComparison.Ordinal);
    }
}
