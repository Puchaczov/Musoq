using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal enum SourceCacheability
{
    CacheCandidate,
    NotCacheable,
    Unknown
}
