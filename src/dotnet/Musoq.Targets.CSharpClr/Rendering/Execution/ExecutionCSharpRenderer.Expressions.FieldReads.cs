using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{

    private ExpressionSyntax RenderFieldRead(ExecutionFieldRead fieldRead, ExecutionRenderContext context)
    {
        if (fieldRead.AccessStrategy is DirectScalarValueAccess)
            return NativeEnumReadNormalizer.Normalize(fieldRead, RenderDirectScalarValueRead(fieldRead));

        if (fieldRead.AccessStrategy is ApplyOrdinalityAccess ordinality)
            return CreateIdentifierName(ordinality.VariableName);

        if (fieldRead.AccessStrategy is NestedClrPropertyAccess nestedProperty)
            return NestedPropertyReadRenderer.Render(fieldRead, nestedProperty);

        if (fieldRead.AccessStrategy is GeneratedRowNestedAccess generatedNested)
            return RenderGeneratedRowNestedFieldRead(fieldRead, generatedNested, context);

        if (fieldRead.AccessStrategy is GeneratedDictionaryNestedAccess)
            throw new InvalidOperationException("Erased dictionary member access must be rejected before rendering.");

        if (fieldRead.AccessStrategy is NestedPositionalAccess nestedPositional)
            return NestedPositionalFieldReadRenderer.Render(fieldRead, nestedPositional);

        if (fieldRead.AccessStrategy is ReflectedMemberAccess)
            throw new InvalidOperationException(
                "Generated execution encountered a reflected member access after source policy validation.");

        if (fieldRead.AccessStrategy is ContextAccess contextAccess)
            return RenderContextFieldRead(fieldRead, contextAccess, context);

        if (fieldRead.AccessStrategy is GeneratedRowContextAccess generatedContext)
            return RenderGeneratedRowContextRead(fieldRead, generatedContext, context);

        if (fieldRead.AccessStrategy is RuntimeDynamicMemberAccess)
            throw new InvalidOperationException("Dynamic execution must be rejected before rendering.");

        if (fieldRead.AccessStrategy is GeneratedFieldAccess generatedField)
            return RenderGeneratedFieldRead(fieldRead, generatedField);

        if (fieldRead.AccessStrategy is GeneratedRowTypeAccess generatedRow)
            return RenderGeneratedRowTypeFieldRead(fieldRead, generatedRow);

        if (fieldRead.AccessStrategy is PositionalAccess positional)
            return RenderPositionalFieldRead(fieldRead, positional);

        if (RequiresParsedFieldRead(fieldRead))
            return NativeEnumReadNormalizer.Normalize(
                fieldRead,
                SyntaxFactory.ParseExpression(CreateFieldReadExpressionText(fieldRead)));

        if (string.IsNullOrWhiteSpace(fieldRead.Alias))
            return SyntaxFactory.IdentifierName(fieldRead.FieldName);

        return NativeEnumReadNormalizer.Normalize(
            fieldRead,
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                CreateIdentifierName(fieldRead.Alias),
                CreateIdentifierName(fieldRead.FieldName)));
    }

    private ExpressionSyntax RenderMemberRead(
        ExecutionMemberRead memberRead,
        ExecutionRenderContext context)
    {
        if (memberRead.IsDynamic)
            throw new InvalidOperationException("Dynamic member execution must be rejected before rendering.");
        var receiver = RenderExpression(memberRead.Receiver, context);
        if (memberRead.IsNullSafe)
            return NullSafeMemberSyntax.Render(memberRead, receiver);
        return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
            receiver, CreateIdentifierName(memberRead.MemberName));
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
                : throw UnsupportedShape.Of($"Context read {fieldRead.Alias}[{contextIndex}] has no concrete source carrier.");
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

        ExpressionSyntax sourceValue;
        if (generatedNested.FieldIndex is { } fieldIndex)
        {
            var rootTypeName = generatedNested.ValueTypeName ??
                throw new InvalidOperationException("Indexed nested rows require a concrete root type.");
            sourceValue = CreateNativeRowValueRead(typedRow, fieldIndex, SyntaxFactory.ParseTypeName(rootTypeName));
        }
        else
            sourceValue = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                typedRow, CreateIdentifierName(generatedNested.FieldName));

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

        if (generatedNested.IsNullSafe)
        {
            value = SyntaxFactory.ParseExpression($"{sourceValue}?.{generatedNested.PropertyPath.Replace(".", "?.", StringComparison.Ordinal)}");
            var returnType = fieldRead.ReturnType.RequireClrType();
            if (returnType.IsValueType && Nullable.GetUnderlyingType(returnType) is null)
                value = SyntaxFactory.BinaryExpression(SyntaxKind.CoalesceExpression, value,
                    SyntaxFactory.DefaultExpression(CreateTypeSyntax(fieldRead.ReturnType)));
        }

        if (generatedNested.IsRowCarrier ||
            !string.IsNullOrWhiteSpace(generatedNested.ValueTypeName) ||
            !string.IsNullOrWhiteSpace(fieldRead.GeneratedTypeName) ||
            fieldRead.ReturnType.RequireClrType() == typeof(object))
            return value;

        return SyntaxFactory.CastExpression(CreateTypeSyntax(fieldRead.ReturnType), value);
    }

    private static ExpressionSyntax RenderPositionalFieldRead(
        ExecutionFieldRead fieldRead,
        PositionalAccess positional)
    {
        if (string.IsNullOrWhiteSpace(fieldRead.Alias))
            throw new InvalidOperationException("Positional field reads require a source alias.");

        var readType = positional.ValueType ?? fieldRead.ReturnType;
        var valueTypeSyntax = fieldRead.GeneratedTypeName is { Length: > 0 } generatedTypeName
            ? SyntaxFactory.ParseTypeName(generatedTypeName)
            : CreateTypeSyntax(readType);
        if (readType.RequireClrType() == typeof(object) && string.IsNullOrWhiteSpace(fieldRead.GeneratedTypeName))
            throw new InvalidOperationException("Native positional reads require a concrete column type.");

        ExpressionSyntax value = CreateNativeRowValueRead(CreateIdentifierName(fieldRead.Alias), positional.Index, valueTypeSyntax);
        if (fieldRead.EnumType != null && readType.RequireClrType().IsEnum)
            return NativeEnumReadNormalizer.Normalize(fieldRead with { SourceReadType = readType }, value);
        if (readType != fieldRead.ReturnType && string.IsNullOrWhiteSpace(fieldRead.GeneratedTypeName))
            value = SyntaxFactory.CastExpression(CreateTypeSyntax(fieldRead.ReturnType), value);
        return value;
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
        var valueConstructors = constructors.Where(static constructor =>
            constructor != GeneratedRowContextConstructor.NoContext).ToArray();
        if (valueConstructors.Length != 1)
            return false;

        return TryCreateGeneratedRowContextStorageRead(
            row,
            valueConstructors[0],
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
