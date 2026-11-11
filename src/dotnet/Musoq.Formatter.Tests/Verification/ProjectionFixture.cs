namespace Musoq.Formatter.Tests.Verification;

internal static class ProjectionFixture
{
    internal static string Create(string declaration) =>
        $"namespace Musoq.Parser.Nodes {{ public class FixtureNode {{ public string Name {{get;}} {declaration} }} }}\nnamespace Musoq.Formatter.Tests.Verification\n{{\n    class AstProjector\n    {{\n        void Project(Musoq.Parser.Nodes.FixtureNode value) {{ Member(nameof(value.Name)); Write(value.Name); }}\n        void Write(string value) {{}}\n        void Member(string name) {{}}\n        void Dispatch(Musoq.Parser.Nodes.FixtureNode value) =>\n            ProjectNode(value, \"Musoq.Parser.Nodes.FixtureNode\", static (writer, node) => writer.Project(node));\n        void ProjectNode<T>(T value, string identity, System.Action<AstProjector,T> projection) {{}}\n    }}\n}}";
}
