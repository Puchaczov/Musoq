using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>One string-only, NULL-propagating concatenation, evaluated left to right.</summary>
public sealed record ExecutionStringConcatenation : ExecutionExpression
{
    private IReadOnlyList<ExecutionExpression> _expressions = [];

    public ExecutionStringConcatenation(IReadOnlyList<ExecutionExpression> expressions)
        : base(ExecutionClrBindingFactory.FromClr(typeof(string)))
    {
        ArgumentNullException.ThrowIfNull(expressions);
        if (expressions.Count < 2)
            throw new ArgumentException("Concatenation requires at least two operands.", nameof(expressions));
        Expressions = expressions;
    }

    public IReadOnlyList<ExecutionExpression> Expressions
    {
        get => _expressions;
        init => _expressions = ExecutionIrCollections.Freeze(value.SelectMany(static expression =>
            expression is ExecutionStringConcatenation concatenation ? concatenation.Expressions : [expression]).ToArray());
    }
}
