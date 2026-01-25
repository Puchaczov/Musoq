using System;
using System.Reflection;

namespace Musoq.Plugins;

/// <summary>
///     Default implementation of ILibraryMethodResolver that uses reflection on LibraryBase.
///     Can be replaced with alternative implementations (e.g., cached, precompiled, mock for testing).
/// </summary>
internal class LibraryMethodResolver : ILibraryMethodResolver
{
    private static readonly Type LibraryBaseType = typeof(LibraryBase);

    /// <inheritdoc />
    public MethodInfo ResolveMethod(string methodName, Type[] parameterTypes)
    {
        var method = LibraryBaseType.GetMethod(methodName, parameterTypes);

        if (method == null)
        {
            var paramList = string.Join(", ", Array.ConvertAll(parameterTypes, t => t.Name));
            throw new InvalidOperationException(
                $"Method {methodName}({paramList}) not found in {LibraryBaseType.Name}");
        }

        return method;
    }
}
