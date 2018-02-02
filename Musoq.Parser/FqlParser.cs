using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser
{
    public class FqlParser
    {
        private readonly Lexer _lexer;

        private bool _isInGroupBySection;

        public FqlParser(Lexer lexer)
        {
            _lexer = lexer;
        }

        private Token Current => _lexer.Current();

        public RootNode ComposeAll()
        {
            _lexer.Next();
            while (Current.TokenType != TokenType.EndOfFile)
            {
                switch (Current.TokenType)
                {
                    case TokenType.Select:
                        return new RootNode(ComposeSetOps(0));
                }
            }

            return new RootNode(null);
        }

        private Node ComposeSetOps(int nestingLevel)
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

                var currentLevel = nestingLevel;
                var nextSet = ComposeSetOps(currentLevel + 1);
                var isQuery = nextSet is QueryNode;
                switch (setOperatorType)
                {
                    case TokenType.Except:
                        node = new ExceptNode(string.Empty, keys, node, nextSet, currentLevel != 0, isQuery);
                        break;
                    case TokenType.Union:
                        node = new UnionNode(string.Empty, keys, node, nextSet, currentLevel != 0, isQuery);
                        break;
                    case TokenType.UnionAll:
                        node = new UnionAllNode(string.Empty, keys, node, nextSet, currentLevel != 0, isQuery);
                        break;
                    case TokenType.Intersect:
                        node = new IntersectNode(string.Empty, keys, node, nextSet, currentLevel != 0, isQuery);
                        break;
                }
            }

            return isSet || nestingLevel > 0 ? node : CreateSingleSet(query);
        }

        private MultiStatementNode CreateSingleSet(QueryNode node)
        {
            var createTableNode = new CreateTableNode(node.From.Schema, node.From.Method, node.From.Parameters,
                new string[0], node.Select.Fields, string.Empty);
            return new MultiStatementNode(new Node[] {createTableNode, node}, node.ReturnType);
        }

        private string[] ComposeSetOperatorKeys()
        {
            var keys = new List<string>();
            if (Current.TokenType == TokenType.LeftParenthesis)
            {
                Consume(TokenType.LeftParenthesis);
                keys.Add(Current.Value);
                Consume(Current.TokenType);
                while (Current.TokenType == TokenType.Comma)
                {
                    Consume(TokenType.Comma);
                    keys.Add(Current.Value);
                    Consume(TokenType.Column);
                }

                Consume(TokenType.RightParenthesis);
            }

            return keys.ToArray();
        }

        private static bool IsSetOperator(TokenType currentTokenType)
        {
            return new[]
                {TokenType.Union, TokenType.UnionAll, TokenType.Except, TokenType.Intersect}.Contains(currentTokenType);
        }

        private QueryNode ComposeQuery()
        {
            var selectNode = ComposeSelectNode();
            var fromNode = ComposeFrom();
            var whereNode = ComposeWhereNode();
            var groupBy = ComposeGrouByNode();
            return new QueryNode(selectNode, fromNode, whereNode, groupBy);
        }

        private GroupByNode ComposeGrouByNode()
        {
            if (Current.TokenType == TokenType.GroupBy)
            {
                Consume(TokenType.GroupBy);

                var fields = ConsumeFields();

                if (Current.TokenType != TokenType.Having) return new GroupByNode(fields, null);

                Consume(TokenType.Having);

                _isInGroupBySection = true;
                var having = new HavingNode(ComposeOperations());
                _isInGroupBySection = false;

                return new GroupByNode(fields, having);
            }

            return null;
        }

        private SelectNode ComposeSelectNode()
        {
            Consume(TokenType.Select);
            ConsumeWhiteSpaces();

            var fields = ConsumeFields();

            return new SelectNode(fields);
        }

        private FieldNode[] ConsumeFields()
        {
            var fields = new List<FieldNode>();
            var i = 0;

            do
            {
                fields.Add(ConsumeField(i++));
            } while (Current.TokenType != TokenType.From && Current.TokenType != TokenType.Having &&
                     ConsumeAndGetToken().TokenType == TokenType.Comma);

            return fields.ToArray();
        }

        private FieldNode ConsumeField(int order)
        {
            var fieldExpression = ComposeOperations();
            var alias = ComposeAlias();
            return new FieldNode(fieldExpression, order, alias);
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
            }

            return string.Empty;
        }

        private Node ComposeOperations()
        {
            var node = ComposeEqualityOperators();
            while (IsQueryOperator(Current))
                switch (Current.TokenType)
                {
                    case TokenType.And:
                        Consume(TokenType.And);
                        node = new AndNode(new ShortCircuitingNodeLeft(node, TokenType.And),
                            new ShortCircuitingNodeRight(ComposeEqualityOperators(), TokenType.And));
                        break;
                    case TokenType.Or:
                        Consume(TokenType.Or);
                        node = new OrNode(new ShortCircuitingNodeLeft(node, TokenType.Or),
                            new ShortCircuitingNodeRight(ComposeEqualityOperators(), TokenType.Or));
                        break;
                    default:
                        throw new NotSupportedException();
                }
            return node;
        }

        private Node ComposeEqualityOperators()
        {
            var node = ComposeArithmeticOperators(Precendence.Level1);
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
                    default:
                        throw new NotSupportedException();
                }

            return node;
        }

        private Node ComposeArithmeticOperators(Precendence precendence)
        {
            Node node = null;
            switch (precendence)
            {
                case Precendence.Level1:
                {
                    node = ComposeArithmeticOperators(Precendence.Level2);
                    while (IsArithmeticOperator(Current, Precendence.Level1))
                        switch (Current.TokenType)
                        {
                            case TokenType.Star:
                                Consume(TokenType.Star);
                                node = new StarNode(node, ComposeArithmeticOperators(Precendence.Level1));
                                break;
                            case TokenType.FSlash:
                                Consume(TokenType.FSlash);
                                node = new FSlashNode(node, ComposeArithmeticOperators(Precendence.Level1));
                                break;
                            case TokenType.Mod:
                                Consume(TokenType.Mod);
                                node = new ModuloNode(node, ComposeArithmeticOperators(Precendence.Level1));
                                break;
                        }
                    break;
                }
                case Precendence.Level2:
                {
                    node = ComposeArithmeticOperators(Precendence.Level3);
                    while (IsArithmeticOperator(Current, Precendence.Level2))
                        switch (Current.TokenType)
                        {
                            case TokenType.Plus:
                                Consume(TokenType.Plus);
                                node = new AddNode(node, ComposeArithmeticOperators(Precendence.Level1));
                                break;
                            case TokenType.Hyphen:
                                Consume(TokenType.Hyphen);
                                node = new HyphenNode(node, ComposeArithmeticOperators(Precendence.Level1));
                                break;
                        }
                    break;
                }
                case Precendence.Level3:
                    if (IsArithmeticOperator(Current, Precendence.Level3))
                    {
                        while (IsArithmeticOperator(Current, Precendence.Level3))
                        {
                            switch (Current.TokenType)
                            {
                                case TokenType.Hyphen:
                                    Consume(TokenType.Hyphen);
                                    node = new StarNode(new IntegerNode("-1"), ComposeArithmeticOperators(Precendence.Level1));
                                    break;
                            }
                        }
                    }
                    else
                    {
                        node = ComposeArithmeticOperators(Precendence.Level4);
                    }
                    break;
                case Precendence.Level4:
                    node = ComposeBaseTypes();

                    if (node is AccessColumnNode columnNode)
                    {
                        var isComplexObjectAccessor = IsComplexObjectAccessor(Current);

                        if (isComplexObjectAccessor)
                        {
                            Consume(TokenType.Dot);
                            var ct = Current;

                            switch (Current.TokenType)
                            {
                                case TokenType.Column:
                                case TokenType.KeyAccess:
                                case TokenType.NumericAccess:
                                case TokenType.Property:
                                    node = new AccessPropertyNode(node, ComposeBaseTypes(), true, ct.Value);
                                    break;
                            }

                            isComplexObjectAccessor = IsComplexObjectAccessor(Current);
                        }

                        while (isComplexObjectAccessor)
                        {
                            Consume(TokenType.Dot);
                            var ct = Current;

                            switch (Current.TokenType)
                            {
                                case TokenType.Column:
                                case TokenType.KeyAccess:
                                case TokenType.NumericAccess:
                                case TokenType.Property:
                                    node = new AccessPropertyNode(node, ComposeBaseTypes(), false, ct.Value);
                                    break;
                            }

                            isComplexObjectAccessor = IsComplexObjectAccessor(Current);
                        }
                    }
                    break;
            }

            return node;
        }

        private bool IsComplexObjectAccessor(Token current)
        {
            return current.TokenType == TokenType.Dot;
        }

        private FromNode ComposeFrom()
        {
            Consume(TokenType.From);
            var schemaName = ComposeWord();
            Consume(TokenType.Dot);
            var accessMethod = ComposeAccessMethod();
            var alias = ComposeAlias();

            if (string.IsNullOrEmpty(alias))
                alias =
                    $"{schemaName.ToString()}.{accessMethod.ToString()}";

            var fromNode = new SchemaFromNode(schemaName.Value, accessMethod.Name,
                accessMethod.Arguments.Args.Select(GetValueOfBasicType).ToArray(), alias);
            return fromNode;
        }

        private static string GetValueOfBasicType(Node node)
        {
            switch (node)
            {
                case WordNode word:
                    return word.Value;
                case DecimalNode numeric:
                    return numeric.Value.ToString(CultureInfo.InvariantCulture);
                case IntegerNode integer:
                    return integer.Value.ToString();
            }

            throw new NotSupportedException();
        }

        private void ConsumeWhiteSpaces()
        {
            while (Current.TokenType == TokenType.WhiteSpace)
                Consume(TokenType.WhiteSpace);
        }

        private WhereNode ComposeWhereNode()
        {
            if (Current.TokenType == TokenType.Where)
            {
                Consume(TokenType.Where);
                return new WhereNode(ComposeOperations());
            }

            return new WhereNode(new PutTrueNode());
        }

        private void Consume(TokenType tokenType)
        {
            if (Current.TokenType.Equals(tokenType))
            {
                _lexer.Next();
                return;
            }

            throw new UnexpectedTokenException<TokenType>(_lexer.Position, Current);
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

                    args.Add(ComposeOperations());
                } while (Current.TokenType == TokenType.Comma);
            }

            Consume(TokenType.RightParenthesis);

            return new ArgsListNode(args.ToArray());
        }

        private Node ComposeBaseTypes()
        {
            switch (Current.TokenType)
            {
                case TokenType.Decimal:
                    var token = ConsumeAndGetToken(TokenType.Decimal);
                    return new DecimalNode(token.Value);
                case TokenType.Integer:
                    token = ConsumeAndGetToken(TokenType.Integer);
                    return new IntegerNode(token.Value);
                case TokenType.Word:
                    return ComposeWord();
                case TokenType.Function:
                    return ComposeAccessMethod();
                case TokenType.Column:

                    if (!(Current is ColumnToken column))
                        throw new ArgumentNullException();

                    Consume(TokenType.Column);

                    return new AccessColumnNode(column.Value, column.Span);
                case TokenType.KeyAccess:
                    var keyAccess = (KeyAccessToken)Current;
                    Consume(TokenType.KeyAccess);
                    return new AccessObjectKeyNode(keyAccess);
                case TokenType.NumericAccess:
                    var numiercAccess = (NumericAccessToken) Current;
                    Consume(TokenType.NumericAccess);
                    return new AccessObjectArrayNode(numiercAccess);
                case TokenType.Property:
                    token = ConsumeAndGetToken(TokenType.Property);
                    return new PropertyValueNode(token.Value);
                case TokenType.Star:
                    Consume(TokenType.Star);
                    return new AllColumnsNode();
                case TokenType.LeftParenthesis:
                    return SkipComposeSkip(TokenType.LeftParenthesis, f => f.ComposeOperations(), TokenType.RightParenthesis);
            }

            throw new NotSupportedException();
        }

        private WordNode ComposeWord()
        {
            return new WordNode(ConsumeAndGetToken(TokenType.Word).Value);
        }

        private AccessMethodNode ComposeAccessMethod()
        {
            if (!(Current is FunctionToken func))
                throw new ArgumentNullException();

            Consume(TokenType.Function);

            var args = ComposeArgs();

            return _isInGroupBySection
                ? new GroupByAccessMethodNode(func, args, null, null)
                : new AccessMethodNode(func, args, null);
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

        private TNode SkipComposeSkip<TNode>(TokenType pStatenent, Func<FqlParser, TNode> parserAction,
            TokenType aStatement)
        {
            Consume(pStatenent);
            return ComposeAndSkip(parserAction, aStatement);
        }

        private TNode ComposeAndSkip<TNode>(Func<FqlParser, TNode> parserAction, TokenType statement)
        {
            if (parserAction == null)
                throw new ArgumentNullException(nameof(parserAction));

            var node = parserAction(this);
            Consume(statement);
            return node;
        }

        private static bool IsArithmeticOperator(Token currentToken, Precendence precendence)
        {
            switch (precendence)
            {
                case Precendence.Level1:
                    return currentToken.TokenType == TokenType.Star ||
                           currentToken.TokenType == TokenType.FSlash ||
                           currentToken.TokenType == TokenType.Mod;
                case Precendence.Level2:
                    return currentToken.TokenType == TokenType.Plus ||
                           currentToken.TokenType == TokenType.Hyphen;
                case Precendence.Level3:
                    return currentToken.TokenType == TokenType.Hyphen;
                case Precendence.Level4:
                    return true;
            }

            return false;
        }

        private static bool IsEqualityOperator(Token currentToken)
        {
            return currentToken.TokenType == TokenType.Greater ||
                   currentToken.TokenType == TokenType.GreaterEqual ||
                   currentToken.TokenType == TokenType.Less ||
                   currentToken.TokenType == TokenType.LessEqual ||
                   currentToken.TokenType == TokenType.Equality ||
                   currentToken.TokenType == TokenType.Not ||
                   currentToken.TokenType == TokenType.Diff ||
                   currentToken.TokenType == TokenType.Like ||
                   currentToken.TokenType == TokenType.NotLike;
        }

        private static bool IsQueryOperator(Token currentToken)
        {
            return currentToken.TokenType == TokenType.And || currentToken.TokenType == TokenType.Or;
        }

        private static Type InferMinimalNumericType(Token token)
        {
            if (bool.TryParse(token.Value, out var bResult))
                return typeof(bool);

            if (short.TryParse(token.Value, out var sResult))
                return typeof(short);

            return typeof(long);
        }

        private enum Precendence : short
        {
            Level1,
            Level2,
            Level3,
            Level4
        }
    }
}