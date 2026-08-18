namespace Musoq.Schema;

/// <summary>
/// Provides typed reads from one source row while it is being materialized.
/// Implementations may be ref structs and must be kept concrete by the source hot path.
/// </summary>
public interface IQuerySourceFieldReader
{
    T Read<T>(int slot);
}
