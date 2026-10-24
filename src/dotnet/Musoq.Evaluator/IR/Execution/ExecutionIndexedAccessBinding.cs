using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Helpers;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

internal static class ExecutionIndexedAccessBinding
{
    public static ExecutionExpression Bind(ExecutionArrayAccess access)
    {
        ArgumentNullException.ThrowIfNull(access);
        if (BindGeneratedArray(access) is { } generated)
            return generated;
        if (access.Array is ExecutionScriptParameterRead or ExecutionScriptVariableRead ||
            ExecutionIrAnalysis.FlattenExpressions(access.Array).Any(IsDynamicRead))
            return access;
        var collection = access.Array.ReturnType.ResolveClrType();
        if (ConcreteValueShape.FindUnresolvedValueType(collection) != null)
            return access;
        var method = Resolve(collection, access.Index.ReturnType.ResolveClrType());
        if (method == null)
            return access;
        return new ExecutionMethodCall(ExecutionClrBindingFactory.FromClr(method), [access.Array, access.Index],
            null, access.ReturnType, null);
    }

    private static ExecutionMethodCall? BindGeneratedArray(ExecutionArrayAccess access)
    {
        if (access.Array is not ExecutionFieldRead { GeneratedTypeName: { } typeName } ||
            !typeName.EndsWith("[]", StringComparison.Ordinal) ||
            access.Array.ReturnType.ResolveClrType() != typeof(object[]) ||
            access.Index.ReturnType.ResolveClrType() != typeof(int))
            return null;

        var method = typeof(SafeArrayAccess).GetMethod(nameof(SafeArrayAccess.GetArrayElement))!
            .MakeGenericMethod(typeof(object));
        ExecutionExpression[] arguments = [access.Array, access.Index];
        var bindings = GeneratedScalarGenericBinding.Resolve(method, arguments);
        return new ExecutionMethodCall(ExecutionClrBindingFactory.FromClr(method), arguments, null, access.ReturnType, null)
        {
            GeneratedGenericArguments = bindings,
            GeneratedResultTypeName = GeneratedScalarGenericBinding.ResolveResult(method, bindings)
        };
    }

    private static bool IsDynamicRead(ExecutionExpression expression)
        => expression is ExecutionMemberRead { IsDynamic: true } or
            ExecutionFieldRead { AccessStrategy: RuntimeDynamicMemberAccess or RuntimeDynamicMemberPathAccess };

    private static MethodInfo? Resolve(Type collection, Type index)
    {
        if (collection == typeof(string) && IsIntegerIndex(index))
            return typeof(SafeArrayAccess).GetMethod(nameof(SafeArrayAccess.GetTypedStringCharacter));
        var dictionary = FindContract(collection, typeof(IReadOnlyDictionary<,>));
        if (dictionary != null)
            return ResolveDictionary(new DictionaryContract(collection, dictionary, index, DictionaryAccess.ReadOnly));
        dictionary = FindContract(collection, typeof(IDictionary<,>));
        if (dictionary != null)
            return ResolveDictionary(new DictionaryContract(collection, dictionary, index, DictionaryAccess.Mutable));
        return ResolveList(collection, index);
    }

    private static MethodInfo? ResolveList(Type collection, Type index)
    {
        if (!IsIntegerIndex(index))
            return null;
        var list = FindContract(collection, typeof(IReadOnlyList<>));
        if (list != null)
            return Close(nameof(SafeArrayAccess.GetTypedReadOnlyListElement), collection, list.GetGenericArguments()[0]);
        list = FindContract(collection, typeof(IList<>));
        if (list != null)
            return Close(nameof(SafeArrayAccess.GetTypedListElement), collection, list.GetGenericArguments()[0]);
        return null;
    }

    private static MethodInfo? ResolveDictionary(DictionaryContract dictionary)
    {
        var arguments = dictionary.Contract.GetGenericArguments();
        var key = arguments[0];
        var nullable = Nullable.GetUnderlyingType(dictionary.Index) == key;
        if (!nullable && !key.IsAssignableFrom(dictionary.Index))
            return null;
        var name = (dictionary.Access, nullable) switch
        {
            (DictionaryAccess.ReadOnly, false) => nameof(SafeArrayAccess.GetTypedDictionaryValue),
            (DictionaryAccess.Mutable, false) => nameof(SafeArrayAccess.GetTypedMutableDictionaryValue),
            (DictionaryAccess.ReadOnly, true) => nameof(SafeArrayAccess.GetTypedNullableDictionaryValue),
            (DictionaryAccess.Mutable, true) => nameof(SafeArrayAccess.GetTypedNullableMutableDictionaryValue),
            _ => throw new InvalidOperationException("Unknown indexed dictionary contract.")
        };
        return Close(name, dictionary.Collection, key, arguments[1]);
    }

    private static bool IsIntegerIndex(Type type) => type == typeof(int) || type == typeof(int?);

    private static Type? FindContract(Type type, Type definition)
        => type.GetInterfaces().Prepend(type).FirstOrDefault(candidate =>
            candidate.IsGenericType && candidate.GetGenericTypeDefinition() == definition);

    private static MethodInfo Close(string name, params Type[] arguments)
        => typeof(SafeArrayAccess).GetMethod(name)!.MakeGenericMethod(arguments);

    private enum DictionaryAccess { ReadOnly, Mutable }
    private sealed record DictionaryContract(Type Collection, Type Contract, Type Index, DictionaryAccess Access);
}
