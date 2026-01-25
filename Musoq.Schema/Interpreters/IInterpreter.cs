namespace Musoq.Schema.Interpreters;

/// <summary>
///     Base interface for all interpretation schemas.
///     Interpretation schemas parse raw data (bytes or text) into structured objects.
/// </summary>
/// <typeparam name="TOut">The type of the parsed result object.</typeparam>
public interface IInterpreter<TOut>
{
    /// <summary>
    ///     Gets the name of the schema that this interpreter implements.
    /// </summary>
    string SchemaName { get; }
}
