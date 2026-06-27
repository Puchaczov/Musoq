using System;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.CodeGeneration;

namespace Musoq.Converter;

public interface ITypedQueryDiagnosticsProvider
{
    TypedQueryDiagnostics Diagnostics { get; }
}
