using Musoq.Service.Core.Models;
using System;
using System.Collections.Concurrent;

namespace Musoq.Service.Core
{
    public class ExecutionStateDictionary : ConcurrentDictionary<Guid, ExecutionState> { }
}
