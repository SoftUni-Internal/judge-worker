namespace OJS.Workers.Common
{
    using OJS.Workers.Common.Models;

    public interface ISubmission
    {
        object Id { get; }

        string AdditionalCompilerArguments { get; }

        string ProcessingComment { get; set; }

        int MemoryLimit { get; }

        int TimeLimit { get; }

        string Code { get; }

        byte[] FileContent { get; }

        string AllowedFileExtensions { get; }

        CompilerType CompilerType { get; }

        ExecutionContextType ExecutionContextType { get; }

        ExecutionStrategyType ExecutionStrategyType { get; }
    }
}