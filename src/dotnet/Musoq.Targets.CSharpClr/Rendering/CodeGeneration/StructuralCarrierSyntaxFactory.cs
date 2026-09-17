using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Targets.CSharpClr;

/// <summary>
/// Emits typed storage for structural script values. <see cref="StructuralValue"/>
/// remains the public host and metadata representation; generated execution code
/// uses these carriers so fields are never transported through object slots.
/// </summary>
internal static class StructuralCarrierSyntaxFactory
{
    private const string CarrierPrefix = "__musoqStructuralCarrier_";
    private const string AdapterPrefix = "__musoqStructuralAdapt_";

    internal static IReadOnlyList<MemberDeclarationSyntax> CreateMembers(
        IReadOnlyList<ScriptVariableDefinition> definitions,
        IEnumerable<ExecutionStructuralConversion> conversions)
    {
        return CreateMembers(definitions, Array.Empty<ScriptParameterDefinition>(), conversions);
    }

    internal static IReadOnlyList<MemberDeclarationSyntax> CreateMembers(
        IReadOnlyList<ScriptVariableDefinition> definitions,
        IReadOnlyList<ScriptParameterDefinition> parameterDefinitions,
        IEnumerable<ExecutionStructuralConversion> conversions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(parameterDefinitions);
        ArgumentNullException.ThrowIfNull(conversions);

        var members = new List<MemberDeclarationSyntax>();
        for (var definitionIndex = 0; definitionIndex < definitions.Count; definitionIndex++)
        {
            if (definitions[definitionIndex].StructuralType is { } descriptor)
                AddCarrierMembers(members, descriptor, definitionIndex, []);
        }

        var parameterOffset = definitions.Count;
        for (var parameterIndex = 0; parameterIndex < parameterDefinitions.Count; parameterIndex++)
        {
            if (parameterDefinitions[parameterIndex].Contract.StructuralType is { } descriptor)
                AddCarrierMembers(members, descriptor, parameterOffset + parameterIndex, []);
        }

        var adapters = conversions
            .Select(item =>
            {
                var (name, index, descriptor) = item.Input switch
                {
                    ExecutionScriptVariableRead read when TryFindDefinition(definitions, read.Name, out var variableIndex, out var variable) =>
                        (read.Name, variableIndex, variable.StructuralType),
                    ExecutionScriptParameterRead read when TryFindParameter(parameterDefinitions, read.Name, out var parameterIndex, out var parameter) =>
                        (read.Name, parameterOffset + parameterIndex, parameter.Contract.StructuralType),
                    _ => (string.Empty, -1, null)
                };

                if (index < 0 || descriptor == null)
                    return (Key: string.Empty, Item: item, Name: name, Index: index, Descriptor: descriptor);
                return (Key: CreateAdapterName(
                    index,
                    ScriptParameterSyntaxFactory.ResolveExecutionClrType(item.TargetType)),
                    Item: item,
                    Name: name,
                    Index: index,
                    Descriptor: descriptor);
            })
            .Where(static item => item.Key.Length > 0)
            .GroupBy(static item => item.Key, StringComparer.Ordinal)
            .Select(static group => group.First())
            .ToArray();

        foreach (var item in adapters)
        {
            if (item.Descriptor is not { } sourceDescriptor)
                continue;

            var targetType = ScriptParameterSyntaxFactory.ResolveExecutionClrType(item.Item.TargetType);
            if (NeedsAdapter(sourceDescriptor, targetType))
                members.AddRange(new AdapterBuilder(item.Index, sourceDescriptor).Build(targetType));
        }

        return members;
    }

    internal static bool TryCreateVariableStorage(
        IReadOnlyList<ScriptVariableDefinition> definitions,
        ScriptVariableDefinition definition,
        out TypeSyntax type,
        out ExpressionSyntax initializer)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.StructuralType is not { } descriptor ||
            !TryFindDefinition(definitions, definition.Name, out var definitionIndex, out _))
        {
            type = null!;
            initializer = null!;
            return false;
        }

        type = SyntaxFactory.ParseTypeName(CreateStorageType(descriptor, definitionIndex, []));
        initializer = CreateValueExpression(definition.Value, descriptor, definitionIndex, []);
        return true;
    }

    internal static bool TryCreateConversion(
        IReadOnlyList<ScriptVariableDefinition> definitions,
        ExecutionStructuralConversion conversion,
        ExecutionCSharpRenderer renderer,
        ExecutionRenderContext context,
        out ExpressionSyntax expression)
    {
        return TryCreateConversion(
            definitions,
            Array.Empty<ScriptParameterDefinition>(),
            conversion,
            renderer,
            context,
            out expression);
    }

    internal static bool TryCreateConversion(
        IReadOnlyList<ScriptVariableDefinition> variableDefinitions,
        IReadOnlyList<ScriptParameterDefinition> parameterDefinitions,
        ExecutionStructuralConversion conversion,
        ExecutionCSharpRenderer renderer,
        ExecutionRenderContext context,
        out ExpressionSyntax expression)
    {
        ArgumentNullException.ThrowIfNull(variableDefinitions);
        ArgumentNullException.ThrowIfNull(parameterDefinitions);
        ArgumentNullException.ThrowIfNull(conversion);
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(context);

        var targetType = ScriptParameterSyntaxFactory.ResolveExecutionClrType(conversion.TargetType);
        var (definitionIndex, sourceDescriptor) = conversion.Input switch
        {
            ExecutionScriptVariableRead read when TryFindDefinition(variableDefinitions, read.Name, out var variableIndex, out var variable) =>
                (variableIndex, variable.StructuralType),
            ExecutionScriptParameterRead read when TryFindParameter(parameterDefinitions, read.Name, out var parameterIndex, out var parameter) =>
                (variableDefinitions.Count + parameterIndex, parameter.Contract.StructuralType),
            _ => (-1, null)
        };

        // Primitive array parameters predate structural declarations and keep
        // the legacy contract shape for source compatibility.  Their execution
        // read is already exposed as the typed IReadOnlyList<T> sequence; no
        // carrier adapter is required, but it still participates in the same
        // conversion node used by structural source preparation.
        if (conversion.Input is ExecutionScriptParameterRead primitiveArrayRead &&
            TryFindParameter(parameterDefinitions, primitiveArrayRead.Name, out _, out var primitiveArrayDefinition) &&
            primitiveArrayDefinition.Contract.StructuralType == null &&
            primitiveArrayDefinition.ParameterType.IsArray &&
            primitiveArrayDefinition.ParameterType.GetArrayRank() == 1 &&
            StructuralCollectionContract.TryGetElementType(targetType, out var targetElement) &&
            targetElement == primitiveArrayDefinition.ParameterType.GetElementType())
        {
            expression = renderer.RenderExpression(conversion.Input, context);
            return true;
        }

        if (definitionIndex < 0 || sourceDescriptor == null)
        {
            expression = null!;
            return false;
        }

        if (!NeedsAdapter(sourceDescriptor, targetType))
        {
            expression = renderer.RenderExpression(conversion.Input, context);
            return true;
        }

        expression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName(CreateAdapterName(definitionIndex, targetType)))
            .WithArgumentList(ExecutionSyntaxFactory.CreateArgumentList(
                renderer.RenderExpression(conversion.Input, context)));
        return true;
    }

    internal static bool TryCreateParameterStorage(
        IReadOnlyList<ScriptVariableDefinition> variableDefinitions,
        ScriptParameterDefinition definition,
        int parameterIndex,
        out TypeSyntax type)
    {
        ArgumentNullException.ThrowIfNull(variableDefinitions);
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.Contract.StructuralType is not { } descriptor)
        {
            type = null!;
            return false;
        }

        type = SyntaxFactory.ParseTypeName(
            CreateStorageType(descriptor, variableDefinitions.Count + parameterIndex, []));
        return true;
    }

    internal static string CreateAdapterName(int definitionIndex, Type targetType) =>
        AdapterPrefix + definitionIndex.ToString(CultureInfo.InvariantCulture) + "_" +
        StableHash(EvaluationHelper.GetCastableType(targetType)).ToString(CultureInfo.InvariantCulture);

    private static bool NeedsAdapter(StructuralTypeDescriptor source, Type targetType)
    {
        if (source.Kind == StructuralTypeKind.Record)
            return true;

        if (source.Kind != StructuralTypeKind.Collection ||
            source.ElementType == null ||
            !StructuralCollectionContract.TryGetElementType(targetType, out var targetElement))
            return false;

        return source.ElementType.Kind == StructuralTypeKind.Record ||
               (source.ElementType.Kind == StructuralTypeKind.Collection &&
                NeedsAdapter(source.ElementType, targetElement));
    }

    private static void AddCarrierMembers(
        ICollection<MemberDeclarationSyntax> members,
        StructuralTypeDescriptor descriptor,
        int definitionIndex,
        IReadOnlyList<int> path)
    {
        if (descriptor.Kind == StructuralTypeKind.Record)
        {
            for (var index = 0; index < descriptor.Fields.Count; index++)
            {
                var fieldType = descriptor.Fields[index].Type;
                if (fieldType.Kind is StructuralTypeKind.Record or StructuralTypeKind.Collection)
                    AddCarrierMembers(members, fieldType, definitionIndex, Append(path, index));
            }

            members.Add(CreateCarrierStruct(descriptor, definitionIndex, path));
            return;
        }

        if (descriptor.Kind == StructuralTypeKind.Collection &&
            descriptor.ElementType is { Kind: StructuralTypeKind.Record or StructuralTypeKind.Collection } element)
            AddCarrierMembers(members, element, definitionIndex, Append(path, -1));
    }

    private static StructDeclarationSyntax CreateCarrierStruct(
        StructuralTypeDescriptor descriptor,
        int definitionIndex,
        IReadOnlyList<int> path)
    {
        var name = CreateCarrierName(definitionIndex, path);
        var presenceCount = Math.Max(1, (descriptor.Fields.Count + 63) / 64);
        var lines = new List<string>
        {
            $"private readonly struct {name}",
            "{"
        };

        for (var index = 0; index < descriptor.Fields.Count; index++)
        {
            lines.Add(
                $"public readonly {CreateStorageType(descriptor.Fields[index].Type, definitionIndex, Append(path, index))} " +
                $"F{index.ToString(CultureInfo.InvariantCulture)};");
        }

        for (var index = 0; index < presenceCount; index++)
            lines.Add($"public readonly ulong P{index.ToString(CultureInfo.InvariantCulture)};");

        var parameters = descriptor.Fields
            .Select((field, index) =>
                $"{CreateStorageType(field.Type, definitionIndex, Append(path, index))} f{index.ToString(CultureInfo.InvariantCulture)}")
            .Concat(Enumerable.Range(0, presenceCount)
                .Select(index => $"ulong p{index.ToString(CultureInfo.InvariantCulture)}"));
        lines.Add($"public {name}({string.Join(", ", parameters)})");
        lines.Add("{");
        for (var index = 0; index < descriptor.Fields.Count; index++)
            lines.Add($"F{index.ToString(CultureInfo.InvariantCulture)} = f{index.ToString(CultureInfo.InvariantCulture)};");
        for (var index = 0; index < presenceCount; index++)
            lines.Add($"P{index.ToString(CultureInfo.InvariantCulture)} = p{index.ToString(CultureInfo.InvariantCulture)};");
        lines.Add("}");
        lines.Add("}");

        return (StructDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(
            string.Join(Environment.NewLine, lines))!;
    }

    internal static string CreateStorageType(
        StructuralTypeDescriptor descriptor,
        int definitionIndex,
        IReadOnlyList<int> path)
    {
        var text = descriptor.Kind switch
        {
            StructuralTypeKind.Scalar => EvaluationHelper.GetCastableType(descriptor.CoreValueType),
            StructuralTypeKind.Record => CreateCarrierName(definitionIndex, path),
            StructuralTypeKind.Collection =>
                $"{CreateStorageType(descriptor.ElementType!, definitionIndex, Append(path, -1))}[]",
            _ => throw new InvalidOperationException($"Unknown structural type '{descriptor.Kind}'.")
        };

        if (descriptor.IsNullable && descriptor.Kind != StructuralTypeKind.Scalar)
            text += "?";
        return text;
    }

    internal static ExpressionSyntax CreateValueExpression(
        object? value,
        StructuralTypeDescriptor descriptor,
        int definitionIndex,
        IReadOnlyList<int> path)
    {
        if (value == null || value is StructuralValue { Kind: StructuralTypeKind.Scalar, Scalar: null })
            return SyntaxFactory.DefaultExpression(
                SyntaxFactory.ParseTypeName(CreateStorageType(descriptor, definitionIndex, path)));

        if (descriptor.Kind == StructuralTypeKind.Scalar)
        {
            var scalar = value is StructuralValue structural ? structural.Scalar : value;
            return ScriptParameterSyntaxFactory.CreateDefaultArgumentExpression(
                descriptor.CoreValueType,
                scalar,
                descriptor);
        }

        if (descriptor.Kind == StructuralTypeKind.Record)
            return CreateRecordExpression(value, descriptor, definitionIndex, path);

        var elements = value switch
        {
            StructuralValue { Kind: StructuralTypeKind.Collection } structural =>
                structural.Elements.Cast<object?>().ToArray(),
            Array array => array.Cast<object?>().ToArray(),
            _ => throw new InvalidOperationException(
                $"Structural collection value '{value.GetType().Name}' is invalid.")
        };
        var elementPath = Append(path, -1);
        return ExecutionSyntaxFactory.CreateArrayCreation(
            CreateStorageType(descriptor.ElementType!, definitionIndex, elementPath),
            elements.Select(element =>
                CreateValueExpression(element, descriptor.ElementType!, definitionIndex, elementPath)));
    }

    private static ExpressionSyntax CreateRecordExpression(
        object value,
        StructuralTypeDescriptor descriptor,
        int definitionIndex,
        IReadOnlyList<int> path)
    {
        if (value is not StructuralValue { Kind: StructuralTypeKind.Record } record)
            throw new InvalidOperationException("Structural record storage requires a structural record value.");

        var presence = new ulong[Math.Max(1, (descriptor.Fields.Count + 63) / 64)];
        var arguments = new List<ExpressionSyntax>(descriptor.Fields.Count + presence.Length);
        for (var index = 0; index < descriptor.Fields.Count; index++)
        {
            var field = descriptor.Fields[index];
            var present = record.Fields.TryGetValue(field.Name, out var fieldValue);
            object? valueToRender = fieldValue;
            if (!present && field.HasDefault)
            {
                valueToRender = field.Default.Value;
                present = true;
            }

            if (present)
                presence[index / 64] |= 1UL << (index % 64);

            arguments.Add(CreateValueExpression(
                valueToRender,
                field.Type,
                definitionIndex,
                Append(path, index)));
        }

        arguments.AddRange(presence.Select(static value =>
            SyntaxFactory.ParseExpression(value.ToString(CultureInfo.InvariantCulture) + "UL")));
        return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.ParseTypeName(CreateCarrierName(definitionIndex, path)))
            .WithArgumentList(ExecutionSyntaxFactory.CreateArgumentList(arguments));
    }

    private sealed class AdapterBuilder
    {
        private readonly int _definitionIndex;
        private readonly StructuralTypeDescriptor _sourceRoot;
        private readonly List<MemberDeclarationSyntax> _members = [];
        private readonly HashSet<string> _created = new(StringComparer.Ordinal);

        public AdapterBuilder(int definitionIndex, StructuralTypeDescriptor sourceRoot)
        {
            _definitionIndex = definitionIndex;
            _sourceRoot = sourceRoot;
        }

        public IReadOnlyList<MemberDeclarationSyntax> Build(Type targetType)
        {
            var target = StructuralTypeDescriptor.FromClrType(targetType);
            var rootName = CreateAdapterName(_definitionIndex, targetType);
            _created.Add(rootName);
            var body = _sourceRoot.Kind == StructuralTypeKind.Record
                ? CreateRecordBody(_sourceRoot, target, targetType, "source", [])
                : CreateCollectionBody(_sourceRoot, target, targetType, "source", []);
            _members.Add(ParseMethod(
                EvaluationHelper.GetCastableType(targetType),
                rootName,
                CreateStorageType(_sourceRoot, _definitionIndex, []),
                body));
            return _members;
        }

        private string CreateRecordBody(
            StructuralTypeDescriptor source,
            StructuralTypeDescriptor target,
            Type targetType,
            string sourceExpression,
            IReadOnlyList<int> path)
        {
            var nullGuard = source.IsNullable
                ? $"if ({sourceExpression} == null) return default({EvaluationHelper.GetCastableType(targetType)});{Environment.NewLine}"
                : string.Empty;
            var actualSource = source.IsNullable ? sourceExpression + ".Value" : sourceExpression;
            return nullGuard + $"return {CreateRecordAdaptation(source, target, actualSource, path)};";
        }

        private string CreateCollectionBody(
            StructuralTypeDescriptor source,
            StructuralTypeDescriptor target,
            Type targetType,
            string sourceExpression,
            IReadOnlyList<int> path)
        {
            var sourceElement = source.ElementType ??
                throw new InvalidOperationException("Source collection element is missing.");
            var targetElement = target.ElementType ??
                throw new InvalidOperationException("Target collection element is missing.");
            var elementPath = Append(path, -1);
            var element = CreateElementAdaptation(
                sourceElement,
                targetElement,
                $"{sourceExpression}[index]",
                elementPath);

            var nullGuard = source.IsNullable
                ? $"if ({sourceExpression} == null) return default({EvaluationHelper.GetCastableType(targetType)});{Environment.NewLine}"
                : string.Empty;
            var targetElementType = CreateTargetStorageType(targetElement);
            return nullGuard +
                   $"var result = new {targetElementType}[{sourceExpression}.Length];{Environment.NewLine}" +
                   $"for (var index = 0; index < {sourceExpression}.Length; index++){Environment.NewLine}" +
                   $"{{{Environment.NewLine}    result[index] = {element};{Environment.NewLine}}}{Environment.NewLine}" +
                   "return result;";
        }

        private string CreateRecordAdaptation(
            StructuralTypeDescriptor source,
            StructuralTypeDescriptor target,
            string sourceExpression,
            IReadOnlyList<int> path)
        {
            var targetType = ScriptParameterSyntaxFactory.ResolveStructuralClrType(target);
            var constructor = StructuralInputMetadata.SelectConstructor(targetType);
            var arguments = new List<string>(constructor.GetParameters().Length);

            foreach (var parameter in constructor.GetParameters())
            {
                var targetField = target.Fields.FirstOrDefault(field =>
                    string.Equals(field.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
                var sourceIndex = -1;
                StructuralFieldDescriptor? sourceField = null;
                for (var index = 0; index < source.Fields.Count; index++)
                {
                    if (!string.Equals(source.Fields[index].Name, parameter.Name, StringComparison.OrdinalIgnoreCase))
                        continue;
                    sourceIndex = index;
                    sourceField = source.Fields[index];
                    break;
                }

                if (sourceField == null || targetField == null)
                {
                    arguments.Add(CreateTargetDefault(parameter, targetField));
                    continue;
                }

                var sourceValue = $"{sourceExpression}.F{sourceIndex.ToString(CultureInfo.InvariantCulture)}";
                var converted = CreateElementAdaptation(
                    sourceField.Type,
                    targetField.Type,
                    sourceValue,
                    Append(path, sourceIndex));
                var word = sourceIndex / 64;
                var bit = 1UL << (sourceIndex % 64);
                var present =
                    $"(({sourceExpression}.P{word.ToString(CultureInfo.InvariantCulture)} & " +
                    $"{bit.ToString(CultureInfo.InvariantCulture)}UL) != 0UL)";
                arguments.Add($"({present} ? {converted} : {CreateTargetDefault(parameter, targetField)})");
            }

            return $"new {EvaluationHelper.GetCastableType(targetType)}({string.Join(", ", arguments)})";
        }

        private string CreateElementAdaptation(
            StructuralTypeDescriptor source,
            StructuralTypeDescriptor target,
            string sourceExpression,
            IReadOnlyList<int> path)
        {
            if (source.Kind == StructuralTypeKind.Record && target.Kind == StructuralTypeKind.Record)
            {
                if (source.IsNullable)
                {
                    var targetType = CreateTargetStorageType(target);
                    var nonNull = CreateRecordAdaptation(source, target, sourceExpression + ".Value", path);
                    return $"({sourceExpression}.HasValue ? {nonNull} : default({targetType}))";
                }

                return CreateRecordAdaptation(source, target, sourceExpression, path);
            }

            if (source.Kind == StructuralTypeKind.Collection && target.Kind == StructuralTypeKind.Collection)
            {
                if (source.ElementType is { Kind: StructuralTypeKind.Scalar } sourceScalar &&
                    target.ElementType is { Kind: StructuralTypeKind.Scalar } targetScalar &&
                    sourceScalar.CoreValueType == targetScalar.CoreValueType)
                {
                    // An array is already a valid IReadOnlyList/IEnumerable receiver.
                    // Keep the typed array so scalar collection fields do not incur a copy.
                    return sourceExpression;
                }

                var methodName = CreateNestedAdapterName(path, source, target);
                EnsureNestedAdapter(methodName, source, target, path);
                return $"{methodName}({sourceExpression})";
            }

            if (source.Kind == StructuralTypeKind.Scalar && target.Kind == StructuralTypeKind.Scalar)
            {
                return source.CoreValueType == target.CoreValueType
                    ? sourceExpression
                    : $"({CreateTargetStorageType(target)}){sourceExpression}";
            }

            return $"({CreateTargetStorageType(target)}){sourceExpression}";
        }

        private void EnsureNestedAdapter(
            string methodName,
            StructuralTypeDescriptor source,
            StructuralTypeDescriptor target,
            IReadOnlyList<int> path)
        {
            if (!_created.Add(methodName))
                return;

            var sourceType = CreateStorageType(source, _definitionIndex, path);
            var targetType = CreateTargetStorageType(target);
            var body = CreateCollectionBody(
                source,
                target,
                ScriptParameterSyntaxFactory.ResolveStructuralClrType(target),
                "source",
                path);
            _members.Add(ParseMethod(targetType, methodName, sourceType, body));
        }

        private string CreateNestedAdapterName(
            IReadOnlyList<int> path,
            StructuralTypeDescriptor source,
            StructuralTypeDescriptor target) =>
            AdapterPrefix + _definitionIndex.ToString(CultureInfo.InvariantCulture) + "_" +
            StableHash(string.Join("/", path.Select(static value => value.ToString(CultureInfo.InvariantCulture))) +
                       ":" + source.ToCanonicalSql() + ":" + target.ToCanonicalSql())
                .ToString(CultureInfo.InvariantCulture);

        private static string CreateTargetStorageType(StructuralTypeDescriptor descriptor)
        {
            var text = descriptor.Kind switch
            {
                StructuralTypeKind.Scalar => EvaluationHelper.GetCastableType(descriptor.CoreValueType),
                StructuralTypeKind.Record => EvaluationHelper.GetCastableType(
                    ScriptParameterSyntaxFactory.ResolveStructuralClrType(descriptor)),
                StructuralTypeKind.Collection => $"{CreateTargetStorageType(descriptor.ElementType!)}[]",
                _ => throw new InvalidOperationException($"Unknown target structural type '{descriptor.Kind}'.")
            };
            if (descriptor.IsNullable && descriptor.Kind != StructuralTypeKind.Scalar)
                text += "?";
            return text;
        }

        private static string CreateTargetDefault(
            ParameterInfo parameter,
            StructuralFieldDescriptor? field)
        {
            if (field?.HasDefault != true)
                return $"default({EvaluationHelper.GetCastableType(parameter.ParameterType)})";

            return ScriptParameterSyntaxFactory.CreateDefaultArgumentExpression(
                    parameter.ParameterType,
                    field.Default.Value,
                    field.Type)
                .NormalizeWhitespace()
                .ToFullString();
        }

        private static MethodDeclarationSyntax ParseMethod(
            string returnType,
            string methodName,
            string sourceType,
            string body) =>
            (MethodDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(
                $"private static {returnType} {methodName}({sourceType} source) {{ {body} }}")!;
    }

    internal static string CreateCarrierName(int definitionIndex, IReadOnlyList<int> path)
    {
        var suffix = path.Count == 0
            ? "root"
            : string.Join("_", path.Select(static value =>
                value < 0 ? "a" : value.ToString(CultureInfo.InvariantCulture)));
        return CarrierPrefix + definitionIndex.ToString(CultureInfo.InvariantCulture) + "_" + suffix;
    }

    private static bool TryFindDefinition(
        IReadOnlyList<ScriptVariableDefinition> definitions,
        string name,
        out int index,
        out ScriptVariableDefinition definition)
    {
        for (var current = 0; current < definitions.Count; current++)
        {
            if (!string.Equals(definitions[current].Name, name, StringComparison.Ordinal))
                continue;

            index = current;
            definition = definitions[current];
            return true;
        }

        index = -1;
        definition = null!;
        return false;
    }

    private static bool TryFindDefinition(
        IReadOnlyList<ScriptVariableDefinition> definitions,
        string name,
        out ScriptVariableDefinition definition) =>
        TryFindDefinition(definitions, name, out _, out definition);

    private static bool TryFindParameter(
        IReadOnlyList<ScriptParameterDefinition> definitions,
        string name,
        out int index,
        out ScriptParameterDefinition definition)
    {
        for (var current = 0; current < definitions.Count; current++)
        {
            if (!string.Equals(definitions[current].Name, name, StringComparison.Ordinal))
                continue;

            index = current;
            definition = definitions[current];
            return true;
        }

        index = -1;
        definition = null!;
        return false;
    }

    private static IReadOnlyList<int> Append(IReadOnlyList<int> path, int value)
    {
        var result = new int[path.Count + 1];
        for (var index = 0; index < path.Count; index++)
            result[index] = path[index];
        result[^1] = value;
        return result;
    }

    private static uint StableHash(string value)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (var character in value)
            {
                hash ^= character;
                hash *= 16777619;
            }

            return hash;
        }
    }
}
