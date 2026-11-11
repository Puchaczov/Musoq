using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private ExpressionSyntax RenderByteBufferProgram(ExecutionByteBufferProgram program, ExecutionRenderContext context)
    {
        var arguments = program.Inputs.Select(input => RenderExpression(input, context)).ToList();
        if (program.Concatenation is { } fallback)
            arguments.Add(
                CreateIdentifierName(
                    fallback.Target?.Name ?? throw new InvalidOperationException("Buffer fallback has no bound target.")
                )
            );
        return SyntaxFactory
            .InvocationExpression(CreateIdentifierName(program.HelperName))
            .WithArgumentList(CreateArgumentList(arguments.ToArray()));
    }

    private IEnumerable<MemberDeclarationSyntax> CreateByteBufferPrograms(
        ExecutionPlan plan,
        ExecutionRenderContext context
    )
    {
        var programs = ExecutionIrAnalysis
            .FlattenExpressions(plan.Body)
            .Select(expression =>
                expression switch
                {
                    ExecutionByteBufferSnapshot snapshot => snapshot.Program,
                    ExecutionByteBufferOperation operation => operation.Program,
                    _ => null,
                }
            )
            .Where(program => program is not null)
            .Cast<ExecutionByteBufferProgram>()
            .DistinctBy(program => program.HelperName);
        return programs.Select(program => CreateByteBufferProgram(program, context));
    }

    private MemberDeclarationSyntax CreateByteBufferProgram(
        ExecutionByteBufferProgram program,
        ExecutionRenderContext context
    )
    {
        var parameters = program
            .Inputs.Select(
                (input, index) =>
                    $"{(index == 0 && program.InputIsView ? ByteBufferViewShape.TypeName : CreateTypeSyntax(input.ReturnType).ToString())} __bufferArg{index.ToString(CultureInfo.InvariantCulture)}"
            )
            .ToList();
        if (program.Concatenation is { } fallback)
            parameters.Add($"{CreateTypeSyntax(fallback.Method.RequireClrMethod().DeclaringType!)} __bufferLibrary");
        var body = new StringBuilder();
        if (program.InputIsView)
            body.AppendLine(
                "bool __null = __bufferArg0.Storage is null; int __offset = __bufferArg0.Offset; int __length = __bufferArg0.LogicalLength;"
            );
        else if (program.SegmentCount == 0)
            body.AppendLine(
                "bool __null = __bufferArg0 is null; int __offset = 0; int __length = __bufferArg0?.Length ?? 0;"
            );
        else
            AppendConcatenationValidation(body, program, context);
        foreach (var step in program.Steps)
            AppendByteBufferStep(body, program, step);
        AppendByteBufferResult(body, program);
        var result = program.Result switch
        {
            ExecutionByteBufferResultKind.Length => "int?",
            ExecutionByteBufferResultKind.View => ByteBufferViewShape.TypeName,
            _ => "byte[]",
        };
        return SyntaxFactory.ParseMemberDeclaration(
            $"private static {result} {program.HelperName}({string.Join(", ", parameters)}) {{ {body} }}"
        )!;
    }

    private void AppendConcatenationValidation(
        StringBuilder body,
        ExecutionByteBufferProgram program,
        ExecutionRenderContext context
    )
    {
        var segments = Enumerable
            .Range(0, program.SegmentCount)
            .Select(index => "__bufferArg" + index.ToString(CultureInfo.InvariantCulture))
            .ToArray();
        body.Append("bool __invalid = ")
            .Append(string.Join(" || ", segments.Select(segment => segment + " is null")))
            .AppendLine(";");
        body.Append("long __total = ")
            .Append(string.Join(" + ", segments.Select(segment => $"(long)({segment}?.Length ?? 0)")))
            .AppendLine(";");
        body.AppendLine("if (__invalid || __total > int.MaxValue) {");
        var fallback =
            program.Concatenation ?? throw new InvalidOperationException("Concatenation fallback was not lowered.");
        var argumentType = fallback.Arguments[0].ReturnType;
        body.Append(CreateTypeSyntax(argumentType))
            .Append(" __bufferArg")
            .Append(program.Inputs.Count)
            .Append(" = (")
            .Append(CreateTypeSyntax(argumentType))
            .Append(")new byte[][] { ")
            .Append(string.Join(", ", segments))
            .AppendLine(" };");
        var call = RenderMethodCall(
            fallback with
            {
                Target = new ExecutionVariable("__bufferLibrary", fallback.Target!.Type),
            },
            context
        );
        body.Append("var __fallback = ").Append(call).AppendLine(";");
        body.AppendLine(
            program.Result switch
            {
                ExecutionByteBufferResultKind.Length => "return __fallback?.Length;",
                ExecutionByteBufferResultKind.View =>
                    $"return new {ByteBufferViewShape.TypeName}(__fallback, 0, __fallback?.Length ?? 0, __fallback?.Length ?? 0);",
                _ => "return __fallback;",
            }
        );
        body.AppendLine("}");
        body.AppendLine("bool __null = false; int __offset = 0; int __length = (int)__total;");
    }

    private static void AppendByteBufferResult(StringBuilder body, ExecutionByteBufferProgram program)
    {
        if (program.Result == ExecutionByteBufferResultKind.Length)
        {
            body.AppendLine("return __null ? (int?)null : __length;");
            return;
        }
        body.AppendLine(
            program.Result == ExecutionByteBufferResultKind.View
                ? $"if (__null) return new {ByteBufferViewShape.TypeName}(null, 0, 0, 0);"
                : "if (__null) return null;"
        );
        body.Append("int __available = global::System.Math.Min(__length, ")
            .Append((program.MaximumReadBytes ?? int.MaxValue).ToString(CultureInfo.InvariantCulture))
            .AppendLine(");");
        if (program.SegmentCount == 0)
        {
            var storage = program.InputIsView ? "__bufferArg0.Storage" : "__bufferArg0";
            body.Append(
                    "var __result = __available == 0 ? global::System.Array.Empty<byte>() : global::Musoq.Plugins.ByteBufferSnapshot.Copy("
                )
                .Append(storage)
                .AppendLine(", __offset, __available, __available);");
            AppendStoredBufferReturn(body, program);
            return;
        }
        if (program.EmptyAllocation == ExecutionByteBufferEmptyAllocation.Shared)
            body.AppendLine(
                program.Result == ExecutionByteBufferResultKind.View
                    ? $"if (__available == 0) return new {ByteBufferViewShape.TypeName}(global::System.Array.Empty<byte>(), 0, 0, __length);"
                    : "if (__available == 0) return global::System.Array.Empty<byte>();"
            );
        body.AppendLine(
            "var __result = new byte[__available]; int __remaining = __available; int __skip = __offset; int __position = 0;"
        );
        for (var index = 0; index < program.SegmentCount; index++)
        {
            var segment = "__bufferArg" + index.ToString(CultureInfo.InvariantCulture);
            body.Append("if (__skip >= ")
                .Append(segment)
                .Append(".Length) { __skip -= ")
                .Append(segment)
                .AppendLine(".Length; } else {");
            body.Append("int __copy = global::System.Math.Min(__remaining, ")
                .Append(segment)
                .AppendLine(".Length - __skip);");
            body.Append("global::System.Array.Copy(")
                .Append(segment)
                .AppendLine(", __skip, __result, __position, __copy);");
            body.AppendLine("__position += __copy; __remaining -= __copy; __skip = 0; }");
        }
        AppendStoredBufferReturn(body, program);
    }

    private static void AppendStoredBufferReturn(StringBuilder body, ExecutionByteBufferProgram program) =>
        body.AppendLine(
            program.Result == ExecutionByteBufferResultKind.View
                ? $"return new {ByteBufferViewShape.TypeName}(__result, 0, __available, __length);"
                : "return __result;"
        );

    private ExpressionSyntax RenderByteBufferViewAccess(
        ExecutionByteBufferOperation view,
        ExecutionRenderContext context
    )
    {
        var value = RenderExpression(view.Original, context);
        return SyntaxFactory.ParseExpression(
            view.ViewAccess == ExecutionByteBufferViewAccess.Storage
                ? $"{value}.Storage"
                : $"({value}.Storage is null ? (int?)null : {value}.LogicalLength)"
        );
    }
}
