using System;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;
using Musoq.Schema.Optimization;

namespace Musoq.Converter.Tests.Schema;

public class SystemSchema() : SchemaBase(System, CreateLibrary())
{
    private const string Dual = "dual";
    private const string System = "system";

    public override SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters)
        => base.DescribeSource(name, context, parameters) with
        {
            ExecutionPermissions = SourceExecutionPermissions.ConcurrentAccess | SourceExecutionPermissions.ConcurrentRowProcessing
        };

    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters)
    {
        switch (name.ToLowerInvariant())
        {
            case Dual:
                return new DualTable();
        }

        throw new NotSupportedException(name);
    }

    public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext executionContext, TArguments nativeArguments)
    {
        switch (name.ToLowerInvariant())
        {
            case Dual:
                return EnsureSourceType<T, DualEntity>(name, new DualRowSource());
        }

        throw new NotSupportedException(name);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();

        var library = new EmptyLibrary();

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
