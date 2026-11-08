using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class QueryRunContextTests
{
    [TestMethod]
    public void Constructor_WhenTypedParametersAreCaptured_ShouldOwnTheOriginalValues()
    {
        var values = new TypedParameterValues();
        values.Set("name", "Ada");
        var context = new QueryRunContext(CancellationToken.None, values.Capture());
        values.Set("name", "Grace");
        values.Set("extra", 42);
        Assert.AreEqual("Ada", context.TypedParameters.GetRequired<string>("name"));
        Assert.IsFalse(context.TypedParameters.Contains("extra"));
    }

    [TestMethod]
    public void TypedOptions_WhenValuesAreMutated_ShouldKeepTheCapturedSlots()
    {
        var values = new TypedParameterValues();
        values.Set("name", "Ada");
        var options = new TypedQueryRunOptions { TypedParameters = values.Capture() };
        values.Set("name", "Grace");
        Assert.AreEqual("Ada", options.TypedParameters.GetRequired<string>("name"));
        Assert.AreEqual(1, options.TypedParameters.Count);
        Assert.IsNull(typeof(QueryRunContext).GetProperty("RuntimeParameters"));
        Assert.IsNull(typeof(TypedQueryRunOptions).GetProperty("Parameters"));
    }

    [TestMethod]
    public void NotifyCallbacks_WhenHandlersAreProvided_ShouldUseCapturedSenderAndArgs()
    {
        var sender = new object();
        object? phaseSender = null;
        QueryPhaseEventArgs? phaseArgs = null;
        object? progressSender = null;
        DataSourceEventArgs? progressArgs = null;
        var dataSourceArgs = new DataSourceEventArgs("query-1", "source", DataSourcePhase.RowsKnown, 10);
        var context = new QueryRunContext(
            CancellationToken.None,
            phaseChanged: (callbackSender, args) =>
            {
                phaseSender = callbackSender;
                phaseArgs = args;
            },
            dataSourceProgress: (callbackSender, args) =>
            {
                progressSender = callbackSender;
                progressArgs = args;
            },
            sender: sender,
            queryId: "query-1");

        context.NotifyPhaseChanged(QueryPhase.Select);
        context.NotifyDataSourceProgress(new object(), dataSourceArgs);

        Assert.AreSame(sender, phaseSender);
        Assert.IsNotNull(phaseArgs);
        Assert.AreEqual("query-1", phaseArgs.QueryId);
        Assert.AreEqual(QueryPhase.Select, phaseArgs.Phase);
        Assert.AreSame(sender, progressSender);
        Assert.AreSame(dataSourceArgs, progressArgs);
    }

    [TestMethod]
    public void CancellationToken_WhenCancelled_ShouldBePropagated()
    {
        using var cancellation = new CancellationTokenSource();
        var context = new QueryRunContext(cancellation.Token);

        cancellation.Cancel();

        Assert.AreEqual(cancellation.Token, context.CancellationToken);
        Assert.Throws<OperationCanceledException>(context.ThrowIfCancellationRequested);
    }

    [TestMethod]
    public void NotifyCallbacks_WhenHandlersAreNull_ShouldNotThrow()
    {
        var context = new QueryRunContext(CancellationToken.None);

        context.NotifyPhaseChanged(QueryPhase.Begin);
        context.NotifyPhaseChanged("query", QueryPhase.End);
        context.NotifyDataSourceProgress(new DataSourceEventArgs("query", "source", DataSourcePhase.Begin));
        context.NotifyDataSourceProgress(new object(), new DataSourceEventArgs("query", "source", DataSourcePhase.End));
    }

    [TestMethod]
    public void Capture_WhenOptionsAreProvided_ShouldSnapshotOptions()
    {
        var parameters = new Dictionary<string, object?> { ["limit"] = 10 };
        var options = new TypedQueryRunOptions(CancellationToken.None, global::Musoq.Tests.Common.NativeParameterTestInputs.Capture<global::Musoq.Evaluator.TypedParameterSnapshot>(parameters));
        var sender = new object();
        var context = QueryRunContext.Capture(options, sender, "query");

        parameters["limit"] = 20;

        Assert.AreSame(sender, context.Sender);
        Assert.AreEqual("query", context.QueryId);
        Assert.AreEqual(10, context.TypedParameters.GetRequired<int>("limit"));
    }

    [TestMethod]
    public void QueryRunContext_ShouldNotExposeMutableParametersProperty()
    {
        var property = typeof(QueryRunContext).GetProperty("Parameters");

        Assert.IsNull(property);
    }
}
