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
    public void Constructor_WhenParametersAreProvided_ShouldSnapshotValues()
    {
        var parameters = new Dictionary<string, object?> { ["name"] = "Ada" };
        var context = new QueryRunContext(CancellationToken.None, parameters);

        parameters["name"] = "Grace";
        parameters["extra"] = 42;

        Assert.AreEqual("Ada", context.RuntimeParameters["name"]);
        Assert.IsFalse(context.RuntimeParameters.ContainsKey("extra"));
    }

    [TestMethod]
    public void Parameters_WhenCaptured_ShouldNotExposeMutableDictionary()
    {
        var parameters = new Dictionary<string, object?> { ["name"] = "Ada" };
        var context = new QueryRunContext(CancellationToken.None, parameters);

        Assert.AreNotSame(parameters, context.RuntimeParameters);
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, object?>)context.RuntimeParameters)["name"] = "Grace");
    }

    [TestMethod]
    public void TypedQueryRunOptions_WhenObjectInitializerParametersAreMutated_ShouldKeepSnapshot()
    {
        var parameters = new Dictionary<string, object?> { ["name"] = "Ada" };
        var options = new TypedQueryRunOptions { Parameters = parameters };

        parameters["name"] = "Grace";

        Assert.IsNotNull(options.Parameters);
        Assert.AreEqual("Ada", options.Parameters["name"]);
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, object?>)options.Parameters)["name"] = "Grace");
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
        var options = new TypedQueryRunOptions(CancellationToken.None, parameters);
        var sender = new object();
        var context = QueryRunContext.Capture(options, sender, "query");

        parameters["limit"] = 20;

        Assert.AreSame(sender, context.Sender);
        Assert.AreEqual("query", context.QueryId);
        Assert.AreEqual(10, context.RuntimeParameters["limit"]);
    }

    [TestMethod]
    public void QueryRunContext_ShouldNotExposeMutableParametersProperty()
    {
        var property = typeof(QueryRunContext).GetProperty("Parameters");

        Assert.IsNull(property);
    }
}
