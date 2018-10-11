namespace OJS.Workers.ExecutionStrategies.Models
{
    using OJS.Workers.Common;

    public class RawResult : Result
    {
        public ProcessExecutionResultType ResultType { get; set; }

        public string Output { get; set; }
    }
}