namespace Musoq.Evaluator.Visitors;

/// <summary>One ordered lookahead arm and its complete bound text body.</summary>
public sealed record BoundTextSwitchArm(string? Pattern, BoundTextField Body);
