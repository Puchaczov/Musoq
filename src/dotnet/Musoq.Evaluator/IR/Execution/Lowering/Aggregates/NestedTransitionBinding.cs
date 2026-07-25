namespace Musoq.Evaluator.IR.Execution.Lowering.Aggregates;

internal readonly record struct NestedTransitionBinding(
    FieldBinding Binding,
    string PropertyPath);
