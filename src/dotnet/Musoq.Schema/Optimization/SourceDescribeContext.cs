namespace Musoq.Schema.Optimization;

public sealed record SourceDescribeContext(
    SourceIdentity Identity,
    SourceMetadataContext MetadataContext);
