using System;

namespace Musoq.Evaluator.Exceptions;

public class UnresolvableMethodException(string message) : Exception(message);