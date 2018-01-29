namespace FQL.Evaluator.Tables
{
    public interface IValue<in TKey>
    {
        bool FitsTheIndex(TKey key);
    }
}