using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record CapturedLocal(string Name, Type Type, string? GeneratedRowTypeName = null);
