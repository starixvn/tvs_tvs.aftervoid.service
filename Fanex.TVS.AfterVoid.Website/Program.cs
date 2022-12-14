using Fanex.TVS.AfterVoid.Website.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TicketVoidingSystem.Services;
using TicketVoidingSystem.SignalR;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    const int port = 5000;
    options.ListenAnyIP(port); // to listen for incoming http connection on port 5000
});

// Add services to the container.
builder.Services.LoadConfig();
builder.Services.AddControllers();
builder.Services.AddHttpClient<TicketVoidingSystemHttpClientService, TicketVoidingSystemHttpClientServiceImp>();
builder.Services.AddHttpClient<CashOutHttpClientService, CashOutHttpClientServiceImp>();
builder.Services.RegisterFanexData(builder.Configuration);
builder.Services.RegisterFanexExceptionHandler();
builder.Services.RegisterSentry(builder.Configuration);
builder.Services.RegisterONEbookUM(builder.Configuration);
builder.Services.RegisterFanexCaching(builder.Configuration);
builder.Services.RegisterRemotingRef();
builder.Services.RegisterMailService();
builder.Services.RegisterLetstalk();
builder.Services.RegisterAutoMapper();
builder.Services.RegisterSignalR(builder.Configuration);
builder.Services.RegisterBusinessService();
builder.Services.RegisterQuartzScheduler();

//
var app = builder.Build();
app.UseDeveloperExceptionPage();
app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<NotificationHub>("/SignalR/NotificationHub");

app.Run();