namespace Musoq.Plugins;

/// <summary>Identifies the SQL evaluation context of a function declaration.</summary>
public enum SqlFunctionKind
{
    /// <summary>Evaluates a scalar expression.</summary>
    Scalar,
    /// <summary>Evaluates a grouped aggregate.</summary>
    Aggregate,
    /// <summary>Evaluates an OVER expression.</summary>
    Window
}
