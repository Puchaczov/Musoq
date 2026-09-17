using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Visitors;

internal static class TypedSourceBindingMetadata
{
    public static SchemaMethodInfo[] PreferTypedSource(
        ISchema schema,
        string methodName,
        SchemaMethodInfo[] reflectedMethods)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(reflectedMethods);

        if (schema is not ITypedSourceSchema typedSchema ||
            !typedSchema.TryGetTypedSourceRegistration(methodName, out var registration))
            return reflectedMethods;

        return registration.Overloads
            .Select(overload =>
            {
                var constructor = overload.Constructor;
                var arguments = constructor.GetParameters()
                    .Where(static parameter => parameter.ParameterType != typeof(SourceExecutionContext))
                    .Select(static parameter => (
                        Name: parameter.Name ?? throw new InvalidOperationException("Typed source constructor parameter has no name."),
                        Type: parameter.ParameterType))
                    .ToArray();
                var metadata = new ConstructorInfo(constructor, overload.InjectsExecutionContext, arguments)
                {
                    StructuralContract = overload.Contract,
                    SourceStableId = overload.StableId
                };
                return new SchemaMethodInfo(methodName, metadata);
            })
            .ToArray();
    }
}