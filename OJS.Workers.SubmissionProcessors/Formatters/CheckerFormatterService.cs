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
                { "c-sharp-code-checker", "csharp-code" },
            };

        public string Format(string obj)
            => this.map.ContainsKey(obj.ToHyphenSeparatedWords())
                ? this.map[obj.ToHyphenSeparatedWords()]
                : obj.ToHyphenSeparatedWords();
    }
}