#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
///     Emits window-function related syntax constructs: buffering, key extraction,
///     partition/order computation, and the final output loop.
/// </summary>
internal static class WindowEmitter
{
    internal const string Buffer = "_wBuf";
    internal const string PartitionPrefix = "_wPart_";
    internal const string OrderPrefix = "_wOrd_";
    internal const string ValuePrefix = "_wVal_";
    internal const string ResultPrefix = "_wResult_";
    internal const string RowIndex = "_wIdx";

    private const string ExtractionIndex = "_wI";
    private const string OuterOrderPrefix = "_wOrdOut_";
    private const string SortedOrder = "_wSortedOrder";
    private const string SortLhs = "_wA";
    private const string SortRhs = "_wB";
    private const string GroupsPrefix = "_wGroups_";
    private const string SortedPrefix = "_wSorted_";

    private const string HelpersType = "Musoq.Evaluator.Helpers.WindowFunctionHelpers";
    private const string WindowFunctionInterfaceType = "Musoq.Plugins.IWindowFunction";
    private const string ListType = "System.Collections.Generic.List<Musoq.Schema.DataSources.IObjectResolver>";
    private const string ScoreVariable = "score";

    /// <summary>
    ///     Creates the buffer declaration: var _wBuf = new List&lt;IObjectResolver&gt;();
    /// </summary>
    public static StatementSyntax CreateBufferDeclaration()
    {
        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxHelper.CreateAssignment(Buffer,
                SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(ListType))
                    .WithArgumentList(SyntaxFactory.ArgumentList())));
    }

    /// <summary>
    ///     Creates the statement: _wBuf.Add(score);
    /// </summary>
    public static StatementSyntax CreateBufferAdd()
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(Buffer),
                        SyntaxFactory.IdentifierName("Add")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(ScoreVariable))))));
    }

    /// <summary>
    ///     Creates: var score = _wBuf[indexVariable];
    /// </summary>
    public static StatementSyntax CreateScoreFromBuffer(string indexVariable)
    {
        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxHelper.CreateAssignment(ScoreVariable,
                SyntaxHelper.CreateElementAccess(Buffer, SyntaxFactory.IdentifierName(indexVariable))));
    }

    /// <summary>
    ///     Creates: var arrayName = new object[_wBuf.Count];
    /// </summary>
    public static StatementSyntax CreateObjectArrayDeclaration(string arrayName)
    {
        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxHelper.CreateAssignment(arrayName,
                SyntaxFactory.ArrayCreationExpression(
                    SyntaxFactory.ArrayType(
                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                        SyntaxFactory.SingletonList(
                            SyntaxFactory.ArrayRankSpecifier(
                                SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                    BufferCount())))))));
    }

    /// <summary>
    ///     Creates: arrayName[indexExpr] = (object)(valueExpr);
    /// </summary>
    public static StatementSyntax CreateArrayAssignment(
        string arrayName,
        ExpressionSyntax indexExpr,
        ExpressionSyntax valueExpr)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxHelper.CreateElementAccess(arrayName, indexExpr),
                SyntaxFactory.CastExpression(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                    SyntaxFactory.ParenthesizedExpression(valueExpr))));
    }

    /// <summary>
    ///     Creates a for loop: for (var _wI = 0; _wI &lt; _wBuf.Count; _wI++) { body }
    /// </summary>
    public static StatementSyntax CreateExtractionLoop(BlockSyntax body)
    {
        return StatementEmitter.CreateForLoop(
            ExtractionIndex,
            0,
            SyntaxFactory.BinaryExpression(
                SyntaxKind.LessThanExpression,
                SyntaxFactory.IdentifierName(ExtractionIndex),
                BufferCount()),
            SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.PreIncrementExpression,
                SyntaxFactory.IdentifierName(ExtractionIndex)),
            body);
    }

    /// <summary>
    ///     Builds a composite key expression using Roslyn API.
    ///     Single expression: (object)(expr)
    ///     Multiple expressions: WindowFunctionHelpers.CompositeKey((object)(e1), (object)(e2), ...)
    /// </summary>
    public static ExpressionSyntax BuildCompositeKeyExpression(ExpressionSyntax[] expressions)
    {
        if (expressions.Length == 1)
        {
            return SyntaxFactory.CastExpression(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                SyntaxFactory.ParenthesizedExpression(expressions[0]));
        }

        var args = expressions.Select(e =>
            SyntaxFactory.Argument(
                SyntaxFactory.CastExpression(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                    SyntaxFactory.ParenthesizedExpression(e))));

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(HelpersType),
                    SyntaxFactory.IdentifierName("CompositeKey")))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(args)));
    }

    /// <summary>
    ///     Analyzes registrations to deduplicate partition, order, and value arrays,
    ///     and determines shared partition/sort resolution across windows.
    /// </summary>
    public static WindowKeyMapping BuildKeyMapping(IReadOnlyList<WindowRegistration> registrations)
    {
        var count = registrations.Count;
        var mapping = new WindowKeyMapping(count);

        var partSigToName = new Dictionary<string, string>();
        var ordSigToName = new Dictionary<string, string>();
        var valSigToName = new Dictionary<string, string>();
        var nextPart = 0;
        var nextOrd = 0;
        var nextVal = 0;

        foreach (var reg in registrations)
        {
            if (reg.PartitionExpressions.Length > 0)
            {
                var sig = ComputeExpressionSignature(reg.PartitionExpressions);
                if (!partSigToName.TryGetValue(sig, out var name))
                {
                    name = $"{PartitionPrefix}{nextPart++}";
                    partSigToName[sig] = name;
                    mapping.UniquePartitionArrays.Add((name, reg.PartitionExpressions));
                }

                mapping.PartitionArray[reg.Index] = name;
            }

            if (reg.OrderExpressions.Length > 0)
            {
                var sig = ComputeExpressionSignature(reg.OrderExpressions);
                if (!ordSigToName.TryGetValue(sig, out var name))
                {
                    name = $"{OrderPrefix}{nextOrd++}";
                    ordSigToName[sig] = name;
                    mapping.UniqueOrderArrays.Add((name, reg.OrderExpressions));
                }

                mapping.OrderArray[reg.Index] = name;
            }

            if (reg.FunctionArgExpressions.Length > 0)
            {
                var valueSig = reg.FunctionArgExpressions[0].NormalizeWhitespace().ToFullString();
                if (!valSigToName.TryGetValue(valueSig, out var name))
                {
                    name = $"{ValuePrefix}{nextVal++}";
                    valSigToName[valueSig] = name;
                    mapping.UniqueValueArrays.Add((name, reg.FunctionArgExpressions[0]));
                }

                mapping.ValueArray[reg.Index] = name;
            }
            else if (reg.FactoryMethod != null && reg.OrderExpressions.Length > 0)
            {
                mapping.ValueArray[reg.Index] = mapping.OrderArray[reg.Index];
            }
        }

        var groupSeen = new Dictionary<string, string>();
        var sortSeen = new Dictionary<string, string>();
        var nextGroup = 0;
        var nextSort = 0;

        foreach (var reg in registrations)
        {
            var partSig = reg.PartitionExpressions.Length > 0
                ? ComputeExpressionSignature(reg.PartitionExpressions)
                : "__null__";

            if (!groupSeen.TryGetValue(partSig, out var groupsName))
            {
                groupsName = $"{GroupsPrefix}{nextGroup++}";
                groupSeen[partSig] = groupsName;
                mapping.PartitionResolutions.Add(
                    new PartitionResolutionInfo(groupsName, mapping.PartitionArray[reg.Index]));
            }

            if (reg.OrderExpressions.Length > 0)
            {
                var ordSig = ComputeExpressionSignature(reg.OrderExpressions);
                var descSig = string.Join(",",
                    reg.OrderByFields.Select(f => f.Order == Order.Descending));
                var fullSig = $"{partSig}|{ordSig}|{descSig}";

                if (!sortSeen.TryGetValue(fullSig, out var sortedName))
                {
                    sortedName = $"{SortedPrefix}{nextSort++}";
                    sortSeen[fullSig] = sortedName;
                    var descFlags = reg.OrderByFields
                        .Select(f => f.Order == Order.Descending)
                        .ToArray();
                    mapping.SortResolutions.Add(
                        new SortResolutionInfo(sortedName, groupsName,
                            mapping.OrderArray[reg.Index]!, descFlags));
                }

                mapping.PartitionsVar[reg.Index] = sortedName;
                mapping.IsSorted[reg.Index] = true;
            }
            else
            {
                mapping.PartitionsVar[reg.Index] = groupsName;
                mapping.IsSorted[reg.Index] = false;
            }
        }

        MarkCombinableSortResolutions(mapping);

        return mapping;
    }

    /// <summary>
    ///     Identifies sort resolutions whose groups var is never used directly by any
    ///     unsorted compute method, allowing ResolvePartitions + SortPartitions to be
    ///     combined into a single ResolveSortedPartitions call (sorts in-place, no clone).
    /// </summary>
    private static void MarkCombinableSortResolutions(WindowKeyMapping mapping)
    {
        var directlyUsedGroups = new HashSet<string>();
        for (var i = 0; i < mapping.PartitionsVar.Length; i++)
        {
            if (!mapping.IsSorted[i])
                directlyUsedGroups.Add(mapping.PartitionsVar[i]);
        }

        var sortCountPerGroup = new Dictionary<string, int>();
        foreach (var sort in mapping.SortResolutions)
        {
            sortCountPerGroup.TryGetValue(sort.GroupsVarName, out var count);
            sortCountPerGroup[sort.GroupsVarName] = count + 1;
        }

        for (var i = 0; i < mapping.SortResolutions.Count; i++)
        {
            var sort = mapping.SortResolutions[i];
            if (directlyUsedGroups.Contains(sort.GroupsVarName))
                continue;

            if (sortCountPerGroup[sort.GroupsVarName] != 1)
                continue;

            mapping.SortResolutions[i] = sort with { CanCombineWithResolve = true };
        }
    }

    /// <summary>
    ///     Generates key extraction statements using deduplicated arrays from the mapping.
    /// </summary>
    public static BlockSyntax GenerateKeyExtraction(
        BlockSyntax fullBlock, WindowKeyMapping mapping)
    {
        foreach (var (name, _) in mapping.UniquePartitionArrays)
            fullBlock = fullBlock.AddStatements(CreateObjectArrayDeclaration(name));

        foreach (var (name, _) in mapping.UniqueOrderArrays)
            fullBlock = fullBlock.AddStatements(CreateObjectArrayDeclaration(name));

        foreach (var (name, _) in mapping.UniqueValueArrays)
            fullBlock = fullBlock.AddStatements(CreateObjectArrayDeclaration(name));

        var totalAssignments = mapping.UniquePartitionArrays.Count
                               + mapping.UniqueOrderArrays.Count
                               + mapping.UniqueValueArrays.Count;

        if (totalAssignments > 0)
        {
            var extractionBody = StatementEmitter.CreateEmptyBlock();
            extractionBody = extractionBody.AddStatements(CreateScoreFromBuffer(ExtractionIndex));

            foreach (var (name, expressions) in mapping.UniquePartitionArrays)
            {
                var keyExpr = BuildCompositeKeyExpression(expressions);
                extractionBody = extractionBody.AddStatements(
                    CreateArrayAssignment(name,
                        SyntaxFactory.IdentifierName(ExtractionIndex), keyExpr));
            }

            foreach (var (name, expressions) in mapping.UniqueOrderArrays)
            {
                var keyExpr = BuildCompositeKeyExpression(expressions);
                extractionBody = extractionBody.AddStatements(
                    CreateArrayAssignment(name,
                        SyntaxFactory.IdentifierName(ExtractionIndex), keyExpr));
            }

            foreach (var (name, expression) in mapping.UniqueValueArrays)
            {
                extractionBody = extractionBody.AddStatements(
                    CreateArrayAssignment(name,
                        SyntaxFactory.IdentifierName(ExtractionIndex), expression));
            }

            fullBlock = fullBlock.AddStatements(CreateExtractionLoop(extractionBody));
        }

        return fullBlock;
    }

    /// <summary>
    ///     Generates ResolvePartitions and SortPartitions calls from the mapping.
    ///     When a sort resolution can be combined (its groups var is not used elsewhere),
    ///     emits a single ResolveSortedPartitions call that sorts in-place without cloning.
    /// </summary>
    public static BlockSyntax GeneratePartitionResolution(BlockSyntax fullBlock, WindowKeyMapping mapping)
    {
        var combinedGroupVars = new HashSet<string>();
        foreach (var sort in mapping.SortResolutions)
        {
            if (sort.CanCombineWithResolve)
                combinedGroupVars.Add(sort.GroupsVarName);
        }

        foreach (var res in mapping.PartitionResolutions)
        {
            if (combinedGroupVars.Contains(res.GroupsVarName))
                continue;

            var partitionArg = res.PartitionArrayName != null
                ? (ExpressionSyntax)SyntaxFactory.IdentifierName(res.PartitionArrayName)
                : SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

            var call = CreateHelperInvocation("ResolvePartitions",
                SyntaxFactory.Argument(BufferCount()),
                SyntaxFactory.Argument(partitionArg));

            fullBlock = fullBlock.AddStatements(
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxHelper.CreateAssignment(res.GroupsVarName, call)));
        }

        foreach (var sort in mapping.SortResolutions)
        {
            if (sort.CanCombineWithResolve)
            {
                var partRes = mapping.PartitionResolutions.First(
                    r => r.GroupsVarName == sort.GroupsVarName);
                var partitionArg = partRes.PartitionArrayName != null
                    ? (ExpressionSyntax)SyntaxFactory.IdentifierName(partRes.PartitionArrayName)
                    : SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

                var call = CreateHelperInvocation("ResolveSortedPartitions",
                    SyntaxFactory.Argument(BufferCount()),
                    SyntaxFactory.Argument(partitionArg),
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName(sort.OrderArrayName)),
                    SyntaxFactory.Argument(CreateBoolArrayExpression(sort.DescendingFlags)));

                fullBlock = fullBlock.AddStatements(
                    SyntaxFactory.LocalDeclarationStatement(
                        SyntaxHelper.CreateAssignment(sort.SortedVarName, call)));
            }
            else
            {
                var call = CreateHelperInvocation("SortPartitions",
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName(sort.GroupsVarName)),
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName(sort.OrderArrayName)),
                    SyntaxFactory.Argument(CreateBoolArrayExpression(sort.DescendingFlags)));

                fullBlock = fullBlock.AddStatements(
                    SyntaxFactory.LocalDeclarationStatement(
                        SyntaxHelper.CreateAssignment(sort.SortedVarName, call)));
            }
        }

        return fullBlock;
    }

    /// <summary>
    ///     Generates computation statements for all window functions
    ///     using pre-resolved partitions from the mapping.
    /// </summary>
    public static BlockSyntax GenerateComputation(
        BlockSyntax fullBlock, IReadOnlyList<WindowRegistration> registrations, WindowKeyMapping mapping)
    {
        foreach (var reg in registrations)
        {
            var normalizedName = NormalizeWindowFunctionName(reg.FunctionName);
            var resultVariable = $"{ResultPrefix}{reg.Index}";
            var partitionsExpr = (ExpressionSyntax)SyntaxFactory.IdentifierName(
                mapping.PartitionsVar[reg.Index]);

            ExpressionSyntax computeExpr;

            if (reg.FactoryMethod != null)
            {
                computeExpr = CreatePluginWindowFunctionCall(
                    reg, partitionsExpr, mapping);
            }
            else
            {
                computeExpr = normalizedName switch
                {
                    "lag" => CreatePreResolvedOffsetCall(
                        "ComputeLag", reg, partitionsExpr, mapping.ValueArray[reg.Index]),
                    "lead" => CreatePreResolvedOffsetCall(
                        "ComputeLead", reg, partitionsExpr, mapping.ValueArray[reg.Index]),
                    _ => throw new NotSupportedException(
                        $"Window function '{reg.FunctionName}' is not supported. " +
                        "Provide a plugin implementation using [WindowFunction] attribute.")
                };
            }

            fullBlock = fullBlock.AddStatements(
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxHelper.CreateAssignment(resultVariable, computeExpr)));
        }

        return fullBlock;
    }

    /// <summary>
    ///     Generates the output loop, either simple or with an outer ORDER BY sort.
    /// </summary>
    public static BlockSyntax GenerateOutput(
        BlockSyntax fullBlock,
        BlockSyntax selectBlock,
        StatementSyntax? skip,
        BlockSyntax? take,
        (FieldOrderedNode Field, ExpressionSyntax Syntax)[] orderByFields,
        string queryAlias)
    {
        var outputBody = StatementEmitter.CreateEmptyBlock();
        outputBody = outputBody.AddStatements(CreateScoreFromBuffer(RowIndex));
        outputBody = outputBody.AddStatements(QueryEmitter.GenerateStatsUpdateStatement());

        if (skip != null)
            outputBody = outputBody.AddStatements(skip);

        if (take != null)
            outputBody = outputBody.AddStatements(take.Statements.ToArray());

        if (!string.IsNullOrEmpty(queryAlias))
            outputBody = outputBody.AddStatements(
                QueryEmitter.GeneratePhaseChangeStatement(queryAlias, QueryPhase.Select));

        outputBody = outputBody.AddStatements(selectBlock.Statements.ToArray());

        if (orderByFields.Length > 0)
            fullBlock = GenerateOrderedOutput(fullBlock, outputBody, orderByFields);
        else
            fullBlock = fullBlock.AddStatements(CreateOutputLoop(outputBody));

        return fullBlock;
    }

    /// <summary>
    ///     Creates a result access expression: (castType)_wResult_N[_wIdx]
    /// </summary>
    public static ExpressionSyntax CreateResultAccess(int index, string castType)
    {
        return SyntaxFactory.CastExpression(
            SyntaxFactory.ParseTypeName(castType),
            SyntaxHelper.CreateElementAccess($"{ResultPrefix}{index}",
                SyntaxFactory.IdentifierName(RowIndex)));
    }

    private static ExpressionSyntax CreatePreResolvedOffsetCall(
        string methodName, WindowRegistration reg,
        ExpressionSyntax partitionsExpr, string? valueArrayName)
    {
        var valueArg = valueArrayName != null
            ? (ExpressionSyntax)SyntaxFactory.IdentifierName(valueArrayName)
            : CreateEmptyObjectArray();

        var offsetArg = reg.FunctionArgExpressions.Length > 1
            ? SyntaxFactory.CastExpression(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
                SyntaxFactory.ParenthesizedExpression(reg.FunctionArgExpressions[1]))
            : (ExpressionSyntax)SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1));

        var defaultArg = reg.FunctionArgExpressions.Length > 2
            ? SyntaxFactory.CastExpression(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                SyntaxFactory.ParenthesizedExpression(reg.FunctionArgExpressions[2]))
            : (ExpressionSyntax)SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

        return CreateHelperInvocation(methodName,
            SyntaxFactory.Argument(BufferCount()),
            SyntaxFactory.Argument(partitionsExpr),
            SyntaxFactory.Argument(valueArg),
            SyntaxFactory.Argument(offsetArg),
            SyntaxFactory.Argument(defaultArg));
    }

    private static ExpressionSyntax CreatePluginWindowFunctionCall(
        WindowRegistration reg, ExpressionSyntax partitionsExpr, WindowKeyMapping mapping)
    {
        var valueArg = mapping.ValueArray[reg.Index] != null
            ? (ExpressionSyntax)SyntaxFactory.IdentifierName(mapping.ValueArray[reg.Index]!)
            : CreateEmptyObjectArray();

        var sortedArg = SyntaxFactory.LiteralExpression(
            mapping.IsSorted[reg.Index] ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);

        var factoryMethod = reg.FactoryMethod!;
        var libraryType = factoryMethod.ReflectedType!.FullName!.Replace("+", ".");
        var factoryName = factoryMethod.Name;

        var factoryCall = SyntaxFactory.CastExpression(
            SyntaxFactory.ParseTypeName(WindowFunctionInterfaceType),
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ParenthesizedExpression(
                            SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.ParseTypeName(libraryType))
                                .WithArgumentList(SyntaxFactory.ArgumentList())),
                        SyntaxFactory.IdentifierName(factoryName)))
                .WithArgumentList(SyntaxFactory.ArgumentList()));

        if (reg.FunctionArgExpressions.Length > 1)
        {
            var extraArgs = new ExpressionSyntax[reg.FunctionArgExpressions.Length - 1];
            for (var i = 1; i < reg.FunctionArgExpressions.Length; i++)
            {
                extraArgs[i - 1] = SyntaxFactory.CastExpression(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                    SyntaxFactory.ParenthesizedExpression(reg.FunctionArgExpressions[i]));
            }

            var extraArgsArray = SyntaxFactory.ArrayCreationExpression(
                    SyntaxFactory.ArrayType(
                            SyntaxFactory.NullableType(
                                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))))
                        .WithRankSpecifiers(SyntaxFactory.SingletonList(
                            SyntaxFactory.ArrayRankSpecifier(
                                SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                    SyntaxFactory.OmittedArraySizeExpression())))))
                .WithInitializer(
                    SyntaxFactory.InitializerExpression(
                        SyntaxKind.ArrayInitializerExpression,
                        SyntaxFactory.SeparatedList<ExpressionSyntax>(extraArgs)));

            return CreateHelperInvocation("ComputePluginWindowFunction",
                SyntaxFactory.Argument(BufferCount()),
                SyntaxFactory.Argument(partitionsExpr),
                SyntaxFactory.Argument(sortedArg),
                SyntaxFactory.Argument(valueArg),
                SyntaxFactory.Argument(factoryCall),
                SyntaxFactory.Argument(extraArgsArray));
        }

        return CreateHelperInvocation("ComputePluginWindowFunction",
            SyntaxFactory.Argument(BufferCount()),
            SyntaxFactory.Argument(partitionsExpr),
            SyntaxFactory.Argument(sortedArg),
            SyntaxFactory.Argument(valueArg),
            SyntaxFactory.Argument(factoryCall));
    }

    private static BlockSyntax GenerateOrderedOutput(
        BlockSyntax fullBlock,
        BlockSyntax outputBody,
        (FieldOrderedNode Field, ExpressionSyntax Syntax)[] orderByFields)
    {
        for (var i = 0; i < orderByFields.Length; i++)
            fullBlock = fullBlock.AddStatements(CreateObjectArrayDeclaration($"{OuterOrderPrefix}{i}"));

        var extractBody = StatementEmitter.CreateEmptyBlock();
        extractBody = extractBody.AddStatements(CreateScoreFromBuffer(RowIndex));

        for (var i = 0; i < orderByFields.Length; i++)
        {
            extractBody = extractBody.AddStatements(
                CreateArrayAssignment($"{OuterOrderPrefix}{i}",
                    SyntaxFactory.IdentifierName(RowIndex), orderByFields[i].Syntax));
        }

        fullBlock = fullBlock.AddStatements(CreateOutputLoop(extractBody));

        fullBlock = fullBlock.AddStatements(CreateSortedOrderList());

        fullBlock = fullBlock.AddStatements(CreateSortStatement(orderByFields));

        var foreachOutput = StatementEmitter.CreateForeach(
            RowIndex,
            SyntaxFactory.IdentifierName(SortedOrder),
            outputBody);
        fullBlock = fullBlock.AddStatements(foreachOutput);

        return fullBlock;
    }

    /// <summary>
    ///     Creates: for (var _wIdx = 0; _wIdx &lt; _wBuf.Count; _wIdx++) { body }
    /// </summary>
    private static StatementSyntax CreateOutputLoop(BlockSyntax body)
    {
        return StatementEmitter.CreateForLoop(
            RowIndex,
            0,
            SyntaxFactory.BinaryExpression(
                SyntaxKind.LessThanExpression,
                SyntaxFactory.IdentifierName(RowIndex),
                BufferCount()),
            SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.PreIncrementExpression,
                SyntaxFactory.IdentifierName(RowIndex)),
            body);
    }

    /// <summary>
    ///     Creates: var _wSortedOrder = Enumerable.ToList(Enumerable.Range(0, _wBuf.Count));
    /// </summary>
    private static StatementSyntax CreateSortedOrderList()
    {
        var rangeCall = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("System.Linq.Enumerable"),
                    SyntaxFactory.IdentifierName("Range")))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))),
                        SyntaxFactory.Argument(BufferCount())
                    })));

        var toListCall = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("System.Linq.Enumerable"),
                    SyntaxFactory.IdentifierName("ToList")))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(rangeCall))));

        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxHelper.CreateAssignment(SortedOrder, toListCall));
    }

    /// <summary>
    ///     Creates the Sort lambda that compares order-by fields using CompareValues.
    /// </summary>
    private static StatementSyntax CreateSortStatement(
        (FieldOrderedNode Field, ExpressionSyntax Syntax)[] orderByFields)
    {
        var statements = new List<StatementSyntax>();

        for (var i = 0; i < orderByFields.Length; i++)
        {
            var isDesc = orderByFields[i].Field.Order == Order.Descending;

            var compareCall = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(HelpersType),
                        SyntaxFactory.IdentifierName("CompareValues")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(new[]
                        {
                            SyntaxFactory.Argument(
                                SyntaxHelper.CreateElementAccess($"{OuterOrderPrefix}{i}",
                                    SyntaxFactory.IdentifierName(SortLhs))),
                            SyntaxFactory.Argument(
                                SyntaxHelper.CreateElementAccess($"{OuterOrderPrefix}{i}",
                                    SyntaxFactory.IdentifierName(SortRhs))),
                            SyntaxFactory.Argument(
                                SyntaxFactory.LiteralExpression(
                                    isDesc ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression))
                        })));

            var cmpDecl = SyntaxFactory.LocalDeclarationStatement(
                SyntaxHelper.CreateAssignment("_cmp", compareCall));

            var earlyReturn = StatementEmitter.CreateIf(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.NotEqualsExpression,
                    SyntaxFactory.IdentifierName("_cmp"),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))),
                StatementEmitter.CreateReturn(SyntaxFactory.IdentifierName("_cmp")));

            statements.Add(SyntaxFactory.Block(cmpDecl, earlyReturn));
        }

        statements.Add(StatementEmitter.CreateReturn(
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))));

        var lambdaBody = SyntaxFactory.Block(statements);

        var lambda = SyntaxFactory.ParenthesizedLambdaExpression(
            SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(SortLhs)),
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(SortRhs))
                })),
            lambdaBody);

        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(SortedOrder),
                        SyntaxFactory.IdentifierName("Sort")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(lambda)))));
    }

    private static InvocationExpressionSyntax CreateHelperInvocation(string methodName, params ArgumentSyntax[] args)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(HelpersType),
                    SyntaxFactory.IdentifierName(methodName)))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(args)));
    }

    /// <summary>
    ///     _wBuf.Count
    /// </summary>
    private static ExpressionSyntax BufferCount()
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(Buffer),
            SyntaxFactory.IdentifierName("Count"));
    }

    /// <summary>
    ///     new object[_wBuf.Count]
    /// </summary>
    private static ExpressionSyntax CreateEmptyObjectArray()
    {
        return SyntaxFactory.ArrayCreationExpression(
            SyntaxFactory.ArrayType(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                SyntaxFactory.SingletonList(
                    SyntaxFactory.ArrayRankSpecifier(
                        SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(BufferCount())))));
    }

    /// <summary>
    ///     new bool[] { true, false, ... } or new bool[0]
    /// </summary>
    private static ExpressionSyntax CreateBoolArrayExpression(bool[] flags)
    {
        if (flags.Length == 0)
        {
            return SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.ArrayType(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
                    SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(
                            SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(0)))))));
        }

        var elements = flags.Select(d =>
            (ExpressionSyntax)SyntaxFactory.LiteralExpression(
                d ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression));

        return SyntaxFactory.ArrayCreationExpression(
            SyntaxFactory.ArrayType(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
                SyntaxFactory.SingletonList(
                    SyntaxFactory.ArrayRankSpecifier(
                        SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.OmittedArraySizeExpression())))),
            SyntaxFactory.InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                SyntaxFactory.SeparatedList(elements)));
    }

    /// <summary>
    ///     Replaces return statements with continue in a syntax tree.
    /// </summary>
    public static StatementSyntax ReplaceReturnWithContinue(StatementSyntax statement)
    {
        return new ReturnToContinueRewriter().Visit(statement) as StatementSyntax ?? statement;
    }

    internal static string NormalizeWindowFunctionName(string functionName)
    {
        return functionName.ToLowerInvariant().Replace("_", "");
    }

    private static string ComputeExpressionSignature(ExpressionSyntax[] expressions)
    {
        if (expressions.Length == 1)
            return expressions[0].NormalizeWhitespace().ToFullString();

        return string.Join("|",
            expressions.Select(e => e.NormalizeWhitespace().ToFullString()));
    }

    /// <summary>
    ///     Roslyn syntax rewriter that replaces ReturnStatementSyntax with ContinueStatementSyntax.
    /// </summary>
    private sealed class ReturnToContinueRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
        {
            if (node.Expression == null)
                return SyntaxFactory.ContinueStatement().WithTriviaFrom(node);

            return base.VisitReturnStatement(node);
        }
    }

    /// <summary>
    ///     Registration data for a single window function, passed from the visitor to the emitter.
    /// </summary>
    public sealed class WindowRegistration
    {
        public required int Index { get; init; }

        public required string FunctionName { get; init; }

        public required ExpressionSyntax[] PartitionExpressions { get; init; }

        public required ExpressionSyntax[] OrderExpressions { get; init; }

        public required FieldOrderedNode[] OrderByFields { get; init; }

        public required ExpressionSyntax[] FunctionArgExpressions { get; init; }

        public required Type ReturnType { get; init; }

        public MethodInfo? FactoryMethod { get; init; }
    }

    /// <summary>
    ///     Mapping produced by BuildKeyMapping, consumed by extraction, resolution, and computation phases.
    /// </summary>
    public sealed class WindowKeyMapping
    {
        public string?[] PartitionArray { get; }
        public string?[] OrderArray { get; }
        public string?[] ValueArray { get; }
        public string[] PartitionsVar { get; }
        public bool[] IsSorted { get; }

        public List<(string Name, ExpressionSyntax[] Expressions)> UniquePartitionArrays { get; } = [];
        public List<(string Name, ExpressionSyntax[] Expressions)> UniqueOrderArrays { get; } = [];
        public List<(string Name, ExpressionSyntax Expression)> UniqueValueArrays { get; } = [];
        public List<PartitionResolutionInfo> PartitionResolutions { get; } = [];
        public List<SortResolutionInfo> SortResolutions { get; } = [];

        public WindowKeyMapping(int registrationCount)
        {
            PartitionArray = new string?[registrationCount];
            OrderArray = new string?[registrationCount];
            ValueArray = new string?[registrationCount];
            PartitionsVar = new string[registrationCount];
            IsSorted = new bool[registrationCount];
        }
    }

    public readonly record struct PartitionResolutionInfo(
        string GroupsVarName, string? PartitionArrayName);

    public readonly record struct SortResolutionInfo(
        string SortedVarName, string GroupsVarName,
        string OrderArrayName, bool[] DescendingFlags,
        bool CanCombineWithResolve = false);
}
