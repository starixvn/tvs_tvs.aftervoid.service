using System;
using Fanex.TVS.AfterVoid.Website.Jobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Website.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> logger;

    public HomeController(ILogger<HomeController> logger)
    {
        this.logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var jobStatus =
            $"{{{nameof(AfterVoidJob)} OnProcessing: {AppSettingService.AppSetting.OnProcessingAfterVoid}}} - {DateTime.Now}";
        logger.LogInformation(jobStatus);
        return Ok("Ok-200-Fanex.TVS.AfterVoid.Service");
    }
}