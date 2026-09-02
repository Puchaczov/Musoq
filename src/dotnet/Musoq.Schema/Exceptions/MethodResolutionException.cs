using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Schema.Exceptions;

/// <summary>
///     Exception thrown when method resolution fails in schema operations.
///     Provides detailed information about the failed method resolution including available alternatives.
/// </summary>
public class MethodResolutionException : InvalidOperationException, IDiagnosticException
{
    public MethodResolutionException(
        string methodName,
        string[] providedParameterTypes,
        string[] availableSignatures,
        string message)
        : this(methodName, providedParameterTypes, availableSignatures, message, "no-matching-overload")
    {
    }

    public MethodResolutionException(
        string methodName,
        string[] providedParameterTypes,
        string[] availableSignatures,
        string message,
        string resolutionReason)
        : base(message)
    {
        MethodName = methodName;
        ProvidedParameterTypes = providedParameterTypes;
        AvailableSignatures = availableSignatures;
        ResolutionReason = resolutionReason;
    }

    public MethodResolutionException(string message, Exception innerException)
        : base(message, innerException)
    {
        MethodName = string.Empty;
        ProvidedParameterTypes = [];
        AvailableSignatures = [];
        ResolutionReason = "unknown";
    }

    public MethodResolutionException(string message)
        : base(message)
    {
        MethodName = string.Empty;
        ProvidedParameterTypes = [];
        AvailableSignatures = [];
        ResolutionReason = "unknown";
    }

    public MethodResolutionException()
    {
        MethodName = string.Empty;
        ProvidedParameterTypes = [];
        AvailableSignatures = [];
        ResolutionReason = "unknown";
    }

    public string MethodName { get; }
    public string[] ProvidedParameterTypes { get; }
    public string[] AvailableSignatures { get; }

    public string ResolutionReason { get; }

    /// <summary>Gets the precise callable-resolution diagnostic for this failure.</summary>
    public DiagnosticCode Code => ResolutionReason switch
    {
        "unknown-callable" => DiagnosticCode.MQ3086_UnknownCallable,
        "invalid-arity" => DiagnosticCode.MQ3087_InvalidCallableArity,
        "ambiguous-overload" => DiagnosticCode.MQ3089_AmbiguousCallableOverload,
        _ => DiagnosticCode.MQ3088_NoMatchingCallableOverload
    };

    /// <inheritdoc />
    public TextSpan? Span => null;

    /// <inheritdoc />
    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return Diagnostic.ErrorUnknownLocation(Code, Message)
            .WithArgument("methodName", MethodName)
            .WithArgument("providedTypes", string.Join(", ", ProvidedParameterTypes))
            .WithArgument("candidateSignatures", string.Join("; ", AvailableSignatures))
            .WithArgument("resolutionReason", ResolutionReason);
    }

    public static MethodResolutionException ForUnresolvedMethod(
        string methodName,
        string[] providedParameterTypes,
        string[] availableSignatures)
    {
        return ForUnresolvedMethod(
            methodName,
            providedParameterTypes,
            availableSignatures,
            "no-matching-overload");
    }

    public static MethodResolutionException ForUnresolvedMethod(
        string methodName,
        string[] providedParameterTypes,
        string[] availableSignatures,
        string resolutionReason)
    {
        ArgumentNullException.ThrowIfNull(providedParameterTypes);
        ArgumentNullException.ThrowIfNull(availableSignatures);
        var providedParams = providedParameterTypes.Length == 0
            ? "no parameters"
            : string.Join(", ", providedParameterTypes);
        var availableOptions = availableSignatures.Length == 0
            ? "No methods available with this name."
            : $"Available method signatures: {string.Join("; ", availableSignatures)}";

        var message = $"Cannot resolve method '{methodName}' with parameters ({providedParams}). " +
                      $"{availableOptions} " +
                      "Please check the method name and parameter types.";

        return new MethodResolutionException(
            methodName,
            providedParameterTypes,
            availableSignatures,
            message,
            resolutionReason);
    }

    public static MethodResolutionException ForAmbiguousMethod(
        string methodName,
        string[] providedParameterTypes,
        string[] matchingSignatures)
    {
        var providedParams = string.Join(", ", providedParameterTypes);
        var matches = string.Join("; ", matchingSignatures);

        var message = $"The method call '{methodName}({providedParams})' is ambiguous. " +
                      $"Multiple method signatures match: {matches}. " +
                      "Please provide more specific parameter types to resolve the ambiguity.";

        return new MethodResolutionException(
            methodName,
            providedParameterTypes,
            matchingSignatures,
            message,
            "ambiguous-overload");
    }
}
