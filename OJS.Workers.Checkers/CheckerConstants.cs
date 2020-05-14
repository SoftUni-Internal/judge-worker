namespace OJS.Workers.Checkers
{
    public class CheckerConstants
    {
        public static class TypeNames
        {
            public const string ExactMatch = nameof(ExactChecker);
            public const string CaseInsensitive = nameof(CaseInsensitiveChecker);
            public const string Precision = nameof(PrecisionChecker);
            public const string Sort = nameof(SortChecker);
            public const string Trim = nameof(TrimChecker);
            public const string TrimEnd = nameof(TrimEndChecker);
        }
    }
}