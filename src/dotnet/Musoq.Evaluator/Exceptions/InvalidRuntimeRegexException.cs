using System.Text.RegularExpressions;

namespace Musoq.Evaluator.Exceptions;

internal sealed class InvalidRuntimeRegexException(RegexParseException innerException)
    : ArgumentException("The runtime RLIKE pattern is invalid.", innerException);
