using System.Collections.Generic;

namespace Musoq.Evaluator.Runtime;

public interface IQueryRows<out T> : IEnumerable<T>
{
}
