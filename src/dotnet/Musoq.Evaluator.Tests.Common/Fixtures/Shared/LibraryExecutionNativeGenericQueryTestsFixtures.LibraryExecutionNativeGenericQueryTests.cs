using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionNativeGenericQueryTestsFixtures
{
    internal struct JsonColumnVisitor : Musoq.Schema.IColumnValueVisitor
    {
        internal Type? Type;
        internal string? Json;

        public void Visit<T>(int columnIndex, in T value)
        {
            Assert.AreEqual(0, columnIndex);
            Type = typeof(T);
            Json = System.Text.Json.JsonSerializer.Serialize(value);
        }
    }

    public sealed record Input(int Position)
    {
        public string Label => "input";
    }

    public sealed class GenericLibrary : LibraryBase
    {
        [BindableMethod]
        public int NativeLength<T>(IReadOnlyList<T> values) => values.Count;

        [BindableMethod]
        public IReadOnlyList<T> NativeEcho<T>(IReadOnlyList<T> values) => values;

        [BindableMethod]
        public int ReferenceElements<T>(IReadOnlyList<T> values)
            where T : class => values.Count;

        [BindableMethod]
        public int StructElements<T>(IReadOnlyList<T> values)
            where T : struct => values.Count;

        [BindableMethod]
        public int UnmanagedElements<T>(IReadOnlyList<T> values)
            where T : unmanaged => values.Count;

        [BindableMethod]
        public int ConstructorElements<T>(IReadOnlyList<T> values)
            where T : new() => values.Count;

        [BindableMethod]
        public IReadOnlyList<T> StructEcho<T>(IReadOnlyList<T> values)
            where T : struct => values;

        [BindableMethod]
        public int NeedsStruct<T>(T value)
            where T : struct => 1;

        [BindableMethod]
        public int ReferenceValue<T>(T value)
            where T : class => 1;
    }
}
