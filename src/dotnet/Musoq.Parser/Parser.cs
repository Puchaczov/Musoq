using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Recovery;
using Musoq.Parser.Tokens;
using KeyAccessToken = Musoq.Parser.Tokens.KeyAccessToken;
using NumericAccessToken = Musoq.Parser.Tokens.NumericAccessToken;

namespace Musoq.Parser;

public class Parser
{
    private static readonly Regex ColumnRegex = new(@"\[[^\]]+\]|(\w+)|(\*)", RegexOptions.Compiled);

    private static readonly TokenType[] SetOperators =
        [TokenType.Union, TokenType.UnionAll, TokenType.Except, TokenType.Intersect];

    private static readonly string[] ClauseKeywords =
        ["WHERE", "GROUP", "ORDER", "HAVING", "TAKE", "SKIP", "UNION", "EXCEPT", "INTERSECT", "JOIN", "INNER", "OUTER", "CROSS"];

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
            { TokenType.LeftShift, (1, Associativity.Left) }, // Left shift
            { TokenType.RightShift, (1, Associativity.Left) }, // Right shift
            { TokenType.Plus, (2, Associativity.Left) },
            { TokenType.Hyphen, (2, Associativity.Left) },
            { TokenType.Star, (3, Associativity.Left) },
            { TokenType.FSlash, (3, Associativity.Left) },
            { TokenType.Mod, (3, Associativity.Left) },
            { TokenType.Dot, (4, Associativity.Left) }
        };

    private readonly ErrorRecoveryManager? _recoveryManager;

    private int _fromPosition;

    private bool _hasReplacedToken;
    private Token _replacedToken;

    /// <summary>
    ///     Creates a parser with basic lexer (original API - throws on errors).
    /// </summary>
    public Parser(ILexer lexer)
    {
        _lexer = lexer ?? throw new ArgumentNullException(nameof(lexer),
            "Lexer cannot be null. Please provide a valid lexer instance.");
        _enableRecovery = false;
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

        if (enableRecovery)
            _recoveryManager = new ErrorRecoveryManager(diagnostics, _lexer.SourceText);
    }

    private Token Current => _hasReplacedToken ? _replacedToken : _lexer.Current();

    private Token Previous { get; set; }

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
        var syncPoints = new HashSet<TokenType>
        {
            TokenType.Select, TokenType.From, TokenType.With,
            TokenType.Desc, TokenType.Table, TokenType.Couple,
            TokenType.Semicolon, TokenType.EndOfFile
        };

        while (Current.TokenType != TokenType.EndOfFile)
        {
            if (syncPoints.Contains(Current.TokenType))
            {
                if (Current.TokenType == TokenType.Semicolon) _lexer.Next();
                return Current.TokenType != TokenType.EndOfFile;
            }

            _lexer.Next();
        }

        return false;
    }

    private bool TryConsume(TokenType expected, out Token consumed)
    {
        consumed = Current;

        if (Current.TokenType == expected)
        {
            Previous = Current;
            _hasReplacedToken = false;
            _lexer.Next();
            return true;
        }

        if (_enableRecovery && _diagnostics != null)
        {
            RecordError(
                DiagnosticCode.MQ2002_MissingToken,
                $"Expected '{expected}' but found '{Current.TokenType}'.",
                Current.Span);


            if (TryRecoverFromMissingToken(expected))
            {
                consumed = Previous;
                return true;
            }
        }

        return false;
    }

    private bool TryRecoverFromMissingToken(TokenType expected)
    {
        switch (expected)
        {
            case TokenType.From
                when Current.TokenType is TokenType.Word or TokenType.Identifier or TokenType.MethodAccess:

                return true;

            case TokenType.RightParenthesis
                when Current.TokenType is TokenType.From or TokenType.Where or TokenType.GroupBy or TokenType.OrderBy:

                return true;

            case TokenType.Comma when Current.TokenType is TokenType.Word or TokenType.Identifier or TokenType.Function:

                return true;
        }

        return false;
    }

    private StatementNode ComposeStatement()
    {
        if (Current.TokenType == TokenType.Identifier &&
            (Current.Value.Equals("binary", StringComparison.OrdinalIgnoreCase) ||
             Current.Value.Equals("text", StringComparison.OrdinalIgnoreCase)))
        {
            _lexer.IsSchemaContext = true;
            return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeInterpretationSchema()),
                TokenType.Semicolon);
        }

        switch (Current.TokenType)
        {
            case TokenType.Desc:
                return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeDesc()), TokenType.Semicolon);
            case TokenType.Select:
                return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeSetOperators(0)), TokenType.Semicolon);
            case TokenType.From:
                return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeSetOperators(0)), TokenType.Semicolon);
            case TokenType.With:
                return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeCteExpression()), TokenType.Semicolon);
            case TokenType.Table:
                return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeTable()), TokenType.Semicolon);
            case TokenType.Couple:
                return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeCouple()), TokenType.Semicolon);
            case TokenType.Binary:
            case TokenType.Text:
                return ComposeAndSkipIfPresent(p => new StatementNode(p.ComposeInterpretationSchema()),
                    TokenType.Semicolon);

            default:
                throw new SyntaxException(
                    $"Cannot compose statement, {Current.TokenType} is not expected here",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2001_UnexpectedToken,
                    Current.Span);
        }
    }

    private Node ComposeDesc()
    {
        Consume(Current.TokenType);

        if (Current.TokenType == TokenType.Functions)
        {
            Consume(TokenType.Functions);

            if (Current.TokenType == TokenType.MethodAccess)
            {
                var sourceAlias = Current.Value;
                var schemaName = EnsureHashPrefix(sourceAlias);
                ComposeAccessMethod(sourceAlias);

                return new DescNode(
                    new SchemaFromNode(schemaName, string.Empty, ArgsListNode.Empty, string.Empty, 1),
                    DescForType.FunctionsForSchema);
            }

            var schemaNameForFunctions = ComposeSchemaName();
            var schemaToken = Current;

            if (Current.TokenType == TokenType.Dot)
            {
                Consume(TokenType.Dot);

                if (Current is FunctionToken)
                {
                    ComposeAccessMethod(string.Empty);
                    return new DescNode(
                        new SchemaFromNode(schemaNameForFunctions, string.Empty, ArgsListNode.Empty,
                            string.Empty, 1), DescForType.FunctionsForSchema);
                }

                ConsumeAndGetToken(TokenType.Property);
            }

            return new DescNode(
                new SchemaFromNode(schemaNameForFunctions, string.Empty, ArgsListNode.Empty, string.Empty,
                    schemaToken.Span.Start), DescForType.FunctionsForSchema);
        }

        if (Current.TokenType == TokenType.MethodAccess)
        {
            var sourceAlias = Current.Value;
            var schemaName = EnsureHashPrefix(sourceAlias);
            var accessMethod = ComposeAccessMethod(sourceAlias);

            var fromNode = new SchemaFromNode(schemaName, accessMethod.Name, accessMethod.Arguments, string.Empty, 1);
            return TryParseColumnClause(fromNode);
        }

        var name = ComposeSchemaName();
        var startToken = Current;

        if (Current.TokenType == TokenType.Dot)
        {
            Consume(TokenType.Dot);

            FromNode fromNode;
            if (Current is FunctionToken)
            {
                var accessMethod = ComposeAccessMethod(string.Empty);

                fromNode = new SchemaFromNode(name, accessMethod.Name, accessMethod.Arguments, string.Empty, 1);
                return TryParseColumnClause(fromNode);
            }

            var methodName = new WordNode(ConsumeAndGetToken(TokenType.Property).Value);

            fromNode = new SchemaFromNode(name, methodName.Value, ArgsListNode.Empty, string.Empty, 1);
            return new DescNode(fromNode, DescForType.Constructors);
        }

        return new DescNode(
            new SchemaFromNode(name, string.Empty, ArgsListNode.Empty, string.Empty, startToken.Span.Start),
            DescForType.Schema);
    }

    private DescNode TryParseColumnClause(FromNode fromNode)
    {
        var isColumnKeyword = Current.TokenType is TokenType.Word or TokenType.Identifier &&
                              string.Equals(Current.Value, ColumnKeywordToken.TokenText,
                                  StringComparison.OrdinalIgnoreCase);

        if (!isColumnKeyword)
            return new DescNode(fromNode, DescForType.SpecificConstructor);

        Consume(Current.TokenType);
        var column = ParseColumnAccess();
        return new DescNode(fromNode, DescForType.SpecificColumn, column);
    }

    private Node ParseColumnAccess()
    {
        var node = ComposeArithmeticExpression(0);
        ValidateColumnAccessNode(node);
        return node;
    }

    private void ValidateColumnAccessNode(Node node)
    {
        switch (node)
        {
            case DotNode d:
                ValidateColumnAccessNode(d.Root);
                ValidateColumnAccessNode(d.Expression);
                break;
            case PropertyValueNode:
            case WordNode:
            case IdentifierNode:
                break;
            default:
                throw new SyntaxException(
                    $"Invalid column path. Expected property path but received {node.GetType().Name}",
                    _lexer.AlreadyResolvedQueryPart);
        }
    }

    private string ComposeSchemaName()
    {
        if (Current.TokenType == TokenType.Word)
            return ComposeWord().Value;

        if (Current.TokenType == TokenType.Identifier)
        {
            var identifier = ConsumeAndGetToken(TokenType.Identifier).Value;
            return EnsureHashPrefix(identifier);
        }

        throw new SyntaxException($"Expected schema name (Word or Identifier) but received {Current.TokenType}",
            _lexer.AlreadyResolvedQueryPart);
    }

    private static string EnsureHashPrefix(string name)
    {
        return name.StartsWith('#') ? name : $"#{name}";
    }

    private CoupleNode ComposeCouple()
    {
        Consume(TokenType.Couple);

        var from = ComposeSchemaMethod();

        Consume(TokenType.With);
        Consume(TokenType.Table);

        var name = Current.Value;

        Consume(Current.TokenType);

        Consume(TokenType.As);

        var identifierNode = (IdentifierNode)ComposeBaseTypes();

        return new CoupleNode(from, name, identifierNode.Name);
    }

    private Node ComposeInterpretationSchema()
    {
        var schemaParser = new SchemaParser(_lexer);


        return schemaParser.ParseSchemaFromCurrentPosition();
    }

    private CreateTableNode ComposeTable()
    {
        Consume(Current.TokenType);
        var tableName = Current.Value;
        Consume(TokenType.Identifier);
        Consume(TokenType.LBracket);

        var columns = new List<(string ColumnName, string TypeName)>();
        while (Current.TokenType != TokenType.RBracket)
        {
            var fieldName = Current.Value;
            Consume(TokenType.Identifier);

            Consume(TokenType.Colon);

            var typeName = Current.Value;
            Consume(TokenType.Identifier);

            if (Current.TokenType == TokenType.QuestionMark)
            {
                typeName += "?";
                Consume(TokenType.QuestionMark);
            }

            if (Current.TokenType == TokenType.Comma)
                Consume(TokenType.Comma);

            columns.Add((fieldName, typeName));
        }

        Consume(Current.TokenType);

        return new CreateTableNode(tableName, columns.ToArray());
    }

    private CteExpressionNode ComposeCteExpression()
    {
        Consume(TokenType.With);

        PushFromAliasesScope();
        try
        {
            var expressions = new List<CteInnerExpressionNode>();

            if (ComposeBaseTypes() is not IdentifierNode col)
                throw new SyntaxException($"Expected token is {TokenType.Identifier} but received {Current.TokenType}",
                    _lexer.AlreadyResolvedQueryPart);

            RegisterFromAlias(col.Name);

            Consume(TokenType.As);
            Consume(TokenType.LeftParenthesis);
            var innerSets = ComposeSetOperators(0);
            expressions.Add(new CteInnerExpressionNode(innerSets, col.Name));
            Consume(TokenType.RightParenthesis);

            while (Current.TokenType == TokenType.Comma)
            {
                Consume(TokenType.Comma);

                col = ComposeBaseTypes() as IdentifierNode ??
                      throw new InvalidOperationException(nameof(IdentifierNode));

                if (col is null)
                    throw new SyntaxException(
                        $"Expected token is {TokenType.Identifier} but received {Current.TokenType}",
                        _lexer.AlreadyResolvedQueryPart);

                RegisterFromAlias(col.Name);

                Consume(TokenType.As);

                Consume(TokenType.LeftParenthesis);
                innerSets = ComposeSetOperators(0);
                Consume(TokenType.RightParenthesis);
                expressions.Add(new CteInnerExpressionNode(innerSets, col.Name));
            }

            var outerSets = ComposeSetOperators(0);

            return new CteExpressionNode(expressions.ToArray(), outerSets);
        }
        finally
        {
            PopFromAliasesScope();
        }
    }

    private Node ComposeSetOperators(int nestingLevel)
    {
        var isSet = false;
        var query = ComposeQuery();

        Node node = query;
        while (IsSetOperator(Current.TokenType))
        {
            isSet = true;
            var setOperatorType = Current.TokenType;
            Consume(Current.TokenType);

            var keys = ComposeSetOperatorKeys();

            var nextSet = ComposeSetOperators(nestingLevel + 1);
            var isQuery = nextSet is QueryNode;
            node = setOperatorType switch
            {
                TokenType.Except => new ExceptNode(string.Empty, keys, node, nextSet, nestingLevel != 0, isQuery),
                TokenType.Union => new UnionNode(string.Empty, keys, node, nextSet, nestingLevel != 0, isQuery),
                TokenType.UnionAll => new UnionAllNode(string.Empty, keys, node, nextSet, nestingLevel != 0,
                    isQuery),
                TokenType.Intersect => new IntersectNode(string.Empty, keys, node, nextSet, nestingLevel != 0,
                    isQuery),
                _ => node
            };
        }

        return isSet || nestingLevel > 0 ? node : new SingleSetNode(query);
    }

    private string[] ComposeSetOperatorKeys()
    {
        var keys = new List<string>();

        if (Current.TokenType != TokenType.LeftParenthesis) return keys.ToArray();

        Consume(TokenType.LeftParenthesis);

        if (Current.TokenType == TokenType.RightParenthesis)
        {
            Consume(TokenType.RightParenthesis);
            return keys.ToArray();
        }

        keys.Add(ParsePotentiallyDottedName());
        while (Current.TokenType == TokenType.Comma)
        {
            Consume(TokenType.Comma);
            keys.Add(ParsePotentiallyDottedName());
        }

        Consume(TokenType.RightParenthesis);

        return keys.ToArray();
    }

    private string ParsePotentiallyDottedName()
    {
        var value = Current.Value;
        Consume(Current.TokenType);
        if (Current.TokenType != TokenType.Dot) return value;

        Consume(Current.TokenType);
        value = $"{value}.{Current.Value}";
        Consume(Current.TokenType);
        return value;
    }

    private static bool IsSetOperator(TokenType currentTokenType)
    {
        return SetOperators.Contains(currentTokenType);
    }

    private QueryNode ComposeQuery()
    {
        QueryNode query;
        if (Current.TokenType == TokenType.Select)
            query = ComposeRegularQuery();
        else if (Current.TokenType == TokenType.From)
            query = ComposeReorderedQuery();
        else
            throw new NotSupportedException("Cannot recognize if query is regular or reordered.");
        return query;
    }

    private QueryNode ComposeRegularQuery()
    {
        PushFromAliasesScope();
        try
        {
            _fromPosition += 1;
            var selectNode = ComposeSelectNode();
            var fromNode = ComposeFrom();
            fromNode = ComposeJoinOrApply(fromNode);
            var whereNode = ComposeWhere(false);
            var groupBy = ComposeGroupByNode();
            var window = ComposeWindowClause();
            var orderBy = ComposeOrderBy();
            var skip = ComposeSkip();
            var take = ComposeTake();
            return new QueryNode(selectNode, fromNode, whereNode, groupBy, orderBy, skip, take, window);
        }
        finally
        {
            PopFromAliasesScope();
        }
    }

    private QueryNode ComposeReorderedQuery()
    {
        PushFromAliasesScope();
        try
        {
            _fromPosition += 1;
            var fromNode = ComposeFrom();
            fromNode = ComposeJoinOrApply(fromNode);
            var whereNode = ComposeWhere(false);
            var groupBy = ComposeGroupByNode();
            var window = ComposeWindowClause();
            var selectNode = ComposeSelectNode();
            var orderBy = ComposeOrderBy();
            var skip = ComposeSkip();
            var take = ComposeTake();
            return new QueryNode(selectNode, fromNode, whereNode, groupBy, orderBy, skip, take, window);
        }
        finally
        {
            PopFromAliasesScope();
        }
    }

    private FromNode ComposeJoinOrApply(FromNode from)
    {
        if (!IsJoinOrApplyToken(Current.TokenType)) return new ExpressionFromNode(from);

        while (IsJoinOrApplyToken(Current.TokenType))
            switch (Current.TokenType)
            {
                case TokenType.InnerJoin:
                    Consume(TokenType.InnerJoin);
                    from = new JoinFromNode(from,
                        ComposeAndSkip(parser => parser.ComposeFrom(false), TokenType.On),
                        ComposeOperations(),
                        JoinType.Inner);
                    break;
                case TokenType.OuterJoin:
                    var outerToken = (OuterJoinToken)Current;
                    Consume(TokenType.OuterJoin);
                    from = new JoinFromNode(from,
                        ComposeAndSkip(parser => parser.ComposeFrom(false), TokenType.On),
                        ComposeOperations(),
                        outerToken.Type == OuterJoinType.Left
                            ? JoinType.OuterLeft
                            : JoinType.OuterRight);
                    break;
                case TokenType.CrossApply:
                    Consume(TokenType.CrossApply);
                    from = new ApplyFromNode(from, Compose(parser => parser.ComposeFrom(false, true)), ApplyType.Cross);
                    break;
                case TokenType.OuterApply:
                    Consume(TokenType.OuterApply);
                    from = new ApplyFromNode(from, Compose(parser => parser.ComposeFrom(false, true)), ApplyType.Outer);
                    break;
                case TokenType.AsOfJoin:
                    var asOfToken = (AsOfJoinToken)Current;
                    Consume(TokenType.AsOfJoin);
                    from = new JoinFromNode(from,
                        ComposeAndSkip(parser => parser.ComposeFrom(false), TokenType.On),
                        ComposeOperations(),
                        asOfToken.IsLeft
                            ? JoinType.AsOfLeft
                            : JoinType.AsOf);
                    break;
            }

        if (from is JoinFromNode joinFrom) from = new JoinNode(joinFrom);

        if (from is ApplyFromNode applyFrom) from = new ApplyNode(applyFrom);

        return new ExpressionFromNode(from);
    }

    private static bool IsJoinOrApplyToken(TokenType currentTokenType)
    {
        return currentTokenType is TokenType.InnerJoin or TokenType.OuterJoin or TokenType.CrossApply
            or TokenType.OuterApply or TokenType.AsOfJoin;
    }

    private OrderByNode ComposeOrderBy()
    {
        if (Current.TokenType != TokenType.OrderBy) return null;

        Consume(TokenType.OrderBy);
        return new OrderByNode(ComposeOrderedFields());
    }

    private TakeNode ComposeTake()
    {
        if (Current.TokenType == TokenType.Take)
        {
            Consume(TokenType.Take);
            var valueSpan = Current.Span;
            var intNode = ComposeInteger();

            if (IsNegativeInteger(intNode))
                RecordError(DiagnosticCode.MQ2030_UnsupportedSyntax,
                    "TAKE value must be non-negative.",
                    valueSpan);

            return new TakeNode(intNode);
        }

        return null;
    }

    private SkipNode ComposeSkip()
    {
        if (Current.TokenType == TokenType.Skip)
        {
            Consume(TokenType.Skip);
            var valueSpan = Current.Span;
            var intNode = ComposeInteger();

            if (IsNegativeInteger(intNode))
                RecordError(DiagnosticCode.MQ2030_UnsupportedSyntax,
                    "SKIP value must be non-negative.",
                    valueSpan);

            return new SkipNode(intNode);
        }

        return null;
    }

    private static bool IsNegativeInteger(IntegerNode intNode)
    {
        return intNode.ObjValue switch
        {
            int i => i < 0,
            long l => l < 0,
            short s => s < 0,
            sbyte sb => sb < 0,
            _ => false
        };
    }

    private GroupByNode ComposeGroupByNode()
    {
        if (Current.TokenType != TokenType.GroupBy) return null;

        Consume(TokenType.GroupBy);

        if (Current.TokenType == TokenType.Comma)
            throw new SyntaxException("Unnecessary comma found after GROUP BY clause.",
                _lexer.AlreadyResolvedQueryPart);

        var fields = ComposeFields();

        if (Previous.TokenType == TokenType.Comma && Current.TokenType == TokenType.EndOfFile)
            throw new SyntaxException("Unnecessary comma found after GROUP BY clause.",
                _lexer.AlreadyResolvedQueryPart);

        if (fields.Length == 0) throw new NotSupportedException("Group by clause does not have any fields.");
        if (Current.TokenType != TokenType.Having) return new GroupByNode(fields, null);

        Consume(TokenType.Having);

        var having = new HavingNode(ComposeOperations());

        return new GroupByNode(fields, having);
    }

    private WindowNode ComposeWindowClause()
    {
        if (Current.TokenType != TokenType.Window) return null;

        Consume(TokenType.Window);

        var definitions = new List<WindowDefinitionNode>();

        do
        {
            if (definitions.Count > 0)
                Consume(TokenType.Comma);

            var nameToken = Current;
            Consume(Current.TokenType);
            var windowName = nameToken.Value;

            Consume(TokenType.As);

            var spec = ComposeWindowSpecification();
            definitions.Add(new WindowDefinitionNode(windowName, spec));
        } while (Current.TokenType == TokenType.Comma);

        return new WindowNode(definitions.ToArray());
    }

    private WindowSpecificationNode ComposeWindowSpecification()
    {
        Consume(TokenType.LeftParenthesis);

        FieldNode[] partitionFields = null;
        FieldOrderedNode[] orderByFields = null;

        if (Current.TokenType == TokenType.PartitionBy)
        {
            Consume(TokenType.PartitionBy);
            partitionFields = ComposeFields();
        }

        if (Current.TokenType == TokenType.OrderBy)
        {
            Consume(TokenType.OrderBy);
            orderByFields = ComposeOrderedFields();
        }

        Consume(TokenType.RightParenthesis);

        return new WindowSpecificationNode(partitionFields, orderByFields);
    }

    private Node TryComposeWindowFunction(AccessMethodNode methodNode)
    {
        if (Current.TokenType == TokenType.Over)
        {
            Consume(TokenType.Over);

            if (Current.TokenType == TokenType.LeftParenthesis)
            {
                var spec = ComposeWindowSpecification();
                return new WindowFunctionNode(methodNode, spec);
            }

            var windowName = Current.Value;
            Consume(Current.TokenType);
            return new WindowFunctionNode(methodNode, windowName);
        }

        if (Current is FunctionToken { Value: var funcName } && funcName.Equals("over", StringComparison.OrdinalIgnoreCase))
        {
            Consume(TokenType.Function);
            var spec = ComposeWindowSpecification();
            return new WindowFunctionNode(methodNode, spec);
        }

        return methodNode;
    }

    private SelectNode ComposeSelectNode()
    {
        Consume(TokenType.Select);
        ConsumeWhiteSpaces();

        var isDistinct = false;
        if (Current.TokenType == TokenType.Distinct)
        {
            Consume(TokenType.Distinct);
            ConsumeWhiteSpaces();
            isDistinct = true;
        }

        if (Current.TokenType == TokenType.Comma)
            throw new SyntaxException("Unnecessary comma found after SELECT keyword.", _lexer.AlreadyResolvedQueryPart);

        var fields = ComposeFields();

        if (fields.Length == 0)
            throw new SyntaxException(
                "SELECT list cannot be empty.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2005_InvalidSelectList,
                Current.Span);

        if (Previous.TokenType == TokenType.Comma && Current.TokenType == TokenType.From)
            throw new SyntaxException("Unnecessary comma found at the end of SELECT clause.",
                _lexer.AlreadyResolvedQueryPart);

        return new SelectNode(fields, isDistinct);
    }

    private FieldNode[] ComposeFields()
    {
        var fields = new List<FieldNode>();
        var i = 0;

        do
        {
            if (Current.TokenType == TokenType.From) break;

            if (Current.TokenType == TokenType.EndOfFile) break;

            fields.Add(ConsumeField(i++));
        } while (!IsSetOperator(Current.TokenType) && Current.TokenType != TokenType.RightParenthesis &&
                 Current.TokenType != TokenType.From && Current.TokenType != TokenType.Having &&
                 Current.TokenType != TokenType.Skip && Current.TokenType != TokenType.Take &&
                 Current.TokenType != TokenType.Select &&
                 Current.TokenType != TokenType.OrderBy &&
                 Current.TokenType != TokenType.Window &&
                 ConsumeAndGetToken().TokenType == TokenType.Comma);

        return fields.ToArray();
    }

    private FieldOrderedNode[] ComposeOrderedFields()
    {
        var fields = new List<FieldOrderedNode>();
        var i = 0;

        do
        {
            fields.Add(ConsumeFieldOrdered(i++));
        } while (!IsSetOperator(Current.TokenType) && Current.TokenType != TokenType.RightParenthesis &&
                 Current.TokenType != TokenType.Skip && Current.TokenType != TokenType.Take &&
                 ConsumeAndGetToken().TokenType == TokenType.Comma);

        return fields.ToArray();
    }

    private FieldNode ConsumeField(int order)
    {
        var fieldExpression = ComposeOperations();
        var (alias, _) = ComposeAlias();
        return new FieldNode(fieldExpression, order, alias);
    }

    private FieldOrderedNode ConsumeFieldOrdered(int level)
    {
        var fieldExpression = ComposeOperations();
        var order = ComposeOrder();
        return new FieldOrderedNode(fieldExpression, level, string.Empty, order);
    }

    private (string Alias, TextSpan Span) ComposeAlias()
    {
        switch (Current.TokenType)
        {
            case TokenType.As:
                Consume(TokenType.As);
                var token = Current;
                Consume(Current.TokenType);
                return (token.Value, token.Span);
            case TokenType.Word:
                var wordToken = ConsumeAndGetToken(TokenType.Word);
                return (wordToken.Value, wordToken.Span);
            case TokenType.Identifier:
                if (IsLikelyMisspelledClauseKeyword(Current.Value))
                    return (string.Empty, default);
                var idToken = ConsumeAndGetToken(TokenType.Identifier);
                return (idToken.Value, idToken.Span);
        }

        return (string.Empty, default);
    }

    private static bool IsLikelyMisspelledClauseKeyword(string identifier)
    {
        var maxDistance = identifier.Length >= MinLengthForLargerDistance ? LongWordMaxDistance : ShortWordMaxDistance;

        return ErrorCatalog.GetDidYouMeanSuggestion(identifier, ClauseKeywords, maxDistance: maxDistance) != null;
    }

    private Order ComposeOrder()
    {
        switch (Current.TokenType)
        {
            case TokenType.Asc:
                Consume(TokenType.Asc);
                return Order.Ascending;
            case TokenType.Desc:
                Consume(TokenType.Desc);
                return Order.Descending;
            case TokenType.Comma:
                return Order.Ascending;
            case TokenType.EndOfFile:
                return Order.Ascending;
            case TokenType.Semicolon:
                return Order.Ascending;
            case TokenType.RightParenthesis:
                return Order.Ascending;
            case TokenType.Skip:
                return Order.Ascending;
            case TokenType.Take:
                return Order.Ascending;
            default:
                throw new NotSupportedException(
                    $"Unrecognized token for ComposeOrder(), the token was {Current.TokenType}");
        }
    }

    private Node ComposeOperations()
    {
        var node = ComposeEqualityOperators();

        while (IsQueryOperator(Current))
            switch (Current.TokenType)
            {
                case TokenType.And:
                    Consume(TokenType.And);
                    node = new AndNode(node, ComposeEqualityOperators());
                    break;
                case TokenType.Or:
                    Consume(TokenType.Or);
                    node = new OrNode(node, ComposeEqualityOperators());
                    break;
                default:
                    throw new NotSupportedException(
                        $"Unrecognized token for ComposeOperations(), the token was {Current.TokenType}");
            }

        return node;
    }

    private Node ComposeArithmeticExpression(int minPrecedence)
    {
        var left = ComposeBaseTypes(minPrecedence);

        if (IsNumericToken(Current)) left = new AddNode(left, ComposeBaseTypes(minPrecedence));

        while (IsArithmeticBinaryOperator(Current) &&
               _precedenceDictionary[Current.TokenType].Precendence >= minPrecedence)
        {
            var curr = Current;
            var op = _precedenceDictionary[Current.TokenType];
            var nextMinPrecedence = op.Associativity == Associativity.Left ? op.Precendence + 1 : op.Precendence;
            Consume(Current.TokenType);


            if (curr.TokenType == TokenType.Dot && IsSqlKeywordToken(Current.TokenType))
                ReplaceCurrentToken(new ColumnToken(Current.Value, Current.Span));

            var right = ComposeArithmeticExpression(nextMinPrecedence);

            left = curr.TokenType switch
            {
                TokenType.Plus => new AddNode(left, right),
                TokenType.Hyphen => new HyphenNode(left, right),
                TokenType.Star => new StarNode(left, right),
                TokenType.FSlash => new FSlashNode(left, right),
                TokenType.Mod => new ModuloNode(left, right),
                TokenType.Dot => new DotNode(left, right, string.Empty),
                TokenType.Ampersand => new BitwiseAndNode(left, right),
                TokenType.Pipe => new BitwiseOrNode(left, right),
                TokenType.Caret => new BitwiseXorNode(left, right),
                TokenType.LeftShift => new LeftShiftNode(left, right),
                TokenType.RightShift => new RightShiftNode(left, right),
                _ => throw new NotSupportedException($"{curr.TokenType} is not supported while parsing expression.")
            };
        }

        return left;
    }

    private Node ComposeEqualityOperators()
    {
        var node = ComposeArithmeticExpression(0);

        while (IsEqualityOperator(Current))
            switch (Current.TokenType)
            {
                case TokenType.GreaterEqual:
                    Consume(TokenType.GreaterEqual);
                    node = new GreaterOrEqualNode(node, ComposeEqualityOperators());
                    break;
                case TokenType.Greater:
                    Consume(TokenType.Greater);
                    node = new GreaterNode(node, ComposeEqualityOperators());
                    break;
                case TokenType.LessEqual:
                    Consume(TokenType.LessEqual);
                    node = new LessOrEqualNode(node, ComposeEqualityOperators());
                    break;
                case TokenType.Less:
                    Consume(TokenType.Less);
                    node = new LessNode(node, ComposeEqualityOperators());
                    break;
                case TokenType.Equality:
                    Consume(TokenType.Equality);
                    node = new EqualityNode(node, ComposeEqualityOperators());
                    break;
                case TokenType.Diff:
                    if (Current.Value == "!=")
                        throw new SyntaxException(
                            "Invalid operator '!='. Use '<>' instead.",
                            _lexer.AlreadyResolvedQueryPart,
                            DiagnosticCode.MQ2019_InvalidOperator,
                            Current.Span);
                    Consume(TokenType.Diff);
                    node = new DiffNode(node, ComposeEqualityOperators());
                    break;
                case TokenType.Not:
                    Consume(TokenType.Not);
                    node = new NotNode(node);
                    break;
                case TokenType.Like:
                    Consume(TokenType.Like);
                    node = new LikeNode(node, ComposeBaseTypes());
                    break;
                case TokenType.NotLike:
                    Consume(TokenType.NotLike);
                    node = new NotNode(new LikeNode(node, ComposeBaseTypes()));
                    break;
                case TokenType.RLike:
                    Consume(TokenType.RLike);
                    node = new RLikeNode(node, ComposeBaseTypes());
                    break;
                case TokenType.NotRLike:
                    Consume(TokenType.NotRLike);
                    node = new NotNode(new RLikeNode(node, ComposeBaseTypes()));
                    break;
                case TokenType.Contains:
                    Consume(TokenType.Contains);
                    node = new ContainsNode(node, ComposeNonEmptyArgs("CONTAINS"));
                    break;
                case TokenType.Is:
                    Consume(TokenType.Is);
                    node = Current.TokenType == TokenType.Not
                        ? SkipComposeSkip(TokenType.Not, _ => new IsNullNode(node, true), TokenType.Null)
                        : ComposeAndSkip(parser => new IsNullNode(node, false), TokenType.Null);
                    break;
                case TokenType.In:
                    Consume(TokenType.In);
                    node = new InNode(node, ComposeArgs());
                    break;
                case TokenType.NotIn:
                    Consume(TokenType.NotIn);
                    node = new NotNode(new InNode(node, ComposeArgs()));
                    break;
                case TokenType.Between:
                    node = ComposeBetween(node);
                    break;
                default:
                    throw new NotSupportedException(
                        $"Unrecognized token for ComposeEqualityOperators(), the token was {Current.TokenType}");
            }

        return node;
    }

    private Node ComposeBetween(Node expression)
    {
        Consume(TokenType.Between);
        var min = ComposeArithmeticExpression(0);
        Consume(TokenType.And);
        var max = ComposeArithmeticExpression(0);
        return new BetweenNode(expression, min, max);
    }

    private SchemaMethodFromNode ComposeSchemaMethod()
    {
        if (Current.TokenType == TokenType.MethodAccess)
        {
            var sourceAlias = Current.Value;
            var schemaName = EnsureHashPrefix(sourceAlias);
            var accessMethod = ComposeAccessMethod(sourceAlias);
            var (alias, _) = ComposeAlias();

            return new SchemaMethodFromNode(alias, schemaName, accessMethod.Name);
        }

        var schemaNode = ComposeSchemaName();
        ConsumeAsColumn(TokenType.Dot);
        var identifier = (IdentifierNode)ComposeBaseTypes();
        var (composeAlias, _) = ComposeAlias();

        return new SchemaMethodFromNode(composeAlias, schemaNode, identifier.Name);
    }

    private FromNode ComposeFrom(bool fromKeywordBefore = true, bool isApplyContext = false)
    {
        if (fromKeywordBefore)
            Consume(TokenType.From);

        string alias;
        TextSpan aliasSpan;
        if (Current.TokenType == TokenType.Word)
        {
            var name = ComposeWord();

            FromNode fromNode;
            if (Current.TokenType == TokenType.Dot)
            {
                Consume(TokenType.Dot);
                var accessMethod = ComposeAccessMethod(string.Empty);

                (alias, aliasSpan) = ComposeAlias();

                fromNode = new SchemaFromNode(name.Value, accessMethod.Name, accessMethod.Arguments, alias,
                    _fromPosition);
            }
            else
            {
                (alias, aliasSpan) = ComposeAlias();
                fromNode = new ReferentialFromNode(name.Value, alias);
            }

            if (!aliasSpan.IsEmpty)
                fromNode.WithSpan(aliasSpan);

            if (!string.IsNullOrWhiteSpace(alias))
                RegisterFromAlias(alias);

            return fromNode;
        }

        if (Current.TokenType == TokenType.Function)
        {
            var method = ComposeAccessMethod(string.Empty);
            (alias, aliasSpan) = ComposeAlias();

            if (!string.IsNullOrWhiteSpace(alias))
                RegisterFromAlias(alias);

            var fromNode = new AliasedFromNode(method.Name, method.Arguments, alias, _fromPosition);
            if (!aliasSpan.IsEmpty)
                fromNode.WithSpan(aliasSpan);
            return fromNode;
        }

        if (Current.TokenType == TokenType.MethodAccess)
        {
            var sourceAlias = Current.Value;
            var accessMethod = ComposeAccessMethod(sourceAlias);
            (alias, aliasSpan) = ComposeAlias();

            var isSchemaReference = sourceAlias.StartsWith('#') ||
                                    !isApplyContext ||
                                    (isApplyContext && !IsKnownFromAlias(sourceAlias));

            if (isSchemaReference && !sourceAlias.StartsWith('#'))
            {
                var schemaName = EnsureHashPrefix(sourceAlias);

                if (!string.IsNullOrWhiteSpace(alias))
                    RegisterFromAlias(alias);

                var fromNode = new SchemaFromNode(schemaName, accessMethod.Name, accessMethod.Arguments, alias, _fromPosition);
                if (!aliasSpan.IsEmpty)
                    fromNode.WithSpan(aliasSpan);
                return fromNode;
            }

            if (sourceAlias.StartsWith('#'))
            {
                if (!string.IsNullOrWhiteSpace(alias))
                    RegisterFromAlias(alias);

                var fromNode = new SchemaFromNode(sourceAlias, accessMethod.Name, accessMethod.Arguments, alias, _fromPosition);
                if (!aliasSpan.IsEmpty)
                    fromNode.WithSpan(aliasSpan);
                return fromNode;
            }

            if (string.IsNullOrWhiteSpace(alias))
                throw new NotSupportedException("Alias cannot be empty when parsing From clause.");

            var accessFromNode = new AccessMethodFromNode(alias, sourceAlias, accessMethod);
            if (!aliasSpan.IsEmpty)
                accessFromNode.WithSpan(aliasSpan);

            RegisterFromAlias(alias);

            return accessFromNode;
        }


        var baseNode = ComposeBaseTypes();
        var columnName = baseNode switch
        {
            IdentifierNode id => id.Name,
            WordNode word => word.Value,
            _ => throw new NotSupportedException($"Expected identifier or word but got {baseNode.GetType().Name}")
        };

        if (Current.TokenType == TokenType.Dot)
        {
            Consume(Current.TokenType);

            if (Current.TokenType == TokenType.Function)
            {
                var accessMethod = ComposeAccessMethod(string.Empty);
                (alias, aliasSpan) = ComposeAlias();

                var schemaName = EnsureHashPrefix(columnName);

                if (!string.IsNullOrWhiteSpace(alias))
                    RegisterFromAlias(alias);

                var fromNode = new SchemaFromNode(schemaName, accessMethod.Name, accessMethod.Arguments, alias, _fromPosition);
                if (!aliasSpan.IsEmpty)
                    fromNode.WithSpan(aliasSpan);
                return fromNode;
            }

            var properties = new List<string>();
            var anyParsed = false;

            while (Current.TokenType == TokenType.Property)
            {
                if (!anyParsed)
                    anyParsed = true;

                var propertyName = Current.Value;
                properties.Add(propertyName);

                Consume(TokenType.Property);

                if (Current.TokenType == TokenType.Dot)
                {
                    Consume(TokenType.Dot);
                    continue;
                }

                break;
            }

            if (anyParsed)
            {
                (alias, aliasSpan) = ComposeAlias();

                if (string.IsNullOrWhiteSpace(alias))
                    throw new NotSupportedException("Alias cannot be empty when parsing From clause.");

                RegisterFromAlias(alias);
                var fromNode = new PropertyFromNode(alias, columnName, properties.ToArray());
                if (!aliasSpan.IsEmpty)
                    fromNode.WithSpan(aliasSpan);
                return fromNode;
            }

            throw new NotSupportedException($"Unrecognized token {Current.TokenType} when parsing From clause.");
        }

        (alias, aliasSpan) = ComposeAlias();

        if (!string.IsNullOrWhiteSpace(alias))
            RegisterFromAlias(alias);

        var inMemoryNode = new InMemoryTableFromNode(columnName, alias);
        if (!aliasSpan.IsEmpty)
            inMemoryNode.WithSpan(aliasSpan);
        return inMemoryNode;
    }

    private void ConsumeWhiteSpaces()
    {
        while (Current.TokenType == TokenType.WhiteSpace)
            Consume(TokenType.WhiteSpace);
    }

    private WhereNode ComposeWhere(bool withoutWhereToken)
    {
        if (Current.TokenType == TokenType.Where)
        {
            Consume(TokenType.Where);
            return new WhereNode(ComposeOperations());
        }

        if (withoutWhereToken)
            return new WhereNode(ComposeOperations());

        return null;
    }

    private void Consume(TokenType tokenType)
    {
        if (!Current.TokenType.Equals(tokenType))
            throw new SyntaxException(
                $"Expected token is {tokenType} but received {Current.TokenType}.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span);

        Previous = Current;
        _hasReplacedToken = false;
        _lexer.Next();
    }


    private void ConsumeAsColumn(TokenType tokenType)
    {
        if (!Current.TokenType.Equals(tokenType))
            throw new SyntaxException(
                $"Expected token is {tokenType} but received {Current.TokenType}.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span);

        _hasReplacedToken = false;
        _lexer.NextOf(ColumnRegex,
            value => new ColumnToken(value, new TextSpan(_lexer.Position, _lexer.Position + value.Length)));
    }

    private ArgsListNode ComposeArgs()
    {
        var args = new List<Node>();

        Consume(TokenType.LeftParenthesis);

        if (Current.TokenType != TokenType.RightParenthesis)
            do
            {
                if (Current.TokenType == TokenType.Comma)
                    Consume(Current.TokenType);

                args.Add(ComposeEqualityOperators());
            } while (Current.TokenType == TokenType.Comma);

        Consume(TokenType.RightParenthesis);

        return new ArgsListNode(args.ToArray());
    }

    private ArgsListNode ComposeNonEmptyArgs(string operatorName)
    {
        var args = ComposeArgs();

        if (args.Args.Length != 0)
            return args;

        throw new SyntaxException(
            $"{operatorName} requires at least one argument inside parentheses.",
            _lexer.AlreadyResolvedQueryPart);
    }

    private (ArgsListNode Args, bool IsDistinct) ComposeArgsWithDistinct()
    {
        var args = new List<Node>();
        var isDistinct = false;

        Consume(TokenType.LeftParenthesis);


        if (Current.TokenType == TokenType.Distinct)
        {
            Consume(TokenType.Distinct);
            isDistinct = true;
        }

        if (Current.TokenType != TokenType.RightParenthesis)
            do
            {
                if (Current.TokenType == TokenType.Comma)
                    Consume(Current.TokenType);

                args.Add(ComposeEqualityOperators());
            } while (Current.TokenType == TokenType.Comma);

        Consume(TokenType.RightParenthesis);

        return (new ArgsListNode(args.ToArray()), isDistinct);
    }

    private Node ComposeBaseTypes(int minPrecedence = 0)
    {
        switch (Current.TokenType)
        {
            case TokenType.Decimal:
                var token = ConsumeAndGetToken(TokenType.Decimal);
                return new DecimalNode(token.Value, token.Span);
            case TokenType.Integer:
                return ComposeInteger();
            case TokenType.HexadecimalInteger:
                return ComposeHexInteger();
            case TokenType.BinaryInteger:
                return ComposeBinaryInteger();
            case TokenType.OctalInteger:
                return ComposeOctalInteger();
            case TokenType.Word:
            case TokenType.StringLiteral:
                return ComposeWord();
            case TokenType.Skip:
            case TokenType.Take:
                ReplaceCurrentToken(new FunctionToken(Current.Value, Current.Span));
                return ComposeAccessMethod(string.Empty);
            case TokenType.Function:
                return TryComposeWindowFunction(ComposeAccessMethod(string.Empty));
            case TokenType.Identifier:

                if (Current is not ColumnToken column)
                    throw new ArgumentNullException(
                        $"Expected token is {TokenType.Identifier} but received {Current.TokenType}");

                Consume(TokenType.Identifier);

                return new IdentifierNode(column.Value, null, column.Span);
            case TokenType.KeyAccess:
                var keyAccess = (KeyAccessToken)Current;
                Consume(TokenType.KeyAccess);
                return new AccessObjectKeyNode(keyAccess);
            case TokenType.NumericAccess:
                var numericAccess = (NumericAccessToken)Current;
                Consume(TokenType.NumericAccess);
                return new AccessObjectArrayNode(numericAccess);
            case TokenType.MethodAccess:
                var methodAccess = (MethodAccessToken)Current;
                Consume(TokenType.MethodAccess);
                Consume(TokenType.Dot);
                return TryComposeWindowFunction(ComposeAccessMethod(methodAccess.Alias));
            case TokenType.Property:
                token = ConsumeAndGetToken(TokenType.Property);
                return new PropertyValueNode(token.Value).WithSpan(token.Span);
            case TokenType.AliasedStar:
                token = ConsumeAndGetToken(TokenType.AliasedStar);
                return TryComposeStarModifiers(
                    new AllColumnsNode(token.Value.Replace(".*", string.Empty)).WithSpan(token.Span));
            case TokenType.Star:
                token = ConsumeAndGetToken(TokenType.Star);
                return TryComposeStarModifiers(
                    new AllColumnsNode().WithSpan(token.Span));
            case TokenType.True:
                token = ConsumeAndGetToken(TokenType.True);
                return new BooleanNode(true, token.Span);
            case TokenType.False:
                token = ConsumeAndGetToken(TokenType.False);
                return new BooleanNode(false, token.Span);
            case TokenType.LeftParenthesis:
                return SkipComposeSkip(TokenType.LeftParenthesis, f => f.ComposeOperations(),
                    TokenType.RightParenthesis);
            case TokenType.Hyphen:
                Consume(TokenType.Hyphen);
                return new StarNode(new IntegerNode("-1", "s"),
                    Compose(f => f.ComposeArithmeticExpression(minPrecedence)));
            case TokenType.Case:
                var (whenThenNodes, elseNode) = ComposeCase();
                return new CaseNode(whenThenNodes, elseNode);
            case TokenType.FieldLink:
                token = ConsumeAndGetToken();
                return new FieldLinkNode(token.Value).WithSpan(token.Span);
            case TokenType.Null:
                token = ConsumeAndGetToken(TokenType.Null);
                return new NullNode(token.Span);
            default:

                if (IsSchemaKeywordToken(Current.TokenType)) return ComposeSchemaTokenAsWord();
                break;
        }

        throw new NotSupportedException(
            $"Token {Current.Value}({Current.TokenType}) at position {Current.Span.Start} cannot be used here.");
    }

    private Node TryComposeStarModifiers(Node node)
    {
        if (node is not AllColumnsNode allColumns)
            return node;

        if (!IsStarModifierStart())
        {
            ThrowIfNearMissStarModifier();
            return node;
        }

        string likePattern = null;
        var isNotLike = false;
        string[] excludeColumns = null;
        StarReplaceItemNode[] replaceItems = null;

        if (Current.TokenType is TokenType.Like or TokenType.NotLike)
        {
            isNotLike = Current.TokenType == TokenType.NotLike;
            Consume(Current.TokenType);

            if (Current.TokenType != TokenType.Word && Current.TokenType != TokenType.StringLiteral)
                throw new SyntaxException(
                    "Expected a string pattern after LIKE in star expression.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2003_InvalidExpression,
                    Current.Span);

            likePattern = Current.Value;
            Consume(Current.TokenType);
        }

        if (IsContextSensitiveKeyword("exclude"))
        {
            Consume(Current.TokenType);
            excludeColumns = ComposeExcludeList();
        }

        if (IsContextSensitiveKeyword("replace"))
        {
            Consume(Current.TokenType);
            replaceItems = ComposeReplaceList();
        }

        if (IsStarModifierStart())
            throw new SyntaxException(
                "Duplicate or out-of-order star modifier. Expected order: LIKE/NOT LIKE, EXCLUDE, REPLACE.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2030_UnsupportedSyntax,
                Current.Span);

        return new AllColumnsNode(
            allColumns.Alias,
            likePattern,
            isNotLike,
            excludeColumns,
            replaceItems).WithSpan(allColumns.Span);
    }

    private bool IsStarModifierStart()
    {
        return Current.TokenType is TokenType.Like or TokenType.NotLike
               || IsContextSensitiveKeyword("exclude")
               || IsContextSensitiveKeyword("replace");
    }

    private bool IsContextSensitiveKeyword(string keyword)
    {
        return Current.TokenType == TokenType.Identifier
               && string.Equals(Current.Value, keyword, StringComparison.OrdinalIgnoreCase);
    }

    private void ThrowIfNearMissStarModifier()
    {
        if (Current.TokenType != TokenType.Identifier)
            return;

        var value = Current.Value;
        if (IsNearMiss(value, "exclude") || IsNearMiss(value, "replace"))
            throw new SyntaxException(
                $"Unknown modifier '{value}' after star expression. Did you mean EXCLUDE or REPLACE?",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span);
    }

    private static bool IsNearMiss(string input, string target)
    {
        if (string.Equals(input, target, StringComparison.OrdinalIgnoreCase))
            return false;

        if (input.Length < 3)
            return false;

        if (target.StartsWith(input, StringComparison.OrdinalIgnoreCase) ||
            input.StartsWith(target, StringComparison.OrdinalIgnoreCase))
            return true;

        var distance = LevenshteinDistance(input.ToLowerInvariant(), target.ToLowerInvariant());
        return distance <= 2;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        var m = a.Length;
        var n = b.Length;
        var dp = new int[m + 1, n + 1];

        for (var i = 0; i <= m; i++) dp[i, 0] = i;
        for (var j = 0; j <= n; j++) dp[0, j] = j;

        for (var i = 1; i <= m; i++)
        for (var j = 1; j <= n; j++)
        {
            var cost = a[i - 1] == b[j - 1] ? 0 : 1;
            dp[i, j] = Math.Min(
                Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                dp[i - 1, j - 1] + cost);
        }

        return dp[m, n];
    }

    private string[] ComposeExcludeList()
    {
        if (Current.TokenType != TokenType.LeftParenthesis)
            throw new SyntaxException(
                $"EXCLUDE requires a parenthesized column list. Expected '(' but found '{Current.Value}'. Usage: EXCLUDE (Column1, Column2).",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span);

        Consume(TokenType.LeftParenthesis);

        if (Current.TokenType == TokenType.RightParenthesis)
            throw new SyntaxException(
                "EXCLUDE list must contain at least one column name. Usage: EXCLUDE (Column1, Column2).",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2003_InvalidExpression,
                Current.Span);

        var columns = new List<string>();

        do
        {
            if (Current.TokenType == TokenType.Comma)
                Consume(TokenType.Comma);

            var columnName = ConsumeColumnIdentifier();
            columns.Add(columnName);
        } while (Current.TokenType == TokenType.Comma);

        if (Current.TokenType != TokenType.RightParenthesis)
            throw new SyntaxException(
                $"Expected ')' to close EXCLUDE list but found '{Current.Value}'. Check for missing commas between column names.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span);

        Consume(TokenType.RightParenthesis);

        return columns.ToArray();
    }

    private StarReplaceItemNode[] ComposeReplaceList()
    {
        if (Current.TokenType != TokenType.LeftParenthesis)
            throw new SyntaxException(
                $"REPLACE requires a parenthesized list. Expected '(' but found '{Current.Value}'. Usage: REPLACE (expression AS Column).",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span);

        Consume(TokenType.LeftParenthesis);

        if (Current.TokenType == TokenType.RightParenthesis)
            throw new SyntaxException(
                "REPLACE list must contain at least one replacement. Usage: REPLACE (expression AS Column).",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2003_InvalidExpression,
                Current.Span);

        var items = new List<StarReplaceItemNode>();

        do
        {
            if (Current.TokenType == TokenType.Comma)
                Consume(TokenType.Comma);

            var expression = ComposeOperations();

            if (Current.TokenType != TokenType.As)
                throw new SyntaxException(
                    $"Expected AS keyword after expression in REPLACE item but found '{Current.Value}'. Usage: REPLACE (expression AS ColumnName).",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2001_UnexpectedToken,
                    Current.Span);

            Consume(TokenType.As);

            var columnName = ConsumeColumnIdentifier();

            items.Add(new StarReplaceItemNode(expression, columnName));
        } while (Current.TokenType == TokenType.Comma);

        if (Current.TokenType != TokenType.RightParenthesis)
            throw new SyntaxException(
                $"Expected ')' to close REPLACE list but found '{Current.Value}'. Check for missing commas between items.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span);

        Consume(TokenType.RightParenthesis);

        return items.ToArray();
    }

    private string ConsumeColumnIdentifier()
    {
        if (Current.TokenType is TokenType.Identifier or TokenType.Word)
        {
            var name = Current.Value;
            Consume(Current.TokenType);
            return name;
        }

        if (IsSqlKeywordToken(Current.TokenType))
        {
            var name = Current.Value;
            Consume(Current.TokenType);
            return name;
        }

        throw new SyntaxException(
            $"Expected a column name but found '{Current.Value}'.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2001_UnexpectedToken,
            Current.Span);
    }

    private ((Node When, Node Then)[] WhenThenNodes, Node ElseNode) ComposeCase()
    {
        Consume(TokenType.Case);

        Node? subjectExpression = null;
        if (Current.TokenType != TokenType.When)
            subjectExpression = ComposeArithmeticExpression(0);

        var whenThenNodes = new List<(Node When, Node Then)>();

        while (Current.TokenType == TokenType.When)
        {
            Consume(TokenType.When);
            Node whenNode;
            if (subjectExpression != null)
                whenNode = new EqualityNode(subjectExpression, ComposeArithmeticExpression(0));
            else
                whenNode = ComposeOperations();
            Consume(TokenType.Then);
            var thenNode = ComposeEqualityOperators();

            whenThenNodes.Add((
                new WhenNode(whenNode),
                new ThenNode(thenNode)));
        }

        Consume(TokenType.Else);
        var elseNode = ComposeEqualityOperators();
        Consume(TokenType.End);

        return (whenThenNodes.ToArray(), new ElseNode(elseNode));
    }

    private IntegerNode ComposeInteger()
    {
        var token = (IntegerToken)ConsumeAndGetToken(TokenType.Integer);
        return new IntegerNode(token.Value, token.Abbreviation, token.Span);
    }

    private HexIntegerNode ComposeHexInteger()
    {
        var token = (HexIntegerToken)ConsumeAndGetToken(TokenType.HexadecimalInteger);
        return new HexIntegerNode(token.Value, token.Span);
    }

    private BinaryIntegerNode ComposeBinaryInteger()
    {
        var token = (BinaryIntegerToken)ConsumeAndGetToken(TokenType.BinaryInteger);
        return new BinaryIntegerNode(token.Value, token.Span);
    }

    private OctalIntegerNode ComposeOctalInteger()
    {
        var token = (OctalIntegerToken)ConsumeAndGetToken(TokenType.OctalInteger);
        return new OctalIntegerNode(token.Value, token.Span);
    }

    private WordNode ComposeWord()
    {
        var tokenType = Current.TokenType;

        var token = tokenType switch
        {
            TokenType.Word => ConsumeAndGetToken(TokenType.Word),
            TokenType.StringLiteral => ConsumeAndGetToken(TokenType.StringLiteral),
            _ => throw new NotSupportedException($"Expected Word or StringLiteral but got {tokenType}")
        };
        return new WordNode(token.Value, token.Span);
    }

    private WordNode ComposeSchemaTokenAsWord()
    {
        var token = ConsumeAndGetToken();
        return new WordNode(token.Value, token.Span);
    }

    private static bool IsSchemaKeywordToken(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.Binary or TokenType.Text or
                TokenType.LittleEndian or TokenType.BigEndian or
                TokenType.ByteType or TokenType.SByteType or
                TokenType.ShortType or TokenType.UShortType or
                TokenType.IntType or TokenType.UIntType or
                TokenType.LongType or TokenType.ULongType or
                TokenType.FloatType or TokenType.DoubleType or
                TokenType.BitsType or TokenType.Align or
                TokenType.StringType or TokenType.Utf8 or
                TokenType.Utf16Le or TokenType.Utf16Be or
                TokenType.Ascii or TokenType.Latin1 or TokenType.Ebcdic or
                TokenType.Trim or TokenType.RTrim or TokenType.LTrim or
                TokenType.NullTerm or TokenType.Check or
                TokenType.At or TokenType.Colon or
                TokenType.Pattern or TokenType.Literal or
                TokenType.Until or TokenType.Between or
                TokenType.Chars or TokenType.Token or
                TokenType.Rest or TokenType.Whitespace or
                TokenType.Optional or TokenType.Repeat or
                TokenType.Switch or TokenType.Nested or
                TokenType.Escaped or TokenType.Greedy or TokenType.Lazy or
                TokenType.Lower or TokenType.Upper or
                TokenType.Capture or TokenType.Extends or TokenType.End => true,
            _ => false
        };
    }

    private static bool IsSqlKeywordToken(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.And or TokenType.Or or TokenType.Not or
                TokenType.Where or TokenType.Select or TokenType.From or
                TokenType.Like or TokenType.NotLike or TokenType.RLike or TokenType.NotRLike or
                TokenType.As or TokenType.Is or TokenType.Null or
                TokenType.Union or TokenType.UnionAll or TokenType.Except or TokenType.Intersect or
                TokenType.GroupBy or TokenType.Having or TokenType.Contains or
                TokenType.Skip or TokenType.Take or TokenType.With or
                TokenType.InnerJoin or TokenType.OuterJoin or TokenType.CrossApply or TokenType.OuterApply or
                TokenType.On or TokenType.OrderBy or TokenType.Asc or TokenType.Desc or
                TokenType.Functions or TokenType.True or TokenType.False or
                TokenType.In or TokenType.NotIn or TokenType.Table or TokenType.Couple or
                TokenType.Case or TokenType.When or TokenType.Then or TokenType.Else or
                TokenType.Distinct or TokenType.ColumnKeyword or TokenType.Between => true,
            _ => false
        };
    }

    private AccessMethodNode ComposeAccessMethod(string alias)
    {
        ArgsListNode args;
        bool isDistinct;

        if (Current is FunctionToken func)
        {
            Consume(TokenType.Function);
            (args, isDistinct) = ComposeArgsWithDistinct();
            return new AccessMethodNode(func, args, null, false, null, alias, isDistinct);
        }

        if (Current is MethodAccessToken)
        {
            Consume(TokenType.MethodAccess);
            Consume(TokenType.Dot);
            var token = (FunctionToken)ConsumeAndGetToken(TokenType.Function);
            (args, isDistinct) = ComposeArgsWithDistinct();

            return new AccessMethodNode(token, args, null, false,
                null, alias, default, isDistinct);
        }

        throw new NotSupportedException(
            $"Unrecognized token for ComposeAccessMethod(), the token was {Current.TokenType}");
    }

    private Token ConsumeAndGetToken(TokenType expected)
    {
        var token = Current;
        Consume(expected);
        return token;
    }

    private Token ConsumeAndGetToken()
    {
        return ConsumeAndGetToken(Current.TokenType);
    }

    private TNode SkipComposeSkip<TNode>(TokenType pType, Func<Parser, TNode> parserAction, TokenType aType)
    {
        Consume(pType);
        return ComposeAndSkip(parserAction, aType);
    }

    private TNode ComposeAndSkip<TNode>(Func<Parser, TNode> parserAction, TokenType type)
    {
        var node = Compose(parserAction);
        Consume(type);
        return node;
    }

    private TNode ComposeAndSkipIfPresent<TNode>(Func<Parser, TNode> parserAction, TokenType type)
    {
        var node = Compose(parserAction);
        if (Current.TokenType == type)
            Consume(type);

        return node;
    }

    private TNode Compose<TNode>(Func<Parser, TNode> parserAction)
    {
        if (parserAction == null)
            throw new ArgumentNullException(nameof(parserAction));

        var node = parserAction(this);
        return node;
    }

    private static bool IsArithmeticBinaryOperator(Token currentToken)
    {
        return currentToken.TokenType is TokenType.Star or TokenType.FSlash or TokenType.Mod or TokenType.Plus
            or TokenType.Hyphen or TokenType.Dot or TokenType.Ampersand or TokenType.Pipe or TokenType.Caret
            or TokenType.LeftShift or TokenType.RightShift;
    }

    private static bool IsEqualityOperator(Token currentToken)
    {
        return currentToken.TokenType is TokenType.Greater or TokenType.GreaterEqual or TokenType.Less
            or TokenType.LessEqual or TokenType.Equality or TokenType.Not or TokenType.Diff or TokenType.Like
            or TokenType.NotLike or TokenType.Contains or TokenType.Is or TokenType.In or TokenType.NotIn
            or TokenType.RLike or TokenType.NotRLike or TokenType.Between;
    }

    private static bool IsQueryOperator(Token currentToken)
    {
        return currentToken.TokenType is TokenType.And or TokenType.Or;
    }

    private static bool IsNumericToken(Token current)
    {
        return current.TokenType is TokenType.Decimal or TokenType.Integer or TokenType.HexadecimalInteger
            or TokenType.BinaryInteger or TokenType.OctalInteger;
    }

    private void PushFromAliasesScope()
    {
        _fromAliasesStack.Push(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private void PopFromAliasesScope()
    {
        _fromAliasesStack.Pop();
    }

    private void RegisterFromAlias(string alias)
    {
        if (_fromAliasesStack.Count > 0 && !string.IsNullOrEmpty(alias))
            _fromAliasesStack.Peek().Add(alias);
    }

    private bool IsKnownFromAlias(string alias)
    {
        foreach (var scope in _fromAliasesStack)
            if (scope.Contains(alias))
                return true;
        return false;
    }

    private enum Associativity
    {
        Left,
        Right
    }
}
