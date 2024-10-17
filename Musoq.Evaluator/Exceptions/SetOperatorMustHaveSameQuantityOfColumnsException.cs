using System;

namespace Musoq.Evaluator.Exceptions;

public class SetOperatorMustHaveSameQuantityOfColumnsException()
    : Exception("Set operator must have the same quantity of columns in both queries");