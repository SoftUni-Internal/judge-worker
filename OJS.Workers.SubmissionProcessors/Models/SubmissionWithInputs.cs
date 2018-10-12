namespace OJS.Workers.SubmissionProcessors.Models
{
    using System.Collections.Generic;

    public class SubmissionWithInputs : BaseSubmission
    {
        public IEnumerable<string> Inputs { get; set; }
    }
}