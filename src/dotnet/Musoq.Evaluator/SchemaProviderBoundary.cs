using Musoq.Schema.Exceptions;

namespace Musoq.Evaluator;

/// <summary>
/// Keeps failures raised by an extension schema provider distinguishable from
/// query validation failures. Provider code is outside the evaluator's
/// diagnostic contract and must not be classified by exception type alone.
/// </summary>
internal static class SchemaProviderBoundary
{
    public static T Invoke<T>(Func<T> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            return operation();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SchemaProviderFailureException)
        {
            throw;
        }
        catch (SchemaArgumentException)
        {
            throw;
        }
        catch (TableNotFoundException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new SchemaProviderFailureException(exception);
        }
    }
}
