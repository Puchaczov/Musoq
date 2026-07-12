using System;

namespace Musoq.Targets.Abstractions;

internal sealed record TargetExportBinaryBlob
{
    private readonly byte[] _content;

    public TargetExportBinaryBlob(string name, byte[] content, string contentType)
    {
        ArgumentNullException.ThrowIfNull(content);

        Name = TargetArtifactPath.Normalize(name, nameof(name));
        _content = (byte[])content.Clone();
        ContentType = string.IsNullOrWhiteSpace(contentType) || !contentType.Contains('/', StringComparison.Ordinal)
            ? throw new ArgumentException("Binary content type must be a MIME type.", nameof(contentType))
            : contentType;
    }

    public string Name { get; }

    public byte[] Content => (byte[])_content.Clone();

    public string ContentType { get; }
}
