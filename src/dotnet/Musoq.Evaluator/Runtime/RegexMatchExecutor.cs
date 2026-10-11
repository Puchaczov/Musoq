using System.Text.RegularExpressions;
using Musoq.Evaluator.Exceptions;

namespace Musoq.Evaluator.Runtime;

internal static class RegexMatchExecutor
{
    internal static bool IsMatch(Regex regex, string input, string patternOperator)
    {
        try
        {
            return regex.IsMatch(input);
        }
        catch (RegexMatchTimeoutException cause)
        {
            var engine = (regex.Options & RegexOptions.NonBacktracking) != 0
                ? "NonBacktracking"
                : "Backtracking";
            throw new RegexExecutionTimeoutException(patternOperator, engine, cause);
        }
    }
}
