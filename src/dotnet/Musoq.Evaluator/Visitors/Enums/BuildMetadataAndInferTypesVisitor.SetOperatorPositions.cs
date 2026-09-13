using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private string CreateSetOperatorPositionKey()
    {
        var key = _queryState.SetKey++;
        return key.ToString(System.Globalization.CultureInfo.InvariantCulture).ToSetOperatorKey(key.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private string PreviousSetOperatorPositionKey()
    {
        return (_queryState.SetKey - 2).ToString(System.Globalization.CultureInfo.InvariantCulture).ToSetOperatorKey((_queryState.SetKey - 2).ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
