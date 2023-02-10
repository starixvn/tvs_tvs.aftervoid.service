using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;
using Fanex.Logging.Extensions.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Fanex.TVS.AfterVoid.Website.Utilities;

[ExcludeFromCodeCoverage]
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate next;
    public IConfiguration Configuration { get; }

    public ExceptionHandlerMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        this.next = next;
        Configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await ExceptionHandler.HandleAsync(exception, context);
            await HandleException(context, exception);
        }
    }

    private static async Task HandleException(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        var errorMessage = $"<b>Exception Error: {exception.Message} </b> {exception.StackTrace}";
        await context.Response.WriteAsync(errorMessage).ConfigureAwait(false);
    }
}