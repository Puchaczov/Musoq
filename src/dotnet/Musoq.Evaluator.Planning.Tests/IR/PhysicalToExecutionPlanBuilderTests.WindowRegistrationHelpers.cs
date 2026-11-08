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
        return CreateRankingRegistration(nameof(LibraryBase.RowNumber), "RowNumber", orderFields, partitionKeys);
    }

    private static WindowRegistration CreateRankRegistration(params OrderField[] orderFields)
    {
        return CreateRankingRegistration(nameof(LibraryBase.Rank), "Rank", orderFields, []);
    }

    private static WindowRegistration CreateDenseRankRegistration(
        OrderField[] orderFields,
        IrExpression[] partitionKeys)
    {
        return CreateRankingRegistration(nameof(LibraryBase.DenseRank), "DenseRank", orderFields, partitionKeys);
    }

    private static WindowRegistration CreatePercentRankRegistration(
        OrderField[] orderFields,
        IrExpression[] partitionKeys,
        int windowIndex = 0)
    {
        return CreateRankingRegistration(
            nameof(LibraryBase.PercentRank),
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
            nameof(LibraryBase.CumeDist),
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
        var method = ResolveTypedWindowDeclaration("FirstValue", value);

        return new WindowRegistration(
            method,
            "FirstValue",
            [],
            orderFields,
            [value],
            0,
            method.ReturnType,
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
        var method = ResolveTypedWindowDeclaration(functionName, value);
        return new WindowRegistration(
            method,
            functionName,
            partitionKeys,
            orderFields,
            [value, .. arguments],
            windowIndex,
            method.ReturnType);
    }

    private static System.Reflection.MethodInfo ResolveTypedWindowDeclaration(string name, IrExpression value)
    {
        var method = Musoq.Evaluator.Visitors.WindowDeclarationBinding.ResolveStandard(name, [value.ReturnType]) ??
                     throw new InvalidOperationException($"Window declaration '{name}' was not found.");
        if (!method.IsGenericMethodDefinition)
            return method;
        return method.MakeGenericMethod(Nullable.GetUnderlyingType(value.ReturnType) ?? value.ReturnType);
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
