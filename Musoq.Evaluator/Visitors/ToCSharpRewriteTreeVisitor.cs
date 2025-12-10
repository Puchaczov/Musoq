using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors.CodeGeneration;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;
using ExpressionFromNode = Musoq.Parser.Nodes.From.ExpressionFromNode;
using InMemoryTableFromNode = Musoq.Parser.Nodes.From.InMemoryTableFromNode;
using JoinInMemoryWithSourceTableFromNode = Musoq.Parser.Nodes.From.JoinInMemoryWithSourceTableFromNode;
using JoinSourcesTableFromNode = Musoq.Parser.Nodes.From.JoinSourcesTableFromNode;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Evaluator.Visitors;

public class ToCSharpRewriteTreeVisitor : DefensiveVisitorBase, IToCSharpTranslationExpressionVisitor
{
    protected override string VisitorName => nameof(ToCSharpRewriteTreeVisitor);

    private static readonly MethodInfo LikeMethod = typeof(Operators).GetMethod(nameof(Operators.Like));
    private static readonly MethodInfo RLikeMethod = typeof(Operators).GetMethod(nameof(Operators.RLike));
    private static readonly MethodInfo ContainsMethod = typeof(Operators).GetMethod(nameof(Operators.Contains));

    private readonly Dictionary<string, int> _inMemoryTableIndexes = new();

    private readonly List<SyntaxNode> _members = [];
    private readonly Stack<string> _methodNames = new();
    private int _rowClassCounter;

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
    private readonly CompilationOptions _compilationOptions;
    
    private readonly SetOperationEmitter _setOperationEmitter;
    private readonly QueryEmitter _queryEmitter;
    private readonly QueryClauseEmitter _queryClauseEmitter;
    private readonly CseManager _cseManager;
    private readonly CompilationContextManager _compilationContext;
    private readonly DescStatementEmitter _descStatementEmitter;

    public ToCSharpRewriteTreeVisitor(
        IEnumerable<Assembly> assemblies,
        IDictionary<string, int[]> setOperatorFieldIndexes,
        IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]> inferredColumns,
        string assemblyName,
        CompilationOptions compilationOptions)
    {
        ValidateConstructorParameter(nameof(assemblies), assemblies);
        ValidateConstructorParameter(nameof(setOperatorFieldIndexes), setOperatorFieldIndexes);
        ValidateConstructorParameter(nameof(inferredColumns), inferredColumns);
        ValidateStringParameter(nameof(assemblyName), assemblyName, "constructor");
        ValidateConstructorParameter(nameof(compilationOptions), compilationOptions);

        _setOperationEmitter = new SetOperationEmitter(new Dictionary<string, int[]>(setOperatorFieldIndexes));
        InferredColumns = inferredColumns;
        Workspace = RoslynSharedFactory.Workspace;
        Nodes = new Stack<SyntaxNode>();
        _compilationOptions = compilationOptions;

        Generator = RoslynSharedFactory.Generator;
        _queryEmitter = new QueryEmitter(Generator);
        _queryClauseEmitter = new QueryClauseEmitter(Generator);
        _descStatementEmitter = new DescStatementEmitter(Generator);
        var nonDeterministicFunctions = NonDeterministicMethodsScanner.ScanForNonDeterministicMethods(assemblies);
        _cseManager = new CseManager(nonDeterministicFunctions);
        
        _compilationContext = new CompilationContextManager(RoslynSharedFactory.CreateCompilation(assemblyName));
        _compilationContext.InitializeDefaults();
        _compilationContext.InitializeCoreReferences(assemblies);

        AccessToClassPath = $"{Namespace}.{ClassName}";
    }

    private string Namespace { get; } =
        $"{Resources.Compilation.NamespaceConstantPart}_{StringHelpers.GenerateNamespaceIdentifier()}";

    private static string ClassName => "CompiledQuery";

    public string AccessToClassPath { get; }

    public AdhocWorkspace Workspace { get; }

    public SyntaxGenerator Generator { get; }

    public CSharpCompilation Compilation => _compilationContext.GetCompilation();

    private Stack<SyntaxNode> Nodes { get; }

    private List<StatementSyntax> Statements { get; } = [];

    private List<Stack<SyntaxNode>> NullSuspiciousNodes { get; } = [];

    private IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]> InferredColumns { get; }

    private bool _isInsideCaseWhen;
    
    public void SetCaseWhenContext(bool isInside)
    {
        _isInsideCaseWhen = isInside;
    }

    public void InitializeCseForQuery(Node queryNode)
    {
        _cseManager.Initialize(queryNode, _compilationOptions.UseCommonSubexpressionElimination);
    }

    public override void Visit(DescNode node)
    {
        _descStatementEmitter.EmitDescStatement(
            node,
            Statements,
            _members,
            _methodNames,
            AddNamespace);
    }

    public override void Visit(StarNode node)
    {
        SyntaxBinaryOperationHelper.ProcessMultiplyOperation(Nodes, Generator);
    }

    public override void Visit(FSlashNode node)
    {
        SyntaxBinaryOperationHelper.ProcessDivideOperation(Nodes, Generator);
    }

    public override void Visit(ModuloNode node)
    {
        SyntaxBinaryOperationHelper.ProcessModuloOperation(Nodes, Generator);
    }

    public override void Visit(AddNode node)
    {
        SyntaxBinaryOperationHelper.ProcessAddOperation(Nodes, Generator);
    }

    public override void Visit(HyphenNode node)
    {
        SyntaxBinaryOperationHelper.ProcessSubtractOperation(Nodes, Generator);
    }

    public override void Visit(AndNode node)
    {
        SyntaxBinaryOperationHelper.ProcessLogicalAndOperation(Nodes, Generator);
    }

    public override void Visit(OrNode node)
    {
        SyntaxBinaryOperationHelper.ProcessLogicalOrOperation(Nodes, Generator);
    }

    public override void Visit(EqualityNode node)
    {
        ComparisonEmitter.ProcessEqualityComparison(node.Left, node.Right, Nodes, Generator);
    }

    public override void Visit(GreaterOrEqualNode node)
    {
        SyntaxBinaryOperationHelper.ProcessGreaterThanOrEqualOperation(Nodes, Generator);
    }

    public override void Visit(LessOrEqualNode node)
    {
        SyntaxBinaryOperationHelper.ProcessLessThanOrEqualOperation(Nodes, Generator);
    }

    public override void Visit(GreaterNode node)
    {
        SyntaxBinaryOperationHelper.ProcessGreaterThanOperation(Nodes, Generator);
    }

    public override void Visit(LessNode node)
    {
        SyntaxBinaryOperationHelper.ProcessLessThanOperation(Nodes, Generator);
    }

    public override void Visit(DiffNode node)
    {
        SyntaxBinaryOperationHelper.ProcessValueNotEqualsOperation(Nodes, Generator);
    }

    public override void Visit(NotNode node)
    {
        SyntaxBinaryOperationHelper.ProcessLogicalNotOperation(Nodes, Generator);
    }

    public override void Visit(LikeNode node)
    {
        PatternMatchEmitter.ProcessPatternMatch(node.Left, node.Right, nameof(Operators.Like), LikeMethod, Nodes, Visit);
    }

    public override void Visit(RLikeNode node)
    {
        PatternMatchEmitter.ProcessPatternMatch(node.Left, node.Right, nameof(Operators.RLike), RLikeMethod, Nodes, Visit);
    }

    public override void Visit(FieldNode node)
    {
        ApplyFieldResult(FieldEmitter.ProcessFieldNode(node.ReturnType, Nodes.Pop(), Generator));
    }

    public override void Visit(FieldOrderedNode node)
    {
        ApplyFieldResult(FieldEmitter.ProcessFieldOrderedNode(node.ReturnType, Nodes.Pop(), Generator));
    }
    
    private void ApplyFieldResult(FieldEmitter.FieldNodeResult result)
    {
        AddReference(result.RequiredTypes);
        AddNamespace(result.RequiredTypes);
        Nodes.Push(result.Expression);
    }

    public override void Visit(StringNode node)
    {
        Nodes.Push(LiteralNodeSyntaxConverter.ConvertStringNode(node));
    }

    public override void Visit(DecimalNode node)
    {
        Nodes.Push(LiteralNodeSyntaxConverter.ConvertDecimalNode(node));
    }

    public override void Visit(IntegerNode node)
    {
        Nodes.Push(LiteralNodeSyntaxConverter.ConvertIntegerNode(node));
    }

    public override void Visit(HexIntegerNode node)
    {
        Nodes.Push(LiteralNodeSyntaxConverter.ConvertHexIntegerNode(node));
    }

    public override void Visit(BinaryIntegerNode node)
    {
        Nodes.Push(LiteralNodeSyntaxConverter.ConvertBinaryIntegerNode(node));
    }

    public override void Visit(OctalIntegerNode node)
    {
        Nodes.Push(LiteralNodeSyntaxConverter.ConvertOctalIntegerNode(node));
    }

    public override void Visit(BooleanNode node)
    {
        Nodes.Push(LiteralNodeSyntaxConverter.ConvertBooleanNode(node, Generator));
    }

    public override void Visit(WordNode node)
    {
        Nodes.Push(LiteralNodeSyntaxConverter.ConvertWordNode(node, Generator));
    }

    public override void Visit(NullNode node)
    {
        Nodes.Push(LiteralNodeSyntaxConverter.ConvertNullNode(node, Generator));
    }

    public override void Visit(ContainsNode node)
    {
        var comparisonValues = (ArgumentListSyntax)Nodes.Pop();
        var valueExpression = Nodes.Pop();

        var result = ContainsEmitter.ProcessContainsNode(node, valueExpression, comparisonValues, ContainsMethod);
        Nodes.Push(result.ArgumentList);
        Visit(result.MethodNode);
    }

    public override void Visit(AccessMethodNode node)
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

        var resultExpr = ApplyCseIfNeeded(node.Id, accessMethodExpr, node.ReturnType);
        
        Nodes.Push(resultExpr);
    }

    public override void Visit(AccessRawIdentifierNode node)
    {
        Nodes.Push(SyntaxFactory.IdentifierName(node.Name));
    }

    public override void Visit(IsNullNode node)
    {
        var expression = (ExpressionSyntax)Nodes.Pop();
        var result = NullCheckEmitter.ProcessIsNull(node.Expression.ReturnType, node.IsNegated, expression);
        
        Nodes.Push(result.Expression);
    }

    public override void Visit(AccessColumnNode node)
    {
        ApplyAccessColumnResult(AccessColumnEmitter.GenerateColumnAccess(node, _type, Generator));
    }
    
    private void ApplyAccessColumnResult(AccessColumnEmitter.AccessColumnResult result)
    {
        AddNamespace(result.RequiredTypes);
        AddReference(result.RequiredTypes);
        if (result.ShouldTrackForNullCheck && NullSuspiciousNodes.Count > 0)
            NullSuspiciousNodes[^1].Push(result.Expression);
        Nodes.Push(result.Expression);
    }

    public override void Visit(IdentifierNode node)
    {
        var tableIndex = CteEmitter.GetCteIndex(node.Name, _inMemoryTableIndexes);
        Nodes.Push(CteEmitter.CreateCteReference(tableIndex));
    }

    public override void Visit(AccessObjectArrayNode node)
    {
        var result = AccessObjectArrayNodeProcessor.ProcessAccessObjectArrayNode(node, Nodes);
        AddNamespace(result.RequiredNamespace);
        Nodes.Push(result.Expression);
    }

    public override void Visit(AccessObjectKeyNode node)
    {
        var result = AccessObjectKeyNodeProcessor.ProcessAccessObjectKeyNode(node, Nodes);
        AddNamespace(result.RequiredNamespace);
        Nodes.Push(result.Expression);
    }

    public override void Visit(PropertyValueNode node)
    {
        Nodes.Push(FieldEmitter.CreatePropertyAccess((ExpressionSyntax)Nodes.Pop(), node.Name));
    }

    public override void Visit(ArgsListNode node)
    {
        Nodes.Push(StatementEmitter.CreateArgumentListFromStack(Nodes, node.Args.Length));
    }


    public override void Visit(SelectNode node)
    {
        var result = _queryClauseEmitter.ProcessSelect(node, Nodes, _scope, _type, _queryAlias, ref _rowClassCounter);
        _members.Add(result.RowClass);
        _selectBlock = result.SelectBlock;
    }

    public override void Visit(WhereNode node)
    {
        Nodes.Push(_queryClauseEmitter.ProcessWhere(Nodes.Pop(), _isResultParallelizationImpossible, _type == MethodAccessType.ResultQuery));
    }

    public override void Visit(GroupByNode node)
    {
        var result = _queryClauseEmitter.ProcessGroupBy(node, Nodes, _scope);
        _groupValues = result.GroupValues;
        _groupKeys = result.GroupKeys;
        _groupHaving = result.GroupHaving;
        Statements.Add(result.GroupFieldsStatement);
        AddNamespace(result.RequiredNamespace);
    }

    public override void Visit(HavingNode node)
    {
        Nodes.Push(_queryClauseEmitter.ProcessHaving(Nodes.Pop()));
    }

    public override void Visit(SkipNode node)
    {
        var result = _queryClauseEmitter.ProcessSkip(node.Value);
        Statements.Add(result.Declaration);
        Nodes.Push(result.IfStatement);
    }

    public override void Visit(TakeNode node)
    {
        var result = _queryClauseEmitter.ProcessTake(node.Value);
        Statements.Add(result.Declaration);
        Nodes.Push(result.Block);
    }

    public override void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
        var result = JoinInMemoryWithSourceTableNodeProcessor.Process(
            node, Nodes.Pop(), Generator, _scope, _queryAlias, GetRowsSourceOrEmpty, GenerateCancellationExpression);
        _emptyBlock = result.EmptyBlock;
        _joinOrApplyBlock = result.ComputingBlock;
    }

    public override void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        var getRowsSourceWrapper = GetRowsSourceOrEmpty;
        Func<StatementSyntax[], BlockSyntax> blockWrapper = Block;
        var cancellationWrapper = GenerateCancellationExpression;
        
        var result = ApplyInMemoryWithSourceTableNodeProcessor.ProcessApplyInMemoryWithSourceTable(
            node, Generator, _scope, _queryAlias, getRowsSourceWrapper, blockWrapper, cancellationWrapper);
        
        _emptyBlock = result.EmptyBlock;
        _joinOrApplyBlock = result.ComputingBlock;
    }

    public override void Visit(SchemaFromNode node)
    {
        var result = SchemaNodeEmitter.ProcessSchemaFromNode(
            node.Id, node.Alias, node.Schema, node.Method, _schemaFromIndex++, InferredColumns[node], (ArgumentListSyntax)Nodes.Pop());

        Statements.Add(result.TableInfoStatement);
        Statements.Add(result.SchemaStatement);
        AddRowsSource(node.Alias, result.RowsStatement);
    }
    
    private void AddRowsSource(string alias, LocalDeclarationStatementSyntax rowsStatement)
    {
        if (_isInsideJoinOrApply)
            _getRowsSourceStatement.Add(alias, rowsStatement);
        else
            Statements.Add(rowsStatement);
    }

    public override void Visit(JoinSourcesTableFromNode node)
    {
        _emptyBlock = StatementEmitter.CreateEmptyBlock();
        _joinOrApplyBlock = JoinSourcesTableProcessingHelper.ProcessJoinSourcesTable(
            node, Generator, _scope, _queryAlias, 
            JoinEmitter.CreateJoinConditionCheck(Nodes.Pop(), Generator), 
            _emptyBlock, GetRowsSourceOrEmpty, Block, GenerateCancellationExpression,
            CreateExpressionEvaluator(), _compilationOptions);
    }
    
    private Func<Node, ExpressionSyntax> CreateExpressionEvaluator()
    {
        return n => {
            n.Accept(new ToCSharpRewriteTreeTraverseVisitor(this, new ScopeWalker(_scope), _compilationOptions));
            return (ExpressionSyntax)Nodes.Pop();
        };
    }

    public override void Visit(ApplySourcesTableFromNode node)
    {
        var result = ApplySourcesTableNodeProcessor.ProcessApplySourcesTable(
            node, Generator, _scope, _queryAlias, GetRowsSourceOrEmpty, Block, GenerateCancellationExpression);
        
        _emptyBlock = result.EmptyBlock;
        _joinOrApplyBlock = result.ComputingBlock;
    }

    public override void Visit(InMemoryTableFromNode node)
    {
        var tableIndex = _inMemoryTableIndexes[node.VariableName];
        _getRowsSourceStatement.Add(node.Alias, SchemaNodeEmitter.CreateInMemoryTableRowsSource(node.Alias, tableIndex));
    }

    public override void Visit(ExpressionFromNode node)
    {
        Nodes.Push(StatementEmitter.CreateEmptyBlock());
    }

    public override void Visit(AccessMethodFromNode node)
    {
        AddNamespace(node.ReturnType);
        var sourceExpression = (ExpressionSyntax)Nodes.Pop();
        _getRowsSourceStatement.Add(node.Alias, SchemaNodeEmitter.CreateEnumerableRowsSource(node.Alias, node.ReturnType, sourceExpression));
    }

    public override void Visit(PropertyFromNode node)
    {
        AddNamespace(node.ReturnType);
        
        var propertiesChain = node.PropertiesChain
            .Select(p => (p.PropertyName, p.PropertyType))
            .ToArray();
        
        _getRowsSourceStatement.Add(
            node.Alias, 
            SchemaNodeEmitter.CreatePropertyRowsSource(node.Alias, node.SourceAlias, node.ReturnType, propertiesChain));
    }

    public override void Visit(CreateTransformationTableNode node)
    {
        var tableName = _scope[MetaAttributes.CreateTableVariableName];
        Statements.Add(node.ForGrouping 
            ? TransformationTableEmitter.CreateGroupingTransformationTable(tableName)
            : TransformationTableEmitter.CreateRegularTransformationTable(node, tableName, AddReference, AddNamespace));
    }

    public override void Visit(QueryNode node)
    {
        var result = QueryNodeProcessor.ProcessQueryNode(
            node,
            Nodes,
            _selectBlock,
            GenerateCseVariableDeclarations().ToArray(),
            GetRowsSourceOrEmpty,
            _scope[MetaAttributes.SourceName],
            !_isResultParallelizationImpossible,
            Generator);

        Statements.AddRange(result.Statements);

        _getRowsSourceStatement.Clear();
        _isResultParallelizationImpossible = false;
    }

    public override void Visit(InternalQueryNode node)
    {
        var select = _selectBlock;
        var where = node.Where != null ? Nodes.Pop() as StatementSyntax : null;
        var block = (BlockSyntax)Nodes.Pop();
        var cseDeclarations = GenerateCseVariableDeclarations().ToArray();

        if (node.GroupBy != null)
        {
            var result = InternalQueryNodeProcessor.ProcessGroupByPath(
                node,
                Nodes,
                block,
                cseDeclarations,
                where,
                _groupKeys,
                _groupValues,
                _groupHaving,
                GetRowsSourceOrEmpty,
                _scope[MetaAttributes.SourceName],
                _queryEmitter.CreateIndexToColumnMap);

            Statements.AddRange(result.Statements);
        }
        else
        {
            Statements.AddRange(InternalQueryNodeProcessor.ProcessJoinApplyPath(select, _joinOrApplyBlock));
        }

        _getRowsSourceStatement.Clear();
        _isResultParallelizationImpossible = false;
    }

    private static StatementSyntax GenerateCancellationExpression()
    {
        return QueryEmitter.GenerateCancellationCheck();
    }

    public override void Visit(RootNode node)
    {
        var methodCallExpression = $"{_methodNames.Pop()}(Provider, PositionalEnvironmentVariables, QueriesInformation, Logger, token)";
        
        ClassEmitter.AddRunnableMembers(_members, methodCallExpression);

        var inMemoryTables = ClassEmitter.CreateInMemoryTablesField(_inMemoryTableIndex);
        _members.Insert(0, inMemoryTables);

        var classDeclaration = ClassEmitter.CreateClassDeclaration(Generator, ClassName, _members);
        var ns = ClassEmitter.CreateNamespaceDeclaration(Namespace, _compilationContext.GetNamespaces(), (ClassDeclarationSyntax)classDeclaration);
        var compilationUnit = ClassEmitter.CreateCompilationUnit(ns);
        var formatted = ClassEmitter.FormatCompilationUnit(compilationUnit, Workspace);

        _compilationContext.AddSyntaxTree(ClassEmitter.CreateSyntaxTree(formatted));
    }

    public override void Visit(UnionNode node)
    {
        ProcessSetOperation("Union", nameof(BaseOperations.Union));
    }

    public override void Visit(UnionAllNode node)
    {
        ProcessSetOperation("UnionAll", nameof(BaseOperations.UnionAll));
    }

    public override void Visit(ExceptNode node)
    {
        ProcessSetOperation("Except", nameof(BaseOperations.Except));
    }

    public override void Visit(IntersectNode node)
    {
        ProcessSetOperation("Intersect", nameof(BaseOperations.Intersect));
    }

    private void ProcessSetOperation(string operationSuffix, string baseOperationMethodName)
    {
        var result = _setOperationEmitter.ProcessSetOperation(
            _methodNames,
            operationSuffix,
            baseOperationMethodName,
            _scope[MetaAttributes.SetOperatorName]);

        _methodNames.Push(result.CombinedMethodName);
        _members.Add(result.Method);
    }

    public override void Visit(RefreshNode node)
    {
        var block = MultiStatementEmitter.ProcessRefreshNode(node.Nodes.Length, Nodes);
        if (block != null)
            Nodes.Push(block);
    }

    public override void Visit(PutTrueNode node)
    {
        Nodes.Push(MultiStatementEmitter.CreatePutTrueExpression(Generator));
    }

    public override void Visit(MultiStatementNode node)
    {
        Statements.Insert(0, MultiStatementEmitter.CreateStatsDeclaration());

        var methodName = MultiStatementEmitter.GenerateMethodName(_scope, _setOperatorMethodIdentifier);
        _methodNames.Push(methodName);

        var method = MultiStatementEmitter.CreateMethod(methodName, Statements);
        _members.Add(method);
        _typesToInstantiate.Clear();
        Statements.Clear();
    }

    public override void Visit(CteExpressionNode node)
    {
        var result = CteExpressionNodeProcessor.ProcessCteExpressionNode(node, _methodNames, Nodes);
        
        _members.Add(result.Method);
        _methodNames.Push(result.MethodName);
    }

    public override void Visit(CteInnerExpressionNode node)
    {
        CteEmitter.TryRegisterCteIndex(node.Name, _inMemoryTableIndexes, ref _inMemoryTableIndex);
        var tableIndex = CteEmitter.GetCteIndex(node.Name, _inMemoryTableIndexes);
        var assignment = CteEmitter.CreateCteInnerExpressionAssignment(node.Name, tableIndex, _methodNames.Peek());
        Nodes.Push(assignment);
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
        _compilationContext.TrackNamespace(columnTypeNamespace);
    }

    private void AddNamespace(params Type[] types)
    {
        _compilationContext.TrackNamespaces(types);
    }

    private void AddReference(params Type[] types)
    {
        _compilationContext.TrackTypes(types);
    }

    private void AddReference(params string[] assemblyDllsPaths)
    {
        foreach (var path in assemblyDllsPaths)
            _compilationContext.AddAssemblyReference(path);
    }

    private void AddReference(params Assembly[] assemblies)
    {
        _compilationContext.AddAssemblyReferences(assemblies);
    }

    public override void Visit(OrderByNode node)
    {
        AddNamespace(QueryClauseEmitter.GetOrderByNamespace());
    }

    public override void Visit(CaseNode node)
    {
        ApplyCaseNodeResult(CaseNodeProcessor.ProcessCaseNode(
            node, Nodes, _typesToInstantiate, _oldType, _queryAlias, ref _caseWhenMethodIndex,
            _cseManager.GetDeclarations()));
    }
    
    private void ApplyCaseNodeResult(CaseNodeProcessor.ProcessCaseNodeResult result)
    {
        foreach (var ns in result.RequiredNamespaces)
            AddNamespace(ns);
        _members.Add(result.Method);
        Nodes.Push(result.MethodInvocation);
    }

    public override void Visit(FieldLinkNode node)
    {
        throw new NotSupportedException();
    }

    private StatementSyntax GetRowsSourceOrEmpty(string alias)
    {
        return _getRowsSourceStatement.TryGetValue(alias, out var value)
            ? value
            : SyntaxFactory.EmptyStatement();
    }


    private static BlockSyntax Block(params StatementSyntax[] statements)
    {
        return StatementEmitter.CreateBlock(statements.Where(f => f is not EmptyStatementSyntax));
    }

    private SyntaxNode ApplyCseIfNeeded(string expressionId, SyntaxNode expression, Type expressionType)
    {
        var result = _cseManager.ApplyIfNeeded(expressionId, (ExpressionSyntax)expression, expressionType, _isInsideCaseWhen);
        return result;
    }
    
    private IEnumerable<StatementSyntax> GenerateCseVariableDeclarations()
    {
        return _cseManager.GenerateDeclarations();
    }
}