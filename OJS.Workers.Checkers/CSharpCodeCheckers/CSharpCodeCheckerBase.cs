namespace OJS.Workers.Checkers.CSharpCodeCheckers
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

    public abstract class CSharpCodeCheckerBase
        : Checker
    {
        private IChecker customChecker;
        private readonly ObjectCache cache;
        private const int CacheSlidingExpirationDays = 7;

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

        protected abstract Type CompileCheckerAssembly(string sourceCode);

        protected Type GetCustomCheckerType(Assembly assembly)
        {
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
    }
}