using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class NullableArithmeticTypeTests
{
    [TestMethod]
    [DataRow("+", false)]
    [DataRow("-", false)]
    [DataRow("*", false)]
    [DataRow("/", false)]
    [DataRow("%", false)]
    [DataRow("+", true)]
    [DataRow("-", true)]
    [DataRow("*", true)]
    [DataRow("/", true)]
    [DataRow("%", true)]
    public void ArithmeticWithNullableOperand_ShouldExposeItsNullableTypeToCallableBinding(string operation, bool nullableRight)
    {
        var left = new AccessColumnNode("Left", "a", nullableRight ? typeof(int) : typeof(int?), default);
        var right = new AccessColumnNode("Right", "a", nullableRight ? typeof(int?) : typeof(int), default);
        BinaryNode expression = operation switch
        {
            "+" => new AddNode(left, right),
            "-" => new HyphenNode(left, right),
            "*" => new StarNode(left, right),
            "/" => new FSlashNode(left, right),
            "%" => new ModuloNode(left, right),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        Assert.AreEqual(typeof(int?), expression.ReturnType);
    }
}
