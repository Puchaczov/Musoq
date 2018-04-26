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
using Microsoft.CodeAnalysis.Host.Mef;
using Musoq.Evaluator.CSharpTemplates;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tables;
using Musoq.Parser.Nodes;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors
{
    public class ToCSharpRewriteTreeVisitor : ISchemaAwareExpressionVisitor
    {
        private class QueryParts
        {
            public SyntaxNode[] From { get; set; }

            public SyntaxNode Where { get; set; }

            public SyntaxNode[] Select { get; set; }
        }

        public AdhocWorkspace Workspace { get; }

        public SyntaxGenerator Generator { get; }

        public CSharpCompilation Compilation { get; private set; }

        private Stack<SyntaxNode> Nodes { get; }

        private QueryParts Parts { get; } = new QueryParts();

        private readonly List<MethodDeclarationSyntax> _methods = new List<MethodDeclarationSyntax>();

        private readonly List<string> _namespaces = new List<string>();

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
            var args = new SyntaxNode[node.ArgsCount];

            for (int i = node.ArgsCount - 1; i >= 0; i--)
            {
                args[i] = Nodes.Pop();
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
            Nodes.Push(Generator.ElementAccessExpression(Generator.IdentifierName("row"), new []
            {
                SyntaxHelper.StringLiteralArgument(node.Name)
            }));
        }

        public void Visit(AllColumnsNode node)
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
            var variableNameKeyword = SyntaxFactory.Identifier(SyntaxTriviaList.Empty, "select", SyntaxTriviaList.Create(SyntaxHelper.WhiteSpace));

            var syntaxList = new SeparatedSyntaxList<ExpressionSyntax>();
            for (int i = 0; i < node.Fields.Length; i++)
            {
                syntaxList = syntaxList.Add((ExpressionSyntax)Nodes.Pop());
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
                "table",
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

            Parts.Select = new SyntaxNode[]
            {
                SyntaxFactory.LocalDeclarationStatement(variableDeclaration),
                SyntaxFactory.ExpressionStatement(invocation)
            };
        }

        public void Visit(WhereNode node)
        {
            Parts.Where = Generator.IfStatement(Generator.LogicalNotExpression(Nodes.Pop()),
                new SyntaxNode[] { SyntaxFactory.ContinueStatement() });
        }

        public void Visit(GroupByNode node)
        {
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

            Parts.From = new SyntaxNode[]
            {
                SyntaxFactory.LocalDeclarationStatement(createdSchema),
                SyntaxFactory.LocalDeclarationStatement(createdSchemaRows),
                SyntaxFactory.IdentifierName($"{node.Alias}Rows.Rows")
            };
        }

        public void Visit(NestedQueryFromNode node)
        {
        }

        public void Visit(CteFromNode node)
        {
        }

        public void Visit(JoinFromNode node)
        {
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
            var foreachStatement = SyntaxFactory.ForEachStatement(
                SyntaxFactory.Token(SyntaxKind.ForEachKeyword),
                SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                SyntaxFactory.IdentifierName("var").WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                SyntaxFactory.Identifier("row").WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                SyntaxFactory.Token(SyntaxKind.InKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                (IdentifierNameSyntax)Parts.From[2],
                SyntaxFactory.Token(SyntaxKind.CloseParenToken),
                SyntaxFactory.Block(new []
                {
                    (StatementSyntax)Parts.Where,
                    (StatementSyntax)Parts.Select[0],
                    (StatementSyntax)Parts.Select[1]
                }));

            var tableColumns = new ExpressionSyntax[node.Select.Fields.Length];
            for (int i = 0; i < tableColumns.Length; i++)
            {
                tableColumns[i] =
                    SyntaxHelper.CreaateObjectOf(
                        nameof(Column),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(new []
                            {
                                SyntaxHelper.StringLiteralArgument(node.Select.Fields[i].FieldName),
                                SyntaxHelper.TypeLiteralArgument(node.Select.Fields[i].ReturnType.Name),
                                SyntaxHelper.IntLiteralArgument(node.Select.Fields[i].FieldOrder)
                            })));
            }

            var createTable = SyntaxHelper.CreateAssignment(
                "table",
                SyntaxHelper.CreaateObjectOf(
                    nameof(Table),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(new []
                        {
                            SyntaxHelper.StringLiteralArgument("test_table_name"),
                            SyntaxHelper.CreateArrayOfArgument(nameof(Column), tableColumns)
                        }))));

            var createTableExpression = SyntaxFactory.LocalDeclarationStatement(createTable);

            var returnStatement = Generator.ReturnStatement(SyntaxFactory.IdentifierName("table").WithLeadingTrivia(SyntaxHelper.WhiteSpace));
            var block = SyntaxFactory.Block(new []
            {
                (StatementSyntax)Parts.From[0],
                (StatementSyntax)Parts.From[1],
                createTableExpression,
                foreachStatement,
                (StatementSyntax)returnStatement
            });

            var method = SyntaxFactory.MethodDeclaration(
                new SyntaxList<AttributeListSyntax>(),
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace)),
                SyntaxFactory.IdentifierName(nameof(Table)).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                null,
                SyntaxFactory.Identifier("RunQuery"),
                null,
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(new []
                    {
                        SyntaxFactory.Parameter(
                            new SyntaxList<AttributeListSyntax>(), 
                            SyntaxTokenList.Create(
                                new SyntaxToken()), 
                            SyntaxFactory.IdentifierName(nameof(ISchemaProvider)).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                            SyntaxFactory.Identifier("provider"), null)
                    })),
                new SyntaxList<TypeParameterConstraintClauseSyntax>(), 
                block,
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

            var tree = SyntaxFactory.SyntaxTree(Formatter.Format(compilationUnit, Workspace));

            var text = tree.GetText();

            var s = text.ToString();

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

        public string CurrentSchema { get; set; }
        public string CurrentTable { get; }
        public string[] CurrentParameters { get; }

        public void SetCurrentTable(string table, string[] parameters)
        {
        }
    }
}
