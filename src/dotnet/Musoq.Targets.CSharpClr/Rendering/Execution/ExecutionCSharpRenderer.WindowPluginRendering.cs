using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private List<StatementSyntax> RenderComputePluginWindow(ExecutionComputePluginWindow window)
    {
        if (window.LibraryGet is null || window.ContextBuffers is null)
            throw new InvalidOperationException("Window execution requires a validated typed operation and concrete context buffers; legacy factories are unsupported.");
        return RenderLibraryWindowContext(window);
    }
}
