using System.Collections.Generic;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Visitors;

internal sealed record BoundTextPattern(
    string Pattern,
    TextPatternModifiers Modifiers,
    IReadOnlyList<string> CaptureGroups
);
