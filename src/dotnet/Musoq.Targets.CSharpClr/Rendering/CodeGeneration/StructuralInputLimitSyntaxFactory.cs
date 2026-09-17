using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Targets.CSharpClr;

/// <summary>
/// Emits receiver-limit checks from the planner's structural shape.  The
/// generated walker reads concrete fields and indexed collections directly;
/// it never routes successful values through an object or reflection helper.
/// </summary>
internal static class StructuralInputLimitSyntaxFactory
{
    private const string HelperPrefix = "__musoqCheckStructural_";

    internal static IReadOnlyList<MemberDeclarationSyntax> CreateMembers(
        ExecutionBlock body)
    {
        ArgumentNullException.ThrowIfNull(body);

        return ExecutionIrAnalysis.CollectExpressions<ExecutionStructuralConversion>(body)
            .Where(static conversion => conversion.LimitBinding != null &&
                                        conversion.Input is not ExecutionCteCollectionInput)
            .GroupBy(GetHelperName, StringComparer.Ordinal)
            .Select(group => CreateMember(group.First(), group.Key))
            .ToArray();
    }

    internal static ExpressionSyntax RenderGuard(
        ExpressionSyntax value,
        Type valueType,
        ExecutionStructuralLimitBinding binding,
        ExecutionRenderContext context,
        ExecutionCSharpRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(valueType);
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(renderer);

        return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(GetHelperName(valueType, binding)))
            .WithArgumentList(ExecutionSyntaxFactory.CreateArgumentList(
                value,
                SyntaxFactory.IdentifierName("token")));
    }

    internal static string GetHelperName(ExecutionStructuralConversion conversion) =>
        GetHelperName(conversion.TargetType.RequireClrType(), conversion.LimitBinding!);

    private static string GetHelperName(Type valueType, ExecutionStructuralLimitBinding binding)
    {
        var key = string.Join(
            "|",
            valueType.AssemblyQualifiedName,
            binding.Origin,
            binding.Path,
            binding.SourceContextId,
            binding.Limits.MaxDepth,
            binding.Limits.MaxNodes,
            binding.Limits.MaxStringBytes);
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return HelperPrefix + Convert.ToHexString(digest.AsSpan(0, 8)).ToLowerInvariant();
    }

    private static MemberDeclarationSyntax CreateMember(
        ExecutionStructuralConversion conversion,
        string helperName)
    {
        var binding = conversion.LimitBinding ??
            throw new InvalidOperationException("A structural limit helper requires binding metadata.");
        var valueType = conversion.TargetType.RequireClrType();
        var descriptor = Musoq.Schema.StructuralInputs.StructuralTypeDescriptor.FromClrType(valueType);
        var valueTypeText = EvaluationHelper.GetCastableType(valueType);
        var lines = new List<string>
        {
            "token.ThrowIfCancellationRequested();",
            "long __musoqStructuralNodes = 0L;",
            "long __musoqStructuralStrings = 0L;",
            "int __musoqStructuralMaxDepth = 0;"
        };

        var state = new EmitterState(lines);
        EmitMeasure(descriptor, valueType, "value", 1, state, "root");

        var sourceContext = binding.SourceContextId == null
            ? "null"
            : Quote(binding.SourceContextId);
        lines.Add(
            $"global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded(" +
            $"{Quote(OriginText(binding.Origin))}, {Quote(binding.Path)}, " +
            "new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(" +
            "__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), " +
            $"{binding.Limits.MaxDepth.ToString(CultureInfo.InvariantCulture)}, " +
            $"{binding.Limits.MaxNodes.ToString(CultureInfo.InvariantCulture)}L, " +
            $"{binding.Limits.MaxStringBytes.ToString(CultureInfo.InvariantCulture)}L, token, {sourceContext});");
        lines.Add("return value;");

        var body = string.Join(Environment.NewLine, lines.Select(static line => "    " + line));
        return SyntaxFactory.ParseMemberDeclaration(
            $"private static {valueTypeText} {helperName}({valueTypeText} value, System.Threading.CancellationToken token)\n" +
            $"{{\n{body}\n}}")!;
    }

    internal static void EmitMeasure(
        Musoq.Schema.StructuralInputs.StructuralTypeDescriptor descriptor,
        Type clrType,
        string expression,
        int depth,
        EmitterState state,
        string path)
    {
        state.AddNode(depth);

        switch (descriptor.Kind)
        {
            case Musoq.Schema.StructuralInputs.StructuralTypeKind.Scalar:
                if (descriptor.ScalarType == typeof(string))
                {
                    state.Lines.Add(
                        $"if ({expression} != null) __musoqStructuralStrings = checked(__musoqStructuralStrings + checked((long){expression}.Length * 2L));");
                }

                return;

            case Musoq.Schema.StructuralInputs.StructuralTypeKind.Record:
                EmitRecord(descriptor, clrType, expression, depth, state, path);
                return;

            case Musoq.Schema.StructuralInputs.StructuralTypeKind.Collection:
                EmitCollection(descriptor, clrType, expression, depth, state, path);
                return;

            default:
                throw new InvalidOperationException($"Unknown structural type '{descriptor.Kind}'.");
        }
    }

    private static void EmitRecord(
        Musoq.Schema.StructuralInputs.StructuralTypeDescriptor descriptor,
        Type clrType,
        string expression,
        int depth,
        EmitterState state,
        string path)
    {
        var nullableValueType = Nullable.GetUnderlyingType(clrType);
        if (nullableValueType != null)
        {
            var bodyExpression = expression + ".Value";
            state.Lines.Add($"if ({expression}.HasValue)");
            state.Lines.Add("{");
            foreach (var field in descriptor.Fields)
                EmitField(descriptor, nullableValueType, bodyExpression, field, depth, state, path);
            state.Lines.Add("}");
            return;
        }

        if (!clrType.IsValueType)
        {
            state.Lines.Add($"if ({expression} != null)");
            state.Lines.Add("{");
        }

        foreach (var field in descriptor.Fields)
            EmitField(descriptor, clrType, expression, field, depth, state, path);

        if (!clrType.IsValueType)
            state.Lines.Add("}");
    }

    private static void EmitField(
        Musoq.Schema.StructuralInputs.StructuralTypeDescriptor descriptor,
        Type clrType,
        string expression,
        Musoq.Schema.StructuralInputs.StructuralFieldDescriptor field,
        int depth,
        EmitterState state,
        string path)
    {
        var memberName = ResolveMemberName(clrType, field.Name);
        var fieldExpression = expression + "." + memberName;
        var fieldType = GetClrType(field.Type);
        EmitMeasure(field.Type, fieldType, fieldExpression, depth + 1, state, path + "." + field.Name);
    }

    private static void EmitCollection(
        Musoq.Schema.StructuralInputs.StructuralTypeDescriptor descriptor,
        Type clrType,
        string expression,
        int depth,
        EmitterState state,
        string path)
    {
        if (!clrType.IsValueType)
        {
            state.Lines.Add($"if ({expression} != null)");
            state.Lines.Add("{");
        }

        var elementDescriptor = descriptor.ElementType ??
            throw new InvalidOperationException("A structural collection element is required.");
        var elementType = GetClrType(elementDescriptor);
        var index = state.NextName("index");
        var collection = state.NextName("collection");
        var count = state.NextName("count");
        var elementExpression = collection + "[" + index + "]";

        if (clrType.IsArray)
        {
            state.Lines.Add($"var {collection} = {expression};");
            state.Lines.Add($"var {count} = {collection}.Length;");
        }
        else
        {
            state.Lines.Add(
                $"var {collection} = (System.Collections.Generic.IReadOnlyList<{EvaluationHelper.GetCastableType(elementType)}>){expression};");
            state.Lines.Add($"var {count} = {collection}.Count;");
        }

        state.Lines.Add(
            $"for (var {index} = 0; {index} < {count}; {index}++)");
        state.Lines.Add("{");
        state.Lines.Add($"if (({index} & 1023) == 0) token.ThrowIfCancellationRequested();");
        EmitMeasure(elementDescriptor, elementType, elementExpression, depth + 1, state, path + "[]");
        state.Lines.Add("}");

        if (!clrType.IsValueType)
            state.Lines.Add("}");
    }

    internal sealed class EmitterState
    {
        private int _nameOrdinal;

        internal EmitterState(List<string> lines)
        {
            Lines = lines;
        }

        internal List<string> Lines { get; }

        internal void AddNode(int depth)
        {
            Lines.Add("__musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);");
            Lines.Add($"if ({depth.ToString(CultureInfo.InvariantCulture)} > __musoqStructuralMaxDepth) __musoqStructuralMaxDepth = {depth.ToString(CultureInfo.InvariantCulture)};");
        }

        internal string NextName(string stem) =>
            "__musoqStructural_" + stem + _nameOrdinal++.ToString(CultureInfo.InvariantCulture);
    }

    internal static string ResolveMemberName(Type type, string fieldName)
    {
        var property = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(property => string.Equals(property.Name, fieldName, StringComparison.OrdinalIgnoreCase));
        if (property != null)
            return property.Name;

        var field = type.GetFields(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(field => string.Equals(field.Name, fieldName, StringComparison.OrdinalIgnoreCase));
        if (field != null)
            return field.Name;

        throw new InvalidOperationException(
            $"Structural receiver '{type.FullName}' has no public member for field '{fieldName}'.");
    }

    internal static Type GetClrType(Musoq.Schema.StructuralInputs.StructuralTypeDescriptor descriptor)
    {
        var type = ScriptParameterSyntaxFactory.ResolveStructuralClrType(descriptor);
        if (descriptor.IsNullable && type.IsValueType && Nullable.GetUnderlyingType(type) == null)
            return typeof(Nullable<>).MakeGenericType(type);
        return type;
    }

    private static string OriginText(ExecutionStructuralInputOrigin origin) =>
        origin.ToString().ToLowerInvariant();

    private static string Quote(string value) =>
        SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(value))
            .ToFullString();
}
