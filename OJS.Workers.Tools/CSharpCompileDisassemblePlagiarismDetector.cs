namespace OJS.Workers.Tools;

using OJS.Services.Worker.Models.Anti_Cheating;
using OJS.Workers.Common;

public class CSharpCompileDisassemblePlagiarismDetector : CompileDisassemblePlagiarismDetector
{
    private const string CSharpCompilerAdditionalArguments =
        "/optimize+ /nologo /reference:System.Numerics.dll /reference:PowerCollections.dll";

    public CSharpCompileDisassemblePlagiarismDetector(
        ICompiler compiler,
        string compilerPath,
        IDisassembler disassembler,
        ISimilarityFinder similarityFinder)
        : base(compiler, compilerPath, disassembler, similarityFinder)
    {
    }

    protected override string CompilerAdditionalArguments => CSharpCompilerAdditionalArguments;
}