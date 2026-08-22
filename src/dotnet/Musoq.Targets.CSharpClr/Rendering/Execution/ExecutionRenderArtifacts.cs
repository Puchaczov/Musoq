using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal sealed record ExecutionRenderArtifacts(
    ExecutionRenderContext RenderContext,
    IReadOnlyList<StatementSyntax> SetupStatements,
    int EntryStatementCount);
