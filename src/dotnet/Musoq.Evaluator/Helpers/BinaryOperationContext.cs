namespace Musoq.Evaluator.Helpers;

/// <summary>
///     Describes the semantic context of a binary operation for type conversion decisions.
/// </summary>
public enum BinaryOperationContext
{
    /// <summary>Standard operations like equality/inequality that use string-or-object conversion.</summary>
    Standard,

    /// <summary>Relational comparisons (>, <, >=, <=) that use comparison-mode conversion.</summary>
    RelationalComparison,

    /// <summary>Arithmetic operations (+, -, *, /, %, bitwise) that use numeric-only conversion for object types.</summary>
    ArithmeticOperation
}
