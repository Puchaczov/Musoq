using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Musoq.Evaluator;

internal static class ParameterSnapshot
{
    private static readonly ReadOnlyDictionary<string, object?> Empty =
        new(new Dictionary<string, object?>(StringComparer.Ordinal));

    public static IReadOnlyDictionary<string, object?> EmptyReadOnly => Empty;

    public static IDictionary<string, object?> EmptyDictionary => Empty;

    public static Dictionary<string, object?> CaptureMutableOrEmpty(
        IEnumerable<KeyValuePair<string, object?>>? parameters)
    {
        return parameters == null
            ? new Dictionary<string, object?>(StringComparer.Ordinal)
            : new Dictionary<string, object?>(parameters, StringComparer.Ordinal);
    }

    public static IReadOnlyDictionary<string, object?>? CaptureReadOnlyOrNull(
        IEnumerable<KeyValuePair<string, object?>>? parameters)
    {
        var snapshot = CaptureReadOnlyOrEmpty(parameters);
        return ReferenceEquals(snapshot, Empty) ? null : snapshot;
    }

    public static IReadOnlyDictionary<string, object?> CaptureReadOnlyOrEmpty(
        IEnumerable<KeyValuePair<string, object?>>? parameters)
    {
        if (parameters == null)
            return Empty;

        var snapshot = new Dictionary<string, object?>(parameters, StringComparer.Ordinal);
        return snapshot.Count == 0 ? Empty : new ReadOnlyDictionary<string, object?>(snapshot);
    }

    public static IDictionary<string, object?> CaptureDictionaryOrEmpty(
        IEnumerable<KeyValuePair<string, object?>>? parameters)
    {
        if (parameters == null)
            return Empty;

        var snapshot = new Dictionary<string, object?>(parameters, StringComparer.Ordinal);
        return snapshot.Count == 0 ? Empty : new ReadOnlyDictionary<string, object?>(snapshot);
    }

    public static bool IsEmpty(IReadOnlyDictionary<string, object?> parameters)
    {
        return ReferenceEquals(parameters, Empty) || parameters.Count == 0;
    }
}
