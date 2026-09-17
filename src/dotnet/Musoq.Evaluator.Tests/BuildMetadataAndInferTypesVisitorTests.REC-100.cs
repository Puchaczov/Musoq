using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

public partial class BuildMetadataAndInferTypesVisitorTests
{
    [TestMethod]
    public void DiagnosticREC100_QueryLocalEnumTypeNames_ShouldResolveWithoutCaseSensitivity()
    {
        var visitor = Analyze(
            "enum WorkState : byte { Ready = 1ub };" +
            "table Jobs { Status: WORKSTATE?, Previous: workstate };" +
            "couple #capture.any with table Jobs as Jobs;" +
            "select Status from Jobs()",
            new CaptureMetadataContextSchemaProvider(_ => { }));

        Assert.IsTrue(visitor.QueryLocalEnumTypes.TryGetValue("workstate", out var descriptor));
        Assert.IsNotNull(descriptor);
        Assert.AreEqual("WorkState", descriptor.DisplayName);
        Assert.AreEqual(EnumTypeOrigin.QueryLocal, descriptor.Origin);
        Assert.AreEqual(EnumUnderlyingKind.Byte, descriptor.UnderlyingKind);
    }

    [TestMethod]
    public void DiagnosticREC100_QueryLocalNumericAliases_ShouldKeepFirstDeclaredCanonicalName()
    {
        var visitor = Analyze(
            "flags enum PermissionMask : uint { None = 0ui, Read = 1ui, AliasRead = 1ui, Write = 2ui, ReadWrite = 3ui };" +
            "select 1 from #EnvironmentVariables.All()");

        var descriptor = visitor.QueryLocalEnumTypes["permissionmask"];
        Assert.IsTrue(descriptor.TryGetValue("AliasRead", out var aliasValue));
        Assert.IsTrue(descriptor.Aliases.TryGetValue("AliasRead", out var aliasCanonical));
        Assert.AreEqual("Read", aliasCanonical);
        Assert.IsTrue(descriptor.TryGetCanonicalName(aliasValue, out var canonicalAlias));
        Assert.AreEqual("Read", canonicalAlias);

        Assert.IsTrue(descriptor.TryGetValue("ReadWrite", out var compositeValue));
        Assert.IsTrue(descriptor.TryGetCanonicalName(compositeValue, out var canonicalComposite));
        Assert.AreEqual("ReadWrite", canonicalComposite);
        Assert.AreEqual(5, descriptor.Members.Count);
        Assert.IsTrue(descriptor.Members.Select(static member => member.Name).Contains("AliasRead"));
    }
}
