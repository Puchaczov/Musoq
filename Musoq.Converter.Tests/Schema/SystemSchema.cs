using System;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;
using Musoq.Plugins;

namespace Musoq.Converter.Tests.Schema;

public partial class SystemSchema : SchemaBase
{
    private const string Dual = "dual";
    private const string System = "system";

    public SystemSchema()
        : base(System, CreateLibrary())
    {
    }

    public override ISchemaTable GetTableByName(string name, RuntimeContext runtimeContext, params object[] parameters)
    {
        switch (name.ToLowerInvariant())
        {
            case Dual:
                return new DualTable();
        }

        throw new NotSupportedException(name);
    }

    public override RowSource GetRowSource(string name, RuntimeContext interCommunicator, params object[] parameters)
    {
        switch (name.ToLowerInvariant())
        {
            case Dual:
                return new DualRowSource();
        }

        throw new NotSupportedException(name);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();

        var library = new TestLibrary(); // Use TestLibrary which inherits from LibraryBase

        methodsManager.RegisterLibraries(library);

        return new MethodsAggregator(methodsManager);
    }

    public override SchemaMethodInfo[] GetConstructors()
    {
        var constructors = new List<SchemaMethodInfo>();

        constructors.AddRange(TypeHelper.GetSchemaMethodInfosForType<DualRowSource>(Dual));

        return constructors.ToArray();
    }
}