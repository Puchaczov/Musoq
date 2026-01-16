using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public class Parser
{
    private readonly Lexer _lexer;
    
    private static readonly Regex ColumnRegex = new(Lexer.TokenRegexDefinition.KColumn, RegexOptions.Compiled);
    
    public Parser(Lexer lexer)
    {
        _lexer = lexer ?? throw new ArgumentNullException(nameof(lexer), "Lexer cannot be null. Please provide a valid lexer instance.");
    }

    private static readonly TokenType[] SetOperators =
        [TokenType.Union, TokenType.UnionAll, TokenType.Except, TokenType.Intersect];

    private bool _hasReplacedToken;
    private Token _replacedToken;
    private Token _previousToken;
    private int _fromPosition;
    private readonly Stack<HashSet<string>> _fromAliasesStack = new();

    private readonly Dictionary<TokenType, (short Precendence, Associativity Associativity)> _precedenceDictionary =
        new()
        {
            {TokenType.Plus, (1, Associativity.Left)},
            {TokenType.Hyphen, (1, Associativity.Left)},
            {TokenType.Star, (2, Associativity.Left)},
            {TokenType.FSlash, (2, Associativity.Left)},
            {TokenType.Mod, (2, Associativity.Left)},
            {TokenType.Dot, (3, Associativity.Left)}
        };

    private Token Current => _hasReplacedToken ? _replacedToken : _lexer.Current();
        
    private Token Previous => _previousToken;

    private void ReplaceCurrentToken(Token newToken)
    {
        _replacedToken = newToken;
        _hasReplacedToken = true;
    }

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
                    throw new SyntaxException("Failed to compose statement. The SQL query structure is invalid.", _lexer.AlreadyResolvedQueryPart);
                
                statements.Add(statement);
            }

            return new RootNode(new StatementsArrayNode(statements.ToArray()));
        }
        catch (Exception ex) when (!(ex is SyntaxException))
        {
            throw new SyntaxException($"An error occurred while parsing the SQL query: {ex.Message}", _lexer.AlreadyResolvedQueryPart, ex);
        }
    }

    private StatementNode ComposeStatement()
    {
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

            default:
                throw new SyntaxException($"Cannot compose statement, {Current.TokenType} is not expected here", _lexer.AlreadyResolvedQueryPart);
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
                var accessMethod = ComposeAccessMethod(sourceAlias);
                
                return new DescNode(new SchemaFromNode(schemaName, accessMethod.Name, accessMethod.Arguments, string.Empty, 1), DescForType.FunctionsForSchema);
            }
            
            var schemaNameForFunctions = ComposeSchemaName();
            var schemaToken = Current;
            
            if (Current.TokenType == TokenType.Dot)
            {
                Consume(TokenType.Dot);
                
                if (Current is FunctionToken)
                {
                    var accessMethod = ComposeAccessMethod(string.Empty);
                    return new DescNode(new SchemaFromNode(schemaNameForFunctions, accessMethod.Name, accessMethod.Arguments, string.Empty, 1), DescForType.FunctionsForSchema);
                }
                else
                {
                    ConsumeAndGetToken(TokenType.Property);
                }
            }
            
            return new DescNode(new SchemaFromNode(schemaNameForFunctions, string.Empty, ArgsListNode.Empty, string.Empty, schemaToken.Span.Start), DescForType.FunctionsForSchema);
        }

        if (Current.TokenType == TokenType.MethodAccess)
        {
            var sourceAlias = Current.Value;
            var schemaName = EnsureHashPrefix(sourceAlias);
            var accessMethod = ComposeAccessMethod(sourceAlias);
            
            return new DescNode(new SchemaFromNode(schemaName, accessMethod.Name, accessMethod.Arguments, string.Empty, 1), DescForType.SpecificConstructor);
        }

        var name = ComposeSchemaName();
        var startToken = Current;

        if(Current.TokenType == TokenType.Dot)
        {
            Consume(TokenType.Dot);

            FromNode fromNode;
            if (Current is FunctionToken)
            {
                var accessMethod = ComposeAccessMethod(string.Empty);

                fromNode = new SchemaFromNode(name, accessMethod.Name, accessMethod.Arguments, string.Empty, 1);
                return new DescNode(fromNode, DescForType.SpecificConstructor);
            }

            var methodName = new WordNode(ConsumeAndGetToken(TokenType.Property).Value);

            fromNode = new SchemaFromNode(name, methodName.Value, ArgsListNode.Empty, string.Empty, 1);
            return new DescNode(fromNode, DescForType.Constructors);
        }
        else
        {
            return new DescNode(new SchemaFromNode(name, string.Empty, ArgsListNode.Empty, string.Empty, startToken.Span.Start), DescForType.Schema);
        }
    }
    
    /// <summary>
    /// Composes a schema name from the current token, handling both hash (#schema) and 
    /// hash-optional (schema) syntax. Always returns the name with the hash prefix.
    /// </summary>
    private string ComposeSchemaName()
    {
        if (Current.TokenType == TokenType.Word)
            return EnsureHashPrefix(ComposeWord().Value);
        
        if (Current.TokenType == TokenType.Identifier)
        {
            var identifier = ConsumeAndGetToken(TokenType.Identifier).Value;
            return EnsureHashPrefix(identifier);
        }
        
        throw new SyntaxException($"Expected schema name (Word or Identifier) but received {Current.TokenType}", _lexer.AlreadyResolvedQueryPart);
    }
    
    /// <summary>
    /// Ensures the schema name has the hash prefix (#).
    /// </summary>
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
            var typeName = Current.Value;
            Consume(TokenType.Word);

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

        var expressions = new List<CteInnerExpressionNode>();

        if (ComposeBaseTypes() is not IdentifierNode col)
        {
            throw new SyntaxException($"Expected token is {TokenType.Identifier} but received {Current.TokenType}", _lexer.AlreadyResolvedQueryPart);
        }
            
        Consume(TokenType.As);
        Consume(TokenType.LeftParenthesis);
        var innerSets = ComposeSetOperators(0);
        expressions.Add(new CteInnerExpressionNode(innerSets, col.Name));
        Consume(TokenType.RightParenthesis);

        while (Current.TokenType == TokenType.Comma)
        {
            Consume(TokenType.Comma);

            col = ComposeBaseTypes() as IdentifierNode;

            if (col is null)
            {
                throw new SyntaxException($"Expected token is {TokenType.Identifier} but received {Current.TokenType}", _lexer.AlreadyResolvedQueryPart);
            }
                
            Consume(TokenType.As);

            Consume(TokenType.LeftParenthesis);
            innerSets = ComposeSetOperators(0);
            Consume(TokenType.RightParenthesis);
            expressions.Add(new CteInnerExpressionNode(innerSets, col.Name));
        }

        var outerSets = ComposeSetOperators(0);

        return new CteExpressionNode(expressions.ToArray(), outerSets);
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
        
        var value = Current.Value;
        Consume(Current.TokenType);
        if (Current.TokenType == TokenType.Dot)
        {
            Consume(Current.TokenType);
            value = $"{value}.{Current.Value}";
            Consume(Current.TokenType);
        }
        keys.Add(value);
        while (Current.TokenType == TokenType.Comma)
        {
            Consume(TokenType.Comma);
            value = Current.Value;
            Consume(Current.TokenType);
            if (Current.TokenType == TokenType.Dot)
            {
                Consume(Current.TokenType);
                value = $"{value}.{Current.Value}";
                Consume(Current.TokenType);
            }
            keys.Add(value);
        }

        Consume(TokenType.RightParenthesis);

        return keys.ToArray();
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
            throw new NotSupportedException($"Cannot recognize if query is regular or reordered.");
        return query;
    }

    private QueryNode ComposeRegularQuery()
    {
        _fromPosition += 1;
        var selectNode = ComposeSelectNode();
        PushFromAliasesScope();
        try
        {
            var fromNode = ComposeFrom();

            fromNode = ComposeJoinOrApply(fromNode);

            var whereNode = ComposeWhere(false);
            var groupBy = ComposeGroupByNode();
            var orderBy = ComposeOrderBy();
            var skip = ComposeSkip();
            var take = ComposeTake();
            return new QueryNode(selectNode, fromNode, whereNode, groupBy, orderBy, skip, take);
        }
        finally
        {
            PopFromAliasesScope();
        }
    }

    private QueryNode ComposeReorderedQuery()
    {
        _fromPosition += 1;
        PushFromAliasesScope();
        try
        {
            var fromNode = ComposeFrom();
            fromNode = ComposeJoinOrApply(fromNode);
            var whereNode = ComposeWhere(false);
            var groupBy = ComposeGroupByNode();
            var selectNode = ComposeSelectNode();
            var orderBy = ComposeOrderBy();
            var skip = ComposeSkip();
            var take = ComposeTake();
            return new QueryNode(selectNode, fromNode, whereNode, groupBy, orderBy, skip, take);
        }
        finally
        {
            PopFromAliasesScope();
        }
    }

    private void PushFromAliasesScope()
    {
        _fromAliasesStack.Push(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private void PopFromAliasesScope()
    {
        if (_fromAliasesStack.Count > 0)
            _fromAliasesStack.Pop();
    }

    private void RegisterFromAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return;

        if (_fromAliasesStack.Count == 0)
            return;

        _fromAliasesStack.Peek().Add(alias);
    }

    private bool IsKnownFromAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias) || _fromAliasesStack.Count == 0)
            return false;

        return _fromAliasesStack.Peek().Contains(alias);
    }

    private FromNode ComposeJoinOrApply(FromNode from)
    {
        if (!IsJoinOrApplyToken(Current.TokenType)) return new ExpressionFromNode(from);

        while (IsJoinOrApplyToken(Current.TokenType))
        {
            switch (Current.TokenType)
            {
                case TokenType.InnerJoin:
                    Consume(TokenType.InnerJoin);
                    from = new JoinFromNode(from,
                        ComposeAndSkip(parser => parser.ComposeFrom(false, false), TokenType.On), 
                        ComposeOperations(),
                        JoinType.Inner);
                    break;
                case TokenType.OuterJoin:
                    var outerToken = (OuterJoinToken) Current;
                    Consume(TokenType.OuterJoin);
                    from = new JoinFromNode(from,
                        ComposeAndSkip(parser => parser.ComposeFrom(false, false), TokenType.On), 
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
            }
        }

        if (from is JoinFromNode joinFrom)
        {
            from = new JoinNode(joinFrom);
        }

        if (from is ApplyFromNode applyFrom)
        {
            from = new ApplyNode(applyFrom);
        }
            
        return new ExpressionFromNode(from);
    }

    private static bool IsJoinOrApplyToken(TokenType currentTokenType)
    {
        return currentTokenType is TokenType.InnerJoin or TokenType.OuterJoin or TokenType.CrossApply or TokenType.OuterApply;
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
            return new TakeNode(ComposeInteger());
        }

        return null;
    }

    private SkipNode ComposeSkip()
    {
        if (Current.TokenType == TokenType.Skip)
        {
            Consume(TokenType.Skip);
            return new SkipNode(ComposeInteger());
        }

        return null;
    }

    private GroupByNode ComposeGroupByNode()
    {
        if (Current.TokenType != TokenType.GroupBy) return null;
            
        Consume(TokenType.GroupBy);
                
        if (Current.TokenType == TokenType.Comma)
        {
            throw new SyntaxException("Unnecessary comma found after GROUP BY clause.", _lexer.AlreadyResolvedQueryPart);
        }

        var fields = ComposeFields();

        if (Previous.TokenType == TokenType.Comma && Current.TokenType == TokenType.EndOfFile)
        {
            throw new SyntaxException("Unnecessary comma found after GROUP BY clause.", _lexer.AlreadyResolvedQueryPart);
        }
                
        if (fields.Length == 0) throw new NotSupportedException("Group by clause does not have any fields.");
        if (Current.TokenType != TokenType.Having) return new GroupByNode(fields, null);

        Consume(TokenType.Having);
                
        var having = new HavingNode(ComposeOperations());

        return new GroupByNode(fields, having);

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
        {
            throw new SyntaxException("Unnecessary comma found after SELECT keyword.", _lexer.AlreadyResolvedQueryPart);
        }

        var fields = ComposeFields();
            
        if (Previous.TokenType == TokenType.Comma && Current.TokenType == TokenType.From)
        {
            throw new SyntaxException("Unnecessary comma found at the end of SELECT clause.", _lexer.AlreadyResolvedQueryPart);
        }

        return new SelectNode(fields, isDistinct);
    }

    private FieldNode[] ComposeFields()
    {
        var fields = new List<FieldNode>();
        var i = 0;

        do
        {
            if (Current.TokenType == TokenType.From)
            {
                break;
            }
                
            if (Current.TokenType == TokenType.EndOfFile)
            {
                break;
            }
                
            fields.Add(ConsumeField(i++));
        } while (!IsSetOperator(Current.TokenType) && Current.TokenType != TokenType.RightParenthesis &&
                 Current.TokenType != TokenType.From && Current.TokenType != TokenType.Having &&
                 Current.TokenType != TokenType.Skip && Current.TokenType != TokenType.Take &&
                 Current.TokenType != TokenType.Select &&
                 Current.TokenType != TokenType.OrderBy &&
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
        var alias = ComposeAlias();
        return new FieldNode(fieldExpression, order, alias);
    }

    private FieldOrderedNode ConsumeFieldOrdered(int level)
    {
        var fieldExpression = ComposeOperations();
        var order = ComposeOrder();
        return new FieldOrderedNode(fieldExpression, level, string.Empty, order);
    }

    private string ComposeAlias()
    {
        switch (Current.TokenType)
        {
            case TokenType.As:
                Consume(TokenType.As);
                var name = Current.Value;
                Consume(Current.TokenType);
                return name;
            case TokenType.Word:
                return ConsumeAndGetToken(TokenType.Word).Value;
            case TokenType.Identifier:
                return ConsumeAndGetToken(TokenType.Identifier).Value;
        }

        return string.Empty;
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
                throw new NotSupportedException($"Unrecognized token for ComposeOrder(), the token was {Current.TokenType}");
        }
    }

    private Node ComposeOperations()
    {
        var node = ComposeEqualityOperators();

        while (IsQueryOperator(Current))
        {
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
                    throw new NotSupportedException($"Unrecognized token for ComposeOperations(), the token was {Current.TokenType}");
            }
        }
            
        return node;
    }

    private Node ComposeArithmeticExpression(int minPrecedence)
    {
        var left = ComposeBaseTypes(minPrecedence);

        if (IsNumericToken(Current))
        {
            left = new AddNode(left, ComposeBaseTypes(minPrecedence));
        }

        while (IsArithmeticBinaryOperator(Current) && _precedenceDictionary[Current.TokenType].Precendence >= minPrecedence)
        {
            var curr = Current;
            var op = _precedenceDictionary[Current.TokenType];
            var nextMinPrecedence = op.Associativity == Associativity.Left ? op.Precendence + 1 : op.Precendence;
            Consume(Current.TokenType);
            var right = ComposeArithmeticExpression(nextMinPrecedence);

            left = curr.TokenType switch
            {
                TokenType.Plus => new AddNode(left, right),
                TokenType.Hyphen => new HyphenNode(left, right),
                TokenType.Star => new StarNode(left, right),
                TokenType.FSlash => new FSlashNode(left, right),
                TokenType.Mod => new ModuloNode(left, right),
                TokenType.Dot => new DotNode(left, right, string.Empty),
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
                    node = new ContainsNode(node, ComposeArgs());
                    break;
                case TokenType.Is:
                    Consume(TokenType.Is);
                    node = Current.TokenType == TokenType.Not ? 
                        SkipComposeSkip(TokenType.Not, _ => new IsNullNode(node, true), TokenType.Null) : 
                        ComposeAndSkip(parser => new IsNullNode(node, false), TokenType.Null);
                    break;
                case TokenType.In:
                    Consume(TokenType.In);
                    node = new InNode(node, ComposeArgs());
                    break;
                case TokenType.NotIn:
                    Consume(TokenType.NotIn);
                    node = new NotNode(new InNode(node, ComposeArgs()));
                    break;
                default:
                    throw new NotSupportedException($"Unrecognized token for ComposeEqualityOperators(), the token was {Current.TokenType}");
            }

        return node;
    }

    private SchemaMethodFromNode ComposeSchemaMethod()
    {
        if (Current.TokenType == TokenType.MethodAccess)
        {
            var sourceAlias = Current.Value;
            var schemaName = EnsureHashPrefix(sourceAlias);
            var accessMethod = ComposeAccessMethod(sourceAlias);
            var alias = ComposeAlias();
            
            return new SchemaMethodFromNode(alias, schemaName, accessMethod.Name);
        }
        
        var schemaNode = ComposeSchemaName();
        ConsumeAsColumn(TokenType.Dot);
        var identifier = (IdentifierNode)ComposeBaseTypes();
        var composeAlias = ComposeAlias();

        return new SchemaMethodFromNode(composeAlias, schemaNode, identifier.Name);
    }

    private FromNode ComposeFrom(bool fromKeywordBefore = true, bool isApplyContext = false)
    {
        if (fromKeywordBefore)
            Consume(TokenType.From);

        string alias;
        if (Current.TokenType == TokenType.Word)
        {
            var name = ComposeWord();

            FromNode fromNode;
            if(Current.TokenType == TokenType.Dot)
            {
                Consume(TokenType.Dot);
                var accessMethod = ComposeAccessMethod(string.Empty);

                alias = ComposeAlias();

                var schemaName = EnsureHashPrefix(name.Value);
                fromNode = new SchemaFromNode(schemaName, accessMethod.Name, accessMethod.Arguments, alias, _fromPosition);
            }
            else
            {
                alias = ComposeAlias();
                fromNode = new ReferentialFromNode(name.Value, alias);
            }

            RegisterFromAlias(alias);
            return fromNode;
        }

        if (Current.TokenType == TokenType.Function)
        {
            var method = ComposeAccessMethod(string.Empty);
            alias = ComposeAlias();

            RegisterFromAlias(alias);
            return new AliasedFromNode(method.Name, method.Arguments, alias, _fromPosition);
        }

        if (Current.TokenType == TokenType.MethodAccess)
        {
            var sourceAlias = Current.Value;
            var accessMethod = ComposeAccessMethod(sourceAlias);
            alias = ComposeAlias();
            
            var canTreatAsSchema = !sourceAlias.StartsWith('#') && (!isApplyContext || !IsKnownFromAlias(sourceAlias));
            if (canTreatAsSchema)
            {
                var schemaName = EnsureHashPrefix(sourceAlias);
                var schemaFromNode = new SchemaFromNode(schemaName, accessMethod.Name, accessMethod.Arguments, alias, _fromPosition);
                RegisterFromAlias(alias);
                return schemaFromNode;
            }
            
            if (string.IsNullOrWhiteSpace(alias))
                throw new NotSupportedException("Alias cannot be empty when parsing From clause.");
                    
            var fromNode = new AccessMethodFromNode(alias, sourceAlias, accessMethod);

            RegisterFromAlias(alias);

            return fromNode;
        }
            
        var column = (IdentifierNode) ComposeBaseTypes();

        if (Current.TokenType == TokenType.Dot)
        {
            Consume(Current.TokenType);
            
            if (Current.TokenType == TokenType.Function)
            {
                var accessMethod = ComposeAccessMethod(string.Empty);
                alias = ComposeAlias();
                
                var schemaName = EnsureHashPrefix(column.Name);
                var schemaFromNode = new SchemaFromNode(schemaName, accessMethod.Name, accessMethod.Arguments, alias, _fromPosition);
                RegisterFromAlias(alias);
                return schemaFromNode;
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
                alias = ComposeAlias();
            
                if (string.IsNullOrWhiteSpace(alias))
                    throw new NotSupportedException("Alias cannot be empty when parsing From clause.");
                
                var propertyFromNode = new PropertyFromNode(alias, column.Name, properties.ToArray());
                RegisterFromAlias(alias);
                return propertyFromNode;
            }
                
            throw new NotSupportedException($"Unrecognized token {Current.TokenType} when parsing From clause.");
        }
            
        alias = ComposeAlias();
        var inMemoryFromNode = new InMemoryTableFromNode(column.Name, alias);
        RegisterFromAlias(alias);
        return inMemoryFromNode;
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
            throw new SyntaxException($"Expected token is {tokenType} but received {Current.TokenType}.", _lexer.AlreadyResolvedQueryPart);
            
        _previousToken = Current;
        _hasReplacedToken = false;
        _lexer.Next();
    }
        
    private void ConsumeAsColumn(TokenType tokenType)
    {
        if (!Current.TokenType.Equals(tokenType))
            throw new SyntaxException($"Expected token is {tokenType} but received {Current.TokenType}.", _lexer.AlreadyResolvedQueryPart);
            
        _hasReplacedToken = false;
        _lexer.NextOf(ColumnRegex, value => new ColumnToken(value, new TextSpan(_lexer.Position, _lexer.Position + value.Length)));
    }

    private ArgsListNode ComposeArgs()
    {
        var args = new List<Node>();

        Consume(TokenType.LeftParenthesis);

        if (Current.TokenType != TokenType.RightParenthesis)
        {
            do
            {
                if (Current.TokenType == TokenType.Comma)
                    Consume(Current.TokenType);

                args.Add(ComposeEqualityOperators());
            } while (Current.TokenType == TokenType.Comma);
        }

        Consume(TokenType.RightParenthesis);

        return new ArgsListNode(args.ToArray());
    }

    private Node ComposeBaseTypes(int minPrecedence = 0)
    {
        switch (Current.TokenType)
        {
            case TokenType.Decimal:
                var token = ConsumeAndGetToken(TokenType.Decimal);
                return new DecimalNode(token.Value);
            case TokenType.Integer:
                return ComposeInteger();
            case TokenType.HexadecimalInteger:
                return ComposeHexInteger();
            case TokenType.BinaryInteger:
                return ComposeBinaryInteger();
            case TokenType.OctalInteger:
                return ComposeOctalInteger();
            case TokenType.Word:
                return ComposeWord();
            case TokenType.Skip:
            case TokenType.Take:
                ReplaceCurrentToken(new FunctionToken(Current.Value, Current.Span));
                return ComposeAccessMethod(string.Empty);
            case TokenType.Function:
                return ComposeAccessMethod(string.Empty);
            case TokenType.Identifier:

                if (Current is not ColumnToken column)
                    throw new ArgumentNullException($"Expected token is {TokenType.Identifier} but received {Current.TokenType}");

                Consume(TokenType.Identifier);

                return new IdentifierNode(column.Value);
            case TokenType.KeyAccess:
                var keyAccess = (KeyAccessToken) Current;
                Consume(TokenType.KeyAccess);
                return new AccessObjectKeyNode(keyAccess);
            case TokenType.NumericAccess:
                var numericAccess = (NumericAccessToken) Current;
                Consume(TokenType.NumericAccess);
                return new AccessObjectArrayNode(numericAccess);
            case TokenType.MethodAccess:
                var methodAccess = (MethodAccessToken) Current;
                Consume(TokenType.MethodAccess);
                Consume(TokenType.Dot);
                return ComposeAccessMethod(methodAccess.Alias);
            case TokenType.Property:
                token = ConsumeAndGetToken(TokenType.Property);
                return new PropertyValueNode(token.Value);
            case TokenType.AliasedStar:
                token = ConsumeAndGetToken(TokenType.AliasedStar);
                return new AllColumnsNode(token.Value.Replace(".*", string.Empty));
            case TokenType.Star:
                Consume(TokenType.Star);
                return new AllColumnsNode();
            case TokenType.True:
                Consume(TokenType.True);
                return new BooleanNode(true);
            case TokenType.False:
                Consume(TokenType.False);
                return new BooleanNode(false);
            case TokenType.LeftParenthesis:
                return SkipComposeSkip(TokenType.LeftParenthesis, f => f.ComposeOperations(),
                    TokenType.RightParenthesis);
            case TokenType.Hyphen:
                Consume(TokenType.Hyphen);
                return new StarNode(new IntegerNode("-1", "s"), Compose(f => f.ComposeArithmeticExpression(minPrecedence)));
            case TokenType.Case:
                var (whenThenNodes, elseNode) = ComposeCase();
                return new CaseNode(whenThenNodes, elseNode);
            case TokenType.FieldLink:
                return new FieldLinkNode(ConsumeAndGetToken().Value);
            case TokenType.Null:
                Consume(TokenType.Null);
                return new NullNode();
        }

        throw new NotSupportedException($"Token {Current.Value}({Current.TokenType}) at position {Current.Span.Start} cannot be used here.");
    }

    private ((Node When, Node Then)[] WhenThenNodes, Node ElseNode) ComposeCase()
    {
        Consume(TokenType.Case);

        var whenThenNodes = new List<(Node When, Node Then)>();

        while(Current.TokenType == TokenType.When)
        {
            Consume(TokenType.When);
            var whenNode = ComposeOperations();
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
        return new IntegerNode(token.Value, token.Abbreviation);
    }

    private HexIntegerNode ComposeHexInteger()
    {
        var token = (HexIntegerToken)ConsumeAndGetToken(TokenType.HexadecimalInteger);
        return new HexIntegerNode(token.Value);
    }

    private BinaryIntegerNode ComposeBinaryInteger()
    {
        var token = (BinaryIntegerToken)ConsumeAndGetToken(TokenType.BinaryInteger);
        return new BinaryIntegerNode(token.Value);
    }

    private OctalIntegerNode ComposeOctalInteger()
    {
        var token = (OctalIntegerToken)ConsumeAndGetToken(TokenType.OctalInteger);
        return new OctalIntegerNode(token.Value);
    }

    private WordNode ComposeWord()
    {
        return new WordNode(ConsumeAndGetToken(TokenType.Word).Value);
    }

    private AccessMethodNode ComposeAccessMethod(string alias)
    {
        ArgsListNode args;
        if (Current is FunctionToken func)
        {
            Consume(TokenType.Function);
            args = ComposeArgs();
            return new AccessMethodNode(func, args, null, false, null, alias);
        }
            
        if (Current is MethodAccessToken)
        {
            Consume(TokenType.MethodAccess);
            Consume(TokenType.Dot);
            var token = (FunctionToken)ConsumeAndGetToken(TokenType.Function);
            args = ComposeArgs();

            return new AccessMethodNode(token, args, null, false,
                null, alias);
        }
            
        throw new NotSupportedException($"Unrecognized token for ComposeAccessMethod(), the token was {Current.TokenType}");
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
        return currentToken.TokenType is TokenType.Star or TokenType.FSlash or TokenType.Mod or TokenType.Plus or TokenType.Hyphen or TokenType.Dot;
    }

    private static bool IsEqualityOperator(Token currentToken)
    {
        return currentToken.TokenType is TokenType.Greater or TokenType.GreaterEqual or TokenType.Less or TokenType.LessEqual or TokenType.Equality or TokenType.Not or TokenType.Diff or TokenType.Like or TokenType.NotLike or TokenType.Contains or TokenType.Is or TokenType.In or TokenType.NotIn or TokenType.RLike or TokenType.NotRLike;
    }

    private static bool IsQueryOperator(Token currentToken)
    {
        return currentToken.TokenType is TokenType.And or TokenType.Or;
    }

    private static bool IsNumericToken(Token current)
    {
        return current.TokenType is TokenType.Decimal or TokenType.Integer or TokenType.HexadecimalInteger or TokenType.BinaryInteger or TokenType.OctalInteger;
    }

    private enum Associativity
    {
        Left,
        Right
    }
}