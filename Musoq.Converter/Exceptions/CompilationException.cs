using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Musoq.Converter.Exceptions;

/// <summary>
/// Exception thrown when C# code compilation fails during query processing.
/// Provides detailed compilation diagnostics and helpful guidance for resolution.
/// </summary>
public class CompilationException : Exception
{
    public string GeneratedCode { get; }
    public IEnumerable<Diagnostic> CompilationErrors { get; }
    public string QueryContext { get; }

    public CompilationException(string message, string generatedCode = null, IEnumerable<Diagnostic> compilationErrors = null, string queryContext = null)
        : base(message)
    {
        GeneratedCode = generatedCode ?? string.Empty;
        CompilationErrors = compilationErrors ?? Enumerable.Empty<Diagnostic>();
        QueryContext = queryContext ?? string.Empty;
    }

    public CompilationException(string message, Exception innerException, string generatedCode = null, IEnumerable<Diagnostic> compilationErrors = null, string queryContext = null)
        : base(message, innerException)
    {
        GeneratedCode = generatedCode ?? string.Empty;
        CompilationErrors = compilationErrors ?? Enumerable.Empty<Diagnostic>();
        QueryContext = queryContext ?? string.Empty;
    }

    public static CompilationException ForCompilationFailure(IEnumerable<Diagnostic> errors, string generatedCode, string queryContext = "Unknown")
    {
        var errorList = errors.ToList();
        var errorDetails = string.Join("\n", errorList
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => $"- {d.GetMessage()} (Line: {d.Location.GetLineSpan().StartLinePosition.Line + 1})"));

        var message = $"Failed to compile the generated C# code for your SQL query. " +
                     $"Compilation errors:\n{errorDetails}\n\n" +
                     "This usually indicates a problem with the SQL query syntax or unsupported operations. " +
                     "Please check your query for syntax errors, column references, and method calls.";

        return new CompilationException(message, generatedCode, errorList, queryContext);
    }

    public static CompilationException ForAssemblyLoadFailure(Exception innerException, string queryContext = "Unknown")
    {
        var message = "The compiled query assembly could not be loaded. " +
                     "This may be due to missing dependencies or incompatible assembly references. " +
                     "Please ensure all required data source plugins are properly installed.";

        return new CompilationException(message, innerException, queryContext: queryContext);
    }

    public static CompilationException ForTypeResolutionFailure(string typeName, string queryContext = "Unknown")
    {
        var message = $"Could not resolve type '{typeName}' in the compiled assembly. " +
                     "This indicates a problem with code generation or missing references. " +
                     "Please check your query syntax and ensure all required schemas are registered.";

        return new CompilationException(message, queryContext: queryContext);
    }
}