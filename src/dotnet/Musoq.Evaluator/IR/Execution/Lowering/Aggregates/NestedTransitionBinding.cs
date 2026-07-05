namespace Musoq.Evaluator.IR.Execution;

internal readonly record struct NestedTransitionBinding(
    FieldBinding Binding,
    string PropertyPath);
