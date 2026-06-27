using System;

namespace Musoq.Evaluator.IR;

/// <summary>
///     Centralizes construction of <see cref="NotSupportedException"/> instances reported when the
///     Execution IR lowering or C# rendering encounters a shape it does not handle. Routing these
///     through one helper keeps the diagnostic wording consistent across the code generation backend.
/// </summary>
internal static class UnsupportedShape
{
    public static NotSupportedException Of(string subject)
    {
        ArgumentNullException.ThrowIfNull(subject);
        return new NotSupportedException($"{subject} is not supported.");
    }

    public static NotSupportedException Of(string subject, string consumer)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(consumer);
        return new NotSupportedException($"{subject} is not supported by {consumer}.");
    }
}
