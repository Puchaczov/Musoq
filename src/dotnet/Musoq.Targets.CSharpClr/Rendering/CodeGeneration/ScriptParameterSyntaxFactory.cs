using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

internal static class ScriptParameterSyntaxFactory
{
    // CLR metadata access is intentionally kept in this compatibility factory.
    // Portable execution descriptors are resolved by the target binding context;
    // script-parameter and generated structural metadata are the one existing
    // CSharpClr boundary that is allowed to materialize CLR syntax.
    internal static Type ResolveExecutionClrType(ExecutionTypeRef typeRef) =>
        (typeRef ?? throw new ArgumentNullException(nameof(typeRef))).RequireClrType();

    internal static Type ResolveStructuralClrType(StructuralTypeDescriptor descriptor) =>
        (descriptor ?? throw new ArgumentNullException(nameof(descriptor))).ClrType ?? descriptor.CoreValueType;

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
        object? defaultValue,
        StructuralTypeDescriptor? structuralType = null)
    {
        if (structuralType != null)
            return CreateStructuralStorageExpression(parameterType, defaultValue, structuralType);

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
        var arguments = new List<ArgumentSyntax>
        {
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
                    : contract.IsStructured
                        ? CreateStructuralValueExpression(contract.DefaultValue, contract.StructuralType!)
                        : CreateDefaultValueExpression(contract.DefaultValue))
        };

        // Keep the established scalar constructor shape byte-for-byte stable. The
        // additive structural descriptor is emitted only for structured contracts.
        if (contract.StructuralType != null)
        {
            arguments.Add(SyntaxFactory.Argument(CreateStructuralTypeExpression(contract.StructuralType)));
        }

        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(nameof(ScriptParameterContract)))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
    }

    private static ExpressionSyntax CreateStructuralTypeExpression(StructuralTypeDescriptor descriptor)
    {
        return descriptor.Kind switch
        {
            StructuralTypeKind.Scalar => CreateStaticInvocation(
                nameof(StructuralTypeDescriptor),
                nameof(StructuralTypeDescriptor.Scalar),
                SyntaxFactory.TypeOfExpression(CreateTypeSyntax(descriptor.ScalarType!)),
                CreateBooleanLiteral(descriptor.IsNullable)),
            StructuralTypeKind.Record => CreateStaticInvocation(
                nameof(StructuralTypeDescriptor),
                nameof(StructuralTypeDescriptor.Record),
                SyntaxFactory.TypeOfExpression(CreateTypeSyntax(typeof(StructuralValue))),
                CreateArrayExpression(
                    SyntaxFactory.IdentifierName(nameof(StructuralFieldDescriptor)),
                    descriptor.Fields.Select(CreateStructuralFieldExpression).ToArray()),
                CreateBooleanLiteral(descriptor.IsNullable)),
            StructuralTypeKind.Collection => CreateStaticInvocation(
                nameof(StructuralTypeDescriptor),
                nameof(StructuralTypeDescriptor.Collection),
                SyntaxFactory.TypeOfExpression(CreateTypeSyntax(descriptor.ClrType ?? descriptor.CoreValueType)),
                CreateStructuralTypeExpression(descriptor.ElementType!),
                CreateBooleanLiteral(descriptor.IsNullable)),
            _ => throw new InvalidOperationException($"Structural kind '{descriptor.Kind}' cannot be emitted.")
        };
    }

    private static ExpressionSyntax CreateStructuralFieldExpression(StructuralFieldDescriptor field)
    {
        return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.IdentifierName(nameof(StructuralFieldDescriptor)))
            .WithArgumentList(CreateArgumentList(
                CreateStringLiteral(field.Name),
                CreateStructuralTypeExpression(field.Type),
                CreateBooleanLiteral(field.Required),
                CreateStructuralDefaultExpression(field.Default)));
    }

    private static ExpressionSyntax CreateStructuralDefaultExpression(StructuralDefaultDescriptor descriptor)
    {
        if (!descriptor.HasValue)
            return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        var value = descriptor.Value == null
            ? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
            : CreateDefaultValueExpression(descriptor.Value);
        return CreateStaticInvocation(
            nameof(StructuralDefaultDescriptor),
            nameof(StructuralDefaultDescriptor.Create),
            value,
            CreateStringLiteral(descriptor.CanonicalText ?? "null"));
    }

    private static ExpressionSyntax CreateStructuralStorageExpression(
        Type storageType,
        object? value,
        StructuralTypeDescriptor descriptor)
    {
        if (value == null)
            return SyntaxFactory.DefaultExpression(CreateTypeSyntax(storageType));

        return descriptor.Kind switch
        {
            StructuralTypeKind.Scalar => CreateDefaultValueExpression(
                value is StructuralValue scalar && scalar.Kind == StructuralTypeKind.Scalar
                    ? scalar.Scalar ?? throw new InvalidOperationException("A non-null structural scalar cannot contain null.")
                    : value),
            StructuralTypeKind.Record => CreateStructuralValueExpression(value, descriptor),
            StructuralTypeKind.Collection when value is Array array => CreateArrayExpression(
                CreateTypeSyntax(descriptor.ElementType!.CoreValueType),
                array.Cast<object?>()
                    .Select(element => CreateStructuralStorageExpression(
                        descriptor.ElementType!.CoreValueType,
                        element,
                        descriptor.ElementType!))
                    .ToArray()),
            _ => throw new InvalidOperationException(
                $"Structural storage value '{value.GetType().Name}' does not match '{descriptor.ToCanonicalSql()}'.")
        };
    }
    private static ExpressionSyntax CreateStructuralValueExpression(
        object? value,
        StructuralTypeDescriptor descriptor)
    {
        if (value == null)
            return CreateStaticInvocation(
                nameof(StructuralValue),
                nameof(StructuralValue.FromScalar),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));

        if (value is StructuralValue structural)
        {
            return structural.Kind switch
            {
                StructuralTypeKind.Scalar => CreateStaticInvocation(
                    nameof(StructuralValue),
                    nameof(StructuralValue.FromScalar),
                    structural.Scalar == null
                        ? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                        : CreateDefaultValueExpression(structural.Scalar)),
                StructuralTypeKind.Record => CreateStaticInvocation(
                    nameof(StructuralValue),
                    nameof(StructuralValue.FromRecord),
                    CreateStructuralRecordFields(structural, descriptor)),
                StructuralTypeKind.Collection => CreateStaticInvocation(
                    nameof(StructuralValue),
                    nameof(StructuralValue.FromCollection),
                    CreateStructuralElementArray(
                        structural.Elements.Select(element => CreateStructuralValueExpression(element, descriptor.ElementType!)).ToArray(),
                        SyntaxFactory.IdentifierName(nameof(StructuralValue)))),
                _ => throw new InvalidOperationException($"Structural value kind '{structural.Kind}' cannot be emitted.")
            };
        }

        if (descriptor.Kind == StructuralTypeKind.Scalar)
            return CreateStaticInvocation(
                nameof(StructuralValue),
                nameof(StructuralValue.FromScalar),
                CreateDefaultValueExpression(value));

        if (descriptor.Kind == StructuralTypeKind.Collection && value is Array array)
        {
            var elements = array.Cast<object?>()
                .Select(element => CreateStructuralValueExpression(element, descriptor.ElementType!))
                .ToArray();
            return CreateStaticInvocation(
                nameof(StructuralValue),
                nameof(StructuralValue.FromCollection),
                CreateStructuralElementArray(elements, SyntaxFactory.IdentifierName(nameof(StructuralValue))));
        }

        throw new InvalidOperationException($"Structural default value '{value.GetType().Name}' cannot be emitted.");
    }

    private static ExpressionSyntax CreateStructuralRecordFields(
        StructuralValue structural,
        StructuralTypeDescriptor descriptor)
    {
        var keyValuePairType = SyntaxFactory.GenericName("KeyValuePair")
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SeparatedList<TypeSyntax>([
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                    SyntaxFactory.IdentifierName(nameof(StructuralValue))])));
        var fields = structural.Fields
            .Select(field =>
            {
                var fieldDescriptor = descriptor.Fields.FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, field.Key, StringComparison.OrdinalIgnoreCase));
                var fieldType = fieldDescriptor?.Type ?? StructuralTypeDescriptor.Scalar(
                    field.Value.Scalar?.GetType() ?? typeof(object),
                    field.Value.Scalar == null);
                return SyntaxFactory.ObjectCreationExpression(keyValuePairType)
                    .WithArgumentList(CreateArgumentList(
                        CreateStringLiteral(field.Key),
                        CreateStructuralValueExpression(field.Value, fieldType)));
            })
            .ToArray();
        return CreateArrayExpression(
            SyntaxFactory.GenericName("KeyValuePair")
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList<TypeSyntax>([
                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                        SyntaxFactory.IdentifierName(nameof(StructuralValue))]))),
            fields);
    }

    private static ExpressionSyntax CreateStructuralElementArray(
        IReadOnlyList<ExpressionSyntax> elements,
        TypeSyntax elementType)
    {
        return CreateArrayExpression(elementType, elements);
    }

    private static ExpressionSyntax CreateArrayExpression(
        TypeSyntax elementType,
        IReadOnlyList<ExpressionSyntax> elements)
    {
        return SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.ArrayType(elementType)
                    .WithRankSpecifiers(SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.OmittedArraySizeExpression())))))
            .WithInitializer(SyntaxFactory.InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                SyntaxFactory.SeparatedList(elements)));
    }

    private static InvocationExpressionSyntax CreateStaticInvocation(
        string typeName,
        string methodName,
        params ExpressionSyntax[] arguments)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(typeName),
                    SyntaxFactory.IdentifierName(methodName)))
            .WithArgumentList(CreateArgumentList(arguments));
    }

    private static LiteralExpressionSyntax CreateBooleanLiteral(bool value) =>
        SyntaxFactory.LiteralExpression(value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);

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
