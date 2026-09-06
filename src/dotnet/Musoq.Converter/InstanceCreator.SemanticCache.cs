using System;
using System.Threading;
using Musoq.Converter.Build;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    private static void CompleteSemanticCache(
        BuildItems items,
        bool commit,
        CancellationToken? cancellationToken = null)
    {
        ArgumentNullException.ThrowIfNull(items);

        try
        {
            if (commit && items.SemanticCachePublication is { } publication)
                SemanticTemplateCache.Commit(publication, cancellationToken ?? items.CancellationToken);
            else if (!commit)
                SemanticTemplateCache.Discard(items.SemanticCachePublication);
        }
        finally
        {
            items.SemanticCachePublication = null;
            var semanticCacheFlight = items.SemanticCacheFlight;
            items.SemanticCacheFlight = null;
            semanticCacheFlight?.Dispose();
        }
    }
}
