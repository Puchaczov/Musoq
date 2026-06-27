using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed record MultiStatementIndexes(
        IReadOnlyDictionary<string, int> CteIndexes,
        IReadOnlyDictionary<string, int> ProducerIndexByName,
        Dictionary<string, GeneratedRowShape> CteShapesByName,
        string? StatementNamePrefix);

    private sealed record ParallelCteLevel(
        int Level,
        IReadOnlyList<PhysicalCteDefinition> Definitions);

    private sealed record CteDefinitionPrefixBuildResult(
        bool Supported,
        IReadOnlyList<RowShape> Shapes,
        IReadOnlyList<ExecutionNode> Nodes,
        string UnsupportedReason)
    {
        public static CteDefinitionPrefixBuildResult Success(
            IReadOnlyList<RowShape> shapes,
            IReadOnlyList<ExecutionNode> nodes)
        {
            return new CteDefinitionPrefixBuildResult(true, shapes, nodes, string.Empty);
        }

        public static CteDefinitionPrefixBuildResult Unsupported(string reason)
        {
            return new CteDefinitionPrefixBuildResult(false, [], [], reason);
        }
    }

    private sealed record CteDefinitionPruningPlan(
        IReadOnlyDictionary<string, IReadOnlySet<string>> RequiredColumnsByName,
        IReadOnlySet<string> ContextFreeDefinitions)
    {
        public static CteDefinitionPruningPlan Empty { get; } = new(
            new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        public bool TryGetRequiredColumns(string definitionName, out IReadOnlySet<string> columns)
        {
            return RequiredColumnsByName.TryGetValue(definitionName, out columns!);
        }

        public bool CanDropContexts(string definitionName)
        {
            return ContextFreeDefinitions.Contains(definitionName);
        }
    }

    private sealed record InterpretSourceValidationResult(
        bool Supported,
        string UnsupportedReason)
    {
        public static InterpretSourceValidationResult Success()
        {
            return new InterpretSourceValidationResult(true, string.Empty);
        }

        public static InterpretSourceValidationResult Unsupported(string reason)
        {
            return new InterpretSourceValidationResult(false, reason);
        }
    }

    private sealed record SourceBuildResult(
        bool Supported,
        JoinSource Source,
        string UnsupportedReason)
    {
        public static SourceBuildResult Success(JoinSource source)
        {
            return new SourceBuildResult(true, source, string.Empty);
        }

        public static SourceBuildResult Unsupported(string reason)
        {
            return new SourceBuildResult(
                false,
                new JoinSource(
                    new PhysicalMultiStatementNode([]),
                    new GeneratedRowShape(string.Empty, []),
                    new ExecutionVariable(string.Empty, typeof(object)),
                    [],
                    new ExecutionVariableRead(new ExecutionVariable(string.Empty, typeof(object))),
                    [],
                    0),
                reason);
        }
    }
}
