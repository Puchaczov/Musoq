using System;

namespace Musoq.Evaluator.Exceptions;

public class SetOperatorMustHaveKeyColumnsException(string setOperator)
    : Exception($"{setOperator} operator must have keys");
