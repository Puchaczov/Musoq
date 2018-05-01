using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Evaluator
{
    public interface IRunnable
    {
        Table Run();

        ISchemaProvider Provider { get; set; }
    }
}