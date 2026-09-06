using System;

namespace Musoq.Converter.Build;

public partial class BuildItems
{
    internal SemanticTemplateCachePublication? SemanticCachePublication { get; set; }

    internal IDisposable? SemanticCacheFlight { get; set; }

    internal bool RetainSemanticCacheState { get; set; }
}
