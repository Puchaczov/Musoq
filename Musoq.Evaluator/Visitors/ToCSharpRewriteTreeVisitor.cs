using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using AliasedFromNode = Musoq.Parser.Nodes.From.AliasedFromNode;
using ExpressionFromNode = Musoq.Parser.Nodes.From.ExpressionFromNode;
using Group = Musoq.Plugins.Group;
using InMemoryTableFromNode = Musoq.Parser.Nodes.From.InMemoryTableFromNode;
using JoinFromNode = Musoq.Parser.Nodes.From.JoinFromNode;
using JoinInMemoryWithSourceTableFromNode = Musoq.Parser.Nodes.From.JoinInMemoryWithSourceTableFromNode;
using JoinSourcesTableFromNode = Musoq.Parser.Nodes.From.JoinSourcesTableFromNode;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;
using SchemaMethodFromNode = Musoq.Parser.Nodes.From.SchemaMethodFromNode;
using TextSpan = Musoq.Parser.TextSpan;

namespace Musoq.Evaluator.Visitors;

public class ToCSharpRewriteTreeVisitor : DefensiveVisitorBase, IToCSharpTranslationExpressionVisitor
{

    /// <summary>
    /// Gets the name of this visitor for error reporting.
    /// </summary>
    protected override string VisitorName => nameof(ToCSharpRewriteTreeVisitor);

    private readonly Dictionary<string, int> _inMemoryTableIndexes = new();
    private readonly List<string> _loadedAssemblies = [];

    private readonly List<SyntaxNode> _members = [];
    private readonly Stack<string> _methodNames = new();

    private readonly List<string> _namespaces = [];
    private readonly IDictionary<string, int[]> _setOperatorFieldIndexes;

    private readonly Dictionary<string, Type> _typesToInstantiate = new();
    private BlockSyntax _emptyBlock;
    private SyntaxNode _groupHaving;

    private readonly Dictionary<string, LocalDeclarationStatementSyntax> _getRowsSourceStatement = new();

    private VariableDeclarationSyntax _groupKeys;
    private VariableDeclarationSyntax _groupValues;

    private int _inMemoryTableIndex;
    private int _setOperatorMethodIdentifier;
    private int _caseWhenMethodIndex;
    private int _schemaFromIndex;

    private BlockSyntax _joinOrApplyBlock;
    private string _queryAlias;
    private Scope _scope;
    private BlockSyntax _selectBlock;
    private MethodAccessType _oldType;
    private MethodAccessType _type;
    private bool _isInsideJoinOrApply;
    private bool _isResultParallelizationImpossible;

    public ToCSharpRewriteTreeVisitor(
        IEnumerable<Assembly> assemblies,
        IDictionary<string, int[]> setOperatorFieldIndexes,
        IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]> inferredColumns,
        string assemblyName)
    {
        // Validate constructor parameters
        ValidateConstructorParameter(nameof(assemblies), assemblies);
        ValidateConstructorParameter(nameof(setOperatorFieldIndexes), setOperatorFieldIndexes);
        ValidateConstructorParameter(nameof(inferredColumns), inferredColumns);
        ValidateStringParameter(nameof(assemblyName), assemblyName, "constructor");

        _setOperatorFieldIndexes = setOperatorFieldIndexes;
        InferredColumns = inferredColumns;
        Workspace = new AdhocWorkspace();
        Nodes = new Stack<SyntaxNode>();

        Generator = SyntaxGenerator.GetGenerator(Workspace, LanguageNames.CSharp);

        Compilation = CSharpCompilation.Create(assemblyName);
        Compilation = Compilation.AddReferences(RuntimeLibraries.References);

        SafeExecute(() =>
        {
            AddReference(typeof(object));
            AddReference(typeof(CancellationToken));
            AddReference(typeof(ISchema));
            AddReference(typeof(LibraryBase));
            AddReference(typeof(Table));
        }, "initializing references");
        AddReference(typeof(SyntaxFactory));
        AddReference(typeof(ExpandoObject));
        AddReference(typeof(SchemaFromNode));

        var abstractionDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Microsoft.Extensions.Logging.Abstractions.dll");
        
        AddReference(abstractionDll);
        AddReference(typeof(ILogger));
        
        AddReference(assemblies.ToArray());

        Compilation = Compilation.WithOptions(
            new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
#if DEBUG
                    optimizationLevel: OptimizationLevel.Debug,
#else
                        optimizationLevel: OptimizationLevel.Release,
#endif
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default)
                .WithConcurrentBuild(true)
                .WithMetadataImportOptions(MetadataImportOptions.Public));

        AccessToClassPath = $"{Namespace}.{ClassName}";

        AddNamespace("System");
        AddNamespace(typeof(CancellationToken).Namespace);
        AddNamespace("Microsoft.Extensions.Logging");
        AddNamespace("System.Collections.Generic");
        AddNamespace("System.Threading.Tasks");
        AddNamespace("System.Linq");
        AddNamespace("Musoq.Plugins");
        AddNamespace("Musoq.Schema");
        AddNamespace("Musoq.Evaluator");
        AddNamespace("Musoq.Parser.Nodes.From");
        AddNamespace("Musoq.Parser.Nodes");
        AddNamespace("Musoq.Evaluator.Tables");
        AddNamespace("Musoq.Evaluator.Helpers");
        AddNamespace("System.Dynamic");
    }

    public string Namespace { get; } =
        $"{Resources.Compilation.NamespaceConstantPart}_{StringHelpers.GenerateNamespaceIdentifier()}";

    public string ClassName => "CompiledQuery";

    public string AccessToClassPath { get; }

    public AdhocWorkspace Workspace { get; }

    public SyntaxGenerator Generator { get; }

    public CSharpCompilation Compilation { get; private set; }

    private Stack<SyntaxNode> Nodes { get; }

    private List<StatementSyntax> Statements { get; } = [];

    private List<Stack<SyntaxNode>> NullSuspiciousNodes { get; } = [];

    private IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]> InferredColumns { get; }

    public void Visit(Node node)
    {
    }

    public void Visit(DescNode node)
    {
        AddNamespace(typeof(EvaluationHelper).Namespace);

        switch (node.Type)
        {
            case DescForType.Constructors:
                CreateDescForConstructors(node);
                break;
            case DescForType.Schema:
                CreateDescForSchema(node);
                break;
            case DescForType.SpecificConstructor:
                CreateDescForSpecificConstructor(node);
                break;
            case DescForType.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        Statements.Clear();
    }

    public void Visit(StarNode node)
    {
        SyntaxBinaryOperationHelper.ProcessMultiplyOperation(Nodes, Generator);
    }

    public void Visit(FSlashNode node)
    {
        SyntaxBinaryOperationHelper.ProcessDivideOperation(Nodes, Generator);
    }

    public void Visit(ModuloNode node)
    {
        SyntaxBinaryOperationHelper.ProcessModuloOperation(Nodes, Generator);
    }

    public void Visit(AddNode node)
    {
        SyntaxBinaryOperationHelper.ProcessAddOperation(Nodes, Generator);
    }

    public void Visit(HyphenNode node)
    {
        SyntaxBinaryOperationHelper.ProcessSubtractOperation(Nodes, Generator);
    }

    public void Visit(AndNode node)
    {
        SyntaxBinaryOperationHelper.ProcessLogicalAndOperation(Nodes, Generator);
    }

    public void Visit(OrNode node)
    {
        SyntaxBinaryOperationHelper.ProcessLogicalOrOperation(Nodes, Generator);
    }

    public void Visit(ShortCircuitingNodeLeft node)
    {
    }

    public void Visit(ShortCircuitingNodeRight node)
    {
    }

    public void Visit(EqualityNode node)
    {
        var b = Nodes.Pop();
        var a = Nodes.Pop();

        // Handle char vs string comparison
        if (IsCharVsStringComparison(node.Left, node.Right, a, b))
        {
            // Convert string literal to char for comparison
            var convertedComparison = HandleCharStringComparison(node.Left, node.Right, a, b);
            Nodes.Push(convertedComparison);
        }
        else
        {
            // Use the helper for normal equality operations
            Nodes.Push(a);
            Nodes.Push(b);
            SyntaxBinaryOperationHelper.ProcessValueEqualsOperation(Nodes, Generator);
        }
    }

    private bool IsCharVsStringComparison(Node leftNode, Node rightNode, SyntaxNode leftSyntax, SyntaxNode rightSyntax)
    {
        // Check if we have a character access node compared with a string literal
        var leftIsChar = IsCharacterAccess(leftNode);
        var rightIsChar = IsCharacterAccess(rightNode);
        var leftIsString = leftNode is WordNode;
        var rightIsString = rightNode is WordNode;

        return (leftIsChar && rightIsString) || (leftIsString && rightIsChar);
    }

    private bool IsCharacterAccess(Node node)
    {
        // Check if this is a character access from a string column
        if (node is AccessObjectArrayNode arrayNode)
        {
            return arrayNode.IsColumnAccess && arrayNode.ColumnType == typeof(string);
        }
        return false;
    }

    private SyntaxNode HandleCharStringComparison(Node leftNode, Node rightNode, SyntaxNode leftSyntax, SyntaxNode rightSyntax)
    {
        // Determine which side is the character and which is the string
        var leftIsChar = IsCharacterAccess(leftNode);
        var leftIsString = leftNode is WordNode leftWord;
        
        if (leftIsChar && rightNode is WordNode rightWord)
        {
            // Left is char, right is string - convert string to char
            var charValue = rightWord.Value.Length > 0 ? rightWord.Value[0] : '\0';
            var charLiteral = SyntaxFactory.LiteralExpression(
                SyntaxKind.CharacterLiteralExpression,
                SyntaxFactory.Literal(charValue));
            return Generator.ValueEqualsExpression(leftSyntax, charLiteral);
        }
        else if (leftIsString && IsCharacterAccess(rightNode))
        {
            // Left is string, right is char - convert string to char
            var leftWordNode = (WordNode)leftNode;
            var charValue = leftWordNode.Value.Length > 0 ? leftWordNode.Value[0] : '\0';
            var charLiteral = SyntaxFactory.LiteralExpression(
                SyntaxKind.CharacterLiteralExpression,
                SyntaxFactory.Literal(charValue));
            return Generator.ValueEqualsExpression(charLiteral, rightSyntax);
        }

        // Fallback to standard comparison
        return Generator.ValueEqualsExpression(leftSyntax, rightSyntax);
    }

    public void Visit(GreaterOrEqualNode node)
    {
        SyntaxBinaryOperationHelper.ProcessGreaterThanOrEqualOperation(Nodes, Generator);
    }

    public void Visit(LessOrEqualNode node)
    {
        SyntaxBinaryOperationHelper.ProcessLessThanOrEqualOperation(Nodes, Generator);
    }

    public void Visit(GreaterNode node)
    {
        SyntaxBinaryOperationHelper.ProcessGreaterThanOperation(Nodes, Generator);
    }

    public void Visit(LessNode node)
    {
        SyntaxBinaryOperationHelper.ProcessLessThanOperation(Nodes, Generator);
    }

    public void Visit(DiffNode node)
    {
        SyntaxBinaryOperationHelper.ProcessValueNotEqualsOperation(Nodes, Generator);
    }

    public void Visit(NotNode node)
    {
        var a = Nodes.Pop();
        Nodes.Push(Generator.LogicalNotExpression(a));
    }

    public void Visit(LikeNode node)
    {
        var b = Nodes.Pop();
        var a = Nodes.Pop();

        var arg = SyntaxGenerationHelper.CreateArgumentList(
            (ExpressionSyntax) a,
            (ExpressionSyntax) b);

        Nodes.Push(arg);

        Visit(new AccessMethodNode(
            new FunctionToken(nameof(Operators.Like), TextSpan.Empty),
            new ArgsListNode([node.Left, node.Right]), null, false,
            typeof(Operators).GetMethod(nameof(Operators.Like))));
    }

    public void Visit(RLikeNode node)
    {
        var b = Nodes.Pop();
        var a = Nodes.Pop();

        var arg = SyntaxGenerationHelper.CreateArgumentList(
            (ExpressionSyntax) a,
            (ExpressionSyntax) b);

        Nodes.Push(arg);

        Visit(new AccessMethodNode(
            new FunctionToken(nameof(Operators.RLike), TextSpan.Empty),
            new ArgsListNode([node.Left, node.Right]), null, false,
            typeof(Operators).GetMethod(nameof(Operators.RLike))));
    }

    public void Visit(InNode node)
    {
    }

    public void Visit(FieldNode node)
    {
        var types = EvaluationHelper.GetNestedTypes(node.ReturnType);

        AddReference(types);
        AddNamespace(types);

        var typeIdentifier =
            SyntaxFactory.IdentifierName(
                EvaluationHelper.GetCastableType(node.ReturnType));

        if (node.ReturnType is NullNode.NullType)
        {
            typeIdentifier = SyntaxFactory.IdentifierName("object");
        }

        if (typeof(IDynamicMetaObjectProvider).IsAssignableFrom(node.ReturnType))
        {
            typeIdentifier = SyntaxFactory.IdentifierName("dynamic");
        }

        var expression = Nodes.Pop();

        var castedExpression = Generator.CastExpression(typeIdentifier, expression);
        Nodes.Push(castedExpression);
    }

    public void Visit(FieldOrderedNode node)
    {
        var types = EvaluationHelper.GetNestedTypes(node.ReturnType);

        AddReference(types);
        AddNamespace(types);

        var typeIdentifier = SyntaxFactory.IdentifierName(
            EvaluationHelper.GetCastableType(node.ReturnType));

        if (node.ReturnType is NullNode.NullType)
        {
            typeIdentifier = SyntaxFactory.IdentifierName("object");
        }

        if (typeof(IDynamicMetaObjectProvider).IsAssignableFrom(node.ReturnType))
        {
            typeIdentifier = SyntaxFactory.IdentifierName("dynamic");
        }

        var castedExpression = Generator.CastExpression(
            typeIdentifier, Nodes.Pop());

        Nodes.Push(castedExpression);
    }

    public void Visit(StringNode node)
    {
        Nodes.Push(LiteralNodeSyntaxConverter.ConvertStringNode(node));
    }

    public void Visit(DecimalNode node)
    {
        Nodes.Push(LiteralNodeSyntaxConverter.ConvertDecimalNode(node));
    }

    public void Visit(IntegerNode node)
    {
        Nodes.Push(LiteralNodeSyntaxConverter.ConvertIntegerNode(node));
    }

    public void Visit(BooleanNode node)
    {
        Nodes.Push(LiteralNodeSyntaxConverter.ConvertBooleanNode(node, Generator));
    }

    public void Visit(WordNode node)
    {
        Nodes.Push(LiteralNodeSyntaxConverter.ConvertWordNode(node, Generator));
    }

    public void Visit(NullNode node)
    {
        Nodes.Push(LiteralNodeSyntaxConverter.ConvertNullNode(node, Generator));
    }

    public void Visit(ContainsNode node)
    {
        var comparisonValues = (ArgumentListSyntax) Nodes.Pop();
        var a = Nodes.Pop();

        var expressions = new ExpressionSyntax[comparisonValues.Arguments.Count];
        for (var index = 0; index < comparisonValues.Arguments.Count; index++)
        {
            var argument = comparisonValues.Arguments[index];
            expressions[index] = argument.Expression;
        }

        var objExpression = SyntaxHelper.CreateArrayOfObjects(node.ReturnType.Name, expressions);

        var arg = SyntaxGenerationHelper.CreateArgumentList(
            (ExpressionSyntax) a,
            objExpression);

        Nodes.Push(arg);

        Visit(new AccessMethodNode(
            new FunctionToken(nameof(Operators.Contains), TextSpan.Empty),
            new ArgsListNode([node.Left, node.Right]), null, false,
            typeof(Operators).GetMethod(nameof(Operators.Contains))));
    }

    public void Visit(AccessMethodNode node)
    {
        var accessMethodExpr = AccessMethodNodeProcessor.ProcessAccessMethodNode(
            node,
            Generator,
            Nodes,
            Statements,
            _typesToInstantiate,
            _scope,
            _type,
            _isInsideJoinOrApply,
            NullSuspiciousNodes,
            AddNamespace);

        Nodes.Push(accessMethodExpr);
    }

    public void Visit(AccessRawIdentifierNode node)
    {
        Nodes.Push(SyntaxFactory.IdentifierName(node.Name));
    }

    public void Visit(IsNullNode node)
    {
        if (node.Expression.ReturnType.IsTrueValueType())
        {
            Nodes.Pop();
            Nodes.Push(
                node.IsNegated
                    ? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                    : SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression));
            return;
        }

        if (node.IsNegated)
            Nodes.Push(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.NotEqualsExpression,
                    (ExpressionSyntax) Nodes.Pop(),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));
        else
            Nodes.Push(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    (ExpressionSyntax) Nodes.Pop(),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));
    }

    public void Visit(AccessRefreshAggregationScoreNode node)
    {
    }

    public void Visit(AccessColumnNode node)
    {
        var variableName = _type switch
        {
            MethodAccessType.TransformingQuery => $"{node.Alias}Row",
            MethodAccessType.ResultQuery or MethodAccessType.CaseWhen => "score",
            _ => throw new NotSupportedException($"Unrecognized method access type ({_type})")
        };

        var sNode = Generator.ElementAccessExpression(
            Generator.IdentifierName(variableName),
            SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal($"@\"{node.Name}\"", node.Name))));

        var types = EvaluationHelper.GetNestedTypes(node.ReturnType);

        AddNamespace(types);
        AddReference(types);

        var typeIdentifier =
            SyntaxFactory.IdentifierName(
                EvaluationHelper.GetCastableType(node.ReturnType));

        if (node.ReturnType is NullNode.NullType)
        {
            typeIdentifier = SyntaxFactory.IdentifierName("object");
        }

        if (typeof(IDynamicMetaObjectProvider).IsAssignableFrom(node.ReturnType))
        {
            typeIdentifier = SyntaxFactory.IdentifierName("dynamic");
        }

        sNode = Generator.CastExpression(typeIdentifier, sNode);

        if (!node.ReturnType.IsTrueValueType() && NullSuspiciousNodes.Count > 0)
            NullSuspiciousNodes[^1].Push(sNode);

        Nodes.Push(sNode);
    }

    public void Visit(AllColumnsNode node)
    {
    }

    public void Visit(IdentifierNode node)
    {
        // Check if this identifier is in the in-memory table indexes
        if (_inMemoryTableIndexes.TryGetValue(node.Name, out var tableIndex))
        {
            Nodes.Push(SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName("_tableResults"))
                .WithArgumentList(
                    SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(tableIndex)))))));
        }
        else
        {
            // If not found in _inMemoryTableIndexes, treat as raw identifier
            // This handles cases like PIVOT aggregation where identifiers refer to 
            // source table columns that aren't in the in-memory table indexes yet
            Nodes.Push(SyntaxFactory.IdentifierName(node.Name));
        }
    }

    public void Visit(AccessObjectArrayNode node)
    {
        var result = AccessObjectArrayNodeProcessor.ProcessAccessObjectArrayNode(node, Nodes);
        AddNamespace(result.RequiredNamespace);
        Nodes.Push(result.Expression);
    }

    public void Visit(AccessObjectKeyNode node)
    {
        var result = AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(node, Nodes);
        AddNamespace(result.RequiredNamespace);
        Nodes.Push(result.Expression);
    }

    public void Visit(PropertyValueNode node)
    {
        var exp = SyntaxFactory.ParenthesizedExpression((ExpressionSyntax) Nodes.Pop());

        Nodes.Push(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                exp,
                SyntaxFactory.IdentifierName(node.Name)));
    }

    public void Visit(DotNode node)
    {
    }

    public void Visit(AccessCallChainNode node)
    {
    }

    public void Visit(ArgsListNode node)
    {
        var args = SyntaxFactory.SeparatedList<ArgumentSyntax>();

        for (var i = 0; i < node.Args.Length; i++)
            args = args.Add(SyntaxFactory.Argument((ExpressionSyntax) Nodes.Pop()));

        var rArgs = SyntaxFactory.SeparatedList<ArgumentSyntax>();

        for (var i = args.Count - 1; i >= 0; i--) rArgs = rArgs.Add(args[i]);

        Nodes.Push(SyntaxFactory.ArgumentList(rArgs));
    }


    public void Visit(SelectNode node)
    {
        _selectBlock = SelectNodeProcessor.ProcessSelectNode(node, Nodes, _scope, _type);
    }

    public void Visit(GroupSelectNode node)
    {
    }

    public void Visit(WhereNode node)
    {
        var ifStatement =
            Generator.IfStatement(
                    Generator.LogicalNotExpression(Nodes.Pop()),
                    [
                        _isResultParallelizationImpossible || _type != MethodAccessType.ResultQuery
                            ? SyntaxFactory.ContinueStatement()
                            : SyntaxFactory.ReturnStatement()
                    ]
                )
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        Nodes.Push(ifStatement);
    }

    public void Visit(GroupByNode node)
    {
        var result = GroupByNodeProcessor.ProcessGroupByNode(node, Nodes, _scope);
        
        _groupValues = result.GroupValues;
        _groupKeys = result.GroupKeys;
        _groupHaving = result.GroupHaving;
        
        Statements.Add(result.GroupFieldsStatement);
        AddNamespace(typeof(GroupKey).Namespace);
    }

    public void Visit(HavingNode node)
    {
        Nodes.Push(Generator.IfStatement(Generator.LogicalNotExpression(Nodes.Pop()),
                [SyntaxFactory.ContinueStatement()])
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed));
    }

    public void Visit(SkipNode node)
    {
        var identifier = "skipAmount";

        var skip = SyntaxFactory.LocalDeclarationStatement(
                SyntaxHelper.CreateAssignment(identifier, (ExpressionSyntax) Generator.LiteralExpression(1)))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        var ifStatement = Generator.IfStatement(
            Generator.LessThanOrEqualExpression(
                SyntaxFactory.IdentifierName(identifier),
                Generator.LiteralExpression(node.Value)),
            [
                SyntaxFactory.PostfixUnaryExpression(
                    SyntaxKind.PostIncrementExpression,
                    SyntaxFactory.IdentifierName(identifier)),
                SyntaxFactory.ContinueStatement()
            ]);

        Statements.Add(skip);

        Nodes.Push(ifStatement);
    }

    public void Visit(TakeNode node)
    {
        var identifier = "tookAmount";

        var take = SyntaxFactory.LocalDeclarationStatement(
            SyntaxHelper.CreateAssignment(identifier, (ExpressionSyntax) Generator.LiteralExpression(0)));

        var ifStatement =
            (StatementSyntax) Generator.IfStatement(
                Generator.ValueEqualsExpression(
                    SyntaxFactory.IdentifierName(identifier),
                    Generator.LiteralExpression(node.Value)),
                [
                    SyntaxFactory.BreakStatement()
                ]);

        var incTookAmount =
            SyntaxFactory.ExpressionStatement(
                SyntaxFactory.PostfixUnaryExpression(
                    SyntaxKind.PostIncrementExpression,
                    SyntaxFactory.IdentifierName(identifier)));

        Statements.Add(take);

        Nodes.Push(SyntaxFactory.Block(ifStatement, incTookAmount));
    }

    public void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
        var ifStatement = (StatementSyntax)Generator.IfStatement(Generator.LogicalNotExpression(Nodes.Pop()),
                [SyntaxFactory.ContinueStatement()])
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        _emptyBlock = SyntaxFactory.Block();

        var computingBlock = node.JoinType switch
        {
            JoinType.Inner => JoinProcessingHelper.ProcessInnerJoin(
                node, ifStatement, _emptyBlock, Generator, GetRowsSourceOrEmpty, GenerateCancellationExpression),
            JoinType.OuterLeft => JoinProcessingHelper.ProcessOuterLeftJoin(
                node, ifStatement, _emptyBlock, Generator, _scope, _queryAlias, GetRowsSourceOrEmpty, GenerateCancellationExpression),
            JoinType.OuterRight => JoinProcessingHelper.ProcessOuterRightJoin(
                node, ifStatement, _emptyBlock, Generator, _scope, _queryAlias, GetRowsSourceOrEmpty, GenerateCancellationExpression),
            _ => throw new ArgumentException($"Unsupported join type: {node.JoinType}")
        };

        _joinOrApplyBlock = computingBlock;
    }

    public void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        // Create wrappers for the method signatures expected by the helper
        Func<string, StatementSyntax> getRowsSourceWrapper = alias => GetRowsSourceOrEmpty(alias);
        Func<StatementSyntax[], BlockSyntax> blockWrapper = statements => Block(statements);
        Func<StatementSyntax> cancellationWrapper = () => GenerateCancellationExpression();
        
        var result = ApplyInMemoryWithSourceTableNodeProcessor.ProcessApplyInMemoryWithSourceTable(
            node, Generator, _scope, _queryAlias, getRowsSourceWrapper, blockWrapper, cancellationWrapper);
        
        _emptyBlock = result.EmptyBlock;
        _joinOrApplyBlock = result.ComputingBlock;
    }

    public void Visit(SchemaFromNode node)
    {
        // DEFENSIVE: Handle case where node might not be in InferredColumns
        // This can happen during PIVOT processing with fallback nodes
        ISchemaColumn[] originColumns;
        if (!InferredColumns.TryGetValue(node, out originColumns))
        {
            // Fallback: try to get columns from any similar node or create empty array
            originColumns = InferredColumns.Values.FirstOrDefault() ?? new ISchemaColumn[0];
        }

        var tableInfoVariableName = node.Alias.ToInfoTable();
        var tableInfoObject = SyntaxHelper.CreateAssignment(
            tableInfoVariableName,
            SyntaxHelper.CreateArrayOf(
                nameof(ISchemaColumn),
                originColumns.Select(column => SyntaxHelper.CreateObjectOf(nameof(Column),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList([
                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(column.ColumnName))),
                        SyntaxHelper.TypeLiteralArgument(EvaluationHelper.GetCastableType(column.ColumnType)),
                        SyntaxHelper.IntLiteralArgument(column.ColumnIndex)
                    ])))).Cast<ExpressionSyntax>().ToArray()));

        var createdSchema = SyntaxHelper.CreateAssignmentByMethodCall(
            node.Alias,
            "provider",
            nameof(ISchemaProvider.GetSchema),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                SyntaxFactory.SeparatedList([
                    SyntaxHelper.StringLiteralArgument(node.Schema)
                ]),
                SyntaxFactory.Token(SyntaxKind.CloseParenToken)
            )
        );

        var args = new List<ExpressionSyntax>();
        var stackNode = Nodes.Pop();
        
        // DEFENSIVE: Handle unexpected node types during PIVOT processing
        if (stackNode is ArgumentListSyntax argList)
        {
            args.AddRange(argList.Arguments.Select(arg => arg.Expression));
        }
        else if (stackNode is LiteralExpressionSyntax literal)
        {
            // Handle case where a literal expression is on the stack instead of argument list
            // This can happen during PIVOT processing
            args.Add(literal);
        }
        else
        {
            // For other node types, try to use them as expressions if possible
            if (stackNode is ExpressionSyntax expr)
            {
                args.Add(expr);
            }
            // If it's not an expression, we'll leave args empty and continue
        }

        var createdSchemaRows = SyntaxHelper.CreateAssignmentByMethodCall(
            $"{node.Alias}Rows",
            node.Alias,
            nameof(ISchema.GetRowSource),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList([
                    SyntaxHelper.StringLiteralArgument(node.Method),
                    SyntaxFactory.Argument(
                        CreateRuntimeContext(node, SyntaxFactory.IdentifierName(tableInfoVariableName))),
                    SyntaxFactory.Argument(
                        SyntaxHelper.CreateArrayOf(
                            nameof(Object),
                            args.ToArray()))
                ])
            ));

        Statements.Add(SyntaxFactory.LocalDeclarationStatement(tableInfoObject));
        Statements.Add(SyntaxFactory.LocalDeclarationStatement(createdSchema));

        if (_isInsideJoinOrApply)
        {
            _getRowsSourceStatement.Add(node.Alias, SyntaxFactory.LocalDeclarationStatement(createdSchemaRows));
        }
        else
        {
            Statements.Add(SyntaxFactory.LocalDeclarationStatement(createdSchemaRows));
        }
    }

    public void Visit(JoinSourcesTableFromNode node)
    {
        var ifStatement = Generator.IfStatement(Generator.LogicalNotExpression(Nodes.Pop()),
                [SyntaxFactory.ContinueStatement()])
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        _emptyBlock = SyntaxFactory.Block();

        _joinOrApplyBlock = JoinSourcesTableProcessingHelper.ProcessJoinSourcesTable(
            node, 
            Generator, 
            _scope, 
            _queryAlias, 
            ifStatement, 
            _emptyBlock, 
            GetRowsSourceOrEmpty, 
            Block, 
            GenerateCancellationExpression);
    }

    public void Visit(ApplySourcesTableFromNode node)
    {
        _emptyBlock = SyntaxFactory.Block();

        var computingBlock = SyntaxFactory.Block();
        switch (node.ApplyType)
        {
            case ApplyType.Cross:
                computingBlock =
                    computingBlock.AddStatements(
                        GetRowsSourceOrEmpty(node.First.Alias),
                        SyntaxFactory.ForEachStatement(SyntaxFactory.IdentifierName("var"),
                            SyntaxFactory.Identifier($"{node.First.Alias}Row"),
                            SyntaxFactory.IdentifierName($"{node.First.Alias}Rows.Rows"),
                            Block(
                                GetRowsSourceOrEmpty(node.Second.Alias),
                                SyntaxFactory.ForEachStatement(
                                    SyntaxFactory.IdentifierName("var"),
                                    SyntaxFactory.Identifier($"{node.Second.Alias}Row"),
                                    SyntaxFactory.IdentifierName($"{node.Second.Alias}Rows.Rows"),
                                    SyntaxFactory.Block(
                                        GenerateCancellationExpression(),
                                        _emptyBlock)))));
                break;
            case ApplyType.Outer:

                var fullTransitionTable = _scope.ScopeSymbolTable.GetSymbol<TableSymbol>(_queryAlias);
                var expressions = new List<ExpressionSyntax>();

                foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[0]))
                {
                    expressions.Add(
                        SyntaxFactory.ElementAccessExpression(
                            SyntaxFactory.IdentifierName($"{node.First.Alias}Row"),
                            SyntaxFactory.BracketedArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(
                                        (LiteralExpressionSyntax) Generator.LiteralExpression(
                                            column.ColumnName))))));
                }

                foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[1]))
                {
                    expressions.Add(
                        SyntaxFactory.CastExpression(
                            SyntaxFactory.IdentifierName(EvaluationHelper.GetCastableType(column.ColumnType)),
                            (LiteralExpressionSyntax) Generator.NullLiteralExpression()));
                }

                var arrayType = SyntaxFactory.ArrayType(
                    SyntaxFactory.IdentifierName("object"),
                    new SyntaxList<ArrayRankSpecifierSyntax>(
                        SyntaxFactory.ArrayRankSpecifier(
                            SyntaxFactory.SingletonSeparatedList(
                                (ExpressionSyntax) SyntaxFactory.OmittedArraySizeExpression()))));

                var rewriteSelect =
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier("select"),
                                null,
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.ArrayCreationExpression(
                                        arrayType,
                                        SyntaxFactory.InitializerExpression(
                                            SyntaxKind.ArrayInitializerExpression,
                                            SyntaxFactory.SeparatedList(expressions)))))));


                var invocation = SyntaxHelper.CreateMethodInvocation(
                    _scope[MetaAttributes.SelectIntoVariableName],
                    nameof(Table.Add),
                    [
                        SyntaxFactory.Argument(
                            SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.Token(SyntaxKind.NewKeyword)
                                    .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                                SyntaxFactory.ParseTypeName(nameof(ObjectsRow)),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SeparatedList(
                                    [
                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("select")),
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.IdentifierName($"{node.First.Alias}Row"),
                                                SyntaxFactory.IdentifierName(
                                                    $"{nameof(IObjectResolver.Contexts)}"))),
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))
                                    ])
                                ),
                                SyntaxFactory.InitializerExpression(SyntaxKind.ComplexElementInitializerExpression))
                        )
                    ]);

                computingBlock =
                    computingBlock.AddStatements(
                        GetRowsSourceOrEmpty(node.First.Alias),
                        SyntaxFactory.ForEachStatement(SyntaxFactory.IdentifierName("var"),
                            SyntaxFactory.Identifier($"{node.First.Alias}Row"),
                            SyntaxFactory.IdentifierName($"{node.First.Alias}Rows.Rows"),
                            Block(
                                SyntaxFactory.LocalDeclarationStatement(
                                    SyntaxHelper.CreateAssignment("hasAnyRowMatched",
                                        (LiteralExpressionSyntax) Generator.FalseLiteralExpression())),
                                GetRowsSourceOrEmpty(node.Second.Alias),
                                SyntaxFactory.ForEachStatement(
                                    SyntaxFactory.IdentifierName("var"),
                                    SyntaxFactory.Identifier($"{node.Second.Alias}Row"),
                                    SyntaxFactory.IdentifierName($"{node.Second.Alias}Rows.Rows"),
                                    SyntaxFactory.Block(
                                        GenerateCancellationExpression(),
                                        _emptyBlock,
                                        SyntaxFactory.IfStatement(
                                            (PrefixUnaryExpressionSyntax) Generator.LogicalNotExpression(
                                                SyntaxFactory.IdentifierName("hasAnyRowMatched")),
                                            SyntaxFactory.Block(
                                                SyntaxFactory.ExpressionStatement(
                                                    SyntaxFactory.AssignmentExpression(
                                                        SyntaxKind.SimpleAssignmentExpression,
                                                        SyntaxFactory.IdentifierName("hasAnyRowMatched"),
                                                        (LiteralExpressionSyntax) Generator
                                                            .TrueLiteralExpression())))))),
                                SyntaxFactory.IfStatement(
                                    (PrefixUnaryExpressionSyntax) Generator.LogicalNotExpression(
                                        SyntaxFactory.IdentifierName("hasAnyRowMatched")),
                                    SyntaxFactory.Block(
                                        SyntaxFactory.LocalDeclarationStatement(rewriteSelect),
                                        SyntaxFactory.ExpressionStatement(invocation))))));
                break;
        }

        _joinOrApplyBlock = computingBlock;
    }

    public void Visit(InMemoryTableFromNode node)
    {
        var tableArgument = SyntaxFactory.Argument(
            SyntaxFactory
                .ElementAccessExpression(
                    SyntaxFactory.IdentifierName("_tableResults")).WithArgumentList(
                    SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(
                                        _inMemoryTableIndexes[
                                            node.VariableName])))))));

        var literalTrueArgument = SyntaxFactory.Argument(
            SyntaxFactory.LiteralExpression(
                SyntaxKind.TrueLiteralExpression));

        _getRowsSourceStatement.Add(node.Alias, SyntaxFactory.LocalDeclarationStatement(SyntaxFactory
            .VariableDeclaration(SyntaxFactory.IdentifierName("var")).WithVariables(
                SyntaxFactory.SingletonSeparatedList(SyntaxFactory
                    .VariableDeclarator(SyntaxFactory.Identifier(node.Alias.ToRowsSource())).WithInitializer(
                        SyntaxFactory.EqualsValueClause(SyntaxFactory
                            .InvocationExpression(SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                                SyntaxFactory.IdentifierName(nameof(EvaluationHelper.ConvertTableToSource))))
                            .WithArgumentList(
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SeparatedList([
                                        tableArgument,
                                        literalTrueArgument
                                    ])))))))));
    }

    public void Visit(JoinFromNode node)
    {
    }

    public void Visit(ApplyFromNode node)
    {
    }

    public void Visit(ExpressionFromNode node)
    {
        Nodes.Push(SyntaxFactory.Block());
    }

    public void Visit(AccessMethodFromNode node)
    {
        AddNamespace(node.ReturnType);

        _getRowsSourceStatement.Add(node.Alias, SyntaxFactory.LocalDeclarationStatement(SyntaxFactory
            .VariableDeclaration(SyntaxFactory.IdentifierName("var")).WithVariables(
                SyntaxFactory.SingletonSeparatedList(SyntaxFactory
                    .VariableDeclarator(SyntaxFactory.Identifier(node.Alias.ToRowsSource())).WithInitializer(
                        SyntaxFactory.EqualsValueClause(SyntaxFactory
                            .InvocationExpression(SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                                SyntaxFactory.IdentifierName(nameof(EvaluationHelper.ConvertEnumerableToSource))))
                            .WithArgumentList(
                                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.CastExpression(
                                            SyntaxFactory.ParseTypeName(
                                                EvaluationHelper.GetCastableType(node.ReturnType)),
                                            (ExpressionSyntax) Nodes.Pop())))))))))));
    }

    public void Visit(SchemaMethodFromNode node)
    {
    }

    public void Visit(PropertyFromNode node)
    {
        AddNamespace(node.ReturnType);

        ExpressionSyntax propertyAccess = SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.CastExpression(
                SyntaxFactory.ParseTypeName(EvaluationHelper.GetCastableType(node.PropertiesChain[0].PropertyType)),
                SyntaxFactory.ElementAccessExpression(
                    SyntaxFactory.IdentifierName($"{node.SourceAlias}Row"),
                    SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    SyntaxFactory.Literal(node.PropertiesChain[0].PropertyName))))))));

        for (var i = 1; i < node.PropertiesChain.Length; i++)
        {
            propertyAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                propertyAccess,
                SyntaxFactory.IdentifierName(node.PropertiesChain[i].PropertyName));
        }

        var statement = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(node.Alias.ToRowsSource()))
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                                                SyntaxFactory.IdentifierName(
                                                    nameof(EvaluationHelper.ConvertEnumerableToSource))))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.CastExpression(
                                                            SyntaxFactory.ParseTypeName(
                                                                EvaluationHelper.GetCastableType(node.ReturnType)),
                                                            propertyAccess))))))))));

        _getRowsSourceStatement.Add(node.Alias, statement);
    }

    public void Visit(AliasedFromNode node)
    {
    }

    public void Visit(CreateTransformationTableNode node)
    {
        if (!node.ForGrouping)
        {
            var cols = new List<ExpressionSyntax>();

            foreach (var field in node.Fields)
            {
                var type = field.ReturnType;

                var types = EvaluationHelper.GetNestedTypes(type);

                AddNamespace(types);
                AddReference(types);

                cols.Add(
                    SyntaxHelper.CreateObjectOf(
                        nameof(Column),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList([
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        SyntaxFactory.Literal(
                                            $"@\"{field.FieldName.Replace("\"", "'")}\"",
                                            field.FieldName))),
                                SyntaxHelper.TypeLiteralArgument(
                                    EvaluationHelper.GetCastableType(type)),
                                SyntaxHelper.IntLiteralArgument(field.FieldOrder)
                            ]))));
            }

            var createObject = SyntaxHelper.CreateAssignment(
                _scope[MetaAttributes.CreateTableVariableName],
                SyntaxHelper.CreateObjectOf(
                    nameof(Table),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                        [
                            SyntaxFactory.Argument((ExpressionSyntax) Generator.LiteralExpression(node.Name)),
                            SyntaxFactory.Argument(
                                SyntaxHelper.CreateArrayOf(
                                    nameof(Column),
                                    cols.ToArray()))
                        ]))));

            Statements.Add(SyntaxFactory.LocalDeclarationStatement(createObject));
        }
        else
        {
            var createObject = SyntaxHelper.CreateAssignment(
                _scope[MetaAttributes.CreateTableVariableName],
                SyntaxHelper.CreateObjectOf(
                    NamingHelper.ListOf<Group>(),
                    SyntaxFactory.ArgumentList()));
            Statements.Add(SyntaxFactory.LocalDeclarationStatement(createObject));
        }
    }

    public void Visit(RenameTableNode node)
    {
    }

    public void Visit(TranslatedSetTreeNode node)
    {
    }

    public void Visit(IntoNode node)
    {
    }

    public void Visit(QueryScope node)
    {
    }

    public void Visit(ShouldBePresentInTheTable node)
    {
    }

    public void Visit(TranslatedSetOperatorNode node)
    {
    }

    public void Visit(QueryNode node)
    {
        var detailedQuery = (DetailedQueryNode) node;

        var orderByFields = detailedQuery.OrderBy is not null
            ? new (FieldOrderedNode Field, ExpressionSyntax Syntax)[detailedQuery.OrderBy.Fields.Length]
            : [];

        for (var i = orderByFields.Length - 1; i >= 0; i--)
        {
            var orderBy = detailedQuery.OrderBy!;
            var field = orderBy.Fields[i];
            var syntax = (ExpressionSyntax) Nodes.Pop();
            orderByFields[i] = (field, syntax);
        }

        var skip = node.Skip != null ? Nodes.Pop() as StatementSyntax : null;
        var take = node.Take != null ? Nodes.Pop() as BlockSyntax : null;

        var select = _selectBlock;
        var where = node.Where != null ? Nodes.Pop() as StatementSyntax : null;

        var block = (BlockSyntax) Nodes.Pop();

        block = block.AddStatements(GenerateCancellationExpression());

        if (where != null)
            block = block.AddStatements(where);

        block = block.AddStatements(GenerateStatsUpdateStatements());

        if (skip != null)
            block = block.AddStatements(skip);

        if (take != null)
            block = block.AddStatements(take.Statements.ToArray());
        block = block.AddStatements(select.Statements.ToArray());
        var fullBlock = SyntaxFactory.Block();

        fullBlock = fullBlock.AddStatements(
            GetRowsSourceOrEmpty(node.From.Alias),
            _isResultParallelizationImpossible
                ? SyntaxHelper.Foreach("score", _scope[MetaAttributes.SourceName], block, orderByFields)
                : SyntaxHelper.ParallelForeach("score", _scope[MetaAttributes.SourceName], block));

        fullBlock = fullBlock.AddStatements(
            (StatementSyntax) Generator.ReturnStatement(
                SyntaxFactory.IdentifierName(detailedQuery.ReturnVariableName)));

        Statements.AddRange(fullBlock.Statements);

        _getRowsSourceStatement.Clear();
        _isResultParallelizationImpossible = false;
    }

    public void Visit(InternalQueryNode node)
    {
        var select = _selectBlock;
        var where = node.Where != null ? Nodes.Pop() as StatementSyntax : null;

        var block = (BlockSyntax) Nodes.Pop();

        if (node.GroupBy != null)
        {
            Statements.Add(SyntaxFactory
                .ParseStatement("var rootGroup = new Group(null, new string[0], new string[0]);")
                .WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturn)).NormalizeWhitespace());
            Statements.Add(SyntaxFactory.ParseStatement("var usedGroups = new HashSet<Group>();")
                .WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturn)).NormalizeWhitespace());
            Statements.Add(SyntaxFactory.ParseStatement("var groups = new Dictionary<GroupKey, Group>();")
                .WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturn)).NormalizeWhitespace());

            block = block.AddStatements(GenerateCancellationExpression());

            if (where != null)
                block = block.AddStatements(where);

            block = block.AddStatements(SyntaxFactory.LocalDeclarationStatement(_groupKeys));
            block = block.AddStatements(SyntaxFactory.LocalDeclarationStatement(_groupValues));

            block = block.AddStatements(SyntaxFactory.ParseStatement("var parent = rootGroup;")
                    .WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturn)))
                .NormalizeWhitespace();
            block = block.AddStatements(SyntaxFactory.ParseStatement("Group group = null;")
                    .WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturn)))
                .NormalizeWhitespace();

            block = block.AddStatements(GroupForStatement());

            if (node.Refresh.Nodes.Length > 0)
                block = block.AddStatements(((BlockSyntax) Nodes.Pop()).Statements.ToArray());

            if (node.GroupBy.Having != null)
                block = block.AddStatements((StatementSyntax) _groupHaving);

            var indexToColumnMapCode = new InitializerExpressionSyntax[node.Select.Fields.Length];

            for (int i = 0, j = node.Select.Fields.Length - 1; i < node.Select.Fields.Length; i++, --j)
                indexToColumnMapCode[i] =
                    SyntaxFactory.InitializerExpression(
                        SyntaxKind.ComplexElementInitializerExpression,
                        SyntaxFactory.SeparatedList<ExpressionSyntax>()
                            .Add((LiteralExpressionSyntax) Generator.LiteralExpression(j))
                            .Add((LiteralExpressionSyntax) Generator.LiteralExpression(
                                node.Select.Fields[i].FieldName.Replace("\"", "'"))));

            const string indexToValueDictVariableName = "indexToValueDict";

            var columnToValueDict = SyntaxHelper.CreateAssignment(
                indexToValueDictVariableName, SyntaxHelper.CreateObjectOf(
                    "Dictionary<int, string>",
                    SyntaxFactory.ArgumentList(),
                    SyntaxFactory.InitializerExpression(
                        SyntaxKind.ObjectInitializerExpression,
                        SyntaxFactory.SeparatedList<ExpressionSyntax>()
                            .AddRange(indexToColumnMapCode))));

            Statements.Add(SyntaxFactory.LocalDeclarationStatement(columnToValueDict));

            block = block.AddStatements(AddGroupStatement(node.From.Alias.ToGroupingTable()));
            block = GroupByForeach(block, node.From.Alias, node.From.Alias.ToRowItem(),
                _scope[MetaAttributes.SourceName]);
            Statements.AddRange(block.Statements);
        }
        else
        {
            _emptyBlock = _joinOrApplyBlock.DescendantNodes().OfType<BlockSyntax>()
                .First(f => f.Statements.Count == 0);
            _joinOrApplyBlock = _joinOrApplyBlock.ReplaceNode(_emptyBlock, select.Statements);
            Statements.AddRange(_joinOrApplyBlock.Statements);
        }

        _getRowsSourceStatement.Clear();
        _isResultParallelizationImpossible = false;
    }

    private StatementSyntax GenerateCancellationExpression()
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("token"),
                    SyntaxFactory.IdentifierName(
                        nameof(CancellationToken.ThrowIfCancellationRequested)))));
    }

    public void Visit(RootNode node)
    {
        var methodCallExpression = $"{_methodNames.Pop()}(Provider, PositionalEnvironmentVariables, QueriesInformation, Logger, token)";
        var method = MethodDeclarationHelper.CreateRunMethod(methodCallExpression);

        var providerParam = MethodDeclarationHelper.CreatePublicProperty(nameof(ISchemaProvider), nameof(IRunnable.Provider));

        var positionalEnvironmentVariablesParam = MethodDeclarationHelper.CreatePositionalEnvironmentVariablesProperty();

        var queriesInformationParam = MethodDeclarationHelper.CreateQueriesInformationProperty();

        var loggerParam = MethodDeclarationHelper.CreatePublicProperty(nameof(ILogger), nameof(IRunnable.Logger));

        _members.Add(method);
        _members.Add(providerParam);
        _members.Add(positionalEnvironmentVariablesParam);
        _members.Add(queriesInformationParam);
        _members.Add(loggerParam);

        var inMemoryTables = SyntaxFactory
            .FieldDeclaration(SyntaxFactory
                .VariableDeclaration(SyntaxFactory.ArrayType(SyntaxFactory.IdentifierName(nameof(Table)))
                    .WithRankSpecifiers(SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(
                            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                SyntaxFactory.OmittedArraySizeExpression()))))).WithVariables(
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory
                        .VariableDeclarator(SyntaxFactory.Identifier("_tableResults")).WithInitializer(
                            SyntaxFactory.EqualsValueClause(SyntaxFactory.ArrayCreationExpression(SyntaxFactory
                                .ArrayType(SyntaxFactory.IdentifierName(nameof(Table))).WithRankSpecifiers(
                                    SyntaxFactory.SingletonList(
                                        SyntaxFactory.ArrayRankSpecifier(
                                            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                                    SyntaxFactory.Literal(_inMemoryTableIndex))))))))))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));

        _members.Insert(0, inMemoryTables);

        var classDeclaration = Generator.ClassDeclaration(ClassName, Array.Empty<string>(), Accessibility.Public,
            DeclarationModifiers.None,
            null,
            [
                SyntaxFactory.IdentifierName(nameof(BaseOperations)),
                SyntaxFactory.IdentifierName(nameof(IRunnable))
            ], _members);

        var ns = SyntaxFactory.NamespaceDeclaration(
            SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(Namespace)),
            SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
            SyntaxFactory.List(
                _namespaces.Select(
                    n => SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(n)))),
            SyntaxFactory.List<MemberDeclarationSyntax>([(ClassDeclarationSyntax) classDeclaration]));

        var compilationUnit = SyntaxFactory.CompilationUnit(
            SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
            SyntaxFactory.List<UsingDirectiveSyntax>(),
            SyntaxFactory.List<AttributeListSyntax>(),
            SyntaxFactory.List<MemberDeclarationSyntax>([ns]));

        var options = Workspace.Options;
        options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInMethods, true);
        options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInProperties, true);

        var formatted = Formatter.Format(compilationUnit, Workspace);

        Compilation = Compilation.AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(formatted.ToFullString(),
            new CSharpParseOptions(LanguageVersion.CSharp8), null, Encoding.ASCII));
    }

    public void Visit(SingleSetNode node)
    {
    }

    public void Visit(UnionNode node)
    {
        var b = _methodNames.Pop();
        var a = _methodNames.Pop();
        var name = $"{a}_Union_{b}";
        _methodNames.Push(name);

        var aInvocation = SyntaxFactory
            .InvocationExpression(SyntaxFactory.IdentifierName(a))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                        [
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("provider")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("queriesInformation")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("logger")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("token"))
                        ]
                    )));

        var bInvocation = SyntaxFactory
            .InvocationExpression(SyntaxFactory.IdentifierName(b))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                        [
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("provider")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("queriesInformation")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("logger")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("token"))
                        ]
                    )));

        _members.Add(GenerateMethod(name, nameof(BaseOperations.Union), _scope[MetaAttributes.SetOperatorName],
            aInvocation, bInvocation));
    }

    public void Visit(UnionAllNode node)
    {
        var b = _methodNames.Pop();
        var a = _methodNames.Pop();
        var name = $"{a}_UnionAll_{b}";
        _methodNames.Push(name);

        var aInvocation = SyntaxFactory
            .InvocationExpression(SyntaxFactory.IdentifierName(a))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                        [
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("provider")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("queriesInformation")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("logger")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("token"))
                        ]
                    )));

        var bInvocation = SyntaxFactory
            .InvocationExpression(SyntaxFactory.IdentifierName(b))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                        [
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("provider")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("queriesInformation")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("logger")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("token"))
                        ]
                    )));

        _members.Add(GenerateMethod(name, nameof(BaseOperations.UnionAll), _scope[MetaAttributes.SetOperatorName],
            aInvocation, bInvocation));
    }

    public void Visit(ExceptNode node)
    {
        var b = _methodNames.Pop();
        var a = _methodNames.Pop();
        var name = $"{a}_Except_{b}";
        _methodNames.Push(name);

        var aInvocation = SyntaxFactory
            .InvocationExpression(SyntaxFactory.IdentifierName(a))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                        [
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("provider")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("queriesInformation")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("logger")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("token"))
                        ]
                    )));

        var bInvocation = SyntaxFactory
            .InvocationExpression(SyntaxFactory.IdentifierName(b))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                        [
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("provider")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("queriesInformation")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("logger")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("token"))
                        ]
                    )));

        _members.Add(GenerateMethod(name, nameof(BaseOperations.Except), _scope[MetaAttributes.SetOperatorName],
            aInvocation, bInvocation));
    }

    public void Visit(IntersectNode node)
    {
        var b = _methodNames.Pop();
        var a = _methodNames.Pop();
        var name = $"{a}_Intersect_{b}";
        _methodNames.Push(name);

        var aInvocation = SyntaxFactory
            .InvocationExpression(SyntaxFactory.IdentifierName(a))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                        [
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("provider")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("queriesInformation")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("logger")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("token"))
                        ]
                    )));

        var bInvocation = SyntaxFactory
            .InvocationExpression(SyntaxFactory.IdentifierName(b))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                        [
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("provider")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("queriesInformation")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("logger")),
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("token"))
                        ]
                    )));

        _members.Add(GenerateMethod(name, nameof(BaseOperations.Intersect), _scope[MetaAttributes.SetOperatorName],
            aInvocation, bInvocation));
    }

    public void Visit(RefreshNode node)
    {
        if (node.Nodes.Length == 0)
            return;

        var block = SyntaxFactory.Block();
        for (var i = 0; i < node.Nodes.Length; i++)
            block = block.AddStatements(
                SyntaxFactory.ExpressionStatement((ExpressionSyntax) Nodes.Pop()));

        Nodes.Push(block);
    }

    public void Visit(PutTrueNode node)
    {
        Nodes.Push(Generator.ValueEqualsExpression(Generator.LiteralExpression(1), Generator.LiteralExpression(1)));
    }

    public void Visit(MultiStatementNode node)
    {
        Statements.Insert(0, SyntaxFactory.LocalDeclarationStatement(
            SyntaxHelper.CreateAssignment(
                "stats",
                SyntaxHelper.CreateObjectOf(
                    nameof(AmendableQueryStats),
                    SyntaxFactory.ArgumentList()))));

        var methodName = $"{_scope[MetaAttributes.MethodName]}_{_setOperatorMethodIdentifier}";
        if (_scope.IsInsideNamedScope("CTE Inner Expression"))
            methodName = $"{methodName}_Inner_Cte";

        _methodNames.Push(methodName);

        var method = MethodDeclarationHelper.CreateStandardPrivateMethod(methodName, SyntaxFactory.Block(Statements));

        _members.Add(method);
        _typesToInstantiate.Clear();
        Statements.Clear();
    }

    public void Visit(CteExpressionNode node)
    {
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(node, _methodNames, Nodes);
        
        _members.Add(result.Method);
        _methodNames.Push(result.MethodName);
    }

    public void Visit(CteInnerExpressionNode node)
    {
        if (!_inMemoryTableIndexes.ContainsKey(node.Name))
            _inMemoryTableIndexes.Add(node.Name, _inMemoryTableIndex++);

        Nodes.Push(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName("_tableResults")).WithArgumentList(
                SyntaxFactory.BracketedArgumentList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(_inMemoryTableIndexes[node.Name])))))),
            SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(_methodNames.Peek())).WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                    [
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("provider")),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("queriesInformation")),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("logger")),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("token"))
                    ]))))));
    }

    public void Visit(JoinNode node)
    {
    }

    public void Visit(ApplyNode node)
    {
    }

    public void SetScope(Scope scope)
    {
        _scope = scope;
    }

    public void SetQueryIdentifier(string identifier)
    {
        _queryAlias = identifier;
    }

    public MethodAccessType SetMethodAccessType(MethodAccessType type)
    {
        _oldType = _type;
        _type = type;

        return _oldType;
    }

    public void SetResultParallelizationImpossible()
    {
        _isResultParallelizationImpossible = true;
    }

    public void IncrementMethodIdentifier()
    {
        _setOperatorMethodIdentifier += 1;
    }

    public void SetInsideJoinOrApply(bool state)
    {
        _isInsideJoinOrApply = state;
    }

    public void AddNullSuspiciousSection()
    {
        NullSuspiciousNodes.Add(new Stack<SyntaxNode>());
    }

    public void RemoveNullSuspiciousSection()
    {
        NullSuspiciousNodes.RemoveAt(NullSuspiciousNodes.Count - 1);
    }

    private void AddNamespace(string columnTypeNamespace)
    {
        if (!_namespaces.Contains(columnTypeNamespace))
            _namespaces.Add(columnTypeNamespace);
    }

    private void AddNamespace(params Type[] types)
    {
        foreach (var type in types)
            AddNamespace(type.Namespace);
    }

    private void AddReference(params Type[] types)
    {
        foreach (var type in types)
        {
            if (_loadedAssemblies.Contains(type.Assembly.Location)) continue;

            _loadedAssemblies.Add(type.Assembly.Location);

            var reference = MetadataReference.CreateFromFile(type.Assembly.Location);
            
            Compilation =
                Compilation.AddReferences(reference);
        }
    }

    private void AddReference(params string[] assemblyDllsPaths)
    {
        foreach (var assemblyDllPath in assemblyDllsPaths)
        {
            if (_loadedAssemblies.Contains(assemblyDllPath)) continue;

            _loadedAssemblies.Add(assemblyDllPath);

            var reference = MetadataReference.CreateFromFile(assemblyDllPath);
            
            Compilation =
                Compilation.AddReferences(reference);
        }
    }

    private void AddReference(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            if (_loadedAssemblies.Contains(assembly.Location)) continue;

            _loadedAssemblies.Add(assembly.Location);

            var reference = MetadataReference.CreateFromFile(assembly.Location);
            
            Compilation =
                Compilation.AddReferences(reference);
        }
    }

    private StatementSyntax GenerateStatsUpdateStatements()
    {
        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier("currentRowStats"))
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName("stats"),
                                            SyntaxFactory.IdentifierName(nameof(AmendableQueryStats
                                                .IncrementRowNumber)))))))));
    }

    private BlockSyntax GroupByForeach(BlockSyntax foreachInstructions, string alias, string variableName,
        string tableVariable)
    {
        return Block(
            GetRowsSourceOrEmpty(alias),
            SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier(variableName),
                SyntaxFactory.IdentifierName(tableVariable),
                foreachInstructions).NormalizeWhitespace());
    }

    private StatementSyntax AddGroupStatement(string scoreTable)
    {
        return SyntaxFactory.IfStatement(
            SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression,
                SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("usedGroups"), SyntaxFactory.IdentifierName("Contains")))
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("group")))))),
            SyntaxFactory.Block(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(scoreTable),
                            SyntaxFactory.IdentifierName("Add")))
                        .WithArgumentList(SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("group")))))),
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("usedGroups"), SyntaxFactory.IdentifierName("Add")))
                        .WithArgumentList(
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("group"))))))));
    }

    private StatementSyntax GroupForStatement()
    {
        return
            SyntaxFactory.ForStatement(SyntaxFactory.Block(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName("token"),
                                SyntaxFactory.IdentifierName(nameof(CancellationToken
                                    .ThrowIfCancellationRequested))))),
                    SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(
                                SyntaxFactory.IdentifierName("var"))
                            .WithVariables(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.VariableDeclarator(
                                            SyntaxFactory.Identifier("key"))
                                        .WithInitializer(
                                            SyntaxFactory.EqualsValueClause(
                                                SyntaxFactory
                                                    .ElementAccessExpression(SyntaxFactory.IdentifierName("keys"))
                                                    .WithArgumentList(SyntaxFactory.BracketedArgumentList(
                                                        SyntaxFactory.SingletonSeparatedList(
                                                            SyntaxFactory.Argument(
                                                                SyntaxFactory.IdentifierName("i")))))))))),
                    SyntaxFactory.IfStatement(
                            SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName("groups"),
                                SyntaxFactory.IdentifierName("ContainsKey"))).WithArgumentList(
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key"))))),
                            SyntaxFactory.Block(SyntaxFactory.SingletonList<StatementSyntax>(
                                SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName("group"),
                                    SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName("groups"))
                                        .WithArgumentList(
                                            SyntaxFactory.BracketedArgumentList(
                                                SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.IdentifierName("key"))))))))))
                        .WithElse(SyntaxFactory.ElseClause(SyntaxFactory.Block(
                            SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                SyntaxFactory.IdentifierName("group"),
                                SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName("Group"))
                                    .WithArgumentList(SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SeparatedList<ArgumentSyntax>(new SyntaxNodeOrToken[]
                                        {
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("parent")),
                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                            SyntaxFactory.Argument(SyntaxFactory
                                                .ElementAccessExpression(
                                                    SyntaxFactory.IdentifierName("groupFieldsNames"))
                                                .WithArgumentList(SyntaxFactory.BracketedArgumentList(
                                                    SyntaxFactory.SingletonSeparatedList(
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName("i")))))),
                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                            SyntaxFactory.Argument(SyntaxFactory
                                                .ElementAccessExpression(SyntaxFactory.IdentifierName("values"))
                                                .WithArgumentList(SyntaxFactory.BracketedArgumentList(
                                                    SyntaxFactory.SingletonSeparatedList(
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName("i"))))))
                                        }))))),
                            SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("groups"),
                                        SyntaxFactory.IdentifierName("Add")))
                                    .WithArgumentList(
                                        SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                            new SyntaxNodeOrToken[]
                                            {
                                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key")),
                                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("group"))
                                            }))))))),
                    SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName("parent"), SyntaxFactory.IdentifierName("group")))))
                .WithDeclaration(SyntaxFactory
                    .VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                    .WithVariables(
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory
                            .VariableDeclarator(SyntaxFactory.Identifier("i"))
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(0)))))))
                .WithCondition(SyntaxFactory.BinaryExpression(SyntaxKind.LessThanExpression,
                    SyntaxFactory.IdentifierName("i"),
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("keys"),
                        SyntaxFactory.IdentifierName("Length"))))
                .WithIncrementors(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                    SyntaxFactory.PrefixUnaryExpression(SyntaxKind.PreIncrementExpression,
                        SyntaxFactory.IdentifierName("i"))));
    }

    private MethodDeclarationSyntax GenerateMethod(string methodName, string setOperator, string key,
        ExpressionSyntax firstTableExpression, ExpressionSyntax secondTableExpression)
    {
        var body = SyntaxFactory.Block(
            SyntaxFactory.SingletonList<StatementSyntax>(
                SyntaxFactory.ReturnStatement(
                    SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(setOperator))
                        .WithArgumentList(
                            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                new SyntaxNodeOrToken[]
                                {
                                    SyntaxFactory.Argument(firstTableExpression),
                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                    SyntaxFactory.Argument(secondTableExpression),
                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                    SyntaxFactory.Argument(SyntaxFactory
                                        .ParenthesizedLambdaExpression(
                                            GenerateLambdaBody("first", "second", key))
                                        .WithParameterList(SyntaxFactory.ParameterList(
                                            SyntaxFactory.SeparatedList<ParameterSyntax>(
                                                new SyntaxNodeOrToken[]
                                                {
                                                    SyntaxFactory.Parameter(
                                                        SyntaxFactory.Identifier("first")),
                                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                    SyntaxFactory.Parameter(
                                                        SyntaxFactory.Identifier("second"))
                                                }))))
                                }))))));

        return MethodDeclarationHelper.CreateStandardPrivateMethod(methodName, body);
    }

    private CSharpSyntaxNode GenerateLambdaBody(string first, string second, string key)
    {
        var indexes = _setOperatorFieldIndexes[key];
        var equality = SyntaxFactory
            .InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName(first)).WithArgumentList(
                        SyntaxFactory.BracketedArgumentList(SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(indexes[0])))))), SyntaxFactory.IdentifierName("Equals")))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Argument(SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName(second))
                    .WithArgumentList(SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(indexes[0]))))))))));


        var subExpressions = new Stack<ExpressionSyntax>();
        subExpressions.Push(equality);

        for (var i = 1; i < indexes.Length; i++)
        {
            equality = SyntaxFactory
                .InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName(first)).WithArgumentList(
                            SyntaxFactory.BracketedArgumentList(SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(indexes[i])))))), SyntaxFactory.IdentifierName("Equals")))
                .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(SyntaxFactory
                        .ElementAccessExpression(SyntaxFactory.IdentifierName(second))
                        .WithArgumentList(SyntaxFactory.BracketedArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(indexes[i]))))))))));

            subExpressions.Push(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.LogicalAndExpression,
                    subExpressions.Pop(),
                    equality));
        }

        return subExpressions.Pop();
    }

    public void Visit(OrderByNode node)
    {
        AddNamespace("Musoq.Evaluator");
    }

    public void Visit(CreateTableNode node)
    {
    }

    public void Visit(CoupleNode node)
    {
    }

    public void Visit(StatementsArrayNode node)
    {
    }

    public void Visit(StatementNode node)
    {
    }

    public void Visit(CaseNode node)
    {
        var result = CaseNodeProcessor.ProcessCaseNode(
            node, Nodes, _typesToInstantiate, _oldType, _queryAlias, ref _caseWhenMethodIndex);
        
        foreach (var ns in result.RequiredNamespaces)
        {
            AddNamespace(ns);
        }
        
        _members.Add(result.Method);
        Nodes.Push(result.MethodInvocation);
    }

    public void Visit(WhenNode node)
    {
    }

    public void Visit(ThenNode node)
    {
    }

    public void Visit(ElseNode node)
    {
    }

    public void Visit(FieldLinkNode node)
    {
        throw new NotSupportedException();
    }

    private ObjectCreationExpressionSyntax CreateRuntimeContext(SchemaFromNode node,
        ExpressionSyntax originallyInferredColumns)
    {
        return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.IdentifierName(nameof(RuntimeContext)))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                    [
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("token")),
                        SyntaxFactory.Argument(
                            originallyInferredColumns),
                        SyntaxFactory.Argument(
                            SyntaxFactory.ElementAccessExpression(
                                    SyntaxFactory.IdentifierName(
                                        "positionalEnvironmentVariables"))
                                .WithArgumentList(
                                    SyntaxFactory.BracketedArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(
                                                SyntaxFactory.LiteralExpression(
                                                    SyntaxKind.NumericLiteralExpression,
                                                    SyntaxFactory.Literal(
                                                        _schemaFromIndex++))))))
                        ),
                        SyntaxFactory.Argument(
                            SyntaxFactory.ElementAccessExpression(
                                    SyntaxFactory.IdentifierName("queriesInformation"))
                                .WithArgumentList(
                                    SyntaxFactory.BracketedArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(
                                                SyntaxFactory.LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    SyntaxFactory.Literal(node.Id))))))),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("logger"))
                    ])));
    }

    private void CreateDescForSpecificConstructor(DescNode node)
    {
        CreateDescMethod(node,
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                        SyntaxFactory.IdentifierName(nameof(EvaluationHelper.GetSpecificTableDescription))))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("schemaTable"))))), true);
    }

    private void CreateDescForSchema(DescNode node)
    {
        CreateDescMethod(node,
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                        SyntaxFactory.IdentifierName(nameof(EvaluationHelper.GetSpecificSchemaDescriptions))))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("desc"))))), false);
    }

    private void CreateDescForConstructors(DescNode node)
    {
        CreateDescMethod(node,
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                        SyntaxFactory.IdentifierName(nameof(EvaluationHelper.GetConstructorsForSpecificMethod))))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                        [
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("desc")),
                            SyntaxHelper.StringLiteralArgument(((SchemaFromNode) node.From).Method)
                        ]))), false);
    }

    private void CreateDescMethod(DescNode node, InvocationExpressionSyntax invocationExpression,
        bool useProvidedTable)
    {
        var schemaNode = (SchemaFromNode) node.From;
        var createdSchema = SyntaxHelper.CreateAssignmentByMethodCall(
            "desc",
            "provider",
            nameof(ISchemaProvider.GetSchema),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                SyntaxFactory.SeparatedList([
                    SyntaxHelper.StringLiteralArgument(schemaNode.Schema)
                ]),
                SyntaxFactory.Token(SyntaxKind.CloseParenToken)
            )
        );

        if (useProvidedTable)
        {
            var args = schemaNode.Parameters.Args.Select(arg =>
                (ExpressionSyntax) Generator.LiteralExpression(((ConstantValueNode) arg).ObjValue)).ToArray();

            var originallyInferredColumns = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Array"),
                        SyntaxFactory.GenericName(
                                SyntaxFactory.Identifier("Empty"))
                            .WithTypeArgumentList(
                                SyntaxFactory.TypeArgumentList(
                                    SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                        SyntaxFactory.IdentifierName("ISchemaColumn"))))))
                .NormalizeWhitespace();

            var getTable = SyntaxHelper.CreateAssignmentByMethodCall(
                "schemaTable",
                "desc",
                nameof(ISchema.GetTableByName),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                    SyntaxFactory.SeparatedList([
                        SyntaxHelper.StringLiteralArgument(schemaNode.Method),
                        SyntaxFactory.Argument(CreateRuntimeContext(schemaNode, originallyInferredColumns)),
                        SyntaxFactory.Argument(SyntaxHelper.CreateArrayOf(nameof(Object), args))
                    ]),
                    SyntaxFactory.Token(SyntaxKind.CloseParenToken)
                )
            );

            var returnStatement = SyntaxFactory.ReturnStatement(invocationExpression);

            Statements.AddRange([
                SyntaxFactory.LocalDeclarationStatement(createdSchema),
                SyntaxFactory.LocalDeclarationStatement(getTable),
                returnStatement
            ]);
        }
        else
        {
            var returnStatement = SyntaxFactory.ReturnStatement(invocationExpression);

            Statements.AddRange([
                SyntaxFactory.LocalDeclarationStatement(createdSchema),
                returnStatement
            ]);
        }

        var methodName = "GetTableDesc";

        var method = SyntaxFactory.MethodDeclaration(
            [],
            SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
            SyntaxFactory.IdentifierName(nameof(Table)).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
            null,
            SyntaxFactory.Identifier(methodName),
            null,
            SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList([
                    SyntaxFactory.Parameter(
                        [],
                        SyntaxTokenList.Create(
                            new SyntaxToken()),
                        SyntaxFactory.IdentifierName(nameof(ISchemaProvider))
                            .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                        SyntaxFactory.Identifier("provider"), null),

                    SyntaxFactory.Parameter(
                        [],
                        SyntaxTokenList.Create(
                            new SyntaxToken()),
                        SyntaxFactory.GenericName(
                                SyntaxFactory.Identifier("IReadOnlyDictionary"))
                            .WithTypeArgumentList(
                                SyntaxFactory.TypeArgumentList(
                                    SyntaxFactory.SeparatedList<TypeSyntax>(
                                        new SyntaxNodeOrToken[]
                                        {
                                            SyntaxFactory.PredefinedType(
                                                SyntaxFactory.Token(SyntaxKind.UIntKeyword)),
                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                            SyntaxFactory.GenericName(
                                                    SyntaxFactory.Identifier("IReadOnlyDictionary"))
                                                .WithTypeArgumentList(
                                                    SyntaxFactory.TypeArgumentList(
                                                        SyntaxFactory.SeparatedList<TypeSyntax>(
                                                            new SyntaxNodeOrToken[]
                                                            {
                                                                SyntaxFactory.PredefinedType(
                                                                    SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                                                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                                SyntaxFactory.PredefinedType(
                                                                    SyntaxFactory.Token(SyntaxKind.StringKeyword))
                                                            })))
                                        })))
                            .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                        SyntaxFactory.Identifier("positionalEnvironmentVariables"), null),

                    SyntaxFactory.Parameter(
                            SyntaxFactory.Identifier("queriesInformation"))
                        .WithType(
                            SyntaxFactory.GenericName(
                                    SyntaxFactory.Identifier("IReadOnlyDictionary"))
                                .WithTypeArgumentList(
                                    SyntaxFactory.TypeArgumentList(
                                        SyntaxFactory.SeparatedList<TypeSyntax>(
                                            new SyntaxNodeOrToken[]
                                            {
                                                SyntaxFactory.PredefinedType(
                                                    SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                SyntaxFactory.TupleType(
                                                    SyntaxFactory.SeparatedList<TupleElementSyntax>(
                                                        new SyntaxNodeOrToken[]
                                                        {
                                                            SyntaxFactory.TupleElement(
                                                                    SyntaxFactory.IdentifierName("SchemaFromNode"))
                                                                .WithIdentifier(
                                                                    SyntaxFactory.Identifier("FromNode")),
                                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                            SyntaxFactory.TupleElement(
                                                                    SyntaxFactory.GenericName(
                                                                            SyntaxFactory.Identifier(
                                                                                "IReadOnlyCollection"))
                                                                        .WithTypeArgumentList(
                                                                            SyntaxFactory.TypeArgumentList(
                                                                                SyntaxFactory
                                                                                    .SingletonSeparatedList<
                                                                                        TypeSyntax>(
                                                                                        SyntaxFactory
                                                                                            .IdentifierName(
                                                                                                "ISchemaColumn")))))
                                                                .WithIdentifier(
                                                                    SyntaxFactory.Identifier("UsedColumns")),
                                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                            SyntaxFactory.TupleElement(
                                                                    SyntaxFactory.IdentifierName("WhereNode"))
                                                                .WithIdentifier(
                                                                    SyntaxFactory.Identifier("WhereNode")),
                                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                            SyntaxFactory.TupleElement(
                                                                    SyntaxFactory.IdentifierName("bool"))
                                                                .WithIdentifier(
                                                                    SyntaxFactory.Identifier(
                                                                        "HasExternallyProvidedTypes"))
                                                        }))
                                            })))),
                    
                    SyntaxFactory.Parameter(
                        [],
                        SyntaxTokenList.Create(
                            new SyntaxToken()),
                        SyntaxFactory.IdentifierName(nameof(ILogger))
                            .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                        SyntaxFactory.Identifier("logger"), null),

                    SyntaxFactory.Parameter(
                        [],
                        SyntaxTokenList.Create(
                            new SyntaxToken()),
                        SyntaxFactory.IdentifierName(nameof(CancellationToken))
                            .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                        SyntaxFactory.Identifier("token"), null),
                ])),
            [],
            SyntaxFactory.Block(Statements),
            null);

        _members.Add(method);
        _methodNames.Push(methodName);
    }



    private StatementSyntax GetRowsSourceOrEmpty(string alias)
    {
        return _getRowsSourceStatement.TryGetValue(alias, out var value)
            ? value
            : SyntaxFactory.EmptyStatement();
    }



    /// <summary>
    /// Gets the C# type name for code generation
    /// </summary>
    private static string GetTypeName(Type type)
    {
        return EvaluationHelper.GetCastableType(type);
    }

    public void Visit(PivotNode node)
    {
        // PIVOT node processing should typically happen within PivotFromNode context
        // during code generation. If we reach here directly, it means the PIVOT aggregations
        // are being processed independently, which can happen during complex query processing.
        // Handle this defensively without throwing an exception.
        
        // Do nothing - PIVOT processing is handled by PivotNodeProcessor within PivotFromNode
        // The aggregations would have been resolved during metadata building phase
    }

    public void Visit(PivotFromNode node)
    {
        // Visit the source first
        node.Source.Accept(this);
        
        // Process the PIVOT transformation
        var result = PivotNodeProcessor.ProcessPivotFromNode(node, Nodes, _scope);
        
        // Add the transformation statement
        Statements.Add(result.PivotTransformStatement);
        
        // Create a reference to the pivot table for further processing
        var pivotTableRef = SyntaxFactory.IdentifierName(result.PivotTableVariable);
        Nodes.Push(pivotTableRef);
    }

    private static BlockSyntax Block(params StatementSyntax[] statements)
    {
        return SyntaxFactory.Block(statements.Where(f => f is not EmptyStatementSyntax));
    }
}