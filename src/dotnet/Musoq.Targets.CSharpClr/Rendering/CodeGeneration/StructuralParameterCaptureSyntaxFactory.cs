using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator;
using Musoq.Evaluator.Helpers;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Targets.CSharpClr;

/// <summary>Emits the generated, typed host-parameter preflight path.</summary>
internal static class StructuralParameterCaptureSyntaxFactory
{
    private const string CapturePrefix = "__musoqCaptureStructuralParameter_";
    private const string FieldNamesPrefix = "__musoqStructuralParameterFields_";

    internal static IReadOnlyList<MemberDeclarationSyntax> CreateMembers(
        IReadOnlyList<ScriptVariableDefinition> variableDefinitions,
        IReadOnlyList<ScriptParameterDefinition> parameterDefinitions)
    {
        ArgumentNullException.ThrowIfNull(variableDefinitions);
        ArgumentNullException.ThrowIfNull(parameterDefinitions);

        var members = new List<MemberDeclarationSyntax>();
        var parameterOffset = variableDefinitions.Count;
        for (var parameterIndex = 0; parameterIndex < parameterDefinitions.Count; parameterIndex++)
        {
            var definition = parameterDefinitions[parameterIndex];
            if (definition.Contract.StructuralType is not { } descriptor)
                continue;

            var globalIndex = parameterOffset + parameterIndex;
            var builder = new CaptureBuilder(globalIndex, descriptor);
            members.AddRange(builder.Build());
        }

        if (parameterDefinitions.Any(static definition => definition.Contract.IsStructured))
            members.AddRange(CreateSnapshotMembers(variableDefinitions, parameterDefinitions));

        return members;
    }

    private static IReadOnlyList<MemberDeclarationSyntax> CreateSnapshotMembers(
        IReadOnlyList<ScriptVariableDefinition> variableDefinitions,
        IReadOnlyList<ScriptParameterDefinition> parameterDefinitions)
    {
        var parameterOffset = variableDefinitions.Count;
        var lines = new List<string>
        {
            "ArgumentNullException.ThrowIfNull(supplied);",
            "cancellationToken.ThrowIfCancellationRequested();",
            "var __musoqCaptureState = new StructuralParameterCaptureState(cancellationToken);",
            "var __musoqSnapshot = new Dictionary<string, object?>(StringComparer.Ordinal);"
        };

        for (var parameterIndex = 0; parameterIndex < parameterDefinitions.Count; parameterIndex++)
        {
            var definition = parameterDefinitions[parameterIndex];
            var name = Escape(definition.Name);
            var globalIndex = parameterOffset + parameterIndex;
            var rawName = $"__musoqRawParameter_{parameterIndex.ToString(CultureInfo.InvariantCulture)}";
            var descriptor = definition.Contract.StructuralType;
            if (descriptor != null)
            {
                var captureName = CreateCaptureName(globalIndex, []);
                lines.Add($"if (supplied.TryGetValue(\"{name}\", out var {rawName}))");
                lines.Add("{");
                lines.Add("    try");
                lines.Add("    {");
                lines.Add($"        __musoqSnapshot[\"{name}\"] = {captureName}({rawName}, __musoqCaptureState, 1, \"${name}\");");
                lines.Add("    }");
                lines.Add("    catch (OperationCanceledException)");
                lines.Add("    {");
                lines.Add("        throw;");
                lines.Add("    }");
                lines.Add("    catch (global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException)");
                lines.Add("    {");
                lines.Add("        throw;");
                lines.Add("    }");
                lines.Add("    catch (Exception exception)");
                lines.Add("    {");
                lines.Add($"        throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.TypeMismatch(\"{name}\", typeof({ScriptParameterSyntaxFactory.CreateTypeSyntax(definition.ParameterType)}), {rawName}, exception);");
                lines.Add("    }");
                lines.Add("}");
                if (definition.HasDefaultValue)
                {
                    lines.Add("else");
                    lines.Add("{");
                    var metrics = MeasureConstant(definition.DefaultValue, descriptor);
                    lines.Add($"    __musoqCaptureState.ReserveMetrics(new StructuralInputMetrics({metrics.MaxDepth}, {metrics.NodeCount.ToString(CultureInfo.InvariantCulture)}L, {metrics.StringBytes.ToString(CultureInfo.InvariantCulture)}L), 1);");
                    var defaultExpression = StructuralCarrierSyntaxFactory.CreateValueExpression(
                        definition.DefaultValue,
                        descriptor,
                        globalIndex,
                        [])
                        .NormalizeWhitespace()
                        .ToFullString();
                    lines.Add($"    __musoqSnapshot[\"{name}\"] = {defaultExpression};");
                    lines.Add("}");
                }
                else if (definition.IsRequired)
                {
                    lines.Add("else");
                    lines.Add("{");
                    lines.Add($"    throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.MissingRequired(\"{name}\");");
                    lines.Add("}");
                }

                continue;
            }

            lines.Add($"if (supplied.ContainsKey(\"{name}\"))");
            lines.Add("{");
            if (definition.ParameterType.IsArray)
            {
                var elementType = definition.ParameterType.GetElementType() ?? typeof(object);
                var elementTypeText = ScriptParameterSyntaxFactory.CreateTypeSyntax(elementType);
                lines.Add($"    __musoqSnapshot[\"{name}\"] = ScriptParameterBinder.GetRequiredCollection<{elementTypeText}>(supplied, \"{name}\");");
            }
            else
            {
                var typeText = ScriptParameterSyntaxFactory.CreateTypeSyntax(definition.ParameterType);
                lines.Add($"    __musoqSnapshot[\"{name}\"] = ScriptParameterBinder.GetRequired<{typeText}>(supplied, \"{name}\");");
            }
            lines.Add("}");
            if (definition.IsRequired)
            {
                lines.Add("else");
                lines.Add("{");
                lines.Add($"    throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.MissingRequired(\"{name}\");");
                lines.Add("}");
            }
        }

        lines.Add("ScriptParameterBinder.ValidateNoUnknownParameters(supplied, __musoqDeclaredParameterNames);");
        lines.Add("return new System.Collections.ObjectModel.ReadOnlyDictionary<string, object?>(__musoqSnapshot);");

        var names = parameterDefinitions.Count == 0
            ? "Array.Empty<string>()"
            : $"new string[] {{ {string.Join(", ", parameterDefinitions.Select(definition => $"\"{Escape(definition.Name)}\""))} }}";
        var source = $"private static readonly string[] __musoqDeclaredParameterNames = {names};";
        var field = (FieldDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(source)!;
        var method = (MethodDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(
            "public IReadOnlyDictionary<string, object?> CaptureParameterSnapshot(IReadOnlyDictionary<string, object?> supplied, CancellationToken cancellationToken) { " +
            string.Join(Environment.NewLine, lines) +
            " }")!;
        return new SnapshotMembers(field, method).Members;
    }

    private sealed class CaptureBuilder
    {
        private readonly int _globalIndex;
        private readonly StructuralTypeDescriptor _root;
        private readonly List<MemberDeclarationSyntax> _members = [];
        private readonly HashSet<string> _created = new(StringComparer.Ordinal);

        public CaptureBuilder(int globalIndex, StructuralTypeDescriptor root)
        {
            _globalIndex = globalIndex;
            _root = root;
        }

        public IReadOnlyList<MemberDeclarationSyntax> Build()
        {
            CreateShape(_root, []);
            return _members;
        }

        private void CreateShape(StructuralTypeDescriptor descriptor, IReadOnlyList<int> path)
        {
            var methodName = CreateCaptureName(_globalIndex, path);
            if (!_created.Add(methodName))
                return;

            if (descriptor.Kind == StructuralTypeKind.Record)
            {
                _members.Add(CreateFieldNamesField(descriptor, path));
                foreach (var field in descriptor.Fields)
                {
                    if (field.Type.Kind != StructuralTypeKind.Scalar)
                        CreateShape(field.Type, Append(path, IndexOf(descriptor, field)));
                }
            }
            else if (descriptor.ElementType is { } element)
            {
                CreateShape(element, Append(path, -1));
            }

            _members.Add(CreateCaptureMethod(descriptor, path));
        }

        private MemberDeclarationSyntax CreateCaptureMethod(
            StructuralTypeDescriptor descriptor,
            IReadOnlyList<int> path)
        {
            var returnType = StructuralCarrierSyntaxFactory.CreateStorageType(descriptor, _globalIndex, path);
            var body = descriptor.Kind switch
            {
                StructuralTypeKind.Scalar => CreateScalarBody(descriptor),
                StructuralTypeKind.Record => CreateRecordBody(descriptor, path),
                StructuralTypeKind.Collection => CreateCollectionBody(descriptor, path),
                _ => throw new InvalidOperationException($"Unknown structural kind '{descriptor.Kind}'.")
            };
            return (MethodDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(
                $"private static {returnType} {CreateCaptureName(_globalIndex, path)}(object? rawValue, StructuralParameterCaptureState state, int depth, string path) {{ {body} }}")!;
        }

        private string CreateScalarBody(StructuralTypeDescriptor descriptor)
        {
            var type = StructuralCarrierSyntaxFactory.CreateStorageType(descriptor, _globalIndex, []);
            return $"return StructuralParameterCaptureRuntime.ReadScalar<{type}>(rawValue, path, {descriptor.IsNullable.ToString().ToLowerInvariant()}, state, depth);";
        }

        private string CreateRecordBody(StructuralTypeDescriptor descriptor, IReadOnlyList<int> path)
        {
            var type = StructuralCarrierSyntaxFactory.CreateStorageType(descriptor, _globalIndex, path);
            var lines = new List<string>
            {
                "state.ReserveNode(depth);",
                $"if (rawValue == null || rawValue is StructuralValue {{ Kind: StructuralTypeKind.Scalar, Scalar: null }}) {{ {(descriptor.IsNullable ? $"return default({type});" : "throw new InvalidOperationException(\"A non-null structural record was expected.\");")} }}",
                $"state.Enter(rawValue, path);",
                "try",
                "{",
                $"    StructuralParameterCaptureRuntime.ValidateRecord(rawValue, {CreateFieldNamesName(_globalIndex, path)}, path);"
            };

            var arguments = new List<string>();
            for (var index = 0; index < descriptor.Fields.Count; index++)
            {
                var field = descriptor.Fields[index];
                var rawName = $"__rawField{index.ToString(CultureInfo.InvariantCulture)}";
                var presentName = $"__presentField{index.ToString(CultureInfo.InvariantCulture)}";
                lines.Add($"    var {presentName} = StructuralParameterCaptureRuntime.TryGetRecordField(rawValue, \"{Escape(field.Name)}\", out var {rawName});");
                var fieldPath = $"path + \".{Escape(field.Name)}\"";
                var value = field.Type.Kind == StructuralTypeKind.Scalar
                    ? $"StructuralParameterCaptureRuntime.ReadScalar<{StructuralCarrierSyntaxFactory.CreateStorageType(field.Type, _globalIndex, Append(path, index))}>({rawName}, {fieldPath}, {field.Type.IsNullable.ToString().ToLowerInvariant()}, state, depth + 1)"
                    : CreateCaptureCall(field.Type, Append(path, index), rawName, "state", "depth + 1", fieldPath);
                var defaultValue = field.HasDefault
                    ? StructuralCarrierSyntaxFactory.CreateValueExpression(field.Default.Value, field.Type, _globalIndex, Append(path, index))
                        .NormalizeWhitespace().ToFullString()
                    : $"default({StructuralCarrierSyntaxFactory.CreateStorageType(field.Type, _globalIndex, Append(path, index))})";
                var localType = StructuralCarrierSyntaxFactory.CreateStorageType(field.Type, _globalIndex, Append(path, index));
                lines.Add($"    {localType} __field{index.ToString(CultureInfo.InvariantCulture)};");
                lines.Add($"    if ({presentName})");
                lines.Add("    {");
                lines.Add($"        __field{index.ToString(CultureInfo.InvariantCulture)} = {value};");
                lines.Add("    }");
                if (field.HasDefault)
                {
                    lines.Add("    else");
                    lines.Add("    {");
                    var metrics = MeasureConstant(field.Default.Value, field.Type);
                    lines.Add($"        state.ReserveMetrics(new StructuralInputMetrics({metrics.MaxDepth}, {metrics.NodeCount.ToString(CultureInfo.InvariantCulture)}L, {metrics.StringBytes.ToString(CultureInfo.InvariantCulture)}L), depth + 1);");
                    lines.Add($"        __field{index.ToString(CultureInfo.InvariantCulture)} = {defaultValue};");
                    lines.Add($"        {presentName} = true;");
                    lines.Add("    }");
                }
                else if (field.Required)
                {
                    lines.Add("    else");
                    lines.Add("    {");
                    lines.Add($"        throw new InvalidOperationException(\"Structural value '\" + path + \"' is missing required field '{Escape(field.Name)}'.\");");
                    lines.Add("    }");
                }
                else
                {
                    lines.Add("    else");
                    lines.Add("    {");
                    lines.Add($"        __field{index.ToString(CultureInfo.InvariantCulture)} = default;");
                    lines.Add("    }");
                }

                arguments.Add($"__field{index.ToString(CultureInfo.InvariantCulture)}");
            }

            // Presence is emitted from runtime booleans so explicit null and
            // declaration defaults remain present while omitted optional fields
            // remain absent.
            var presenceCount = Math.Max(1, (descriptor.Fields.Count + 63) / 64);
            for (var word = 0; word < presenceCount; word++)
            {
                var terms = new List<string>();
                for (var index = word * 64; index < Math.Min(descriptor.Fields.Count, (word + 1) * 64); index++)
                    terms.Add($"(__presentField{index.ToString(CultureInfo.InvariantCulture)} ? {((1UL << (index % 64)).ToString(CultureInfo.InvariantCulture))}UL : 0UL)");
                arguments.Add(string.Join(" | ", terms));
            }

            lines.Add($"    return new {type}({string.Join(", ", arguments)});");
            lines.Add("}");
            lines.Add("finally");
            lines.Add("{");
            lines.Add("    state.Exit(rawValue);");
            lines.Add("}");
            return string.Join(Environment.NewLine, lines);
        }

        private string CreateCollectionBody(StructuralTypeDescriptor descriptor, IReadOnlyList<int> path)
        {
            var type = StructuralCarrierSyntaxFactory.CreateStorageType(descriptor, _globalIndex, path);
            var element = descriptor.ElementType ?? throw new InvalidOperationException("Collection element type is missing.");
            var elementType = StructuralCarrierSyntaxFactory.CreateStorageType(element, _globalIndex, Append(path, -1));
            var elementPath = "path + \"[\" + index + \"]\"";
            var structuralValue = CreateCaptureCall(
                element,
                Append(path, -1),
                "structural.Elements[index]",
                "state",
                "depth + 1",
                elementPath);
            var referenceValue = CreateCaptureCall(
                element,
                Append(path, -1),
                "referenceList[index]",
                "state",
                "depth + 1",
                elementPath);
            var typedValue = element.Kind == StructuralTypeKind.Scalar
                ? element.ScalarType == typeof(string)
                    ? "StructuralParameterCaptureRuntime.ReadTypedString(typedList[index], state, depth + 1)"
                    : $"StructuralParameterCaptureRuntime.ReadTypedScalar<{elementType}>(typedList[index], state, depth + 1)"
                : CreateCaptureCall(
                    element,
                    Append(path, -1),
                    "typedList[index]",
                    "state",
                    "depth + 1",
                    elementPath);
            var typedArrayValue = element.Kind == StructuralTypeKind.Scalar
                ? element.ScalarType == typeof(string)
                    ? "StructuralParameterCaptureRuntime.ReadTypedString(typedArray[index], state, depth + 1)"
                    : $"StructuralParameterCaptureRuntime.ReadTypedScalar<{elementType}>(typedArray[index], state, depth + 1)"
                : CreateCaptureCall(
                    element,
                    Append(path, -1),
                    "typedArray[index]",
                    "state",
                    "depth + 1",
                    elementPath);
            var typedCountType = elementType;
            var nullBranch = descriptor.IsNullable
                ? $"return default({type});"
                : "throw new InvalidOperationException(\"A non-null structural collection was expected.\");";
            var lines = new List<string>
            {
                "state.ReserveNode(depth);",
                $"if (rawValue == null || rawValue is StructuralValue {{ Kind: StructuralTypeKind.Scalar, Scalar: null }}) {{ {nullBranch} }}",
                "state.Enter(rawValue, path);",
                "try",
                "{",
                $"    var count = StructuralParameterCaptureRuntime.GetCollectionCount<{typedCountType}>(rawValue, path);",
                "    state.EnsureCollectionLowerBound(count);",
                $"    var result = new {elementType}[count];"
            };
            AddCollectionLoop(lines, $"rawValue is {elementType}[] typedArray", typedArrayValue, first: true);
            AddCollectionLoop(lines, $"rawValue is IReadOnlyList<{elementType}> typedList", typedValue, first: false);
            AddCollectionLoop(lines, "rawValue is IReadOnlyList<object?> referenceList", referenceValue, first: false);
            AddCollectionLoop(lines, "rawValue is StructuralValue { Kind: StructuralTypeKind.Collection } structural", structuralValue, first: false);
            lines.Add("    else");
            lines.Add("    {");
            lines.Add("        throw new InvalidOperationException(\"Structural collection does not expose a supported indexed representation.\");");
            lines.Add("    }");
            lines.Add("    return result;");
            lines.Add("}");
            lines.Add("finally");
            lines.Add("{");
            lines.Add("    state.Exit(rawValue);");
            lines.Add("}");
            return string.Join(Environment.NewLine, lines);
        }

        private static void AddCollectionLoop(
            ICollection<string> lines,
            string condition,
            string value,
            bool first)
        {
            if (!first)
                lines.Add("    else");
            lines.Add($"    if ({condition})");
            lines.Add("    {");
            lines.Add("        for (var index = 0; index < count; index++)");
            lines.Add("        {");
            lines.Add("            if ((index & 1023) == 0) state.CancellationToken.ThrowIfCancellationRequested();");
            lines.Add($"            result[index] = {value};");
            lines.Add("        }");
            lines.Add("    }");
        }

        private MemberDeclarationSyntax CreateFieldNamesField(StructuralTypeDescriptor descriptor, IReadOnlyList<int> path)
        {
            var values = string.Join(", ", descriptor.Fields.Select(field => $"\"{Escape(field.Name)}\""));
            return SyntaxFactory.ParseMemberDeclaration(
                $"private static readonly string[] {CreateFieldNamesName(_globalIndex, path)} = new[] {{ {values} }};")!;
        }

        private string CreateCaptureCall(
            StructuralTypeDescriptor descriptor,
            IReadOnlyList<int> path,
            string raw,
            string state,
            string depth,
            string valuePath)
        {
            if (descriptor.Kind == StructuralTypeKind.Scalar)
            {
                var type = StructuralCarrierSyntaxFactory.CreateStorageType(descriptor, _globalIndex, path);
                return $"StructuralParameterCaptureRuntime.ReadScalar<{type}>({raw}, {valuePath}, {descriptor.IsNullable.ToString().ToLowerInvariant()}, {state}, {depth})";
            }

            return $"{CreateCaptureName(_globalIndex, path)}({raw}, {state}, {depth}, {valuePath})";
        }

        private static int IndexOf(StructuralTypeDescriptor descriptor, StructuralFieldDescriptor field)
        {
            for (var index = 0; index < descriptor.Fields.Count; index++)
                if (ReferenceEquals(descriptor.Fields[index], field))
                    return index;
            throw new InvalidOperationException("Structural field metadata is not indexed.");
        }
    }

    private static StructuralInputMetrics MeasureConstant(
        object? value,
        StructuralTypeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return MeasureConstant(value, descriptor, 1);
    }

    private static StructuralInputMetrics MeasureConstant(
        object? value,
        StructuralTypeDescriptor descriptor,
        int depth)
    {
        var metrics = new StructuralInputMetrics(depth, 1, 0);
        if (value is StructuralValue scalar && scalar.Kind == StructuralTypeKind.Scalar)
            value = scalar.Scalar;
        if (descriptor.Kind == StructuralTypeKind.Scalar)
        {
            if (value is string text)
                metrics = metrics with { StringBytes = checked((long)text.Length * sizeof(char)) };
            return metrics;
        }

        if (value == null)
            return metrics;

        if (descriptor.Kind == StructuralTypeKind.Record &&
            value is StructuralValue { Kind: StructuralTypeKind.Record } record)
        {
            foreach (var field in descriptor.Fields)
            {
                if (record.Fields.TryGetValue(field.Name, out var fieldValue))
                    metrics = Merge(metrics, MeasureConstant(fieldValue, field.Type, depth + 1));
                else if (field.HasDefault)
                    metrics = Merge(metrics, MeasureConstant(field.Default.Value, field.Type, depth + 1));
            }

            return metrics;
        }

        if (descriptor.Kind == StructuralTypeKind.Collection)
        {
            IEnumerable<object?> elements;
            if (value is StructuralValue { Kind: StructuralTypeKind.Collection } structural)
                elements = structural.Elements.Cast<object?>();
            else if (value is Array array)
                elements = array.Cast<object?>();
            else
                elements = [];
            var elementType = descriptor.ElementType ?? throw new InvalidOperationException("Collection element type is missing.");
            foreach (var element in elements)
                metrics = Merge(metrics, MeasureConstant(element, elementType, depth + 1));
        }

        return metrics;
    }

    private static StructuralInputMetrics Merge(
        StructuralInputMetrics left,
        StructuralInputMetrics right) =>
        new(
            Math.Max(left.MaxDepth, right.MaxDepth),
            checked(left.NodeCount + right.NodeCount),
            checked(left.StringBytes + right.StringBytes));

    private static string CreateCaptureName(int index, IReadOnlyList<int> path)
    {
        var suffix = path.Count == 0
            ? "root"
            : string.Join("_", path.Select(static value => value < 0 ? "a" : value.ToString(CultureInfo.InvariantCulture)));
        return CapturePrefix + index.ToString(CultureInfo.InvariantCulture) + "_" + suffix;
    }

    private static string CreateFieldNamesName(int index, IReadOnlyList<int> path)
    {
        var suffix = path.Count == 0
            ? "root"
            : string.Join("_", path.Select(static value => value < 0 ? "a" : value.ToString(CultureInfo.InvariantCulture)));
        return FieldNamesPrefix + index.ToString(CultureInfo.InvariantCulture) + "_" + suffix;
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private static IReadOnlyList<int> Append(IReadOnlyList<int> path, int value)
    {
        var result = new int[path.Count + 1];
        for (var index = 0; index < path.Count; index++)
            result[index] = path[index];
        result[^1] = value;
        return result;
    }

    private sealed class SnapshotMembers(FieldDeclarationSyntax field, MethodDeclarationSyntax method)
    {
        public IReadOnlyList<MemberDeclarationSyntax> Members { get; } = [field, method];
    }
}
