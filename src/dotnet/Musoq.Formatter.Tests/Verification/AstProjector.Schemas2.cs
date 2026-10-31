namespace Musoq.Formatter.Tests.Verification;

internal sealed partial class AstProjector
{
    private void Project(global::Musoq.Parser.Nodes.InterpretationSchema.SchemaComment value)
    {
        Member(nameof(value.Text));
        Write(value.Text);
    }
}
