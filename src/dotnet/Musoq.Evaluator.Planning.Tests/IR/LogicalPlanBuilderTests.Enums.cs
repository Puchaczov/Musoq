using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.IR;

public partial class LogicalPlanBuilderTests
{
    [TestMethod]
    public void WhenProjectingEnumColumn_ShouldPreserveLogicalDescriptorInOutputSchema()
    {
        var descriptor = new EnumTypeDescriptor(
            "JobStatus",
            EnumTypeOrigin.QueryLocal,
            EnumUnderlyingKind.Int32,
            false,
            [new EnumMemberDescriptor("Running", EnumScalarValue.FromInt32(20))]);
        var from = CreateSchemaFrom("t");
        var select = CreateSelect(Field(Column("Status", "t", typeof(int)), 0, "Status"));
        var query = CreateQuery(select, from);
        var root = new RootNode(new SingleSetNode(query));
        IReadOnlyDictionary<string, ISchemaColumn[]> inferredColumns =
            new Dictionary<string, ISchemaColumn[]>
            {
                ["t"] =
                [
                    new Musoq.Schema.DataSources.SchemaColumn(
                        "Status",
                        0,
                        typeof(int),
                        typeof(int),
                        descriptor)
                ]
            };

        var result = Build(root, inferredColumns);

        Assert.IsInstanceOfType<ProjectNode>(result);
        var project = (ProjectNode)result;
        Assert.AreEqual(descriptor.Fingerprint, project.Fields[0].Expression.EnumType?.Fingerprint);
        Assert.AreEqual(descriptor.Fingerprint, project.OutputSchema.Columns[0].EnumType?.Fingerprint);
        Assert.AreEqual(typeof(int), project.OutputSchema.Columns[0].Type);
    }
}
