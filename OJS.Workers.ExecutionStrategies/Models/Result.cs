namespace OJS.Workers.ExecutionStrategies.Models
{
    public abstract class Result
    {
        public int TimeUsed { get; set; }

        public int MemoryUsed { get; set; }

        public string ExecutionComment { get; set; }
    }
}