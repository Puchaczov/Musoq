using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ExecutionValueTupleTypeShapeTests
{
    [TestMethod]
    public void CreatesAndRecognizesCanonicalNestedValueTupleShapes()
    {
        foreach (var elementCount in new[] { 2, 7, 8, 9, 15 })
        {
            var elementTypes = new Type[elementCount];
            for (var index = 0; index < elementTypes.Length; index++)
                elementTypes[index] = (index % 3) switch
                {
                    0 => typeof(int),
                    1 => typeof(string),
                    _ => typeof(int?)
                };

            Assert.IsTrue(ValueTupleTypeShape.TryCreate(elementTypes, out var tupleType));
            Assert.IsTrue(ValueTupleTypeShape.TryGetElementTypes(tupleType, out var flattenedTypes));
            CollectionAssert.AreEqual(elementTypes, flattenedTypes);

            if (elementCount > 7)
            {
                Assert.AreEqual(8, tupleType.GetGenericArguments().Length);
                Assert.IsTrue(ValueTupleTypeShape.IsValueTuple(tupleType));
            }
        }
    }

    [TestMethod]
    public void DoesNotTreatSingleValueTupleAsACompositeKey()
    {
        Assert.IsFalse(ValueTupleTypeShape.TryGetElementTypes(typeof(ValueTuple<int>), out _));
        Assert.IsFalse(ValueTupleTypeShape.TryCreate([typeof(int)], out _));
    }
}
