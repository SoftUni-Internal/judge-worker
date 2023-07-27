namespace OJS.Workers.SubmissionProcessors.Formatters
{
    public interface IFormatterServiceFactory
    {
        IFormatterService<T> Get<T>();
    }
}