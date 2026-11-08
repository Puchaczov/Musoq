using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryFunctionCatalogQueryTests
{
    [TestMethod]
    public void RegistrationSnapshots_WhenAnotherManagerAddsAnOverride_ShouldKeepIndependentSqlBindings()
    {
        var original = new MethodsManager();
        original.RegisterLibraries(new AncestorLibrary());
        var changed = new MethodsManager();
        changed.RegisterLibraries(new AncestorLibrary());
        changed.RegisterLibraries(new DescendantLibrary());
        var fresh = new MethodsManager();
        fresh.RegisterLibraries(new AncestorLibrary());

        AssertRegisteredPrefix(changed, "derived:seed");
        AssertRegisteredPrefix(original, "base:seed");
        AssertRegisteredPrefix(fresh, "base:seed");
    }

    private void AssertRegisteredPrefix(MethodsManager manager, string expected)
    {
        const string query = "select Prefix(Text) as Value from #registered.all()";
        var provider = new GenericSchemaProvider(new Dictionary<string, ISchema>
        {
            ["#registered"] = new RegistrationSchema(new MethodsAggregator(manager))
        });
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString("N"),
            provider, LoggerResolver, TestCompilationOptions);
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(string)));
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(expected, table.Single().GetValue<string>(0));
    }

    public sealed record RegistrationInput(string Text);

    private sealed class RegistrationSchema(MethodsAggregator methods) : SchemaBase("registered", methods)
    {
        public override ISchemaTable GetTableByName(string name, SourceMetadataContext context,
            params object?[] parameters) => new GenericEntityTable<RegistrationInput>();

        public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext context,
            TArguments parameters) => EnsureSourceType<T, RegistrationInput>(name,
            new EntitySource<RegistrationInput>(
                new IReadOnlyList<RegistrationInput>[] { new RegistrationInput[] { new("seed") } },
                new Dictionary<string, int>(), new Dictionary<int, Func<RegistrationInput, object?>>()));

        public override SchemaMethodInfo[] GetRawConstructors(string methodName, SourceMetadataContext context)
            => [new SchemaMethodInfo("all", ConstructorInfo.Empty())];
    }
}
