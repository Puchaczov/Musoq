using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionConcurrencyQueryTestsFixtures
{
    public sealed class ReceiverLifetimeLibrary : LibraryBase
    {
        private int _next;

        [BindableMethod, NonDeterministic]
        public int NextSerialValue() => checked(++_next);
    }
}
