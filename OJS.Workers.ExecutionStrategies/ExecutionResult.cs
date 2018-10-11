namespace OJS.Workers.ExecutionStrategies
{
    using System.Collections.Generic;
    using OJS.Workers.ExecutionStrategies.Models;

    public class ExecutionResult
    {
        public bool IsCompiledSuccessfully { get; set; }

        public string CompilerComment { get; set; }

        public List<RawResult> RawResults { get; set; } = new List<RawResult>();

        public List<TestResult> TestResults { get; set; } = new List<TestResult>();
    }
}