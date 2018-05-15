using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;

namespace Musoq.Evaluator.Visitors
{
    public enum MethodAccessType
    {
        ResultQuery,
        TransformingQuery
    }
    public class ToCSharpRewriteTreeVisitor : IToCSharpTranslationExpressionVisitor
    {
        public AdhocWorkspace Workspace { get; }

        public SyntaxGenerator Generator { get; }

        public CSharpCompilation Compilation { get; private set; }

        private Stack<SyntaxNode> Nodes { get; }

        private readonly List<SyntaxNode> _methods = new List<SyntaxNode>();
        private bool _hasGroupBy = false;
        private bool _hasJoin = false;

        private readonly List<string> _namespaces = new List<string>();
        private Scope _scope;
        private int _names;
        private int _sources;
        private int _joins;
        private MethodAccessType _type;
        private string _queryAlias;
        private string _transformedSourceTable;

        private readonly Dictionary<string, Type> _typesToInstantiate = new Dictionary<string, Type>();
        private bool _changeMethodAccessToColumnAccess;
        
        private VariableDeclarationSyntax _groupValues;
        private VariableDeclarationSyntax _groupKeys;
        private SyntaxNode _groupHaving;
        private List<string> _loadedAssemblies = new List<string>();

        private List<StatementSyntax> Statements { get; } = new List<StatementSyntax>();

        public ToCSharpRewriteTreeVisitor(IEnumerable<Assembly> assemblies)
        {
            Workspace = new AdhocWorkspace();

            Generator = SyntaxGenerator.GetGenerator(Workspace, LanguageNames.CSharp);
            Generator.NamespaceImportDeclaration("System");
            Nodes = new Stack<SyntaxNode>();

            var objLocation = typeof(object).GetTypeInfo().Assembly.Location;
            var path = new FileInfo(objLocation);
            var directory = path.Directory;

            Compilation = CSharpCompilation.Create("InMemoryAssembly.dll");

            foreach (var file in directory.GetFiles("System*.dll"))
            {
                try
                {
                    AssemblyName.GetAssemblyName(file.FullName);
                    Compilation = Compilation.AddReferences(MetadataReference.CreateFromFile(file.FullName));
                }
                catch (System.IO.FileNotFoundException)
                {
                    System.Console.WriteLine("The file cannot be found.");
                }
                catch (System.BadImageFormatException)
                {
                    System.Console.WriteLine("The file is not an assembly.");
                }
                catch (System.IO.FileLoadException)
                {
                    System.Console.WriteLine("The assembly has already been loaded.");
                }
            }

            Compilation = Compilation
                .AddReferences(MetadataReference.CreateFromFile("C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\2.0.0\\netstandard.dll"));

            AddReference(typeof(object));
            AddReference(typeof(ISchema));
            AddReference(typeof(LibraryBase));
            AddReference(typeof(Table));
            AddReference(typeof(SyntaxFactory));
            AddReference(assemblies.ToArray());

            Compilation = Compilation.WithOptions(
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Debug,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default)
                    .WithConcurrentBuild(true)
                    .WithPlatform(Platform.AnyCpu));

            AddNamespace("System");
            AddNamespace("System.Collections.Generic");
            AddNamespace("Musoq.Plugins");
            AddNamespace("Musoq.Schema");
            AddNamespace("Musoq.Evaluator");
            AddNamespace("Musoq.Evaluator.Tables");
            AddNamespace("Musoq.Evaluator.Helpers");

            Statements.Add(SyntaxFactory.LocalDeclarationStatement(
                    SyntaxHelper.CreateAssignment(
                        "stats",
                        SyntaxHelper.CreateObjectOf(
                            nameof(AmendableQueryStats),
                            SyntaxFactory.ArgumentList()))));
        }

        public void Visit(Node node)
        {
        }

        public void Visit(DescNode node)
        {
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
            Nodes.Push(Generator.ValueEqualsExpression(a, b));
        }

        public void Visit(GreaterOrEqualNode node)
        {
            var b = Nodes.Pop();
            var a = Nodes.Pop();
            Nodes.Push(Generator.GreaterThanOrEqualExpression(a, b));
        }

        public void Visit(LessOrEqualNode node)
        {
            var b = Nodes.Pop();
            var a = Nodes.Pop();
            Nodes.Push(Generator.LessThanOrEqualExpression(a, b));
        }

        public void Visit(GreaterNode node)
        {
            var b = Nodes.Pop();
            var a = Nodes.Pop();
            Nodes.Push(Generator.GreaterThanExpression(a, b));
        }

        public void Visit(LessNode node)
        {
            var b = Nodes.Pop();
            var a = Nodes.Pop();
            Nodes.Push(Generator.LessThanExpression(a, b));
        }

        public void Visit(DiffNode node)
        {
            var b = Nodes.Pop();
            var a = Nodes.Pop();
            Nodes.Push(Generator.ValueNotEqualsExpression(a, b));
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
                    SyntaxFactory.Argument((ExpressionSyntax)a),
                    SyntaxFactory.Argument((ExpressionSyntax)b),
                }));

            Nodes.Push(arg);

            Visit(new AccessMethodNode(
                new FunctionToken(nameof(Operators.Like), TextSpan.Empty),
                new ArgsListNode(new[] { node.Left, node.Right }), null, typeof(Operators).GetMethod(nameof(Operators.Like))));
        }

        public void Visit(FieldNode node)
        {
            var types = EvaluationHelper.GetNestedTypes(node.ReturnType);
            AddReference(types);
            AddNamespace(types);
            var castedExpression = Generator.CastExpression(
                SyntaxFactory.IdentifierName(
                    EvaluationHelper.GetCastableType(node.ReturnType)), Nodes.Pop());
            Nodes.Push(castedExpression);
        }

        public void Visit(StringNode node)
        {
            Nodes.Push(Generator.LiteralExpression(node.Value));
        }

        public void Visit(DecimalNode node)
        {
            Nodes.Push(Generator.LiteralExpression(node.Value));
        }

        public void Visit(IntegerNode node)
        {
            Nodes.Push(Generator.LiteralExpression(node.Value));
        }

        public void Visit(WordNode node)
        {
            Nodes.Push(Generator.LiteralExpression(node.Value));
        }

        public void Visit(ContainsNode node)
        {
            var comparsionValues = (ArgumentListSyntax)Nodes.Pop();
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
                    SyntaxFactory.Argument((ExpressionSyntax)a),
                    SyntaxFactory.Argument(objExpression)
                }));

            Nodes.Push(arg);

            Visit(new AccessMethodNode(
                new FunctionToken(nameof(Operators.Contains), TextSpan.Empty),
                new ArgsListNode(new[] { node.Left, node.Right }), null, typeof(Operators).GetMethod(nameof(Operators.Contains))));
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

            _scope.ScopeSymbolTable.AddSymbolIfNotExist(method.ReflectedType.Name, new TypeSymbol(method.ReflectedType));

            foreach (var parameterInfo in parameters)
            {
                switch (parameterInfo.GetCustomAttribute<InjectTypeAttribute>())
                {
                    case InjectSourceAttribute injectSource:
                        string objectName;

                        switch (_type)
                        {
                            case MethodAccessType.TransformingQuery:
                                objectName = $"{_queryAlias}Row";
                                break;
                            case MethodAccessType.ResultQuery:
                                objectName = "score";
                                break;
                            default:
                                throw new NotSupportedException();
                        }

                        args.Add(
                            SyntaxFactory.Argument(
                                SyntaxFactory.CastExpression(
                                    SyntaxFactory.IdentifierName(EvaluationHelper.GetCastableType(parameterInfo.ParameterType)), 
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName(objectName), 
                                        SyntaxFactory.IdentifierName(nameof(IObjectResolver.Context))))));
                        break;
                    case InjectGroupAttribute injectGroup:
                        args.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("group")));
                        break;
                    case InjectGroupAccessName injectGroupAccessName:
                        break;
                    case InjectQueryStats injectQueryStats:
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
            
            Nodes.Push(
                Generator.InvocationExpression(
                    Generator.MemberAccessExpression(
                        Generator.IdentifierName(variableName),
                        Generator.IdentifierName(node.Name)), 
                    args));
            
        }

        public void Visit(GroupByAccessMethodNode node)
        {
        }

        public void Visit(AccessRefreshAggreationScoreNode node)
        {
        }

        public void Visit(AccessColumnNode node)
        {
            SyntaxNode sNode;

            string variableName;
            switch (_type)
            {
                case MethodAccessType.TransformingQuery:
                    variableName = $"{node.Alias}Row";
                    break;
                case MethodAccessType.ResultQuery:
                    variableName = "score";
                    break;
                default:
                    throw new NotSupportedException();
            }

            sNode = Generator.ElementAccessExpression(
                Generator.IdentifierName(variableName),
                SyntaxHelper.StringLiteralArgument(node.Name));

            var types = EvaluationHelper.GetNestedTypes(node.ReturnType);
            AddNamespace(types);
            AddReference(types);

            sNode = Generator.CastExpression(
                SyntaxFactory.IdentifierName(
                    EvaluationHelper.GetCastableType(node.ReturnType)), sNode);

            Nodes.Push(sNode);
        }

        public void Visit(AllColumnsNode node)
        {
        }

        public void Visit(IdentifierNode node)
        {
        }

        public void Visit(AccessObjectArrayNode node)
        {
            var exp = SyntaxFactory.ParenthesizedExpression((ExpressionSyntax)Nodes.Pop());

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
            var exp = SyntaxFactory.ParenthesizedExpression((ExpressionSyntax)Nodes.Pop());

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

            for (int i = 0; i < node.Args.Length; i++)
            {
                args = args.Add(SyntaxFactory.Argument((ExpressionSyntax)Nodes.Pop()));
            }

            var rargs = SyntaxFactory.SeparatedList<ArgumentSyntax>();

            for (int i = args.Count - 1; i >= 0; i--)
            {
                rargs = rargs.Add(args[i]);
            }

            Nodes.Push(SyntaxFactory.ArgumentList(rargs));
        }


        public void Visit(SelectNode node)
        {
            string scoreTable = _scope[MetaAttributes.SelectIntoVariableName];

            var variableNameKeyword = SyntaxFactory.Identifier(SyntaxTriviaList.Empty, "select", SyntaxTriviaList.Create(SyntaxHelper.WhiteSpace));
            var syntaxList = new ExpressionSyntax[node.Fields.Length];

            for (int i = 0; i < node.Fields.Length; i++)
            {
                syntaxList[node.Fields.Length - 1 - i] = (ExpressionSyntax)Nodes.Pop();
            }

            var array = SyntaxHelper.CreateArrayOfObjects(syntaxList.ToArray());
            var equalsClause = SyntaxFactory.EqualsValueClause(SyntaxFactory.Token(SyntaxKind.EqualsToken).WithTrailingTrivia(SyntaxHelper.WhiteSpace), array);

            var variableDecl = SyntaxFactory.VariableDeclarator(variableNameKeyword, null, equalsClause);
            var list = SyntaxFactory.SeparatedList(new List<VariableDeclaratorSyntax>()
            {
                variableDecl
            });

            var variableDeclaration =
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var").WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                    list);

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
                                    new[]
                                    {
                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(variableNameKeyword.Text))
                                    })
                            ),
                            SyntaxFactory.InitializerExpression(SyntaxKind.ComplexElementInitializerExpression))
                    )
                });

            var a1 = SyntaxFactory.LocalDeclarationStatement(variableDeclaration).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
            var a2 = SyntaxFactory.ExpressionStatement(invocation).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            _selectBlock = SyntaxFactory.Block(a1, a2);
        }

        public void Visit(GroupSelectNode node)
        {
        }

        public void Visit(WhereNode node)
        {
            var ifStatement = Generator.IfStatement(Generator.LogicalNotExpression(Nodes.Pop()),
                new SyntaxNode[] {SyntaxFactory.ContinueStatement()}).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            Nodes.Push(ifStatement);
        }

        public void Visit(GroupByNode node)
        {
            _hasGroupBy = true;
 
            var args = new SyntaxNode[node.Fields.Length];

            SyntaxNode having = null;
            if (node.Having != null)
                having = Nodes.Pop();

            var syntaxList = new ExpressionSyntax[node.Fields.Length];

            for (int i = 0, j = node.Fields.Length - 1; i < node.Fields.Length; i++, j--)
            {
                args[j] = Nodes.Pop();
            }

            var keysElements = new List<ObjectCreationExpressionSyntax>();

            for (int i = 0; i < args.Length; i++)
            {
                syntaxList[i] =
                    SyntaxHelper.CreateArrayOfObjects(args.Take(i + 1).Cast<ExpressionSyntax>().ToArray());

                var currentKey = new ArgumentSyntax[i + 1];
                for (int j = i; j >= 0; j--)
                {
                    currentKey[j] = SyntaxFactory.Argument((ExpressionSyntax)args[j]);
                }

                keysElements.Add(
                    SyntaxHelper.CreateObjectOf(
                        nameof(GroupKey),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(currentKey))));
            }

            _groupValues = SyntaxHelper.CreateAssignment("values", SyntaxHelper.CreateArrayOf(nameof(Object), syntaxList, 2));
            _groupKeys = SyntaxHelper.CreateAssignment("keys", SyntaxHelper.CreateArrayOfObjects(nameof(GroupKey), keysElements.Cast<ExpressionSyntax>().ToArray()));
            _groupHaving = having;


            var groupFields = _scope.ScopeSymbolTable.GetSymbol<FieldsNamesSymbol>("groupFields");

            StringBuilder fieldNames = new StringBuilder();
            string fieldName = string.Empty;
            fieldNames.Append("var groupFieldsNames = new string[][]{");
            for (int i = 0; i < groupFields.Names.Length - 1; i++)
            {
                fieldName = $"new string[]{{{groupFields.Names.Where((f, idx) => idx <= i).Select(f => $"\"{f}\"").Aggregate((a, b) => a + "," + b)}}}";
                fieldNames.Append(fieldName);
                fieldNames.Append(',');
            }

            fieldName = $"new string[]{{{groupFields.Names.Select(f => $"\"{f}\"").Aggregate((a, b) => a + "," + b)}}}";
            fieldNames.Append(fieldName);
            fieldNames.Append("};");

            Statements.Add(SyntaxFactory.ParseStatement(fieldNames.ToString()));

            AddNamespace(typeof(GroupKey).Namespace);
        }

        public void Visit(HavingNode node)
        {
            Nodes.Push(Generator.IfStatement(Generator.LogicalNotExpression(Nodes.Pop()),
                new SyntaxNode[] { SyntaxFactory.ContinueStatement() }).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed));
        }

        public void Visit(SkipNode node)
        {
            var skip = SyntaxFactory.LocalDeclarationStatement(
                SyntaxHelper.CreateAssignment("skipAmount", (ExpressionSyntax)Generator.LiteralExpression(1))).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            var ifStatement = Generator.IfStatement(
                Generator.LessThanOrEqualExpression(
                    SyntaxFactory.IdentifierName("skipAmount"),
                    Generator.LiteralExpression(node.Value)),
                new SyntaxNode[]
                {
                    SyntaxFactory.PostfixUnaryExpression(
                        SyntaxKind.PostIncrementExpression, 
                        SyntaxFactory.IdentifierName("skipAmount")),
                    SyntaxFactory.ContinueStatement()
                });

            Statements.Add(skip);

            Nodes.Push(ifStatement);
        }

        public void Visit(TakeNode node)
        {
            var take = SyntaxFactory.LocalDeclarationStatement(
                SyntaxHelper.CreateAssignment("tookAmount", (ExpressionSyntax)Generator.LiteralExpression(0)));

            var ifStatement = 
                    (StatementSyntax)Generator.IfStatement(
                        Generator.ValueEqualsExpression(
                            SyntaxFactory.IdentifierName("tookAmount"),
                            Generator.LiteralExpression(node.Value)),
                        new SyntaxNode[]
                        {
                            SyntaxFactory.BreakStatement()
                        });

            var incTookAmount = 
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.PostfixUnaryExpression(
                    SyntaxKind.PostIncrementExpression,
                    SyntaxFactory.IdentifierName("tookAmount")));

            Statements.Add(take);

            Nodes.Push(SyntaxFactory.Block(new[]
            {
                ifStatement,
                incTookAmount
            }));
        }

        private BlockSyntax _joinBlock;
        private BlockSyntax _emptyBlock;
        private BlockSyntax _selectBlock;

        public void Visit(JoinInMemoryWithSourceTableFromNode node)
        {
            var ifStatement = Generator.IfStatement(Generator.LogicalNotExpression(Nodes.Pop()),
                new SyntaxNode[] { SyntaxFactory.ContinueStatement() }).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            _emptyBlock = SyntaxFactory.Block();

            var foreaches = SyntaxFactory.ForEachStatement(SyntaxFactory.IdentifierName("var"), SyntaxFactory.Identifier($"{node.InMemoryTableAlias}Row"),
                    SyntaxFactory.IdentifierName($"EvaluationHelper.ConvertTableToSource({node.InMemoryTableAlias}TransitionTable).Rows"),
                    SyntaxFactory.Block(SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ForEachStatement(
                        SyntaxFactory.IdentifierName("var"), SyntaxFactory.Identifier($"{node.SourceTable.Alias}Row"),
                        SyntaxFactory.IdentifierName($"{node.SourceTable.Alias}Rows.Rows"),
                        SyntaxFactory.Block((StatementSyntax)ifStatement, _emptyBlock)))))
                .NormalizeWhitespace();

            _joinBlock = SyntaxFactory.Block(foreaches);
        }

        public void Visit(SchemaFromNode node)
        {
            var createdSchema = SyntaxHelper.CreateAssignmentByMethodCall(
                node.Alias, 
                "provider", 
                nameof(ISchemaProvider.GetSchema),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                    SyntaxFactory.SeparatedList(new []
                    {
                        SyntaxHelper.StringLiteralArgument(node.Schema)
                    }),
                    SyntaxFactory.Token(SyntaxKind.CloseParenToken)
                )
            );

            var args = node.Parameters.Select(f => SyntaxHelper.StringLiteral(f.Escape())).Cast<ExpressionSyntax>();

            var createdSchemaRows = SyntaxHelper.CreateAssignmentByMethodCall(
                $"{node.Alias}Rows",
                node.Alias,
                nameof(ISchema.GetRowSource),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new []
                    {
                        SyntaxHelper.StringLiteralArgument(node.Method),
                        SyntaxFactory.Argument(
                            SyntaxHelper.CreateArrayOf(
                                nameof(String),
                                args.ToArray()))
                    })
                ));

            Statements.Add(SyntaxFactory.LocalDeclarationStatement(createdSchema));
            Statements.Add(SyntaxFactory.LocalDeclarationStatement(createdSchemaRows));
        }

        public void Visit(JoinSourcesTableFromNode node)
        {
            var ifStatement = Generator.IfStatement(Generator.LogicalNotExpression(Nodes.Pop()),
                new SyntaxNode[] { SyntaxFactory.ContinueStatement() }).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            _emptyBlock = SyntaxFactory.Block();

            var foreaches = SyntaxFactory.ForEachStatement(SyntaxFactory.IdentifierName("var"), SyntaxFactory.Identifier($"{node.First.Alias}Row"),
                    SyntaxFactory.IdentifierName($"{node.First.Alias}Rows.Rows"),
                    SyntaxFactory.Block(SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ForEachStatement(
                        SyntaxFactory.IdentifierName("var"), SyntaxFactory.Identifier($"{node.Second.Alias}Row"),
                        SyntaxFactory.IdentifierName($"{node.Second.Alias}Rows.Rows"),
                        SyntaxFactory.Block((StatementSyntax)ifStatement, _emptyBlock)))))
                .NormalizeWhitespace();

            _joinBlock = SyntaxFactory.Block(foreaches);
        }

        public void Visit(InMemoryTableFromNode node)
        {
        }

        public void Visit(JoinFromNode node)
        {
            _hasJoin = true;
        }

        public void Visit(ExpressionFromNode node)
        {
            Nodes.Push(SyntaxFactory.Block());
        }

        private void AddNamespace(string columnTypeNamespace)
        {
            if(!_namespaces.Contains(columnTypeNamespace))
                _namespaces.Add(columnTypeNamespace);
        }

        private void AddNamespace(Type[] types)
        {
            foreach(var type in types)
                AddNamespace(type.Namespace);
        }

        private void AddReference(params Type[] types)
        {
            foreach (var type in types)
            {
                if (!_loadedAssemblies.Contains(type.Assembly.Location))
                {
                    _loadedAssemblies.Add(type.Assembly.Location);
                    Compilation =
                        Compilation.AddReferences(MetadataReference.CreateFromFile(type.Assembly.Location));
                }
            }
        }

        private void AddReference(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                if (!_loadedAssemblies.Contains(assembly.Location))
                {
                    _loadedAssemblies.Add(assembly.Location);
                    Compilation =
                        Compilation.AddReferences(MetadataReference.CreateFromFile(assembly.Location));
                }
            }
        }

        public void Visit(CreateTableNode node)
        {
            var cols = new List<ExpressionSyntax>();

            foreach (var field in node.Fields)
            {
                var types = EvaluationHelper.GetNestedTypes(field.ReturnType);
                AddNamespace(types);
                AddReference(types);

                cols.Add(
                    SyntaxHelper.CreateObjectOf(
                        nameof(Column),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(new[]
                            {
                                SyntaxHelper.StringLiteralArgument(field.FieldName),
                                SyntaxHelper.TypeLiteralArgument(
                                    EvaluationHelper.GetCastableType(field.ReturnType)),
                                SyntaxHelper.IntLiteralArgument(field.FieldOrder)
                            }))));
            }

            var createObject = SyntaxHelper.CreateAssignment(
                _scope[MetaAttributes.CreateTableVariableName],
                SyntaxHelper.CreateObjectOf(
                    nameof(Table),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            new []
                            {
                                SyntaxFactory.Argument((ExpressionSyntax)Generator.LiteralExpression(node.Name)),
                                SyntaxFactory.Argument(
                                    SyntaxHelper.CreateArrayOf(
                                        nameof(Column),
                                        cols.ToArray()))
                            }))));

            Statements.Add(SyntaxFactory.LocalDeclarationStatement(createObject));
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
            var where = Nodes.Pop() as StatementSyntax;

            var block = (BlockSyntax)Nodes.Pop();
            block = block.AddStatements(where);

            if (skip != null)
                block = block.AddStatements(skip);

            if (take != null)
                block = block.AddStatements(take.Statements.ToArray());

            block = block.AddStatements(select.Statements.ToArray());
            block = block.AddStatements(GenerateStatsUpdateStatements());
            var fullBlock = SyntaxFactory.Block();

            fullBlock = fullBlock.AddStatements(SyntaxHelper.Foreach("score", _scope[MetaAttributes.SourceName], block));
            fullBlock = fullBlock.AddStatements((StatementSyntax) Generator.ReturnStatement(SyntaxFactory.IdentifierName(detailedQuery.ReturnVariableName)));

            Statements.AddRange(fullBlock.Statements);
        }

        private StatementSyntax GenerateStatsUpdateStatements()
        {
            return SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                SyntaxKind.AddAssignmentExpression,
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("stats"), SyntaxFactory.IdentifierName("RowNumber")),
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1))));
        }

        public void Visit(InternalQueryNode node)
        {
            var select = _selectBlock;
            var where = Nodes.Pop() as StatementSyntax;

            var block = (BlockSyntax)Nodes.Pop();

            if (node.GroupBy != null)
            {
                Statements.Add(SyntaxFactory.ParseStatement("var rootGroup = new Group(null, new string[0], new string[0]);").WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturnLineFeed)));
                Statements.Add(SyntaxFactory.ParseStatement("var usedGroups = new HashSet<Group>();").WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturnLineFeed)));
                Statements.Add(SyntaxFactory.ParseStatement("var groups = new Dictionary<GroupKey, Group>();").WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturnLineFeed)));
                
                block = block.AddStatements(where);
                block = block.AddStatements(SyntaxFactory.LocalDeclarationStatement(_groupKeys));
                block = block.AddStatements(SyntaxFactory.LocalDeclarationStatement(_groupValues));

                block = block.AddStatements(SyntaxFactory.ParseStatement("var parent = rootGroup;").WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturnLineFeed)));
                block = block.AddStatements(SyntaxFactory.ParseStatement("Group group = null;").WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturnLineFeed)));
                
                block = block.AddStatements(GroupForStatement());

                if (node.Refresh.Nodes.Length > 0)
                    block = block.AddStatements(((BlockSyntax) Nodes.Pop()).Statements.ToArray());

                if(node.GroupBy.Having != null)
                    block = block.AddStatements((StatementSyntax) _groupHaving);

                var indexToColumnMapCode = new InitializerExpressionSyntax[node.Select.Fields.Length];

                for (int i = 0, j = node.Select.Fields.Length - 1; i < node.Select.Fields.Length; i++, --j)
                {
                    indexToColumnMapCode[i] =
                        SyntaxFactory.InitializerExpression(
                            SyntaxKind.ComplexElementInitializerExpression,
                            SyntaxFactory.SeparatedList<ExpressionSyntax>()
                                .Add((LiteralExpressionSyntax)Generator.LiteralExpression(j))
                                .Add((LiteralExpressionSyntax)Generator.LiteralExpression(node.Select.Fields[i].FieldName)));
                }

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

                block = block.AddStatements(AddGroupStatement(node.From.Alias.ToGroupingTable(), indexToValueDictVariableName));
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
        }

        private BlockSyntax GroupByForeach(BlockSyntax foreachInstructions, string variableName, string tableVariable)
        {
            return SyntaxFactory.Block(SyntaxFactory.ForEachStatement(SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.Identifier(variableName), SyntaxFactory.IdentifierName(tableVariable),
                foreachInstructions).NormalizeWhitespace());
        }

        private StatementSyntax AddGroupStatement(string scoreTable, string indexToColumnVariableName)
        {
            return SyntaxFactory.IfStatement(
                    SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression,
                        SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName("usedGroups"), SyntaxFactory.IdentifierName("Contains")))
                            .WithArgumentList(
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("group")))))),
                    SyntaxFactory.Block(
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(scoreTable),
                                    SyntaxFactory.IdentifierName("Add")))
                                .WithArgumentList(
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(
                                                SyntaxHelper.CreateObjectOf(nameof(GroupRow),
                                                    SyntaxFactory.ArgumentList(
                                                        SyntaxFactory.SeparatedList(
                                                            new[]
                                                            {
                                                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("group")),
                                                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(indexToColumnVariableName))
                                                            })))))))),
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("usedGroups"), SyntaxFactory.IdentifierName("Add")))
                                .WithArgumentList(
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("group"))))))))
                .NormalizeWhitespace();
        }

        private StatementSyntax GroupForStatement()
        {
            return
                SyntaxFactory.ForStatement(SyntaxFactory.Block(
                        SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var")).WithVariables(
                            SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("key"))
                                .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName("keys"))
                                    .WithArgumentList(SyntaxFactory.BracketedArgumentList(
                                        SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("i")))))))))),
                        SyntaxFactory.IfStatement(
                                SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("groups"), SyntaxFactory.IdentifierName("ContainsKey"))).WithArgumentList(
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key"))))),
                                SyntaxFactory.Block(SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName("group"),
                                    SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName("groups")).WithArgumentList(
                                        SyntaxFactory.BracketedArgumentList(
                                            SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key"))))))))))
                            .WithElse(SyntaxFactory.ElseClause(SyntaxFactory.Block(
                                SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName("group"),
                                    SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName("Group")).WithArgumentList(SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SeparatedList<ArgumentSyntax>(new SyntaxNodeOrToken[]
                                        {
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("parent")), SyntaxFactory.Token(SyntaxKind.CommaToken),
                                            SyntaxFactory.Argument(SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName("groupFieldsNames"))
                                                .WithArgumentList(SyntaxFactory.BracketedArgumentList(
                                                    SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("i")))))),
                                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                                            SyntaxFactory.Argument(SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName("values"))
                                                .WithArgumentList(SyntaxFactory.BracketedArgumentList(
                                                    SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("i"))))))
                                        }))))),
                                SyntaxFactory.ExpressionStatement(
                                    SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("groups"), SyntaxFactory.IdentifierName("Add"))).WithArgumentList(
                                        SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(new SyntaxNodeOrToken[]
                                        {
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("key")), SyntaxFactory.Token(SyntaxKind.CommaToken),
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("group"))
                                        }))))))),
                        SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName("parent"), SyntaxFactory.IdentifierName("group")))))
                    .WithDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword))).WithVariables(
                        SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("i"))
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(0)))))))
                    .WithCondition(SyntaxFactory.BinaryExpression(SyntaxKind.LessThanExpression, SyntaxFactory.IdentifierName("i"),
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("keys"),
                            SyntaxFactory.IdentifierName("Length"))))
                    .WithIncrementors(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                        SyntaxFactory.PrefixUnaryExpression(SyntaxKind.PreIncrementExpression, SyntaxFactory.IdentifierName("i"))))
                    .NormalizeWhitespace();
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
                    SyntaxFactory.SeparatedList(new ParameterSyntax[0])),
                new SyntaxList<TypeParameterConstraintClauseSyntax>(),
                SyntaxFactory.Block(SyntaxFactory.ParseStatement("return RunQuery(Provider);")),
                null);

            var param = SyntaxFactory.PropertyDeclaration(
                new SyntaxList<AttributeListSyntax>(),
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
                SyntaxFactory.IdentifierName(nameof(ISchemaProvider)).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                null,
                SyntaxFactory.Identifier(nameof(IRunnable.Provider)),
                SyntaxFactory.AccessorList(
                    SyntaxFactory.List<AccessorDeclarationSyntax>()
                        .Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
                        .Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))), 
                null, 
                null);

            _methods.Add(method);
            _methods.Add(param);

            var classDeclaration = Generator.ClassDeclaration("CompiledQuery", new string[0], Accessibility.Public, DeclarationModifiers.None,
                null, new SyntaxNode[]{ SyntaxFactory.IdentifierName(nameof(IRunnable)) }, _methods);

            var ns = SyntaxFactory.NamespaceDeclaration(
                SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Query.Compiled")),
                SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
                SyntaxFactory.List(
                    _namespaces.Select(
                        n => SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(n)))),
                SyntaxFactory.List<MemberDeclarationSyntax>(new []{ (ClassDeclarationSyntax)classDeclaration }));

            var compilationUnit = SyntaxFactory.CompilationUnit(
                SyntaxFactory.List<ExternAliasDirectiveSyntax>(),
                SyntaxFactory.List<UsingDirectiveSyntax>(),
                SyntaxFactory.List<AttributeListSyntax>(),
                SyntaxFactory.List<MemberDeclarationSyntax>(new []{ ns }));

            OptionSet options = Workspace.Options;
            options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInMethods, true);
            options = options.WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInProperties, true);

            var project = Workspace.AddProject("xyz", LanguageNames.CSharp);
            var document = project.AddDocument("AutoGenerated.cs", compilationUnit);

            SyntaxNode formatted = Formatter.Format(compilationUnit, Workspace);

            var builder = new StringBuilder();
            using(var writer = new StringWriter(builder))
            {
                formatted.WriteTo(writer);
            }

            var testPath = Path.Combine("E:\\Temp",
                $"{Guid.NewGuid().ToString()}.cs");

            using (var file = new StreamWriter(File.OpenWrite(testPath)))
            {
                file.Write(builder.ToString());
            }

            Debug.WriteLine("START");
            foreach(var item in Compilation.ExternalReferences)
                Debug.WriteLine(item.Display);
            Debug.WriteLine("STOP");

            Compilation = Compilation.AddSyntaxTrees(new[]
            {
                SyntaxFactory.ParseSyntaxTree(builder.ToString(), new CSharpParseOptions(LanguageVersion.Latest), testPath, Encoding.Unicode)
            });
        }

        public void Visit(SingleSetNode node)
        {
        }

        public void Visit(UnionNode node)
        {
        }

        public void Visit(UnionAllNode node)
        {
        }

        public void Visit(ExceptNode node)
        {
        }

        public void Visit(RefreshNode node)
        {
            if (node.Nodes.Length == 0)
                return;

            var block = SyntaxFactory.Block();
            for (int i = 0, k = node.Nodes.Length - 1; i < node.Nodes.Length; i++, k--)
            {
                block = block.AddStatements(
                    SyntaxFactory.ExpressionStatement((ExpressionSyntax)Nodes.Pop()));
            }

            Nodes.Push(block);
        }

        public void Visit(IntersectNode node)
        {
        }

        public void Visit(PutTrueNode node)
        {
            Nodes.Push(Generator.ValueEqualsExpression(Generator.LiteralExpression(1), Generator.LiteralExpression(1)));
        }

        public void Visit(MultiStatementNode node)
        {
            var method = SyntaxFactory.MethodDeclaration(
                new SyntaxList<AttributeListSyntax>(),
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
                SyntaxFactory.IdentifierName(nameof(Table)).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                null,
                SyntaxFactory.Identifier("RunQuery"),
                null,
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Parameter(
                            new SyntaxList<AttributeListSyntax>(),
                            SyntaxTokenList.Create(
                                new SyntaxToken()),
                            SyntaxFactory.IdentifierName(nameof(ISchemaProvider)).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                            SyntaxFactory.Identifier("provider"), null)
                    })),
                new SyntaxList<TypeParameterConstraintClauseSyntax>(),
                SyntaxFactory.Block(Statements),
                null);

            _methods.Add(method);
            Statements.Clear();
        }

        public void Visit(CteExpressionNode node)
        {
        }

        public void Visit(CteInnerExpressionNode node)
        {
        }

        public void Visit(JoinsNode node)
        {
            _hasJoin = true;
        }

        public void Visit(JoinNode node)
        {
            _hasJoin = true;
        }

        public void QueryBegins()
        {
        }

        public void QueryEnds()
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

        public void SetCodePattern(StringBuilder code)
        {
        }

        public void SetJoinsAmount(int amount)
        {
        }

        public void SetMethodAccessType(MethodAccessType type)
        {
            _type = type;
        }

        public void SelectBegins()
        {
        }

        public void SelectEnds()
        {
        }

        public void TurnOnAggregateMethodsToColumnAcceess()
        {
            _changeMethodAccessToColumnAccess = true;
        }

        public void TurnOffAggregateMethodsToColumnAcceess()
        {
            _changeMethodAccessToColumnAccess = false;
        }
    }
}
