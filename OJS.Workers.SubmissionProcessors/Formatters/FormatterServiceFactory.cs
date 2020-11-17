namespace OJS.Workers.SubmissionProcessors.Formatters
{
    using System;

    using System.Collections.Generic;
    using OJS.Workers.Common.Models;

    public class FormatterServiceFactory
        : IFormatterServiceFactory
    {
        private readonly Dictionary<Type, object> services;

        public FormatterServiceFactory()
            => this.services = new Dictionary<Type, object>
            {
                { typeof(string), new CheckerFormatterService() },
                { typeof(ExecutionType), new ExecutionTypeFormatterService() },
                { typeof(ExecutionStrategyType), new ExecutionStrategyFormatterService() },
            };

        public IFormatterService<T> Get<T>()
            => this.services.ContainsKey(typeof(T))
                ? (IFormatterService<T>)this.services[typeof(T)]
                : null;
    }
}