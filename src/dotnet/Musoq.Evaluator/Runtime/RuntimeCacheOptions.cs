namespace Musoq.Evaluator.Runtime;

internal static class RuntimeCacheOptions
{
    public const int PatternCacheSize = 512;

    public const int DynamicAccessorCacheSize = 512;

    public const int XmlDocumentationCacheSize = 128;

    public const int MetadataReferenceCacheSize = 256;

    public const int TypeHintAttributeCacheSize = 512;

    public const int CastableTypeCacheSize = 512;

    public const int ObjectChunkAdapterCacheSize = 512;

    public const int InMemorySourceShapeCacheSize = 512;

    public const int HasIndexerCacheSize = 512;

    public const int IsIndexableCacheSize = 512;

    public static readonly TimeSpan DefaultRegexTimeout = TimeSpan.FromMilliseconds(250);
}
