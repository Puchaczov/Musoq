using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class AllocationOwnershipTests
{
    public static IEnumerable<object[]> Cases => AllocationOwnershipCases.Cases();

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void Constructor_ShouldOwnItsCollectionStorage(Type type, string property, string kind)
    {
        var fixture = Construct(type, property, kind);
        var owned = (IList)type.GetProperty(property)!.GetValue(fixture.Record)!;
        var expected = owned[0];

        fixture.Mutable[0] = fixture.Replacement;

        Assert.AreEqual(expected, owned[0]);
    }

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void RecordCloneInitializer_ShouldOwnItsCollectionStorage(Type type, string property, string kind)
    {
        var fixture = Construct(type, property, kind);
        var clone = type.GetMethod("<Clone>$")!.Invoke(fixture.Record, null)!;
        var member = type.GetProperty(property)!;
        member.SetValue(clone, fixture.Input);
        var owned = (IList)member.GetValue(clone)!;
        var expected = owned[0];

        fixture.Mutable[0] = fixture.Replacement;

        Assert.AreEqual(expected, owned[0]);
    }

    [TestMethod]
    public void NestedValuesConstructor_ShouldCopyOuterAndInnerCollections()
    {
        var first = new ExecutionRowValue("value", new ExecutionLiteral(1, typeof(int)));
        var row = new[] { first };
        IReadOnlyList<ExecutionRowValue>[] rows = [row];
        var node = new ExecutionCreateValuesRows(new ExecutionVariable("rows", typeof(object)), new GeneratedRowShape("Row", []), rows);

        row[0] = new ExecutionRowValue("changed", new ExecutionLiteral(2, typeof(int)));
        rows[0] = [];

        Assert.AreSame(first, node.Values[0][0]);
    }

    [TestMethod]
    public void NestedValuesWithExpression_ShouldCopyOuterAndInnerCollections()
    {
        var first = new ExecutionRowValue("value", new ExecutionLiteral(1, typeof(int)));
        var row = new[] { first };
        IReadOnlyList<ExecutionRowValue>[] rows = [row];
        var original = new ExecutionCreateValuesRows(new ExecutionVariable("rows", typeof(object)), new GeneratedRowShape("Row", []), []);
        var clone = original with { Values = rows };

        row[0] = new ExecutionRowValue("changed", new ExecutionLiteral(2, typeof(int)));
        rows[0] = [];

        Assert.AreSame(first, clone.Values[0][0]);
    }

    private static OwnershipFixture Construct(Type type, string property, string kind)
    {
        var factory = new AllocationTraversalFixtures(true);
        var constructor = type.GetConstructors().Where(candidate => candidate.GetParameters()
                .Any(parameter => string.Equals(parameter.Name, property, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(candidate => candidate.GetParameters().Length).First();
        var parameter = constructor.GetParameters().Single(candidate => string.Equals(candidate.Name, property, StringComparison.OrdinalIgnoreCase));
        var element = parameter.ParameterType.GetGenericArguments()[0];
        var array = Array.CreateInstance(element, 2);
        array.SetValue(factory.Create(element), 0);
        array.SetValue(factory.Create(element), 1);
        var storage = CreateStorage(element, array, kind);
        var arguments = constructor.GetParameters().Select(argument => argument == parameter ? storage.Input :
            argument.HasDefaultValue ? argument.DefaultValue : factory.Create(argument.ParameterType)).ToArray();
        var replacement = element == typeof(ExecutionNode) ? new ExecutionContinue() : factory.Create(element);
        if (Equals(storage.Mutable[0], replacement))
            throw new InvalidOperationException("The ownership fixture must replace its element with a different value.");
        return new OwnershipFixture(constructor.Invoke(arguments), storage.Input, storage.Mutable, replacement);
    }

    private static (object Input, IList Mutable) CreateStorage(Type element, Array array, string kind)
    {
        if (kind == "Array")
            return (array, array);
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(element), [array])!;
        if (kind == "List")
            return (list, list);
        return (Activator.CreateInstance(typeof(ReadOnlyCollection<>).MakeGenericType(element), [list])!, list);
    }

    private sealed record OwnershipFixture(object Record, object Input, IList Mutable, object Replacement);
}
