using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Optimization;
using Musoq.Targets.Abstractions;

namespace Musoq.Evaluator.IR.Execution.Portability;

internal static class ExecutionPortableSymbolCatalog
{
    private static readonly IReadOnlyDictionary<Type, string> PrimitiveNames = new Dictionary<Type, string>
    {
        [typeof(bool)] = "bool",
        [typeof(byte)] = "uint8",
        [typeof(sbyte)] = "int8",
        [typeof(short)] = "int16",
        [typeof(ushort)] = "uint16",
        [typeof(int)] = "int32",
        [typeof(uint)] = "uint32",
        [typeof(long)] = "int64",
        [typeof(ulong)] = "uint64",
        [typeof(float)] = "float32",
        [typeof(double)] = "float64",
        [typeof(decimal)] = "decimal",
        [typeof(char)] = "char",
        [typeof(string)] = "string",
        [typeof(DateTime)] = "datetime",
        [typeof(DateTimeOffset)] = "datetimeoffset",
        [typeof(Guid)] = "guid",
        [typeof(TimeSpan)] = "timespan",
        [typeof(void)] = "void"
    };

    private static readonly IReadOnlyDictionary<Type, ExecutionPortableContainerDefinition> PortableContainerDefinitions = new Dictionary<Type, ExecutionPortableContainerDefinition>
    {
        [typeof(IEnumerable<>)] = Sequence("sequence", ExecutionPortableContainerBindingKind.Enumerable),
        [typeof(IReadOnlyCollection<>)] = Sequence("sequence", ExecutionPortableContainerBindingKind.ReadOnlyCollection),
        [typeof(IReadOnlyList<>)] = Sequence("sequence", ExecutionPortableContainerBindingKind.ReadOnlyList),
        [typeof(ICollection<>)] = List("list", ExecutionPortableContainerBindingKind.Collection),
        [typeof(IList<>)] = List("list", ExecutionPortableContainerBindingKind.ListInterface),
        [typeof(List<>)] = List("list", ExecutionPortableContainerBindingKind.List),
        [typeof(IReadOnlyDictionary<,>)] = Map("map", isMutable: false, ExecutionPortableContainerBindingKind.ReadOnlyDictionary),
        [typeof(IDictionary<,>)] = Map("map", isMutable: true, ExecutionPortableContainerBindingKind.DictionaryInterface),
        [typeof(Dictionary<,>)] = Map("map", isMutable: true, ExecutionPortableContainerBindingKind.Dictionary),
        [typeof(KeyValuePair<,>)] = Pair("pair", ExecutionPortableContainerBindingKind.KeyValuePair),
        [typeof(HashSet<>)] = Set("set", ExecutionPortableContainerBindingKind.HashSet)
    };

    private static readonly IReadOnlySet<Type> HostImportTypes = new HashSet<Type>
    {
        typeof(IQueryRunnable),
        typeof(ITableRunnable),
        typeof(ISchemaProvider),
        typeof(ISchema),
        typeof(ISchemaTable),
        typeof(ISchemaColumn),
        typeof(SchemaColumn),
        typeof(SourceExecutionPlan),
        typeof(SourcePlanRequest),
        typeof(SourcePlanResult),
        typeof(SourceIdentity),
        typeof(Table),
        typeof(Row),
        typeof(Key)
    };

    private static readonly IReadOnlyDictionary<Type, string> HostImportGenericDefinitions = new Dictionary<Type, string>
    {
        [typeof(IQueryRows<>)] = "Musoq.Evaluator.Runtime.IQueryRows",
        [typeof(ITableRowBatchSource<>)] = "Musoq.Evaluator.Runtime.ITableRowBatchSource",
        [typeof(TableRows<>)] = "Musoq.Evaluator.Runtime.TableRows",
        [typeof(QueryTableEnumerable<>)] = "Musoq.Evaluator.Runtime.QueryTableEnumerable",
        [typeof(QueryRowShardedEnumerable<>)] = "Musoq.Evaluator.Runtime.QueryRowShardedEnumerable"
    };

    public static bool TryGetPrimitiveName(Type type, out string primitiveName)
    {
        return PrimitiveNames.TryGetValue(type, out primitiveName!);
    }

    public static bool TryGetPortableContainer(
        Type genericDefinition,
        out ExecutionPortableContainerDefinition definition)
    {
        return PortableContainerDefinitions.TryGetValue(genericDefinition, out definition);
    }

    public static bool TryGetHostImportTypeReason(Type type, out string reason)
    {
        var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        if (HostImportTypes.Contains(type) || HostImportTypes.Contains(definition))
        {
            reason = "Known Musoq host runtime type catalog entry.";
            return true;
        }

        if (HostImportGenericDefinitions.TryGetValue(definition, out var genericName))
        {
            reason = $"Known Musoq host runtime generic type catalog entry '{genericName}'.";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    public static ExecutionPortableCallableKind ClassifyCallable(MethodInfo method, out string reason)
    {
        if (method.GetCustomAttributes(inherit: false).Any(static attribute => attribute is AggregateFunctionAttribute))
        {
            reason = "Known Musoq aggregate callable catalog entry.";
            return ExecutionPortableCallableKind.HostAggregate;
        }

        var declaringType = method.DeclaringType;
        if (declaringType != null && typeof(LibraryBase).IsAssignableFrom(declaringType))
        {
            reason = "Known Musoq plugin library callable catalog entry.";
            return ExecutionPortableCallableKind.HostPlugin;
        }

        reason = $"No portable callable catalog entry for CLR method '{declaringType?.FullName}.{method.Name}'.";
        return ExecutionPortableCallableKind.ClrMethod;
    }

    public static ExecutionIntrinsicCallableKind ClassifyIntrinsicCallable(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);

        return method.DeclaringType == typeof(LibraryBase) &&
               string.Equals(method.Name, nameof(LibraryBase.Coalesce), StringComparison.Ordinal)
            ? ExecutionIntrinsicCallableKind.Coalesce
            : ExecutionIntrinsicCallableKind.None;
    }

    private static ExecutionPortableContainerDefinition Sequence(
        string stableName,
        ExecutionPortableContainerBindingKind bindingKind) =>
        new(
            stableName,
            ExecutionPortableTypeKind.Sequence,
            new ExecutionPortableContainerContract(
                ExecutionPortableContainerKind.Sequence,
                IsOrdered: true,
                IsMutable: false,
                RequiresKeyEquality: false,
                RequiresKeyHashing: false,
                bindingKind));

    private static ExecutionPortableContainerDefinition List(
        string stableName,
        ExecutionPortableContainerBindingKind bindingKind) =>
        new(
            stableName,
            ExecutionPortableTypeKind.List,
            new ExecutionPortableContainerContract(
                ExecutionPortableContainerKind.List,
                IsOrdered: true,
                IsMutable: true,
                RequiresKeyEquality: false,
                RequiresKeyHashing: false,
                bindingKind));

    private static ExecutionPortableContainerDefinition Map(
        string stableName,
        bool isMutable,
        ExecutionPortableContainerBindingKind bindingKind) =>
        new(
            stableName,
            ExecutionPortableTypeKind.Map,
            new ExecutionPortableContainerContract(
                ExecutionPortableContainerKind.Map,
                IsOrdered: false,
                IsMutable: isMutable,
                RequiresKeyEquality: true,
                RequiresKeyHashing: true,
                bindingKind));

    private static ExecutionPortableContainerDefinition Set(
        string stableName,
        ExecutionPortableContainerBindingKind bindingKind) =>
        new(
            stableName,
            ExecutionPortableTypeKind.Set,
            new ExecutionPortableContainerContract(
                ExecutionPortableContainerKind.Set,
                IsOrdered: false,
                IsMutable: true,
                RequiresKeyEquality: true,
                RequiresKeyHashing: true,
                bindingKind));

    private static ExecutionPortableContainerDefinition Pair(
        string stableName,
        ExecutionPortableContainerBindingKind bindingKind) =>
        new(
            stableName,
            ExecutionPortableTypeKind.Pair,
            new ExecutionPortableContainerContract(
                ExecutionPortableContainerKind.Pair,
                IsOrdered: true,
                IsMutable: false,
                RequiresKeyEquality: false,
                RequiresKeyHashing: false,
                bindingKind));

    public readonly record struct ExecutionPortableContainerDefinition(
        string StableName,
        ExecutionPortableTypeKind TypeKind,
        ExecutionPortableContainerContract Contract);
}
