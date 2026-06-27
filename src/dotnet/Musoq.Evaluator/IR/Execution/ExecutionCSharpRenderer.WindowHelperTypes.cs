using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static TypeSyntax CreateWindowHelperItemTypeSyntax(
        ExecutionVariable item,
        string? generatedRowTypeName)
    {
        return CreateVariableTypeSyntax(CreateWindowHelperItem(item, generatedRowTypeName));
    }

    private static TypeSyntax CreateWindowRowsParameterType(
        ExecutionVariable buffer,
        ExecutionVariable item,
        string? generatedRowTypeName)
    {
        return CreateReadOnlyListTypeSyntax(CreateWindowHelperItemTypeSyntax(item, generatedRowTypeName));
    }

    private static ExecutionVariable CreateWindowHelperItem(
        ExecutionVariable item,
        string? generatedRowTypeName)
    {
        return string.IsNullOrWhiteSpace(generatedRowTypeName)
            ? item
            : item with { GeneratedRowTypeName = generatedRowTypeName };
    }

    private static string? ResolveGeneratedRowTypeName(
        ExecutionVariable buffer,
        ExecutionVariable item,
        IReadOnlyDictionary<string, string> materializedRowTypeNames)
    {
        return materializedRowTypeNames.TryGetValue(buffer.Name, out var generatedRowTypeName)
            ? generatedRowTypeName
            : buffer.GeneratedRowTypeName ?? item.GeneratedRowTypeName;
    }

    private static void AddMaterializedRowTypeName(
        ExecutionNode node,
        Dictionary<string, string> materializedRowTypeNames)
    {
        switch (node)
        {
            case ExecutionMaterializeList { GeneratedRowShape: not null } materialize:
                materializedRowTypeNames[materialize.Buffer.Name] = materialize.GeneratedRowShape.TypeName;
                break;
            case ExecutionMaterializeFilteredList { GeneratedRowShape: not null } materialize:
                materializedRowTypeNames[materialize.Buffer.Name] = materialize.GeneratedRowShape.TypeName;
                break;
            case ExecutionMaterializeExpandoList materialize:
                materializedRowTypeNames[materialize.Buffer.Name] = materialize.Shape.TypeName;
                break;
        }
    }
}
