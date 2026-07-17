using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

public partial class BuildMetadataAndInferTypesVisitorUtilitiesTests
{
    [TestMethod]
    public void IsValidQueryExpressionType_CorrelatedScalarCarrier_ShouldReturnTrue()
    {
        Assert.IsTrue(BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(
            typeof(CorrelatedScalarSubqueryResult<string>)));
        Assert.IsTrue(BuildMetadataAndInferTypesVisitorUtilities.IsValidQueryExpressionType(
            typeof(CorrelatedScalarSubqueryResult<int>?)));
    }
}
