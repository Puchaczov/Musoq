using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    /// <summary>
    /// Returns structural receiving contracts without evaluating argument
    /// expressions or opening a datasource row source. A non-null supplied
    /// list is the compile-time selected contract; null requests deterministic
    /// metadata inventory for every overload.
    /// </summary>
    public static Table GetStructuralArgumentDescriptions(
        ISchema schema,
        string methodName,
        IReadOnlyList<StructuralArgumentDescription>? selected,
        SourceExecutionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        ArgumentNullException.ThrowIfNull(executionContext);
        executionContext.EndWorkToken.ThrowIfCancellationRequested();

        var table = new Table("desc", [
            new Column("Overload", typeof(int), 0),
            new Column("Path", typeof(string), 1),
            new Column("Kind", typeof(string), 2),
            new Column("Type", typeof(string), 3),
            new Column("Required", typeof(bool?), 4),
            new Column("Nullable", typeof(bool), 5),
            new Column("HasDefault", typeof(bool), 6),
            new Column("Default", typeof(string), 7),
            new Column("MaxDepth", typeof(int?), 8),
            new Column("MaxNodes", typeof(int?), 9),
            new Column("MaxStringBytes", typeof(long?), 10)
        ]);

        if (selected != null)
        {
            AddRows(table, selected, executionContext);
            return table;
        }

        var reflectedMethods = schema.GetRawConstructors(methodName, executionContext);
        var preferredMethods = TypedSourceBindingMetadata.PreferTypedSource(schema, methodName, reflectedMethods);
        var usesTypedRegistration = schema is ITypedSourceSchema typedSchema &&
                                     typedSchema.TryGetTypedSourceRegistration(methodName, out _);
        var methods = usesTypedRegistration
            ? preferredMethods.Select(static method => (Method: method, Signature: string.Empty)).ToArray()
            : preferredMethods
                .Select(static method => (Method: method, Signature: StructuralArgumentDescriptionFactory.CanonicalSignature(method)))
                .OrderBy(static item => item.Signature, StringComparer.Ordinal)
                .ThenBy(static item => item.Method.ConstructorInfo.OriginConstructor?.DeclaringType?.AssemblyQualifiedName,
                    StringComparer.Ordinal)
                .ThenBy(static item => item.Method.ConstructorInfo.OriginConstructor?.MetadataToken ?? 0)
                .ToArray();

        for (var overload = 0; overload < methods.Length; overload++)
        {
            executionContext.EndWorkToken.ThrowIfCancellationRequested();
            AddRows(table, StructuralArgumentDescriptionFactory.Create(methods[overload].Method, overload), executionContext);
        }

        return table;
    }

    private static void AddRows(
        Table table,
        IReadOnlyList<StructuralArgumentDescription> descriptions,
        SourceExecutionContext executionContext)
    {
        foreach (var description in descriptions)
        {
            executionContext.EndWorkToken.ThrowIfCancellationRequested();
            table.AddUnchecked(new DescriptionArgumentRow(description));
        }
    }
}
