using System;
using Fanex.Caching.Redis;
using Fanex.Data;
using Fanex.Logging;
using Fanex.Logging.Sentry.RavenClient;
using Fanex.TVS.AfterVoid.Website.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TicketVoidingSystem.Services;
using TicketVoidingSystem.SignalR;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// Add services to the container.
services.LoadConfig(configuration);
services.AddControllers();
services.AddMemoryCache();
services.AddHttpClient<TVSWebSiteHttpClientService, TVSWebSiteHttpClientServiceImp>();
services.AddHttpClient<CashOutHttpClientService, CashOutHttpClientServiceImp>();
services.AddFanexData(configuration);
services.AddExceptionHandler();
services.AddFanexLogSentry(configuration);
services.AddOneBookUm(configuration);
services.AddRedisCache(configuration.GetSection("Fanex.Caching.Redis"));
services.AddRemotingRef();
services.AddLetstalk();
services.AddAutoMapper();
services.AddTvsSignalR();
services.AddBusinessService();
services.AddBackgroundService();

// Build app
var app = builder.Build();
app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<NotificationHub>("/SignalR/NotificationHub");

var message = $"Fanex.TVS.AfterVoid.Service - Started - {DateTimeOffset.Now}";
Console.WriteLine(message);
await LogService.InfoAsync(message);

await app.RunAsync();

message = $"Fanex.TVS.AfterVoid.Service - Shutdown!!! - {DateTimeOffset.Now}";
Console.WriteLine(message);
await LogService.InfoAsync(message);