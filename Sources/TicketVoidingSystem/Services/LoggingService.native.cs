using System;
using System.Threading.Tasks;
using Fanex.Logging;
using Fanex.Logging.Extensions;

namespace TicketVoidingSystem.Services;

public interface LoggingService
{
    Task ErrorAsync(string message, Exception exception);

    Task ErrorAsync(string message);

    Task InfoAsync(string message);
}

public class LoggingServiceImp : LoggingService
{
    public async Task ErrorAsync(string message, Exception exception)
    {
        var logException = new Exception(message, exception);
        await HandleException(logException);
    }

    public async Task ErrorAsync(string message)
    {
        await LogService.ErrorAsync(message);
    }

    public async Task InfoAsync(string message)
    {
        await LogService.InfoAsync(message);
    }

    private static async Task HandleException(Exception exception)
    {
        var logMessage = new LogMessage(exception.Message)
            .AddSection("Server Info", CommonInfoGetters.GetServerInfo())
            .AddSection("Exception Info", CommonInfoGetters.GetExceptionInfo(exception.InnerException))
            .ToString();
        await LogService.ErrorAsync(logMessage);
    }
}