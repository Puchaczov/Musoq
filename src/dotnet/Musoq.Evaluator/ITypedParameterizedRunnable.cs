namespace Musoq.Evaluator;

/// <summary>Receives a captured execution's typed parameter slots.</summary>
public interface ITypedParameterizedRunnable
{
    /// <summary>Gets or sets the immutable slots owned by the current execution.</summary>
    TypedParameterSnapshot TypedParameters { get; set; }
}
