namespace OJS.Workers.ExecutionStrategies.Models
{
    using OJS.Workers.Common.Extensions;

    public class SimpleInputModel
        : BaseInputModel
    {
        public byte[] TaskSkeleton { get; set; }

        public string TaskSkeletonAsString => this.TaskSkeleton.Decompress();

        public string Input { get; set; }
    }
}