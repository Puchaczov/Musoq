using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Runtime;

/// <summary>Runs tolerant interpretations and optionally reports their stable outcome codes.</summary>
public static class TolerantInterpretation
{
    // This opt-in lets a local host collect counts while keeping its existing Core package references.
    private const string ObserverProperty = "Musoq.InterpretationAttempt.v1";

    /// <summary>Binds an optional observer once before execution and shares it with generated helpers.</summary>
    public static QueryRunContext? Bind(QueryRunContext context)
    {
        context.InterpretationObserver = CaptureObserver();
        return context.QueryProgress is not null || context.InterpretationObserver is not null ? context : null;
    }

    /// <summary>Creates context for token-based generated entry points only when it is needed.</summary>
    public static QueryRunContext? Bind(
        CancellationToken token,
        QueryProgressEventHandler? progress,
        object? sender,
        string queryId
    )
    {
        var observer = CaptureObserver();
        return progress is null && observer is null
            ? null
            : new QueryRunContext(token, queryProgress: progress, sender: sender, queryId: queryId)
            {
                InterpretationObserver = observer,
            };
    }

    /// <summary>Returns the established tolerant binary result.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? TryInterpret<T>(
        IBytesInterpreter<T> interpreter,
        ReadOnlySpan<byte> input,
        QueryRunContext? context,
        string schema
    ) =>
        context?.InterpretationObserver is { } observe
            ? Report(InterpretationStatus.ReadAt(interpreter, input, 0), observe, "TryInterpret", schema)
            : InterpretationStatus.TryInterpret(interpreter, input);

    /// <summary>Returns the established tolerant text result.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? TryParse<T>(
        ITextInterpreter<T> interpreter,
        string? input,
        QueryRunContext? context,
        string schema
    ) =>
        context?.InterpretationObserver is { } observe
            ? Report(InterpretationStatus.ReadAt(interpreter, input.AsSpan(), 0), observe, "TryParse", schema)
            : InterpretationStatus.TryParse(interpreter, input);

    private static Action<string, string, int>? CaptureObserver()
    {
        for (var activity = Activity.Current; activity is not null; activity = activity.Parent)
            if (activity.GetCustomProperty(ObserverProperty) is Action<string, string, int> observe)
                return observe;
        return null;
    }

    private static T? Report<T>(
        ParseAttempt<T> result,
        Action<string, string, int> observe,
        string operation,
        string schema
    )
    {
        observe(operation, schema, (int)(result.Failure?.ErrorCode ?? 0));
        return result.Succeeded ? result.Value : default;
    }
}
