using System;

namespace Musoq.Evaluator.Exceptions;

public class TypeNotFoundException(string message) : Exception(message);
