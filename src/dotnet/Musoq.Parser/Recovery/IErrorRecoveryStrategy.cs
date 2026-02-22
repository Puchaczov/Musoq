namespace Musoq.Parser.Recovery;

/// <summary>
///     Interface for implementing error recovery strategies in the parser.
/// </summary>
public interface IErrorRecoveryStrategy
{
    /// <summary>
    ///     Gets the name of this recovery strategy.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Gets the priority of this strategy (higher = tried first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    ///     Attempts to recover from a parsing error.
    /// </summary>
    /// <param name="context">The recovery context containing parser state.</param>
    /// <returns>True if recovery was successful, false otherwise.</returns>
    bool TryRecover(RecoveryContext context);
}
