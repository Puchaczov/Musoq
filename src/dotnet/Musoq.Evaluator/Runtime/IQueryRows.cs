using System.Collections.Generic;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Runtime;

public interface IQueryRows<out T> : IEnumerable<T>
{
}
