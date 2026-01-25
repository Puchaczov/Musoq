using System;

namespace Musoq.Evaluator.Exceptions;

public class UnknownPropertyException(string message) : Exception(message);
