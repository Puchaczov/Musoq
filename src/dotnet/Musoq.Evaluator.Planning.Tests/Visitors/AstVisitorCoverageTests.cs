using Microsoft.CodeAnalysis;
using Musoq.Evaluator.Tests.Architecture;
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
        AssertDeclaresVisitMethods(CompilerContractSymbols.For(typeof(CloneQueryVisitor)));
    }

    [TestMethod]
    public void RawTraverseVisitor_ShouldDeclareVisitMethodForEveryExpressionVisitorNode()
    {
        AssertDeclaresVisitMethods(CompilerContractSymbols.For(typeof(RawTraverseVisitor<>)));
    }

    [TestMethod]
    public void NoOpExpressionVisitor_ShouldDeclareVisitMethodForEveryExpressionVisitorNode()
    {
        AssertDeclaresVisitMethods(CompilerContractSymbols.For(typeof(NoOpExpressionVisitor)));
    }

    [TestMethod]
    public void RewriteQueryTraverseVisitor_ShouldKeepDomainSpecificVisitOverrides()
    {
        AssertDeclaresVisitMethods(
            CompilerContractSymbols.For(typeof(RewriteQueryTraverseVisitor)),
            CompilerContractSymbols.For(typeof(DotNode)),
            CompilerContractSymbols.For(typeof(GroupByNode)),
            CompilerContractSymbols.For(typeof(JoinSourcesTableFromNode)),
            CompilerContractSymbols.For(typeof(ApplySourcesTableFromNode)),
            CompilerContractSymbols.For(typeof(JoinFromNode)),
            CompilerContractSymbols.For(typeof(ApplyFromNode)),
            CompilerContractSymbols.For(typeof(AccessMethodFromNode)),
            CompilerContractSymbols.For(typeof(AliasedFromNode)),
            CompilerContractSymbols.For(typeof(QueryNode)),
            CompilerContractSymbols.For(typeof(WindowFunctionNode)),
            CompilerContractSymbols.For(typeof(WindowSpecificationNode)),
            CompilerContractSymbols.For(typeof(WindowDefinitionNode)),
            CompilerContractSymbols.For(typeof(WindowNode)),
            CompilerContractSymbols.For(typeof(InternalQueryNode)),
            CompilerContractSymbols.For(typeof(UnionNode)),
            CompilerContractSymbols.For(typeof(UnionAllNode)),
            CompilerContractSymbols.For(typeof(ExceptNode)),
            CompilerContractSymbols.For(typeof(IntersectNode)),
            CompilerContractSymbols.For(typeof(CteExpressionNode)),
            CompilerContractSymbols.For(typeof(CteInnerExpressionNode)),
            CompilerContractSymbols.For(typeof(InterpretCallNode)),
            CompilerContractSymbols.For(typeof(ParseCallNode)),
            CompilerContractSymbols.For(typeof(TryInterpretCallNode)),
            CompilerContractSymbols.For(typeof(TryParseCallNode)),
            CompilerContractSymbols.For(typeof(PartialInterpretCallNode)),
            CompilerContractSymbols.For(typeof(PartialParseCallNode)),
            CompilerContractSymbols.For(typeof(InterpretAtCallNode)));
    }

    [TestMethod]
    public void InterpretationSchemaDefinitionSkippingTraverseVisitor_ShouldKeepSchemaDefinitionVisitOverrides()
    {
        AssertDeclaresVisitMethods(
            CompilerContractSymbols.For(typeof(InterpretationSchemaDefinitionSkippingTraverseVisitor<>)),
            CompilerContractSymbols.For(typeof(BinarySchemaNode)),
            CompilerContractSymbols.For(typeof(TextSchemaNode)),
            CompilerContractSymbols.For(typeof(FieldDefinitionNode)),
            CompilerContractSymbols.For(typeof(ComputedFieldNode)),
            CompilerContractSymbols.For(typeof(TextFieldDefinitionNode)),
            CompilerContractSymbols.For(typeof(FieldConstraintNode)),
            CompilerContractSymbols.For(typeof(PrimitiveTypeNode)),
            CompilerContractSymbols.For(typeof(ByteArrayTypeNode)),
            CompilerContractSymbols.For(typeof(StringTypeNode)),
            CompilerContractSymbols.For(typeof(SchemaReferenceTypeNode)),
            CompilerContractSymbols.For(typeof(ArrayTypeNode)),
            CompilerContractSymbols.For(typeof(BitsTypeNode)),
            CompilerContractSymbols.For(typeof(AlignmentNode)),
            CompilerContractSymbols.For(typeof(RepeatUntilTypeNode)),
            CompilerContractSymbols.For(typeof(InlineSchemaTypeNode)));
    }

    [TestMethod]
    public void BuildMetadataAndInferTypesTraverseVisitor_ShouldKeepDomainSpecificVisitOverrides()
    {
        AssertDeclaresVisitMethods(
            CompilerContractSymbols.For(typeof(BuildMetadataAndInferTypesTraverseVisitor)),
            CompilerContractSymbols.For(typeof(GroupSelectNode)),
            CompilerContractSymbols.For(typeof(DotNode)),
            CompilerContractSymbols.For(typeof(GroupByNode)),
            CompilerContractSymbols.For(typeof(HavingNode)),
            CompilerContractSymbols.For(typeof(QualifyNode)),
            CompilerContractSymbols.For(typeof(JoinSourcesTableFromNode)),
            CompilerContractSymbols.For(typeof(JoinFromNode)),
            CompilerContractSymbols.For(typeof(ApplyFromNode)),
            CompilerContractSymbols.For(typeof(ValuesFromNode)),
            CompilerContractSymbols.For(typeof(WindowFunctionNode)),
            CompilerContractSymbols.For(typeof(QueryNode)),
            CompilerContractSymbols.For(typeof(UnionNode)),
            CompilerContractSymbols.For(typeof(UnionAllNode)),
            CompilerContractSymbols.For(typeof(ExceptNode)),
            CompilerContractSymbols.For(typeof(IntersectNode)),
            CompilerContractSymbols.For(typeof(CteExpressionNode)),
                CompilerContractSymbols.For(typeof(CteInnerExpressionNode)));
    }

    private static void AssertDeclaresVisitMethods(ITypeSymbol visitorType)
    {
        var declaredVisitTypes = visitorType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(method => method.MetadataName == nameof(IExpressionVisitor.Visit))
            .Select(method => method.GetParameters())
            .Where(parameters => parameters.Length == 1)
            .Select(parameters => parameters[0].ParameterType)
            .ToHashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        var missing = CompilerContractSymbols.For(typeof(IExpressionVisitor))
            .GetMethods()
            .Where(method => method.MetadataName == nameof(IExpressionVisitor.Visit))
            .Select(method => method.GetParameters())
            .Where(parameters => parameters.Length == 1)
            .Select(parameters => parameters[0].ParameterType)
            .Where(type => CompilerContractSymbols.For(typeof(Node)).IsAssignableFrom(type))
            .Where(type => !declaredVisitTypes.Contains(type))
            .Select(type => type.MetadataName)
            .OrderBy(static name => name)
            .ToArray();

        Assert.IsEmpty(
            missing,
            $"{visitorType.Name} must explicitly handle every node in {nameof(IExpressionVisitor)}: " +
            string.Join(", ", missing));
    }

    private static void AssertDeclaresVisitMethods(ITypeSymbol visitorType, params ITypeSymbol[] expectedNodeTypes)
    {
        var declaredVisitTypes = visitorType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(method => method.MetadataName == nameof(IExpressionVisitor.Visit))
            .Select(method => method.GetParameters())
            .Where(parameters => parameters.Length == 1)
            .Select(parameters => parameters[0].ParameterType)
            .ToHashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        var missing = expectedNodeTypes
            .Where(type => !declaredVisitTypes.Contains(type))
            .Select(type => type.MetadataName)
            .OrderBy(static name => name)
            .ToArray();

        Assert.IsEmpty(
            missing,
            $"{visitorType.Name} must explicitly keep the expected domain visit methods: " +
            string.Join(", ", missing));
    }
}
