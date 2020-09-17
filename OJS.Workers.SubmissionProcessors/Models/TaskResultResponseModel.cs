namespace OJS.Workers.SubmissionProcessors.Models
{
    using System.Collections.Generic;

    public class TaskResultResponseModel
    {
        private const string ExecutionTimeValue = "just now";

        public int Points { get; set; }

        public string TimeElapsedFormatted => ExecutionTimeValue;

        public IEnumerable<TestResultResponseModel> TestResults { get; set; }
    }
}