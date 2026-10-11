using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator;

public partial class CompilationOptions
{
    /// <summary>Gets the finite elapsed-time budget for each LIKE/RLIKE regex match. Defaults to two seconds.</summary>
    public TimeSpan RegexMatchTimeout { get; private init; } = RuntimeCacheOptions.DefaultRegexTimeout;

    /// <summary>Returns a copy with a finite regex match budget between one millisecond and the .NET maximum.</summary>
    public CompilationOptions WithRegexMatchTimeout(TimeSpan timeout) =>
        Clone(regexMatchTimeout: RegexMatchTimeoutPolicy.Validate(timeout));
}
