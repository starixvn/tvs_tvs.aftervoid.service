using Fanex.TVS.AfterVoid.Website.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TicketVoidingSystem.Services;
using TicketVoidingSystem.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.LoadConfig(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddHttpClient<TicketVoidingSystemHttpClientService, TicketVoidingSystemHttpClientServiceImp>();
builder.Services.AddHttpClient<CashOutHttpClientService, CashOutHttpClientServiceImp>();
builder.Services.RegisterFanexData(builder.Configuration);
builder.Services.RegisterFanexExceptionHandler();
builder.Services.RegisterSentry();
builder.Services.RegisterONEbookUM(builder.Configuration);
builder.Services.RegisterFanexCaching(builder.Configuration);
builder.Services.RegisterRemotingRef();
builder.Services.RegisterMailService();
builder.Services.RegisterLetstalk();
builder.Services.RegisterAutoMapper();
builder.Services.RegisterSignalR();
builder.Services.RegisterBusinessService();
builder.Services.RegisterQuartzScheduler();

// Build app
var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<NotificationHub>("/SignalR/NotificationHub");

#if DEBUG
app.Run();
#else
await app.RunAsync("http://0.0.0.0:5000");
#endif