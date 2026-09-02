using System.Collections.Generic;
using System.Text.RegularExpressions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private static readonly Regex ColumnRegex = new(@"\[[^\]]+\]|(\w+)|(\*)", RegexOptions.Compiled);

    private static readonly TokenType[] SetOperators =
        [TokenType.Union, TokenType.UnionAll, TokenType.Except, TokenType.Intersect];

    private static readonly string[] ClauseKeywords =
            ["WHERE", "GROUP", "ORDER", "HAVING", "TAKE", "SKIP", "UNION", "EXCEPT", "INTERSECT", "JOIN", "INNER", "OUTER", "CROSS", "QUALIFY"];

    private const int MinLengthForLargerDistance = 5;
    private const int ShortWordMaxDistance = 1;
    private const int LongWordMaxDistance = 2;

    private readonly DiagnosticBag? _diagnostics;
    private readonly bool _enableRecovery;
    private readonly Stack<HashSet<string>> _fromAliasesStack = new();
    private readonly ILexer _lexer;

    private int _fromPosition;

    private bool _hasReplacedToken;
    private Token? _replacedToken;

    /// <summary>
    ///     Creates a parser with basic lexer (original API - throws on errors).
    /// </summary>
    public Parser(ILexer lexer)
    {
        _lexer = lexer ?? throw new ArgumentNullException(nameof(lexer),
            "Lexer cannot be null. Please provide a valid lexer instance.");
        _enableRecovery = false;
        _hasReplacedToken = false;
        _replacedToken = null;
    }

    /// <summary>
    ///     Creates a parser with diagnostic collection and error recovery support.
    /// </summary>
    /// <param name="lexer">The lexer to use.</param>
    /// <param name="diagnostics">The diagnostic bag to collect errors.</param>
    /// <param name="enableRecovery">Whether to enable error recovery mode.</param>
    public Parser(ILexer lexer, DiagnosticBag diagnostics, bool enableRecovery = true)
    {
        _lexer = lexer ?? throw new ArgumentNullException(nameof(lexer));
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        _enableRecovery = enableRecovery;
    }

    private Token Current => _hasReplacedToken && _replacedToken != null ? _replacedToken : _lexer.Current();

    private Token? Previous { get; set; }

    private void ReplaceCurrentToken(Token newToken)
    {
        _replacedToken = newToken;
        _hasReplacedToken = true;
    }

    /// <summary>
    ///     Parses the input and returns a ParseResult with diagnostics.
    ///     This is the LSP-friendly API that collects errors instead of throwing.
    /// </summary>
    public ParseResult ParseWithDiagnostics()
    {
        var sourceText = _lexer.SourceText ?? new SourceText(_lexer.Input);
        var diagnostics = _diagnostics ?? new DiagnosticBag { SourceText = sourceText };

        try
        {
            _lexer.Next();
            var statements = new List<StatementNode>();

            while (Current.TokenType != TokenType.EndOfFile)
                try
                {
                    EnsureStatementSeparator(statements);
                    var statement = ComposeStatement();
                    if (statement != null)
                    {
                        statements.Add(statement);
                    }
                    else if (_enableRecovery)
                    {
                        if (HasLexicalDiagnostic())
                        {
                            if (!TryRecoverToNextStatement())
                                break;
                            continue;
                        }

                        RecordIncompleteStatementIfNeeded();

                        if (!TryRecoverToNextStatement())
                            break;
                    }
                    else
                    {
                        RecordError(
                            DiagnosticCode.MQ2016_IncompleteStatement,
                            "Failed to compose statement. The SQL query structure is invalid.",
                            Current.Span);
                        break;
                    }
                }
                catch (SyntaxException ex) when (_enableRecovery)
                {
                    if (HasLexicalDiagnostic())
                    {
                        if (!TryRecoverToNextStatement())
                            break;
                        continue;
                    }

                    RecordSyntaxExceptionIfNeeded(ex);
                    if (!TryRecoverToNextStatement())
                        break;
                }
                catch (NotSupportedException ex) when (_enableRecovery)
                {
                    if (HasLexicalDiagnostic())
                    {
                        if (!TryRecoverToNextStatement())
                            break;
                        continue;
                    }

                    // Parser-owned unsupported shapes are query syntax failures.  Convert them
                    // to the typed syntax diagnostic at this boundary so internal NotSupported
                    // exceptions can never leak into the public diagnostic classifier.
                    RecordSyntaxExceptionIfNeeded(new SyntaxException(
                        ex.Message,
                        _lexer.AlreadyResolvedQueryPart,
                        DiagnosticCode.MQ2030_UnsupportedSyntax,
                        Current.Span,
                        ex));
                    if (!TryRecoverToNextStatement())
                        break;
                }

            if (statements.Count == 1 && statements[0].Node is ParameterBlockNode &&
                !diagnostics.HasErrors && !HasLexicalDiagnostic())
            {
                RecordError(
                    DiagnosticCode.MQ2016_IncompleteStatement,
                    "A parameter block must be followed by a query or another script statement.",
                    Current.Span);
            }
            else if (statements.Count == 0 && !diagnostics.HasErrors && !HasLexicalDiagnostic())
            {
                RecordError(
                    DiagnosticCode.MQ2016_IncompleteStatement,
                    "The query contains no executable statement. Provide a SELECT query or another supported statement.",
                    Current.Span);
            }

            var root = statements.Count > 0
                ? new RootNode(new StatementsArrayNode(statements.ToArray()))
                : null;

            return CreateParseResult(root, sourceText, diagnostics);
        }
        catch (Exception ex)
        {
            if (ex.TryToDiagnostic(sourceText, out var diagnostic) && diagnostic != null)
            {
                RecordError(
                    diagnostic.Code,
                    diagnostic.Message,
                    diagnostic.Span);

                return CreateFailedParseResult(sourceText, diagnostics);
            }

            var fallbackDiagnostic = ex.ToDiagnosticOrGeneric(sourceText);
            var span = fallbackDiagnostic.Span == TextSpan.Empty ? Current.Span : fallbackDiagnostic.Span;
            RecordError(
                fallbackDiagnostic.Code,
                fallbackDiagnostic.Message,
                span);

            return CreateFailedParseResult(sourceText, diagnostics);
        }
    }

    /// <summary>
    ///     Original API - throws SyntaxException on errors.
    /// </summary>
    public RootNode ComposeAll()
    {
        try
        {
            _lexer.Next();
            var statements = new List<StatementNode>();
            while (Current.TokenType != TokenType.EndOfFile)
            {
                EnsureStatementSeparator(statements);
                var statement = ComposeStatement();
                if (statement == null)
                    throw new SyntaxException("Failed to compose statement. The SQL query structure is invalid.",
                        _lexer.AlreadyResolvedQueryPart);

                statements.Add(statement);
            }

            if (statements.Count == 1 && statements[0].Node is ParameterBlockNode)
                throw new SyntaxException(
                    "A parameter block must be followed by a query or another script statement.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2016_IncompleteStatement,
                    Current.Span);

            return new RootNode(new StatementsArrayNode(statements.ToArray()));
        }
        catch (Exception ex) when (!(ex is SyntaxException))
        {
            throw new SyntaxException($"An error occurred while parsing the SQL query: {ex.Message}",
                _lexer.AlreadyResolvedQueryPart, ex);
        }
    }

    private void EnsureStatementSeparator(IReadOnlyList<StatementNode> statements)
    {
        if (statements.Count == 0 || Previous?.TokenType == TokenType.Semicolon ||
            statements[^1].Node is ParameterBlockNode or ScriptVariableDeclarationNode or EnumDeclarationNode or CreateTableNode or
            CoupleNode or BinarySchemaNode or TextSchemaNode || Current.TokenType != TokenType.Select)
            return;

        throw new SyntaxException(
            "Multiple statements in a batch must be separated by a semicolon.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2001_UnexpectedToken,
            Current.Span);
    }

    private void RecordError(DiagnosticCode code, string message, TextSpan span)
    {
        if (_diagnostics == null) return;

        var diagnostic = SyntaxDiagnosticEnhancer.CreateDiagnostic(code, message, span, Current, _lexer.SourceText);
        _diagnostics.Add(diagnostic);
    }

    private void RecordSyntaxException(SyntaxException ex)
    {
        if (_diagnostics == null) return;

        var span = ex.Span ?? Current.Span;
        var diagnostic = IsLexicalDiagnostic(ex.Code)
            ? SyntaxDiagnosticEnhancer.EnhanceLexerDiagnostic(ex.Code, ex.Message, span, _lexer.SourceText)
            : SyntaxDiagnosticEnhancer.CreateDiagnostic(ex.Code, ex.Message, span, Current, _lexer.SourceText);
        _diagnostics.Add(ParserDiagnosticFacts.ApplyExceptionPayload(diagnostic, ex));
    }

    private bool TryRecoverToNextStatement()
    {
        while (Current.TokenType is not TokenType.Semicolon and not TokenType.EndOfFile)
            _lexer.Next();

        while (Current.TokenType == TokenType.Semicolon)
            _lexer.Next();

        return Current.TokenType != TokenType.EndOfFile;
    }

    private bool HasLexicalDiagnostic()
    {
        foreach (var diagnostic in _lexer.Diagnostics)
        {
            if (IsLexicalDiagnostic(diagnostic.Code))
                return true;
        }

        if (_diagnostics == null)
            return false;

        foreach (var diagnostic in _diagnostics)
        {
            if (IsLexicalDiagnostic(diagnostic.Code))
                return true;
        }

        return false;
    }

    private static bool IsLexicalDiagnostic(DiagnosticCode code)
    {
        var numericCode = (int)code;
        return numericCode is >= 1001 and <= 1009 ||
               code == DiagnosticCode.MQ2011_MissingClosingBracket;
    }

}
