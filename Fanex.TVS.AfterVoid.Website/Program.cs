using Fanex.TVS.AfterVoid.Website.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TicketVoidingSystem.Services;
using TicketVoidingSystem.SignalR;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add services to the container.
services.LoadConfig(builder.Configuration);
services.AddControllers();
services.AddMemoryCache();
services.AddHttpClient<TVSWebSiteHttpClientService, TVSWebSiteHttpClientServiceImp>();
services.AddHttpClient<CashOutHttpClientService, CashOutHttpClientServiceImp>();
services.RegisterFanexData(builder.Configuration);
services.RegisterFanexExceptionHandler();
services.RegisterSentry();
services.RegisterONEbookUM(builder.Configuration);
services.RegisterFanexCaching(builder.Configuration);
services.RegisterRemotingRef();
services.RegisterLetstalk();
services.RegisterAutoMapper();
services.RegisterSignalR();
services.RegisterBusinessService();
services.RegisterQuartzScheduler();

// Build app
var app = builder.Build();
app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<NotificationHub>("/SignalR/NotificationHub");

app.Run();