namespace Musoq.Schema.Diagnostics;

/// <summary>Provider-independent failure semantics; provider codes and categories remain extensible.</summary>
public enum DataSourceFailureKind
{
    InvalidInput,
    InvalidConfiguration,
    AccessDenied,
    ResourceNotFound,
    RateLimited,
    Unavailable,
    Timeout,
    ProviderFailure
}

/// <summary>Determines whether a provider report interrupts query execution.</summary>
public enum DataSourceDiagnosticSeverity
{
    Warning,
    Error
}
