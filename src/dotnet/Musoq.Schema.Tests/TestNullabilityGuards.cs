using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Schema.Tests;

internal static class TestNullabilityGuards
{
    public static T Require<T>(T? value, string description) where T : class
    {
        return value ?? throw new AssertFailedException($"Expected {description} to be non-null.");
    }
}
