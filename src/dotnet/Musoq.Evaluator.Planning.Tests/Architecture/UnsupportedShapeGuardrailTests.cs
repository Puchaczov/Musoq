using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class UnsupportedShapeGuardrailTests
{
    [TestMethod]
    public void Of_WhenGivenSubject_ShouldProduceCanonicalUnsupportedMessage()
    {
        var exception = UnsupportedShape.Of("Append mode Foo");

        Assert.AreEqual("Append mode Foo is not supported.", exception.Message);
    }

    [TestMethod]
    public void Of_WhenGivenSubjectAndConsumer_ShouldNameTheConsumer()
    {
        var exception = UnsupportedShape.Of("Execution node 'Bar'", "the C# backend");

        Assert.AreEqual("Execution node 'Bar' is not supported by the C# backend.", exception.Message);
    }

    [TestMethod]
    public void Of_WhenSubjectIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => UnsupportedShape.Of(null!));
    }

    [TestMethod]
    public void Of_WhenConsumerIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => UnsupportedShape.Of("subject", null!));
    }
}
