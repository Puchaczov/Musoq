using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class LibraryFunctionCatalogTests
{
    [TestMethod]
    public void OpenGenericOverloads_WhenArrayAndEnumerableInputsDiffer_ShouldRemainDistinct()
    {
        var functions = LibraryFunctionCatalog.For(typeof(GenericLibrary)).Functions
            .Where(static function => function.Name == nameof(GenericLibrary.Items)).ToArray();

        Assert.HasCount(2, functions);
        Assert.IsTrue(functions.Any(static function => function.Parameters[0].Type.IsArray));
        Assert.IsTrue(functions.Any(static function => function.Parameters[0].Type.IsGenericType));
    }

    [TestMethod]
    public void Catalog_WhenReadRepeatedly_ShouldReuseImmutableDeclarations()
    {
        var first = LibraryFunctionCatalog.For(typeof(GenericLibrary));
        var second = LibraryFunctionCatalog.For(typeof(GenericLibrary));

        Assert.AreSame(first, second);
        Assert.IsTrue(first.Functions.Where(static function => function.Declaration.DeclaringType == typeof(GenericLibrary)).All(static function =>
            function.ExecutionPermissions == FunctionExecutionPermissions.None));
    }

    [TestMethod]
    public void AggregateAliases_WhenEqualPrecedenceSignaturesCompete_ShouldRejectRegistration()
    {
        var error = Assert.Throws<System.InvalidOperationException>(() =>
            LibraryFunctionCatalog.For(typeof(ConflictingLibrary)));

        Assert.Contains("Competing Aggregate declarations", error.Message);
    }

    [TestMethod]
    public void Registration_WhenAnUnrelatedLibraryConflicts_ShouldPreserveThePreviousCatalog()
    {
        var methods = new MethodsManager();
        methods.RegisterLibraries(new FirstLibrary());
        var original = methods.GetAllFunctions();

        Assert.Throws<System.InvalidOperationException>(() => methods.RegisterLibraries(new SecondLibrary()));

        Assert.IsTrue(original.SequenceEqual(methods.GetAllFunctions()));
        Assert.IsFalse(methods.GetAllFunctions().Any(static function => function.Name == nameof(SecondLibrary.Other)));
    }

    [TestMethod]
    public void CombinedDeclaration_WhenOnlyAggregateIsOverridden_ShouldKeepOneWindowAndTheDerivedAggregate()
    {
        var methods = new MethodsManager();
        methods.RegisterLibraries(new AggregateOverrideLibrary());
        var functions = methods.GetAllFunctions().Where(static function => function.Name == "Measure").ToArray();

        Assert.HasCount(2, functions);
        Assert.AreEqual(typeof(AggregateOverrideLibrary), functions.Single(static function =>
            function.Kind == SqlFunctionKind.Aggregate).Declaration.DeclaringType);
        Assert.AreEqual(typeof(CombinedLibrary), functions.Single(static function =>
            function.Kind == SqlFunctionKind.Window).Declaration.DeclaringType);
        Assert.IsTrue(methods.TryGetWindowFunction("measure", out var window));
        Assert.AreEqual(typeof(CombinedLibrary), window.DeclaringType);
    }

    public class CombinedLibrary : LibraryBase
    {
        [AggregateFunction(typeof(SumAggregateKernel<int>))]
        [WindowFunction(KernelType = typeof(SumAggregateKernel<int>), Modes = WindowEvaluationModes.Running)]
        public int? Measure(int? value) => AggregateFunction.NotInvoked<int?>();
    }

    public sealed class AggregateOverrideLibrary : CombinedLibrary
    {
        [AggregateFunction(typeof(SumAggregateKernel<int>))]
        public new int? Measure(int? value) => AggregateFunction.NotInvoked<int?>();
    }

    public sealed class GenericLibrary : LibraryBase
    {
        [BindableMethod]
        public int Items<T>(T[] values) => values.Length;

        [BindableMethod]
        public int Items<T>(IEnumerable<T> values) => values.Count();
    }

    public sealed class ConflictingLibrary : LibraryBase
    {
        [AggregateFunction(typeof(SumAggregateKernel<int>), Name = "Same")]
        public int? First(int? value) => AggregateFunction.NotInvoked<int?>();

        [AggregateFunction(typeof(SumAggregateKernel<int>), Name = "Same")]
        public int? Second(int? value) => AggregateFunction.NotInvoked<int?>();
    }

    public sealed class FirstLibrary : LibraryBase
    {
        [BindableMethod]
        public int Value(int value) => value;
    }

    public sealed class SecondLibrary : LibraryBase
    {
        [BindableMethod]
        public int Other() => 1;

        [BindableMethod]
        public int Value(int value) => value;
    }
}
