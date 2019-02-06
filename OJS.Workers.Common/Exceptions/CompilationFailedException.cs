namespace OJS.Workers.Common.Exceptions
{
    using System;

    public class CompilationFailedException : Exception
    {
        public CompilationFailedException(string compilerComment)
            : base(compilerComment)
        {
        }
    }
}
