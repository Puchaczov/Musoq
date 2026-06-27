using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private static readonly Regex ColumnRegex = new(@"\[[^\]]+\]|(\w+)|(\*)", RegexOptions.Compiled);

    private static readonly TokenType[] SetOperators =
        [TokenType.Union, TokenType.UnionAll, TokenType.Except, TokenType.Intersect];

    private static readonly string[] ClauseKeywords =
            ["WHERE", "GROUP", "ORDER", "HAVING", "TAKE", "SKIP", "UNION", "EXCEPT", "INTERSECT", "JOIN", "INNER", "OUTER", "CROSS", "QUALIFY"];

    private static readonly FrozenSet<TokenType> StatementRecoverySyncPoints =
        new[] { TokenType.Select, TokenType.From, TokenType.Pivot, TokenType.Unpivot, TokenType.With, TokenType.Desc, TokenType.Table, TokenType.Couple, TokenType.Semicolon, TokenType.EndOfFile }.ToFrozenSet();

    private const int MinLengthForLargerDistance = 5;
    private const int ShortWordMaxDistance = 1;
    private const int LongWordMaxDistance = 2;

    private readonly DiagnosticBag? _diagnostics;
    private readonly bool _enableRecovery;
    private readonly Stack<HashSet<string>> _fromAliasesStack = new();

    private readonly ILexer _lexer;

    private readonly Dictionary<TokenType, (short Precendence, Associativity Associativity)> _precedenceDictionary =
        new()
        {
            { TokenType.Pipe, (0, Associativity.Left) }, // Bitwise OR - lowest bitwise precedence
            { TokenType.Caret, (0, Associativity.Left) }, // Bitwise XOR
            { TokenType.Ampersand, (0, Associativity.Left) }, // Bitwise AND
            { TokenType.NullCoalescing, (0, Associativity.Right) },
            { TokenType.LeftShift, (1, Associativity.Left) }, // Left shift
            { TokenType.RightShift, (1, Associativity.Left) }, // Right shift
            { TokenType.Plus, (2, Associativity.Left) },
            { TokenType.Hyphen, (2, Associativity.Left) },
            { TokenType.Star, (3, Associativity.Left) },
            { TokenType.FSlash, (3, Associativity.Left) },
            { TokenType.Mod, (3, Associativity.Left) },
            { TokenType.Dot, (4, Associativity.Left) }
        };

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
                    var statement = ComposeStatement();
                    if (statement != null)
                    {
                        statements.Add(statement);
                    }
                    else if (_enableRecovery)
                    {
                        RecordError(
                            DiagnosticCode.MQ2016_IncompleteStatement,
                            "Failed to compose statement. The SQL query structure is invalid.",
                            Current.Span);

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
                    RecordSyntaxException(ex);
                    if (!TryRecoverToNextStatement())
                        break;
                }
                catch (NotSupportedException ex) when (_enableRecovery)
                {
                    RecordError(
                        DiagnosticCode.MQ2030_UnsupportedSyntax,
                        ex.Message,
                        Current.Span);
                    if (!TryRecoverToNextStatement())
                        break;
                }

            var root = statements.Count > 0
                ? new RootNode(new StatementsArrayNode(statements.ToArray()))
                : null;

            return new ParseResult(root, sourceText, diagnostics.ToSortedList());
        }
        catch (Exception ex)
        {
            if (ex.TryToDiagnostic(sourceText, out var diagnostic) && diagnostic != null)
            {
                RecordError(
                    diagnostic.Code,
                    diagnostic.Message,
                    diagnostic.Span);

                return ParseResult.Failed(sourceText, diagnostics.ToSortedList());
            }

            var fallbackDiagnostic = ex.ToDiagnosticOrGeneric(sourceText);
            var span = fallbackDiagnostic.Span == TextSpan.Empty ? Current.Span : fallbackDiagnostic.Span;
            RecordError(
                fallbackDiagnostic.Code,
                fallbackDiagnostic.Message,
                span);

            return ParseResult.Failed(sourceText, diagnostics.ToSortedList());
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
                var statement = ComposeStatement();
                if (statement == null)
                    throw new SyntaxException("Failed to compose statement. The SQL query structure is invalid.",
                        _lexer.AlreadyResolvedQueryPart);

                statements.Add(statement);
            }

            return new RootNode(new StatementsArrayNode(statements.ToArray()));
        }
        catch (Exception ex) when (!(ex is SyntaxException))
        {
            throw new SyntaxException($"An error occurred while parsing the SQL query: {ex.Message}",
                _lexer.AlreadyResolvedQueryPart, ex);
        }
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
        var diagnostic = SyntaxDiagnosticEnhancer.CreateDiagnostic(ex.Code, ex.Message, span, Current, _lexer.SourceText);
        _diagnostics.Add(diagnostic);
    }

    private bool TryRecoverToNextStatement()
    {
        while (Current.TokenType != TokenType.EndOfFile)
        {
            if (StatementRecoverySyncPoints.Contains(Current.TokenType))
            {
                if (Current.TokenType == TokenType.Semicolon) _lexer.Next();
                return Current.TokenType != TokenType.EndOfFile;
            }

            _lexer.Next();
        }

        return false;
    }

}
