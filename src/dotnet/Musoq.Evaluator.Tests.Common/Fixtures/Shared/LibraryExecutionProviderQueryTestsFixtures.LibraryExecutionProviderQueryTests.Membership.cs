using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionProviderQueryTestsFixtures
{
    public sealed class MembershipRow(int? value, int? first, int? second)
    {
        public int? Value { get; } = value;
        public int? First { get; } = first;
        public int? Second { get; } = second;
        public List<string> Events { get; } = [];
    }

    public sealed class MembershipLibrary : LibraryBase
    {
        [BindableMethod]
        [NonDeterministic]
        public int? Record([InjectSpecificSource(typeof(MembershipRow))] MembershipRow row, int? value, string label)
        {
            row.Events.Add(label);
            return value;
        }
    }
}
