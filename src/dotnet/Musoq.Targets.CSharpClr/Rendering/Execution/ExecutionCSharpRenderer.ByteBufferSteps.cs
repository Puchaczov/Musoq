using System.Globalization;
using System.Text;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static void AppendByteBufferStep(StringBuilder body, ExecutionByteBufferProgram program, ExecutionByteBufferStep step)
    {
        var argument = "__bufferArg" + step.Argument.ToString(CultureInfo.InvariantCulture);
        switch (step.Kind)
        {
            case ExecutionByteBufferStepKind.Skip:
                body.Append("{ int __skipCount = global::System.Math.Min(global::System.Math.Max(").Append(argument)
                    .AppendLine(", 0), __length); __offset += __skipCount; __length -= __skipCount; }");
                return;
            case ExecutionByteBufferStepKind.Take:
                body.Append("__length = global::System.Math.Min(__length, global::System.Math.Max(").Append(argument).AppendLine(", 0));");
                return;
            case ExecutionByteBufferStepKind.NullWhenEmpty:
                body.AppendLine("if (__length == 0) __null = true;");
                return;
            case ExecutionByteBufferStepKind.CheckedRange:
                AppendCheckedBufferRange(body, program, step);
                return;
            default:
                throw new NotSupportedException($"Unknown buffer step '{step.Kind}'.");
        }
    }

    private static void AppendCheckedBufferRange(StringBuilder body, ExecutionByteBufferProgram program, ExecutionByteBufferStep step)
    {
        if (program.SegmentCount != 0)
            throw new InvalidOperationException("Checked range validation requires a materialized input boundary.");
        body.Append("{ int? __start = (int?)__bufferArg").Append(step.Argument.ToString(CultureInfo.InvariantCulture))
            .Append("; int? __count = (int?)__bufferArg").Append(step.LengthArgument.ToString(CultureInfo.InvariantCulture)).AppendLine(";");
        body.AppendLine("if (__null || !__start.HasValue || !__count.HasValue) { __null = true; __length = 0; } else {");
        body.AppendLine("int __s = __start.GetValueOrDefault(); int __c = __count.GetValueOrDefault();");
        body.AppendLine("if (__s < 0 || __c < 0 || __s > __length - __c) {");
        body.AppendLine("var __original = global::Musoq.Plugins.ByteBufferSnapshot.Copy(__bufferArg0, __offset, __length, __length);");
        body.AppendLine("_ = global::Musoq.Plugins.ByteBufferSnapshot.Copy(__original, __s, __c, 0); }");
        body.AppendLine("__offset += __s; __length = __c; } }");
    }
}
