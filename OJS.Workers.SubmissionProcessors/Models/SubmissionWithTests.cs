namespace OJS.Workers.SubmissionProcessors.Models
{
    using System.Collections.Generic;

    using OJS.Workers.ExecutionStrategies.Models;

    public class SubmissionWithTests : BaseSubmission
    {
        public string CheckerAssemblyName { get; set; }

        public string CheckerParameter { get; set; }

        public string CheckerTypeName { get; set; }

        public IEnumerable<TestContext> Tests { get; set; }

        public byte[] TaskSkeleton { get; set; }
    }
}