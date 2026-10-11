using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private JoinSource MaterializeAsOfStructSource(JoinSource source, string resultTableName)
    {
        var alias = source.Variable.Name;
        var fields = source.Shape.Fields.Select((field, index) => new ProjectedField(
            field.Name, new ColumnRef(alias, field.Name, field.Type.ResolveClrType()) { EnumType = field.EnumType }, index)).ToArray();
        var lookup = RowShapeLookup.CreateSourceShapeLookup(source.Shape);
        var shape = CreateGeneratedShape(CreateIdentifierCandidate($"{resultTableName}{alias}AsOfRow", 0), fields, lookup);
        var buffer = new ExecutionVariable(CreateIdentifierCandidate($"{resultTableName}{alias}AsOfRows", 0), typeof(object));
        var append = CreateAppendRow(buffer, shape, fields, lookup);
        var setup = source.Setup.ToList();
        setup.Add(CreateTable(buffer, shape));
        setup.Add(CreateSourceLoop(source.Shape, source.Rows, source.Variable, new ExecutionBlock([append])));
        var rows = new ExecutionRowStream(buffer, ExecutionRowStreamKind.Rows, ExecutionRowStreamRowsAccess.TableRows);
        var typedBuffer = new ExecutionVariable(CreateIdentifierCandidate($"{buffer.Name}Typed", 0), typeof(object), shape.TypeName);
        setup.Add(CreateMaterializeListNode(rows, typedBuffer, shape));
        var transition = CreateTypedMaterializedTransitionTableRowShape(alias, shape);

        return source with
        {
            Shape = transition,
            Variable = new ExecutionVariable(alias, typeof(object), shape.TypeName),
            Rows = new ExecutionVariableRead(typedBuffer),
            Setup = setup,
            Shapes = [..source.Shapes, shape, transition],
            GeneratedRowShape = shape
        };
    }
}
