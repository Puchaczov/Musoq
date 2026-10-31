using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Formatter.Tests.Verification;

namespace Musoq.Formatter.Tests;

[TestClass]
public sealed class CompilerInputTests
{
    [TestMethod]
    public void MissingEvaluatedSource_ShouldFailWithoutDirectoryFallback()
    {
        var inputs = CompilerInputs.Read("Tests") with { Sources = [Path.Combine(AppContext.BaseDirectory, "missing-verification-source.cs")] };

        Assert.ThrowsExactly<InvalidOperationException>(() => inputs.Compile("MissingSource"));
    }

    [TestMethod]
    public void MissingEvaluatedReference_ShouldFailWithoutAssemblyDiscovery()
    {
        var inputs = CompilerInputs.Read("Tests") with { References = [Path.Combine(AppContext.BaseDirectory, "missing-verification-reference.dll")] };

        Assert.ThrowsExactly<InvalidOperationException>(() => inputs.Compile("MissingReference"));
    }

    [TestMethod]
    public void UnknownLanguageVersion_ShouldFailWithoutDefaulting()
    {
        var inputs = CompilerInputs.Read("Tests") with { Language = "unknown-language" };

        Assert.ThrowsExactly<InvalidOperationException>(() => inputs.Compile("UnknownLanguage"));
    }

    [TestMethod]
    public void UnknownNullableSetting_ShouldFailWithoutDefaulting()
    {
        var inputs = CompilerInputs.Read("Tests") with { Nullable = "unknown-nullable" };

        Assert.ThrowsExactly<InvalidOperationException>(() => inputs.Compile("UnknownNullable"));
    }
}
