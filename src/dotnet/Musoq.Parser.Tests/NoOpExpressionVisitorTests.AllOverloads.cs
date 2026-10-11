using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

public partial class NoOpExpressionVisitorTests
{
    [TestMethod]
    public void NoOpExpressionVisitor_VisitRemainingOverloads_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();

        // Collection, subquery, parameter, and script-variable nodes.
        visitor.Visit((InQueryNode)null!);
        visitor.Visit((ExistsQueryNode)null!);
        visitor.Visit((ScalarSubqueryNode)null!);
        visitor.Visit((ParameterBlockNode)null!);
        visitor.Visit((ParameterDeclarationNode)null!);
        visitor.Visit((ParameterReferenceNode)null!);
        visitor.Visit((ScriptVariableDeclarationNode)null!);
        visitor.Visit((ScriptVariableReferenceNode)null!);

        // Access and property nodes.
        visitor.Visit((AccessMethodNode)null!);
        visitor.Visit((AccessRawIdentifierNode)null!);
        visitor.Visit((RowPresenceNode)null!);
        visitor.Visit((AccessRefreshAggregationScoreNode)null!);
        visitor.Visit((AccessCallChainNode)null!);

        // Query projection and source nodes.
        visitor.Visit((GroupSelectNode)null!);
        visitor.Visit((JoinInMemoryWithSourceTableFromNode)null!);
        visitor.Visit((ApplyInMemoryWithSourceTableFromNode)null!);
        visitor.Visit((AliasedFromNode)null!);
        visitor.Visit((JoinSourcesTableFromNode)null!);
        visitor.Visit((ApplySourcesTableFromNode)null!);
        visitor.Visit((InMemoryTableFromNode)null!);
        visitor.Visit((ValuesFromNode)null!);
        visitor.Visit((UnpivotFromNode)null!);
        visitor.Visit((JoinFromNode)null!);
        visitor.Visit((ApplyFromNode)null!);
        visitor.Visit((ExpressionFromNode)null!);
        visitor.Visit((InterpretFromNode)null!);
        visitor.Visit((SchemaMethodFromNode)null!);
        visitor.Visit((PropertyFromNode)null!);
        visitor.Visit((AccessMethodFromNode)null!);

        // Query, statement, set, and table nodes.
        visitor.Visit((CreateTransformationTableNode)null!);
        visitor.Visit((RenameTableNode)null!);
        visitor.Visit((QueryScope)null!);
        visitor.Visit((ShouldBePresentInTheTable)null!);
        visitor.Visit((TranslatedSetOperatorNode)null!);
        visitor.Visit((InternalQueryNode)null!);
        visitor.Visit((PutTrueNode)null!);
        visitor.Visit((MultiStatementNode)null!);
        visitor.Visit((StatementsArrayNode)null!);
        visitor.Visit((StatementNode)null!);
        visitor.Visit((CteExpressionNode)null!);
        visitor.Visit((CteInnerExpressionNode)null!);
        visitor.Visit((JoinNode)null!);
        visitor.Visit((ApplyNode)null!);
        visitor.Visit((CreateTableNode)null!);
        visitor.Visit((EnumDeclarationNode)null!);
        visitor.Visit((EnumMemberNode)null!);
        visitor.Visit((CoupleNode)null!);

        // Expression and interpretation-call nodes.
        visitor.Visit((CoalesceNode)null!);
        visitor.Visit((ArrayIndexNode)null!);
        visitor.Visit((InterpretAtCallNode)null!);
        visitor.Visit((PartialInterpretCallNode)null!);

        // Interpretation schema nodes.
        visitor.Visit((BinarySchemaNode)null!);
        visitor.Visit((TextSchemaNode)null!);
        visitor.Visit((FieldDefinitionNode)null!);
        visitor.Visit((TextFieldDefinitionNode)null!);
        visitor.Visit((ComputedFieldNode)null!);
        visitor.Visit((FieldConstraintNode)null!);
        visitor.Visit((FieldValueValidationNode)null!);
        visitor.Visit((PrimitiveTypeNode)null!);
        visitor.Visit((ByteArrayTypeNode)null!);
        visitor.Visit((BinarySwitchTypeNode)null!);
        visitor.Visit((StringTypeNode)null!);
        visitor.Visit((SchemaReferenceTypeNode)null!);
        visitor.Visit((ArrayTypeNode)null!);
        visitor.Visit((BitsTypeNode)null!);
        visitor.Visit((AlignmentNode)null!);
        visitor.Visit((RepeatUntilTypeNode)null!);
        visitor.Visit((InlineSchemaTypeNode)null!);
        visitor.Visit((SubstreamTypeNode)null!);

        // Window nodes.
        visitor.Visit((WindowFunctionNode)null!);
        visitor.Visit((WindowSpecificationNode)null!);
        visitor.Visit((WindowFrameNode)null!);
        visitor.Visit((WindowFrameBoundNode)null!);
        visitor.Visit((WindowDefinitionNode)null!);
        visitor.Visit((WindowNode)null!);
        visitor.Visit((QualifyNode)null!);
    }
}
