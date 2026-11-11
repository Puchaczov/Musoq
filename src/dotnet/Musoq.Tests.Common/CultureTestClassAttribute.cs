using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Tests.Common;

/// <summary>Runs each original MSTest invocation under the repository's intended test culture.</summary>
public sealed class CultureTestClassAttribute : TestClassAttribute
{
    public override TestMethodAttribute GetTestMethodAttribute(TestMethodAttribute testMethodAttribute)
    {
        ArgumentNullException.ThrowIfNull(testMethodAttribute);
        return new ScopedTestMethodAttribute(testMethodAttribute);
    }

    private sealed class ScopedTestMethodAttribute : TestMethodAttribute
    {
        private readonly TestMethodAttribute _original;

        public ScopedTestMethodAttribute(
            TestMethodAttribute original,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0
        )
            : base(original.DeclaringFilePath ?? filePath, original.DeclaringLineNumber ?? lineNumber)
        {
            _original = original;
            DisplayName = original.DisplayName;
            UnfoldingStrategy = original.UnfoldingStrategy;
        }

        public override async Task<TestResult[]> ExecuteAsync(ITestMethod testMethod)
        {
            using var scope = new CultureScope(Culture.DefaultCulture);
            return await _original.ExecuteAsync(testMethod).ConfigureAwait(false);
        }
    }
}
