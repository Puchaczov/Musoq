using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution.Facts;

namespace Musoq.Evaluator.IR.Execution;

internal static class GeneratedRowVariableTypes
{
    public static Dictionary<string, HashSet<string>> Collect(ExecutionBlock block,
        IReadOnlyDictionary<int, GeneratedRowShape>? storedShapes = null)
    {
        ArgumentNullException.ThrowIfNull(block);
        var result = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var table in GeneratedRowTableShapes.Collect(block))
            AddVariableType(result, table.Key, table.Value.TypeName);

        foreach (var node in ExecutionIrAnalysis.FlattenNodes(block))
        {
            foreach (var variable in ExecutionNodeFacts.GetDeclaredVariables(node))
            {
                if (!string.IsNullOrWhiteSpace(variable.GeneratedRowTypeName))
                    AddVariableType(result, variable.Name, variable.GeneratedRowTypeName);
            }

            AddGeneratedRowVariableTypeFromNode(result, node, storedShapes);
            AddGeneratedRowItemTypeFromSource(result, node, storedShapes);
        }

        return result;
    }

    private static void AddGeneratedRowVariableTypeFromNode(
        Dictionary<string, HashSet<string>> variableTypeNamesByName,
        ExecutionNode node, IReadOnlyDictionary<int, GeneratedRowShape>? storedShapes)
    {
        switch (node)
        {
            case ExecutionLet let:
                AddGeneratedRowVariableTypeFromSource(variableTypeNamesByName, let.Variable, let.Value, storedShapes);
                break;
            case ExecutionAssign assign:
                AddGeneratedRowVariableTypeFromSource(variableTypeNamesByName, assign.Variable, assign.Value, storedShapes);
                break;
            case ExecutionMaterializeList { GeneratedRowShape: { } shape } materialize:
                AddVariableType(variableTypeNamesByName, materialize.Buffer.Name, shape.TypeName);
                break;
            case ExecutionMaterializeFilteredList { GeneratedRowShape: { } shape } materialize:
                AddVariableType(variableTypeNamesByName, materialize.Buffer.Name, shape.TypeName);
                break;
            case ExecutionHashProbe { GeneratedRowTypeName: { } typeName } hashProbe:
                AddVariableType(variableTypeNamesByName, hashProbe.Matches.Name, typeName);
                break;
        }
    }

    private static void AddGeneratedRowVariableTypeFromSource(
        Dictionary<string, HashSet<string>> variableTypeNamesByName,
        ExecutionVariable variable,
        ExecutionExpression source, IReadOnlyDictionary<int, GeneratedRowShape>? storedShapes)
    {
        if (TryResolveGeneratedRowTypeName(source, storedShapes, out var typeName))
            AddVariableType(variableTypeNamesByName, variable.Name, typeName);
    }

    private static void AddGeneratedRowItemTypeFromSource(
        Dictionary<string, HashSet<string>> variableTypeNamesByName,
        ExecutionNode node, IReadOnlyDictionary<int, GeneratedRowShape>? storedShapes)
    {
        switch (node)
        {
            case ExecutionForEach loop:
                AddGeneratedRowItemTypeFromSource(variableTypeNamesByName, loop.Item, loop.Source, storedShapes);
                break;
            case ExecutionForEachWithOrdinality loop:
                AddGeneratedRowItemTypeFromSource(variableTypeNamesByName, loop.Item, loop.Source, storedShapes);
                break;
            case ExecutionForEachIndexed loop:
                AddGeneratedRowItemTypeFromSource(variableTypeNamesByName, loop.Item, loop.Source);
                break;
            case ExecutionMaterializeFilteredList materialize:
                AddGeneratedRowItemTypeFromSource(variableTypeNamesByName, materialize.Item, materialize.Source, storedShapes);
                break;
        }
    }

    private static void AddGeneratedRowItemTypeFromSource(
        Dictionary<string, HashSet<string>> variableTypeNamesByName,
        ExecutionVariable item,
        ExecutionExpression source, IReadOnlyDictionary<int, GeneratedRowShape>? storedShapes)
    {
        if (TryResolveGeneratedRowTypeName(source, storedShapes, out var typeName))
        {
            AddVariableType(variableTypeNamesByName, item.Name, typeName);
            return;
        }

        var sourceVariable = source switch
        {
            ExecutionVariableRead read => read.Variable,
            ExecutionRowStream stream => stream.Variable,
            _ => null
        };
        if (sourceVariable is not null &&
            variableTypeNamesByName.TryGetValue(sourceVariable.Name, out var typeNames))
        {
            foreach (var sourceTypeName in typeNames)
                AddVariableType(variableTypeNamesByName, item.Name, sourceTypeName);
        }
    }

    private static void AddGeneratedRowItemTypeFromSource(
        Dictionary<string, HashSet<string>> variableTypeNamesByName,
        ExecutionVariable item,
        ExecutionVariable source)
    {
        if (!string.IsNullOrWhiteSpace(source.GeneratedRowTypeName))
        {
            AddVariableType(variableTypeNamesByName, item.Name, source.GeneratedRowTypeName);
            return;
        }

        if (!variableTypeNamesByName.TryGetValue(source.Name, out var typeNames))
            return;

        foreach (var sourceTypeName in typeNames)
            AddVariableType(variableTypeNamesByName, item.Name, sourceTypeName);
    }

    private static bool TryResolveGeneratedRowTypeName(ExecutionExpression source,
        IReadOnlyDictionary<int, GeneratedRowShape>? storedShapes, out string typeName)
    {
        switch (source)
        {
            case ExecutionStoredTableRows { GeneratedRowShape: { } shape }:
                typeName = shape.TypeName;
                return true;
            case ExecutionStoredTableRows stored when storedShapes is not null && storedShapes.TryGetValue(stored.TableIndex, out var storedShape):
                typeName = storedShape.TypeName;
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

    private static void AddVariableType(
        Dictionary<string, HashSet<string>> variableTypeNamesByName,
        string variableName,
        string typeName)
    {
        if (!variableTypeNamesByName.TryGetValue(variableName, out var typeNames))
        {
            typeNames = new HashSet<string>(StringComparer.Ordinal);
            variableTypeNamesByName.Add(variableName, typeNames);
        }

        typeNames.Add(typeName);
    }

}
