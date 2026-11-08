using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests.Visitors;

[TestClass]
public sealed class AstVisitorCoverageTests
{
    [TestMethod]
    public void CloneQueryVisitor_ShouldDeclareVisitMethodForEveryExpressionVisitorNode()
    {
        AssertDeclaresVisitMethods(typeof(CloneQueryVisitor));
    }

    [TestMethod]
    public void RawTraverseVisitor_ShouldDeclareVisitMethodForEveryExpressionVisitorNode()
    {
        AssertDeclaresVisitMethods(typeof(RawTraverseVisitor<>));
    }

    [TestMethod]
    public void NoOpExpressionVisitor_ShouldDeclareVisitMethodForEveryExpressionVisitorNode()
    {
        AssertDeclaresVisitMethods(typeof(NoOpExpressionVisitor));
    }

    [TestMethod]
    public void RewriteQueryTraverseVisitor_ShouldKeepDomainSpecificVisitOverrides()
    {
        AssertDeclaresVisitMethods(
            typeof(RewriteQueryTraverseVisitor),
            typeof(DotNode),
            typeof(GroupByNode),
            typeof(JoinSourcesTableFromNode),
            typeof(ApplySourcesTableFromNode),
            typeof(JoinFromNode),
            typeof(ApplyFromNode),
            typeof(AccessMethodFromNode),
            typeof(AliasedFromNode),
            typeof(QueryNode),
            typeof(WindowFunctionNode),
            typeof(WindowSpecificationNode),
            typeof(WindowDefinitionNode),
            typeof(WindowNode),
            typeof(InternalQueryNode),
            typeof(UnionNode),
            typeof(UnionAllNode),
            typeof(ExceptNode),
            typeof(IntersectNode),
            typeof(CteExpressionNode),
            typeof(CteInnerExpressionNode),
            typeof(InterpretCallNode),
            typeof(ParseCallNode),
            typeof(TryInterpretCallNode),
            typeof(TryParseCallNode),
            typeof(PartialInterpretCallNode),
            typeof(PartialParseCallNode),
            typeof(InterpretAtCallNode));
    }

    [TestMethod]
    public void InterpretationSchemaDefinitionSkippingTraverseVisitor_ShouldKeepSchemaDefinitionVisitOverrides()
    {
        AssertDeclaresVisitMethods(
            typeof(InterpretationSchemaDefinitionSkippingTraverseVisitor<>),
            typeof(BinarySchemaNode),
            typeof(TextSchemaNode),
            typeof(FieldDefinitionNode),
            typeof(ComputedFieldNode),
            typeof(TextFieldDefinitionNode),
            typeof(FieldConstraintNode),
            typeof(PrimitiveTypeNode),
            typeof(ByteArrayTypeNode),
            typeof(StringTypeNode),
            typeof(SchemaReferenceTypeNode),
            typeof(ArrayTypeNode),
            typeof(BitsTypeNode),
            typeof(AlignmentNode),
            typeof(RepeatUntilTypeNode),
            typeof(InlineSchemaTypeNode));
    }

    [TestMethod]
    public void BuildMetadataAndInferTypesTraverseVisitor_ShouldKeepDomainSpecificVisitOverrides()
    {
        AssertDeclaresVisitMethods(
            typeof(BuildMetadataAndInferTypesTraverseVisitor),
            typeof(GroupSelectNode),
            typeof(DotNode),
            typeof(GroupByNode),
            typeof(HavingNode),
            typeof(QualifyNode),
            typeof(JoinSourcesTableFromNode),
            typeof(JoinFromNode),
            typeof(ApplyFromNode),
            typeof(ValuesFromNode),
            typeof(WindowFunctionNode),
            typeof(QueryNode),
            typeof(UnionNode),
            typeof(UnionAllNode),
            typeof(ExceptNode),
            typeof(IntersectNode),
            typeof(CteExpressionNode),
                typeof(CteInnerExpressionNode));
    }

    private static void AssertDeclaresVisitMethods(Type visitorType)
    {
        var declaredVisitTypes = visitorType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(method => method.Name == nameof(IExpressionVisitor.Visit))
            .Select(method => method.GetParameters())
            .Where(parameters => parameters.Length == 1)
            .Select(parameters => parameters[0].ParameterType)
            .ToHashSet();

        var missing = typeof(IExpressionVisitor)
            .GetMethods()
            .Where(method => method.Name == nameof(IExpressionVisitor.Visit))
            .Select(method => method.GetParameters())
            .Where(parameters => parameters.Length == 1)
            .Select(parameters => parameters[0].ParameterType)
            .Where(type => typeof(Node).IsAssignableFrom(type))
            .Where(type => !declaredVisitTypes.Contains(type))
            .Select(type => type.Name)
            .OrderBy(static name => name)
            .ToArray();

        Assert.IsEmpty(
            missing,
            $"{visitorType.Name} must explicitly handle every node in {nameof(IExpressionVisitor)}: " +
            string.Join(", ", missing));
    }

    private static void AssertDeclaresVisitMethods(Type visitorType, params Type[] expectedNodeTypes)
    {
        var declaredVisitTypes = visitorType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(method => method.Name == nameof(IExpressionVisitor.Visit))
            .Select(method => method.GetParameters())
            .Where(parameters => parameters.Length == 1)
            .Select(parameters => parameters[0].ParameterType)
            .ToHashSet();

        var missing = expectedNodeTypes
            .Where(type => !declaredVisitTypes.Contains(type))
            .Select(type => type.Name)
            .OrderBy(static name => name)
            .ToArray();

        Assert.IsEmpty(
            missing,
            $"{visitorType.Name} must explicitly keep the expected domain visit methods: " +
            string.Join(", ", missing));
    }
}
