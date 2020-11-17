namespace OJS.Workers.SubmissionProcessors.Formatters
{
    using OJS.Workers.Common.Extensions;
    using OJS.Workers.Common.Models;

    public class ExecutionTypeFormatterService
        : IExecutionTypeFormatterService
    {
        public string Format(ExecutionType obj)
            => obj.ToString().ToHyphenSeparatedWords();
    }
}