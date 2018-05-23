using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Evaluator
{
    public interface IRunnable
    {
        ISchemaProvider Provider { get; set; }
        Table Run();
    }
}