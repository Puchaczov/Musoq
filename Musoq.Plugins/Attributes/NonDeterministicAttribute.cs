using System;

namespace Musoq.Plugins.Attributes;

/// <summary>
/// Marks a method as non-deterministic, meaning it may return different values 
/// for the same inputs across different invocations.
/// Non-deterministic functions are never cached by the Common Subexpression Elimination (CSE) optimization.
/// Examples include NewId(), Rand(), Now(), etc.
/// </summary>
/// <remarks>
/// Apply this attribute to any bindable method that:
/// - Generates random values (e.g., Rand, NewId, Guid)
/// - Returns time-dependent values (e.g., Now, GetDate, UtcNow)
/// - Should produce a new value on each invocation even if called with same arguments
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public class NonDeterministicAttribute : Attribute;
