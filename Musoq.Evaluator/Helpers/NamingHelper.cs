namespace Musoq.Evaluator.Helpers
{
    public static class NamingHelper
    {
        public static string ToTransitionTable(this string name)
        {
            return $"{name}TransitionTable";
        }

        public static string ToGroupingTable(this string name)
        {
            return $"{name}GroupingTable";
        }

        public static string ToTransformedScore(this string name)
        {
            return $"{name}TransformedScore";
        }

        public static string ToRowsSource(this string name)
        {
            return $"{name}Rows";
        }

        public static string ToRowItem(this string name)
        {
            return $"{name}Row";
        }

        public static string ToScoreTable(this string name)
        {
            return $"{name}Score";
        }

        public static string ToTransformedRowsSource(this string name)
        {
            return $"{nameof(EvaluationHelper)}.{nameof(EvaluationHelper.ConvertTableToSource)}({name}).Rows";
        }

        public static string WithRowsUsage(this string name)
        {
            return $"{name}.Rows";
        }

        public static string ToColumnName(string alias, string name)
        {
            return $"{alias}.{name}";
        }

        public static string ToColumnName(string alias, string name, string scope)
        {
            return $"{alias}:{scope}['{name}']";
        }
    }
}
