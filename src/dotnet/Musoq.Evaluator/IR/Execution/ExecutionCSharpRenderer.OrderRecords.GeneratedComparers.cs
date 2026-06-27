using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static bool CanUseGeneratedRowTopOffset(ExecutionTopOffsetTable topOffset, GeneratedRowShape rowShape)
    {
        return CanUseGeneratedRowOrderComparer(topOffset.Keys, rowShape);
    }

    private static bool CanUseGeneratedRowOrderComparer(
        IReadOnlyList<ExecutionOrderField> keys,
        GeneratedRowShape rowShape)
    {
        return keys.Count > 0 &&
               keys.All(key => key.OutputIndex >= 0 && key.OutputIndex < rowShape.Fields.Count);
    }

    private static MemberDeclarationSyntax CreateGeneratedRowOrderComparerClass(
        GeneratedRowShape rowShape,
        IReadOnlyList<ExecutionOrderField> keys)
    {
        var typeName = CreateGeneratedRowOrderComparerTypeName(rowShape, keys);
        var body = new List<string>
        {
            $"private sealed class {typeName} : IComparer<{rowShape.TypeName}>",
            "{",
            $"    public static readonly {typeName} Instance = new {typeName}();",
            string.Empty,
            $"    public int Compare({rowShape.TypeName} left, {rowShape.TypeName} right)",
            "    {"
        };

        for (var index = 0; index < keys.Count; index++)
        {
            var key = keys[index];
            var field = rowShape.Fields[key.OutputIndex];
            AddOrderRecordComparisonStatements(body, index, key, field);
        }

        body.Add("        return 0;");
        body.Add("    }");
        body.Add("}");

        return SyntaxFactory.ParseMemberDeclaration(string.Join(Environment.NewLine, body))!;
    }

    private static string CreateGeneratedRowOrderComparerTypeName(
        GeneratedRowShape rowShape,
        IReadOnlyList<ExecutionOrderField> keys)
    {
        var suffix = string.Join(
            "_",
            keys.Select(key =>
                $"{key.OutputIndex.ToString(CultureInfo.InvariantCulture)}{(key.Descending ? "D" : "A")}{FormatNullOrderingSuffix(key.NullOrdering)}"));

        return CreateIdentifierCandidate($"{rowShape.TypeName}OrderBy_{suffix}Comparer", 0);
    }
}
