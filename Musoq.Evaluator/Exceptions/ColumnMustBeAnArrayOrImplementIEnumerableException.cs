using System;

namespace Musoq.Evaluator.Exceptions;

public class ColumnMustBeAnArrayOrImplementIEnumerableException()
    : Exception("Column must be an array or implement IEnumerable<T> interface");