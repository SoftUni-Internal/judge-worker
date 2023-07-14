using SoftUni.Services.Infrastructure;

namespace OJS.Workers.SubmissionProcessors.Formatters
{
    public interface IFormatterServiceFactory : ISingletonService
    {
        IFormatterService<T> Get<T>();
    }
}