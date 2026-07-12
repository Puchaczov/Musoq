using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private InvocationExpressionSyntax CreateCompositeKeyInvocation(
        ExecutionCompositeKey compositeKey,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(WindowFunctionHelpers)),
                    SyntaxFactory.IdentifierName(nameof(WindowFunctionHelpers.CompositeKey))))
            .WithArgumentList(CreateArgumentList(compositeKey.Parts.Select(part => RenderExpression(part, context)).ToArray()));
    }

    private TupleExpressionSyntax CreateValueTupleKeyExpression(
        ExecutionValueTupleKey valueTupleKey,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(
            valueTupleKey.Parts.Select(part => SyntaxFactory.Argument(RenderExpression(part, context)))));
    }

    private static ExpressionSyntax CreateValueTupleKeyExpression(int keyCount)
    {
        if (keyCount == 1)
            return SyntaxFactory.InvocationExpression(SyntaxFactory.ParseExpression("ValueTuple.Create"))
                .WithArgumentList(CreateArgumentList(SyntaxFactory.IdentifierName(CreateGroupKeyVariableName(0))));

        return SyntaxFactory.ParseExpression(
            $"({string.Join(", ", Enumerable.Range(0, keyCount).Select(CreateGroupKeyVariableName))})");
    }

    private static ArrayCreationExpressionSyntax CreateSizedArrayCreation(
        string elementTypeName,
        ExpressionSyntax size)
    {
        return SyntaxFactory.ArrayCreationExpression(
            SyntaxFactory.ArrayType(SyntaxFactory.IdentifierName(elementTypeName))
                .WithRankSpecifiers(SyntaxFactory.SingletonList(
                    SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList(size)))));
    }

    private static ArrayCreationExpressionSyntax CreateSizedArrayCreation(
        Type elementType,
        ExpressionSyntax size)
    {
        return SyntaxFactory.ArrayCreationExpression(
            SyntaxFactory.ArrayType(CreateTypeSyntax(elementType))
                .WithRankSpecifiers(SyntaxFactory.SingletonList(
                    SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList(size)))));
    }

    private static ArrayCreationExpressionSyntax CreateSizedArrayCreation(
        ExecutionTypeRef elementType,
        ExpressionSyntax size) =>
        CreateSizedArrayCreation(elementType.RequireClrType(), size);

    private static ArrayCreationExpressionSyntax CreateWindowKeyArrayCreation(
        ExecutionVariable keyArray,
        ExpressionSyntax size)
    {
        return CreateSizedArrayCreation(GetArrayElementType(keyArray), size);
    }

    private static Type GetArrayElementType(ExecutionVariable variable)
    {
        return variable.Type.RequireClrType().GetElementType() ?? typeof(object);
    }

    private static LiteralExpressionSyntax CreateBooleanLiteral(bool value)
    {
        return SyntaxFactory.LiteralExpression(value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
    }

    private static LiteralExpressionSyntax CreateIntLiteral(int value)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(value));
    }

    private static MemberAccessExpressionSyntax CreateTableRowsRead(string tableName)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(tableName),
            SyntaxFactory.IdentifierName("Rows"));
    }

    private ExpressionSyntax CreateRowsRead(ExecutionVariable rowsOwner, ExecutionRenderContext context)
    {
        return TryGetTypedRowBufferShape(rowsOwner.Name, context, out _)
            ? SyntaxFactory.IdentifierName(rowsOwner.Name)
            : CreateTableRowsRead(rowsOwner.Name);
    }

    private ExpressionSyntax CreateRowsCountRead(ExecutionVariable rowsOwner, ExecutionRenderContext context)
    {
        var rowsExpression = TryGetTypedRowBufferShape(rowsOwner.Name, context, out _)
            ? (ExpressionSyntax)SyntaxFactory.IdentifierName(rowsOwner.Name)
            : CreateTableRowsRead(rowsOwner.Name);

        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            rowsExpression,
            SyntaxFactory.IdentifierName("Count"));
    }

    private static bool CanRenderShape(RowShape shape)
    {
        return shape switch
        {
            SourceEntityShape source => (CanReferenceType(source.EntityType) ||
                                         source.Fields.Count == 0 ||
                                         UsesReflectedMemberAccess(source)) &&
                                        CanRenderFieldTypes(source.Fields),
            ExpandoAdapterShape expando => CanRenderExpandoAdapterShape(expando),
            GeneratedRowShape generated => CanRenderGeneratedRowShape(generated),
            ValuesRowShape values => CanRenderGeneratedRowShape(values.GeneratedShape),
            GeneratedRecordShape generated => CanRenderGeneratedRecordShape(generated),
            HashPayloadShape hashPayload => CanRenderHashPayloadShape(hashPayload),
            AggregateGroupShape aggregateGroup => CanRenderAggregateGroupShape(aggregateGroup),
            TableRowShape => true,
            _ => false
        };
    }

    private static bool UsesReflectedMemberAccess(SourceEntityShape source)
    {
        return source.Fields.Any(static field => field.AccessStrategy is ReflectedMemberAccess);
    }

    private static bool CanRenderExpandoAdapterShape(ExpandoAdapterShape shape)
    {
        return CanRenderIdentifier(shape.TypeName) &&
               CanRenderFieldNames(shape.Fields.Select(field => field.Name)) &&
               CanRenderFieldTypes(shape.Fields);
    }

    private static bool CanRenderGeneratedRowShape(GeneratedRowShape shape)
    {
        return CanRenderIdentifier(shape.TypeName) &&
               CanRenderFieldNames(shape.Fields.Select(GetGeneratedFieldName)) &&
               shape.Fields.Select(GetGeneratedFieldName).All(static fieldName => !GeneratedRowNamingPolicy.IsRendererReservedMemberName(fieldName)) &&
               CanRenderFieldTypes(shape.Fields);
    }

    private static bool CanRenderGeneratedRecordShape(GeneratedRecordShape shape)
    {
        return CanRenderIdentifier(shape.TypeName) &&
               CanRenderFieldNames(shape.Fields.Select(GetGeneratedFieldName)) &&
               CanRenderFieldTypes(shape.Fields);
    }

    private static bool CanRenderHashPayloadShape(HashPayloadShape shape)
    {
        return CanRenderIdentifier(shape.TypeName) &&
               CanRenderFieldNames(GetHashPayloadFields(shape).Select(GetGeneratedFieldName)) &&
               CanRenderFieldTypes(GetHashPayloadFields(shape));
    }

    private static bool CanRenderFieldTypes(IEnumerable<FieldBinding> fields)
    {
        return fields.All(field => CanReferenceType(field.Type));
    }

    private static bool CanRenderFieldNames(IEnumerable<string> fieldNames)
    {
        var seenNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var fieldName in fieldNames)
        {
            if (!CanRenderIdentifier(fieldName) || !seenNames.Add(fieldName))
                return false;
        }

        return true;
    }

    private static bool CanRenderIdentifier(string identifier)
    {
        return GeneratedRowNamingPolicy.CanRenderIdentifier(identifier);
    }

    private static string CreateIdentifierCandidate(string value, int disambiguator)
    {
        return GeneratedRowNamingPolicy.CreateRendererIdentifierCandidate(value, disambiguator);
    }

    private static bool CanReferenceType(Type type)
    {
        if (type.IsByRef || type.IsPointer)
            return false;

        if (type.IsArray)
            return type.GetElementType() is { } elementType && CanReferenceType(elementType);

        var nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType != null)
            return CanReferenceType(nullableType);

        if (type.IsGenericType)
            return CanReferencePublicType(type.GetGenericTypeDefinition()) &&
                   type.GetGenericArguments().All(CanReferenceType);

        return CanReferencePublicType(type);
    }

    private static bool CanReferenceType(ExecutionTypeRef type) => CanReferenceType(type.RequireClrType());

    private static bool IsValueTupleType(Type type, int arity)
    {
        return type.IsGenericType &&
               type.GetGenericArguments().Length == arity &&
               type.Namespace == typeof(ValueTuple).Namespace &&
               type.Name.StartsWith("ValueTuple`", StringComparison.Ordinal);
    }

    private static Type CreateValueTupleType(Type[] keyTypes)
    {
        return keyTypes.Length switch
        {
            2 => typeof(ValueTuple<,>).MakeGenericType(keyTypes.ToArray()),
            3 => typeof(ValueTuple<,,>).MakeGenericType(keyTypes.ToArray()),
            4 => typeof(ValueTuple<,,,>).MakeGenericType(keyTypes.ToArray()),
            5 => typeof(ValueTuple<,,,,>).MakeGenericType(keyTypes.ToArray()),
            6 => typeof(ValueTuple<,,,,,>).MakeGenericType(keyTypes.ToArray()),
            7 => typeof(ValueTuple<,,,,,,>).MakeGenericType(keyTypes.ToArray()),
            _ => throw new NotSupportedException(
                $"ValueTuple window keys support 2 to 7 key parts, but got {keyTypes.Length.ToString(CultureInfo.InvariantCulture)}.")
        };
    }

    private static bool CanReferencePublicType(Type type)
    {
        if (!type.IsNested)
            return type.IsPublic;

        return type is { IsNestedPublic: true, DeclaringType: not null } &&
               CanReferencePublicType(type.DeclaringType);
    }
}
