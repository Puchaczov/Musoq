using System;
using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;

namespace Musoq.Converter.Build;

internal sealed record TargetRenderInputBuildContext
{
    public TargetRenderInputBuildContext(
        CompilationOptions compilationOptions,
        QueryResultMode queryResultMode,
        TargetScriptBindingContract scriptBinding,
        TargetReferenceInventory references,
        TargetRenderOptions options,
        TargetRenderInputCompilerState compilerState)
    {
        CompilationOptions = compilationOptions ?? throw new ArgumentNullException(nameof(compilationOptions));
        QueryResultMode = queryResultMode;
        ScriptBinding = scriptBinding ?? TargetScriptBindingContract.Empty;
        References = references ?? TargetReferenceInventory.Empty;
        Options = options ?? TargetRenderOptions.Empty;
        CompilerState = compilerState ?? throw new ArgumentNullException(nameof(compilerState));
    }

    public CompilationOptions CompilationOptions { get; }

    public QueryResultMode QueryResultMode { get; }

    public TargetScriptBindingContract ScriptBinding { get; }

    public TargetReferenceInventory References { get; }

    public TargetRenderOptions Options { get; }

    internal TargetRenderInputCompilerState CompilerState { get; }
}
