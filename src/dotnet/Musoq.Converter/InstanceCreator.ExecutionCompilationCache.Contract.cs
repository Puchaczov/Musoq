using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Musoq.Converter.Build;
using Musoq.Evaluator.IR.Physical;
using Musoq.Schema;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    private static string CreateSemanticExecutionContractFingerprint(
        BuildItems items,
        ISchemaProvider schemaProvider)
    {
        var facts = TargetArtifactSemanticFactsFactory.From(items);
        var builder = new StringBuilder();
        builder.Append(CompiledQueryArtifactSupport.ComputeSemanticShapeHash(facts, "<execution-cache>"));
        builder.AppendLine();
        builder.Append(PhysicalPlanPrinter.Print(items.PhysicalPlan ?? throw new InvalidOperationException(
            "Execution compilation cache requires a physical plan.")));
        builder.AppendLine();
        builder.Append(items.InterpreterSourceCode ?? string.Empty);
        builder.AppendLine();
        builder.Append(CreateProviderContractSignature(schemaProvider));
        builder.AppendLine();
        foreach (var type in items.AdditionalReferenceTypes
                     .Select(static type => type.AssemblyQualifiedName ?? type.FullName ?? type.Name)
                     .OrderBy(static name => name, StringComparer.Ordinal))
            builder.Append(type).AppendLine();

        return CompiledQueryArtifactSupport.ComputeHash(builder.ToString());
    }

    private static string CreateProviderContractSignature(ISchemaProvider schemaProvider)
    {
        var builder = new StringBuilder();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var field in GetDeclaredInstanceFields(schemaProvider.GetType())
                     .OrderBy(static field => field.DeclaringType?.FullName, StringComparer.Ordinal)
                     .ThenBy(static field => field.Name, StringComparer.Ordinal))
        {
            builder.Append(field.DeclaringType?.FullName).Append('.').Append(field.Name).Append('=');
            AppendProviderContractValue(builder, field.GetValue(schemaProvider), 1, visited);
            builder.Append(';');
        }

        return builder.ToString();
    }

    internal static string CreateSemanticProviderContractSignatureForCache(ISchemaProvider schemaProvider)
    {
        return CreateProviderContractSignature(schemaProvider);
    }

    private static void AppendProviderContractValue(
        StringBuilder builder,
        object? value,
        int depth,
        HashSet<object> visited)
    {
        if (value is null)
        {
            builder.Append("<null>");
            return;
        }

        var type = value.GetType();
        builder.Append(type.AssemblyQualifiedName ?? type.FullName ?? type.Name).Append(':');
        if (depth > 2 || !visited.Add(value))
        {
            builder.Append("<opaque>");
            return;
        }

        // This is only the coarse cache bucket.  Provider-owned scalar fields
        // and dictionary shapes are useful for separating source modes, while
        // nested schema instances contain mutable runtime caches and settings
        // that must be validated by the exact post-planning contract instead.
        if (depth > 1 &&
            value is not string &&
            value is not Type &&
            value is not Enum &&
            value is not bool &&
            value is not IFormattable &&
            value is not IDictionary &&
            value is not IEnumerable)
        {
            builder.Append("<opaque>");
            return;
        }

        // Mutable schema switches, such as source-runtime-setting declaration
        // modes, are intentionally checked by the exact planning contract.
        // They must not make the coarse bucket unstable between the planning
        // probe and the real compilation.
        if (depth > 1 && value is Enum)
        {
            builder.Append("<opaque>");
            return;
        }

        if (depth > 1 && value is IFormattable && value is not string)
        {
            builder.Append("<opaque>");
            return;
        }

        switch (value)
        {
            case string text:
                builder.Append(text);
                return;
            case Type representedType:
                builder.Append(representedType.AssemblyQualifiedName ?? representedType.FullName ?? representedType.Name);
                return;
            case Enum enumValue:
                builder.Append(enumValue);
                return;
            case bool boolean:
                builder.Append(boolean ? "true" : "false");
                return;
            case IFormattable formattable:
                builder.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                return;
            case IDictionary dictionary:
                foreach (var entry in ReadDictionaryEntries(dictionary))
                {
                    builder.Append("key=");
                    AppendProviderContractValue(builder, entry.Key, depth + 1, visited);
                    builder.Append("value-type=")
                        .Append(entry.Value?.GetType().AssemblyQualifiedName ?? "<null>")
                        .Append(';');
                }

                return;
            case IEnumerable:
                builder.Append("<sequence>");
                return;
        }

        foreach (var field in GetDeclaredInstanceFields(type)
                     .OrderBy(static field => field.DeclaringType?.FullName, StringComparer.Ordinal)
                     .ThenBy(static field => field.Name, StringComparer.Ordinal))
        {
            builder.Append(field.Name).Append('=');
            AppendProviderContractValue(builder, field.GetValue(value), depth + 1, visited);
            builder.Append(';');
        }
    }

    private static IEnumerable<FieldInfo> GetDeclaredInstanceFields(Type type)
    {
        return type.GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.DeclaredOnly);
    }

    private static IEnumerable<(object? Key, object? Value)> ReadDictionaryEntries(IDictionary dictionary)
    {
        var entries = new List<(object? Key, object? Value)>();
        foreach (var item in (IEnumerable)dictionary)
        {
            if (item is DictionaryEntry dictionaryEntry)
            {
                entries.Add((dictionaryEntry.Key, dictionaryEntry.Value));
                continue;
            }

            var itemType = item.GetType();
            entries.Add((
                itemType.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public)?.GetValue(item),
                itemType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public)?.GetValue(item)));
        }

        entries.Sort(static (left, right) => StringComparer.Ordinal.Compare(
            left.Key?.ToString(),
            right.Key?.ToString()));
        return entries;
    }
}
