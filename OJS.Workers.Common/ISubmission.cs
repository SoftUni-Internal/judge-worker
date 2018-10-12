namespace OJS.Workers.Common
{
    using OJS.Workers.Common.Models;

    public interface ISubmission
    {
        object Id { get; set; }

        string AdditionalCompilerArguments { get; set; }

        string ProcessingComment { get; set; }

        int MemoryLimit { get; set; }

        int TimeLimit { get; set; }

        byte[] FileContent { get;  }

        string AllowedFileExtensions { get; }

        CompilerType CompilerType { get; set; }

        ExecutionContextType ExecutionContextType { get; set; }

        ExecutionStrategyType ExecutionStrategyType { get; set; }
    }
}