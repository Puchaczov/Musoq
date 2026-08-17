using System.Diagnostics.CodeAnalysis;
namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    [DoesNotReturn]
    public static bool ThrowScalarSubqueryCardinalityViolation()
    {
        throw new InvalidOperationException("Scalar subquery returned more than one row.");
    }
}
