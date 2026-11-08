using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionPerformanceQueryTestsFixtures
{

    internal static void AssertNoExecutionBoxes(CompiledQuery query)
    {
        var boxed = GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query)
            .Where(GeneratedExecutionIlInspection.ContainsBoxOpcode)
            .Select(static method => method.DeclaringType + "." + method.Name).ToArray();
        Assert.IsEmpty(boxed, "Generated numeric and custom execution must not box values: " + string.Join(", ", boxed));
    }

}
