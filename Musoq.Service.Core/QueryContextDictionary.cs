using CacheManager.Core;
using Musoq.Evaluator;
using Musoq.Service.Client.Core;
using System;
using System.Collections.Concurrent;

namespace Musoq.Service.Core
{
    public class QueryContextDictionary : ConcurrentDictionary<Guid, QueryContext>
    {
    }
}
