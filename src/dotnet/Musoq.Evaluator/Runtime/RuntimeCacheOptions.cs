namespace Musoq.Evaluator.Runtime;

internal static class RuntimeCacheOptions
{
    public const int PatternCacheSize = 512;

    public const int DynamicAccessorCacheSize = 512;

    public const int XmlDocumentationCacheSize = 128;

    public static readonly TimeSpan DefaultRegexTimeout = TimeSpan.FromMilliseconds(250);
}
