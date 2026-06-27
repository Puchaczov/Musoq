using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

public partial class NoOpExpressionVisitorTests
{
    [TestMethod]
    public void NoOpExpressionVisitor_ShouldExposeEveryVisitOverloadFromExpressionVisitor()
    {
        var requiredNodeTypes = typeof(IExpressionVisitor)
            .GetMethods()
            .Where(static method => method.Name == nameof(IExpressionVisitor.Visit))
            .Select(static method => method.GetParameters())
            .Where(static parameters => parameters.Length == 1)
            .Select(static parameters => parameters[0].ParameterType)
            .Where(static parameterType => typeof(Node).IsAssignableFrom(parameterType))
            .Distinct()
            .ToArray();

        var implementedNodeTypes = typeof(NoOpExpressionVisitor)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(static method => method.Name == nameof(IExpressionVisitor.Visit))
            .Select(static method => method.GetParameters())
            .Where(static parameters => parameters.Length == 1)
            .Select(static parameters => parameters[0].ParameterType)
            .Where(static parameterType => typeof(Node).IsAssignableFrom(parameterType))
            .Distinct()
            .ToHashSet();

        var missing = requiredNodeTypes
            .Where(type => !implementedNodeTypes.Contains(type))
            .Select(static type => type.Name)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            missing,
            "NoOpExpressionVisitor should expose every IExpressionVisitor Visit overload: " +
            string.Join(", ", missing));
    }
}
