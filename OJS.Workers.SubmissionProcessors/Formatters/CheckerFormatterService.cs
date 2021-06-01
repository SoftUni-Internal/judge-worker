namespace OJS.Workers.SubmissionProcessors.Formatters
{
    using System.Collections.Generic;

    using OJS.Workers.Common.Extensions;

    public class CheckerFormatterService
        : ICheckerFormatterService
    {
        private readonly IDictionary<string, string> map;

        public CheckerFormatterService()
            => this.map = new Dictionary<string, string>()
            {
                { "trim-checker", "trim" },
                { "trim-end-checker", "trim-end" },
                { "case-insensitive-checker", "case-insensitive" },
                { "precision-checker", "precision" },
                { "sort-checker", "sort" },
                { "c-sharp-code-checker", "csharp-code" },
                { "exact-checker", "exact-match" },
            };

        public string Format(string obj)
            => this.map.ContainsKey(obj.ToHyphenSeparatedWords())
                ? this.map[obj.ToHyphenSeparatedWords()]
                : obj.ToHyphenSeparatedWords();
    }
}