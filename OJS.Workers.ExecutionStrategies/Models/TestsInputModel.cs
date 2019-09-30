namespace OJS.Workers.ExecutionStrategies.Models
{
    using System.Collections.Generic;

    using OJS.Workers.Checkers;
    using OJS.Workers.Common;
    using OJS.Workers.Common.Extensions;

    using static OJS.Workers.Common.Constants;

    public class TestsInputModel
    {
        public string CheckerAssemblyName { get; set; } = DefaultCheckerAssemblyName;

        public string CheckerTypeName { get; set; }

        public string CheckerParameter { get; set; }

        public byte[] TaskSkeleton { get; set; }

        public string TaskSkeletonAsString => this.TaskSkeleton.Decompress();

        public IEnumerable<TestContext> Tests { get; set; }

        public IChecker GetChecker() => Checker.CreateChecker(
            this.CheckerAssemblyName,
            this.CheckerTypeName,
            this.CheckerParameter);
    }
}