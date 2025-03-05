namespace OJS.Workers.SubmissionProcessors.Models
{
    using OJS.Workers.Common.Models;

    public class ExceptionModel
    {
        public string Message { get; set; }

        public string StackTrace { get; set; }

        public ExceptionType? ExceptionType { get; set; }
    }
}