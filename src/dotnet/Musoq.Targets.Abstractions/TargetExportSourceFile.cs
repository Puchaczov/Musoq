using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.Abstractions;

internal sealed record TargetExportSourceFile
{
    public TargetExportSourceFile(string path, string language, string content)
    {
        Path = TargetArtifactPath.Normalize(path, nameof(path));
        Language = string.IsNullOrWhiteSpace(language)
            ? throw new ArgumentException("Source language cannot be empty.", nameof(language))
            : language;
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    public string Path { get; }

    public string Language { get; }

    public string Content { get; }
}

internal static class TargetArtifactPath
{
    public static string Normalize(string path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Artifact path cannot be empty.", parameterName);

        var normalized = path.Replace('\\', '/');
        if (normalized.StartsWith("/", StringComparison.Ordinal) ||
            normalized.Contains(':', StringComparison.Ordinal))
        {
            throw new ArgumentException("Artifact path must be relative.", parameterName);
        }

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(static segment => segment is "." or ".."))
            throw new ArgumentException("Artifact path cannot contain current or parent directory segments.", parameterName);

        return string.Join('/', segments);
    }

    public static void RequireUnique<T>(
        IEnumerable<T> values,
        Func<T, string> pathSelector,
        string label)
    {
        var duplicate = values
            .GroupBy(pathSelector, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicate != null)
            throw new ArgumentException($"{label} path '{duplicate.Key}' is duplicated.", label);
    }
}
