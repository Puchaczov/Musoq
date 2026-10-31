namespace Musoq.Formatter.Tests.Verification;

internal static class ProjectionFixture
{
    internal static string Create(string declaration) => $$"""
        namespace Musoq.Parser.Nodes { public class FixtureNode { public string Name {get;} {{declaration}} } }
        namespace Musoq.Formatter.Tests.Verification
        {
            class AstProjector
            {
                void Project(Musoq.Parser.Nodes.FixtureNode value) { Member(nameof(value.Name)); Write(value.Name); }
                void Write(string value) {}
                void Member(string name) {}
                void Dispatch(Musoq.Parser.Nodes.FixtureNode value) =>
                    ProjectNode(value, "Musoq.Parser.Nodes.FixtureNode", static (writer, node) => writer.Project(node));
                void ProjectNode<T>(T value, string identity, System.Action<AstProjector,T> projection) {}
            }
        }
        """;
}
