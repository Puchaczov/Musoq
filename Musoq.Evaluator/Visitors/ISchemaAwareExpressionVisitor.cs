using System.Text;
using Musoq.Evaluator.Utils;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors
{
    public interface IScopeAwareExpressionVisitor : IExpressionVisitor
    {
        void QueryBegins();
        void QueryEnds();

        void SetScope(Scope scope);

        void SetQueryIdentifier(string identifier);

        void SetCodePattern(StringBuilder code);
        void SetJoinsAmount(int amount);

        void SetMethodAccessType(MethodAccessType type);
    }
}