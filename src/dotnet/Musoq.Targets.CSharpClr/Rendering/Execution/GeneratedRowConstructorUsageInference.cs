using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution.Facts;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static Dictionary<string, HashSet<string>> CollectGeneratedRowVariableTypeNames(
        ExecutionBlock block,
        IReadOnlyDictionary<int, TypedStoredTableResult>? typedStoredTableResults = null)
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var node in ExecutionIrAnalysis.FlattenNodes(block))
        {
            foreach (var variable in ExecutionNodeFacts.GetDeclaredVariables(node))
            {
                if (!string.IsNullOrWhiteSpace(variable.GeneratedRowTypeName))
                    AddGeneratedRowVariableTypeName(result, variable.Name, variable.GeneratedRowTypeName);
            }

            AddGeneratedRowVariableTypeFromNode(result, node, typedStoredTableResults);
            AddGeneratedRowItemTypeFromSource(result, node, typedStoredTableResults);
        }

        return result;
    }

    private static void AddGeneratedRowVariableTypeFromNode(
        Dictionary<string, HashSet<string>> variableTypeNamesByName,
        ExecutionNode node,
        IReadOnlyDictionary<int, TypedStoredTableResult>? typedStoredTableResults)
    {
        switch (node)
        {
            case ExecutionLet let:
                AddGeneratedRowVariableTypeFromSource(variableTypeNamesByName, let.Variable, let.Value, typedStoredTableResults);
                break;
            case ExecutionAssign assign:
                AddGeneratedRowVariableTypeFromSource(variableTypeNamesByName, assign.Variable, assign.Value, typedStoredTableResults);
                break;
            case ExecutionMaterializeList { GeneratedRowShape: { } shape } materialize:
                AddGeneratedRowVariableTypeName(variableTypeNamesByName, materialize.Buffer.Name, shape.TypeName);
                break;
            case ExecutionMaterializeFilteredList { GeneratedRowShape: { } shape } materialize:
                AddGeneratedRowVariableTypeName(variableTypeNamesByName, materialize.Buffer.Name, shape.TypeName);
                break;
        }
    }

    private static void AddGeneratedRowVariableTypeFromSource(
        Dictionary<string, HashSet<string>> variableTypeNamesByName,
        ExecutionVariable variable,
        ExecutionExpression source,
        IReadOnlyDictionary<int, TypedStoredTableResult>? typedStoredTableResults)
    {
        if (TryResolveGeneratedRowTypeName(source, typedStoredTableResults, out var typeName))
            AddGeneratedRowVariableTypeName(variableTypeNamesByName, variable.Name, typeName);
    }

    private static void AddGeneratedRowItemTypeFromSource(
        Dictionary<string, HashSet<string>> variableTypeNamesByName,
        ExecutionNode node,
        IReadOnlyDictionary<int, TypedStoredTableResult>? typedStoredTableResults)
    {
        switch (node)
        {
            case ExecutionForEach loop:
                AddGeneratedRowItemTypeFromSource(variableTypeNamesByName, loop.Item, loop.Source, typedStoredTableResults);
                break;
            case ExecutionForEachWithOrdinality loop:
                AddGeneratedRowItemTypeFromSource(variableTypeNamesByName, loop.Item, loop.Source, typedStoredTableResults);
                break;
            case ExecutionForEachIndexed loop:
                AddGeneratedRowItemTypeFromSource(variableTypeNamesByName, loop.Item, loop.Source);
                break;
            case ExecutionMaterializeFilteredList materialize:
                AddGeneratedRowItemTypeFromSource(variableTypeNamesByName, materialize.Item, materialize.Source, typedStoredTableResults);
                break;
        }
    }

    private static void AddGeneratedRowItemTypeFromSource(
        Dictionary<string, HashSet<string>> variableTypeNamesByName,
        ExecutionVariable item,
        ExecutionExpression source,
        IReadOnlyDictionary<int, TypedStoredTableResult>? typedStoredTableResults = null)
    {
        if (TryResolveGeneratedRowTypeName(source, typedStoredTableResults, out var typeName))
        {
            AddGeneratedRowVariableTypeName(variableTypeNamesByName, item.Name, typeName);
            return;
        }

        if (source is ExecutionVariableRead variableRead &&
            variableTypeNamesByName.TryGetValue(variableRead.Variable.Name, out var typeNames))
        {
            foreach (var sourceTypeName in typeNames)
                AddGeneratedRowVariableTypeName(variableTypeNamesByName, item.Name, sourceTypeName);
        }
    }

    private static void AddGeneratedRowItemTypeFromSource(
        Dictionary<string, HashSet<string>> variableTypeNamesByName,
        ExecutionVariable item,
        ExecutionVariable source)
    {
        if (!string.IsNullOrWhiteSpace(source.GeneratedRowTypeName))
        {
            AddGeneratedRowVariableTypeName(variableTypeNamesByName, item.Name, source.GeneratedRowTypeName);
            return;
        }

        if (!variableTypeNamesByName.TryGetValue(source.Name, out var typeNames))
            return;

        foreach (var sourceTypeName in typeNames)
            AddGeneratedRowVariableTypeName(variableTypeNamesByName, item.Name, sourceTypeName);
    }

    private static bool TryResolveGeneratedRowTypeName(
        ExecutionExpression source,
        IReadOnlyDictionary<int, TypedStoredTableResult>? typedStoredTableResults,
        out string typeName)
    {
        switch (source)
        {
            case ExecutionStoredTableRows { GeneratedRowShape: { } shape }:
                typeName = shape.TypeName;
                return true;
            case ExecutionStoredTableRows storedRows when
                typedStoredTableResults != null &&
                typedStoredTableResults.TryGetValue(storedRows.TableIndex, out var typedResult):
                typeName = typedResult.RowShape.TypeName;
                return true;
            case ExecutionVariableRead { Variable.GeneratedRowTypeName: { } generatedRowTypeName }:
                typeName = generatedRowTypeName;
                return true;
            case ExecutionRowStream { Variable.GeneratedRowTypeName: { } generatedRowTypeName }:
                typeName = generatedRowTypeName;
                return true;
            default:
                typeName = string.Empty;
                return false;
        }
    }

    private static void AddGeneratedRowVariableTypeName(
        Dictionary<string, HashSet<string>> variableTypeNamesByName,
        string variableName,
        string typeName)
    {
        if (!variableTypeNamesByName.TryGetValue(variableName, out var typeNames))
        {
            typeNames = [];
            variableTypeNamesByName.Add(variableName, typeNames);
        }

        typeNames.Add(typeName);
    }
}
