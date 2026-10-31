using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Targets.CSharpClr;

internal sealed class TypedStructuredJsonSyntax(int definitionIndex)
{
    private readonly List<MemberDeclarationSyntax> _members = [];

    public IReadOnlyList<MemberDeclarationSyntax> Create(StructuralTypeDescriptor descriptor, ScriptParameterDefinition definition)
    {
        AddShape(descriptor, []);
        var type = StructuralCarrierSyntaxFactory.CreateStorageType(descriptor, definitionIndex, []);
        _members.Add(SyntaxFactory.ParseMemberDeclaration($$"""
            private static {{type}} __musoqBindTypedJson_{{definitionIndex}}(ExecutionState executionState, string name, StructuralParameterCaptureState state)
            {
                try
                {
                    {{DefaultBinding(descriptor, definition)}}
                    return {{MethodName(definitionIndex, [])}}(executionState.TypedParameters.GetRequired<System.Text.Json.JsonElement>(name), state, 1, "$" + name);
                }
                catch (OperationCanceledException) { throw; }
                catch (global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException) { throw; }
                catch (Exception exception)
                {
                    throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.TypeMismatch(name, typeof({{type.Replace("[]?", "[]", StringComparison.Ordinal)}}), typeof(System.Text.Json.JsonElement), exception);
                }
            }
            """)!);
        return _members;
    }

    private string DefaultBinding(StructuralTypeDescriptor descriptor, ScriptParameterDefinition definition)
    {
        if (!definition.HasDefaultValue) return string.Empty;
        var metrics = StructuralConstantMetrics.MeasureConstant(definition.DefaultValue, descriptor);
        var value = StructuralCarrierSyntaxFactory.CreateValueExpression(definition.DefaultValue, descriptor,
            definitionIndex, []).NormalizeWhitespace().ToFullString();
        return $$"""
            if (!executionState.TypedParameters.Contains(name))
            {
                state.ReserveMetrics(new StructuralInputMetrics({{metrics.MaxDepth}}, {{metrics.NodeCount}}L, {{metrics.StringBytes}}L), 1);
                return {{value}};
            }
            """;
    }

    public static string MethodName(int index, IReadOnlyList<int> path) =>
        "__musoqDecodeTypedJson_" + index.ToString(CultureInfo.InvariantCulture) + "_" +
        string.Join("_", path.Select(static step => step < 0 ? "element" : step.ToString(CultureInfo.InvariantCulture)));

    private void AddShape(StructuralTypeDescriptor descriptor, int[] path)
    {
        if (descriptor.Kind == StructuralTypeKind.Record)
            for (var index = 0; index < descriptor.Fields.Count; index++)
                AddShape(descriptor.Fields[index].Type, [..path, index]);
        if (descriptor.ElementType is { } element)
            AddShape(element, [..path, -1]);

        var type = StructuralCarrierSyntaxFactory.CreateStorageType(descriptor, definitionIndex, path);
        var body = descriptor.Kind switch
        {
            StructuralTypeKind.Scalar => ScalarBody(descriptor, type),
            StructuralTypeKind.Record => RecordBody(descriptor, path),
            StructuralTypeKind.Collection => CollectionBody(descriptor, path),
            _ => throw new InvalidOperationException("Unsupported JSON structural kind.")
        };
        if (descriptor.IsNullable && descriptor.Kind != StructuralTypeKind.Scalar)
            body = $"if (value.ValueKind == System.Text.Json.JsonValueKind.Null) {{ state.ReserveNode(depth); return default({type}); }} " + body;
        _members.Add(SyntaxFactory.ParseMemberDeclaration(
            $"private static {type} {MethodName(definitionIndex, path)}(System.Text.Json.JsonElement value, StructuralParameterCaptureState state, int depth, string path) {{ {body} }}")!);
    }

    private static string ScalarBody(StructuralTypeDescriptor descriptor, string storageType)
    {
        var scalarType = descriptor.ScalarType!;
        var read = ScalarRead(scalarType);
        if (read is null)
            return $"throw new InvalidOperationException(\"No concrete JSON reader is registered for '{scalarType}'.\");";
        if (scalarType.IsEnum)
            read = $"({storageType})({read})";
        var nullRead = descriptor.IsNullable ? $"return default({storageType});" :
            $"throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.NullNotAllowed(path, typeof({storageType}));";
        return $"state.ReserveNode(depth); if (value.ValueKind == System.Text.Json.JsonValueKind.Null) {{ {nullRead} }} " +
            $"var result = {read}; " + (scalarType == typeof(string) ? "state.ReserveString(result); " : "") + "return result;";
    }

    private static string? ScalarRead(Type scalarType) => Type.GetTypeCode(scalarType) switch
    {
        TypeCode.String => "value.GetString()!",
        TypeCode.Char => "TypedParameterJsonReader.ReadChar(value, path)",
        TypeCode.Boolean => "value.GetBoolean()",
        TypeCode.Byte => "value.GetByte()",
        TypeCode.SByte => "value.GetSByte()",
        TypeCode.Int16 => "value.GetInt16()",
        TypeCode.UInt16 => "value.GetUInt16()",
        TypeCode.Int32 => "value.GetInt32()",
        TypeCode.UInt32 => "value.GetUInt32()",
        TypeCode.Int64 => "value.GetInt64()",
        TypeCode.UInt64 => "value.GetUInt64()",
        TypeCode.Single => "value.GetSingle()",
        TypeCode.Double => "value.GetDouble()",
        TypeCode.Decimal => "value.GetDecimal()",
        TypeCode.DateTime => "value.GetDateTime()",
        _ when scalarType == typeof(DateTimeOffset) => "value.GetDateTimeOffset()",
        _ when scalarType == typeof(Guid) => "value.GetGuid()",
        _ when scalarType == typeof(TimeSpan) => "System.TimeSpan.Parse(value.GetString()!, System.Globalization.CultureInfo.InvariantCulture)",
        _ => null
    };

    private string RecordBody(StructuralTypeDescriptor descriptor, int[] path)
    {
        var lines = new List<string> { "TypedParameterJsonReader.RequireRecord(value, state, depth, path);" };
        var namesField = MethodName(definitionIndex, path) + "_fields";
        var names = string.Join(", ", descriptor.Fields.Select(field => SyntaxFactory.Literal(field.Name).Text));
        _members.Add(SyntaxFactory.ParseMemberDeclaration($"private static readonly string[] {namesField} = new string[] {{ {names} }};")!);
        lines.Add($"TypedParameterJsonReader.ValidateFields(value, {namesField}, path);");
        var arguments = new List<string>();
        var presence = new List<string>();
        for (var index = 0; index < descriptor.Fields.Count; index++)
        {
            var field = descriptor.Fields[index];
            var fieldPath = path.Append(index).ToArray();
            lines.AddRange(RecordField(field, index, fieldPath));
            arguments.Add($"field{index}");
            presence.Add(field.HasDefault ? "true" : $"present{index}");
        }
        for (var word = 0; word < Math.Max(1, (presence.Count + 63) / 64); word++)
        {
            var bits = Enumerable.Range(word * 64, Math.Min(64, presence.Count - word * 64))
                .Select(index => $"({presence[index]} ? {(1UL << (index % 64)).ToString(CultureInfo.InvariantCulture)}UL : 0UL)");
            arguments.Add(presence.Count == 0 ? "0UL" : string.Join(" | ", bits));
        }
        lines.Add($"return new {StructuralCarrierSyntaxFactory.CreateCarrierName(definitionIndex, path)}({string.Join(", ", arguments)});");
        return string.Join("\n", lines);
    }

    private IEnumerable<string> RecordField(StructuralFieldDescriptor field, int index, int[] path)
    {
        var type = StructuralCarrierSyntaxFactory.CreateStorageType(field.Type, definitionIndex, path);
        var name = SyntaxFactory.Literal(field.Name).Text;
        var memberPath = SyntaxFactory.Literal("." + field.Name).Text;
        yield return $"var (present{index}, input{index}) = TypedParameterJsonReader.FindField(value, {name});";
        if (field.Required && !field.HasDefault)
            yield return $"if (!present{index}) throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.MissingRequired(path + {memberPath});";

        var fallback = $"default({type})";
        if (field.HasDefault)
        {
            fallback = StructuralCarrierSyntaxFactory.CreateValueExpression(field.Default.Value, field.Type,
                definitionIndex, path).NormalizeWhitespace().ToFullString();
            var metrics = StructuralConstantMetrics.MeasureConstant(field.Default.Value, field.Type);
            yield return $"if (!present{index}) state.ReserveMetrics(new StructuralInputMetrics({metrics.MaxDepth}, {metrics.NodeCount}L, {metrics.StringBytes}L), depth + 1);";
        }
        yield return $"{type} field{index} = present{index} ? {MethodName(definitionIndex, path)}(input{index}, state, depth + 1, path + {memberPath}) : {fallback};";
    }

    private string CollectionBody(StructuralTypeDescriptor descriptor, int[] path)
    {
        var elementPath = path.Append(-1).ToArray();
        var elementType = StructuralCarrierSyntaxFactory.CreateStorageType(descriptor.ElementType!, definitionIndex, elementPath);
        var allocation = StructuralCarrierSyntaxFactory.CreateArrayAllocation(elementType, "count");
        return $$"""
            state.ReserveNode(depth);
            if (value.ValueKind != System.Text.Json.JsonValueKind.Array)
                throw global::Musoq.Evaluator.Exceptions.ScriptParameterBindingException.TypeMismatch(path, typeof({{elementType}}[]), typeof(System.Text.Json.JsonElement));
            var count = value.GetArrayLength();
            state.EnsureCollectionLowerBound(count);
            var result = {{allocation}};
            for (var index = 0; index < count; index++)
                result[index] = {{MethodName(definitionIndex, elementPath)}}(value[index], state, depth + 1, path + "[" + index + "]");
            return result;
            """;
    }
}
