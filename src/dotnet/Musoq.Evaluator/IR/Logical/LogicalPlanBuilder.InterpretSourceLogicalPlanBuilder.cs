using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using IrNodes = Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Logical;

public sealed partial class LogicalPlanBuilder
{
    private IrNodes.InterpretSourceNode CreateInterpretSource(InterpretFromNode node, OutputSchema schema)
    {
        var kind = node.InterpretCall switch
        {
            InterpretCallNode => IrNodes.InterpretSourceKind.Interpret,
            InterpretAtCallNode => IrNodes.InterpretSourceKind.InterpretAt,
            ParseCallNode => IrNodes.InterpretSourceKind.Parse,
            TryInterpretCallNode => IrNodes.InterpretSourceKind.TryInterpret,
            TryParseCallNode => IrNodes.InterpretSourceKind.TryParse,
            PartialInterpretCallNode => IrNodes.InterpretSourceKind.PartialInterpret,
            PartialParseCallNode => IrNodes.InterpretSourceKind.PartialParse,
            _ => throw UnsupportedShape.Of(
                $"Interpret source call type '{node.InterpretCall.GetType().Name}'")
        };

        var derivedSchema = BuildInterpretOutputSchema(node);
        var outputSchema = ShouldUseDerivedInterpretSchema(schema, derivedSchema)
            ? derivedSchema
            : schema;

        return new IrNodes.InterpretSourceNode(
            node.SchemaName,
            kind,
            ConvertInterpretSourceArguments(node.InterpretCall),
            node.Alias,
            ResolveInterpretResultType(node),
            MapApplyKind(node.ApplyType),
            outputSchema);
    }

    private static OutputSchema BuildInterpretOutputSchema(InterpretFromNode node)
    {
        if (node.InterpretCall is PartialInterpretCallNode or PartialParseCallNode)
            return CreatePartialInterpretOutputSchema();

        var resultType = ResolveInterpretResultType(node);
        if (resultType == typeof(object))
            return OutputSchema.Empty;

        var properties = resultType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead)
            .Where(property => property.GetIndexParameters().Length == 0)
            .Where(property => property.DeclaringType == resultType)
            .Where(property => !string.Equals(property.Name, "SchemaName", StringComparison.Ordinal))
            .ToArray();

        if (properties.Length == 0)
            return OutputSchema.Empty;

        var columns = new ColumnSchema[properties.Length];

        for (var i = 0; i < properties.Length; i++)
            columns[i] = new ColumnSchema(properties[i].Name, properties[i].PropertyType, i);

        return new OutputSchema(columns);
    }

    private static OutputSchema CreatePartialInterpretOutputSchema()
    {
        return new OutputSchema(
        [
            new ColumnSchema("ParsedFields", typeof(Dictionary<string, object?>), 0),
            new ColumnSchema("ErrorField", typeof(string), 1),
            new ColumnSchema("ErrorMessage", typeof(string), 2),
            new ColumnSchema("BytesConsumed", typeof(int), 3)
        ]);
    }

    private static bool ShouldUseDerivedInterpretSchema(OutputSchema inferredSchema, OutputSchema derivedSchema)
    {
        if (derivedSchema.Columns.Length == 0)
            return false;

        if (inferredSchema.Columns.Length == 0)
            return true;

        foreach (var column in inferredSchema.Columns)
        {
            if (string.Equals(column.Name, "EntityType", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(column.Name, "Rows", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        if (inferredSchema.Columns.Length != derivedSchema.Columns.Length)
            return true;

        for (var i = 0; i < inferredSchema.Columns.Length; i++)
        {
            if (!string.Equals(inferredSchema.Columns[i].Name, derivedSchema.Columns[i].Name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private IrExpression[] ConvertInterpretSourceArguments(Node interpretCall)
    {
        return interpretCall switch
        {
            InterpretCallNode interpret => [_converter.Convert(interpret.DataSource)],
            TryInterpretCallNode tryInterpret => [_converter.Convert(tryInterpret.DataSource)],
            PartialInterpretCallNode partialInterpret => [_converter.Convert(partialInterpret.DataSource)],
            PartialParseCallNode partialParse => [_converter.Convert(partialParse.DataSource)],
            ParseCallNode parse => [_converter.Convert(parse.DataSource)],
            TryParseCallNode tryParse => [_converter.Convert(tryParse.DataSource)],
            InterpretAtCallNode interpretAt =>
            [
                _converter.Convert(interpretAt.DataSource),
                _converter.Convert(interpretAt.Offset)
            ],
            _ => throw UnsupportedShape.Of(
                $"Interpret source call type '{interpretCall.GetType().Name}'")
        };
    }

    private static Type ResolveInterpretResultType(InterpretFromNode node)
    {
        return node.ReturnType
            ?? node.InterpretCall.ReturnType
            ?? typeof(object);
    }
}
