namespace OJS.Workers.ExecutionStrategies.Models
{
    using System.Collections.Generic;

    public class NonCompetitiveExecutionContext : ExecutionContext<string>
    {
        public override IEnumerable<string> Tests { get; set; }
    }
}