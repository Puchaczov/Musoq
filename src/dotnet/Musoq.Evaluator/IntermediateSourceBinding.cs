namespace Musoq.Evaluator;

internal readonly record struct IntermediateProducerId(int Value);
internal readonly record struct IntermediateSourceBinding(IntermediateProducerId ProducerId, string StorageName);
internal readonly record struct IntermediateSourceProducer(IntermediateSourceBinding Source, int StatementIndex);
