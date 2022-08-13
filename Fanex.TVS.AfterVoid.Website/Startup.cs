using System.Diagnostics.CodeAnalysis;
using Fanex.TVS.AfterVoid.Service.Services;
using Fanex.TVS.AfterVoid.Website.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TicketVoidingSystem.Services;
using TicketVoidingSystem.SignalR;

namespace Fanex.TVS.AfterVoid.Website
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.LoadConfig();
            services.AddControllers();
            services.AddRazorPages().AddRazorRuntimeCompilation();
            services.AddHttpClient<TicketVoidingSystemHttpClientService, TicketVoidingSystemHttpClientServiceImp>();
            services.AddHttpClient<CashOutHttpClientService, CashOutHttpClientServiceImp>();
            services.RegisterFanexData(Configuration);
            services.RegisterONEbookUM(Configuration);
            services.RegisterSentry(Configuration);
            services.RegisterFanexCaching(Configuration);
            services.RegisterRemotingRef();
            services.RegisterMailService();
            services.RegisterLetstalk();
            services.RegisterAutoMapper();
            services.RegisterSignalR(Configuration);
            services.RegisterInternalServices();
            services.RegisterQuartzScheduler();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseHttpsRedirection();
            app.UseMiddleware<ExceptionHandlerMiddleware>();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapHub<NotificationHub>("/SignalR/NotificationHub");
            });
        }
    }
}