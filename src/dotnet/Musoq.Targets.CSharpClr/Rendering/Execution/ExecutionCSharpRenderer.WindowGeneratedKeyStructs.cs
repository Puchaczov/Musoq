using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static GeneratedWindowKeyStruct CreateGeneratedWindowKeyStruct(ExecutionWindowKeyArray keyArray)
    {
        var shape = keyArray.Shape ?? throw new InvalidOperationException("Window key shape is required.");
        var typeName = shape.GeneratedElementTypeName ??
                       throw new InvalidOperationException("Generated window key type is required.");
        var parts = shape.GeneratedParts ??
                    throw new InvalidOperationException("Generated window key parts are required.");

        return new GeneratedWindowKeyStruct(typeName, shape.IsGeneratedOrderKey, parts);
    }

    private static MemberDeclarationSyntax CreateGeneratedWindowKeyStruct(GeneratedWindowKeyStruct key)
    {
        var fields = string.Join(
            Environment.NewLine,
            key.Parts.Select((part, index) =>
                $"        private readonly {CreateGeneratedKeyTypeName(part.Type)} _value{index.ToString(CultureInfo.InvariantCulture)};"));
        var constructorParameters = string.Join(
            ", ",
            key.Parts.Select((part, index) =>
                $"{CreateGeneratedKeyTypeName(part.Type)} value{index.ToString(CultureInfo.InvariantCulture)}"));
        var constructorAssignments = string.Join(
            Environment.NewLine,
            key.Parts.Select((_, index) =>
                $"            _value{index.ToString(CultureInfo.InvariantCulture)} = value{index.ToString(CultureInfo.InvariantCulture)};"));
        var compareMethods = key.IsOrderKey
            ? string.Join(Environment.NewLine, key.Parts.Select(CreateGeneratedWindowKeyCompareMethod))
            : string.Empty;
        var compareTo = key.IsOrderKey ? CreateGeneratedWindowKeyCompareTo(key) : string.Empty;
        var peerEquals = key.NeedsPeerEquals ? CreateGeneratedWindowKeyPeerEquals(key) : string.Empty;
        var source =
            $"private readonly struct {key.TypeName} : System.IEquatable<{key.TypeName}>{(key.IsOrderKey ? $", System.IComparable<{key.TypeName}>" : string.Empty)}" + Environment.NewLine +
            "{" + Environment.NewLine +
            fields + Environment.NewLine + Environment.NewLine +
            $"    public {key.TypeName}({constructorParameters})" + Environment.NewLine +
            "    {" + Environment.NewLine +
            constructorAssignments + Environment.NewLine +
            "    }" + Environment.NewLine +
            compareTo +
            peerEquals +
            $"    public bool Equals({key.TypeName} other)" + Environment.NewLine +
            "    {" + Environment.NewLine +
            CreateGeneratedWindowKeyEqualsBody(key) + Environment.NewLine +
            "    }" + Environment.NewLine + Environment.NewLine +
            "    public override bool Equals(object obj)" + Environment.NewLine +
            "    {" + Environment.NewLine +
            $"        return obj is {key.TypeName} other && Equals(other);" + Environment.NewLine +
            "    }" + Environment.NewLine + Environment.NewLine +
            "    public override int GetHashCode()" + Environment.NewLine +
            "    {" + Environment.NewLine +
            CreateGeneratedWindowKeyHashBody(key) + Environment.NewLine +
            "    }" + Environment.NewLine +
            compareMethods +
            "}";

        return SyntaxFactory.ParseMemberDeclaration(source) ??
               throw new InvalidOperationException("Generated window key struct could not be parsed.");
    }

    private static string CreateGeneratedWindowKeyPeerEquals(GeneratedWindowKeyStruct key)
    {
        return Environment.NewLine +
               $"    public bool PeerEquals({key.TypeName} other)" + Environment.NewLine +
               "    {" + Environment.NewLine +
               CreateGeneratedWindowKeyEqualsBody(key) + Environment.NewLine +
               "    }" + Environment.NewLine +
               Environment.NewLine;
    }

    private static string CreateGeneratedWindowKeyCompareTo(GeneratedWindowKeyStruct key)
    {
        var comparisons = string.Join(
            Environment.NewLine,
            key.Parts.Select((_, index) => $$"""

                    var comparison{{index.ToString(CultureInfo.InvariantCulture)}} = CompareValue{{index.ToString(CultureInfo.InvariantCulture)}}(_value{{index.ToString(CultureInfo.InvariantCulture)}}, other._value{{index.ToString(CultureInfo.InvariantCulture)}});
                    if (comparison{{index.ToString(CultureInfo.InvariantCulture)}} != 0)
                        return comparison{{index.ToString(CultureInfo.InvariantCulture)}};
            """));

        return $$"""

                public int CompareTo({{key.TypeName}} other)
                {{{comparisons}}

                    return 0;
                }

            """;
    }

    private static string CreateGeneratedWindowKeyCompareMethod(ExecutionWindowGeneratedKeyPart part, int index)
    {
        var typeName = CreateGeneratedKeyTypeName(part.Type);
        var direction = part.Descending ? "-comparison" : "comparison";
        var compare = part.Type.RequireClrType() == typeof(string)
            ? "System.String.CompareOrdinal(left, right)"
            : Nullable.GetUnderlyingType(part.Type.RequireClrType()) != null
                ? "left.Value.CompareTo(right.Value)"
                : "left.CompareTo(right)";

        if (part.Type.RequireClrType().IsValueType && Nullable.GetUnderlyingType(part.Type.RequireClrType()) == null)
            return CreateGeneratedNonNullableCompareMethod(index, typeName, compare, direction);

        return CreateGeneratedNullableCompareMethod(part, index, typeName, compare, direction);
    }

    private static string CreateGeneratedNonNullableCompareMethod(
        int index,
        string typeName,
        string compare,
        string direction)
    {
        return $$"""

                private static int CompareValue{{index.ToString(CultureInfo.InvariantCulture)}}({{typeName}} left, {{typeName}} right)
                {
                    var comparison = {{compare}};
                    return {{direction}};
                }
            """;
    }

    private static string CreateGeneratedNullableCompareMethod(
        ExecutionWindowGeneratedKeyPart part,
        int index,
        string typeName,
        string compare,
        string direction)
    {
        var (nullLeft, nullRight) = CreateGeneratedNullComparisons(part);

        return $$"""

                private static int CompareValue{{index.ToString(CultureInfo.InvariantCulture)}}({{typeName}} left, {{typeName}} right)
                {
                    if ({{CreateGeneratedKeyNullCheck("left", part.Type)}})
                        return {{CreateGeneratedKeyNullCheck("right", part.Type)}} ? 0 : {{nullLeft}};

                    if ({{CreateGeneratedKeyNullCheck("right", part.Type)}})
                        return {{nullRight}};

                    var comparison = {{compare}};
                    return {{direction}};
                }
            """;
    }

    private static (string Left, string Right) CreateGeneratedNullComparisons(ExecutionWindowGeneratedKeyPart part)
    {
        return part.NullOrdering switch
        {
            NullOrdering.First => ("-1", "1"),
            NullOrdering.Last => ("1", "-1"),
            _ => part.Descending ? ("1", "-1") : ("-1", "1")
        };
    }

    private static string CreateGeneratedWindowKeyEqualsBody(GeneratedWindowKeyStruct key)
    {
        if (key.Parts.Count == 0)
            return "            return true;";

        return "        return " + string.Join(
            " &&" + Environment.NewLine + "               ",
            key.Parts.Select((part, index) =>
                CreateGeneratedWindowKeyEqualsExpression(part.Type, index))) + ";";
    }

    private static string CreateGeneratedWindowKeyEqualsExpression(Type type, int index)
    {
        var valueName = $"_value{index.ToString(CultureInfo.InvariantCulture)}";
        var otherName = $"other._value{index.ToString(CultureInfo.InvariantCulture)}";
        return type == typeof(string)
            ? $"System.String.Equals({valueName}, {otherName}, System.StringComparison.Ordinal)"
            : $"System.Collections.Generic.EqualityComparer<{CreateGeneratedKeyTypeName(type)}>.Default.Equals({valueName}, {otherName})";
    }

    private static string CreateGeneratedWindowKeyEqualsExpression(ExecutionTypeRef type, int index) =>
        CreateGeneratedWindowKeyEqualsExpression(type.RequireClrType(), index);

    private static string CreateGeneratedWindowKeyHashBody(GeneratedWindowKeyStruct key)
    {
        var statements = new List<string> { "            var hash = new System.HashCode();" };
        statements.AddRange(key.Parts.Select((part, index) =>
        {
            var valueName = $"_value{index.ToString(CultureInfo.InvariantCulture)}";
            return part.Type.RequireClrType() == typeof(string)
                ? $"            hash.Add({valueName}, System.StringComparer.Ordinal);"
                : $"            hash.Add({valueName});";
        }));
        statements.Add("            return hash.ToHashCode();");
        return string.Join(Environment.NewLine, statements);
    }

    private static string CreateGeneratedKeyNullCheck(string variableName, Type type)
    {
        return Nullable.GetUnderlyingType(type) != null
            ? $"!{variableName}.HasValue"
            : !type.IsValueType
                ? $"{variableName} == null"
                : "false";
    }

    private static string CreateGeneratedKeyNullCheck(string variableName, ExecutionTypeRef type) =>
        CreateGeneratedKeyNullCheck(variableName, type.RequireClrType());

    private static string CreateGeneratedKeyTypeName(Type type)
    {
        return EvaluationHelper.GetCastableType(type);
    }

    private static string CreateGeneratedKeyTypeName(ExecutionTypeRef type) =>
        CreateGeneratedKeyTypeName(type.RequireClrType());
}
