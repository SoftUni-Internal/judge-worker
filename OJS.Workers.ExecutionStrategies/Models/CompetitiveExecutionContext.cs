namespace OJS.Workers.ExecutionStrategies.Models
{
    using System.Collections.Generic;

    using OJS.Workers.Common.Extensions;

    public class CompetitiveExecutionContext : ExecutionContext<TestContext>
    {
        public int SubmissionId { get; set; }

        public string CheckerAssemblyName { get; set; }

        public string CheckerTypeName { get; set; }

        public string CheckerParameter { get; set; }

        public byte[] TaskSkeleton { get; set; }

        public string TaskSkeletonAsString => this.TaskSkeleton.Decompress();

        public override IEnumerable<TestContext> Tests { get; set; }
    }
}