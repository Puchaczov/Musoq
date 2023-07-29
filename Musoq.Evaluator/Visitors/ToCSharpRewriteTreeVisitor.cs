using System;
using System.Collections.Generic;
using System.Dynamic;
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
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Parser;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using AliasedFromNode = Musoq.Parser.Nodes.From.AliasedFromNode;
using ExpressionFromNode = Musoq.Parser.Nodes.From.ExpressionFromNode;
using InMemoryTableFromNode = Musoq.Parser.Nodes.From.InMemoryTableFromNode;
using JoinFromNode = Musoq.Parser.Nodes.From.JoinFromNode;
using JoinInMemoryWithSourceTableFromNode = Musoq.Parser.Nodes.From.JoinInMemoryWithSourceTableFromNode;
using JoinsNode = Musoq.Parser.Nodes.From.JoinsNode;
using JoinSourcesTableFromNode = Musoq.Parser.Nodes.From.JoinSourcesTableFromNode;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;
using SchemaMethodFromNode = Musoq.Parser.Nodes.From.SchemaMethodFromNode;
using TextSpan = Musoq.Parser.TextSpan;

namespace Musoq.Evaluator.Visitors
{
    public class ToCSharpRewriteTreeVisitor : IToCSharpTranslationExpressionVisitor
    {
        private readonly Dictionary<string, int> _inMemoryTableIndexes = new();
        private readonly List<string> _loadedAssemblies = new();

        private readonly List<SyntaxNode> _members = new();
        private readonly Stack<string> _methodNames = new();

        private readonly List<string> _namespaces = new();
        private readonly IDictionary<string, int[]> _setOperatorFieldIndexes;

        private readonly Dictionary<string, Type> _typesToInstantiate = new();
        private BlockSyntax _emptyBlock;
        private SyntaxNode _groupHaving;

        private VariableDeclarationSyntax _groupKeys;
        private VariableDeclarationSyntax _groupValues;

        private int _inMemoryTableIndex;
        private int _setOperatorMethodIdentifier;
        private int _caseWhenMethodIndex;
        private int _schemaFromIndex;

        private BlockSyntax _joinBlock;
        private string _queryAlias;
        private Scope _scope;
        private BlockSyntax _selectBlock;
        private MethodAccessType _oldType;
        private MethodAccessType _type;

        public ToCSharpRewriteTreeVisitor(
            IEnumerable<Assembly> assemblies,
            IDictionary<string, int[]> setOperatorFieldIndexes,
            IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]> inferredColumns,
            string assemblyName)
        {
            _setOperatorFieldIndexes = setOperatorFieldIndexes;
            InferredColumns = inferredColumns;
            Workspace = new AdhocWorkspace();
            Nodes = new Stack<SyntaxNode>();

            Generator = SyntaxGenerator.GetGenerator(Workspace, LanguageNames.CSharp);

            Compilation = CSharpCompilation.Create(assemblyName);

            Compilation = Compilation.AddReferences(RuntimeLibraries.References);

            var env = new Plugins.Environment();
            Compilation = Compilation
                .AddReferences(
                    MetadataReference.CreateFromFile(env.Value<string>(Constants.NetStandardDllEnvironmentName)));

            AddReference(typeof(object));
            AddReference(typeof(CancellationToken));
            AddReference(typeof(ISchema));
            AddReference(typeof(LibraryBase));
            AddReference(typeof(Table));
            AddReference(typeof(SyntaxFactory));
            AddReference(typeof(ExpandoObject));
            AddReference(typeof(SchemaFromNode));
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
            AddNamespace("System.Collections.Generic");
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

        public string ClassName { get; } = "CompiledQuery";

        public string AccessToClassPath { get; }

        public AdhocWorkspace Workspace { get; }

        public SyntaxGenerator Generator { get; }

        public CSharpCompilation Compilation { get; private set; }

        private Stack<SyntaxNode> Nodes { get; }

        private List<StatementSyntax> Statements { get; } = new();
        private Stack<SyntaxNode> NullSuspiciousNodes { get; } = new();
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
            }

            Statements.Clear();
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
                                new[]
                                {
                                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("desc")),
                                    SyntaxHelper.StringLiteralArgument(((SchemaFromNode) node.From).Method)
                                }))), false);
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
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxHelper.StringLiteralArgument(schemaNode.Schema)
                    }),
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
                        SyntaxFactory.SeparatedList(new[]
                        {
                            SyntaxHelper.StringLiteralArgument(schemaNode.Method),
                            SyntaxFactory.Argument(CreateRuntimeContext(schemaNode, originallyInferredColumns)),
                            SyntaxFactory.Argument(SyntaxHelper.CreateArrayOf(nameof(Object), args))
                        }),
                        SyntaxFactory.Token(SyntaxKind.CloseParenToken)
                    )
                );

                var returnStatement = SyntaxFactory.ReturnStatement(invocationExpression);

                Statements.AddRange(new StatementSyntax[]
                {
                    SyntaxFactory.LocalDeclarationStatement(createdSchema),
                    SyntaxFactory.LocalDeclarationStatement(getTable),
                    returnStatement
                });
            }
            else
            {
                var returnStatement = SyntaxFactory.ReturnStatement(invocationExpression);

                Statements.AddRange(new StatementSyntax[]
                {
                    SyntaxFactory.LocalDeclarationStatement(createdSchema),
                    returnStatement
                });
            }

            var methodName = "GetTableDesc";

            var method = SyntaxFactory.MethodDeclaration(
                new SyntaxList<AttributeListSyntax>(),
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
                SyntaxFactory.IdentifierName(nameof(Table)).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                null,
                SyntaxFactory.Identifier(methodName),
                null,
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Parameter(
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxTokenList.Create(
                                new SyntaxToken()),
                            SyntaxFactory.IdentifierName(nameof(ISchemaProvider))
                                .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                            SyntaxFactory.Identifier("provider"), null),

                        SyntaxFactory.Parameter(
                            new SyntaxList<AttributeListSyntax>(),
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
                                                                        SyntaxFactory.Identifier("WhereNode"))
                                                            }))
                                                })))),

                        SyntaxFactory.Parameter(
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxTokenList.Create(
                                new SyntaxToken()),
                            SyntaxFactory.IdentifierName(nameof(CancellationToken))
                                .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                            SyntaxFactory.Identifier("token"), null)
                    })),
                new SyntaxList<TypeParameterConstraintClauseSyntax>(),
                SyntaxFactory.Block(Statements),
                null);

            _members.Add(method);
            _methodNames.Push(methodName);
        }

        public void Visit(StarNode node)
        {
            var b = Nodes.Pop();
            var a = Nodes.Pop();
            Nodes.Push(Generator.MultiplyExpression(a, b));
        }

        public void Visit(FSlashNode node)
        {
            var b = Nodes.Pop();
            var a = Nodes.Pop();
            Nodes.Push(Generator.DivideExpression(a, b));
        }

        public void Visit(ModuloNode node)
        {
            var b = Nodes.Pop();
            var a = Nodes.Pop();
            Nodes.Push(Generator.ModuloExpression(a, b));
        }

        public void Visit(AddNode node)
        {
            var b = Nodes.Pop();
            var a = Nodes.Pop();
            Nodes.Push(Generator.AddExpression(a, b));
        }

        public void Visit(HyphenNode node)
        {
            var b = Nodes.Pop();
            var a = Nodes.Pop();
            Nodes.Push(Generator.SubtractExpression(a, b));
        }

        public void Visit(AndNode node)
        {
            var b = Nodes.Pop();
            var a = Nodes.Pop();
            Nodes.Push(Generator.LogicalAndExpression(a, b));
        }

        public void Visit(OrNode node)
        {
            var b = Nodes.Pop();
            var a = Nodes.Pop();
            Nodes.Push(Generator.LogicalOrExpression(a, b));
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

            var rawSyntax = Generator.ValueEqualsExpression(a, b);
            var guardedSyntax = GenerateNullGuards(rawSyntax);

            Nodes.Push(guardedSyntax);
        }

        public void Visit(GreaterOrEqualNode node)
        {
            var b = Nodes.Pop();
            var a = Nodes.Pop();

            var rawSyntax = Generator.GreaterThanOrEqualExpression(a, b);
            var guardedSyntax = GenerateNullGuards(rawSyntax);

            Nodes.Push(guardedSyntax);
        }

        public void Visit(LessOrEqualNode node)
        {
            var b = Nodes.Pop();
            var a = Nodes.Pop();

            var rawSyntax = Generator.LessThanOrEqualExpression(a, b);
            var guardedSyntax = GenerateNullGuards(rawSyntax);

            Nodes.Push(guardedSyntax);
        }

        public void Visit(GreaterNode node)
        {
            var b = Nodes.Pop();
            var a = Nodes.Pop();

            var rawSyntax = Generator.GreaterThanExpression(a, b);
            var guardedSyntax = GenerateNullGuards(rawSyntax);

            Nodes.Push(guardedSyntax);
        }

        public void Visit(LessNode node)
        {
            var b = Nodes.Pop();
            var a = Nodes.Pop();

            var rawSyntax = Generator.LessThanExpression(a, b);
            var guardedSyntax = GenerateNullGuards(rawSyntax);

            Nodes.Push(guardedSyntax);
        }

        public void Visit(DiffNode node)
        {
            var b = Nodes.Pop();
            var a = Nodes.Pop();

            var rawSyntax = Generator.ValueNotEqualsExpression(a, b);
            var guardedSyntax = GenerateNullGuards(rawSyntax);

            Nodes.Push(guardedSyntax);
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

            var arg = SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.Argument((ExpressionSyntax) a),
                    SyntaxFactory.Argument((ExpressionSyntax) b)
                }));

            Nodes.Push(arg);

            Visit(new AccessMethodNode(
                new FunctionToken(nameof(Operators.Like), TextSpan.Empty),
                new ArgsListNode(new[] {node.Left, node.Right}), null, false,
                typeof(Operators).GetMethod(nameof(Operators.Like))));
        }

        public void Visit(RLikeNode node)
        {
            var b = Nodes.Pop();
            var a = Nodes.Pop();

            var arg = SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.Argument((ExpressionSyntax) a),
                    SyntaxFactory.Argument((ExpressionSyntax) b)
                }));

            Nodes.Push(arg);

            Visit(new AccessMethodNode(
                new FunctionToken(nameof(Operators.RLike), TextSpan.Empty),
                new ArgsListNode(new[] {node.Left, node.Right}), null, false,
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
            Nodes.Push(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal($"@\"{node.Value}\"", node.Value)));
        }

        public void Visit(DecimalNode node)
        {
            Nodes.Push(Generator.LiteralExpression(node.Value));
        }

        public void Visit(IntegerNode node)
        {
            Nodes.Push(Generator.LiteralExpression(node.ObjValue));
        }

        public void Visit(BooleanNode node)
        {
            Nodes.Push(Generator.LiteralExpression(node.Value));
        }

        public void Visit(WordNode node)
        {
            Nodes.Push(Generator.LiteralExpression(node.Value));
        }

        public void Visit(ContainsNode node)
        {
            var comparsionValues = (ArgumentListSyntax) Nodes.Pop();
            var a = Nodes.Pop();

            var expressions = new ExpressionSyntax[comparsionValues.Arguments.Count];
            for (var index = 0; index < comparsionValues.Arguments.Count; index++)
            {
                var argument = comparsionValues.Arguments[index];
                expressions[index] = argument.Expression;
            }

            var objExpression = SyntaxHelper.CreateArrayOfObjects(node.ReturnType.Name, expressions);

            var arg = SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.Argument((ExpressionSyntax) a),
                    SyntaxFactory.Argument(objExpression)
                }));

            Nodes.Push(arg);

            Visit(new AccessMethodNode(
                new FunctionToken(nameof(Operators.Contains), TextSpan.Empty),
                new ArgsListNode(new[] {node.Left, node.Right}), null, false,
                typeof(Operators).GetMethod(nameof(Operators.Contains))));
        }

        public void Visit(AccessMethodNode node)
        {
            var args = new List<ArgumentSyntax>();

            var parameters = node.Method.GetParameters().GetParametersWithAttribute<InjectTypeAttribute>();

            var method = node.Method;

            var variableName = $"{node.Alias}{method.ReflectedType.Name}Lib";

            if (!_typesToInstantiate.ContainsKey(variableName))
            {
                _typesToInstantiate.Add(variableName, method.ReflectedType);
                AddNamespace(method.ReflectedType.Namespace);

                Statements.Add(
                    SyntaxFactory.LocalDeclarationStatement(
                        SyntaxHelper.CreateAssignment(
                            variableName,
                            SyntaxHelper.CreateObjectOf(
                                method.ReflectedType.Name,
                                SyntaxFactory.ArgumentList()))));
            }

            _scope.ScopeSymbolTable.AddSymbolIfNotExist(method.ReflectedType.Name,
                new TypeSymbol(method.ReflectedType));

            foreach (var parameterInfo in parameters)
            {
                var attribute = parameterInfo.GetCustomAttributeThatInherits<InjectTypeAttribute>();
                
                switch (attribute)
                {
                    case InjectSpecificSourceAttribute _:
                    case InjectSourceAttribute _:

                        if (node.CanSkipInjectSource)
                            continue;

                        string objectName;

                        switch (_type)
                        {
                            case MethodAccessType.TransformingQuery:
                                objectName = $"{_queryAlias}Row";
                                break;
                            case MethodAccessType.ResultQuery:
                            case MethodAccessType.CaseWhen:
                                objectName = "score";
                                break;
                            default:
                                throw new NotSupportedException($"Unrecognized method access type ({_type})");
                        }

                        var typeIdentifier = SyntaxFactory.IdentifierName(
                            EvaluationHelper.GetCastableType(parameterInfo.ParameterType));

                        if (parameterInfo.ParameterType == typeof(ExpandoObject))
                        {
                            typeIdentifier = SyntaxFactory.IdentifierName("dynamic");
                        }

                        var aliases =
                            _scope.Parent.ScopeSymbolTable.GetSymbol<AliasesPositionsSymbol>(MetaAttributes
                                .AllQueryContexts);
                        var currentContext = aliases.AliasesPositions[node.Alias];

                        args.Add(
                            SyntaxFactory.Argument(
                                SyntaxFactory.CastExpression(
                                    typeIdentifier,
                                    SyntaxFactory.ElementAccessExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName(objectName),
                                            SyntaxFactory.IdentifierName(nameof(IObjectResolver.Contexts))),
                                        SyntaxFactory.BracketedArgumentList(
                                            SyntaxFactory.SeparatedList(
                                                new[]
                                                {
                                                    SyntaxFactory.Argument(
                                                        SyntaxFactory.LiteralExpression(
                                                            SyntaxKind.NumericLiteralExpression,
                                                            SyntaxFactory.Literal(currentContext)))
                                                }))))));
                        break;
                    case InjectGroupAttribute _:

                        switch (_type)
                        {
                            case MethodAccessType.ResultQuery: //do not inject in result query.
                                break;
                            default:
                                args.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("group")));
                                break;
                        }

                        break;
                    case InjectGroupAccessName _:
                        break;
                    case InjectQueryStatsAttribute _:
                        args.Add(
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName("stats")));
                        break;
                }
            }

            var tmpArgs = (ArgumentListSyntax) Nodes.Pop();

            for (var index = 0; index < tmpArgs.Arguments.Count; index++)
            {
                var item = tmpArgs.Arguments[index];
                args.Add(item);
            }

            SyntaxNode accessMethodExpr;

            if (node.Method.IsGenericMethod && method.GetCustomAttribute<AggregationMethodAttribute>() != null)
            {
                var genericArgs = node.Method.GetGenericArguments();
                var syntaxArgs = new List<SyntaxNodeOrToken>();

                for (int i = 0; i < genericArgs.Length - 1; ++i)
                {
                    syntaxArgs.Add(SyntaxFactory.IdentifierName(genericArgs[i].FullName));
                    syntaxArgs.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                }

                syntaxArgs.Add(SyntaxFactory.IdentifierName(genericArgs[genericArgs.Length - 1].FullName));

                TypeArgumentListSyntax typeArgs;
                if (syntaxArgs.Count < 2)
                {
                    typeArgs = SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            (IdentifierNameSyntax) syntaxArgs[0]));
                }
                else
                {
                    typeArgs = SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SeparatedList<TypeSyntax>(
                            syntaxArgs.ToArray()));
                }

                var genericName = SyntaxFactory
                    .GenericName(node.Name)
                    .WithTypeArgumentList(
                        typeArgs
                            .WithLessThanToken(
                                SyntaxFactory.Token(SyntaxKind.LessThanToken))
                            .WithGreaterThanToken(
                                SyntaxFactory.Token(SyntaxKind.GreaterThanToken)));

                accessMethodExpr = Generator.InvocationExpression(
                    Generator.MemberAccessExpression(
                        Generator.IdentifierName(variableName),
                        genericName),
                    args);
            }
            else
            {
                accessMethodExpr = Generator.InvocationExpression(
                    Generator.MemberAccessExpression(
                        Generator.IdentifierName(variableName),
                        Generator.IdentifierName(node.Name)),
                    args);
            }

            if (!node.ReturnType.IsTrueValueType())
                NullSuspiciousNodes.Push(accessMethodExpr);

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

        public void Visit(AccessRefreshAggreationScoreNode node)
        {
        }

        public void Visit(AccessColumnNode node)
        {
            string variableName;
            switch (_type)
            {
                case MethodAccessType.TransformingQuery:
                    variableName = $"{node.Alias}Row";
                    break;
                case MethodAccessType.ResultQuery:
                case MethodAccessType.CaseWhen:
                    variableName = "score";
                    break;
                default:
                    throw new NotSupportedException($"Unrecognized method access type ({_type})");
            }

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

            if (typeof(IDynamicMetaObjectProvider).IsAssignableFrom(node.ReturnType))
            {
                typeIdentifier = SyntaxFactory.IdentifierName("dynamic");
            }

            sNode = Generator.CastExpression(typeIdentifier, sNode);

            if (!node.ReturnType.IsTrueValueType())
                NullSuspiciousNodes.Push(sNode);

            Nodes.Push(sNode);
        }

        public void Visit(AllColumnsNode node)
        {
        }

        public void Visit(IdentifierNode node)
        {
            Nodes.Push(SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName("_tableResults"))
                .WithArgumentList(
                    SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(_inMemoryTableIndexes[node.Name])))))));
        }

        public void Visit(AccessObjectArrayNode node)
        {
            var exp = SyntaxFactory.ParenthesizedExpression((ExpressionSyntax) Nodes.Pop());

            Nodes.Push(SyntaxFactory
                .ElementAccessExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    exp, SyntaxFactory.IdentifierName(node.Name))).WithArgumentList(
                    SyntaxFactory.BracketedArgumentList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                            SyntaxFactory.Literal(node.Token.Index)))))));
        }

        public void Visit(AccessObjectKeyNode node)
        {
            var exp = SyntaxFactory.ParenthesizedExpression((ExpressionSyntax) Nodes.Pop());

            Nodes.Push(SyntaxFactory
                .ElementAccessExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    exp, SyntaxFactory.IdentifierName(node.Name))).WithArgumentList(
                    SyntaxFactory.BracketedArgumentList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                            SyntaxFactory.Literal(node.Token.Key)))))));
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

            var rargs = SyntaxFactory.SeparatedList<ArgumentSyntax>();

            for (var i = args.Count - 1; i >= 0; i--) rargs = rargs.Add(args[i]);

            Nodes.Push(SyntaxFactory.ArgumentList(rargs));
        }


        public void Visit(SelectNode node)
        {
            var scoreTable = _scope[MetaAttributes.SelectIntoVariableName];

            var variableNameKeyword = SyntaxFactory.Identifier(SyntaxTriviaList.Empty, "select",
                SyntaxTriviaList.Create(SyntaxHelper.WhiteSpace));
            var syntaxList = new ExpressionSyntax[node.Fields.Length];

            for (var i = 0; i < node.Fields.Length; i++)
                syntaxList[node.Fields.Length - 1 - i] = (ExpressionSyntax) Nodes.Pop();

            var array = SyntaxHelper.CreateArrayOfObjects(syntaxList.ToArray());
            var equalsClause = SyntaxFactory.EqualsValueClause(
                SyntaxFactory.Token(SyntaxKind.EqualsToken).WithTrailingTrivia(SyntaxHelper.WhiteSpace), array);

            var variableDecl = SyntaxFactory.VariableDeclarator(variableNameKeyword, null, equalsClause);
            var list = SyntaxFactory.SeparatedList(new List<VariableDeclaratorSyntax>
            {
                variableDecl
            });

            var variableDeclaration =
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var").WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                    list);

            var contexts = _scope[MetaAttributes.Contexts].Split(',');

            var contextsExpressions = new List<ArgumentSyntax>
            {
                SyntaxFactory.Argument(
                    SyntaxFactory.IdentifierName(variableNameKeyword.Text))
            };

            foreach (var context in contexts)
            {
                string rowVariableName;

                switch (_type)
                {
                    case MethodAccessType.TransformingQuery:
                        rowVariableName = $"{context}Row";
                        break;
                    case MethodAccessType.ResultQuery:
                    case MethodAccessType.CaseWhen:
                        rowVariableName = "score";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                contextsExpressions.Add(
                    SyntaxFactory.Argument(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(rowVariableName),
                            SyntaxFactory.IdentifierName($"{nameof(IObjectResolver.Contexts)}"))));
            }

            var invocation = SyntaxHelper.CreateMethodInvocation(
                scoreTable,
                nameof(Table.Add),
                new[]
                {
                    SyntaxFactory.Argument(
                        SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.Token(SyntaxKind.NewKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                            SyntaxFactory.ParseTypeName(nameof(ObjectsRow)),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList(
                                    contextsExpressions.ToArray())
                            ),
                            SyntaxFactory.InitializerExpression(SyntaxKind.ComplexElementInitializerExpression))
                    )
                });

            var a1 = SyntaxFactory.LocalDeclarationStatement(variableDeclaration)
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
            var a2 = SyntaxFactory.ExpressionStatement(invocation)
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            NullSuspiciousNodes.Clear();
            _selectBlock = SyntaxFactory.Block(a1, a2);
        }

        public void Visit(GroupSelectNode node)
        {
        }

        public void Visit(WhereNode node)
        {
            var ifStatement =
                Generator.IfStatement(
                        Generator.LogicalNotExpression(Nodes.Pop()),
                        new SyntaxNode[]
                        {
                            SyntaxFactory.ContinueStatement()
                        })
                    .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            NullSuspiciousNodes.Clear();
            Nodes.Push(ifStatement);
        }

        public void Visit(GroupByNode node)
        {
            var args = new SyntaxNode[node.Fields.Length];

            SyntaxNode having = null;
            if (node.Having != null)
                having = Nodes.Pop();

            var syntaxList = new ExpressionSyntax[node.Fields.Length];

            for (int i = 0, j = node.Fields.Length - 1; i < node.Fields.Length; i++, j--) args[j] = Nodes.Pop();

            var keysElements = new List<ObjectCreationExpressionSyntax>();

            for (var i = 0; i < args.Length; i++)
            {
                syntaxList[i] =
                    SyntaxHelper.CreateArrayOfObjects(args.Take(i + 1).Cast<ExpressionSyntax>().ToArray());

                var currentKey = new ArgumentSyntax[i + 1];
                for (var j = i; j >= 0; j--) currentKey[j] = SyntaxFactory.Argument((ExpressionSyntax) args[j]);

                keysElements.Add(
                    SyntaxHelper.CreateObjectOf(
                        nameof(GroupKey),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(currentKey))));
            }

            _groupValues =
                SyntaxHelper.CreateAssignment("values", SyntaxHelper.CreateArrayOf(nameof(Object), syntaxList, 2));
            _groupKeys = SyntaxHelper.CreateAssignment("keys",
                SyntaxHelper.CreateArrayOfObjects(nameof(GroupKey), keysElements.Cast<ExpressionSyntax>().ToArray()));
            _groupHaving = having;


            var groupFields = _scope.ScopeSymbolTable.GetSymbol<FieldsNamesSymbol>("groupFields");

            var fieldNames = new StringBuilder();
            string fieldName;
            fieldNames.Append("var groupFieldsNames = new string[][]{");
            for (var i = 0; i < groupFields.Names.Length - 1; i++)
            {
                fieldName =
                    $"new string[]{{{groupFields.Names.Where((f, idx) => idx <= i).Select(f => $"@\"{f}\"").Aggregate((a, b) => a + "," + b)}}}";
                fieldNames.Append(fieldName);
                fieldNames.Append(',');
            }

            fieldName =
                $"new string[]{{{groupFields.Names.Select(f => $"@\"{f}\"").Aggregate((a, b) => a + "," + b)}}}";
            fieldNames.Append(fieldName);
            fieldNames.Append("};");

            Statements.Add(SyntaxFactory.ParseStatement(fieldNames.ToString()));
            NullSuspiciousNodes.Clear();

            AddNamespace(typeof(GroupKey).Namespace);
        }

        public void Visit(HavingNode node)
        {
            NullSuspiciousNodes.Clear();
            Nodes.Push(Generator.IfStatement(Generator.LogicalNotExpression(Nodes.Pop()),
                    new SyntaxNode[] {SyntaxFactory.ContinueStatement()})
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
                new SyntaxNode[]
                {
                    SyntaxFactory.PostfixUnaryExpression(
                        SyntaxKind.PostIncrementExpression,
                        SyntaxFactory.IdentifierName(identifier)),
                    SyntaxFactory.ContinueStatement()
                });

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
                    new SyntaxNode[]
                    {
                        SyntaxFactory.BreakStatement()
                    });

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
            var ifStatement = Generator.IfStatement(Generator.LogicalNotExpression(Nodes.Pop()),
                    new SyntaxNode[] {SyntaxFactory.ContinueStatement()})
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            _emptyBlock = SyntaxFactory.Block();

            var computingBlock = SyntaxFactory.Block();
            switch (node.JoinType)
            {
                case JoinType.Inner:
                    computingBlock = computingBlock.AddStatements(
                        SyntaxFactory.ForEachStatement(
                            SyntaxFactory.IdentifierName("var"),
                            SyntaxFactory.Identifier($"{node.InMemoryTableAlias}Row"),
                            SyntaxFactory.IdentifierName(
                                $"{nameof(EvaluationHelper)}.{nameof(EvaluationHelper.ConvertTableToSource)}({node.InMemoryTableAlias}TransitionTable).{nameof(RowSource.Rows)}"),
                            SyntaxFactory.Block(
                                SyntaxFactory.SingletonList<StatementSyntax>(
                                    SyntaxFactory.ForEachStatement(
                                        SyntaxFactory.IdentifierName("var"),
                                        SyntaxFactory.Identifier($"{node.SourceTable.Alias}Row"),
                                        SyntaxFactory.IdentifierName($"{node.SourceTable.Alias}Rows.Rows"),
                                        SyntaxFactory.Block(
                                            GenerateCancellationExpression(),
                                            (StatementSyntax) ifStatement,
                                            _emptyBlock))))));
                    break;
                case JoinType.OuterLeft:

                    var fullTransitionTable = _scope.ScopeSymbolTable.GetSymbol<TableSymbol>(_queryAlias);
                    var fieldNames =
                        _scope.ScopeSymbolTable.GetSymbol<FieldsNamesSymbol>(MetaAttributes.OuterJoinSelect);
                    var expressions = new List<ExpressionSyntax>();

                    int j = 0;
                    for (int i = 0; i < fullTransitionTable.CompoundTables.Length - 1; i++)
                    {
                        foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[i]))
                        {
                            expressions.Add(
                                SyntaxFactory.ElementAccessExpression(
                                    SyntaxFactory.IdentifierName($"{node.InMemoryTableAlias}Row"),
                                    SyntaxFactory.BracketedArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(
                                                (LiteralExpressionSyntax) Generator.LiteralExpression(
                                                    fieldNames.Names[j]))))));

                            j += 1;
                        }
                    }

                    foreach (var column in fullTransitionTable.GetColumns(
                                 fullTransitionTable.CompoundTables[^1]))
                    {
                        expressions.Add(
                            SyntaxFactory.CastExpression(
                                SyntaxFactory.IdentifierName(
                                    EvaluationHelper.GetCastableType(column.ColumnType)),
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
                        new[]
                        {
                            SyntaxFactory.Argument(
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.Token(SyntaxKind.NewKeyword)
                                        .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                                    SyntaxFactory.ParseTypeName(nameof(ObjectsRow)),
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SeparatedList(
                                            new[]
                                            {
                                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("select")),
                                                SyntaxFactory.Argument(
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.IdentifierName($"{node.InMemoryTableAlias}Row"),
                                                        SyntaxFactory.IdentifierName(
                                                            $"{nameof(IObjectResolver.Contexts)}"))),
                                                SyntaxFactory.Argument(
                                                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))
                                            })
                                    ),
                                    SyntaxFactory.InitializerExpression(SyntaxKind.ComplexElementInitializerExpression))
                            )
                        });

                    computingBlock = computingBlock.AddStatements(
                        SyntaxFactory.ForEachStatement(
                            SyntaxFactory.IdentifierName("var"),
                            SyntaxFactory.Identifier($"{node.InMemoryTableAlias}Row"),
                            SyntaxFactory.IdentifierName(
                                $"{nameof(EvaluationHelper)}.{nameof(EvaluationHelper.ConvertTableToSource)}({node.InMemoryTableAlias}TransitionTable).{nameof(RowSource.Rows)}"),
                            SyntaxFactory.Block(
                                SyntaxFactory.LocalDeclarationStatement(
                                    SyntaxHelper.CreateAssignment("hasAnyRowMatched",
                                        SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))),
                                SyntaxFactory.ForEachStatement(
                                    SyntaxFactory.IdentifierName("var"),
                                    SyntaxFactory.Identifier($"{node.SourceTable.Alias}Row"),
                                    SyntaxFactory.IdentifierName($"{node.SourceTable.Alias}Rows.Rows"),
                                    SyntaxFactory.Block(
                                        GenerateCancellationExpression(),
                                        (StatementSyntax) ifStatement,
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
                case JoinType.OuterRight:

                    fullTransitionTable = _scope.ScopeSymbolTable.GetSymbol<TableSymbol>(_queryAlias);
                    fieldNames = _scope.ScopeSymbolTable.GetSymbol<FieldsNamesSymbol>(MetaAttributes.OuterJoinSelect);
                    expressions = new List<ExpressionSyntax>();

                    j = 0;
                    for (int i = 0; i < fullTransitionTable.CompoundTables.Length - 1; i++)
                    {
                        foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[i]))
                        {
                            expressions.Add(
                                SyntaxFactory.CastExpression(
                                    SyntaxFactory.IdentifierName(
                                        EvaluationHelper.GetCastableType(column.ColumnType)),
                                    (LiteralExpressionSyntax) Generator.NullLiteralExpression()));

                            j += 1;
                        }
                    }

                    foreach (var column in fullTransitionTable.GetColumns(
                                 fullTransitionTable.CompoundTables[fullTransitionTable.CompoundTables.Length - 1]))
                    {
                        expressions.Add(
                            SyntaxFactory.ElementAccessExpression(
                                SyntaxFactory.IdentifierName($"{node.SourceTable.Alias}Row"),
                                SyntaxFactory.BracketedArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            (LiteralExpressionSyntax) Generator.LiteralExpression(
                                                fieldNames.Names[j++]))))));
                    }

                    arrayType = SyntaxFactory.ArrayType(
                        SyntaxFactory.IdentifierName("object"),
                        new SyntaxList<ArrayRankSpecifierSyntax>(
                            SyntaxFactory.ArrayRankSpecifier(
                                SyntaxFactory.SingletonSeparatedList(
                                    (ExpressionSyntax) SyntaxFactory.OmittedArraySizeExpression()))));

                    rewriteSelect =
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


                    invocation = SyntaxHelper.CreateMethodInvocation(
                        _scope[MetaAttributes.SelectIntoVariableName],
                        nameof(Table.Add),
                        new[]
                        {
                            SyntaxFactory.Argument(
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.Token(SyntaxKind.NewKeyword)
                                        .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                                    SyntaxFactory.ParseTypeName(nameof(ObjectsRow)),
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SeparatedList(
                                            new[]
                                            {
                                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("select")),
                                                SyntaxFactory.Argument(
                                                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                                SyntaxFactory.Argument(
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.IdentifierName($"{node.SourceTable.Alias}Row"),
                                                        SyntaxFactory.IdentifierName(
                                                            $"{nameof(IObjectResolver.Contexts)}")))
                                            })
                                    ),
                                    SyntaxFactory.InitializerExpression(SyntaxKind.ComplexElementInitializerExpression))
                            )
                        });

                    computingBlock = computingBlock.AddStatements(
                        SyntaxFactory.ForEachStatement(
                            SyntaxFactory.IdentifierName("var"),
                            SyntaxFactory.Identifier($"{node.SourceTable.Alias}Row"),
                            SyntaxFactory.IdentifierName($"{node.SourceTable.Alias}Rows.Rows"),
                            SyntaxFactory.Block(
                                SyntaxFactory.LocalDeclarationStatement(
                                    SyntaxHelper.CreateAssignment("hasAnyRowMatched",
                                        SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))),
                                SyntaxFactory.ForEachStatement(
                                    SyntaxFactory.IdentifierName("var"),
                                    SyntaxFactory.Identifier($"{node.InMemoryTableAlias}Row"),
                                    SyntaxFactory.IdentifierName(
                                        $"{nameof(EvaluationHelper)}.{nameof(EvaluationHelper.ConvertTableToSource)}({node.InMemoryTableAlias}TransitionTable).{nameof(RowSource.Rows)}"),
                                    SyntaxFactory.Block(
                                        GenerateCancellationExpression(),
                                        (StatementSyntax) ifStatement,
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

            NullSuspiciousNodes.Clear();

            _joinBlock = computingBlock;
        }

        public void Visit(SchemaFromNode node)
        {
            var originColumns = InferredColumns[node];

            var listOfColumns = new List<ExpressionSyntax>();
            foreach (var column in originColumns)
            {
                listOfColumns.Add(
                    SyntaxHelper.CreateObjectOf(
                        nameof(Column),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(new[]
                            {
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        SyntaxFactory.Literal(column.ColumnName))),
                                SyntaxHelper.TypeLiteralArgument(
                                    EvaluationHelper.GetCastableType(column.ColumnType)),
                                SyntaxHelper.IntLiteralArgument(column.ColumnIndex)
                            }))));
            }

            var tableInfoVariableName = node.Alias.ToInfoTable();
            var tableInfoObject = SyntaxHelper.CreateAssignment(
                tableInfoVariableName,
                SyntaxHelper.CreateArrayOf(
                    nameof(ISchemaColumn),
                    listOfColumns.ToArray()));

            var createdSchema = SyntaxHelper.CreateAssignmentByMethodCall(
                node.Alias,
                "provider",
                nameof(ISchemaProvider.GetSchema),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxHelper.StringLiteralArgument(node.Schema)
                    }),
                    SyntaxFactory.Token(SyntaxKind.CloseParenToken)
                )
            );

            var args = new List<ExpressionSyntax>();
            var argList = (ArgumentListSyntax) Nodes.Pop();
            args.AddRange(argList.Arguments.Select(arg => arg.Expression));

            var createdSchemaRows = SyntaxHelper.CreateAssignmentByMethodCall(
                $"{node.Alias}Rows",
                node.Alias,
                nameof(ISchema.GetRowSource),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxHelper.StringLiteralArgument(node.Method),
                        SyntaxFactory.Argument(
                            CreateRuntimeContext(node, SyntaxFactory.IdentifierName(tableInfoVariableName))),
                        SyntaxFactory.Argument(
                            SyntaxHelper.CreateArrayOf(
                                nameof(Object),
                                args.ToArray()))
                    })
                ));

            Statements.Add(SyntaxFactory.LocalDeclarationStatement(tableInfoObject));
            Statements.Add(SyntaxFactory.LocalDeclarationStatement(createdSchema));
            Statements.Add(SyntaxFactory.LocalDeclarationStatement(createdSchemaRows));
        }

        public void Visit(JoinSourcesTableFromNode node)
        {
            var ifStatement = Generator.IfStatement(Generator.LogicalNotExpression(Nodes.Pop()),
                    new SyntaxNode[] {SyntaxFactory.ContinueStatement()})
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            _emptyBlock = SyntaxFactory.Block();

            var computingBlock = SyntaxFactory.Block();
            switch (node.JoinType)
            {
                case JoinType.Inner:
                    computingBlock =
                        computingBlock.AddStatements(
                            SyntaxFactory.ForEachStatement(SyntaxFactory.IdentifierName("var"),
                                SyntaxFactory.Identifier($"{node.First.Alias}Row"),
                                SyntaxFactory.IdentifierName($"{node.First.Alias}Rows.Rows"),
                                SyntaxFactory.Block(
                                    SyntaxFactory.SingletonList<StatementSyntax>(
                                        SyntaxFactory.ForEachStatement(
                                            SyntaxFactory.IdentifierName("var"),
                                            SyntaxFactory.Identifier($"{node.Second.Alias}Row"),
                                            SyntaxFactory.IdentifierName($"{node.Second.Alias}Rows.Rows"),
                                            SyntaxFactory.Block(
                                                GenerateCancellationExpression(),
                                                (StatementSyntax) ifStatement,
                                                _emptyBlock))))));
                    break;
                case JoinType.OuterLeft:

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
                                SyntaxFactory.IdentifierName(
                                    EvaluationHelper.GetCastableType(column.ColumnType)),
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
                        new[]
                        {
                            SyntaxFactory.Argument(
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.Token(SyntaxKind.NewKeyword)
                                        .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                                    SyntaxFactory.ParseTypeName(nameof(ObjectsRow)),
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SeparatedList(
                                            new[]
                                            {
                                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("select")),
                                                SyntaxFactory.Argument(
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.IdentifierName($"{node.First.Alias}Row"),
                                                        SyntaxFactory.IdentifierName(
                                                            $"{nameof(IObjectResolver.Contexts)}"))),
                                                SyntaxFactory.Argument(
                                                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))
                                            })
                                    ),
                                    SyntaxFactory.InitializerExpression(SyntaxKind.ComplexElementInitializerExpression))
                            )
                        });

                    computingBlock =
                        computingBlock.AddStatements(
                            SyntaxFactory.ForEachStatement(SyntaxFactory.IdentifierName("var"),
                                SyntaxFactory.Identifier($"{node.First.Alias}Row"),
                                SyntaxFactory.IdentifierName($"{node.First.Alias}Rows.Rows"),
                                SyntaxFactory.Block(
                                    SyntaxFactory.LocalDeclarationStatement(
                                        SyntaxHelper.CreateAssignment("hasAnyRowMatched",
                                            (LiteralExpressionSyntax) Generator.FalseLiteralExpression())),
                                    SyntaxFactory.ForEachStatement(
                                        SyntaxFactory.IdentifierName("var"),
                                        SyntaxFactory.Identifier($"{node.Second.Alias}Row"),
                                        SyntaxFactory.IdentifierName($"{node.Second.Alias}Rows.Rows"),
                                        SyntaxFactory.Block(
                                            GenerateCancellationExpression(),
                                            (StatementSyntax) ifStatement,
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
                case JoinType.OuterRight:

                    fullTransitionTable = _scope.ScopeSymbolTable.GetSymbol<TableSymbol>(_queryAlias);
                    expressions = new List<ExpressionSyntax>();

                    foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[0]))
                    {
                        expressions.Add(
                            SyntaxFactory.CastExpression(
                                SyntaxFactory.IdentifierName(
                                    EvaluationHelper.GetCastableType(column.ColumnType)),
                                (LiteralExpressionSyntax) Generator.NullLiteralExpression()));
                    }

                    foreach (var column in fullTransitionTable.GetColumns(fullTransitionTable.CompoundTables[1]))
                    {
                        expressions.Add(
                            SyntaxFactory.ElementAccessExpression(
                                SyntaxFactory.IdentifierName($"{node.Second.Alias}Row"),
                                SyntaxFactory.BracketedArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            (LiteralExpressionSyntax) Generator.LiteralExpression(
                                                column.ColumnName))))));
                    }

                    arrayType = SyntaxFactory.ArrayType(
                        SyntaxFactory.IdentifierName("object"),
                        new SyntaxList<ArrayRankSpecifierSyntax>(
                            SyntaxFactory.ArrayRankSpecifier(
                                SyntaxFactory.SingletonSeparatedList(
                                    (ExpressionSyntax) SyntaxFactory.OmittedArraySizeExpression()))));

                    rewriteSelect =
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


                    invocation = SyntaxHelper.CreateMethodInvocation(
                        _scope[MetaAttributes.SelectIntoVariableName],
                        nameof(Table.Add),
                        new[]
                        {
                            SyntaxFactory.Argument(
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.Token(SyntaxKind.NewKeyword)
                                        .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                                    SyntaxFactory.ParseTypeName(nameof(ObjectsRow)),
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SeparatedList(
                                            new[]
                                            {
                                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("select")),
                                                SyntaxFactory.Argument(
                                                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                                SyntaxFactory.Argument(
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.IdentifierName($"{node.Second.Alias}Row"),
                                                        SyntaxFactory.IdentifierName(
                                                            $"{nameof(IObjectResolver.Contexts)}")))
                                            })
                                    ),
                                    SyntaxFactory.InitializerExpression(SyntaxKind.ComplexElementInitializerExpression))
                            )
                        });

                    computingBlock =
                        computingBlock.AddStatements(
                            SyntaxFactory.ForEachStatement(SyntaxFactory.IdentifierName("var"),
                                SyntaxFactory.Identifier($"{node.Second.Alias}Row"),
                                SyntaxFactory.IdentifierName($"{node.Second.Alias}Rows.Rows"),
                                SyntaxFactory.Block(
                                    SyntaxFactory.LocalDeclarationStatement(
                                        SyntaxHelper.CreateAssignment("hasAnyRowMatched",
                                            (LiteralExpressionSyntax) Generator.FalseLiteralExpression())),
                                    SyntaxFactory.ForEachStatement(
                                        SyntaxFactory.IdentifierName("var"),
                                        SyntaxFactory.Identifier($"{node.First.Alias}Row"),
                                        SyntaxFactory.IdentifierName($"{node.First.Alias}Rows.Rows"),
                                        SyntaxFactory.Block(
                                            GenerateCancellationExpression(),
                                            (StatementSyntax) ifStatement,
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

            NullSuspiciousNodes.Clear();
            _joinBlock = computingBlock;
        }

        public void Visit(InMemoryTableFromNode node)
        {
            Statements.Add(SyntaxFactory.LocalDeclarationStatement(SyntaxFactory
                .VariableDeclaration(SyntaxFactory.IdentifierName("var")).WithVariables(
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory
                        .VariableDeclarator(SyntaxFactory.Identifier(node.Alias.ToRowsSource())).WithInitializer(
                            SyntaxFactory.EqualsValueClause(SyntaxFactory
                                .InvocationExpression(SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper.ConvertTableToSource))))
                                .WithArgumentList(
                                    SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(SyntaxFactory
                                            .ElementAccessExpression(
                                                SyntaxFactory.IdentifierName("_tableResults")).WithArgumentList(
                                                SyntaxFactory.BracketedArgumentList(
                                                    SyntaxFactory.SingletonSeparatedList(
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.LiteralExpression(
                                                                SyntaxKind.NumericLiteralExpression,
                                                                SyntaxFactory.Literal(
                                                                    _inMemoryTableIndexes[
                                                                        node.VariableName]))))))))))))))));
        }

        public void Visit(JoinFromNode node)
        {
        }

        public void Visit(ExpressionFromNode node)
        {
            Nodes.Push(SyntaxFactory.Block());
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
                                SyntaxFactory.SeparatedList(new[]
                                {
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            SyntaxFactory.Literal($"@\"{field.FieldName}\"", field.FieldName))),
                                    SyntaxHelper.TypeLiteralArgument(
                                        EvaluationHelper.GetCastableType(type)),
                                    SyntaxHelper.IntLiteralArgument(field.FieldOrder)
                                }))));
                }

                var createObject = SyntaxHelper.CreateAssignment(
                    _scope[MetaAttributes.CreateTableVariableName],
                    SyntaxHelper.CreateObjectOf(
                        nameof(Table),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(
                                new[]
                                {
                                    SyntaxFactory.Argument((ExpressionSyntax) Generator.LiteralExpression(node.Name)),
                                    SyntaxFactory.Argument(
                                        SyntaxHelper.CreateArrayOf(
                                            nameof(Column),
                                            cols.ToArray()))
                                }))));

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

            NullSuspiciousNodes.Clear();
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

            var skip = node.Skip != null ? Nodes.Pop() as StatementSyntax : null;
            var take = node.Take != null ? Nodes.Pop() as BlockSyntax : null;

            var select = _selectBlock;
            var where = node.Where != null ? Nodes.Pop() as StatementSyntax : null;

            var block = (BlockSyntax) Nodes.Pop();

            block = block.AddStatements(GenerateStatsUpdateStatements());
            block = block.AddStatements(GenerateCancellationExpression());

            if (where != null)
                block = block.AddStatements(where);

            if (skip != null)
                block = block.AddStatements(skip);

            if (take != null)
                block = block.AddStatements(take.Statements.ToArray());

            block = block.AddStatements(select.Statements.ToArray());
            var fullBlock = SyntaxFactory.Block();

            fullBlock = fullBlock.AddStatements(SyntaxHelper.Foreach("score", _scope[MetaAttributes.SourceName],
                block));
            fullBlock = fullBlock.AddStatements(
                (StatementSyntax) Generator.ReturnStatement(
                    SyntaxFactory.IdentifierName(detailedQuery.ReturnVariableName)));

            Statements.AddRange(fullBlock.Statements);

            NullSuspiciousNodes.Clear();
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
                    .WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturnLineFeed)));
                Statements.Add(SyntaxFactory.ParseStatement("var usedGroups = new HashSet<Group>();")
                    .WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturnLineFeed)));
                Statements.Add(SyntaxFactory.ParseStatement("var groups = new Dictionary<GroupKey, Group>();")
                    .WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturnLineFeed)));

                block = block.AddStatements(GenerateCancellationExpression());

                if (where != null)
                    block = block.AddStatements(where);

                block = block.AddStatements(SyntaxFactory.LocalDeclarationStatement(_groupKeys));
                block = block.AddStatements(SyntaxFactory.LocalDeclarationStatement(_groupValues));

                block = block.AddStatements(SyntaxFactory.ParseStatement("var parent = rootGroup;")
                    .WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturnLineFeed)));
                block = block.AddStatements(SyntaxFactory.ParseStatement("Group group = null;")
                    .WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturnLineFeed)));

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
                                .Add((LiteralExpressionSyntax) Generator.LiteralExpression(node.Select.Fields[i]
                                    .FieldName)));

                var indexToValueDictVariableName = "indexToValueDict";

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
                block = GroupByForeach(block, node.From.Alias.ToRowItem(), _scope[MetaAttributes.SourceName]);
                Statements.AddRange(block.Statements);
            }
            else
            {
                _emptyBlock = _joinBlock.DescendantNodes().OfType<BlockSyntax>()
                    .First(f => f.Statements.Count == 0);
                _joinBlock = _joinBlock.ReplaceNode(_emptyBlock, select.Statements);
                Statements.AddRange(_joinBlock.Statements);
            }

            NullSuspiciousNodes.Clear();
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
            var method = SyntaxFactory.MethodDeclaration(
                new SyntaxList<AttributeListSyntax>(),
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
                SyntaxFactory.IdentifierName(nameof(Table)).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                null,
                SyntaxFactory.Identifier(nameof(IRunnable.Run)),
                null,
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Parameter(
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxTokenList.Create(new SyntaxToken()),
                            SyntaxFactory.IdentifierName(nameof(CancellationToken))
                                .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                            SyntaxFactory.Identifier("token"), null)
                    })),
                new SyntaxList<TypeParameterConstraintClauseSyntax>(),
                SyntaxFactory.Block(SyntaxFactory.ParseStatement(
                    $"return {_methodNames.Pop()}(Provider, PositionalEnvironmentVariables, QueriesInformation, token);")),
                null);

            var providerParam = SyntaxFactory.PropertyDeclaration(
                new SyntaxList<AttributeListSyntax>(),
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
                SyntaxFactory.IdentifierName(nameof(ISchemaProvider)).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                null,
                SyntaxFactory.Identifier(nameof(IRunnable.Provider)),
                SyntaxFactory.AccessorList(
                    SyntaxFactory.List<AccessorDeclarationSyntax>()
                        .Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
                        .Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))),
                null,
                null);

            var positionalEnvironmentVariablesParam = SyntaxFactory.PropertyDeclaration(
                new SyntaxList<AttributeListSyntax>(),
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
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
                null,
                SyntaxFactory.Identifier(nameof(IRunnable.PositionalEnvironmentVariables)),
                SyntaxFactory.AccessorList(
                    SyntaxFactory.List<AccessorDeclarationSyntax>()
                        .Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
                        .Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))),
                null,
                null);

            var queriesInformationParam = SyntaxFactory.PropertyDeclaration(
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
                                                                    SyntaxFactory.Identifier("IReadOnlyCollection"))
                                                                .WithTypeArgumentList(
                                                                    SyntaxFactory.TypeArgumentList(
                                                                        SyntaxFactory
                                                                            .SingletonSeparatedList<TypeSyntax>(
                                                                                SyntaxFactory.IdentifierName(
                                                                                    "ISchemaColumn")))))
                                                        .WithIdentifier(
                                                            SyntaxFactory.Identifier("UsedColumns")),
                                                    SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                    SyntaxFactory.TupleElement(
                                                            SyntaxFactory.IdentifierName("WhereNode"))
                                                        .WithIdentifier(
                                                            SyntaxFactory.Identifier("WhereNode"))
                                                }))
                                    }))),
                    SyntaxFactory.Identifier("QueriesInformation"))
                .WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAccessorList(
                    SyntaxFactory.AccessorList(
                        SyntaxFactory.List(
                            new[]
                            {
                                SyntaxFactory.AccessorDeclaration(
                                        SyntaxKind.GetAccessorDeclaration)
                                    .WithSemicolonToken(
                                        SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                                SyntaxFactory.AccessorDeclaration(
                                        SyntaxKind.SetAccessorDeclaration)
                                    .WithSemicolonToken(
                                        SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                            })))
                .NormalizeWhitespace();

            _members.Add(method);
            _members.Add(providerParam);
            _members.Add(positionalEnvironmentVariablesParam);
            _members.Add(queriesInformationParam);

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
                new SyntaxNode[]
                {
                    SyntaxFactory.IdentifierName(nameof(BaseOperations)),
                    SyntaxFactory.IdentifierName(nameof(IRunnable))
                }, _members);

            var ns = SyntaxFactory.NamespaceDeclaration(
                SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(Namespace)),
                SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
                SyntaxFactory.List(
                    _namespaces.Select(
                        n => SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(n)))),
                SyntaxFactory.List<MemberDeclarationSyntax>(new[] {(ClassDeclarationSyntax) classDeclaration}));

            var compilationUnit = SyntaxFactory.CompilationUnit(
                SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
                SyntaxFactory.List<UsingDirectiveSyntax>(),
                SyntaxFactory.List<AttributeListSyntax>(),
                SyntaxFactory.List<MemberDeclarationSyntax>(new[] {ns}));

            var options = Workspace.Options;
            options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInMethods, true);
            options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInProperties, true);

            var formatted = Formatter.Format(compilationUnit, Workspace);

            Compilation = Compilation.AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(formatted.ToFullString(),
                new CSharpParseOptions(LanguageVersion.CSharp7_3), null, Encoding.ASCII));
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
                            new SyntaxNode[]
                            {
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("provider")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("queriesInformation")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("token"))
                            }
                        )));

            var bInvocation = SyntaxFactory
                .InvocationExpression(SyntaxFactory.IdentifierName(b))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            new SyntaxNode[]
                            {
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("provider")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("queriesInformation")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("token"))
                            }
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
                            new SyntaxNode[]
                            {
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("provider")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("queriesInformation")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("token"))
                            }
                        )));

            var bInvocation = SyntaxFactory
                .InvocationExpression(SyntaxFactory.IdentifierName(b))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            new SyntaxNode[]
                            {
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("provider")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("queriesInformation")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("token"))
                            }
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
                            new SyntaxNode[]
                            {
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("provider")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("queriesInformation")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("token"))
                            }
                        )));

            var bInvocation = SyntaxFactory
                .InvocationExpression(SyntaxFactory.IdentifierName(b))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            new SyntaxNode[]
                            {
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("provider")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("queriesInformation")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("token"))
                            }
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
                            new SyntaxNode[]
                            {
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("provider")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("queriesInformation")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("token"))
                            }
                        )));

            var bInvocation = SyntaxFactory
                .InvocationExpression(SyntaxFactory.IdentifierName(b))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            new SyntaxNode[]
                            {
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("provider")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("queriesInformation")),
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName("token"))
                            }
                        )));

            _members.Add(GenerateMethod(name, nameof(BaseOperations.Intersect), _scope[MetaAttributes.SetOperatorName],
                aInvocation, bInvocation));
        }

        public void Visit(RefreshNode node)
        {
            if (node.Nodes.Length == 0)
                return;

            var block = SyntaxFactory.Block();
            for (int i = 0, k = node.Nodes.Length - 1; i < node.Nodes.Length; i++, k--)
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

            var method = SyntaxFactory.MethodDeclaration(
                new SyntaxList<AttributeListSyntax>(),
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
                SyntaxFactory.IdentifierName(nameof(Table)).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                null,
                SyntaxFactory.Identifier(methodName),
                null,
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Parameter(
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxTokenList.Create(
                                new SyntaxToken()),
                            SyntaxFactory.IdentifierName(nameof(ISchemaProvider))
                                .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                            SyntaxFactory.Identifier("provider"), null),

                        SyntaxFactory.Parameter(
                            new SyntaxList<AttributeListSyntax>(),
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
                                                                        SyntaxFactory.Identifier("WhereNode"))
                                                            }))
                                                })))),

                        SyntaxFactory.Parameter(
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxTokenList.Create(
                                new SyntaxToken()),
                            SyntaxFactory.IdentifierName(nameof(CancellationToken))
                                .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                            SyntaxFactory.Identifier("token"), null)
                    })),
                new SyntaxList<TypeParameterConstraintClauseSyntax>(),
                SyntaxFactory.Block(Statements),
                null);

            _members.Add(method);
            _typesToInstantiate.Clear();
            Statements.Clear();
        }

        public void Visit(CteExpressionNode node)
        {
            var statements = new List<StatementSyntax>();

            var resultCteMethodName = _methodNames.Pop();

            foreach (var _ in node.InnerExpression)
            {
                _methodNames.Pop();
                statements.Add((StatementSyntax) Nodes.Pop());
            }

            statements.Reverse();

            var methodName = "CteResultQuery";

            statements.Add(
                SyntaxFactory.ReturnStatement(SyntaxFactory
                    .InvocationExpression(SyntaxFactory.IdentifierName(resultCteMethodName)).WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(new[]
                            {
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("provider")),
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("queriesInformation")),
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("token"))
                            })))));

            var method = SyntaxFactory.MethodDeclaration(
                new SyntaxList<AttributeListSyntax>(),
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
                SyntaxFactory.IdentifierName(nameof(Table)).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                null,
                SyntaxFactory.Identifier(methodName),
                null,
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Parameter(
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxTokenList.Create(
                                new SyntaxToken()),
                            SyntaxFactory.IdentifierName(nameof(ISchemaProvider))
                                .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                            SyntaxFactory.Identifier("provider"), null),

                        SyntaxFactory.Parameter(
                            new SyntaxList<AttributeListSyntax>(),
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
                                                                        SyntaxFactory.Identifier("WhereNode"))
                                                            }))
                                                })))),

                        SyntaxFactory.Parameter(
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxTokenList.Create(
                                new SyntaxToken()),
                            SyntaxFactory.IdentifierName(nameof(CancellationToken))
                                .WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                            SyntaxFactory.Identifier("token"), null)
                    })),
                new SyntaxList<TypeParameterConstraintClauseSyntax>(),
                SyntaxFactory.Block(statements),
                null);

            _members.Add(method);
            _methodNames.Push(methodName);
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
                            new[]
                            {
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("provider")),
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("positionalEnvironmentVariables")),
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("queriesInformation")),
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("token"))
                            }))))));
        }

        public void Visit(JoinsNode node)
        {
        }

        public void Visit(JoinNode node)
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

        public void IncrementMethodIdentifier()
        {
            _setOperatorMethodIdentifier += 1;
        }

        private void AddNamespace(string columnTypeNamespace)
        {
            if (!_namespaces.Contains(columnTypeNamespace))
                _namespaces.Add(columnTypeNamespace);
        }

        private void AddNamespace(Type[] types)
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
                Compilation =
                    Compilation.AddReferences(MetadataReference.CreateFromFile(type.Assembly.Location));
            }
        }

        private void AddReference(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
                if (!_loadedAssemblies.Contains(assembly.Location))
                {
                    _loadedAssemblies.Add(assembly.Location);
                    Compilation =
                        Compilation.AddReferences(MetadataReference.CreateFromFile(assembly.Location));
                }
        }

        private StatementSyntax GenerateStatsUpdateStatements()
        {
            return SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                SyntaxKind.AddAssignmentExpression,
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("stats"), SyntaxFactory.IdentifierName("RowNumber")),
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1))));
        }

        private BlockSyntax GroupByForeach(BlockSyntax foreachInstructions, string variableName, string tableVariable)
        {
            return SyntaxFactory.Block(
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
            return SyntaxFactory
                .MethodDeclaration(SyntaxFactory.IdentifierName(nameof(Table)), SyntaxFactory.Identifier(methodName))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
                .WithParameterList(SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(
                        new[]
                        {
                            SyntaxFactory.Parameter(SyntaxFactory.Identifier("provider"))
                                .WithType(SyntaxFactory.IdentifierName(nameof(ISchemaProvider))),
                            SyntaxFactory.Parameter(
                                new SyntaxList<AttributeListSyntax>(),
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
                                                                            SyntaxFactory.Token(SyntaxKind
                                                                                .StringKeyword)),
                                                                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                                                                        SyntaxFactory.PredefinedType(
                                                                            SyntaxFactory.Token(SyntaxKind
                                                                                .StringKeyword))
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
                                                                            SyntaxFactory.IdentifierName(
                                                                                "SchemaFromNode"))
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
                                                                            SyntaxFactory.Identifier("WhereNode"))
                                                                }))
                                                    })))),
                            SyntaxFactory.Parameter(SyntaxFactory.Identifier("token"))
                                .WithType(SyntaxFactory.IdentifierName(nameof(CancellationToken)))
                        }))).WithBody(
                    SyntaxFactory.Block(
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
                                            })))))));
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

        private SyntaxNode GenerateNullGuards(SyntaxNode rawNode)
        {
            if (NullSuspiciousNodes.Count > 1)
            {
                rawNode = SyntaxFactory.ParenthesizedExpression(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.LogicalAndExpression,
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.LogicalAndExpression,
                            SyntaxFactory.BinaryExpression(
                                SyntaxKind.NotEqualsExpression,
                                (ExpressionSyntax) NullSuspiciousNodes.Pop(),
                                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                            SyntaxFactory.BinaryExpression(
                                SyntaxKind.NotEqualsExpression,
                                (ExpressionSyntax) NullSuspiciousNodes.Pop(),
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NullLiteralExpression))),
                        (BinaryExpressionSyntax) rawNode));
            }
            else if (NullSuspiciousNodes.Count == 1)
            {
                rawNode = SyntaxFactory.ParenthesizedExpression(
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.LogicalAndExpression,
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.NotEqualsExpression,
                            (ExpressionSyntax) NullSuspiciousNodes.Pop(),
                            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                        (BinaryExpressionSyntax) rawNode));
            }

            return rawNode;
        }

        public void Visit(OrderByNode node)
        {
            AddNamespace("System.Linq");
        }

        public void Visit(CreateTableNode node)
        {
        }

        public void Visit(CoupleNode node)
        {
        }

        public void Visit(SchemaMethodFromNode node)
        {
        }

        public void Visit(AliasedFromNode node)
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
            (Node When, Node Then) whenThenPair = node.WhenThenPairs[0];
            var then = Nodes.Pop();
            var when = Nodes.Pop();

            var ifStatement =
                SyntaxFactory.IfStatement(
                    (ExpressionSyntax) when,
                    SyntaxFactory.Block(
                        SyntaxFactory.SingletonList<StatementSyntax>(
                            SyntaxFactory.ReturnStatement(
                                (ExpressionSyntax) then))));

            var ifStatements = new List<IfStatementSyntax>
            {
                ifStatement
            };

            for (int i = 1; i < node.WhenThenPairs.Length; i++)
            {
                whenThenPair = node.WhenThenPairs[i];
                then = Nodes.Pop();
                when = Nodes.Pop();

                ifStatements.Add(
                    SyntaxFactory.IfStatement(
                        (ExpressionSyntax) when,
                        SyntaxFactory.Block(
                            SyntaxFactory.SingletonList<StatementSyntax>(
                                SyntaxFactory.ReturnStatement(
                                    (ExpressionSyntax) then)))));
            }

            var elseNode = Nodes.Pop();

            ifStatements[^1] =
                ifStatements[^1].WithElse(
                    SyntaxFactory.ElseClause(
                        SyntaxFactory.Block(
                            SyntaxFactory.SingletonList<StatementSyntax>(
                                SyntaxFactory.ReturnStatement(
                                    (ExpressionSyntax) elseNode)))));

            IfStatementSyntax first;
            IfStatementSyntax second;

            IfStatementSyntax newIfStatement = null;

            for (var i = ifStatements.Count - 2; i >= 1; i -= 1)
            {
                first = ifStatements[i];
                second = ifStatements[i + 1];

                ifStatements.RemoveAt(i + 1);
                ifStatements.RemoveAt(i);

                newIfStatement =
                    first.WithElse(
                        SyntaxFactory.ElseClause(second));

                ifStatements.Add(newIfStatement);
            }

            if (ifStatements.Count == 2)
            {
                first = ifStatements[0];
                second = ifStatements[1];

                ifStatements.RemoveAt(1);
                ifStatements.RemoveAt(0);

                newIfStatement =
                    first.WithElse(
                        SyntaxFactory.ElseClause(second));
            }
            else
            {
                newIfStatement = ifStatements[0];

                ifStatements.RemoveAt(0);
            }

            ifStatement = newIfStatement ?? throw new NullReferenceException(nameof(newIfStatement));

            AddNamespace(node.ReturnType.Namespace);
            AddNamespace(typeof(IObjectResolver).Namespace);

            var methodName = $"CaseWhen_{_caseWhenMethodIndex++}";

            var parameters = new List<ParameterSyntax>();
            var callParameters = new List<SyntaxNode>();

            parameters.Add(
                SyntaxFactory.Parameter(
                    SyntaxFactory.Identifier("score")
                ).WithType(
                    SyntaxFactory.IdentifierName(nameof(IObjectResolver))
                ));

            var rowVariableName = string.Empty;
            switch (_oldType)
            {
                case MethodAccessType.TransformingQuery:
                    rowVariableName = $"{_queryAlias}Row";
                    break;
                case MethodAccessType.ResultQuery:
                    rowVariableName = "score";
                    break;
            }

            callParameters.Add(
                SyntaxFactory.Argument(
                    SyntaxFactory.IdentifierName(rowVariableName)));

            foreach (var variableNameTypePair in _typesToInstantiate)
            {
                parameters.Add(
                    SyntaxFactory.Parameter(
                        SyntaxFactory.Identifier(variableNameTypePair.Key)
                    ).WithType(
                        SyntaxFactory.IdentifierName(variableNameTypePair.Value.Name)
                    ));

                callParameters.Add(
                    SyntaxFactory.Argument(
                        SyntaxFactory.IdentifierName(variableNameTypePair.Key)));
            }

            var method = SyntaxFactory
                .MethodDeclaration(
                    SyntaxFactory.IdentifierName(node.ReturnType.FullName),
                    SyntaxFactory.Identifier(methodName))
                .WithModifiers(
                    SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
                .WithParameterList(
                    SyntaxFactory.ParameterList(
                        SyntaxFactory.SeparatedList(parameters.ToArray()))
                )
                .WithBody(
                    SyntaxFactory.Block(
                        SyntaxFactory.SingletonList<StatementSyntax>(ifStatement)));

            _members.Add(method);

            Nodes.Push(
                SyntaxHelper.CreateMethodInvocation("this", methodName, callParameters.ToArray()));
        }

        public void Visit(FieldLinkNode node)
        {
            throw new NotSupportedException();
        }

        private ObjectCreationExpressionSyntax CreateRuntimeContext(SchemaFromNode node, ExpressionSyntax originallyInferredColumns)
        {
            return SyntaxFactory.ObjectCreationExpression(
                    SyntaxFactory.IdentifierName(nameof(RuntimeContext)))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            new[]
                            {
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
                                                            SyntaxFactory.Literal(node.Id)))))))
                            })));
        }
    }
}