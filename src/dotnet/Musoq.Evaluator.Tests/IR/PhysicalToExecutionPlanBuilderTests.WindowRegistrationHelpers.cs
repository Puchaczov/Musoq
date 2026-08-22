using System;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{

    private static WindowRegistration CreateRowNumberRegistration(OrderField orderField)
    {
        return CreateRowNumberRegistration([orderField], []);
    }

    private static WindowRegistration CreateRowNumberRegistration(OrderField[] orderFields)
    {
        return CreateRowNumberRegistration(orderFields, []);
    }

    private static WindowRegistration CreateRowNumberRegistration(
        OrderField orderField,
        IrExpression[] partitionKeys)
    {
        return CreateRowNumberRegistration([orderField], partitionKeys);
    }

    private static WindowRegistration CreateRowNumberRegistration(
        OrderField[] orderFields,
        IrExpression[] partitionKeys)
    {
        return CreateRankingRegistration(nameof(LibraryBase.WindowRowNumber), "RowNumber", orderFields, partitionKeys);
    }

    private static WindowRegistration CreateRankRegistration(params OrderField[] orderFields)
    {
        return CreateRankingRegistration(nameof(LibraryBase.WindowRank), "Rank", orderFields, []);
    }

    private static WindowRegistration CreateDenseRankRegistration(
        OrderField[] orderFields,
        IrExpression[] partitionKeys)
    {
        return CreateRankingRegistration(nameof(LibraryBase.WindowDenseRank), "DenseRank", orderFields, partitionKeys);
    }

    private static WindowRegistration CreatePercentRankRegistration(
        OrderField[] orderFields,
        IrExpression[] partitionKeys,
        int windowIndex = 0)
    {
        return CreateRankingRegistration(
            nameof(LibraryBase.WindowPercentRank),
            "PercentRank",
            orderFields,
            partitionKeys,
            typeof(double),
            windowIndex);
    }

    private static WindowRegistration CreateCumeDistRegistration(
        OrderField[] orderFields,
        IrExpression[] partitionKeys,
        int windowIndex = 0)
    {
        return CreateRankingRegistration(
            nameof(LibraryBase.WindowCumeDist),
            "CumeDist",
            orderFields,
            partitionKeys,
            typeof(double),
            windowIndex);
    }

    private static WindowRegistration CreateLagRegistration(
        IrExpression value,
        OrderField[] orderFields,
        IrExpression[] partitionKeys,
        IrExpression[] arguments,
        int windowIndex = 0)
    {
        return CreateOffsetRegistration("Lag", value, orderFields, partitionKeys, arguments, windowIndex);
    }

    private static WindowRegistration CreateLeadRegistration(
        IrExpression value,
        OrderField[] orderFields,
        IrExpression[] partitionKeys,
        IrExpression[] arguments,
        int windowIndex = 0)
    {
        return CreateOffsetRegistration("Lead", value, orderFields, partitionKeys, arguments, windowIndex);
    }

    private static WindowRegistration CreateFirstValueRegistration(
        IrExpression value,
        OrderField[] orderFields,
        WindowFrameNode? frame = null)
    {
        var method = typeof(LibraryBase).GetMethod(nameof(LibraryBase.WindowFirstValue), Type.EmptyTypes) ??
                     throw new InvalidOperationException("FirstValue window method was not found.");

        return new WindowRegistration(
            method,
            "FirstValue",
            [],
            orderFields,
            [value],
            0,
            typeof(object),
            frame);
    }

    private static WindowRegistration CreateOffsetRegistration(
        string functionName,
        IrExpression value,
        OrderField[] orderFields,
        IrExpression[] partitionKeys,
        IrExpression[] arguments,
        int windowIndex)
    {
        return new WindowRegistration(
            null!,
            functionName,
            partitionKeys,
            orderFields,
            [value, .. arguments],
            windowIndex,
            typeof(object));
    }

    private static WindowRegistration CreateRankingRegistration(
        string methodName,
        string functionName,
        OrderField[] orderFields,
        IrExpression[] partitionKeys,
        Type? returnType = null,
        int windowIndex = 0)
    {
        var method = typeof(LibraryBase).GetMethod(methodName, Type.EmptyTypes) ??
                     throw new InvalidOperationException($"{functionName} window method was not found.");

        return new WindowRegistration(
            method,
            functionName,
            partitionKeys,
            orderFields,
            [],
            windowIndex,
            returnType ?? typeof(long));
    }

}
