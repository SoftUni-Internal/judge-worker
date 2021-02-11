using System.Reflection;
using OJS.Workers.Checkers.CSharpCodeCheckers;

namespace OJS.Workers.Checkers
{
    using System;
    using System.CodeDom.Compiler;
    using System.Linq;
    using System.Runtime.Caching;
    using System.Text;

    using Microsoft.CSharp;

    using OJS.Workers.Common;

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
            if (compilerResults.Errors.HasErrors)
            {
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

            return this.GetCustomCheckerType(compilerResults.CompiledAssembly);
        }
    }
}
