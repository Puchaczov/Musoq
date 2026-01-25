using System;

namespace Musoq.Evaluator.Exceptions;

public class FieldLinkIndexOutOfRangeException(int index, int groups) : Exception(
    $"There is no group selected by '{index}' value. Max allowed group index for this query is {groups}");
