using System.Text;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private static void AppendGeneratedLine(StringBuilder builder, string line)
    {
        builder.AppendLine(line);
    }
}
