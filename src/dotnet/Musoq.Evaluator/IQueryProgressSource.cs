namespace Musoq.Evaluator;

/// <summary>
///     Optional additive contract for runnables that expose engine-owned row progress.
/// </summary>
public interface IQueryProgressSource
{
    event QueryProgressEventHandler QueryProgress;
}
