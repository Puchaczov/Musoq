using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.IR;

public partial class ExpressionIrTests
{
    [TestMethod]
    public void ExpressionConverter_WhenScriptVariableReferenceNode_ShouldCreateScriptVariableRef()
    {
        var converted = _converter.Convert(new ScriptVariableReferenceNode("topic", typeof(string)));

        Assert.IsInstanceOfType<ScriptVariableRef>(converted);
        var variable = (ScriptVariableRef)converted;
        Assert.AreEqual("topic", variable.Name);
        Assert.AreEqual(typeof(string), variable.ReturnType);
    }

    [TestMethod]
    public void Visitor_WhenCustomVisitor_ShouldDispatchScriptVariableRef()
    {
        var visitor = new TypeNameVisitor();

        Assert.AreEqual("ScriptVariableRef", visitor.Visit(new ScriptVariableRef("topic", typeof(string))));
    }
}