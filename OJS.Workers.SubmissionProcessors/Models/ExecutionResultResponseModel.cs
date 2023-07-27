namespace OJS.Workers.SubmissionProcessors.Models
{
    public class ExecutionResultResponseModel
    {
        public string Id { get; set; }

        public bool IsCompiledSuccessfully { get; set; }

        public string CompilerComment { get; set; }

        public OutputResultResponseModel OutputResult { get; set; }

        public TaskResultResponseModel TaskResult { get; set; }
    }
}