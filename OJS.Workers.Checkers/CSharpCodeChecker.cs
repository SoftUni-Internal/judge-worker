namespace OJS.Workers.Checkers
{
    using System;
    using System.CodeDom.Compiler;
    using System.Text;

    using Microsoft.CSharp;
    using OJS.Workers.Checkers.CSharpCodeCheckers;

    public class CSharpCodeChecker
        : CSharpCodeCheckerBase
    {
        protected override Type CompileCheckerAssembly(string sourceCode)
        {
            var codeProvider = new CSharpCodeProvider();
            var compilerParameters = new CompilerParameters { GenerateInMemory = true, };
            compilerParameters.ReferencedAssemblies.Add("System.dll");
            compilerParameters.ReferencedAssemblies.Add("System.Core.dll");
            compilerParameters.ReferencedAssemblies.Add("OJS.Workers.Common.dll");
            var compilerResults = codeProvider.CompileAssemblyFromSource(compilerParameters, sourceCode);

            this.CheckForErrors(compilerResults);
            return this.GetCustomCheckerType(compilerResults.CompiledAssembly);
        }

        private void CheckForErrors(CompilerResults compilerResults)
        {
            if (!compilerResults.Errors.HasErrors)
            {
                return;
            }

            var errorsStringBuilder = new StringBuilder();
            foreach (CompilerError error in compilerResults.Errors)
            {
                errorsStringBuilder.AppendLine(error.ToString());
            }

            // TODO: Introduce class CompilerException and throw exception of this type
            throw new Exception(
                string.Format(
                    "Could not compile checker!{0}Errors:{0}{1}",
                    Environment.NewLine,
                    errorsStringBuilder));
        }
    }
}
