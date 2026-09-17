namespace Musoq.Evaluator.IR.Execution;

/// <summary>Prepares an owned typed structural value at an explicit execution lifetime.</summary>
public sealed record ExecutionPrepareStructuralInput(
    ExecutionVariable Target,
    ExecutionExpression Input,
    ExecutionStructuralConstructionPlan Plan) : ExecutionNode
{
    public ExecutionVariable Target { get; } = Target ?? throw new ArgumentNullException(nameof(Target));

    public ExecutionExpression Input { get; init; } = Input ?? throw new ArgumentNullException(nameof(Input));

    public ExecutionStructuralConstructionPlan Plan { get; } = Plan ?? throw new ArgumentNullException(nameof(Plan));
}
