using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Exceptions;

/// <summary>
///     Exception thrown when query compilation or execution fails.
///     Wraps one or more <see cref="MusoqErrorEnvelope" /> instances
///     providing structured, user-facing error information.
/// </summary>
public sealed class MusoqQueryException : Exception
{
    /// <summary>
    ///     Creates a new MusoqQueryException with a single envelope.
    /// </summary>
    public MusoqQueryException(MusoqErrorEnvelope envelope)
        : base(envelope.Message)
    {
        Envelopes = [envelope];
    }

    /// <summary>
    ///     Creates a new MusoqQueryException with a single envelope and inner exception.
    /// </summary>
    public MusoqQueryException(MusoqErrorEnvelope envelope, Exception innerException)
        : base(envelope.Message, innerException)
    {
        Envelopes = [envelope];
    }

    /// <summary>
    ///     Creates a new MusoqQueryException with multiple envelopes.
    /// </summary>
    public MusoqQueryException(IReadOnlyList<MusoqErrorEnvelope> envelopes)
        : base(BuildMessage(envelopes))
    {
        Envelopes = envelopes ?? throw new ArgumentNullException(nameof(envelopes));
    }

    /// <summary>
    ///     Creates a new MusoqQueryException with multiple envelopes and inner exception.
    /// </summary>
    public MusoqQueryException(IReadOnlyList<MusoqErrorEnvelope> envelopes, Exception innerException)
        : base(BuildMessage(envelopes), innerException)
    {
        Envelopes = envelopes ?? throw new ArgumentNullException(nameof(envelopes));
    }

    /// <summary>
    ///     Gets the error envelopes describing what went wrong.
    /// </summary>
    public IReadOnlyList<MusoqErrorEnvelope> Envelopes { get; }

    /// <summary>
    ///     Gets the primary (first) error envelope.
    /// </summary>
    public MusoqErrorEnvelope PrimaryEnvelope => Envelopes[0];

    /// <summary>
    ///     Formats all envelopes as spec-compliant text.
    /// </summary>
    public string FormatText()
    {
        if (Envelopes.Count == 1)
            return MusoqErrorEnvelopeFormatter.FormatText(Envelopes[0]);

        var sb = new StringBuilder();
        for (var i = 0; i < Envelopes.Count; i++)
        {
            if (i > 0)
            {
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }

            sb.Append(MusoqErrorEnvelopeFormatter.FormatText(Envelopes[i]));
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Formats all envelopes as a JSON array.
    /// </summary>
    public string FormatJson()
    {
        if (Envelopes.Count == 1)
            return MusoqErrorEnvelopeFormatter.FormatJson(Envelopes[0]);

        var sb = new StringBuilder();
        sb.Append('[');
        for (var i = 0; i < Envelopes.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(MusoqErrorEnvelopeFormatter.FormatJson(Envelopes[i]));
        }

        sb.Append(']');
        return sb.ToString();
    }

    private static string BuildMessage(IReadOnlyList<MusoqErrorEnvelope> envelopes)
    {
        if (envelopes == null || envelopes.Count == 0)
            return "Query compilation failed.";

        if (envelopes.Count == 1)
            return envelopes[0].Message;

        return $"{envelopes[0].Message} (+{envelopes.Count - 1} more error{(envelopes.Count > 2 ? "s" : "")})";
    }
}
