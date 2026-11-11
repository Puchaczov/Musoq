using System.Collections.Generic;
using System.Reflection;
using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Expressions;

/// <summary>
/// Inspects the already-bound declaration, overload and closed generic arguments.
/// No member discovery or invocation is performed here.
/// </summary>
internal static partial class ByteBufferMethodBinding
{
    internal static ByteBufferCallableSemantics? Describe(MethodInfo method)
    {
        if (method.DeclaringType != typeof(LibraryBase) || method.IsStatic)
            return null;

        if (method.IsGenericMethod)
            return DescribeByteSequence(method);

        return DescribeEncoding(method) ?? DescribeReader(method);
    }

    private static ByteBufferCallableSemantics? DescribeByteSequence(MethodInfo method)
    {
        if (
            !method.IsConstructedGenericMethod
            || method.GetGenericArguments() is not [var element]
            || element != typeof(byte)
        )
            return null;

        var sequence = typeof(IEnumerable<byte>);
        return method.Name switch
        {
            nameof(LibraryBase.Skip) when Matches(method, sequence, sequence, typeof(int)) => new(
                ByteBufferCallableKind.Skip
            ),
            nameof(LibraryBase.Take) when Matches(method, sequence, sequence, typeof(int)) => new(
                ByteBufferCallableKind.Take
            ),
            nameof(LibraryBase.SkipAndTake) when Matches(method, sequence, sequence, typeof(int), typeof(int)) => new(
                ByteBufferCallableKind.SkipAndTake
            ),
            nameof(LibraryBase.EnumerableToArray) when Matches(method, typeof(byte[]), sequence) => new(
                ByteBufferCallableKind.Materialize
            ),
            nameof(LibraryBase.MergeArrays) when Matches(method, typeof(byte[]), typeof(byte[][])) => new(
                ByteBufferCallableKind.Concatenate,
                ArgumentStorage: ByteBufferArgumentStorage.Array
            ),
            nameof(LibraryBase.MergeArrays) when Matches(method, typeof(byte[]), typeof(ReadOnlySpan<byte[]>)) => new(
                ByteBufferCallableKind.Concatenate,
                ArgumentStorage: ByteBufferArgumentStorage.ReadOnlySpan
            ),
            nameof(LibraryBase.Length)
                when Matches(method, typeof(int?), sequence) || Matches(method, typeof(int?), typeof(byte[])) => new(
                ByteBufferCallableKind.Length
            ),
            _ => null,
        };
    }

    private static bool Matches(MethodInfo method, Type result, params Type[] arguments)
    {
        if (method.ReturnType != result)
            return false;
        var parameters = method.GetParameters();
        if (parameters.Length != arguments.Length)
            return false;
        for (var index = 0; index < parameters.Length; index++)
            if (parameters[index].ParameterType != arguments[index])
                return false;
        return true;
    }
}
