using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

/// <summary>
///     Comprehensive stress tests for the binary and text interpretation schema implementation.
///     Covers edge cases, boundary conditions, and complex compositions derived from the
///     Musoq Interpretation Schemas specification (musoq-binary-text-spec.md).
/// </summary>
[TestClass]
public partial class StressTestsInterpretationSchemasTests
{
    private static readonly ILoggerResolver LoggerResolver = new TestsLoggerResolver();
    private static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);


}
