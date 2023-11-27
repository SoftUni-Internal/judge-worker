namespace OJS.Workers.Tools;

using OJS.Services.Worker.Models.Anti_Cheating;
using OJS.Workers.Common;

public class CSharpDotNetCoreCompileDisassemblePlagiarismDetector
    : CompileDisassemblePlagiarismDetector
{
    private const string CSharpCompilerAdditionalArguments = "-nologo";

    public CSharpDotNetCoreCompileDisassemblePlagiarismDetector(
        ICompiler compiler,
        string compilerPath,
        IDisassembler disassembler,
        ISimilarityFinder similarityFinder)
        : base(compiler, compilerPath, disassembler, similarityFinder)
    {
    }

    protected override string CompilerAdditionalArguments => CSharpCompilerAdditionalArguments;
}