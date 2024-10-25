using System;
using Fanex.TVS.AfterVoid.Website.Jobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TicketVoidingSystem.Services;

namespace Fanex.TVS.AfterVoid.Website.Controllers;

public class HomeController(ILogger<HomeController> logger) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var jobStatus =
            $"{{{nameof(AfterVoidJob)} OnProcessing: {AppSettingService.AppSetting.OnProcessingAfterVoid}}} - {DateTimeOffset.Now}";
        logger.LogInformation(jobStatus);
        return Ok("Ok-200-Fanex.TVS.AfterVoid.Service");
    }
}