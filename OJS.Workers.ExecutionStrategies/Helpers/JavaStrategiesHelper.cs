namespace OJS.Workers.ExecutionStrategies.Helpers
{
    using System;

    using OJS.Workers.Common.Helpers;

    internal static class JavaStrategiesHelper
    {
        private const string JvmInsufficientMemoryMessage =
            "There is insufficient memory for the Java Runtime Environment to continue.";

        private const string JvmFailedToReserveMemoryMessage =
            "Failed to allocate initial concurrent mark overflow mark stack.";

        private const string JvmInitializationErrorMessage = "Error occurred during initialization of VM";

        public static char ClassPathArgumentSeparator
            => OSPlatformHelpers.IsWindows() ? ';' : ':';

        /// <summary>
        /// Validates if the Java Virtual Machine has been initialized successfully,
        /// by checking for critical error messages and throws exception when has any.
        /// </summary>
        /// <param name="processReceivedOutput">The received output from the process executor</param>
        public static void ValidateJvmInitialization(string processReceivedOutput)
        {
            const string errorMessageAppender = " Please contact an administrator.";

            if (processReceivedOutput.Contains(JvmInsufficientMemoryMessage))
            {
                throw new InsufficientMemoryException(JvmInsufficientMemoryMessage + errorMessageAppender);
            }

            if (processReceivedOutput.Contains(JvmFailedToReserveMemoryMessage))
            {
                throw new InsufficientMemoryException(JvmFailedToReserveMemoryMessage + errorMessageAppender);
            }

            if (processReceivedOutput.Contains(JvmInitializationErrorMessage))
            {
                throw new Exception(JvmInitializationErrorMessage + errorMessageAppender);
            }
        }
    }
}
