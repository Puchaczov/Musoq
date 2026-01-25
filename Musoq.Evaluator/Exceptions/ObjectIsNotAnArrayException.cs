using System;

namespace Musoq.Evaluator.Exceptions;

public class ObjectIsNotAnArrayException(string message) : Exception(message);
