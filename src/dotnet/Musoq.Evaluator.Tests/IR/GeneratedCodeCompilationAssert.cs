using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.Tests.IR;

internal static class GeneratedCodeCompilationAssert
{
    public static void Succeeds(CompilationUnitSyntax compilationUnit)
    {
        ArgumentNullException.ThrowIfNull(compilationUnit);
        RuntimeLibraries.CreateReferences();

        var compilationContext = new CompilationContextManager(
            RoslynSharedFactory.CreateCompilation(Guid.NewGuid().ToString("N")));
        compilationContext.InitializeDefaults();
        compilationContext.InitializeCoreReferences([typeof(GeneratedCodeCompilationAssert).Assembly]);
        compilationContext.AddSyntaxTree(ClassEmitter.CreateSyntaxTreeDirect(compilationUnit));

        var errors = compilationContext.GetCompilation()
            .GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString())
            .ToArray();

        if (errors.Length == 0)
            return;

        Assert.Fail(string.Join(Environment.NewLine, errors));
    }
}