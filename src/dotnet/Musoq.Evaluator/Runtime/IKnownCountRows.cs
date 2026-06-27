using System.Collections.Generic;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Runtime;

public interface IKnownCountRows<out T> : IQueryRows<T>
{
    int Count { get; }
}
