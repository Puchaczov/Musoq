using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser;

/// <summary>
///     Exception thrown when parsing fails and ThrowIfErrors is called.
/// </summary>
public sealed class ParseException : Exception
{
    /// <summary>
    ///     Creates a new parse exception.
    /// </summary>
    public ParseException(string message, IEnumerable<Diagnostic> diagnostics)
        : base(message)
    {
        Diagnostics = diagnostics.ToList();
    }

    /// <summary>
    ///     Gets the diagnostics that caused the exception.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
}
