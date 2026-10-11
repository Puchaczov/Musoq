using System.Text.RegularExpressions;

namespace Musoq.Evaluator.Exceptions;

/// <summary>Reports a pattern predicate that exhausted its finite match budget.</summary>
internal sealed class RegexExecutionTimeoutException : TimeoutException
{
    internal RegexExecutionTimeoutException(string patternOperator, string engine, RegexMatchTimeoutException cause)
        : base("The pattern predicate exceeded its regex match timeout.", cause)
    {
        PatternOperator = patternOperator;
        Engine = engine;
        MatchTimeout = cause.MatchTimeout;
    }

    internal string PatternOperator { get; }
    internal string Engine { get; }
    internal TimeSpan MatchTimeout { get; }
}
