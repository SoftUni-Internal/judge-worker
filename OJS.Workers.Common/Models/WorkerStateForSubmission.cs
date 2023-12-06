namespace OJS.Workers.Common.Models
{
    public enum WorkerStateForSubmission
    {
        Ready = 0,
        Unhealthy = 1,
        DisabledStrategy = 2,
        DisabledCompilerType = 3,
        NullableSubmission = 4,
        NotEnabledStrategy = 5,
    }
}