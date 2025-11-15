using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class AliasGeneratorTests
{
    [TestMethod]
    public void GenerateAliasTest()
    {
        Assert.AreEqual(
            "ko3iko", 
            AliasGenerator.CreateAliasIfEmpty(string.Empty, [], "1"));
        Assert.AreEqual(
            "d40v7n", 
            AliasGenerator.CreateAliasIfEmpty(string.Empty, ["ko3iko"], "1"));
    }
}
