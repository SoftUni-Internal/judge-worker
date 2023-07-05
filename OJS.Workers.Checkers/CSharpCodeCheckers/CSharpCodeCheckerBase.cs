namespace OJS.Workers.Checkers.CSharpCodeCheckers
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Caching;
    using OJS.Workers.Common;

    public abstract class CSharpCodeCheckerBase
        : Checker
    {
        private const int CacheSlidingExpirationDays = 7;
        private readonly ObjectCache cache;
        private IChecker customChecker;

        public CSharpCodeCheckerBase()
                => this.cache = MemoryCache.Default;

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
    }
}