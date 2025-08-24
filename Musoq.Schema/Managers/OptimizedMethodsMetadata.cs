using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Schema.Compilation;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Helpers;

namespace Musoq.Schema.Managers;

/// <summary>
/// Optimized methods metadata manager that uses compiled expression trees for method resolution.
/// Provides significant performance improvements over reflection-based method calls.
/// Falls back to original MethodsMetadata when compilation caching is disabled.
/// </summary>
public class OptimizedMethodsMetadata : MethodsMetadata
{
    /// <summary>
    /// Enhanced method result that includes both the MethodInfo and optional compiled delegate.
    /// </summary>
    public class OptimizedMethodResult
    {
        public MethodInfo Method { get; init; }
        public Delegate CompiledDelegate { get; init; }
        public bool IsCompiled => CompiledDelegate != null;
    }

    /// <summary>
    /// Gets an optimized method that fits name and types of arguments passed.
    /// Returns both the MethodInfo and optionally a compiled delegate for faster invocation.
    /// </summary>
    /// <param name="name">Function name</param>
    /// <param name="methodArgs">Types of method arguments</param>
    /// <param name="entityType">Type of entity.</param>
    /// <returns>Optimized method result with MethodInfo and optional compiled delegate.</returns>
    public OptimizedMethodResult GetOptimizedMethod(string name, Type[] methodArgs, Type entityType)
    {
        // Get the basic method info using existing logic
        var method = GetMethod(name, methodArgs, entityType);
        
        // If compilation caching is disabled, return method without delegate
        if (!SchemaMethodCompilationCacheManager.IsEnabled)
        {
            return new OptimizedMethodResult
            {
                Method = method,
                CompiledDelegate = null
            };
        }

        try
        {
            // Attempt to get or compile the method delegate
            var compiledDelegate = GetCompiledDelegate(method, methodArgs, entityType);
            
            return new OptimizedMethodResult
            {
                Method = method,
                CompiledDelegate = compiledDelegate
            };
        }
        catch (Exception)
        {
            // If compilation fails, fall back to reflection-based method
            return new OptimizedMethodResult
            {
                Method = method,
                CompiledDelegate = null
            };
        }
    }

    /// <summary>
    /// Tries to get an optimized method if it exists.
    /// </summary>
    /// <param name="name">The method name.</param>
    /// <param name="methodArgs">The types of arguments methods contains.</param>
    /// <param name="entityType">The type of entity.</param>
    /// <param name="result">Optimized method result of founded method.</param>
    /// <returns>True if method exists, otherwise false.</returns>
    public bool TryGetOptimizedMethod(string name, Type[] methodArgs, Type entityType, out OptimizedMethodResult result)
    {
        try
        {
            if (!TryGetMethod(name, methodArgs, entityType, out var method))
            {
                result = null;
                return false;
            }

            // If compilation caching is disabled, return method without delegate
            if (!SchemaMethodCompilationCacheManager.IsEnabled)
            {
                result = new OptimizedMethodResult
                {
                    Method = method,
                    CompiledDelegate = null
                };
                return true;
            }

            try
            {
                // Attempt to get or compile the method delegate
                var compiledDelegate = GetCompiledDelegate(method, methodArgs, entityType);
                
                result = new OptimizedMethodResult
                {
                    Method = method,
                    CompiledDelegate = compiledDelegate
                };
                return true;
            }
            catch (Exception)
            {
                // If compilation fails, fall back to reflection-based method
                result = new OptimizedMethodResult
                {
                    Method = method,
                    CompiledDelegate = null
                };
                return true;
            }
        }
        catch (Exception)
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Gets or compiles a delegate for the specified method.
    /// </summary>
    /// <param name="method">The method to compile</param>
    /// <param name="methodArgs">The argument types for the method</param>
    /// <param name="entityType">The entity type context</param>
    /// <returns>A compiled delegate for the method</returns>
    private static Delegate GetCompiledDelegate(MethodInfo method, Type[] methodArgs, Type entityType)
    {
        // Build the parameter types for compilation
        var parameterTypes = BuildParameterTypesForCompilation(method, methodArgs, entityType);
        
        // Get or compile the method
        return SchemaMethodCompilationCacheManager.Instance.GetOrCompileMethod(method, parameterTypes);
    }

    /// <summary>
    /// Builds the complete parameter type array for method compilation.
    /// This includes injected parameters (like source, group, etc.) and user arguments.
    /// </summary>
    /// <param name="method">The method to analyze</param>
    /// <param name="methodArgs">The user-provided argument types</param>
    /// <param name="entityType">The entity type context</param>
    /// <returns>Complete array of parameter types for compilation</returns>
    private static Type[] BuildParameterTypesForCompilation(MethodInfo method, Type[] methodArgs, Type entityType)
    {
        var parameters = method.GetParameters();
        var compilationTypes = new List<Type>();

        // Handle instance methods - add the library instance type
        if (!method.IsStatic)
        {
            compilationTypes.Add(method.DeclaringType ?? typeof(object));
        }

        // Process each parameter to determine what needs to be injected
        var userArgIndex = 0;
        foreach (var parameter in parameters)
        {
            var parameterType = parameter.ParameterType;
            var attributes = parameter.GetCustomAttributes().ToArray();

            // Check for injection attributes
            var hasInjectSource = attributes.Any(attr => attr.GetType().Name == "InjectSourceAttribute");
            var hasInjectGroup = attributes.Any(attr => attr.GetType().Name == "InjectGroupAttribute");
            var hasInjectQueryStats = attributes.Any(attr => attr.GetType().Name == "InjectQueryStatsAttribute");
            var hasInjectSpecificSource = attributes.Any(attr => attr.GetType().Name == "InjectSpecificSourceAttribute");

            if (hasInjectSource)
            {
                // Inject the entity type
                compilationTypes.Add(entityType ?? typeof(object));
            }
            else if (hasInjectGroup)
            {
                // Inject group type - typically Group from Musoq.Plugins
                compilationTypes.Add(parameterType);
            }
            else if (hasInjectQueryStats)
            {
                // Inject query stats type
                compilationTypes.Add(parameterType);
            }
            else if (hasInjectSpecificSource)
            {
                // Inject specific source type
                compilationTypes.Add(parameterType);
            }
            else
            {
                // Regular user parameter
                if (userArgIndex < methodArgs.Length)
                {
                    compilationTypes.Add(methodArgs[userArgIndex]);
                    userArgIndex++;
                }
                else if (parameter.HasDefaultValue)
                {
                    // Optional parameter with default value
                    compilationTypes.Add(parameterType);
                }
                else
                {
                    // This shouldn't happen if method resolution was correct
                    throw new InvalidOperationException($"Parameter mismatch for method {method.Name}");
                }
            }
        }

        return compilationTypes.ToArray();
    }

    /// <summary>
    /// Gets cache statistics for the compilation cache.
    /// </summary>
    /// <returns>Cache statistics including performance metrics</returns>
    public static SchemaMethodCompilationCache.CacheStatistics GetCompilationCacheStatistics()
    {
        return SchemaMethodCompilationCacheManager.GetStatistics();
    }

    /// <summary>
    /// Clears the compilation cache.
    /// Useful for testing or memory management.
    /// </summary>
    public static void ClearCompilationCache()
    {
        SchemaMethodCompilationCacheManager.ClearCache();
    }

    /// <summary>
    /// Invokes a compiled method delegate with the provided arguments.
    /// Handles both Action and Func delegates properly.
    /// </summary>
    /// <param name="compiledDelegate">The compiled delegate to invoke</param>
    /// <param name="args">The arguments to pass to the method</param>
    /// <returns>The result of the method invocation, or null for void methods</returns>
    public static object InvokeCompiledMethod(Delegate compiledDelegate, params object[] args)
    {
        if (compiledDelegate == null)
            throw new ArgumentNullException(nameof(compiledDelegate));

        try
        {
            // Use DynamicInvoke for flexibility with different delegate types
            return compiledDelegate.DynamicInvoke(args);
        }
        catch (TargetParameterCountException ex)
        {
            throw new ArgumentException($"Parameter count mismatch when invoking compiled method. Expected {compiledDelegate.Method.GetParameters().Length}, got {args?.Length ?? 0}", ex);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Parameter type mismatch when invoking compiled method: {ex.Message}", ex);
        }
        catch (TargetInvocationException ex)
        {
            // Unwrap the actual exception
            throw ex.InnerException ?? ex;
        }
    }
}