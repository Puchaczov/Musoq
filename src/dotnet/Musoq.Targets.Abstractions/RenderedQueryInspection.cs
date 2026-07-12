using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Musoq.Targets.Abstractions;

internal sealed record RenderedQueryInspection
{
    public RenderedQueryInspection(
        ExecutionTargetId targetId,
        string? generatedCSharpCode,
        IReadOnlyDictionary<string, string>? sourceMetadata)
    {
        TargetId = targetId;
        GeneratedCSharpCode = generatedCSharpCode;
        SourceMetadata = FreezeDictionary(sourceMetadata);
    }

    public ExecutionTargetId TargetId { get; }

    public string? GeneratedCSharpCode { get; }

    public IReadOnlyDictionary<string, string> SourceMetadata { get; }

    private static IReadOnlyDictionary<string, string> FreezeDictionary(
        IReadOnlyDictionary<string, string>? values)
    {
        return new ReadOnlyDictionary<string, string>(
            values is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(values, StringComparer.Ordinal));
    }
}
