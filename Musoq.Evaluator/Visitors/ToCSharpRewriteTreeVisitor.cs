using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Musoq.Evaluator.CSharpTemplates;
using Musoq.Evaluator.Tables;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors
{
    public class ToCSharpRewriteTreeVisitor : ISchemaAwareExpressionVisitor
    {
        private class QueryParts
        {
            public SyntaxNode From { get; set; }

            public SyntaxNode Where { get; set; }

            public SyntaxNode Select { get; set; }
        }

        public AdhocWorkspace Workspace { get; }

        public SyntaxGenerator Generator { get; }

        public CSharpCompilation Compilation { get; }

        private Stack<SyntaxNode> Nodes { get; }

        private QueryParts Parts { get; } = new QueryParts();

        public ToCSharpRewriteTreeVisitor(IEnumerable<System.Reflection.Assembly> assemblies)
        {
            Workspace = new AdhocWorkspace();
            Generator = SyntaxGenerator.GetGenerator(Workspace, LanguageNames.CSharp);
            Generator.NamespaceImportDeclaration("System");
            Nodes = new Stack<SyntaxNode>();
            Compilation = CSharpCompilation.Create("test");
            Compilation = Compilation
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddReferences(assemblies.Select(f => MetadataReference.CreateFromFile(f.Location)).ToArray());
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
            Nodes.Push(Generator.InvocationExpression(Generator.MemberAccessExpression(Generator.IdentifierName("row"), Generator.IdentifierName(node.Name))));
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

        private static SyntaxTrivia WhiteSpace => SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " ");

        public void Visit(SelectNode node)
        {
            var variableNameKeyword = SyntaxFactory.Identifier(SyntaxTriviaList.Empty, "select", SyntaxTriviaList.Create(WhiteSpace));
            var newKeyword = SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.NewKeyword, SyntaxTriviaList.Create(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " ")));
            var objectKeyword =
                SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.ObjectKeyword, SyntaxTriviaList.Empty);

            var syntaxList = new SeparatedSyntaxList<ExpressionSyntax>();
            for (int i = 0; i < node.Fields.Length; i++)
            {
                syntaxList = syntaxList.Add((ExpressionSyntax)Nodes.Pop());
            }

            var rankSpecifiers = new SyntaxList<ArrayRankSpecifierSyntax>();

            rankSpecifiers = rankSpecifiers.Add(
                SyntaxFactory.ArrayRankSpecifier(
                    SyntaxFactory.Token(SyntaxKind.OpenBracketToken),
                    new SeparatedSyntaxList<ExpressionSyntax>()
                    {
                        SyntaxFactory.OmittedArraySizeExpression(
                            SyntaxFactory.Token(SyntaxKind.OmittedArraySizeExpressionToken))
                    },
                    SyntaxFactory.Token(SyntaxKind.CloseBracketToken)));

            var array = SyntaxFactory.ArrayCreationExpression(
                newKeyword,
                SyntaxFactory.ArrayType(SyntaxFactory.PredefinedType(objectKeyword), rankSpecifiers), 
                SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, syntaxList));
            var equalsClause = SyntaxFactory.EqualsValueClause(SyntaxFactory.Token(SyntaxKind.EqualsToken).WithTrailingTrivia(WhiteSpace), array);

            var variableDecl = SyntaxFactory.VariableDeclarator(variableNameKeyword, null, equalsClause);
            var list = SyntaxFactory.SeparatedList(new List<VariableDeclaratorSyntax>()
            {
                variableDecl
            });

            var variableDeclaration =
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var").WithTrailingTrivia(WhiteSpace),
                    list);

            var invocation = SyntaxFactory
                .InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("table"),
                        SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(nameof(Table.Add))
                    ),
                    SyntaxFactory.ArgumentList(
                        new SeparatedSyntaxList<ArgumentSyntax>()
                        {
                            SyntaxFactory.Argument(
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.Token(SyntaxKind.NewKeyword).WithTrailingTrivia(WhiteSpace), 
                                    SyntaxFactory.ParseTypeName(nameof(ObjectsRow)), 
                                    SyntaxFactory.ArgumentList(
                                        new SeparatedSyntaxList<ArgumentSyntax>()
                                        {
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(variableNameKeyword.Text))
                                        }
                                    ),
                                    SyntaxFactory.InitializerExpression(SyntaxKind.ComplexElementInitializerExpression))
                                )
                        }));

            Nodes.Push(SyntaxFactory.FieldDeclaration(new SyntaxList<AttributeListSyntax>(), new SyntaxTokenList(), variableDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
        }

        public void Visit(WhereNode node)
        {
            Nodes.Push(Generator.IfStatement(Generator.LogicalNotExpression(Nodes.Pop()),
                new SyntaxNode[] {SyntaxFactory.ContinueStatement()}));

            Parts.From = Nodes.Pop();
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
        }

        public void Visit(InternalQueryNode node)
        {
        }

        public void Visit(RootNode node)
        {
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
