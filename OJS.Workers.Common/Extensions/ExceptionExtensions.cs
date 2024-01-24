namespace OJS.Workers.Common.Extensions
{
    using System;
    using System.Collections.Generic;

    public static class ExceptionExtensions
    {
        public static string GetAllMessages(this Exception exception)
        {
            var allMessages = new List<string>
            {
                exception.Message,
            };

            var innerException = exception.InnerException;

            while (innerException != null)
            {
                if (!string.IsNullOrWhiteSpace(innerException.Message))
                {
                    allMessages.Add(innerException.Message);
                }

                innerException = innerException.InnerException;
            }

            return string.Join(Environment.NewLine, allMessages);
        }
    }
}
