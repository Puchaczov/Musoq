using System;

namespace Musoq.Schema.Helpers;

/// <summary>
/// Utility class for normalizing method names to enable case-insensitive method resolution.
/// </summary>
public static class MethodNameNormalizer
{
    /// <summary>
    /// Normalizes a method name by converting to lowercase and removing underscores.
    /// This enables case-insensitive method resolution where MyMethod, mymethod, 
    /// my_method, and MY_METHOD all resolve to the same normalized form.
    /// </summary>
    /// <param name="methodName">The method name to normalize.</param>
    /// <returns>The normalized method name (lowercase, no underscores).</returns>
    /// <exception cref="ArgumentNullException">Thrown when methodName is null.</exception>
    /// <exception cref="ArgumentException">Thrown when methodName is empty or whitespace.</exception>
    public static string Normalize(string methodName)
    {
        if (methodName == null)
            throw new ArgumentNullException(nameof(methodName));
        
        if (string.IsNullOrWhiteSpace(methodName))
            throw new ArgumentException("Method name cannot be empty or whitespace.", nameof(methodName));
            
        return methodName.ToLowerInvariant().Replace("_", "");
    }
}