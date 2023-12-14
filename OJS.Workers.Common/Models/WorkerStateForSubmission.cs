namespace OJS.Workers.Common.Models
{
    public enum WorkerStateForSubmission
    {
        Ready = 0,
        Unhealthy = 1,
        DisabledStrategy = 2,
        DisabledCompilerType = 3,
        NotEnabledStrategy = 5,
    }
}