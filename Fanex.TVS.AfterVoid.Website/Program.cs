using Fanex.Data;
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
services.AddFanexData(builder.Configuration);
services.AddExceptionHandler();
services.AddSentry();
services.AddOneBookUm(builder.Configuration);
services.AddFanexCaching(builder.Configuration);
services.AddRemotingRef();
services.AddLetstalk();
services.AddAutoMapper();
services.AddTvsSignalR();
services.AddBusinessService();
services.AddQuartzScheduler();
// Build app
var app = builder.Build();
app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<NotificationHub>("/SignalR/NotificationHub");

await app.RunAsync();