namespace OJS.Workers.Executors.Process
{
    using System;

    internal struct ProcessThreadTimes
    {
        public long create;
        public long exit;
        public long kernel;
        public long user;

        public DateTime StartTime => DateTime.FromFileTime(this.create);

        public DateTime ExitTime => DateTime.FromFileTime(this.exit);

        public TimeSpan PrivilegedProcessorTime => new TimeSpan(this.kernel);

        public TimeSpan UserProcessorTime => new TimeSpan(this.user);

        public TimeSpan TotalProcessorTime => new TimeSpan(this.user + this.kernel);
    }
}
