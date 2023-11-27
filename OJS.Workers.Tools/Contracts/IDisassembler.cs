using OJS.Workers.Tools;

namespace OJS.Services.Worker.Models.Anti_Cheating;

public interface IDisassembler
{
    DisassembleResult Disassemble(string compiledFilePath, string? additionalArguments = null);
}