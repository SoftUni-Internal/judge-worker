using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using System.Text.RegularExpressions;

namespace OJS.Workers.Checkers
{
    using System;
    using System.Text;
    using OJS.Workers.Checkers.CSharpCodeCheckers;

    public class CSharpCodeChecker
        : CSharpCodeCheckerBase
    {
        protected override Type CompileCheckerAssembly(string sourceCode)
        {
           var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Regex).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")),
                MetadataReference.CreateFromFile("OJS.Workers.Common.dll"),

            };

            var compilation = CSharpCompilation.Create(
                "CheckerAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);

            if (!result.Success)
            {
                var failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                var errorsStringBuilder = new StringBuilder();
                foreach (var diagnostic in failures)
                {
                    errorsStringBuilder.AppendLine($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                }

                throw new Exception(
                    $"Could not compile checker!{Environment.NewLine}Errors:{Environment.NewLine}{errorsStringBuilder}");
            }

            ms.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(ms.ToArray());
            return this.GetCustomCheckerType(assembly);
        }
    }
}