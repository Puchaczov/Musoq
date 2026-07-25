using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Logical;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator;

/// <summary>
///     Provides LSP-friendly query analysis that collects diagnostics instead of throwing exceptions.
///     This is the main entry point for language server functionality.
/// </summary>
public sealed class QueryAnalyzer
{
    private readonly CompilationOptions? _compilationOptions;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ISchemaProvider _schemaProvider;

    /// <summary>
    ///     Creates a new QueryAnalyzer with the specified schema provider.
    /// </summary>
    /// <param name="schemaProvider">Provider for schema definitions.</param>
    /// <param name="loggerFactory">Optional logger factory for internal logging.</param>
    /// <param name="compilationOptions">Optional compilation options.</param>
    public QueryAnalyzer(
        ISchemaProvider schemaProvider,
        ILoggerFactory? loggerFactory = null,
        CompilationOptions? compilationOptions = null)
    {
        _schemaProvider = schemaProvider ?? throw new ArgumentNullException(nameof(schemaProvider));
        _loggerFactory = loggerFactory;
        _compilationOptions = compilationOptions;
    }

    /// <summary>
    ///     Analyzes a SQL query and returns all diagnostics.
    ///     Unlike compilation methods, this will not throw on errors - all issues are collected as diagnostics.
    /// </summary>
    /// <param name="query">The SQL query text to analyze.</param>
    /// <returns>Analysis result containing AST and diagnostics.</returns>
    public QueryAnalysisResult Analyze(string query)
    {
        var sourceText = new SourceText(query);
        var diagnosticBag = new DiagnosticBag { SourceText = sourceText };


        RootNode? rootNode = null;
        RootNode? parsedQueryTree = null;
        try
        {
            var lexer = new Lexer(query, true);
            var parser = new Musoq.Parser.Parser(lexer, diagnosticBag);
            var parseResult = parser.ParseWithDiagnostics();

            rootNode = parseResult.Root;
            parsedQueryTree = rootNode;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SchemaProviderFailureException ex)
        {
            RethrowProviderFailure(ex);
        }
        catch (ParseException ex)
        {
            AddParseDiagnostics(diagnosticBag, ex, sourceText);
        }
        catch (Exception ex) when (EvaluatorExceptionTaxonomy.IsExpectedQueryFailure(ex))
        {
            diagnosticBag.AddError(ex, sourceText);
        }

        if (rootNode == null)
            return new QueryAnalysisResult
            {
                Root = null,
                Diagnostics = diagnosticBag.ToSortedList()
            };

        try
        {
            RecursiveCtePrevalidator.Validate(rootNode);
            rootNode = new PreLogicalNormalizer().Normalize(rootNode).NormalizedRoot;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SchemaProviderFailureException ex)
        {
            RethrowProviderFailure(ex);
        }
        catch (Exception ex) when (EvaluatorExceptionTaxonomy.IsExpectedQueryFailure(ex))
        {
            diagnosticBag.AddError(ex, sourceText);
        }

        if (diagnosticBag.HasErrors)
            return new QueryAnalysisResult
            {
                Root = rootNode,
                Diagnostics = diagnosticBag.ToSortedList()
            };

        var diagnosticContext = new DiagnosticContext(sourceText);
        SemanticMetadataSnapshot? metadataSnapshot = null;
        SemanticScopeArtifact? scopeArtifact = null;
        var normalizedQueryTree = rootNode;

        try
        {
            var logger = _loggerFactory?.CreateLogger<BuildMetadataAndInferTypesVisitor>()
                         ?? new NullLogger<BuildMetadataAndInferTypesVisitor>();

            var metadataVisitor = new BuildMetadataAndInferTypesVisitor(
                _schemaProvider,
                new Dictionary<string, string[]>(),
                logger,
                diagnosticContext,
                _compilationOptions);

            var traverseVisitor = new BuildMetadataAndInferTypesTraverseVisitor(metadataVisitor);
            rootNode.Accept(traverseVisitor);


            if (metadataVisitor.Root is { } typedRoot)
            {
                rootNode = typedRoot;
                metadataSnapshot = metadataVisitor.CreateSemanticMetadataSnapshot();
                scopeArtifact = SemanticScopeArtifact.Capture(traverseVisitor.Scope);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SchemaProviderFailureException ex)
        {
            RethrowProviderFailure(ex);
        }
        catch (Exception ex) when (EvaluatorExceptionTaxonomy.IsExpectedQueryFailure(ex))
        {
            diagnosticContext.ReportException(ex);
        }


        var allDiagnostics = diagnosticBag.ToSortedList().ToList();
        allDiagnostics.AddRange(diagnosticContext.Diagnostics);

        var semanticArtifacts = metadataSnapshot is { } snapshot &&
                                parsedQueryTree is { } parsed &&
                                normalizedQueryTree is { } normalized &&
                                rootNode is { } metadataRoot &&
                                scopeArtifact is { } scope
            ? new SemanticPhaseArtifacts
            {
                ParsedQuery = parsed,
                NormalizedQuery = normalized,
                MetadataQuery = metadataRoot,
                Metadata = snapshot,
                Scope = scope,
                Diagnostics = allDiagnostics.ToArray()
            }
            : null;

        return new QueryAnalysisResult
        {
            Root = rootNode,
            Diagnostics = allDiagnostics,
            SemanticArtifacts = semanticArtifacts
        };
    }

    /// <summary>
    ///     Performs quick syntax validation only (no semantic analysis).
    ///     Use this for real-time validation as the user types.
    /// </summary>
    /// <param name="query">The SQL query text to validate.</param>
    /// <returns>Analysis result containing parse errors only.</returns>
    public QueryAnalysisResult ValidateSyntax(string query)
    {
        var sourceText = new SourceText(query);
        var diagnosticBag = new DiagnosticBag { SourceText = sourceText };

        RootNode? rootNode = null;
        try
        {
            var lexer = new Lexer(query, true);
            var parser = new Musoq.Parser.Parser(lexer, diagnosticBag);
            var parseResult = parser.ParseWithDiagnostics();
            rootNode = parseResult.Root;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SchemaProviderFailureException ex)
        {
            RethrowProviderFailure(ex);
        }
        catch (ParseException ex)
        {
            AddParseDiagnostics(diagnosticBag, ex, sourceText);
        }
        catch (Exception ex) when (EvaluatorExceptionTaxonomy.IsExpectedQueryFailure(ex))
        {
            diagnosticBag.AddError(ex, sourceText);
        }

        return new QueryAnalysisResult
        {
            Root = rootNode,
            Diagnostics = diagnosticBag.ToSortedList()
        };
    }

    private static void AddParseDiagnostics(DiagnosticBag diagnosticBag, ParseException exception, SourceText sourceText)
    {
        if (exception.Diagnostics.Count > 0)
        {
            diagnosticBag.AddRange(exception.Diagnostics);
            return;
        }

        diagnosticBag.AddError(exception, sourceText);
    }

    private static void RethrowProviderFailure(SchemaProviderFailureException exception)
    {
        ExceptionDispatchInfo.Capture(exception.InnerException ?? exception).Throw();
        throw new InvalidOperationException("Schema provider failure rethrow did not propagate.");
    }
}
