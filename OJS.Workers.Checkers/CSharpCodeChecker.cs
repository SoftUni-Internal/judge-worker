namespace OJS.Workers.Checkers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Caching;
    using System.Text.RegularExpressions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Emit;
    using Microsoft.CodeAnalysis.Text;

    using OJS.Workers.Common;

    public class CSharpCodeChecker : Checker
    {
        private const int CacheSlidingExpirationDays = 7;

        private readonly ObjectCache cache;

        private IChecker customChecker;

        public CSharpCodeChecker() => this.cache = MemoryCache.Default;

        public override CheckerResult Check(string inputData, string receivedOutput, string expectedOutput, bool isTrialTest)
        {
            if (this.customChecker == null)
            {
                throw new InvalidOperationException("Please call SetParameter first with non-null string.");
            }

            var result = this.customChecker.Check(inputData, receivedOutput, expectedOutput, isTrialTest);
            return result;
        }

        public override void SetParameter(string parameter)
        {
            if (this.cache[parameter] is IChecker customCheckerFromCache)
            {
                this.customChecker = customCheckerFromCache;
                return;
            }

            var type = this.CompileCheckerAssembly(parameter);

            if (Activator.CreateInstance(type) is IChecker instance)
            {
                this.cache.Set(
                    parameter,
                    instance,
                    new CacheItemPolicy { SlidingExpiration = TimeSpan.FromDays(CacheSlidingExpirationDays) });

                this.customChecker = instance;
            }
            else
            {
                throw new Exception($"Cannot create an instance of type {type.FullName}!");
            }
        }

        private Type GetCustomCheckerType(byte[] assemblyBytesArray)
        {
            var assembly = Assembly.Load(assemblyBytesArray);

            var types = assembly
                .GetTypes()
                .Where(x => typeof(IChecker).IsAssignableFrom(x))
                .ToList();

            if (types.Count > 1)
            {
                throw new Exception("More than one implementation of OJS.Workers.Common.IChecker was found!");
            }

            var type = types.FirstOrDefault();
            if (type == null)
            {
                throw new Exception("Implementation of OJS.Workers.Common.IChecker not found!");
            }

            return type;
        }

        private void CheckForErrors(EmitResult result)
        {
            if (result.Success)
            {
                return;
            }

            var errors = result.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error);
            var errorsString = string.Join(",", errors.Select(x => x.GetMessage()));

            // TODO: Introduce class CompilerException and throw exception of this type
            throw new Exception(
                string.Format(
                    "Could not compile checker!{0}Errors:{0}{1}",
                    Environment.NewLine,
                    errorsString));
        }

        private Type CompileCheckerAssembly(string sourceCode)
        {
            var codeString = SourceText.From(sourceCode);
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_3);

            var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);
            var systemDir = Path.GetDirectoryName(typeof(object).Assembly.Location);

            var references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute)
                    .Assembly
                    .Location),
                MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo)
                    .Assembly
                    .Location),
                MetadataReference.CreateFromFile(typeof(Regex).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IChecker).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(systemDir, "System.Collections.dll")),
                MetadataReference.CreateFromFile(Path.Combine(systemDir, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(Path.Combine(systemDir, "netstandard.dll")),
            };

            using (var assemblyStream = new MemoryStream())
            {
                var result = CSharpCompilation.Create(
                       Guid.NewGuid().ToString(),
                       new[] { parsedSyntaxTree },
                       references: references,
                       options: new CSharpCompilationOptions(
                           OutputKind.DynamicallyLinkedLibrary,
                           optimizationLevel: OptimizationLevel.Release,
                           assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));
                var compilationResult = result.Emit(assemblyStream);
                this.CheckForErrors(compilationResult);
                assemblyStream.Seek(0, SeekOrigin.Begin);
                var type = this.GetCustomCheckerType(assemblyStream.ToArray());

                return type;
            }
        }
    }
}
