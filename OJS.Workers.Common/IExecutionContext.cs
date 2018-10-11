namespace OJS.Workers.Common
{
    using OJS.Workers.Common.Models;

    public interface IExecutionContext
    {
        CompilerType CompilerType { get; set; }

        string AdditionalCompilerArguments { get; set; }

        string Code { get; }

        byte[] FileContent { get; set; }

        string AllowedFileExtensions { get; set; }

        int TimeLimit { get; set; }

        int MemoryLimit { get; set; }
    }
}