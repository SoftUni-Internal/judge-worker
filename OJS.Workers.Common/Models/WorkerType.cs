namespace OJS.Workers.Common.Models
{
    /// <summary>
    /// Subset of workers on which a submission is to be executed.
    /// Stored in database, so be careful with changing values.
    /// </summary>
    public enum WorkerType
    {
        Default = 0,
        Legacy = 1,
        Local = 2,
        Alpha = 3,
        LegacyCloud = 4,
    }
}