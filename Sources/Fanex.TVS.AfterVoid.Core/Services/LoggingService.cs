using System;
using System.Diagnostics.CodeAnalysis;
using Fanex.Logging;
using Fanex.Logging.Extensions;
using Fanex.Logging.Extensions.Database;

namespace Fanex.TVS.AfterVoid.Core.Services
{
    public interface LoggingService
    {
        void Error(string message, Exception exception);

        void Error(Exception exception);

        void Error(string message);

        void Info(string message);
    }

    [ExcludeFromCodeCoverage]
    public class LoggingServiceImp : LoggingService
    {
        public void Error(string message, Exception exception)
        {
            var logException = new Exception(message, exception);
            HandleException(logException);
        }

        public void Error(Exception exception)
        {
            HandleException(exception);
        }

        public void Error(string message)
        {
            LogService.Error(message);
        }

        public void Info(string message)
        {
            LogService.Info(message);
        }

        private static void HandleException(Exception exception)
        {
            var logMessage = new LogMessage(exception.Message)
                .AddSection("Database Info", DatabaseInfoGetters.GetDbExceptionInfo(exception))
                .AddSection("Server Info", CommonInfoGetters.GetServerInfo())
                .AddSection("Exception Info", CommonInfoGetters.GetExceptionInfo(exception.InnerException))
                .ToString();
            LogService.Error(logMessage);
        }
    }
}