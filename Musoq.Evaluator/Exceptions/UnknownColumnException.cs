using System;

namespace Musoq.Evaluator.Exceptions;

public class UnknownColumnException(string message) : Exception(message);