using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Musoq.Targets.Abstractions;

namespace Musoq.Evaluator.IR.Execution;

internal static partial class ExecutionOperationCatalog
{
    private static readonly FrozenDictionary<Type, ExecutionOperationId> NodeOperations =
        ExecutionNodeDefinitionCatalog.Definitions
            .ToFrozenDictionary(static definition => definition.NodeType, static definition => definition.OperationId);

    private static readonly FrozenDictionary<Type, ExecutionOperationId> ExpressionOperations =
        CreateExpressionOperations().ToFrozenDictionary(static pair => pair.Key, static pair => pair.Value);

    public static IReadOnlySet<ExecutionOperationId> AllOperationIds { get; } = NodeOperations.Values
        .Concat(ExpressionOperations.Values)
        .ToFrozenSet();

    public static IReadOnlySet<Type> RegisteredNodeTypes { get; } = NodeOperations.Keys.ToFrozenSet();

    public static IReadOnlySet<Type> RegisteredExpressionTypes { get; } = ExpressionOperations.Keys.ToFrozenSet();

    internal static IReadOnlySet<ExecutionOperationId> CSharpClrSupportedOperationIds { get; } =
        NodeOperations
            .Where(static pair => ExecutionNodeDefinitionCatalog.Definitions
                .Single(definition => definition.NodeType == pair.Key)
                .Behavior.TargetCapability == ExecutionNodeTargetCapability.Supported)
            .Select(static pair => pair.Value)
            .Concat(ExpressionOperations.Values)
            .ToFrozenSet();

    public static ExecutionOperationId Resolve(ExecutionNode node) =>
        Resolve(NodeOperations, node.GetType(), "node");

    public static ExecutionOperationId Resolve(ExecutionExpression expression) =>
        Resolve(ExpressionOperations, expression.GetType(), "expression");

    private static ExecutionOperationId Resolve(
        IReadOnlyDictionary<Type, ExecutionOperationId> operations,
        Type concreteType,
        string operationKind)
    {
        return operations.TryGetValue(concreteType, out var operationId)
            ? operationId
            : throw new NotSupportedException(
                $"Execution IR {operationKind} '{concreteType.FullName}' has no registered operation id.");
    }

    private static KeyValuePair<Type, ExecutionOperationId> Operation<TOperation>(string id) =>
        new(typeof(TOperation), new ExecutionOperationId(id));
}
