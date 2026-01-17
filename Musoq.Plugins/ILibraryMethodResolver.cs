using System;
using System.Reflection;

namespace Musoq.Plugins;

/// <summary>
///     Interface for resolving LibraryBase methods by name and parameter types.
///     Decouples the visitor from direct reflection on LibraryBase type.
///     Follows Dependency Inversion Principle (DIP) - high-level visitor depends on abstraction, not concrete LibraryBase.
/// </summary>
internal interface ILibraryMethodResolver
{
    /// <summary>
    ///     Resolves a method from LibraryBase by name and parameter types.
    /// </summary>
    /// <param name="methodName">The name of the method to resolve.</param>
    /// <param name="parameterTypes">The parameter types of the method.</param>
    /// <returns>MethodInfo for the resolved method.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the method is not found.</exception>
    MethodInfo ResolveMethod(string methodName, Type[] parameterTypes);
}