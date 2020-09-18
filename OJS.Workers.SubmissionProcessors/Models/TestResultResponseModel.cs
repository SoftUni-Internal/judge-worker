namespace OJS.Workers.SubmissionProcessors.Models
{
    public class TestResultResponseModel
    {
        public int Id { get; set; }

        public string ResultType { get; set; }

        public string ExecutionComment { get; set; }

        public string Output { get; set; }
    }
}