using System;

namespace Musoq.Targets.CSharpClr;

internal sealed class ExecutionRenderContext
{
    internal ExecutionRenderContext(
        ExecutionRenderOptions options,
        ExecutionRenderSession session)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Session = session ?? throw new ArgumentNullException(nameof(session));
    }

    internal ExecutionRenderOptions Options { get; }

    internal ExecutionRenderSession Session { get; }
}
