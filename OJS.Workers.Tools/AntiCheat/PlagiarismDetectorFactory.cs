﻿namespace OJS.Workers.Tools.AntiCheat
{
    using System;

    using OJS.Workers.Common;
    using OJS.Workers.Common.Models;
    using OJS.Workers.Compilers;
    using OJS.Workers.Tools.AntiCheat.Contracts;
    using OJS.Workers.Tools.Disassemblers;

    public class PlagiarismDetectorFactory : IPlagiarismDetectorFactory
    {
        public IPlagiarismDetector CreatePlagiarismDetector(PlagiarismDetectorCreationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            switch (context.Type)
            {
                case PlagiarismDetectorType.CSharpCompileDisassemble:
                    return new CSharpCompileDisassemblePlagiarismDetector(
                        new CSharpCompiler(Settings.CSharpCompilerProcessExitTimeOutMultiplier),
                        context.CompilerPath,
                        new DotNetDisassembler(context.DisassemblerPath),
                        context.SimilarityFinder);

                case PlagiarismDetectorType.CSharpDotNetCoreCompileDisassemble:
                    return new CSharpDotNetCoreCompileDisasaemblePlagiarismDetector(
                        new CSharpDotNetCoreCompiler(
                            Settings.CSharpDotNetCoreCompilerProcessExitTimeOutMultiplier,
                            Settings.CSharpDotNetCoreCompilerPath(ExecutionStrategyType.DotNetCoreCompileExecuteAndCheck),
                            Settings.DotNetCoreSharedAssembliesPath(ExecutionStrategyType.DotNetCoreCompileExecuteAndCheck)),
                        context.CompilerPath,
                        new DotNetDisassembler(context.DisassemblerPath),
                        context.SimilarityFinder);

                case PlagiarismDetectorType.JavaCompileDisassemble:
                    return new JavaCompileDisassemblePlagiarismDetector(
                        new JavaCompiler(Settings.JavaCompilerProcessExitTimeOutMultiplier),
                        context.CompilerPath,
                        new JavaDisassembler(context.DisassemblerPath),
                        context.SimilarityFinder);

                case PlagiarismDetectorType.PlainText:
                    return new PlainTextPlagiarismDetector(context.SimilarityFinder);

                default:
                    throw new ArgumentOutOfRangeException(nameof(context), "Invalid plagiarism detector type!");
            }
        }
    }
}
