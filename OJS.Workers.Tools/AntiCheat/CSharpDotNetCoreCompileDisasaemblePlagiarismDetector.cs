namespace OJS.Workers.Tools.AntiCheat
{
    using OJS.Workers.Common;
    using OJS.Workers.Tools.Disassemblers.Contracts;
    using OJS.Workers.Tools.Similarity;

    public class CSharpDotNetCoreCompileDisasaemblePlagiarismDetector
        : CompileDisassemblePlagiarismDetector
    {
        private const string CSharpCompilerAdditionalArguments = "-nologo";

        public CSharpDotNetCoreCompileDisasaemblePlagiarismDetector(
            ICompiler compiler,
            string compilerPath,
            IDisassembler disassembler,
            ISimilarityFinder similarityFinder)
            : base(compiler, compilerPath, disassembler, similarityFinder)
        {
        }

        protected override string CompilerAdditionalArguments => CSharpCompilerAdditionalArguments;
    }
}
