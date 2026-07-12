using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Execution;

internal sealed class PostOperationProjectionPlanner(
    Func<string, ProjectedField[], IReadOnlyDictionary<string, RowShape>, GeneratedRowShape> createGeneratedShape,
    Func<IReadOnlyList<PostOperation>, IReadOnlyList<ProjectedField>, string?> validateHiddenSortFields)
{
    public BuildResult<PostOperationProjection> Create(
        string resultTableName,
        string resultShapeName,
        ProjectedField[] publicFields,
        IReadOnlyList<PostOperation> postOperations,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var publicShape = createGeneratedShape(resultShapeName, publicFields, sourceLookup);
        var hiddenFields = CreateHiddenSortFields(publicShape, publicFields, postOperations, sourceLookup);
        if (!hiddenFields.Supported)
            return BuildResult<PostOperationProjection>.Unsupported(hiddenFields.UnsupportedReason);

        var hiddenSortUnsupportedReason = validateHiddenSortFields(postOperations, hiddenFields.Value);
        if (hiddenSortUnsupportedReason != null)
            return BuildResult<PostOperationProjection>.Unsupported(hiddenSortUnsupportedReason);

        if (hiddenFields.Value.Count == 0)
        {
            return BuildResult<PostOperationProjection>.Success(new PostOperationProjection(
                publicFields,
                publicShape,
                new ExecutionVariable(resultTableName, typeof(object)),
                postOperations,
                null,
                [publicShape]));
        }

        var materializedFields = publicFields.Concat(hiddenFields.Value).ToArray();
        var workingShape = createGeneratedShape(
            CreateSortWorkingShapeName(resultShapeName),
            materializedFields,
            sourceLookup);
        var finalProjection = new TableProjection(
            new ExecutionVariable(resultTableName, typeof(object)),
            publicShape,
            Enumerable.Range(0, publicFields.Length).ToArray());

        return BuildResult<PostOperationProjection>.Success(new PostOperationProjection(
            materializedFields,
            workingShape,
            new ExecutionVariable(CreateSortWorkingTableName(resultTableName), typeof(object)),
            ReplaceSortProjectedFields(postOperations, materializedFields),
            finalProjection,
            [workingShape, publicShape]));
    }

    public static BuildResult<IReadOnlyList<ProjectedField>> CreateHiddenSortFields(
        GeneratedRowShape publicShape,
        ProjectedField[] publicFields,
        IReadOnlyList<PostOperation> postOperations,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var fields = new List<ProjectedField>();
        var usedNames = new HashSet<string>(
            publicFields.Select(field => field.OutputName),
            StringComparer.OrdinalIgnoreCase);

        foreach (var key in postOperations.SelectMany(GetOrderKeys))
        {
            if (RowShapeLookup.ResolveProjectedField(publicShape, key, publicFields) != null)
                continue;

            var expression = ExecutionExpressionConverter.Convert(key.Expression, sourceLookup);
            fields.Add(new ProjectedField(
                CreateHiddenSortFieldName(usedNames, fields.Count),
                key.Expression,
                publicFields.Length + fields.Count));
        }

        return BuildResult<IReadOnlyList<ProjectedField>>.Success(fields);
    }

    public static PostOperation[] ReplaceSortProjectedFields(
        IReadOnlyList<PostOperation> postOperations,
        ProjectedField[] materializedFields)
    {
        return postOperations
            .Select(operation => operation switch
            {
                SortOperation sort => sort with { ProjectedFields = materializedFields },
                TopNOperation topN => topN with { ProjectedFields = materializedFields },
                TopOffsetOperation topOffset => topOffset with { ProjectedFields = materializedFields },
                _ => operation
            })
            .ToArray();
    }

    public static string CreateSortWorkingShapeName(string resultShapeName)
    {
        return $"{resultShapeName}WithSortKeys";
    }

    private static IEnumerable<OrderField> GetOrderKeys(PostOperation operation)
    {
        return operation switch
        {
            SortOperation sort => sort.Keys,
            TopNOperation topN => topN.Keys,
            TopOffsetOperation topOffset => topOffset.Keys,
            _ => []
        };
    }

    private static string CreateHiddenSortFieldName(HashSet<string> usedNames, int index)
    {
        var baseName = $"__sortKey{index.ToString(CultureInfo.InvariantCulture)}";
        var candidate = baseName;
        var suffix = 1;

        while (!usedNames.Add(candidate))
        {
            candidate = $"{baseName}_{suffix.ToString(CultureInfo.InvariantCulture)}";
            suffix++;
        }

        return candidate;
    }

    private static string CreateSortWorkingTableName(string resultTableName)
    {
        return $"{resultTableName}WithSortKeys";
    }
}
