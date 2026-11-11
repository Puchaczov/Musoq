using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionProviderQueryTestsFixtures
{
    internal sealed class SecretSettingsResolver : ISourceRuntimeSettingsResolver
    {
        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request) =>
            new Dictionary<string, string> { ["TOKEN"] = "wave8-secret-token" };
    }

    internal struct DiagnosticColumnTypes : IColumnValueVisitor
    {
        public int Count;

        public void Visit<T>(int index, in T value)
        {
            var expectedType = index == 0 ? typeof(int) : typeof(string);
            var actualType = typeof(T);
            Assert.AreEqual(expectedType, actualType);
            Count++;
        }
    }

    internal struct SettingColumnTypes : IColumnValueVisitor
    {
        public int Count;
        public bool ExposedSecret;

        public void Visit<T>(int index, in T value)
        {
            var expectedType = index is 1 or 2 ? typeof(bool) : typeof(string);
            var actualType = typeof(T);
            Assert.AreEqual(expectedType, actualType);
            Count++;
            if (typeof(T) == typeof(string))
            {
                var copy = value;
                var text = global::System.Runtime.CompilerServices.Unsafe.As<T, string>(ref copy);
                ExposedSecret |= text.Contains("wave8-secret-token", StringComparison.Ordinal);
            }
        }
    }
}
