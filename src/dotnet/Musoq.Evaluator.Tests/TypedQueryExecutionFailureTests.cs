using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class TypedQueryExecutionFailureTests
{
    [TestMethod]
    public void Run_WhenTypedRowsRaiseUnexpectedFailure_ShouldExposeSafeExecutionDiagnostic()
    {
        var query = new CompiledTypedQuery<int>(new ThrowingTypedRunnable());

        var exception = Assert.Throws<QueryExecutionException>(() => query.Run().ToArray());

        Assert.AreEqual(DiagnosticCode.MQ9002_InternalExecutionError, exception.Envelope!.Code);
        Assert.IsInstanceOfType<InvalidOperationException>(exception.InnerException);
        Assert.IsFalse(exception.FormatText().Contains("private typed detail", StringComparison.Ordinal));
    }

    private sealed class ThrowingTypedRunnable : ITypedRunnable<int>
    {
        public ISchemaProvider Provider { get; set; } = null!;

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; } =
            new Dictionary<string, IReadOnlyDictionary<string, string>>();

        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; } =
            new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>();

        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; } =
            new Dictionary<string, SourceExecutionPlan>();

        public ILogger Logger { get; set; } = NullLogger.Instance;

        #pragma warning disable CS0067
        public event QueryPhaseEventHandler? PhaseChanged;

        public event DataSourceEventHandler? DataSourceProgress;
        #pragma warning restore CS0067

        public IEnumerable<int> Run(TypedQueryRunOptions options)
        {
            return ThrowingRows();
        }

        public IEnumerable<int> Run(CancellationToken token)
        {
            return ThrowingRows();
        }

        private static IEnumerable<int> ThrowingRows()
        {
            yield return 1;
            throw new InvalidOperationException("private typed detail");
        }
    }
}
