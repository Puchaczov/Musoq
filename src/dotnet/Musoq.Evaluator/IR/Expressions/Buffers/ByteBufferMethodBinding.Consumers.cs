using System.Reflection;
using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Expressions;

internal static partial class ByteBufferMethodBinding
{
    private static ByteBufferCallableSemantics? DescribeEncoding(MethodInfo method) =>
        method.Name switch
        {
            nameof(LibraryBase.ToBase64) when Matches(method, typeof(string), typeof(byte[])) => new(
                ByteBufferCallableKind.Encode,
                ByteBufferEncoding.Base64
            ),
            nameof(LibraryBase.ToBase64)
                when Matches(method, typeof(string), typeof(byte[]), typeof(int), typeof(int))
                    || Matches(method, typeof(string), typeof(byte[]), typeof(int?), typeof(int?)) => new(
                ByteBufferCallableKind.Encode,
                ByteBufferEncoding.Base64,
                EncodingRange: ByteBufferEncodingRange.CheckedSlice
            ),
            nameof(LibraryBase.FromBase64) when Matches(method, typeof(byte[]), typeof(string)) => new(
                ByteBufferCallableKind.Decode,
                ByteBufferEncoding.Base64
            ),
            nameof(LibraryBase.ToHex) when Matches(method, typeof(string), typeof(byte[]), typeof(string)) => new(
                ByteBufferCallableKind.Encode,
                ByteBufferEncoding.Hex,
                DelimiterArgument: 1
            ),
            nameof(LibraryBase.FromHexToBytes) when Matches(method, typeof(byte[]), typeof(string)) => new(
                ByteBufferCallableKind.Decode,
                ByteBufferEncoding.Hex
            ),
            _ => null,
        };

    private static ByteBufferCallableSemantics? DescribeReader(MethodInfo method)
    {
        var width = method.Name switch
        {
            nameof(LibraryBase.FromBytesToBool) => 1,
            nameof(LibraryBase.FromBytesToChar)
            or nameof(LibraryBase.FromBytesToInt16)
            or nameof(LibraryBase.FromBytesToUInt16)
            or nameof(LibraryBase.FromBytesToHalf) => 2,
            nameof(LibraryBase.FromBytesToInt32)
            or nameof(LibraryBase.FromBytesToUInt32)
            or nameof(LibraryBase.FromBytesToFloat) => 4,
            nameof(LibraryBase.FromBytesToInt64)
            or nameof(LibraryBase.FromBytesToUInt64)
            or nameof(LibraryBase.FromBytesToDouble) => 8,
            _ => 0,
        };
        if (width == 0)
            return null;
        var result = Nullable.GetUnderlyingType(method.ReturnType);
        if (result is null || !result.IsValueType)
            return null;
        if (Matches(method, method.ReturnType, typeof(byte[])))
            return new(ByteBufferCallableKind.FixedWidthRead, ReadWidth: width);
        return Matches(method, method.ReturnType, typeof(byte[]), typeof(bool))
            ? new(ByteBufferCallableKind.FixedWidthRead, ReadWidth: width, PaddingArgument: 1)
            : null;
    }
}
