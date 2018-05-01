using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.Helpers;
using Environment = Musoq.Plugins.Environment;

namespace Musoq.Evaluator.Visitors
{
    public enum MethodAccessType
    {
        SelectTable,
        JoinedTable,
        JoinedSelectTable
    }
    public class ToCSharpRewriteTreeVisitor : IScopeAwareExpressionVisitor
    {
        public AdhocWorkspace Workspace { get; }

        public SyntaxGenerator Generator { get; }

        public CSharpCompilation Compilation { get; private set; }

        private Stack<SyntaxNode> Nodes { get; }

        private readonly List<MethodDeclarationSyntax> _methods = new List<MethodDeclarationSyntax>();
        private bool _hasGroupBy = false;
        private bool _hasJoin = false;

        public bool HasGroupByOrJoin => _hasGroupBy || _hasJoin;

        private readonly List<string> _namespaces = new List<string>();
        private Scope _scope;

        private StringBuilder Script => _scope.Script;

        private readonly StringBuilder _declStatements = new StringBuilder();
        private int _names;
        private int _sources;
        private int _joins;
        private int _joinsAmount;
        private MethodAccessType _type;
        private string _queryAlias;
        private string _transformedSourceTable;
        private string _scoreTable;
        private string _scoreTableName;
        private string _scoreColumns;

        private List<string> _preDeclarations = new List<string>();

        public ToCSharpRewriteTreeVisitor(IEnumerable<Assembly> assemblies)
        {
            Workspace = new AdhocWorkspace();
            Generator = SyntaxGenerator.GetGenerator(Workspace, LanguageNames.CSharp);
            Generator.NamespaceImportDeclaration("System");
            Nodes = new Stack<SyntaxNode>();

            var objLocation = typeof(object).GetTypeInfo().Assembly.Location;
            var path = new FileInfo(objLocation);

            var mscorlib = Path.Combine(path.Directory.FullName, "mscorlib.dll");
            var system = Path.Combine(path.Directory.FullName, "System.dll");
            var systemCore = Path.Combine(path.Directory.FullName, "System.Core.dll");
            var runtime = Path.Combine(path.Directory.FullName, "System.Runtime.dll");

            Compilation = CSharpCompilation.Create("InMemoryAssembly.dll");
            Compilation = Compilation
                .AddReferences(MetadataReference.CreateFromFile(mscorlib))
                .AddReferences(MetadataReference.CreateFromFile(system))
                .AddReferences(MetadataReference.CreateFromFile(systemCore))
                .AddReferences(MetadataReference.CreateFromFile(runtime))
                .AddReferences(MetadataReference.CreateFromFile(typeof(ISchema).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(LibraryBase).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(Table).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(SyntaxFactory).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile("C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App\\2.0.0\\netstandard.dll"))
                .AddReferences(assemblies.Select(a => MetadataReference.CreateFromFile(a.Location)));

            Compilation = Compilation.WithOptions(
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Debug,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));

            _namespaces.Add("System");
            _namespaces.Add("Musoq.Plugins");
            _namespaces.Add("Musoq.Schema");
            _namespaces.Add("Musoq.Evaluator.Tables");
            _namespaces.Add("Musoq.Evaluator.Helpers");
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
            var identifier = Generator.IdentifierName("lib");
            Generator.InvocationExpression(identifier);
        }

        public void Visit(FieldNode node)
        {
            if(!_namespaces.Contains(node.ReturnType.Namespace))
                _namespaces.Add(node.ReturnType.Namespace);
            var type = Compilation.GetTypeByMetadataName(node.ReturnType.FullName);
            var castedExpression = Generator.CastExpression(type, Nodes.Pop());
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
        }

        public void Visit(AccessMethodNode node)
        {
            var args = new List<SyntaxNode>();

            var parameters = node.Method.GetParameters().GetParametersWithAttribute<InjectTypeAttribute>();

            foreach (var parameterInfo in parameters)
            {
                switch (parameterInfo.GetCustomAttribute<InjectTypeAttribute>())
                {
                    case InjectSourceAttribute injectSource:
                        args.Add(SyntaxHelper.CreateMethodInvocation("row", "Context"));
                        break;
                    case InjectGroupAttribute injectGroup:
                        break;
                    case InjectGroupAccessName injectGroupAccessName:
                        break;
                    case InjectQueryStats injectQueryStats:
                        break;
                }
            }

            for (int i = node.ArgsCount - 1; i >= 0; i--)
            {
                args.Add(Nodes.Pop());
            }

            Nodes.Push(Generator.InvocationExpression(Generator.MemberAccessExpression(Generator.IdentifierName("lib"),
                Generator.IdentifierName(node.Name)), args));
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
            switch (_type)
            {
                case MethodAccessType.JoinedTable:
                    sNode = Generator.ElementAccessExpression(Generator.IdentifierName($"{node.Alias}Row"),
                        SyntaxHelper.StringLiteralArgument(node.Name));
                    break;
                case MethodAccessType.JoinedSelectTable:
                    sNode = Generator.ElementAccessExpression(Generator.IdentifierName("score"), 
                        SyntaxHelper.StringLiteralArgument($"{node.Alias}.{node.Name}"));
                    break;
                case MethodAccessType.SelectTable:
                    sNode = Generator.ElementAccessExpression(Generator.IdentifierName("score"),
                        SyntaxHelper.StringLiteralArgument(node.Name));
                    break;
                default:
                    throw new NotSupportedException();
            }

            var type = Compilation.GetTypeByMetadataName(node.ReturnType.FullName);
            sNode = Generator.CastExpression(type, sNode);

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
        }

        public void Visit(AccessObjectKeyNode node)
        {
        }

        public void Visit(PropertyValueNode node)
        {
        }

        public void Visit(DotNode node)
        {
        }

        public void Visit(AccessCallChainNode node)
        {
        }

        public void Visit(ArgsListNode node)
        {
            var args = new SeparatedSyntaxList<SyntaxNode>();

            for (int i = 0; i < node.Args.Length; i++)
            {
                args.Add(Nodes.Pop());
            }

            var rargs = new SeparatedSyntaxList<SyntaxNode>();

            for (int i = args.Count - 1; i >= 0; i--)
            {
                rargs.Add(args[i]);
            }

            Nodes.Push(SyntaxFactory.ArgumentList(rargs));
        }


        public void Visit(SelectNode node)
        {
            var scoreTable = $"{_queryAlias}ScoreTable";
            var variableNameKeyword = SyntaxFactory.Identifier(SyntaxTriviaList.Empty, "select", SyntaxTriviaList.Create(SyntaxHelper.WhiteSpace));
            var syntaxList = new ExpressionSyntax[node.Fields.Length];
            var cols = new List<ExpressionSyntax>();

            for (int i = 0; i < node.Fields.Length; i++)
            {
                syntaxList[node.Fields.Length - 1 - i] = (ExpressionSyntax) Nodes.Pop();
                cols.Add(
                    SyntaxHelper.CreaateObjectOf(
                        nameof(Column),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(new[]
                            {
                                SyntaxHelper.StringLiteralArgument(node.Fields[i].FieldName),
                                SyntaxHelper.TypeLiteralArgument(node.Fields[i].ReturnType.Name),
                                SyntaxHelper.IntLiteralArgument(node.Fields[i].FieldOrder)
                            }))));
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

            var tableCols = SyntaxHelper.CreateArrayOf(nameof(Column), cols.ToArray());
            var a1 = SyntaxFactory.LocalDeclarationStatement(variableDeclaration);
            var a2 = SyntaxFactory.ExpressionStatement(invocation);
            var code = new StringBuilder();
            code.Append(a1.ToFullString());
            code.Append(a2.ToFullString());

            _scoreColumns = tableCols.ToFullString();
            Script.Replace("{score_table_name}", $"\"{_queryAlias}\"");
            Script.Replace("{score_columns}", tableCols.ToFullString());
            Script.Replace("{score_table}", scoreTable);

            if (HasGroupByOrJoin)
            {
                Script.Replace("{source_rows}", $"{nameof(EvaluationHelper)}.{nameof(EvaluationHelper.ConvertTableToSource)}({_transformedSourceTable})");
            }
            else
            {
                Script.Replace("{source_rows}", $"{_queryAlias}Rows");
            }

            Script.Replace("{select_statements}", code.ToString());
        }

        public void Visit(WhereNode node)
        {
            var ifStatement = Generator.IfStatement(Generator.LogicalNotExpression(Nodes.Pop()),
                new SyntaxNode[] {SyntaxFactory.ContinueStatement()});

            Script.Replace("{where_statement}", ifStatement.ToFullString());
        }

        public void Visit(GroupByNode node)
        {
            _hasGroupBy = true;
        }

        public void Visit(HavingNode node)
        {
            Nodes.Push(Generator.IfStatement(Generator.LogicalNotExpression(Nodes.Pop()),
                new SyntaxNode[] { SyntaxFactory.ContinueStatement() }));
        }

        public void Visit(SkipNode node)
        {
        }

        public void Visit(TakeNode node)
        {
        }

        public void Visit(ExistingTableFromNode node)
        {
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

            var args = node.Parameters.Select(SyntaxHelper.StringLiteral).Cast<ExpressionSyntax>();

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

            _declStatements.Append(SyntaxFactory.LocalDeclarationStatement(createdSchema).ToFullString());
            _declStatements.Append(SyntaxFactory.LocalDeclarationStatement(createdSchemaRows));

            Script.Replace($"{{source{_sources++}}}", SyntaxFactory.IdentifierName($"{node.Alias}Rows.Rows").ToFullString());
            Script.Replace($"{{name{_names++}}}", SyntaxFactory.IdentifierName($"{node.Alias}Row").ToFullString());
        }

        public void Visit(NestedQueryFromNode node)
        {
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
            _transformedSourceTable = "transformedTable";
            for (int i = _joinsAmount - 2; i >= 0;)
            {
                Script.Replace($"{{join_condition{i--}}}", Nodes.Pop().ToFullString());
            }

            var tableSymbol = _scope.ScopeSymbolTable.GetSymbol<TableSymbol>(node.Expression.Alias);

            var args = new List<ExpressionSyntax>();
            var cols = new List<ExpressionSyntax>();

            int newColumnIndex = 0;
            foreach (var table in tableSymbol.CompoundTables)
            {
                var columns = tableSymbol.GetColumn(table);
                ISchemaColumn column = null;
                for (var index = 0; index < columns.Length - 1; index++)
                {
                    column = columns[index];
                    args.Add(
                        SyntaxHelper.CreateElementAccess(
                            $"{table}Row", 
                            new[]
                            {
                                SyntaxFactory.Argument((ExpressionSyntax)Generator.LiteralExpression(column.ColumnName)),
                            }));

                    cols.Add(
                        SyntaxHelper.CreaateObjectOf(
                            nameof(Column),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList(new[]
                                {
                                    SyntaxHelper.StringLiteralArgument($"{table}.{column.ColumnName}"),
                                    SyntaxHelper.TypeLiteralArgument(column.ColumnType.Name),
                                    SyntaxHelper.IntLiteralArgument(newColumnIndex)
                                }))));

                    AddNamespace(column.ColumnType.Namespace);

                    newColumnIndex += 1;
                }

                column = columns[columns.Length - 1];

                args.Add(
                    SyntaxHelper.CreateElementAccess(
                        $"{table}Row",
                        new[]
                        {
                            SyntaxFactory.Argument((ExpressionSyntax)Generator.LiteralExpression(column.ColumnName))
                        }));

                cols.Add(
                    SyntaxHelper.CreaateObjectOf(
                        nameof(Column),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(new[]
                            {
                                SyntaxHelper.StringLiteralArgument($"{table}.{column.ColumnName}"),
                                SyntaxHelper.TypeLiteralArgument(column.ColumnType.Name),
                                SyntaxHelper.IntLiteralArgument(newColumnIndex)
                            }))));

                AddNamespace(column.ColumnType.Namespace);

                newColumnIndex += 1;
            }

            var select = SyntaxHelper.CreateAssignment("columns", SyntaxHelper.CreateArrayOfObjects(args.ToArray()));

            var methodInvocation = SyntaxHelper.CreateMethodInvocation(
                _transformedSourceTable, 
                nameof(Table.Add), 
                new[]
                {
                    SyntaxHelper.CreaateObjectOf(
                        nameof(ObjectsRow), 
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(
                                new List<ArgumentSyntax>()
                                {
                                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("columns"))
                                })))
                });

            var createColumns = SyntaxHelper.CreateArrayOf(nameof(Column), cols.ToArray());

            var code = new StringBuilder();
            code.Append(SyntaxFactory.LocalDeclarationStatement(select).ToFullString());
            code.Append(SyntaxFactory.ExpressionStatement(methodInvocation).ToFullString());

            Script.Replace("{transformed_select_statements}", code.ToString());
            Script.Replace("{transformed_table_name}", $"\"{node.Alias}\"");
            Script.Replace("{transformed_table_columns}", createColumns.ToFullString());
            Script.Replace("{transformed_source_table}", _transformedSourceTable);

            _scoreTable = $"{node.Alias}Table";
            _scoreTableName = node.Alias;
        }

        private void AddNamespace(string columnTypeNamespace)
        {
            if(!_namespaces.Contains(columnTypeNamespace))
                _namespaces.Add(columnTypeNamespace);
        }

        public void Visit(CreateTableNode node)
        {
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

        public void Visit(IntoGroupNode node)
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
            Script.Replace("{decl_statement}", _declStatements.ToString());

            var method = SyntaxFactory.MethodDeclaration(
                new SyntaxList<AttributeListSyntax>(),
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
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
                SyntaxFactory.Block(SyntaxFactory.ParseStatement(Script.ToString())),
                null);

            _methods.Add(method);
        }

        public void Visit(InternalQueryNode node)
        {
        }

        public void Visit(RootNode node)
        {
            var classDeclaration = Generator.ClassDeclaration("CompiledQuery", new string[0], Accessibility.Public, DeclarationModifiers.None,
                null, null, _methods);

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

            var tree = SyntaxFactory.SyntaxTree(Formatter.Format(compilationUnit, Workspace), encoding: Encoding.Unicode, path: Path.GetTempFileName());

            var text = tree.GetText();
            var test = text.ToString();

            Compilation = Compilation.AddSyntaxTrees(new[]
            {
                SyntaxFactory.ParseSyntaxTree(text, new CSharpParseOptions(LanguageVersion.Latest), Path.GetTempFileName())
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
        }

        public void Visit(CteExpressionNode node)
        {
        }

        public void Visit(CteInnerExpressionNode node)
        {
        }

        public void Visit(JoinsNode node)
        {
        }

        public void Visit(JoinNode node)
        {
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
            _joinsAmount = amount;
        }

        public void SetMethodAccessType(MethodAccessType type)
        {
            _type = type;
        }
    }
}
