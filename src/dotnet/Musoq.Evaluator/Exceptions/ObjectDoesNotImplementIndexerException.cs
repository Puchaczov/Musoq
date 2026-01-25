using System;

namespace Musoq.Evaluator.Exceptions;

public class ObjectDoesNotImplementIndexerException(string message) : Exception(message);
