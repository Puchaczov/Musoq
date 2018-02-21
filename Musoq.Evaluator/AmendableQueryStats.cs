using Musoq.Plugins;

namespace Musoq.Evaluator
{
    public class AmendableQueryStats : QueryStats
    {
        public new int RowNumber
        {
            get => base.RowNumber;
            set => base.RowNumber = value;
        }
    }
}