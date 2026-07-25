using System;

namespace Musoq.Evaluator;

internal sealed class SchemaProviderFailureException(Exception innerException)
    : Exception("The schema provider failed while resolving a schema.", innerException);
