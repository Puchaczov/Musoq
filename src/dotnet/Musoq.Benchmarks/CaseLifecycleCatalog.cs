using System.Text.Json;

namespace Musoq.Benchmarks;

public sealed record BenchmarkCaseRetirement(string CaseId, string Reason, string? ReplacementCaseId = null);

/// <summary>Frozen scheduling metadata; never part of a workload's measurement identity.</summary>
public sealed class CaseCatalogSnapshot
{
    public CaseCatalogSnapshot(
        int revision,
        IReadOnlyList<string> knownCaseIds,
        IReadOnlyList<BenchmarkCaseRetirement> retiredCases
    )
    {
        ArgumentNullException.ThrowIfNull(knownCaseIds);
        ArgumentNullException.ThrowIfNull(retiredCases);
        if (revision <= 0 || knownCaseIds.Count == 0)
            throw new InvalidDataException("A case catalog requires a positive revision and known cases.");
        var known = knownCaseIds.ToHashSet(StringComparer.Ordinal);
        if (known.Count != knownCaseIds.Count || known.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("Known case IDs must be nonempty and unique.");
        var retired = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in retiredCases)
        {
            if (item is null || !known.Contains(item.CaseId) || !retired.Add(item.CaseId))
                throw new InvalidDataException("Retirements must identify unique known canonical cases.");
            if (string.IsNullOrWhiteSpace(item.Reason))
                throw new InvalidDataException($"Retired case '{item.CaseId}' requires a reason.");
            if (
                item.ReplacementCaseId is { } replacement
                && (!known.Contains(replacement) || replacement == item.CaseId)
            )
                throw new InvalidDataException($"Retired case '{item.CaseId}' has an invalid replacement.");
        }
        Revision = revision;
        KnownCaseIds = Array.AsReadOnly(known.Order(StringComparer.Ordinal).ToArray());
        RetiredCases = Array.AsReadOnly(retiredCases.OrderBy(item => item.CaseId, StringComparer.Ordinal).ToArray());
    }

    public int Revision { get; }
    public IReadOnlyList<string> KnownCaseIds { get; }
    public IReadOnlyList<BenchmarkCaseRetirement> RetiredCases { get; }

    public BenchmarkCaseRetirement? Retirement(string caseId) =>
        RetiredCases.FirstOrDefault(item => item.CaseId == caseId);

    public bool SameAs(CaseCatalogSnapshot? other) =>
        other is not null
        && Revision == other.Revision
        && KnownCaseIds.SequenceEqual(other.KnownCaseIds, StringComparer.Ordinal)
        && RetiredCases.SequenceEqual(other.RetiredCases);

    public void ValidateSelection(IEnumerable<string> selectedIds, bool official)
    {
        if (!official)
            return;
        var active = KnownCaseIds
            .Except(RetiredCases.Select(item => item.CaseId), StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        if (!active.SetEquals(selectedIds))
            throw new InvalidDataException(
                "Official selection must contain every active canonical case and no retired cases."
            );
    }
}

public static class CaseLifecycleCatalog
{
    public const string RelativePath = "benchmarks/case-lifecycle.json";

    public static CaseCatalogSnapshot Load(IEnumerable<string> knownCaseIds) =>
        Read(Path.Combine(ProvenanceCollector.RepositoryRoot(), RelativePath), knownCaseIds);

    public static CaseCatalogSnapshot Read(string path, IEnumerable<string> knownCaseIds)
    {
        var file =
            JsonSerializer.Deserialize<LifecycleFile>(File.ReadAllText(path), ArtifactJson.Options)
            ?? throw new InvalidDataException("Case lifecycle catalog is empty.");
        if (file.RetiredCases is null)
            throw new InvalidDataException("Case lifecycle catalog requires retiredCases.");
        return new(file.Revision, knownCaseIds.ToArray(), file.RetiredCases);
    }

    private sealed record LifecycleFile(int Revision, BenchmarkCaseRetirement[]? RetiredCases);
}

public sealed record CaseListing(CaseDefinition Definition, string Status, string? Reason, string? ReplacementCaseId);
