namespace Musoq.Evaluator;

public interface IProfiledTypedRunnable<TOut>
{
    TypedQueryProfileResult<TOut> RunWithProfile(TypedQueryRunOptions options);
}
