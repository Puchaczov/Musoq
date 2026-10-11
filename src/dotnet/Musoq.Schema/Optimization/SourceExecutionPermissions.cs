namespace Musoq.Schema.Optimization;

/// <summary>Explicit scheduling permissions declared by a datasource.</summary>
[Flags]
public enum SourceExecutionPermissions
{
    /// <summary>Requires serial source access and row processing.</summary>
    None = 0,
    /// <summary>Allows independent source access calls to overlap.</summary>
    ConcurrentAccess = 1,
    /// <summary>Allows already supplied rows to be read and processed concurrently.</summary>
    ConcurrentRowProcessing = 2
}
