using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;

namespace Musoq.Evaluator.Runtime;

internal interface ICSharpCompilationFactory
{
    IReadOnlySet<string> PreloadedAssemblyPaths { get; }

    AdhocWorkspace Workspace { get; }

    SyntaxGenerator Generator { get; }

    CSharpCompilation CreateCompilation(string assemblyName);
}
