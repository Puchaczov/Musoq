using System;

namespace Musoq.Evaluator.Exceptions;

public class UnknownColumnOrAliasException(string message) : Exception(message);