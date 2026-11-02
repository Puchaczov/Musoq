using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Musoq.Parser.Diagnostics;

namespace Musoq.Schema.Diagnostics;

/// <summary>An immutable, explicitly public provider report. Never pass raw SDK exception text or secrets.</summary>
public sealed class DataSourceDiagnostic
{
    /// <summary>Creates a report without optional correction or location details.</summary>
    public DataSourceDiagnostic(string code, string category, DataSourceDiagnosticSeverity severity, string message)
        : this(code, category, message, new DataSourceDiagnosticOptions { Severity = severity })
    {
    }
    public DataSourceDiagnostic(string code, string category, string message, DataSourceDiagnosticOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(options);
        ValidateCode(code);
        ValidateOptions(options);
        Code = code;
        Category = DiagnosticSafety.SanitizeForDisplay(category);
        Severity = options.Severity;
        var details = CopyDetails(options.Details);
        var context = new Diagnostic(DiagnosticCode.MQ7020_ReportedDataSourceFailure, DiagnosticSeverity.Error,
            message, SourceLocation.None, arguments: details, sourceKind: DiagnosticSourceKind.DataSource);
        Message = PublicText(message, context, details);
        SuggestedFixes = Array.AsReadOnly(options.SuggestedFixes.Take(8).Select(fix => PublicText(fix, context, details)).ToArray());
        FailureKind = options.FailureKind;
        if (FailureKind is null && Severity == DataSourceDiagnosticSeverity.Error)
            FailureKind = DataSourceFailureKind.ProviderFailure;
        InputLocation = options.InputLocation;
        ArgumentName = options.ArgumentName is null ? null : DiagnosticSafety.SanitizeForDisplay(options.ArgumentName);
        ArgumentIndex = options.ArgumentIndex;
        CanRetry = options.CanRetry;
        RetryAfter = options.RetryAfter;
        Details = FreezePublicDetails(context, details);
    }

    private static void ValidateCode(string code)
    {
        if (code.Length > 128 || code.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '.' and not '_' and not '-'))
            throw new ArgumentException("Provider codes must contain at most 128 ASCII letters, digits, dots, underscores or hyphens.", nameof(code));
    }

    private static void ValidateOptions(DataSourceDiagnosticOptions options)
    {
        if (options.Severity is not DataSourceDiagnosticSeverity.Warning and not DataSourceDiagnosticSeverity.Error)
            throw new ArgumentOutOfRangeException(nameof(options), "Unknown report severity.");
        if (options.FailureKind.HasValue && (options.FailureKind < DataSourceFailureKind.InvalidInput || options.FailureKind > DataSourceFailureKind.ProviderFailure))
            throw new ArgumentOutOfRangeException(nameof(options), "Unknown failure kind.");
        if (options.ArgumentIndex is < 0) throw new ArgumentOutOfRangeException(nameof(options), "Argument indexes must be zero based.");
        if (options.RetryAfter < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(options), "Retry delays cannot be negative.");
        ArgumentNullException.ThrowIfNull(options.SuggestedFixes);
        ArgumentNullException.ThrowIfNull(options.Details);
    }

    private static Dictionary<string, string> CopyDetails(IReadOnlyDictionary<string, string> details)
    {
        var copy = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in details.Take(16))
            copy[DiagnosticSafety.SanitizeForDisplay(item.Key)] = item.Value;
        return copy;
    }

    private static string PublicText(string text, Diagnostic context, IReadOnlyDictionary<string, string> details)
    {
        var result = DiagnosticSafety.SanitizeText(text, context) ?? string.Empty;
        var publicDetails = DiagnosticSafety.SanitizeArguments(context);
        foreach (var item in details)
            if ((IsPathKey(item.Key) || publicDetails[item.Key] == DiagnosticSafety.RedactedValue) && !string.IsNullOrEmpty(item.Value))
                result = result.Replace(item.Value, DiagnosticSafety.RedactedValue, StringComparison.Ordinal);
        return DiagnosticSafety.SanitizeForDisplay(result);
    }

    private static IReadOnlyDictionary<string, string> FreezePublicDetails(Diagnostic context, IReadOnlyDictionary<string, string> details)
    {
        var safe = new Dictionary<string, string>(DiagnosticSafety.SanitizeArguments(context), StringComparer.Ordinal);
        foreach (var item in details)
            safe[item.Key] = IsPathKey(item.Key) ? DiagnosticSafety.RedactedValue : DiagnosticSafety.SanitizeForDisplay(safe[item.Key]);
        return new ReadOnlyDictionary<string, string>(safe);
    }

    private static bool IsPathKey(string key) => key.EndsWith("path", StringComparison.OrdinalIgnoreCase) || key.Equals("paths", StringComparison.OrdinalIgnoreCase);

    public string Code { get; }
    public string Category { get; }
    public DataSourceDiagnosticSeverity Severity { get; }
    public string Message { get; }
    public IReadOnlyList<string> SuggestedFixes { get; }
    public DataSourceFailureKind? FailureKind { get; }
    public IReadOnlyDictionary<string, string> Details { get; }
    public DataSourceInputLocation? InputLocation { get; }
    public string? ArgumentName { get; }
    public int? ArgumentIndex { get; }
    public bool CanRetry { get; }
    public TimeSpan? RetryAfter { get; }

    /// <summary>Creates the standard Core diagnostic without exposing private exception details.</summary>
    public Diagnostic ToDiagnostic()
    {
        var arguments = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["providerCode"] = Code,
            ["providerCategory"] = Category,
            ["canRetry"] = CanRetry ? "true" : "false"
        };
        if (FailureKind.HasValue) arguments["failureKind"] = FailureKind.Value.ToString();
        if (ArgumentName is not null) arguments["argumentName"] = ArgumentName;
        if (ArgumentIndex.HasValue) arguments["argumentIndex"] = ArgumentIndex.Value.ToString(CultureInfo.InvariantCulture);
        if (RetryAfter.HasValue) arguments["retryAfterMilliseconds"] = RetryAfter.Value.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
        foreach (var item in Details) arguments["detail." + item.Key] = item.Value;
        return new Diagnostic(
            Severity == DataSourceDiagnosticSeverity.Error ? DiagnosticCode.MQ7020_ReportedDataSourceFailure : DiagnosticCode.MQ5027_ReportedDataSourceWarning,
            Severity == DataSourceDiagnosticSeverity.Error ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
            Message, SourceLocation.None,
            suggestedFixes: SuggestedFixes.Select(DiagnosticAction.Suggestion),
            phase: DiagnosticPhase.DataSource, sourceKind: DiagnosticSourceKind.DataSource,
            arguments: arguments).WithInputLocation(InputLocation is null ? null : new DiagnosticInputLocation(
                InputLocation.Line, InputLocation.Column, InputLocation.ByteOffset, InputLocation.CharacterOffset, InputLocation.BytePositionInLine));
    }
}
