using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.IR;

public partial class ExpressionIrTests
{
    [TestMethod]
    public void Converter_WhenStringNodeContainsWindowsPath_ShouldPreserveBackslashes()
    {
        var node = new StringNode(@"C:\new\test", default);

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<Literal>(result);
        Assert.AreEqual(@"C:\new\test", ((Literal)result).Value);
    }
}
